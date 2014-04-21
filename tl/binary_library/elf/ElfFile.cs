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

namespace binary_library.elf
{
    public partial class ElfFile : BinaryFile, IBinaryFile, IBinaryFileTypeName
    {
        enum ElfClass { ELFCLASSNONE = 0, ELFCLASS32 = 1, ELFCLASS62 = 2 };
        enum ElfData { ELFDATANONE = 0, ELFDATA2LSB = 1, ELFDATA2MSB = 2 };

        ElfClass ec = ElfClass.ELFCLASSNONE;
        ElfData ed = ElfData.ELFDATANONE;
        uint e_type;
        int e_machine;
        ulong e_entry;
        long e_phoff;
        long e_shoff;
        uint e_flags;
        int e_ehsize;
        int e_phentsize;
        int e_phnum;
        int e_shentsize;
        int e_shnum;
        int e_shstrndx;

        ISection AbsSection;
        ISection CommonSection;

        const int EM_NONE = 0;
        const int EM_M32 = 1;
        const int EM_SPARC = 2;
        const int EM_386 = 3;
        const int EM_68K = 4;
        const int EM_88K = 5;
        const int EM_860 = 7;
        const int EM_MIPS = 8;
        const int EM_ARM = 40;
        const int EM_X86_64 = 62;

        Dictionary<int, string> MachineTypes;
        Dictionary<ElfClass, string> BinaryTypes;

        public override Bitness Bitness
        {
            get
            {
                switch (ec)
                {
                    case ElfClass.ELFCLASS32:
                        return binary_library.Bitness.Bits32;
                    case ElfClass.ELFCLASS62:
                        return binary_library.Bitness.Bits64;
                    default:
                        return binary_library.Bitness.BitsUnknown;
                }
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string[] GetSupportedFileTypes()
        {
            return new string[] { "elf", "elf32", "elf64" };
        }

        public override IProgramHeader ProgramHeader
        {
            get { throw new NotImplementedException(); }
        }

        protected override void Write(System.IO.BinaryWriter w)
        {
            base.Write(w);
        }

        public override void Init()
        {
            AbsSection = new DummySection("ABS", this);
            CommonSection = new DummySection("COMMON", this);

            MachineTypes = new Dictionary<int, string>();
            MachineTypes[EM_NONE] = "none";
            MachineTypes[EM_386] = "i386";
            MachineTypes[EM_ARM] = "arm";
            MachineTypes[EM_X86_64] = "x86_64";

            BinaryTypes = new Dictionary<ElfClass, string>();
            BinaryTypes[ElfClass.ELFCLASS32] = "elf";
            BinaryTypes[ElfClass.ELFCLASS62] = "elf64";

            InitRelocTypes();
        }

        protected override void Read(System.IO.BinaryReader r)
        {
            Init();

            // Read e_ident
            byte[] e_ident = r.ReadBytes(16);
            if ((e_ident[0] != 0x7f) || (e_ident[1] != (byte)'E') || (e_ident[2] != (byte)'L') || (e_ident[3] != (byte)'F'))
                throw new Exception("Not an ELF file");
            ec = (ElfClass)e_ident[4];
            ed = (ElfData)e_ident[5];

            if ((ec != ElfClass.ELFCLASS32) && (ec != ElfClass.ELFCLASS62))
                throw new Exception("Invalid ELF class: " + e_ident[4].ToString());
            if (ed != ElfData.ELFDATA2LSB)
                throw new Exception("Invalid ELF data type: " + e_ident[5].ToString());

            if (e_ident[6] != 1)
                throw new Exception("Invalid ELF version: " + e_ident[6].ToString());

            // Read the rest of the file header
            switch (ec)
            {
                case ElfClass.ELFCLASS32:
                    ReadElf32FileHeader(r);
                    break;

                case ElfClass.ELFCLASS62:
                    ReadElf64FileHeader(r);
                    break;
            }

            // Identify the arch, OS and machine type
            os = "none";
            binary_type = BinaryTypes[ec];
            architecture = MachineTypes[e_machine];

            // First iterate through and identify the offsets of each section
            List<long> sect_offsets = new List<long>();
            for (int i = 0; i < e_shnum; i++)
            {
                long sh_start = e_shoff + i * e_shentsize;
                r.BaseStream.Seek(sh_start, System.IO.SeekOrigin.Begin);

                switch (ec)
                {
                    case ElfClass.ELFCLASS32:
                        r.BaseStream.Seek(16, System.IO.SeekOrigin.Current);
                        sect_offsets.Add(r.ReadInt32());
                        break;
                    case ElfClass.ELFCLASS62:
                        r.BaseStream.Seek(24, System.IO.SeekOrigin.Current);
                        sect_offsets.Add(r.ReadInt64());
                        break;
                }
            }

            // Now load the section data
            List<SectionHeader> sect_headers = new List<SectionHeader>();
            for (int i = 0; i < e_shnum; i++)
            {
                // First load the section header
                long sh_start = e_shoff + i * e_shentsize;
                r.BaseStream.Seek(sh_start, System.IO.SeekOrigin.Begin);
                SectionHeader sh = null;
                switch (ec)
                {
                    case ElfClass.ELFCLASS32:
                        sh = ReadElf32SectionHeader(r);
                        break;
                    case ElfClass.ELFCLASS62:
                        sh = ReadElf64SectionHeader(r);
                        break;
                }
                sect_headers.Add(sh);

                // Now get the name
                string name = "unknown";
                if (e_shstrndx != 0)
                {
                    long name_offset = sect_offsets[e_shstrndx] +
                        sh.sh_name;
                    name = ReadString(r, name_offset);
                }

                // Now load the actual section
                // Decide on the type of section
                ISection sect = null;
                switch (sh.sh_type)
                {
                    case SectionHeader.SHT_NULL:
                        break;
                    case SectionHeader.SHT_PROGBITS:
                    case SectionHeader.SHT_REL:
                    case SectionHeader.SHT_RELA:
                    case SectionHeader.SHT_HASH:
                    case SectionHeader.SHT_DYNAMIC:
                    case SectionHeader.SHT_NOTE:
                    case SectionHeader.SHT_STRTAB:
                        sect = new ContentsSection(this);
                        break;
                    case SectionHeader.SHT_SYMTAB:
                    case SectionHeader.SHT_DYNSYM:
                        sect = new ElfSymbolSection(this);
                        break;
                    case SectionHeader.SHT_NOBITS:
                        sect = new BssSection(this);
                        break;
                }

                if (sect != null)
                {
                    // Interpret the section type
                    sect.IsWriteable = ((sh.sh_flags & SectionHeader.SHF_WRITE) != 0);
                    sect.IsAlloc = ((sh.sh_flags & SectionHeader.SHF_ALLOC) != 0);
                    sect.IsExecutable = ((sh.sh_flags & SectionHeader.SHF_EXECINSTR) != 0);

                    sect.LoadAddress = sh.sh_addr;
                    sect.Name = name;
                    sect.AddrAlign = sh.sh_addralign;

                    // Load the contents if it has any
                    if (sect.HasData)
                    {
                        sect.Length = sh.sh_size;
                        r.BaseStream.Seek(sh.sh_offset, System.IO.SeekOrigin.Begin);
                        for (long l = 0; l < sh.sh_size; l++)
                            sect.Data[(int)l] = r.ReadByte();
                    }


                }
                sections.Add(sect);
            }

            // Interpret symbols
            for (int i = 0; i < e_shnum; i++)
            {
                SectionHeader sh = sect_headers[i];
                ISection sect = sections[i];

                if ((sh.sh_type == SectionHeader.SHT_SYMTAB) && sect.HasData)
                {
                    int cur_sym = 0;
                    for (long p = 0; p < sect.Length; p += sh.sh_entsize)
                    {
                        ElfSymbol s = null;

                        switch (ec)
                        {
                            case ElfClass.ELFCLASS32:
                                s = ReadElf32Symbol(sect.Data, (int)p);
                                break;
                            case ElfClass.ELFCLASS62:
                                s = ReadElf64Symbol(sect.Data, (int)p);
                                break;
                        }

                        if (s != null)
                        {
                            // Load up the name of the symbol
                            if (sh.sh_link != 0)
                                s.Name = ReadString(sections[sh.sh_link].Data, s.st_name);

                            // Offset
                            s.Offset = s.st_value;

                            // Length
                            s.Size = s.st_size;

                            // Type
                            switch (s.st_info >> 4)
                            {
                                case ElfSymbol.STB_GLOBAL:
                                    s.Type = SymbolType.Global;
                                    break;
                                case ElfSymbol.STB_LOCAL:
                                    s.Type = SymbolType.Local;
                                    break;
                                case ElfSymbol.STB_WEAK:
                                    s.Type = SymbolType.Weak;
                                    break;
                            }

                            // DefinedIn
                            if (s.st_shndx == 0)
                            {
                                s.DefinedIn = null;
                                s.Type = SymbolType.Undefined;
                            }
                            else if (s.st_shndx == -15)
                            {
                                // SHN_ABS
                                s.DefinedIn = AbsSection;
                            }
                            else if (s.st_shndx == -14)
                            {
                                // SHN_COMMON
                                s.DefinedIn = CommonSection;
                            }
                            else
                                s.DefinedIn = sections[s.st_shndx];
                        }

                        if(!symbols.Contains(s))
                            symbols.Add(s);
                        ((ElfSymbolSection)sect).elf_syms[cur_sym++] = s;
                    }
                }
            }

            // Interpret relocations
            for (int i = 0; i < e_shnum; i++)
            {
                SectionHeader sh = sect_headers[i];
                ISection sect = sections[i];

                switch (sh.sh_type)
                {
                    case SectionHeader.SHT_REL:
                        ReadRelocationSection(sh, sect, sections, false);
                        break;
                    case SectionHeader.SHT_RELA:
                        ReadRelocationSection(sh, sect, sections, true);
                        break;
                }
            }
        }

        private ElfSymbol ReadElf64Symbol(IList<byte> data, int p)
        {
            ElfSymbol s = new ElfSymbol();

            s.st_name = ListReader.ReadInt32(data, ref p);
            s.st_info = ListReader.ReadByte(data, ref p);
            s.st_other = ListReader.ReadByte(data, ref p);
            s.st_shndx = ListReader.ReadInt16(data, ref p);
            s.st_value = ListReader.ReadUInt64(data, ref p);
            s.st_size = ListReader.ReadInt64(data, ref p);

            return s;
        }

        private ElfSymbol ReadElf32Symbol(IList<byte> data, int p)
        {
            ElfSymbol s = new ElfSymbol();

            s.st_name = ListReader.ReadInt32(data, ref p);
            s.st_value = ListReader.ReadUInt32(data, ref p);
            s.st_size = ListReader.ReadInt32(data, ref p);
            s.st_info = ListReader.ReadByte(data, ref p);
            s.st_other = ListReader.ReadByte(data, ref p);
            s.st_shndx = ListReader.ReadInt16(data, ref p);

            return s;
        }

        private void ReadRelocationSection(SectionHeader sh, ISection sect, List<ISection> sections, bool is_rela)
        {
            ElfSymbolSection sym_sect = sections[sh.sh_link] as ElfSymbolSection;
            if (sym_sect == null)
                throw new Exception("Invalid section referenced");
            ISection defined_in = sections[sh.sh_info];

            for (long p = 0; p < sect.Length; p += sh.sh_entsize)
            {
                ElfRelocation r = null;

                switch (ec)
                {
                    case ElfClass.ELFCLASS32:
                        r = ReadElf32Relocation(sect.Data, (int)p, is_rela);
                        break;
                    case ElfClass.ELFCLASS62:
                        r = ReadElf64Relocation(sect.Data, (int)p, is_rela);
                        break;
                }

                if (r != null)
                {
                    /* Identify the symbol it references */
                    ISymbol sym = sym_sect.elf_syms[r.r_sym];
                    r.References = sym;

                    /* Identify the section the relocation is in */
                    r.DefinedIn = defined_in;
                    r.Offset = r.r_offset;

                    /* Identify the type */
                    r.Type = reloc_types[e_machine][r.r_type];


                    /* Identify the addend */
                    if (is_rela)
                        r.Addend = r.r_addend;
                    else
                        r.Addend = r.Type.GetCurrentValue(r);
                }

                relocs.Add(r);
            }
        }

        private ElfRelocation ReadElf64Relocation(IList<byte> data, int p, bool is_rela)
        {
            ElfRelocation r = new ElfRelocation();

            r.r_offset = ListReader.ReadUInt64(data, ref p);
            r.r_type = ListReader.ReadInt32(data, ref p);
            r.r_sym = ListReader.ReadInt32(data, ref p);
            if (is_rela)
                r.r_addend = ListReader.ReadInt64(data, ref p);

            return r;
        }

        private ElfRelocation ReadElf32Relocation(IList<byte> data, int p, bool is_rela)
        {
            ElfRelocation r = new ElfRelocation();

            r.r_offset = ListReader.ReadUInt32(data, ref p);
            r.r_type = ListReader.ReadByte(data, ref p);
            r.r_sym = ListReader.ReadInt16(data, ref p);
            r.r_sym <<= 8;
            r.r_sym += ListReader.ReadByte(data, ref p);
            if (is_rela)
                r.r_addend = ListReader.ReadInt32(data, ref p);

            return r;
        }

        private string ReadString(System.IO.BinaryReader r, long offset)
        {
            long cur_offset = r.BaseStream.Position;
            r.BaseStream.Seek(offset, System.IO.SeekOrigin.Begin);
            string ret = ReadString(r);
            r.BaseStream.Seek(cur_offset, System.IO.SeekOrigin.Begin);
            return ret;
        }

        private string ReadString(System.IO.BinaryReader r)
        {
            List<char> c = new List<char>();
            byte b;
            while ((b = r.ReadByte()) != 0)
                c.Add((char)b);
            return new string(c.ToArray());
        }

        private string ReadString(IList<byte> d, long offset)
        {
            int o = (int)offset;
            List<char> c = new List<char>();
            byte b;
            while ((b = ListReader.ReadByte(d, ref o)) != 0)
                c.Add((char)b);
            return new string(c.ToArray());
        }

        class SectionHeader
        {
            internal const uint SHT_NULL = 0;
            internal const uint SHT_PROGBITS = 1;
            internal const uint SHT_SYMTAB = 2;
            internal const uint SHT_STRTAB = 3;
            internal const uint SHT_RELA = 4;
            internal const uint SHT_HASH = 5;
            internal const uint SHT_DYNAMIC = 6;
            internal const uint SHT_NOTE = 7;
            internal const uint SHT_NOBITS = 8;
            internal const uint SHT_REL = 9;
            internal const uint SHT_SHLIB = 10;
            internal const uint SHT_DYNSYM = 11;

            internal const uint SHF_WRITE = 1;
            internal const uint SHF_ALLOC = 2;
            internal const uint SHF_EXECINSTR = 4;

            internal int sh_name;
            internal uint sh_type;
            internal ulong sh_flags;
            internal ulong sh_addr;
            internal long sh_offset;
            internal long sh_size;
            internal int sh_link;
            internal int sh_info;
            internal long sh_addralign;
            internal long sh_entsize;
        }

        class ElfSymbolSection : ContentsSection, ISection
        {
            public ElfSymbolSection(IBinaryFile binary_file) : base(binary_file) { }
            internal Dictionary<int, ISymbol> elf_syms = new Dictionary<int, ISymbol>();
        }

        class ElfSymbol : binary_library.Symbol, ISymbol
        {
            internal const uint STB_LOCAL = 0;
            internal const uint STB_GLOBAL = 1;
            internal const uint STB_WEAK = 2;

            internal const uint STT_NOTYPE = 0;
            internal const uint STT_OBJECT = 1;
            internal const uint STT_FUNC = 2;
            internal const uint STT_SECTION = 3;
            internal const uint STT_FILE = 4;

            internal int st_name;
            internal uint st_info;
            internal uint st_other;
            internal int st_shndx;
            internal ulong st_value;
            internal long st_size;
        }

        class ElfRelocation : Relocation, IRelocation
        {
            internal ulong r_offset;
            internal long r_addend;
            internal int r_sym;
            internal int r_type;
        }

        private SectionHeader ReadElf64SectionHeader(System.IO.BinaryReader r)
        {
            SectionHeader sh = new SectionHeader();
            sh.sh_name = r.ReadInt32();
            sh.sh_type = r.ReadUInt32();
            sh.sh_flags = r.ReadUInt64();
            sh.sh_addr = r.ReadUInt64();
            sh.sh_offset = r.ReadInt64();
            sh.sh_size = r.ReadInt64();
            sh.sh_link = r.ReadInt32();
            sh.sh_info = r.ReadInt32();
            sh.sh_addralign = r.ReadInt64();
            sh.sh_entsize = r.ReadInt64();
            return sh;
        }

        private SectionHeader ReadElf32SectionHeader(System.IO.BinaryReader r)
        {
            SectionHeader sh = new SectionHeader();
            sh.sh_name = r.ReadInt32();
            sh.sh_type = r.ReadUInt32();
            sh.sh_flags = r.ReadUInt32();
            sh.sh_addr = r.ReadUInt32();
            sh.sh_offset = r.ReadInt32();
            sh.sh_size = r.ReadInt32();
            sh.sh_link = r.ReadInt32();
            sh.sh_info = r.ReadInt32();
            sh.sh_addralign = r.ReadInt32();
            sh.sh_entsize = r.ReadInt32();
            return sh;
        }

        private void ReadElf64FileHeader(System.IO.BinaryReader r)
        {
            e_type = r.ReadUInt16();
            e_machine = r.ReadUInt16();
            r.ReadUInt32();     // e_version
            e_entry = r.ReadUInt64();
            e_phoff = r.ReadInt64();
            e_shoff = r.ReadInt64();
            e_flags = r.ReadUInt32();
            e_ehsize = r.ReadInt16();
            e_phentsize = r.ReadInt16();
            e_phnum = r.ReadInt16();
            e_shentsize = r.ReadInt16();
            e_shnum = r.ReadInt16();
            e_shstrndx = r.ReadInt16();
        }

        private void ReadElf32FileHeader(System.IO.BinaryReader r)
        {
            e_type = r.ReadUInt16();
            e_machine = r.ReadUInt16();
            r.ReadUInt32();     // e_version
            e_entry = r.ReadUInt32();
            e_phoff = r.ReadInt32();
            e_shoff = r.ReadInt32();
            e_flags = r.ReadUInt32();
            e_ehsize = r.ReadInt16();
            e_phentsize = r.ReadInt16();
            e_phnum = r.ReadInt16();
            e_shentsize = r.ReadInt16();
            e_shnum = r.ReadInt16();
            e_shstrndx = r.ReadInt16();
        }

        public override ISection GetGlobalSection()
        {
            return AbsSection;
        }
    }
}
