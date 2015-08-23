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
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 1);
            if(io != null)
                return (byte)io.Read(Addr, 1);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public uint ReadIODWord(ulong Addr)
        {
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 4);
            if (io != null)
            {
                var ret = (uint)io.Read(Addr, 4);
                //System.Diagnostics.Debugger.Log(0, "acpipc", "ReadIODWord: " + ret.ToString("X8") + " from: " + Addr.ToString("X8"));
                return ret;
            }
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public ulong ReadIOQWord(ulong Addr)
        {
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 8);
            if (io != null)
                return io.Read(Addr, 8);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public ushort ReadIOWord(ulong Addr)
        {
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 2);
            if (io != null)
                return (ushort)io.Read(Addr, 2);
            else
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
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 1);
            if (io != null)
                io.Write(Addr, 1, v);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public void WriteIODWord(ulong Addr, uint v)
        {
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 4);
            if (io != null)
            {
                //System.Diagnostics.Debugger.Log(0, "acpipc", "WriteIODWord: " + v.ToString("X8") + " to: " + Addr.ToString("X8"));
                io.Write(Addr, 4, v);
            }
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public void WriteIOQWord(ulong Addr, ulong v)
        {
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 8);
            if (io != null)
                io.Write(Addr, 8, v);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public void WriteIOWord(ulong Addr, ushort v)
        {
            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 2);
            if (io != null)
                io.Write(Addr, 2, v);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
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

        public void WritePCIDWord(uint bus, uint device, uint func, uint offset, uint val)
        {
            if ((offset & 0x3) != 0)
                throw new Exception("WritePCIDWord: offset not aligned: " + offset.ToString());

            uint addr = offset;
            addr |= func << 8;
            addr |= device << 11;
            addr |= bus << 16;
            addr |= 0x80000000U;

            WriteIODWord(0xcf8, addr);
            WriteIODWord(0xcfc, val);
        }

        public uint ReadPCIDWord(uint bus, uint device, uint func, uint offset)
        {
            if ((offset & 0x3) != 0)
                throw new Exception("WritePCIDWord: offset not aligned: " + offset.ToString());

            uint addr = offset;
            addr |= func << 8;
            addr |= device << 11;
            addr |= bus << 16;
            addr |= 0x80000000U;

            WriteIODWord(0xcf8, addr);

            var ret = ReadIODWord(0xcfc);
            //System.Diagnostics.Debugger.Log(0, "acpipc", "ReadPCIDWord: bus: " + bus.ToString() + ", device: " + device.ToString() + ", func: " + func.ToString() + ", offset: " + offset.ToString() + ", returns: " + ret.ToString("X8"));
            return ReadIODWord(0xcfc);
        }

        public ushort ReadPCIWord(uint bus, uint device, uint func, uint offset)
        {
            uint offset_align = offset & 0xfffc;
            int offset_shift = (int)(offset_align - offset) * 8;

            uint align_val = ReadPCIDWord(bus, device, func, offset_align);

            var ret = (ushort)((align_val >> offset_shift) & 0xffffU);
            //System.Diagnostics.Debugger.Log(0, "acpipc", "ReadPCIWord: bus: " + bus.ToString() + ", device: " + device.ToString() + ", func: " + func.ToString() + ", offset: " + offset.ToString() + ", returns: " + ret.ToString("X4"));
            return ret;
        }

        public byte ReadPCIByte(uint bus, uint device, uint func, uint offset)
        {
            uint offset_align = offset & 0xfffc;
            int offset_shift = (int)(offset_align - offset) * 8;

            uint align_val = ReadPCIDWord(bus, device, func, offset_align);

            var ret = (byte)((align_val >> offset_shift) & 0xffU);
            //System.Diagnostics.Debugger.Log(0, "acpipc", "ReadPCIByte: bus: " + bus.ToString() + ", device: " + device.ToString() + ", func: " + func.ToString() + ", offset: " + offset.ToString() + ", returns: " + ret.ToString("X2"));
            return ret;
        }

        public void WritePCIWord(uint bus, uint device, uint func, uint offset, ushort val)
        {
            uint offset_align = offset & 0xfffc;
            int offset_shift = (int)(offset_align - offset) * 8;

            uint align_val = ReadPCIDWord(bus, device, func, offset_align);
            uint mask = 0xffffU << offset_shift;
            align_val &= ~mask;
            align_val |= ((uint)val) << offset_shift;

            WritePCIDWord(bus, device, func, offset_align, align_val);
        }

        public void WritePCIByte(uint bus, uint device, uint func, uint offset, byte val)
        {
            uint offset_align = offset & 0xfffc;
            int offset_shift = (int)(offset_align - offset) * 8;

            uint align_val = ReadPCIDWord(bus, device, func, offset_align);
            uint mask = 0xffU << offset_shift;
            align_val &= ~mask;
            align_val |= ((uint)val) << offset_shift;

            WritePCIDWord(bus, device, func, offset_align, align_val);
        }
    }
}
