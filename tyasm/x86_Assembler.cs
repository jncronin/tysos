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

namespace tyasm
{
    partial class x86_Assembler : Assembler
    {
        protected override AssemblerState CreateAssemblerState(binary_library.IBinaryFile file)
        {
            AssemblerState ret =  base.CreateAssemblerState(file);
            ret.cur_bitness = binary_library.Bitness.Bits32;
            return ret;
        }

        public x86_Assembler()
        {
            // Init registers

            regs["al"] = new Reg { id = 0 };
            regs["cl"] = new Reg { id = 1 };
            regs["dl"] = new Reg { id = 2 };
            regs["bl"] = new Reg { id = 3 };
            regs["ah"] = new Reg { id = 4 };
            regs["ch"] = new Reg { id = 5 };
            regs["dh"] = new Reg { id = 6 };
            regs["bh"] = new Reg { id = 7 };

            regs["ax"] = new Reg { id = 0 };
            regs["cx"] = new Reg { id = 1 };
            regs["dx"] = new Reg { id = 2 };
            regs["bx"] = new Reg { id = 3 };
            regs["sp"] = new Reg { id = 4 };
            regs["bp"] = new Reg { id = 5 };
            regs["si"] = new Reg { id = 6 };
            regs["di"] = new Reg { id = 7 };

            regs["eax"] = new Reg { id = 0 };
            regs["ecx"] = new Reg { id = 1 };
            regs["edx"] = new Reg { id = 2 };
            regs["ebx"] = new Reg { id = 3 };
            regs["esp"] = new Reg { id = 4 };
            regs["ebp"] = new Reg { id = 5 };
            regs["esi"] = new Reg { id = 6 };
            regs["edi"] = new Reg { id = 7 };

            regs["rax"] = new Reg { id = 0 };
            regs["rcx"] = new Reg { id = 1 };
            regs["rdx"] = new Reg { id = 2 };
            regs["rbx"] = new Reg { id = 3 };
            regs["rsp"] = new Reg { id = 4 };
            regs["rbp"] = new Reg { id = 5 };
            regs["rsi"] = new Reg { id = 6 };
            regs["rdi"] = new Reg { id = 7 };

            regs["r8"] = new Reg { id = 0, is_high = true };
            regs["r9"] = new Reg { id = 1, is_high = true };
            regs["r10"] = new Reg { id = 2, is_high = true };
            regs["r11"] = new Reg { id = 3, is_high = true };
            regs["r12"] = new Reg { id = 4, is_high = true };
            regs["r13"] = new Reg { id = 5, is_high = true };
            regs["r14"] = new Reg { id = 6, is_high = true };
            regs["r15"] = new Reg { id = 7, is_high = true };

            regs["cr0"] = new Reg { id = 0 };
            regs["cr1"] = new Reg { id = 1 };
            regs["cr2"] = new Reg { id = 2 };
            regs["cr3"] = new Reg { id = 3 };
            regs["cr4"] = new Reg { id = 4 };
            regs["cr5"] = new Reg { id = 5 };
            regs["cr6"] = new Reg { id = 6 };
            regs["cr7"] = new Reg { id = 7 };
        }
    }
}
