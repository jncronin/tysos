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
    class PIT_8253
    {
        const ushort Channel_0_data = 0x40;
        const ushort Channel_1_data = 0x41;
        const ushort Channel_2_data = 0x42;
        const ushort Mode_Command = 0x43;

        public const ulong Frequency = 1193182;        // 1.193182 MHz

        public static void ResetPIT(ushort reset_val)
        {
            libsupcs.IoOperations.PortOut(Mode_Command, 0x00);       // set channel 0 to binary mode, mode 0
            libsupcs.IoOperations.PortOut(Channel_0_data, (byte)(reset_val & 0xff));
            libsupcs.IoOperations.PortOut(Channel_0_data, (byte)(reset_val >> 8));
        }

        public static ushort ReadPIT()
        {
            byte b_low, b_high;

            libsupcs.IoOperations.PortOut(Mode_Command, 0x00);       // set channel 0 to binary mode, also 'latch' current PIT value to store it
            b_low = libsupcs.IoOperations.PortInb(Channel_0_data);
            b_high = libsupcs.IoOperations.PortInb(Channel_0_data);

            return (ushort)((ushort)b_low + (((ushort)b_high) << 8));
        }
    }
}
