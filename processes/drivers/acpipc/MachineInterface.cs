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

namespace acpipc
{
    class MachineInterface : Aml.IMachineInterface
    {
        acpipc a;

        public MachineInterface(acpipc acpi)
        {
            a = acpi;
        }

        public byte ReadIOByte(ulong Addr)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 1) <= (io.Addr64 + io.Length64)))
                    return (byte)io.Read(Addr, 1);
            }
            throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public uint ReadIODWord(ulong Addr)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 4) <= (io.Addr64 + io.Length64)))
                    return (uint)io.Read(Addr, 4);
            }
            throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public ulong ReadIOQWord(ulong Addr)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 8) <= (io.Addr64 + io.Length64)))
                    return (ulong)io.Read(Addr, 8);
            }
            throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public ushort ReadIOWord(ulong Addr)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 2) <= (io.Addr64 + io.Length64)))
                    return (ushort)io.Read(Addr, 2);
            }
            throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public byte ReadMemoryByte(ulong Addr)
        {
            throw new NotImplementedException();
        }

        public uint ReadMemoryDWord(ulong Addr)
        {
            throw new NotImplementedException();
        }

        public ulong ReadMemoryQWord(ulong Addr)
        {
            throw new NotImplementedException();
        }

        public ushort ReadMemoryWord(ulong Addr)
        {
            throw new NotImplementedException();
        }

        public void WriteIOByte(ulong Addr, byte v)
        {
            foreach(tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 1) <= (io.Addr64 + io.Length64)))
                    io.Write(Addr, 1, v);
            }
        }

        public void WriteIODWord(ulong Addr, uint v)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 4) <= (io.Addr64 + io.Length64)))
                    io.Write(Addr, 4, v);
            }
        }

        public void WriteIOQWord(ulong Addr, ulong v)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 8) <= (io.Addr64 + io.Length64)))
                    io.Write(Addr, 8, v);
            }
        }

        public void WriteIOWord(ulong Addr, ushort v)
        {
            foreach (tysos.x86_64.IOResource io in a.ios)
            {
                if (Addr >= io.Addr64 && ((Addr + 2) <= (io.Addr64 + io.Length64)))
                    io.Write(Addr, 2, v);
            }
        }

        public void WriteMemoryByte(ulong Addr, byte v)
        {
            throw new NotImplementedException();
        }

        public void WriteMemoryDWord(ulong Addr, uint v)
        {
            throw new NotImplementedException();
        }

        public void WriteMemoryQWord(ulong Addr, ulong v)
        {
            throw new NotImplementedException();
        }

        public void WriteMemoryWord(ulong Addr, ushort v)
        {
            throw new NotImplementedException();
        }
    }
}
