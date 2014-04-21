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
        IEnumerable<OutputBlock> x86_64_assign_i8_gprptr_label(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // mov tempreg, [label wrt rip]
            // optional (if constant_offset != 0): lea tempreg/destreg, [tempreg + offset]
            // optional (if assigning to contents of/stackloc): mov [destreg], tempreg

            x86_64_gpr tempreg;
            x86_64_gpr saved_reg = null;
            x86_64_gpr destreg = null;
            bool to_contents_of = false;
            bool is_stack_loc = false;

            List<OutputBlock> ret = new List<OutputBlock>();

            if ((result.hardware_loc is hardware_contentsof) || (result.hardware_loc is hardware_stackloc))
            {
                if (result.hardware_loc is hardware_contentsof)
                    destreg = ((hardware_contentsof)result.hardware_loc).base_loc as x86_64_gpr;
                else
                    is_stack_loc = true;

                tempreg = Rdi;

                if (tempreg == null)
                {
                    if (is_stack_loc)
                        saved_reg = Rdx;
                    else
                    {
                        if (destreg == Rdx)
                            saved_reg = Rcx;
                        else
                            saved_reg = Rdx;
                    }
                    tempreg = new x86_64_gpr { reg = saved_reg.reg };
                    ret.Add(new CodeBlock { Code = this.SaveLocation(saved_reg) });
                }
                to_contents_of = true;
            }
            else
                tempreg = destreg = result.hardware_loc as x86_64_gpr;

            // do mov tempreg, [label wrt rip]
            // note in the following Rbp is actually the encoding for rip
            if (op1.base_var.v.is_function)
            {
                if (_options.PIC)
                {
                    switch (OType)
                    {
                        case OutputType.x86_64_large_elf64:
                        case OutputType.x86_64_small_elf64:
                            ret.Add(new CodeBlock(EncOpcode(tempreg, Rbp, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = tempreg, Operand2 = Rbp }));
                            ret.Add(new RelocationBlock
                            {
                                Target = op1.base_var.v.label,
                                RelType = x86_64.x86_64_elf64.R_X86_64_PLT32,
                                Size = 4,
                                Value = -4
                            });
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
                        case OutputType.x86_64_jit:
                            ret.Add(new CodeBlock(EncAddOpcode(tempreg, true, 0xb8)));
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf64.R_X86_64_64, Size = 8, Value = 0 });
                            break;
                        case OutputType.x86_64_small_elf64:
                            ret.Add(new CodeBlock(EncOpcode(0, tempreg, 3, false, 0, 0xc7)));
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf64.R_X86_64_32, Size = 4, Value = 0 });
                            break;
                        default:
                            throw new Exception("Unknown output type");
                    }
                }
            }
            else
            {
                if (_options.PIC)
                {
                    switch (OType)
                    {
                        case OutputType.x86_64_large_elf64:
                        case OutputType.x86_64_small_elf64:
                            ret.Add(new CodeBlock
                            {
                                Code = EncOpcode(tempreg, Rbp, 0, true, 0, 0x8b)
                            });
                            ret.Add(new RelocationBlock
                            {
                                Target = op1.base_var.v.label,
                                RelType =
                                    x86_64.x86_64_elf64.R_X86_64_GOTPCREL,
                                Size = 4,
                                Value = -4
                            });
                            break;
                        case OutputType.i586_elf64:
                            // Here we do mov tempreg, [label wrt ebx]
                            ret.Add(new CodeBlock { Code = new byte[] { 0x8b, ModRM(2, (byte)((int)tempreg.reg % 8), (byte)((int)Rbx.reg % 8)) } });
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf64.R_X86_64_GOT32, Size = 4, Value = 0 });
                            break;
                        case OutputType.i586_elf:
                            // Here we do mov tempreg, [label wrt ebx]
                            ret.Add(new CodeBlock { Code = new byte[] { 0x8b, ModRM(2, (byte)((int)tempreg.reg % 8), (byte)((int)Rbx.reg % 8)) } });
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf32.R_386_GOT32, Size = 4, Value = 0 });
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
                        case OutputType.x86_64_jit:
                            // mov tempreg, imm64
                            ret.Add(new CodeBlock(EncAddOpcode(tempreg, true, 0xb8), new x86_64_Instruction { opcode = "mov", Operand1 = tempreg, Operand2 = op1.base_var.v.hardware_loc }));
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf64.R_X86_64_64, Size = 8, Value = 0 });
                            break;
                        case OutputType.x86_64_small_elf64:
                            // mov tempreg, imm32
                            ret.Add(new CodeBlock(EncOpcode(0, tempreg, 3, false, 0, 0xc7)));
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf64.R_X86_64_32, Size = 4, Value = 0 });
                            break;
                        case OutputType.i586_elf64:
                            // mov tempreg, imm32
                            ret.Add(new CodeBlock(EncAddOpcode(tempreg, false, 0xb8), new x86_64_Instruction { opcode = "mov", Operand1 = tempreg, Operand2 = op1.base_var.v.hardware_loc }));
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf64.R_X86_64_32, Size = 4, Value =0 });
                            break;
                        case OutputType.i586_elf:
                            // mov tempreg, imm32
                            ret.Add(new CodeBlock(EncAddOpcode(tempreg, false, 0xb8), new x86_64_Instruction { opcode = "mov", Operand1 = tempreg, Operand2 = op1.base_var.v.hardware_loc }));
                            ret.Add(new RelocationBlock { Target = op1.base_var.v.label, RelType = x86_64.x86_64_elf32.R_386_32, Size = 4, Value = 0 });
                            break;
                        default:
                            throw new Exception("Unknown output type");
                    }
                }
            }

            if (op1.constant_offset != 0)
                // do lea tempreg, [tempreg + offset]
                ret.Add(new CodeBlock { Code = EncOpcode(tempreg, tempreg, 0, ia == IA.x86_64, op1.constant_offset, 0x8d) });

            if (to_contents_of)
            {
                // mov [destreg], tempreg
                hardware_location destloc;
                int offset;

                if (is_stack_loc)
                {
                    destloc = result.hardware_loc;
                    offset = 0;
                }
                else
                {
                    destloc = destreg;
                    offset = ((hardware_contentsof)result.hardware_loc).const_offset;
                }
                ret.Add(new CodeBlock
                {
                    Code = EncOpcode(tempreg, destloc, 1, ia == IA.x86_64, offset, 0x89)
                });

                /*if (saved_reg == null)
                    state.reg_alloc.FreeRegister(tempreg);
                else
                    ret.Add(new CodeBlock { Code = this.RestoreLocation(saved_reg) });*/
            }

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_assign_i8_sptr_dgpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // lea dest, [src]
            return OBList(EncOpcode(result.hardware_loc, ((hardware_addressof)op1.hardware_loc).base_loc, 0, true, 0, 0x8d));
        }

        IEnumerable<OutputBlock> x86_64_assign_i4_sptr_dgpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // lea dest, [src]
            return OBList(EncOpcode(result.hardware_loc, ((hardware_addressof)op1.hardware_loc).base_loc, 0, false, 0, 0x8d));
        }

        IEnumerable<OutputBlock> x86_64_assign_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0x8b));
        }

        IEnumerable<OutputBlock> x86_64_assign_i4_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, result.hardware_loc, 3, false, 0, 0x89));
        }

        IEnumerable<OutputBlock> x86_64_assign_i4_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();
            if (((x86_64_gpr)result.hardware_loc).is_extended)
                ret.Add(RexB(true));
            ret.Add((byte)(0xb8 + ((x86_64_gpr)result.hardware_loc).base_val));
            ret.AddRange(ToByteArraySignExtend(op1.constant_val, 4));
            return OBList(ret);
        }

        IEnumerable<OutputBlock> x86_64_assign_i4_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(0, result.hardware_loc, 3, false, 0, 0xc7), ToByteArray(Convert.ToInt32(op1.constant_val)));
        }

        IEnumerable<OutputBlock> x86_64_assign_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, true, 0, 0x8b));
        }

        IEnumerable<OutputBlock> x86_64_assign_i8_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, result.hardware_loc, 3, true, 0, 0x89));
        }

        IEnumerable<OutputBlock> x86_64_assign_i8_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<byte> ret = new List<byte>();
            ret.Add((byte)(RexW(true) | RexB(((x86_64_gpr)result.hardware_loc).is_extended)));
            ret.Add((byte)(0xb8 + ((x86_64_gpr)result.hardware_loc).base_val));
            ret.AddRange(ToByteArraySignExtend(op1.constant_val, 8));
            return OBList(ret);
        }

        IEnumerable<OutputBlock> x86_64_ldcatchobj_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new CodeBlock(EncOpcode(Rbx, result.hardware_loc, 3, true, 0, 0x89), new x86_64_Instruction { opcode = "mov", Operand1 = result.hardware_loc, Operand2 = Rdi }));
        }

        IEnumerable<OutputBlock> x86_64_peek_u1_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // movzx destreg, [srcreg]
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, (ia == IA.x86_64), 0, 0x0f, 0xb6));
        }

        IEnumerable<OutputBlock> x86_64_peek_u2_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // movzx destreg, [srcreg]
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, (ia == IA.x86_64), 0, 0x0f, 0xb7));
        }

        IEnumerable<OutputBlock> x86_64_peek_i1_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // movsx destreg, [srcreg]
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, (ia == IA.x86_64), 0, 0x0f, 0xbe));
        }

        IEnumerable<OutputBlock> x86_64_peek_i2_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // movsx destreg, [srcreg]
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, (ia == IA.x86_64), 0, 0x0f, 0xbf));
        }

        IEnumerable<OutputBlock> x86_64_peek_u1_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of a specified hardware address to a register + zero extend to int32

            if (ia == IA.x86_64)
            {
                // Do mov destreg, imm64; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, true, 0xb8,
                    ToByteArray(Convert.ToUInt64(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, true, 0, 0x0f, 0xb6));
            }
            else
            {
                // Do mov destreg, imm32; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, false, 0xb8,
                    ToByteArray(Convert.ToUInt32(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, false, 0, 0x0f, 0xb6));
            }
        }

        IEnumerable<OutputBlock> x86_64_peek_u2_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of a specified hardware address to a register + zero extend to int32

            if (ia == IA.x86_64)
            {
                // Do mov destreg, imm64; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, true, 0xb8,
                    ToByteArray(Convert.ToUInt64(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, true, 0, 0x0f, 0xb7));
            }
            else
            {
                // Do mov destreg, imm32; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, false, 0xb8,
                    ToByteArray(Convert.ToUInt32(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, false, 0, 0x0f, 0xb7));
            }
        }

        IEnumerable<OutputBlock> x86_64_peek_i1_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of a specified hardware address to a register + sign extend to int32

            if (ia == IA.x86_64)
            {
                // Do mov destreg, imm64; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, true, 0xb8,
                    ToByteArray(Convert.ToUInt64(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, true, 0, 0x0f, 0xbe));
            }
            else
            {
                // Do mov destreg, imm32; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, false, 0xb8,
                    ToByteArray(Convert.ToUInt32(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, false, 0, 0x0f, 0xbe));
            }
        }

        IEnumerable<OutputBlock> x86_64_peek_i2_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of a specified hardware address to a register + sign extend to int32

            if (ia == IA.x86_64)
            {
                // Do mov destreg, imm64; movzx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, true, 0xb8,
                    ToByteArray(Convert.ToUInt64(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, true, 0, 0x0f, 0xbf));
            }
            else
            {
                // Do mov destreg, imm32; movsx destreg, [destreg]
                return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, false, 0xb8,
                    ToByteArray(Convert.ToUInt32(op1.constant_val))),
                    EncOpcode(result.hardware_loc, result.hardware_loc, 0, false, 0, 0x0f, 0xbf));
            }
        }


        IEnumerable<OutputBlock> x86_64_peek_u4_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of a specified hardware address to a register + sign extend to int32

            // Do mov destreg, imm64; mov destreg, [destreg]
            return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, (ia == IA.x86_64), 0xb8,
                ToByteArray(Convert.ToUInt64(op1.constant_val))),
                EncOpcode(result.hardware_loc, result.hardware_loc, 0, false, 0, 0x8b));
        }

        IEnumerable<OutputBlock> x86_64_peek_u8_gpr_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of a specified hardware address to a register + sign extend to int32

            if (ia == IA.i586)
                throw new NotImplementedException();

            // Do mov destreg, imm64; movsx destreg, [destreg]
            return OBList(EncAddOpcode(result.hardware_loc as x86_64_gpr, true, 0xb8,
                ToByteArray(Convert.ToUInt64(op1.constant_val))),
                EncOpcode(result.hardware_loc, result.hardware_loc, 0, true, 0, 0x8b));
        }

        IEnumerable<OutputBlock> x86_64_peek_r48_dxmm_sgpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of the address specified in the source register to an xmm

            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, false, 0, (byte)((op == ThreeAddressCode.Op.peek_r4) ? 0xf3 : 0xf2), 0x0f, 0x10));
        }

        IEnumerable<OutputBlock> x86_64_poke_r48_o1gpr_o2xmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of an xmm to an address pointed to by a gpr

            return OBList(EncOpcode(op2.hardware_loc, op1.hardware_loc, 0, false, 0, (byte)((op == ThreeAddressCode.Op.peek_r4) ? 0xf3 : 0xf2), 0x0f, 0x11));
        }

        IEnumerable<OutputBlock> x86_64_peek_u_gpr_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of the address in op1 to the result

            if ((op == ThreeAddressCode.Op.peek_u) || (op == ThreeAddressCode.Op.ldobj_i))
            {
                if (ia == IA.i586)
                    op = ThreeAddressCode.Op.ldobj_i4;
                else
                    op = ThreeAddressCode.Op.ldobj_i8;
            }

            switch (op)
            {
                case ThreeAddressCode.Op.peek_u:
                case ThreeAddressCode.Op.peek_u8:
                case ThreeAddressCode.Op.ldobj_i8:
                case ThreeAddressCode.Op.ldobj_i:
                    return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, true, 0, false, 0x8b));
                case ThreeAddressCode.Op.peek_u4:
                case ThreeAddressCode.Op.ldobj_i4:
                    return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, false, 0, false, 0x8b));
                case ThreeAddressCode.Op.peek_u2:
                    return OBList(new byte[] { 0x66 }, EncOpcode(result.hardware_loc, op1.hardware_loc, 0, false, 0, false, 0x8b));
                case ThreeAddressCode.Op.peek_u1:
                    return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 0, false, 0, true, 0x8a));
                default:
                    throw new NotSupportedException();
            }
        }

        IEnumerable<OutputBlock> x86_64_poke_u1_gprconst_gprmemconst(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Move the contents of op2 to the address op1

            // Mov tempreg, imm64; mov [tempreg], srcreg/const

            if (op == ThreeAddressCode.Op.poke_u)
            {
                if (ia == IA.x86_64)
                    op = ThreeAddressCode.Op.poke_u8;
                else
                    op = ThreeAddressCode.Op.poke_u4;
            }

            List<OutputBlock> obs = new List<OutputBlock>();
            x86_64_gpr tr = Rdi;
            if (op1.type == var.var_type.Const)
            {
                obs.Add(new CodeBlock
                {
                    Code = EncAddOpcode(tr, true, 0xb8, ToByteArraySignExtend(op1.constant_val, 8))
                });
            }
            else
            {
                obs.Add(new CodeBlock
                {
                    Code = EncOpcode(tr, op1.hardware_loc, 3, (ia == IA.x86_64), 0, 0x8b)
                });
            }
            if (op2.type == var.var_type.Const)
            {
                switch (op)
                {
                    case ThreeAddressCode.Op.poke_u1:
                        obs.Add(new CodeBlock { Code = EncOpcode(0, tr, 0, false, op1.constant_offset, true, 0xc6) });
                        //obs.Add(new CodeBlock { Code = ToByteArray(Convert.ToByte(op2.constant_val)) });
                        obs.Add(new CodeBlock { Code = ToByteArrayZeroExtend(op2.constant_val, 1) });
                        break;
                    case ThreeAddressCode.Op.poke_u2:
                        obs.Add(new CodeBlock { Code = new byte[] { 0x66 } });
                        obs.Add(new CodeBlock { Code = EncOpcode(0, tr, 0, false, op1.constant_offset, 0xc7) });
                        //obs.Add(new CodeBlock { Code = ToByteArray(Convert.ToUInt16(op2.constant_val)) });
                        obs.Add(new CodeBlock { Code = ToByteArrayZeroExtend(op2.constant_val, 2) });
                        break;
                    case ThreeAddressCode.Op.poke_u4:
                        obs.Add(new CodeBlock { Code = EncOpcode(0, tr, 0, false, op1.constant_offset, 0xc7) });
                        //obs.Add(new CodeBlock { Code = ToByteArray(Convert.ToUInt32(op2.constant_val)) });
                        obs.Add(new CodeBlock { Code = ToByteArrayZeroExtend(op2.constant_val, 4) });
                        break;
                    case ThreeAddressCode.Op.poke_u8:
                    case ThreeAddressCode.Op.poke_u:
                        if (FitsInt32(op2.constant_val))
                        {
                            obs.Add(new CodeBlock { Code = EncOpcode(0, tr, 0, true, op1.constant_offset, 0xc7) });
                            obs.Add(new CodeBlock { Code = ToByteArrayZeroExtend(op2.constant_val, 4) });
                        }
                        else
                            throw new NotSupportedException();
                        break;
                }
            }
            else
            {
                switch (op)
                {
                    case ThreeAddressCode.Op.poke_u1:
                        obs.Add(new CodeBlock { Code = EncOpcode(op2.hardware_loc, tr, 0, false, op1.constant_offset, true, 0x88) });
                        break;
                    case ThreeAddressCode.Op.poke_u2:
                        obs.Add(new CodeBlock { Code = new byte[] { 0x66 } });
                        obs.Add(new CodeBlock { Code = EncOpcode(op2.hardware_loc, tr, 0, false, op1.constant_offset, 0x89) });
                        break;
                    case ThreeAddressCode.Op.poke_u4:
                        obs.Add(new CodeBlock { Code = EncOpcode(op2.hardware_loc, tr, 0, false, op1.constant_offset, 0x89) });
                        break;
                    case ThreeAddressCode.Op.poke_u8:
                    case ThreeAddressCode.Op.poke_u:
                        obs.Add(new CodeBlock { Code = EncOpcode(op2.hardware_loc, tr, 0, true, op1.constant_offset, 0x89) });
                        break;
                }
            }
            return obs;
        }

        IEnumerable<OutputBlock> x86_64_portout_u2_u1_const_rax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new List<byte> { 0xe6 }, ToByteArray(Convert.ToByte(op1.constant_val)));
        }

        IEnumerable<OutputBlock> x86_64_portout_u2_u1_dx_rax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new List<byte> { 0xee }); }

        IEnumerable<OutputBlock> x86_64_portout_u2_u2_dx_rax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new List<byte> { 0x66, 0xef }); }

        IEnumerable<OutputBlock> x86_64_portout_u2_u4_dx_rax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new List<byte> { 0xef }); }

        IEnumerable<OutputBlock> x86_64_portin_u2_u1_o1dx_drax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new CodeBlock(new byte[] { 0xec }, new x86_64_Instruction { opcode = "inb" }),
                new CodeBlock(EncOpcode(Rax, Rax, 3, false, 0, 0x0f, 0xb6), new x86_64_Instruction { opcode = "movzx", Operand1 = Rax, Operand2 = Rax }));
        }

        IEnumerable<OutputBlock> x86_64_portin_u2_u2_o1dx_drax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new CodeBlock(new byte[] { 0x66, 0xed }, new x86_64_Instruction { opcode = "inw" }),
                new CodeBlock(EncOpcode(Rax, Rax, 3, false, 0, 0x0f, 0xb7), new x86_64_Instruction { opcode = "movzx", Operand1 = Rax, Operand2 = Rax }));
        }

        IEnumerable<OutputBlock> x86_64_portin_u2_u4_o1dx_drax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(new CodeBlock(new byte[] { 0xed }, new x86_64_Instruction { opcode = "ind" })); }


        IEnumerable<OutputBlock> x86_64_stobj_vt(ThreeAddressCode.Op op, var result, var op1, var op2, ThreeAddressCode tac, AssemblerState state)
        {
            if (tac.VTSize.HasValue == false)
                throw new Exception("VTSize not assigned");
            List<OutputBlock> ret = new List<OutputBlock>();

            // Store the value in op2 to the address in op1

            if (!op2.hardware_loc.Equals(Rsi))
            {
                // Load the address of the value in op2 to Rsi
                ret.AddRange(OBList(EncOpcode(Rsi, op2.hardware_loc, 0, true, 0, 0x8d)));
            }

            int vt_size = tac.VTSize.Value;
            int cur_offset = 0;
            int cur_reps = 0;
            int cur_leftover = 0;
            int cur_blocksize = 8;

            /* do the move
             * 
             * dest address is in Rdi, source in Rsi */
            do
            {
                cur_reps = (vt_size - cur_offset) / cur_blocksize;
                cur_leftover = vt_size - cur_offset - cur_reps * cur_blocksize;

                /* store cur_reps to rcx */
                ret.AddRange(OBList(EncOpcode(0, Rcx, 3, true, 0, 0xc7),
                    ToByteArray(cur_reps)));

                /*do rep movsX */
                switch (cur_blocksize)
                {
                    case 8:
                        ret.AddRange(OBList(new byte[] { 0xf3, 0x48, 0xa5 }));
                        break;
                    case 4:
                        ret.AddRange(OBList(new byte[] { 0xf3, 0xa5 }));
                        break;
                    case 2:
                        ret.AddRange(OBList(new byte[] { 0x66, 0xf3, 0xa5 }));
                        break;
                    case 1:
                        ret.AddRange(OBList(new byte[] { 0xf3, 0xa4 }));
                        break;
                    default:
                        throw new NotSupportedException();
                }

                cur_offset += cur_reps * cur_blocksize;
                cur_blocksize /= 2;
            } while (cur_leftover > 0);

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_assign_virtftnptr_smem_dmem(ThreeAddressCode.Op op, var result, var op1, var op2, ThreeAddressCode tac, AssemblerState state)
        {
            /* Move the 128 bit virtftnptr object from op1 to result
             * 
             * We use xmm move to be slightly more efficient
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncOpcode(Xmm15, op1.hardware_loc, 0, false, 0, 0xf3, 0x0f, 0x6f), new x86_64_Instruction { opcode = "movdqu", Operand1 = Xmm15, Operand2 = op1.hardware_loc }));
            ret.Add(new CodeBlock(EncOpcode(Xmm15, result.hardware_loc, 0, false, 0, 0xf3, 0x0f, 0x7f), new x86_64_Instruction { opcode = "movdqu", Operand1 = result.hardware_loc, Operand2 = Xmm15 }));
            return ret;
        }

        IEnumerable<OutputBlock> x86_64_assign_to_virtftnptr_o1gprmem_o2_gprmem_dmem(ThreeAddressCode.Op op, var result, var op1, var op2, ThreeAddressCode tac, AssemblerState state)
        {
            /* Create a new 128 bit virtftnptr object from the values in op1 and op2 
             * 
             * lea rdi, [result]
             * mov [rdi], op1
             * mov [rdi + 8], op2
             * 
             * remember that x86_64_assign clobbers Rcx, we also clobber Rdi here
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncOpcode(Rdi, result.hardware_loc, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = result.hardware_loc }));
            x86_64_assign(new hardware_contentsof { base_loc = Rdi, const_offset = 0, size = 8 }, op1.hardware_loc, ret);
            x86_64_assign(new hardware_contentsof { base_loc = Rdi, const_offset = 8, size = 8 }, op2.hardware_loc, ret);
            return ret;
        }

        IEnumerable<OutputBlock> x86_64_assign_from_virtftn_ptr_o1mem_dgprmem(ThreeAddressCode.Op op, var result, var op1, var op2, ThreeAddressCode tac, AssemblerState state)
        {
            /* Extract the ptr field from a virtftnptr object
             * 
             * lea rdi, [op1]
             * mov dest, [op1]
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncOpcode(Rdi, op1.hardware_loc, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = op1.hardware_loc }));
            x86_64_assign(result.hardware_loc, new hardware_contentsof { base_loc = Rdi, const_offset = 0, size = 8 }, ret);
            
            return ret;
        }

        IEnumerable<OutputBlock> x86_64_assign_from_virtftn_thisadjust_o1mem_dgprmem(ThreeAddressCode.Op op, var result, var op1, var op2, ThreeAddressCode tac, AssemblerState state)
        {
            /* Extract the this_adjust field from a virtftnptr object
             * 
             * lea rdi, [op1]
             * mov dest, [op1]
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncOpcode(Rdi, op1.hardware_loc, 0, true, 0, 0x8d), new x86_64_Instruction { opcode = "lea", Operand1 = Rdi, Operand2 = op1.hardware_loc }));
            x86_64_assign(result.hardware_loc, new hardware_contentsof { base_loc = Rdi, const_offset = 8, size = 8 }, ret);

            return ret;
        }
    }
}
