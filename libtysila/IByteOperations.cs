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

namespace libtysila
{
    interface IByteOperations
    {
        object FromByteArray(BaseType_Type type, IList<byte> v, int offset);
        object FromByteArray(BaseType_Type type, IList<byte> v);

        ulong FromByteArrayU8(IList<byte> v);
        ulong FromByteArrayU8(IList<byte> v, int offset);
        long FromByteArrayI8(IList<byte> v);
        long FromByteArrayI8(IList<byte> v, int offset);
        uint FromByteArrayU4(IList<byte> v);
        uint FromByteArrayU4(IList<byte> v, int offset);
        int FromByteArrayI4(IList<byte> v);
        int FromByteArrayI4(IList<byte> v, int offset);
        byte FromByteArrayU1(IList<byte> v);
        byte FromByteArrayU1(IList<byte> v, int offset);
        sbyte FromByteArrayI1(IList<byte> v);
        sbyte FromByteArrayI1(IList<byte> v, int offset);
        ushort FromByteArrayU2(IList<byte> v);
        ushort FromByteArrayU2(IList<byte> v, int offset);
        short FromByteArrayI2(IList<byte> v);
        short FromByteArrayI2(IList<byte> v, int offset);
        char FromByteArrayChar(IList<byte> v);
        char FromByteArrayChar(IList<byte> v, int offset);
        float FromByteArrayR4(IList<byte> v);
        float FromByteArrayR4(IList<byte> v, int offset);
        double FromByteArrayR8(IList<byte> v);
        double FromByteArrayR8(IList<byte> v, int offset);

        byte[] ToByteArray(byte v);
        byte[] ToByteArray(sbyte v);
        byte[] ToByteArray(short v);
        byte[] ToByteArray(ushort v);
        byte[] ToByteArray(int v);
        byte[] ToByteArray(uint v);
        byte[] ToByteArray(long v);
        byte[] ToByteArray(ulong v);
        byte[] ToByteArray(IntPtr v);
        byte[] ToByteArray(UIntPtr v);
        byte[] ToByteArray(ValueType v);
        byte[] ToByteArray(object v);
        byte[] ToByteArray(bool v);
        byte[] ToByteArray(float v);
        byte[] ToByteArray(double v);

        byte[] ToByteArrayZeroExtend(object v, int byte_count);
        byte[] ToByteArraySignExtend(object v, int byte_count);

        void SetByteArray(IList<byte> target, int t_offset, IList<byte> source, int s_offset, int s_size);
        void SetByteArray(IList<byte> target, int t_offset, IList<byte> source);
        void SetByteArray(IList<byte> target, int t_offset, byte v);
        void SetByteArray(IList<byte> target, int t_offset, short v);
        void SetByteArray(IList<byte> target, int t_offset, int v);
        void SetByteArray(IList<byte> target, int t_offset, int v, int v_size);
    }
}
