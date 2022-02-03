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
using System.Runtime.CompilerServices;

#if !NET6_0_OR_GREATER
#if _WIN32
    using nuint = System.UInt32;
    using nint = System.Int32;
#else
    using nuint = System.UInt64;
    using nint = System.Int64;
#endif
#endif

namespace tysos
{
    unsafe abstract class VirtMem
    {
        public PageProvider pmem;

        protected static VirtMem cur_vmem;

        public const uint FLAG_allocate = 0x1;
        public const uint FLAG_writeable = 0x2;
        public const uint FLAG_write_through = 0x8;
        public const uint FLAG_cache_disable = 0x10;

        public struct VMapping
        {
            public nuint vaddr;
            public nuint len;
            public nuint paddr;
            public uint flags;
        }
        public abstract VMapping Map(nuint paddr, nuint len, nuint vaddr, uint flags);

        /** <summary>Is the provided virtual address actually mapped?</summary>
         */
        public abstract bool IsValid(ulong vaddr);

        public abstract nuint PageSize { get; }


        // Override the IsValidPtr function in libsupcs.x86_64_Unwinder()
        [libsupcs.MethodAlias("_ZN8libsupcs17libsupcs#2Ex86_648Unwinder_10IsValidPtr_Rb_P1y")]
        private static bool IsValidPtr(ulong ptr)
        { return cur_vmem.IsValid(ptr); }
    }
}
