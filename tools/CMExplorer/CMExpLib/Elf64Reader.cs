using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CMExpLib
{
    public class Elf64Reader : IDisposable
    {
        string fname;
        BinaryReader r;

        ElfHeader ehdr;

        public class ElfHeader
        {
            public BinaryReader r;
            internal string fname;

            public string FileName { get { return fname; } }

            public uint e_ident1;

            public byte ei_class;
            public byte ei_data;
            public byte ei_version;
            public byte ei_osabi;
            public byte ei_abiversion;

            public ushort e_type;
            public ushort e_machinetype;
            public uint e_version;
            public ulong e_entry;
            public ulong e_phoff;
            public ulong e_shoff;
            public uint e_flags;
            public ushort e_ehsize;
            public ushort e_phentsize;
            public ushort e_phnum;
            public ushort e_shentsize;
            public ushort e_shnum;
            public ushort e_shstrndx;

            public List<Section> all_sects = new List<Section>();
            public List<Section> load_sects = new List<Section>();
            public List<Section> sym_sects = new List<Section>();
            public List<Section> rel_sects = new List<Section>();

            public Dictionary<string, string> comments = new Dictionary<string, string>();

            public SymbolTable stab;

            public ulong ReadPointer()
            {
                return r.ReadUInt64();
            }

            public ulong ReadPointer(ulong offset)
            {
                long old_pos = r.BaseStream.Position;
                r.BaseStream.Seek((long)offset, SeekOrigin.Begin);
                ulong ret = r.ReadUInt64();
                r.BaseStream.Seek(old_pos, SeekOrigin.Begin);
                return ret;
            }

            public ulong VaddrToOffset(ulong vaddr)
            {
                foreach (Section s in load_sects)
                {
                    if ((vaddr >= s.sh_addr) && (vaddr < (s.sh_addr + s.sh_size)))
                        return vaddr - s.sh_addr + s.sh_offset;
                }
                throw new Exception("Virtual address: 0x" + vaddr.ToString("X16") + " is not in any loadable section");
            }

            public MetadataObject GetRoot()
            {
                MetadataObject ret = new MetadataObject();
                ret.Address = 0;
                ret.ehdr = this;
                
                List<object> asms = new List<object>();
                foreach (SymbolTable.Symbol s in stab.AssemblySymbols.Values)
                    asms.Add(MetadataObject.ReadReference(s.vaddr, this));

                ret.Fields["Assemblies"] = asms.ToArray();
                ret.file_offset = 0;
                ret.LayoutName = "File root";

                return ret;
            }

            public MetadataObject GetSymbols()
            {
                MetadataObject ret = new MetadataObject();
                ret.Address = 0;
                ret.ehdr = this;

                List<object> syms = new List<object>();
                foreach (SymbolTable.Symbol s in stab.Symbols.Values)
                {
                    // Only list those symbols we can actually display
                    try
                    {
                        switch (libtysila.Mangler2.DemangleName(s.Name, this.stab.ass).ObjectType)
                        {
                            case libtysila.Mangler2.ObjectToMangle.ObjectTypeType.Assembly:
                            case libtysila.Mangler2.ObjectToMangle.ObjectTypeType.FieldInfo:
                            case libtysila.Mangler2.ObjectToMangle.ObjectTypeType.MethodInfo:
                            case libtysila.Mangler2.ObjectToMangle.ObjectTypeType.Module:
                            case libtysila.Mangler2.ObjectToMangle.ObjectTypeType.TypeInfo:
                                syms.Add(new MetadataObject.Reference { Address = s.vaddr, ehdr = this, file_offset = s.offset, Name = s.Name, Type = "__sym" });
                                break;
                        }
                    }
                    catch (Exception)
                    { }
                }
                ret.Fields["Symbols"] = syms.ToArray();
                ret.file_offset = 0;
                ret.LayoutName = "File symbols";

                return ret;
            }
        }

        public class Section
        {
            public ElfHeader ehdr;

            public string Name
            {
                get
                {
                    if (ehdr == null)
                        return "unknown";

                    return ReadString(ehdr, (uint)ehdr.e_shstrndx, (uint)sh_name);
                }
            }

            public uint sh_name;
            public uint sh_type;
            public ulong sh_flags;
            public ulong sh_addr;
            public ulong sh_offset;
            public ulong sh_size;
            public uint sh_link;
            public uint sh_info;
            public ulong sh_addralign;
            public ulong sh_entsize;
        }

        static string ReadString(ElfHeader ehdr, uint strtabndx, uint strndx)
        {
            long old_pos = ehdr.r.BaseStream.Position;

            Section str_sect = ehdr.all_sects[(int)strtabndx];
            ehdr.r.BaseStream.Seek((long)str_sect.sh_offset + (long)strndx, SeekOrigin.Begin);

            byte cur_byte;
            List<char> str = new List<char>();
            do
            {
                cur_byte = ehdr.r.ReadByte();

                if (cur_byte != 0)
                    str.Add((char)cur_byte);
            } while (cur_byte != 0);
            string ret = new string(str.ToArray());

            ehdr.r.BaseStream.Seek(old_pos, SeekOrigin.Begin);
            return ret;
        }

        public ElfHeader Read(string filename, libtysila.Assembler.FileLoader floader)
        {
            fname = filename;
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            r = new BinaryReader(fs);

            ehdr = new ElfHeader();
            ehdr.r = r;
            ehdr.fname = filename;

            // Check its an ELF file
            ehdr.e_ident1 = r.ReadUInt32();
            if (ehdr.e_ident1 != 0x464c457f)
                throw new Exception("Not an ELF file");

            ehdr.ei_class = r.ReadByte();
            if (ehdr.ei_class != 0x2)
                throw new Exception("Not an ELF64 file");
            ehdr.ei_data = r.ReadByte();
            if (ehdr.ei_data != 0x1)
                throw new Exception("Not an ELF64 LSB file");
            ehdr.ei_version = r.ReadByte();
            ehdr.ei_osabi = r.ReadByte();
            ehdr.ei_abiversion = r.ReadByte();

            r.BaseStream.Seek(16, SeekOrigin.Begin);
            ehdr.e_type = r.ReadUInt16();
            ehdr.e_machinetype = r.ReadUInt16();
            ehdr.e_version = r.ReadUInt32();
            ehdr.e_entry = r.ReadUInt64();
            ehdr.e_phoff = r.ReadUInt64();
            ehdr.e_shoff = r.ReadUInt64();
            ehdr.e_flags = r.ReadUInt32();
            ehdr.e_ehsize = r.ReadUInt16();
            ehdr.e_phentsize = r.ReadUInt16();
            ehdr.e_phnum = r.ReadUInt16();
            ehdr.e_shentsize = r.ReadUInt16();
            ehdr.e_shnum = r.ReadUInt16();
            ehdr.e_shstrndx = r.ReadUInt16();

            bool is_exec = (ehdr.e_type == 0x2);
            if(!is_exec)
                throw new Exception("Not an ELF64 executable");

            // Now load the sections
            r.BaseStream.Seek((long)ehdr.e_shoff, SeekOrigin.Begin);
            for (int i = 0; i < (int)ehdr.e_shnum; i++)
            {
                r.BaseStream.Seek((long)ehdr.e_shoff + (long)i * (long)ehdr.e_shentsize, SeekOrigin.Begin);

                Section s = new Section();
                s.ehdr = ehdr;
                s.sh_name = r.ReadUInt32();
                s.sh_type = r.ReadUInt32();
                s.sh_flags = r.ReadUInt64();
                s.sh_addr = r.ReadUInt64();
                s.sh_offset = r.ReadUInt64();
                s.sh_size = r.ReadUInt64();
                s.sh_link = r.ReadUInt32();
                s.sh_info = r.ReadUInt32();
                s.sh_addralign = r.ReadUInt64();
                s.sh_entsize = r.ReadUInt64();
                ehdr.all_sects.Add(s);

                // Decide on the type of the section
                if ((s.sh_type == 1) && ((s.sh_flags & 2) != 0))
                {
                    // Loadable program section
                    ehdr.load_sects.Add(s);
                }
                else if (s.sh_type == 2)
                {
                    // Symbol table
                    ehdr.sym_sects.Add(s);
                }
                else if ((s.sh_type == 4) || (s.sh_type == 9))
                {
                    // Relocation table
                    ehdr.rel_sects.Add(s);
                }
            }

            // Find the comment section
            foreach (Section s in ehdr.all_sects)
            {
                if (s.Name == ".comment")
                {
                    ehdr.r.BaseStream.Seek((long)s.sh_offset, SeekOrigin.Begin);
                    string comment = Encoding.UTF8.GetString(ehdr.r.ReadBytes((int)s.sh_size));
                    string[] comments = comment.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    bool found_tysila = false;
                    foreach (string c in comments)
                    {
                        if (c.StartsWith("tysila"))
                            found_tysila = true;
                        else if (c.StartsWith("endtysila"))
                            found_tysila = false;
                        else if (found_tysila)
                        {
                            try
                            {
                                string[] val_args = c.Split(new string[] { ": " }, StringSplitOptions.None);
                                ehdr.comments.Add(val_args[0], val_args[1]);
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }

            SymbolTable stab = new SymbolTable();

            // Now load the symbols
            foreach (Section sym_sect in ehdr.sym_sects)
            {
                for (ulong sym_off = 0; sym_off < sym_sect.sh_size; sym_off += sym_sect.sh_entsize)
                {
                    ehdr.r.BaseStream.Seek((long)sym_sect.sh_offset + (long)sym_off, SeekOrigin.Begin);

                    uint st_name = ehdr.r.ReadUInt32();
                    byte st_info = ehdr.r.ReadByte();
                    byte st_other = ehdr.r.ReadByte();
                    ushort st_shndx = ehdr.r.ReadUInt16();
                    ulong st_value = ehdr.r.ReadUInt64();
                    ulong st_size = ehdr.r.ReadUInt64();

                    byte st_type = (byte)(st_info & 0xf);
                    byte st_bind = (byte)((st_info >> 4) & 0xf);

                    if ((st_type == 1) || (st_type == 2))
                    {
                        // STT_OBJECT or STT_FUNC
                        string name = ReadString(ehdr, sym_sect.sh_link, st_name);

                        if (is_exec)
                        {
                            // For executables st_value is the load address of the symbol - we therfore need to find
                            //  its offset from its st_shndx link
                            ulong vaddr = st_value;
                            ulong size = st_size;

                            Section sect = ehdr.all_sects[(int)st_shndx];
                            ulong offset = st_value - sect.sh_addr + sect.sh_offset;
                            stab.Add(name, vaddr, offset, size, ehdr);
                        }
                        else
                            throw new NotImplementedException();
                    }
                }
            }

            // Try and create an assembler
            if (!ehdr.comments.ContainsKey("arch"))
                throw new Exception("Architecture not specified in ELF file");
            try
            {
                stab.ass = libtysila.Assembler.CreateAssembler(libtysila.Assembler.ParseArchitectureString(ehdr.comments["arch"]), floader, null, null);

            }
            catch (Exception e)
            {
                throw new Exception("Unable to create assembler: " + ehdr.comments["arch"] + Environment.NewLine + "Error: " + e.ToString(), e);
            }
            stab.lm = new LayoutManager(stab.ass, stab);
            ehdr.stab = stab;

            return ehdr;
        }

        public void Dispose()
        {
            if(r != null)
                r.Close();
        }
    }
}
