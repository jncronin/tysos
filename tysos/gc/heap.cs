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

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.gc
{
    class heap
    {
        internal static heap_arena free_space;
        static object lock_obj;
        static bool is_initialized = false;

        abstract internal unsafe class heap_structure
        {
            internal unsafe struct tag
            {
                public tag* parent;
                public tag* left;
                public tag* right;
                public ulong size;
            }

            public abstract void Add(tag* node);
            public abstract void Remove(tag* node);
        }

        public static void Init(ulong small_cutoff, ulong small_start, ulong small_end, ulong long_start, ulong long_end)
        {
            // use placement new to initialize the structures (we currently have
            // no malloc function)

            ulong orig_start_addr = small_start;
            ulong orig_long_start = long_start;

            ulong init_blk_addr = small_start;
            if (init_blk_addr == 0)
                init_blk_addr = long_start;

            ulong free_addr = init_blk_addr;
            ulong free_len = (ulong)((libsupcs.TysosType)typeof(heap_arena)).GetClassSize();
            init_blk_addr += free_len;
            util.align(init_blk_addr, 8);
            free_space = (heap_arena)lib.Type.PlacementNew((libsupcs.TysosType)typeof(heap_arena), free_addr);

            ulong lock_addr = init_blk_addr;
            ulong lock_len = (ulong)((libsupcs.TysosType)typeof(object)).GetClassSize();
            init_blk_addr += lock_len;
            util.align(init_blk_addr, 8);
            lock_obj = (object)lib.Type.PlacementNew((libsupcs.TysosType)typeof(object), lock_addr);

            //size -= (start_addr - orig_start_addr);

            if (small_start != 0)
                small_start = init_blk_addr;
            else
                long_start = init_blk_addr;

            free_space.Init(small_cutoff, small_start, small_end, long_start, long_end);
            //free_space.Init(start_addr, size);
            is_initialized = true;
        }

        public static ulong Alloc(ulong size)
        {
            size = util.align(size, 8);
            ulong ret = free_space.Alloc(size, 0);

            if (ret == 0)
            {
                Formatter.WriteLine("heap: out of heap space", Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }

            return ret;
        }

        public static void Free(ulong addr)
        {
            free_space.Free(addr);
        }
    }
}
