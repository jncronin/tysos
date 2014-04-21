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

namespace aml_interpreter
{
    partial class AML
    {
        ulong base_vaddr;
        int offset;

        byte ReadByte()
        {
            unsafe
            {
                byte ret = *(byte*)(base_vaddr + (ulong)offset);
                offset += 1;
                return ret;
            }
        }
        ushort ReadWord()
        {
            unsafe
            {
                ushort ret = *(ushort*)(base_vaddr + (ulong)offset);
                offset += 2;
                return ret;
            }
        }
        uint ReadDWord()
        {
            unsafe
            {
                uint ret = *(uint*)(base_vaddr + (ulong)offset);
                offset += 4;
                return ret;
            }
        }
        ulong ReadQWord()
        {
            unsafe
            {
                ulong ret = *(ulong*)(base_vaddr + (ulong)offset);
                offset += 8;
                return ret;
            }
        }

        int CurOffset()
        { return offset; }
        void AdjustOffset(int delta)
        { offset += delta; }

        internal void SetInput(ulong vaddr)
        { base_vaddr = vaddr; }
    }
}
