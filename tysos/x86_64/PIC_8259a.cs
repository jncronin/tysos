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
    class PIC_8295a
    {
        const ushort PIC1_COMMAND = 0x20;
        const ushort PIC1_DATA = 0x21;
        const ushort PIC2_COMMAND = 0xa0;
        const ushort PIC2_DATA = 0xa1;

        internal static void Disable()
        {
            Formatter.WriteLine("Disabling PIC", Program.arch.DebugOutput);

            /* We want to use the LAPIC and IOAPIC, so disable the PIC */
            libsupcs.IoOperations.PortOut(PIC2_DATA, (byte)0xff);
            libsupcs.IoOperations.PortOut(PIC1_DATA, (byte)0xff);
        }
    }
}
