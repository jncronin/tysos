/* Copyright (C) 2013 by John Cronin
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
using System.Text;
using libasm;

namespace libtysila.arm
{
    partial class arm_Assembler
    {
        IEnumerable<OutputBlock> arm_cmp_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncDPROpcode(cond.Always, 0x15, op1.hardware_loc, R0, 0, 0, op2.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_cmp_i4_gpr_imm12(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            uint val = FromByteArrayU4(ToByteArrayZeroExtend(op2.constant_val, 4));
            return OBList(EncImmOpcode(cond.Always, 0x15, (uint)((arm_gpr)op1.hardware_loc).reg, R0, val));
        }

        IEnumerable<OutputBlock> arm_seteq_i4_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncImmOpcode(cond.Equal, 0x1a, 0, result.hardware_loc, 1),
                EncImmOpcode(cond.NotEqual, 0x1a, 0, result.hardware_loc, 0));
        }

        IEnumerable<OutputBlock> arm_br(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            cond c = cond.Always;
            uint b_opcode = 0xa;

            switch (op)
            {
                case ThreeAddressCode.Op.ba:
                    c = cond.UnsignedHigher;
                    break;
                case ThreeAddressCode.Op.bae:
                    c = cond.CarrySet;
                    break;
                case ThreeAddressCode.Op.bb:
                    c = cond.CarryClear;
                    break;
                case ThreeAddressCode.Op.bbe:
                    c = cond.UnsignedLowerSame;
                    break;
                case ThreeAddressCode.Op.beq:
                    c = cond.Equal;
                    break;
                case ThreeAddressCode.Op.bg:
                    c = cond.SignedGreater;
                    break;
                case ThreeAddressCode.Op.bge:
                    c = cond.SignedGreaterEqual;
                    break;
                case ThreeAddressCode.Op.bl:
                    c = cond.SignedLess;
                    break;
                case ThreeAddressCode.Op.ble:
                    c = cond.SignedLessEqual;
                    break;
                case ThreeAddressCode.Op.bne:
                    c = cond.NotEqual;
                    break;
                case ThreeAddressCode.Op.br:
                    c = cond.Always;
                    break;
            }

            int target_node = ((BrEx)tac).Block_Target;

            // ARM jumps are relative to PC, which is 8 bytes ahead of the current instruction
            // In addition, the B instruction shifts its immediate value 2 bits left so we have
            // to adjust for these in the reference
            return OBList(new NodeReference { block_id = target_node, length = 3, offset = -8, shift_after_offset = 2 },
                new CodeBlock { Code = new byte[] { (byte)(((uint)c << 4) | b_opcode) } });
        }

        IEnumerable<OutputBlock> arm_brehclause(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            int target_node = ((BrEx)tac).Block_Target;
            uint b_opcode = 0xb;

            /* The exception handling blocks do not do push { lr } at the start, therefore we have to do it here:
             * 
             * add lr, pc, #4       <= lr = .next_instruction
             * push lr
             * b target_node
             * .next_instruction:
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.AddRange(arm_add_i4_gpr_gpr_imm(ThreeAddressCode.Op.add_i4, new var { hardware_loc = LR }, new var { hardware_loc = PC },
                new var { constant_val = 4, hardware_loc = new const_location { c = 4 } }, null, state));
            ret.Add(EncSingleRegListOpcode(cond.Always, 0x12, SP, LR));
            ret.Add(new NodeReference { block_id = target_node, length = 3, offset = -8, shift_after_offset = 2 });
            ret.Add(new CodeBlock { Code = new byte[] { (byte)(((uint)cond.Always << 4) | b_opcode) } });
            return ret;
        }
    }
}