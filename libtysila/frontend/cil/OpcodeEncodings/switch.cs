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
    partial class switch_
    {
        public static void tybel_switch(CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_value = il.stack_vars_after.Pop(ass);
            Signature.Param p_value = il.stack_after.Pop();

            if (p_value.CliType(ass) != Assembler.CliType.int32)
                throw new Assembler.AssemblerException("switch: value is not of type Int32 (" + p_value.ToString() + ")",
                    il.il, mtc);

            for (int i = 0; i < il.il.inline_array.Count; i++)
            {
                int target = il.il.il_offset_after + il.il.inline_array[i];
                CilNode target_cil = state.offset_map[target];
                string l_target = "L" + target_cil.il_label.ToString();

                ass.BrIf(state, il.stack_vars_before, new libasm.hardware_addressoflabel(l_target, false), loc_value,
                    new libasm.const_location { c = i }, ThreeAddressCode.OpName.beq, Assembler.CliType.int32, il.il.tybel);
            }
        }
    }
}
