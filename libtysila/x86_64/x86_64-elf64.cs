/* Copyright (C) 2012 by John Cronin
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

// constants for elf64 output

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila.x86_64
{
    public class x86_64_elf64
    {
        public const UInt32 R_X86_64_NONE = 0;
        public const UInt32 R_X86_64_64 = 1;
        public const UInt32 R_X86_64_PC32 = 2;
        public const UInt32 R_X86_64_GOT32 = 3;
        public const UInt32 R_X86_64_PLT32 = 4;
        public const UInt32 R_X86_64_COPY = 5;
        public const UInt32 R_X86_64_GLOB_DAT = 6;
        public const UInt32 R_X86_64_JUMP_SLOT = 7;
        public const UInt32 R_X86_64_RELATIVE = 8;
        public const UInt32 R_X86_64_GOTPCREL = 9;
        public const UInt32 R_X86_64_32 = 10;
        public const UInt32 R_X86_64_32S = 11;
        public const UInt32 R_X86_64_16 = 12;
        public const UInt32 R_X86_64_PC16 = 13;
        public const UInt32 R_X86_64_8 = 14;
        public const UInt32 R_X86_64_PC8 = 15;
        public const UInt32 R_X86_64_PC64 = 24;
        public const UInt32 R_X86_64_GOTPC32 = 26;
    }

    public class x86_64_elf32
    {
        public const UInt32 R_386_NONE = 0;
        public const UInt32 R_386_32 = 1;
        public const UInt32 R_386_PC32 = 2;
        public const UInt32 R_386_GOT32 = 3;
        public const UInt32 R_386_PLT32 = 4;
        public const UInt32 R_386_COPY = 5;
        public const UInt32 R_386_GLOB_DAT = 6;
        public const UInt32 R_386_JMP_SLOT = 7;
        public const UInt32 R_386_RELATIVE = 8;
        public const UInt32 R_386_GOTOFF = 9;
        public const UInt32 R_386_GOTPC = 10;
    }
}
