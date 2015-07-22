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
    
    public abstract class RangeResource
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
    }

    public abstract class RangeResource32 : RangeResource
    {
        struct range { public uint addr; public uint length; }
        List<range> free_ranges;

        internal RangeResource32(uint addr, uint length)
        {
            free_ranges = new List<range>();
            free_ranges.Add(new range { addr = addr, length = length });
        }

        public override RangeResource Split(uint base_addr, uint length)
        {
            int range_to_split = -1;
            for(int i = 0; i < free_ranges.Count; i++)
            {
                if (base_addr >= free_ranges[i].addr &&
                    (base_addr + length) <= (free_ranges[i].addr + free_ranges[i].length))
                {
                    range_to_split = i;
                    break;
                }
            }

            if (range_to_split == -1)
                return null;

            /* Create a new resource of the appropriate subclass */
            System.Reflection.ConstructorInfo ci = 
                this.GetType().GetConstructor(new Type[] { typeof(uint), typeof(uint) });

            RangeResource ret = ci.Invoke(new object[] { base_addr, length }) as RangeResource;

            /* split the original chunk into parts
             * 
             * There are 4 possibilities:
             * 
             * 1) The new chunk is all of the old chunk - delete the old chunk
             * 2) The new chunk starts at the beginning of the old - increase the
             *          base_addr of the old chunk and shorten it
             * 3) The new chunk ends at the end of the old - shorten the old one
             * 4) The new chunk is within the old one - we need to shorten the old one
             *          and add a new one at the end
             */

            uint old_base = free_ranges[range_to_split].addr;
            uint old_len = free_ranges[range_to_split].length;

            if (base_addr == old_base && length == old_len)
                free_ranges.RemoveAt(range_to_split);
            else if (base_addr == old_base)
            {
                range new_range = new range { addr = old_base + length, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else if(base_addr + length == old_base + old_len)
            {
                range new_range = new range { addr = old_base, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else
            {
                range new_range_1 = new range { addr = old_base, length = base_addr - old_base };
                range new_range_2 = new range { addr = base_addr + length, length = old_base + old_len - base_addr - length };
                free_ranges[range_to_split] = new_range_1;
                free_ranges.Add(new_range_2);
            }         

            return ret;
        }

        public override RangeResource Split(ulong base_addr_ul, ulong length_ul)
        {
            if (base_addr_ul > uint.MaxValue || base_addr_ul + length_ul > uint.MaxValue)
                return null;

            uint base_addr = (uint)base_addr_ul;
            uint length = (uint)length_ul;

            int range_to_split = -1;
            for (int i = 0; i < free_ranges.Count; i++)
            {
                if (base_addr >= free_ranges[i].addr &&
                    (base_addr + length) <= (free_ranges[i].addr + free_ranges[i].length))
                {
                    range_to_split = i;
                    break;
                }
            }

            if (range_to_split == -1)
                return null;

            /* Create a new resource of the appropriate subclass */
            System.Reflection.ConstructorInfo ci =
                this.GetType().GetConstructor(new Type[] { typeof(uint), typeof(uint) });

            RangeResource ret = ci.Invoke(new object[] { base_addr, length }) as RangeResource;

            /* split the original chunk into parts
             * 
             * There are 4 possibilities:
             * 
             * 1) The new chunk is all of the old chunk - delete the old chunk
             * 2) The new chunk starts at the beginning of the old - increase the
             *          base_addr of the old chunk and shorten it
             * 3) The new chunk ends at the end of the old - shorten the old one
             * 4) The new chunk is within the old one - we need to shorten the old one
             *          and add a new one at the end
             */

            uint old_base = free_ranges[range_to_split].addr;
            uint old_len = free_ranges[range_to_split].length;

            if (base_addr == old_base && length == old_len)
                free_ranges.RemoveAt(range_to_split);
            else if (base_addr == old_base)
            {
                range new_range = new range { addr = old_base + length, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else if (base_addr + length == old_base + old_len)
            {
                range new_range = new range { addr = old_base, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else
            {
                range new_range_1 = new range { addr = old_base, length = base_addr - old_base };
                range new_range_2 = new range { addr = base_addr + length, length = old_base + old_len - base_addr - length };
                free_ranges[range_to_split] = new_range_1;
                free_ranges.Add(new_range_2);
            }

            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType().Name);
            sb.Append(": \n");
            foreach(range r in free_ranges)
            {
                sb.Append("    ");
                sb.Append(r.addr.ToString("X8"));
                sb.Append(" - ");
                sb.Append((r.addr + r.length).ToString("X8"));
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public override uint Addr32
        {
            get { if (free_ranges.Count > 0) return free_ranges[0].addr; else return 0; }
        }
        public override ulong Addr64
        {
            get { if (free_ranges.Count > 0) return free_ranges[0].addr; else return 0; }
        }
        public override uint Length32
        {
            get { if (free_ranges.Count > 0) return free_ranges[0].length; else return 0; }
        }
        public override ulong Length64
        {
            get { if (free_ranges.Count > 0) return free_ranges[0].length; else return 0; }
        }
    }

    public abstract class RangeResource64 : RangeResource
    {
        struct range { public ulong addr; public ulong length; }
        List<range> free_ranges;

        internal RangeResource64(ulong addr, ulong length)
        {
            free_ranges = new List<range>();
            free_ranges.Add(new range { addr = addr, length = length });
        }

        public override RangeResource Split(uint base_addr, uint length)
        {
            int range_to_split = -1;
            for (int i = 0; i < free_ranges.Count; i++)
            {
                if (base_addr >= free_ranges[i].addr &&
                    (base_addr + length) <= (free_ranges[i].addr + free_ranges[i].length))
                {
                    range_to_split = i;
                    break;
                }
            }

            if (range_to_split == -1)
                return null;

            /* Create a new resource of the appropriate subclass */
            System.Reflection.ConstructorInfo ci =
                this.GetType().GetConstructor(new Type[] { typeof(uint), typeof(uint) });

            RangeResource ret = ci.Invoke(new object[] { base_addr, length }) as RangeResource;

            /* split the original chunk into parts
             * 
             * There are 4 possibilities:
             * 
             * 1) The new chunk is all of the old chunk - delete the old chunk
             * 2) The new chunk starts at the beginning of the old - increase the
             *          base_addr of the old chunk and shorten it
             * 3) The new chunk ends at the end of the old - shorten the old one
             * 4) The new chunk is within the old one - we need to shorten the old one
             *          and add a new one at the end
             */

            ulong old_base = free_ranges[range_to_split].addr;
            ulong old_len = free_ranges[range_to_split].length;

            if (base_addr == old_base && length == old_len)
                free_ranges.RemoveAt(range_to_split);
            else if (base_addr == old_base)
            {
                range new_range = new range { addr = old_base + length, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else if (base_addr + length == old_base + old_len)
            {
                range new_range = new range { addr = old_base, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else
            {
                range new_range_1 = new range { addr = old_base, length = base_addr - old_base };
                range new_range_2 = new range { addr = base_addr + length, length = old_base + old_len - base_addr - length };
                free_ranges[range_to_split] = new_range_1;
                free_ranges.Add(new_range_2);
            }

            return ret;
        }

        public override RangeResource Split(ulong base_addr, ulong length)
        {
            int range_to_split = -1;
            for (int i = 0; i < free_ranges.Count; i++)
            {
                if (base_addr >= free_ranges[i].addr &&
                    (base_addr + length) <= (free_ranges[i].addr + free_ranges[i].length))
                {
                    range_to_split = i;
                    break;
                }
            }

            if (range_to_split == -1)
                return null;

            /* Create a new resource of the appropriate subclass */
            System.Reflection.ConstructorInfo ci =
                this.GetType().GetConstructor(new Type[] { typeof(uint), typeof(uint) });

            RangeResource ret = ci.Invoke(new object[] { base_addr, length }) as RangeResource;

            /* split the original chunk into parts
             * 
             * There are 4 possibilities:
             * 
             * 1) The new chunk is all of the old chunk - delete the old chunk
             * 2) The new chunk starts at the beginning of the old - increase the
             *          base_addr of the old chunk and shorten it
             * 3) The new chunk ends at the end of the old - shorten the old one
             * 4) The new chunk is within the old one - we need to shorten the old one
             *          and add a new one at the end
             */

            ulong old_base = free_ranges[range_to_split].addr;
            ulong old_len = free_ranges[range_to_split].length;

            if (base_addr == old_base && length == old_len)
                free_ranges.RemoveAt(range_to_split);
            else if (base_addr == old_base)
            {
                range new_range = new range { addr = old_base + length, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else if (base_addr + length == old_base + old_len)
            {
                range new_range = new range { addr = old_base, length = old_len - length };
                free_ranges[range_to_split] = new_range;
            }
            else
            {
                range new_range_1 = new range { addr = old_base, length = base_addr - old_base };
                range new_range_2 = new range { addr = base_addr + length, length = old_base + old_len - base_addr - length };
                free_ranges[range_to_split] = new_range_1;
                free_ranges.Add(new_range_2);
            }

            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType().Name);
            sb.Append(": \n");
            foreach (range r in free_ranges)
            {
                sb.Append("    ");
                sb.Append(r.addr.ToString("X16"));
                sb.Append(" - ");
                sb.Append((r.addr + r.length).ToString("X16"));
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public override uint Addr32
        {
            get { if (free_ranges.Count > 0) return (uint)(free_ranges[0].addr & 0xffffffff); else return 0; }
        }
        public override ulong Addr64
        {
            get { if (free_ranges.Count > 0) return free_ranges[0].addr; else return 0; }
        }
        public override uint Length32
        {
            get { if (free_ranges.Count > 0) return (uint)(free_ranges[0].length & 0xffffffff); else return 0; }
        }
        public override ulong Length64
        {
            get { if (free_ranges.Count > 0) return free_ranges[0].length; else return 0; }
        }
    }

    public class VirtualMemoryResource32 : RangeResource32
    {
        internal VirtualMemoryResource32(uint addr, uint length) : base(addr, length) { }

        public override unsafe void Write(uint addr, uint length, uint val)
        {
            switch(length)
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
            switch(length)
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
    }

    public class VirtualMemoryResource64 : RangeResource64
    {
        internal VirtualMemoryResource64(ulong addr, ulong length) : base(addr, length) { }

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

            Program.arch.VirtMem.map_page(vmem.Addr64, this.Addr64);
        }

        public void Map(VirtualMemoryResource64 vmem)
        {
            uint max_length = this.Length32;
            if (vmem.Length32 > max_length)
                max_length = vmem.Length32;

            if (max_length == 0)
                return;

            Program.arch.VirtMem.map_page(vmem.Addr64, this.Addr64);
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

            Program.arch.VirtMem.map_page(vmem.Addr64, this.Addr64);
        }

        public void Map(VirtualMemoryResource64 vmem)
        {
            ulong max_length = this.Length64;
            if (vmem.Length64 > max_length)
                max_length = vmem.Length64;

            if (max_length == 0)
                return;

            Program.arch.VirtMem.map_page(vmem.Addr64, this.Addr64);
        }
    }
}
