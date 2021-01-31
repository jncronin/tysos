/* Copyright (C) 2015 by John Cronin
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

namespace tysos.elf
{
    class ElfFileReader
    {
        static ulong ReadUInt64(lib.File s)
        {
            byte[] val = new byte[8];
            s.Read(val, 0, 8);
            return BitConverter.ToUInt64(val, 0);
        }

        static long ReadInt64(lib.File s)
        {
            byte[] val = new byte[8];
            s.Read(val, 0, 8);
            return BitConverter.ToInt64(val, 0);
        }

        static uint ReadUInt32(lib.File s)
        {
            byte[] val = new byte[4];
            s.Read(val, 0, 4);
            return BitConverter.ToUInt32(val, 0);
        }

        static ElfReader.Elf64_Ehdr ReadHeader(lib.File s)
        {
            ElfReader.Elf64_Ehdr ret = new ElfReader.Elf64_Ehdr();
            ret.e_ident_1 = ReadUInt64(s);
            ret.e_ident_2 = ReadUInt64(s);
            ret.e_type_machine = ReadUInt32(s);
            ret.e_machine = ReadUInt32(s);
            ret.e_entry = ReadUInt64(s);
            ret.e_phoff = ReadUInt64(s);
            ret.e_shoff = ReadUInt64(s);
            ret.e_flags = ReadUInt32(s);
            ret.e_ehsize_phentsize = ReadUInt32(s);
            ret.e_phnum_shentsize = ReadUInt32(s);
            ret.e_shnum_shstrndx = ReadUInt32(s);

            /* magic should equal 0x7f, 'E', 'L', 'F'
                * which is equal to 0x464c457f */
            uint magic = (uint)(ret.e_ident_1 & 0xffffffff);
            if (magic != 0x464c457f)
                throw new Exception("Not a valid Elf magic number: " + magic.ToString());

            /* EI_CLASS should equal ELFCLASS64 (= 2) */
            ulong ei_class = (ret.e_ident_1 >> 32) & 0xff;
            if (ei_class != 2)
                throw new Exception("Invalid EI_CLASS: " + ei_class.ToString());

            return ret;
        }

        static ElfReader.Elf64_Shdr ReadShdr(lib.File s)
        {
            ElfReader.Elf64_Shdr ret = new ElfReader.Elf64_Shdr();
            ret.sh_name = ReadUInt32(s);
            ret.sh_type = ReadUInt32(s);
            ret.sh_flags = ReadUInt64(s);
            ret.sh_addr = ReadUInt64(s);
            ret.sh_offset = ReadUInt64(s);
            ret.sh_size = ReadUInt64(s);
            ret.sh_link = ReadUInt32(s);
            ret.sh_info = ReadUInt32(s);
            ret.sh_addralign = ReadUInt64(s);
            ret.sh_entsize = ReadUInt64(s);

            return ret;
        }

        static ElfReader.Elf64_Sym ReadSym(lib.File s)
        {
            ElfReader.Elf64_Sym ret = new ElfReader.Elf64_Sym();
            ret.st_name = ReadUInt32(s);
            ret.st_info_other_shndx = ReadUInt32(s);
            ret.st_value = ReadUInt64(s);
            ret.st_size = ReadUInt64(s);

            return ret;
        }

        static ElfReader.Elf64_Rela ReadRela(lib.File s)
        {
            ElfReader.Elf64_Rela ret = new ElfReader.Elf64_Rela();
            ret.r_offset = ReadUInt64(s);
            ret.r_info = ReadUInt64(s);
            ret.r_addend = ReadInt64(s);

            return ret;
        }

        static unsafe byte* ReadStructure(lib.File s, long pos, long len)
        {
            byte[] ret = new byte[len];
            s.Seek(pos, System.IO.SeekOrigin.Begin);
            int bytes_read = s.Read(ret, 0, (int)len);
            if (bytes_read != len)
                throw new Exception("ReadStructure: read " + bytes_read.ToString() +
                    "bytes, expected " + len.ToString());
            return (byte*)libsupcs.MemoryOperations.GetInternalArray(ret);
        }

        static unsafe byte* ReadStructure(lib.File s, ulong pos, ulong len)
        { return ReadStructure(s, (long)pos, (long)len); }

        public static unsafe ulong LoadObject(Virtual_Regions vreg, VirtMem vmem, SymbolTable stab, lib.File s, string name, out ulong tls_size)
        {
            ElfReader.Elf64_Ehdr ehdr = ReadHeader(s);

            /* Load up section headers */
            ulong e_shentsize = ehdr.e_shentsize;
            byte* shdrs = ReadStructure(s, ehdr.e_shoff, ehdr.e_shnum * e_shentsize);

            /* Load up section string table */
            ElfReader.Elf64_Shdr *shdr_shstr = (ElfReader.Elf64_Shdr *)(shdrs + ehdr.e_shstrndx * e_shentsize);
            byte* sect_shstr = ReadStructure(s, shdr_shstr->sh_offset, shdr_shstr->sh_size);

            /* Iterate through the sections marked SHF_ALLOC, allocating space as we go */
            uint e_shnum = (uint)ehdr.e_shnum;
            byte* sect_header = shdrs;

            /* .to files are relocatable but have the entry point set anyway */
            ulong text_section = 0;
            ulong hdr_epoint = ehdr.e_entry;

            ulong start = 0;
            tls_size = Program.arch.tysos_tls_length;

            for (uint i = 0; i < e_shnum; i++)
            {
                ElfReader.Elf64_Shdr* cur_shdr = (ElfReader.Elf64_Shdr*)sect_header;

                if ((cur_shdr->sh_flags & 0x2) == 0x2)
                {
                    /* SHF_ALLOC */
                    if((cur_shdr->sh_flags & (1 << 10)) != 0)
                    {
                        // TLS
                        tls_size += cur_shdr->sh_size;
                        continue;
                    }
                    // get its name
                    byte* name_addr = sect_shstr + cur_shdr->sh_name;
                    string sect_name = new string((sbyte*)name_addr);

                    /* Register with gc if writeable and not executable */
                    bool gc_data = false;
                    if (((cur_shdr->sh_flags & 0x1) != 0) && ((cur_shdr->sh_flags & 0x4) == 0))
                        gc_data = true;

                    // allocate space for it
                    ulong sect_addr = vreg.AllocRegion(cur_shdr->sh_size, 0x1000, 
                        name + sect_name, 0, 
                        Virtual_Regions.Region.RegionType.ModuleSection, 
                        gc_data).start;
                    cur_shdr->sh_addr = sect_addr;

                    if (sect_addr + cur_shdr->sh_size > 0x7effffffff)
                        throw new Exception("Object section allocated beyond limit of small code model");

                    // is this .text?
                    if (sect_name == ".text")
                        text_section = sect_addr;

                    // copy the section to its destination
                    if (cur_shdr->sh_type == 0x1)
                    {
                        /* SHT_PROGBITS */

                        // Convert the VirtualRegion to a managed byte array
                        //byte[] sect_data = libsupcs.TysosArrayType.CreateByteArray((byte*)sect_addr, (int)cur_shdr->sh_size);
                        byte[] sect_data = libsupcs.Array.CreateSZArray<byte>((int)cur_shdr->sh_size, (void*)sect_addr);

                        // Read the section data into it
                        s.Seek((long)cur_shdr->sh_offset, System.IO.SeekOrigin.Begin);
                        s.Read(sect_data, 0, (int)cur_shdr->sh_size);
                    }
                    else if (cur_shdr->sh_type == 0x8)
                    {
                        /* SHT_NOBITS */
                        libsupcs.MemoryOperations.MemSet((void*)sect_addr, 0, (int)cur_shdr->sh_size);
                    }
                }

                sect_header += e_shentsize;
            }

            /* Iterate through defined symbols, loading them into the symbol table */
            sect_header = shdrs;
            for (uint i = 0; i < e_shnum; i++)
            {
                ElfReader.Elf64_Shdr* cur_shdr = (ElfReader.Elf64_Shdr*)sect_header;

                if (cur_shdr->sh_type == 0x2)
                {
                    /* SHT_SYMTAB */

                    // Load up the section data
                    byte* shdr_data = ReadStructure(s, cur_shdr->sh_offset, cur_shdr->sh_size);
                    ulong offset = cur_shdr->sh_info * cur_shdr->sh_entsize;
                    cur_shdr->sh_addr = (ulong)shdr_data;

                    while (offset < cur_shdr->sh_size)
                    {
                        ElfReader.Elf64_Sym* cur_sym = (ElfReader.Elf64_Sym*)(shdr_data + offset);

                        bool is_vis = false;
                        bool is_weak = false;

                        uint st_bind = (cur_sym->st_info_other_shndx >> 4) & 0xf;

                        if (st_bind == 1)
                        {
                            // STB_GLOBAL
                            is_vis = true;
                        }
                        else if (st_bind == 2)
                        {
                            // STB_WEAK
                            is_vis = true;
                            is_weak = true;
                        }

                        if (is_vis)
                        {
                            /* Get the symbol's name */

                            // If the appropriate string table is not loaded, then load it
                            ElfReader.Elf64_Shdr* strtab = (ElfReader.Elf64_Shdr*)(shdrs + cur_shdr->sh_link * e_shentsize);
                            if(strtab->sh_addr == 0)
                                strtab->sh_addr = (ulong)ReadStructure(s, strtab->sh_offset, strtab->sh_size);

                            // Get the name
                            string sym_name = new string((sbyte*)(strtab->sh_addr + cur_sym->st_name));

                            uint st_shndx = (cur_sym->st_info_other_shndx >> 16) & 0xffff;

                            if (st_shndx != 0)
                            {
                                if (is_weak == false || stab.GetAddress(sym_name) == 0)
                                {
                                    ulong sym_addr = ((ElfReader.Elf64_Shdr*)(shdrs + st_shndx * e_shentsize))->sh_addr +
                                        cur_sym->st_value;

                                    if (sym_name == "_start")
                                        start = sym_addr;
                                    else
                                        stab.Add(sym_name, sym_addr, cur_sym->st_size);
                                }
                            }
                        }


                        offset += cur_shdr->sh_entsize;
                    }
                }

                sect_header += e_shentsize;
            }

            /* Iterate through relocations, fixing them up as we go */
            sect_header = shdrs;
            for (uint i = 0; i < e_shnum; i++)
            {
                ElfReader.Elf64_Shdr* cur_shdr = (ElfReader.Elf64_Shdr*)sect_header;

                if (cur_shdr->sh_type == 0x9)
                    throw new NotImplementedException("rel sections not supported");
                if (cur_shdr->sh_type == 0x4)
                {
                    /* SHT_RELA */

                    ElfReader.Elf64_Shdr* cur_symtab = (ElfReader.Elf64_Shdr*)(shdrs + cur_shdr->sh_link * e_shentsize);
                    ElfReader.Elf64_Shdr* rela_sect = (ElfReader.Elf64_Shdr*)(shdrs + cur_shdr->sh_info * e_shentsize);

                    /* Load section */
                    byte* shdr_data = ReadStructure(s, cur_shdr->sh_offset, cur_shdr->sh_size);
                    cur_shdr->sh_addr = (ulong)shdr_data;

                    ulong offset = 0;
                    while (offset < cur_shdr->sh_size)
                    {
                        ElfReader.Elf64_Rela* cur_rela = (ElfReader.Elf64_Rela*)(shdr_data + offset);

                        ulong r_offset = rela_sect->sh_addr + cur_rela->r_offset;
                        ulong r_sym = cur_rela->r_info >> 32;
                        ulong r_type = cur_rela->r_info & 0xffffffff;
                        long r_addend = cur_rela->r_addend;

                        ElfReader.Elf64_Sym* rela_sym = (ElfReader.Elf64_Sym*)(cur_symtab->sh_addr + r_sym * cur_symtab->sh_entsize);
                        uint st_bind = (rela_sym->st_info_other_shndx >> 4) & 0xf;

                        ulong S = 0;
                        if (st_bind == 0)
                        {
                            /* STB_LOCAL symbols have not been loaded into the symbol table
                             * We need to use the value stored in the symbol table */
                            uint st_shndx = (rela_sym->st_info_other_shndx >> 16) & 0xffff;
                            S = ((ElfReader.Elf64_Shdr*)(shdrs + e_shentsize * st_shndx))->sh_addr +
                                rela_sym->st_value;
                        }
                        else
                        {
                            /* Get the symbol address from the symbol table */
                            ElfReader.Elf64_Shdr* link_sect = (ElfReader.Elf64_Shdr*)(shdrs + cur_symtab->sh_link * e_shentsize);
                            string sym_name = new string((sbyte*)(link_sect->sh_addr + rela_sym->st_name));

                            S = stab.GetAddress(sym_name);
                            if (S == 0)
                                throw new Exception("undefined reference to " + sym_name);
                        }

                        /* Perform the relocation */
                        switch (r_type)
                        {
                            case 1:
                                // R_X86_64_64: S + A
                                {
                                    ulong c = S;
                                    if (r_addend > 0)
                                        c += (ulong)r_addend;
                                    else
                                        c -= (ulong)(-r_addend);
                                    *(ulong*)r_offset = c;
                                }
                                break;

                            case 2:
                                // R_X86_64_PC32: S + A - P
                                {
                                    if (S > (ulong)long.MaxValue)
                                        throw new Exception("S too large");
                                    if (r_offset > (ulong)long.MaxValue)
                                        throw new Exception("P too large");
                                    long c = (long)S + r_addend - (long)r_offset;

                                    if (c < int.MinValue || c > int.MaxValue)
                                        throw new Exception("Relocation truncated to fit");
                                    *(int*)r_offset = (int)c;
                                }
                                break;

                            case 10:
                                // R_X86_64_32: S + A
                                {
                                    if (S > (ulong)long.MaxValue)
                                        throw new Exception("S too large");
                                    long c = (long)S + r_addend;

                                    if (c < 0 || c > uint.MaxValue)
                                        throw new Exception("Relocation truncated to fit");
                                    *(uint*)r_offset = (uint)c;
                                }
                                break;

                            case 11:
                                // R_X86_64_32S: S + A
                                {
                                    if (S > (ulong)long.MaxValue)
                                        throw new Exception("S too large");
                                    long c = (long)S + r_addend;

                                    if (c < int.MinValue || c > int.MaxValue)
                                        throw new Exception("Relocation truncated to fit");
                                    *(int*)r_offset = (int)c;
                                }
                                break;

                            default:
                                throw new Exception("Unsupported relocation type: " +
                                    r_type.ToString());
                        }

                        offset += cur_shdr->sh_entsize;
                    }
                }

                sect_header += e_shentsize;
            }

            if(start == 0)
            {
                // assume .to file - use entry point in header
                start = text_section + hdr_epoint;
            }
            return start;
        }

    }
}
