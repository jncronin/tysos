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
        IEnumerable<OutputBlock> x86_64_set(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            byte codebyte;
            switch (op)
            {
                case ThreeAddressCode.Op.seteq:
                    codebyte = 0x94;
                    break;
                case ThreeAddressCode.Op.setg:
                    if (tac.is_float)
                        codebyte = 0x93;        // float comparison ([u]comiss/d sets only ZF, PF and CF)
                    else
                        codebyte = 0x9f;
                    break;
                case ThreeAddressCode.Op.seta:
                    codebyte = 0x93;
                    break;
                case ThreeAddressCode.Op.setb:
                    codebyte = 0x92;
                    break;
                case ThreeAddressCode.Op.setl:
                    if (tac.is_float)
                        codebyte = 0x92;        // float comparison ([u]comiss/d sets only ZF, PF and CF)
                    else
                        codebyte = 0x9c;
                    break;
                default:
                    throw new NotSupportedException();
            }
            // the set command only affects the first byte, therefore we need to movsx it afterwards
            return OBList(EncOpcode(2, result.hardware_loc, 3, false, 0, 0x0f, codebyte),
                EncOpcode(result.hardware_loc, result.hardware_loc, 3, false, 0, 0x0f, 0xbe));
        }

        IEnumerable<OutputBlock> x86_64_cmp_i8_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, true, 0, 0x3b));
        }

        IEnumerable<OutputBlock> x86_64_cmp_i8_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op2.hardware_loc, op1.hardware_loc, 3, true, 0, 0x39));
        }

        IEnumerable<OutputBlock> x86_64_cmp_i4_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op1.hardware_loc, op2.hardware_loc, 3, false, 0, 0x3b));
        }

        IEnumerable<OutputBlock> x86_64_cmp_i4_gprmem_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(op2.hardware_loc, op1.hardware_loc, 3, false, 0, 0x39));
        }


        IEnumerable<OutputBlock> x86_64_cmp_i4_gprmem_const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            int cv = 0;
            if (op2.constant_val.GetType() == typeof(IntPtr))
                cv = ((IntPtr)op2.constant_val).ToInt32();
            else
                cv = Convert.ToInt32(op2.constant_val);

            if (op1.hardware_loc.Equals(Rax))
                return OBList(new byte[] { 0x3d }, ToByteArray(Convert.ToInt32(cv)));
            return OBList(EncOpcode(7, op1.hardware_loc, 3, false, 0, 0x81),
                ToByteArray(Convert.ToInt32(cv)));
        }

        IEnumerable<OutputBlock> x86_64_br_ehclause(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // To leave exception clauses we do a ret, therefore to enter them we need to do a call
            // The stack is assured to be empty before these clauses, so we do not have to save any registers

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(new byte[] { 0xe8 }, new x86_64_Instruction { opcode = "call" }));
            ret.Add(new NodeReference { block_id = ((BrEx)tac).Block_Target, length = 4, offset = -4 });
            return ret;
        }

        IEnumerable<OutputBlock> x86_64_br(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> obshort = new List<OutputBlock>();
            List<OutputBlock> oblong = new List<OutputBlock>();
            switch (op)
            {
                case ThreeAddressCode.Op.br:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0xeb } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0xe9 } });
                    break;

                case ThreeAddressCode.Op.ba:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0x77 } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x87 } });
                    break;

                case ThreeAddressCode.Op.bae:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0x73 } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x83 } });
                    break;

                case ThreeAddressCode.Op.bb:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0x72 } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x82 } });
                    break;

                case ThreeAddressCode.Op.bbe:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0x76 } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x86 } });
                    break;

                case ThreeAddressCode.Op.beq:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0x74 } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x84 } });
                    break;

                case ThreeAddressCode.Op.bg:
                    if (tac.is_float)
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x77 } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x87 } });
                    }
                    else
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x7f } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x8f } });
                    }
                    break;

                case ThreeAddressCode.Op.bge:
                    if (tac.is_float)
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x73 } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x83 } });
                    }
                    else
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x7d } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x8d } });
                    }
                    break;

                case ThreeAddressCode.Op.bl:
                    if (tac.is_float)
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x72 } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x82 } });
                    }
                    else
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x7c } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x8c } });
                    }
                    break;

                case ThreeAddressCode.Op.ble:
                    if (tac.is_float)
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x76 } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x86 } });
                    }
                    else
                    {
                        obshort.Add(new CodeBlock { Code = new List<byte> { 0x7e } });
                        oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x8e } });
                    }
                    break;

                case ThreeAddressCode.Op.bne:
                    obshort.Add(new CodeBlock { Code = new List<byte> { 0x75 } });
                    oblong.Add(new CodeBlock { Code = new List<byte> { 0x0f, 0x85 } });
                    break;

                default:
                    throw new NotSupportedException();
            }

            int target_node = ((BrEx)tac).Block_Target;

            obshort.Add(new NodeReference { block_id = target_node, length = 1, offset = -1 });
            oblong.Add(new NodeReference { block_id = target_node, length = 4, offset = -4 });

            return new List<OutputBlock> { new BlockChoice { Choices = new List<IList<OutputBlock>> { obshort, oblong } } };
        }
    }
}
