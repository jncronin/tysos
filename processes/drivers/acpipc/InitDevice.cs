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
        internal FADT fadt;
        tysos.lib.File.Property[] props;
        internal ulong p_dsdt_addr, dsdt_len;
        internal List<tysos.VirtualMemoryResource64> ssdts = new List<tysos.VirtualMemoryResource64>();

        Dictionary<string, int> next_device_id = new Dictionary<string, int>(new tysos.Program.MyGenericEqualityComparer<string>());

        List<tysos.ServerObject> gsi_providers = new List<tysos.ServerObject>();
        List<InterruptSourceOverrideStructure> isos = new List<InterruptSourceOverrideStructure>();
        List<LocalAPICStructure> lapics = new List<LocalAPICStructure>();
        
        ACPIInterrupt[] isa_irqs = new ACPIInterrupt[16];
        Dictionary<int, PCIInterrupt> pci_ints = new Dictionary<int, PCIInterrupt>(
            new tysos.Program.MyGenericEqualityComparer<int>());
        List<string> lnks = new List<string>(); // PCI Interrupt Link objects

        internal Aml.Namespace n;
        internal MachineInterface mi;

        //internal tysos.x86_64.IOResource 

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
                    System.Diagnostics.Debugger.Log(0, null, "adding table\n");
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

            System.Diagnostics.Debugger.Log(0, null, "finished parsing resources\n");

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
                        else if (apicstruct is LocalAPICStructure)
                            lapics.Add(apicstruct as LocalAPICStructure);
                    }
                }
            }

            /* Generate interrupt resources for the standard ISA IRQs */
            for(int i = 0; i < 16; i++)
                isa_irqs[i] = GenerateISAIRQ(i);

            /* Dump VBox ACPI interface */
            /*var vbox_idx = ios.AllocFixed(0x4048, 4);
            var vbox_dat = ios.AllocFixed(0x404c, 4);
            for(uint i = 0; i < 26; i++)
            {
                vbox_idx.Write(vbox_idx.Addr64, 4, i * 4);
                var val = vbox_dat.Read(vbox_dat.Addr64, 4);
                System.Diagnostics.Debugger.Log(0, "acpipc", "VBoxACPI: " + i.ToString() + ": " + val.ToString("X8"));
            }*/

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

            /* Initialize the namespace

            To do this we:
            1) initialize the main namespace (\_SB_._INI)
            2) run each device's _STA method.
                - if _STA & 0x1 = 0,
                    - if _STA & 0x8 = 0 remove device and children from namespace
                    - else remove device (but still enumerate children)
                - else, run _INI on device
            3) tell ACPI we are using IOAPIC (\_PIC(1))
            */

            EvaluateObject("\\_SB_._INI");

            /* We do the initialization this way to ensure we always initialize
            root objects before children.  By definition parent objects have
            shorter names than children, therefore we do all devices of length 1,
            then 2 etc until all have been covered.

            If we find a non-functional device (bit 3 not set), we at that stage
            find all children of it and mark them as already initialized (so that
            they are not parsed in the loop)
            */

            int num_to_parse = n.Devices.Count;
            List<string> dev_names = new List<string>(n.Devices.Keys);
            int depth = 1;

            while(num_to_parse > 0)
            {
                for(int i = 0; i < dev_names.Count; i++)
                {
                    ACPIName dev_name = dev_names[i];
                    if (dev_name == null)
                        continue;
                    if (dev_name.ElementCount != depth)
                        continue;

                    ACPIObject dev_obj = n.FindObject(dev_name);
                    if (dev_obj.Initialized)
                        continue;

                    System.Diagnostics.Debugger.Log(0, "acpipc", "Executing " + dev_name + "._STA");

                    // Run _STA on the device
                    int sta_val = 0;
                    var sta = n.EvaluateTo(dev_name + "._STA", mi, ACPIObject.DataType.Integer);
                    if (sta == null)
                        sta_val = 0xf;
                    else
                        sta_val = (int)sta.IntegerData;

                    dev_obj.Present = ((sta_val & 0x1) != 0);
                    dev_obj.Functioning = ((sta_val & 0x8) != 0);

                    if (dev_obj.Present == false && dev_obj.Functioning == false)
                    {
                        // Do not run _INI, do not examine device children

                        System.Diagnostics.Debugger.Log(0, "acpipc", "Device is not present or functioning.  Disabling children");

                        for(int j = 0; j < dev_names.Count; j++)
                        {
                            ACPIName subdev_name = dev_names[j];
                            if (subdev_name == null)
                                continue;
                            if (subdev_name.ElementCount <= dev_name.ElementCount)
                                continue;
                            bool is_subdev = true;
                            for(int k = 0; k < dev_name.ElementCount; k++)
                            {
                                if(subdev_name.NameElement(k).Equals(dev_name.NameElement(k)) == false)
                                {
                                    is_subdev = false;
                                    break;
                                }
                            }
                            if(is_subdev)
                            {
                                System.Diagnostics.Debugger.Log(0, "acpipc", "Disabling child " + subdev_name);
                                num_to_parse--;
                                dev_names[j] = null;
                            }
                        }
                    }
                    else if(dev_obj.Present)
                    {
                        // Run _INI, examine children
                        System.Diagnostics.Debugger.Log(0, "acpipc", "Executing " + dev_name + "._INI");
                        n.Evaluate(dev_name + "._INI", mi);
                        dev_obj.Initialized = true;
                    }

                    num_to_parse--;
                }

                depth++;
            }

            System.Diagnostics.Debugger.Log(0, "acpipc", "Executing \\_PIC");
            EvaluateObject("\\_PIC", new ACPIObject[] { 1 });

            /* Generate a list of PCI Interrupt Links - we pass these as resources
            to PCI devices */
            foreach (KeyValuePair<string, Aml.ACPIObject> kvp in n.Devices)
            {
                if (kvp.Value.Initialized == false)
                    continue;

                var hid = n.EvaluateTo(kvp.Key + "._HID", mi, ACPIObject.DataType.Integer);
                if (hid == null)
                    continue;

                if (hid.IntegerData == 0x0f0cd041U)
                    lnks.Add(kvp.Key);
            }

            /* Now extract a list of devices that have a _HID object.
            These are the only ones ACPI needs to enumerate, all others are
            enumerated by the respective bus enumerator */
            foreach (KeyValuePair<string, Aml.ACPIObject> kvp in n.Devices)
            {
                List<string> hid_strs = new List<string>();
                Aml.ACPIObject hid = n.FindObject(kvp.Key + "._HID", false);
                if (hid == null)
                    continue;
                if (kvp.Value.Initialized == false)
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
                hid_strs.Add(hid_str);

                /* Also add all compatible IDs */
                Aml.ACPIObject cid = n.Evaluate(kvp.Key + "._CID", mi);
                if(cid != null)
                {
                    switch(cid.Type)
                    {
                        case ACPIObject.DataType.Integer:
                            hid_strs.Add(cid.IntegerData.ToString("X8"));
                            break;
                        case ACPIObject.DataType.String:
                            hid_strs.Add(cid.Data as string);
                            break;
                        case ACPIObject.DataType.Package:
                            var pd = cid.Data as Aml.ACPIObject[];
                            foreach(var icid in pd)
                            {
                                switch(icid.Type)
                                {
                                    case ACPIObject.DataType.Integer:
                                        hid_strs.Add(icid.IntegerData.ToString("X8"));
                                        break;
                                    case ACPIObject.DataType.String:
                                        hid_strs.Add(icid.Data as string);
                                        break;
                                    default:
                                        hid_strs.Add(icid.Type.ToString() + ": " + icid.Data.ToString());
                                        break;
                                }
                            }
                            break;
                        default:
                            hid_strs.Add(cid.Type.ToString() + ": " + cid.Data.ToString());
                            break;
                    }
                }

                AddDevice(hid_strs, kvp.Key, kvp.Value, n, mi);
            }
            foreach(KeyValuePair<string, Aml.ACPIObject> kvp in n.Processors)
            {
                AddDevice("cpu", kvp.Key, kvp.Value, n, mi);
            }

            /* Take command of hardware resources */
            if (fadt != null)
            {
                System.Diagnostics.Debugger.Log(0, null, "FADT: " +
                    "PM1a_EVT_BLK: " + fadt.PM1a_EVT_BLK.ToString() +
                    ", PM1a_CNT_BLK: " + fadt.PM1a_CNT_BLK.ToString() +
                    ", PM1b_EVT_BLK: " + fadt.PM1b_EVT_BLK.ToString() +
                    ", PM1b_CNT_BLK: " + fadt.PM1b_CNT_BLK.ToString() +
                    ", PM2_CNT_BLK: " + fadt.PM2_CNT_BLK.ToString() +
                    ", PM_TMR_BLK: " + fadt.PM_TMR_BLK.ToString() +
                    ", GPE0_BLK: " + fadt.GPE0_BLK.ToString() +
                    ", GPE1_BLK: " + fadt.GPE1_BLK.ToString());

                var sci = isa_irqs[fadt.SCI_INT];
                if (sci != null)
                {
                    System.Diagnostics.Debugger.Log(0, null, "SCI_INT mapped to " + sci.ToString());
                    sci.RegisterHandler(new tysos.Resources.InterruptLine.InterruptHandler(SCIInt));
                }

                /* Set ACPI mode */
                var smi_cmd = ios.AllocFixed(fadt.SMI_CMD, 1, true);
                if (smi_cmd != null)
                {
                    if ((fadt.PM1_CNT.Read() & 0x1) != 0)
                    {
                        System.Diagnostics.Debugger.Log(0, null, "Already in ACPI mode");
                    }
                    else
                    {
                        System.Diagnostics.Debugger.Log(0, null, "Setting ACPI mode");
                        smi_cmd.Write(smi_cmd.Addr64, 1, fadt.ACPI_ENABLE);
                        while ((fadt.PM1_CNT.Read() & 0x1) == 0) ;
                        System.Diagnostics.Debugger.Log(0, null, "Set ACPI mode");
                    }
                }

                /* Say that we handle fixed power and sleep button events */
                fadt.PM1_EN.Write((1UL << 8) | (1UL << 9));
            }

            return true;
        }

        bool SCIInt()
        {
            System.Diagnostics.Debugger.Log(0, null, "SCIINT");
            bool ret = false;

            /* Check PM1 status register */
            ulong pm1_sts = fadt.PM1_STS.Read();
            if ((pm1_sts & (1UL << 8)) != 0)
            {
                HandleFixedPowerButtonEvent();
                ret = true;
            }
            if((pm1_sts & (1UL << 9)) != 0)
            {
                HandleFixedSleepButtonEvent();
                ret = true;
            }

            /* Check GPE0 status register */
            ulong gpe0_sts = fadt.GPE0_STS.Read();
            ulong gpe1_sts = fadt.GPE1_STS.Read();

            return ret;
        }

        private void HandleFixedPowerButtonEvent()
        {
            System.Diagnostics.Debugger.Log(0, null, "POWER BUTTON");

            /* Write 1 to the register to clear it (see 4.7.3.1.1) */
            fadt.PM1_STS.Write(1UL << 8);

            /* Shutdown routine (see 15.1.7):
                \_PTS(5)
                Execute OS shutdown stuff (flush disk caches etc)
                \_GTS(5)

                Write SLP_TYPa (from \_S5) with SLP_ENa set to PM1a_CNT
                Write SLP_TYPb (from \_S5) with SLP_ENb set to PM1b_CNT
            */

            var s5 = n.Evaluate("\\_S5_", mi);
            if (s5 == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "\\_S5 returned null");
                return;
            }
            if(s5.Type != ACPIObject.DataType.Package)
            {
                System.Diagnostics.Debugger.Log(0, null, "\\_S5 returned invalid type: " + s5.Type.ToString());
                return;
            }
            var pdat = s5.Data as ACPIObject[];
            if(pdat == null)
            {
                System.Diagnostics.Debugger.Log(0, null, "\\_S5 returned invalid data type: " + s5.Data.GetType().ToString());
                return;
            }
            if(pdat.Length < 2)
            {
                System.Diagnostics.Debugger.Log(0, null, "\\_S5 returned too short a package (" + pdat.Length.ToString() + " items)");
                return;
            }
            
            // SLP_TYPx values are 3 bits long
            var slp_typa = pdat[0].IntegerData & 0x7UL;
            var slp_typb = pdat[1].IntegerData & 0x7UL;

            // build the new value for PM1x with SLP_TYP at bit 10 and SLP_EN (bit 13) set
            var pm1a_val = (slp_typa << 10) | (1UL << 13);
            var pm1b_val = (slp_typb << 10) | (1UL << 13);

            System.Diagnostics.Debugger.Log(0, null, "\\_S5 returned " + slp_typa.ToString("X") + " and " + slp_typb.ToString());

            n.Evaluate("\\_PTS", mi, new ACPIObject[] { 5 });
            n.Evaluate("\\_GTS", mi, new ACPIObject[] { 5 });

            fadt.PM1a_CNT_BLK.Write(pm1a_val);
            fadt.PM1b_CNT_BLK.Write(pm1b_val);
        }

        private void HandleFixedSleepButtonEvent()
        {
            System.Diagnostics.Debugger.Log(0, null, "SLEEP BUTTON");

            /* Write 1 to the register to clear it (see 4.7.3.1.1) */
            fadt.PM1_STS.Write(1UL << 9);
        }

        internal ACPIObject EvaluateObject(ACPIName name, IList<ACPIObject> args)
        {
            Dictionary<int, ACPIObject> d_args = new Dictionary<int, ACPIObject>(
                new tysos.Program.MyGenericEqualityComparer<int>());
            for (int i = 0; i < args.Count; i++)
                d_args[i] = args[i];

            var ret = n.Evaluate(name, mi, d_args);
            if (ret == null)
                System.Diagnostics.Debugger.Log(0, "acpipc", name + " returned null");
            else
                System.Diagnostics.Debugger.Log(0, "acpipc", name + " returned " + ret.Type.ToString());
            return ret;
        }

        internal ACPIObject EvaluateObject(ACPIName name)
        {
            return EvaluateObject(name, new ACPIObject[] { });
        }

        internal PCIInterrupt GeneratePCIIRQ(int gsi)
        {
            lock(pci_ints)
            {
                if (pci_ints.ContainsKey(gsi))
                    return pci_ints[gsi];

                GlobalSystemInterrupt gsi_obj = GetGSI(gsi);
                if (gsi_obj == null)
                    return null;
                PCIInterrupt ret = new PCIInterrupt(gsi_obj);
                pci_ints[gsi] = ret;
                return ret;
            }
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
