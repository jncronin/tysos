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
using tysos;

namespace ACPI_PC
{
    class AcpiTables
    {
        ulong rdsp_vaddr;
        public List<AcpiTable> tables;

        public HpetTable Hpet;
        public ApicTable Apic;
        public FixedACPIDescriptionTable Fadt;
        public List<SsdtTable> Ssdts;

        const ulong RSD_PTR = 0x2052545020445352UL;

        public AcpiTables()
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

            unsafe
            {
                /* Map in the BDA */
                ulong bda_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(0x0, 0x1000, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);

                /* map in the EBDA */
                ulong ebda_paddr = ((ulong)*(ushort*)(bda_vaddr + 0x40e)) << 4;
                ulong ebda_length = 0xa0000 - ebda_paddr;
                ulong ebda_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(ebda_paddr, ebda_length, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);

                rdsp_vaddr = 0;
                ulong cur_addr = ebda_vaddr;
                while (cur_addr < (ebda_vaddr + ebda_length))
                {
                    if(*(ulong *)cur_addr == RSD_PTR)
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
                    ulong search_base_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(0xe0000, 0x20000, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);
                    cur_addr = search_base_vaddr;
                    while (cur_addr < (search_base_vaddr + 0x20000))
                    {
                        if (*(ulong*)cur_addr == RSD_PTR)
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

                /* Map in the xsdt */
                ulong xsdt_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(sdt_paddr, sdt_length, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);
                XSDT* xsdt = (XSDT*)xsdt_vaddr;

                /* Now remap it based on its new length */
                xsdt_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(sdt_paddr, xsdt->length, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);

                /* Read in the various tables */
                tables = new List<AcpiTable>();
                Ssdts = new List<SsdtTable>();
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
        }

        private unsafe void InterpretTable(ulong table_paddr)
        {
            ulong table_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(table_paddr, 0x8, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);

            uint signature = *(uint*)table_vaddr;               // First 4 bytes of all tables are the signature
            ulong length = (ulong)*(uint*)(table_vaddr + 4);    // Next 4 are its length

            /* reload the table now we know its length */
            table_vaddr = tysos.Syscalls.MemoryFunctions.MapPhysicalMemory(table_paddr, length, Syscalls.MemoryFunctions.CacheType.Uncacheable, true);

            if (signature == AcpiTable.SIG_APIC)
                InterpretApicTable(table_vaddr, length);
            else if (signature == AcpiTable.SIG_HPET)
                InterpretHpetTable(table_vaddr, length);
            else if (signature == AcpiTable.SIG_FACP)
                InterpretFADT(table_vaddr, length);
            else if (signature == AcpiTable.SIG_SSDT)
                InterpretSSDT(table_vaddr, length);
        }

        private void InterpretSSDT(ulong table_vaddr, ulong length)
        {
            SsdtTable ssdt = new SsdtTable();

            ssdt.start_vaddr = table_vaddr;
            ssdt.length = length;
            ssdt.signature = AcpiTable.SIG_SSDT;
            ssdt.ssdt_vaddr = table_vaddr;

            Ssdts.Add(ssdt);
            tables.Add(ssdt);
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

            apic.IOAPICs = new List<ApicTable.IOAPICStructure>();
            apic.IOSAPICs = new List<ApicTable.IOAPICStructure>();
            apic.LAPICNMIs = new List<ApicTable.LAPICNMIStructure>();
            apic.LAPICs = new List<ApicTable.LAPICStructure>();
            apic.NMIs = new List<ApicTable.NMIStructure>();
            apic.InterruptSourceOverrides = new List<ApicTable.ISOStructure>();

            ulong cur_vaddr = table_vaddr + 44;
            while (cur_vaddr < (table_vaddr + table_length))
            {
                int type = (int)*(byte*)(cur_vaddr + 0);
                int length = (int)*(byte*)(cur_vaddr + 1);

                if (type == ApicTable.TYPE_LAPIC)
                {
                    ApicTable.LAPICStructure lapic = new ApicTable.LAPICStructure();

                    lapic.type = type;
                    lapic.length = length;
                    lapic.proc_id = (int)*(byte*)(cur_vaddr + 2);
                    lapic.lapic_id = (int)*(byte*)(cur_vaddr + 3);
                    lapic.flags = *(uint*)(cur_vaddr + 4);

                    apic.LAPICs.Add(lapic);
                }
                else if (type == ApicTable.TYPE_IOAPIC)
                {
                    ApicTable.IOAPICStructure ioapic = new ApicTable.IOAPICStructure();

                    ioapic.type = type;
                    ioapic.length = length;
                    ioapic.ioapic_id = (int)*(byte*)(cur_vaddr + 2);
                    ioapic.ioapic_paddr = (ulong)*(uint*)(cur_vaddr + 4);
                    ioapic.global_system_interrupt_base = *(uint*)(cur_vaddr + 8);

                    apic.IOAPICs.Add(ioapic);
                }
                else if (type == ApicTable.TYPE_INTERRUPT_SOURCE_OVERRIDE)
                {
                    ApicTable.ISOStructure iso = new ApicTable.ISOStructure();

                    iso.type = type;
                    iso.length = length;
                    byte bus = *(byte*)(cur_vaddr + 2);
                    if (bus == 0)
                        iso.bus = ApicTable.ISOStructure.BusType.ISA;
                    else
                        throw new Exception("Invalid bus type");
                    iso.source = (int)*(byte*)(cur_vaddr + 3);
                    iso.global_system_interrupt = *(uint*)(cur_vaddr + 4);
                    iso.flags = (uint)*(ushort*)(cur_vaddr + 8);

                    apic.InterruptSourceOverrides.Add(iso);
                }
                else if (type == ApicTable.TYPE_NMI)
                {
                    ApicTable.NMIStructure nmi = new ApicTable.NMIStructure();

                    nmi.type = type;
                    nmi.length = length;
                    nmi.flags = (uint)*(ushort*)(cur_vaddr + 2);
                    nmi.global_system_interrupt = *(uint*)(cur_vaddr + 4);

                    apic.NMIs.Add(nmi);
                }
                else if (type == ApicTable.TYPE_LAPIC_NMI)
                {
                    ApicTable.LAPICNMIStructure lapic_nmi = new ApicTable.LAPICNMIStructure();

                    lapic_nmi.type = type;
                    lapic_nmi.length = length;
                    lapic_nmi.proc_id = (int)*(byte*)(cur_vaddr + 2);
                    lapic_nmi.flags = (uint)*(ushort*)(cur_vaddr + 3);
                    lapic_nmi.lapic_lint = (int)*(byte*)(cur_vaddr + 5);

                    apic.LAPICNMIs.Add(lapic_nmi);
                }
                else if (type == ApicTable.TYPE_IOSAPIC)
                {
                    ApicTable.IOAPICStructure iosapic = new ApicTable.IOAPICStructure();

                    iosapic.type = type;
                    iosapic.length = length;
                    iosapic.ioapic_id = (int)*(byte*)(cur_vaddr + 2);
                    iosapic.global_system_interrupt_base = *(uint*)(cur_vaddr + 4);
                    iosapic.ioapic_paddr = *(ulong*)(cur_vaddr + 8);

                    apic.IOSAPICs.Add(iosapic);
                }

                cur_vaddr += (ulong)length;
            }

            Apic = apic;
            tables.Add(apic);
        }

        private unsafe void InterpretFADT(ulong table_vaddr, ulong table_length)
        {
            FixedACPIDescriptionTable fadt = new FixedACPIDescriptionTable();

            fadt.start_vaddr = table_vaddr;
            fadt.length = table_length;
            fadt.signature = AcpiTable.SIG_FACP;

            fadt._facs_paddr = *(uint*)(table_vaddr + 36);
            fadt._dsdt_paddr = *(uint*)(table_vaddr + 40);
            fadt.preferred_pm_profile = (uint)*(byte*)(table_vaddr + 45);
            fadt.sci_int = (uint)*(ushort*)(table_vaddr + 46);
            fadt.sci_cmd = (uint)*(uint*)(table_vaddr + 48);
            fadt.acpi_enable = (uint)*(byte*)(table_vaddr + 52);
            fadt.acpi_disable = (uint)*(byte*)(table_vaddr + 53);
            fadt.s4bios_req = (uint)*(byte*)(table_vaddr + 54);
            fadt.pstate_cnt = (uint)*(byte*)(table_vaddr + 55);
            fadt._pm1a_evt_blk = (uint)*(uint*)(table_vaddr + 56);
            fadt._pm1b_evt_blk = (uint)*(uint*)(table_vaddr + 60);
            fadt._pm1a_cnt_blk = (uint)*(uint*)(table_vaddr + 64);
            fadt._pm1b_cnt_blk = (uint)*(uint*)(table_vaddr + 68);
            fadt._pm2_cnt_blk = (uint)*(uint*)(table_vaddr + 72);
            fadt._pm_tmr_blk = (uint)*(uint*)(table_vaddr + 76);
            fadt._gpe0_blk = (uint)*(uint*)(table_vaddr + 80);
            fadt._gpe1_blk = (uint)*(uint*)(table_vaddr + 84);
            fadt.pm1_evt_len = (uint)*(byte*)(table_vaddr + 88);
            fadt.pm1_cnt_len = (uint)*(byte*)(table_vaddr + 89);
            fadt.pm2_cnt_len = (uint)*(byte*)(table_vaddr + 90);
            fadt.pm_tmr_len = (uint)*(byte*)(table_vaddr + 91);
            fadt.gpe0_blk_len = (uint)*(byte*)(table_vaddr + 92);
            fadt.gpe1_blk_len = (uint)*(byte*)(table_vaddr + 93);
            fadt.gpe1_base = (uint)*(byte*)(table_vaddr + 94);
            fadt.cst_cnt = (uint)*(byte*)(table_vaddr + 95);
            fadt.p_lvl2_lat = (uint)*(ushort*)(table_vaddr + 96);
            fadt.p_lvl3_lat = (uint)*(ushort*)(table_vaddr + 98);
            fadt.flush_size = (uint)*(ushort*)(table_vaddr + 100);
            fadt.flush_stride = (uint)*(ushort*)(table_vaddr + 102);
            fadt.duty_offset = (uint)*(byte*)(table_vaddr + 104);
            fadt.duty_width = (uint)*(byte*)(table_vaddr + 105);
            fadt.day_alrm = (uint)*(byte*)(table_vaddr + 106);
            fadt.mon_alrm = (uint)*(byte*)(table_vaddr + 107);
            fadt.century = (uint)*(byte*)(table_vaddr + 108);
            fadt.iapc_boot_arch = (uint)*(ushort*)(table_vaddr + 109);
            fadt.flags = (uint)*(uint*)(table_vaddr + 112);
            fadt.reset_reg = GenericAddressStructure.Interpret(table_vaddr + 116);
            fadt.reset_value = (uint)*(byte*)(table_vaddr + 128);
            fadt.x_firmware_ctrl = *(ulong*)(table_vaddr + 132);
            fadt.x_dsdt = *(ulong*)(table_vaddr + 140);
            fadt.x_pm1a_evt_blk = GenericAddressStructure.Interpret(table_vaddr + 148);
            fadt.x_pm1b_evt_blk = GenericAddressStructure.Interpret(table_vaddr + 160);
            fadt.x_pm1a_cnt_blk = GenericAddressStructure.Interpret(table_vaddr + 172);
            fadt.x_pm1b_cnt_blk = GenericAddressStructure.Interpret(table_vaddr + 184);
            fadt.x_pm2_cnt_blk = GenericAddressStructure.Interpret(table_vaddr + 196);
            fadt.x_pm_tmr_blk = GenericAddressStructure.Interpret(table_vaddr + 208);
            fadt.x_gpe0_blk = GenericAddressStructure.Interpret(table_vaddr + 220);
            fadt.x_gpe1_blk = GenericAddressStructure.Interpret(table_vaddr + 232);
    
            Fadt = fadt;
            tables.Add(fadt);
        }

        public class GenericAddressStructure
        {
            public enum AddressSpace { SystemMemory, SystemIO, PCIConfigSpace, EmbeddedController, SMBus, FunctionalFixedHardware, OEMDefined, Unknown }
            public enum AccessSize { Undefined, Byte, Word, DWord, QWord }

            public AddressSpace Location;
            public AccessSize Size;

            public uint RegisterBitWidth;
            public uint RegisterBitOffset;

            public ulong Address;

            public static GenericAddressStructure Interpret(ulong gas_vaddr)
            {
                GenericAddressStructure ret = new GenericAddressStructure();
                unsafe
                {
                    uint addr_space_id = (uint)*(byte*)gas_vaddr;
                    ret.RegisterBitWidth = (uint)*(byte*)(gas_vaddr + 1);
                    ret.RegisterBitOffset = (uint)*(byte*)(gas_vaddr + 2);
                    uint access_size = (uint)*(byte*)(gas_vaddr + 3);

                    ret.Address = *(ulong*)(gas_vaddr + 4);

                    if (addr_space_id == 0)
                        ret.Location = AddressSpace.SystemMemory;
                    else if (addr_space_id == 1)
                        ret.Location = AddressSpace.SystemIO;
                    else if (addr_space_id == 2)
                        ret.Location = AddressSpace.PCIConfigSpace;
                    else if (addr_space_id == 3)
                        ret.Location = AddressSpace.EmbeddedController;
                    else if (addr_space_id == 4)
                        ret.Location = AddressSpace.SMBus;
                    else if (addr_space_id == 0x7f)
                        ret.Location = AddressSpace.FunctionalFixedHardware;
                    else if (addr_space_id < 0x7f)
                        ret.Location = AddressSpace.Unknown;
                    else if (addr_space_id >= 0xc0)
                        ret.Location = AddressSpace.OEMDefined;
                    else
                        ret.Location = AddressSpace.Unknown;

                    if (access_size == 0)
                        ret.Size = AccessSize.Undefined;
                    else if (access_size == 1)
                        ret.Size = AccessSize.Byte;
                    else if (access_size == 2)
                        ret.Size = AccessSize.Word;
                    else if (access_size == 3)
                        ret.Size = AccessSize.DWord;
                    else if (access_size == 4)
                        ret.Size = AccessSize.QWord;
                }
                return ret;
            }
        }

        public class FixedACPIDescriptionTable : AcpiTable
        {
            public uint _facs_paddr;
            public uint _dsdt_paddr;
            public uint preferred_pm_profile;
            public uint sci_int;
            public uint sci_cmd;
            public uint acpi_enable;
            public uint acpi_disable;
            public uint s4bios_req;
            public uint pstate_cnt;
            public uint _pm1a_evt_blk;
            public uint _pm1b_evt_blk;
            public uint _pm1a_cnt_blk;
            public uint _pm1b_cnt_blk;
            public uint _pm2_cnt_blk;
            public uint _pm_tmr_blk;
            public uint _gpe0_blk;
            public uint _gpe1_blk;
            public uint pm1_evt_len;
            public uint pm1_cnt_len;
            public uint pm2_cnt_len;
            public uint pm_tmr_len;
            public uint gpe0_blk_len;
            public uint gpe1_blk_len;
            public uint gpe1_base;
            public uint cst_cnt;
            public uint p_lvl2_lat;
            public uint p_lvl3_lat;
            public uint flush_size;
            public uint flush_stride;
            public uint duty_offset;
            public uint duty_width;
            public uint day_alrm;
            public uint mon_alrm;
            public uint century;
            public uint iapc_boot_arch;
            public uint flags;
            public GenericAddressStructure reset_reg;
            public uint reset_value;
            public ulong x_firmware_ctrl;
            public ulong x_dsdt;
            public GenericAddressStructure x_pm1a_evt_blk, x_pm1b_evt_blk, x_pm1a_cnt_blk,
                x_pm1b_cnt_blk, x_pm2_cnt_blk, x_pm_tmr_blk, x_gpe0_blk, x_gpe1_blk;

            public bool WBINVD { get { return (flags & 0x1) == 0x1; } }
            public bool WBINVD_FLUSH { get { return (flags & 0x2) == 0x2; } }
            public bool PROC_C1 { get { return (flags & 0x4) == 0x4; } }
            public bool P_LVL2_UP { get { return (flags & 0x8) == 0x8; } }
            public bool PWR_BUTTON { get { return (flags & 0x10) == 0x10; } }
            public bool SLP_BUTTON { get { return (flags & 0x20) == 0x20; } }
            public bool FIX_RTC { get { return (flags & 0x40) == 0x40; } }
            public bool RTC_S4 { get { return (flags & 0x80) == 0x80; } }
            public bool TMR_VAL_EXT { get { return (flags & 0x100) == 0x100; } }
            public bool DCK_CAP { get { return (flags & 0x200) == 0x200; } }
            public bool RESET_REG_SUP { get { return (flags & 0x400) == 0x400; } }
            public bool SEALED_CASE { get { return (flags & 0x800) == 0x800; } }
            public bool HEADLESS { get { return (flags & 0x1000) == 0x1000; } }
            public bool CPU_SW_SLP { get { return (flags & 0x2000) == 0x2000; } }
            public bool PCI_EXP_WAK { get { return (flags & 0x4000) == 0x4000; } }
            public bool USE_PLATFORM_CLOCK { get { return (flags & 0x8000) == 0x8000; } }
            public bool S4_RTC_STS_VALID { get { return (flags & 0x10000) == 0x10000; } }
            public bool REMOTE_POWER_ON_CAPABLE { get { return (flags & 0x20000) == 0x20000; } }
            public bool FORCE_APIC_CLUSTER_MODEL { get { return (flags & 0x40000) == 0x40000; } }
            public bool FORCE_APIC_PHYSICAL_DESTINATION_MODE { get { return (flags & 0x80000) == 0x80000; } }

            public bool LEGACY_DEVICES { get { return (iapc_boot_arch & 0x1) == 0x1; } }
            public bool HAS8042 { get { return (iapc_boot_arch & 0x2) == 0x2; } }
            public bool VGA_NOT_PRESENT { get { return (iapc_boot_arch & 0x4) == 0x4; } }
            public bool MSI_NOT_SUPPORTED { get { return (iapc_boot_arch & 0x8) == 0x8; } }
            public bool PCIE_ASPM_CONTROLS { get { return (iapc_boot_arch & 0x10) == 0x10; } }

            public ulong firmware_ctrl { get { if (_facs_paddr == 0x0) return x_firmware_ctrl; else return (ulong)_facs_paddr; } }
            public ulong dsdt { get { if (_dsdt_paddr == 0x0) return x_dsdt; else return (ulong)_dsdt_paddr; } }
        }

        public class SsdtTable : AcpiTable
        {
            public ulong ssdt_vaddr;
        }

        public class ApicTable : AcpiTable
        {
            public uint lapic_paddr;
            public uint flags;

            public bool Has8259 { get { return (flags & 0x1) == 0x1; } }

            public const int TYPE_LAPIC = 0;
            public const int TYPE_IOAPIC = 1;
            public const int TYPE_INTERRUPT_SOURCE_OVERRIDE = 2;
            public const int TYPE_NMI = 3;
            public const int TYPE_LAPIC_NMI = 4;
            public const int TYPE_LAPIC_ADDR_OVERRIDE = 5;
            public const int TYPE_IOSAPIC = 6;
            public const int TYPE_LSAPIC = 7;
            public const int TYPE_PLATFORM_INTERRUPT_SOURCES = 8;
            public const int TYPE_LX2APIC = 9;

            public List<IOAPICStructure> IOAPICs;
            public List<IOAPICStructure> IOSAPICs;
            public List<LAPICStructure> LAPICs;
            public List<ISOStructure> InterruptSourceOverrides;
            public List<NMIStructure> NMIs;
            public List<LAPICNMIStructure> LAPICNMIs;

            public class APICStructure
            {
                public int type;
                public int length;
            }

            public class LAPICStructure : APICStructure
            {
                public int proc_id;
                public int lapic_id;
                public uint flags;

                public bool Enabled { get { return (flags & 0x1) == 0x1; } }
            }

            public class IOAPICStructure : APICStructure
            {
                public int ioapic_id;
                public ulong ioapic_paddr;
                public uint global_system_interrupt_base;
            }

            public class MPSINTIFlagsStructure : APICStructure
            {
                public uint flags;

                public enum PolarityType { Bus, ActiveHigh, ActiveLow }
                public enum TriggerModeType { Bus, Edge, Level }

                public PolarityType Polarity
                {
                    get
                    {
                        if ((flags & 3) == 0x0) return PolarityType.Bus;
                        else if ((flags & 0x3) == 0x1) return PolarityType.ActiveHigh;
                        else if ((flags & 0x3) == 0x3) return PolarityType.ActiveLow;
                        else throw new Exception("Invalid Polarity type");
                    }
                }

                public TriggerModeType TriggerMode
                {
                    get
                    {
                        if ((flags & 0xc) == 0x0) return TriggerModeType.Bus;
                        else if ((flags & 0xc) == 0x4) return TriggerModeType.Edge;
                        else if ((flags & 0xc) == 0xc) return TriggerModeType.Level;
                        else throw new Exception("Invalid TriggerMode type");
                    }
                }
            }

            public class ISOStructure : MPSINTIFlagsStructure
            {
                public enum BusType { ISA };
                public BusType bus;

                public int source;
                public uint global_system_interrupt;
            }

            public class NMIStructure : MPSINTIFlagsStructure
            {
                public uint global_system_interrupt;
            }

            public class LAPICNMIStructure : MPSINTIFlagsStructure
            {
                public int proc_id;
                public int lapic_lint;
            }
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
