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
        internal bool is_vbox = false;

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

        // TODO: make ~16 cached Pmem areas
        tysos.PhysicalMemoryResource64 cur_pmem = null;
        tysos.VirtualMemoryResource64 vmem_page = null;

        private tysos.VirtualMemoryResource64 map_page(ulong addr)
        {
            if(vmem_page == null)
            {
                vmem_page = a.vmems.Alloc(0x1000, 0x1000);
            }

            // is this the currently mapped page?
            var pmem_page_start = addr & ~0xfffUL;
            if(cur_pmem == null || cur_pmem.Addr64 != pmem_page_start)
            {
                cur_pmem = a.pmems.AllocFixed(pmem_page_start, 0x1000);
                cur_pmem.Map(vmem_page);
            }

            return vmem_page;
        }


        public byte ReadMemoryByte(ulong Addr)
        {
            System.Diagnostics.Debugger.Log(0, null, "Read byte at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            return (byte)vm.Read((Addr & 0xfffUL) + vm.Addr64, 1);
        }

        public uint ReadMemoryDWord(ulong Addr)
        {
            System.Diagnostics.Debugger.Log(0, null, "Read dword at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            return (uint)vm.Read((Addr & 0xfffUL) + vm.Addr64, 4);
        }

        public ulong ReadMemoryQWord(ulong Addr)
        {
            System.Diagnostics.Debugger.Log(0, null, "Read qword at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            return vm.Read((Addr & 0xfffUL) + vm.Addr64, 8);
        }

        public ushort ReadMemoryWord(ulong Addr)
        {
            System.Diagnostics.Debugger.Log(0, null, "Read word at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            return (ushort)vm.Read((Addr & 0xfffUL) + vm.Addr64, 2);
        }

        StringBuilder vbox_dbg;

        void vbox_write(byte v)
        {
            vbox_write(v.ToString("X2"));
            vbox_write_char(0);
        }

        void vbox_write(ushort v)
        {
            vbox_write(v.ToString("X4"));
            vbox_write_char(0);
        }

        void vbox_write(uint v)
        {
            vbox_write(v.ToString("X8"));
            vbox_write_char(0);
        }

        void vbox_write(string s)
        {
            foreach (char c in s)
                vbox_write_char((byte)c);
        }

        void vbox_write_char(byte v)
        {
                if(vbox_dbg == null)
                    vbox_dbg = new StringBuilder();
                if (v == 0 || v == '\n')
                {
                    if (vbox_dbg != null)
                    {
                        System.Diagnostics.Debugger.Log(0, "acpipc", "VBOXDBG: " + vbox_dbg.ToString());
                        vbox_dbg = null;
                    }
                }
                else
                    vbox_dbg.Append((char)v);
        }

        public void WriteIOByte(ulong Addr, byte v)
        {
            /* If its a write to the vbox dbg port, log it */
            if(Addr == 0x3001 && is_vbox)
            {
                vbox_write_char(v);
            }
            else if(Addr == 0x3000 && is_vbox)
            {
                vbox_write(v);
            }

            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 1);
            if (io != null)
                io.Write(Addr, 1, v);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public void WriteIODWord(ulong Addr, uint v)
        {
            /* If its a write to the vbox dbg port, log it */
            if (Addr == 0x3000 && is_vbox)
            {
                vbox_write(v);
            }

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
            /* If its a write to the vbox dbg port, log it */
            if (Addr == 0x3000 && is_vbox)
            {
                vbox_write(v);
            }

            tysos.x86_64.IOResource io = a.ios.Contains(Addr, 2);
            if (io != null)
                io.Write(Addr, 2, v);
            else
                throw new Exception("Invalid IO port: " + Addr.ToString("X"));
        }

        public void WriteMemoryByte(ulong Addr, byte v)
        {
            System.Diagnostics.Debugger.Log(0, null, "Write byte at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            vm.Write((Addr & 0xfffUL) + vm.Addr64, 1, v);
        }

        public void WriteMemoryDWord(ulong Addr, uint v)
        {
            System.Diagnostics.Debugger.Log(0, null, "Write dword at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            vm.Write((Addr & 0xfffUL) + vm.Addr64, 4, v);
        }

        public void WriteMemoryQWord(ulong Addr, ulong v)
        {
            System.Diagnostics.Debugger.Log(0, null, "Write qword at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            vm.Write((Addr & 0xfffUL) + vm.Addr64, 8, v);
        }

        public void WriteMemoryWord(ulong Addr, ushort v)
        {
            System.Diagnostics.Debugger.Log(0, null, "Write word at " + Addr.ToString("X16"));
            var vm = map_page(Addr);
            vm.Write((Addr & 0xfffUL) + vm.Addr64, 2, v);
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
