﻿/* Copyright (C) 2016 by John Cronin
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

namespace binary_library.elf
{
    partial class ElfFile
    {
        /* x86_64 relocations */
        public const int R_X86_64_NONE = 0;
        public const int R_X86_64_64 = 1;
        public const int R_X86_64_PC32 = 2;
        public const int R_X86_64_GOT32 = 3;
        public const int R_X86_64_PLT32 = 4;
        public const int R_X86_64_COPY = 5;
        public const int R_X86_64_GLOB_DAT = 6;
        public const int R_X86_64_JUMP_SLOT = 7;
        public const int R_X86_64_RELATIVE = 8;
        public const int R_X86_64_GOTPCREL = 9;
        public const int R_X86_64_32 = 10;
        public const int R_X86_64_32S = 11;
        public const int R_X86_64_16 = 12;
        public const int R_X86_64_PC16 = 13;
        public const int R_X86_64_8 = 14;
        public const int R_X86_64_PC8 = 15;
        public const int R_X86_64_DTPMOD64 = 16;
        public const int R_X86_64_DTPOFF64 = 17;
        public const int R_X86_64_TPOFF64 = 18;
        public const int R_X86_64_TLSGD = 19;
        public const int R_X86_64_TLSLD = 20;
        public const int R_X86_64_DTPOFF32 = 21;
        public const int R_X86_64_GOTTPOFF = 22;
        public const int R_X86_64_TPOFF32 = 23;
        public const int R_X86_64_PC64 = 24;
        public const int R_X86_64_GOTOFF64 = 25;
        public const int R_X86_64_GOTPC32 = 26;
        public const int R_X86_64_SIZE32 = 32;
        public const int R_X86_64_SIZE64 = 33;
        public const int R_X86_64_GOTPC32_TLSDESC = 34;
        public const int R_X86_64_TLSDESC_CALL = 35;
        public const int R_X86_64_TLSDESC = 36;

        public const int R_386_NONE = 0;
        public const int R_386_32 = 1;
        public const int R_386_PC32 = 2;
        public const int R_386_GOT32 = 3;
        public const int R_386_PLT32 = 4;
        public const int R_386_COPY = 5;
        public const int R_386_GLOB_DAT = 6;
        public const int R_386_JMP_SLOT = 7;
        public const int R_386_RELATIVE = 8;
        public const int R_386_GOTOFF = 9;
        public const int R_386_GOTPC = 10;

        public class Rel_x86_64_32 : IRelocationType
        {
            public int Length
            {
                get { return 4; }
            }

            public ulong KeepMask
            {
                get { return 0; }
            }

            public ulong SetMask
            {
                get { return 0xffffffff; }
            }

            public string Name
            {
                get { return "R_X86_64_32"; }
            }

            public int Type
            {
                get { return (int)R_X86_64_32; }
            }

            public long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress + reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 32; } }
            public bool IsSigned
            { get { return false; } }
            public int BitOffset
            { get { return 0; } }
        }

        public class Rel_x86_64_32s : IRelocationType
        {
            public int Length
            {
                get { return 4; }
            }

            public ulong KeepMask
            {
                get { return 0; }
            }

            public ulong SetMask
            {
                get { return 0xffffffff; }
            }

            public string Name
            {
                get { return "R_X86_64_32S"; }
            }

            public int Type
            {
                get { return (int)R_X86_64_32S; }
            }

            public long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress + reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 32; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 0; } }
        }


        public class Rel_x86_64_pc32 : IRelocationType
        {
            public int Length
            {
                get { return 4; }
            }

            public ulong KeepMask
            {
                get { return 0; }
            }

            public ulong SetMask
            {
                get { return 0xffffffff; }
            }

            public string Name
            {
                get { return "R_X86_64_PC32"; }
            }

            public int Type
            {
                get { return (int)R_X86_64_PC32; }
            }

            public long Evaluate(IRelocation reloc)
            {
                // S + A - P
                return (long)(reloc.References.DefinedIn.LoadAddress + reloc.References.Offset) + reloc.Addend - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset);
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 32; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 0; } }
        }

        public class Rel_x86_64_64 : IRelocationType
        {
            public int Length
            {
                get { return 8; }
            }

            public ulong KeepMask
            {
                get { return 0; }
            }

            public ulong SetMask
            {
                get { return 0xffffffffffffffff; }
            }

            public string Name
            {
                get { return "R_X86_64_64"; }
            }

            public int Type
            {
                get { return (int)R_X86_64_64; }
            }

            public long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress + reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 64; } }
            public bool IsSigned
            { get { return false; } }
            public int BitOffset
            { get { return 0; } }
        }

        public class Rel_386_32 : IRelocationType
        {
            public int Length
            {
                get { return 4; }
            }

            public ulong KeepMask
            {
                get { return 0; }
            }

            public ulong SetMask
            {
                get { return 0xffffffff; }
            }

            public string Name
            {
                get { return "R_386_32"; }
            }

            public int Type
            {
                get { return (int)R_386_32; }
            }

            public long Evaluate(IRelocation reloc)
            {
                // S + A
                return (long)(reloc.References.DefinedIn.LoadAddress + reloc.References.Offset) + reloc.Addend;
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 32; } }
            public bool IsSigned
            { get { return false; } }
            public int BitOffset
            { get { return 0; } }
        }

        public class Rel_386_PC32 : IRelocationType
        {
            public int Length
            {
                get { return 4; }
            }

            public ulong KeepMask
            {
                get { return 0; }
            }

            public ulong SetMask
            {
                get { return 0xffffffff; }
            }

            public string Name
            {
                get { return "R_386_PC32"; }
            }

            public int Type
            {
                get { return (int)R_386_PC32; }
            }

            public long Evaluate(IRelocation reloc)
            {
                // S + A - P
                return (long)(reloc.References.DefinedIn.LoadAddress + reloc.References.Offset) + reloc.Addend - (long)(reloc.DefinedIn.LoadAddress + reloc.Offset);
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public int BitLength
            { get { return 32; } }
            public bool IsSigned
            { get { return true; } }
            public int BitOffset
            { get { return 0; } }
        }

        public class Rel_x86_64_TLS_TPOFF32 : IRelocationType
        {
            public int Length => throw new NotImplementedException();

            public ulong KeepMask => throw new NotImplementedException();

            public ulong SetMask => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public int Type => 23;

            public int BitLength => throw new NotImplementedException();

            public bool IsSigned => throw new NotImplementedException();

            public int BitOffset => throw new NotImplementedException();

            public long Evaluate(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }
        }

        public class Rel_x86_64_TLS_DTPOFF32 : IRelocationType
        {
            public int Length => throw new NotImplementedException();

            public ulong KeepMask => throw new NotImplementedException();

            public ulong SetMask => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public int Type => 21;

            public int BitLength => throw new NotImplementedException();

            public bool IsSigned => throw new NotImplementedException();

            public int BitOffset => throw new NotImplementedException();

            public long Evaluate(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }
        }

        public class Rel_386_TLS_DTPOFF32 : IRelocationType
        {
            public int Length => throw new NotImplementedException();

            public ulong KeepMask => throw new NotImplementedException();

            public ulong SetMask => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public int Type => 36;

            public int BitLength => throw new NotImplementedException();

            public bool IsSigned => throw new NotImplementedException();

            public int BitOffset => throw new NotImplementedException();

            public long Evaluate(IRelocation reloc)
            {
                throw new NotImplementedException();
            }

            public long GetCurrentValue(IRelocation reloc)
            {
                throw new NotImplementedException();
            }
        }
    }
}
