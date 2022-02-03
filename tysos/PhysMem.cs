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

#if !NET6_0_OR_GREATER
#if _WIN32
    using nuint = UInt32;
    using nint = Int32;
#else
    using nuint = UInt64;
    using nint = Int64;
#endif
#endif

    unsafe abstract class PhysMem : PageProvider
    {
        public VirtMem vmem;

        public abstract nuint Allocate(nuint len);
        public abstract nuint AllocateFixed(nuint paddr, nuint len);
        public abstract void Release(nuint paddr, nuint len);
        public abstract nuint FreeSpace { get; }

        nuint bp = nuint.MaxValue;
        public virtual nuint BlankPage
        {
            get
            {
                if (bp == nuint.MaxValue)
                    bp = GetPage();
                return bp;
            }
        }

        abstract protected class PmemRegion
        {
            public PmemRegion next;
            public uint zone;
            public PhysMem pmem;

            public abstract void Release(nuint paddr, nuint len);
            public abstract nuint Allocate(nuint len);
            public abstract nuint AllocateFixed(nuint paddr, nuint len);
            public abstract nuint FreeSpace { get; }

            public virtual nuint StartAddr { get; protected set; }
            public virtual nuint Length { get; protected set; }
            public virtual nuint EndAddr { get { return StartAddr + Length; } }
        }

        /** <summary>Bitmap</summary> */
        protected class PmemBitmap : PmemRegion
        {
            nuint* bmp;
            nuint baddr;
            nuint bcount;
            nuint ucount;
            nuint ssize;

            public PmemBitmap(PhysMem _pmem, nuint paddrbase, nuint len,
                nuint _ssize = 4096)
            {
                pmem = _pmem;
                ssize = _ssize;

                // Calculate the size of the bitmap
                bcount = (ulong)len / ssize;
                ucount = util.align(bcount, 64) / 64;

                // Zero out the array
                bmp = (ulong*)pmem.vmem.Map(paddrbase, bcount, 0, 0).vaddr;
                for (ulong i = 0; i < ucount; i++)
                    bmp[i] = 0UL;

                // Add the used pages
                var x = util.align(paddrbase + ucount * 8, ssize);
                StartAddr = x;
                Length = len - (x - paddrbase);

                baddr = util.align(paddrbase, ssize);
                while(x + ssize <= (ulong)paddrbase + (ulong)len)
                {
                    var bit_idx = (x - baddr) / ssize;
                    var uidx = bit_idx / 64;
                    var bidx = bit_idx % 64;

                    bmp[uidx] |= (1UL) << (int)bidx;

                    x += ssize;
                }
            }

            public override nuint Allocate(nuint len)
            {
                throw new NotImplementedException();
            }

            public override nuint AllocateFixed(nuint paddr, nuint len)
            {
                throw new NotImplementedException();
            }

            public override void Release(nuint paddr, nuint len)
            {
                throw new NotImplementedException();
            }

            public override ulong FreeSpace => throw new NotImplementedException();
        }

        /** <summary>One level stack</summary> */
        protected class PmemStack : PmemRegion
        {
            nuint ssize;
            Collections.RingBuffer<nuint> rbs;

            public PmemStack(PhysMem _pmem, nuint paddrbase, nuint len,
                nuint _ssize = 4096)
            {
                pmem = _pmem;
                ssize = _ssize;

                // Calculate start addresse and length for the ring buffer structure
                nuint sbasep = paddrbase;
                nuint slen = len / _ssize * (nuint)sizeof(nuint);

                // Create the ring buffer
                rbs = new Collections.RingBuffer<nuint>((void*)pmem.vmem.Map(sbasep, slen, 0, 0).vaddr, (int)slen);

                // Add the pages
                nuint x = slen + sbasep;
                x = util.align(x, ssize);
                StartAddr = x;
                Length = len - (x - paddrbase);

                while (x + ssize <= paddrbase + len)
                {
                    rbs.Enqueue(x);
                    x += ssize;
                }
            }

            public override void Release(nuint paddr, nuint len)
            {
                throw new NotImplementedException();
            }

            public override nuint Allocate(nuint len)
            {
                if (len > ssize)
                    return 0;

                if (rbs.Dequeue(out var ret))
                    return ret;
                else
                    return 0;
            }

            public override nuint AllocateFixed(nuint paddr, nuint len)
            {
                throw new NotImplementedException();
            }

            public override ulong FreeSpace => (ulong)rbs.Count * ssize;
        }

        /** <summary>Implements a buddy-esque two-level stack with small
         *   and large page sizes</summary> */
        protected class PmemTwoLevelStack : PmemRegion
        {
            nuint ssize, lsize;
            Collections.RingBuffer<nuint> rbs, rbl;

            public PmemTwoLevelStack(PhysMem _pmem, nuint paddrbase, nuint len,
                nuint _ssize = 4096, nuint _lsize = 2048 * 1024)
            {
                pmem = _pmem;
                ssize = _ssize;
                lsize = _lsize;

                // Calculate start addresses and lengths for the ring buffer structure
                nuint sbasep = paddrbase;
                nuint slen = len / _ssize * (nuint)sizeof(nuint);
                nuint lbasep = sbasep + slen;
                nuint llen = len / _lsize * (nuint)sizeof(nuint);

                // Create the ring buffers
                rbs = new Collections.RingBuffer<nuint>((void*)pmem.vmem.Map(sbasep, slen, 0, 0).vaddr, (int)slen);
                rbl = new Collections.RingBuffer<nuint>((void*)pmem.vmem.Map(lbasep, llen, 0, 0).vaddr, (int)llen);

                // Add the pages, use small pages at either end and then large pages in the middle
                nuint x = llen + lbasep;
                x = util.align(x, ssize);
                StartAddr = x;
                Length = len - (x - paddrbase);

                // This assumes page sizes are powers of 2
                var lmask = lsize - 1;

                // Small pages at start
                while((x & lmask) != 0 & (x + ssize <= paddrbase + len))
                {
                    rbs.Enqueue(x);
                    x += ssize;
                }

                // Large pages in middle
                while(x + lsize <= paddrbase + len)
                {
                    rbl.Enqueue(x);
                    x += lsize;
                }

                // Small pages at end
                while(x + ssize <= paddrbase + len)
                {
                    rbs.Enqueue(x);
                    x += ssize;
                }

                // Debug
                Formatter.Write("PmemTwoLevelStack: rbs count ", Program.arch.DebugOutput);
                Formatter.Write((ulong)rbs.Count, Program.arch.DebugOutput);
                Formatter.Write(", lbs count ", Program.arch.DebugOutput);
                Formatter.Write((ulong)rbl.Count, Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
            }

            public override void Release(nuint paddr, nuint len)
            {
                throw new NotImplementedException();
            }

            public override nuint Allocate(nuint len)
            {
                if (len > lsize)
                    return 0;
                
                if(len <= ssize)
                {
                    // Small alloc
                    if (rbs.Dequeue(out var ret))
                        return ret;
                    else
                    {
                        // See if we can break up a large block
                        if(rbl.Dequeue(out ret))
                        {
                            Formatter.Write("PmemTwoLevelStack break large block: rbs count ", Program.arch.DebugOutput);
                            Formatter.Write((ulong)rbs.Count, Program.arch.DebugOutput);
                            Formatter.Write(", lbs count ", Program.arch.DebugOutput);
                            Formatter.Write((ulong)rbl.Count, Program.arch.DebugOutput);

                            // we can - store everything after the first entry back
                            for (nuint x = ret + ssize; x < ret + lsize; x += ssize)
                                rbs.Enqueue(x);

                            Formatter.Write(" -> rbs count ", Program.arch.DebugOutput);
                            Formatter.Write((ulong)rbs.Count, Program.arch.DebugOutput);
                            Formatter.Write(", lbs count ", Program.arch.DebugOutput);
                            Formatter.Write((ulong)rbl.Count, Program.arch.DebugOutput);
                            Formatter.WriteLine(Program.arch.DebugOutput);

                            return ret;
                        }
                        else
                        {
                            // Cannot
                            return 0;
                        }
                    }
                }
                else
                {
                    // Large alloc
                    if (rbl.Dequeue(out var ret))
                        return ret;
                    else
                        return 0;
                }
            }

            public override nuint AllocateFixed(nuint paddr, nuint len)
            {
                throw new NotImplementedException();
            }

            public override ulong FreeSpace => (ulong)rbs.Count * ssize + (ulong)rbl.Count * lsize;
        }
    }

    public abstract class PageProvider
    {
        public abstract ulong GetPage();
    }
}
