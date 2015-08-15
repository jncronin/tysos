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
    class ACPIObject
    {
        public ACPIName Name;

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
            public Namespace n;
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

        public ACPIObject EvaluateTo(DataType dest_type, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            if (this.Type == dest_type)
                return this;

            /* First evaluate fields methods etc */
            ACPIObject ret = Evaluate(mi, s, n);

            if (ret.Type == dest_type)
                return ret;

            /* Then try to convert the data if required */
            switch (ret.Type)
            {
                case DataType.String:
                    {
                        string src = (string)ret.Data;
                        switch (dest_type)
                        {
                            case DataType.Buffer:
                                {
                                    byte[] dst = new byte[src.Length];
                                    for (int i = 0; i < src.Length; i++)
                                        dst[i] = (byte)src[i];
                                    return new ACPIObject(DataType.Buffer, dst);
                                }
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Write(ACPIObject d, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            Write(d, 0, mi, s, n);
        }

        public void Write(ACPIObject d, int Offset, IMachineInterface mi, Namespace.State s, Namespace n)
        {
            if (Type == DataType.Uninitialized ||
                (Type == DataType.ObjectReference &&
                ((ObjRefData)Data).Object == null) ||
                (Type == DataType.Integer &&
                (ulong)Data == 0UL))
                return;

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
                        ACPIObject dst = ((ObjRefData)Data).Object.Evaluate(mi, s, n);
                        int idx = ((ObjRefData)Data).Index;
                        dst.Write(d, idx, mi, s, n);
                        return;
                    }
                default:
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                case DataType.DDBHandle:
                    throw new NotImplementedException();
                case DataType.Device:
                    return this;
                case DataType.Event:
                    return this;
                case DataType.FieldUnit:
                    if (Data is FieldUnitData)
                        return DataAccess.ReadField(Data as FieldUnitData, mi);
                    else if (Data is IndexFieldUnitData)
                        return DataAccess.ReadIndexField(Data as IndexFieldUnitData, mi, s, n);
                    throw new NotSupportedException();
                case DataType.Integer:
                    return this;
                case DataType.Local:
                    return s.Locals[(int)Data];
                case DataType.Method:
                    {
                        Namespace.State new_state = new Namespace.State();
                        new_state.Args = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>());
                        new_state.Locals = new Dictionary<int, ACPIObject>(new tysos.Program.MyGenericEqualityComparer<int>());
                        ACPIObject ret;
                        new_state.Scope = Name;

                        MethodData md = Data as MethodData;
                        int midx = md.Offset;

                        if (!n.ParseTermList(md.AML, ref midx, md.Length, out ret, new_state))
                            throw new Exception();

                        return ret;
                    }
                case DataType.Mutex:
                    return this;
                case DataType.ObjectReference:
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
