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

namespace libtysila.tybel
{
    public abstract class Node : timple.BaseNode
    {
        public IList<timple.BaseNode> next, prev;
        public override IList<timple.BaseNode> Next { get { return next; } }
        public override IList<timple.BaseNode> Prev { get { return prev; } }
        public timple.TreeNode TimpleInst;

        public abstract IList<byte> Assemble();

        public abstract bool IsMove { get; }
    }

    public class LabelNode : Node
    {
        public string Label;
        public override string ToString()
        {
            return Label + ":";
        }

        public LabelNode(string label) { Label = label; }

        public override IList<byte> Assemble()
        {
            return new byte[] { };
        }

        public override ICollection<vara> defs
        {
            get { return new vara[] { }; }
        }

        public override ICollection<vara> uses
        {
            get { return new vara[] { }; }
        }

        public override bool IsMove
        {
            get { return false; }
        }
    }
}

namespace libtysila
{
    partial class Assembler
    {
        public abstract IList<tybel.Node> SelectInstruction(timple.TreeNode inst, ref int next_var);
    }
}
