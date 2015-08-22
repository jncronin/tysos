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

    public class IORangeManager : Resources.RangeResourceManager<IOResource>
    {
        public void Init(IEnumerable<lib.File.Property> props)
        {
            foreach (lib.File.Property prop in props)
            {
                if (prop.Name == "io")
                {
                    IOResource io = prop.Value as IOResource;
                    if (io != null)
                        AddFree(io);
                }
            }
        }
    }

    public class x86_64_Interrupt : Resources.CpuInterruptLine
    {
        public unsafe override bool RegisterHandler(InterruptHandler handler)
        {
            /* Build a new interrupt handler which does:

            push rax
            push rbx
            push rcx
            push rdx
            push rsi
            push rdi
            push rbp
            push r8
            push r9
            push r10
            push r11
            push r12
            push r13
            push r14
            push r15
            push all xmms

            mov rdi, target
            call handler

            if returns true send eoi

            restore all xmms
            restore all gprs

            iret
            
            */

            List<byte> func = new List<byte>();
            func.AddRange(new byte[] { 0x9c, 0x50, 0x53, 0x51, 0x52, 0x56,
            0x57, 0x55, 0x41, 0x50, 0x41, 0x51, 0x41, 0x52, 0x42, 0x53,
            0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x81,
            0xec, 0x80, 0x00, 0x00, 0x00, });

            func.AddRange(new byte[]
            {
                0x66, 0x0f, 0xd6, 0x04, 0x24,
                0x66, 0x0f, 0xd6, 0x4c, 0x24, 0x08,
                0x66, 0x0f, 0xd6, 0x54, 0x24, 0x10,
                0x66, 0x0f, 0xd6, 0x5c, 0x24, 0x18,
                0x66, 0x0f, 0xd6, 0x64, 0x24, 0x20,
                0x66, 0x0f, 0xd6, 0x6c, 0x24, 0x28,
                0x66, 0x0f, 0xd6, 0x74, 0x24, 0x30,
                0x66, 0x0f, 0xd6, 0x7c, 0x24, 0x38,
                0x66, 0x44, 0x0f, 0xd6, 0x44, 0x24, 0x40,
                0x66, 0x44, 0x0f, 0xd6, 0x4c, 0x24, 0x48,
                0x66, 0x44, 0x0f, 0xd6, 0x54, 0x24, 0x50,
                0x66, 0x44, 0x0f, 0xd6, 0x5c, 0x24, 0x58,
                0x66, 0x44, 0x0f, 0xd6, 0x64, 0x24, 0x60,
                0x66, 0x44, 0x0f, 0xd6, 0x6c, 0x24, 0x68,
                0x66, 0x44, 0x0f, 0xd6, 0x74, 0x24, 0x70,
                0x66, 0x44, 0x0f, 0xd6, 0x7c, 0x24, 0x78,

                0x48, 0xbf
            });

            ulong target = libsupcs.CastOperations.ReinterpretAsUlong(handler.Target);
            func.AddRange(BitConverter.GetBytes(target));

            func.Add(0xe8);

            uint meth = (uint)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(handler);
            func.AddRange(BitConverter.GetBytes(meth));

            func.AddRange(new byte[] { 0x48, 0xa9, 0x01, 0x00, 0x00, 0x00,
                0x74, 0x0f,
                0x48, 0xbf
            });

            ulong cpu_ptr = libsupcs.CastOperations.ReinterpretAsUlong(cpu);
            func.AddRange(BitConverter.GetBytes(cpu_ptr));

            func.Add(0xe8);
            uint send_lapic = (uint)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(new SendLapicEOIDelegate(SendLapicEOI));
            func.AddRange(BitConverter.GetBytes(send_lapic));

            func.AddRange(new byte[]
            {
                0xf3, 0x0f, 0x7e, 0x04, 0x24,
                0xf3, 0x0f, 0x7e, 0x4c, 0x24, 0x08,
                0xf3, 0x0f, 0x7e, 0x54, 0x24, 0x10,
                0xf3, 0x0f, 0x7e, 0x5c, 0x24, 0x18,
                0xf3, 0x0f, 0x7e, 0x64, 0x24, 0x20,
                0xf3, 0x0f, 0x7e, 0x6c, 0x24, 0x28,
                0xf3, 0x0f, 0x7e, 0x74, 0x24, 0x30,
                0xf3, 0x0f, 0x7e, 0x7c, 0x24, 0x38,
                0xf3, 0x44, 0x0f, 0x7e, 0x44, 0x24, 0x40,
                0xf3, 0x44, 0x0f, 0x7e, 0x4c, 0x24, 0x48,
                0xf3, 0x44, 0x0f, 0x7e, 0x54, 0x24, 0x50,
                0xf3, 0x44, 0x0f, 0x7e, 0x5c, 0x24, 0x58,
                0xf3, 0x44, 0x0f, 0x7e, 0x64, 0x24, 0x60,
                0xf3, 0x44, 0x0f, 0x7e, 0x6c, 0x24, 0x68,
                0xf3, 0x44, 0x0f, 0x7e, 0x74, 0x24, 0x70,
                0xf3, 0x44, 0x0f, 0x7e, 0x7c, 0x24, 0x78,
                0x48, 0x81, 0xc4, 0x80, 0x00, 0x00, 0x00,
                0x41, 0x5f,
                0x41, 0x5e,
                0x41, 0x5d,
                0x41, 0x5c,
                0x41, 0x5b,
                0x41, 0x5a,
                0x41, 0x59,
                0x41, 0x58,
                0x5d,
                0x5f,
                0x5e,
                0x5a,
                0x59,
                0x5b,
                0x58,
                0x9d,
                0x48, 0xcf
            });

            byte[] arr_func = func.ToArray();
            ulong func_ptr = (ulong)libsupcs.MemoryOperations.GetInternalArray(arr_func);

            // TODO make Interrupts cpu-specific
            Program.arch.Interrupts.InstallHandler(cpu_int_no, func_ptr);

            return true;
        }

        delegate void SendLapicEOIDelegate(x86_64_cpu c);

        static void SendLapicEOI(x86_64_cpu c)
        {
            c.CurrentLApic.SendEOI();
        }
    }
}
