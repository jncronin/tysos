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

namespace libtysila
{
    partial class Assembler
    {
        protected internal virtual void InitArchIntCalls(Dictionary<string, libtysila.frontend.cil.Opcode.TybelEncodeFunc> int_calls) { }
    }
}

namespace libtysila.frontend.cil.OpcodeEncodings
{
    partial class call
    {
        static Dictionary<string, Opcode.TybelEncodeFunc> int_calls = null;

        static void init_int_calls(Assembler ass)
        {
            int_calls = new Dictionary<string, Opcode.TybelEncodeFunc>();

            int_calls["_Zu1SM_0_9get_Chars_Rc_P2u1ti"] = string_getChars;
            int_calls["_Zu1SM_0_10get_Length_Ri_P1u1t"] = string_getLength;

            int_calls["_ZX15ArrayOperationsM_0_17GetArrayClassSize_Ri_P0"] = get_array_class_size;
            int_calls["_ZX15ArrayOperationsM_0_19GetInnerArrayOffset_Ri_P0"] = get_array_inner_array_offset;
            int_calls["_ZX15ArrayOperationsM_0_14GetSizesOffset_Ri_P0"] = get_array_sizes_offset;
            int_calls["_ZX15ArrayOperationsM_0_17GetLoboundsOffset_Ri_P0"] = get_array_lobounds_offset;
            int_calls["_ZX15ArrayOperationsM_0_17GetElemTypeOffset_Ri_P0"] = get_array_elem_type_offset;
            int_calls["_ZX15ArrayOperationsM_0_25GetInnerArrayLengthOffset_Ri_P0"] = get_array_inner_array_length_offset;
            int_calls["_ZX15ArrayOperationsM_0_17GetElemSizeOffset_Ri_P0"] = get_array_elem_size_offset;
            int_calls["_ZX15ArrayOperationsM_0_13GetRankOffset_Ri_P0"] = get_array_rank_offset;
            int_calls["_ZX16MemoryOperationsM_0_16GetInternalArray_RPv_P1W6System5Array"] = get_array_internal_array_Pv_u1A;

            int_calls["_ZX15OtherOperationsM_0_3Mul_Ru1I_P2u1Iu1I"] = mul_I;
            int_calls["_ZX15OtherOperationsM_0_3Mul_Ru1U_P2u1Uu1U"] = mul_U;
            int_calls["_ZX15OtherOperationsM_0_3Sub_Ru1I_P2u1Iu1I"] = sub_I;
            int_calls["_ZX15OtherOperationsM_0_3Sub_Ru1U_P2u1Uu1U"] = sub_U;
            int_calls["_ZX15OtherOperationsM_0_3Add_Ru1I_P2u1Iu1I"] = add_I;
            int_calls["_ZX15OtherOperationsM_0_3Add_Ru1U_P2u1Uu1U"] = add_U;

            int_calls["_ZX15OtherOperationsM_0_14GetPointerSize_Ri_P0"] = get_pointer_size;
            int_calls["_ZX9TysosTypeM_0_14GetSizeAsField_Ri_P1u1t"] = null;
            int_calls["_ZX9TysosTypeM_0_21GetSizeAsArrayElement_Ri_P1u1t"] = null;

            int_calls["_ZX15OtherOperationsM_0_5CallI_Rv_P1u1U"] = call_I;
            int_calls["_ZX15OtherOperationsM_0_5CallI_Rv_P1y"] = call_I;
            int_calls["_ZX15OtherOperationsM_0_5CallI_Rv_P1i"] = call_I;

            int_calls["_ZX15OtherOperationsM_0_4Halt_Rv_P0"] = halt;

            int_calls["_ZX15ClassOperationsM_0_27GetVtblExtendsVtblPtrOffset_Ru1U_P0"] = get_vtbl_extendsvtblptr_offset;
            int_calls["_ZX15ClassOperationsM_0_26GetVtblInterfacesPtrOffset_Ru1U_P0"] = get_vtbl_ifaceptr_offset;
            int_calls["_ZX15ClassOperationsM_0_24GetVtblTypeInfoPtrOffset_Ru1U_P0"] = get_vtbl_typeinfoptr_offset;
            int_calls["_ZX15ClassOperationsM_0_22GetObjectIdFieldOffset_Ru1U_P0"] = get_ti_objid_offset;
            int_calls["_ZX15ClassOperationsM_0_18GetVtblFieldOffset_Ru1U_P0"] = get_ti_vtbl_offset;

            int_calls["_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Uy"] = poke_U8;
            int_calls["_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Uj"] = poke_U4;
            int_calls["_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Ut"] = poke_U2;
            int_calls["_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Uh"] = poke_U1;
            int_calls["_ZX16MemoryOperationsM_0_6PeekU8_Ry_P1u1U"] = peek_U8;
            int_calls["_ZX16MemoryOperationsM_0_6PeekU4_Rj_P1u1U"] = peek_U4;
            int_calls["_ZX16MemoryOperationsM_0_6PeekU2_Rt_P1u1U"] = peek_U2;
            int_calls["_ZX16MemoryOperationsM_0_6PeekU1_Rh_P1u1U"] = peek_U1;

            int_calls["_ZX15OtherOperationsM_0_16GetUsedStackSize_Ri_P0"] = get_used_stack_size;

            ass.InitArchIntCalls(int_calls);
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
                if (enc_func == null)
                    throw new Assembler.AssemblerException("call: internal call " + call_mtc.ToString() + " not currently implemented",
                        il.il, mtc);
                enc_func(il, ass, mtc, ref next_block, state, attrs);
                return true;
            }

            /* Handle ReinterpretAs functions */
            if (call_mtc.meth.CustomAttributes.ContainsKey("libsupcs.ReinterpretAsMethod"))
            {
                Signature.Param p_from = call_mtc.msig.Method.Params[0];
                Signature.Param p_to = call_mtc.msig.Method.RetType;

                il.stack_after.Pop();
                il.stack_after.Push(p_to);

                libasm.hardware_location loc_from = il.stack_vars_after.Pop(ass);
                libasm.hardware_location loc_to = il.stack_vars_after.GetAddressFor(p_to, ass);
                ass.Assign(state, il.stack_vars_before, loc_to, loc_from, p_to.CliType(ass), il.il.tybel);
                return true;
            }

            /* Handle GetArg functions */
            if (call_mtc.ToString().StartsWith("_ZX14CastOperationsM_0_") && call_mtc.ToString().Contains("GetArg"))
            {
                /* Strip out the argument number */
                List<char> arg_no = new List<char>();
                int idx = "GetArg".Length;
                while (Char.IsDigit(call_mtc.meth.Name[idx]))
                {
                    arg_no.Add(call_mtc.meth.Name[idx]);
                    idx++;
                }
                string arg_no_s = new string(arg_no.ToArray());
                int arg_no_i = Int32.Parse(arg_no_s);

                libasm.hardware_location src = state.la_locs[arg_no_i];
                libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(state.las[arg_no_i], ass);

                ass.Assign(state, il.stack_vars_before, dest, src, state.las[arg_no_i].CliType(ass), il.il.tybel);

                il.stack_after.Push(state.las[arg_no_i]);
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

        static void string_getLength(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_str = il.stack_vars_after.Pop(ass);
            Signature.Param p_str = il.stack_after.Pop();

            libasm.hardware_location t1 = ass.GetTemporary(state);

            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(new Signature.Param(BaseType_Type.I4), ass);

            ass.Assign(state, il.stack_vars_before, t1, loc_str, Assembler.CliType.native_int, il.il.tybel);
            ass.Assign(state, il.stack_vars_before, t1, new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetStringFieldOffset(Assembler.StringFields.length), size = 4 }, Assembler.CliType.int32, il.il.tybel);
            ass.Assign(state, il.stack_vars_before, dest, t1, Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(new Signature.Param(BaseType_Type.I4));
        }

        static void string_getChars(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_idx = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_str = il.stack_vars_after.Pop(ass);

            Signature.Param p_idx = il.stack_after.Pop();
            Signature.Param p_str = il.stack_after.Pop();

            libasm.hardware_location t1 = ass.GetTemporary(state);
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

        static void get_array_class_size(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.array_type_size),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_inner_array_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_sizes_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.sizes),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_lobounds_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.lobounds),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_elem_type_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.elemtype),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_inner_array_length_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array_length),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_elem_size_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.elem_size),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_rank_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetArrayFieldOffset(Assembler.ArrayFields.rank),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_array_internal_array_Pv_u1A(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            libasm.hardware_location loc_array = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(
                new Signature.UnmanagedPointer { _ass = ass, BaseType = new Signature.BaseType(BaseType_Type.Void) }, ass);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            if (!(loc_array is libasm.register))
            {
                libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                ass.Assign(state, il.stack_vars_before, t1, loc_array, Assembler.CliType.native_int, il.il.tybel);
                loc_array = t1;
            }

            ass.Assign(state, il.stack_vars_before, loc_dest, 
                new libasm.hardware_contentsof { base_loc = loc_array, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.array_type_size), 
                    size = ass.GetSizeOfPointer() }, Assembler.CliType.native_int, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void mul_I(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();

            libasm.hardware_location loc_b = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.I);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.NumOp(state, il.stack_vars_before, loc_dest, loc_a, loc_b, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.mul), il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void mul_U(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();

            libasm.hardware_location loc_b = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.NumOp(state, il.stack_vars_before, loc_dest, loc_a, loc_b, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.mul_un), il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void add_I(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();

            libasm.hardware_location loc_b = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.I);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.NumOp(state, il.stack_vars_before, loc_dest, loc_a, loc_b, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void add_U(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();

            libasm.hardware_location loc_b = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.NumOp(state, il.stack_vars_before, loc_dest, loc_a, loc_b, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void sub_I(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();

            libasm.hardware_location loc_b = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.I);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.NumOp(state, il.stack_vars_before, loc_dest, loc_a, loc_b, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub), il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void sub_U(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();

            libasm.hardware_location loc_b = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.NumOp(state, il.stack_vars_before, loc_dest, loc_a, loc_b, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub), il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_pointer_size(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetSizeOfPointer(),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void call_I(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            libasm.hardware_location loc_target = il.stack_vars_after.Pop(ass);

            if (!(loc_target is libasm.register))
            {
                libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                ass.Assign(state, il.stack_vars_before, t1, loc_target, Assembler.CliType.native_int, il.il.tybel);
                loc_target = t1;
            }

            ass.Call(state, il.stack_vars_before, loc_target, null, new libasm.hardware_location[] { }, ass.callconv_null, il.il.tybel);
        }

        static void halt(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* This should be replaced in arch-specific assemblers to disable interrupts, pause cpu etc */

            int l_halt = state.next_blk++;
            string s_halt = "L" + l_halt.ToString();
            il.il.tybel.Add(new tybel.LabelNode(s_halt, true));
            ass.Br(state, il.stack_vars_before, new libasm.hardware_addressoflabel(s_halt, false), il.il.tybel);
        }

        static void get_vtbl_extendsvtblptr_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetVTTIFieldOffset(Assembler.VTTIFields.vtbl_extendsvtblptr),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_vtbl_ifaceptr_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetVTTIFieldOffset(Assembler.VTTIFields.vtbl_ifaceptr),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_vtbl_typeinfoptr_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetVTTIFieldOffset(Assembler.VTTIFields.vtbl_typeinfoptr),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_ti_objid_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetVTTIFieldOffset(Assembler.VTTIFields.ti_objid),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void get_ti_vtbl_offset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, ass.GetVTTIFieldOffset(Assembler.VTTIFields.ti_vtblptr),
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void peek_U1(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U1);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Peek(state, il.stack_vars_before, loc_dest, loc_addr, 1, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void peek_U2(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U2);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Peek(state, il.stack_vars_before, loc_dest, loc_addr, 2, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void peek_U4(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Peek(state, il.stack_vars_before, loc_dest, loc_addr, 4, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void peek_U8(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U8);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Peek(state, il.stack_vars_before, loc_dest, loc_addr, 8, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void poke_U1(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();
            libasm.hardware_location loc_v = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            ass.Poke(state, il.stack_vars_before, loc_addr, loc_v, 1, il.il.tybel);
        }

        static void poke_U2(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();
            libasm.hardware_location loc_v = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            ass.Poke(state, il.stack_vars_before, loc_addr, loc_v, 2, il.il.tybel);
        }

        static void poke_U4(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();
            libasm.hardware_location loc_v = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            ass.Poke(state, il.stack_vars_before, loc_addr, loc_v, 4, il.il.tybel);
        }

        static void poke_U8(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            il.stack_after.Pop();
            il.stack_after.Pop();
            libasm.hardware_location loc_v = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            ass.Poke(state, il.stack_vars_before, loc_addr, loc_v, 8, il.il.tybel);
        }

        static void get_used_stack_size(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("sthrow", false), null,
                new libasm.hardware_location[] { Assembler.throw_NotImplementedException }, ass.callconv_sthrow, il.il.tybel);

            il.stack_after.Push(p_dest);
        }
    }
}
