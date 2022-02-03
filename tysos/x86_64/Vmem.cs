using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.x86_64
{
    unsafe class Vmem : VirtMem
    {
        /* For x86_64 there are currently 4 page table levels, covering a 48-bit virtual space (256 TiB).
         * 
         * The physical memory limits for Tysos are defined as 1 PML4T entry i.e. 0.5 TiB/512 GiB
         * This will increase for a future 5-level page table version.
         * 
         * We divide the virtual space to be:
         *  - Top 512 GiB - recursive mappings                                  @ 0xffffff8000000000
         *  - Next highest 512 GiB - direct mapping of all physical memory      @ 0xffffff0000000000
         *  - Everything else in higher half - heap                             @ 0xffff800000000000
         */

        const ulong pmem_max = 0x8000000000UL;
        internal const ulong direct_start = 0xffffff0000000000UL;

        const ulong ps4k = 0x1000UL;
        const ulong ps2m = 0x200000UL;
        const ulong ps1g = 0x40000000UL;

        const ulong pm4k = ps4k - 1UL;
        const ulong pm2m = ps2m - 1UL;
        const ulong pm1g = ps1g - 1UL;

        const ulong pstruct_start = 0xffffff8000000000;
        ulong* pstructs = (ulong*)pstruct_start;
        const ulong paddr_mask = 0xffffffffff000;
        const ulong page_mask = 0xfffffffffffff000;
        const ulong canonical_only = 0xffffffffffff;

        const ulong psize = 0x1000;

        public override VMapping Map(ulong paddr, ulong len, ulong vaddr, uint flags)
        {
            // If no vaddr is specified, we can simply use the direct mapping
            if(vaddr == 0 && ((flags & FLAG_allocate) == 0))
            {
                if ((ulong)paddr + (ulong)len > pmem_max)
                    throw new ArgumentOutOfRangeException("paddr", "paddr + len exceeds physical memory limits");

                return new VMapping { flags = 0, paddr = paddr, len = len, vaddr = paddr + direct_start };
            }

            // Map as 4k chunks for now

            /*Formatter.Write("x86_64.Vmem.Map, paddr=", Program.arch.DebugOutput);
            Formatter.Write(paddr, "X", Program.arch.DebugOutput);       
            Formatter.Write(", vaddr=", Program.arch.DebugOutput);
            Formatter.Write(vaddr, "X", Program.arch.DebugOutput);
            Formatter.Write(", len=", Program.arch.DebugOutput);
            Formatter.Write(len, "X", Program.arch.DebugOutput);*/
            
            // Align everything to 4k
            var new_paddr = paddr & ~0xfffUL;
            len = util.align(len + paddr, 0x1000) - new_paddr;
            vaddr &= ~0xfffUL;
            paddr = new_paddr;

            var cur_paddr = paddr;
            var cur_vaddr = vaddr;
            var cur_len = len;


            /*Formatter.Write(", new_paddr=", Program.arch.DebugOutput);
            Formatter.Write(new_paddr, "X", Program.arch.DebugOutput);
            Formatter.Write(", new_vaddr=", Program.arch.DebugOutput);
            Formatter.Write(vaddr, "X", Program.arch.DebugOutput);
            Formatter.Write(", new_len=", Program.arch.DebugOutput);
            Formatter.Write(len, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);*/

            while(cur_len != 0UL)
            {
                if ((flags & FLAG_allocate) != 0)
                    cur_paddr = pmem.GetPage();
                Map4k(cur_vaddr, cur_paddr, pmem, flags);

                cur_len -= 0x1000UL;
                cur_paddr += 0x1000UL;
                cur_vaddr += 0x1000UL;
            }

            return new VMapping { flags = flags, paddr = paddr, len = len, vaddr = vaddr };
        }

        public override ulong PageSize => psize;

        public Vmem()
        {
            VirtMem.cur_vmem = this;
        }

        /** <summary>Generate the required page tables to support a direct mapping of
         * physical memory at 0xffffff0000000000</summary> */
        public unsafe void GenerateDirectMapping(List<EarlyPageProvider.EPPRegion> free_blocks, PageProvider pp)
        {
            // TODO: get from CPUID
            bool arch_has_2m_pages = false;
            bool arch_has_1g_pages = false;

            // Current virtual addresses of page table entries
            //ulong* cpml4, cpdpt, cpd, cpt;

            // Loop through each block
            for(int i = 0; i < free_blocks.Count; i++)
            {
                var fb = free_blocks[i];

                var x = fb.start;

                while (x + 0x1000 <= fb.start + fb.length)
                {
                    // Can we do a large mapping?
                    if (arch_has_1g_pages && ((x & pm1g) == 0) && (x + ps1g <= fb.start + fb.length))
                    {
                        Map1G(direct_start + x, x, pp, FLAG_writeable);
                        x += ps1g;
                    }
                    else if(arch_has_2m_pages && ((x * pm2m) == 0) && (x + ps2m <= fb.start + fb.length))
                    {
                        Map2M(direct_start + x, x, pp, FLAG_writeable);
                        x += ps2m;
                    }
                    else
                    {
                        Map4k(direct_start + x, x, pp, FLAG_writeable);
                        x += ps4k;
                    }
                }
            }
        }

        ulong get_pml4t_entry_addr(ulong vaddr)
        { return (vaddr >> 39) + 0xffffffe00; }
        ulong get_pdpt_entry_addr(ulong vaddr)
        { return (vaddr >> 30) + 0xffffc0000; }
        ulong get_pd_entry_addr(ulong vaddr)
        { return (vaddr >> 21) + 0xff8000000; }
        ulong get_pt_entry_addr(ulong vaddr)
        { return vaddr >> 12; }
        bool is_enabled(ulong pte)
        {
            if ((pte & 0x1) == 0x1)
                return true;
            return false;
        }

        private void Map4k(ulong vaddr, ulong paddr, PageProvider pp, uint flags = 0)
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
            if ((pstructs[pml4t_entry_addr] & 0x1) == 0)
            {
                ulong p_page = pp.GetPage();
                pstructs[pml4t_entry_addr] = 0x3 | (p_page & paddr_mask);
                libsupcs.x86_64.Cpu.Invlpg((pdpt_entry_addr * 8 + pstruct_start) & page_mask);
                libsupcs.MemoryOperations.QuickClearAligned16((pdpt_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
            }
            if ((pstructs[pdpt_entry_addr] & 0x1) == 0)
            {
                ulong p_page = pp.GetPage();
                pstructs[pdpt_entry_addr] = 0x3 | (p_page & paddr_mask);
                libsupcs.x86_64.Cpu.Invlpg((pd_entry_addr * 8 + pstruct_start) & page_mask);
                libsupcs.MemoryOperations.QuickClearAligned16((pd_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
            }
            if ((pstructs[pdpt_entry_addr] & 0x1) == 0)
            {
                ulong p_page = pp.GetPage();
                pstructs[pdpt_entry_addr] = 0x3 | (p_page & paddr_mask);
                libsupcs.x86_64.Cpu.Invlpg((pd_entry_addr * 8 + pstruct_start) & page_mask);
                libsupcs.MemoryOperations.QuickClearAligned16((pd_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
            }
            if ((pstructs[pd_entry_addr] & 0x1) == 0)
            {
                ulong p_page = pp.GetPage();
                pstructs[pd_entry_addr] = 0x3 | (p_page & paddr_mask);
                libsupcs.x86_64.Cpu.Invlpg((pt_entry_addr * 8 + pstruct_start) & page_mask);
                libsupcs.MemoryOperations.QuickClearAligned16((pt_entry_addr * 8 + pstruct_start) & page_mask, 0x1000);
            }

            /* Set the attributes of the page */
            ulong page_attrs = 0x1; // Present bit
            if ((flags & FLAG_writeable) != 0)
                page_attrs |= 0x2;
            if ((flags & FLAG_write_through) != 0)
                page_attrs |= 0x8;
            if ((flags & FLAG_cache_disable) != 0)
                page_attrs |= 0x10;

            pstructs[pt_entry_addr] = page_attrs | (paddr & paddr_mask);
            libsupcs.x86_64.Cpu.Invlpg(vaddr & page_mask);
        }

        private void Map2M(ulong v, ulong x, PageProvider pp, uint flags = 0)
        {
            throw new NotImplementedException();
        }

        private void Map1G(ulong v, ulong x, PageProvider pp, uint flags = 0)
        {
            throw new NotImplementedException();
        }

        public override bool IsValid(ulong vaddr)
        {
            /* Follows the same rules as map_page */
            ulong page_index = vaddr & canonical_only;

            // Indices within the pagine hierarchy
            ulong pml4t_entry_addr = get_pml4t_entry_addr(page_index);
            ulong pdpt_entry_addr = get_pdpt_entry_addr(page_index);
            ulong pd_entry_addr = get_pd_entry_addr(page_index);
            ulong pt_entry_addr = get_pt_entry_addr(page_index);

            if (!is_enabled(pstructs[pml4t_entry_addr]))
                return false;
            if (!is_enabled(pstructs[pdpt_entry_addr]))
                return false;
            if (!is_enabled(pstructs[pd_entry_addr]))
                return false;
            if (!is_enabled(pstructs[pt_entry_addr]))
                return false;

            return true;
        }
    }
}
