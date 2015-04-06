/* Copyright (C) 2011 by John Cronin
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

#define GC_STATS

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.gc
{
    class tysos_gc
    {
        internal static tysos_gc cur_gc = null;
        internal static Event run_gc = null;
        static WaitAnyEvent gc_wae = null;
        static TimerEvent gc_te = null;
        internal static bool debug_alloc = false;

#if GC_STATS
        internal static ulong small_frees = 0;
        internal static ulong large_frees = 0;
#endif

        // 5 seconds maximum between collections
        internal static long TickDelay = 50000000;

        internal static ulong Alloc(ulong size)
        {
            if (size == 0)
                return 0;

            if (debug_alloc)
            {
                Formatter.Write("gcmalloc: ", Program.arch.DebugOutput);
                Formatter.Write(size, "x", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
            }

            /* Else use the default allocator */
            ulong obj_addr = heap.Alloc(size);

            /* TODO: redefine gcmalloc to accept a typeinfo pointer as one of its arguments
             * This will require some rewriting of tysila */
            if (cur_gc != null)
                cur_gc.RegisterObject(obj_addr);

            return obj_addr;
        }

        internal static void Init()
        {
            cur_gc = new tysos_gc();
        }

        internal unsafe void RegisterObject(ulong obj_addr)
        {
            // TODO
        }

        internal static bool ScheduleCollection()
        {
            if (run_gc == null)
                DoCollection();
            else
                run_gc.Set();
            return true;
        }

        internal static void CollectionThread()
        {
            /* Currently the collection will run either when the timer elapses
             * or the run_gc event is signalled
             */

            while (true)
            {
                // Reset the events
                run_gc = new Event();
                gc_wae = new WaitAnyEvent();
                gc_te = new TimerEvent(TickDelay);

                gc_wae.Children.Add(run_gc);
                gc_wae.Children.Add(gc_te);

                // Wait on signals
                Syscalls.SchedulerFunctions.Block(gc_wae);

                // Run the collection
                DoCollection();
            }
        }

        internal unsafe static void DoCollection()
        {
            Formatter.WriteLine("GC: Beginning collection", Program.arch.DebugOutput);

            ulong cur_heap = heap.free_space.cur_arena;

#if GC_STATS
            small_frees = 0;
            large_frees = 0;
            heap_arena.dump_small_area_stats(cur_heap);
#endif

            // First, iterate through all the used blocks setting their referenced tags to 0
            ulong cur_blk = *(ulong*)(cur_heap + heap_arena.arena_in_use_list_start);
            heap_arena.blk_list_traversal(cur_heap, cur_blk, clear_referenced_flag);

            // Now iterate through all runs, setting their referenced tags to 0
            for (ulong i = 0; i < heap_arena.arena_run_size_count; i++)
            {
                ulong cur_run = *(ulong*)(cur_heap + heap_arena.arena_first_32 + i * 8);

                while (cur_run != 0)
                {
                    *(ulong*)(cur_run + heap_arena.run_gc_visited_bmp) = 0;
                    *(ulong*)(cur_run + heap_arena.run_gc_visited_bmp + 8) = 0;

                    cur_run = *(ulong*)(cur_run + heap_arena.run_next);
                }
            }

            /* Now iterate through all the roots of the current image (static variables and the stack),
             * setting the referenced flag as we go
             * 
             * The function we call to set the referenced flag for each object should:
             * 1) check whether the address is a valid heap address
             * 2) work out the greatest value less than or equal to the address in the used tree
             * 3) see if the address falls within the object identified above
             * 4) ensure the referenced flag of the current object is not already set
             * 5) if all the above tests are passed then set the referenced flag, then:
             * 6) iterate through all the members of the object (? on 4 byte boundaries up to the length
             *          of the object), and call ourselves with the value
             */

            // iterate all saved stacks
            for (int cpu_i = 0; cpu_i < Program.arch.Processors.Count; cpu_i++)
            {
                Cpu cpu = Program.arch.Processors[cpu_i];
                Scheduler s = cpu.CurrentScheduler;

                if (s != null)
                {
                    collect_thread_list(s.blocking_tasks);
                    collect_thread_list(s.sleeping_tasks);
                    for (int i = 0; i < s.running_tasks.Length; i++)
                        collect_thread_list(s.running_tasks[i]);
                }
            }

            // iterate the current stack - currently x86_64 specific
            ulong rsp = libsupcs.x86_64.Cpu.RSP;
            ulong e_address = Program.arch.ExitAddress;
            while (*(ulong*)rsp != e_address)
            {
                collect_object(*(ulong*)rsp);
                rsp += 8;
            }

            // iterate static variables
            if (Program.stab != null)
            {
                for (int sv_i = 0; sv_i < Program.stab.static_fields_addresses.Count; sv_i++)
                {
                    ulong sv_start = Program.stab.static_fields_addresses[sv_i];
                    ulong sv_length = Program.stab.static_fields_lengths[sv_i];

                    ulong obj_alignment = Program.cur_cpu_data.IntPtrSize;
                    for (ulong cur_offset = 0; cur_offset < sv_length; cur_offset += obj_alignment)
                        collect_object(*(ulong*)(sv_start + cur_offset));
                }
            }

            // Now iterate through the used blocks again, freeing those who do not have the referenced flag set
            cur_blk = *(ulong*)(cur_heap + heap_arena.arena_in_use_list_start);
            heap_arena.blk_list_traversal(cur_heap, cur_blk, free_if_not_referenced);

            // Now iterate through all small regions again, freeing those which are not referenced
            for (ulong i = 0; i < heap_arena.arena_run_size_count; i++)
            {
                ulong cur_run = *(ulong*)(cur_heap + heap_arena.arena_first_32 + i * 8);

                while (cur_run != 0)
                {
#if GC_STATS
                    ulong alloc_count = (ulong)bit_count(*(ulong*)(cur_run + heap_arena.run_free_bmp));
                    alloc_count += (ulong)bit_count(*(ulong*)(cur_run + heap_arena.run_free_bmp + 8));
#endif
                    *(ulong*)(cur_run + heap_arena.run_free_bmp) &= *(ulong*)(cur_run + heap_arena.run_gc_visited_bmp);
                    *(ulong*)(cur_run + heap_arena.run_free_bmp + 8) &= *(ulong*)(cur_run + heap_arena.run_gc_visited_bmp + 8);

                    /* Calculate the run_free_count and run_first_free_idx values */
                    ulong free_bmp_1 = *(ulong*)(cur_run + heap_arena.run_free_bmp);
                    ulong free_bmp_2 = *(ulong*)(cur_run + heap_arena.run_free_bmp + 8);
                    byte run_free_count = 0;
                    byte run_first_free_idx = 0;
                    for (int j = 0; j < 64; j++)
                    {
                        ulong val = (free_bmp_1 >> j) & 0x1;
                        if (val == 1)
                        {
                            run_free_count++;
                            if (run_first_free_idx == 0)
                                run_first_free_idx = (byte)j;
                        }
                    }
                    for (int j = 0; j < 64; j++)
                    {
                        ulong val = (free_bmp_2 >> j) & 0x1;
                        if (val == 1)
                        {
                            run_free_count++;
                            if (run_first_free_idx == 0)
                                run_first_free_idx = (byte)(j + 64);
                        }
                    }
                    *(byte*)(cur_run + heap_arena.run_first_free_idx) = run_first_free_idx;
                    *(byte*)(cur_run + heap_arena.run_free_count) = run_free_count;                      

#if GC_STATS
                    ulong post_alloc_count = (ulong)bit_count(*(ulong*)(cur_run + heap_arena.run_free_bmp));
                    post_alloc_count += (ulong)bit_count(*(ulong*)(cur_run + heap_arena.run_free_bmp + 8));

                    small_frees += (alloc_count - post_alloc_count);
#endif

                    cur_run = *(ulong*)(cur_run + heap_arena.run_next);

                }
            }

            Formatter.WriteLine("GC: End of collection", Program.arch.DebugOutput);
#if GC_STATS
            Formatter.WriteLine("GC: Collected " + small_frees.ToString() + " small blocks and " + large_frees.ToString() + " large blocks", Program.arch.DebugOutput);
            heap_arena.dump_small_area_stats(cur_heap);
#endif
        }

#if GC_STATS
        static int bit_count(ulong v)
        {
            int ret = 0;
            for (int i = 0; i < 64; i++)
            {
                if ((v & 0x1) == 0x1)
                    ret++;
                v >>= 1;
            }
            return ret;
        }
#endif

        private static void collect_thread_list(Collections.LinkedList<Thread> thread_list)
        {
            Collections.LinkedList<Thread>.Node n = thread_list.f;

            while (n != null)
            {
                collect_thread(n.item);
                n = n.next;
            }
        }

        private static void collect_thread(Thread thread)
        {
            bool grows_downwards = thread.saved_state.StackGrowsDownwards();
            ulong cur_sp = thread.saved_state.GetSavedStackPointer();
            ulong max_sp = thread.saved_state.GetMaximumStack();
            ulong stack_size = thread.saved_state.GetStackItemSize();

            if (grows_downwards)
            {
                while (cur_sp < max_sp)
                {
                    collect_object(cur_sp);
                    cur_sp += stack_size;
                }
            }
            else
            {
                cur_sp -= stack_size;
                while (cur_sp >= max_sp)
                {
                    collect_object(cur_sp);
                    cur_sp -= stack_size;
                }
            }
        }

        private unsafe static void collect_object(ulong cur_obj)
        {
            // Collect an object

            // 1) Is the object a valid heap address?
            ulong containing_heap = heap.free_space.cur_arena;
            bool is_small_object = false;
            if ((cur_obj >= *(ulong*)(containing_heap + heap_arena.arena_small_start)) &&
                (cur_obj < *(ulong*)(containing_heap + heap_arena.arena_small_end)))
            {
                is_small_object = true;
            }
            else if ((cur_obj >= *(ulong*)(containing_heap + heap_arena.arena_large_start)) &&
                (cur_obj < *(ulong*)(containing_heap + heap_arena.arena_large_end)))
            {
                is_small_object = false;
            }
            else
                return;

            if (is_small_object)
            {
                // For small objects, find the run that contains the object, ensuring it is defined
                ulong cur_run = cur_obj & ~0xfffUL;
                if (cur_run >= *(ulong*)(containing_heap + heap_arena.arena_next_free_small))
                    return;

                ulong run_size = *(ulong*)(cur_run + heap_arena.run_size);
                ulong run_idx = (cur_obj - cur_run) / run_size;

                // See if the object is defined
                ulong act_byte = run_idx / 8;
                int act_bit = (int)(run_idx % 8);
                ulong def_byte = *(ulong*)(cur_run + heap_arena.run_free_bmp + act_byte);
                ulong def_bit = (def_byte >> act_bit) & 1;

                if (def_bit == 1)
                    return;

                // See if it is already visited
                ulong visited_byte = *(ulong*)(cur_run + heap_arena.run_gc_visited_bmp + act_byte);
                ulong visited_bit = (visited_byte >> act_bit) & 1;

                if (visited_bit == 1)
                    return;

                // Set its visited bit
                visited_byte &= ~(1UL << act_bit);
                *(ulong*)(cur_run + heap_arena.run_gc_visited_bmp + act_byte) = visited_byte;

                // Visit each of its members
                ulong obj_start = cur_run + run_idx * run_size;
                for (ulong i = 0; i < run_size; i += 4)
                    collect_object(obj_start + i);
            }
            else
            {
                // 2) Work out the greatest value less than or equal to the current value
                ulong found_obj = heap.free_space.blk_get_less_equal(containing_heap, *(ulong*)(containing_heap + heap_arena.arena_blk_used_root), cur_obj,
                    heap_arena.BLK_SORT_INDEX_ADDR_OF & heap_arena.blk_header_length);

                // 3) See if the object falls within this object
                ulong obj_start = found_obj + heap_arena.blk_header_length;
                ulong obj_length = *(ulong*)(found_obj + heap_arena.blk_length);
                if (cur_obj < obj_start)
                    return;
                if (cur_obj >= (obj_start + obj_length))
                    return;

                // 4) Ensure the referenced flag is not already set
                ulong blk_flags = *(ulong*)(found_obj + heap_arena.blk_gc_flags);
                if ((blk_flags & heap_arena.BLK_GC_FLAGS_GC_REFERENCED) == heap_arena.BLK_GC_FLAGS_GC_REFERENCED)
                    return;

                // 5) Set the referenced flag
                blk_flags &= heap_arena.BLK_GC_FLAGS_GC_REFERENCED;
                *(ulong*)(found_obj + heap_arena.blk_gc_flags) = blk_flags;

                // 6) Try to collect all the members of this object
                ulong obj_alignment = 4;
                if ((blk_flags & heap_arena.BLK_GC_FLAGS_INTPTR_ARRAY) == heap_arena.BLK_GC_FLAGS_INTPTR_ARRAY)
                    obj_alignment = Program.cur_cpu_data.IntPtrSize;
                for (ulong cur_offset = 0; cur_offset < obj_length; cur_offset += obj_alignment)
                    collect_object(*(ulong*)(obj_start + cur_offset));
            }
        }

        static unsafe void clear_referenced_flag(ulong blk)
        {
            ulong flags = *(ulong *)(blk + heap_arena.blk_gc_flags);
            flags &= ~heap_arena.BLK_GC_FLAGS_GC_REFERENCED;
            *(ulong*)(blk + heap_arena.blk_gc_flags) = flags;
        }

        static unsafe void free_if_not_referenced(ulong blk)
        {
            ulong flags = *(ulong*)(blk + heap_arena.blk_gc_flags);
            if ((flags & heap_arena.BLK_GC_FLAGS_GC_REFERENCED) == 0)
            {
                // call free
                heap.Free(blk + heap_arena.blk_header_length);

#if GC_STATS
                large_frees++;
#endif

                //Formatter.Write("Freeing unused heap location: 0x", Program.arch.DebugOutput);
                //Formatter.Write(blk, "X", Program.arch.DebugOutput);
                //Formatter.WriteLine(Program.arch.DebugOutput);
            }
        }
    }
}
