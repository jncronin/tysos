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

/* A simple expand-only heap */

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.gc
{
    class simple_heap
    {
        static ulong start;
        static ulong end;
        static ulong cur_ptr;

        internal static void Init(ulong start_addr, ulong end_addr)
        {
            start = start_addr;
            end = end_addr;
            cur_ptr = start_addr;
        }

        internal static ulong Alloc(ulong size)
        {
            if ((end - cur_ptr) < size)
            {
                gc.Heap = gc.HeapType.PerCPU;
                if ((Program.arch != null) && (Program.arch.DebugOutput != null))
                {
                    Formatter.WriteLine("simple_heap: out of heap space", Program.arch.DebugOutput);
                    Formatter.Write("simple_heap: request for chunk of size ", Program.arch.DebugOutput);
                    Formatter.Write(size, "X", Program.arch.DebugOutput);
                    Formatter.Write(", start: ", Program.arch.DebugOutput);
                    Formatter.Write(start, "X", Program.arch.DebugOutput);
                    Formatter.Write(", end: ", Program.arch.DebugOutput);
                    Formatter.Write(end, "X", Program.arch.DebugOutput);
                    Formatter.Write(", cur_ptr: ", Program.arch.DebugOutput);
                    Formatter.Write(cur_ptr, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                }

                throw new Exception("simple_heap: Out of heap space");
            }
            ulong ret = cur_ptr;
            cur_ptr += size;
            cur_ptr = util.align(cur_ptr, 8);
            return ret;
        }
    }
}
