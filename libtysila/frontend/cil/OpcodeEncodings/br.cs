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
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs)
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
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs)
        {
            vara a = il.stack_vars_after.Pop();
            il.stack_after.Pop();

            ThreeAddressCode.Op br_op = GetCmpOp(a.DataType, il.opcode.opcode1);

            vara v_c = vara.Const(0, a.DataType);

            il.tacs.Add(new timple.TimpleBrNode(br_op, -1, -1, a, v_c));
        }

        private static ThreeAddressCode.Op GetCmpOp(Assembler.CliType ct, Opcode.SingleOpcodes op)
        {
            switch (op)
            {
                case Opcode.SingleOpcodes.beq:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.beq_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.beq_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.beq_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.beq_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.beq_i;
                    }
                    return ThreeAddressCode.Op.beq;

                case Opcode.SingleOpcodes.bge:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bge_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bge_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bge_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bge_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bge_i;
                    }
                    return ThreeAddressCode.Op.bge;

                case Opcode.SingleOpcodes.bge_un:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bae_r4_un;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bae_r8_un;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bae_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bae_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bae_i;
                    }
                    return ThreeAddressCode.Op.bae;

                case Opcode.SingleOpcodes.bgt:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bg_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bg_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bg_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bg_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bg_i;
                    }
                    return ThreeAddressCode.Op.bg;

                case Opcode.SingleOpcodes.bgt_un:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.ba_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.ba_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.ba_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.ba_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.ba_i;
                    }
                    return ThreeAddressCode.Op.ba;

                case Opcode.SingleOpcodes.ble:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.ble_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.ble_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.ble_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.ble_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.ble_i;
                    }
                    return ThreeAddressCode.Op.ble;

                case Opcode.SingleOpcodes.ble_un:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bbe_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bbe_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bbe_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bbe_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bbe_i;
                    }
                    return ThreeAddressCode.Op.bbe;

                case Opcode.SingleOpcodes.blt:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bl_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bl_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bl_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bl_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bl_i;
                    }
                    return ThreeAddressCode.Op.bl;

                case Opcode.SingleOpcodes.blt_un:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bb_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bb_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bb_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bb_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bb_i;
                    }
                    return ThreeAddressCode.Op.bb;

                case Opcode.SingleOpcodes.bne_un:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bne_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bne_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bne_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bne_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bne_i;
                    }
                    return ThreeAddressCode.Op.bne;

                case Opcode.SingleOpcodes.brfalse:
                case Opcode.SingleOpcodes.brfalse_s:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.beq_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.beq_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.beq_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.beq_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.beq_i;
                    }
                    return ThreeAddressCode.Op.beq;

                case Opcode.SingleOpcodes.brtrue:
                case Opcode.SingleOpcodes.brtrue_s:
                    switch (ct)
                    {
                        case Assembler.CliType.F32:
                            return ThreeAddressCode.Op.bne_r4;
                        case Assembler.CliType.F64:
                            return ThreeAddressCode.Op.bne_r8;
                        case Assembler.CliType.int32:
                            return ThreeAddressCode.Op.bne_i4;
                        case Assembler.CliType.int64:
                            return ThreeAddressCode.Op.bne_i8;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            return ThreeAddressCode.Op.bne_i;
                    }
                    return ThreeAddressCode.Op.bne;

                default:
                    throw new NotImplementedException();
            }
        }

        private static ThreeAddressCode.Op GetCmpOp(Assembler.CliType cliType)
        {
            switch (cliType)
            {
                case Assembler.CliType.F32:
                    return ThreeAddressCode.Op.cmp_r4;

                case Assembler.CliType.F64:
                    return ThreeAddressCode.Op.cmp_r8;

                case Assembler.CliType.int32:
                    return ThreeAddressCode.Op.cmp_i4;

                case Assembler.CliType.int64:
                    return ThreeAddressCode.Op.cmp_i8;

                case Assembler.CliType.native_int:
                case Assembler.CliType.O:
                case Assembler.CliType.reference:
                    return ThreeAddressCode.Op.cmp_i;

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
