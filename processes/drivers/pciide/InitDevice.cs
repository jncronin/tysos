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
using tysos;
using tysos.lib;
using tysos.Resources;

namespace pciide
{
    partial class pciide : tysos.lib.VirtualDirectoryServer, tysos.Interfaces.IFileSystem
    {
        pci.PCIConfiguration pciconf;

        internal enum DriverType { PCINative, Legacy, Unknown };
        DriverType dt;

        internal pciide(tysos.lib.File.Property[] Properties, DriverType type)
        {
            root = new List<tysos.lib.File.Property>(Properties);
            dt = type;
        }

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "pciide", "PCI IDE driver started");

            pciconf = pci.PCIConfiguration.GetPCIConf(root);
            if(pciconf == null)
            {
                System.Diagnostics.Debugger.Log(0, "pciide", "no PCI configuration provided");
                return false;
            }

            if(dt == DriverType.Unknown)
            {
                /* Get the type from PCI config space */
                uint conf_8 = pciconf.ReadConfig(0x8);
                uint progIF = (conf_8 >> 8) & 0xffU;
                if ((progIF & 0x5) == 0x5)
                    dt = DriverType.PCINative;
                else
                    dt = DriverType.Legacy;
            }

            /* Get IO port ranges */
            var pri_cmd = pciconf.GetBAR(0);
            var pri_ctrl = pciconf.GetBAR(1);
            var sec_cmd = pciconf.GetBAR(2);
            var sec_ctrl = pciconf.GetBAR(3);

            /* Get IRQ ports */
            tysos.Resources.InterruptLine pri_int = null, sec_int = null;
            switch (dt)
            {
                case DriverType.Legacy:
                    foreach (var r in root)
                    {
                        if (r.Name == "interrupt" && (r.Value is tysos.Resources.InterruptLine))
                        {
                            var r_int = r.Value as tysos.Resources.InterruptLine;
                            if (r_int.ShortName == "IRQ14")
                                pri_int = r_int;
                            else if (r_int.ShortName == "IRQ15")
                                sec_int = r_int;
                        }
                    }
                    break;

                case DriverType.PCINative:
                    foreach (var r in root)
                    {
                        if (r.Name == "interrupt" && (r.Value is tysos.Resources.InterruptLine))
                        {
                            var r_int = r.Value as tysos.Resources.InterruptLine;
                            if (r_int.ShortName.StartsWith("PCI"))
                            {
                                pri_int = r_int;
                                sec_int = r_int;
                                break;
                            }
                        }
                    }
                    break;
            }

            /* Create child devices */
            if (pri_cmd.Length64 != 0 && pri_ctrl.Length64 != 0 && pri_int != null)
                CreateChannel(0, pri_cmd, pri_ctrl, pri_int);
            if (sec_cmd.Length64 != 0 && sec_ctrl.Length64 != 0 && sec_int != null)
                CreateChannel(1, sec_cmd, sec_ctrl, sec_int);

            root.Add(new File.Property { Name = "class", Value = "bus" });
            Tags.Add("class");

            return true;
        }

        private void CreateChannel(int idx, RangeResource cmd, RangeResource ctrl, InterruptLine interrupt)
        {
            List<tysos.lib.File.Property> props = new List<tysos.lib.File.Property>();
            string channel_name = "";
            switch(idx)
            {
                case 0:
                    channel_name = "Primary ";
                    break;
                case 1:
                    channel_name = "Secondary ";
                    break;
                case 2:
                    channel_name = "Tertiary ";
                    break;
                case 3:
                    channel_name = "Quaternary ";
                    break;
            }

            props.Add(new tysos.lib.File.Property { Name = "driver", Value = "ata" });
            props.Add(new tysos.lib.File.Property { Name = "name", Value = channel_name + "ATA Channel" });
            props.Add(new tysos.lib.File.Property { Name = "manufacturer", Value = "Generic" });
            props.Add(new tysos.lib.File.Property { Name = "io", Value = cmd });
            props.Add(new tysos.lib.File.Property { Name = "io", Value = ctrl });
            props.Add(new tysos.lib.File.Property { Name = "interrupt", Value = interrupt });
            props.Add(new tysos.lib.File.Property { Name = "channel", Value = idx });

            children["ata_" + idx.ToString()] = props;

            System.Diagnostics.Debugger.Log(0, "pciide", "Created ATA channel:");
            foreach (var prop in props)
            {
                System.Diagnostics.Debugger.Log(0, "pciide", "  " + prop.Name + ": " + prop.Value.ToString());
            }

        }
    }
}
