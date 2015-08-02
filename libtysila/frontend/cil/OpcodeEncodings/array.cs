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
            if (!(loc_arr is libasm.register))
            {
                libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);
                ass.Assign(state, il.stack_vars_before, t2, loc_arr, Assembler.CliType.native_int, il.il.tybel);
                loc_arr = t2;
            }
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
            bool signed = false;
            bool is_ldelema = false;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldelem:
                case Opcode.SingleOpcodes.ldelema:
                    type = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass).tsig;
                    break;
                case Opcode.SingleOpcodes.ldelem_i:
                    type = new Signature.Param(BaseType_Type.I);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldelem_i1:
                    type = new Signature.Param(BaseType_Type.I1);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldelem_i2:
                    type = new Signature.Param(BaseType_Type.I2);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldelem_i4:
                    type = new Signature.Param(BaseType_Type.I4);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldelem_i8:
                    type = new Signature.Param(BaseType_Type.I8);
                    signed = true;
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

            if (il.il.opcode.opcode1 == Opcode.SingleOpcodes.ldelema)
                is_ldelema = true;

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

            if(p_index.CliType(ass) != Assembler.CliType.int32 && p_index.CliType(ass) != Assembler.CliType.native_int)
                throw new Assembler.AssemblerException("ldelem: index is not of types int32 or native int " +
                    "(instead of type " + p_index.ToString() + ")", il.il, mtc);

            libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
            libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);

            /* Get the array object into t1 */
            ass.Assign(state, il.stack_vars_before, t1, loc_arr, Assembler.CliType.native_int, il.il.tybel);

            if (il.il.int_array == false)
            {
                /* Array bounds check */
                ass.ThrowIf(state, il.stack_vars_before, loc_index,
                    new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array_length), size = 4 },
                    new libasm.hardware_addressoflabel("sthrow", false), new libasm.const_location { c = Assembler.throw_IndexOutOfRangeException },
                    Assembler.CliType.int32, ThreeAddressCode.OpName.throwge_un,
                    il.il.tybel);

                /* Load up the address of the inner_array member */
                ass.Assign(state, il.stack_vars_before, t1,
                    new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array), size = ass.GetSizeOfIntPtr() },
                    Assembler.CliType.native_int, il.il.tybel);
            }

            /* Load up the element offset */
            if(Signature.ParamCompare(p_index, new Signature.Param(BaseType_Type.I4), ass))
            {
                ass.Conv(state, il.stack_vars_before, t2, loc_index, new Signature.BaseType(BaseType_Type.I), new Signature.BaseType(BaseType_Type.I4), false, il.il.tybel);
                loc_index = t2;
            }
            ass.Mul(state, il.stack_vars_before, t2, loc_index, new libasm.const_location { c = ass.GetPackedSizeOf(type) }, Assembler.CliType.native_int, il.il.tybel);

            /* Find the element address */
            ass.Add(state, il.stack_vars_before, t1, t1, t2, Assembler.CliType.native_int, il.il.tybel);

            if (is_ldelema)
            {
                type = new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = type.Type }, ass);
                libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(type, ass);
                ass.Assign(state, il.stack_vars_before, loc_ret, t1, Assembler.CliType.native_int, il.il.tybel);
                il.stack_after.Push(type);
            }
            else
            {
                /* Load up the element */
                libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(type, ass);
                ass.Peek(state, il.stack_vars_before, loc_ret, t1, ass.GetPackedSizeOf(type), il.il.tybel);
                //ass.Assign(state, il.stack_vars_before, loc_ret, new libasm.hardware_contentsof { base_loc = t1, size = ass.GetPackedSizeOf(type) }, type.CliType(ass), il.il.tybel);

                if(type.CliType(ass) != Assembler.CliType.vt)
                    ass.Conv(state, il.stack_vars_before, loc_ret, loc_ret, new Signature.BaseType(type.CliType(ass)),
                        new Signature.BaseType(ass.GetUnderlyingType(type)), signed, il.il.tybel);

                il.stack_after.Push(type);
            }
        }

        public static void tybel_stelem(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param type = null;
            Signature.Param p_value = il.stack_after.Pop();
            Signature.Param p_index = il.stack_after.Pop();
            Signature.Param p_arr = il.stack_after.Pop();
            libasm.hardware_location loc_value = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_index = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_arr = il.stack_vars_after.Pop(ass);

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.stelem:
                    type = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass).tsig;
                    break;
                case Opcode.SingleOpcodes.stelem_i:
                    type = new Signature.Param(BaseType_Type.I);
                    break;
                case Opcode.SingleOpcodes.stelem_i1:
                    type = new Signature.Param(BaseType_Type.I1);
                    break;
                case Opcode.SingleOpcodes.stelem_i2:
                    type = new Signature.Param(BaseType_Type.I2);
                    break;
                case Opcode.SingleOpcodes.stelem_i4:
                    type = new Signature.Param(BaseType_Type.I4);
                    break;
                case Opcode.SingleOpcodes.stelem_i8:
                    type = new Signature.Param(BaseType_Type.I8);
                    break;
                case Opcode.SingleOpcodes.stelem_r4:
                    type = new Signature.Param(BaseType_Type.R4);
                    break;
                case Opcode.SingleOpcodes.stelem_r8:
                    type = new Signature.Param(BaseType_Type.R8);
                    break;
                case Opcode.SingleOpcodes.stelem_ref:
                    type = new Signature.Param(BaseType_Type.Object);
                    if (p_arr.Type is Signature.ZeroBasedArray)
                        type = new Signature.Param(((Signature.ZeroBasedArray)p_arr.Type).ElemType, ass);
                    break;
                default:
                    throw new Exception("Unsupported stelem opcode: " + il.il.opcode.ToString());
            }

            if (p_arr.Type is Signature.ZeroBasedArray)
            {
                Signature.ZeroBasedArray zba_arr = p_arr.Type as Signature.ZeroBasedArray;
                if (!ass.IsArrayElementCompatibleWith(new Signature.Param(zba_arr.ElemType, ass), type))
                    throw new Assembler.AssemblerException("stelem: type: " + type.ToString() + " is not " +
                        "array-element-compatible-with array type: " + zba_arr.ElemType.ToString(),
                        il.il, mtc);
            }
            else
                throw new NotImplementedException("stelem: array is not of type ZeroBasedArray");

            if (p_index.CliType(ass) != Assembler.CliType.int32 && p_index.CliType(ass) != Assembler.CliType.native_int)
                throw new Assembler.AssemblerException("stelem: index is not of types int32 or native int " +
                    "(instead of type " + p_index.ToString() + ")", il.il, mtc);

            libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
            libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);

            /* Get the array object into t1 */
            ass.Assign(state, il.stack_vars_before, t1, loc_arr, Assembler.CliType.native_int, il.il.tybel);

            if (il.il.int_array == false)
            {
                /* Array bounds check */
                ass.ThrowIf(state, il.stack_vars_before, loc_index,
                    new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array_length), size = 4 },
                    new libasm.hardware_addressoflabel("sthrow", false), new libasm.const_location { c = Assembler.throw_IndexOutOfRangeException },
                    Assembler.CliType.int32, ThreeAddressCode.OpName.throwge_un,
                    il.il.tybel);

                /* Load up the address of the inner_array member */
                ass.Assign(state, il.stack_vars_before, t1,
                    new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array), size = ass.GetSizeOfIntPtr() },
                    Assembler.CliType.native_int, il.il.tybel);
            }

            /* Load up the element offset */
            if (Signature.ParamCompare(p_index, new Signature.Param(BaseType_Type.I4), ass))
            {
                ass.Conv(state, il.stack_vars_before, t2, loc_index, new Signature.BaseType(BaseType_Type.I), new Signature.BaseType(BaseType_Type.I4), false, il.il.tybel);
                loc_index = t2;
            }
            ass.Mul(state, il.stack_vars_before, t2, loc_index, new libasm.const_location { c = ass.GetPackedSizeOf(type) }, Assembler.CliType.native_int, il.il.tybel);

            /* Find the element address */
            ass.Add(state, il.stack_vars_before, t1, t1, t2, Assembler.CliType.native_int, il.il.tybel);

            /* Store the element */
            ass.Poke(state, il.stack_vars_before, t1, loc_value, ass.GetPackedSizeOf(type), il.il.tybel);
        }


        public static void tybel_newarr(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Assembler.TypeToCompile elem_type = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.ZeroBasedArray zba = new Signature.ZeroBasedArray { _ass = ass, ElemType = elem_type.tsig.Type };
            Signature.Param p_zba = new Signature.Param(zba, ass);
            Assembler.TypeToCompile arr_type = ass.CreateArray(p_zba, 1, elem_type, true);
            zba.ArrayType = arr_type.type;

            libasm.hardware_location loc_numelems = il.stack_vars_after.Pop(ass);
            Signature.Param p_numelems = il.stack_after.Pop();

            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(p_zba, ass);

            libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
            libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);

            /* Convert numElems to native int */
            if (!Signature.ParamCompare(p_numelems, new Signature.Param(BaseType_Type.I), ass) &&
                !Signature.ParamCompare(p_numelems, new Signature.Param(BaseType_Type.U), ass))
            {
                if (p_numelems.CliType(ass) != Assembler.CliType.int32)
                    throw new Assembler.AssemblerException("newarr: numElems is not of type int32 or native int " +
                        "(is of type " + p_numelems.ToString() + ")", il.il, mtc);

                ass.Conv(state, il.stack_vars_before, t2, loc_numelems, new Signature.BaseType(BaseType_Type.I),
                    new Signature.BaseType(p_numelems), false, il.il.tybel);
                loc_numelems = t2;
                p_numelems = new Signature.Param(BaseType_Type.I4);
            }
            //if(!(loc_numelems is libasm.register))
            else
            {
                ass.Assign(state, il.stack_vars_before, t2, loc_numelems, Assembler.CliType.native_int, il.il.tybel);
                loc_numelems = t2;
            }

            /* Allocate memory for the array.  Array size =
             *     (numElems * elemSize) + sizes_array_size + lobounds_array_size + array_type_size
             *     
             * Where sizes_array_size and lobounds_array_size are SizeOf(Int32)
             * Let static_size = array_type_size + 2 * 4
             */
            Layout l_arr_type = Layout.GetTypeInfoLayout(arr_type, ass, true);
            int static_size = l_arr_type.ClassSize + 8;

            // t1 = numElems * elemSize
            int elem_size = ass.GetPackedSizeOf(elem_type.tsig);
            if (elem_size > 1)
                ass.Mul(state, il.stack_vars_before, t1, loc_numelems, new libasm.const_location { c = elem_size }, Assembler.CliType.native_int, il.il.tybel);
            else
                ass.Assign(state, il.stack_vars_before, t1, loc_numelems, Assembler.CliType.native_int, il.il.tybel);

            // t1 = t1 + static_size
            ass.Add(state, il.stack_vars_before, t1, t1, new libasm.const_location { c = static_size }, Assembler.CliType.native_int, il.il.tybel);
            // t1 = gcmalloc(t1)
            Stack temp_stack = il.stack_vars_before.Clone();
            temp_stack.MarkUsed(t1); temp_stack.MarkUsed(t2);
            ass.Call(state, temp_stack, new libasm.hardware_addressoflabel("gcmalloc", false), t1, new libasm.hardware_location[] { t1 },
                ass.callconv_gcmalloc, il.il.tybel);

            /* Fill in the array fields */
            int lobounds_offset = l_arr_type.ClassSize;
            int sizes_offset = lobounds_offset + 4;
            int data_offset = sizes_offset + 4;

            // __vtbl
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = l_arr_type.vtbl_offset, size = ass.GetSizeOfPointer() },
                new libasm.hardware_addressoflabel(l_arr_type.typeinfo_object_name, l_arr_type.FixedLayout[Layout.ID_VTableStructure].Offset, true),
                Assembler.CliType.native_int, il.il.tybel);

            // __rank
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.rank), size = 4 },
                new libasm.const_location { c = 1 }, Assembler.CliType.int32, il.il.tybel);

            // __elemsize
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.elem_size), size = 4 },
                new libasm.const_location { c = elem_size }, Assembler.CliType.int32, il.il.tybel);

            // __inner_array_length
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array_length), size = 4 },
                t2, Assembler.CliType.int32, il.il.tybel);

            // __elemtype
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.elemtype), size = ass.GetSizeOfIntPtr() },
                new libasm.hardware_addressoflabel(Mangler2.MangleTypeInfo(elem_type, ass), true), Assembler.CliType.native_int, il.il.tybel);

            // __lobounds
            ass.LoadAddress(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.lobounds), size = ass.GetSizeOfIntPtr() },
                new libasm.hardware_contentsof { base_loc = t1, const_offset = lobounds_offset }, il.il.tybel);

            // __sizes
            ass.LoadAddress(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.sizes), size = ass.GetSizeOfIntPtr() },
                new libasm.hardware_contentsof { base_loc = t1, const_offset = sizes_offset }, il.il.tybel);

            // __inner_array
            ass.LoadAddress(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array), size = ass.GetSizeOfIntPtr() },
                new libasm.hardware_contentsof { base_loc = t1, const_offset = data_offset }, il.il.tybel);

            // __lobounds[0]
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = lobounds_offset, size = 4 },
                new libasm.const_location { c = 0 }, Assembler.CliType.int32, il.il.tybel);
            
            // __sizes[0]
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = sizes_offset, size = 4 },
                t2, Assembler.CliType.int32, il.il.tybel);

            // TODO: ?zero memory if not done by gcmalloc

            // __objid
            ass.Call(state, temp_stack, new libasm.hardware_addressoflabel("getobjid", false), t2, new libasm.hardware_location[] { },
                ass.callconv_getobjid, il.il.tybel);
            ass.Assign(state, temp_stack,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = l_arr_type.obj_id_offset, size = 4 },
                t2, Assembler.CliType.int32, il.il.tybel);

            // return
            ass.Assign(state, temp_stack, loc_ret, t1, Assembler.CliType.native_int, il.il.tybel);
            il.stack_after.Push(p_zba);
        }
    }
}
