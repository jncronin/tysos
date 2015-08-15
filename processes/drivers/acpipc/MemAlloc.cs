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
    partial class acpipc
    {
        List<tysos.VirtualMemoryResource64> vmem_allocated = new List<tysos.VirtualMemoryResource64>();
        List<tysos.PhysicalMemoryResource64> pmem_allocated = new List<tysos.PhysicalMemoryResource64>();

        tysos.PhysicalMemoryResource64 AllocPmemFixed(ulong base_addr, ulong length)
        { return AllocPmemFixed(base_addr, length, true); }
        tysos.PhysicalMemoryResource64 AllocPmemFixed(ulong base_addr, ulong length, bool can_override)
        {
            /* First, determine if the requested range is available */
            tysos.PhysicalMemoryResource64 src = null;

            foreach (tysos.PhysicalMemoryResource64 pmem in pmems)
            {
                if (base_addr >= pmem.Addr64 && ((base_addr + length) <= (pmem.Addr64 + pmem.Length64)))
                {
                    src = pmem;
                    break;
                }
            }

            if (src == null)
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "AllocPmemFixed: source range not found for " +
                    base_addr.ToString("X16") + " - " + (base_addr + length).ToString("X16"));
                return null;
            }

            if (can_override == false)
            {
                /* Now ensure we don't override any other allocated region */
                foreach (tysos.PhysicalMemoryResource64 pmem in pmem_allocated)
                {
                    if (pmem.Intersects(base_addr, length))
                    {
                        System.Diagnostics.Debugger.Log(0, "acpipc", "AllocPmemFixed: requested range " +
                            base_addr.ToString("X16") + " - " + (base_addr + length).ToString("X16") +
                            " overrides allocated region: " + pmem.Addr64.ToString("X16") +
                            " - " + (pmem.Addr64 + pmem.Length64).ToString("X16"));
                        return null;
                    }
                }
            }

            /* Create the range to return */
            tysos.PhysicalMemoryResource64 ret = src.Split(base_addr, length) as
                tysos.PhysicalMemoryResource64;
            pmem_allocated.Add(ret);

            return ret;
        }

        ulong next_vaddr = 0;
        int next_vaddr_region = 0;
        tysos.VirtualMemoryResource64 cur_heap_region = null;
        tysos.VirtualMemoryResource64 AllocVmem(ulong length)
        {
            /* provide a simple expand-only heap */

            /* if either the current heap isn't allocated, or is not large enough
            to handle the request, move on to the next heap */
            if (cur_heap_region == null || ((next_vaddr + length) > (cur_heap_region.Addr64 + cur_heap_region.Length64)))
            {
                if (next_vaddr_region >= vmems.Count)
                    return null;
                cur_heap_region = vmems[next_vaddr_region++];
                next_vaddr = cur_heap_region.Addr64;
            }
            tysos.VirtualMemoryResource64 ret = cur_heap_region.Split(next_vaddr, length)
                as tysos.VirtualMemoryResource64;
            if (ret == null)
                return null;

            next_vaddr += length;
            if((next_vaddr & 0xfff) != 0)
            {
                next_vaddr &= ~0xfffUL;
                next_vaddr += 0x1000;
            }

            return ret;
        }

        tysos.VirtualMemoryResource64 AllocVmemFixed(ulong base_addr, ulong length, bool can_override)
        {
            /* First, determine if the requested range is available */
            tysos.VirtualMemoryResource64 src = null;

            foreach (tysos.VirtualMemoryResource64 vmem in vmems)
            {
                if (base_addr >= vmem.Addr64 && ((base_addr + length) <= (vmem.Addr64 + vmem.Length64)))
                {
                    src = vmem;
                    break;
                }
            }

            if (src == null)
            {
                /*System.Diagnostics.Debugger.Log(0, "acpipc", "AllocVmemFixed: source range not found for " +
                    base_addr.ToString("X16") + " - " + (base_addr + length).ToString("X16"));*/
                return null;
            }

            if (can_override == false)
            {
                /* Now ensure we don't override any other allocated region */
                foreach (tysos.VirtualMemoryResource64 vmem in vmem_allocated)
                {
                    if (vmem.Intersects(base_addr, length))
                    {
                        /*System.Diagnostics.Debugger.Log(0, "acpipc", "AllocVmemFixed: requested range " +
                            base_addr.ToString("X16") + " - " + (base_addr + length).ToString("X16") +
                            " overrides allocated region: " + vmem.Addr64.ToString("X16") +
                            " - " + (vmem.Addr64 + vmem.Length64).ToString("X16"));*/
                        return null;
                    }
                }
            }

            /* Create the range to return */
            tysos.VirtualMemoryResource64 ret = src.Split(base_addr, length) as
                tysos.VirtualMemoryResource64;
            vmem_allocated.Add(ret);

            return ret;
        }

    }
}
