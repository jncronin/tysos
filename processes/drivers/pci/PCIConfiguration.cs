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
using tysos.lib;

/* Encapsulate access to PCI configuration space for a particular device
as a Resouce object */

namespace pci
{
    public class PCIConfiguration : tysos.Resource
    {
        tysos.x86_64.IOResource CONFIG_ADDRESS, CONFIG_DATA;
        internal int bus, dev, func;
        internal hostbridge hb;

        internal PCIConfiguration(tysos.x86_64.IOResource config_address,
            tysos.x86_64.IOResource config_data, int _bus, int _dev, int _func,
            hostbridge hostbridge)
        {
            CONFIG_ADDRESS = config_address;
            CONFIG_DATA = config_data;
            bus = _bus;
            dev = _dev;
            func = _func;
            hb = hostbridge;
        }

        public uint ReadConfig(int reg_no)
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

        public void WriteConfig(int reg_no, uint val)
        {
            /* Disallow write access to BARs - these have to be remapped
            by the pci driver itself via a call to GetBAR to ensure a
            driver does not arbritrarily grant its device write access to
            any part of memory */
            if (reg_no >= 0x10 && reg_no < 0x28)
                return;

            uint address = (uint)reg_no;
            address &= 0xfc;        // zero out lower 2 bits
            address |= ((uint)func & 0x7U) << 8;
            address |= ((uint)dev & 0x1fU) << 11;
            address |= ((uint)bus & 0xffU) << 16;
            address |= 0x80000000U;

            CONFIG_ADDRESS.Write(CONFIG_ADDRESS.Addr32, 4, address);
            CONFIG_DATA.Write(CONFIG_DATA.Addr32, 4, val);
        }

        public tysos.RangeResource GetBAR(int bar_no)
        {
            return hb.Invoke("GetBAR", new object[] { this, bar_no },
                new Type[] { typeof(PCIConfiguration), typeof(int) })
                as tysos.RangeResource;
        }
    }
}
