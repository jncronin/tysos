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
    class set
    {
        public static void enc_set(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            vara a = il.stack_vars_before.Peek(1);
            vara b = il.stack_vars_before.Peek(0);
            il.stack_vars_after.Pop();
            il.stack_vars_after.Pop();
            il.stack_after.Pop();
            il.stack_after.Pop();

            if (a.DataType != b.DataType)
                throw new Exception("Mimatched datatypes on " + il.ToString() + ": " + a.DataType.ToString() + " and " + b.DataType.ToString());

            vara r = vara.Logical(next_variable++, Assembler.CliType.int32);

            ThreeAddressCode.OpName op;
            switch (il.opcode)
            {
                case 0xfe01:
                    // ceq
                    op = ThreeAddressCode.OpName.seteq;
                    break;
                case 0xfe02:
                    // cgt
                    op = ThreeAddressCode.OpName.setg;
                    break;
                case 0xfe03:
                    // cgt.un
                    op = ThreeAddressCode.OpName.seta;
                    break;
                case 0xfe04:
                    // clt
                    op = ThreeAddressCode.OpName.setl;
                    break;
                case 0xfe05:
                    // clt.un
                    op = ThreeAddressCode.OpName.setb;
                    break;
                default:
                    throw new NotSupportedException();
            }

            il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(op, a.DataType), r, a, b));

            il.stack_after.Push(new Signature.Param(BaseType_Type.I4));
            il.stack_vars_after.Push(r);
        }
    }
}
