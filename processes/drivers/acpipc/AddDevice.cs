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
using tysos.lib;
using acpipc.Aml;

namespace acpipc
{
    partial class acpipc
    {
        private void AddDevice(string hid_str, string name, Aml.Namespace n,
            Aml.IMachineInterface mi)
        {
            System.Diagnostics.Debugger.Log(0, "acpipc", name + ": " + hid_str);

            /* This is a mapping of PNP IDs/ACPI IDs to tysos device driver names */
            string tysos_driver = "unknown";
            string tysos_subdriver = null;

            if (hid_str == "0A0CD041")
            {
                // PNP0C0A  ACPI control method battery
                tysos_driver = "acpipc";
                tysos_subdriver = "bat";
            }
            else if (hid_str == "030AD041")
            {
                // PNP0A03 PCI bus
                tysos_driver = "pci";
                tysos_subdriver = "hostbridge";
            }
            else if (hid_str == "0001D041")
            {
                // PNP0100  AT system timer
                tysos_driver = "isa";
                tysos_subdriver = "pit";
            }
            else if (hid_str == "0F0CD041")
            {
                // PNP0C0F  PCI interrupt link device
                tysos_driver = null;
            }
            else if (hid_str == "ACPI0003")
            {
                // ACPI0003 ACPI Power source device
                tysos_driver = "acpipc";
                tysos_subdriver = "power";
            }
            else if (hid_str == "0301D041")
            {
                // PNP0103  HPET
                tysos_driver = "hpet";
            }
            else if (hid_str == "0303D041")
            {
                // PNP0303  IBM enhanced keyboard (101/102-key, PS/2 mouse support)
                tysos_driver = "ps2k";
            }
            else if (hid_str == "030FD041")
            {
                // PNP0F03  Microsoft PS/2-style Mouse
                tysos_driver = "ps2m";
            }
            else if (hid_str == "0000D041")
            {
                // PNP0000  AT programmable interrupt controller
                tysos_driver = null;
            }
            else if (hid_str == "000BD041")
            {
                // PNP0B00  AT real-time clock
                tysos_driver = "isa";
                tysos_subdriver = "rtc";
            }
            else if (hid_str == "0105D041")
            {
                // PNP0501  16550A-compatible COM port
                tysos_driver = "isa";
                tysos_subdriver = "serial";
            }
            else if (hid_str == "0007D041")
            {
                // PNP0700  PC standard floppy disk controller
                tysos_driver = "fdc";
            }
            else if (name == "0004D041")
            {
                // PNP0400  Standard LPT printer port
                tysos_driver = "isa";
                tysos_subdriver = "lpt";
            }
            else if (hid_str == "0002D041")
            {
                // PNP0200  AT DMA controller
                tysos_driver = "isa";
                tysos_subdriver = "dma";
            }
            else if (hid_str == "cpu")
            {
                // ACPI cpu object
                tysos_driver = "cpu";
            }

            if (tysos_driver == null)
                return;

            /* Populate the device nodes properties */
            List<tysos.lib.File.Property> props = new List<tysos.lib.File.Property>();
            props.Add(new tysos.lib.File.Property { Name = "driver", Value = tysos_driver });
            if (tysos_subdriver != null)
                props.Add(new tysos.lib.File.Property { Name = "subdriver", Value = tysos_subdriver });
            props.Add(new tysos.lib.File.Property { Name = "acpiid", Value = hid_str });
            props.Add(new tysos.lib.File.Property { Name = "acpiname", Value = name });

            /* Get resources owned by the device */
            Aml.ACPIObject resources = n.Evaluate(name + "._CRS", mi);
            if (resources != null)
                InterpretResources(resources, props);

            /* Generate a unique name for the device */
            int dev_no = 0;
            StringBuilder sb = new StringBuilder(tysos_driver);
            if (tysos_subdriver != null)
            {
                sb.Append("_");
                sb.Append(tysos_subdriver);
            }
            string base_dev = sb.ToString();
            if (next_device_id.ContainsKey(base_dev))
                dev_no = next_device_id[base_dev];
            sb.Append("_");
            sb.Append(dev_no.ToString());
            next_device_id[base_dev] = dev_no + 1;
            string dev_name = sb.ToString();

            /* Create a file system node for the device */
            children[dev_name] = props;

            System.Diagnostics.Debugger.Log(0, "acpipc", "AddDevice: created device: " + dev_name);
            foreach (tysos.lib.File.Property prop in props)
                System.Diagnostics.Debugger.Log(0, "acpipc", "  " + prop.Name + ": " + prop.Value.ToString());
        }

        private void InterpretResources(ACPIObject resources, List<File.Property> props)
        {
            /* The value of _CRS should be a Buffer, i.e. an array of type byte[] */
            if(resources.Type != ACPIObject.DataType.Buffer)
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: WARNING: " +
                    "_CRS output is not of type Package, but rather of type " +
                    resources.Type.ToString());
                return;
            }

            byte[] rs = resources.Data as byte[];
            if(rs == null)
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: WARNING: " +
                    "_CRS output data is not of type byte[]");
                return;
            }

            //System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: valid _CRS buffer found, length " + rs.Length.ToString());

            int idx = 0;
            while (idx < rs.Length)
            {
                byte tag = rs[idx];
                if ((tag & 0x80) == 0)
                {
                    /* Small resource */
                    int len = tag & 0x7;
                    int type = (tag >> 3) & 0xf;

                    //System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small resource: type: " + type.ToString() + ", len: " + len.ToString());

                    switch(type)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small reserved item");
                            throw new NotSupportedException();
                        case 4:
                            //System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small irq format item");
                            {
                                uint irq_mask_1 = rs[idx + 1];
                                uint irq_mask_2 = rs[idx + 2];

                                for(int i = 0; i < 8; i++)
                                {
                                    if ((irq_mask_1 & 0x1) != 0)
                                        props.Add(new File.Property { Name = "irq", Value = i });
                                    irq_mask_1 >>= 1;
                                }
                                for(int i = 0; i < 8; i++)
                                {
                                    if((irq_mask_2 & 0x1) != 0)
                                        props.Add(new File.Property { Name = "irq", Value = i + 8 });
                                    irq_mask_2 >>= 1;
                                }
                            }
                            break;
                        case 5:
                            //System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small dma format item");
                            {
                                uint dma_mask = rs[idx + 1];

                                for (int i = 0; i < 8; i++)
                                {
                                    if ((dma_mask & 0x1) != 0)
                                        props.Add(new File.Property { Name = "dma", Value = i });
                                    dma_mask >>= 1;
                                }
                            }
                            break;
                        case 6:
                            System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small start dependent functions item");
                            throw new NotImplementedException();
                        case 7:
                            System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small end dependent functions item");
                            throw new NotImplementedException();
                        case 8:
                            //System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small io port descriptor item");
                            {
                                uint io_base = rs[idx + 2];
                                io_base |= (uint)rs[idx + 3] << 8;

                                uint io_len = rs[idx + 7];

                                props.Add(new File.Property { Name = "io", Value = ios.AllocFixed(io_base, io_len, true) });
                            }

                            break;
                        case 9:
                            System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small fixed location io port descriptor item");
                            throw new NotImplementedException();
                        case 0xa:
                        case 0xb:
                        case 0xc:
                        case 0xd:
                            System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small reserved item");
                            throw new NotSupportedException();
                        case 0xe:
                            System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: small vendor defined descriptor item");
                            throw new NotImplementedException();
                    }

                    if (type == 0xf)
                        break;

                    idx++;
                    idx += len;
                }
                else
                {
                    uint type = tag & 0x7fU;
                    uint len = rs[idx + 1];
                    len |= (uint)rs[idx + 2] << 8;

                    //System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: large resource: type: " + type.ToString() + ", len: " + len.ToString());

                    switch(type)
                    {
                        case 0x6:
                            {
                                ulong min = BitConverter.ToUInt32(rs, idx + 4);
                                ulong length = BitConverter.ToUInt32(rs, idx + 8);
                                props.Add(new File.Property { Name = "pmem", Value = pmems.AllocFixed(min, length, true) });
                            }
                            break;
                        case 0x7:
                        case 0x8:
                        case 0xa:
                            {
                                int res_type = rs[idx + 3];
                                int gen_flags = rs[idx + 4];
                                int spec_flags = rs[idx + 5];
                                ulong gran = 0;
                                ulong min = 0;
                                ulong max = 0;
                                ulong tran = 0;
                                ulong length = 0;

                                switch(type)
                                {
                                    case 0x7:
                                        gran = BitConverter.ToUInt32(rs, idx + 6);
                                        min = BitConverter.ToUInt32(rs, idx + 10);
                                        max = BitConverter.ToUInt32(rs, idx + 14);
                                        tran = BitConverter.ToUInt32(rs, idx + 18);
                                        length = BitConverter.ToUInt32(rs, idx + 22);
                                        break;
                                    case 0x8:
                                        gran = BitConverter.ToUInt16(rs, idx + 6);
                                        min = BitConverter.ToUInt16(rs, idx + 8);
                                        max = BitConverter.ToUInt16(rs, idx + 10);
                                        tran = BitConverter.ToUInt16(rs, idx + 12);
                                        length = BitConverter.ToUInt16(rs, idx + 14);
                                        break;
                                    case 0xa:
                                        gran = BitConverter.ToUInt64(rs, idx + 6);
                                        min = BitConverter.ToUInt64(rs, idx + 14);
                                        max = BitConverter.ToUInt64(rs, idx + 22);
                                        tran = BitConverter.ToUInt64(rs, idx + 30);
                                        length = BitConverter.ToUInt64(rs, idx + 38);
                                        break;
                                }

                                /*System.Diagnostics.Debugger.Log(0, "acpipc", "InterpretResources: large resource: res_type: " +
                                    res_type.ToString("X8") + ", gen_flags: " + gen_flags.ToString("X8") +
                                    ", spec_flags: " + spec_flags.ToString("X8") +
                                    ", gran: " + gran.ToString("X16") +
                                    ", min: " + min.ToString("X16") +
                                    ", max: " + max.ToString("X16") +
                                    ", tran: " + tran.ToString("X16") +
                                    ", length: " + length.ToString("X16"));*/

                                switch (res_type)
                                {
                                    case 0:
                                        props.Add(new File.Property { Name = "pmem", Value = pmems.AllocFixed(min, length, true) });
                                        break;
                                    case 1:
                                        props.Add(new File.Property { Name = "io", Value = ios.AllocFixed((uint)min, (uint)length, true) });
                                        break;
                                }
                            }
                            break;

                        default:
                            throw new NotImplementedException("large descriptor type " + type.ToString());
                    }

                    idx += 3;
                    idx += (int)len;
                }
            }
        }

    }
}
