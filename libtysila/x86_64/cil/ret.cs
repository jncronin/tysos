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
            /* Emit profiling code */
            if (attrs.profile)
            {
                string s_mangled = Mangler2.MangleMethod(mtc, ass);
                vara v_mangled = mtc.meth.m.StringTable.GetStringAddress(s_mangled, ass);
                ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("profile", false),
                    null, new libasm.hardware_location[] {
                        new libasm.hardware_addressoflabel(v_mangled.LabelVal, v_mangled.Offset, true),
                        new libasm.hardware_addressoflabel(v_mangled.LabelVal, v_mangled.Offset + ass.GetStringFieldOffset(Assembler.StringFields.data_offset), true),
                        new libasm.const_location { c = s_mangled.Length },
                        new libasm.const_location { c = 1 }
                    },
                    ass.callconv_profile, il.il.tybel);
            } 
            
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
            if (attrs.attrs.ContainsKey("libsupcs.Uninterruptible"))
            {
                ((x86_64_Assembler)ass).ChooseInstruction(((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586 ? x86_64_asm.opcode.POPFD : x86_64_asm.opcode.POPFQ, il.il.tybel);
            }
            ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.LEAVE, il.il.tybel);

            if (attrs.attrs.ContainsKey("libsupcs.ISR"))
            {
                if (attrs.attrs.ContainsKey("libsupcs.x86_64.Cpu+ISRErrorCode"))
                {
                    // Pop the error code from the stack
                    ((x86_64_Assembler)ass).ChooseInstruction(((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586 ? x86_64_asm.opcode.ADDL : x86_64_asm.opcode.ADDQ, il.il.tybel, vara.MachineReg(x86_64_Assembler.Rsp), vara.Const(ass.GetSizeOfPointer(), Assembler.CliType.native_int));
                }
                ((x86_64_Assembler)ass).ChooseInstruction(((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586 ? x86_64_asm.opcode.IRET : x86_64_asm.opcode.IRETQ, il.il.tybel);
            }
            else
                ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.RETN, il.il.tybel);
        }
    }
}
