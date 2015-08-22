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

/* In a multi-processor system, there will be certain data which is specific to a
 * certain CPU.
 * 
 * That is represented here
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos
{
    public abstract unsafe class Cpu
    {
        protected int cpu_id;
        bool cpu_alloc = false;

        virtual internal void InitCurrentCpu() { }

        protected Thread currentThread = null;
        protected Scheduler currentScheduler = null;
        protected Timer currentTimer = null;

        protected byte* cpu_alloc_current;
        protected byte* cpu_alloc_max;

        protected List<Resources.InterruptLine> interrupts = new List<Resources.InterruptLine>();
        virtual public ICollection<Resources.InterruptLine> Interrupts { get { return interrupts; } }

        virtual public Thread CurrentThread { get { return currentThread; } }
        virtual public Process CurrentProcess
        {
            get
            {
                if (CurrentThread == null)
                    return null;
                return CurrentThread.owning_process;
            }
        }

        virtual internal Scheduler CurrentScheduler
        {
            get { return currentScheduler; }
            set { currentScheduler = value; }
        }

        virtual internal Timer CurrentTimer
        {
            get { return currentTimer; }
            set { currentTimer = value; }
        }

        virtual internal ulong IntPtrSize
        {
            get { return (ulong)libsupcs.OtherOperations.GetPointerSize(); }
        }

        virtual internal int Id
        {
            get { return cpu_id; }
        }

        virtual internal bool UseCpuAlloc { get { return cpu_alloc; } set { cpu_alloc = value; if (value) gc.gc.Heap = gc.gc.HeapType.PerCPU; } }
        virtual internal ulong CpuAlloc(ulong size)
        {
            if (cpu_alloc_max - cpu_alloc_current < (long)size)
            {
                Formatter.WriteLine("*** FATAL ERROR ***", Program.arch.DebugOutput);
                Formatter.WriteLine("CPU heap exhausted", Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
                return 0;
            }

            byte* ret = cpu_alloc_current;
            cpu_alloc_current += size;
            return (ulong)ret;
        }
    }
}
