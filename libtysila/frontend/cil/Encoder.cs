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
        public class EncoderState
        {
            public List<Signature.Param> las, lvs;
            public List<libasm.hardware_location> la_locs, lv_locs;
            internal Stack lv_stack, la_stack;
            public int next_blk = 0;
            public CallConv cc;
            public Dictionary<int, CilNode> offset_map = new Dictionary<int, CilNode>();
            public int largest_var_stack = 0;
            public Assembler.MethodToCompile mtc;
            public util.Set<libasm.hardware_location> used_locs = new util.Set<libasm.hardware_location>();
        }

        public static List<tybel.Node> Encode(CilGraph instrs, Assembler.MethodToCompile mtc, Assembler ass,
            Assembler.MethodAttributes attrs)
        {
            List<tybel.Node> ret = new List<tybel.Node>();
            EncoderState state = new EncoderState();
            state.las = GetLocalArgs(mtc, ass);
            state.lvs = GetLocalVars(mtc, ass);
            int next_block = 0;

            state.cc = ass.call_convs[attrs.call_conv](mtc, CallConv.StackPOV.Callee, ass);
            attrs.cc = state.cc;
            state.mtc = mtc;

            /* For now, have all local args and vars be on the stack */
            Stack arg_stack = ass.GetStack(libasm.hardware_stackloc.StackType.Arg);
            state.la_locs = new List<libasm.hardware_location>();
            state.la_stack = arg_stack;
            foreach (Signature.Param la in state.las)
                state.la_locs.Add(arg_stack.GetAddressFor(la, ass));

            Stack var_stack = ass.GetStack(libasm.hardware_stackloc.StackType.LocalVar);
            state.lv_locs = new List<libasm.hardware_location>();
            state.lv_stack = var_stack;
            foreach (Signature.Param lv in state.lvs)
                state.lv_locs.Add(var_stack.GetAddressFor(lv, ass));

            state.used_locs.AddRange(state.la_locs);
            state.used_locs.AddRange(state.lv_locs);

            /* Assign a label to each cil instruction */
            state.next_blk = 1;
            foreach (CilNode n in instrs.LinearStream)
            {
                n.il_label = state.next_blk++;
                state.offset_map[n.il.il_offset] = n;
            }

            /* Encode the instructions */
            util.Set<CilNode> visited = new util.Set<CilNode>();
            foreach (CilNode start in instrs.Starts)
            {
                start.stack_vars_before = ass.GetStack();
                start.stack_before = new util.Stack<Signature.Param>();

                if (start.ehclause_start != null)
                {
                    if (start.ehclause_start.IsCatch || start.ehclause_start.IsFault)
                    {
                        Signature.Param except_obj_type = Metadata.GetTTC(start.ehclause_start.ClassToken, mtc.GetTTC(ass), mtc.msig, ass).tsig;
                        start.stack_vars_before.GetAddressFor(except_obj_type, ass);
                        start.stack_before.Push(except_obj_type);
                    }
                }
                DFEncode(start, mtc, ass, visited, ref next_block, state, attrs);
            }

            /* Reorder the linear stream for rewritten nodes */
            for (int i = 0; i < instrs.linear_stream.Count; i++)
            {
                if (((CilNode)instrs.linear_stream[i]).replaced_by != null)
                {
                    CilNode old = instrs.linear_stream[i] as CilNode;

                    instrs.linear_stream.RemoveAt(i);
                    foreach (CilNode repl in old.replaced_by)
                    {
                        repl.il_label = state.next_blk++;
                        instrs.linear_stream.Insert(i++, repl);
                    }

                    state.offset_map[old.il.il_offset] = old.replaced_by[0];
                }
            }

            /* Add the method prefix */
            ass.Enter(state, attrs, ret);

            /* Store callee-saved registers */
            ret.Add(new tybel.SpecialNode { Type = tybel.SpecialNode.SpecialNodeType.SaveCalleeSaved, Val = state.used_locs });

            /* Add to code to initialize local vars */
            if (mtc.meth.Body.InitLocals)
            {
                for (int i = 0; i < state.lv_locs.Count; i++)
                {
                    Stack vs = ass.GetStack();
                    util.Stack<Signature.Param> ps = new util.Stack<Signature.Param>();
                    CilNode c1 = new CilNode { stack_vars_before = vs, stack_before = ps, il_label = -1, il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldloca_s)], inline_int = i } };
                    DFEncode(c1, mtc, ass, visited, ref next_block, state, attrs);
                    CilNode c2 = new CilNode { stack_vars_before = c1.stack_vars_after, stack_before = c1.stack_after, il_label = -1, il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.initobj)], inline_tok = new TTCToken { ttc = Metadata.GetTTC(state.lvs[i], mtc.GetTTC(ass), mtc.msig, ass) } } };
                    DFEncode(c2, mtc, ass, visited, ref next_block, state, attrs);

                    ret.AddRange(c1.il.tybel);
                    ret.AddRange(c2.il.tybel);
                }
            }

            /* Finally loop through again and add the tacs to the output stream */
            foreach (CilNode n in instrs.LinearStream)
            {
                bool is_start = false;
                if (instrs.Starts.Contains(n))
                    is_start = true;
                foreach (tybel.Node tac in n.il.tybel)
                {
                    if (is_start)
                    {
                        tac.IsStart = true;
                        is_start = false;
                    }
                    tac.InnerNode = n;
                    ret.Add(tac);
                }
            }

            return ret;
        }

        private static void DFEncode(CilNode n, Assembler.MethodToCompile mtc, Assembler ass, util.Set<CilNode> visited, ref int next_block,
            EncoderState state, Assembler.MethodAttributes attrs)
        {
            if (visited.Contains(n))
                return;
            visited.Add(n);


            // TODO
            CilNode new_n = DecomposeComplexOpcodes.DecomposeComplexOpts(n, ass, mtc, ref state.next_blk, attrs);

            if (new_n != n)
            {
                new_n.il_label = n.il_label;
                DFEncode(new_n, mtc, ass, visited, ref next_block, state, attrs);
                return;
            }

            if (n.il.opcode.TybelEncoder == null)
                throw new Exception("No encoding available for " + n.il.ToString());
            n.stack_vars_after = n.stack_vars_before.Clone();
            n.stack_after = new util.Stack<Signature.Param>(n.stack_before);
            n.il.tybel = new List<tybel.Node>();

            if(n.il_label >= 0)
                n.il.tybel.Add(new tybel.LabelNode("L" + n.il_label.ToString(), true));
            n.il.tybel.Add(new tybel.CilNode { Node = n });

            if (n.stack_vars_before.ByteSize > state.largest_var_stack)
                state.largest_var_stack = n.stack_vars_before.ByteSize;
            n.il.opcode.TybelEncoder(n, ass, mtc, ref state.next_blk, state, attrs);
            if (n.stack_vars_after.ByteSize > state.largest_var_stack)
                state.largest_var_stack = n.stack_vars_after.ByteSize;

            if(n.stack_vars_before.UsedLocations != null)
                state.used_locs.AddRange(n.stack_vars_before.UsedLocations);
            if(n.stack_vars_after.UsedLocations != null)
                state.used_locs.AddRange(n.stack_vars_after.UsedLocations);

            util.Set<timple.BaseNode> next_visited = new util.Set<timple.BaseNode>();

            while (true)
            {
                CilNode next = null;
                for (int i = 0; i < n.Next.Count; i++)
                {
                    if (!next_visited.Contains(n.Next[i]))
                    {
                        next = n.Next[i] as CilNode;
                        next_visited.Add(next);
                        break;
                    }
                }
                if (next == null)
                    break;

                if (next.stack_vars_before == null)
                {
                    next.stack_before = new util.Stack<Signature.Param>(n.stack_after);
                    next.stack_vars_before = n.stack_vars_after.Clone();
                }
                else
                {
                    // TODO: merge stacks
                    next.stack_before = new util.Stack<Signature.Param>(n.stack_after);
                    next.stack_vars_before = n.stack_vars_after.Clone();
                }

                DFEncode(next, mtc, ass, visited, ref next_block, state, attrs);
            }
        }


        public static List<timple.TreeNode> Encode2(CilGraph instrs, Assembler.MethodToCompile mtc, Assembler ass,
            Assembler.MethodAttributes attrs, out int next_var, out int next_blk)
        {
            List<timple.TreeNode> ret = new List<timple.TreeNode>();
            int next_variable = 0;
            int next_block = 0;

            /* Assign local args and vars */
            List<Signature.Param> las = GetLocalArgs(mtc, ass);
            List<Signature.Param> lvs = GetLocalVars(mtc, ass);

            List<vara> la_vars = new List<vara>();
            foreach (Signature.Param la in las)
                la_vars.Add(vara.Logical(next_variable++, la.CliType(ass), la));

            List<vara> lv_vars = new List<vara>();
            foreach (Signature.Param lv in lvs)
                lv_vars.Add(vara.Logical(next_variable++, lv.CliType(ass), lv));

            /* First encode each instruction */
            util.Set<CilNode> visited = new util.Set<CilNode>();
            foreach (CilNode start in instrs.Starts)
            {
                start.il.stack_before = new util.Stack<Signature.Param>();
                start.il.stack_vars_before = new util.Stack<vara>();
                DFEncode(start, mtc, ass, visited, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs, attrs);
            }

            /* Reorder the linear stream for rewritten nodes */
            for (int i = 0; i < instrs.linear_stream.Count; i++)
            {
                if (((CilNode)instrs.linear_stream[i]).replaced_by != null)
                {
                    CilNode old = instrs.linear_stream[i] as CilNode;

                    instrs.linear_stream.RemoveAt(i);
                    foreach (CilNode repl in old.replaced_by)
                        instrs.linear_stream.Insert(i++, repl);
                }
            }

            /* Now loop through again and insert label nodes at the appropriate positions */
            visited.Clear();
            foreach (CilNode start in instrs.Starts)
            {
                DFInsertLabels(start, ref next_block, visited);
            }

            /* Loop through and add branch instructions at the end of basic blocks */
            visited.Clear();
            foreach (CilNode start in instrs.Starts)
                DFInsertBranches(start, visited);

            /* Finally loop through again and add the tacs to the output stream */
            foreach (CilNode n in instrs.LinearStream)
            {
                bool is_start = false;
                if (instrs.Starts.Contains(n))
                    is_start = true;
                foreach (timple.TreeNode tac in n.il.tacs)
                {
                    if (is_start)
                    {
                        tac.IsStart = true;
                        is_start = false;
                    }
                    tac.InnerNode = n;
                    ret.Add(tac);
                }
            }

            next_blk = next_block;
            next_var = next_variable;
            return ret;
        }

        private static void DFInsertBranches(CilNode n, util.Set<CilNode> visited)
        {
            if (visited.Contains(n))
                return;
            visited.Add(n);

            if (n.Next.Count == 1)
            {
                CilNode next = n.Next[0] as CilNode;
                if (next.il.tacs.Count >= 1 && (next.il.tacs[0] is timple.TimpleLabelNode))
                {
                    if (n.il.tacs.Count == 0 || !(n.il.tacs[n.il.tacs.Count - 1] is timple.TimpleBrNode))
                    {
                        if (ThreeAddressCode.OpFallsThrough(n.il.tacs[n.il.tacs.Count - 1].Op))
                        {
                            timple.TimpleLabelNode tln = next.il.tacs[0] as timple.TimpleLabelNode;
                            n.il.tacs.Add(new timple.TimpleBrNode(tln));
                        }
                    }
                }
            }

            foreach (CilNode next in n.Next)
                DFInsertBranches(next, visited);
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
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            if (visited.Contains(n))
                return;
            visited.Add(n);

            CilNode new_n = DecomposeComplexOpcodes.DecomposeComplexOpts(n, ass, mtc, ref next_block, attrs);

            if (new_n != n)
            {
                DFEncode(new_n, mtc, ass, visited, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs, attrs);
                return;
            }
            
            if (n.il.opcode.Encoder == null)
                throw new Exception("No encoding available for " + n.il.ToString());
            n.il.stack_after = new util.Stack<Signature.Param>(n.il.stack_before);
            n.il.stack_vars_after = new util.Stack<vara>(n.il.stack_vars_before);
            n.il.tacs = new List<timple.TreeNode>();
            if (n.Prev.Count == 0)
            {
                int block_id;
                if (n.ehclause_start != null)
                {
                    if (n.ehclause_start.BlockId == -1)
                    {
                        block_id = next_block++;
                        n.ehclause_start.BlockId = block_id;
                    }
                    else
                        block_id = n.ehclause_start.BlockId;
                }
                else
                    block_id = next_block++;

                n.il.tacs.Add(new timple.TimpleLabelNode(block_id));

                if(block_id == 0)
                    n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.enter), vara.Void(), vara.Label(Mangler2.MangleMethod(mtc, ass), false), vara.Void()));

                for (int i = 0; i < la_vars.Count; i++)
                {
                    if (la_vars[i].DataType == Assembler.CliType.vt)
                    {
                        vara v_addr = vara.Logical(next_variable++, Assembler.CliType.native_int);
                        n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.touch), la_vars[i], vara.Void(), vara.Void()));
                        n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_addr, vara.AddrOf(la_vars[i]), vara.Void()));

                        Layout l = Layout.GetLayout(Metadata.GetTTC(la_vars[i].vt_type, mtc.GetTTC(ass), mtc.msig, ass), ass);
                        List<Layout.Field> flat_fields = l.GetFlattenedInstanceFieldLayout(mtc.GetTTC(ass), mtc.msig, ass);

                        for (int j = 0; j < flat_fields.Count; j++)
                        {
                            Assembler.CliType ct = flat_fields[j].field.fsig.CliType(ass);
                            n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.la_load), vara.ContentsOf(v_addr, flat_fields[j].offset, ct), vara.Const(i, Assembler.CliType.int32), vara.Const(j, Assembler.CliType.int32)));
                        }
                    }
                        else n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.la_load), la_vars[i], vara.Const(i, Assembler.CliType.int32), vara.Void()));
                }
                for (int i = 0; i < lv_vars.Count; i++)
                {
                    if (lv_vars[i].DataType == Assembler.CliType.vt)
                    {
                        vara v_addr = vara.Logical(next_variable++, Assembler.CliType.native_int);
                        n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.touch), lv_vars[i], vara.Void(), vara.Void()));
                        n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_addr, vara.AddrOf(lv_vars[i]), vara.Void()));

                        Layout l = Layout.GetLayout(Metadata.GetTTC(lv_vars[i].vt_type, mtc.GetTTC(ass), mtc.msig, ass), ass);
                        List<Layout.Field> flat_fields = l.GetFlattenedInstanceFieldLayout(mtc.GetTTC(ass), mtc.msig, ass);

                        for (int j = 0; j < flat_fields.Count; j++)
                        {
                            Assembler.CliType ct = flat_fields[j].field.fsig.CliType(ass);
                            n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.lv_load), vara.ContentsOf(v_addr, flat_fields[j].offset, ct), vara.Const(i, Assembler.CliType.int32), vara.Const(j, Assembler.CliType.int32)));
                        }
                    }
                    else
                        n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.lv_load), lv_vars[i], vara.Const(i, Assembler.CliType.int32), vara.Void()));
                }

                if (block_id == 0 && mtc.meth.Body.InitLocals)
                {
                    foreach (vara lv_var in lv_vars)
                    {
                        if (lv_var.DataType == Assembler.CliType.vt)
                        {
                            /* Initialise each member to 0 */
                            Layout l = Layout.GetLayout(Metadata.GetTTC(lv_var.vt_type, mtc.GetTTC(ass), mtc.msig, ass), ass);
                            List<Layout.Field> flat_fields = l.GetFlattenedInstanceFieldLayout(mtc.GetTTC(ass), mtc.msig, ass);

                            vara v_addr = vara.Logical(next_variable++, Assembler.CliType.native_int);
                            n.il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_addr, vara.AddrOf(lv_var), vara.Void()));

                            for (int i = 0; i < flat_fields.Count; i++)
                            {
                                Assembler.CliType ct = flat_fields[i].field.fsig.CliType(ass);
                                n.il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, ct), vara.ContentsOf(v_addr, flat_fields[i].offset, ct), vara.Const(0, ct), vara.Void()));
                            }
                        }
                        else
                            n.il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, lv_var.DataType), lv_var, vara.Const(0, lv_var.DataType), vara.Void()));
                    }
                }
            }

            n.il.opcode.Encoder(n.il, ass, mtc, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs, attrs);

            util.Set<timple.BaseNode> next_visited = new util.Set<timple.BaseNode>();

            while(true)
            {
                CilNode next = null;
                for (int i = 0; i < n.Next.Count; i++)
                {
                    if (!next_visited.Contains(n.Next[i]))
                    {
                        next = n.Next[i] as CilNode;
                        next_visited.Add(next);
                        break;
                    }
                }
                if (next == null)
                    break;

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

                DFEncode(next, mtc, ass, visited, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs, attrs);
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
                    /* Value types expect the this pointer to be a managed reference to an instance of the value type (CIL I:13.3).
                     * 
                     * However, we (in Assembler.AssembleMethod) rewrite instance methods on boxed virtual types to unbox the
                     * first argument, then call the method on the managed pointer version.
                     */


                    Signature.BaseOrComplexType this_bct = this_ttc.tsig.Type;
                    if (this_bct is Signature.BoxedType)
                    {
                        Signature.Param p_bt = new Signature.Param(this_bct, ass);
                        las.Add(p_bt);
                    }
                    else if (this_bct is Signature.ManagedPointer)
                    {
                        this_bct = ((Signature.ManagedPointer)this_bct).ElemType;
                        Signature.Param mptr_type = new Signature.Param(new Signature.ManagedPointer { ElemType = this_bct }, ass);
                        las.Add(mptr_type);
                    }
                    else
                    {
                        /* Assume we're a managed pointer method */
                        Signature.Param mptr_type = new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = this_bct }, ass);
                        las.Add(mptr_type);
                        //throw new Exception("Function on value type with neither managed pointer or boxed type specified");
                    }
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

        internal static List<Signature.Param> GetLocalVars(Assembler.MethodToCompile mtc, Assembler ass)
        { return GetLocalVars(mtc.meth, mtc.tsig, mtc.msig, ass); }

        internal static List<Signature.Param> GetLocalVars(Metadata.MethodDefRow meth, Signature.BaseOrComplexType containing_type, 
            Signature.BaseMethod containing_meth, Assembler ass)
        {
            List<Signature.Param> lvs = new List<Signature.Param>();

            Signature.LocalVars sig = meth.GetLocalVars(ass);
            foreach (Signature.Param p in sig.Vars)
                lvs.Add(Signature.ResolveGenericParam(p, containing_type, containing_meth, ass));

            return lvs;
        }

    }
}
