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

namespace ACPI_PC
{
    class IOAPIC : tysos.IPIC
    {
        ulong base_reg_paddr;
        ulong base_reg_vaddr;
        int gsi_base;
        int gsi_len;
        int id;

        const uint ID_register = 0x0;
        const uint Ver_register = 0x1;
        const uint Arbritration_register = 0x2;
        const uint Redirection_table = 0x10;

        public IOAPIC(int apic_id, ulong paddr, int global_sys_interrupt_base, List<AcpiTables.ApicTable.ISOStructure> isos, tysos.InterruptMap imap)
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("IOAPIC: ID: " + apic_id.ToString() + " paddr: 0x" + paddr.ToString("X") + " global_sys_interrupt_base: " + global_sys_interrupt_base.ToString() + "\n");
            base_reg_paddr = paddr;
            base_reg_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(paddr, 0x1000, tysos.Syscalls.MemoryFunctions.CacheType.Uncacheable, true);
            id = apic_id;

            gsi_base = global_sys_interrupt_base;
            uint ver = ReadRegister(Ver_register);
            gsi_len = (int)((ver >> 16) & 0xff) + 1;

            // Set up default ISA IRQs
            for (int i = gsi_base; i < (gsi_base + gsi_len); i++)
            {
                int act_irq = i;

                foreach (AcpiTables.ApicTable.ISOStructure iso in isos)
                {
                    /* If this is the destination of a remap then change our source */
                    if ((int)iso.global_system_interrupt == i)
                        act_irq = iso.source;
                    else if (iso.source == i)
                    {
                        /* else if this is being remapped elsewhere then ignore it */
                        act_irq = -1;
                    }
                }

                string name = null;
                switch (act_irq)
                {
                    case 0:
                        name = "PIT";
                        break;
                    case 1:
                        name = "Keyboard";
                        break;
                    case 3:
                        name = "COM2";
                        break;
                    case 4:
                        name = "COM1";
                        break;
                    case 6:
                        name = "FDC";
                        break;
                    case 7:
                        name = "Spurious";
                        break;
                    case 8:
                        name = "RTC";
                        break;
                    case 14:
                        name = "ATA0";
                        break;
                    case 15:
                        name = "ATA1";
                        break;
                }

                if (name != null)
                {
                    tysos.PICEntry pe = new tysos.PICEntry();
                    pe.PIC = this;
                    pe.pri = i - gsi_base;
                    imap.RegisterInterrupt(name, pe);
                }
            }
        }

        public override string ToString()
        {
            return "IOAPIC_" + id.ToString() + " (base_addr: 0x" + base_reg_paddr.ToString("X") + " gsi_base: " + gsi_base.ToString() + ")";
        }

        unsafe void WriteRegister(uint offset, uint val)
        {
            *(uint*)(base_reg_vaddr) = offset;
            *(uint*)(base_reg_vaddr + 0x10) = val;
        }

        unsafe uint ReadRegister(uint offset)
        {
            *(uint*)(base_reg_vaddr) = offset;
            return *(uint*)(base_reg_vaddr + 0x10);
        }

        internal static void DisablePIC()
        {
            libsupcs.IoOperations.PortOut(0xa1, (byte)0xff);
            libsupcs.IoOperations.PortOut(0x21, (byte)0xff);
        }

        public void EnableIRQ(int pri, int cpu_vector, int cpu_id)
        {
            uint offset_1 = (uint)(Redirection_table + pri * 2);
            uint offset_2 = offset_1 + 1;

            uint low_val = ReadRegister(offset_1);
            uint high_val = ReadRegister(offset_2);

            high_val &= 0xffffff00;     // mask destination field
            high_val |= ((uint)cpu_id << 24);

            low_val &= 0xfffeffff;      // clear mask bit
            low_val &= 0xfffff7ff;      // set to physical mode
            low_val &= 0xffffff00;      // clear then set vector
            low_val |= ((uint)cpu_vector & 0xff);

            WriteRegister(offset_1, low_val);
            WriteRegister(offset_2, high_val);
        }

        public void DisableIRQ(int pri)
        {
            throw new NotImplementedException();
        }
    }
}
