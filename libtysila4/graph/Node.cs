/* Copyright (C) 2016 by John Cronin
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libtysila4.graph
{
    public abstract class NodeContents
    {
        public BaseNode n;
    }

    public abstract class BaseNode : IEquatable<BaseNode>
    {
        public NodeContents c
        {
            get { return _c; }
            set { _c = value; if (c != null) c.n = this; }
        }
        NodeContents _c;

        public abstract IEnumerable<BaseNode> Prev { get; }
        public abstract IEnumerable<BaseNode> Next { get; }

        public virtual void SetDefaultNext(BaseNode node)
        { AddNext(node); }

        public abstract void AddNext(BaseNode node);
        public abstract void RemoveNext(BaseNode node);
        public abstract void AddPrev(BaseNode node);
        public abstract void RemovePrev(BaseNode node);

        public abstract bool IsMulti();
        public abstract int NextCount { get; }
        public abstract int PrevCount { get; }
        public abstract BaseNode Next1 { get; }
        public abstract BaseNode Prev1 { get; }
        public virtual BaseNode Next2 { get { return null; } }
        public virtual BaseNode Prev2 { get { return null; } }

        public abstract void ReplacePrev(BaseNode old_node, BaseNode new_node);
        public abstract void ReplaceNext(BaseNode old_node, BaseNode new_node);

        public override string ToString()
        {
            if (_c == null)
                return "bb" + bb.ToString();
            else
                return _c.ToString();
        }

        public Graph g;
        public int bb;

        public metadata.ExceptionHeader ehdr;

        public bool Equals(BaseNode other)
        {
            return this == other;
        }
    }

    public class Node : BaseNode
    {
        public BaseNode p, n;

        public override IEnumerable<BaseNode> Prev { get { if (p == null) yield break; else yield return p; } }
        public override IEnumerable<BaseNode> Next { get { if (n == null) yield break; else yield return n; } }

        public override void AddNext(BaseNode node) { n = node; }
        public override void RemoveNext(BaseNode node) { if(node == n) n = null; }
        public override void AddPrev(BaseNode node) { p = node; }
        public override void RemovePrev(BaseNode node) { if (node == p) p = null; }
        public override bool IsMulti() { return false; }
        public override int NextCount { get { if (n == null) return 0; return 1; } }
        public override int PrevCount { get { if (p == null) return 0; return 1; } }
        public override BaseNode Next1 { get { return n; } }
        public override BaseNode Prev1 { get { return p; } }

        public override void ReplacePrev(BaseNode old_node, BaseNode new_node)
        {
            if (p.Equals(old_node))
                p = new_node;
        }
        public override void ReplaceNext(BaseNode old_node, BaseNode new_node)
        {
            if (n.Equals(old_node))
                n = new_node;
        }
    }

    public class MultiNode : BaseNode
    {
        public List<BaseNode> p = new List<BaseNode>();
        public List<BaseNode> n = new List<BaseNode>();

        public override IEnumerable<BaseNode> Prev { get { return p; } }
        public override IEnumerable<BaseNode> Next { get { return n; } }

        public override void AddNext(BaseNode node) { n.Add(node); }
        public override void RemoveNext(BaseNode node) { if (n.Contains(node)) n.Remove(node); }
        public override void AddPrev(BaseNode node) { p.Add(node); }
        public override void RemovePrev(BaseNode node) { if (p.Contains(node)) p.Remove(node); }
        public override bool IsMulti() { return true; }
        public override int NextCount { get { return n.Count; } }
        public override int PrevCount { get { return p.Count; } }
        public override BaseNode Next1 { get { return n[0]; } }
        public override BaseNode Prev1 { get { return p[0]; } }
        public override BaseNode Next2 { get { return n[1]; } }
        public override BaseNode Prev2 { get { return p[1]; } }

        public override void SetDefaultNext(BaseNode node)
        {
            n.Insert(0, node);
        }

        public override void ReplacePrev(BaseNode old_node, BaseNode new_node)
        {
            for(int i = 0; i < p.Count; i++)
            {
                if (p[i].Equals(old_node))
                    p[i] = new_node;
            }
        }
        public override void ReplaceNext(BaseNode old_node, BaseNode new_node)
        {
            for (int i = 0; i < n.Count; i++)
            {
                if (n[i].Equals(old_node))
                    n[i] = new_node;
            }
        }
    }
}
