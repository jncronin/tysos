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
    class br
    {
        public static void br_two(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            vara a = il.stack_vars_before.Peek(1);
            vara b = il.stack_vars_before.Peek(0);
            il.stack_vars_after.Pop();
            il.stack_vars_after.Pop();
            il.stack_after.Pop();
            il.stack_after.Pop();

            if(a.DataType != b.DataType)
                throw new Exception("Mimatched datatypes on " + il.ToString() + ": " + a.DataType.ToString() + " and " + b.DataType.ToString());

            Opcode.SingleOpcodes op = GetSingleCmpOp(il.opcode);
            ThreeAddressCode.Op br_op = GetCmpOp(a.DataType, op);

            il.tacs.Add(new timple.TimpleBrNode(br_op, -1, -1, a, b));
        }

        public static void br_one(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            vara a = il.stack_vars_after.Pop();
            il.stack_after.Pop();

            ThreeAddressCode.Op br_op = GetCmpOp(a.DataType, il.opcode.opcode1);

            vara v_c = vara.Const(0, a.DataType);

            il.tacs.Add(new timple.TimpleBrNode(br_op, -1, -1, a, v_c));
        }

        public static void br_none(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vara, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            il.tacs.Add(new timple.TimpleBrNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.br), -1, -1, vara.Void(), vara.Void()));
        }

        private static ThreeAddressCode.Op GetCmpOp(Assembler.CliType ct, Opcode.SingleOpcodes op)
        {
            ThreeAddressCode.Op ret = new ThreeAddressCode.Op();
            switch (op)
            {
                case Opcode.SingleOpcodes.brfalse:
                case Opcode.SingleOpcodes.brfalse_s:
                case Opcode.SingleOpcodes.beq:
                    ret.Operator = ThreeAddressCode.OpName.beq;
                    break;

                case Opcode.SingleOpcodes.bge:
                    ret.Operator = ThreeAddressCode.OpName.bge;
                    break;

                case Opcode.SingleOpcodes.bge_un:
                    ret.Operator = ThreeAddressCode.OpName.bae;
                    break;

                case Opcode.SingleOpcodes.bgt:
                    ret.Operator = ThreeAddressCode.OpName.bg;
                    break;

                case Opcode.SingleOpcodes.bgt_un:
                    ret.Operator = ThreeAddressCode.OpName.ba;
                    break;

                case Opcode.SingleOpcodes.ble:
                    ret.Operator = ThreeAddressCode.OpName.ble;
                    break;

                case Opcode.SingleOpcodes.ble_un:
                    ret.Operator = ThreeAddressCode.OpName.bbe;
                    break;

                case Opcode.SingleOpcodes.blt:
                    ret.Operator = ThreeAddressCode.OpName.bl;
                    break;

                case Opcode.SingleOpcodes.blt_un:
                    ret.Operator = ThreeAddressCode.OpName.bb;
                    break;

                case Opcode.SingleOpcodes.bne_un:
                case Opcode.SingleOpcodes.brtrue:
                case Opcode.SingleOpcodes.brtrue_s:
                    ret.Operator = ThreeAddressCode.OpName.bne;
                    break;

                default:
                    throw new NotImplementedException();
            }

            ret.Type = ct;
            return ret;
        }

        private static ThreeAddressCode.Op GetCmpOp(Assembler.CliType cliType)
        {
            switch (cliType)
            {
                case Assembler.CliType.F32:
                case Assembler.CliType.F64:
                case Assembler.CliType.int32:
                case Assembler.CliType.int64:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.cmp, cliType);

                case Assembler.CliType.native_int:
                case Assembler.CliType.O:
                case Assembler.CliType.reference:
                    return new ThreeAddressCode.Op(ThreeAddressCode.OpName.cmp, Assembler.CliType.native_int);

                case Assembler.CliType.vt:
                case Assembler.CliType.void_:
                    throw new NotSupportedException();

                default:
                    throw new NotSupportedException();
            }
        }

        private static Opcode.SingleOpcodes GetSingleCmpOp(Opcode opcode)
        {
            switch (opcode.opcode1)
            {
                case Opcode.SingleOpcodes.beq:
                case Opcode.SingleOpcodes.beq_s:
                    return Opcode.SingleOpcodes.beq;

                case Opcode.SingleOpcodes.bge:
                case Opcode.SingleOpcodes.bge_s:
                    return Opcode.SingleOpcodes.bge;

                case Opcode.SingleOpcodes.bge_un:
                case Opcode.SingleOpcodes.bge_un_s:
                    return Opcode.SingleOpcodes.bge_un;

                case Opcode.SingleOpcodes.bgt:
                case Opcode.SingleOpcodes.bgt_s:
                    return Opcode.SingleOpcodes.bgt;

                case Opcode.SingleOpcodes.bgt_un:
                case Opcode.SingleOpcodes.bgt_un_s:
                    return Opcode.SingleOpcodes.bgt_un;
                    
                case Opcode.SingleOpcodes.ble:
                case Opcode.SingleOpcodes.ble_s:
                    return Opcode.SingleOpcodes.ble;
                    
                case Opcode.SingleOpcodes.ble_un:
                case Opcode.SingleOpcodes.ble_un_s:
                    return Opcode.SingleOpcodes.ble_un;
                    
                case Opcode.SingleOpcodes.blt:
                case Opcode.SingleOpcodes.blt_s:
                    return Opcode.SingleOpcodes.blt;

                case Opcode.SingleOpcodes.blt_un:
                case Opcode.SingleOpcodes.blt_un_s:
                    return Opcode.SingleOpcodes.blt_un;

                case Opcode.SingleOpcodes.bne_un:
                case Opcode.SingleOpcodes.bne_un_s:
                    return Opcode.SingleOpcodes.bne_un;

                default:
                    throw new NotSupportedException("br does not implement " + opcode.ToString());
            }
        }
    }
}
