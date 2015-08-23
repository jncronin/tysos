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

using acpipc.Aml;
using System;
using System.Collections.Generic;
using System.Text;
using tysos.lib;

/* Encapsulate access to ACPI configuration space for a particular device
as a Resouce object.

    We allow access to all objects below the device name for the current device
    as well as providing a list of all subdevices.

    Access to certain resources (_CRS) are provided as a list of resources
*/

namespace acpipc
{
    public class ACPIConfiguration : tysos.Resource
    {
        internal acpipc acpi;
        internal Aml.ACPIName device;

        public Aml.ACPIName DeviceName { get { return device; } }

        internal ACPIConfiguration(acpipc acpiDev, Aml.ACPIName deviceName)
        {
            acpi = acpiDev;
            device = deviceName;
        }

        /** <summary>Convert a name that is either absolute or relative to the
        current device to an absolute name in the namespace, and check we have
        access to it (i.e. it is a subname of our current name)</summary> */
        Aml.ACPIName GetValidName(string name)
        {
            if (name == null)
                return null;
            if (name.Length == 0)
                name = "\\";

            string act_name = null;

            if (name[0] == '\\')
                act_name = name;
            else if (name[0] == '.')
                act_name = device.ToString() + name;
            else
                act_name = device.ToString() + "." + name;

            Aml.ACPIName ret = act_name;

            /* Now check the new name is a subobject of the current device */
            if (ret.ElementCount < device.ElementCount)
                return null;
            for(int i = 0; i < device.ElementCount; i++)
            {
                if (ret.NameElement(i).Equals(device.NameElement(i)) == false)
                    return null;
            }

            return ret;
        }

        /**<summary>Convert an IList of arguments to a Dictionary</summary> */
        Dictionary<int, Aml.ACPIObject> GetArguments(IList<Aml.ACPIObject> args)
        {
            Dictionary<int, Aml.ACPIObject> ret = new Dictionary<int, Aml.ACPIObject>(
                new tysos.Program.MyGenericEqualityComparer<int>());

            for (int i = 0; i < args.Count; i++)
                ret[i] = args[i];

            return ret;
        }

        public Aml.ACPIObject EvaluateObject(string name)
        {
            return EvaluateObject(name, new Aml.ACPIObject[] { });
        }

        public Aml.ACPIObject EvaluateObject(string name, IList<Aml.ACPIObject> args)
        {
            return acpi.n.Evaluate(GetValidName(name), acpi.mi, GetArguments(args));
        }

        public Aml.ACPIObject EvaluateObject(string name, Aml.ACPIObject.DataType to_type)
        {
            return EvaluateObject(name, new Aml.ACPIObject[] { }, to_type);
        }

        public Aml.ACPIObject EvaluateObject(string name, IList<Aml.ACPIObject> args, Aml.ACPIObject.DataType to_type)
        {
            ACPIName valid_name = GetValidName(name);
            Aml.ACPIObject ret = acpi.n.Evaluate(valid_name, acpi.mi, GetArguments(args));
            if (ret == null)
                return null;

            Namespace.State s = new Namespace.State
            {
                Args = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                Locals = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>()),
                Scope = acpi.n.FindObject(name).Name
            };

            return ret.EvaluateTo(to_type, acpi.mi, s, acpi.n);
        }

        public ICollection<File.Property> GetCurrentResources(string crs_object)
        {
            Aml.ACPIObject crs = EvaluateObject(crs_object);
            if (crs == null)
                return new File.Property[] { };
            if (crs.Type != Aml.ACPIObject.DataType.Buffer)
                return new File.Property[] { };

            List<File.Property> ret = new List<File.Property>();
            acpi.InterpretResources(crs, ret);
            return ret;
        }

        public ICollection<File.Property> GetCurrentResources()
        {
            return GetCurrentResources("._CRS");
        }

        public IDictionary<int, ACPIInterrupt[]> GetPRT()
        {
            var prt = EvaluateObject("_PRT");
            if (prt == null || prt.Type != ACPIObject.DataType.Package)
                return null;

            Dictionary<int, ACPIInterrupt[]> prts = new Dictionary<int, ACPIInterrupt[]>(
                new tysos.Program.MyGenericEqualityComparer<int>());

            foreach (var prtentry in (ACPIObject[])prt.Data)
            {
                if (prtentry.Type == ACPIObject.DataType.Package)
                {
                    var prtobj = prtentry.Data as ACPIObject[];
                    int dev = (int)prtobj[0].IntegerData;
                    if (!prts.ContainsKey(dev))
                        prts[dev] = new ACPIInterrupt[4];

                    int pin = (int)prtobj[1].IntegerData;

                    StringBuilder sb = new StringBuilder();
                    sb.Append("_PRT: ");
                    for (int i = 0; i < prtobj.Length; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append(i.ToString());
                        sb.Append(": ");
                        sb.Append(prtobj[i].Type.ToString());
                        sb.Append(": ");
                        sb.Append(prtobj[i].Data.ToString());
                    }
                    System.Diagnostics.Debugger.Log(0, "pci", sb.ToString());

                    System.Diagnostics.Debugger.Log(0, "pci", "PRT: dev: " + dev.ToString() + ", pin: " + pin.ToString() + ", source.Type: " + prtobj[2].Type.ToString() + ", sourceIndex: " + prtobj[3].IntegerData.ToString());

                    if (prtobj[2].Type == ACPIObject.DataType.ObjectReference)
                    {
                        ACPIObject.ObjRefData ord = prtobj[2].Data as
                            ACPIObject.ObjRefData;

                        System.Diagnostics.Debugger.Log(0, "pci", ord.Object.Name);

                        var crs = acpi.n.Evaluate(ord.Object.Name + "._CRS", acpi.mi);
                        List<File.Property> props = new List<File.Property>();
                        acpi.InterpretResources(crs, props);

                        if (props.Count == 1)
                        {
                            prts[dev][pin] = props[0].Value as ACPIInterrupt;
                        }
                    }
                    else if (prtobj[2].Type == ACPIObject.DataType.Integer &&
                        prtobj[2].IntegerData == 0)
                    {
                        int gsi = (int)prtobj[3].IntegerData;
                        prts[dev][pin] = acpi.GeneratePCIIRQ(gsi);
                    }
                }
            }

            return prts;
        }

        public static IDictionary<Aml.ACPIName, ACPIConfiguration> GetConfiguration(IEnumerable<tysos.lib.File.Property> props)
        {
            Dictionary<Aml.ACPIName, ACPIConfiguration> ret = new Dictionary<Aml.ACPIName, ACPIConfiguration>(
                new tysos.Program.MyGenericEqualityComparer<Aml.ACPIName>());
            foreach(var prop in props)
            {
                if(prop.Name == "acpiconf" && (prop.Value is ACPIConfiguration))
                {
                    var conf = prop.Value as ACPIConfiguration;
                    ret[conf.DeviceName] = conf;
                }
            }

            return ret;
        }

        public IList<Aml.ACPIName> GetDevices()
        {
            return GetDevices(-1);
        }

        public IList<Aml.ACPIName> GetDevices(int depth)
        {
            List<Aml.ACPIName> ret = new List<Aml.ACPIName>();

            foreach (var dev in acpi.n.Devices)
            {
                Aml.ACPIName dev_name = dev.Key;
                if (dev.Value.Initialized == false)
                    continue;

                /* Now check the new name is a subobject of the current device */
                if (dev_name.ElementCount < device.ElementCount)
                    continue;
                if (depth != -1 && dev_name.ElementCount > device.ElementCount + depth)
                    continue;

                bool subdevice = true;
                for (int i = 0; i < device.ElementCount; i++)
                {
                    if (dev_name.NameElement(i).Equals(device.NameElement(i)) == false)
                    {
                        subdevice = false;
                        break;
                    }
                }

                if (subdevice)
                    ret.Add(dev_name);
            }

            return ret;
        }

        public override string ToString()
        {
            return "ACPIConfiguration (" + DeviceName + ")";
        }
    }
}
