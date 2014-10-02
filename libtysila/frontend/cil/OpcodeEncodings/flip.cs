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
    partial class flip
    {
        public static void enc_flip(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            int a_idx, b_idx;

            switch (il.opcode.opcode2)
            {
                case Opcode.DoubleOpcodes.flip:
                    a_idx = 1;
                    b_idx = 2;
                    break;
                case Opcode.DoubleOpcodes.flip3:
                    a_idx = 1;
                    b_idx = 3;
                    break;
                default:
                    throw new NotSupportedException();
            }

            vara v_c = il.stack_vars_before.Peek(a_idx);
            Signature.Param p_c = il.stack_before.Peek(a_idx);

            il.stack_vars_after[a_idx] = il.stack_vars_before[b_idx];
            il.stack_after[a_idx] = il.stack_before[b_idx];

            il.stack_vars_after[b_idx] = v_c;
            il.stack_after[b_idx] = p_c;
        }
    }
}
