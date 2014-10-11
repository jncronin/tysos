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
    class ldc
    {
        public static void tybel_ldc_i4(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int p = 0;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldc_i4:
                    p = il.il.inline_int;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_0:
                    p = 0;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_1:
                    p = 1;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_2:
                    p = 2;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_3:
                    p = 3;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_4:
                    p = 4;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_5:
                    p = 5;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_6:
                    p = 6;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_7:
                    p = 7;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_8:
                    p = 8;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_m1:
                    p = -1;
                    break;

                case Opcode.SingleOpcodes.ldc_i4_s:
                    p = il.il.inline_int;
                    break;
            }

            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(new Signature.Param(Assembler.CliType.int32), ass);
            libasm.hardware_location src = new libasm.const_location { c = p };

            x86_64_Assembler.EncMov(ass as x86_64_Assembler, state, dest, src, Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(new Signature.Param(BaseType_Type.I4));
        }

        public static void tybel_ldc_i8(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            long p = il.il.inline_int64;

            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(new Signature.Param(Assembler.CliType.int64), ass);

            if (((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586)
            {
                x86_64_Assembler x = ass as x86_64_Assembler;
                libasm.multiple_hardware_location mhl_dest = x86_64_Assembler.mhl_split(x, state, dest, 2, 4);
                int low = ass.FromByteArrayI4(il.il.inline_val, 0);
                int high = ass.FromByteArrayI4(il.il.inline_val, 4);
                ass.Assign(state, il.stack_vars_before, mhl_dest[0], new libasm.const_location { c = low }, Assembler.CliType.int32, il.il.tybel);
                ass.Assign(state, il.stack_vars_before, mhl_dest[1], new libasm.const_location { c = high }, Assembler.CliType.int32, il.il.tybel);
            }
            else
            {
                libasm.hardware_location src = new libasm.const_location { c = p };

                if(!(dest is libasm.x86_64_gpr))
                {
                    x86_64_Assembler.EncMov(ass as x86_64_Assembler, state, x86_64_Assembler.Rax, src, Assembler.CliType.int64, il.il.tybel);
                    x86_64_Assembler.EncMov(ass as x86_64_Assembler, state, dest, x86_64_Assembler.Rax, Assembler.CliType.int64, il.il.tybel);
                }
                else
                    x86_64_Assembler.EncMov(ass as x86_64_Assembler, state, dest, src, Assembler.CliType.int64, il.il.tybel);
            }

            il.stack_after.Push(new Signature.Param(BaseType_Type.I8));
        }

        public static void tybel_ldc_r4(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            uint p = LSB_Assembler.FromByteArrayU4S(il.il.inline_val, 0);

            Signature.Param p_ret = new Signature.Param(BaseType_Type.R4);
            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(p_ret, ass);
            dest = x86_64_Assembler.ResolveStackLoc(ass as x86_64_Assembler, state, dest);
            libasm.hardware_location act_dest = dest;

            /* Load the bytes to Rax, then copy to the xmm */
            if (!(act_dest is libasm.x86_64_xmm))
                act_dest = x86_64_Assembler.Xmm0;

            ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rax, new libasm.const_location { c = p },
                Assembler.CliType.int32, il.il.tybel);
            ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.MOVD, il.il.tybel, act_dest, x86_64_Assembler.Rax);

            if (!dest.Equals(act_dest))
                ass.Assign(state, il.stack_vars_before, dest, act_dest, Assembler.CliType.F32, il.il.tybel);

            il.stack_after.Push(p_ret);
        }

        public static void tybel_ldc_r8(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            ulong p = LSB_Assembler.FromByteArrayU8S(il.il.inline_val, 0);

            Signature.Param p_ret = new Signature.Param(BaseType_Type.R8);
            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(p_ret, ass);
            dest = x86_64_Assembler.ResolveStackLoc(ass as x86_64_Assembler, state, dest);
            libasm.hardware_location act_dest = dest;

            if (!(act_dest is libasm.x86_64_xmm))
                act_dest = x86_64_Assembler.Xmm0;

            x86_64_Assembler x = ass as x86_64_Assembler;
            if (x.ia == x86_64_Assembler.IA.i586)
            {
                /* Push the immediate value x2 then load with movsd and restore the stack pointer */
                uint low = LSB_Assembler.FromByteArrayU4S(il.il.inline_val, 0);
                uint high = LSB_Assembler.FromByteArrayU4S(il.il.inline_val, 1);
                x.ChooseInstruction(x86_64_asm.opcode.PUSH, il.il.tybel, new libasm.const_location { c = high });
                x.ChooseInstruction(x86_64_asm.opcode.PUSH, il.il.tybel, new libasm.const_location { c = low });
                x.ChooseInstruction(x86_64_asm.opcode.MOVSD, il.il.tybel, act_dest, new libasm.hardware_contentsof { base_loc = x86_64_Assembler.Rsp, size = 8 });
                x.ChooseInstruction(x86_64_asm.opcode.ADDL, il.il.tybel, x86_64_Assembler.Rsp, new libasm.const_location { c = 8 });
            }
            else
            {
                /* Load the bytes to Rax, then copy to the xmm */

                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rax, new libasm.const_location { c = p },
                    Assembler.CliType.int64, il.il.tybel);
                ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.MOVQ, il.il.tybel, act_dest, x86_64_Assembler.Rax);
            }

            if (!dest.Equals(act_dest))
                ass.Assign(state, il.stack_vars_before, dest, act_dest, Assembler.CliType.F64, il.il.tybel);

            il.stack_after.Push(p_ret);
        }

        public static void tybel_ldnull(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(new Signature.Param(Assembler.CliType.O), ass);
            libasm.hardware_location src = new libasm.const_location { c = 0 };

            x86_64_Assembler.EncMov(ass as x86_64_Assembler, state, dest, src, Assembler.CliType.O, il.il.tybel);

            il.stack_after.Push(new Signature.Param(BaseType_Type.Object));
        }
    }
}
