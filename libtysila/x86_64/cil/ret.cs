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
using libtysila.frontend.cil;

namespace libtysila.x86_64.cil
{
    class ret
    {
        public static void tybel_ret(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            if (state.cc.ReturnValue != null)
            {
                libasm.hardware_location retval = il.stack_vars_after.Pop(ass);
                ((x86_64_Assembler)ass).Assign(state, il.stack_vars_before, state.cc.ReturnValue, retval, il.stack_after.Pop().CliType(ass), il.il.tybel);
            }
            if (state.cc.HiddenRetValArgument != null)
            {
                libasm.hardware_location retval = il.stack_vars_after.Pop(ass);
                libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);
                ass.LoadAddress(state, il.stack_vars_before, t1, retval, il.il.tybel);
                ass.Assign(state, il.stack_vars_before, t2, state.la_stack.GetAddressOf(state.cc.Arguments.Count, ass), Assembler.CliType.native_int,
                    il.il.tybel);
                ass.MemCpy(state, il.stack_vars_before, t2, t1, ass.GetSizeOf(state.cc.MethodSig.Method.RetType), il.il.tybel);
            }

            il.il.tybel.Add(new tybel.SpecialNode { Type = tybel.SpecialNode.SpecialNodeType.RestoreCalleeSaved, Val = state.used_locs });
            ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.LEAVE, il.il.tybel);
            ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.RETN, il.il.tybel);
        }
    }
}
