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
    class loc
    {
        public static void ldloc(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs)
        {
            int p = 0;

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldloc_0:
                    p = 0;
                    break;
                case Opcode.SingleOpcodes.ldloc_1:
                    p = 1;
                    break;
                case Opcode.SingleOpcodes.ldloc_2:
                    p = 2;
                    break;
                case Opcode.SingleOpcodes.ldloc_3:
                    p = 3;
                    break;
                case Opcode.SingleOpcodes.ldloc_s:
                    p = il.inline_int;
                    break;
                default:
                    throw new Exception("Unimplemented ldloc opcode: " + il.ToString());
            }

            il.stack_after.Push(lvs[p]);

            vara v = vara.Logical(next_variable++, lv_vars[p].DataType);
            il.tacs.Add(new timple.TimpleNode(Assembler.GetAssignTac(lv_vars[p].DataType), v, lv_vars[p], vara.Void()));
            il.stack_vars_after.Push(v);
        }

        public static void stloc(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs)
        {
            int p = 0;

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.stloc_0:
                    p = 0;
                    break;
                case Opcode.SingleOpcodes.stloc_1:
                    p = 1;
                    break;
                case Opcode.SingleOpcodes.stloc_2:
                    p = 2;
                    break;
                case Opcode.SingleOpcodes.stloc_3:
                    p = 3;
                    break;
                case Opcode.SingleOpcodes.stloc_s:
                    p = il.inline_int;
                    break;
                default:
                    throw new Exception("Unimplemented stloc opcode: " + il.ToString());
            }

            il.stack_after.Pop();

            vara v = il.stack_vars_after.Pop();
            il.tacs.Add(new timple.TimpleNode(Assembler.GetAssignTac(lv_vars[p].DataType), lv_vars[p], v, vara.Void()));
        }
    }
}
