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
    class stack
    {
        public static void dup(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            il.stack_after.Push(il.stack_after.Peek());
            il.stack_vars_after.Push(il.stack_vars_after.Peek());
        }

        public static void tybel_dup(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_type = il.stack_after.Peek();
            il.stack_after.Push(p_type);
            libasm.hardware_location loc_src = il.stack_vars_after.GetAddressOf(il.stack_before.Count - 1, ass);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_type, ass);
            ass.Assign(state, il.stack_vars_before, loc_dest, loc_src, p_type.CliType(ass), il.il.tybel);
        }

        public static void tybel_flip(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int opcode = il.il.opcode.opcode;
            int a = 0, b = 0;
            if (opcode == Opcode.OpcodeVal(Opcode.DoubleOpcodes.flip))
            {
                a = 1;
                b = 2;
            }
            else if (opcode == Opcode.OpcodeVal(Opcode.DoubleOpcodes.flip3))
            {
                a = 1;
                b = 3;
            }
            else throw new Exception("Invalid flip opcode: " + il.il.opcode.ToString());

            /* Flip the param stack */
            Signature.Param old = il.stack_after[il.stack_after.Count - a];
            il.stack_after[il.stack_after.Count - a] = il.stack_after[il.stack_after.Count - b];
            il.stack_after[il.stack_after.Count - b] = old;

            /* Flip the hardware stack */
            libasm.hardware_location t1 = ass.GetTemporary();
            Assembler.CliType ct = old.CliType(ass);
            libasm.hardware_location loc_a = il.stack_vars_after.GetAddressOf(il.stack_after.Count - a, ass);
            libasm.hardware_location loc_b = il.stack_vars_after.GetAddressOf(il.stack_after.Count - b, ass);
            ass.Assign(state, il.stack_vars_before, t1, loc_a, ct, il.il.tybel);
            ass.Assign(state, il.stack_vars_before, loc_a, loc_b, ct, il.il.tybel);
            ass.Assign(state, il.stack_vars_before, loc_b, t1, ct, il.il.tybel);
        }
    }
}
