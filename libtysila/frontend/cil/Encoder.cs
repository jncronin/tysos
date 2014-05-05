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

namespace libtysila.frontend.cil
{
    public class Encoder
    {
        public static List<timple.TreeNode> Encode(CilGraph instrs, Assembler.MethodToCompile mtc, Assembler ass)
        {
            List<timple.TreeNode> ret = new List<timple.TreeNode>();
            int next_variable = 0;
            int next_block = 0;

            /* Assign local args and vars */
            List<Signature.Param> las = GetLocalArgs(mtc, ass);
            List<Signature.Param> lvs = GetLocalVars(mtc, ass);

            List<vara> la_vars = new List<vara>();
            foreach (Signature.Param la in las)
                la_vars.Add(vara.Logical(next_variable++, la.CliType(ass)));

            List<vara> lv_vars = new List<vara>();
            foreach (Signature.Param lv in lvs)
                lv_vars.Add(vara.Logical(next_variable++, lv.CliType(ass)));

            /* First encode each instruction */
            util.Set<CilNode> visited = new util.Set<CilNode>();
            foreach (CilNode start in instrs.Starts)
            {
                start.il.stack_before = new util.Stack<Signature.Param>();
                start.il.stack_vars_before = new util.Stack<vara>();
                DFEncode(start, mtc, ass, visited, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs);
            }

            /* Now loop through again and insert label nodes at the appropriate positions */
            visited.Clear();
            foreach (CilNode start in instrs.Starts)
            {
                DFInsertLabels(start, ref next_block, visited);
            }

            /* Finally loop through again and add the tacs to the output stream */
            foreach (CilNode n in instrs.LinearStream)
            {
                foreach (timple.TreeNode tac in n.il.tacs)
                {
                    tac.InnerNode = n;
                    ret.Add(tac);
                }
            }

            return ret;
        }

        private static void DFInsertLabels(CilNode n, ref int next_block, util.Set<CilNode> visited)
        {
            if (visited.Contains(n))
                return;
            visited.Add(n);

            /* If the nodes last instruction is a branch, check its successors for
             * Label nodes at the beginning, insert them if not, and patch up if 
             * necessary (patch successor index 0 for those with 1 successor, and 
             * index 1 for those with 2; the default here is to fall through)
             */

            if ((n.il.tacs.Count > 0) && (n.il.tacs[n.il.tacs.Count - 1] is timple.TimpleBrNode))
            {
                for (int i = 0; i < n.Next.Count; i++)
                {
                    int target_block_id;
                    CilNode next = n.Next[i] as CilNode;
                    if ((next.il.tacs.Count > 0) && (next.il.tacs[0] is timple.TimpleLabelNode))
                        target_block_id = ((timple.TimpleLabelNode)next.il.tacs[0]).BlockId;
                    else
                    {
                        target_block_id = next_block++;
                        next.il.tacs.Insert(0, new timple.TimpleLabelNode(target_block_id));
                    }

                    if (((n.Next.Count == 1) && (i == 0)) || (n.Next.Count == 2) && (i == 1))
                        ((timple.TimpleBrNode)n.il.tacs[n.il.tacs.Count - 1]).BlockTargetTrue = target_block_id;
                    if ((n.Next.Count == 2) && (i == 0))
                        ((timple.TimpleBrNode)n.il.tacs[n.il.tacs.Count - 1]).BlockTargetFalse = target_block_id;
                }
            }

            foreach (CilNode next in n.Next)
                DFInsertLabels(next, ref next_block, visited);
        }

        private static void DFEncode(CilNode n, Assembler.MethodToCompile mtc, Assembler ass, util.Set<CilNode> visited, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs)
        {
            if (visited.Contains(n))
                return;
            visited.Add(n);

            if (n.il.opcode.Encoder == null)
                throw new Exception("No encoding available for " + n.il.ToString());
            n.il.stack_after = new util.Stack<Signature.Param>(n.il.stack_before);
            n.il.stack_vars_after = new util.Stack<vara>(n.il.stack_vars_before);
            n.il.tacs = new List<timple.TreeNode>();
            if (n.Prev.Count == 0)
            {
                n.il.tacs.Add(new timple.TimpleLabelNode(next_block++));
                for (int i = 0; i < la_vars.Count; i++)
                    n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.localarg, la_vars[i], vara.Const(i, Assembler.CliType.int32), vara.Void()));
            }
            n.il.opcode.Encoder(n.il, ass, mtc, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs);
           

            foreach (CilNode next in n.Next)
            {
                if (next.il.stack_before == null)
                {
                    next.il.stack_before = new util.Stack<Signature.Param>(n.il.stack_after);
                    next.il.stack_vars_before = new util.Stack<vara>(n.il.stack_vars_after);
                }
                else
                {
                    // TODO: merge stacks
                    next.il.stack_before = new util.Stack<Signature.Param>(n.il.stack_after);
                    next.il.stack_vars_before = new util.Stack<vara>(n.il.stack_vars_after);
                }

                DFEncode(next, mtc, ass, visited, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs);
            }
        }

        private static List<Signature.Param> GetLocalArgs(Assembler.MethodToCompile mtc, Assembler ass)
        { return GetLocalArgs(mtc.msig, mtc.meth, mtc.tsig, mtc.msig, ass); }
        private static List<Signature.Param> GetLocalArgs(Signature.BaseMethod sig, Metadata.MethodDefRow mdr, Signature.BaseOrComplexType containing_type, Signature.BaseMethod containing_meth,
            Assembler ass)
        {
            Signature.Method meth = null;
            if (sig is Signature.Method)
                meth = sig as Signature.Method;
            else if (sig is Signature.GenericMethod)
                meth = ((Signature.GenericMethod)sig).GenMethod;

            Signature.BaseOrComplexType this_pointer = containing_type;

            Assembler.TypeToCompile this_ttc = new Assembler.TypeToCompile { _ass = ass, type = Metadata.GetTypeDef(containing_type, ass), tsig = new Signature.Param(containing_type, ass) };

            List<Signature.Param> las = new List<Signature.Param>();

            if (meth.HasThis && (!meth.ExplicitThis))
            {

                if (this_ttc.type.IsValueType(ass))
                {
                    /* Value types expect the this pointer to be a managed reference to an instance of the value type (CIL I:13.3) */
                    Signature.BaseOrComplexType this_bct = this_ttc.tsig.Type;
                    if (this_bct is Signature.BoxedType)
                        this_bct = ((Signature.BoxedType)this_bct).Type;
                    if (this_bct is Signature.ManagedPointer)
                        this_bct = ((Signature.ManagedPointer)this_bct).ElemType;
                    Signature.Param mptr_type = new Signature.Param(new Signature.ManagedPointer { ElemType = this_bct }, ass);
                    las.Add(mptr_type);
                }
                else
                {
                    las.Add(this_ttc.tsig);
                }
            }

            for (int i = 0; i < meth.Params.Count; i++)
            {
                Signature.Param p = meth.Params[i];
                Signature.Param p2 = Signature.ResolveGenericParam(p, containing_type, containing_meth, ass);
                las.Add(p2);
            }

            return las;
        }

        private static List<Signature.Param> GetLocalVars(Assembler.MethodToCompile mtc, Assembler ass)
        { return GetLocalVars(mtc.meth, mtc.tsig, mtc.msig, ass); }

        private static List<Signature.Param> GetLocalVars(Metadata.MethodDefRow meth, Signature.BaseOrComplexType containing_type, 
            Signature.BaseMethod containing_meth, Assembler ass)
        {
            List<Signature.Param> lvs = new List<Signature.Param>();

            Signature.LocalVars sig = meth.GetLocalVars(ass);
            int var_no = 0;
            foreach (Signature.Param p in sig.Vars)
                lvs.Add(Signature.ResolveGenericParam(p, containing_type, containing_meth, ass));

            return lvs;
        }

    }
}
