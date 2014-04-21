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
        public struct Tss_struct
        {
            uint reserved1;
            public ulong rsp0;
            public ulong rsp1;
            public ulong rsp2;
            ulong reserved2;
            public ulong ist1;
            public ulong ist2;
            public ulong ist3;
            public ulong ist4;
            public ulong ist5;
            public ulong ist6;
            public ulong ist7;
            ulong reserved3;
            public uint io_map_addr;
        }

        public Tss(ulong gdt_addr, ulong tss, ulong rsp0, ulong ist_block, ulong ist_length)
        {
            unsafe
            {
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("rsp0")).Offset)) = rsp0;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist1")).Offset)) = ist_block + 1 * ist_length;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist2")).Offset)) = ist_block + 2 * ist_length;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist3")).Offset)) = ist_block + 3 * ist_length;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist4")).Offset)) = ist_block + 4 * ist_length;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist5")).Offset)) = ist_block + 5 * ist_length;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist6")).Offset)) = ist_block + 6 * ist_length;
                *(ulong*)(tss + (ulong)(((libsupcs.TysosField)typeof(Tss_struct).GetField("ist7")).Offset)) = ist_block + 7 * ist_length;
            }

            tysos.Collections.StaticULongArray gdt = new tysos.Collections.StaticULongArray(gdt_addr, 64);

            uint dw_1 = (uint)((tss << 16) & 0xffffffff) + 108;
            uint dw_2 = (uint)((tss >> 16) & 0xff);
            dw_2 |= ((uint)((tss >> 24) & 0xff)) << 24;
            dw_2 |= 0x8900;     // set as TSS (64-bit) available, dpl 0, granularity = 0
            ulong low_qword = (((ulong)dw_2) << 32) | (ulong)dw_1;
            ulong high_qword = (tss >> 32) & 0xffffffff;

            gdt[5] = low_qword;
            gdt[6] = high_qword;

            libsupcs.x86_64.Cpu.Ltr(0x28);
        }
    }
}
