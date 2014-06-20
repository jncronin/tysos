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
    class ldc
    {
        public static void ldc_i4(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            int p = 0;

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldc_i4:
                    p = il.inline_int;
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
                    p = il.inline_int;
                    break;
            }

            vara v_c = vara.Const(p, Assembler.CliType.int32);
            vara v = vara.Logical(next_variable++, Assembler.CliType.int32);
            il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.assign_i4, v, v_c, vara.Void()));

            il.stack_after.Push(new Signature.Param(BaseType_Type.I4));
            il.stack_vars_after.Push(v);
        }
    }
}
