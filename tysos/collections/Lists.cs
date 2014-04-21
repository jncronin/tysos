/* Copyright (C) 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/* Pretty much a direct copy of the implementations from my old C++ kernel,
 * tysos-cxx (see https://github.com/jncronin/Tysos-cxx)
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.Collections
{
    class LinkedList<T> : IList<T> where T : class
    {
        internal protected class Node
        {
            public T item;
            public Node next, prev;

            public Node(T i) { item = i; next = null; prev = null; }
        }

        internal protected Node f, l;
        protected int count;

        protected virtual void _insertbef(Node cur, Node n)
        {
            if (cur.prev != null)
            {
                cur.prev.next = n;
                n.prev = cur.prev;
            }
            else
            {
                n.prev = null;
                f = n;
            }

            cur.prev = n;
            n.next = cur;
            count++;
        }

        protected virtual void _insertend(Node n)
        {
            if (l == null)
                f = l = n;
            else
            {
                n.prev = l;
                l.next = n;
                l = n;
            }
            count++;
        }

        protected virtual int _find(T i) { Node n; return _find(i, out n); }
        protected virtual int _find(T i, out Node n)
        {
            Node c = f;
            int index = 0;

            while (c != null)
            {
                if (i == c.item)
                {
                    n = c;
                    return index;
                }

                c = c.next;
                index++;
            }

            n = null;
            return -1;
        }

        protected virtual Node _item(int index)
        {
            Node c = f;

            while (c != null)
            {
                if (index == 0)
                    return c;
                c = c.next;
                index--;
            }

            return null;
        }

        protected virtual void _remove(Node n)
        {
            if (n == null)
                return;

            if (n.prev != null)
                n.prev.next = n.next;
            else
                f = n.next;

            if (n.next != null)
                n.next.prev = n.prev;
            else
                l = n.prev;

            count--;
        }

        public int IndexOf(T item)
        {
            return _find(item);
        }

        public void Insert(int index, T item)
        {
            Node next = _item(index);
            Node n = new Node(item);

            if (next != null)
                _insertbef(next, n);
            else
                _insertend(n);
        }

        public void RemoveAt(int index)
        {
            _remove(_item(index));
        }

        public T this[int index]
        {
            get
            {
                Node n = _item(index);
                if (n == null)
                    throw new IndexOutOfRangeException();
                return n.item;
            }
            set
            {
                Node n = _item(index);
                if (n == null)
                    throw new IndexOutOfRangeException();
                n.item = value;
            }
        }

        public void Add(T item)
        {
            Node n = new Node(item);
            _insertend(n);
        }

        public void Clear()
        {
            while (l != null)
                _remove(l);
        }

        public bool Contains(T item)
        {
            if (IndexOf(item) >= 0)
                return true;
            else
                return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Node c = f;
            int index = arrayIndex;

            while (c != null)
            {
                array[index] = c.item;
                index++;
                c = c.next;
            }
        }

        public int Count
        {
            get { return count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            Node n;

            if (_find(item, out n) < 0)
                return false;
            _remove(n);
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    class Queue<T> : LinkedList<T> where T : class 
    {
        public virtual T GetFirst() { return GetFirst(true); }
        public virtual T GetFirst(bool remove)
        {
            T ret = null;

            if (f != null)
            {
                ret = f.item;
                if (remove)
                    _remove(f);
            }
            return ret;
        }
    }

    class DeltaQueue<T> : Queue<T> where T : class
    {
        protected class DeltaNode : LinkedList<T>.Node
        {
            public long d;

            public DeltaNode(T item, long delta) : base(item) { d = delta; }
        }

        protected virtual long _sumto(DeltaNode n)
        {
            DeltaNode c = f as DeltaNode;
            long sum = 0;

            while (c != null)
            {
                sum += c.d;

                if (c == n)
                    return sum;

                c = c.next as DeltaNode;
            }

            return sum;
        }

        protected override void _remove(Node n)
        {
            if (n == null)
                return;

            if (n.prev != null)
                n.prev.next = n.next;
            else
                f = n.next;

            if (n.next != null)
            {
                n.next.prev = n.prev;

                // Add the delta of the node to be removed onto the next one in the list
                ((DeltaNode)n.next).d += ((DeltaNode)n).d;
            }
            else
                l = n.prev;

            count--;
        }

        public virtual void Add(T item, long value) { Add(item, value, false); }
        public virtual void Add(T item, long value, bool absolute)
        {
            if (absolute)
                value -= _sumto(l as DeltaNode);

            if (value <= 0)
                value = 0;

            DeltaNode n = new DeltaNode(item, value);
            _insertend(n);
        }

        public virtual void InsertAtDelta(T item, long value)
        {
            DeltaNode c = f as DeltaNode;
            DeltaNode n = new DeltaNode(item, value);

            while (c != null)
            {
                if (c.next != null)
                {
                    if (value <= ((DeltaNode)c.next).d)
                    {
                        n.d = value;
                        _insertbef(c.next, n);
                        ((DeltaNode)c.next).d -= value;
                        return;
                    }
                }

                value -= c.d;
                c = c.next as DeltaNode;
            }

            n.d = value;
            _insertend(n);
        }

        /** <summary>Return the next entry with delta of 0 </summary> */
        public virtual T GetZero()
        {
            T ret = null;

            if (f == null)
                return null;

            if (((DeltaNode)f).d == 0)
            {
                ret = f.item;
                _remove(f);
            }
            return ret;
        }

        public virtual long DecreaseDelta(long decval)
        {
            DeltaNode c = f as DeltaNode;

            while (c != null)
            {
                if (c.d >= decval)
                {
                    c.d -= decval;
                    return 0;
                }

                decval -= c.d;
                c.d = 0;
                c = c.next as DeltaNode;
            }

            return decval;
        }
    }
}
