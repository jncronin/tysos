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

namespace tydb
{
    class x86_64_dbgarch : dbgarch
    {
        internal override bool Init(string[] features)
        {
            registers = new register[] {
                new register { name = "rax", length = 8, id = 0 },
                new register { name = "rbx", length = 8, id = 1 },
                new register { name = "rcx", length = 8, id = 2 },
                new register { name = "rdx", length = 8, id = 3 },
                new register { name = "rsi", length = 8, id = 4 },
                new register { name = "rdi", length = 8, id = 5 },
                new register { name = "rbp", length = 8, id = 6 },
                new register { name = "rsp", length = 8, id = 7 },
                new register { name = "r8", length = 8, id = 8 },
                new register { name = "r9", length = 8, id = 9 },
                new register { name = "r10", length = 8, id = 10 },
                new register { name = "r11", length = 8, id = 11 },
                new register { name = "r12", length = 8, id = 12 },
                new register { name = "r13", length = 8, id = 13 },
                new register { name = "r14", length = 8, id = 14 },
                new register { name = "r15", length = 8, id = 15 },
                new register { name = "rip", length = 8, id = 16 },
                new register { name = "rflags", length = 4, id = 17 }
            };

            // register ID of the program counter
            PC_id = 16;

            // lsb architecture
            is_lsb = true;

            // disassembler
            disasm = new tydisasm.x86_64.x86_64_disasm();

            // assembler
            ass = libtysila.Assembler.CreateAssembler(libtysila.Assembler.ParseArchitectureString("x86_64s-elf64-tysos"), null, null, null);

            address_size = 8;
            data_size = 8;

            return true;
        }
    }
}
