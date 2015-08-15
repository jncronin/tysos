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
                while(cur_addr < length)
                {
                    APICStructure s;

                    uint type = (uint)table.Read(table.Addr64 + cur_addr, 1);
                    uint s_length = (uint)table.Read(table.Addr64 + cur_addr + 1, 1);

                    if (type == 0)
                    {
                        LocalAPICStructure las = new LocalAPICStructure();
                        las.APICProcessorID = (uint)table.Read(table.Addr64 + cur_addr + 2, 1);
                        las.APICID = (uint)table.Read(table.Addr64 + cur_addr + 3, 1);
                        las.Flags = (uint)table.Read(table.Addr64 + cur_addr + 4, 4);

                        System.Diagnostics.Debugger.Log(0, "acpipc", "LocalAPICStructure: " +
                            "APICProcessorID: " + las.APICProcessorID.ToString("X8") +
                            ", APICID: " + las.APICProcessorID.ToString("X8") +
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

                    cur_addr += s_length;
                }

                ret = atbl;
            }
            else if(sig_string == "SSDT")
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
                ret = new Table();

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

        // TODO PM/GPE blocks

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
        public uint APICProcessorID;
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
