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

/* This is the class which stores a list of all regions known to the operating system */

namespace tysos
{
    class Virtual_Regions
    {
        public class Region
        {
            public ulong start;
            public ulong length;
            public ulong stack_protect;
            public ulong end { get { return start + length - 1; } }

            public bool intersects(Region other)
            {
                if (this.end < other.start)
                    return false;
                if (this.start > other.end)
                    return false;
                return true;
            }

            public bool contains(ulong addr)
            {
                if (addr < this.start)
                    return false;
                if (addr > this.end)
                    return false;
                return true;
            }

            public enum RegionType
            {
                Tysos,
                Heap,
                PageTables,
                Other,
                NonCanonical,
                Stack,
                SSE_state,
                CPU_specific,
                IPC,
                ModuleSection,
                Devices,
                Free
            }

            public int proc_affinity = 0;
            public string name;
            public RegionType type;

            public Region prev, next;
        }

        public Region list_start, list_end;

        public Region tysos, free, noncanonical, heap, pts, devs;

        void Add(Region r)
        {
            if(list_start == null)
            {
                list_start = list_end = r;
                r.next = r.prev = null;
            }
            else
            {
                r.prev = list_end;
                list_end.next = r;
                list_end = r;
                r.next = null;
            }
        }

        public ulong Alloc(ulong length, ulong align, string name)
        { return AllocRegion(length, align, name, 0, Region.RegionType.Other).start; }

        public Region AllocRegion(ulong length, ulong align, string name, ulong stack_protect, Region.RegionType r_type)
        { return AllocRegion(length, align, name, stack_protect, r_type, false); }

        public Region AllocRegion(ulong length, ulong align, string name, ulong stack_protect, Region.RegionType r_type,
            bool gc_data)
        {
            /* Allocate a new region from the 'free' region */

            lock (this)
            {
                ulong start = util.align(free.start, align);
                if ((start + length + stack_protect) > (free.start + free.length))
                {
                    System.Diagnostics.Debugger.Log(0, "VirtualRegions", "Out of memory allocating space for " + name +
                        ", start: " + start.ToString("X") + ", length: " + length.ToString() + ", stack_protect: " + stack_protect.ToString("X") +
                        ", free.start: " + free.start.ToString("X") + ", free.length: " + free.length.ToString("X"));
                    throw new OutOfMemoryException();
                }
                ulong old_free_start = free.start;
                free.start = start + length + stack_protect;
                free.length -= (free.start - old_free_start);

                Region ret = new Region();
                ret.type = r_type;
                ret.name = name;
                ret.start = start;
                ret.length = length + stack_protect;
                ret.stack_protect = stack_protect;

                Add(ret);

                if (gc_data)
                {
                    if (gc.gengc.heap == null)
                    {
                        Formatter.WriteLine("VirtualRegions: attempt to allocate gc_data region without valid gc", Program.arch.DebugOutput);
                    }
                    else
                    {
                        unsafe
                        {
                            gc.gengc.heap.AddRoots((byte*)(start + stack_protect), (byte*)(start + length + stack_protect));
                        }
                    }
                }

                Formatter.Write("VirtualRegions: Allocated new region: ", Program.arch.DebugOutput);
                Formatter.Write(name, Program.arch.DebugOutput);
                Formatter.Write(", start: ", Program.arch.DebugOutput);
                Formatter.Write(start, "X", Program.arch.DebugOutput);
                Formatter.Write(", length: ", Program.arch.DebugOutput);
                Formatter.Write(length, "X", Program.arch.DebugOutput);
                if (gc_data)
                    Formatter.Write(", gc_data", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);

                return ret;
            }
        }

        public void Dump(IDebugOutput o)
        {
            Formatter.WriteLine("Virtual regions:", o);

            Region cur_r = list_start;

            while (cur_r != null)
            {
                Formatter.Write(cur_r.name, o);
                Formatter.Write("  start: 0x", o);
                Formatter.Write(cur_r.start, "X", o);
                Formatter.Write("  end: 0x", o);
                Formatter.Write(cur_r.end, "X", o);
                Formatter.WriteLine(o);

                cur_r = cur_r.next;
            }
        }

        public Virtual_Regions(ulong tysos_base, ulong tysos_length)
        {
            // Set up the virtual region allocator

            /* Virtual memory looks like:
             * 
             * NullPage:        0x00000000 00000000 - 0x00000000 00000fff
             * Tysos:           tysos_base - (tysos_base + tysos_length - 1)
             * Free:            tysos_end - 0x00006fff 00000000
             * Devices:         0x00007000 00000000 - 0x00007fff ffffffff
             * NonCanonical     0x00008000 00000000 - 0xffff7fff ffffffff
             * Heap:            0xffff8000 00000000 - 0xffffff7f 00000000
             * PageTables:      0xffffff80 00000000 - 0xffffffff ffffffff
             *
             * 
             * Any new sections requested are taken off the start of free
             */

            list_start = list_end = null;

            if (tysos_base != 0)
            {
                Region null_page = new Region();
                null_page.type = Region.RegionType.NonCanonical;
                null_page.name = "NullPage";
                null_page.start = 0;
                null_page.length = 0x1000;
                if (tysos_base < null_page.length)
                    null_page.length = tysos_base;
                Add(null_page);
            }

            tysos = new Region();
            tysos.type = Region.RegionType.Tysos;
            tysos.name = "Tysos";
            tysos.start = tysos_base;
            tysos.length = tysos_length;
            Add(tysos);

            free = new Region();
            free.type = Region.RegionType.Free;
            free.name = "Free region";
            free.start = util.align(tysos.end, Program.arch.VirtMem.PageSize);
            free.length = 0x700000000000 - free.start;
            Add(free);

            devs = new Region();
            devs.type = Region.RegionType.Devices;
            devs.name = "Devices";
            devs.start = 0x700000000000;
            devs.length = 0x100000000000;
            Add(devs);

            noncanonical = new Region();
            noncanonical.type = Region.RegionType.NonCanonical;
            noncanonical.name = "NonCanonical";
            noncanonical.start = 0x0000800000000000;
            noncanonical.length = 0xffff000000000000;
            Add(noncanonical);

            heap = new Region();
            heap.type = Region.RegionType.Heap;
            heap.name = "Heap";
            heap.start = 0xffff800000000000;
            heap.length = 0x7f0000000000;
            Add(heap);

            pts = new Region();
            pts.type = Region.RegionType.PageTables;
            pts.name = "PageTables";
            pts.start = 0xffffff8000000000;
            pts.length = 0x8000000000;
            Add(pts);
        }
    }
}
