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

namespace tysos
{
    public abstract class Resource
    {
    }

    public class TestResource : Resource
    {
        public override string ToString()
        {
            return "TestResource";
        }
    }

    public abstract class RangeResource : Resource
    {
        public abstract RangeResource Split(uint base_addr, uint length);
        public abstract RangeResource Split(ulong base_addr, ulong length);
        public abstract void Write(uint addr, uint length, uint val);
        public abstract void Write(ulong addr, ulong length, ulong val);
        public abstract uint Read(uint addr, uint length);
        public abstract ulong Read(ulong addr, ulong length);

        public abstract uint Addr32 { get; }
        public abstract ulong Addr64 { get; }
        public abstract uint Length32 { get; }
        public abstract ulong Length64 { get; }

        public virtual uint End32 { get { return Addr32 + Length32; } }
        public virtual ulong End64 { get { return Addr64 + Length64; } }

        public abstract bool Intersects(uint base_addr, uint length);
        public abstract bool Intersects(ulong base_addr, ulong length);
    }

    public abstract class RangeResource32 : RangeResource
    {
        protected uint a, l;

        internal RangeResource32(uint addr, uint length)
        {
            a = addr;
            l = length;
        }

        public override RangeResource Split(uint base_addr, uint length)
        {
            if (base_addr < a)
                return null;
            if ((base_addr + length) > (a + l))
                return null;

            /* Create a new resource of the appropriate subclass */
            System.Reflection.ConstructorInfo ci =
                this.GetType().GetConstructor(new Type[] { typeof(uint), typeof(uint) });

            RangeResource ret = ci.Invoke(new object[] { base_addr, length }) as RangeResource;

            return ret;
        }

        public override RangeResource Split(ulong base_addr_ul, ulong length_ul)
        {
            if (base_addr_ul > uint.MaxValue || base_addr_ul + length_ul > uint.MaxValue)
                return null;

            uint base_addr = (uint)base_addr_ul;
            uint length = (uint)length_ul;

            return Split(base_addr, length);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType().Name);
            sb.Append(": ");
            sb.Append(a.ToString("X8"));
            sb.Append(" - ");
            sb.Append((a + l).ToString("X8"));
            return sb.ToString();
        }

        public override uint Addr32
        {
            get { return a; }
        }
        public override ulong Addr64
        {
            get { return a; }
        }
        public override uint Length32
        {
            get { return l; }
        }
        public override ulong Length64
        {
            get { return l; }
        }

        public override bool Intersects(uint base_addr, uint length)
        {
            if ((base_addr + length) <= a)
                return false;
            if (base_addr >= (a + l))
                return false;
            return true;
        }

        public override bool Intersects(ulong base_addr, ulong length)
        {
            if (base_addr > uint.MaxValue)
                return false;
            if (base_addr + length > uint.MaxValue)
                length = uint.MaxValue - base_addr;
            return Intersects((uint)base_addr, (uint)length);
        }
    }

    public abstract class RangeResource64 : RangeResource
    {
        protected ulong a, l;

        internal RangeResource64(ulong addr, ulong length)
        {
            a = addr;
            l = length;
        }

        public override RangeResource Split(uint base_addr, uint length)
        {
            return Split((ulong)base_addr, (ulong)length);
        }

        public override RangeResource Split(ulong base_addr, ulong length)
        {
            if (base_addr < a)
                return null;
            if ((base_addr + length) > (a + l))
                return null;

            /* Create a new resource of the appropriate subclass */
            System.Reflection.ConstructorInfo ci =
                this.GetType().GetConstructor(new Type[] { typeof(ulong), typeof(ulong) });

            RangeResource ret = ci.Invoke(new object[] { base_addr, length }) as RangeResource;

            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType().Name);
            sb.Append(": ");
            sb.Append(a.ToString("X16"));
            sb.Append(" - ");
            sb.Append((a + l).ToString("X16"));
            return sb.ToString();
        }

        public override uint Addr32
        {
            get { return (uint)a; }
        }
        public override ulong Addr64
        {
            get { return a; }
        }
        public override uint Length32
        {
            get { return (uint)l; }
        }
        public override ulong Length64
        {
            get { return l; }
        }

        public override bool Intersects(ulong base_addr, ulong length)
        {
            if ((base_addr + length) <= a)
                return false;
            if (base_addr >= (a + l))
                return false;
            return true;
        }

        public override bool Intersects(uint base_addr, uint length)
        {
            return Intersects((ulong)base_addr, (ulong)length);
        }
    }

    public class VirtualMemoryResource32 : RangeResource32
    {
        internal VirtualMemoryResource32(uint addr, uint length) : base(addr, length) { }

        internal RangeResource mapped_to;

        public RangeResource MappedTo { get { return mapped_to; } }

        public override unsafe void Write(uint addr, uint length, uint val)
        {
            switch (length)
            {
                case 1:
                    *(byte*)addr = (byte)(val & 0xff);
                    break;
                case 2:
                    *(ushort*)addr = (ushort)(val & 0xffff);
                    break;
                case 4:
                case 8:
                    *(uint*)addr = val;
                    break;
            }
        }

        public override unsafe void Write(ulong addr, ulong length, ulong val)
        {
            switch (length)
            {
                case 1:
                    *(byte*)addr = (byte)(val & 0xff);
                    break;
                case 2:
                    *(ushort*)addr = (ushort)(val & 0xffff);
                    break;
                case 4:
                    *(uint*)addr = (uint)(val & 0xffffffff);
                    break;
                case 8:
                    *(ulong*)addr = val;
                    break;
            }
        }

        public override unsafe uint Read(uint addr, uint length)
        {
            switch (length)
            {
                case 1:
                    return *(byte*)addr;
                case 2:
                    return *(ushort*)addr;
                case 4:
                case 8:
                    return *(uint*)addr;
            }
            return 0;
        }

        public override unsafe ulong Read(ulong addr, ulong length)
        {
            switch (length)
            {
                case 1:
                    return *(byte*)addr;
                case 2:
                    return *(ushort*)addr;
                case 4:
                    return *(uint*)addr;
                case 8:
                    return *(ulong*)addr;
            }
            return 0;
        }

        public unsafe byte[] ToArray()
        {
            if (l > (uint)Int32.MaxValue)
                throw new Exception("region is too big for byte array");
            throw new NotImplementedException();
            //return libsupcs.TysosArrayType.CreateByteArray((byte*)a, (int)l);
        }

        public override string ToString()
        {
            if (mapped_to == null)
                return base.ToString();
            else
                return base.ToString() + " mapped to " + mapped_to.ToString();
        }
    }

    public class VirtualMemoryResource64 : RangeResource64
    {
        internal VirtualMemoryResource64(ulong addr, ulong length) : base(addr, length) { }

        internal RangeResource mapped_to;

        public RangeResource MappedTo { get { return mapped_to; } }

        public override unsafe void Write(uint addr, uint length, uint val)
        {
            switch (length)
            {
                case 1:
                    *(byte*)addr = (byte)(val & 0xff);
                    break;
                case 2:
                    *(ushort*)addr = (ushort)(val & 0xffff);
                    break;
                case 4:
                case 8:
                    *(uint*)addr = val;
                    break;
            }
        }

        public override unsafe void Write(ulong addr, ulong length, ulong val)
        {
            switch (length)
            {
                case 1:
                    *(byte*)addr = (byte)(val & 0xff);
                    break;
                case 2:
                    *(ushort*)addr = (ushort)(val & 0xffff);
                    break;
                case 4:
                    *(uint*)addr = (uint)(val & 0xffffffff);
                    break;
                case 8:
                    *(ulong*)addr = val;
                    break;
            }
        }

        public override unsafe uint Read(uint addr, uint length)
        {
            switch (length)
            {
                case 1:
                    return *(byte*)addr;
                case 2:
                    return *(ushort*)addr;
                case 4:
                case 8:
                    return *(uint*)addr;
            }
            return 0;
        }

        public override unsafe ulong Read(ulong addr, ulong length)
        {
            switch (length)
            {
                case 1:
                    return *(byte*)addr;
                case 2:
                    return *(ushort*)addr;
                case 4:
                    return *(uint*)addr;
                case 8:
                    return *(ulong*)addr;
            }
            return 0;
        }

        public unsafe byte[] ToArray()
        {
            if (l > (ulong)Int32.MaxValue)
                throw new Exception("region is too big for byte array");
            return libsupcs.Array.CreateSZArray<byte>((int)l, (void*)Addr64);
            //return libsupcs.TysosArrayType.CreateByteArray((byte*)a, (int)l);
        }

        public void Map(PhysicalMemoryResource64 pmem)
        {
            pmem.Map(this);
        }

        public void Map()
        {
            ulong buf = Program.arch.GetBuffer(Length64);
            if (buf == 0)
            {
                if (Length64 == 0x1000)
                {
                    // Try using standard page mapping instead
                    ulong pmemaddr = Program.arch.VirtMem.map_page(Addr64);
                    mapped_to = new PhysicalMemoryResource64(pmemaddr, 0x1000);
                }
                else
                    throw new Exception("GetBuffer failed");
            }
            else
            {
                PhysicalMemoryResource64 pmem = new PhysicalMemoryResource64(buf, Length64);
                pmem.Map(this);
            }
        }

        public override string ToString()
        {
            if (mapped_to == null)
                return base.ToString();
            else
                return base.ToString() + " mapped to " + mapped_to.ToString();
        }
    }

    public class PhysicalMemoryResource32 : RangeResource32
    {
        internal PhysicalMemoryResource32(uint addr, uint length) : base(addr, length) { }

        public override void Write(uint addr, uint length, uint val)
        {
            throw new NotSupportedException();
        }

        public override void Write(ulong addr, ulong length, ulong val)
        {
            throw new NotSupportedException();
        }

        public override uint Read(uint addr, uint length)
        {
            throw new NotSupportedException();
        }

        public override ulong Read(ulong addr, ulong length)
        {
            throw new NotSupportedException();
        }

        public void Map(VirtualMemoryResource32 vmem)
        {
            uint max_length = this.Length32;
            if (vmem.Length32 > max_length)
                max_length = vmem.Length32;

            if (max_length == 0)
                return;

            ulong offset = 0;
            while (offset < max_length)
            {
                Program.arch.VirtMem.map_page(vmem.Addr64 + offset, this.Addr64 + offset);
                offset += Program.arch.PageSize;
            }

            vmem.mapped_to = this;
        }

        public void Map(VirtualMemoryResource64 vmem)
        {
            uint max_length = this.Length32;
            if (vmem.Length32 > max_length)
                max_length = vmem.Length32;

            if (max_length == 0)
                return;

            ulong offset = 0;
            while (offset < max_length)
            {
                Program.arch.VirtMem.map_page(vmem.Addr64 + offset, this.Addr64 + offset);
                offset += Program.arch.PageSize;
            }

            vmem.mapped_to = this;
        }
    }

    public class PhysicalMemoryResource64 : RangeResource64
    {
        internal PhysicalMemoryResource64(ulong addr, ulong length) : base(addr, length) { }

        public override void Write(uint addr, uint length, uint val)
        {
            throw new NotSupportedException();
        }

        public override void Write(ulong addr, ulong length, ulong val)
        {
            throw new NotSupportedException();
        }

        public override uint Read(uint addr, uint length)
        {
            throw new NotSupportedException();
        }

        public override ulong Read(ulong addr, ulong length)
        {
            throw new NotSupportedException();
        }

        public void Map(VirtualMemoryResource32 vmem)
        {
            ulong max_length = this.Length64;
            if (vmem.Length64 > max_length)
                max_length = vmem.Length64;

            if (max_length == 0)
                return;

            ulong offset = 0;
            while (offset < max_length)
            {
                Program.arch.VirtMem.map_page(vmem.Addr64 + offset, this.Addr64 + offset);
                offset += Program.arch.PageSize;
            }

            vmem.mapped_to = this;
        }

        public void Map(VirtualMemoryResource64 vmem)
        {
            ulong max_length = this.Length64;
            if (vmem.Length64 > max_length)
                max_length = vmem.Length64;

            if (max_length == 0)
                return;

            ulong offset = 0;
            while (offset < max_length)
            {
                Program.arch.VirtMem.map_page(vmem.Addr64 + offset, this.Addr64 + offset);
                offset += Program.arch.PageSize;
            }

            vmem.mapped_to = this;
        }
    }
}

namespace tysos.Resources
{
    public abstract class InterruptLine : Resource
    {
        public delegate bool InterruptHandler();
        public abstract bool RegisterHandler(InterruptHandler handler);
        public virtual void Enable() { }
        public virtual void Disable() { }
        public abstract string ShortName { get; }
    }

    public abstract class CpuInterruptLine : InterruptLine
    {
        protected internal Cpu cpu;
        protected internal int cpu_int_no;

        public int CpuInterrupt { get { return cpu_int_no; } }
        public int CpuID { get { return cpu.Id; } }

        public override string ShortName
        {
            get
            {
                return "CPU" + cpu.Id.ToString() + "." + cpu_int_no.ToString();
            }
        }
    }

    public abstract class SharedInterruptLine : InterruptLine
    {
        List<InterruptHandler> handlers = new List<InterruptHandler>();
        List<Thread> handler_threads = new List<Thread>();
        protected InterruptLine shared_line;

        int spurious = 0;
        const int SPURIOUS_LIMIT = 100;

        public SharedInterruptLine(InterruptLine SharedLine)
        { shared_line = SharedLine; }

        public override bool RegisterHandler(InterruptHandler handler)
        {
            System.Diagnostics.Debugger.Log(0, "SharedInterruptLine", ShortName + " RegisterHandler");
            handlers.Add(handler);
            handler_threads.Add(Program.arch.CurrentCpu.CurrentThread);
            if(handlers.Count == 1)
            {
                if (shared_line != null)
                    shared_line.RegisterHandler(new InterruptHandler(Handler));
            }
            return true;
        }

        bool Handler()
        {
            foreach(InterruptHandler h in handlers)
            {
                if (h() == true)
                {
                    if (spurious > 0)
                        spurious--;
                    else
                        spurious = 0;   // handle multiple accesses creating negative number

                    return true;
                }
            }

            StringBuilder sb = new StringBuilder("ERROR: Spurious interrupt - not handled.  ");
            sb.Append("Potential culprits: ");
            for(int i = 0; i < handler_threads.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(handler_threads[i].ProcessName);
            }

            spurious++;

            if(spurious > SPURIOUS_LIMIT)
            {
                sb.Append(" - disabling interrupt line");
                shared_line.Disable();
            }

            System.Diagnostics.Debugger.Log(0, this.ToString(), sb.ToString());
            return true;
        }
    }
}
