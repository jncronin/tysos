/* Copyright (C) 2015 by John Croninq
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
using tysos.Messages;

namespace acpipc
{
    partial class Program
    {
        static List<tysos.VirtualMemoryResource64> vmems = new List<tysos.VirtualMemoryResource64>();
        static List<tysos.PhysicalMemoryResource64> pmems = new List<tysos.PhysicalMemoryResource64>();
        static List<tysos.x86_64.IOResource> ios = new List<tysos.x86_64.IOResource>();
        static List<Table> tables = new List<Table>();

        private static void _InitDevice(ICollection<tysos.StructuredStartupParameters.Param> resources,
            tysos.IFile file, ref object device_node)
        {
            tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: received InitDevice message\n");

            /* Interpret the resources we have */
            foreach(tysos.StructuredStartupParameters.Param p in resources)
            {
                if (p.Name == "vmem")
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: adding vmem area\n");
                    vmems.Add(p.Value as tysos.VirtualMemoryResource64);
                } else if(p.Name == "pmem")
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: adding pmem area\n");
                    pmems.Add(p.Value as tysos.PhysicalMemoryResource64);
                }
                else if(p.Name == "io")
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: adding io area\n");
                    ios.Add(p.Value as tysos.x86_64.IOResource);
                }
                else if(p.Name.StartsWith("table_"))
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: adding table\n");
                    tables.Add(Table.InterpretTable(p.Value as tysos.VirtualMemoryResource64));                    
                }
            }

            tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: finished parsing resources\n");
            throw new NotImplementedException();
        }
    }
}
