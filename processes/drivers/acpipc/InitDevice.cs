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
using acpipc.Aml;
using tysos.lib;

namespace acpipc
{
    partial class acpipc : tysos.lib.VirtualDirectoryServer
    {
        internal tysos.Resources.PhysicalMemoryRangeManager pmems = new tysos.Resources.PhysicalMemoryRangeManager();
        internal tysos.Resources.VirtualMemoryRangeManager vmems = new tysos.Resources.VirtualMemoryRangeManager();
        internal tysos.x86_64.IORangeManager ios = new tysos.x86_64.IORangeManager();
        internal List<tysos.Resources.InterruptLine> cpu_interrupts = new List<tysos.Resources.InterruptLine>();

        List<Table> tables = new List<Table>();
        tysos.lib.File.Property[] props;
        internal ulong p_dsdt_addr, dsdt_len;
        internal List<tysos.VirtualMemoryResource64> ssdts = new List<tysos.VirtualMemoryResource64>();

        Dictionary<string, int> next_device_id = new Dictionary<string, int>(new tysos.Program.MyGenericEqualityComparer<string>());

        List<tysos.ServerObject> gsi_providers = new List<tysos.ServerObject>();
        List<InterruptSourceOverrideStructure> isos = new List<InterruptSourceOverrideStructure>();
        ISAInterrupt[] isa_irqs = new ISAInterrupt[16];

        Aml.Namespace n;
        MachineInterface mi;

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
                if(p.Name.StartsWith("table_"))
                {
                    tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: adding table\n");
                    tables.Add(Table.InterpretTable(p.Value as tysos.VirtualMemoryResource64, this));                    
                }
                if(p.Name == "interrupts")
                {
                    cpu_interrupts.AddRange(p.Value as IEnumerable<tysos.Resources.InterruptLine>);
                }
            }
            vmems.Init(props);
            pmems.Init(props);
            ios.Init(props);

            tysos.Syscalls.DebugFunctions.DebugWrite("acpipc: finished parsing resources\n");

            /* Execute drivers for any IOAPICs we've found */
            List<tysos.ServerObject> ioapics = new List<tysos.ServerObject>();
            foreach(var table in tables)
            {
                if(table is APIC)
                {
                    APIC apic = table as APIC;
                    foreach(APICStructure apicstruct in apic.APICs)
                    {
                        if (apicstruct is IOAPICStructure)
                        {
                            IOAPICStructure ias = apicstruct as IOAPICStructure;
                            List<File.Property> ias_props = new List<File.Property>();
                            ias_props.Add(new File.Property { Name = "pmem", Value = pmems.AllocFixed(ias.IOAPICAddress, 0x1000) });
                            ias_props.Add(new File.Property { Name = "vmem", Value = vmems.Alloc(0x1000) });
                            ias_props.Add(new File.Property { Name = "gsibase", Value = ias.GSIBase });
                            ias_props.Add(new File.Property { Name = "ioapicid", Value = ias.IOAPICID });
                            ias_props.Add(new File.Property { Name = "interrupts", Value = cpu_interrupts });
                            string a_name = "ioapic_" + ias.IOAPICID.ToString();
                            children.Add(a_name, ias_props);
                            System.Diagnostics.Debugger.Log(0, "acpipc", "Starting IOAPIC driver for " + a_name);

                            ioapic.ioapic ioapic = new ioapic.ioapic(ias_props.ToArray());

                            tysos.Process p_ioapic = tysos.Process.CreateProcess(a_name,
                                new System.Threading.ThreadStart(ioapic.MessageLoop),
                                new object[] { ioapic });
                            p_ioapic.Start();

                            gsi_providers.Add(ioapic);
                        }
                        else if (apicstruct is InterruptSourceOverrideStructure)
                            isos.Add(apicstruct as InterruptSourceOverrideStructure);
                    }
                }
            }

            /* Generate interrupt resources for the standard ISA IRQs */
            for(int i = 0; i < 16; i++)
                isa_irqs[i] = GenerateISAIRQ(i);

            /* Now allocate space for the DSDT */
            if(p_dsdt_addr == 0)
            {
                throw new Exception("DSDT not found");
            }
            tysos.PhysicalMemoryResource64 p_dsdt = pmems.AllocFixed(p_dsdt_addr, dsdt_len);
            tysos.VirtualMemoryResource64 v_dsdt = vmems.Alloc(dsdt_len, 0x1000);
            p_dsdt.Map(v_dsdt);

            dsdt_len = v_dsdt.Read(v_dsdt.Addr64 + 4, 4);
            System.Diagnostics.Debugger.Log(0, "acpipc", "DSDT table length " + dsdt_len.ToString("X16"));

            p_dsdt = pmems.AllocFixed(p_dsdt_addr, dsdt_len, true);
            v_dsdt = vmems.Alloc(dsdt_len, 0x1000);
            p_dsdt.Map(v_dsdt);

            System.Diagnostics.Debugger.Log(0, "acpipc", "DSDT region: " + v_dsdt.Addr64.ToString("X16") +
                " - " + (v_dsdt.Addr64 + v_dsdt.Length64).ToString("X16"));

            /* Execute the DSDT followed by SSDTs */
            mi = new MachineInterface(this);
            n = new Aml.Namespace(mi);

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

        public ACPIObject EvaluateObject(string name)
        {
            return n.Evaluate(name, mi);
        }


        private ISAInterrupt GenerateISAIRQ(int i)
        {
            /* Parse interrupt source overrides to find out the gsi of this irq */
            int gsi_num = i;
            bool is_level_trigger = false;
            bool is_low_active = false;

            foreach(var iso in isos)
            {
                if(iso.Bus == 0 && iso.Source == i)
                {
                    gsi_num = (int)iso.GSI;
                    System.Diagnostics.Debugger.Log(0, "acpipc", "IRQ: " + i.ToString() + " remaps to GSI: " + gsi_num.ToString());
                    switch((iso.Flags >> 2) & 0x3)
                    {
                        case 0:
                            is_level_trigger = false;
                            break;
                        case 1:
                            is_level_trigger = false;
                            break;
                        case 3:
                            is_level_trigger = true;
                            break;
                    }
                    switch(iso.Flags & 0x3)
                    {
                        case 0:
                            if (is_level_trigger)
                                is_low_active = true;
                            else
                                is_low_active = false;
                            break;
                        case 1:
                            is_low_active = false;
                            break;
                        case 3:
                            is_low_active = true;
                            break;
                    }
                }
            }

            GlobalSystemInterrupt gsi = GetGSI(gsi_num);
            if(gsi == null)
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "GetGSI(" + gsi_num.ToString() + ") returned false");
                return null;
            }

            ISAInterrupt ret = new ISAInterrupt(gsi);
            ret.is_level_trigger = is_level_trigger;
            ret.is_low_active = is_low_active;
            ret.irq = i;

            System.Diagnostics.Debugger.Log(0, "acpipc", "Created ISA Interrupt handler for IRQ " + i.ToString());
            return ret;
        }

        GlobalSystemInterrupt GetGSI(int gsi_num)
        {
            foreach(var gsi_provider in gsi_providers)
            {
                GlobalSystemInterrupt ret = gsi_provider.Invoke("GetInterruptLine",
                    new object[] { gsi_num }, new Type[] { typeof(int) })
                    as GlobalSystemInterrupt;

                if (ret != null)
                    return ret;
            }

            return null;
        }
    }
}
