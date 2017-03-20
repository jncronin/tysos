/* Copyright (C) 2015 by John Cronin
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

using System.Collections.Generic;
using tysos;

namespace net
{
    partial class net
    {
        /* Convert from network byte order */
        public static ushort ReadWord(byte[] buf, int offset)
        {
            return (ushort)(((uint)buf[offset] << 8) | buf[offset + 1]);
        }

        public static IPv4Address ReadIPv4Addr(byte[] buf, int offset)
        { return new IPv4Address { addr = ReadDWord(buf, offset) }; }

        public static uint ReadDWord(byte[] buf, int offset)
        {
            return (uint)buf[offset] << 24 | (uint)buf[offset + 1] << 16 |
                (uint)buf[offset + 2] << 8 | buf[offset + 3];
        }

        public static HWAddr ReadHWAddr(byte[] buf, int offset)
        {
            HWAddr ret = new HWAddr();
            for (int i = 0; i < 6; i++)
                ret.MAC[i] = buf[offset + i];
            return ret;
        }
    }
}
