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

namespace pciide
{
    partial class pciide : tysos.lib.VirtualDirectoryServer
    {
        pci.PCIConfiguration pciconf;

        tysos.RangeResource[] command;
        tysos.RangeResource[] control;

        public pciide(tysos.lib.File.Property[] Properties)
        {
            root = new List<tysos.lib.File.Property>(Properties);
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

            /* Set the controller to native PCI mode if possible */
            uint conf_8 = pciconf.ReadConfig(0x8);
            uint progIF = (conf_8 >> 8) & 0xffU;
            bool is_native = false;
            System.Diagnostics.Debugger.Log(0, "pciide", "controller reports progIF as " + progIF.ToString("X8"));
            if((progIF & 0x5) == 0x5)
            {
                // already in native mode
                is_native = true;
            }
            else
            {
                // not already in native PCI mode on both channels

                if ((progIF & 0xa) != 0xa)
                {
                    // one or both channels is in a fixed mode and so can't be
                    // programmed to native PCI mode
                    is_native = false;
                }
                else
                {
                    // Attempt to program native mode

                    conf_8 |= (0x5 << 8);

                    System.Diagnostics.Debugger.Log(0, "pciide", "writing " + conf_8.ToString("X8") + " to control register");

                    pciconf.WriteConfig(0x8, conf_8);

                    // See if we succeeded
                    conf_8 = pciconf.ReadConfig(0x8);
                    progIF = (conf_8 >> 8) & 0xffU;

                    is_native = (progIF & 0x5) == 0x5;
                }
            }

            /* Enable the controller */
            uint conf_4 = pciconf.ReadConfig(0x4);
            conf_4 |= 0x3U;
            pciconf.WriteConfig(0x4, conf_4);

            /* Get the value of BARs */
            for (int i = 0; i < 6; i++)
            {
                tysos.RangeResource rr = pciconf.GetBAR(i);
                if (rr == null)
                    System.Diagnostics.Debugger.Log(0, "pciide", "BAR" + i.ToString() + ": null");
                else
                    System.Diagnostics.Debugger.Log(0, "pciide", "BAR" + i.ToString() + ": " + rr.ToString());
            }

            command = new tysos.RangeResource[2];
            control = new tysos.RangeResource[2];

            command[0] = pciconf.GetBAR(0);
            control[0] = pciconf.GetBAR(1);
            command[1] = pciconf.GetBAR(2);
            control[1] = pciconf.GetBAR(3);

            /* Get the value of IRQ line/pin */
            uint conf_3c = pciconf.ReadConfig(0x3c);
            System.Diagnostics.Debugger.Log(0, "pciide", "Conf 3c: " + conf_3c.ToString("X8"));

            return true;
        }
    }
}
