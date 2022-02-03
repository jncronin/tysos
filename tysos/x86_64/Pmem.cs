using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.x86_64
{
    class Pmem : PhysMem
    {
        PhysMem.PmemBitmap dma;
        PhysMem.PmemRegion upper;

        public override ulong GetPage()
        {
            var ret = Allocate(0x1000);

            /*Formatter.Write("x86_64.Pmem.GetPage() returning ", Program.arch.DebugOutput);
            Formatter.Write(ret, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);*/

            if(ret == 0)
            {
                Formatter.WriteLine("x86_64.Pmem.GetPage() returning null", Program.arch.DebugOutput);

                var x = upper;
                while(x != null)
                {
                    Formatter.Write("Region: ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)x.FreeSpace, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    x = x.next;
                }
                while (true) ;
            }

            libsupcs.MemoryOperations.QuickClearAligned16(Vmem.direct_start + ret, 0x1000);

            return ret;
        }

        public override ulong Allocate(ulong len)
        {
            var x = upper;
            while(x != null)
            {
                var r = x.Allocate(len);
                if (r != 0)
                    return r;
                x = x.next;
            }
            if (dma != null)
                return dma.Allocate(len);
            else
                return 0;
        }

        public override ulong FreeSpace
        {
            get
            {
                ulong v = 0;
                if (dma != null)
                    v += dma.FreeSpace;
                var x = upper;
                while(x != null)
                {
                    v += x.FreeSpace;
                    x = x.next;
                }
                return v;
            }
        }

        public override ulong AllocateFixed(ulong paddr, ulong len)
        {
            throw new NotImplementedException();
        }

        public override void Release(ulong paddr, ulong len)
        {
            // Is the new region entirely within a block? In which case add to it.
            var x = upper;

            while(x != null)
            {
                if (paddr >= x.StartAddr && (paddr + len) < x.EndAddr)
                {
                    x.Release(paddr, len);
                    return;
                }

                x = x.next;
            }

            // Else create a new block
            var nb = new PhysMem.PmemTwoLevelStack(this, paddr, len);

            // Add to start of list?
            if (upper == null)
            {
                if (System.Threading.Interlocked.CompareExchange(ref upper, nb, null) == null)
                    return;
            }

            // Add to the end of the list
            while(true)
            {
                // Find last
                var last = upper;
                while (last.next != null)
                    last = last.next;

                if (System.Threading.Interlocked.CompareExchange(ref last.next, nb, null) == null)
                    return;
            }
        }
    }
}
