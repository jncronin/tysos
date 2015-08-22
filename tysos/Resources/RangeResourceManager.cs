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

namespace tysos.Resources
{
    public class RangeResourceManager<T> where T : RangeResource
    {
        List<T> free = new List<T>();
        List<T> used = new List<T>();

        public void AddFree(T region)
        {
            free.Add(region);
        }

        public T AllocFixed(ulong base_addr, ulong length)
        { return AllocFixed(base_addr, length, false); }

        public T AllocFixed(ulong base_addr, ulong length, bool can_overwrite)
        {
            /* First, determine if the requested range is available */
            T src = null;

            foreach (T free_item in free)
            {
                if (base_addr >= free_item.Addr64 && ((base_addr + length) <= free_item.End64))
                {
                    src = free_item;
                    break;
                }
            }

            if (src == null)
            {
                System.Diagnostics.Debugger.Log(0, Program.arch.CurrentCpu.CurrentProcess.name,
                    "AllocFixed: source range not found for " +
                    base_addr.ToString("X16") + " - " + (base_addr + length).ToString("X16"));
                return null;
            }

            if (can_overwrite == false)
            {
                /* Now ensure we don't override any other allocated region */
                foreach (T used_item in used)
                {
                    if (used_item.Intersects(base_addr, length))
                    {
                        System.Diagnostics.Debugger.Log(0, Program.arch.CurrentCpu.CurrentProcess.name,
                            "AllocFixed: requested range " +
                            base_addr.ToString("X16") + " - " + (base_addr + length).ToString("X16") +
                            " overrides allocated region: " + used_item.Addr64.ToString("X16") +
                            " - " + used_item.End64.ToString("X16"));
                        return null;
                    }
                }
            }

            /* Create the range to return */
            T ret = src.Split(base_addr, length) as T;
            used.Add(ret);

            return ret;
        }

        public T Contains(ulong base_addr, ulong length)
        {
            foreach(T free_item in free)
            {
                if (base_addr >= free_item.Addr64 && (base_addr + length) <= free_item.End64)
                    return free_item;
            }
            return null;
        }

        public T Alloc(ulong length)
        { return Alloc(length, 1); }

        public T Alloc(ulong length, ulong align)
        { return Alloc(length, align, false); }

        public T Alloc(ulong length, ulong align, bool bits32)
        {
            foreach(T free_item in free)
            {
                ulong test_addr = free_item.Addr64;
                while(test_addr + length < free_item.End64)
                {
                    if (bits32 && test_addr + length > 0x100000000UL)
                        break;

                    bool intersects = false;
                    ulong intersects_end = 0;
                    test_addr = util.align(test_addr, align);

                    foreach(T used_item in used)
                    {
                        if(used_item.Intersects(test_addr, length))
                        {
                            intersects = true;
                            intersects_end = used_item.End64;
                            break;
                        }
                    }

                    if (intersects)
                        test_addr = intersects_end;
                    else
                    {
                        /* Create the range to return */
                        T ret = free_item.Split(test_addr, length) as T;
                        used.Add(ret);

                        return ret;
                    }
                }
            }

            // No free range found
            return null;
        }
    }

    public class PhysicalMemoryRangeManager : RangeResourceManager<PhysicalMemoryResource64>
    {
        public void Init(IEnumerable<lib.File.Property> props)
        {
            foreach(lib.File.Property prop in props)
            {
                if(prop.Name == "pmem")
                {
                    PhysicalMemoryResource64 pmem = prop.Value as PhysicalMemoryResource64;
                    if (pmem != null)
                        AddFree(pmem);
                }
            }
        }
    }

    public class VirtualMemoryRangeManager : RangeResourceManager<VirtualMemoryResource64>
    {
        public void Init(IEnumerable<lib.File.Property> props)
        {
            foreach (lib.File.Property prop in props)
            {
                if (prop.Name == "vmem")
                {
                    VirtualMemoryResource64 vmem = prop.Value as VirtualMemoryResource64;
                    if (vmem != null)
                        AddFree(vmem);
                }
            }
        }
    }
}
