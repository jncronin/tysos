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
using System.Runtime.CompilerServices;

namespace tysos
{
    unsafe class VirtMem
    {
        Pmem pmem;
        ulong temp_page_va;
        tysos.Collections.StaticULongArray pml4t;
        tysos.Collections.StaticULongArray paging_structures;
        tysos.Collections.StaticULongArray temp_page;

        ulong* pstructs;

        public const ulong page_size = 0x1000;
        
        /* Constants to pass to map_page - alloc_page means map a new page r/w,
         * blank_page means map the blank page read-only */
        internal const ulong alloc_page = 0xffffffffffffffff;
        internal const ulong blank_page = 0xfffffffffffffffe;

        const ulong paddr_mask = 0xffffffffff000;
        const ulong page_mask = 0xfffffffffffff000;
        const ulong canonical_only = 0xffffffffffff;
        const ulong pstruct_start = 0xffffff8000000000;

        public ulong blank_page_paddr = 0;

        static VirtMem cur_vmem;

        public VirtMem(Pmem _pmem, ulong _temp_page_va)
        {
            pmem = _pmem;
            temp_page_va = _temp_page_va;

            /* Ensure that the last entry of the table points back on itself and is enabled */
            // This is now done by efiloader
            pml4t = new tysos.Collections.StaticULongArray(page_mask, 512);
            /*ulong cr3 = libsupcs.x86_64.Cpu.Cr3;
            ulong last_paddr = cr3 & paddr_mask;
            last_paddr |= 0x3;
            pml4t[511] = last_paddr;

            libsupcs.x86_64.Cpu.Cr3 = cr3;*/

            /* The paging structures now occupy the last 512 GiB (0x8000000000) of virtual memory
             *  which starts at 0xffffff8000000000 
             *  
             * There are a total of 512 * 512 * 512 * 512   8 byte entries
             * 
             * The last 512                             entries are in the PML4T
             * The last 512 x 512                       entries are in the PDPTs
             * The last 512 x 512 x 512                 entries are in the PDs
             * The other                                entries are in the PTs
             * 
             * The PDs therefore start at entry  (512^4 - 512^3)    = 0xff8000000
             * The PDPTs         start at entry  (512^4 - 512^2)    = 0xffffc0000
             * The PMLT          starts at entry (512^4 - 512^1)    = 0xffffffe00
             */

            paging_structures = new tysos.Collections.StaticULongArray(0xffffff8000000000, 0x1000000000);
            pstructs = (ulong*)(0xffffff8000000000);

            // Ensure we can access the temporary page we have been given
            if (!is_enabled(paging_structures[get_pml4t_entry_addr(_temp_page_va)]))
            {
                Program.arch.BootInfoOutput.Write("Unable to access temporary page: PML4T entry not enabled!");
                libsupcs.OtherOperations.Halt();
            }
            if (!is_enabled(paging_structures[get_pdpt_entry_addr(_temp_page_va)]))
            {
                Program.arch.BootInfoOutput.Write("Unable to access temporary page: PDPT entry not enabled!");
                libsupcs.OtherOperations.Halt();
            }
            if (!is_enabled(paging_structures[get_pd_entry_addr(_temp_page_va)]))
            {
                Program.arch.BootInfoOutput.Write("Unable to access temporary page: PD entry not enabled!");
                libsupcs.OtherOperations.Halt();
            }
            if (!is_enabled(paging_structures[get_pt_entry_addr(_temp_page_va)]))
            {
                Program.arch.BootInfoOutput.Write("Unable to access temporary page: PT entry not enabled!");
                libsupcs.OtherOperations.Halt();
            }

            // Set up write access to the temporary page to allow rapid reading, writing and wiping of it
            temp_page = new tysos.Collections.StaticULongArray(_temp_page_va, 512);

            cur_vmem = this;
        }

        /** <summary>Is the provided virtual address actually mapped?</summary>
         */
        public bool is_valid(ulong vaddr)
        {
            /* Follows the same rules as map_page */
            ulong page_index = vaddr & canonical_only;

            // Indices within the pagine hierarchy
            ulong pml4t_entry_addr = get_pml4t_entry_addr(page_index);
            ulong pdpt_entry_addr = get_pdpt_entry_addr(page_index);
            ulong pd_entry_addr = get_pd_entry_addr(page_index);
            ulong pt_entry_addr = get_pt_entry_addr(page_index);

            if (!is_enabled(paging_structures[pml4t_entry_addr]))
                return false;
            if (!is_enabled(paging_structures[pdpt_entry_addr]))
                return false;
            if (!is_enabled(paging_structures[pd_entry_addr]))
                return false;
            if (!is_enabled(paging_structures[pt_entry_addr]))
                return false;

            return true;
        }

        // Override the IsValidPtr function in libsupcs.x86_64_Unwinder()
        [libsupcs.MethodAlias("_ZN8libsupcs17libsupcs#2Ex86_648Unwinder_10IsValidPtr_Rb_P1y")]
        private static bool IsValidPtr(ulong ptr)
        { return cur_vmem.is_valid(ptr); }

        public ulong map_page(ulong vaddr) { return map_page(vaddr, alloc_page, true, false, false); }
        public ulong map_page(ulong vaddr, ulong paddr) { return map_page(vaddr, paddr, true, false, false); }

        [libsupcs.Profile(false)]
        public ulong map_page(ulong vaddr, ulong paddr, bool writeable, bool cache_disable, bool write_through)
        {
            /* Optimised based on profiling concerns */

            /* in the current 48 bit implementation of x86_64, only the first 48 bits of the address
             * are used, the upper 16 bits need to be a sign-extension (i.e. equal to the 48th bit)
             * The virtual region system should ensure this sign extension, but we need to chop off the sign
             * extension to properly index into the tables */
            ulong page_index = vaddr & canonical_only;

            // Traverse the paging structures to map the page
            ulong pml4t_entry_addr = (page_index >> 39) + 0xffffffe00;
            ulong pdpt_entry_addr = (page_index >> 30) + 0xffffc0000;
            ulong pd_entry_addr = (page_index >> 21) + 0xff8000000;
            ulong pt_entry_addr = page_index >> 12;

            // Create pages in the paging hierarchy as necessary
            //Program.arch.BootInfoOutput.Write("a");
            if ((pstructs[pml4t_entry_addr] & 0x1) == 0)
            {
                if (pmem == null)
                {
                    Program.arch.BootInfoOutput.Write("pmem invalid");
                    libsupcs.OtherOperations.Halt();
                }
                //Formatter.Write("A", Program.arch.DebugOutput);
                ulong p_page = pmem.BeginGetPage();
                //Formatter.Write("1", Program.arch.DebugOutput);
                pstructs[pml4t_entry_addr] = 0x3 | (p_page & paddr_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg((pdpt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("3", Program.arch.DebugOutput);
                pmem.EndGetPage(p_page, (pdpt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("4", Program.arch.DebugOutput);
                libsupcs.MemoryOperations.QuickClearAligned16((pdpt_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
                //Formatter.Write("5", Program.arch.DebugOutput);
            }

            //Program.arch.BootInfoOutput.Write("b");
            if ((pstructs[pdpt_entry_addr] & 0x1) == 0)
            {
                if (pmem == null)
                {
                    Program.arch.BootInfoOutput.Write("pmem invalid");
                    libsupcs.OtherOperations.Halt();
                }
                //Formatter.Write("B", Program.arch.DebugOutput);
                ulong p_page = pmem.BeginGetPage();
                //Formatter.Write("1", Program.arch.DebugOutput);
                pstructs[pdpt_entry_addr] = 0x3 | (p_page & paddr_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg((pd_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("3", Program.arch.DebugOutput);
                pmem.EndGetPage(p_page, (pd_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("4", Program.arch.DebugOutput);
                libsupcs.MemoryOperations.QuickClearAligned16((pd_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
                //Formatter.Write("5", Program.arch.DebugOutput);
            }

            //Program.arch.BootInfoOutput.Write("c");
            if ((pstructs[pd_entry_addr] & 0x1) == 0)
            {
                if (pmem == null)
                {
                    Program.arch.BootInfoOutput.Write("pmem invalid");
                    libsupcs.OtherOperations.Halt();
                }
                //Formatter.Write("C", Program.arch.DebugOutput);
                ulong p_page = pmem.BeginGetPage();
                //Formatter.Write("1", Program.arch.DebugOutput);
                pstructs[pd_entry_addr] = 0x3 | (p_page & paddr_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg((pt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("3", Program.arch.DebugOutput);
                pmem.EndGetPage(p_page, (pt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("4", Program.arch.DebugOutput);
                libsupcs.MemoryOperations.QuickClearAligned16((pt_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
                //Formatter.Write("5", Program.arch.DebugOutput);
            }

            /* Set the attributes of the page */
            ulong page_attrs = 0x1; // Present bit
            if ((paddr != blank_page) && writeable)
                page_attrs |= 0x2;
            if (write_through)
                page_attrs |= 0x8;
            if (cache_disable)
                page_attrs |= 0x10;

            if (paddr == alloc_page)
            {
                // Allocate a page if there is not one already allocated, or we are requesting
                //  a write to the blank page
                //Program.arch.BootInfoOutput.Write("d");
                if ((pstructs[pt_entry_addr] & 0x1) == 0 || 
                    (writeable && ((pstructs[pt_entry_addr] & page_mask) == blank_page_paddr)))
                {
                    if (pmem == null)
                    {
                        Program.arch.BootInfoOutput.Write("pmem invalid");
                        libsupcs.OtherOperations.Halt();
                    }
                    //Formatter.Write("D", Program.arch.DebugOutput);
                    paddr = pmem.BeginGetPage();
                    //Formatter.Write("1", Program.arch.DebugOutput);
                    pstructs[pt_entry_addr] = page_attrs | (paddr & paddr_mask);
                    //Formatter.Write("2", Program.arch.DebugOutput);
                    libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
                    //Formatter.Write("3", Program.arch.DebugOutput);
                    pmem.EndGetPage(paddr, vaddr & page_mask);
                    //Formatter.Write("4", Program.arch.DebugOutput);
                    libsupcs.MemoryOperations.QuickClearAligned16(vaddr & page_mask, 0x1000);
                    //Formatter.Write("5", Program.arch.DebugOutput);

                    //Formatter.WriteLine(Program.arch.DebugOutput);
                    //Formatter.Write("map: ", Program.arch.DebugOutput);
                    //Formatter.Write(paddr & paddr_mask, "X", Program.arch.DebugOutput);
                    //Formatter.Write(" to ", Program.arch.DebugOutput);
                    //Formatter.Write(vaddr & page_mask, "X", Program.arch.DebugOutput);
                    //Formatter.WriteLine(Program.arch.DebugOutput);

                    return paddr & paddr_mask;
                }
                else
                {
                    //Program.arch.BootInfoOutput.Write("e");
                    return pstructs[pt_entry_addr] & paddr_mask;
                }
            }
            else if(paddr == blank_page)
            {
                //Program.arch.BootInfoOutput.Write("f");
                pstructs[pt_entry_addr] = page_attrs | (blank_page_paddr & paddr_mask);
                libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
                return blank_page_paddr;
            }
            else
            {
                //Program.arch.BootInfoOutput.Write("g");
                // Map the actual page
                //Formatter.Write("E", Program.arch.DebugOutput);
                pstructs[pt_entry_addr] = page_attrs | (paddr & paddr_mask);
                //Formatter.Write("1", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                return paddr & paddr_mask;
            }
        }

        public ulong map_page_old(ulong vaddr, ulong paddr, bool writeable, bool cache_disable, bool write_through)
        {
            /* in the current 48 bit implementation of x86_64, only the first 48 bits of the address
             * are used, the upper 16 bits need to be a sign-extension (i.e. equal to the 48th bit)
             * The virtual region system should ensure this sign extension, but we need to chop off the sign
             * extension to properly index into the tables */
            ulong page_index = vaddr & canonical_only;

            // Traverse the paging structures to map the page
            ulong pml4t_entry_addr = get_pml4t_entry_addr(page_index);
            ulong pdpt_entry_addr = get_pdpt_entry_addr(page_index);
            ulong pd_entry_addr = get_pd_entry_addr(page_index);
            ulong pt_entry_addr = get_pt_entry_addr(page_index);

            // Create pages in the paging hierarchy as necessary
            if (!is_enabled(paging_structures[pml4t_entry_addr]))
            {
                if (pmem == null)
                {
                    Program.arch.BootInfoOutput.Write("pmem invalid");
                    libsupcs.OtherOperations.Halt();
                }
                //Formatter.Write("A", Program.arch.DebugOutput);
                ulong p_page = pmem.BeginGetPage();
                //Formatter.Write("1", Program.arch.DebugOutput);
                paging_structures[pml4t_entry_addr] = 0x3 | (p_page & paddr_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg((pdpt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("3", Program.arch.DebugOutput);
                pmem.EndGetPage(p_page, (pdpt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("4", Program.arch.DebugOutput);
                libsupcs.MemoryOperations.QuickClearAligned16((pdpt_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
                //Formatter.Write("5", Program.arch.DebugOutput);
            }

            if (!is_enabled(paging_structures[pdpt_entry_addr]))
            {
                if (pmem == null)
                {
                    Program.arch.BootInfoOutput.Write("pmem invalid");
                    libsupcs.OtherOperations.Halt();
                }
                //Formatter.Write("B", Program.arch.DebugOutput);
                ulong p_page = pmem.BeginGetPage();
                //Formatter.Write("1", Program.arch.DebugOutput);
                paging_structures[pdpt_entry_addr] = 0x3 | (p_page & paddr_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg((pd_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("3", Program.arch.DebugOutput);
                pmem.EndGetPage(p_page, (pd_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("4", Program.arch.DebugOutput);
                libsupcs.MemoryOperations.QuickClearAligned16((pd_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
                //Formatter.Write("5", Program.arch.DebugOutput);
            }

            if (!is_enabled(paging_structures[pd_entry_addr]))
            {
                if (pmem == null)
                {
                    Program.arch.BootInfoOutput.Write("pmem invalid");
                    libsupcs.OtherOperations.Halt();
                }
                //Formatter.Write("C", Program.arch.DebugOutput);
                ulong p_page = pmem.BeginGetPage();
                //Formatter.Write("1", Program.arch.DebugOutput);
                paging_structures[pd_entry_addr] = 0x3 | (p_page & paddr_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg((pt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("3", Program.arch.DebugOutput);
                pmem.EndGetPage(p_page, (pt_entry_addr * 8 + pstruct_start) & page_mask);
                //Formatter.Write("4", Program.arch.DebugOutput);
                libsupcs.MemoryOperations.QuickClearAligned16((pt_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
                //Formatter.Write("5", Program.arch.DebugOutput);
            }

            /* Set the attributes of the page */
            ulong page_attrs = 0x1; // Present bit
            if ((paddr != blank_page) && writeable)
                page_attrs |= 0x2;
            if (write_through)
                page_attrs |= 0x8;
            if (cache_disable)
                page_attrs |= 0x10;

            if (paddr == alloc_page)
            {
                // Allocate a page if there is not one already allocated, or we are requesting
                //  a write to the blank page
                if (!is_enabled(paging_structures[pt_entry_addr]) ||
                    (writeable && ((paging_structures[pt_entry_addr] & page_mask) == blank_page_paddr)))
                {
                    if (pmem == null)
                    {
                        Program.arch.BootInfoOutput.Write("pmem invalid");
                        libsupcs.OtherOperations.Halt();
                    }
                    //Formatter.Write("D", Program.arch.DebugOutput);
                    paddr = pmem.BeginGetPage();
                    //Formatter.Write("1", Program.arch.DebugOutput);
                    paging_structures[pt_entry_addr] = page_attrs | (paddr & paddr_mask);
                    //Formatter.Write("2", Program.arch.DebugOutput);
                    libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
                    //Formatter.Write("3", Program.arch.DebugOutput);
                    pmem.EndGetPage(paddr, vaddr & page_mask);
                    //Formatter.Write("4", Program.arch.DebugOutput);
                    libsupcs.MemoryOperations.QuickClearAligned16(vaddr & page_mask, 0x1000);
                    //Formatter.Write("5", Program.arch.DebugOutput);
                    return paddr & paddr_mask;
                }
                else
                {
                    return paging_structures[pt_entry_addr] & paddr_mask;
                }
            }
            else if (paddr == blank_page)
            {
                paging_structures[pt_entry_addr] = page_attrs | (blank_page_paddr & paddr_mask);
                libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
                return blank_page_paddr;
            }
            else
            {
                // Map the actual page
                //Formatter.Write("E", Program.arch.DebugOutput);
                paging_structures[pt_entry_addr] = page_attrs | (paddr & paddr_mask);
                //Formatter.Write("1", Program.arch.DebugOutput);
                libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
                //Formatter.Write("2", Program.arch.DebugOutput);
                return paddr & paddr_mask;
            }
        }


        bool is_enabled(ulong pte)
        {
            if ((pte & 0x1) == 0x1)
                return true;
            return false;
        }

        public void SetPmem(Pmem _pmem)
        {
            pmem = _pmem;
        }

        ulong get_pml4t_entry_addr(ulong vaddr)
        { return (vaddr >> 39) + 0xffffffe00; }
        ulong get_pdpt_entry_addr(ulong vaddr)
        { return (vaddr >> 30) + 0xffffc0000; }
        ulong get_pd_entry_addr(ulong vaddr)
        { return (vaddr >> 21) + 0xff8000000; }
        ulong get_pt_entry_addr(ulong vaddr)
        { return vaddr >> 12; }

        /*ulong get_pml4t_entry_no(ulong vaddr)
        { return (vaddr >> 39) & 0x1ff; }
        ulong get_pdpt_entry_no(ulong vaddr)
        { return (vaddr >> 30) & 0x1ff; }
        ulong get_pd_entry_no(ulong vaddr)
        { return (vaddr >> 21) & 0x1ff; }
        ulong get_pt_entry_no(ulong vaddr)
        { return (vaddr >> 12) & 0x1ff; }*/
    }
}
