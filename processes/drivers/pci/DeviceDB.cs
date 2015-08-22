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

namespace pci
{
    class DeviceDBKey
    {
        public uint VendorID = 0xffffffff;
        public uint DeviceID = 0xffffffff;
        public uint RevisionID = 0xffffffff;

        public uint ClassCode = 0xffffffff;
        public uint SubclassCode = 0xffffffff;
        public uint ProgIF = 0xffffffff;
    }

    class DeviceDBEntry
    {
        public DeviceDBKey Key;
        public string HumanDeviceName;
        public string HumanManufacturerName;
        public string DriverName;
        public string SubdriverName;
        public IList<BAROverride> BAROverrides;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (DriverName == null)
                sb.Append("no driver required");
            else
                sb.Append(DriverName);
            sb.Append(" (");
            
            if(HumanManufacturerName != null && HumanManufacturerName != "")
            {
                sb.Append(HumanManufacturerName);
                sb.Append(" ");
            }

            if (HumanDeviceName != null && HumanDeviceName != "")
            {
                sb.Append(HumanDeviceName);
            }

            sb.Append(")");
            return sb.ToString();
        }
    }

    class BAROverride
    {
        public ulong Value;
        public ulong Length;
        public int Type;        // 0 = memory, 1 = IO
    }

    partial class DeviceDB
    {
        static Dictionary<DeviceDBKey, DeviceDBEntry> db_vd, db_vdr, db_cs, db_csp;

        public static DeviceDBEntry GetDeviceDetails(DeviceDBKey id)
        {
            /* Try and match on vendor, device and revision, followed by
            vendor and device, then class, subclass and progIF followed by
            class and subclass */

            DeviceDBEntry ret = null;

            DeviceDBEntry vdr;
            if(db_vdr.TryGetValue(id, out vdr))
            {
                if (ret == null)
                    ret = vdr;
                System.Diagnostics.Debugger.Log(0, "pci", "matched vendor/device/revision to " + vdr.ToString());
            }

            DeviceDBEntry vd;
            if (db_vd.TryGetValue(id, out vd))
            {
                if (ret == null)
                    ret = vd;
                System.Diagnostics.Debugger.Log(0, "pci", "matched vendor/device to " + vd.ToString());
            }

            DeviceDBEntry csp;
            if (db_csp.TryGetValue(id, out csp))
            {
                if (ret == null)
                    ret = csp;
                System.Diagnostics.Debugger.Log(0, "pci", "matched class/subclass/progIF to " + csp.ToString());
            }

            DeviceDBEntry cs;
            if (db_cs.TryGetValue(id, out cs))
            {
                if (ret == null)
                    ret = cs;
                System.Diagnostics.Debugger.Log(0, "pci", "matched class/subclass to " + cs.ToString());
            }

            if (ret != null)
                ret.Key = id;

            return ret;
        }
    }

    class VendorDeviceComparer : IEqualityComparer<DeviceDBKey>
    {
        public bool Equals(DeviceDBKey x, DeviceDBKey y)
        {
            if (x.DeviceID != y.DeviceID)
                return false;
            if (x.VendorID != y.VendorID)
                return false;
            return true;
        }

        public int GetHashCode(DeviceDBKey obj)
        {
            return obj.VendorID.GetHashCode() ^ obj.DeviceID.GetHashCode();
        }
    }

    class VendorDeviceRevisionComparer : IEqualityComparer<DeviceDBKey>
    {
        public bool Equals(DeviceDBKey x, DeviceDBKey y)
        {
            if (x.DeviceID != y.DeviceID)
                return false;
            if (x.VendorID != y.VendorID)
                return false;
            if (x.RevisionID != y.RevisionID)
                return false;
            return true;
        }

        public int GetHashCode(DeviceDBKey obj)
        {
            return obj.VendorID.GetHashCode() ^ obj.DeviceID.GetHashCode() ^
                obj.RevisionID.GetHashCode();
        }
    }

    class ClassSubclassComparer : IEqualityComparer<DeviceDBKey>
    {
        public bool Equals(DeviceDBKey x, DeviceDBKey y)
        {
            if (x.ClassCode != y.ClassCode)
                return false;
            if (x.SubclassCode != y.SubclassCode)
                return false;
            return true;
        }

        public int GetHashCode(DeviceDBKey obj)
        {
            return obj.ClassCode.GetHashCode() ^ obj.SubclassCode.GetHashCode();
        }
    }

    class ClassSubclassProgIFComparer : IEqualityComparer<DeviceDBKey>
    {
        public bool Equals(DeviceDBKey x, DeviceDBKey y)
        {
            if (x.ClassCode != y.ClassCode)
                return false;
            if (x.SubclassCode != y.SubclassCode)
                return false;
            if (x.ProgIF != y.ProgIF)
                return false;
            return true;
        }

        public int GetHashCode(DeviceDBKey obj)
        {
            return obj.ClassCode.GetHashCode() ^ obj.SubclassCode.GetHashCode() ^
                obj.ProgIF.GetHashCode();
        }
    }
}
