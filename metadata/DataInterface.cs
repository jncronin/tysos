/* Copyright (C) 2016 by John Cronin
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

namespace metadata
{
    public abstract class DataInterface
    {
        string _name;
        public virtual string Name { get { return _name; } set { _name = value; } }
        public abstract byte ReadByte(int offset);

        public virtual sbyte ReadSByte(int offset)
        {
            var b = ReadByte(offset);
            if (b >= 0x80)
                return (sbyte)(-128 + (sbyte)(b - 0x80));
            else
                return (sbyte)b;
        }
        public virtual ushort ReadUShort(int offset)
        {
            return (ushort)(ReadByte(offset) | (uint)(ReadByte(offset + 1) << 8));
        }
        public virtual short ReadShort(int offset)
        {
            var u = ReadUShort(offset);
            if (u >= 0x8000)
                return (short)(-32768 + (short)(u - 0x8000));
            else
                return (short)u;
        }
        public virtual uint ReadUInt(int offset)
        {
            return ReadByte(offset) |
                ((uint)ReadByte(offset + 1) << 8) |
                ((uint)ReadByte(offset + 2) << 16) |
                ((uint)ReadByte(offset + 3) << 24);
        }
        public virtual int ReadInt(int offset)
        {
            var u = ReadUInt(offset);
            if (u >= 0x80000000U)
                return -2147483648 + (int)(u - 0x80000000U);
            else
                return (int)u;
        }
        public virtual ulong ReadULong(int offset)
        {
            return ReadByte(offset) |
                ((ulong)ReadByte(offset + 1) << 8) |
                ((ulong)ReadByte(offset + 2) << 16) |
                ((ulong)ReadByte(offset + 3) << 24) |
                ((ulong)ReadByte(offset + 4) << 32) |
                ((ulong)ReadByte(offset + 5) << 40) |
                ((ulong)ReadByte(offset + 6) << 48) |
                ((ulong)ReadByte(offset + 7) << 56);
        }
        public virtual long ReadLong(int offset)
        {
            var u = ReadULong(offset);
            if (u >= 0x8000000000000000UL)
                return -9223372036854775808 + (long)(u - 0x8000000000000000UL);
            else
                return (long)u;
        }

        public virtual int GetLength()
        {
            return 0;
        }

        public virtual DataInterface Clone(int offset)
        {
            // Clone into a new data interface.  It is undefined whether they
            //  represent the same data (i.e. deep or shallow copy).

            // The default implementation copies everything into an ArrayInterface
            int to_copy = GetLength() - offset;
            if (to_copy < 0)
                to_copy = 0;

            byte[] b = new byte[to_copy];
            for (int i = 0; i < to_copy; i++)
                b[i] = ReadByte(offset + i);

            return new ArrayInterface(b);
        }
    }

    public class StreamInterface : DataInterface
    {
        System.IO.Stream s;
        int o = 0;

        public StreamInterface(System.IO.Stream str)
        {
            s = str;
        }
        public StreamInterface(System.IO.Stream str, int offset)
        {
            s = str;
            o = offset;
        }

        public override byte ReadByte(int offset)
        {
            s.Seek(offset + o, System.IO.SeekOrigin.Begin);
            return (byte)s.ReadByte();
        }

        public override DataInterface Clone(int offset)
        {
            return new StreamInterface(s, o + offset);
        }
    }

    public class ArrayInterface : DataInterface
    {
        IList<byte> _arr;
        int _offset = 0;

        public ArrayInterface(IList<byte> arr)
        {
            _arr = arr;
            _offset = 0;
        }

        public ArrayInterface(IList<byte> arr, int offset)
        {
            _arr = arr;
            _offset = offset;
        }

        public override byte ReadByte(int offset)
        {
            return _arr[offset + _offset];
        }

        public override int GetLength()
        {
            return _arr.Count;
        }

        public override DataInterface Clone(int offset)
        {
            return new ArrayInterface(_arr, _offset + offset);
        }
    }
}
