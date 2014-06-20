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
    class Return
    {
        public static void ret(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            ThreeAddressCode.Op ret_op = ThreeAddressCode.Op.invalid;

            switch (mtc.msig.Method.RetType.CliType(ass))
            {
                case Assembler.CliType.void_:
                    if (il.stack_before.Count != 0)
                        throw new Exception("ret instruction from method returning void with non-empty stack");
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.ret_void, vara.Void(), vara.Void(), vara.Void()));
                    return;

                case Assembler.CliType.F32:
                case Assembler.CliType.F64:
                    ret_op = ThreeAddressCode.Op.ret_r8;
                    break;

                case Assembler.CliType.int32:
                    ret_op = ThreeAddressCode.Op.ret_i4;
                    break;

                case Assembler.CliType.int64:
                    ret_op = ThreeAddressCode.Op.ret_i8;
                    break;

                case Assembler.CliType.native_int:
                case Assembler.CliType.O:
                case Assembler.CliType.reference:
                    ret_op = ThreeAddressCode.Op.ret_i;
                    break;

                case Assembler.CliType.vt:
                    ret_op = ThreeAddressCode.Op.ret_vt;
                    break;
            }

            if (il.stack_before.Count != 1)
                throw new Exception("invalid number of parameters on stack(" + il.stack_before.Count + ") at ret instruction");

            vara v = il.stack_vars_after.Pop();
            il.stack_after.Pop();

            il.tacs.Add(new timple.TimpleNode(ret_op, vara.Void(), v, vara.Void()));            
        }
    }
}
