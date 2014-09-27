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
    partial class call
    {
        static Dictionary<string, Opcode.TybelEncodeFunc> int_calls = null;

        static void init_int_calls(Assembler ass)
        {
            int_calls = new Dictionary<string, Opcode.TybelEncodeFunc>();

            int_calls["_Zu1SM_0_9get_Chars_Rc_P2u1ti"] = string_getChars;
            int_calls["_Zu1SM_0_10get_Length_Ri_P1u1t"] = null;
        }

        private static bool enc_intcall(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            if (int_calls == null)
                init_int_calls(ass);

            Assembler.MethodToCompile call_mtc;
            if(il.il.inline_tok is MTCToken)
                call_mtc = ((MTCToken)il.il.inline_tok).mtc;
            else
                call_mtc = Metadata.GetMTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);

            string mangled_name = Mangler2.MangleMethod(call_mtc, ass);

            Opcode.TybelEncodeFunc enc_func;

            if (int_calls.TryGetValue(mangled_name, out enc_func))
            {
                enc_func(il, ass, mtc, ref next_block, state, attrs);
                return true;
            }
            return false;
        }

        internal static bool ProvidesIntcall(Assembler.MethodToCompile mtc, Assembler ass)
        {
            if (int_calls == null)
                init_int_calls(ass);

            Opcode.TybelEncodeFunc intcall;
            if (int_calls.TryGetValue(Mangler2.MangleMethod(mtc, ass), out intcall) == false)
                return false;
            return intcall != null;
        }

        static void string_getChars(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_idx = il.stack_vars_before.Pop(ass);
            libasm.hardware_location loc_str = il.stack_vars_before.Pop(ass);

            Signature.Param p_idx = il.stack_before.Pop();
            Signature.Param p_str = il.stack_before.Pop();

            libasm.hardware_location t1 = ass.GetTemporary();
            libasm.hardware_location t2 = ass.GetTemporary();
            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(new Signature.Param(BaseType_Type.Char), ass);

            ass.Add(state, il.stack_vars_before, t1, loc_str, new libasm.const_location { c = ass.GetStringFieldOffset(Assembler.StringFields.length) }, Assembler.CliType.native_int, il.il.tybel);
            ass.Assign(state, il.stack_vars_before, t1, new libasm.hardware_contentsof { base_loc = t1 }, Assembler.CliType.native_int, il.il.tybel);
            ass.ThrowIf(state, il.stack_vars_before, loc_idx, t1, new libasm.hardware_addressoflabel("sthrow", false), new libasm.const_location { c = Assembler.throw_IndexOutOfRangeException }, Assembler.CliType.int32, ThreeAddressCode.OpName.throwge_un, il.il.tybel);
            ass.Mul(state, il.stack_vars_before, t1, loc_idx, new libasm.const_location { c = 2 }, Assembler.CliType.native_int, il.il.tybel);
            ass.Add(state, il.stack_vars_before, t1, t1, new libasm.const_location { c = ass.GetStringFieldOffset(Assembler.StringFields.data_offset) }, Assembler.CliType.native_int, il.il.tybel);
            ass.Add(state, il.stack_vars_before, t1, t1, loc_str, Assembler.CliType.native_int, il.il.tybel);
            ass.Peek(state, il.stack_vars_before, dest, t1, 2, il.il.tybel);

            il.stack_after.Push(new Signature.Param(BaseType_Type.Char));
        }

        static void string_getChars(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            // char get_Chars(int32 idx)
            // if(idx >= length) throw IndexOutOfRangeException
            // if(idx < 0) throw IndexOutOfRangeException
            // addr = obj + obj.ClassSize + idx * char.Size
            // retval = peek_u2(addr)

            vara v_str = il.stack_vars_before[il.stack_before.Count - 2];
            vara v_idx = il.stack_vars_before[il.stack_before.Count - 1];

            vara v_length = vara.Logical(next_variable++, Assembler.CliType.int32);
            vara v_addr = vara.Logical(next_variable++, Assembler.CliType.native_int);
            vara v_offset = vara.Logical(next_variable++, Assembler.CliType.int32);
            vara v_ret = vara.Logical(next_variable++, Assembler.CliType.int32);

            //enc_checknullref(i, v_str);

            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_length, vara.ContentsOf(v_str, ass.GetStringFieldOffset(Assembler.StringFields.length), Assembler.CliType.int32), vara.Void()));
            il.tacs.Add(new timple.TimpleThrowBrNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.throwge_un), v_idx, v_length, vara.Label("sthrow", false), vara.Const(Assembler.throw_IndexOutOfRangeException, Assembler.CliType.int32)));
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_offset, v_idx, vara.Const(2, Assembler.CliType.int32)));
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add), v_offset, v_offset, vara.Const(ass.GetStringFieldOffset(Assembler.StringFields.data_offset), Assembler.CliType.int32)));
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_uzx), v_addr, v_offset, vara.Void()));
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), v_addr, v_addr, v_str));
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u2), v_ret, v_addr, vara.Void()));

            il.stack_vars_after.Pop();
            il.stack_vars_after.Pop();
            il.stack_after.Pop();
            il.stack_after.Pop();

            il.stack_vars_after.Push(v_ret);
            il.stack_after.Push(new Signature.Param(BaseType_Type.Char));
        }
    }
}
