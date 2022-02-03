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
        ulong gs;

        internal unsafe x86_64_cpu(Virtual_Regions.Region cpu_region)
        {
            /* We divide the cpu region into a pointer at offset 0 which
            points to the actual Cpu class ('this' pointer) and a cpu-specific
            heap following it.

            We then set the GS pointer to the start of the cpu region, thus
            gs:0x0 is a pointer to the current Cpu instance for the current
            processor.  Setting of cur cpu's gs is done in InitCurrentCpu()
            which is called on the BSP and every AP (and also sets up other
            stuff like Apic address) */

            gs = cpu_region.start;
            *(void**)cpu_region.start = libsupcs.CastOperations.ReinterpretAsPointer(this);
            this.cpu_alloc_current = (byte*)cpu_region.start + 8;
            this.cpu_alloc_max = (byte*)cpu_region.end;
        }

        internal unsafe override void InitCurrentCpu()
        {
            /* First, get the APIC id of the current processor - we have to
            map in the LAPIC registers first */
            ulong apic_base = libsupcs.x86_64.Cpu.RdMsr(LApic.IA32_APIC_BASE_MSR) & LApic.IA32_APIC_BASE_ADDR_MASK;

            LApicAddress = Program.arch.VirtualRegions.Alloc(0x1000, 0x1000,
                "LAPIC");
            Program.arch.VirtMem.Map(apic_base, 0x1000, LApicAddress, VirtMem.FLAG_writeable);

            uint apic_id = *(uint*)(LApicAddress + LApic.LAPIC_ID_offset) >> 24;
            cpu_id = (int)apic_id;
            Formatter.Write("x86_64_cpu: cpu id ", Program.arch.DebugOutput);
            Formatter.Write((ulong)cpu_id, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Next, set GS base to be the start of our cpu_region */
            // IA32_GS_BASE is 0xC0000101
            libsupcs.x86_64.Cpu.WrMsr(0xc0000101, gs);

            /* Expose the available interrupt lines */
            for(int i = 32; i < 255; i++)
            {
                // Vectors 0x40 is used for the LAPIC timer, 0x60 for spurious vector
                if (i == 0x40 || i == 0x60)
                    continue;

                x86_64_Interrupt interrupt = new x86_64_Interrupt();
                interrupt.cpu = this;
                interrupt.cpu_int_no = i;
                interrupts.Add(interrupt);
            }
        }

        internal ulong LApicAddress;
        internal LApic cur_lapic = null;

        public LApic CurrentLApic { get { return cur_lapic; } set { cur_lapic = value; } }

        public Dictionary<int, ulong> interrupt_handlers = new Dictionary<int, ulong>(new Program.MyGenericEqualityComparer<int>());
	}
}
