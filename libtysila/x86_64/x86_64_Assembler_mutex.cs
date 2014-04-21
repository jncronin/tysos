/* Copyright (C) 2011 by John Cronin
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
        IEnumerable<OutputBlock> x86_64_try_acquire_i4_dgpr_s1gpr_s2gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* Do cmpxchg [op1], op2
             * 
             * Unset locks are 0
             * 
             * This clobbers eax
             * Set eax to 0 first
             * If eax == [op1]
             *  - [op1] = op2
             *  - ZF set
             * If not equal
             *  - eax = [op1]
             *  - ZF clear
             * To get result
             *  - setz al
             *  - movzx dest, al
             */

            List<OutputBlock> ret = new List<OutputBlock>();

            hardware_location h_op1 = op1.hardware_loc;
            hardware_location h_op2 = op2.hardware_loc;

            if (h_op1.Equals(Rax))
            {
                x86_64_assign(Rdi, Rax, ret);
                h_op1 = Rdi;
            }
            if (h_op2.Equals(Rax))
            {
                x86_64_assign(Rdi, Rax, ret);
                h_op2 = Rdi;
            }
            
            ret.Add(new CodeBlock(EncOpcode(Rax, Rax, 3, true, 0, 0x31), new x86_64_Instruction { opcode = "xor", Operand1 = Rax, Operand2 = Rax }));
            ret.Add(new CodeBlock(EncOpcode(h_op2, h_op1, 0, false, 0, 0xf0, 0x0f, 0xb1), new x86_64_Instruction { opcode = "lock cmpxchg", Operand1 = new hardware_contentsof { base_loc = h_op1 }, Operand2 = h_op2 }));
            ret.Add(new CodeBlock(EncOpcode(0, Rax, 3, false, 0, true, 0x0f, 0x94), new x86_64_Instruction { opcode = "setz", Operand1 = Rax }));
            ret.Add(new CodeBlock(EncOpcode(result.hardware_loc, Rax, 3, false, 0, true, 0x0f, 0xb6), new x86_64_Instruction { opcode = "movzx", Operand1 = result.hardware_loc, Operand2 = Rax }));

            return ret;
        }

        IEnumerable<OutputBlock> x86_64_release_i4_s1gpr_s2rax(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* For release, we want to compare [op1] with op2, and set [op1] to 0 if true
             * 
             * to do this, eax = op2
             * rsi = 0
             * lock cmpxchg [op1], rsi
             */

            List<OutputBlock> ret = new List<OutputBlock>();

            hardware_location h_op1 = op1.hardware_loc;

            if (h_op1.Equals(Rsi))
            {
                x86_64_assign(Rdi, Rsi, ret);
                h_op1 = Rdi;
            }

            ret.Add(new CodeBlock(EncOpcode(Rsi, Rsi, 3, true, 0, 0x31), new x86_64_Instruction { opcode = "xor", Operand1 = Rsi, Operand2 = Rsi }));
            ret.Add(new CodeBlock(EncOpcode(Rsi, h_op1, 0, false, 0, 0xf0, 0x0f, 0xb1), new x86_64_Instruction { opcode = "lock cmpxchg", Operand1 = new hardware_contentsof { base_loc = h_op1 }, Operand2 = Rsi }));

            return ret;
        }
    }
}
