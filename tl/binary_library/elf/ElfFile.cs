/* Copyright (C) 2013-2016 by John Cronin
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
using System.IO;
using System.Text;

namespace binary_library.elf
{
    public partial class ElfFile : BinaryFile, IBinaryFile, IBinaryFileTypeName
    {
        enum ElfClass { ELFCLASSNONE = 0, ELFCLASS32 = 1, ELFCLASS64 = 2 };
        enum ElfData { ELFDATANONE = 0, ELFDATA2LSB = 1, ELFDATA2MSB = 2 };

        public bool CreateHashSection = false;
        public bool CreateDynamicSection = false;

        bool use_rela = true;

        public ElfFile(Bitness b)
        {
            switch(b)
            {
                case Bitness.Bits64:
                    ec = ElfClass.ELFCLASS64;
                    break;
                case Bitness.Bits32:
                case Bitness.BitsUnknown:
                    ec = ElfClass.ELFCLASS32;
                    break;
                default:
                    throw new Exception("Invalid ELF bitness: " + b.ToString());
            }
        }

        public ElfFile() : this(Bitness.Bits32) { }

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
        const int EM_JCA = 0x434a;

        Dictionary<int, string> MachineTypes;
        Dictionary<ElfClass, string> BinaryTypes;
        Dictionary<string, int> RMachineTypes;
        Dictionary<string, ElfClass> RBinaryTypes;
        Dictionary<string, ElfData> RDataTypes;
        Dictionary<string, string> MachToBinaryTypes;

        public override Bitness Bitness
        {
            get
            {
                switch (ec)
                {
                    case ElfClass.ELFCLASS32:
                        return binary_library.Bitness.Bits32;
                    case ElfClass.ELFCLASS64:
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
            return new string[] { "elf", "elf32", "elf64", ".o", ".obj" };
        }

        public override IProgramHeader ProgramHeader
        {
            get { throw new NotImplementedException(); }
        }

        protected override void Write(System.IO.BinaryWriter w)
        {
            Init();

            // Some options require a dynamic section
            if (CreateHashSection && IsExecutable)
                CreateDynamicSection = true;

            // First, build a list of output sections
            List<SectionHeader> osects = new List<SectionHeader>();
            Dictionary<ISection, int> osect_map = new Dictionary<ISection, int>();
            osects.Add(new NullSection());

            StringSection strtab = new StringSection();
            StringSection shstrtab = new StringSection();

            SectionHeader sh_dyn = null;
            SectionHeader sh_hash = null;

            foreach(var sect in sections)
            {
                SectionHeader sh = new SectionHeader();
                if (sect.Name == ".dynamic")
                {
                    if (CreateDynamicSection)
                        sh_dyn = sh;
                    else
                        continue;
                }
                if(sect.Name == ".hash")
                {
                    if (CreateHashSection)
                        sh_hash = sh;
                    else
                        continue;
                }

                sh.sh_name = AllocateString(sect.Name, shstrtab);
                if (sect.HasData)
                    sh.sh_type = 1;
                else
                    sh.sh_type = 8;
                sh.sh_flags = 0;
                if (sect.IsWriteable)
                    sh.sh_flags |= 0x1;
                if (sect.IsAlloc)
                    sh.sh_flags |= 0x2;
                if (sect.IsExecutable)
                    sh.sh_flags |= 0x4;
                sh.sh_addr = sect.LoadAddress;
                sh.sh_size = sect.Length;

                sh.sh_link = 0;
                sh.sh_info = 0;
                sh.sh_addralign = sect.AddrAlign;
                sh.sh_entsize = 0;

                osect_map[sect] = osects.Count;
                osects.Add(sh);                
            }

            strtab.sh_name = AllocateString(".strtab", shstrtab);
            int strtab_idx = osects.Count;
            osects.Add(strtab);

            shstrtab.sh_name = AllocateString(".shstrtab", shstrtab);
            int shstrtab_idx = osects.Count;
            e_shstrndx = shstrtab_idx;
            osects.Add(shstrtab);

            SectionHeader symtab = new SectionHeader();
            symtab.sh_name = AllocateString(".symtab", shstrtab);
            symtab.sh_link = strtab_idx;
            symtab.sh_type = 2;
            int symtab_idx = osects.Count;
            osects.Add(symtab);

            // Build symbol table
            List<ElfSymbol> osyms = new List<ElfSymbol>();
            osyms.Add(new ElfSymbol());
            Dictionary<ISymbol, int> sym_map = new Dictionary<ISymbol, int>();
            Dictionary<string, int> sym_str_map = new Dictionary<string, int>();

            // Add local symbols first
            foreach(var sym in GetSymbols())
            { 
                if (sym.Type == SymbolType.Local)
                    AddSymbol(sym, strtab, osyms, sym_map, sym_str_map, osect_map);
            }
            int last_local = osyms.Count;
            foreach(var sym in GetSymbols())
            {
                if (sym.Type != SymbolType.Local)
                    AddSymbol(sym, strtab, osyms, sym_map, sym_str_map, osect_map);
            }
            symtab.sh_info = last_local;

            // Write out relocations into the appropriate sections
            Dictionary<ISection, SectionHeader> reloc_sections = new Dictionary<ISection, SectionHeader>();
            Dictionary<ISection, List<ElfRelocation>> elfrelocs = new Dictionary<ISection, List<ElfRelocation>>();

            foreach (var reloc in relocs)
            {
                SectionHeader reloc_sect_idx = null;
                if (reloc_sections.TryGetValue(reloc.DefinedIn, out reloc_sect_idx) == false)
                {
                    reloc_sect_idx = new SectionHeader();
                    reloc_sect_idx.index = osects.Count;
                    if (use_rela)
                    {
                        reloc_sect_idx.sh_name = AllocateString(".rela" + reloc.DefinedIn.Name, shstrtab);
                        if (ec == ElfClass.ELFCLASS32)
                            reloc_sect_idx.sh_entsize = 12;
                        else
                            reloc_sect_idx.sh_entsize = 24;
                        reloc_sect_idx.sh_type = 4;
                    }
                    else
                    {
                        reloc_sect_idx.sh_name = AllocateString(".rel" + reloc.DefinedIn.Name, shstrtab);
                        if (ec == ElfClass.ELFCLASS32)
                            reloc_sect_idx.sh_entsize = 8;
                        else
                            reloc_sect_idx.sh_entsize = 16;
                        reloc_sect_idx.sh_type = 9;
                    }
                    reloc_sect_idx.sh_link = symtab_idx;
                    reloc_sect_idx.sh_info = osect_map[reloc.DefinedIn];
                    if (Bitness == Bitness.Bits64)
                        reloc_sect_idx.sh_entsize = 24;
                    osects.Add(reloc_sect_idx);
                    reloc_sections[reloc.DefinedIn] = reloc_sect_idx;
                    elfrelocs[reloc.DefinedIn] = new List<ElfRelocation>();
                }

                var er = new ElfRelocation();
                if(sym_str_map.TryGetValue(reloc.References.Name, out er.r_sym) == false)
                {
                    // Create a new undefined symbol for the relocation
                    var ext_sym = new Symbol {
                        Name = reloc.References.Name,
                        definedin = null,
                        ObjectType = SymbolObjectType.Unknown,
                        Type = SymbolType.Global,
                        Offset = reloc.References.Offset,
                        Size = reloc.References.Size
                    };
                    er.r_sym = AddSymbol(ext_sym, strtab, osyms, sym_map, sym_str_map, osect_map);
                }
                er.r_addend = reloc.Addend;
                er.r_offset = reloc.Offset;
                er.r_type = reloc.Type.Type;
                elfrelocs[reloc.DefinedIn].Add(er);

                /* If using REL relocations, put addend in original
                section */
                if(use_rela == false)
                {
                    var s = reloc.DefinedIn;
                    var size = reloc.Type.Length;

                    for(int i = 0; i < size; i++)
                    {
                        var cur_offset = (int)reloc.Offset + i;
                        s.Data[cur_offset] = (byte)((reloc.Addend >> (i * 8)) & 0xff);
                    }
                }
            }

            // Write out file header
            // e_ident
            w.Write((byte)0x7f);
            w.Write((byte)'E');
            w.Write((byte)'L');
            w.Write((byte)'F');
            w.Write((byte)ec);
            w.Write((byte)ed);
            w.Write((byte)1);
            for (int i = 0; i < 9; i++)
                w.Write((byte)0);
            // store fh_start because we will re-write the header later
            int fh_start = (int)w.BaseStream.Position;
            switch(ec)
            {
                case ElfClass.ELFCLASS32:
                    WriteElf32FileHeader(w);
                    break;
                case ElfClass.ELFCLASS64:
                    WriteElf64FileHeader(w);
                    break;
                default:
                    throw new Exception("Invalid ELF class");
            }

            // Now write out the section data
            foreach(var s in sections)
            {
                // align up to addralign
                int sect_idx;
                if (!osect_map.TryGetValue(s, out sect_idx))
                    continue;
                var sh = osects[sect_idx];
                if (sh.sh_addralign != 0)
                {
                    while ((w.BaseStream.Position % sh.sh_addralign) != 0)
                        w.Write((byte)0);
                }

                sh.sh_offset = w.BaseStream.Position;

                if (sh == sh_hash)
                {
                    WriteHash(w, osyms, 0, 0);
                    sh.sh_size = w.BaseStream.Position - sh.sh_offset;
                    //WriteHashTable(sh_hash, osyms, w);
                }
                else if (sh == sh_dyn)
                    WriteDynTable(sh_dyn, sh_hash, w);
                else if (s.HasData == false)
                    continue;
                else
                {
                    // write out data
                    foreach (byte b in s.Data)
                        w.Write(b);
                }
            }

            // Write out string tables
            while ((w.BaseStream.Position % 16) != 0)
                w.Write((byte)0);
            strtab.sh_offset = w.BaseStream.Position;
            foreach (byte b in strtab.oput)
                w.Write(b);
            strtab.sh_size = strtab.oput.Count;

            while ((w.BaseStream.Position % 16) != 0)
                w.Write((byte)0);
            shstrtab.sh_offset = w.BaseStream.Position;
            foreach (byte b in shstrtab.oput)
                w.Write(b);
            shstrtab.sh_size = shstrtab.oput.Count;

            // Write out symbol table
            while ((w.BaseStream.Position % 16) != 0)
                w.Write((byte)0);
            symtab.sh_offset = w.BaseStream.Position;
            foreach (var sym in osyms)
            {
                switch (ec)
                {
                    case ElfClass.ELFCLASS32:
                        WriteElf32Symbol(sym, w);
                        break;
                    case ElfClass.ELFCLASS64:
                        WriteElf64Symbol(sym, w);
                        break;
                }
            }

            // Write out relocation tables
            foreach(var rsh_ent in reloc_sections)
            {
                while ((w.BaseStream.Position % 16) != 0)
                    w.Write((byte)0);

                var rlist = elfrelocs[rsh_ent.Key];
                var rsh = rsh_ent.Value;

                rsh.sh_offset = w.BaseStream.Position;
                foreach(var r in rlist)
                {
                    switch (ec)
                    {
                        case ElfClass.ELFCLASS32:
                            WriteElf32Relocation(r, w);
                            break;
                        case ElfClass.ELFCLASS64:
                            WriteElf64Relocation(r, w);
                            break;
                    }
                }
                rsh.sh_size = w.BaseStream.Position - rsh.sh_offset;
            }

            // Fill in file type specific details in various structures
            switch (ec)
            {
                case ElfClass.ELFCLASS32:
                    symtab.sh_entsize = 16;
                    symtab.sh_size = osyms.Count * symtab.sh_entsize;
                    e_shentsize = 40;
                    e_phentsize = 32;
                    break;
                case ElfClass.ELFCLASS64:
                    symtab.sh_entsize = 24;
                    symtab.sh_size = osyms.Count * symtab.sh_entsize;
                    e_shentsize = 64;
                    e_phentsize = 56;
                    break;
            }

            // Write out section header table
            while ((w.BaseStream.Position % 16) != 0)
                w.Write((byte)0);

            e_shoff = w.BaseStream.Position;
            foreach(var sect in osects)
            {
                switch(ec)
                {
                    case ElfClass.ELFCLASS32:
                        WriteElf32Section(sect, w);
                        break;
                    case ElfClass.ELFCLASS64:
                        WriteElf64Section(sect, w);
                        break;
                }
            }

            // Write program headers
            e_phnum = 0;
            if (IsExecutable)
            {
                while ((w.BaseStream.Position % 16) != 0)
                    w.Write((byte)0);

                e_phoff = w.BaseStream.Position;
                foreach (var sect in sections)
                {
                    ElfProgramHeader ph = new ElfProgramHeader();
                    ph.p_type = ElfProgramHeader.PT_LOAD;
                    ph.p_offset = (ulong)osects[osect_map[sect]].sh_offset;
                    ph.p_vaddr = sect.LoadAddress;
                    ph.p_paddr = 0;
                    ph.p_filesz = sect.HasData ? (ulong)sect.Data.Count : 0UL;
                    ph.p_memsz = (ulong)sect.Length;
                    ph.p_flags = 0;
                    if (sect.IsExecutable)
                        ph.p_flags |= ElfProgramHeader.PF_X;
                    if (sect.IsWriteable)
                        ph.p_flags |= ElfProgramHeader.PF_W;
                    ph.p_flags |= ElfProgramHeader.PF_R;
                    ph.p_align = (ulong)sect.AddrAlign;

                    if (ph.p_memsz != 0)
                    {
                        switch (ec)
                        {
                            case ElfClass.ELFCLASS32:
                                WriteElf32Segment(ph, w);
                                break;
                            case ElfClass.ELFCLASS64:
                                WriteElf64Segment(ph, w);
                                break;
                        }
                        e_phnum++;
                    }
                }
            }
            if (e_phnum == 0)
                e_phoff = 0;

            // Rewrite file header
            e_shnum = osects.Count;
            if (is_exec)
                e_type = 2;
            else
                e_type = 1;

            if(EntryPoint != null && IsExecutable)
            {
                var s = FindSymbol(EntryPoint);
                if (s != null && s.DefinedIn != null)
                    e_entry = s.Offset + s.DefinedIn.LoadAddress;
                else
                {
                    // default to offset 0x0 in text
                    var sect = FindSection(".text");
                    if(sect == null)
                    {
                        // default to 0x0
#if HAVE_SYSTEM
                        Console.WriteLine("Entry point " + EntryPoint +
                            " not found, defaulting to 0x0");
#endif
                        e_entry = 0;
                    }
                    else
                    {
#if HAVE_SYSTEM
                        Console.WriteLine("Entry point " + EntryPoint +
                            " not found, defaulting to 0x" +
                            sect.LoadAddress.ToString("X"));
#endif
                        e_entry = sect.LoadAddress;
                    }
                }
            }

            int cur_pos = (int)w.BaseStream.Position;
            w.Seek(fh_start, System.IO.SeekOrigin.Begin);
            switch(ec)
            {
                case ElfClass.ELFCLASS32:
                    WriteElf32FileHeader(w);
                    break;
                case ElfClass.ELFCLASS64:
                    WriteElf64FileHeader(w);
                    break;
            }
            w.Seek(cur_pos, System.IO.SeekOrigin.Begin);
        }

        private void WriteDynTable(SectionHeader sh_dyn, SectionHeader sh_hash, BinaryWriter w)
        {
            throw new NotImplementedException();
        }

        private void WriteHashTable(SectionHeader sh_hash, List<ElfSymbol> osyms, BinaryWriter w)
        {
            throw new NotImplementedException();
        }

        private int AddSymbol(ISymbol sym, StringSection strtab,
            List<ElfSymbol> osyms, Dictionary<ISymbol, int> sym_map,
            Dictionary<string, int> sym_str_map,
            Dictionary<ISection, int> sect_map)
        {
            ElfSymbol esym = new ElfSymbol();
            esym.Name = sym.Name;
            esym.st_name = AllocateString(sym.Name, strtab);
            esym.st_value = sym.Offset;

            if (IsExecutable && sym.DefinedIn != null)
                esym.st_value += sym.DefinedIn.LoadAddress;

            esym.st_size = sym.Size;
            esym.st_bind = 0;
            switch(sym.Type)
            {
                case SymbolType.Global:
                    esym.st_bind = 1;
                    break;
                case SymbolType.Weak:
                    esym.st_bind = 2;
                    break;                        
            }
            esym.st_type = 0;
            switch (sym.ObjectType)
            {
                case SymbolObjectType.Object:
                    esym.st_type = 1;
                    break;
                case SymbolObjectType.Function:
                    esym.st_type = 2;
                    break;
            }
            if (sym.DefinedIn == null)
                esym.st_shndx = 0;
            else
            {
                if (!sect_map.TryGetValue(sym.DefinedIn, out esym.st_shndx))
                    return -1;
            }

            int ret = osyms.Count;
            sym_map[sym] = ret;
            sym_str_map[sym.Name] = ret;
            osyms.Add(esym);
            return ret;
        }

        private static int AllocateString(string name, StringSection strtab)
        {
            if (strtab.StringCache == null)
                strtab.StringCache = new Dictionary<string, int>();
            else if (strtab.StringCache.ContainsKey(name))
                return strtab.StringCache[name];

            if (strtab.cur_offset == 0)
            {
                strtab.oput.Add(0);
                strtab.cur_offset++;
            }

            int ret = strtab.cur_offset;
            foreach (char c in name)
            {
                strtab.oput.Add((byte)c);
                strtab.cur_offset++;
            }
            strtab.oput.Add(0);
            strtab.cur_offset++;

            strtab.StringCache[name] = ret;
            return ret;
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
            MachineTypes[EM_JCA] = "jca";

            BinaryTypes = new Dictionary<ElfClass, string>();
            BinaryTypes[ElfClass.ELFCLASS32] = "elf";
            BinaryTypes[ElfClass.ELFCLASS64] = "elf64";

            RMachineTypes = new Dictionary<string, int>();
            foreach (var kvp in MachineTypes)
                RMachineTypes[kvp.Value] = kvp.Key;
            RMachineTypes["x86"] = EM_386;

            RBinaryTypes = new Dictionary<string, ElfClass>();
            foreach (var kvp in BinaryTypes)
                RBinaryTypes[kvp.Value] = kvp.Key;

            RDataTypes = new Dictionary<string, ElfData>();
            RDataTypes["i386"] = ElfData.ELFDATA2LSB;
            RDataTypes["x86"] = ElfData.ELFDATA2LSB;
            RDataTypes["arm"] = ElfData.ELFDATA2LSB;
            RDataTypes["x86_64"] = ElfData.ELFDATA2LSB;
            RDataTypes["jca"] = ElfData.ELFDATA2LSB;

            MachToBinaryTypes = new Dictionary<string, string>();
            MachToBinaryTypes["x86_64"] = "elf64";

            InitRelocTypes();
        }

        public override string Architecture
        {
            get
            {
                return base.Architecture;
            }

            set
            {
                base.Architecture = value;
                e_machine = RMachineTypes[value];
                ed = RDataTypes[value];

                string bt;
                if (MachToBinaryTypes.TryGetValue(value, out bt))
                    BinaryType = bt;

                switch(e_machine)
                {
                    case EM_386:
                        use_rela = false;
                        break;
                }
            }
        }

        public override string BinaryType
        {
            get
            {
                return base.BinaryType;
            }

            set
            {
                base.BinaryType = value;
                ec = RBinaryTypes[value];
            }
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

            if ((ec != ElfClass.ELFCLASS32) && (ec != ElfClass.ELFCLASS64))
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

                case ElfClass.ELFCLASS64:
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
                    case ElfClass.ELFCLASS64:
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
                    case ElfClass.ELFCLASS64:
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
                    sect.Length = sh.sh_size;
                    if (sect.HasData)
                    {
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
                            case ElfClass.ELFCLASS64:
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
                            switch (s.st_bind)
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
                            switch(s.st_type)
                            {
                                case ElfSymbol.STT_FUNC:
                                    s.ObjectType = SymbolObjectType.Function;
                                    break;
                                case ElfSymbol.STT_OBJECT:
                                    s.ObjectType = SymbolObjectType.Object;
                                    break;
                                default:
                                    s.ObjectType = SymbolObjectType.Unknown;
                                    break;                                    
                            }

                            // DefinedIn
                            if (s.st_shndx == 0)
                            {
                                s.definedin = null;
                                s.Type = SymbolType.Undefined;
                            }
                            else if (s.st_shndx == -15)
                            {
                                // SHN_ABS
                                AbsSection.AddSymbol(s);
                            }
                            else if (s.st_shndx == -14)
                            {
                                // SHN_COMMON
                                CommonSection.AddSymbol(s);
                            }
                            else
                            {
                                sections[s.st_shndx].AddSymbol(s);
                            }
                        }

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
            s.st_bind = s.st_info >> 4;
            s.st_type = s.st_info & 0xf;

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
            s.st_bind = s.st_info >> 4;
            s.st_type = s.st_info & 0xf;

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
                    case ElfClass.ELFCLASS64:
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
            uint r_info = ListReader.ReadUInt32(data, ref p);
            r.r_sym = (int)(r_info >> 8);
            r.r_type = (int)(r_info & 0xff);
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

        class NullSection : SectionHeader
        {
            public NullSection()
            {
                sh_name = 0;
                sh_type = 0;
                sh_flags = 0;
                sh_addr = 0;
                sh_offset = 0;
                sh_size = 0;
                sh_link = 0;
                sh_info = 0;
                sh_addralign = 0;
                sh_entsize = 0;
            }
        }

        class StringSection : SectionHeader
        {
            internal Dictionary<string, int> StringCache;
            internal int cur_offset = 0;
            internal List<byte> oput = new List<byte>();

            public StringSection()
            {
                sh_type = 3;
            }
        }

        class ElfProgramHeader
        {
            internal const uint PT_NULL = 0;
            internal const uint PT_LOAD = 1;
            internal const uint PF_X = 1;
            internal const uint PF_W = 2;
            internal const uint PF_R = 4;

            internal uint p_type;
            internal ulong p_offset;
            internal ulong p_vaddr;
            internal ulong p_paddr;
            internal ulong p_filesz;
            internal ulong p_memsz;
            internal uint p_flags;
            internal ulong p_align;
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

            internal int index;
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
            internal uint st_bind;
            internal uint st_type;
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

        private void WriteElf32FileHeader(System.IO.BinaryWriter w)
        {
            e_ehsize = 52;
            w.Write((ushort)e_type);
            w.Write((ushort)e_machine);
            w.Write((uint)1);   // e_version
            w.Write((uint)e_entry);
            w.Write((int)e_phoff);
            w.Write((int)e_shoff);
            w.Write((uint)e_flags);
            w.Write((ushort)e_ehsize);
            w.Write((ushort)e_phentsize);
            w.Write((ushort)e_phnum);
            w.Write((ushort)e_shentsize);
            w.Write((ushort)e_shnum);
            w.Write((ushort)e_shstrndx);
        }

        private void WriteElf64FileHeader(System.IO.BinaryWriter w)
        {
            e_ehsize = 64;
            w.Write((ushort)e_type);
            w.Write((ushort)e_machine);
            w.Write((uint)1);   // e_version
            w.Write((ulong)e_entry);
            w.Write((long)e_phoff);
            w.Write((long)e_shoff);
            w.Write((uint)e_flags);
            w.Write((ushort)e_ehsize);
            w.Write((ushort)e_phentsize);
            w.Write((ushort)e_phnum);
            w.Write((ushort)e_shentsize);
            w.Write((ushort)e_shnum);
            w.Write((ushort)e_shstrndx);
        }

        private void WriteElf32Relocation(ElfRelocation r, System.IO.BinaryWriter w)
        {
            w.Write((uint)r.r_offset);
            uint r_info = ((uint)r.r_sym << 8) + ((uint)r.r_type & 0xffU);
            w.Write(r_info);
            if(use_rela)
                w.Write((uint)r.r_addend);
        }

        private void WriteElf64Relocation(ElfRelocation r, System.IO.BinaryWriter w)
        {
            w.Write(r.r_offset);
            ulong r_info = ((ulong)r.r_sym << 32) + ((ulong)r.r_type & 0xffffffffUL);
            w.Write(r_info);
            if(use_rela)
                w.Write(r.r_addend);
        }

        private void WriteElf32Symbol(ElfSymbol sym, System.IO.BinaryWriter w)
        {
            w.Write((uint)sym.st_name);
            w.Write((uint)sym.st_value);
            w.Write((uint)sym.st_size);
            uint st_info = (sym.st_bind << 4) + (sym.st_type & 0xf);
            w.Write((byte)st_info);
            w.Write((byte)0);
            w.Write((ushort)sym.st_shndx);
        }

        private void WriteElf64Symbol(ElfSymbol sym, System.IO.BinaryWriter w)
        {
            w.Write((uint)sym.st_name);
            uint st_info = (sym.st_bind << 4) + (sym.st_type & 0xf);
            w.Write((byte)st_info);
            w.Write((byte)0);
            w.Write((ushort)sym.st_shndx);
            w.Write((ulong)sym.st_value);
            w.Write((ulong)sym.st_size);
        }

        private void WriteElf32Segment(ElfProgramHeader ph, System.IO.BinaryWriter w)
        {
            w.Write((uint)ph.p_type);
            w.Write((uint)ph.p_offset);
            w.Write((uint)ph.p_vaddr);
            w.Write((uint)ph.p_paddr);
            w.Write((uint)ph.p_filesz);
            w.Write((uint)ph.p_memsz);
            w.Write((uint)ph.p_flags);
            w.Write((uint)ph.p_align);
        }

        private void WriteElf64Segment(ElfProgramHeader ph, System.IO.BinaryWriter w)
        {
            throw new NotImplementedException();
        }

        private void WriteElf32Section(SectionHeader sect, System.IO.BinaryWriter w)
        {
            w.Write((uint)sect.sh_name);
            w.Write((uint)sect.sh_type);
            w.Write((uint)sect.sh_flags);
            w.Write((uint)sect.sh_addr);
            w.Write((uint)sect.sh_offset);
            w.Write((uint)sect.sh_size);
            w.Write((uint)sect.sh_link);
            w.Write((uint)sect.sh_info);
            w.Write((uint)sect.sh_addralign);
            w.Write((uint)sect.sh_entsize);
        }

        private void WriteElf64Section(SectionHeader sect, System.IO.BinaryWriter w)
        {
            w.Write((uint)sect.sh_name);
            w.Write((uint)sect.sh_type);
            w.Write((ulong)sect.sh_flags);
            w.Write((ulong)sect.sh_addr);
            w.Write((long)sect.sh_offset);
            w.Write((long)sect.sh_size);
            w.Write((uint)sect.sh_link);
            w.Write((uint)sect.sh_info);
            w.Write((ulong)sect.sh_addralign);
            w.Write((ulong)sect.sh_entsize);
        }

        public override ISection GetGlobalSection()
        {
            return AbsSection;
        }

        public override ISection GetCommonSection()
        {
            return CommonSection;
        }

        public override ISection GetRDataSection()
        {
            var r = base.GetRDataSection();
            r.Name = ".rodata";
            return r;
        }
    }
}
