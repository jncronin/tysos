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
    }
}
