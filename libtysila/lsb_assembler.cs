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

/* Byte operations for a least significant byte ordering assembler */

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace libtysila
{
    public abstract class LSB_Assembler : Assembler, IByteOperations
    {
        public LSB_Assembler(Architecture arch, FileLoader fileLoader, MemberRequestor memberRequestor, AssemblerOptions options) : base(arch, fileLoader, memberRequestor, options) { }

        protected uint[] Split64(object o)
        {
            byte[] byte_arr;
            if (IsSigned(o))
                byte_arr = ToByteArraySignExtend(o, 8);
            else
                byte_arr = ToByteArrayZeroExtend(o, 8);

            byte[] lo = new byte[4];
            byte[] hi = new byte[4];
            Array.Copy(byte_arr, 0, lo, 0, 4);
            Array.Copy(byte_arr, 4, hi, 0, 4);

            uint[] ret = new uint[2];
            ret[0] = FromByteArrayU4(lo);
            ret[1] = FromByteArrayU4(hi);
            return ret;
        }

        #region IByteOperations Members

        public override byte[] ToByteArrayZeroExtend(object v, int byte_count)
        {
            byte[] ba = ToByteArray(v);
            byte[] ret = new byte[byte_count];

            for (int i = 0; i < byte_count; i++)
            {
                if (i < ba.Length)
                    ret[i] = ba[i];
                else
                    ret[i] = 0x00;
            }

            return ret;
        }

        public override byte[] ToByteArraySignExtend(object v, int byte_count)
        {
            byte[] ba = ToByteArray(v);
            byte[] ret = new byte[byte_count];

            // the sign bit is the most significant bit in an lsb system
            byte msbyte = ba[ba.Length - 1];
            byte sign_bit = (byte)((msbyte >> 7) & 0x1);
            byte extend_byte = 0x00;
            if (sign_bit == 0x1)
                extend_byte = 0xff;

            for (int i = 0; i < byte_count; i++)
            {
                if (i < ba.Length)
                    ret[i] = ba[i];
                else
                    ret[i] = extend_byte;
            }

            return ret;
        }

        public override byte[] ToByteArray(byte v)
        {
            return new byte[] { v };
        }

        public override byte[] ToByteArray(sbyte v)
        {
            if (v == sbyte.MinValue) return new byte[] { 0x80 };
            if (v >= 0)
                return ToByteArray(Convert.ToByte(v));
            byte v2 = Convert.ToByte(-v);
            return ToByteArray((byte)(~v2 + 1));
        }

        public override byte[] ToByteArray(short v)
        {
            if (v == short.MinValue) return new byte[] { 0x00, 0x80 };
            if (v >= 0)
                return ToByteArray(Convert.ToUInt16(v));
            ushort v2 = Convert.ToUInt16(-v);
            return ToByteArray((ushort)(~v2 + 1));
        }

        public override byte[] ToByteArray(ushort v)
        {
            return new byte[] { (byte)(v & 0xff),
                (byte)((v >> 8) & 0xff) };
        }

        public override byte[] ToByteArray(int v)
        {
            if (v == int.MinValue) return new byte[] { 0x00, 0x00, 0x00, 0x80 };
            if (v >= 0)
                return ToByteArray(Convert.ToUInt32(v));
            uint v2 = Convert.ToUInt32(-v);
            return ToByteArray((uint)(~v2 + 1));
        }

        public static byte[] ToByteArrayS(uint v)
        {
            return new byte[] { (byte)(v & 0xff),
                (byte)((v >> 8) & 0xff),
                (byte)((v >> 16) & 0xff),
                (byte)((v >> 24) & 0xff) };
        }
        public override byte[] ToByteArray(uint v)
        {
            return ToByteArrayS(v);
        }

        public override byte[] ToByteArray(long v)
        {
            if (v == long.MinValue) return new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 };
            if (v >= 0)
                return ToByteArray(Convert.ToUInt64(v));

            ulong v2 = Convert.ToUInt64(-v);
            return ToByteArray((ulong)(~v2 + 1));
        }

        public static byte[] ToByteArrayS(ulong v)
        {
            return new byte[] { (byte)(v & 0xff),
                (byte)((v >> 8) & 0xff),
                (byte)((v >> 16) & 0xff),
                (byte)((v >> 24) & 0xff),
                (byte)((v >> 32) & 0xff),
                (byte)((v >> 40) & 0xff),
                (byte)((v >> 48) & 0xff),
                (byte)((v >> 56) & 0xff) };
        }

        public override byte[] ToByteArray(ulong v)
        {
            return ToByteArrayS(v);
        }

        public override byte[] ToByteArray(IntPtr v)
        {
            return ToByteArray(v.ToInt64());
        }

        public override byte[] ToByteArray(UIntPtr v)
        {
            return ToByteArray(v.ToUInt64());
        }

        public override byte[] ToByteArray(double v)
        {
            return BitConverter.GetBytes(v);
        }

        public override byte[] ToByteArray(float v)
        {
            return BitConverter.GetBytes(v);
        }

        #endregion

        #region IAssembler Members


        public override byte[] ToByteArray(ValueType v)
        {
            FieldInfo[] fis = v.GetType().GetFields(BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic);

            List<byte> ret = new List<byte>();

            foreach (FieldInfo fi in fis)
                ret.AddRange(ToByteArray(fi.GetValue(v)));

            return ret.ToArray();
        }

        public override byte[] ToByteArray(bool v)
        {
            if (v)
                return ToByteArray((sbyte)-1);
            else
                return ToByteArray((sbyte)0);
        }

        #endregion

        #region IAssembler Members


        public static void SetByteArrayS(IList<byte> target, int t_offset, IList<byte> source, int s_offset, int s_size)
        {
            if (s_size < 0)
                s_size = source.Count - s_offset;
            if ((s_offset + s_size) > source.Count)
                throw new ArgumentOutOfRangeException();
            if((t_offset + s_size) > target.Count)
                throw new ArgumentOutOfRangeException();
            for (int i = 0; i < s_size; i++)
                target[t_offset + i] = source[s_offset + i];
        }

        public override void SetByteArray(IList<byte> target, int t_offset, IList<byte> source, int s_offset, int s_size)
        {
            LSB_Assembler.SetByteArrayS(target, t_offset, source, s_offset, s_size);
        }

        public override void SetByteArray(IList<byte> target, int t_offset, byte v)
        {
            SetByteArray(target, t_offset, this.ToByteArray(v), 0, -1);
        }

        public override void SetByteArray(IList<byte> target, int t_offset, short v)
        {
            SetByteArray(target, t_offset, this.ToByteArray(v), 0, -1);
        }

        public override void SetByteArray(IList<byte> target, int t_offset, int v)
        {
            SetByteArray(target, t_offset, this.ToByteArray(v), 0, -1);
        }

        public override void SetByteArray(IList<byte> target, int t_offset, int v, int v_size)
        {
            if (v_size == 4)
                SetByteArray(target, t_offset, this.ToByteArray(Convert.ToInt32(v)), 0, -1);
            else if (v_size == 2)
                SetByteArray(target, t_offset, this.ToByteArray(Convert.ToInt16(v)), 0, -1);
            else if (v_size == 1)
                SetByteArray(target, t_offset, this.ToByteArray(Convert.ToSByte(v)), 0, -1);
            else
                SetByteArray(target, t_offset, this.ToByteArray(v), 0, v_size);
        }

        public override ulong FromByteArrayU8(IList<byte> v, int offset)
        {
            ulong ret = (ulong)v[offset + 0] + (((ulong)v[offset + 1]) << 8) +
                (((ulong)v[offset + 2]) << 16) + (((ulong)v[offset + 3]) << 24) +
                (((ulong)v[offset + 4]) << 32) + (((ulong)v[offset + 5]) << 40) +
                (((ulong)v[offset + 6]) << 48) + (((ulong)v[offset + 7]) << 56);
            return ret;
        }
        public override ulong FromByteArrayU8(IList<byte> v)
        { return FromByteArrayU8(v, 0); }
        public override long FromByteArrayI8(IList<byte> v, int offset)
        {
            ulong us = FromByteArrayU8(v, offset);
            if (us >= 0x8000000000000000)
                return (long)(Convert.ToInt64(us - 0x8000000000000000) - 0x4000000000000000
                    - 0x4000000000000000);
            else
                return Convert.ToInt64(us);
        }
        public override long FromByteArrayI8(IList<byte> v)
        { return FromByteArrayI8(v, 0); }

        public override uint FromByteArrayU4(IList<byte> v, int offset)
        {
            return FromByteArrayU4S(v, offset);
        }
        public static uint FromByteArrayU4S(IList<byte> v, int offset)
        {
            uint ret = (uint)v[offset + 0] + (((uint)v[offset + 1]) << 8) + 
                (((uint)v[offset + 2]) << 16) + (((uint)v[offset + 3]) << 24);
            return ret;
        }

        public override uint FromByteArrayU4(IList<byte> v)
        { return FromByteArrayU4(v, 0); }

        public override int FromByteArrayI4(IList<byte> v)
        { return FromByteArrayI4(v, 0); }

        public override int FromByteArrayI4(IList<byte> v, int offset)
        {
            uint us = FromByteArrayU4(v, offset);
            if (us >= 0x80000000)
                return (int)(Convert.ToInt32(us - 0x80000000) - 0x80000000);
            else
                return Convert.ToInt32(us);
        }

        public override byte FromByteArrayU1(IList<byte> v)
        { return FromByteArrayU1(v, 0); }

        public override byte FromByteArrayU1(IList<byte> v, int offset)
        {
            return v[offset];
        }

        public override sbyte FromByteArrayI1(IList<byte> v)
        { return FromByteArrayI1(v, 0); }

        public override sbyte FromByteArrayI1(IList<byte> v, int offset)
        {
            byte us = FromByteArrayU1(v, offset);
            if (us >= 0x80)
                return (sbyte)(Convert.ToSByte(us - 0x80) - 0x80);
            else
                return Convert.ToSByte(us);
        }

        public override char FromByteArrayChar(IList<byte> v)
        { return FromByteArrayChar(v, 0); }

        public override char FromByteArrayChar(IList<byte> v, int offset)
        { return (char)FromByteArrayU2(v, 0); }

        public override ushort FromByteArrayU2(IList<byte> v)
        { return FromByteArrayU2(v, 0); }

        public override ushort FromByteArrayU2(IList<byte> v, int offset)
        {
            ushort ret = (ushort)((ushort)v[offset + 0] + (((ushort)v[offset + 1]) << 8));
            return ret;
        }

        public override short FromByteArrayI2(IList<byte> v)
        { return FromByteArrayI2(v, 0); }

        public override short FromByteArrayI2(IList<byte> v, int offset)
        {
            ushort us = FromByteArrayU2(v, offset);
            if (us >= 0x8000)
                return (short)(Convert.ToInt16(us - 0x8000) - 0x8000);
            else
                return Convert.ToInt16(us);
        }

        public override float FromByteArrayR4(IList<byte> v)
        { return FromByteArrayR4(v, 0); }

        public override float FromByteArrayR4(IList<byte> v, int offset)
        {
            byte[] b = new byte[v.Count];
            v.CopyTo(b, 0);
            return BitConverter.ToSingle(b, offset);
        }

        public override double FromByteArrayR8(IList<byte> v)
        { return FromByteArrayR8(v, 0); }

        public override double FromByteArrayR8(IList<byte> v, int offset)
        {
            byte[] b = new byte[v.Count];
            v.CopyTo(b, 0);
            return BitConverter.ToDouble(b, offset);
        }


        public override object FromByteArray(BaseType_Type type, IList<byte> v, int offset)
        {
            switch (type)
            {
                case BaseType_Type.String:
                    return Encoding.Unicode.GetString(new List<byte>(v).ToArray(), offset, v.Count - offset);
                case BaseType_Type.I1:
                    return FromByteArrayI1(v, offset);
                case BaseType_Type.U1:
                    return FromByteArrayU1(v, offset);
                case BaseType_Type.I4:
                    return FromByteArrayI4(v, offset);
                case BaseType_Type.U4:
                    return FromByteArrayU4(v, offset);
                case BaseType_Type.I8:
                    return FromByteArrayI8(v, offset);
                case BaseType_Type.U8:
                    return FromByteArrayU8(v, offset);
                case BaseType_Type.I2:
                    return FromByteArrayI2(v, offset);
                case BaseType_Type.U2:
                    return FromByteArrayU2(v, offset);
                case BaseType_Type.Char:
                    return FromByteArrayChar(v, offset);
                case BaseType_Type.R4:
                    return FromByteArrayR4(v, offset);
                case BaseType_Type.R8:
                    return FromByteArrayR8(v, offset);
                default:
                    throw new Exception("Not currently supported");
            }
        }

        public override object FromByteArray(BaseType_Type type, IList<byte> v)
        { return FromByteArray(type, v, 0); }

        #endregion
    }
}
