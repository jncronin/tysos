/* Copyright (C) 2014 by John Cronin
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
using System.Text;

namespace libtysila.frontend.cil
{
    public class CilNode : timple.BaseNode
    {
        IList<timple.BaseNode> prev = new List<timple.BaseNode>();
        IList<timple.BaseNode> next = new List<timple.BaseNode>();
        public InstructionLine il;
        public int il_label;

        internal Stack stack_vars_before, stack_vars_after;
        internal util.Stack<Signature.Param> stack_before, stack_after;

        public List<CilNode> replaced_by = null;

        public Metadata.MethodBody.EHClause ehclause_start;

        public override IList<timple.BaseNode> Prev
        {
            get { return prev; }
            set { prev = value; }
        }

        public override IList<timple.BaseNode> Next
        {
            get { return next; }
            set { next = value; }
        }

        public override ICollection<vara> uses
        {
            get { throw new NotImplementedException(); }
        }

        public override ICollection<vara> defs
        {
            get { throw new NotImplementedException(); }
        }

        public override string ToString()
        {
            return il.ToString();
        }

        public override timple.BaseNode InsertAfter(timple.BaseNode new_node)
        {
            new_node.Next.Clear();
            new_node.Prev.Clear();

            foreach (timple.BaseNode next in this.Next)
            {
                new_node.Next.Add(next);
                next.Prev.Remove(this);
                next.Prev.Add(new_node);
            }
            this.Next.Clear();
            this.Next.Add(new_node);

            new_node.Prev.Add(this);

            return new_node;
        }

        public override void Remove()
        {
            if (Next.Count > 1 && Prev.Count > 1)
                throw new Exception("Cannot remove node with more than one predecessors and successors");

            foreach (timple.BaseNode next in Next)
            {
                next.Prev.Remove(this);
                foreach (timple.BaseNode prev in Prev)
                    next.Prev.Add(prev);
            }

            foreach (timple.BaseNode prev in Prev)
            {
                prev.Next.Remove(this);
                foreach (timple.BaseNode next in Next)
                    prev.Next.Add(next);
            }
        }
    }
}
