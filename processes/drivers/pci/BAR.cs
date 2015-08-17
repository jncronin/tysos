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
    partial class hostbridge
    {
        public tysos.RangeResource GetBAR(PCIConfiguration conf, int bar_no)
        {
            /* Get the requested BAR of the device passed.
            If its base is zero, but length is not, we have to allocate it
            somewhere */

            /* Ensure the device is enumerated by this bridge */
            if (conf.hb != this)
                return null;

            /* Ensure its a valid BAR index.  For header type 0, max_bar is 6,
            for header 1 its 2 and for others its 0 */
            uint header_type = ReadConfig(conf.bus, conf.dev, conf.func,
                0xc);
            header_type >>= 16;
            header_type &= 0xffU;
            int max_bar = 0;
            if (header_type == 0)
                max_bar = 6;
            else if (header_type == 1)
                max_bar = 2;

            if (bar_no >= max_bar)
                return null;

            /* If bar_no >= 1, read the previous BAR to ensure we're not
            trying to read half way into a 64-bit one */
            if(bar_no >= 1)
            {
                uint prev_bar = ReadConfig(conf.bus, conf.dev, conf.func,
                    (bar_no - 1) * 4 + 0x10);

                if((prev_bar & 0x7) == 0x4)
                {
                    /* Previous bar is a 64 bit memory register */
                    return null;
                }
            }

            /* Read the current value of the bar */
            int bar_addr = bar_no * 4 + 0x10;
            uint bar = ReadConfig(conf.bus, conf.dev, conf.func, bar_addr);

            if((bar & 0x1) == 0)
            {
                // Memory register
                ulong base_addr = 0;
                ulong length = 0;
                switch (bar & 0x7)
                {
                    case 0:
                        // 32 bit address
                        base_addr = bar & 0xfffffff0U;

                        /* To get length, write all 1s, read length, mask out type bits
                        and write the original value back */
                        WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, 0xffffffffU);
                        length = ReadConfig(conf.bus, conf.dev, conf.func, bar_addr);
                        length &= 0xfffffff0U;
                        WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, bar);
                        unchecked { length = ~length; length++; }
                        length &= 0xffffffffU;

                        break;
                    case 2:
                        // 16 bit address
                        base_addr = bar & 0xfff0U;

                        WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, 0xffffU);
                        length = ReadConfig(conf.bus, conf.dev, conf.func, bar_addr);
                        length &= 0xfff0U;
                        WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, bar);
                        unchecked { length = ~length; length++; }
                        length &= 0xffffU;

                        break;
                    case 4:
                        // 64 bit address
                        int next_bar = bar_no + 1;
                        int next_bar_addr = next_bar * 4 + 0x10;
                        if (next_bar >= max_bar)
                            return null;
                        ulong next_bar_val = ReadConfig(conf.bus, conf.dev,
                            conf.func, next_bar_addr);
                        base_addr = bar & 0xfffffff0U;
                        base_addr |= (next_bar_val << 32);

                        WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, 0xffffffffU);
                        length = ReadConfig(conf.bus, conf.dev, conf.func, bar_addr);
                        length &= 0xfffffff0U;
                        WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, bar);

                        WriteConfig(conf.bus, conf.dev, conf.func, next_bar_addr, 0xffffffffU);
                        ulong next_length = ReadConfig(conf.bus, conf.dev, conf.func, next_bar_addr);
                        length |= (next_length << 32);
                        WriteConfig(conf.bus, conf.dev, conf.func, next_bar_addr, (uint)next_bar_val);

                        unchecked { length = ~length; length++; }

                        break;
                }

                tysos.PhysicalMemoryResource64 pmem = AllocPmemFixed(base_addr, length);
                if (pmem != null)
                    return pmem;

                // TODO: allocate a chunk of physical address space for the device
                throw new NotImplementedException();                
            }
            else
            {
                // IO register
                uint base_addr = bar & 0xfffffffcU;

                /* To get length, write all 1s, read length, mask out type bits
                and write the original value back */
                WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, 0xffffffffU);
                uint length = ReadConfig(conf.bus, conf.dev, conf.func, bar_addr);
                length &= 0xfffffffcU;
                WriteConfig(conf.bus, conf.dev, conf.func, bar_addr, bar);
                unchecked { length = ~length; length++; }
                length &= 0xffffffffU;

                tysos.x86_64.IOResource io = AllocIOFixed(base_addr, length);
                if (io != null)
                    return io;

                // TODO: allocate a chunk of IO space for the device
                throw new NotImplementedException();
            }
        }
    }
}
