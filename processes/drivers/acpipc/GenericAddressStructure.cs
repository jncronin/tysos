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

namespace acpipc
{
    abstract class AddrStruct
    {
        public abstract int BitWidth { get; }

        public virtual ulong Read() { return Read(0, (uint)BitWidth); }
        public abstract ulong Read(uint bit_offset, uint bit_width);
        public virtual void Write(ulong val) { Write(val, 0, (uint)BitWidth); }
        public abstract void Write(ulong val, uint bit_offset, uint bit_width);
    }

    class GAS : AddrStruct
    {
        acpipc a;
        int addrspace;
        uint reg_bit_width;
        uint reg_bit_offset;
        int access_size;
        ulong addr;

        public override int BitWidth { get { return (int)reg_bit_width; } }

        public GAS(tysos.RangeResource res, uint offset, acpipc acpi)
        {
            a = acpi;

            addrspace = (int)res.Read(res.Addr64 + offset, 1);
            reg_bit_width = (uint)res.Read(res.Addr64 + offset + 1, 1);
            reg_bit_offset = (uint)res.Read(res.Addr64 + offset + 2, 1);
            access_size = (int)res.Read(res.Addr64 + offset + 3, 1);
            addr = res.Read(res.Addr64 + offset + 4, 8UL);
        }

        public GAS(uint io_addr, uint bit_length, acpipc acpi)
        {
            a = acpi;

            addrspace = 1;
            reg_bit_width = bit_length;
            reg_bit_offset = 0;
            access_size = 1;
            addr = io_addr;
        }

        public override string ToString()
        {
            return "GAS: addrspace: " + addrspace.ToString() +
                ", reg_bit_width: " + reg_bit_width.ToString() +
                ", reg_bit_offset: " + reg_bit_offset.ToString() +
                ", access_size: " + access_size.ToString() +
                ", addr: " + addr.ToString("X");
        }

        public override ulong Read(uint bit_offset, uint bit_width)
        {
            if (addr == 0)
                return 0;
            ulong ret = 0;
            uint bits_read = 0;

            while (bits_read < bit_width)
            {
                ulong val = 0;
                bool valid = false;

                switch (addrspace)
                {
                    case 0:
                        switch (access_size)
                        {
                            case 1:
                                val = a.mi.ReadMemoryByte(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 2:
                                val = a.mi.ReadMemoryWord(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 3:
                                val = a.mi.ReadMemoryDWord(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 4:
                                val = a.mi.ReadMemoryQWord(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                        }
                        break;
                    case 1:
                        switch (access_size)
                        {
                            case 1:
                                val = a.mi.ReadIOByte(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 2:
                                val = a.mi.ReadIOWord(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 3:
                                val = a.mi.ReadIODWord(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 4:
                                val = a.mi.ReadIOQWord(addr + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                        }
                        break;
                    case 2:
                        uint bus = 0;
                        uint dev = (uint)((addr >> 32) & 0xffffUL);
                        uint func = (uint)((addr >> 16) & 0xffffUL);
                        uint reg = (uint)(addr & 0xffffUL);
                        switch (access_size)
                        {
                            case 1:
                                val = a.mi.ReadPCIByte(bus, dev, func, reg + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 2:
                                val = a.mi.ReadPCIWord(bus, dev, func, reg + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                            case 3:
                                val = a.mi.ReadPCIDWord(bus, dev, func, reg + (reg_bit_offset + bit_offset + bits_read) / 8);
                                valid = true;
                                break;
                        }
                        break;
                }

                if(valid == false)
                {
                    System.Diagnostics.Debugger.Log(0, "acpipc", "GAS: Read(): unsupported addrspace: " +
                        addrspace.ToString() + " and reg_bit_width: " + reg_bit_width.ToString());

                    return 0;
                }

                ret |= (val << (int)bits_read);
                switch(access_size)
                {
                    case 1:
                        bits_read += 8;
                        break;
                    case 2:
                        bits_read += 16;
                        break;
                    case 3:
                        bits_read += 32;
                        break;
                    case 4:
                        bits_read += 64;
                        break;
                }
            }

            System.Diagnostics.Debugger.Log(0, "acpipc", "GAS: Read(): read " + ret.ToString("X") + " from " +
                addrspace.ToString() + ":" + addr.ToString("X"));

            return ret;
        }

        public override void Write(ulong val, uint bit_offset, uint bit_width)
        {
            if (addr == 0)
                return;

            uint bits_written = 0;
            while (bits_written < bit_width)
            {
                ulong wval = val >> (int)bits_written;
                uint bits_adjust = 0;
                bool valid = false;
                switch (access_size)
                {
                    case 1:
                        wval &= 0xffUL;
                        bits_adjust = 8;
                        break;
                    case 2:
                        wval &= 0xffffUL;
                        bits_adjust = 16;
                        break;
                    case 4:
                        wval &= 0xffffffffUL;
                        bits_adjust = 32;
                        break;
                    case 8:
                        bits_adjust = 64;
                        break;
                }

                switch (addrspace)
                {
                    case 0:
                        switch (access_size)
                        {
                            case 1:
                                a.mi.WriteMemoryByte(addr + (reg_bit_offset + bit_offset) / 8, (byte)wval);
                                valid = true;
                                break;
                            case 2:
                                a.mi.WriteMemoryWord(addr + (reg_bit_offset + bit_offset) / 8, (ushort)wval);
                                valid = true;
                                break;
                            case 3:
                                a.mi.WriteMemoryDWord(addr + (reg_bit_offset + bit_offset) / 8, (uint)wval);
                                valid = true;
                                break;
                            case 4:
                                a.mi.WriteMemoryQWord(addr + (reg_bit_offset + bit_offset) / 8, wval);
                                valid = true;
                                break;
                        }
                        break;
                    case 1:
                        switch (access_size)
                        {
                            case 1:
                                a.mi.WriteIOByte(addr + (reg_bit_offset + bit_offset) / 8, (byte)wval);
                                valid = true;
                                break;
                            case 2:
                                a.mi.WriteIOWord(addr + (reg_bit_offset + bit_offset) / 8, (ushort)wval);
                                valid = true;
                                break;
                            case 3:
                                a.mi.WriteIODWord(addr + (reg_bit_offset + bit_offset) / 8, (uint)wval);
                                valid = true;
                                break;
                            case 4:
                                a.mi.WriteIOQWord(addr + (reg_bit_offset + bit_offset) / 8, wval);
                                valid = true;
                                break;
                        }
                        break;
                    case 2:
                        uint bus = 0;
                        uint dev = (uint)((addr >> 32) & 0xffffUL);
                        uint func = (uint)((addr >> 16) & 0xffffUL);
                        uint reg = (uint)(addr & 0xffffUL);
                        switch (access_size)
                        {
                            case 1:
                                a.mi.WritePCIByte(bus, dev, func, reg + (reg_bit_offset + bit_offset) / 8, (byte)wval);
                                valid = true;
                                break;
                            case 2:
                                a.mi.WritePCIWord(bus, dev, func, reg + (reg_bit_offset + bit_offset) / 8, (ushort)wval);
                                valid = true;
                                break;
                            case 3:
                                a.mi.WritePCIDWord(bus, dev, func, reg + (reg_bit_offset + bit_offset) / 8, (uint)wval);
                                valid = true;
                                break;
                        }
                        break;
                }

                if(valid == false)
                {
                    System.Diagnostics.Debugger.Log(0, "acpipc", "GAS: Write(): unsupported addrspace: " +
                        addrspace.ToString() + " and reg_bit_width: " + reg_bit_width.ToString());

                    return;
                }

                bits_written += bits_adjust;
            }

            System.Diagnostics.Debugger.Log(0, "acpipc", "GAS: Write(): wrote " + val.ToString("X") + " to " +
                addrspace.ToString() + ":" + addr.ToString("X"));
        }
    }

    class RegGroup : AddrStruct
    {
        AddrStruct A, B;

        public RegGroup(AddrStruct a, AddrStruct b)
        {
            A = a;
            B = b;
        }

        public override int BitWidth
        {
            get
            {
                return A.BitWidth;
            }
        }

        public override ulong Read(uint bit_offset, uint bit_width)
        {
            return A.Read(bit_offset, bit_width) | B.Read(bit_offset, bit_width);
        }

        public override void Write(ulong val, uint bit_offset, uint bit_width)
        {
            A.Write(val, bit_offset, bit_width);
            B.Write(val, bit_offset, bit_width);
        }
    }

    class SplitReg : AddrStruct
    {
        AddrStruct r;
        uint bit_offset;
        int bit_width;

        public SplitReg(AddrStruct R, int BitOffset, int BitWidth)
        {
            r = R;
            bit_offset = (uint)BitOffset;
            bit_width = BitWidth;
        }

        public override int BitWidth
        {
            get
            {
                return bit_width;
            }
        }

        public override ulong Read(uint boffset, uint bit_width)
        {
            return r.Read(bit_offset + boffset, bit_width);
        }

        public override void Write(ulong val, uint boffset, uint bit_width)
        {
            r.Write(val, bit_offset + boffset, bit_width);
        }
    }
}
