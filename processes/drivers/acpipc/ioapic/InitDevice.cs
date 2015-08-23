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

using System;
using System.Collections.Generic;
using System.Text;
using tysos.Resources;

namespace acpipc.ioapic
{
    partial class ioapic : tysos.ServerObject
    {
        tysos.lib.File.Property[] props;

        internal uint gsibase, id;
        tysos.VirtualMemoryResource64 v_conf;
        tysos.PhysicalMemoryResource64 p_conf;
        internal List<tysos.Resources.InterruptLine> ints = new List<tysos.Resources.InterruptLine>();

        IOAPICGSI[] gsis = new IOAPICGSI[24];


        public ioapic(tysos.lib.File.Property[] Properties)
        {
            props = Properties;
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "ioapic", "IOAPIC driver started");

            /* Parse properties looking for resources */
            
            foreach(var prop in props)
            {
                if (prop.Name == "pmem")
                    p_conf = prop.Value as tysos.PhysicalMemoryResource64;
                else if (prop.Name == "vmem")
                    v_conf = prop.Value as tysos.VirtualMemoryResource64;
                else if (prop.Name == "gsibase")
                    gsibase = (uint)prop.Value;
                else if (prop.Name == "ioapicid")
                    id = (uint)prop.Value;
                else if (prop.Name == "interrupts")
                    ints.AddRange(prop.Value as IEnumerable<tysos.Resources.InterruptLine>);
            }

            if(p_conf == null)
            {
                System.Diagnostics.Debugger.Log(0, "ioapic", "no physical memory provided");
                return false;
            }
            if(v_conf == null)
            {
                System.Diagnostics.Debugger.Log(0, "ioapic", "no virtual memory provided");
                return false;
            }

            /* Map our configuration space */
            p_conf.Map(v_conf);

            /* Build a list of free GSIs.  The standard IOAPIC has 24 pins. */
            for(int i = 0; i < 24; i++)
            {
                IOAPICGSI gsi = new IOAPICGSI();
                gsi.apic = this;
                gsi.gsi_num = (int)gsibase + i;
                gsi.ioapic_idx = i;
                gsis[i] = gsi;
            }

            return true;
        }

        public void AddCpuInterrupts(IEnumerable<tysos.Resources.InterruptLine> interrupts)
        {
            lock(ints)
            {
                ints.AddRange(interrupts);
            }
        }

        internal uint ReadRegister(int reg_no)
        {
            v_conf.Write(v_conf.Addr64, 1, (byte)reg_no);
            return (uint)v_conf.Read(v_conf.Addr64 + 0x10U, 4);
        }

        internal void WriteRegister(int reg_no, uint val)
        {
            v_conf.Write(v_conf.Addr64, 1, (byte)reg_no);
            v_conf.Write(v_conf.Addr64 + 0x10U, 4, val);
        }

        public IOAPICGSI GetInterruptLine(int gsi_num)
        {
            if (gsi_num < gsibase || gsi_num >= gsibase + 24)
                return null;

            int ioapic_idx = gsi_num - (int)gsibase;
            if (gsis[ioapic_idx] == null)
                return null;
            else
            {
                IOAPICGSI ret = gsis[ioapic_idx];
                gsis[ioapic_idx] = null;
                return ret;
            }
        }
    }

    class IOAPICGSI : GlobalSystemInterrupt
    {
        internal ioapic apic;
        internal tysos.x86_64.x86_64_Interrupt cpu_int;
        internal int ioapic_idx;

        public override bool RegisterHandler(InterruptHandler handler)
        {
            /* Get a free CPU interrupt */
            if (cpu_int == null)
            {
                lock (apic.ints)
                {
                    if (apic.ints.Count == 0)
                    {
                        System.Diagnostics.Debugger.Log(0, "ioapic", "Unable to register handler for GSI " + gsi_num.ToString() + ": no free cpu interrupts available");
                        return false;
                    }
                    cpu_int = apic.ints[apic.ints.Count - 1] as tysos.x86_64.x86_64_Interrupt;
                    apic.ints.RemoveAt(apic.ints.Count - 1);
                }
            }
            if(cpu_int == null)
            {
                System.Diagnostics.Debugger.Log(0, "ioapic", "CPU register is not a x86_64 interrupt");
                return false;
            }

            /* Disable the ioapic entry for now, then re-enable later once we have
            set up the cpu interrupt and the trigger mode */
            int ioredtbl_idx = 0x10 + 2 * ioapic_idx;
            uint ioredtbl = apic.ReadRegister(ioredtbl_idx);
            ioredtbl &= 0xfffe0000U;
            ioredtbl |= 0x1U << 16;
            apic.WriteRegister(ioredtbl_idx, ioredtbl);

            /* Register the handler with the cpu */
            cpu_int.RegisterHandler(handler);

            /* Don't enable the ioapic entry yet - we do not know the trigger
            mode or polarity - these need to be provided by a separate call
            from the acpi driver (which does know these things) */
            return true;
        }

        internal override void SetMode(bool is_level_trigger, bool is_low_active)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("IOAPIC ");
            sb.Append(apic.id.ToString());
            sb.Append(" pin ");
            sb.Append(ioapic_idx.ToString());
            sb.Append(" gsi ");
            sb.Append((apic.gsibase + (uint)ioapic_idx).ToString());

            if(cpu_int != null)
            {
                sb.Append(" -> ");
                sb.Append(cpu_int.ToString());
            }

            return sb.ToString();
        }
    }
}
