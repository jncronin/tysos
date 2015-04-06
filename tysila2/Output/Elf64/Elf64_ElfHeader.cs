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

namespace Elf64
{
    class MachineType
    {
        public const UInt16 EM_X86_64 = 62;
    }

    class Etype
    {
        public const ushort ET_NONE = 0;
        public const ushort ET_REL = 1;
        public const ushort ET_EXEC = 2;
        public const ushort ET_DYN = 3;
        public const ushort ET_CORE = 4;
    }

    class Eident
    {
        public const int EI_MAG0 = 0;
        public const int EI_MAG1 = 1;
        public const int EI_MAG2 = 2;
        public const int EI_MAG3 = 3;
        public const int EI_CLASS = 4;
        public const int EI_DATA = 5;
        public const int EI_VERSION = 6;
        public const int EI_OSABI = 7;
        public const int EI_ABIVERSION = 8;
        public const int EI_PAD = 9;

        public const byte ELFCLASS32 = 1;
        public const byte ELFCLASS64 = 2;

        public const byte ELFDATA2LSB = 1;
        public const byte ELFDATA2MSB = 2;

        public const byte EV_CURRENT = 1;
    }

    class Elf64_Ehdr
    {
        public byte[] e_ident = new byte[] { 0x7f, 0x45, 0x4c, 0x46, 2, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public UInt64 e_entry = 0;
        public UInt16 e_type = 0;
        public UInt16 e_machine = 0;
        public UInt32 e_version = 0;
        public UInt32 e_flags = 0;
        public List<Elf64_Phdr> phdrs = new List<Elf64_Phdr>();
        public List<Elf64_Shdr> shdrs = new List<Elf64_Shdr>();
        public Elf64_String_Shdr e_shstr;

        public Elf64_String_Shdr e_symstr;
        public UInt16 e_symstrndx;

        public Elf64_Symbol_Shdr e_syms;
        public UInt16 e_symsndx;

        public UInt64 e_phoff;
        public UInt64 e_shoff;
        public UInt16 e_ehsize { get { return (ushort)GetLength(); } }
        public UInt16 e_phentsize = (ushort)Elf64_Phdr.GetLength();
        public UInt16 e_phnum { get { return (ushort)phdrs.Count; } }
        public UInt16 e_shentsize { get { return (ushort)Elf64_Shdr.GetLength(); } }
        public UInt16 e_shnum { get { return (ushort)shdrs.Count; } }
        public UInt16 e_shstrndx;

        public Elf64_Shdr text = new Elf64_Shdr
        {
            Name = "text",
            sh_type = SectionType.SHT_PROGBITS,
            sh_flags = SectionFlags.SHF_ALLOC | SectionFlags.SHF_EXECINSTR,
            sh_addralign = 8
        };
        public Elf64_Shdr data = new Elf64_Shdr
        {
            Name = "data",
            sh_type = SectionType.SHT_PROGBITS,
            sh_flags = SectionFlags.SHF_ALLOC | SectionFlags.SHF_WRITE,
            sh_addralign = 8
        };
        public Elf64_Shdr rodata = new Elf64_Shdr
        {
            Name = "rodata",
            sh_type = SectionType.SHT_PROGBITS,
            sh_flags = SectionFlags.SHF_ALLOC,
            sh_addralign = 8
        };
        public Elf64_Shdr comments = new Elf64_Shdr
        {
            Name = "comment",
            sh_type = SectionType.SHT_PROGBITS,
            sh_flags = 0,
        };
        public Elf64_Shdr bss = new Elf64_Shdr
        {
            Name = "bss",
            sh_type = SectionType.SHT_NOBITS,
            sh_flags = SectionFlags.SHF_ALLOC | SectionFlags.SHF_WRITE,
            sh_addralign = 8
        };

        public Elf64_Rela_Shdr relatext, reladata, relarodata, relabss;

        public void Write(Stream s)
        {
            Elf64Writer.Write(s, e_ident);
            Elf64Writer.Write(s, e_type);
            Elf64Writer.Write(s, e_machine);
            Elf64Writer.Write(s, e_version);
            Elf64Writer.Write(s, e_entry);
            Elf64Writer.Write(s, e_phoff);
            Elf64Writer.Write(s, e_shoff);
            Elf64Writer.Write(s, e_flags);
            Elf64Writer.Write(s, e_ehsize);
            Elf64Writer.Write(s, e_phentsize);
            Elf64Writer.Write(s, e_phnum);
            Elf64Writer.Write(s, e_shentsize);
            Elf64Writer.Write(s, e_shnum);
            Elf64Writer.Write(s, e_shstrndx);
        }

        internal void GenerateRelocationSymbols()
        {
            //e_syms._sh_info = (uint)e_syms.defined_syms.Count;
            e_syms._sh_info = 5;
            GenerateRelocationSymbols(this.relatext);
            GenerateRelocationSymbols(this.reladata);
            GenerateRelocationSymbols(this.relarodata);
            GenerateRelocationSymbols(this.relabss);
            if (relatext.relocs.Count == 0)
                shdrs.Remove(relatext);
            if (reladata.relocs.Count == 0)
                shdrs.Remove(reladata);
            if (relarodata.relocs.Count == 0)
                shdrs.Remove(relarodata);
            if(relabss.relocs.Count == 0)
                shdrs.Remove(relabss);
        }

        private void GenerateRelocationSymbols(Elf64_Rela_Shdr elf64_Rela_Shdr)
        {
            foreach(Elf64_Rela_Shdr.Elf64_Rela rela in elf64_Rela_Shdr.relocs)
            {
                if (e_syms.name_to_sym.ContainsKey(rela.reloc_target))
                    rela.sym_ndx = (uint)e_syms.name_to_sym[rela.reloc_target];
                else
                {
                    // create a new external symbol
                    rela.sym_ndx = (uint)e_syms.defined_syms.Count;
                    e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
                    {
                        name = rela.reloc_target,
                        st_name = this.e_symstr.GetOffset(rela.reloc_target),
                        st_info = Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_GLOBAL |
                        Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_NOTYPE,
                        st_other = 0,
                        st_shndx = 0,
                        st_size = 0,
                        st_value = 0
                    });
                    e_syms.name_to_sym.Add(rela.reloc_target, e_syms.defined_syms.Count - 1);
                }
            }
        }

        public static int GetLength()
        { return 64; }

        public Elf64_Ehdr(string source_filename, string comment)
        {
            // Create a null section
            new Elf64_Null_Shdr().AddTo(shdrs);

            // Create the string section
            CreateStringAndSymbolSections();

            // Create the default sections
            text.SetName(".text", e_shstr);
            data.SetName(".data", e_shstr);
            rodata.SetName(".rodata", e_shstr);
            bss.SetName(".bss", e_shstr);
            comments.SetName(".comment", e_shstr);

            text.AddTo(shdrs);
            data.AddTo(shdrs);
            rodata.AddTo(shdrs);
            bss.AddTo(shdrs);
            comments.AddTo(shdrs);
            comments.data.AddRange(Encoding.UTF8.GetBytes(comment));

            relatext = new Elf64_Rela_Shdr(this, text.index);
            relatext.SetName(".rela.text", e_shstr);
            relatext.AddTo(shdrs);
            reladata = new Elf64_Rela_Shdr(this, data.index);
            reladata.SetName(".rela.data", e_shstr);
            reladata.AddTo(shdrs);
            relarodata = new Elf64_Rela_Shdr(this, rodata.index);
            relarodata.SetName(".rela.rodata", e_shstr);
            relarodata.AddTo(shdrs);
            relabss = new Elf64_Rela_Shdr(this, bss.index);
            relabss.SetName(".rela.bss", e_shstr);
            relabss.AddTo(shdrs);

            // Add in symbols for the file and each progbits section
            e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = source_filename,
                st_name = e_symstr.GetOffset(source_filename),
                st_value = 0,
                st_size = 0,
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_FILE |
                    Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_LOCAL,
                st_other = 0,
                st_shndx = 0xfff1
            });
            e_syms.name_to_sym.Add(source_filename, e_syms.defined_syms.Count - 1);
            e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = ".text",
                st_name = e_symstr.GetOffset(".text"),
                st_value = 0,
                st_size = 0,
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_SECTION |
                    Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_LOCAL,
                st_other = 0,
                st_shndx = text.index
            });
            e_syms.name_to_sym.Add(".text", e_syms.defined_syms.Count - 1);
            e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = ".data",
                st_name = e_symstr.GetOffset(".data"),
                st_value = 0,
                st_size = 0,
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_SECTION |
                    Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_LOCAL,
                st_other = 0,
                st_shndx = data.index
            });
            e_syms.name_to_sym.Add(".data", e_syms.defined_syms.Count - 1);
            e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = ".rodata",
                st_name = e_symstr.GetOffset(".rodata"),
                st_value = 0,
                st_size = 0,
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_SECTION |
                    Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_LOCAL,
                st_other = 0,
                st_shndx = rodata.index
            });
            e_syms.name_to_sym.Add(".rodata", e_syms.defined_syms.Count - 1);
            e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = ".bss",
                st_name = e_symstr.GetOffset(".bss"),
                st_value = 0,
                st_size = 0,
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_SECTION |
                    Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_LOCAL,
                st_other = 0,
                st_shndx = bss.index
            });
            e_syms.name_to_sym.Add(".bss", e_syms.defined_syms.Count - 1);

            // Fill in the fields in the header
            e_machine = MachineType.EM_X86_64;
            e_ident[Eident.EI_CLASS] = Eident.ELFCLASS64;
            e_ident[Eident.EI_DATA] = Eident.ELFDATA2LSB;

            e_version = (UInt32)Eident.EV_CURRENT;
            e_entry = 0;
            e_phoff = 0;
            e_flags = 0;
            e_type = Etype.ET_REL;
        }

        private bool CreateStringAndSymbolSections()
        {
            e_shstr = new Elf64_String_Shdr();
            e_shstr.SetName(".shstrtab", e_shstr);
            e_symstr = new Elf64_String_Shdr();
            e_symstr.SetName(".strtab", e_shstr);

            e_shstrndx = (ushort)shdrs.Count;
            e_shstr.AddTo(shdrs);
            e_symstrndx = (ushort)shdrs.Count;
            e_symstr.AddTo(shdrs);

            e_syms = new Elf64_Symbol_Shdr(this);
            e_syms.SetName(".symtab", e_shstr);
            e_symsndx = (ushort)shdrs.Count;
            e_syms.AddTo(shdrs);
            Elf64_Symbol_Shdr.Elf64_Sym null_sym = new Elf64_Symbol_Shdr.Elf64_Sym();
            null_sym.st_name = 0;
            null_sym.st_info = 0;
            null_sym.st_other = 0;
            null_sym.st_shndx = 0;
            null_sym.st_value = 0;
            null_sym.st_size = 0;
            e_syms.defined_syms.Add(null_sym);

            return true;
        }
    }
}
