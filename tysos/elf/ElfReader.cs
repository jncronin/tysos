/* Copyright (C) 2011-2015 by John Cronin
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

namespace tysos
{
    class ElfReader
    {
        internal struct Elf64_Ehdr
        {
            public UInt64 e_ident_1;
            public UInt64 e_ident_2;
            public UInt32 e_type_machine;
            public UInt32 e_machine;
            public UInt64 e_entry;
            public UInt64 e_phoff;
            public UInt64 e_shoff;
            public UInt32 e_flags;
            public UInt32 e_ehsize_phentsize;
            public UInt32 e_phnum_shentsize;
            public UInt32 e_shnum_shstrndx;

            public ulong e_ehsize { get { return (ulong)(e_ehsize_phentsize & 0xffff); } }
            public ulong e_phentsize { get { return (ulong)((e_ehsize_phentsize >> 16) & 0xffff); } }
            public ulong e_phnum { get { return (ulong)(e_phnum_shentsize & 0xffff); } }
            public ulong e_shentsize { get { return (ulong)((e_phnum_shentsize >> 16) & 0xffff); } }
            public ulong e_shnum { get { return (ulong)(e_shnum_shstrndx & 0xffff); } }
            public ulong e_shstrndx { get { return (ulong)((e_shnum_shstrndx >> 16) & 0xffff); } }
        }

        internal struct Elf64_Shdr
        {
            public UInt32 sh_name;
            public UInt32 sh_type;
            public UInt64 sh_flags;
            public UInt64 sh_addr;
            public UInt64 sh_offset;
            public UInt64 sh_size;
            public UInt32 sh_link;
            public UInt32 sh_info;
            public UInt64 sh_addralign;
            public UInt64 sh_entsize;
        }

        internal struct Elf64_Sym
        {
            public UInt32 st_name;
            public UInt32 st_info_other_shndx;
            public UInt64 st_value;
            public UInt64 st_size;
        }

        internal struct Elf64_Phdr
        {
            public UInt32 p_type;
            public UInt32 p_flags;
            public UInt64 p_offset;
            public UInt64 p_vaddr;
            public UInt64 p_paddr;
            public UInt64 p_filesz;
            public UInt64 p_memsz;
            public UInt64 p_align;
        }

        internal struct Elf64_Dyn
        {
            public Int64 d_tag;
            public UInt64 d_val;

            public const Int64 DT_NULL = 0;
            public const Int64 DT_PLTRELSZ = 2;
            public const Int64 DT_HASH = 4;
            public const Int64 DT_STRTAB = 5;
            public const Int64 DT_SYMTAB = 6;
            public const Int64 DT_RELA = 7;
            public const Int64 DT_RELASZ = 8;
            public const Int64 DT_RELAENT = 9;
            public const Int64 DT_SYMENT = 11;
            public const Int64 DT_PLTREL = 20;
            public const Int64 DT_JMPREL = 23;
        }

        internal struct Elf64_Rela
        {
            public UInt64 r_offset;
            public UInt64 r_info;
            public Int64 r_addend;

            public const UInt32 R_X86_64_64 = 1;
            public const UInt32 R_X86_64_GLOB_DAT = 6;
            public const UInt32 R_X86_64_JUMP_SLOT = 7;
        }

        internal class Elf64_DynamicEntries
        {
            internal ulong dyn_sym_vaddr = 0;
            internal ulong dyn_str_vaddr = 0;
            internal ulong rela_tab_vaddr = 0;
            internal ulong rela_length = 0;
            internal ulong rela_entsize = 0;
            internal ulong sym_entsize = 0;
            internal ulong pltrela_tab_vaddr = 0;
            internal ulong pltrela_length = 0;
            internal ulong hash_vaddr = 0;
        }

        /* Get a program header of a particular type */
        private static unsafe Elf64_Phdr* GetPhdrOfType(Elf64_Ehdr* ehdr, uint type)
        {
            ulong prog_header = (ulong)ehdr + ehdr->e_phoff;

            for (uint i = 0; i < ehdr->e_phnum; i++)
            {
                Elf64_Phdr* cur_phdr = (Elf64_Phdr*)prog_header;

                if (cur_phdr->p_type == type)
                    return cur_phdr;

                prog_header += ehdr->e_phentsize;
            }

            return null;
        }

        /* Get a section header of a particular type */
        private static unsafe Elf64_Shdr* GetShdrOfType(Elf64_Ehdr* ehdr, uint type)
        {
            ulong sect_header = (ulong)ehdr + ehdr->e_shoff;

            for (uint i = 0; i < ehdr->e_shnum; i++)
            {
                Elf64_Shdr* shdr = (Elf64_Shdr*)sect_header;

                if (shdr->sh_type == type)
                    return shdr;

                sect_header += ehdr->e_shentsize;
            }

            throw new Exception("Shdr type " + type.ToString() + " not found.");
        }

        /* Load up the dynamic section */
        private static unsafe Elf64_DynamicEntries GetDynEntries(Elf64_Ehdr* ehdr, ulong load_address)
        {
            Elf64_Phdr* dynamic_phdr = GetPhdrOfType(ehdr, 0x2);
            if (dynamic_phdr == null)
                return null;

            Elf64_DynamicEntries ret = new Elf64_DynamicEntries();
            ulong binary = (ulong)ehdr;

            ulong dtab_addr = binary + dynamic_phdr->p_offset;
            ulong dtab_entsize = 16;
            ulong dtab_end = dtab_addr + dynamic_phdr->p_filesz;

            while (dtab_addr < dtab_end)
            {
                Elf64_Dyn* dyntab_entry = (Elf64_Dyn*)dtab_addr;

                if (dyntab_entry->d_tag == Elf64_Dyn.DT_STRTAB)
                    ret.dyn_str_vaddr = load_address + dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_SYMTAB)
                    ret.dyn_sym_vaddr = load_address + dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_RELA)
                    ret.rela_tab_vaddr = load_address + dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_RELAENT)
                    ret.rela_entsize = dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_RELASZ)
                    ret.rela_length = dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_SYMENT)
                    ret.sym_entsize = dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_PLTREL)
                {
                    if (dyntab_entry->d_val != Elf64_Dyn.DT_RELA)
                        throw new Exception("PLT relocation section does not use rela relocations - currently not supported");
                }
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_JMPREL)
                    ret.pltrela_tab_vaddr = load_address + dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_PLTRELSZ)
                    ret.pltrela_length = dyntab_entry->d_val;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_NULL)
                    dtab_addr = dtab_end;
                else if (dyntab_entry->d_tag == Elf64_Dyn.DT_HASH)
                    ret.hash_vaddr = load_address + dyntab_entry->d_val;

                dtab_addr += dtab_entsize;
            }

            return ret;
        }

        private unsafe static Elf64_Ehdr* VerifyElf(ulong binary)
        {
            Elf64_Ehdr* ehdr = (Elf64_Ehdr*)binary;

            /* magic should equal 0x7f, 'E', 'L', 'F'
             * which is equal to 0x464c457f */
            uint magic = (uint)(ehdr->e_ident_1 & 0xffffffff);
            if (magic != 0x464c457f)
                throw new Exception("Not a valid Elf magic number: " + magic.ToString());

            /* EI_CLASS should equal ELFCLASS64 (= 2) */
            ulong ei_class = (ehdr->e_ident_1 >> 32) & 0xff;
            if (ei_class != 2)
                throw new Exception("Invalid EI_CLASS: " + ei_class.ToString());

            return ehdr;
        }

        public static ulong GetEntryPoint(ulong binary, ulong load_address)
        {
            unsafe
            {
                Elf64_Ehdr* ehdr = VerifyElf(binary);
                return ehdr->e_entry + load_address;
            }
        }

        public static unsafe ulong LoadObject(Virtual_Regions vreg, VirtMem vmem, SymbolTable stab, ulong binary, ulong binary_paddr, string name)
        {
            Elf64_Ehdr* ehdr = VerifyElf(binary);

            /* Iterate through the sections marked SHF_ALLOC, allocating space as we go */
            ulong e_shentsize = ehdr->e_shentsize;
            uint e_shnum = (uint)ehdr->e_shnum;
            ulong sect_header = binary + ehdr->e_shoff;

            ulong start = 0;
           
            for(uint i = 0; i < e_shnum; i++)
            {
                Elf64_Shdr* cur_shdr = (Elf64_Shdr*)sect_header;

                if((cur_shdr->sh_flags & 0x2) == 0x2)
                {
                    /* SHF_ALLOC */

                    // get its name
                    ulong name_addr = binary +
                        ((Elf64_Shdr*)(binary + ehdr->e_shoff + e_shentsize * ehdr->e_shstrndx))->sh_offset +
                        cur_shdr->sh_name;
                    string sect_name = new string((sbyte*)name_addr);

                    // allocate space for it
                    ulong sect_addr = vreg.AllocRegion(cur_shdr->sh_size, 0x1000, name + sect_name, 0, Virtual_Regions.Region.RegionType.ModuleSection).start;
                    cur_shdr->sh_addr = sect_addr;

                    if (sect_addr + cur_shdr->sh_size > 0x7effffffff)
                        throw new Exception("Object section allocated beyond limit of small code model");

                    // copy the section to its destination
                    if (cur_shdr->sh_type == 0x1)
                    {
                        /* SHT_PROGBITS */
                        libsupcs.MemoryOperations.MemCpy((void*)sect_addr, (void*)(binary + cur_shdr->sh_offset),
                            (int)cur_shdr->sh_size);
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
            sect_header = binary + ehdr->e_shoff;
            for (uint i = 0; i < e_shnum; i++)
            {
                Elf64_Shdr* cur_shdr = (Elf64_Shdr*)sect_header;

                if(cur_shdr->sh_type == 0x2)
                {
                    /* SHT_SYMTAB */
                    ulong offset = cur_shdr->sh_info * cur_shdr->sh_entsize;
                    while(offset < cur_shdr->sh_size)
                    {
                        Elf64_Sym* cur_sym = (Elf64_Sym*)(binary + cur_shdr->sh_offset + offset);

                        bool is_vis = false;
                        bool is_weak = false;

                        uint st_bind = (cur_sym->st_info_other_shndx >> 4) & 0xf;

                        if(st_bind == 1)
                        {
                            // STB_GLOBAL
                            is_vis = true;
                        }
                        else if(st_bind == 2)
                        {
                            // STB_WEAK
                            is_vis = true;
                            is_weak = true;
                        }

                        if (is_vis)
                        {
                            ulong sym_name_addr = binary +
                                ((Elf64_Shdr*)(binary + ehdr->e_shoff + e_shentsize * cur_shdr->sh_link))->sh_offset +
                                cur_sym->st_name;
                            string sym_name = new string((sbyte*)sym_name_addr);

                            uint st_shndx = (cur_sym->st_info_other_shndx >> 16) & 0xffff;
                            
                            if(st_shndx != 0)
                            {
                                if(is_weak == false || stab.GetAddress(sym_name) == 0)
                                {
                                    ulong sym_addr = ((Elf64_Shdr*)(binary + ehdr->e_shoff + e_shentsize * st_shndx))->sh_addr +
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
            sect_header = binary + ehdr->e_shoff;
            for (uint i = 0; i < e_shnum; i++)
            {
                Elf64_Shdr* cur_shdr = (Elf64_Shdr*)sect_header;

                if (cur_shdr->sh_type == 0x9)
                    throw new NotImplementedException("rel sections not supported");
                if(cur_shdr->sh_type == 0x4)
                {
                    /* SHT_RELA */

                    Elf64_Shdr* cur_symtab = (Elf64_Shdr*)(binary + ehdr->e_shoff + cur_shdr->sh_link * ehdr->e_shentsize);
                    Elf64_Shdr* rela_sect = (Elf64_Shdr*)(binary + ehdr->e_shoff + cur_shdr->sh_info * ehdr->e_shentsize);

                    ulong offset = 0;
                    while(offset < cur_shdr->sh_size)
                    {
                        Elf64_Rela* cur_rela = (Elf64_Rela*)(binary + cur_shdr->sh_offset + offset);

                        ulong r_offset = rela_sect->sh_addr + cur_rela->r_offset;
                        ulong r_sym = cur_rela->r_info >> 32;
                        ulong r_type = cur_rela->r_info & 0xffffffff;
                        long r_addend = cur_rela->r_addend;

                        Elf64_Sym* rela_sym = (Elf64_Sym*)(binary + cur_symtab->sh_offset + r_sym * cur_symtab->sh_entsize);
                        uint st_bind = (rela_sym->st_info_other_shndx >> 4) & 0xf;


                        ulong S = 0;
                        if(st_bind == 0)
                        {
                            /* STB_LOCAL symbols have not been loaded into the symbol table
                             * We need to use the value stored in the symbol table */
                            uint st_shndx = (rela_sym->st_info_other_shndx >> 16) & 0xffff;
                            S = ((Elf64_Shdr*)(binary + ehdr->e_shoff + e_shentsize * st_shndx))->sh_addr +
                                rela_sym->st_value;
                        }
                        else
                        {
                            /* Get the symbol address from the symbol table */
                            ulong sym_name_addr = binary +
                                ((Elf64_Shdr*)(binary + ehdr->e_shoff + e_shentsize * cur_symtab->sh_link))->sh_offset +
                                rela_sym->st_name;
                            string sym_name = new string((sbyte*)sym_name_addr);

                            S = stab.GetAddress(sym_name);
                            if (S == 0)
                                throw new Exception("undefined reference to " + sym_name);
                        }

                        /* Perform the relocation */
                        switch(r_type)
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

            return start;
        }

        public static ulong LoadModule(Virtual_Regions vreg, VirtMem vmem, SymbolTable stab, ulong binary, ulong binary_paddr, string name)
        {
            unsafe
            {
                Elf64_Ehdr* ehdr = VerifyElf(binary);

                /* Allocate a physical region to host the relocated binary */
                ulong loaded_size = GetLoadedSize(binary);
                ulong pie_base = vreg.Alloc(loaded_size, 0x1000, name + "_exec");

                /* Load up the sections marked PT_LOAD, store the PT_DYNAMIC section
                 * for later */
                ulong e_phentsize = (ulong)((ehdr->e_ehsize_phentsize >> 16) & 0xffff);
                uint e_phnum = (uint)(ehdr->e_phnum_shentsize & 0xffff);
                ulong prog_header = binary + ehdr->e_phoff;
                Elf64_Phdr* dynamic_phdr = null;

                for (uint i = 0; i < e_phnum; i++)
                {
                    Elf64_Phdr* cur_phdr = (Elf64_Phdr*)prog_header;

                    if (cur_phdr->p_type == 0x1)
                    {
                        /* We only load types of PT_LOAD (=1) */

                        /* All modules in tysos (as pointed to by the 'binary' heading are mapped directly
                         * from the physical location they have been loaded to by GRUB)
                         * This implies that the offset of any part of a file within a virtual page is the
                         * same as the offset within the physical page it is mapped from.
                         * 
                         * We can take advantage of this.
                         * If the offset of a section within the binary & 0xfff is equal to its load address & 0xfff
                         * then we can just map the physical addresses contained within the section to its load address,
                         * it they are different we need to map in some new pages at the load address and then copy
                         * the data into it
                         */

                        if (((cur_phdr->p_offset + binary_paddr) & 0xfff) == (cur_phdr->p_vaddr & 0xfff))
                        {
                            if(cur_phdr->p_filesz != cur_phdr->p_memsz)
                                throw new Exception("p_filesz != p_memsz - this is currently unsupported");

                            ulong page_offset = cur_phdr->p_vaddr & 0xfff;

                            ulong paddr_start = cur_phdr->p_offset + binary_paddr - page_offset;
                            ulong vaddr_start = pie_base + cur_phdr->p_vaddr - page_offset;
                            ulong length = util.align(cur_phdr->p_filesz + page_offset, 0x1000);

                            for (ulong j = 0; j < length; j += 0x1000)
                                vmem.map_page(vaddr_start + j, paddr_start + j);
                        }
                        else
                            throw new Exception("Binary not paged aligned - this is currently unsupported");
                    }
                    else if (cur_phdr->p_type == 0x2)
                    {
                        /* Store the PT_DYNAMIC section for later */
                        dynamic_phdr = cur_phdr;
                    }

                    prog_header += e_phentsize;
                }

                if (dynamic_phdr == null)
                    throw new Exception("Dynamic section not found");

                Elf64_DynamicEntries dyn_tab = GetDynEntries(ehdr, pie_base);

                if (dyn_tab.dyn_str_vaddr == 0)
                    throw new Exception("Dynamic string table not found");
                if (dyn_tab.dyn_sym_vaddr == 0)
                    throw new Exception("Dynamic symbol table not found");
                if (dyn_tab.rela_tab_vaddr == 0)
                    throw new Exception("Rela table not found");
                if (dyn_tab.rela_length == 0)
                    throw new Exception("Rela table length not found");
                if (dyn_tab.rela_entsize == 0)
                    throw new Exception("Rela entsize not found");
                if (dyn_tab.sym_entsize == 0)
                    throw new Exception("Symbol entsize not found");

                /* Dump the dynamic sections */
                Formatter.WriteLine("Dynamic section entries:", Program.arch.DebugOutput);
                Formatter.Write(" load address: ", Program.arch.DebugOutput);
                Formatter.Write(pie_base, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);              
                Formatter.Write(" DT_DYNSTR:    ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.dyn_str_vaddr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_DYNSYM:    ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.dyn_sym_vaddr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_RELA:      ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.rela_tab_vaddr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_RELAENT:   ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.rela_entsize, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_RELASZ:    ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.rela_length, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_SYMENT:    ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.sym_entsize, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_JMPREL:    ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.pltrela_tab_vaddr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.Write(" DT_PLTRELSZ:  ", Program.arch.DebugOutput);
                Formatter.Write(dyn_tab.pltrela_length, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);

                /* Now iterate through the dynamic relocations and fix up what
                 * needs to be fixed up
                 */

                ulong cur_rela_vaddr = dyn_tab.rela_tab_vaddr;
                ulong rela_tab_end = dyn_tab.rela_tab_vaddr + dyn_tab.rela_length;
                while (cur_rela_vaddr < rela_tab_end)
                {
                    Elf64_Rela* cur_rela = (Elf64_Rela*)cur_rela_vaddr;

                    uint r_sym = (uint)((cur_rela->r_info >> 32) & 0xffffffff);
                    uint r_type = (uint)(cur_rela->r_info & 0xffffffff);

                    ulong* r_loc = (ulong*)(pie_base + cur_rela->r_offset);

                    if (r_type == Elf64_Rela.R_X86_64_64)
                    {
                        /* R_X86_64_64 relocations are 64 bit and point to S + A */
                        ulong sym_addr = GetSymbolAddr(dyn_tab.dyn_sym_vaddr, dyn_tab.dyn_str_vaddr, dyn_tab.sym_entsize, pie_base, stab, r_sym);
                        *r_loc = util.Add(sym_addr, cur_rela->r_addend);
                    }
                    else if (r_type == Elf64_Rela.R_X86_64_GLOB_DAT)
                    {
                        /* R_X86_64_GLOB_DAT relocations are 64 bit and point to S */
                        ulong sym_addr = GetSymbolAddr(dyn_tab.dyn_sym_vaddr, dyn_tab.dyn_str_vaddr, dyn_tab.sym_entsize, pie_base, stab, r_sym);
                        *r_loc = sym_addr;
                    }
                    else if (r_type == Elf64_Rela.R_X86_64_JUMP_SLOT)
                    {
                        /* R_X86_64_JUMP_SLOT relocations are 64 bit and point to S */
                        ulong sym_addr = GetSymbolAddr(dyn_tab.dyn_sym_vaddr, dyn_tab.dyn_str_vaddr, dyn_tab.sym_entsize, pie_base, stab, r_sym);
                        *r_loc = sym_addr;
                    }
                    else
                        throw new Exception("Relocation type not supported (" + r_type.ToString() + ")");

                    cur_rela_vaddr += dyn_tab.rela_entsize;
                }

                /* Iterate through the pltrela sections, if provided */
                if (dyn_tab.pltrela_tab_vaddr != 0)
                {
                    if (dyn_tab.pltrela_length == 0)
                        throw new Exception("DT_PLTRELSZ not found");

                    ulong cur_pltrela_vaddr = dyn_tab.pltrela_tab_vaddr;
                    ulong pltrela_tab_end = dyn_tab.pltrela_tab_vaddr + dyn_tab.pltrela_length;

                    while (cur_pltrela_vaddr < pltrela_tab_end)
                    {
                        Elf64_Rela* cur_rela = (Elf64_Rela*)cur_pltrela_vaddr;

                        uint r_sym = (uint)((cur_rela->r_info >> 32) & 0xffffffff);
                        uint r_type = (uint)(cur_rela->r_info & 0xffffffff);

                        ulong* r_loc = (ulong*)(pie_base + cur_rela->r_offset);

                        if (r_type == Elf64_Rela.R_X86_64_JUMP_SLOT)
                        {
                            /* R_X86_64_JUMP_SLOT relocations are 64 bit and point to S */
                            ulong sym_addr = GetSymbolAddr(dyn_tab.dyn_sym_vaddr, dyn_tab.dyn_str_vaddr, dyn_tab.sym_entsize, pie_base, stab, r_sym);
                            *r_loc = sym_addr;
                        }
                        else
                            throw new Exception("Relocation type not supported (" + r_type.ToString() + ")");

                        cur_pltrela_vaddr += dyn_tab.rela_entsize;
                    }
                }

                return pie_base;
            }
        }
        
        private unsafe static ulong GetSymbolAddr(ulong dyn_sym_vaddr, ulong dyn_str_vaddr, ulong sym_entsize, ulong pie_base, SymbolTable stab, uint r_sym)
        {
            ulong sym_addr = dyn_sym_vaddr + (ulong)r_sym * sym_entsize;
            Elf64_Sym* sym = (Elf64_Sym*)sym_addr;

            uint st_shndx = (uint)(sym->st_info_other_shndx >> 16);

            /*
            Formatter.Write("Resolving symbol: ", Program.arch.DebugOutput);
            Formatter.Write(new string((sbyte*)(dyn_str_vaddr + sym->st_name)), Program.arch.DebugOutput);
             */

            if (st_shndx == 0)
            {
                /* SHN_UNDEF identifies an undefined symbol - we need to fetch it from the global symbol table */
                string sym_name = new string((sbyte*)(dyn_str_vaddr + sym->st_name));
                ulong addr = stab.GetAddress(sym_name);
                /*
                Formatter.Write(" found in symbol table, address: ", Program.arch.DebugOutput);
                Formatter.Write(addr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput); */
                return addr;
            }
            else
            {
                /* Its a symbol defined within this file */
                /*
                Formatter.Write(" locally defined as: ", Program.arch.DebugOutput);
                Formatter.Write(pie_base + sym->st_value, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);*/
                return pie_base + sym->st_value;
            }
        }

        private unsafe static ulong GetLoadedSize(ulong binary)
        {
            Elf64_Ehdr* ehdr = (Elf64_Ehdr*)binary;

            ulong e_phentsize = (ulong)((ehdr->e_ehsize_phentsize >> 16) & 0xffff);
            uint e_phnum = (uint)(ehdr->e_phnum_shentsize & 0xffff);

            ulong prog_header = binary + ehdr->e_phoff;
            ulong loaded_size = 0;
            for (uint i = 0; i < e_phnum; i++)
            {
                Elf64_Phdr* phdr = (Elf64_Phdr*)prog_header;

                /* We only load types of PT_LOAD (=1) */
                if (phdr->p_type == 0x1)
                {
                    ulong cur_end = phdr->p_vaddr + phdr->p_memsz;

                    if (cur_end > loaded_size)
                        loaded_size = cur_end;
                }

                prog_header += e_phentsize;
            }

            return loaded_size;
        }

        public static unsafe void LoadSymbols(SymbolTable stab, Multiboot.Header header)
        {
            ulong sym_vaddr = Program.map_in(header.tysos_sym_tab_paddr, header.tysos_sym_tab_size,
                "tysos_sym_tab");
            ulong str_vaddr = Program.map_in(header.tysos_str_tab_paddr, header.tysos_str_tab_size,
                "tysos_str_tab");

            ulong sym_count = header.tysos_sym_tab_size / header.tysos_sym_tab_entsize;

            for(ulong i = 0; i < sym_count; i++)
            {
                Elf64_Sym* sym = (Elf64_Sym*)(sym_vaddr + i * header.tysos_sym_tab_entsize);

                ulong name_addr = str_vaddr + sym->st_name;
                string name = new string((sbyte*)name_addr);
                ulong st_info = (ulong)(sym->st_info_other_shndx & 0xff);
                ulong st_shndx = (ulong)(sym->st_info_other_shndx >> 16);
                ulong sym_addr = sym->st_value;

                /* we only load symbols with STB_GLOBAL (=1) or STB_WEAK (=2) binding and
                 * of type STT_OBJECT (=1) or STT_FUNC (=2)
                 * 
                 * In st_info, binding is the high 4 bits, symbol type is low 4
                 * 
                 * Therefore we are looking for 00010001b or 00010010b or 00100001b or 00100010b
                 * which is 0x11 or 0x12 or 0x21 or 0x22
                 */

                if ((st_info != 0x11) && (st_info != 0x12) && (st_info != 0x21) && (st_info != 0x22))
                    continue;

                /* We do not want symbols with st_shndx == SHN_UNDEF (=0)
                 * We ignore symbols with st_shndx == SHN_COMMON (=0xfff2) 
                 */

                if ((st_shndx == 0x0) || (st_shndx == 0xfff2))
                    continue;

                if (name.StartsWith("_static_fields"))
                {
                    /* Load up a list of static objects */
                    ulong* cur_so = (ulong*)sym_addr;

                    while (*cur_so != 0)
                    {
                        ulong static_obj_addr = *(cur_so);
                        ulong typeinfo_addr = *(ulong*)(sym_addr + 8);

                        gc.gc.RegisterObject(static_obj_addr);

                        sym_addr += 16;
                        cur_so = (ulong*)sym_addr;
                    }
                }
                else
                    stab.Add(name, sym_addr, sym->st_size);
            }
        }

        public static unsafe void LoadSymbols(SymbolTable stab, ulong binary, ulong symbol_adjust)
        { LoadSymbols(stab, binary, symbol_adjust, 0); }
        public static unsafe void LoadSymbols(SymbolTable stab, ulong binary, ulong symbol_adjust, ulong tyhash_addr)
        {
            Elf64_Ehdr* ehdr = VerifyElf(binary);

            /* Find the symbol table */
            Elf64_Shdr* sym_tab = GetShdrOfType(ehdr, 0x2);

            /* Find the appropriate string table */
            Elf64_Shdr* str_tab = (Elf64_Shdr*)(binary + ehdr->e_shoff + sym_tab->sh_link * ehdr->e_shentsize);

            /* See if we have an available hash table as an external file */
            if (tyhash_addr != 0)
            {
                bool verified = true;

                // Verify the file
                string verify_str = "TYHASH  ";
                ulong tyhash_start = tyhash_addr;
                foreach (char c in verify_str)
                {
                    if (*(byte*)(tyhash_addr++) != (byte)c)
                    {
                        Formatter.WriteLine("ELF: unable to verify tyhash file", Program.arch.DebugOutput);
                        verified = false;
                        break;
                    }
                }

                if (verified)
                {
                    Formatter.WriteLine("ELF: tyhash file verified", Program.arch.DebugOutput);
                    int ver = *(short*)(tyhash_addr);
                    int endian = *(byte*)(tyhash_addr + 2);
                    int bitness = *(byte*)(tyhash_addr + 3);
                    Formatter.Write("ELF: ver: ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)ver, Program.arch.DebugOutput);
                    Formatter.Write(", endianness: ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)endian, Program.arch.DebugOutput);
                    Formatter.Write(", bitness: ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)bitness, Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    ulong ht_offset = *(uint*)(tyhash_addr + 4);
                    ulong fname_offset = *(uint*)(tyhash_addr + 8);

                    tyhash_addr = tyhash_start + fname_offset;
                    StringBuilder sb = new StringBuilder();
                    byte b;
                    while ((b = *(byte*)(tyhash_addr++)) != 0)
                        sb.Append((char)b);
                    Formatter.Write("ELF: object file: ", Program.arch.DebugOutput);
                    Formatter.WriteLine(sb.ToString(), Program.arch.DebugOutput);

                    ulong hash_addr = tyhash_start + ht_offset;
                    ElfHashTable htable2 = new ElfHashTable(hash_addr, bitness, binary + sym_tab->sh_offset, sym_tab->sh_entsize,
                        binary + str_tab->sh_offset, symbol_adjust);
                    stab.symbol_providers.Add(htable2);
                    return;
                }
            }

            /* Else, try to load from the dynamic table of the ELF file */
            /* Prepare the hashtable structure */
            Elf64_DynamicEntries dyn_entries = GetDynEntries(ehdr, symbol_adjust);
            if (dyn_entries == null)
            {
                /* No dynamic table, we have to use our own dictionary instead */
                LoadSymbols2(stab, binary, symbol_adjust);
                return;
            }
            ElfHashTable htable = new ElfHashTable(dyn_entries.hash_vaddr, 1, binary + sym_tab->sh_offset, sym_tab->sh_entsize,
                binary + str_tab->sh_offset, symbol_adjust);
            stab.symbol_providers.Add(htable);
        }

        public static void LoadSymbols2(SymbolTable stab, ulong binary, ulong symbol_adjust)
        {
            unsafe
            {
                Elf64_Ehdr* ehdr = VerifyElf(binary);

                /* Iterate through the section headers and find the symbol table */
                ulong sect_header = binary + ehdr->e_shoff;
                bool found = false;
                ulong sym_tab_start = 0;
                ulong str_tab_sect_header = 0;
                ulong sym_tab_entsize = 0;
                ulong sym_tab_size = 0;
                
                for (uint i = 0; i < (ehdr->e_shnum_shstrndx & 0xffff); i++)
                {
                    Elf64_Shdr* shdr = (Elf64_Shdr*)sect_header;

                    /* SHT_SYMTAB is 2 */
                    if (shdr->sh_type == 2)
                    {
                        sym_tab_start = binary + shdr->sh_offset;
                        str_tab_sect_header = binary + ehdr->e_shoff + shdr->sh_link * (ulong)((ehdr->e_phnum_shentsize >> 16) & 0xffff);
                        sym_tab_entsize = shdr->sh_entsize;
                        sym_tab_size = shdr->sh_size;
                        found = true;
                        break;
                    }

                    sect_header += (ulong)((ehdr->e_phnum_shentsize >> 16) & 0xffff);
                }
                if (!found)
                    throw new Exception("Symbol table not found");

                /* Load the string table */
                Elf64_Shdr* str_tab = (Elf64_Shdr*)str_tab_sect_header;

                /* Iterate through all symbols, loading them into the symbol table */
                ulong sym_tab_offset = 0;
                while (sym_tab_offset < sym_tab_size)
                {
                    Elf64_Sym* cur_sym = (Elf64_Sym*)(sym_tab_start + sym_tab_offset);

                    ulong name_addr = binary + str_tab->sh_offset + (ulong)cur_sym->st_name;
                    ulong st_info = (ulong)(cur_sym->st_info_other_shndx & 0xff);
                    ulong st_shndx = (ulong)(cur_sym->st_info_other_shndx >> 16);

                    /* we only load symbols with STB_GLOBAL (=1) binding and
                     * of type STT_OBJECT (=1) or STT_FUNC (=2)
                     * 
                     * In st_info, binding is the high 4 bits, symbol type is low 4
                     * 
                     * Therefore we are looking for 00010001b or 00010010b
                     * which is 0x11 or 0x12
                     */

                    if ((st_info != 0x11) && (st_info != 0x12))
                    {
                        sym_tab_offset += sym_tab_entsize;
                        continue;
                    }

                    /* We do not want symbols with st_shndx == SHN_UNDEF (=0)
                     * We ignore symbols with st_shndx == SHN_COMMON (=0xfff2) 
                     */

                    if ((st_shndx == 0x0) || (st_shndx == 0xfff2))
                    {
                        sym_tab_offset += sym_tab_entsize;
                        continue;
                    }
                   
                    string sym_name = new string((sbyte*)name_addr);
                    ulong sym_addr = cur_sym->st_value + symbol_adjust;

                    if (sym_name.StartsWith("_static_fields"))
                    {
                        /* Load up a list of static objects */
                        ulong* cur_so = (ulong*)sym_addr;

                        while (*cur_so != 0)
                        {
                            ulong static_obj_addr = *(cur_so);
                            ulong typeinfo_addr = *(ulong*)(sym_addr + 8);

                            gc.gc.RegisterObject(static_obj_addr);

                            sym_addr += 16;
                            cur_so = (ulong*)sym_addr;
                        }
                    }
                    else if(sym_name != "_start")
                        stab.Add(sym_name, sym_addr);

                    sym_tab_offset += sym_tab_entsize;
                }
                Formatter.WriteLine("ELF file parsed successfully", Program.arch.DebugOutput);
            }
        }

        class ElfHashTable : SymbolTable.SymbolProvider
        {
            ulong nbucket, nchain;
            Collections.StaticULongArray bucket_l;
            Collections.StaticULongArray chain_l;
            Collections.StaticUIntArray bucket_i;
            Collections.StaticUIntArray chain_i;

            ulong sym_tab_start;
            ulong sym_tab_entsize;
            ulong sym_name_tab_start;
            ulong symbol_adjust;
            int bitness;

            public ElfHashTable(ulong elfhash_addr, int _bitness, ulong _sym_tab_start,
                ulong _sym_tab_entsize, ulong _sym_tab_name_start, ulong _symbol_adjust)
            {
                bitness = _bitness;
                sym_tab_start = _sym_tab_start;
                sym_tab_entsize = _sym_tab_entsize;
                sym_name_tab_start = _sym_tab_name_start;
                symbol_adjust = _symbol_adjust;

                unsafe
                {
                    switch (bitness)
                    {
                        case 0:
                            nbucket = (ulong)*(int*)(elfhash_addr);
                            nchain = (ulong)*(int*)(elfhash_addr + 4);
                            bucket_i = new Collections.StaticUIntArray(elfhash_addr + 8, nbucket);
                            chain_i = new Collections.StaticUIntArray(elfhash_addr + 8 + 4 * nbucket, nchain);
                            break;
                        case 1:
                            nbucket = (ulong)*(long*)(elfhash_addr);
                            nchain = (ulong)*(long*)(elfhash_addr + 8);
                            bucket_l = new Collections.StaticULongArray(elfhash_addr + 16, nbucket);
                            chain_l = new Collections.StaticULongArray(elfhash_addr + 16 + 8 * nbucket, nchain);
                            break;
                    }
                }
            }

            protected internal override ulong GetAddress(string s)
            {
                switch (bitness)
                {
                    case 0:
                        return GetAddress32(s);
                    case 1:
                        return GetAddress64(s);
                    default:
                        throw new Exception("Invalid bitness type");
                }
            }

            protected unsafe internal ulong GetAddress64(string s)
            {
                uint hash = HashFunction(s);
                ulong cur_sym_idx = bucket_l[hash % nbucket];

                while (cur_sym_idx != 0)
                {
                    Elf64_Sym* cur_sym = (Elf64_Sym*)(sym_tab_start + cur_sym_idx * sym_tab_entsize);
                    ulong name_addr = sym_name_tab_start + (ulong)cur_sym->st_name;
                    string sym_name = new string((sbyte*)name_addr);

                    if (s.Equals(sym_name))
                        return cur_sym->st_value + symbol_adjust;

                    cur_sym_idx = chain_l[cur_sym_idx];
                }

                return 0;
            }

            protected unsafe internal ulong GetAddress32(string s)
            {
                uint hash = HashFunction(s);
                uint cur_sym_idx = bucket_i[hash % nbucket];

                while (cur_sym_idx != 0)
                {
                    Elf64_Sym* cur_sym = (Elf64_Sym*)(sym_tab_start + cur_sym_idx * sym_tab_entsize);
                    ulong name_addr = sym_name_tab_start + (ulong)cur_sym->st_name;
                    string sym_name = new string((sbyte*)name_addr);

                    if (s.Equals(sym_name))
                        return cur_sym->st_value + symbol_adjust;

                    cur_sym_idx = chain_i[cur_sym_idx];
                }

                return 0;
            }

            protected internal override string GetSymbol(ulong address)
            {
                return null;
            }

            protected internal override string GetSymbolAndOffset(ulong address, out ulong offset)
            {
                offset = 0;
                return null;
            }

            internal static uint HashFunction(string s)
            {
                uint h = 0;
                uint g;

                foreach (char c in s)
                {
                    h = (h << 4) + (byte)c;
                    g = h & 0xf0000000;
                    if (g != 0)
                        h ^= g >> 24;
                    h &= 0x0fffffff;
                }

                return h;
            }
        }
    }
}
