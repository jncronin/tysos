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

namespace tysos.lib
{
    class GC
    {
#if false
        [libsupcs.MethodAlias("_ZW6System2GC_16SuppressFinalize_Rv_P1u1O")]
        [libsupcs.AlwaysCompile]
        static void SuppressFinalize(object o)
        {
        }

        [libsupcs.MethodAlias("_ZW6System2GC_17get_MaxGeneration_Ri_P0")]
        [libsupcs.AlwaysCompile]
        static int get_MaxGeneration()
        {
            return 0;
        }
#endif

        [libsupcs.MethodAlias("_ZW6System2GC_15InternalCollect_Rv_P1i")]
        [libsupcs.AlwaysCompile]
        static void InternalCollect(int generation)
        {
            /* NB this is not compliant with the MS spec which requires an
             * immediate garbage collection.  Instead, we signal the collector
             * to run on the next task switch, which we then cause.
             * 
             * As the garbage collector thread is maximum priority it should
             * run next, however the situation could arise where another top
             * priority thread runs instead, in which case the collection
             * is _almost_ immediately
             */

#if NO_BOEHM
            gc.gc.ScheduleCollection();
            Syscalls.SchedulerFunctions.Yield();
#endif // NO_BOEHM
        }
    }
}
