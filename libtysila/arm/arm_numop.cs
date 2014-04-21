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
        IEnumerable<OutputBlock> arm_mul_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncRdRtRmRnOpcode(cond.Always, 0, result.hardware_loc, R0, op1.hardware_loc, 0x9, op2.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_add_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncDPROpcode(cond.Always, 8, op1.hardware_loc, result.hardware_loc, 0, 0, op2.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_add_i4_gpr_gpr_imm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncImmOpcode(cond.Always, 8, op1.hardware_loc, result.hardware_loc, FromByteArrayU4(ToByteArrayZeroExtend(op2.constant_val, 4))));
        }

        IEnumerable<OutputBlock> arm_add_i8_2gpr_2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // adds, adc
            multiple_hardware_location R = result.hardware_loc as multiple_hardware_location;
            multiple_hardware_location O1 = op1.hardware_loc as multiple_hardware_location;
            multiple_hardware_location O2 = op2.hardware_loc as multiple_hardware_location;

            return OBList(EncDPROpcode(cond.Always, 9, O1.hlocs[0], R.hlocs[0], 0, 0, O2.hlocs[0]),
                EncDPROpcode(cond.Always, 0xb, O1.hlocs[1], R.hlocs[1], 0, 0, O2.hlocs[1]));
        }

        IEnumerable<OutputBlock> arm_add_i8_2gpr_2gpr_imm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // adds, adc
            uint[] c = Split64(op2.constant_val);
            multiple_hardware_location R = result.hardware_loc as multiple_hardware_location;
            multiple_hardware_location O1 = op1.hardware_loc as multiple_hardware_location;

            return OBList(EncImmOpcode(cond.Always, 9, O1.hlocs[0], R.hlocs[0], c[0]),
                EncImmOpcode(cond.Always, 0xb, O1.hlocs[1], R.hlocs[1], c[1]));
        }

        IEnumerable<OutputBlock> arm_sub_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncDPROpcode(cond.Always, 4, op1.hardware_loc, result.hardware_loc, 0, 0, op2.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_sub_i4_gpr_gpr_imm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncImmOpcode(cond.Always, 4, op1.hardware_loc, result.hardware_loc, FromByteArrayU4(ToByteArrayZeroExtend(op2.constant_val, 4))));
        }

        IEnumerable<OutputBlock> arm_and_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncDPROpcode(cond.Always, 0, op1.hardware_loc, result.hardware_loc, 0, 0, op2.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_and_i4_gpr_gpr_imm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncImmOpcode(cond.Always, 0, op1.hardware_loc, result.hardware_loc, FromByteArrayU4(ToByteArrayZeroExtend(op2.constant_val, 4))));
        }

        IEnumerable<OutputBlock> arm_or_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncDPROpcode(cond.Always, 0x18, op1.hardware_loc, result.hardware_loc, 0, 0, op2.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_or_i4_gpr_gpr_imm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncImmOpcode(cond.Always, 0x18, op1.hardware_loc, result.hardware_loc, FromByteArrayU4(ToByteArrayZeroExtend(op2.constant_val, 4))));
        }

        IEnumerable<OutputBlock> arm_not_i4_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncDPROpcode(cond.Always, 0x1e, R0, result.hardware_loc, 0, 0, op1.hardware_loc));
        }

        IEnumerable<OutputBlock> arm_not_i4_gpr_gpr_imm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncImmOpcode(cond.Always, 0x1e, 0, result.hardware_loc, FromByteArrayU4(ToByteArrayZeroExtend(op1.constant_val, 4))));
        }
    }
}
