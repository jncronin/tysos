using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.x86_64
{
    class EarlyPageProvider : PageProvider
    {
        List<EPPRegion> fb;
        EPPRegion sb = null;

        public class EPPRegion
        {
            public ulong start, length, used;
        }

        public EarlyPageProvider(List<EPPRegion> free_blocks)
        {
            fb = free_blocks;
        }

        public override ulong GetPage()
        {
            if (sb == null || sb.length == 0 || sb.used + 0x1000 > sb.length)
                sb = GetBestSliceBlock();


            sb.used += 0x1000;
            return sb.start + sb.length - sb.used;
        }

        private EPPRegion GetBestSliceBlock()
        {
            /* Algorithm for selecting the next free block is:
             *  largest block > 24-bit (16 MiB)
             *  largest block straddling 16 MiB
             *  largest block < 16 MiB
             *  
             *  All must have space for at least 4 pages (4 page table levels)
             */

            int fidx = -1;
            ulong lblock = 0;
            for (int i = 0; i < fb.Count; i++)
            {
                var cfb = fb[i];
                if (cfb.start >= 0x1000000 && cfb.length > cfb.used + 0x4000 && cfb.length - cfb.used > lblock)
                {
                    lblock = cfb.length - cfb.used;
                    fidx = i;
                }
            }

            if (fidx == -1)
            {
                for (int i = 0; i < fb.Count; i++)
                {
                    var cfb = fb[i];
                    if (cfb.start + cfb.length >= 0x1000000 && cfb.length > cfb.used + 0x4000 && cfb.length - cfb.used > lblock)
                    {
                        lblock = cfb.length - cfb.used;
                        fidx = i;
                    }
                }
            }

            if (fidx == -1)
            {
                for (int i = 0; i < fb.Count; i++)
                {
                    var cfb = fb[i];
                    if (cfb.length > cfb.used + 0x4000 && cfb.length - cfb.used > lblock)
                    {
                        lblock = cfb.length - cfb.used;
                        fidx = i;
                    }
                }
            }

            if (fidx == -1)
            {
                Formatter.WriteLine("GetBestSliceBlock failed", Program.arch.DebugOutput);
                return null;
            }

            var bfb = fb[fidx];
            Formatter.Write("GetBestSliceBlock: ", Program.arch.DebugOutput);
            Formatter.Write(bfb.start, "X", Program.arch.DebugOutput);
            Formatter.Write(", ", Program.arch.DebugOutput);
            Formatter.Write(bfb.length, "X", Program.arch.DebugOutput);
            Formatter.Write(", ", Program.arch.DebugOutput);
            Formatter.Write(bfb.used, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            return bfb;
        }
    }
}
