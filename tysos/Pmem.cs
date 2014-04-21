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

#define MEM_DEBUG

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos
{
    partial class Pmem
    {
        /** A Bitmap class storing free pages as '1' and non-free as '0' */
        public class Bitmap
        {
            tysos.Collections.StaticULongArray bmp;

            ulong _base_addr;
            ulong _page_size;
            ulong _max_addr;

            ulong next_free_bmp_index;
            ulong free_count;

            ulong _bmp_entries_in_bytes;
            ulong _bmp_entries_in_qwords;

            public Bitmap(ulong bmp_addr, ulong bmp_maxlength, ulong base_addr, ulong page_size, ulong max_addr)
            {
                // Do some sanity checks
                if (base_addr > max_addr)
                    throw new ArgumentException("base_addr is greater than max_addr!");

                ulong mem_length = max_addr - base_addr;
                ulong pages = mem_length / page_size;
                pages = util.align(pages, 64);      // align to a multiple of 64 so that 8 byte accesses can be used
                if (pages > (bmp_maxlength * 8))
                    throw new ArgumentException("not enough storage space provided");

                if ((mem_length % page_size) != 0)
                    throw new ArgumentException("(max_addr - base_addr) is not a multiple of page_size");

                // Store the arguments
                _base_addr = base_addr;
                _page_size = page_size;
                _max_addr = max_addr;
                next_free_bmp_index = UInt64.MaxValue;
                free_count = 0;
                _bmp_entries_in_bytes = pages / 8;
                _bmp_entries_in_qwords = pages / 64;

                // Create the array and clear it
                bmp = new tysos.Collections.StaticULongArray(bmp_addr, _bmp_entries_in_qwords);
                bmp.Clear(0);
            }

            public void ReleasePage(ulong addr)
            {
                lock (this)
                {
                    _ReleasePage(addr);
                }
            }

            void _ReleasePage(ulong addr)
            {
                ulong page_index = get_page_index(addr);
                ulong bmp_index = page_index / 64;

                ulong bmp_entry = bmp[bmp_index];
                ulong set_bit = 1UL << (int)(page_index % 64);

                if ((bmp_entry & set_bit) == 0x0)
                {
                    bmp_entry |= set_bit;
                    bmp[bmp_index] = bmp_entry;

                    free_count++;
                    if (bmp_index < next_free_bmp_index)
                        next_free_bmp_index = bmp_index;
                }
            }

            public ulong GetPage(ulong addr)
            {
                lock (this)
                {
                    return _GetPage(addr);
                }
            }

            ulong _GetPage(ulong addr)
            {
                ulong page_index = get_page_index(addr);
                ulong bmp_index = page_index / 64;

                ulong bmp_entry = bmp[bmp_index];
                ulong set_bit = 1UL << (int)(page_index % 64);
                if (!((bmp_entry & set_bit) == set_bit))
                    return 0UL;

                bmp_entry &= ~set_bit;
                bmp[bmp_index] = bmp_entry;

                free_count--;
                return addr;
            }

            public ulong GetFreePage()
            {
                lock (this)
                {
                    return _GetFreePage();
                }
            }

            ulong _GetFreePage()
            {
                if (free_count == 0)
                    return 0UL;

                for (ulong i = next_free_bmp_index; i < _bmp_entries_in_qwords; i++)
                {
                    ulong bmp_entry = bmp[i];

                    if (bmp_entry != 0UL)
                    {
                        for (int bit = 0; bit < 64; bit++)
                        {
                            ulong set_bit = 1UL << bit;

                            if ((bmp_entry & set_bit) == set_bit)
                            {
                                // We have found a free page

                                bmp_entry &= ~set_bit;
                                bmp[i] = bmp_entry;

                                free_count--;
                                next_free_bmp_index = i;

                                ulong page_index = i * 64UL + (ulong)bit;
                                return _base_addr + page_index * _page_size;
                            }
                        }
                    }
                }

                return 0UL;
            }

            ulong get_page_index(ulong addr)
            {
                if (addr >= _max_addr)
                    throw new ArgumentOutOfRangeException("addr");
                if (addr < _base_addr)
                    throw new ArgumentOutOfRangeException("addr");

                ulong offset = addr - _base_addr;
                ulong page_id = offset / _page_size;

                return page_id;
            }

            ulong get_bmp_index(ulong addr)
            {
                // each array index stores 64 bits
                return get_page_index(addr) / 64;
            }

            ulong get_bit_index(ulong addr)
            {
                return get_page_index(addr) % 64;
            }

            public ulong GetFreeCount()
            {
                return free_count;
            }

            public void Dump(IDebugOutput o)
            {
                // Dump the array, one qword at a time
                // Each line represents 64 entries, or 256 kiB == 0x40000

                ulong line_size = 64 * _page_size;

                Formatter.WriteLine("Dump of physical memory bitmap", o);
                Formatter.Write("BaseAddr: 0x", o);
                Formatter.Write(_base_addr, "X", o);
                Formatter.Write("PageSize: 0x", o);
                Formatter.Write(_page_size, "X", o);
                Formatter.WriteLine(o);
                Formatter.WriteLine(o);

                for (ulong i = 0; i < _bmp_entries_in_qwords; i++)
                {
                    ulong addr = _base_addr + i * line_size;

                    Formatter.Write(addr, "X", o);
                    Formatter.Write(": ", o);
                    Formatter.Write(bmp[i], "b", o);
                    Formatter.WriteLine(o);
                }
            }
        }

        Bitmap bmp;
        Stack stack;
        Event stack_ready;

        internal ulong bmp_end;

        public Pmem(ulong bmp_base_addr, ulong bmp_length)
        {
            // Set up a basic physical memory allocator.

            // Initially, just use a bitmap for the first 128 Mib and ignore everything after that
            stack_ready = new Event();
            bmp = new Bitmap(bmp_base_addr, bmp_length, 0x0, 0x1000, 0x8000 * bmp_length);
            bmp_end = 0x8000 * bmp_length;
            stack = null;
        }

        public ulong BeginGetPage()
        {
            ulong ret = bmp.GetFreePage();
            if (ret == 0x0UL)
            {
                Formatter.WriteLine("pmem: allocating from stack", Program.arch.DebugOutput);

                if (!stack_ready.IsSet || (stack == null))
                {
                    if ((Program.cur_cpu_data.CurrentScheduler == null) || (Program.arch.Multitasking == false))
                    {
                        Formatter.WriteLine("Out of physical memory!", Program.arch.BootInfoOutput);
                        Formatter.WriteLine("Out of physical memory - initial 128 MiB used and stack not set up yet", Program.arch.DebugOutput);
                        Formatter.Write("Bitmap free pages: ", Program.arch.DebugOutput);
                        Formatter.Write(bmp.GetFreeCount(), Program.arch.DebugOutput);
                        Formatter.WriteLine(Program.arch.DebugOutput);

                        Formatter.Write("stab: sorted list size: ", Program.arch.DebugOutput);
                        Formatter.Write((ulong)Program.stab.offset_to_sym.Count, Program.arch.DebugOutput);
                        Formatter.Write("/", Program.arch.DebugOutput);
                        Formatter.Write((ulong)Program.stab.sym_to_offset.Count, Program.arch.DebugOutput);
                        Formatter.WriteLine(Program.arch.DebugOutput);

                        libsupcs.OtherOperations.Halt();
                    }

                    tysos.Syscalls.SchedulerFunctions.Block(stack_ready);
                }

                ret = stack.BeginPop();

                if (ret == 0x0UL)
                {
                    Program.arch.BootInfoOutput.Write("Out of physical memory");
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    Formatter.Write("Stack count: ", Program.arch.DebugOutput);
                    Formatter.Write(stack.Count, Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    bmp.Dump(Program.arch.DebugOutput);
                    libsupcs.OtherOperations.Halt();
                }
            }

            if ((GetFreeCount() < 1000) && ((GetFreeCount() % 100) == 0))
            {
                Formatter.Write("Warning, physical memory becoming exhausted: ", Program.arch.DebugOutput);
                Formatter.Write(GetFreeCount(), Program.arch.DebugOutput);
                Formatter.WriteLine(" pages left", Program.arch.DebugOutput);
#if MEM_DEBUG
                Formatter.WriteLine("Stack trace:", Program.arch.DebugOutput);
                Unwind.DumpUnwindInfo(Program.arch.GetUnwinder().Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
#endif
                Formatter.WriteLine("Enforcing collection", Program.arch.DebugOutput);
                gc.gc.ScheduleCollection();
            }

            return ret;
        }

        public void EndGetPage(ulong paddr, ulong vaddr)
        {
            if(paddr >= bmp_end)
                stack.EndPop(vaddr);
        }

        public void ReleasePage(ulong addr)
        {
            if (addr == 0x0)
                return;

            if (addr < bmp_end)
                bmp.ReleasePage(addr);
        }

        public void MarkUsed(ulong addr)
        {
            if (addr < bmp_end)
                bmp.GetPage(addr);
        }

        public ulong GetFreeCount()
        {
            return bmp.GetFreeCount();
        }

        public void Dump(IDebugOutput o)
        {
            Formatter.WriteLine("Dump of physical memory:", o);
            bmp.Dump(o);
        }

        internal class FreeRegion { public ulong start; public ulong length; }
        List<FreeRegion> frs;

        public void SetFreeRegions(List<FreeRegion> free_regions)
        {
            frs = free_regions;

            Formatter.WriteLine("pmem: High memory regions:", Program.arch.DebugOutput);
            foreach (FreeRegion fr in frs)
                Formatter.WriteLine("pmem:    start: " + fr.start.ToString("X16") + "  length: " + fr.length.ToString("X16"), Program.arch.DebugOutput);
        }

        internal void TaskFunction()
        {
            Formatter.WriteLine("pmem: in task function", Program.arch.DebugOutput);
            Formatter.WriteLine("pmem: free_region count: " + frs.Count.ToString(), Program.arch.DebugOutput);

            ulong temp_page_va = Program.arch.VirtualRegions.Alloc(0x1000, 0x1000, "pmem_temp_page");
            stack = new Stack(temp_page_va);

            foreach (FreeRegion fr in frs)
            {
                ulong start = util.align(fr.start, 0x1000);
                while ((start + 0x1000) <= (fr.start + fr.length))
                {
                    stack.Push(start);
                    start += 0x1000;
                }
            }

            stack_ready.Set();
            Formatter.WriteLine("pmem: stack created, " + stack.Count.ToString() + " * 4 kiB pages", Program.arch.DebugOutput);
        }
    }
}
