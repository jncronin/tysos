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

namespace libtysila.frontend.cil.OpcodeEncodings
{
    class exceptions
    {
        public static void leave(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            /* Empty the evalation stack */
            il.stack_after.Clear();
            il.stack_vars_after.Clear();

            /* Store local vars before calling finally handlers */
            eh_store_lvs_las(mtc, il, lv_vars, la_vars);

            /* Call any finally blocks */
            if (mtc.meth.Body.exceptions != null)
            {
                foreach (Metadata.MethodBody.EHClause ehclause in mtc.meth.Body.exceptions)
                {
                    if (ehclause.IsFinally && (il.il_offset >= (int)ehclause.TryOffset) && (il.il_offset < (int)(ehclause.TryOffset + ehclause.TryLength)))
                    {
                        // we have found a handling finally block
                        // we should only call the handler if the target is outside of the try block
                        if (((il.il_offset_after + il.inline_int) < (int)ehclause.TryOffset) || ((il.il_offset_after + il.inline_int) >= (int)(ehclause.TryOffset + ehclause.TryLength)))
                        {
                            int block_id = ehclause.BlockId;
                            if (block_id == -1)
                            {
                                ehclause.BlockId = next_block++;
                                block_id = ehclause.BlockId;
                            }
                            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.br_ehclause), vara.Void(), vara.Const(block_id, Assembler.CliType.int32), vara.Void()));
                        }
                    }
                }
            }

            /* Branch to the target */
            il.tacs.Add(new timple.TimpleBrNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.br), -1, -1, vara.Void(), vara.Void()));                               
        }

        public static void tybel_leave(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* Empty the evalation stack */
            il.stack_after.Clear();
            il.stack_vars_after.Clear();

            /* Call any finally blocks */
            if (mtc.meth.Body.exceptions != null)
            {
                foreach (Metadata.MethodBody.EHClause ehclause in mtc.meth.Body.exceptions)
                {
                    if (ehclause.IsFinally && (il.il.il_offset >= (int)ehclause.TryOffset) && (il.il.il_offset < (int)(ehclause.TryOffset + ehclause.TryLength)))
                    {
                        // we have found a handling finally block
                        // we should only call the handler if the target is outside of the try block
                        if (((il.il.il_offset_after + il.il.inline_int) < (int)ehclause.TryOffset) || ((il.il.il_offset_after + il.il.inline_int) >= (int)(ehclause.TryOffset + ehclause.TryLength)))
                        {
                            CilNode eh_target = state.offset_map[(int)ehclause.HandlerOffset];
                            ass.BrEhclause(state, il.stack_vars_before, new libasm.hardware_addressoflabel("L" + eh_target.il_label.ToString(), false), il.il.tybel);
                        }
                    }
                }
            }

            /* Branch to the target */
            CilNode target = state.offset_map[il.il.il_offset_after + il.il.inline_int];
            ass.Br(state, il.stack_vars_before, new libasm.hardware_addressoflabel("L" + target.il_label.ToString(), false), il.il.tybel);
        }

        internal static void eh_store_lvs_las(Assembler.MethodToCompile mtc, InstructionLine il, List<vara> lv_vars, List<vara> la_vars)
        {
            if (mtc.meth.Body.exceptions == null || mtc.meth.Body.exceptions.Count == 0)
                return;
            for (int i = 0; i < lv_vars.Count; i++)
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.lv_store), vara.Void(), vara.Const(i, Assembler.CliType.int32), lv_vars[i]));
            for (int i = 0; i < la_vars.Count; i++)
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.la_store), vara.Void(), vara.Const(i, Assembler.CliType.int32), la_vars[i]));
        }

        public static void endfinally(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            /* Empty the evalation stack */
            il.stack_after.Clear();
            il.stack_vars_after.Clear();

            /* Store any local vars */
            eh_store_lvs_las(mtc, il, lv_vars, la_vars);

            /* Return back to where we came */
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.endfinally), vara.Void(), vara.Void(), vara.Void()));
        }

        public static void enc_throw(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            vara v_exception_obj = il.stack_vars_after.Pop();
            il.stack_after.Pop();
            vara v_methinfo = vara.Logical(next_variable++, Assembler.CliType.O);

            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.ldmethinfo), v_methinfo, vara.Void(), vara.Void()));
            il.tacs.Add(new timple.TimpleCallNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call), vara.Void(), vara.Label("throw", false),
                new vara[] { v_exception_obj }, ass.msig_throw));
        }

        public static void tybel_throw(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_except_obj = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("throw", false), null,
                new libasm.hardware_location[] { loc_except_obj }, ass.callconv_throw, il.il.tybel);
        }

        public static void tybel_sthrow(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_except_obj = new libasm.const_location { c = il.il.inline_int };

            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("sthrow", false), null,
                new libasm.hardware_location[] { loc_except_obj }, ass.callconv_sthrow, il.il.tybel);
        }

        public static void tybel_rethrow(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            ass.EmitWarning(new Assembler.AssemblerException("rethrow: currently not implemented", il.il, mtc));
            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("sthrow", false), null,
                new libasm.hardware_location[] { Assembler.throw_NotImplementedException }, ass.callconv_sthrow, il.il.tybel);
        }

        public static void tybel_throwfalse(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_bool = il.stack_vars_after.Pop(ass);
            Signature.Param p_bool = il.stack_after.Pop();

            if (p_bool.CliType(ass) != Assembler.CliType.int32 && p_bool.CliType(ass) != Assembler.CliType.native_int &&
                p_bool.CliType(ass) != Assembler.CliType.O)
                throw new Exception("invalid throwfalse argument: " + p_bool.ToString());

            ass.ThrowIf(state, il.stack_vars_before, loc_bool, new libasm.const_location { c = 0 },
                new libasm.hardware_addressoflabel("sthrow", false), new libasm.const_location { c = il.il.inline_int },
                p_bool.CliType(ass), ThreeAddressCode.OpName.throweq, il.il.tybel);
        }
    }
}
