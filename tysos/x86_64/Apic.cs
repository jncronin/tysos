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

namespace tysos.x86_64
{
    public class LApic : tysos.Timer
    {
        internal const uint IA32_APIC_BASE_MSR = 0x1b;

        internal const ulong IA32_APIC_BASE_ADDR_MASK = 0xFFFFFF000;

        /* The following are offsets to the registers within the LAPIC address space
         * 
         * Intel 3A:10.4.1, all accesses to the registers are via 128-bit aligned 32-bit read/writes
         * (the 128 bit and 256 bit registers are not contiguous - they are composed of multiple 32 bit registers
         * aligned on 128 bit boundaries (i.e. 32 bits for first dword, then 96 bits padding, then 32 bits for
         * next dword and so on).
         */

        internal const ulong LAPIC_ID_offset = 0x20;
        const ulong LAPIC_version_offset = 0x30;

        /** <summary>Task priority register</summary> */
        const ulong TPR_offset = 0x80;
        /** <summary>Arbritration priority register</summary> */
        const ulong APR_offset = 0x90;
        /** <summary>Processor priority register</summary> */
        const ulong PPR_offset = 0xa0;
        /** <summary>End of interrupt register</summary> */
        const ulong EOI_offset = 0xb0;
        /** <summary>Remote read register</summary> */
        const ulong RRD_offset = 0xc0;
        /** <summary>Logical destination register</summary> */
        const ulong LDR_offset = 0xd0;
        /** <summary>Destination format register</summary> */
        const ulong DFR_offset = 0xe0;
        /** <summary>Spurious interrupt vector register</summary> */
        const ulong Spurious_Interrupt_Vector_reg_offset = 0xf0;
        /** <summary>In-service register bits 0-31</summary> */
        const ulong ISR_dw0 = 0x100;
        /** <summary>In-service register bits 32-63</summary> */
        const ulong ISR_dw1 = 0x110;
        /** <summary>In-service register bits 64-95</summary> */
        const ulong ISR_dw2 = 0x120;
        /** <summary>In-service register bits 96-127</summary> */
        const ulong ISR_dw3 = 0x130;
        /** <summary>In-service register bits 128-159</summary> */
        const ulong ISR_dw4 = 0x140;
        /** <summary>In-service register bits 160-191</summary> */
        const ulong ISR_dw5 = 0x150;
        /** <summary>In-service register bits 192-223</summary> */
        const ulong ISR_dw6 = 0x160;
        /** <summary>In-service register bits 224-255</summary> */
        const ulong ISR_dw7 = 0x170;
        /** <summary>Trigger mode register bits 0-31</summary> */
        const ulong TMR_dw0 = 0x180;
        /** <summary>Trigger mode register bits 32-63</summary> */
        const ulong TMR_dw1 = 0x190;
        /** <summary>Trigger mode register bits 64-95</summary> */
        const ulong TMR_dw2 = 0x1a0;
        /** <summary>Trigger mode register bits 96-127</summary> */
        const ulong TMR_dw3 = 0x1b0;
        /** <summary>Trigger mode register bits 128-159</summary> */
        const ulong TMR_dw4 = 0x1c0;
        /** <summary>Trigger mode register bits 160-191</summary> */
        const ulong TMR_dw5 = 0x1d0;
        /** <summary>Trigger mode register bits 192-223</summary> */
        const ulong TMR_dw6 = 0x1e0;
        /** <summary>Trigger mode register bits 224-255</summary> */
        const ulong TMR_dw7 = 0x1f0;
        /** <summary>Interrupt request register bits 0-31</summary> */
        const ulong IRR_dw0 = 0x200;
        /** <summary>Interrupt request register bits 32-63</summary> */
        const ulong IRR_dw1 = 0x210;
        /** <summary>Interrupt request register bits 64-95</summary> */
        const ulong IRR_dw2 = 0x220;
        /** <summary>Interrupt request register bits 96-127</summary> */
        const ulong IRR_dw3 = 0x230;
        /** <summary>Interrupt request register bits 128-159</summary> */
        const ulong IRR_dw4 = 0x240;
        /** <summary>Interrupt request register bits 160-191</summary> */
        const ulong IRR_dw5 = 0x250;
        /** <summary>Interrupt request register bits 192-223</summary> */
        const ulong IRR_dw6 = 0x260;
        /** <summary>Interrupt request register bits 224-255</summary> */
        const ulong IRR_dw7 = 0x270;
        /** <summary>Error status register</summary> */
        const ulong ESR_offset = 0x280;
        /** <summary>LVT CMCI registers</summary> */
        const ulong LVT_CMCI_offset = 0x2f0;
        /** <summary>Interrupt command register bits 0-31</summary> */
        const ulong ICR_dw0 = 0x300;
        /** <summary>Interrupt command register bits 32-63</summary> */
        const ulong ICR_dw1 = 0x310;
        /** <summary>LVT timer register</summary> */
        const ulong LVT_timer_offset = 0x320;
        /** <summary>LVT thermal sensor register</summary> */
        const ulong LVT_thermal_offset = 0x330;
        /** <summary>LVT performance monitoring counters register</summary> */
        const ulong LVT_perfmon_offset = 0x340;
        /** <summary>LVT LINT0 register</summary> */
        const ulong LVT_LINT0_offset = 0x350;
        /** <summary>LVT LINT1 register</summary> */
        const ulong LVT_LINT1_offset = 0x360;
        /** <summary>LVT error register</summary> */
        const ulong LVT_error_offset = 0x370;
        /** <summary>Initial count register (for timer)</summary> */
        const ulong Initial_Count_offset = 0x380;
        /** <summary>Current count register (for timer)</summary> */
        const ulong Current_Count_offset = 0x390;
        /** <summary>Divide configuration register (for timer)</summary> */
        const ulong Divide_Conf_offset = 0x3e0;
        
        ulong lapic_base_paddr;
        ulong lapic_base_vaddr;
        double cur_timer_freq;
        bool calibrated = false;
        double lapic_base_freq;

        long _interval;

        long ticks = 0;

        internal override long TimerInterval { get { return _interval; } }

        public ulong LApicTimerDivisor
        {
            get
            {
                uint div_val = ReadConfDword(Divide_Conf_offset);
                div_val &= 0xb;

                Formatter.Write("Divider register value: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)div_val, "X", Program.arch.DebugOutput);

                ulong divider = 2;
                if(div_val == 0)
                    divider = 2;
                else if(div_val == 1)
                    divider = 4;
                else if(div_val == 2)
                    divider = 8;
                else if(div_val == 3)
                    divider = 16;
                else if(div_val == 8)
                    divider = 32;
                else if(div_val == 9)
                    divider = 64;
                else if(div_val == 10)
                    divider = 128;
                else if(div_val == 11)
                    divider = 1;
                else
                    throw new Exception("Unknown divider");

                Formatter.Write(" divider: ", Program.arch.DebugOutput);
                Formatter.Write(divider, "d", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);

                return divider;
            }            
        }

        internal LApic(Virtual_Regions vreg, VirtMem vmem)
        {
            /* Initialize the local APIC for the current processor */

            /* First, detect support for LAPIC: CPUID EAX=1 sets bit 9 of EDX if present */
            uint[] cpuid_1 = libsupcs.x86_64.Cpu.Cpuid(1);
            if ((cpuid_1[3] & (1 << 9)) != (1 << 9))
                throw new Exception("LAPIC not supported by this processor");

            /* Read the LAPIC base address */
            ulong lapic_base_msr = libsupcs.x86_64.Cpu.RdMsr(IA32_APIC_BASE_MSR);

            /* The base physical address is contained in bits 12 - 35, left shifted by 12 */
            lapic_base_paddr = lapic_base_msr & IA32_APIC_BASE_ADDR_MASK;

            /* Get the lapic id: CPUID EAX=1 sets bits 31-24 of EBX */
            ulong lapic_id = (ulong)(cpuid_1[1] >> 24);

            Formatter.Write("LAPIC: ID: ", Program.arch.DebugOutput);
            Formatter.Write(lapic_id, "X", Program.arch.DebugOutput);
            Formatter.Write(", paddr: ", Program.arch.DebugOutput);
            Formatter.Write(lapic_base_paddr, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Map in the lapic register space
             * 
             * Intel 3A:10.4.1:
             * The LAPIC register space is 4 kiB and must be mapped to an area of memory marked as
             * strong uncacheable (UC).
             * 
             * Intel 3A:11.5.2.1, table 11-6:
             * Setting PCD and PWT bits on a page table entry ensures UC state regardless of MTRRs             * 
             */
            lapic_base_vaddr = vreg.Alloc(0x1000, 0x1000, "LAPIC " + lapic_id.ToString() + " registers");
            vmem.map_page(lapic_base_vaddr, lapic_base_paddr, true, true, true);

            /* Intel 3A:10.4.7.1:
             * 
             * After reset, the LAPIC state is:
             * 
             * IRR                              = 0
             * ISR                              = 0
             * TMR                              = 0
             * ICR                              = 0
             * LDR                              = 0
             * TPR                              = 0
             * Timer initial count              = 0
             * Timer current count              = 0
             * Divide configuration register    = 0
             * DFR                              = all 1s
             * LVT                              = all 0s except mask bits which are 1
             * Version reg                      = unaffected
             * LAPIC ID reg                     = unique ID
             * Arb ID reg                       = LAPIC ID
             * Spurious interrupt reg           = 0x000000FF
             */
        }

        unsafe uint ReadConfDword(ulong offset)
        {
            return *(uint *)(lapic_base_vaddr + offset);
        }

        unsafe void WriteConfDword(ulong offset, uint val)
        {
            /* Formatter.Write("LApic: WriteConfDword: Writing ", Program.arch.DebugOutput);
            Formatter.Write((ulong)val, "X", Program.arch.DebugOutput);
            Formatter.Write(" to ", Program.arch.DebugOutput);
            Formatter.Write(lapic_base_vaddr + offset, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput); */

            *(uint*)(lapic_base_vaddr + offset) = val;
        }

        public void CalibrateTimerWithPIT()
        {
            /* Calibrate the LApic timer with the PIT
             * 
             * Use the PIT to calculate the system bus frequency */

            /* set up the pit */
            Formatter.WriteLine("Resetting PIT to 0xffff", Program.arch.DebugOutput);
            ushort initial_pit = 0xffff;
            ushort low_threshold = 0x1000;
            PIT_8253.ResetPIT(initial_pit);

            /* set up the lapic timer as masked, one shot, divisor of 1 */
            uint lapic_val = 0xffffffff;
            uint lapic_div = 11; /* bits 0, 1 and 3 set the divisor */
            uint lapic_reg = 0x00030000; /* masked, one-shot, vector 0 */
            WriteConfDword(LVT_timer_offset, lapic_reg);
            WriteConfDword(Divide_Conf_offset, lapic_div);
            WriteConfDword(Initial_Count_offset, lapic_val);
            lapic_val = ReadConfDword(Current_Count_offset);

            initial_pit = PIT_8253.ReadPIT();
            ushort cur_val = initial_pit;

            while (cur_val > low_threshold)
            {
                cur_val = PIT_8253.ReadPIT();
            }

            /* read the lapic timer */
            uint lapic_final_val = ReadConfDword(Current_Count_offset);

            /* calculate the elapsed time according to the pit */
            ulong total_delay_ticks = (ulong)(initial_pit - cur_val);
            ulong ns_delay = total_delay_ticks * 1000000000;
            ns_delay /= PIT_8253.Frequency;

            Formatter.WriteLine("Waited: ", Program.arch.DebugOutput);
            Formatter.Write(ns_delay, "d", Program.arch.DebugOutput);
            Formatter.WriteLine(" nanoseconds", Program.arch.DebugOutput);

            /* calculate the elapsed bus clock cycles */
            ulong bus_cycles = (ulong)(lapic_val - lapic_final_val);
            Formatter.Write(bus_cycles, "d", Program.arch.DebugOutput);
            Formatter.WriteLine(" bus cycles", Program.arch.DebugOutput);

            /* calculate the bus frequency */
            ulong ns_per_cycle = ns_delay / bus_cycles;
            Formatter.Write(ns_per_cycle, "d", Program.arch.DebugOutput);
            Formatter.WriteLine(" ns per bus cycle", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Reset the LAPIC timer to its default state */
            WriteConfDword(LVT_timer_offset, 0x00010000);
        }

        public void SetTimer(bool enabled, uint initial_count, byte target_vector)
        {
            uint lapic_reg = ReadConfDword(LVT_timer_offset);

            lapic_reg |= 0x00020000;                    // set periodic mode
            if (enabled)
                lapic_reg &= 0xfffeffff;               // clear mask bit
            else
                lapic_reg |= 0x00010000;                // else set it

            lapic_reg &= 0xffffff00;                    // clear vector bits
            lapic_reg |= (((uint)target_vector) & 0xff);// set them

            WriteConfDword(Initial_Count_offset, initial_count);
            WriteConfDword(LVT_timer_offset, lapic_reg);

            _interval = Convert.ToInt64(1000000000.0 * Convert.ToDouble((long)initial_count) / lapic_base_freq / Convert.ToDouble((long)LApicTimerDivisor));
            Formatter.Write("LAPIC: timer interval: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)_interval, "d", Program.arch.DebugOutput);
            Formatter.WriteLine(" ns", Program.arch.DebugOutput);
        }

        public void SetTimer(bool enabled, double freq_in_hz, byte target_vector)
        {
            if (!calibrated)
                throw new Exception("Please calibrate the timer first");

            double required_ticks = lapic_base_freq / freq_in_hz / Convert.ToDouble((long)LApicTimerDivisor);
            if (required_ticks < 1.0)
                throw new Exception("Frequency too high (" + required_ticks.ToString() + ")");
            if (required_ticks > Convert.ToDouble((long)uint.MaxValue))
                throw new Exception("Frequency too low (" + required_ticks.ToString() + ")");
            Formatter.WriteLine("LAPIC: SetTimer: freq_in_hz: " + freq_in_hz.ToString() + "  lapic_base_freq: " + lapic_base_freq.ToString() + "  divisor: " + Convert.ToDouble((long)LApicTimerDivisor).ToString(), Program.arch.DebugOutput);
            Formatter.WriteLine("LAPIC: SetTimer: Requested tick count: " + required_ticks.ToString(), Program.arch.DebugOutput);
            uint req_ticks = (uint)Convert.ToInt64(required_ticks);

            uint lapic_reg = 0x00020000;                    // set periodic mode
            if (!enabled)
                lapic_reg = 0x00030000;                     // set mask bit
            lapic_reg |= (((uint)target_vector) & 0xff);    // set interrupt vector we deliver to the cpu on

            WriteConfDword(Initial_Count_offset, req_ticks);
            WriteConfDword(LVT_timer_offset, lapic_reg);

            _interval = Convert.ToInt64(1000000000.0 / freq_in_hz);
            Formatter.Write("LAPIC: timer interval: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)_interval, "d", Program.arch.DebugOutput);
            Formatter.WriteLine(" ns", Program.arch.DebugOutput);
        }

        public void SetSpuriousVector(byte target_vector)
        {
            uint siv = ReadConfDword(Spurious_Interrupt_Vector_reg_offset);

            siv &= 0xffffff00;                          // mask out the current vector
            siv |= (((uint)target_vector) & 0xff);      // set the new vector

            WriteConfDword(Spurious_Interrupt_Vector_reg_offset, siv);
        }

        public void Enable()
        {
            /* to enable, set bit 8 in the spurious interrupt vector register */
            uint siv = ReadConfDword(Spurious_Interrupt_Vector_reg_offset);
            siv |= 0x00000100;
            WriteConfDword(Spurious_Interrupt_Vector_reg_offset, siv);
        }

        internal void CalibrateDirectly(double bus_freq)
        {
            lapic_base_freq = bus_freq;
            calibrated = true;
        }

        internal void CalibrateTimerWithHpet(Hpet hpet)
        { CalibrateTimerWithHpet(hpet, 0x1000000); }

        internal void CalibrateTimerWithHpet(Hpet hpet, ulong tsc_delay)
        {
            /* set up the hpet */
            ulong init_hpet = 0x0;
            hpet.SetMainCounter(init_hpet);
            init_hpet = hpet.ReadMainCounter();

            /* set up the lapic timer as masked, one shot, divisor of 1 */
            uint lapic_val = 0xffffffff;
            //uint lapic_div = 11; /* bits 0, 1 and 3 set the divisor */
            uint lapic_reg = 0x00010000; /* masked, one-shot, vector 0 */
            WriteConfDword(LVT_timer_offset, lapic_reg);
            //WriteConfDword(Divide_Conf_offset, lapic_div);
            WriteConfDword(Initial_Count_offset, lapic_val);

            /* start the hpet */
            hpet.EnableMainCounter();

            /* perform a delay using the tsc */
            ulong init_tsc = libsupcs.x86_64.Cpu.Tsc;
            ulong cur_tsc = init_tsc;
            while (cur_tsc < (init_tsc + tsc_delay))
                cur_tsc = libsupcs.x86_64.Cpu.Tsc;

            /* get the final values of the timers */
            ulong lapic_final_val = ReadConfDword(Current_Count_offset);
            hpet.DisableMainCounter();
            ulong hpet_final_val = hpet.ReadMainCounter();

            ulong delta_hpet = hpet_final_val - init_hpet;
            ulong delta_lapic = lapic_val - lapic_final_val;

            /* At this point:
             * 
             * actual time delay = delta_hpet / hpet_frequency _or_ delta_hpet * hpet_cycle_length
             * bus_cycles = delta_lapic * lapic_divisor
             * 
             * bus_frequency = bus_cycles / actual time delay
             */

            double actual_time_delay = Convert.ToDouble((long)delta_hpet) * hpet.MainCounterCycleLength;
            ulong bus_cycles = delta_lapic * LApicTimerDivisor;
            double bus_frequency = Convert.ToDouble((long)bus_cycles) / actual_time_delay;
            double bus_frequency_mhz = bus_frequency * 0.000001;

            Program.arch.DebugOutput.Write("Bus frequency " + bus_frequency_mhz.ToString() + " Mhz\n");

            Formatter.Write("  delta_tsc: ", Program.arch.DebugOutput);
            Formatter.Write(cur_tsc - init_tsc, "X", Program.arch.DebugOutput);
            Formatter.Write("  init_hpet: ", Program.arch.DebugOutput);
            Formatter.Write(init_hpet, "X", Program.arch.DebugOutput);
            Formatter.Write("  final_hpet: ", Program.arch.DebugOutput);
            Formatter.Write(hpet_final_val, "X", Program.arch.DebugOutput);
            Formatter.Write("  delta_hpet: ", Program.arch.DebugOutput);
            Formatter.Write(delta_hpet, "d", Program.arch.DebugOutput);
            Formatter.Write("  delta_lapic: ", Program.arch.DebugOutput);
            Formatter.Write(delta_lapic, "d", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            lapic_base_freq = bus_frequency;
            calibrated = true;
        }

        public void SendEOI()
        {
            /* Write of _any_ value to the EOI register constitutes a valid EOI message */
            WriteConfDword(EOI_offset, 0);
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("__lapic_eoi")]
        internal static void CurLapicSendEOI()
        {
            ((x86_64_cpu)Program.arch.CurrentCpu).CurrentLApic.SendEOI();
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.CallingConvention("isr")]
        public static unsafe void SpuriousApicInterrupt(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Spurious LAPIC interrupt", Program.arch.DebugOutput);

            /* Spurious vector ISR does not generate an EOI */
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.CallingConvention("isr")]
        internal static unsafe void TimerInterrupt(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            LApic cur_lapic = ((x86_64.x86_64_cpu)Program.arch.CurrentCpu).CurrentLApic;

            cur_lapic.ticks += cur_lapic._interval;
            cur_lapic.SendEOI();
            if (cur_lapic.callback != null)
                cur_lapic.callback(cur_lapic._interval);
        }

        internal override long Ticks { get { return ticks / 100; } }
    }
}
