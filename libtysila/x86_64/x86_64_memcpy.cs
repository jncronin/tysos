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

/* Support copying of value types */

using System;
using System.Collections.Generic;
using System.Text;
using libasm;

namespace libtysila
{
    partial class x86_64_Assembler
    {
        List<OutputBlock> do_memcpy(int size)
        {
            int chunk = 8;
            if (ia == IA.i586)
                chunk = 4;
            while ((size % chunk) != 0)
                chunk /= 2;

            int chunks = size / chunk;

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock { Code = EncOpcode(0, Rcx, 3, (ia == IA.x86_64), 0, 0xc7) }); // mov rcx, chunks
            ret.Add(new CodeBlock { Code = ToByteArraySignExtend(chunks, 4) });

            switch (chunk)
            {
                case 8:
                    ret.Add(new CodeBlock { Code = new byte[] { 0xf3, 0x48, 0xa5 } });
                    break;
                case 4:
                    ret.Add(new CodeBlock { Code = new byte[] { 0xf3, 0xa5 } });
                    break;
                case 2:
                    ret.Add(new CodeBlock { Code = new byte[] { 0x66, 0xf3, 0xa5 } });
                    break;
                case 1:
                    ret.Add(new CodeBlock { Code = new byte[] { 0xf3, 0xa4 } });
                    break;
                default:
                    throw new Exception("Invalid chunk size");
            }

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_memcpy_smem_dmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* lea source to rsi, dest to rdi */
            List<OutputBlock> ret = new List<OutputBlock>();

            ret.Add(new CodeBlock { Code = EncOpcode(Rsi, op1.hardware_loc, 3, (ia == IA.x86_64), 0, 0x8d) });
            ret.Add(new CodeBlock { Code = EncOpcode(Rdi, result.hardware_loc, 3, (ia == IA.x86_64), 0, 0x8d) });

            ret.AddRange(do_memcpy(tac.VTSize.Value));            

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_memcpy_srsi_dmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* lea dest to rdi */
            List<OutputBlock> ret = new List<OutputBlock>();

            ret.Add(new CodeBlock { Code = EncOpcode(Rdi, result.hardware_loc, 3, true, 0, 0x8d) });

            ret.AddRange(do_memcpy(tac.VTSize.Value));

            return ret;
        }
        
        IEnumerable<OutputBlock> x86_64_memcpy_smem_drdi(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* lea source to rsi */
            List<OutputBlock> ret = new List<OutputBlock>();

            ret.Add(new CodeBlock { Code = EncOpcode(Rsi, op1.hardware_loc, 3, true, 0, 0x8d) });

            ret.AddRange(do_memcpy(tac.VTSize.Value));

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_memcpy_srsi_drdi(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return do_memcpy(tac.VTSize.Value);
        }

        IEnumerable<OutputBlock> x86_64_zeromem_o1rdi_o2const(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Zero a portion of memory

            int size = (int)op2.constant_val;
            int count = 0;
            int block_size = 0;
            if((size % 8) == 0)
            {
                block_size = 8;
                count = size / 8;
            }
            else if((size % 4) == 0)
            {
                block_size = 4;
                count = size / 4;
            }
            else if((size % 2) == 0)
            {
                block_size = 2;
                count = size / 2;
            }
            else
            {
                block_size = 1;
                count = size;
            }

            // Load rcx with the count
            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncAddOpcode(Rcx, false, 0xb8, ToByteArrayZeroExtend(count, 4)), new x86_64_Instruction { opcode = "mov", Operand1 = Rcx, Operand2 = new const_location { c = count } }));
            // Load rax with 0
            ret.Add(new CodeBlock(new byte[] { 0x48, 0x31, 0xc0 }, new x86_64_Instruction { opcode = "xor", Operand1 = Rax, Operand2 = Rax }));

            // Do rep stos
            switch (block_size)
            {
                case 1:
                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0xaa }, new x86_64_Instruction { opcode = "rep stosb" }));
                    break;
                case 2:
                    ret.Add(new CodeBlock(new byte[] { 0x66, 0xf3, 0xab }, new x86_64_Instruction { opcode = "rep stosw" }));
                    break;
                case 4:
                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0xab }, new x86_64_Instruction { opcode = "rep stosd" }));
                    break;
                case 8:
                    ret.Add(new CodeBlock(new byte[] { 0xf3, 0x48, 0xab }, new x86_64_Instruction { opcode = "rep stosq" }));
                    break;
            }
            
            return ret;
        }
    }
}
