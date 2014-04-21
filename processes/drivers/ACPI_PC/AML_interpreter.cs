/* Copyright (C) 2011 by John Cronin
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

namespace aml_interpreter
{
    partial class AML
    {
        internal class AMLData : Item
        {
            public enum DataType
            {
                Uninitialized, Buffer, BufferField, DDBHandle, Debug, Device, Event, Field, Integer, IntegerConstant, Method,
                Mutex, ObjRef, OpRegion, Package, PowerResource, Processor, String, ThermalZone, ResourceAccess, LocalArg, LocalVal
            };

            public DataType Type;
            public object Val;

            public bool GreaterThan(AMLData o2)
            {
                if (Type == DataType.Integer)
                {
                    if (Integer > o2.Integer)
                        return true;
                    else
                        return false;
                }
                if (Type == DataType.String)
                {
                    if (String.Compare(String, o2.String) > 0)
                        return true;
                    else
                        return false;
                }
                throw new NotSupportedException();
            }

            public bool EqualTo(AMLData o2)
            {
                if (Type == DataType.Integer)
                {
                    if (Integer == o2.Integer)
                        return true;
                    else
                        return false;
                }
                if (Type == DataType.String)
                {
                    if (String.Compare(String, o2.String) == 0)
                        return true;
                    else
                        return false;
                }
                throw new NotSupportedException();
            }

            public bool LessThan(AMLData o2)
            {
                if (Type == DataType.Integer)
                {
                    if (Integer < o2.Integer)
                        return true;
                    else
                        return false;
                }
                if (Type == DataType.String)
                {
                    if (String.Compare(String, o2.String) < 0)
                        return true;
                    else
                        return false;
                }
                throw new NotSupportedException();
            }

            public static implicit operator long(AMLData d)
            {
                if (d.Type != DataType.Integer)
                    throw new Exception("Not an integer");
                else
                    return Convert.ToInt64(d.Val);
            }
            public static implicit operator bool(AMLData d)
            {
                if (d == 0L)
                    return false;
                return true;
            }

            public long Integer
            {
                get
                {
                    if (Type == DataType.ResourceAccess)
                        return (long)ReadData(ResourceAccess);
                    else if (Type != DataType.Integer)
                        throw new Exception("Not an integer");
                    else
                        return Convert.ToInt64(Val);
                }
            }
            public byte[] Buffer { get { if (Type != DataType.Buffer) throw new Exception("Not a buffer"); else return (byte[])Val; } }
            public Resource_Access ResourceAccess { get { if (Type != DataType.ResourceAccess) throw new Exception("Not a resource access object"); else return (Resource_Access)Val; } }
            public string String { get { if (Type != DataType.String) throw new Exception("Not a string"); else return (string)Val; } }
            public Reference ObjRef { get { if (Type != DataType.ObjRef) throw new Exception("Not an object reference"); else return (Reference)Val; } }

            public class Resource_Access
            {
                public OpRegion.RegionSpaceType RegionSpace;
                public ulong ByteIndex;
                public int LengthInBits;
                public Field.AccessTypeType AccessType;
                public Field.LockRuleType LockRule;
                public Field.UpdateRuleType UpdateRule;
                public int BitIndex;

                public Resource_Access IndexResource;
                public Resource_Access ValueResource;
            }

            public class Reference { public AMLData Val; }
            public class BufferField { public AMLData Source; public AMLData Index; }
        }

        internal class AMLState
        {
            public AMLData[] LocalObj = new AMLData[8];
            public AMLData[] LocalArg = new AMLData[7];

            public bool end_reached = false;
            public AMLData ReturnVal = new AMLData { Type = AMLData.DataType.Uninitialized };
        }

        internal AMLData RunMethod(Method m) { return RunMethod(m.TermList, null, null); }
        internal AMLData RunMethod(Method m, List<AMLData> Arguments) { return RunMethod(m.TermList, null, Arguments); }
        internal AMLData RunMethod(List<Item> opcodes, AMLState state) { return RunMethod(opcodes, state, null); }
        internal AMLData RunMethod(List<Item> opcodes, AMLState state, List<AMLData> Arguments)
        {
            if (state == null)
            {
                state = new AMLState();

                for (int i = 0; i < 8; i++)
                {
                    if (i < 7)
                    {
                        if ((Arguments != null) && (i < Arguments.Count))
                            state.LocalArg[i] = Arguments[i];
                        else
                            state.LocalArg[i] = new AMLData { Type = AMLData.DataType.Uninitialized };
                    }
                    state.LocalObj[i] = new AMLData { Type = AMLData.DataType.Uninitialized };
                }
            }

            foreach (Item i in opcodes)
            {
                if (i is OpCode)
                {
                    OpCode o = i as OpCode;
                    RunOpcode(o, state);
                }
                if (state.end_reached)
                    break;
            }

            return state.ReturnVal;
        }

        internal AMLData GetObject(Item item, AMLState state)
        {
            if (item is UserTermObj)
            {
                UserTermObj uto = item as UserTermObj;

                foreach (string s in uto.Name.AllNameStrings)
                {
                    if (Objects.ContainsKey(s))
                    {
                        Object o = Objects[s];
                        if (o is Method)
                            return RunOpcode(uto, state);
                        else
                            return GetObject(o, state);
                    }
                    if (Fields.ContainsKey(s))
                        return GetObject(Fields[s], state);
                }

                throw new Exception("UserObj: " + uto.Name.FullNameString + " not found");
            }
            else if (item is Method)
                return RunMethod(item as Method);
            else if (item is NamedObject)
            {
                NamedObject no = item as NamedObject;
                return GetObject(no.Val, state);
            }
            else if (item is Buffer)
            {
                Buffer b = item as Buffer;
                long byte_length = do_load(GetObject(b.BufSize, state), state);
                int act_length = b.Init_Val.Length;
                if ((int)byte_length > act_length)
                    act_length = (int)byte_length;

                byte[] ret = new byte[act_length];
                ret.Initialize();

                b.Init_Val.CopyTo(ret, 0);

                return new AMLData { Type = AMLData.DataType.Buffer, Val = ret };
            }
            else if (item is Package)
            {
                Package p = item as Package;

                List<AMLData> entries = new List<AMLData>();
                foreach (Item i in p.Items)
                    entries.Add(GetObject(i, state));
                return new AMLData { Type = AMLData.DataType.Package, Val = entries.ToArray() };
            }
            else if (item is ByteConst)
            { return new AMLData { Type = AMLData.DataType.Integer, Val = (long)((ByteConst)item).Val }; }
            else if (item is WordConst)
            { return new AMLData { Type = AMLData.DataType.Integer, Val = (long)((WordConst)item).Val }; }
            else if (item is DWordConst)
            { return new AMLData { Type = AMLData.DataType.Integer, Val = (long)((DWordConst)item).Val }; }
            else if (item is ConstObj)
            {
                ConstObj co = item as ConstObj;
                switch (co.Type)
                {
                    case ConstObj.DataType.One:
                        return new AMLData { Type = AMLData.DataType.Integer, Val = 1L };
                    case ConstObj.DataType.Ones:
                        return new AMLData { Type = AMLData.DataType.Integer, Val = -1L };
                    case ConstObj.DataType.Zero:
                        return new AMLData { Type = AMLData.DataType.Integer, Val = 0L };
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (item is Field.FieldEntry)
            {
                if (item is Field.IndexFieldEntry)
                {
                    Field.IndexFieldEntry ife = item as Field.IndexFieldEntry;

                    AMLData index = GetObject(new UserTermObj { Name = ife.IndexName }, state);
                    AMLData val = GetObject(new UserTermObj { Name = ife.DataName }, state);

                    AMLData.Resource_Access ret = new AMLData.Resource_Access();
                    ret.AccessType = ife.AccessType;
                    ret.BitIndex = ife.BitIndex % 8;
                    ret.ByteIndex = (ulong)ife.BitIndex / 8;
                    ret.IndexResource = index.ResourceAccess;
                    ret.LengthInBits = ife.BitSize;
                    ret.LockRule = ife.LockRule;
                    ret.RegionSpace = OpRegion.RegionSpaceType.Indexed;
                    ret.UpdateRule = ife.UpdateRule;
                    ret.ValueResource = val.ResourceAccess;

                    return new AMLData { Type = AMLData.DataType.ResourceAccess, Val = ret };
                }
                else if (item is Field.NamedFieldEntry)
                {
                    Field.NamedFieldEntry nfe = item as Field.NamedFieldEntry;

                    OpRegion or = null;
                    foreach (string s in nfe.RegionName.AllNameStrings)
                    {
                        if (OpRegions.ContainsKey(s))
                            or = OpRegions[s];
                    }
                    if (or == null)
                        throw new Exception("OpRegion not found");

                    AMLData.Resource_Access ra = new AMLData.Resource_Access();
                    ra.RegionSpace = or.RegionSpace;

                    ulong byteoffset = (ulong)(nfe.BitIndex / 8);
                    int bitoffset = nfe.BitIndex % 8;
                    long region_offset = GetObject(or.RegionOffset, state);
                    long region_len = GetObject(or.RegionLen, state);
                    ra.ByteIndex = byteoffset + (ulong)region_offset;
                    ra.BitIndex = bitoffset;
                    ra.LengthInBits = nfe.BitSize;
                    ra.AccessType = nfe.AccessType;
                    ra.LockRule = nfe.LockRule;
                    ra.UpdateRule = nfe.UpdateRule;

                    return new AMLData { Type = AMLData.DataType.ResourceAccess, Val = ra };
                }
                throw new NotImplementedException();
            }
            else if (item is OpCode)
                return RunOpcode(item as OpCode, state);
            else if (item is StringConst)
            {
                StringConst sc = item as StringConst;
                if (sc.String == null)
                    return new AMLData { Type = AMLData.DataType.Uninitialized };
                else
                    return new AMLData { Type = AMLData.DataType.String, Val = sc.String };
            }
            else if (item is Arg)
                return new AMLData { Type = AMLData.DataType.LocalArg, Val = ((Arg)item).ArgNo };
            else if (item is LocalObj)
                return new AMLData { Type = AMLData.DataType.LocalVal, Val = ((LocalObj)item).ObjNo };
            else if (item is AMLData)
                return do_load(item as AMLData, state);

            throw new NotImplementedException();
        }

        private AMLData RunOpcode(OpCode o, AMLState state)
        {
            if (o is UserTermObj)
            {
                UserTermObj uto = o as UserTermObj;

                foreach (string s in uto.Name.AllNameStrings)
                {
                    if (Objects.ContainsKey(s))
                    {
                        Method m = Objects[s] as Method;
                        if (m == null)
                            throw new Exception("Error: not a method");

                        List<AMLData> args = new List<AMLData>();
                        foreach (Item arg in uto.Arguments)
                            args.Add(do_load(GetObject(arg, state), state));

                        return RunMethod(m, args);
                    }
                }
            }

            if (o is NamedObject)
            {
                NamedObject no = o as NamedObject;

                NamedObject new_no = new NamedObject();
                new_no.Name = no.Name;

                new_no.Val = GetObject(no.Val, state);

                if (Objects.ContainsKey(new_no.Name.FullNameString))
                    Objects.Remove(new_no.Name.FullNameString);
                Objects.Add(new_no.Name.FullNameString, new_no);                
            }

            switch (o.Opcode)
            {
                case OpCode.Opcodes.MethodOp:
                case OpCode.Opcodes.NameOp:
                case OpCode.Opcodes.NoopOp:
                    return new AMLData { Type = AMLData.DataType.Uninitialized };

                case OpCode.Opcodes.ScopeOp:

                    return RunMethod(o.Arguments, state);

                case OpCode.Opcodes.CreateWordFieldOp:
                    {
                        AMLData Buffer = GetObject(((ByteField)o).SourceBuffer, state);
                        AMLData ByteIndex = GetObject(((ByteField)o).ByteIndex, state);

                        byte b1 = Buffer.Buffer[ByteIndex.Integer];
                        byte b2 = Buffer.Buffer[ByteIndex.Integer + 1];

                        ushort val = (ushort)((ulong)b1 + ((ulong)b2 << 8));

                        WordConst c = new WordConst { Val = val };
                        FieldWithVal f = new FieldWithVal { Name = ((ByteField)o).Name, Val = c };
                        Fields.Add(f.Name.ToString(), f);
                    }
                    break;

                case OpCode.Opcodes.IfOp:
                    {
                        AMLData Predicate = do_load(GetObject(((IfElseOp)o).Predicate, state), state);

                        if (Predicate)
                            RunMethod(((IfElseOp)o).IfList, state);
                        else
                            RunMethod(((IfElseOp)o).ElseList, state);
                    }
                    break;

                case OpCode.Opcodes.AndOp:
                case OpCode.Opcodes.AddOp:
                    {
                        AMLData o1 = do_load(GetObject(o.Arguments[0], state), state);
                        AMLData o2 = do_load(GetObject(o.Arguments[1], state), state);

                        long val = 0;
                        switch (o.Opcode)
                        {
                            case OpCode.Opcodes.AndOp:
                                val = o1.Integer & o2.Integer;
                                break;
                            case OpCode.Opcodes.AddOp:
                                val = o1.Integer + o2.Integer;
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        AMLData ret = new AMLData { Type = AMLData.DataType.Integer, Val = val };

                        AMLData target = GetObject(o.Arguments[2], state);
                        if (target.Type != AMLData.DataType.Uninitialized)
                            do_store(ret, target, state);

                        return ret;
                    }

                case OpCode.Opcodes.LEqualOp:
                case OpCode.Opcodes.LGreaterEqualOp:
                case OpCode.Opcodes.LGreaterOp:
                case OpCode.Opcodes.LLessEqualOp:
                case OpCode.Opcodes.LLessOp:
                case OpCode.Opcodes.LNotEqualOp:
                case OpCode.Opcodes.LandOp:
                case OpCode.Opcodes.LorOp:
                    {
                        AMLData o1 = do_load(GetObject(o.Arguments[0], state), state);
                        AMLData o2 = do_load(GetObject(o.Arguments[1], state), state);

                        bool ret = false;

                        switch (o.Opcode)
                        {
                            case OpCode.Opcodes.LEqualOp:
                                if (o1.Val.Equals(o2.Val))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LGreaterEqualOp:
                                if (o1.GreaterThan(o2) || o1.EqualTo(o2))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LGreaterOp:
                                if (o1.GreaterThan(o2))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LLessEqualOp:
                                if (o1.LessThan(o2) || o1.EqualTo(o2))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LLessOp:
                                if (o1.LessThan(o2))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LNotEqualOp:
                                if (!o1.EqualTo(o2))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LandOp:
                                if ((o1.Integer != 0) && (o2.Integer != 0))
                                    ret = true;
                                break;
                            case OpCode.Opcodes.LorOp:
                                if ((o1.Integer != 0) || (o2.Integer != 0))
                                    ret = true;
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        return new AMLData { Type = AMLData.DataType.Integer, Val = (ret) ? 0x1 : 0x0 };
                    }

                case OpCode.Opcodes.LnotOp:
                    {
                        AMLData o1 = do_load(GetObject(o.Arguments[0], state), state);
                        bool ret = false;

                        if ((o1.Type == AMLData.DataType.Integer) && (o1.Integer == 0))
                            ret = true;

                        return new AMLData { Type = AMLData.DataType.Integer, Val = (ret) ? 0x1 : 0x0 };
                    }

                case OpCode.Opcodes.StoreOp:
                    {
                        AMLData data = do_load(GetObject(o.Arguments[0], state), state);
                        AMLData target = GetObject(o.Arguments[1], state);
                        do_store(data, target, state);
                    }
                    break;

                case OpCode.Opcodes.ReturnOp:
                    {
                        AMLData ret_val = GetObject(o.Arguments[0], state);
                        state.ReturnVal = do_load(ret_val, state);
                        state.end_reached = true;
                    }
                    break;

                case OpCode.Opcodes.SizeOfOp:
                    {
                        AMLData data = GetObject(o.Arguments[0], state);

                        if ((data.Type == AMLData.DataType.LocalArg) || (data.Type == AMLData.DataType.LocalVal) || (data.Type == AMLData.DataType.ResourceAccess))
                            data = do_load(data, state);

                        switch (data.Type)
                        {
                            case AMLData.DataType.String:
                                return new AMLData { Type = AMLData.DataType.Integer, Val = data.String.Length };
                            case AMLData.DataType.Buffer:
                                return new AMLData { Type = AMLData.DataType.Integer, Val = data.Buffer.Length };
                            default:
                                throw new NotImplementedException();
                        }
                    }

                case OpCode.Opcodes.DecrementOp:
                case OpCode.Opcodes.IncrementOp:
                    {
                        AMLData data = GetObject(o.Arguments[0], state);
                        long val = do_load(data, state).Integer;
                        switch (o.Opcode)
                        {
                            case OpCode.Opcodes.DecrementOp:
                                val--;
                                break;
                            case OpCode.Opcodes.IncrementOp:
                                val++;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        do_store(new AMLData { Type = AMLData.DataType.Integer, Val = val }, data, state);
                        break;
                    }

                case OpCode.Opcodes.WhileOp:
                    {
                        IfElseOp w = o as IfElseOp;
                        bool cont = true;
                        while (cont)
                        {
                            AMLData predicate = GetObject(w.Predicate, state);
                            if (do_load(predicate, state))
                                RunMethod(w.IfList, state);
                            else
                                cont = false;
                        }
                    }
                    break;

                case OpCode.Opcodes.DerefOfOp:
                    {
                        AMLData data = GetObject(o.Arguments[0], state);
                        data = do_load(data, state);

                        switch (data.Type)
                        {
                            case AMLData.DataType.ObjRef:
                                {
                                    AMLData.Reference r = data.ObjRef;
                                    return r.Val;
                                }

                            case AMLData.DataType.String:
                                {
                                    string name = data.String;
                                    throw new NotImplementedException();
                                }
                            case AMLData.DataType.BufferField:

                            default:
                                throw new NotSupportedException();
                        }
                    }

                case OpCode.Opcodes.IndexOp:
                    {
                        AMLData source = GetObject(o.Arguments[0], state);
                        source = do_load(source, state);
                        AMLData index = GetObject(o.Arguments[1], state);
                        index = do_load(index, state);

                        AMLData r;

                        switch (source.Type)
                        {
                            case AMLData.DataType.Buffer:
                                AMLData.BufferField ret = new AMLData.BufferField { Index = index, Source = source };
                                r = new AMLData { Type = AMLData.DataType.ObjRef, Val = new AMLData.Reference { Val = new AMLData { Type = AMLData.DataType.BufferField, Val = ret } } };
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        AMLData target = GetObject(o.Arguments[2], state);
                        if (target.Type != AMLData.DataType.Uninitialized)
                            do_store(r, target, state);

                        return r;
                    }

                default:
                    throw new NotImplementedException();
            }

            return new AMLData { Type = AMLData.DataType.Uninitialized };
        }

        private void do_store(AMLData data, AMLData target, AMLState state)
        {
            switch (data.Type)
            {
                case AMLData.DataType.LocalArg:
                case AMLData.DataType.LocalVal:
                case AMLData.DataType.ResourceAccess:
                    data = do_load(data, state);
                    break;
            }

            switch (target.Type)
            {
                case AMLData.DataType.ResourceAccess:
                    WriteData(target.ResourceAccess, data.Val);
                    break;

                case AMLData.DataType.LocalArg:
                    state.LocalArg[(int)target.Val] = data;
                    break;

                case AMLData.DataType.LocalVal:
                    state.LocalObj[(int)target.Val] = data;
                    break;

                case AMLData.DataType.Buffer:
                    {
                        byte[] data_val = null;

                        switch (data.Type)
                        {
                            case AMLData.DataType.String:
                                data_val = new byte[((string)data.Val).Length];
                                for(int i = 0; i < data_val.Length; i++)
                                    data_val[i] = (byte)((string)data.Val)[i];
                                break;
                        }

                        if (data_val == null)
                            throw new Exception();

                        if (data_val.Length > ((byte[])target.Val).Length)
                            throw new Exception();

                        data_val.CopyTo((byte[])target.Val, 0);
                    }
                    break;

                default:
                    throw new Exception();
            }
        }


        private AMLData do_load(AMLData source, AMLState state)
        {
            switch (source.Type)
            {
                case AMLData.DataType.ResourceAccess:
                    return new AMLData { Type = AMLData.DataType.Integer, Val = ReadData(source.ResourceAccess) };

                case AMLData.DataType.LocalArg:
                    return do_load(state.LocalArg[(int)source.Val], state);

                case AMLData.DataType.LocalVal:
                    return do_load(state.LocalObj[(int)source.Val], state);

                case AMLData.DataType.BufferField:
                    {
                        AMLData.BufferField bf = source.Val as AMLData.BufferField;
                        byte[] buf = do_load(bf.Source, state).Buffer;
                        long index = do_load(bf.Index, state).Integer;
                        return new AMLData { Type = AMLData.DataType.Integer, Val = buf[index] };
                    }

                default:
                    return source;
            }
        }
    }
}
