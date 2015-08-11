/* Copyright (C) 2015 by John Cronin
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

namespace tysos.gc
{
    unsafe partial class gengc
    {
        const int max_allocs = 1000;
        const int min_allocs = 100;

        /* Runs at high priority and ensures a collection occurs when
        allocs >= max_allocs */
        public static void MaxAllocCollectThreadProc()
        {
            while (heap == null) ;
            while (true)
            {
                tysos.Syscalls.SchedulerFunctions.Block(new DelegateEvent(
                    delegate () { return heap.allocs >= max_allocs; }));
                Formatter.Write("gengc: performing collection due to high number of allocations (", Program.arch.DebugOutput);
                Formatter.Write((ulong)heap.allocs, Program.arch.DebugOutput);
                Formatter.WriteLine(")", Program.arch.DebugOutput);
                heap.DoCollection();
            }
        }

        /* Runs at low priority and runs a background collection only when
        allocs >= min_allocs */
        public static void MinAllocCollectThreadProc()
        {
            while (heap == null) ;
            while (true)
            {
                tysos.Syscalls.SchedulerFunctions.Block(new DelegateEvent(
                    delegate () { return heap.allocs >= min_allocs; }));
                Formatter.Write("gengc: performing background collection (", Program.arch.DebugOutput);
                Formatter.Write((ulong)heap.allocs, Program.arch.DebugOutput);
                Formatter.WriteLine(" allocations)", Program.arch.DebugOutput);
                heap.DoCollection();
            }
        }
    }
}
