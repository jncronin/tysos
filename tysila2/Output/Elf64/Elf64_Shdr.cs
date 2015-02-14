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
using System.IO;
using System.Reflection;

namespace Elf64
{
    class SectionType
    {
        public const UInt32 SHT_NULL = 0;
        public const UInt32 SHT_PROGBITS = 1;
        public const UInt32 SHT_SYMTAB = 2;
        public const UInt32 SHT_STRTAB = 3;
        public const UInt32 SHT_RELA = 4;
        public const UInt32 SHT_HASH = 5;
        public const UInt32 SHT_DYNAMIC = 6;
        public const UInt32 SHT_NOTE = 7;
        public const UInt32 SHT_NOBITS = 8;
        public const UInt32 SHT_REL = 9;
        public const UInt32 SHT_SHLIB = 10;
        public const UInt32 SHT_DYNSYM = 11;
        public const UInt32 SHT_GROUP = 17;
    }

    class SectionFlags
    {
        public const UInt64 SHF_WRITE = 1;
        public const UInt64 SHF_ALLOC = 2;
        public const UInt64 SHF_EXECINSTR = 4;
        public const UInt64 SHF_GROUP = 0x200;
    }

    class GroupFlags
    {
        public const UInt32 GRP_COMDAT = 0x1;
    }

    class Elf32_Shdr
    {
        Elf32_Shdr() { throw new NotImplementedException(); }
    }

    class Elf64_Shdr
    {
        public UInt32 sh_name;
        public string Name;
        public UInt32 sh_type;
        public UInt64 sh_flags;
        public UInt64 sh_addr;
        public UInt64 sh_offset;

        public ushort index;

        public void SetName(string name, Elf64_String_Shdr strsect)
        {
            Name = name;
            sh_name = strsect.GetOffset(name);
        }

        public void AddTo(IList<Elf64_Shdr> shdrs)
        {
            this.index = Convert.ToUInt16(shdrs.Count);
            shdrs.Add(this);
        }

        public virtual UInt64 sh_size { get {
            if (data == null)
                return 0;
            if (data is ICollection<byte>) return (ulong)((ICollection<byte>)data).Count;
            UInt64 count = 0;
            foreach (byte b in data)
                count++;
            return count;
        }
        }
        public virtual List<byte> data { get { return _data; } set { _data = value; } }
        public virtual UInt32 sh_link { get { return _sh_link; } set { _sh_link = value; } }
        public virtual UInt32 sh_info { get { return _sh_info; } set { _sh_info = value; } }
        public UInt64 sh_addralign = 4;
        public virtual UInt64 sh_entsize { get { return _sh_entsize; } set { _sh_entsize = value; } }

        public static int GetLength()
        { return 64; }

        private UInt32 _sh_link;
        private UInt32 _sh_info;
        private UInt64 _sh_entsize;
        private List<byte> _data = new List<byte>();

        public void Write(Stream s)
        {
            Elf64Writer.Write(s, sh_name);
            Elf64Writer.Write(s, sh_type);
            Elf64Writer.Write(s, sh_flags);
            Elf64Writer.Write(s, sh_addr);
            Elf64Writer.Write(s, sh_offset);
            Elf64Writer.Write(s, sh_size);
            Elf64Writer.Write(s, sh_link);
            Elf64Writer.Write(s, sh_info);
            Elf64Writer.Write(s, sh_addralign);
            Elf64Writer.Write(s, sh_entsize);
        }
    }

    class Elf64_String_Shdr : Elf64_Shdr
    {
        private Dictionary<string, UInt32> strings = new Dictionary<string, UInt32>();

        public UInt32 GetOffset(string str)
        {
            try
            {
                return strings[str];
            }
            catch (KeyNotFoundException)
            {
                if (data == null)
                {
                    data = new List<byte>();
                    data.Add((byte)0);
                }

                uint offset = (uint)data.Count;
                byte[] new_str = Encoding.ASCII.GetBytes(str);
                data.AddRange(new_str);
                data.Add((byte)0);
                strings.Add(str, offset);
                return offset;
            }
        }

        public Elf64_String_Shdr()
        {
            sh_type = SectionType.SHT_STRTAB;
            sh_flags = 0;
            sh_addr = 0;
            sh_link = 0;
            sh_info = 0;
            sh_addralign = 1;
            sh_entsize = 0;
            data = new List<byte> { (byte)0 };
        }
    }

    class Elf64_Null_Shdr : Elf64_Shdr
    {
        public Elf64_Null_Shdr()
        {
            sh_type = SectionType.SHT_NULL;
            sh_flags = 0;
            sh_addr = 0;
            sh_link = 0;
            sh_info = 0;
            sh_addralign = 0;
            sh_entsize = 0;
            sh_name = 0;
        }
    }

    class Elf64_Symbol_Shdr : Elf64_Shdr
    {
        public class Elf64_Sym : IEquatable<Elf64_Sym>, libtysila.ISymbol
        {
            public class BindingFlags
            {
                public const byte STB_LOCAL = 0;
                public const byte STB_GLOBAL = 0x10;
                public const byte STB_WEAK = 0x20;
            }

            public class SymbolTypes
            {
                public const byte STT_NOTYPE = 0;
                public const byte STT_OBJECT = 1;
                public const byte STT_FUNC = 2;
                public const byte STT_SECTION = 3;
                public const byte STT_FILE = 4;
            }

            public UInt32 st_name;
            public byte st_info;
            public byte st_other = 0;
            public UInt16 st_shndx;
            public UInt64 st_value;
            public UInt64 st_size;

            public string name;

            public static int GetLength()
            { return 24; }

            public bool IsWeak
            {
                get { return (st_info & 0xf0) == BindingFlags.STB_WEAK; }
            }

            #region IEquatable<Elf64_Sym> Members

            public bool Equals(Elf64_Sym other)
            {
                if (other == null)
                    return false;
                if (other.name == this.name)
                    return true;
                return false;
            }

            #endregion

            public void Write(object s)
            {
                Elf64Writer.Write(s, st_name);
                Elf64Writer.Write(s, st_info);
                Elf64Writer.Write(s, st_other);
                Elf64Writer.Write(s, st_shndx);
                Elf64Writer.Write(s, st_value);
                Elf64Writer.Write(s, st_size);
            }

            public bool Weak
            {
                get { return IsWeak; }
                set { throw new NotImplementedException(); }
            }

            public string Name
            {
                get { return name; }
                set { throw new NotImplementedException(); }
            }

            public int Offset
            {
                get { return (int)st_value; }
                set { throw new NotImplementedException(); }
            }

            public int Length
            {
                get { return (int)st_size; }
                set { st_size = (ulong)value; }
            }
        }

        public List<Elf64_Sym> defined_syms = new List<Elf64_Sym>();
        internal Dictionary<string, int> name_to_sym = new Dictionary<string, int>();
   
        public override UInt64 sh_entsize { get { return (ulong)Elf64_Sym.GetLength(); } }
        public override uint sh_info { get { return _sh_info; } }
        public uint _sh_info = 0;
        public override List<byte> data
        {
            get
            {
                List<byte> ret = new List<byte>();
                foreach (Elf64_Sym sym in defined_syms)
                    sym.Write(ret);
                return ret;
            }
        }

        public Elf64_Symbol_Shdr(Elf64_Ehdr ehdr)
        {
            sh_type = SectionType.SHT_SYMTAB;
            sh_flags = 0;
            sh_addr = 0;
            sh_link = ehdr.e_symstrndx;
            sh_addralign = 8;
        }
    }

    class Elf64_Rela_Shdr : Elf64_Shdr
    {
        public class Elf64_Rela
        {
            public class RelocationType
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
            }

            public Elf64_Rela(ulong offset, uint reloctype, long addend, string target)
            {

                reloc_target = target;
                r_offset = offset;
                reloc_type = reloctype;
                r_addend = addend;
            }

            public UInt64 r_offset;
            public UInt32 reloc_type;
            public UInt32 sym_ndx;
            public Int64 r_addend;

            public string reloc_target;

            public static int GetLength() { return 24; }

            public void Write(object s)
            {
                Elf64Writer.Write(s, r_offset);
                Elf64Writer.Write(s, reloc_type);
                Elf64Writer.Write(s, sym_ndx);
                Elf64Writer.Write(s, r_addend);
            }
        }

        public List<Elf64_Rela> relocs = new List<Elf64_Rela>();

        public override ulong sh_entsize { get { return (ulong)Elf64_Rela.GetLength(); } }
        public override List<byte> data
        {
            get
            {
                List<byte> ret = new List<byte>();
                foreach (Elf64_Rela rel in relocs)
                    rel.Write(ret);
                return ret;
            }
        }

        public Elf64_Rela_Shdr(Elf64_Ehdr ehdr, ushort data_sect_ndx)
        {
            sh_type = SectionType.SHT_RELA;
            sh_flags = 0;
            sh_addr = 0;
            sh_link = ehdr.e_symsndx;
            sh_info = data_sect_ndx;
            sh_addralign = 8;
        }
    }
}
