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
    class Acpi
    {
        ulong rdsp_vaddr;
        public List<AcpiTable> tables;

        public HpetTable Hpet;
        public ApicTable Apic;

        public unsafe Acpi(Virtual_Regions vreg, VirtMem vmem, Arch.FirmwareConfiguration fwconf)
        {
            ulong xsdt = 0;
            uint rsdt = 0;

            /* Decide which table to use
             * 
             * Use XSDT first if available */

            if(fwconf.ACPI_20_table != null)
            {
                ulong va_acpi_20 = Program.map_in((ulong)fwconf.ACPI_20_table, 36, "RSDP");
                uint acpi_20_len = *(uint*)(va_acpi_20 + 20);
                if (acpi_20_len > 32)
                    xsdt = *(ulong*)(va_acpi_20 + 24);
                if (xsdt == 0)
                    rsdt = *(uint*)(va_acpi_20 + 16);
            } 
            else if(fwconf.ACPI_10_table != null)
            {
                ulong va_acpi_10 = Program.map_in((ulong)fwconf.ACPI_10_table, 24, "RSDP");
                rsdt = *(uint*)(va_acpi_10 + 16);
            }

            if (xsdt == 0 && rsdt == 0)
                throw new Exception("ACPI: RSDP not found");

            Formatter.Write("ACPI: RSDT: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)rsdt, "X", Program.arch.DebugOutput);
            Formatter.Write(", XSDT: ", Program.arch.DebugOutput);
            Formatter.Write(xsdt, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            tables = new List<AcpiTable>();

            if(xsdt != 0)
            {
                /* Use XSDT */
                ulong xsdt_va = Program.map_in(xsdt, 36, "XSDT header");
                uint len = *(uint*)(xsdt_va + 4);
                xsdt_va = Program.map_in(xsdt, len, "XSDT");

                uint entries = (len - 36) / 8;
                for(uint i = 0; i < entries; i++)
                {
                    ulong tbl_address = *(ulong*)(xsdt_va + 36 + i * 8);
                    Formatter.Write("ACPI: XSDT entry: ", Program.arch.DebugOutput);
                    Formatter.Write(tbl_address, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    InterpretTable(tbl_address);
                }
            }
            else if(rsdt != 0)
            {
                /* Use RSDT */
                ulong rsdt_va = Program.map_in(xsdt, 36, "RSDT header");
                uint len = *(uint*)(rsdt_va + 4);
                rsdt_va = Program.map_in(rsdt, len, "RSDT");

                uint entries = (len - 36) / 4;
                for (uint i = 0; i < entries; i++)
                {
                    uint tbl_address = *(uint*)(rsdt_va + 36 + i * 4);
                    Formatter.Write("ACPI: RSDT entry: ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)tbl_address, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    InterpretTable(tbl_address);
                }
            }
        }

        public Acpi(Virtual_Regions vreg, VirtMem vmem, ulong bda_vaddr, Multiboot.MachineMinorType_x86 bios)
        {
            if (bios == Multiboot.MachineMinorType_x86.BIOS)
                InitBIOS(vreg, vmem, bda_vaddr);
            else if (bios == Multiboot.MachineMinorType_x86.UEFI)
                InitUEFI(vreg, vmem, bda_vaddr);
        }

        unsafe void InitUEFI(Virtual_Regions vreg, VirtMem vmem, ulong system_table_paddr)
        {
            System.Diagnostics.Debugger.Break();
        }

        unsafe void InitBIOS(Virtual_Regions vreg, VirtMem vmem, ulong bda_vaddr)
        {
            /* To set up ACPI, we first need to find the Root System Description Pointer (RDSP)
             * This is a data structure that is in one of two places:
             * 
             * 1) The extended bios data area, this is pointed to by the word at physical address 0x040e,
             *      left shifted by 4)
             * 2) The memory from 0xe0000 to 0xfffff
             * 
             * The RDSP is 16-byte aligned and starts with the string value "RSD PTR "
             * this is equal to 0x2052545020445352
             */
            
            ulong ebda_paddr = ((ulong)*(ushort*)(bda_vaddr + 0x40e)) << 4;
            ulong ebda_length = 0xa0000 - ebda_paddr;

            Formatter.Write("EBDA found at: ", Program.arch.DebugOutput);
            Formatter.Write(ebda_paddr, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* map in the EBDA */
            ulong ebda_vaddr = Program.map_in(ebda_paddr, ebda_length, "EBDA");


            rdsp_vaddr = 0;
            ulong cur_addr = ebda_vaddr;
            while (cur_addr < (ebda_vaddr + ebda_length))
            {
                if(*(ulong *)cur_addr == 0x2052545020445352UL)
                {
                    if (verify_rdsp(cur_addr))
                    {
                        rdsp_vaddr = cur_addr;
                        break;
                    }
                }
                cur_addr += 16;
            }

            if (rdsp_vaddr == 0)
            {
                /* we didn't find the RDSP in the EDBA, therefore search the range 0xe0000 to 0xfffff */
                ulong search_base_vaddr = Program.map_in(0xe0000, 0x20000, "RDSP Search");
                cur_addr = search_base_vaddr;
                while (cur_addr < (search_base_vaddr + 0x20000))
                {
                    if (*(ulong*)cur_addr == 0x2052545020445352UL)
                    {
                        if (verify_rdsp(cur_addr))
                        {
                            rdsp_vaddr = cur_addr;
                            break;
                        }
                    }
                    cur_addr += 16;
                }
            }

            if (rdsp_vaddr == 0)
                throw new Exception("RDSP not found");


            /* We have found the RDSP, now interpret it */
            RDSPDescriptor* rdsp = (RDSPDescriptor*)rdsp_vaddr;
            ulong revision = rdsp->checksum_oemid_revision >> 56;
            bool use_xsdt = false;
            ulong sdt_paddr = 0;
            ulong sdt_length = 0;

            if (revision == 0)
            {
                sdt_paddr = (ulong)rdsp->RSDT_paddr;
                sdt_length = 36;
            }
            else
            {
                use_xsdt = true;
                sdt_paddr = rdsp->XSDT_paddr;
                sdt_length = (ulong)rdsp->length;
            }

            if (use_xsdt)
                Formatter.Write("XSDT at ", Program.arch.DebugOutput);
            else
                Formatter.Write("RDST at ", Program.arch.DebugOutput);
            Formatter.Write(sdt_paddr, "X", Program.arch.DebugOutput);
            Formatter.Write(" length: ", Program.arch.DebugOutput);
            Formatter.Write(sdt_length, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Map in the xsdt */
            string tab_name = "RSDT";
            if (use_xsdt)
                tab_name = "XSDT";
            ulong xsdt_vaddr = Program.map_in(sdt_paddr, sdt_length, tab_name);
            XSDT* xsdt = (XSDT*)xsdt_vaddr;

            if (use_xsdt)
                Formatter.Write("XSDT signature: ", Program.arch.DebugOutput);
            else
                Formatter.Write("RSDT signature: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)xsdt->signature, "X", Program.arch.DebugOutput);
            Formatter.Write(" length: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)xsdt->length, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Now remap it based on its new length */
            xsdt_vaddr = Program.map_in(sdt_paddr, xsdt->length, tab_name);

            /* Read in the various tables */
            tables = new List<AcpiTable>();
            ulong table_pointer = xsdt_vaddr + 36;
            while (table_pointer < (xsdt_vaddr + xsdt->length))
            {
                if (use_xsdt)
                {
                    InterpretTable(*(ulong*)table_pointer);
                    table_pointer += 8;
                }
                else
                {
                    InterpretTable((ulong)*(uint*)table_pointer);
                    table_pointer += 4;
                }
            }
        }

        private unsafe void InterpretTable(ulong table_paddr)
        {
            ulong table_vaddr = Program.map_in(table_paddr, 0x8, "ACPI table");

            uint signature = *(uint*)table_vaddr;               // First 4 bytes of all tables are the signature
            ulong length = (ulong)*(uint*)(table_vaddr + 4);    // Next 4 are its length

            /* reload the table now we know its length */
            table_vaddr = Program.map_in(table_paddr, length, "ACPI table");

            Formatter.Write("ACPI table signature: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)signature, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            if (signature == AcpiTable.SIG_APIC)
                InterpretApicTable(table_vaddr, length);
            else if (signature == AcpiTable.SIG_HPET)
                InterpretHpetTable(table_vaddr, length);
        }

        private unsafe void InterpretHpetTable(ulong table_vaddr, ulong table_length)
        {
            HpetTable hpet = new HpetTable();

            hpet.start_vaddr = table_vaddr;
            hpet.length = table_length;
            hpet.signature = AcpiTable.SIG_HPET;
            hpet.event_timer_block_id = *(uint*)(table_vaddr + 36);
            hpet.address_space_id_register_bit_width_offset_reserved = *(uint*)(table_vaddr + 40);
            hpet.paddr = *(ulong*)(table_vaddr + 44);
            hpet.hpet_number_minimum_clock_tick_page_protection = *(uint*)(table_vaddr + 52);

            Hpet = hpet;
            tables.Add(hpet);
        }

        private unsafe void InterpretApicTable(ulong table_vaddr, ulong table_length)
        {
            ApicTable apic = new ApicTable();

            apic.start_vaddr = table_vaddr;
            apic.length = table_length;
            apic.signature = AcpiTable.SIG_APIC;
            apic.lapic_paddr = *(uint*)(table_vaddr + 36);
            apic.flags = *(uint*)(table_vaddr + 40);

            Apic = apic;
            tables.Add(apic);
        }

        public class ApicTable : AcpiTable
        {
            public uint lapic_paddr;
            public uint flags;

            public bool Has8259 { get { return (flags & 0x1) == 0x1; } }
        }

        public class HpetTable : AcpiTable
        {
            public uint event_timer_block_id;
            public uint address_space_id_register_bit_width_offset_reserved;
            public ulong paddr;
            public uint hpet_number_minimum_clock_tick_page_protection;
        }

        public class AcpiTable
        {
            public ulong start_vaddr;
            public ulong length;
            public uint signature;

            public const uint SIG_APIC = 0x43495041;
            public const uint SIG_FACP = 0x50434146;
            public const uint SIG_HPET = 0x54455048;
            public const uint SIG_MCFG = 0x4746434D;
            public const uint SIG_SSDT = 0x54445353;
        }

        struct RDSPDescriptor
        {
            public ulong signature;
            public ulong checksum_oemid_revision;
            public uint RSDT_paddr;
            public uint length;
            public ulong XSDT_paddr;
            public uint xchecksum_reserved;
        }

        struct XSDT
        {
            public uint signature;
            public uint length;
            public ulong revision_checksum_oemid;
            public ulong oem_tableid;
            public uint oem_revision;
            public uint creator_id;
            public uint creator_revision;
        }

        private bool verify_rdsp(ulong cur_addr)
        {
            // TODO - check the checksum of the ACPI tables
            return true;
        }
    }
}
