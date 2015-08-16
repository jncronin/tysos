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

namespace pci
{
    partial class hostbridge : tysos.lib.VirtualDirectoryServer
    {
        internal List<tysos.VirtualMemoryResource64> vmems = new List<tysos.VirtualMemoryResource64>();
        internal List<tysos.PhysicalMemoryResource64> pmems = new List<tysos.PhysicalMemoryResource64>();
        internal List<tysos.x86_64.IOResource> ios = new List<tysos.x86_64.IOResource>();

        internal tysos.x86_64.IOResource CONFIG_ADDRESS, CONFIG_DATA;

        public hostbridge(tysos.lib.File.Property[] Properties)
        {
            root = new List<tysos.lib.File.Property>(Properties);
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "pci", "hostbridge started with " + root.Count.ToString() + " properties");

            /* Extract the resources we have been given */
            foreach (tysos.lib.File.Property p in root)
            {
                if (p.Name == "vmem")
                {
                    System.Diagnostics.Debugger.Log(0, "pci", "adding vmem area");
                    vmems.Add(p.Value as tysos.VirtualMemoryResource64);
                }
                else if (p.Name == "pmem")
                {
                    System.Diagnostics.Debugger.Log(0, "pci", "adding pmem area");
                    pmems.Add(p.Value as tysos.PhysicalMemoryResource64);
                }
                else if (p.Name == "io")
                {
                    System.Diagnostics.Debugger.Log(0, "pci", "adding io area");
                    ios.Add(p.Value as tysos.x86_64.IOResource);
                }
            }

            /* Get CONFIG_DATA and CONFIG_ADDRESS ports */
            CONFIG_ADDRESS = AllocIOFixed(0xcf8, 4);
            CONFIG_DATA = AllocIOFixed(0xcfc, 4);
            if (CONFIG_ADDRESS == null)
                throw new Exception("Unable to obtain CONFIG_ADDRESS");
            if (CONFIG_DATA == null)
                throw new Exception("Unable to obtain CONFIG_DATA");

            /* Enumerate the devices on this bus */
            for(int dev = 0; dev < 32; dev++)
            {
                CheckDevice(0, dev, 0);
            }

            return true;
        }

        private void CheckDevice(int bus, int dev, int func)
        {
            uint vendor_devID = ReadConfig(bus, dev, func, 0);
            uint vendorID = vendor_devID & 0xffff;
            if (vendorID == 0xffff)
                return;

            /* Get the basic fields of the device */
            uint command_status = ReadConfig(bus, dev, func, 4);
            uint revID_progIF_subclass_class = ReadConfig(bus, dev, func, 8);
            uint cacheline_latency_ht_bist = ReadConfig(bus, dev, func, 0xc);

            /* We have found a valid device */
            uint deviceID = vendor_devID >> 16;
            uint classcode = (revID_progIF_subclass_class >> 24) & 0xffU;
            uint subclasscode = (revID_progIF_subclass_class >> 16) & 0xffU;
            uint prog_IF = (revID_progIF_subclass_class >> 8) & 0xffU;

            System.Diagnostics.Debugger.Log(0, "pci", bus.ToString() + ":" + dev.ToString() + ":" + func.ToString() + " : " + vendorID.ToString("X4") + ":" + deviceID.ToString("X4") + "   " + classcode.ToString("X2") + ":" + subclasscode.ToString("X2") + ":" + prog_IF.ToString("X2"));

            uint header_type = (cacheline_latency_ht_bist >> 16) & 0xffU;
            if((header_type & 0x7f) == 0)
            {
                /* It is a device.  Get BARs */
                for(int i = 0; i < 6; i++)
                {
                    int bar_addr = 0x10 + i * 4;
                    uint orig_bar = ReadConfig(bus, dev, func, bar_addr);

                    // Write all 1s to get the length of the region requested
                    WriteConfig(bus, dev, func, bar_addr, 0xffffffffU);
                    uint bar_len = ReadConfig(bus, dev, func, bar_addr);
                    unchecked
                    {
                        bar_len = ~bar_len;
                        bar_len++;
                    }

                    // Write the original value back
                    WriteConfig(bus, dev, func, bar_addr, orig_bar);

                    System.Diagnostics.Debugger.Log(0, "pci", "  " + orig_bar.ToString("X8") + ", " + bar_len.ToString("X8"));
                }
            }


            /* If header type == 0x80, it is a multifunction device */
            if (header_type == 0x80 && func == 0)
            {
                for (int subfunc = 1; subfunc < 8; subfunc++)
                    CheckDevice(bus, dev, subfunc);
            }
        }

        uint ReadConfig(int bus, int dev, int func, int reg_no)
        {
            uint address = (uint)reg_no;
            address &= 0xfc;        // zero out lower 2 bits
            address |= ((uint)func & 0x7U) << 8;
            address |= ((uint)dev & 0x1fU) << 11;
            address |= ((uint)bus & 0xffU) << 16;
            address |= 0x80000000U;

            CONFIG_ADDRESS.Write(CONFIG_ADDRESS.Addr32, 4, address);
            return CONFIG_DATA.Read(CONFIG_DATA.Addr32, 4);
        }

        void WriteConfig(int bus, int dev, int func, int reg_no, uint val)
        {
            uint address = (uint)reg_no;
            address &= 0xfc;        // zero out lower 2 bits
            address |= ((uint)func & 0x7U) << 8;
            address |= ((uint)dev & 0x1fU) << 11;
            address |= ((uint)bus & 0xffU) << 16;
            address |= 0x80000000U;

            CONFIG_ADDRESS.Write(CONFIG_ADDRESS.Addr32, 4, address);
            CONFIG_DATA.Write(CONFIG_DATA.Addr32, 4, val);
        }
    }
}
