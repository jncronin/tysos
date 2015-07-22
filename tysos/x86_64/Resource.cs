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

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.x86_64
{
    public class IOResource : RangeResource32
    {
        internal IOResource(uint addr, uint length) : base(addr, length) { }

        public override void Write(uint addr, uint length, uint val)
        {
            switch(length)
            {
                case 1:
                    libsupcs.IoOperations.PortOut((ushort)addr, (byte)val);
                    break;
                case 2:
                    libsupcs.IoOperations.PortOut((ushort)addr, (ushort)val);
                    break;
                case 4:
                    libsupcs.IoOperations.PortOut((ushort)addr, val);
                    break;
            }
        }

        public override void Write(ulong addr, ulong length, ulong val)
        {
            Write((uint)addr, (uint)length, (uint)val);
        }

        public override uint Read(uint addr, uint length)
        {
            switch(length)
            {
                case 1:
                    return libsupcs.IoOperations.PortInb((ushort)addr);
                case 2:
                    return libsupcs.IoOperations.PortInw((ushort)addr);
                case 4:
                    return libsupcs.IoOperations.PortInd((ushort)addr);
            }
            return 0;
        }

        public override ulong Read(ulong addr, ulong length)
        {
            return Read((uint)addr, (uint)length);
        }
    }
}
