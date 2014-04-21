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

/* Byte operations for a most significant byte ordering assembler */

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    abstract class MSB_Assembler : Assembler
    {
        public MSB_Assembler(Architecture arch, FileLoader fileLoader, MemberRequestor memberRequestor, AssemblerOptions options) : base(arch, fileLoader, memberRequestor, options) { }

        public static byte[] SToByteArray(byte v)
        {
            return new byte[] { v };
        }

        public static byte[] SToByteArray(sbyte v)
        {
            if (v >= 0)
                return SToByteArray(Convert.ToByte(v));
            byte v2 = Convert.ToByte(-v);
            return SToByteArray((byte)(~v2 + 1));
        }

        public static byte[] SToByteArray(short v)
        {
            if (v >= 0)
                return SToByteArray(Convert.ToUInt16(v));
            ushort v2 = Convert.ToUInt16(-v);
            return SToByteArray((ushort)(~v2 + 1));
        }

        public static byte[] SToByteArray(ushort v)
        {
            return new byte[] { (byte)((v >> 8) & 0xff),
                (byte)(v & 0xff) };
        }

        public static byte[] SToByteArray(int v)
        {
            if (v >= 0)
                return SToByteArray(Convert.ToUInt32(v));
            uint v2 = Convert.ToUInt32(-v);
            return SToByteArray((uint)(~v2 + 1));
        }

        public static byte[] SToByteArray(uint v)
        {
            return new byte[] {
                (byte)((v >> 24) & 0xff),
                (byte)((v >> 16) & 0xff),
                (byte)((v >> 8) & 0xff),
                (byte)(v & 0xff)
            };
        }

        public static byte[] SToByteArray(long v)
        {
            if (v >= 0)
                return SToByteArray(Convert.ToUInt64(v));

            ulong v2 = Convert.ToUInt64(-v);
            return SToByteArray((ulong)(~v2 + 1));
        }

        public static byte[] SToByteArray(ulong v)
        {
            return new byte[] {
                (byte)((v >> 56) & 0xff),
                (byte)((v >> 48) & 0xff),
                (byte)((v >> 40) & 0xff),
                (byte)((v >> 32) & 0xff),
                (byte)((v >> 24) & 0xff),
                (byte)((v >> 16) & 0xff),
                (byte)((v >> 8) & 0xff),
                (byte)(v & 0xff)
            };
        }

        public static byte[] SToByteArray(IntPtr v)
        {
            return SToByteArray(v.ToInt64());
        }

        public static byte[] SToByteArray(UIntPtr v)
        {
            return SToByteArray(v.ToUInt64());
        }

    }
}
