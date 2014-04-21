/* Copyright (C) 2008 - 2011 by John Cronin
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
    class Hpet
    {
        ulong hpet_reg_space_vaddr;
        int timer_count;
        bool main_timer_is_64_bit;
        uint counter_clk_period;

        public uint CounterClkPeriod { get { return counter_clk_period; } }
        public bool MainCounterIs64bit { get { return main_timer_is_64_bit; } }
        public int TimerCount { get { return timer_count; } }

        public double MainCounterCycleLength { get { return Convert.ToDouble((long)counter_clk_period) * 0.000000000000001; } }

        public Hpet(Virtual_Regions vreg, VirtMem vmem, ulong paddr)
        {
            Formatter.WriteLine("Initialising HPET", Program.arch.DebugOutput);
            hpet_reg_space_vaddr = Program.map_in(paddr, 0x1000, "HPET", true, true, true);

            /* General capabilities register at offset 0
             * 
             * bits 0-7         revision id
             * bits 12-8        one less than number of timers (i.e. id of last timer)
             * bit 13           count size cap.  1 if main counter is 64 bits wide, else 0
             * bit 14           reserved
             * bit 15           legacy replacement route capable
             * bits 16-31       vendor ID (as per PCI)
             * bits 32-63       rate at which main counter increments in femtoseconds (10 ^ -15)
             */
            ulong gen_cap = ReadQwordRegister(0);
            timer_count = (int)(((gen_cap >> 8) & 0x1f) + 1);
            main_timer_is_64_bit = ((gen_cap & 0x2000) == 0x2000);
            counter_clk_period = (uint)(gen_cap >> 32);

            Formatter.Write("CounterClkPeriod: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)counter_clk_period, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            double hpet_freq_mhz = Convert.ToDouble((long)counter_clk_period);
            hpet_freq_mhz *= 0.000000001;
            hpet_freq_mhz = 1 / hpet_freq_mhz;

            Program.arch.DebugOutput.Write("HPET frequency is " + hpet_freq_mhz.ToString() + " Mhz\n");

            DisableMainCounter();
        }

        public ulong ReadMainCounter()
        {
            /* main counter is at 0xf0 */
            return ReadQwordRegister(0xf0);
        }

        public void DisableMainCounter()
        {
            /* we use the general configuration register at 0x10
             * 
             * if bit 0 is clear the timer is halted
             * if bit 0 is set it is running and will generate interrupts
             */

            ulong conf = ReadQwordRegister(0x10);
            conf &= ~1UL;
            WriteQwordRegister(0x10, conf);
        }

        public void EnableMainCounter()
        {
            ulong conf = ReadQwordRegister(0x10);
            conf |= 1;
            WriteQwordRegister(0x10, conf);
        }

        public void SetMainCounter(ulong val)
        {
            /* we need to disable the counter before writing to it */
            DisableMainCounter();

            /* main counter is at 0xf0 */
            if (!main_timer_is_64_bit)
                val &= 0xffffffff;

            WriteQwordRegister(0xf0, val);
        }

        unsafe ulong ReadQwordRegister(ulong offset)
        {
            return *(ulong*)(hpet_reg_space_vaddr + offset);
        }

        unsafe void WriteQwordRegister(ulong offset, ulong val)
        {
            *(ulong*)(hpet_reg_space_vaddr + offset) = val;
        }
    }
}
