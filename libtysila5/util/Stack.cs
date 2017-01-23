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


namespace libtysila5.util
{
    public class Stack<T> : List<T>
    {
        public void Push(T v)
        {
            Add(v);
        }

        public T Pop()
        {
            if (Count == 0)
                return default(T);
            var ret = this[Count - 1];
            RemoveAt(Count - 1);
            return ret;
        }

        public T Peek(int v = 0)
        {
            return this[Count - 1 - v];
        }

        public Stack() : base() {}

        public Stack(ICollection<T> other) : base(other.Count + 10)
        {
            foreach (var o in other)
                Add(o);
        }

        public Stack(IEnumerable<T> other) : base(other) { }
    }
}
