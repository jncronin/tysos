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
        IEnumerable<OutputBlock> x86_64_negi48_sgprmem_dop1(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(3, op1.hardware_loc, 3, (op == ThreeAddressCode.Op.neg_i4) ? false : true, 0, 0xf7));
        }

        IEnumerable<OutputBlock> x86_64_noti48_sgprmem_dop1(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(2, op1.hardware_loc, 3, (op == ThreeAddressCode.Op.not_i4) ? false : true, 0, 0xf7));
        }

        IEnumerable<OutputBlock> x86_64_divrem_srax_s2gprmem_draxrdx(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();
            byte opcode;
            int reg;
            bool rexw;

            // clear rdx (xor rdx, rdx)
            ret.AddRange(EncOpcode(Rdx, Rdx, 3, true, 0, 0x33));

            switch (op)
            {
                case ThreeAddressCode.Op.div_i:
                case ThreeAddressCode.Op.div_i8:
                case ThreeAddressCode.Op.rem_i:
                case ThreeAddressCode.Op.rem_i8:
                    opcode = 0xf7;      // idiv
                    reg = 0x07;
                    rexw = true;
                    break;
                case ThreeAddressCode.Op.div_i4:
                case ThreeAddressCode.Op.rem_i4:
                    opcode = 0xf7;      // idiv
                    reg = 0x07;
                    rexw = false;
                    break;
                case ThreeAddressCode.Op.div_u:
                case ThreeAddressCode.Op.div_u8:
                case ThreeAddressCode.Op.rem_un_i:
                case ThreeAddressCode.Op.rem_un_i8:
                    opcode = 0xf7;      // div
                    reg = 0x06;
                    rexw = true;
                    break;
                case ThreeAddressCode.Op.div_u4:
                case ThreeAddressCode.Op.rem_un_i4:
                    opcode = 0xf7;      // div
                    reg = 0x06;
                    rexw = false;
                    break;
                default:
                    throw new NotSupportedException();
            }

            ret.AddRange(EncOpcode(reg, op2.hardware_loc, 3, rexw, 0, opcode));
            return OBList(ret);
        }

        IEnumerable<OutputBlock> x86_64_add_i4_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, false, 0, 0x01));
        }

        IEnumerable<OutputBlock> x86_64_add_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, false, 0, 0x03));
        }

        IEnumerable<OutputBlock> x86_64_sub_i4_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, false, 0, 0x29));
        }

        IEnumerable<OutputBlock> x86_64_sub_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, false, 0, 0x2b));
        }

        IEnumerable<OutputBlock> x86_64_add_i8_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, true, 0, 0x01));
        }

        IEnumerable<OutputBlock> x86_64_add_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, true, 0, 0x03));
        }

        IEnumerable<OutputBlock> x86_64_add_i8_gpr_gprptr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, ((hardware_contentsof)op2.hardware_loc).base_loc, 0, true, ((hardware_contentsof)op2.hardware_loc).const_offset, 0x03));
        }

        IEnumerable<OutputBlock> x86_64_sub_i8_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, true, 0, 0x29));
        }

        IEnumerable<OutputBlock> x86_64_sub_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, true, 0, 0x2b));
        }

        IEnumerable<OutputBlock> x86_64_sub_i8_gpr_gprptr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, ((hardware_contentsof)op2.hardware_loc).base_loc, 0, true, ((hardware_contentsof)op2.hardware_loc).const_offset, 0x2b));
        }

        IEnumerable<OutputBlock> x86_64_add_i4_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();

            if((Convert.ToInt32(op2.constant_val) < SByte.MinValue) || (Convert.ToInt32(op2.constant_val) >
                SByte.MaxValue))
            {
                if (op1.hardware_loc.Equals(Rax))
                    ret.Add(0x05);
                else
                    ret.AddRange(EncOpcode(0, op1.hardware_loc, 3, false, 0, 0x81));
                ret.AddRange(ToByteArray(Convert.ToInt32(op2.constant_val)));
            }
            else
            {
                ret.AddRange(EncOpcode(0, op1.hardware_loc, 3, false, 0, 0x83));
                ret.AddRange(ToByteArray(Convert.ToSByte(op2.constant_val)));
            }

            return new List<OutputBlock> { new CodeBlock { Code = ret } };
        }

        IEnumerable<OutputBlock> x86_64_add_i8_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();

            bool use_64 = false;
            if ((op == ThreeAddressCode.Op.add_i8) || ((ia == IA.x86_64) && (op == ThreeAddressCode.Op.add_i)))
                use_64 = true;

            if (!FitsInt32(op2.constant_val))
            {
                throw new Exception("Constant value does not fit in 4 bytes");
            }
            else if (!FitsSByte(op2.constant_val))
            {
                if (op1.hardware_loc.Equals(Rax))
                {
                    if (use_64)
                        ret.AddRange(new byte[] { RexW(true), 0x05 });
                    else
                        ret.AddRange(new byte[] { 0x05 });
                }
                else
                    ret.AddRange(EncOpcode(0, op1.hardware_loc, 3, use_64, 0, 0x81));
                ret.AddRange(ToByteArraySignExtend(op2.constant_val, 4));
            }
            else
            {
                ret.AddRange(EncOpcode(0, op1.hardware_loc, 3, use_64, 0, 0x83));
                ret.AddRange(ToByteArraySignExtend(op2.constant_val, 1));
            }

            return new List<OutputBlock> { new CodeBlock { Code = ret } };
        }

        IEnumerable<OutputBlock> x86_64_mul_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0x0f, 0xaf));
        }

        IEnumerable<OutputBlock> x86_64_mul_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, true, 0, 0x0f, 0xaf));
        }

        IEnumerable<OutputBlock> x86_64_mul_un_i48_o1rax_o2gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return new List<OutputBlock> { new CodeBlock(EncOpcode(4, op2.hardware_loc, 3, (op == ThreeAddressCode.Op.mul_un_i4) ? false : true, 0, 0xf7),
                new x86_64_Instruction { opcode = "mul", Operand1 = op2.hardware_loc })};
        }

        IEnumerable<OutputBlock> x86_64_mul_i4_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();
            if (FitsSByte(op2.constant_val))
            {
                ret.AddRange(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0x6b));
                ret.AddRange(ToByteArraySignExtend(op2.constant_val, 1));
            }
            else if (FitsInt32(op2.constant_val))
            {
                ret.AddRange(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0x69));
                ret.AddRange(ToByteArraySignExtend(op2.constant_val, 4));
            }
            else
                throw new NotImplementedException();
            return OBList(ret);
        }

        IEnumerable<OutputBlock> x86_64_mul_i8_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();
            if (FitsSByte(op2.constant_val))
            {
                ret.AddRange(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, true, 0, 0x6b));
                ret.AddRange(ToByteArraySignExtend(op2.constant_val, 1));
            }
            else if (FitsInt32(op2.constant_val))
            {
                ret.AddRange(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, true, 0, 0x69));
                ret.AddRange(ToByteArraySignExtend(op2.constant_val, 4));
            }
            else
            {
                // Load to Rdi then perform the multiply
                ret.AddRange(EncAddOpcode(Rdi, true, 0xb8, ToByteArraySignExtend(op2.constant_val, 8)));
                ret.AddRange(EncOpcode(result.hardware_loc, Rdi, 3, true, 0, 0x0f, 0xaf));
            }
            return OBList(ret);
        }

        IEnumerable<OutputBlock> x86_64_and_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, false, 0, 0x23)); }

        IEnumerable<OutputBlock> x86_64_and_i4_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, false, 0, 0x21)); }

        IEnumerable<OutputBlock> x86_64_and_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, true, 0, 0x23)); }

        IEnumerable<OutputBlock> x86_64_and_i8_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, true, 0, 0x21)); }


        IEnumerable<OutputBlock> x86_64_or_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, false, 0, 0x0b)); }

        IEnumerable<OutputBlock> x86_64_or_i4_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, false, 0, 0x09)); }

        IEnumerable<OutputBlock> x86_64_or_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, true, 0, 0x0b)); }

        IEnumerable<OutputBlock> x86_64_or_i8_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, true, 0, 0x09)); }

        IEnumerable<OutputBlock> x86_64_xor_i48_do1gprmem_o2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(op2.hardware_loc, result.hardware_loc, 3, (op == ThreeAddressCode.Op.xor_i4) ? false : true, 0, 0x31)); }

        IEnumerable<OutputBlock> x86_64_xor_i48_do1gpr_o2gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op2.hardware_loc, 3, (op == ThreeAddressCode.Op.xor_i4) ? false : true, 0, 0x33)); }

        IEnumerable<OutputBlock> x86_64_shl_i4_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(4, result.hardware_loc, 3, false, 0, 0xc1),
              ToByteArray(Convert.ToSByte(Convert.ToInt64(op2.constant_val) & 0xff)));
        }
        IEnumerable<OutputBlock> x86_64_shl_i4_gprmem_cl(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(4, result.hardware_loc, 3, false, 0, 0xd3)); }
        IEnumerable<OutputBlock> x86_64_shl_i8_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(4, result.hardware_loc, 3, true, 0, 0xc1),
              ToByteArray(Convert.ToSByte(Convert.ToInt64(op2.constant_val) & 0xff)));
        }
        IEnumerable<OutputBlock> x86_64_shl_i8_gprmem_cl(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(4, result.hardware_loc, 3, true, 0, 0xd3)); }

        IEnumerable<OutputBlock> x86_64_shr_i4_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(7, result.hardware_loc, 3, false, 0, 0xc1),
              ToByteArray(Convert.ToSByte(Convert.ToInt64(op2.constant_val) & 0xff)));
        }
        IEnumerable<OutputBlock> x86_64_shr_i4_gprmem_cl(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(7, result.hardware_loc, 3, false, 0, 0xd3)); }
        IEnumerable<OutputBlock> x86_64_shr_i8_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(7, result.hardware_loc, 3, true, 0, 0xc1),
              ToByteArray(Convert.ToSByte(Convert.ToInt64(op2.constant_val) & 0xff)));
        }
        IEnumerable<OutputBlock> x86_64_shr_i8_gprmem_cl(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(7, result.hardware_loc, 3, true, 0, 0xd3)); }

        IEnumerable<OutputBlock> x86_64_shr_un_i48_sgprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(5, result.hardware_loc, 3, (op == ThreeAddressCode.Op.shr_un_i4) ? false : true, 0, 0xc1),
                ToByteArray(Convert.ToSByte(Convert.ToInt64(op2.constant_val) & 0xff)));
        }

        IEnumerable<OutputBlock> x86_64_shr_un_i48_sgprmem_cl(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(5, result.hardware_loc, 3, (op == ThreeAddressCode.Op.shr_un_i4) ? false : true, 0, 0xd3)); }
    }
}
