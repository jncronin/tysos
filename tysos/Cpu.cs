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
    public abstract class Cpu
    {
        public int cpu_id;
        internal Virtual_Regions.Region cpu_data;
        bool cpu_alloc = false;

        abstract internal void Init(Virtual_Regions.Region cpu_region, int id);
        abstract public Thread CurrentThread { get; }
        abstract internal Scheduler CurrentScheduler { get; set; }
        abstract public int RequiredDataSize { get; }
        abstract internal Timer CurrentTimer { get; }
        abstract internal ulong IntPtrSize { get; }
        virtual internal bool UseCpuAlloc { get { return cpu_alloc; } set { cpu_alloc = value; if (value) gc.gc.Heap = gc.gc.HeapType.PerCPU; } }
        abstract internal ulong CpuAlloc(ulong size);
    }
}
