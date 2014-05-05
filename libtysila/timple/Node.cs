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

                    sb.Append(")");
                }
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

    public class TimpleBrNode : TimpleNode
    {
        public int BlockTargetTrue;
        public int BlockTargetFalse;

        public TimpleBrNode(int block_target)
        {
            Op = ThreeAddressCode.Op.br;
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
            Op = ThreeAddressCode.Op.br;
            BlockTargetTrue = tln.BlockId;
            O1 = vara.Void();
            O2 = vara.Void();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Op.ToString());
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
            switch (op)
            {
                case ThreeAddressCode.Op.ba_i:
                    return ThreeAddressCode.Op.bbe_i;
                case ThreeAddressCode.Op.ba_i4:
                    return ThreeAddressCode.Op.bbe_i4;
                case ThreeAddressCode.Op.ba_i8:
                    return ThreeAddressCode.Op.bbe_i8;
                case ThreeAddressCode.Op.ba_r4:
                    return ThreeAddressCode.Op.bbe_r4;
                case ThreeAddressCode.Op.ba_r8:
                    return ThreeAddressCode.Op.bbe_r8;

                case ThreeAddressCode.Op.bae_i:
                    return ThreeAddressCode.Op.bb_i;
                case ThreeAddressCode.Op.bae_i4:
                    return ThreeAddressCode.Op.bb_i4;
                case ThreeAddressCode.Op.bae_i8:
                    return ThreeAddressCode.Op.bb_i8;
                case ThreeAddressCode.Op.bae_r4:
                    return ThreeAddressCode.Op.bb_r4;
                case ThreeAddressCode.Op.bae_r8:
                    return ThreeAddressCode.Op.bb_r8;

                case ThreeAddressCode.Op.bb_i:
                    return ThreeAddressCode.Op.bae_i;
                case ThreeAddressCode.Op.bb_i4:
                    return ThreeAddressCode.Op.bae_i4;
                case ThreeAddressCode.Op.bb_i8:
                    return ThreeAddressCode.Op.bae_i8;
                case ThreeAddressCode.Op.bb_r4:
                    return ThreeAddressCode.Op.bae_r4;
                case ThreeAddressCode.Op.bb_r8:
                    return ThreeAddressCode.Op.bae_r8;

                case ThreeAddressCode.Op.bbe_i:
                    return ThreeAddressCode.Op.ba_i;
                case ThreeAddressCode.Op.bbe_i4:
                    return ThreeAddressCode.Op.ba_i4;
                case ThreeAddressCode.Op.bbe_i8:
                    return ThreeAddressCode.Op.ba_i8;
                case ThreeAddressCode.Op.bbe_r4:
                    return ThreeAddressCode.Op.ba_r4;
                case ThreeAddressCode.Op.bbe_r8:
                    return ThreeAddressCode.Op.ba_r8;

                case ThreeAddressCode.Op.beq_i:
                    return ThreeAddressCode.Op.bne_i;
                case ThreeAddressCode.Op.beq_i4:
                    return ThreeAddressCode.Op.bne_i4;
                case ThreeAddressCode.Op.beq_i8:
                    return ThreeAddressCode.Op.bne_i8;
                case ThreeAddressCode.Op.beq_r4:
                    return ThreeAddressCode.Op.bne_r4;
                case ThreeAddressCode.Op.beq_r8:
                    return ThreeAddressCode.Op.bne_r8;

                case ThreeAddressCode.Op.bg_i:
                    return ThreeAddressCode.Op.ble_i;
                case ThreeAddressCode.Op.bg_i4:
                    return ThreeAddressCode.Op.ble_i4;
                case ThreeAddressCode.Op.bg_i8:
                    return ThreeAddressCode.Op.ble_i8;
                case ThreeAddressCode.Op.bg_r4:
                    return ThreeAddressCode.Op.ble_r4;
                case ThreeAddressCode.Op.bg_r8:
                    return ThreeAddressCode.Op.ble_r8;

                case ThreeAddressCode.Op.bge_i:
                    return ThreeAddressCode.Op.bl_i;
                case ThreeAddressCode.Op.bge_i4:
                    return ThreeAddressCode.Op.bl_i4;
                case ThreeAddressCode.Op.bge_i8:
                    return ThreeAddressCode.Op.bl_i8;
                case ThreeAddressCode.Op.bge_r4:
                    return ThreeAddressCode.Op.bl_r4;
                case ThreeAddressCode.Op.bge_r8:
                    return ThreeAddressCode.Op.bl_r8;

                case ThreeAddressCode.Op.bl_i:
                    return ThreeAddressCode.Op.bge_i;
                case ThreeAddressCode.Op.bl_i4:
                    return ThreeAddressCode.Op.bge_i4;
                case ThreeAddressCode.Op.bl_i8:
                    return ThreeAddressCode.Op.bge_i8;
                case ThreeAddressCode.Op.bl_r4:
                    return ThreeAddressCode.Op.bge_r4;
                case ThreeAddressCode.Op.bl_r8:
                    return ThreeAddressCode.Op.bge_r8;

                case ThreeAddressCode.Op.ble_i:
                    return ThreeAddressCode.Op.bg_i;
                case ThreeAddressCode.Op.ble_i4:
                    return ThreeAddressCode.Op.bg_i4;
                case ThreeAddressCode.Op.ble_i8:
                    return ThreeAddressCode.Op.bg_i8;
                case ThreeAddressCode.Op.ble_r4:
                    return ThreeAddressCode.Op.bg_r4;
                case ThreeAddressCode.Op.ble_r8:
                    return ThreeAddressCode.Op.bg_r8;

                case ThreeAddressCode.Op.bne_i:
                    return ThreeAddressCode.Op.beq_i;
                case ThreeAddressCode.Op.bne_i4:
                    return ThreeAddressCode.Op.beq_i4;
                case ThreeAddressCode.Op.bne_i8:
                    return ThreeAddressCode.Op.beq_i8;
                case ThreeAddressCode.Op.bne_r4:
                    return ThreeAddressCode.Op.beq_r4;
                case ThreeAddressCode.Op.bne_r8:
                    return ThreeAddressCode.Op.beq_r8;

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
