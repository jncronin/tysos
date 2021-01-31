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
        const int max_allocs = 10000;
        const int min_allocs = 100;

        public static bool ScheduleCollection { get; set; } = false;

        /* Runs at high priority and ensures a collection occurs when
        allocs >= max_allocs */
        public static void MaxAllocCollectThreadProc()
        {
            while (heap == null) ;
            while (true)
            {
                tysos.Syscalls.SchedulerFunctions.Block(new DelegateEvent(
                    delegate () { return heap.allocs >= max_allocs; }));
                var state = libsupcs.OtherOperations.EnterUninterruptibleSection();
                System.Diagnostics.Debugger.Log(0, "gengc", "performing collection due to high number of allocations (" + heap.allocs.ToString() + ")");
                heap.DoCollection();
                System.Diagnostics.Debugger.Log(0, "gengc", "max_alloc collection done");
                libsupcs.OtherOperations.ExitUninterruptibleSection(state);
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
                var state = libsupcs.OtherOperations.EnterUninterruptibleSection();
                System.Diagnostics.Debugger.Log(0, "gengc", "performing background collection due to low number of allocations (" + heap.allocs.ToString() + ")");
                heap.DoCollection();
                System.Diagnostics.Debugger.Log(0, "gengc", "min_alloc collection done");
                libsupcs.OtherOperations.ExitUninterruptibleSection(state);
            }
        }

        /* Runs at high priority and runs a background collection when
         *  requested */
        public static void OnRequestCollectThreadProc()
        {
            while (heap == null) ;
            while (true)
            {
                tysos.Syscalls.SchedulerFunctions.Block(new DelegateEvent(
                    delegate () { return ScheduleCollection; }));
                var state = libsupcs.OtherOperations.EnterUninterruptibleSection();
                System.Diagnostics.Debugger.Log(0, "gengc", "performing background collection as requested");
                heap.DoCollection();
                ScheduleCollection = false;
                System.Diagnostics.Debugger.Log(0, "gengc", "requested collection done");
                libsupcs.OtherOperations.ExitUninterruptibleSection(state);
            }
        }
    }
}
