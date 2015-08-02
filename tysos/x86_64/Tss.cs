/* Copyright (C) 2008 - 2011 by John Cronin
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
    class Tss
    {
        internal unsafe static void Init(ulong tss_addr, ulong[] ists)
        {
            byte* tss = (byte*)tss_addr;

            /* Install ISTs */
            for(int i = 0; i < ists.Length && i < 7; i++)
            {
                ulong* tss_entry = (ulong*)(tss + 36 + 8 * i);
                *tss_entry = ists[i];
            }

            /* Install this TSS in the GDT */
            void* gdt;
            ushort gdt_limit;
            libsupcs.x86_64.Cpu.Sgdt(out gdt, out gdt_limit);

            uint dw_1 = (uint)((tss_addr << 16) & 0xffffffff) + 108;
            uint dw_2 = (uint)((tss_addr >> 16) & 0xff);
            dw_2 |= ((uint)((tss_addr >> 24) & 0xff)) << 24;
            dw_2 |= 0x8900;     // set as TSS (64-bit) available, dpl 0, granularity = 0
            ulong low_qword = (((ulong)dw_2) << 32) | (ulong)dw_1;
            ulong high_qword = (tss_addr >> 32) & 0xffffffff;

            *(ulong*)((byte*)gdt + 5 * 8) = low_qword;
            *(ulong*)((byte*)gdt + 6 * 8) = high_qword;

            /* Load the TSS */
            libsupcs.x86_64.Cpu.Ltr(0x28);
        }
    }
}
