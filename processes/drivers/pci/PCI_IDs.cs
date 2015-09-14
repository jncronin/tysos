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

namespace pci
{
    partial class DeviceDB
    {
        static internal void InitDeviceDB(hostbridge hb)
        {
            db_vdr = new Dictionary<DeviceDBKey, DeviceDBEntry>(new VendorDeviceRevisionComparer());
            db_vd = new Dictionary<DeviceDBKey, DeviceDBEntry>(new VendorDeviceComparer());
            db_csp = new Dictionary<DeviceDBKey, DeviceDBEntry>(new ClassSubclassProgIFComparer());
            db_cs = new Dictionary<DeviceDBKey, DeviceDBEntry>(new ClassSubclassComparer());

            // Some defines that are used in more than one entry
            DeviceDBEntry pciide_legacy = new DeviceDBEntry
            {
                DriverName = "pciide",
                HumanManufacturerName = "Generic",
                HumanDeviceName = "PCI IDE Controller (legacy mode)",

                BAROverrides = new BAROverride[] {
                    new BAROverride { Value = 0x1f0, Length = 8, Type = 1 },
                    new BAROverride { Value = 0x3f6, Length = 1, Type = 1 },
                    new BAROverride { Value = 0x170, Length = 8, Type = 1 },
                    new BAROverride { Value = 0x376, Length = 1, Type = 1 }
                },

                ExtraResources = new List<tysos.Resource>()               
            };
            if (hb.isa_irqs.ContainsKey(14)) pciide_legacy.ExtraResources.Add(hb.isa_irqs[14]);
            if (hb.isa_irqs.ContainsKey(15)) pciide_legacy.ExtraResources.Add(hb.isa_irqs[15]);
            
            // Device database

            // The following match on vendor ID and device ID
            db_vd.Add(new DeviceDBKey { VendorID = 0x8086, DeviceID = 0x1237 }, new DeviceDBEntry { DriverName = "pci", HumanManufacturerName = "Intel", HumanDeviceName = "440FX - 82441FX PCI and Memory Controller" });
            db_vd.Add(new DeviceDBKey { VendorID = 0x8086, DeviceID = 0x2415 }, new DeviceDBEntry { DriverName = "ac97", HumanManufacturerName = "Intel", HumanDeviceName = "82801AA AC'97 Audio Controller" });
            db_vd.Add(new DeviceDBKey { VendorID = 0x8086, DeviceID = 0x7000 }, new DeviceDBEntry { DriverName = null, HumanManufacturerName = "Intel", HumanDeviceName = "82371SB PIIX3 ISA" });

            db_vd.Add(new DeviceDBKey { VendorID = 0x8086, DeviceID = 0x7113 }, new DeviceDBEntry { DriverName = null, HumanManufacturerName = "Intel", HumanDeviceName = "82371AB/EB/MB PIIX4 ACPI" });

            db_vd.Add(new DeviceDBKey { VendorID = 0x80ee, DeviceID = 0xbeef }, new DeviceDBEntry { DriverName = "bga", HumanManufacturerName = "Oracle", HumanDeviceName = "VirtualBox Graphics Adapter",
                ExtraResources = new tysos.Resource[] { hb.ios.AllocFixed(0x1ce, 2, true), hb.ios.AllocFixed(0x1cf, 2, true), hb.vmems.Alloc(0x2000000) } });

            db_vd.Add(new DeviceDBKey { VendorID = 0x1022, DeviceID = 0x2000 }, new DeviceDBEntry { DriverName = "pcnet32", HumanManufacturerName = "AMD", HumanDeviceName = "79c970 [PCnet32 LANCE]" });


            // The following match on vendor ID, device ID and revision ID


            // The following match on class and subclass
            db_cs.Add(new DeviceDBKey { ClassCode = 0x01, SubclassCode = 0x01 }, new DeviceDBEntry { DriverName = "pciide", HumanManufacturerName = "Generic", HumanDeviceName = "PCI IDE Controller" });


            // The following match on class, subclass and progIF
            db_csp.Add(new DeviceDBKey { ClassCode = 0x01, SubclassCode = 0x01, ProgIF = 0x00 }, pciide_legacy);
            db_csp.Add(new DeviceDBKey { ClassCode = 0x01, SubclassCode = 0x01, ProgIF = 0x0a }, pciide_legacy);
            db_csp.Add(new DeviceDBKey { ClassCode = 0x01, SubclassCode = 0x01, ProgIF = 0x80 }, pciide_legacy);
            db_csp.Add(new DeviceDBKey { ClassCode = 0x01, SubclassCode = 0x01, ProgIF = 0x8a }, pciide_legacy);

            db_csp.Add(new DeviceDBKey { ClassCode = 0x03, SubclassCode = 0x00, ProgIF = 0x00 }, new DeviceDBEntry { DriverName = "vga", HumanManufacturerName = "Generic", HumanDeviceName = "Standard VGA Controller" });
            db_csp.Add(new DeviceDBKey { ClassCode = 0x0c, SubclassCode = 0x03, ProgIF = 0x00 }, new DeviceDBEntry { DriverName = "uhci", HumanManufacturerName = "Generic", HumanDeviceName = "UHCI Controller" });
            db_csp.Add(new DeviceDBKey { ClassCode = 0x0c, SubclassCode = 0x03, ProgIF = 0x10 }, new DeviceDBEntry { DriverName = "ohci", HumanManufacturerName = "Generic", HumanDeviceName = "OHCI Controller" });
            db_csp.Add(new DeviceDBKey { ClassCode = 0x0c, SubclassCode = 0x03, ProgIF = 0x20 }, new DeviceDBEntry { DriverName = "ehci", HumanManufacturerName = "Generic", HumanDeviceName = "EHCI Controller" });
            db_csp.Add(new DeviceDBKey { ClassCode = 0x0c, SubclassCode = 0x03, ProgIF = 0x30 }, new DeviceDBEntry { DriverName = "xhci", HumanManufacturerName = "Generic", HumanDeviceName = "xHCI Controller" });


        }
    }
}
