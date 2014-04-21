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

namespace binary_library
{
    class ListReader
    {
        internal static byte ReadByte(IList<byte> d, ref int p)
        {
            return d[p++];
        }

        internal static sbyte ReadSByte(IList<byte> d, ref int p)
        {
            unchecked
            {
                return (sbyte)d[p++];
            }
        }

        internal static ushort ReadUInt16(IList<byte> d, ref int p)
        {
            byte[] a = new byte[2] { d[p], d[p + 1] };
            p += 2;
            return BitConverter.ToUInt16(a, 0);
        }

        internal static short ReadInt16(IList<byte> d, ref int p)
        {
            byte[] a = new byte[2] { d[p], d[p + 1] };
            p += 2;
            return BitConverter.ToInt16(a, 0);
        }

        internal static uint ReadUInt32(IList<byte> d, ref int p)
        {
            byte[] a = new byte[4] { d[p], d[p + 1], d[p + 2], d[p + 3] };
            p += 4;
            return BitConverter.ToUInt32(a, 0);
        }

        internal static int ReadInt32(IList<byte> d, ref int p)
        {
            byte[] a = new byte[4] { d[p], d[p + 1], d[p + 2], d[p + 3] };
            p += 4;
            return BitConverter.ToInt32(a, 0);
        }

        internal static ulong ReadUInt64(IList<byte> d, ref int p)
        {
            byte[] a = new byte[8] { d[p], d[p + 1], d[p + 2], d[p + 3], d[p + 4], d[p + 5], d[p + 6], d[p + 7] };
            p += 8;
            return BitConverter.ToUInt64(a, 0);
        }

        internal static long ReadInt64(IList<byte> d, ref int p)
        {
            byte[] a = new byte[8] { d[p], d[p + 1], d[p + 2], d[p + 3], d[p + 4], d[p + 5], d[p + 6], d[p + 7] };
            p += 8;
            return BitConverter.ToInt64(a, 0);
        }
    }
}
