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

namespace libtysila.timple
{
    public class ConstantPropagation
    {
        public static void DoPropagation(TimpleGraph ssa, Liveness l, DomTree d)
        {
            /* Appel p.446 with modifications for variable folding and to update liveness analysis */

            util.Set<BaseNode> W = new util.Set<BaseNode>(ssa.LinearStream);

            while (W.Count > 0)
            {
                TreeNode S = W.ItemAtIndex(0) as TreeNode;
                W.Remove(S);

                bool has_const = false;
                object cval = null;

                if (S is TimpleLabelNode)
                {
                    TimpleLabelNode tln = S as TimpleLabelNode;
                    util.Set<TimplePhiInstNode> phi_to_remove = new util.Set<TimplePhiInstNode>();

                    foreach (TimplePhiInstNode phi in tln.Phis)
                    {
                        /* Strip out unneeded phi args */
                        util.Set<TreeNode> to_remove = new util.Set<TreeNode>();
                        foreach (TreeNode phi_n in phi.VarArgs.Keys)
                        {
                            if (!S.Prev.Contains(phi_n))
                                to_remove.Add(phi_n);
                        }
                        foreach (TreeNode phi_n in to_remove)
                            phi.VarArgs.Remove(phi_n);

                        /* Now determine if the instruction is of the form v = phi(c, c, c, ...) where
                         * c is a constant and all are the same */
                        bool all_equal = true;
                        object phi_cval = null;

                        foreach (vara a in phi.VarArgs.Values)
                        {
                            if (a.VarType == vara.vara_type.Const)
                            {
                                if (phi_cval == null)
                                    phi_cval = a.ConstVal;
                                else
                                {
                                    if (!phi_cval.Equals(a))
                                    {
                                        all_equal = false;
                                        break;
                                    }
                                }
                            }
                            else
                                all_equal = false;
                        }

                        if (all_equal && (phi_cval != null))
                        {
                            l.defs.Remove(phi.R);

                            List<BaseNode> remove_list = new List<BaseNode>();

                            foreach (BaseNode T in l.uses[phi.R])
                            {
                                Substitute(phi_cval, phi.R, (TreeNode)T, remove_list);
                                W.Add(T);
                            }

                            foreach (BaseNode T in remove_list)
                                l.uses[phi.R].Remove(T);

                            phi_to_remove.Add(phi);
                        }
                    }

                    foreach (TimplePhiInstNode phi_remove in phi_to_remove)
                        tln.Phis.Remove(phi_remove);

                    //if (tln.Phis.Count == 0)
                    //    ssa.RemoveNode(tln, l);
                }

                if(!has_const)
                    has_const = EvalConst(S, ref cval);

                if (has_const)
                {
                    //ssa.RemoveNode(S, l);

                    l.defs.Remove(((TimpleNode)S).R);

                    List<BaseNode> remove_list = new List<BaseNode>();

                    foreach (BaseNode T in l.uses[((TimpleNode)S).R])
                    {
                        Substitute(cval, ((TimpleNode)S).R, (TreeNode)T, remove_list);
                        W.Add(T);
                    }

                    foreach (BaseNode T in remove_list)
                        l.uses[((TimpleNode)S).R].Remove(T);

                    if (l.uses[((TimpleNode)S).R].Count == 0)
                        ssa.RemoveNode(S, l);
                }

                if (S is TimpleBrNode)
                {
                    TimpleBrNode S_cmpbr = S as TimpleBrNode;

                    if (ThreeAddressCode.GetOpType(((TreeNode)S).Op) == ThreeAddressCode.OpType.CmpBrOp)
                    {
                        bool pass = true;
                        has_const = EvalCmp(S_cmpbr, ref pass);

                        if (has_const)
                        {
                            BaseNode dest_break = S_cmpbr.next[pass ? 0 : 1];

                            ssa.RemoveEdge(S, dest_break as TreeNode);
                            ssa.RemoveNode(S, l);
                            W.Add(dest_break);
                        }
                    }
                }

                if ((S.prev.Count == 0) && !ssa.Starts.Contains(S))
                    ssa.RemoveNode(S, l);
            }

            /* Loop through again to remove all labels with no phis and only one prev and next */
            List<BaseNode> ns = new List<BaseNode>(ssa.LinearStream);
            foreach (BaseNode n in ns)
            {
                if (n is TimpleLabelNode)
                {
                    TimpleLabelNode tln = n as TimpleLabelNode;
                    if ((tln.Phis.Count) == 0 && (tln.prev.Count == 1) && (tln.next.Count == 1))
                        ssa.RemoveNode(tln, l);
                }
            }
        }

        private static bool EvalCmp(TimpleBrNode S_cmpbr, ref bool pass)
        {
            if (S_cmpbr.O1.VarType != vara.vara_type.Const)
                return false;
            if (S_cmpbr.O2.VarType != vara.vara_type.Const)
                return false;

            switch (S_cmpbr.Op)
            {
                case ThreeAddressCode.Op.bge_i4:
                    pass = (int)S_cmpbr.O1.ConstVal >= (int)S_cmpbr.O2.ConstVal;
                    return true;
            }

            return false;
        }

        private static void Substitute(object cval, vara vara, TreeNode n, IList<BaseNode> remove_list)
        {
            if (n is TimpleLabelNode)
            {
                TimpleLabelNode tln = n as TimpleLabelNode;

                foreach (TimplePhiInstNode phi in tln.Phis)
                {
                    List<TreeNode> tns = new List<TreeNode>(phi.VarArgs.Keys);

                    foreach (TreeNode tn in tns)
                    {
                        if (phi.VarArgs[tn].Equals(vara))
                        {
                            phi.VarArgs[tn] = vara.Const(cval, phi.VarArgs[tn].DataType);
                            remove_list.Add(n);
                        }
                    }
                }
            }
            else if (n is TimpleCallNode)
            {
                TimpleCallNode tcn = n as TimpleCallNode;

                if (tcn.O1.Equals(vara))
                {
                    tcn.O1 = vara.Const(cval, tcn.O1.DataType);
                    remove_list.Add(n);
                }

                for (int i = 0; i < tcn.VarArgs.Count; i++)
                {
                    if (tcn.VarArgs[i].Equals(vara))
                    {
                        tcn.VarArgs[i] = vara.Const(cval, tcn.VarArgs[i].DataType);
                        remove_list.Add(n);
                    }
                }
            }
            else if (n is TimpleNode)
            {
                TimpleNode tn = n as TimpleNode;

                if (tn.O1.Equals(vara))
                {
                    tn.O1 = vara.Const(cval, tn.O1.DataType);
                    remove_list.Add(n);
                }
                if (tn.O2.Equals(vara))
                {
                    tn.O2 = vara.Const(cval, tn.O2.DataType);
                    remove_list.Add(n);
                }
            }
        }

        private static bool EvalConst(TreeNode S, ref object cval)
        {
            if (S is TimpleNode)
            {
                TimpleNode tn = S as TimpleNode;

                switch (tn.Op)
                {
                    case ThreeAddressCode.Op.assign_i4:
                    case ThreeAddressCode.Op.assign_i:
                    case ThreeAddressCode.Op.assign_i8:
                    case ThreeAddressCode.Op.assign_r4:
                    case ThreeAddressCode.Op.assign_r8:
                    case ThreeAddressCode.Op.assign_vt:
                        if (tn.O1.VarType == vara.vara_type.Const)
                        {
                            cval = tn.O1.ConstVal;
                            return true;
                        }
                        break;

                    case ThreeAddressCode.Op.add_i4:
                        if (tn.O1.VarType == vara.vara_type.Const && tn.O2.VarType == vara.vara_type.Const)
                        {
                            cval = (int)tn.O1.ConstVal + (int)tn.O2.ConstVal;
                            return true;
                        }
                        break;
                    case ThreeAddressCode.Op.add_i8:
                        if (tn.O1.VarType == vara.vara_type.Const && tn.O2.VarType == vara.vara_type.Const)
                        {
                            cval = (long)tn.O1.ConstVal + (long)tn.O2.ConstVal;
                            return true;
                        }
                        break;
                }
            }

            return false;
        }
    }
}
