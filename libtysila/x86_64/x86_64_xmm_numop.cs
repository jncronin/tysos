/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace libtysila
{
    partial class x86_64_Assembler
    {
        IEnumerable<OutputBlock> x86_64_ldobj_r8_sgprptr_dxmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            if (!(op1.hardware_loc is x86_64_gpr))
                throw new NotSupportedException();
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, false, 0, (byte)((op == ThreeAddressCode.Op.ldobj_r8) ? 0xf2 : 0xf3), 0x0f, 0x10));
        }

        IEnumerable<OutputBlock> x86_64_cmp_r8_xmm_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, new byte[] { 0x66, 0x0f, 0x2f }));
        }

        IEnumerable<OutputBlock> x86_64_cmp_r8_un_xmm_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, new byte[] { 0x66, 0x0f, 0x2e }));
        }

        IEnumerable<OutputBlock> x86_64_cmp_r4_xmm_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, new byte[] { 0x0f, 0x2f }));
        }

        IEnumerable<OutputBlock> x86_64_cmp_r4_un_xmm_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, new byte[] { 0x0f, 0x2e }));
        }

        IEnumerable<OutputBlock> x86_64_assign_r4_simm_dxmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Load an immediate value to an xmm register

            hardware_location temp_loc = Rdi;
            return OBList(EncAddOpcode(temp_loc as x86_64_gpr, false, 0xb8, ToByteArray((float)op1.constant_val)),
                EncOpcode(result.hardware_loc, temp_loc, 3, false, 0, 0x66, 0x0f, 0x6e));
        }


        IEnumerable<OutputBlock> x86_64_assign_r8_simm_dxmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Load an immediate value to an xmm register

            hardware_location temp_loc = Rdi;
            return OBList(EncAddOpcode(temp_loc as x86_64_gpr, true, 0xb8, ToByteArray((double)op1.constant_val)),
                EncOpcode(result.hardware_loc, temp_loc, 3, true, 0, 0x66, 0x0f, 0x6e));
        }

        IEnumerable<OutputBlock> x86_64_assign_r8_xmm_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, new byte[] { 0xf2, 0x0f, 0x10 }));
        }

        IEnumerable<OutputBlock> x86_64_assign_r8_xmmmem_xmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, result.hardware_loc, 3, false, 0, new byte[] { 0xf2, 0x0f, 0x11 }));
        }

        IEnumerable<OutputBlock> x86_64_assign_r4_xmm_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, new byte[] { 0xf3, 0x0f, 0x10 }));
        }

        IEnumerable<OutputBlock> x86_64_assign_r4_xmmmem_xmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, result.hardware_loc, 3, false, 0, new byte[] { 0xf3, 0x0f, 0x11 }));
        }

        IEnumerable<OutputBlock> x86_64_conv_i48r48_gprmem_xmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            byte r4 = (byte)(((op == ThreeAddressCode.Op.conv_i4_r4) || (op == ThreeAddressCode.Op.conv_i8_r4) || (op == ThreeAddressCode.Op.conv_i_r4)) ? 0xf3 : 0xf2);
            bool i8 = ((op == ThreeAddressCode.Op.conv_i4_r4) || (op == ThreeAddressCode.Op.conv_i4_r8)) ? false : true;
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, i8, 0, new byte[] { r4, 0x0f, 0x2a }));
        }

        IEnumerable<OutputBlock> x86_64_conv_r48i48_dgpr_sxmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            byte r4 = (byte)(((op == ThreeAddressCode.Op.conv_r4_i4) || (op == ThreeAddressCode.Op.conv_r4_i8) || (op == ThreeAddressCode.Op.conv_r4_i)) ? 0xf3 : 0xf2);
            bool i8 = ((op == ThreeAddressCode.Op.conv_r8_i4) || (op == ThreeAddressCode.Op.conv_r4_i4)) ? false : true;
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, i8, 0, new byte[] { r4, 0x0f, 0x2c }));       // convert with truncation as per CIL III:3.27
        }

        IEnumerable<OutputBlock> x86_64_sqrt_r8_dxmm_sxmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0xf2, 0x0f, 0x51));
        }

        IEnumerable<OutputBlock> x86_64_conv_r4r8_dxmm_sxmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0xf3, 0x0f, 0x5a));
        }

        IEnumerable<OutputBlock> x86_64_conv_r8r4_dxmm_sxmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0xf2, 0x0f, 0x5a));
        }

        IEnumerable<OutputBlock> x86_64_add_r8_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf2, 0x0f, 0x58), new x86_64_Instruction { opcode = "addsd", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_add_r4_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf3, 0x0f, 0x58), new x86_64_Instruction { opcode = "addss", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_sub_r8_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf2, 0x0f, 0x5c), new x86_64_Instruction { opcode = "subsd", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_sub_r4_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf3, 0x0f, 0x5c), new x86_64_Instruction { opcode = "subss", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_mul_r8_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf2, 0x0f, 0x59), new x86_64_Instruction { opcode = "mulsd", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_mul_r4_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf3, 0x0f, 0x59), new x86_64_Instruction { opcode = "mulss", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_div_r8_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf2, 0x0f, 0x5e), new x86_64_Instruction { opcode = "divsd", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_div_r4_ds1xmm_s2_xmmmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0xf3, 0x0f, 0x5e), new x86_64_Instruction { opcode = "divss", Operand1 = op1.hardware_loc, Operand2 = op2.hardware_loc })); }

        IEnumerable<OutputBlock> x86_64_neg_r48_dop1_sxmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* signbitss/d contains bitmasks for the sign bits in packed r4s and r8s respectively
             * they are stored in the data section (they must be provided by the kernel/library currently)
             * and are therefore referenced via the GOT
             * 
             * XORing them with an xmm with negate all the packed floats or the one unpacked float in the
             * register */

            string target = "__signbits_r4";
            if (op == ThreeAddressCode.Op.neg_r8)
                target = "__signbits_r8";

            List<OutputBlock> ret = new List<OutputBlock>();
            if (_options.PIC)
            {
                switch (OType)
                {
                    case OutputType.x86_64_large_elf64:
                    case OutputType.x86_64_small_elf64:
                        ret.Add(new CodeBlock(EncOpcode(Rdi, 5, 0, true, 0, 0x8b), new x86_64_Instruction { opcode = "mov", Operand1 = Rdi, Operand2 = new const_location { c = "[signbits]" } }));
                        ret.Add(new RelocationBlock { Target = target, RelType = x86_64.x86_64_elf64.R_X86_64_GOTPCREL, Size = 4, Value = -4 });
                        break;
                    default:
                        throw new Exception("Unknown output type");
                }
            }
            else
            {
                switch (OType)
                {
                    case OutputType.x86_64_large_elf64:
                        ret.Add(new CodeBlock(EncAddOpcode(Rdi, true, 0xb8)));
                        ret.Add(new RelocationBlock { Target = target, RelType = x86_64.x86_64_elf64.R_X86_64_64, Size = 8, Value = 0 });
                        break;
                    case OutputType.x86_64_small_elf64:
                        ret.Add(new CodeBlock(EncOpcode(0, Rdi, 3, false, 0, 0xc7)));
                        ret.Add(new RelocationBlock { Target = target, RelType = x86_64.x86_64_elf64.R_X86_64_32, Size = 4, Value = 0 });
                        break;
                    default:
                        throw new Exception("Unknown output type");
                }
            }

            if (op == ThreeAddressCode.Op.neg_r8)
                ret.Add(new CodeBlock(EncOpcode(result.hardware_loc, Rdi, 0, false, 0, 0x66, 0x0f, 0x57), new x86_64_Instruction { opcode = "xorpd", Operand1 = result.hardware_loc, Operand2 = new hardware_contentsof { base_loc = Rdi } }));
            else
                ret.Add(new CodeBlock(EncOpcode(result.hardware_loc, Rdi, 0, false, 0, 0x0f, 0x57), new x86_64_Instruction { opcode = "xorps", Operand1 = result.hardware_loc, Operand2 = new hardware_contentsof { base_loc = Rdi } }));

            return ret;
        }
    }
}
