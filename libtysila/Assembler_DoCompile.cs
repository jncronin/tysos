/* Copyright (C) 2010 - 2011 by John Cronin
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

namespace libtysila
{
    partial class Assembler
    {
        public class LockedInt
        {
            int _count = 0;
            public int Count { get { lock (this) { return _count; } } set { lock (this) { _count = value; } } }
            static public implicit operator int(LockedInt rtc) { return rtc.Count; }
            static public implicit operator LockedInt(int val) { LockedInt ret = new LockedInt(); ret.Count = val; return ret; }
            static public LockedInt operator ++(LockedInt rtc) { lock (rtc) { rtc._count++; return rtc; } }
            static public LockedInt operator --(LockedInt rtc) { lock (rtc) { rtc._count--; return rtc; } }
            public int Increment { get { lock (this) { return _count++; } } }
            public override string ToString() { lock (this) { return _count.ToString(); } }
        }
    }
}
