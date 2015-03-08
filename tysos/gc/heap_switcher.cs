/* Copyright (C) 2013 by John Cronin
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
using libsupcs;

/* Provides functions for switching between different heaps:
 * 
 * Current options are:
 * 
 * Startup:         A small heap used during system startup
 * BoehmGC:         The main Boehm GC heap
 * TysosGC:         The Tysos GC heap
 * PerCPU:          The per-CPU rescue heap
 * GenGC:           The new tysos generational GC
 */

namespace tysos.gc
{
    class gc
    {
        internal enum HeapType { Startup, BoehmGC, TysosGC, PerCPU, GenGC };
        internal static HeapType Heap;

        [AlwaysCompile]
        [MethodAlias("gcmalloc")]
        internal static ulong Alloc(ulong size)
        {
            ulong ret = 0;
            switch (Heap)
            {
                case HeapType.Startup:
                    ret = simple_heap.Alloc(size);
                    break;

                case HeapType.BoehmGC:
                    ret = boehm.Alloc(size);
                    //Formatter.Write("BoehmGC returned: ", Program.arch.DebugOutput);
                    //Formatter.Write(ret, "X", Program.arch.DebugOutput);
                    //Formatter.WriteLine(Program.arch.DebugOutput);
                    break;

                case HeapType.TysosGC:
                    ret = tysos_gc.Alloc(size);
                    break;

                case HeapType.PerCPU:
                    ret = Program.cur_cpu_data.CpuAlloc(size);
                    break;

                case HeapType.GenGC:
                    unsafe
                    {
                        ret = (ulong)gengc.heap.Alloc((int)size);
                    }
                    break;

                default:
                    throw new Exception("gc.Alloc(): heap type not set");
            }

            if (ret == 0)
                throw new Exception("gc.Alloc(): returned 0");

            unsafe
            {
                libsupcs.MemoryOperations.MemSet((void*)ret, 0, (int)size);
            }

            return ret;
        }

        internal static void RegisterObject(ulong addr)
        {
            switch (Heap)
            {
                case HeapType.TysosGC:
                    tysos_gc.cur_gc.RegisterObject(addr);
                    break;

                case HeapType.BoehmGC:
                    boehm.RegisterObject(addr);
                    break;
            }
        }

        internal static void ScheduleCollection()
        {
            switch (Heap)
            {
                case HeapType.TysosGC:
                    tysos_gc.ScheduleCollection();
                    break;

                case HeapType.BoehmGC:
                    boehm.ScheduleCollection();
                    break;
            }
        }

        internal static void DoCollection()
        {
            switch (Heap)
            {
                case HeapType.TysosGC:
                    tysos_gc.DoCollection();
                    break;

                case HeapType.BoehmGC:
                    boehm.DoCollection();
                    break;
            }
        }
    }
}
