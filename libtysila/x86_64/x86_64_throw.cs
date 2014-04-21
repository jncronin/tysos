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
        IEnumerable<OutputBlock> x86_64_ldmethinfo(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            x86_64_assign(result.hardware_loc, state.methinfo_pointer, ret);
            return ret;
        }

        IEnumerable<OutputBlock> x86_64_throw(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();

            List<OutputBlock> call_block = new List<OutputBlock>();
            byte jmp_dist = 0;

            string target = "throw";
            CallConv cc;
            if (op1.type == var.var_type.Const)
            {
                target = "sthrow";
                cc = callconv_sthrow;
            }
            else
                cc = callconv_throw;

            hardware_location methinfo = state.methinfo_pointer;
            if (methinfo == null)
                methinfo = new const_location { c = 0 };

            CallEx ce = new CallEx(var.Null, new var[] { op1, new var { hardware_loc = methinfo } }, target, cc);
            ce.used_locations = tac.used_locations;
            call_block.AddRange(x86_64_call(ce.Operator, ce.Result, ce.Operand1, ce.Operand2, ce, state));

            /*x86_64_push(op1, call_block, state);
            call_block.Add(new CodeBlock { Code = new byte[] { 0xe8 } });
            call_block.Add(new RelocationBlock
                {
                    RelType = ElfLib.Elf64_Rela_Shdr.Elf64_Rela.RelocationType.R_X86_64_PLT32,
                    Size = 4,
                    Target = target,
                    Value = -4
                });
            call_block.Add(new CodeBlock { Code = EncOpcode(0, Rsp, 3, true, 0, 0x83) });
            call_block.Add(new CodeBlock { Code = new byte[] { 0x8 } });*/

            jmp_dist = (byte)GetLongestBlockLength(call_block);

            switch (op)
            {
                case ThreeAddressCode.Op.throweq:
                    ret.Add(new CodeBlock(new byte[] { 0x75, jmp_dist }, new x86_64_Instruction { opcode = "jne", Operand1 = new const_location { c = jmp_dist } }));
                    break;
                case ThreeAddressCode.Op.throwne:
                    ret.Add(new CodeBlock(new byte[] { 0x74, jmp_dist }, new x86_64_Instruction { opcode = "je", Operand1 = new const_location { c = jmp_dist } }));
                    break;
                case ThreeAddressCode.Op.throw_ovf:
                    ret.Add(new CodeBlock(new byte[] { 0x71, jmp_dist }, new x86_64_Instruction { opcode = "jno", Operand1 = new const_location { c = jmp_dist } }));
                    break;
                case ThreeAddressCode.Op.throw_ovf_un:
                    ret.Add(new CodeBlock(new byte[] { 0x73, jmp_dist }, new x86_64_Instruction { opcode = "jnc", Operand1 = new const_location { c = jmp_dist } }));
                    break;
                case ThreeAddressCode.Op.throwge_un:
                    ret.Add(new CodeBlock(new byte[] { 0x72, jmp_dist }, new x86_64_Instruction { opcode = "jb", Operand1 = new const_location { c = jmp_dist } }));
                    break;
                case ThreeAddressCode.Op.throwg_un:
                    ret.Add(new CodeBlock(new byte[] { 0x76, jmp_dist }, new x86_64_Instruction { opcode = "jbe", Operand1 = new const_location { c = jmp_dist } }));
                    break;
            }
            ret.AddRange(call_block);
            return ret;
        }
    }
}
