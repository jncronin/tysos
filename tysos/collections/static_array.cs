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

namespace tysos.Collections
{
    internal class StaticByteArray
    {
        private ulong base_addr;
        private ulong length;

        public StaticByteArray(ulong BaseAddr, ulong Length)
        { base_addr = BaseAddr; length = Length; }

        public unsafe byte this[ulong i]
        {
            get
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(byte*)(base_addr + i);
            }

            set
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                *(byte*)(base_addr + i) = value;
            }
        }
    }

    internal class StaticUShortArray
    {
        private ulong base_addr;
        private ulong length;

        public StaticUShortArray(ulong BaseAddr, ulong Length)
        { base_addr = BaseAddr; length = Length; }

        public unsafe void Clear(ushort v)
        {
            for (ulong i = 0; i < length; i++)
                *(ushort*)(base_addr + i * 2) = v;
        }

        public unsafe ushort this[ulong i]
        {
            get
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(ushort*)(base_addr + i * 2);
            }

            set
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                *(ushort*)(base_addr + i * 2) = value;
            }
        }

        public unsafe ushort this[int i]
        {
            get
            {
                if ((ulong)i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(ushort*)(base_addr + (ulong)i * 2);
            }

            set
            {
                if ((ulong)i >= length)
                    throw new System.IndexOutOfRangeException();
                *(ushort*)(base_addr + (ulong)i * 2) = value;
            }
        }
    }

    internal class StaticUIntArray
    {
        private ulong base_addr;
        private ulong length;

        public StaticUIntArray(ulong BaseAddr, ulong Length)
        { base_addr = BaseAddr; length = Length; }

        public unsafe void Clear(uint v)
        {
            for (ulong i = 0; i < length; i++)
                *(uint*)(base_addr + i * 4) = v;
        }


        public unsafe uint this[ulong i]
        {
            get
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(uint*)(base_addr + i * 4);
            }

            set
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                *(uint*)(base_addr + i * 4) = value;
            }
        }

        public unsafe uint this[int i]
        {
            get
            {
                if ((ulong)i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(uint*)(base_addr + (ulong)i * 4);
            }

            set
            {
                if ((ulong)i >= length)
                    throw new System.IndexOutOfRangeException();
                *(uint*)(base_addr + (ulong)i * 4) = value;
            }
        }
    }

    internal class StaticULongArray
    {
        private ulong base_addr;
        private ulong length;

        public StaticULongArray(ulong BaseAddr, ulong Length)
        { base_addr = BaseAddr; length = Length; }

        public unsafe void Clear(ulong v)
        {
            for (ulong i = 0; i < length; i++)
                *(ulong*)(base_addr + i * 8) = v;
        }

        public unsafe ulong this[ulong i]
        {
            get
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(ulong*)(base_addr + i * 8);
            }

            set
            {
                if (i >= length)
                    throw new System.IndexOutOfRangeException();
                *(ulong*)(base_addr + i * 8) = value;
            }
        }

        public unsafe ulong this[int i]
        {
            get
            {
                if ((ulong)i >= length)
                    throw new System.IndexOutOfRangeException();
                return *(ulong*)(base_addr + (ulong)i * 8);
            }

            set
            {
                if ((ulong)i >= length)
                    throw new System.IndexOutOfRangeException();
                *(ulong*)(base_addr + (ulong)i * 8) = value;
            }
        }
    }
}
