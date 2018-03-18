﻿/* Copyright (C) 2011 by John Cronin
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

namespace tysos
{
    public abstract class Arch
    {
        internal Pmem PhysMem;
        internal VirtMem VirtMem;
        internal TaskSwitcher Switcher;
        internal Timer SchedulerTimer;
        internal Virtual_Regions VirtualRegions;
        internal List<Cpu> Processors;

        internal ulong tysos_tls_length = 0x1000;      // hard-wired - someday get this from the ELF file

        internal virtual ulong GetBuffer(ulong len) { return 0; }

        public IDebugOutput DebugOutput;
        public IDebugOutput BootInfoOutput;

        internal Interrupts Interrupts;

        internal abstract TaskSwitchInfo CreateTaskSwitchInfo();

        internal abstract List<lib.File.Property> SystemProperties { get; }

        internal abstract void Init(UIntPtr chunk_vaddr, UIntPtr chunk_length, Multiboot.Header mboot);
        internal abstract void EnableMultitasking();
        internal abstract bool Multitasking { get; }

        internal abstract ulong PageSize { get; }
        internal abstract int PointerSize { get; }

        internal abstract ulong ExitAddress { get; }

        internal abstract Cpu CurrentCpu { get; }

        internal abstract libsupcs.Unwinder GetUnwinder();

        internal virtual bool InitGDBStub() { return false; }
        internal virtual void Breakpoint() { }

        internal abstract long GetNow();

        internal abstract string AssemblerArchitecture { get; }

        internal abstract ulong GetMonotonicCount { get; }
    }
}
