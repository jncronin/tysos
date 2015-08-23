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
    public class ACPIObject
    {
        public ACPIName Name;

        public bool Initialized = false;
        public bool Present = false;
        public bool Functioning = false;

        public enum DataType
        {
            Uninitialized, Buffer, BufferField, DDBHandle,
            Device, Event, FieldUnit, Integer, Method, Mutex, ObjectReference,
            OpRegion, Package, PowerResource, Processor, String, ThermalZone,
            Arg, Local
        };

        public DataType Type;

        public object Data;

        public ulong IntegerData { get { return (ulong)Data; } }

        public ACPIObject()
        {
            Type = DataType.Uninitialized;
            Data = null;
        }

        public ACPIObject(DataType type, object data)
        {
            Type = type;
            Data = data;
        }

        public class ObjRefData
        {
            private ACPIObject _object = null;
            public ACPIObject Object
            {
                get
                {
                    if (_object != null)
                        return _object;
                    else if (ObjectName != null && n != null && ObjectName.IsNull != true)
                        return n.FindObject(ObjectName);
                    else
                        return null;
                }

                set
                {
                    _object = value;
                }
            }
            public ACPIName ObjectName = null;
            internal Namespace n;
            public int Index = 0;
        }

        public class ProcessorData
        {
            public ulong ID;
            public ulong BlkAddr;
            public ulong BlkLen;
        }

        public class MethodData
        {
            public byte[] AML;
            public int Offset;
            public int Length;
            public int ArgCount;
            public bool Serialized;
            public int SyncLevel;
        }

        public class BufferFieldData
        {
            public ACPIObject Buffer;
            public int BitOffset;
            public int BitLength;
        }

        public class FieldUnitData : BaseFieldUnitData
        {
            public ACPIObject OpRegion;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("FieldUnit: ");
                sb.Append(((OpRegionData)OpRegion.Data).ToString());
                sb.Append(": BitOffset: ");
                sb.Append(BitOffset.ToString());
                sb.Append(", BitLength: ");
                sb.Append(BitLength.ToString());
                return sb.ToString();
            }
        }

        public class IndexFieldUnitData : BaseFieldUnitData
        {
            public ACPIObject Index;
            public ACPIObject Data;
        }

        public class BaseFieldUnitData
        {
            public int BitOffset;
            public int BitLength;
            public AccessType Access;
            public AccessAttribType AccessAttrib;
            public LockRuleType LockRule;
            public UpdateRuleType UpdateRule;

            public enum AccessAttribType
            {
                Undefined, SMBQuick, SMBSendReceive, SMBByte, SMBWord,
                SMBBlock, SMBProcessCall, SMBBlockProcessCall
            };
            public enum AccessType { AnyAcc, ByteAcc, WordAcc, DWordAcc, QWordAcc, BufferAcc };
            public enum LockRuleType { Lock, NoLock };
            public enum UpdateRuleType { Preserve, WriteAsOnes, WriteAsZeros };
        }

        public class OpRegionData
        {
            public int RegionSpace;
            public ulong Offset, Length;
            public ACPIName Device;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("RegionSpace: ");
                switch (RegionSpace)
                {
                    case 0:
                        sb.Append("SystemMemory");
                        break;
                    case 1:
                        sb.Append("SystemIO");
                        break;
                    case 2:
                        sb.Append("PCI_Config");
                        break;
                    case 3:
                        sb.Append("EmbeddedControl");
                        break;
                    case 4:
                        sb.Append("SMBus");
                        break;
                    case 5:
                        sb.Append("CMOS");
                        break;
                    case 6:
                        sb.Append("PCIBarTarget");
                        break;
                    case 7:
                        sb.Append("IPMI");
                        break;
                    default:
                        sb.Append(RegionSpace.ToString());
                        break;
                }

                sb.Append(", Offset: 0x");
                sb.Append(Offset.ToString("x"));
                sb.Append(", Length: 0x");
                sb.Append(Length.ToString("x"));

                return sb.ToString();
            }
        }

        internal ACPIObject EvaluateTo(DataType dest_type, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            if (this.Type == dest_type)
                return this;

            /* First evaluate fields methods etc */
            ACPIObject ret = Evaluate(mi, s, n);
            return ConvertTo(ret, dest_type);
        }

        internal static ACPIObject ConvertTo(ACPIObject source, DataType dest_type)
        { 
            if (source.Type == dest_type)
                return source;

            /* Then try to convert the data if required */
            switch (source.Type)
            {
                case DataType.String:
                    {
                        string src = (string)source.Data;
                        switch (dest_type)
                        {
                            case DataType.Buffer:
                                {
                                    byte[] dst = new byte[src.Length];
                                    for (int i = 0; i < src.Length; i++)
                                        dst[i] = (byte)src[i];
                                    return new ACPIObject(DataType.Buffer, dst);
                                }

                            case DataType.Integer:
                                {
                                    ulong ret = 0;
                                    int digits = 0;
                                    foreach(char c in src)
                                    {
                                        if(digits == 16)
                                            break;

                                        ulong cur_digit = 0;

                                        if (c >= '0' && c <= '9')
                                            cur_digit = (ulong)(c - '0');
                                        else if (c >= 'a' && c <= 'f')
                                            cur_digit = (ulong)(c - 'a') + 0xa;
                                        else if (c >= 'A' && c <= 'F')
                                            cur_digit = (ulong)(c - 'A') + 0xa;
                                        else
                                            break;

                                        ret <<= 4;
                                        ret |= cur_digit;

                                        digits++;
                                    }
                                    return new ACPIObject(DataType.Integer, digits);
                                }
                        }
                    }
                    break;
                case DataType.Integer:
                    {
                        ulong src = source.IntegerData;
                        switch(dest_type)
                        {
                            case DataType.Buffer:
                                {
                                    byte[] dst = new byte[8];

                                    for(int i = 0; i < 8; i++)
                                    {
                                        dst[i] = (byte)(src & 0xff);
                                        src >>= 8;
                                    }

                                    return new ACPIObject(DataType.Buffer, dst);
                                }
                        }
                    }
                    break;
                case DataType.Buffer:
                    {
                        byte[] src = (byte[])source.Data;
                        switch(dest_type)
                        {
                            case DataType.Integer:
                                {
                                    ulong dst = 0;

                                    for(int i = 0; i < src.Length; i++)
                                    {
                                        byte v = src[i];

                                        if (i > 8)
                                        {
                                            if (v != 0)
                                                throw new Exception("Unable to convert large buffer to integer");
                                        }
                                        else
                                            dst |= (((ulong)v) << (i * 8));
                                    }

                                    return new ACPIObject(DataType.Integer, dst);
                                }
                        }
                    }
                    break;
            }
            throw new NotImplementedException("Convert " + source.Type.ToString() + " to " + dest_type.ToString());
        }

        internal void Write(ACPIObject d, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            Write(d, 0, mi, s, n);
        }

        internal void Write(ACPIObject d, int Offset, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            if (Type == DataType.Uninitialized ||
                (Type == DataType.ObjectReference &&
                ((ObjRefData)Data).Object == null) ||
                (Type == DataType.Integer &&
                (ulong)Data == 0UL))
                return;

            //System.Diagnostics.Debugger.Log(0, "acpipc", "Write: " + d.Type.ToString() + " to " + Type.ToString());

            switch (Type)
            {
                case DataType.Local:
                    s.Locals[(int)Data] = d;
                    return;
                case DataType.Arg:
                    s.Args[(int)Data] = d;
                    return;
                case DataType.Buffer:
                    {
                        ACPIObject new_buf = d.EvaluateTo(DataType.Buffer, mi, s, n);
                        byte[] dst = (byte[])Data;
                        byte[] src = (byte[])new_buf.Data;
                        for (int i = Offset; i < Offset + dst.Length; i++)
                        {
                            if (i < src.Length)
                                dst[i] = src[i];
                            else
                                dst[i] = 0;
                        }
                        return;
                    }
                case DataType.ObjectReference:
                    {
                        ObjRefData ord = Data as ObjRefData;
                        ACPIObject dst = ord.Object;

                        switch (dst.Type)
                        {
                            case DataType.Buffer:
                                dst.Write(d, ord.Index, mi, s, n);
                                return;
                            case DataType.BufferField:
                                dst.Write(d, mi, s, n);
                                return;
                            case DataType.FieldUnit:
                                dst.Write(d, mi, s, n);
                                return;
                            case DataType.Integer:
                                dst.Data = d.EvaluateTo(DataType.Integer, mi, s, n).IntegerData;
                                return;
                            default:
                                throw new NotSupportedException("Write: " + d.Type + " to ObjectReference(" + dst.Type + ")");
                        }
                    }
                case DataType.BufferField:
                    {
                        BufferFieldData bfd = Data as BufferFieldData;
                        ACPIObject new_buf_src = d.EvaluateTo(DataType.Buffer, mi, s, n);
                        ACPIObject new_buf_dst = bfd.Buffer.EvaluateTo(DataType.Buffer, mi, s, n);

                        if (bfd.BitOffset % 8 != 0)
                            throw new NotImplementedException("BitOffset not divisible by 8");
                        if (bfd.BitLength % 8 != 0)
                            throw new NotImplementedException("BitLength not divisible by 8");

                        int byte_offset = bfd.BitOffset / 8;
                        int byte_length = bfd.BitLength / 8;

                        byte[] src = (byte[])new_buf_src.Data;
                        byte[] dst = (byte[])new_buf_dst.Data;

                        for (int i = 0; i < byte_length; i++)
                            dst[byte_offset + i] = src[i];

                        return;
                    }
                case DataType.FieldUnit:
                    if(Data is FieldUnitData)
                    {
                        FieldUnitData fud = Data as FieldUnitData;
                        ACPIObject op_region = fud.OpRegion;
                        if (op_region.Type != DataType.OpRegion)
                            throw new Exception("Write to FieldUnit with invalid OpRegion type (" + op_region.Type.ToString() + ")");
                        OpRegionData ord = op_region.Data as OpRegionData;

                        /* Ensure the requested field index is within the opregion */
                        int byte_offset = fud.BitOffset / 8;
                        int byte_length = fud.BitLength / 8;

                        if ((ulong)byte_offset + (ulong)byte_length > ord.Length)
                            throw new Exception("Write: attempt to write to field beyond length of opregion (offset: " + byte_offset.ToString() + ", length: " + byte_length.ToString() + ", OpRegion.Length: " + ord.Length + ")");

                        if (fud.BitOffset % 8 != 0)
                            throw new NotImplementedException("Write: non-byte aligned offset (" + fud.BitOffset.ToString() + ")");
                        if (fud.BitLength % 8 != 0)
                            throw new NotImplementedException("Write: non-byte aligned length (" + fud.BitLength.ToString() + ")");

                        /* Get the data */
                        ulong int_val = d.EvaluateTo(DataType.Integer, mi, s, n).IntegerData;

                        /* Do the write depending on the op region type */
                        switch(ord.RegionSpace)
                        {
                            case 0:
                                // Memory
                                switch(byte_length)
                                {
                                    case 1:
                                        mi.WriteMemoryByte(ord.Offset + (ulong)byte_offset, (byte)(int_val & 0xff));
                                        return;
                                    case 2:
                                        mi.WriteMemoryWord(ord.Offset + (ulong)byte_offset, (ushort)(int_val & 0xffff));
                                        return;
                                    case 4:
                                        mi.WriteMemoryDWord(ord.Offset + (ulong)byte_offset, (uint)(int_val & 0xffffff));
                                        return;
                                    case 8:
                                        mi.WriteMemoryQWord(ord.Offset + (ulong)byte_offset, int_val);
                                        return;
                                    default:
                                        throw new NotImplementedException("Write: unsupported byte length: " + byte_length.ToString());
                                }
                            case 1:
                                // IO
                                switch (byte_length)
                                {
                                    case 1:
                                        mi.WriteIOByte(ord.Offset + (ulong)byte_offset, (byte)(int_val & 0xff));
                                        return;
                                    case 2:
                                        mi.WriteIOWord(ord.Offset + (ulong)byte_offset, (ushort)(int_val & 0xffff));
                                        return;
                                    case 4:
                                        mi.WriteIODWord(ord.Offset + (ulong)byte_offset, (uint)(int_val & 0xffffff));
                                        return;
                                    case 8:
                                        mi.WriteIOQWord(ord.Offset + (ulong)byte_offset, int_val);
                                        return;
                                    default:
                                        throw new NotImplementedException("Write: unsupported byte length: " + byte_length.ToString());
                                }
                            case 2:
                                // PCI Configuration space
                                {
                                    // try and get the _ADR object for the current device
                                    ACPIObject adr = n.Evaluate(ord.Device.ToString() + "._ADR", mi).EvaluateTo(DataType.Integer, mi, s, n);
                                    if (adr == null)
                                        throw new Exception(ord.Device.ToString() + "._ADR failed");

                                    uint bus = 0;
                                    uint device = ((uint)adr.IntegerData >> 16) & 0xffffU;
                                    uint func = (uint)adr.IntegerData & 0xffffU;

                                    uint offset = (uint)ord.Offset + (uint)byte_offset;

                                    switch(byte_length)
                                    {
                                        case 1:
                                            mi.WritePCIByte(bus, device, func, offset, (byte)int_val);
                                            return;
                                        case 2:
                                            mi.WritePCIWord(bus, device, func, offset, (ushort)int_val);
                                            return;
                                        case 4:
                                            mi.WritePCIDWord(bus, device, func, offset, (uint)int_val);
                                            return;
                                        default:
                                            throw new NotImplementedException("Write: unsupported byte length: " + byte_length.ToString());
                                    }
                                }
                            default:
                                throw new NotImplementedException("Write: unsupported OpRegion type: " + ord.ToString());
                        }
                    }
                    else if(Data is IndexFieldUnitData)
                    {
                        var ifud = Data as IndexFieldUnitData;

                        /* Ensure the requested field index is byte aligned */
                        int byte_offset = ifud.BitOffset / 8;
                        int byte_length = ifud.BitLength / 8;

                        if (ifud.BitOffset % 8 != 0)
                            throw new NotImplementedException("Read: non-byte aligned offset (" + ifud.BitOffset.ToString() + ")");
                        if (ifud.BitLength % 8 != 0)
                            throw new NotImplementedException("Read: non-byte aligned length (" + ifud.BitLength.ToString() + ")");

                        /* Get the data */
                        ulong int_val = d.EvaluateTo(DataType.Integer, mi, s, n).IntegerData;

                        /* Write to index port, write to data port */
                        ifud.Index.Write((ulong)byte_offset, mi, s, n);
                        ifud.Data.Write(int_val, mi, s, n);

                        return;
                    }
                    else
                        throw new NotSupportedException("Invalid FieldUnit data type: " + Data.ToString());
                default:
                    throw new NotImplementedException("Write: " + Type.ToString());
            }
            throw new NotImplementedException();
        }

        public static implicit operator ulong(ACPIObject o)
        {
            return o.IntegerData;
        }

        public static implicit operator ACPIObject(ulong v)
        {
            return new ACPIObject(DataType.Integer, v);
        }

        internal ACPIObject Evaluate(IMachineInterface mi, Namespace.State s, Namespace n)
        {
            /* Evaluate as much as we can */
            switch (Type)
            {
                case DataType.Arg:
                    return s.Args[(int)Data];
                case DataType.Buffer:
                    return this;
                case DataType.BufferField:
                    {
                        BufferFieldData bfd = Data as BufferFieldData;
                        ACPIObject new_buf = bfd.Buffer.EvaluateTo(DataType.Buffer, mi, s, n);

                        if (bfd.BitOffset % 8 != 0)
                            throw new NotImplementedException("BitOffset not divisible by 8");
                        if (bfd.BitLength % 8 != 0)
                            throw new NotImplementedException("BitLength not divisible by 8");

                        int byte_offset = bfd.BitOffset / 8;
                        int byte_length = bfd.BitLength / 8;

                        byte[] ret = new byte[byte_length];
                        byte[] src = (byte[])new_buf.Data;

                        for (int i = 0; i < byte_length; i++)
                            ret[i] = src[byte_offset + i];

                        return new ACPIObject(DataType.Buffer, ret);
                    }                     
                case DataType.DDBHandle:
                    throw new NotImplementedException("DDBHandle");
                case DataType.Device:
                    return this;
                case DataType.Event:
                    return this;
                case DataType.FieldUnit:
                    if (Data is FieldUnitData)
                    {
                        FieldUnitData fud = Data as FieldUnitData;
                        ACPIObject op_region = fud.OpRegion;
                        if (op_region.Type != DataType.OpRegion)
                            throw new Exception("Read from FieldUnit with invalid OpRegion type (" + op_region.Type.ToString() + ")");
                        OpRegionData ord = op_region.Data as OpRegionData;

                        /* Ensure the requested field index is within the opregion */
                        int byte_offset = fud.BitOffset / 8;
                        int byte_length = fud.BitLength / 8;

                        if ((ulong)byte_offset + (ulong)byte_length > ord.Length)
                            throw new Exception("Read: attempt to read to field beyond length of opregion (offset: " + byte_offset.ToString() + ", length: " + byte_length.ToString() + ", OpRegion.Length: " + ord.Length + ")");

                        if (fud.BitOffset % 8 != 0)
                            throw new NotImplementedException("Read: non-byte aligned offset (" + fud.BitOffset.ToString() + ")");
                        if (fud.BitLength % 8 != 0)
                            throw new NotImplementedException("Read: non-byte aligned length (" + fud.BitLength.ToString() + ")");

                        /* Do the read depending on the op region type */
                        switch (ord.RegionSpace)
                        {
                            case 0:
                                // Memory
                                switch (byte_length)
                                {
                                    case 1:
                                        return mi.ReadMemoryByte(ord.Offset + (ulong)byte_offset);
                                    case 2:
                                        return mi.ReadMemoryWord(ord.Offset + (ulong)byte_offset);
                                    case 4:
                                        return mi.ReadMemoryDWord(ord.Offset + (ulong)byte_offset);
                                    case 8:
                                        return mi.ReadMemoryQWord(ord.Offset + (ulong)byte_offset);
                                    default:
                                        throw new NotImplementedException("Read: unsupported byte length: " + byte_length.ToString());
                                }
                            case 1:
                                // IO
                                switch (byte_length)
                                {
                                    case 1:
                                        return mi.ReadIOByte(ord.Offset + (ulong)byte_offset);
                                    case 2:
                                        return mi.ReadIOWord(ord.Offset + (ulong)byte_offset);
                                    case 4:
                                        return mi.ReadIODWord(ord.Offset + (ulong)byte_offset);
                                    case 8:
                                        return mi.ReadIOQWord(ord.Offset + (ulong)byte_offset);
                                    default:
                                        throw new NotImplementedException("Read: unsupported byte length: " + byte_length.ToString());
                                }
                            case 2:
                                // PCI Configuration space
                                {
                                    // try and get the _ADR object for the current device
                                    ACPIObject adr = n.Evaluate(ord.Device.ToString() + "._ADR", mi).EvaluateTo(DataType.Integer, mi, s, n);
                                    if (adr == null)
                                        throw new Exception(ord.Device.ToString() + "._ADR failed");

                                    uint bus = 0;
                                    uint device = ((uint)adr.IntegerData >> 16) & 0xffffU;
                                    uint func = (uint)adr.IntegerData & 0xffffU;

                                    uint offset = (uint)ord.Offset + (uint)byte_offset;

                                    System.Diagnostics.Debugger.Log(0, "acpipc", "Read from PCI conf space for: " + ord.Device.ToString() + ", _ADR: (" + adr.Type.ToString() + ") " + adr.IntegerData.ToString("X"));

                                    switch (byte_length)
                                    {
                                        case 1:
                                            return mi.ReadPCIByte(bus, device, func, offset);
                                        case 2:
                                            return mi.ReadPCIWord(bus, device, func, offset);
                                        case 4:
                                            return mi.ReadPCIDWord(bus, device, func, offset);
                                        default:
                                            throw new NotImplementedException("Read: unsupported byte length: " + byte_length.ToString());
                                    }
                                }
                            default:
                                throw new NotImplementedException("Read: unsupported OpRegion type: " + ord.ToString());
                        }
                    }
                    else if (Data is IndexFieldUnitData)
                    {
                        var ifud = Data as IndexFieldUnitData;

                        /* Ensure the requested field index is byte aligned */
                        int byte_offset = ifud.BitOffset / 8;
                        int byte_length = ifud.BitLength / 8;

                        if (ifud.BitOffset % 8 != 0)
                            throw new NotImplementedException("Read: non-byte aligned offset (" + ifud.BitOffset.ToString() + ")");
                        if (ifud.BitLength % 8 != 0)
                            throw new NotImplementedException("Read: non-byte aligned length (" + ifud.BitLength.ToString() + ")");

                        /* Write to index port, read from data port */
                        ifud.Index.Write((ulong)byte_offset, mi, s, n);
                        var ret = ifud.Data.Evaluate(mi, s, n);
                        switch(byte_length)
                        {
                            case 1:
                                ret.Data = ret.IntegerData & 0xffU;
                                break;
                            case 2:
                                ret.Data = ret.IntegerData & 0xffffU;
                                break;
                        }
                        return ret;
                    }
                    throw new NotSupportedException("Invalid FieldUnit data type: " + Data.ToString());
                case DataType.Integer:
                    return this;
                case DataType.Local:
                    return s.Locals[(int)Data];
                case DataType.Method:
                    {
                        Namespace.State new_state = new Namespace.State();
                        if (s.IsMethodExec)
                            new_state.Args = s.Args;
                        else
                            new_state.Args = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>());
                        new_state.Locals = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>());
                        ACPIObject ret;
                        new_state.Scope = Name;

                        MethodData md = Data as MethodData;
                        int midx = md.Offset;

                        //System.Diagnostics.Debugger.Log(0, "acpipc", "Begin evaluating method: " + Name.ToString());

                        if (!n.ParseTermList(md.AML, ref midx, md.Length, out ret, new_state))
                            throw new Exception();

                        return ret;
                    }
                case DataType.Mutex:
                    return this;
                case DataType.ObjectReference:
                    //System.Diagnostics.Debugger.Log(0, "acpipc", "Begin evaluating object reference: " + ((ACPIObject.ObjRefData)Data).ToString());
                    return this;
                case DataType.OpRegion:
                    return this;
                case DataType.Package:
                    return this;
                case DataType.PowerResource:
                    return this;
                case DataType.Processor:
                    return this;
                case DataType.String:
                    return this;
                case DataType.ThermalZone:
                    return this;
                case DataType.Uninitialized:
                    return this;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
