/* Copyright (C) 2012 by John Cronin
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

namespace tysos.gc
{
    partial class heap_arena
    {
        internal const ulong run_size = 0;
        internal const ulong run_next = 8;
        internal const ulong run_prev = 16;
        internal const ulong run_free_bmp = 24;
        internal const ulong run_gc_visited_bmp = 40;
        internal const ulong run_first_free_idx = 56;
        internal const ulong run_free_count = 57;
        internal const ulong run_header_length = 64;

        private unsafe ulong do_small_alloc(ulong cur_arena, ulong size)
        {
            // first decide on the size of the block to allocate
            /* block size       entry_no
             * 32               0
             * 64               1
             * 128              2
             * 256              3
             * 512              4
             */

            ulong blk_size = 512;
            ulong entry_no = 4;

            if (size <= 32)
            {
                blk_size = 32;
                entry_no = 0;
            }
            else if (size <= 64)
            {
                blk_size = 64;
                entry_no = 1;
            }
            else if (size <= 128)
            {
                blk_size = 128;
                entry_no = 2;
            }
            else if (size <= 256)
            {
                blk_size = 256;
                entry_no = 3;
            }
            
            // get the next free run of the requested size
            ulong run_addr = get_next_free_run(cur_arena, blk_size, entry_no);

            /*Formatter.Write("do_small_alloc: run_free_count: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)*(byte*)(run_addr + run_free_count), Program.arch.DebugOutput);
            Formatter.Write("  bitmap qword 1: ", Program.arch.DebugOutput);
            Formatter.Write(*(ulong*)(run_addr + run_free_bmp), "X", Program.arch.DebugOutput);
            Formatter.Write("  bitmap qword 2: ", Program.arch.DebugOutput);
            Formatter.Write(*(ulong*)(run_addr + run_free_bmp + 8), "X", Program.arch.DebugOutput);
            Formatter.Write("  blk_size: ", Program.arch.DebugOutput);
            Formatter.Write(blk_size, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);*/

            // now iterate through the free bitmap looking for the first free entry of the relevant size
            for (int i = (int)*(byte*)(run_addr + run_first_free_idx); i <= 128; i++)
            {
                ulong act_byte_no = (ulong)(i / 8);
                int act_bit = i % 8;

                ulong act_byte = *(ulong*)(run_addr + run_free_bmp + act_byte_no);
                ulong test_bit = (act_byte >> act_bit) & 1;

                if (test_bit == 1)
                {
                    // we have found a free bit - now clear it
                    ulong set_bit = ~(1UL << act_bit);
                    act_byte &= set_bit;
                    *(ulong*)(run_addr + run_free_bmp + act_byte_no) = act_byte;

                    // set the next free index to this + 1
                    *(byte*)(run_addr + run_first_free_idx) = (byte)(i + 1);

                    // reduce the free count
                    *(byte*)(run_addr + run_free_count) -= 1;

                    // return the address of the area pointed to by this bit
                    ulong ret = run_addr + blk_size * (ulong)i;
                    /*if(debug)
                    {
                        Formatter.Write("do_small_alloc: returning: ", Program.arch.DebugOutput);
                        Formatter.Write(ret, "X", Program.arch.DebugOutput);
                        Formatter.WriteLine(Program.arch.DebugOutput);
                    }*/

                    return run_addr + blk_size * (ulong)i; 
                }
            }

            // we shouldn't get here
            Formatter.WriteLine("do_small_alloc: reached end without finding free space - this should not happen",
                Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
            throw new Exception("do_small_alloc reached end without finding free space, this shouldn't happen");
        }

        private unsafe ulong get_next_free_run(ulong cur_arena, ulong blk_size, ulong entry_no)
        {
            // return the address of the next run of a certain size with free entries
            ulong next_free = *(ulong*)(cur_arena + arena_next_free_32 + entry_no * 8);

            if (next_free == 0)
                return allocate_new_free_run(cur_arena, blk_size, entry_no);

            // Search the list of runs to see if any of them have free space and bring
            //  them to be the next_free pointer (if they are not already) and return
            ulong cur_run = next_free;
            while ((*(ulong*)(cur_run + run_free_bmp) == 0) && (*(ulong*)(cur_run + run_free_bmp + 8) == 0))
            {
                cur_run = *(ulong*)(cur_run + run_next);
                if (cur_run == 0)
                {
                    cur_run = allocate_new_free_run(cur_arena, blk_size, entry_no);
                    break;
                }
            }
            if (cur_run != next_free)
                *(ulong*)(cur_arena + arena_next_free_32 + entry_no * 8) = cur_run;

            return cur_run;
        }

        private unsafe ulong allocate_new_free_run(ulong cur_arena, ulong blk_size, ulong entry_no)
        {
            // allocate a new run of a certain size and add it to the list of runs

            // allocate the run
            ulong next_run = *(ulong*)(cur_arena + arena_next_free_small);
            if (next_run >= *(ulong*)(cur_arena + arena_small_end))
            {
                // This is a critical error - all we can do is halt
                Formatter.WriteLine("*** Critical Error ***", Program.arch.BootInfoOutput);
                Formatter.WriteLine("*** Critical Error ***", Program.arch.DebugOutput);
                Formatter.WriteLine("Out of small memory runs", Program.arch.DebugOutput);
                Formatter.Write("cur_arena: ", Program.arch.DebugOutput);
                Formatter.Write(cur_arena, "X", Program.arch.DebugOutput);
                Formatter.Write("  small_start: ", Program.arch.DebugOutput);
                Formatter.Write(*(ulong*)(cur_arena + arena_small_start), "X", Program.arch.DebugOutput);
                Formatter.Write("  small_end: ", Program.arch.DebugOutput);
                Formatter.Write(*(ulong*)(cur_arena + arena_small_end), "X", Program.arch.DebugOutput);
                Formatter.Write("  small_cutoff: ", Program.arch.DebugOutput);
                Formatter.Write(*(ulong*)(cur_arena + arena_small_cutoff), Program.arch.DebugOutput);
                Formatter.Write("  next_free_small: ", Program.arch.DebugOutput);
                Formatter.Write(*(ulong*)(cur_arena + arena_next_free_small), "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);

                Formatter.WriteLine("Stack trace: ", Program.arch.DebugOutput);
                libsupcs.Unwinder u2 = Program.arch.GetUnwinder();
                u2.UnwindOne();
                while (u2.GetInstructionPointer() != (UIntPtr)Program.arch.ExitAddress)
                {
                    Formatter.Write((ulong)u2.GetInstructionPointer(), "X", Program.arch.DebugOutput);
                    Formatter.Write(": ", Program.arch.DebugOutput);
                    if (u2.GetMethodInfo() != null)
                    {
                        Formatter.Write(u2.GetMethodInfo().DeclaringType.Namespace, Program.arch.DebugOutput);
                        Formatter.Write(".", Program.arch.DebugOutput);
                        Formatter.Write(u2.GetMethodInfo().DeclaringType.Name, Program.arch.DebugOutput);
                        Formatter.Write(".", Program.arch.DebugOutput);
                        Formatter.Write(u2.GetMethodInfo().Name, Program.arch.DebugOutput);
                        Formatter.WriteLine("()", Program.arch.DebugOutput);
                    }
                    else
                        Formatter.WriteLine("unknown method", Program.arch.DebugOutput);

                    u2.UnwindOne();
                }

                libsupcs.OtherOperations.Halt();
            }

            *(ulong*)(cur_arena + arena_next_free_small) += 4096;

            // initialize it
            *(ulong*)(next_run + run_size) = blk_size;

            switch (blk_size)
            {
                case 32:
                    *(ulong*)(next_run + run_free_bmp) = 0xFFFFFFFFFFFFFFFC;
                    *(ulong*)(next_run + run_free_bmp + 8) = 0xFFFFFFFFFFFFFFFF;
                    *(byte*)(next_run + run_free_count) = 126;
                    *(byte*)(next_run + run_first_free_idx) = 2;
                    break;

                case 64:
                    *(ulong*)(next_run + run_free_bmp) = 0xFFFFFFFFFFFFFFFE;
                    *(ulong*)(next_run + run_free_bmp + 8) = 0;
                    *(byte*)(next_run + run_free_count) = 63;
                    *(byte*)(next_run + run_first_free_idx) = 1;
                    break;

                case 128:
                    *(ulong*)(next_run + run_free_bmp) = 0xFFFFFFFE;
                    *(ulong*)(next_run + run_free_bmp + 8) = 0;
                    *(byte*)(next_run + run_free_count) = 31;
                    *(byte*)(next_run + run_first_free_idx) = 1;
                    break;

                case 256:
                    *(ulong*)(next_run + run_free_bmp) = 0xFFFE;
                    *(ulong*)(next_run + run_free_bmp + 8) = 0;
                    *(byte*)(next_run + run_free_count) = 15;
                    *(byte*)(next_run + run_first_free_idx) = 1;
                    break;

                case 512:
                    *(ulong*)(next_run + run_free_bmp) = 0xFE;
                    *(ulong*)(next_run + run_free_bmp + 8) = 0;
                    *(byte*)(next_run + run_free_count) = 7;
                    *(byte*)(next_run + run_first_free_idx) = 1;
                    break;
            }

            for (ulong i = 0; i < 16; i += 8)
                *(ulong*)(next_run + run_gc_visited_bmp + i) = 0;

            // add it to the start of the linked list of runs
            *(ulong*)(next_run + run_prev) = 0;
            ulong first_run_in_list = *(ulong*)(cur_arena + arena_first_32 + entry_no * 8);
            *(ulong*)(next_run + run_next) = first_run_in_list;

            if (first_run_in_list != 0)
                *(ulong*)(first_run_in_list + run_prev) = next_run;

            *(ulong*)(cur_arena + arena_first_32 + entry_no * 8) = next_run;

            // set it as the next free run
            *(ulong*)(cur_arena + arena_next_free_32 + entry_no * 8) = next_run;

            return next_run;
        }

        internal static int count_bits(ulong v)
        {
            int ret = 0;
            for (int i = 0; i < 64; i++)
            {
                if (((v >> i) & 0x1) == 0x1)
                    ret++;
            }
            return ret;
        }

        internal unsafe static void dump_small_area_stats(ulong cur_arena)
        {
            Formatter.WriteLine("HEAP: Small area stats:", Program.arch.DebugOutput);

            for (ulong i = 0; i < 5; i++)
            {
                int area_size = 32;
                switch (i)
                {
                    case 0:
                        area_size = 32;
                        break;
                    case 1:
                        area_size = 64;
                        break;
                    case 2:
                        area_size = 128;
                        break;
                    case 3:
                        area_size = 256;
                        break;
                    case 4:
                        area_size = 512;
                        break;
                }

                ulong cur_run = *(ulong*)(cur_arena + arena_next_free_32 + i * 8);
                int used_runs = 0;
                int free_areas = 0;
                while (cur_run != 0)
                {
                    used_runs++;
                    ulong bmp_1 = *(ulong*)(cur_run + run_free_bmp);
                    ulong bmp_2 = *(ulong*)(cur_run + run_free_bmp + 8);
                    free_areas += count_bits(bmp_1);
                    free_areas += count_bits(bmp_2);
                    cur_run = *(ulong*)(cur_run + run_next);
                }

                // Runs are 4 kiB
                int total_areas = used_runs * 4096 / area_size;
                int used_areas = total_areas - free_areas;

                Formatter.WriteLine("HEAP: Small run area size " + area_size.ToString() + "B: " + total_areas.ToString() +
                    " total areas, " + used_areas.ToString() + " used, " + free_areas.ToString() + " free.",
                    Program.arch.DebugOutput);
            }
        }
    }
}
