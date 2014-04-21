/* Copyright (C) 2013 by John Cronin
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

namespace rpi_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Uart.Init();
            WriteString("Hello from C#!");
            while (true) ;
        }

        internal static void Delay(int count)
        {
            for(int i = 0; i < count; i++);
        }

        static void WriteString(string s)
        {
            foreach (char c in s)
                Uart.Putc(c);
        }
    }

    class Uart
    {
        const UInt32 GPIO_BASE = 0x20200000;
        const UInt32 GPPUD = GPIO_BASE + 0x94;
        const UInt32 GPPUDCLK0 = GPIO_BASE + 0x98;

        const UInt32 UART0_BASE = 0x20201000;
        const UInt32 UART0_DR = UART0_BASE + 0x00;
        const UInt32 UART0_RSRECR = UART0_BASE + 0x04;
        const UInt32 UART0_FR = UART0_BASE + 0x18;
        const UInt32 UART0_ILPR = UART0_BASE + 0x20;
        const UInt32 UART0_IBRD = UART0_BASE + 0x24;
        const UInt32 UART0_FBRD = UART0_BASE + 0x28;
        const UInt32 UART0_LCRH = UART0_BASE + 0x2C;
        const UInt32 UART0_CR = UART0_BASE + 0x30;
        const UInt32 UART0_IFLS = UART0_BASE + 0x34;
        const UInt32 UART0_IMSC = UART0_BASE + 0x38;
        const UInt32 UART0_RIS = UART0_BASE + 0x3C;
        const UInt32 UART0_MIS = UART0_BASE + 0x40;
        const UInt32 UART0_ICR = UART0_BASE + 0x44;
        const UInt32 UART0_DMACR = UART0_BASE + 0x48;
        const UInt32 UART0_ITCR = UART0_BASE + 0x80;
        const UInt32 UART0_ITIP = UART0_BASE + 0x84;
        const UInt32 UART0_ITOP = UART0_BASE + 0x88;
        const UInt32 UART0_TDR = UART0_BASE + 0x8C;

        internal unsafe static void Init()
        {
            *(UInt32 *)UART0_CR = 0;
            *(UInt32*)GPPUD = 0;
            Program.Delay(150);

            *(UInt32*)GPPUDCLK0 = (1 << 14) | (1 << 15);
            Program.Delay(150);

            *(UInt32*)GPPUDCLK0 = 0;

            *(UInt32*)UART0_ICR = 0x7ff;
            *(UInt32*)UART0_IBRD = 1;
            *(UInt32*)UART0_FBRD = 40;

            *(UInt32*)UART0_LCRH = (1 << 4) | (1 << 5) | (1 << 6);

            *(UInt32*)UART0_IMSC = (1 << 1) | (1 << 4) | (1 << 5) |
                (1 << 6) | (1 << 7) | (1 << 8) | (1 << 9) | (1 << 10);

            *(UInt32*)UART0_CR = (1 << 0) | (1 << 8) | (1 << 9);
        }

        internal unsafe static void Putc(char c)
        {
            while ((*(UInt32*)UART0_FR & (1 << 5)) != 0) ;
            *(UInt32*)UART0_DR = (uint)(byte)c;
        }
    }
}
