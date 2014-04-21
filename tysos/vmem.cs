using System;
using System.Collections.Generic;
using System.Text;

namespace testca
{
    class vmem
    {
        const ulong VA_PML4T =  0xfffffffffffff000UL;
        const ulong VA_PDPT =   0xffffffffffe00000UL;
        const ulong VA_PD =     0xffffffffc0000000UL;
        const ulong VA_PT =     0xffffff8000000000UL;

        const ulong PageMask =  0xfffffffffffff000UL;

        const ulong P_PRESENT = 0x1UL;
        const ulong P_WRITEABLE = 0x2UL;

        public IPhysMemAllocator pmem;

        Kernel.StaticArray pmlt4, pdpt, pd, pt;

        ulong get_pml4t_entry_no(ulong addr)
        {
            return (addr >> 39) & 0x1ffUL;
        }
        ulong get_pdpt_entry_no(ulong addr)
        {
            return (addr >> 30) & 0x3fffUL;
        }
        ulong get_pd_entry_no(ulong addr)
        {
            return (addr >> 21) & 0x7ffffffUL;
        }
        ulong get_pt_entry_no(ulong addr)
        {
            return (addr >> 12) & 0xfffffffffUL;
        }

        public bool map_page(ulong paddr, ulong vaddr)
        {
            ulong pmlt4_ent = get_pml4t_entry_no(vaddr);
            ulong pdpt_ent = get_pdpt_entry_no(vaddr);
            ulong pd_ent = get_pd_entry_no(vaddr);
            ulong pt_ent = get_pt_entry_no(vaddr);

            make_pagestruct_entry(pmlt4, pmlt4_ent, true, false, 0, false, VA_PDPT + pdpt_ent * 8);
            make_pagestruct_entry(pdpt, pdpt_ent, true, false, 0, false, VA_PD + pd_ent * 8);
            make_pagestruct_entry(pd, pd_ent, true, false, 0, false, VA_PD + pt_ent * 8);

            make_pagestruct_entry(pt, pt_ent, false, true, paddr, false, vaddr);

            return true;
        }

        void make_pagestruct_entry(Kernel.StaticArray parent, ulong index, bool alloc_new, bool overwrite,
            ulong new_page_paddr, bool unmap, ulong vaddr)
        {
            if (unmap)
            {
                parent.Set(index, 0UL);
                Kernel.Kernel.invlpg(vaddr);
            }
            else if (overwrite)
            {
                parent.Set(index, new_page_paddr | P_PRESENT | P_WRITEABLE);
                Kernel.Kernel.invlpg(vaddr);
            }
            else
            {
                if ((parent.GetUlong(index) & P_PRESENT) != P_PRESENT)
                {
                    if (alloc_new)
                    {
                        // Get a new physical page and map it
                        ulong new_addr = pmem.BeginAlloc();
                        if (new_addr == 0)
                            throw new Exception();
                        parent.Set(index, new_page_paddr | P_PRESENT | P_WRITEABLE);
                        Kernel.Kernel.invlpg(vaddr);
                        
                        // Clear the new page
                        ulong vaddr_base = vaddr & PageMask;
                        pmem.EndAlloc(vaddr_base, new_addr);
                        Kernel.StaticArray sa_vaddr = new Kernel.StaticArray(vaddr_base, 0x1000);
                        sa_vaddr.Clear();
                    }
                    else
                    {
                        parent.Set(index, new_page_paddr | P_PRESENT | P_WRITEABLE);
                        Kernel.Kernel.invlpg(vaddr);
                    }
                }
            }
        }

        public vmem()
        {
            pmlt4 = new Kernel.StaticArray(VA_PML4T, 0x1000UL, 8);
            pdpt = new Kernel.StaticArray(VA_PDPT, 0x200000UL, 8);
            pd = new Kernel.StaticArray(VA_PD, 0x40000000UL, 8);
            pt = new Kernel.StaticArray(VA_PT, 0x8000000000UL, 8);
            pmem = null;
        }
    }
}
