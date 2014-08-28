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

/* The timple three address code is implemented as a doubly linked list of nodes
 * 
 */

namespace libtysila.timple
{
    public abstract class BaseNode
    {
        public abstract IList<BaseNode> Prev { get; }
        public abstract IList<BaseNode> Next { get; }
        public abstract ICollection<vara> uses { get; }
        public abstract ICollection<vara> defs { get; }

        public new BaseNode MemberwiseClone()
        {
            return (BaseNode)base.MemberwiseClone();
        }

        public abstract BaseNode InsertAfter(BaseNode new_node);
        public abstract void Remove(); 
    }

    public class TreeNode : BaseNode
    {
        public IList<BaseNode> prev = new List<BaseNode>();
        public IList<BaseNode> next = new List<BaseNode>();
        public bool visited = false;
        public override ICollection<vara> uses { get { return new vara[] { }; } }
        public override ICollection<vara> defs { get { return new vara[] { }; } }
        public ThreeAddressCode.Op Op;
        public BaseNode InnerNode;
        public override IList<BaseNode> Prev { get { return prev; } }
        public override IList<BaseNode> Next { get { return next; } }

        public override void Remove()
        {
            throw new NotImplementedException();
        }

        public override BaseNode InsertAfter(BaseNode new_node)
        {
            throw new NotImplementedException();
        }
    }

    public class ParentNode : TreeNode
    {
        public ParentNode(TreeNode n) { InnerNode = n; }

        public override string ToString()
        {
            return InnerNode.ToString();
        }
    }

    public class TimpleNode : TreeNode
    {
        public vara R, O1, O2;

        public TimpleNode(ThreeAddressCode.Op op, vara r, vara o1, vara o2)
        {
            Op = op;
            R = r;
            O1 = o1;
            O2 = o2;

            if (ThreeAddressCode.GetOpType(op) == ThreeAddressCode.OpType.AssignOp &&
                O1.DataType == Assembler.CliType.void_)
                O1.DataType = R.DataType;
        }

        protected TimpleNode() { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (R.VarType != vara.vara_type.Void)
            {
                sb.Append(R.ToString());
                sb.Append(" = ");
            }

            if (ThreeAddressCode.GetOpType(Op) == ThreeAddressCode.OpType.AssignOp)
                sb.Append(O1.ToString());
            else
            {
                sb.Append(Op.ToString());
                sb.Append("(");

                if (O1.VarType != vara.vara_type.Void)
                {
                    sb.Append(O1.ToString());

                    if (O2.VarType != vara.vara_type.Void)
                    {
                        sb.Append(", ");
                        sb.Append(O2.ToString());
                    }
                }

                sb.Append(")");
            }

            return sb.ToString();
        }

        public override ICollection<vara> defs
        {
            get
            {
                if (R.VarType == vara.vara_type.Logical)
                    return new vara[] { R };
                else
                    return new vara[] { };
            }
        }

        public override ICollection<vara> uses
        {
            get
            {
                List<vara> ret = new List<vara>();
                if (R.VarType == vara.vara_type.ContentsOf)
                    ret.Add(R);
                if((O1.VarType == vara.vara_type.Logical) || (O1.VarType == vara.vara_type.ContentsOf) || (O1.VarType == vara.vara_type.AddrOf))
                    ret.Add(vara.Logical(O1.LogicalVar, O1.SSA, O1.DataType));
                if((O2.VarType == vara.vara_type.Logical) || (O2.VarType == vara.vara_type.ContentsOf) || (O2.VarType == vara.vara_type.AddrOf))
                    ret.Add(vara.Logical(O2.LogicalVar, O2.SSA, O2.DataType));
                return ret;
            }
        }
    }

    public class TimpleLabelNode : TreeNode
    {
        public IList<TimplePhiInstNode> Phis = new List<TimplePhiInstNode>();

        public string Label;
        public int BlockId;

        public TimpleLabelNode(string label)
        {
            Label = label;
            BlockId = -1;
        }

        public TimpleLabelNode(int block_id)
        {
            Label = null;
            BlockId = block_id;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Label != null)
                sb.Append(Label + ":");
            else
                sb.Append("L" + BlockId.ToString() + ":");

            foreach (TimplePhiInstNode phi in Phis)
            {
                sb.Append(Environment.NewLine);
                sb.Append("  ");
                sb.Append(phi.ToString());
            }
            return sb.ToString();
        }

        public override ICollection<vara> defs
        {
            get
            {
                util.Set<vara> ret = new util.Set<vara>();
                foreach (TimplePhiInstNode phi in Phis)
                    ret.AddRange(phi.defs);
                return ret;
            }
        }

        public override ICollection<vara> uses
        {
            get
            {
                util.Set<vara> ret = new util.Set<vara>();
                foreach (TimplePhiInstNode phi in Phis)
                    ret.AddRange(phi.uses);
                return ret;
            }
        }
    }

    public class TimplePhiInstNode : TimpleNode
    {
        public Dictionary<TreeNode, vara> VarArgs;
        public TimplePhiInstNode(vara ret)
        {
            Op = Assembler.GetPhiTac(ret.DataType);
            R = ret;
            VarArgs = new Dictionary<TreeNode,vara>();
        }

        public override ICollection<vara> defs
        {
            get
            {
                return new vara[] { R };
            }
        }

        public override ICollection<vara> uses
        {
            get
            {
                util.Set<vara> ret = new util.Set<vara>();
                foreach (vara v in VarArgs.Values)
                    if (v.VarType != vara.vara_type.Const)
                        ret.Add(vara.Logical(v.LogicalVar, v.SSA, v.DataType));
                return ret;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(R.ToString());
            sb.Append(" = ");
            sb.Append(Op.ToString());
            sb.Append("(");

            int i = 0;
            foreach (vara v in VarArgs.Values)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(v.ToString());
                i++;
            }
            
            sb.Append(")");
            return sb.ToString();
        }
    }

    public class TimpleThrowBrNode : TimpleNode
    {
        public vara ThrowTarget;

        public TimpleThrowBrNode(ThreeAddressCode.Op op, vara o1, vara o2, vara target)
        {
            Op = op;
            R = vara.Void();
            O1 = o1;
            O2 = o2;
            ThrowTarget = target;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Op.ToString());
            sb.Append("(");
            sb.Append(ThrowTarget.ToString());
            sb.Append(": ");
            sb.Append(O1.ToString());
            sb.Append(", ");
            sb.Append(O2.ToString());
            sb.Append(")");
            return sb.ToString();
        }
    }

    public class TimpleBrNode : TimpleNode
    {
        public int BlockTargetTrue;
        public int BlockTargetFalse;

        public TimpleBrNode(ThreeAddressCode.Op op)
        {
            if (op.Operator != ThreeAddressCode.OpName.endfinally)
                throw new Exception("Need to specify a block number for any branch instruction other than endfinally");
        }

        public TimpleBrNode(int block_target)
        {
            Op = ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.br);
            BlockTargetTrue = block_target;
            O1 = vara.Void();
            O2 = vara.Void();
        }

        public TimpleBrNode(ThreeAddressCode.Op op, int block_target_true, int block_target_false, vara o1, vara o2)
        {
            Op = op;
            BlockTargetTrue = block_target_true;
            BlockTargetFalse = block_target_false;
            O1 = o1;
            O2 = o2;
        }

        public TimpleBrNode(ThreeAddressCode.Op op, TimpleLabelNode block_target_true, TimpleLabelNode block_target_false, vara o1, vara o2)
        {
            Op = op;
            BlockTargetTrue = block_target_true.BlockId;
            BlockTargetFalse = block_target_false.BlockId;
            O1 = o1;
            O2 = o2;
        }


        public TimpleBrNode(TimpleLabelNode tln)
        {
            Op = ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.br);
            BlockTargetTrue = tln.BlockId;
            O1 = vara.Void();
            O2 = vara.Void();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Op.ToString());

            if (Op.Operator == ThreeAddressCode.OpName.endfinally)
            {
                sb.Append("()");
                return sb.ToString();
            }

            sb.Append("(L");
            sb.Append(BlockTargetTrue.ToString());

            if(O1.VarType == vara.vara_type.Void)
            {
                sb.Append(")");
                return sb.ToString();
            }
            else
            {
                sb.Append(", L");
                sb.Append(BlockTargetFalse.ToString());
                sb.Append(": ");
                sb.Append(O1.ToString());
                sb.Append(", ");
                sb.Append(O2.ToString());
                sb.Append(")");
                return sb.ToString();
            }
        }

        public static ThreeAddressCode.Op InvertBr(ThreeAddressCode.Op op)
        {
            switch (op.Operator)
            {
                case ThreeAddressCode.OpName.ba:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bbe, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bae:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bb, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bb:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bae, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bbe:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.ba, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.beq:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bne, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bg:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.ble, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bge:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bl, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bl:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bge, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.ble:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.bg, op.Type, op.VT_Type);
                case ThreeAddressCode.OpName.bne:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.beq, op.Type, op.VT_Type);

                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class TimpleCallNode : TimpleNode
    {
        public IList<vara> VarArgs;
        public Signature.Method MethSig;
        public string CallConv;

        public TimpleCallNode(ThreeAddressCode.Op callop, vara r, vara target, IList<vara> var_args, Signature.Method msig, string callconv)
        {
            Op = callop;
            O1 = target;
            R = r;
            VarArgs = var_args;
            CallConv = callconv;
            MethSig = msig;
        }

        public TimpleCallNode(ThreeAddressCode.Op callop, vara r, vara target, IList<vara> var_args, Signature.Method msig)
            : this(callop, r, target, var_args, msig, "default")
        { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (R.VarType != vara.vara_type.Void)
            {
                sb.Append(R.ToString());
                sb.Append(" = ");
            }

            sb.Append(Op.ToString());
            sb.Append("(");
            sb.Append(O1.ToString());
            sb.Append(": ");

            for (int i = 0; i < VarArgs.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(VarArgs[i].ToString());
            }
            sb.Append(")");

            return sb.ToString();
        }

        public override ICollection<vara> uses
        {
            get
            {
                ICollection<vara> ret = base.uses;
                foreach (vara v in VarArgs)
                {
                    if ((v.VarType == vara.vara_type.Logical) || (v.VarType == vara.vara_type.ContentsOf) || (v.VarType == vara.vara_type.AddrOf))
                        ret.Add(vara.Logical(v.LogicalVar, v.SSA, v.DataType));
                }
                return ret;
            }
        }
    }
}
