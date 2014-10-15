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
using System.Runtime.CompilerServices;

namespace tysos.x86_64
{
	public class x86_64_cpu : tysos.Cpu
	{
        [libsupcs.SpecialType]
        internal class data
        {
            public Thread cur_thread;
            public LApic cur_lapic;
            public Thread last_sse_thread;
            public Scheduler sched;

            [MethodImpl(MethodImplOptions.InternalCall)]
            [libsupcs.ReinterpretAsMethod]
            internal unsafe static extern data ReinterpretAsX86_64_data(ulong addr);
        }

        ulong data_ptr;
        ulong protected_heap, protected_heap_end;

        internal x86_64_cpu(Virtual_Regions.Region cpu_region, int id)
        { Init(cpu_region, id); }
        public x86_64_cpu() { }

        internal override void Init(Virtual_Regions.Region cpu_region, int id)
        {
            cpu_data = cpu_region;
            cpu_id = id;

            data_ptr = cpu_region.start;
            protected_heap = data_ptr + 0x100;
            protected_heap_end = cpu_region.end;

            data d = data.ReinterpretAsX86_64_data(data_ptr);
            d.cur_thread = null;
            d.cur_lapic = null;
            d.last_sse_thread = null;
            d.sched = null;
        }

        public override Thread CurrentThread { get { return data.ReinterpretAsX86_64_data(data_ptr).cur_thread; } }
        public LApic CurrentLApic { get { return data.ReinterpretAsX86_64_data(data_ptr).cur_lapic; } set { data.ReinterpretAsX86_64_data(data_ptr).cur_lapic = value; } }
        internal override Scheduler CurrentScheduler { get { return data.ReinterpretAsX86_64_data(data_ptr).sched; } set { data.ReinterpretAsX86_64_data(data_ptr).sched = value; } }
        public override int RequiredDataSize { get { return 0x2000; } }
        internal override Timer CurrentTimer { get { return data.ReinterpretAsX86_64_data(data_ptr).cur_lapic; } }
        internal override ulong IntPtrSize { get { return 8; } }

        internal override ulong CpuAlloc(ulong size)
        {
            ulong new_end = protected_heap + size;
            if (new_end >= protected_heap_end)
            {
                Formatter.WriteLine("*** FATAL ERROR ***", Program.arch.DebugOutput);
                Formatter.WriteLine("CPU heap exhausted", Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }
            ulong ret = protected_heap;
            protected_heap = new_end;
            return ret;
        }
	}
}
