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
using tysos.lib;

namespace acpipc
{
    partial class acpipc : tysos.lib.VirtualDirectoryServer
    {
        internal List<tysos.VirtualMemoryResource64> vmems = new List<tysos.VirtualMemoryResource64>();
        internal List<tysos.PhysicalMemoryResource64> pmems = new List<tysos.PhysicalMemoryResource64>();
        internal List<tysos.x86_64.IOResource> ios = new List<tysos.x86_64.IOResource>();
        List<Table> tables = new List<Table>();
        tysos.lib.File.Property[] props;
        internal ulong p_dsdt_addr, dsdt_len;
        internal List<tysos.VirtualMemoryResource64> ssdts = new List<tysos.VirtualMemoryResource64>();

        Dictionary<string, int> next_device_id = new Dictionary<string, int>(new tysos.Program.MyGenericEqualityComparer<string>());

        public acpipc(tysos.lib.File.Property[] Properties)
        {
            props = Properties;
            root = new List<File.Property>(Properties);
        }

        public override bool InitServer()
        {
            /* Interpret the resources we have */
            foreach(tysos.lib.File.Property p in props)
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
                    tables.Add(Table.InterpretTable(p.Value as tysos.VirtualMemoryResource64, this));                    
                }
            }

            tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: finished parsing resources\n");

            /* Now allocate space for the DSDT */
            if(p_dsdt_addr == 0)
            {
                throw new Exception("DSDT not found");
            }
            tysos.PhysicalMemoryResource64 p_dsdt = AllocPmemFixed(p_dsdt_addr, dsdt_len);
            tysos.VirtualMemoryResource64 v_dsdt = AllocVmem(dsdt_len);
            p_dsdt.Map(v_dsdt);

            dsdt_len = v_dsdt.Read(v_dsdt.Addr64 + 4, 4);
            System.Diagnostics.Debugger.Log(0, "acpipc", "DSDT table length " + dsdt_len.ToString("X16"));

            p_dsdt = AllocPmemFixed(p_dsdt_addr, dsdt_len);
            v_dsdt = AllocVmem(dsdt_len);
            p_dsdt.Map(v_dsdt);

            System.Diagnostics.Debugger.Log(0, "acpipc", "DSDT region: " + v_dsdt.Addr64.ToString("X16") +
                " - " + (v_dsdt.Addr64 + v_dsdt.Length64).ToString("X16"));

            /* Execute the DSDT followed by SSDTs */
            MachineInterface mi = new MachineInterface(this);
            Aml.Namespace n = new Aml.Namespace(mi);

            System.Diagnostics.Debugger.Log(0, "acpipc", "Executing DSDT");
            Aml.DefBlockHeader h = new Aml.DefBlockHeader();
            int idx = 0;
            byte[] aml = v_dsdt.ToArray();
            n.ParseDefBlockHeader(aml, ref idx, h);
            System.Diagnostics.Debugger.Log(0, "acpipc", "DefBlockHeader parsed");

            Aml.Namespace.State s = new Aml.Namespace.State
            {
                Args = new Dictionary<int, Aml.ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                Locals = new Dictionary<int, Aml.ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                Scope = Aml.ACPIName.RootName
            };
            n.ParseTermList(aml, ref idx, -1, s);
            System.Diagnostics.Debugger.Log(0, "acpipc", "DSDT parsed");

            foreach(tysos.VirtualMemoryResource64 v_ssdt in ssdts)
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "Executing SSDT");
                idx = 0;
                byte[] ssdt_aml = v_ssdt.ToArray();
                Aml.DefBlockHeader h_ssdt = new Aml.DefBlockHeader();
                n.ParseDefBlockHeader(ssdt_aml, ref idx, h_ssdt);
                System.Diagnostics.Debugger.Log(0, "acpipc", "DefBlockHeader parsed");

                s = new Aml.Namespace.State
                {
                    Args = new Dictionary<int, Aml.ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                    Locals = new Dictionary<int, Aml.ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                    Scope = Aml.ACPIName.RootName
                };
                n.ParseTermList(ssdt_aml, ref idx, -1, s);
                System.Diagnostics.Debugger.Log(0, "acpipc", "SSDT parsed");
            }

            /* Now extract a list of devices that have a _HID object.
            These are the only ones ACPI needs to enumerate, all others are
            enumerated by the respective bus enumerator */
            foreach(KeyValuePair<string, Aml.ACPIObject> kvp in n.Devices)
            {
                Aml.ACPIObject hid = n.FindObject(kvp.Key + "._HID", false);
                if (hid == null)
                    continue;
                s = new Aml.Namespace.State
                {
                    Args = new Dictionary<int, Aml.ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                    Locals = new Dictionary<int, Aml.ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                    Scope = hid.Name
                };

                Aml.ACPIObject hid_ret = hid.Evaluate(mi, s, n);
                string hid_str = "";
                switch(hid_ret.Type)
                {
                    case Aml.ACPIObject.DataType.Integer:
                        hid_str = hid_ret.IntegerData.ToString("X8");
                        break;
                    case Aml.ACPIObject.DataType.String:
                        hid_str = (string)hid_ret.Data;
                        break;
                    default:
                        hid_str = hid_ret.Type.ToString() + ": " + hid_ret.Data.ToString();
                        break;
                }

                AddDevice(hid_str, kvp.Key, n, mi);
            }
            foreach(KeyValuePair<string, Aml.ACPIObject> kvp in n.Processors)
            {
                AddDevice("cpu", kvp.Key, n, mi);
            }

            return true;
        }
    }
}
