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

namespace acpipc.Aml
{
    interface IMachineInterface
    {
        byte ReadMemoryByte(ulong Addr);
        ushort ReadMemoryWord(ulong Addr);
        uint ReadMemoryDWord(ulong Addr);
        ulong ReadMemoryQWord(ulong Addr);

        void WriteMemoryByte(ulong Addr, byte v);
        void WriteMemoryWord(ulong Addr, ushort v);
        void WriteMemoryDWord(ulong Addr, uint v);
        void WriteMemoryQWord(ulong Addr, ulong v);

        byte ReadIOByte(ulong Addr);
        ushort ReadIOWord(ulong Addr);
        uint ReadIODWord(ulong Addr);
        ulong ReadIOQWord(ulong Addr);

        void WriteIOByte(ulong Addr, byte v);
        void WriteIOWord(ulong Addr, ushort v);
        void WriteIODWord(ulong Addr, uint v);
        void WriteIOQWord(ulong Addr, ulong v);
    }
}
