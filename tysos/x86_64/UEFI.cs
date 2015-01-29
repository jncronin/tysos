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

namespace tysos.x86_64
{
    unsafe class UEFI : Arch.FirmwareConfiguration
    {
        public const int UEFIHeaderSize = 24;
        public const ulong UEFISystemTableSignature = 0x5453595320494249;

        void* acpi_20_table = null;
        void* acpi_10_table = null;
        void* smbios_table = null;

        public const ulong ACPI20GUID1 = 0x11D3E4F18868E871;
        public const ulong ACPI20GUID2 = 0x81883CC7800022BC;

        public const ulong ACPI10GUID1 = 0x11D32D88EB9D2D30;
        public const ulong ACPI10GUID2 = 0x4DC13F279000169A;

        public const ulong SMBIOSGUID1 = 0x11D32D88EB9D2D31;
        public const ulong SMBIOSGUID2 = 0x4DC13F279000169A;

        public unsafe UEFI(Virtual_Regions vreg, VirtMem vmem, ulong system_table_paddr)
        {
            /* The following assumes a 64-bit platform */
            Formatter.Write("UEFI: system table at paddr: ",
                Program.arch.DebugOutput);
            Formatter.Write(system_table_paddr, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Load up the system table */
            ulong st_vaddr = Program.map_in(system_table_paddr, UEFIHeaderSize, "UEFI ST Header");
            ulong sig = *(ulong*)st_vaddr;
            uint revision = *(uint*)(st_vaddr + 8);
            uint hdr_size = *(uint*)(st_vaddr + 12);
            uint crc32 = *(uint*)(st_vaddr + 16);
            uint reserved = *(uint*)(st_vaddr + 20);

            Formatter.Write("UEFI: system table signature: ", Program.arch.DebugOutput);
            Formatter.Write(sig, "X", Program.arch.DebugOutput);
            Formatter.Write(", revision: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)revision, "X", Program.arch.DebugOutput);
            Formatter.Write(", header size: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)hdr_size, Program.arch.DebugOutput);
            Formatter.Write(", crc32: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)crc32, "X", Program.arch.DebugOutput);
            Formatter.Write(", reserved: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)reserved, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            if (sig != UEFISystemTableSignature)
                throw new Exception("UEFI system table bad signature: " +
                    sig.ToString("X16"));

            st_vaddr = Program.map_in(system_table_paddr, hdr_size, "UEFI ST");

            /* Get the configuration table pointer */
            ulong NumberOfTableEntries = *(ulong*)(st_vaddr + 104);
            ulong ConfigurationTable = *(ulong *)(st_vaddr + 112);

            Formatter.Write("UEFI: NumberOfTableEntries: ", Program.arch.DebugOutput);
            Formatter.Write(NumberOfTableEntries, Program.arch.DebugOutput);
            Formatter.Write(", ConfigurationTable: ", Program.arch.DebugOutput);
            Formatter.Write(ConfigurationTable, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Load the configuration table pointer array.  This is an array of
             * struct {
             *      EFI_GUID            VendorGuid;
             *      VOID *              VendorTable;
             * };
             * located at address ConfigurationTable.
             * 
             * NB EFI_GUID is a 128 bit buffer
             */

            ulong cfg_vaddr = Program.map_in(ConfigurationTable, NumberOfTableEntries * 24,
                "UEFI CfgTableArray");

            for (ulong i = 0; i < NumberOfTableEntries; i++)
            {
                ulong guid1 = *(ulong*)(cfg_vaddr + i * 24);
                ulong guid2 = *(ulong*)(cfg_vaddr + i * 24 + 8);
                ulong table = *(ulong*)(cfg_vaddr + i * 24 + 16);

                Formatter.Write("UEFI: Table GUID: ", Program.arch.DebugOutput);
                Formatter.Write(guid1, "X", Program.arch.DebugOutput);
                Formatter.Write(guid2, "X", Program.arch.DebugOutput);
                Formatter.Write(", Address: ", Program.arch.DebugOutput);
                Formatter.Write(table, "X", Program.arch.DebugOutput);

                if(guid1 == ACPI10GUID1 && guid2 == ACPI10GUID2)
                {
                    Formatter.Write(" (ACPI_10_TABLE)", Program.arch.DebugOutput);
                    acpi_10_table = (void*)table;
                }
                if(guid1 == ACPI20GUID1 && guid2 == ACPI20GUID2)
                {
                    Formatter.Write(" (ACPI_20_TABLE)", Program.arch.DebugOutput);
                    acpi_20_table = (void*)table;
                }
                if(guid1 == SMBIOSGUID1 && guid2 == SMBIOSGUID2)
                {
                    Formatter.Write(" (SMBIOS_TABLE)", Program.arch.DebugOutput);
                    smbios_table = (void*)table;
                }
                Formatter.WriteLine(Program.arch.DebugOutput);
            }
        }

        internal override void* ACPI_20_table
        {
            get { return acpi_20_table; }
        }

        internal override void* ACPI_10_table
        {
            get { return acpi_10_table; }
        }

        internal override void* SMBIOS_table
        {
            get { return smbios_table; }
        }
    }
}
