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

namespace tysos
{
    class SmBios
    {
        ulong smbios_entry_point_vaddr;
        const ulong smbios_section_base_paddr = 0xf0000;
        const ulong smbios_section_length = 0x10000;

        List<SMBiosInfoEntry> info_entries;

        public SmBios(Virtual_Regions vreg, VirtMem vmem)
        {
            /* To find the smbios structure, we traverse the physical memory from 0xf0000 to 0xfffff
             * on 16 byte boundaries looking for the string "_SM_" (or uint 0x5f4d535f)
             */

            ulong smbios_section_base_vaddr = vreg.Alloc(smbios_section_length, 0x1000, "SMBiosSearch");
            for(ulong i = 0; i < smbios_section_length; i+= 0x1000)
                vmem.map_page(smbios_section_base_vaddr + i, smbios_section_base_paddr + i);

            smbios_entry_point_vaddr = smbios_section_base_vaddr;
            bool found = false;
            unsafe
            {
                while (smbios_entry_point_vaddr < (smbios_section_base_vaddr + smbios_section_length))
                {
                    uint test_val = *(uint*)smbios_entry_point_vaddr;
                    if (test_val == 0x5f4d535f)
                    {
                        found = true;
                        break;
                    }

                    smbios_entry_point_vaddr += 16;
                }
            }

            if (!found)
                throw new Exception("SMBios information not found");

            Formatter.Write("SMBios structure found at: ", Program.arch.DebugOutput);
            Formatter.Write(smbios_entry_point_vaddr - smbios_section_base_vaddr + smbios_section_base_paddr, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /* Now we can identify the physical address of the actual SMBios information table */
            ulong smbios_info_paddr;
            ulong smbios_info_length;
            unsafe
            {
                smbios_info_length = (ulong)*((ushort*)(smbios_entry_point_vaddr + 0x16));      // WORD structure table length at offset 0x16
                smbios_info_paddr = (ulong)*((uint *)(smbios_entry_point_vaddr + 0x18));        // DWORD structure table address at offset 0x18
            }

            /* Map in the region */
            ulong smbios_vaddr = Program.map_in(smbios_info_paddr, smbios_info_length, "SMBios");

            /* Debug dump of the area */
            Formatter.Write("SMBIOS info struct: paddr: ", Program.arch.DebugOutput);
            Formatter.Write(smbios_info_paddr, "X", Program.arch.DebugOutput);
            Formatter.Write(" length: ", Program.arch.DebugOutput);
            Formatter.Write(smbios_info_length, "X", Program.arch.DebugOutput);
            Formatter.Write(" vaddr: ", Program.arch.DebugOutput);
            Formatter.Write(smbios_vaddr, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            Formatter.WriteLine("SMBios area dump:", Program.arch.DebugOutput);
            ulong dump_addr = smbios_vaddr;
            unsafe
            {
                while (dump_addr < (smbios_vaddr + smbios_info_length))
                {
                    ulong* val = (ulong*)dump_addr;
                    Formatter.Write(*val, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    dump_addr += 8;
                }
            }

            /* Now parse the info region */
            info_entries = new List<SMBiosInfoEntry>();

            ulong cur_vaddr = smbios_vaddr;
            unsafe
            {
                while (cur_vaddr < (smbios_vaddr + smbios_info_length))
                {
                    SMBiosInfoEntry cur_ent = new SMBiosInfoEntry();

                    cur_ent.start_vaddr = cur_vaddr;
                    cur_ent.type = *(byte*)(cur_vaddr + 0x0);                       // BYTE type field at offset 0
                    cur_ent.data_length = (ulong)*(byte*)(cur_vaddr + 0x1);         // BYTE length at offset 1
                    cur_ent.handle = *(ushort*)(cur_vaddr + 0x2);                   // WORD handle at offset 2

                    /* Now parse the strings, if any
                     * 
                     * The strings are null terminated characters
                     * The string table itself is terminated by a double null byte
                     */
                    cur_vaddr += cur_ent.data_length;
                    cur_ent.strings = new List<string>();

                    if (*(ushort*)cur_vaddr == 0x0000)
                        cur_vaddr += 2;
                    else
                    {
                        do
                        {
                            string s = new string((sbyte*)cur_vaddr);
                            cur_ent.strings.Add(s);
                            cur_vaddr += (ulong)(s.Length + 1);     // skip on the length of the string and the terminating null
                        } while (*(byte*)cur_vaddr != 0x00);        // cur_vaddr should point to the next string, if instead it points to 0 we are at the end of the strings
                        cur_vaddr++;                                // skip onto the next info structure
                    }

                    info_entries.Add(cur_ent);
                }
            }

            /* dump the entries */
            foreach (SMBiosInfoEntry info_entry in info_entries)
            {
                Formatter.Write("SMBIOS entry type: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)info_entry.type, "d", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
            }
        }

        class SMBiosInfoEntry
        {
            public ulong start_vaddr;
            public byte type;
            public ushort handle;
            public ulong data_length;
            public List<string> strings;
        }
    }
}
