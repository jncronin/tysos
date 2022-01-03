using System;
using System.Collections.Generic;
using System.Text;

namespace tysos
{
    /* The physical memory allocator is used to return
     *  - most commonly page sized regions for virtual memory mappings
     *  - occasionally larger structures to use as disk buffers, network card buffers etc
     * 
     * It uses a buddy system with 5 pre-defined region sizes:
     *  - 4 kiB
     *  - 2 MiB
     *  - 64 MiB
     *  - 512 MiB
     *  - 1 TiB
     * 
     * These sizes are a trade-off between practicality (typical x86_64 page sizes and
     *  device buffer sizes) and typical memory sizes of targeted systems.
     * 
     * The 1 TiB level is large enough to cover 4 PiB (52-bit physical address space)
     *  and thus needs 4096 entries at 8 bytes each.  The lower levels just need enough
     *  to contain potentially 2 split upper level sizes (e.g. for the 512 MiB size this
     *  is 2048 entries * 2 = 4096 entries)
     *  
     * 
     * Each individual buddy level is a stack rather than a bitmap (for fast access),
     *  therefore coalescing is not permitted (it is presumed that device buffers will
     *  only be allocated once).
     * Using the RingBuffer<> class we can make everything lock free.
     * 
     * If a request is made for a region of a particular size, first the smallest possible
     *  region size equal to or greater than the request size is identified
     * 
     * If there is a free region of that size then it is returned
     * 
     * Else, a region of the next size up is chosen, and broken up to give the appropriate
     *  sizes.
     * 
     * To add to the allocator, call the AddRegion function - this will automatically align
     *  each region of the appropriate size and set the appropriate bits free.
     */


    unsafe class PhysMem
    {
        Collections.RingBuffer<IntPtr> rb4, rb2m, rb64m, rb512m, rb1t;

        // entries per next level up
        const int epl4 = 512;   // number of 4k pages in 2 MiB
        const int epl2m = 32;   // number of 2 MiB pages in 64 MiB
        const int epl64m = 8;   // number of 64 MiB pages in 512 MiB
        const int epl512m = 2048; // number of 512 MiB pages in 1 TiB

        // required size of each level (in terms of entries)
        const int s4 = epl4 * 2;
        const int s2m = epl2m * 2;
        const int s64m = epl64m * 2;
        const int s512m = epl512m * 2;
        const int s1t = 4096;   // Up to 4 PiB

        struct used_page
        {
            void* vaddr;
            uint flags;
        }

        struct free_page
        {
            void* paddr;
        }

        Collections.RingBuffer<used_page> rbup;
        Collections.RingBuffer<free_page> rbfp;
        
    }
}
