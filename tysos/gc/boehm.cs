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

/* Interface to the Boehm GC */

#define BOEHM

using System;
using System.Collections.Generic;
using System.Text;
using libsupcs;

namespace tysos.gc
{
    class boehm
    {
        [MethodReferenceAlias("GC_init")]
        internal extern static void GC_init();

        [MethodReferenceAlias("GC_malloc")]
        internal extern static ulong Alloc(ulong size);

        [MethodReferenceAlias("GC_schedule_collection")]
        internal extern static void GC_gcollect();

        [MethodReferenceAlias("GC_add_roots")]
        internal extern static unsafe void GC_add_roots(void* a, void* b);

        static ulong initial_brk = 0;
        static ulong cur_brk = 0;
        static ulong max_brk = 0;

        [MethodReferenceAlias("initheap")]
        internal extern static void InitSbrk(ulong start, ulong end);

        internal static void InitHeap(ulong start, ulong end)
        {
            InitSbrk(start, end);

            Init();
        }

        internal static void Init()
        {
            /* TODO: add in current roots to the GC:
             * 
             * Current roots are:
             * 
             * Kernel data section
             * Current startup heap
             */
            GC_init();
        }

        /*[MethodAlias("sbrk")]
        [CallingConvention("gnu")]
        [AlwaysCompile]
        static ulong sbrk(long increment)
        {
            Formatter.Write("sbrk: called with increment ", Program.arch.DebugOutput);
            Formatter.Write((ulong)increment, "X", Program.arch.DebugOutput);
            Formatter.Write(", initial_brk: ", Program.arch.DebugOutput);
            Formatter.Write(initial_brk, "X", Program.arch.DebugOutput);
            Formatter.Write(", cur_brk: ", Program.arch.DebugOutput);
            Formatter.Write(cur_brk, "X", Program.arch.DebugOutput);
            Formatter.Write(", max_brk: ", Program.arch.DebugOutput);
            Formatter.Write(max_brk, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            if (increment == 0)
                return cur_brk;
            else if (increment > 0)
            {
                if ((max_brk - cur_brk) > (ulong)increment)
                {
                    ulong ret = cur_brk;
                    cur_brk += (ulong)increment;
                    return ret;
                }
                else
                    return 0xffffffffffffffff;
            }
            else
            {
                if ((cur_brk - initial_brk) > (ulong)(-increment))
                {
                    ulong ret = cur_brk;
                    cur_brk -= (ulong)(-increment);
                    return ret;
                }
                else
                    return 0xffffffffffffffff;
            }
        } */

        internal static void ScheduleCollection()
        {
            /* TODO: add a schedule task to the scheduler */
            DoCollection();
        }

        internal static void DoCollection()
        {
            GC_gcollect();
        }

        internal static void RegisterObject(ulong addr)
        {

        }

        [MethodAlias("abort")]
        static void Abort()
        {
            Formatter.WriteLine("abort() called", Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }
    }
}
