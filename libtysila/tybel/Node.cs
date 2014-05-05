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

namespace libtysila.tybel
{
    public abstract class Node : timple.BaseNode
    {
        public IList<timple.BaseNode> next, prev;
        public override IList<timple.BaseNode> Next { get { return next; } }
        public override IList<timple.BaseNode> Prev { get { return prev; } }
        public timple.TreeNode TimpleInst;
        public virtual IList<vara> VarList { get { return new vara[] { }; } set { throw new NotSupportedException(); } }

        public abstract IEnumerable<libasm.OutputBlock> Assemble(Assembler ass);

        public abstract bool IsMove { get; }
    }

    public class SpecialNode : Node
    {
        public enum SpecialNodeType { SaveLive, Restore, SaveLiveExcept, SaveLiveIntersect, };
        public SpecialNodeType Type;
        IList<vara> varList;
        public Node SaveNode;
        public override IList<vara> VarList
        {
            get { return varList; }
            set { varList = value; }
        }

        public int IntVal;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Type.ToString());

            switch (Type)
            {
                case SpecialNodeType.SaveLiveExcept:
                case SpecialNodeType.SaveLiveIntersect:
                    sb.Append(" ");
                    for (int i = 0; i < VarList.Count; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append(VarList[i].ToString());
                    }
                    break;
            }

            return sb.ToString();
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

        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass)
        {
            throw new NotImplementedException();
        }
    }

    public class LabelNode : Node
    {
        public string Label;
        public bool Local;
        public override string ToString()
        {
            return Label + ":";
        }

        public LabelNode(string label, bool local) { Label = label; Local = local; }

        public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass)
        {
            if (Local)
                return new libasm.OutputBlock[] { new libasm.LocalSymbol(Label, false) };
            else
                return new libasm.OutputBlock[] { new libasm.ExportedSymbol(Label, false, true) };
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
        public abstract IList<tybel.Node> SelectInstruction(timple.TreeNode inst, ref int next_var, IList<libasm.hardware_location> las);
    }
}
