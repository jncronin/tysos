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
using tysos.Messages;

namespace acpipc
{
    class Table
    {
        string signature;
        tysos.VirtualMemoryResource64 vmem;

        internal static Table InterpretTable(tysos.VirtualMemoryResource64 table,
            acpipc a)
        {
            Table ret;
            char[] sig = new char[4];

            sig[0] = (char)table.Read(table.Addr64, 1);
            sig[1] = (char)table.Read(table.Addr64 + 1, 1);
            sig[2] = (char)table.Read(table.Addr64 + 2, 1);
            sig[3] = (char)table.Read(table.Addr64 + 3, 1);

            string sig_string = new string(sig);
            System.Diagnostics.Debugger.Log(0, "acpipc", "Found table with signature " +
                sig_string);

            // Check checksum
            uint length = (uint)table.Read(table.Addr64 + 4, 4);
            byte csum = 0;
            unchecked
            {
                for(uint i = 0; i < length; i++)
                {
                    csum += (byte)table.Read(table.Addr64 + i, 1);
                }
            }

            if (csum == 0)
                System.Diagnostics.Debugger.Log(0, "acpipc", "table checksum passed");
            else
                System.Diagnostics.Debugger.Log(0, "acpipc", "table checksum failed: " +
                    csum.ToString());

            if (sig_string == "FACP")
            {
                FADT f = new FADT();

                f.FIRMWARE_CTRL = table.Read(table.Addr64 + 132, 8);
                if (f.FIRMWARE_CTRL == 0)
                    f.FIRMWARE_CTRL = table.Read(table.Addr64 + 36, 4);
                f.DSDT = table.Read(table.Addr64 + 140, 8);
                if (f.DSDT == 0)
                    f.DSDT = table.Read(table.Addr64 + 40, 4);

                f.Preferred_PM_Profile = (byte)table.Read(table.Addr64 + 45, 1);
                f.SCI_INT = (ushort)table.Read(table.Addr64 + 46, 2);
                f.SMI_CMD = (uint)table.Read(table.Addr64 + 48, 4);
                f.ACPI_ENABLE = (byte)table.Read(table.Addr64 + 52, 1);
                f.ACPI_DISABLE = (byte)table.Read(table.Addr64 + 53, 1);
                f.S4BIOS_REQ = (byte)table.Read(table.Addr64 + 54, 1);
                f.PSTATE_CNT = (byte)table.Read(table.Addr64 + 55, 1);
                f.PM1_EVT_LEN = (uint)table.Read(table.Addr64 + 88, 1);
                f.PM1_CNT_LEN = (uint)table.Read(table.Addr64 + 89, 1);
                f.PM2_CNT_LEN = (uint)table.Read(table.Addr64 + 90, 1);
                f.PM_TMR_LEN = (uint)table.Read(table.Addr64 + 91, 1);
                f.GPE0_BLK_LEN = (uint)table.Read(table.Addr64 + 92, 1);
                f.GPE1_BLK_LEN = (uint)table.Read(table.Addr64 + 93, 1);
                f.GPE1_BASE = (uint)table.Read(table.Addr64 + 94, 1);

                f.PM1a_EVT_BLK = new GAS((uint)table.Read(table.Addr64 + 56, 4), f.PM1_EVT_LEN * 8, a);
                f.PM1b_EVT_BLK = new GAS((uint)table.Read(table.Addr64 + 60, 4), f.PM1_EVT_LEN * 8, a);
                f.PM1a_CNT_BLK = new GAS((uint)table.Read(table.Addr64 + 64, 4), f.PM1_CNT_LEN * 8, a);
                f.PM1b_CNT_BLK = new GAS((uint)table.Read(table.Addr64 + 68, 4), f.PM1_CNT_LEN * 8, a);
                f.PM2_CNT_BLK = new GAS((uint)table.Read(table.Addr64 + 72, 4), f.PM2_CNT_LEN * 8, a);
                f.PM_TMR_BLK = new GAS((uint)table.Read(table.Addr64 + 76, 4), f.PM_TMR_LEN * 8, a);
                f.GPE0_BLK = new GAS((uint)table.Read(table.Addr64 + 76, 4), f.GPE0_BLK_LEN * 8, a);
                f.GPE1_BLK = new GAS((uint)table.Read(table.Addr64 + 76, 4), f.GPE1_BLK_LEN * 8, a);

                if (table.Read(table.Addr64 + 152, 8) != 0)
                    f.PM1a_EVT_BLK = new GAS(table, 148, a);
                if (table.Read(table.Addr64 + 164, 8) != 0)
                    f.PM1b_EVT_BLK = new GAS(table, 160, a);
                if (table.Read(table.Addr64 + 176, 8) != 0)
                    f.PM1a_CNT_BLK = new GAS(table, 172, a);
                if (table.Read(table.Addr64 + 188, 8) != 0)
                    f.PM1b_CNT_BLK = new GAS(table, 184, a);
                if (table.Read(table.Addr64 + 200, 8) != 0)
                    f.PM2_CNT_BLK = new GAS(table, 196, a);
                if (table.Read(table.Addr64 + 212, 8) != 0)
                    f.PM_TMR_BLK = new GAS(table, 208, a);
                if (table.Read(table.Addr64 + 224, 8) != 0)
                    f.GPE0_BLK = new GAS(table, 220, a);
                if (table.Read(table.Addr64 + 236, 8) != 0)
                    f.GPE1_BLK = new GAS(table, 232, a);

                f.PM1_EVT = new RegGroup(f.PM1a_EVT_BLK, f.PM1b_EVT_BLK);
                f.PM1_CNT = new RegGroup(f.PM1a_CNT_BLK, f.PM1b_CNT_BLK);

                f.PM1_STS = new SplitReg(f.PM1_EVT, 0, f.PM1a_EVT_BLK.BitWidth / 2);
                f.PM1_EN = new SplitReg(f.PM1_EVT, f.PM1a_EVT_BLK.BitWidth / 2,
                    f.PM1a_EVT_BLK.BitWidth / 2);

                f.GPE0_STS = new SplitReg(f.GPE0_BLK, 0, (int)f.GPE0_BLK_LEN * 4);
                f.GPE0_EN = new SplitReg(f.GPE0_BLK, (int)f.GPE0_BLK_LEN * 4,
                    (int)f.GPE0_BLK_LEN * 4);
                f.GPE1_STS = new SplitReg(f.GPE1_BLK, 0, (int)f.GPE1_BLK_LEN * 4);
                f.GPE1_EN = new SplitReg(f.GPE1_BLK, (int)f.GPE1_BLK_LEN * 4,
                    (int)f.GPE1_BLK_LEN * 4);

                f.IAPC_BOOT_ARCH = (ushort)table.Read(table.Addr64 + 109, 2);
                f.Flags = (uint)table.Read(table.Addr64 + 112, 4);

                System.Diagnostics.Debugger.Log(0, "acpipc", "FADT table: DSDT: " +
                    f.DSDT.ToString("X16") +
                    ", FIRMWARE_CTRL: " + f.FIRMWARE_CTRL.ToString("X16") +
                    ", Preferred_PM_Profile: " + f.Preferred_PM_Profile.ToString() +
                    ", IAPC_BOOT_ARCH: " + f.IAPC_BOOT_ARCH.ToString("X4") +
                    ", Flags: " + f.Flags.ToString("X8") + "\n");

                /* Store the DSDT details */
                a.p_dsdt_addr = f.DSDT;
                a.dsdt_len = 36;    // changed later after the DSDT is loaded

                ret = f;
                a.fadt = f;
            }
            else if (sig_string == "APIC")
            {
                APIC atbl = new APIC();

                atbl.LocalAPICAddress = (ulong)table.Read(table.Addr64 + 36, 4);
                atbl.Flags = (uint)table.Read(table.Addr64 + 40, 4);
                atbl.APICs = new List<APICStructure>();

                System.Diagnostics.Debugger.Log(0, "acpipc", "ACPI table: LocalAPICAddress: " +
                    atbl.LocalAPICAddress.ToString("X16") +
                    ", Flags: " + atbl.Flags.ToString("X8") + "\n");

                uint cur_addr = 44;
                while (cur_addr < length)
                {
                    APICStructure s;

                    uint type = (uint)table.Read(table.Addr64 + cur_addr, 1);
                    uint s_length = (uint)table.Read(table.Addr64 + cur_addr + 1, 1);

                    if (type == 0)
                    {
                        LocalAPICStructure las = new LocalAPICStructure();
                        las.ACPIProcessorID = (uint)table.Read(table.Addr64 + cur_addr + 2, 1);
                        las.APICID = (uint)table.Read(table.Addr64 + cur_addr + 3, 1);
                        las.Flags = (uint)table.Read(table.Addr64 + cur_addr + 4, 4);

                        System.Diagnostics.Debugger.Log(0, "acpipc", "LocalAPICStructure: " +
                            "ACPIProcessorID: " + las.ACPIProcessorID.ToString("X8") +
                            ", APICID: " + las.APICID.ToString("X8") +
                            ", Flags: " + las.Flags.ToString("X8") + "\n");

                        s = las;
                    }
                    else if (type == 1)
                    {
                        IOAPICStructure ias = new IOAPICStructure();
                        ias.IOAPICID = (uint)table.Read(table.Addr64 + cur_addr + 2, 1);
                        ias.IOAPICAddress = (ulong)table.Read(table.Addr64 + cur_addr + 4, 4);
                        ias.GSIBase = (uint)table.Read(table.Addr64 + cur_addr + 8, 4);

                        System.Diagnostics.Debugger.Log(0, "acpipc", "IOAPICStructure: " +
                            "IOAPICID: " + ias.IOAPICID.ToString("X8") +
                            ", IOAPICAddress: " + ias.IOAPICAddress.ToString("X16") +
                            ", GSIBase: " + ias.GSIBase.ToString("X8") + "\n");

                        s = ias;
                    }
                    else if (type == 2)
                    {
                        InterruptSourceOverrideStructure iso = new InterruptSourceOverrideStructure();
                        iso.Bus = (int)table.Read(table.Addr64 + cur_addr + 2, 1);
                        iso.Source = (int)table.Read(table.Addr64 + cur_addr + 3, 1);
                        iso.GSI = (uint)table.Read(table.Addr64 + cur_addr + 4, 4);
                        iso.Flags = (uint)table.Read(table.Addr64 + cur_addr + 8, 2);

                        System.Diagnostics.Debugger.Log(0, "acpipc", "InterruptSourceOverrideStructure: " +
                            "Bus: " + iso.Bus.ToString("X8") +
                            ", Source: " + iso.Source.ToString("X16") +
                            ", GSI: " + iso.GSI.ToString("X8") +
                            ", Flags: " + iso.Flags.ToString("X8") + "\n");

                        s = iso;
                    }
                    else
                    {
                        System.Diagnostics.Debugger.Log(0, "acpipc", "APICStructure: unsupported type: " + type.ToString() + "\n");

                        s = new APICStructure();
                    }

                    s.Length = (int)s_length;
                    s.Type = (int)type;

                    atbl.APICs.Add(s);

                    cur_addr += s_length;
                }

                ret = atbl;
            }
            else if (sig_string == "SSDT")
            {
                ret = new Table();

                ulong ssdt_vaddr = table.Addr64;
                ulong ssdt_length = length;

                tysos.VirtualMemoryResource64 ssdt_region = table.Split(ssdt_vaddr, ssdt_length)
                    as tysos.VirtualMemoryResource64;

                System.Diagnostics.Debugger.Log(0, "acpipc", "ACPI table: SSDT: " +
                    ssdt_vaddr.ToString("X16") + " - " + (ssdt_vaddr + ssdt_length).ToString("X16"));

                a.ssdts.Add(ssdt_region);
            }
            else
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "ACPI table: " + sig_string);

                ret = new Table();
            }

            ret.signature = sig_string;
            ret.vmem = table;

            return ret;
        }
    }

    class FADT : Table
    {
        public ulong FIRMWARE_CTRL;
        public ulong DSDT;
        public byte Preferred_PM_Profile;
        public ushort SCI_INT;
        public ulong SMI_CMD;
        public byte ACPI_ENABLE;
        public byte ACPI_DISABLE;
        public byte S4BIOS_REQ;
        public byte PSTATE_CNT;

        public uint PM1_EVT_LEN;
        public uint PM1_CNT_LEN;
        public uint PM2_CNT_LEN;
        public uint PM_TMR_LEN;
        public uint GPE0_BLK_LEN;
        public uint GPE1_BLK_LEN;
        public uint GPE1_BASE;
        public uint CST_CNT;

        public GAS PM1a_EVT_BLK;
        public GAS PM1b_EVT_BLK;
        public GAS PM1a_CNT_BLK;
        public GAS PM1b_CNT_BLK;
        public GAS PM2_CNT_BLK;
        public GAS PM_TMR_BLK;
        public GAS GPE0_BLK;
        public GAS GPE1_BLK;

        public RegGroup PM1_EVT;
        public RegGroup PM1_CNT;

        public SplitReg PM1_STS;
        public SplitReg PM1_EN;

        public SplitReg GPE0_STS;
        public SplitReg GPE0_EN;
        public SplitReg GPE1_STS;
        public SplitReg GPE1_EN;

        public ushort IAPC_BOOT_ARCH;
        public uint Flags;
    }

    class APIC : Table
    {
        public ulong LocalAPICAddress;
        public uint Flags;

        public List<APICStructure> APICs;
    }

    class APICStructure : Table
    {
        public int Type;
        public int Length;
    }

    class LocalAPICStructure : APICStructure
    {
        public uint ACPIProcessorID;
        public uint APICID;
        public uint Flags;
    }

    class IOAPICStructure : APICStructure
    {
        public uint IOAPICID;
        public ulong IOAPICAddress;
        public uint GSIBase;
    }

    class InterruptSourceOverrideStructure : APICStructure
    {
        public int Bus;
        public int Source;
        public uint GSI;
        public uint Flags;
    }
}
