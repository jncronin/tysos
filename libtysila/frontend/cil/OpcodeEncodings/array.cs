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
    class arr
    {
        public static void tybel_ldlen(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_arr = il.stack_vars_after.Pop(ass);
            Signature.Param p_arr = il.stack_after.Pop();

            /* Ensure the stack type is a zero-based array */
            if (!(p_arr.Type is Signature.ZeroBasedArray))
            {
                Assembler.TypeToCompile system_array_ttc = Metadata.GetTTC("mscorlib", "System", "Array", ass);
                ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("castclassex", false), loc_arr,
                    new libasm.hardware_location[] { loc_arr, new libasm.hardware_addressoflabel(Mangler2.MangleTypeInfo(system_array_ttc, ass), true) },
                    ass.callconv_castclassex, il.il.tybel);
            }

            /* Ensure it is not null */
            ass.ThrowIf(state, il.stack_vars_before, loc_arr, new libasm.const_location { c = 0 },
                new libasm.hardware_addressoflabel("sthrow", false), new libasm.const_location { c = Assembler.throw_NullReferenceException },
                Assembler.CliType.native_int, ThreeAddressCode.OpName.throweq, il.il.tybel);

            /* Load up the array length member */
            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(new Signature.Param(BaseType_Type.String), ass);
            libasm.hardware_location loc_sizes = ass.GetTemporary(state);
            ass.Assign(state, il.stack_vars_before, loc_sizes,
                new libasm.hardware_contentsof { base_loc = loc_arr, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.sizes), size = ass.GetSizeOfPointer() },
                Assembler.CliType.native_int, il.il.tybel);
            ass.Assign(state, il.stack_vars_before, loc_ret,
                new libasm.hardware_contentsof { base_loc = loc_sizes, const_offset = 0, size = 4 },
                Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(new Signature.Param(BaseType_Type.I4));
        }

        public static void tybel_ldelem(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param type = null;
            Signature.Param p_index = il.stack_after.Pop();
            Signature.Param p_arr = il.stack_after.Pop();
            libasm.hardware_location loc_index = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_arr = il.stack_vars_after.Pop(ass);

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldelem:
                    type = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass).tsig;
                    break;
                case Opcode.SingleOpcodes.ldelem_i:
                    type = new Signature.Param(BaseType_Type.I);
                    break;
                case Opcode.SingleOpcodes.ldelem_i1:
                    type = new Signature.Param(BaseType_Type.I1);
                    break;
                case Opcode.SingleOpcodes.ldelem_i2:
                    type = new Signature.Param(BaseType_Type.I2);
                    break;
                case Opcode.SingleOpcodes.ldelem_i4:
                    type = new Signature.Param(BaseType_Type.I4);
                    break;
                case Opcode.SingleOpcodes.ldelem_i8:
                    type = new Signature.Param(BaseType_Type.I8);
                    break;
                case Opcode.SingleOpcodes.ldelem_r4:
                    type = new Signature.Param(BaseType_Type.R4);
                    break;
                case Opcode.SingleOpcodes.ldelem_r8:
                    type = new Signature.Param(BaseType_Type.R8);
                    break;
                case Opcode.SingleOpcodes.ldelem_ref:
                    type = new Signature.Param(BaseType_Type.Object);
                    if (p_arr.Type is Signature.ZeroBasedArray)
                        type = new Signature.Param(((Signature.ZeroBasedArray)p_arr.Type).ElemType, ass);
                    break;
                case Opcode.SingleOpcodes.ldelem_u1:
                    type = new Signature.Param(BaseType_Type.U1);
                    break;
                case Opcode.SingleOpcodes.ldelem_u2:
                    type = new Signature.Param(BaseType_Type.U2);
                    break;
                case Opcode.SingleOpcodes.ldelem_u4:
                    type = new Signature.Param(BaseType_Type.U4);
                    break;
                default:
                    throw new Exception("Unsupported ldelem opcode: " + il.il.opcode.ToString());
            }

            if (p_arr.Type is Signature.ZeroBasedArray)
            {
                Signature.ZeroBasedArray zba_arr = p_arr.Type as Signature.ZeroBasedArray;
                if (!ass.IsArrayElementCompatibleWith(new Signature.Param(zba_arr.ElemType, ass), type))
                    throw new Assembler.AssemblerException("ldelem: type: " + type.ToString() + " is not " +
                        "array-element-compatible-with array type: " + zba_arr.ElemType.ToString(),
                        il.il, mtc);
            }
            else
                throw new NotImplementedException("ldelem: array is not of type ZeroBasedArray");

            if(!Signature.ParamCompare(p_index, new Signature.Param(BaseType_Type.I), ass) &&
                !Signature.ParamCompare(p_index, new Signature.Param(BaseType_Type.I4), ass))
                throw new Assembler.AssemblerException("ldelem: index is not of types int32 or native int " +
                    "(instead of type " + p_index.ToString() + ")", il.il, mtc);

            libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
            libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);
            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(type, ass);

            /* Load up the address of the inner_array member */
            ass.Assign(state, il.stack_vars_before, t1,
                new libasm.hardware_contentsof { base_loc = loc_arr, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array), size = ass.GetSizeOfIntPtr() },
                Assembler.CliType.native_int, il.il.tybel);

            /* Load up the element offset */
            if(Signature.ParamCompare(p_index, new Signature.Param(BaseType_Type.I4), ass))
            {
                ass.Conv(state, il.stack_vars_before, t2, loc_index, new Signature.BaseType(BaseType_Type.I), new Signature.BaseType(BaseType_Type.I4), false, il.il.tybel);
                loc_index = t2;
            }
            ass.Mul(state, il.stack_vars_before, t2, loc_index, new libasm.const_location { c = ass.GetPackedSizeOf(type) }, Assembler.CliType.native_int, il.il.tybel);

            /* Find the element address */
            ass.Add(state, il.stack_vars_before, t1, t1, t2, Assembler.CliType.native_int, il.il.tybel);

            /* Load up the element */
            ass.Assign(state, il.stack_vars_before, loc_ret, new libasm.hardware_contentsof { base_loc = t1, size = ass.GetPackedSizeOf(type) }, type.CliType(ass), il.il.tybel);

            il.stack_after.Push(type);
        }
    }
}
