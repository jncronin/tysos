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

namespace tysos
{
    partial class Pmem
    {
        internal class Stack
        {
            ulong temp_page_vaddr;
            ulong next_free_page = 0;
            ulong count = 0;

            internal Stack(ulong Temporary_Page_Vaddr)
            {
                temp_page_vaddr = Temporary_Page_Vaddr;
            }

            internal void Push(ulong vaddr, ulong paddr)
            {
                lock (this)
                {
                    _Push(vaddr, paddr);
                }
            }

            internal void Push(ulong paddr)
            {
                lock(this)
                {
                    Program.arch.VirtMem.map_page(temp_page_vaddr, paddr, true, true, false);
                    _Push(temp_page_vaddr, paddr);
                }
            }

            void _Push(ulong vaddr, ulong paddr)
            {
                if (paddr == 0)
                    return;

                unsafe
                {
                    *(ulong*)vaddr = next_free_page;
                }
                next_free_page = paddr;
                count++;
            }

            internal ulong BeginPop()
            {
                if (next_free_page == 0)
                    return 0;

                System.Threading.Monitor.Enter(this);
                return next_free_page;
            }

            internal void EndPop(ulong vaddr)
            {
                unsafe
                {
                    next_free_page = *(ulong*)vaddr;
                }
                count--;
                System.Threading.Monitor.Exit(this);
            }

            internal ulong Count { get { lock (this) { return count; } } }
        }
    }
}
