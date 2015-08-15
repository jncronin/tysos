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

namespace acpipc.Aml
{
    class DataAccess
    {
        public static void WriteField(ACPIObject val, ACPIObject.FieldUnitData fd, IMachineInterface mi)
        {
            ACPIObject.OpRegionData oprd = fd.OpRegion.Data as ACPIObject.OpRegionData;
            ulong addr = oprd.Offset;
            if ((fd.BitOffset % 8) != 0)
                throw new NotImplementedException();
            addr += (ulong)(fd.BitOffset / 8);

            ulong mask = 0xffffffffffffffff;
            int alength = 0;

            switch (fd.Access)
            {
                case ACPIObject.FieldUnitData.AccessType.ByteAcc:
                    alength = 8;
                    break;
                case ACPIObject.FieldUnitData.AccessType.WordAcc:
                    alength = 16;
                    break;
                case ACPIObject.FieldUnitData.AccessType.DWordAcc:
                    alength = 32;
                    break;
                case ACPIObject.FieldUnitData.AccessType.QWordAcc:
                    alength = 64;
                    break;
                case ACPIObject.FieldUnitData.AccessType.AnyAcc:
                    alength = fd.BitLength;
                    break;
            }

            if (alength <= 8)
                alength = 8;
            else if (alength <= 16)
                alength = 16;
            else if (alength <= 32)
                alength = 32;
            else if (alength <= 64)
                alength = 64;
            else
                throw new Exception("Unsupported access length: " + alength.ToString());

            switch (oprd.RegionSpace)
            {
                case 0:
                    // SystemMemory
                    switch (alength)
                    {
                        case 8:
                            mi.WriteMemoryByte(addr, (byte)val);
                            break;
                        case 16:
                            mi.WriteMemoryWord(addr, (ushort)val);
                            break;
                        case 32:
                            mi.WriteMemoryDWord(addr, (uint)val);
                            break;
                        case 64:
                            mi.WriteMemoryQWord(addr, val);
                            break;
                    }
                    break;
                case 1:
                    // SystemIO
                    switch (alength)
                    {
                        case 8:
                            mi.WriteIOByte(addr, (byte)val);
                            break;
                        case 16:
                            mi.WriteIOWord(addr, (ushort)val);
                            break;
                        case 32:
                            mi.WriteIODWord(addr, (uint)val);
                            break;
                        case 64:
                            mi.WriteIOQWord(addr, val);
                            break;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public static ACPIObject ReadIndexField(ACPIObject.IndexFieldUnitData ifd, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            ACPIObject data = ifd.Data.EvaluateTo(ACPIObject.DataType.FieldUnit, mi, s, n);
            ACPIObject index = ifd.Index.EvaluateTo(ACPIObject.DataType.FieldUnit, mi, s, n);

            if (ifd.BitOffset % 8 != 0)
                throw new NotImplementedException();
            if (ifd.BitLength % 8 != 0)
                throw new NotImplementedException();

            ulong byte_offset = (ulong)(ifd.BitOffset / 8);

            WriteField(byte_offset, index.Data as ACPIObject.FieldUnitData, mi);
            ACPIObject res = ReadField(data.Data as ACPIObject.FieldUnitData, mi);
            return res;
        }

        public static ACPIObject ReadField(ACPIObject.FieldUnitData fd, IMachineInterface mi)
        {
            ACPIObject.OpRegionData oprd = fd.OpRegion.Data as ACPIObject.OpRegionData;
            ulong addr = oprd.Offset;
            if ((fd.BitOffset % 8) != 0)
                throw new NotImplementedException();
            addr += (ulong)(fd.BitOffset / 8);

            ulong res = 0;
            ulong mask = 0xffffffffffffffff;
            int alength = 0;

            switch (fd.Access)
            {
                case ACPIObject.FieldUnitData.AccessType.ByteAcc:
                    alength = 8;
                    break;
                case ACPIObject.FieldUnitData.AccessType.WordAcc:
                    alength = 16;
                    break;
                case ACPIObject.FieldUnitData.AccessType.DWordAcc:
                    alength = 32;
                    break;
                case ACPIObject.FieldUnitData.AccessType.QWordAcc:
                    alength = 64;
                    break;
                case ACPIObject.FieldUnitData.AccessType.AnyAcc:
                    alength = fd.BitLength;
                    break;
            }

            if (alength <= 8)
                alength = 8;
            else if (alength <= 16)
                alength = 16;
            else if (alength <= 32)
                alength = 32;
            else if (alength <= 64)
                alength = 64;
            else
                throw new Exception("Unsupported access length: " + alength.ToString());

            switch (oprd.RegionSpace)
            {
                case 0:
                    // SystemMemory
                    switch (alength)
                    {
                        case 8:
                            res = (ulong)mi.ReadMemoryByte(addr);
                            break;
                        case 16:
                            res = (ulong)mi.ReadMemoryWord(addr);
                            break;
                        case 32:
                            res = (ulong)mi.ReadMemoryDWord(addr);
                            break;
                        case 64:
                            res = (ulong)mi.ReadMemoryQWord(addr);
                            break;
                    }
                    break;
                case 1:
                    // SystemIO
                    switch (alength)
                    {
                        case 8:
                            res = (ulong)mi.ReadIOByte(addr);
                            break;
                        case 16:
                            res = (ulong)mi.ReadIOWord(addr);
                            break;
                        case 32:
                            res = (ulong)mi.ReadIODWord(addr);
                            break;
                        case 64:
                            res = (ulong)mi.ReadIOQWord(addr);
                            break;
                    }
                    break;
                case 2:
                    // PCI Conf space
                    {
                        /* ACPI divides the index into 4x 16-bit words:
                         *  - highest is reserved (0)
                         *  - next is PCI device number
                         *  - next is PCI function number
                         *  - lowest is offset in configuration space
                         * 
                         * ACPI enforces that only bus 0 is used
                         */

                        ulong bus = 0;
                        ulong idx = (ulong)fd.BitOffset / 8UL;
                        ulong device = (idx >> 32) & 0x1f;  // 5 bits for a PCI Device
                        ulong func = (idx >> 16) & 0x7;     // 3 bits for function number
                        ulong offset = idx & 0xff;          // 8 bits for register number

                        ulong act_offset = offset & 0xfc;   // enforce 32-bit access

                        ulong pci_addr = act_offset | (func << 8) | (device << 11);

                        mi.WriteIODWord(0xcf8, (uint)pci_addr);
                        uint v = mi.ReadIODWord(0xcfc);
                        res = (ulong)(v >> (int)(offset - act_offset));
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            mask >>= (64 - fd.BitLength);
            res &= mask;

            return new ACPIObject(ACPIObject.DataType.Integer, res);
        }

    }
}
