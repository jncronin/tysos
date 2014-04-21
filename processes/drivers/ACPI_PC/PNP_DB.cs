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

namespace ACPI_PC
{
    class PNP_DB
    {
        internal const long PCI_Root_HID = 0x030ad041;
        internal const long PCIe_Root_HID = 0x020cd041;
        internal const long BAT_HID = 0x0a0cd041;
        internal const long HPET_HID = 0x0301d041;
        internal const long PS2_Keyboard_HID = 0x0303d041;
        internal const long PS2_Mouse_HID = 0x030fd041;
        internal const long PIC_HID = 0x0000d041;
        internal const long APIC_HID = 0x0300d041;
        internal const long RTC_HID = 0x000bd041;
        internal const long FDC_HID = 0x0007d041;
        internal const long LPT_HID = 0x0004d041;
        internal const long Serial_HID = 0x0105d041;
        internal const long AT_DMA_HID = 0x0002d041;
        internal const long AT_Timer_HID = 0x0001d041;
    }
}
