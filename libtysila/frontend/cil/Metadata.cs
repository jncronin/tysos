/* Copyright (C) 2008 - 2011 by John Cronin
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
using System.IO;

namespace libtysila
{
    public enum BaseType_Type
    {
        End = 0x0,
        Void = 0x1,
        Boolean = 0x2,
        Char = 0x3,
        I1 = 0x4,
        U1 = 0x5,
        I2 = 0x6,
        U2 = 0x7,
        I4 = 0x8,
        U4 = 0x9,
        I8 = 0xa,
        U8 = 0xb,
        R4 = 0xc,
        R8 = 0xd,
        String = 0xe,
        Ptr = 0xf,
        Byref = 0x10,
        ValueType = 0x11,
        Class = 0x12,
        Var = 0x13,
        Array = 0x14,
        GenericInst = 0x15,
        TypedByRef = 0x16,
        I = 0x18,
        U = 0x19,
        FnPtr = 0x1b,
        Object = 0x1c,
        SzArray = 0x1d,
        Boxed = 0xff,

        VirtFtnPtr = 0xfe,

        UninstantiatedGenericParam = 0xfd,
        RefGenericParam = 0xfc,

        Byte = 0x100
    }

    public class Metadata
    {
        public Assembler.AssemblyInformation Information;

        public static Assembler.CliType GetCliType(BaseType_Type etype)
        {
            switch (etype)
            {
                case BaseType_Type.Array:
                    return Assembler.CliType.O;
                case BaseType_Type.Boolean:
                case BaseType_Type.Char:
                case BaseType_Type.I1:
                case BaseType_Type.I2:
                case BaseType_Type.I4:
                case BaseType_Type.U1:
                case BaseType_Type.U2:
                case BaseType_Type.U4:
                case BaseType_Type.Void:
                    return Assembler.CliType.int32;
                case BaseType_Type.Class:
                case BaseType_Type.GenericInst:
                case BaseType_Type.String:
                case BaseType_Type.ValueType:
                    return Assembler.CliType.O;
                case BaseType_Type.Byref:
                case BaseType_Type.Ptr:
                case BaseType_Type.TypedByRef:
                    return Assembler.CliType.reference;
                case BaseType_Type.R4:
                    return Assembler.CliType.F32;
                case BaseType_Type.R8:
                    return Assembler.CliType.F64;
                case BaseType_Type.I:
                case BaseType_Type.U:
                    return Assembler.CliType.native_int;
                case BaseType_Type.I8:
                case BaseType_Type.U8:
                    return Assembler.CliType.int64;
                default:
                    throw new Exception("Unknown element type");
            }   
        }

        public static int GetSize(Assembler.CliType ctype)
        {
            switch (ctype)
            {
                case Assembler.CliType.reference:
                case Assembler.CliType.O:
                case Assembler.CliType.native_int:
                case Assembler.CliType.int64:
                case Assembler.CliType.F64:
                    return 8;
                case Assembler.CliType.int32:
                case Assembler.CliType.F32:
                    return 4;
                default:
                    throw new Exception("CliType not recognised");
            }
        }

        public static byte[] CompressInteger(uint val)
        {
            /* compression rules are:
             *  1) if between 0x00 and 0x7f, encode as 1 byte
             *  2) if between 0x80 and 0x3fff, encode as 2 byte or with 0x8000
             *  3) else encode as 4 byte or with 0xc0000000
             *  write out in msb format */

            if (val <= 0x7f)
                return MSB_Assembler.SToByteArray((byte)val);
            else if (val <= 0x3fff)
                return MSB_Assembler.SToByteArray((ushort)(val | 0x8000));
            else
                return MSB_Assembler.SToByteArray((uint)(val | 0xc0000000));
        }

        public static int ReadCompressedInteger(byte[] sig, int offset)
        { int x = offset; return ReadCompressedInteger(sig, ref x); }

        public static int ReadCompressedInteger(byte[] sig, ref int offset)
        {
            if ((sig[offset] & 0x80) == 0)
            {
                // 1 byte integer
                offset = offset + 1;
                return (int)sig[offset - 1];
            }
            else
            {
                if ((sig[offset] & 0x40) == 0)
                {
                    // 2 byte integer, value in bits 0-13
                    offset = offset + 2;
                    return (int)sig[offset - 1] + (((int)(sig[offset - 2] & 0x3f)) << 8);
                }
                else
                {
                    // 4 byte integer, value in bits 0-28
                    offset = offset + 4;
                    return (int)sig[offset - 1] + (((int)sig[offset - 2]) << 8) +
                        (((int)sig[offset - 3]) << 16) + (((int)(sig[offset - 4] & 0x1f)) << 24);
                }
            }
        }

        public byte[] BlobHeap;
        public byte[] StringHeap;
        public byte[] GUIDHeap;
        public byte[] USHeap;

        public String VersionString;

        public StringTable StringTable = null;

        public enum TableId
        {
            NotUsed = -1,
            Assembly = 0x20,
            AssemblyOS = 0x22,
            AssemblyProcessor = 0x21,
            AssemblyRef = 0x23,
            AssemblyRefOS = 0x25,
            AssemblyRefProcessor = 0x24,
            ClassLayout = 0x0f,
            Constant = 0x0b,
            CustomAttribute = 0x0c,
            DeclSecurity = 0x0e,
            EventMap = 0x12,
            Event = 0x14,
            ExportedType = 0x27,
            Field = 0x04,
            FieldLayout = 0x10,
            FieldMarshal = 0x0d,
            FieldRVA = 0x1d,
            File = 0x26,
            GenericParam = 0x2a,
            GenericParamConstaint = 0x2c,
            ImplMap = 0x1c,
            InterfaceImpl = 0x09,
            ManifestResource = 0x28,
            MemberRef = 0x0a,
            MethodDef = 0x06,
            MethodImpl = 0x19,
            MethodSemantics = 0x18,
            MethodSpec = 0x2b,
            Module = 0x00,
            ModuleRef = 0x1a,
            NestedClass = 0x29,
            Param = 0x08,
            Property = 0x17,
            PropertyMap = 0x15,
            StandAloneSig = 0x11,
            TypeDef = 0x02,
            TypeRef = 0x01,
            TypeSpec = 0x1b
        }

        public struct TableIndex
        {
            public ITableRow[] Table;
            public int Index;
            Metadata _m;

            ITableRow _val;

            public Metadata Metadata { get { return _m; } }

            public ITableRow Value { get { if (_val != null) return _val; if (Index == 0) return null; else return Table[Index - 1]; } }
            public TableId TableId { get { return (TableId)Value.TableId(); } }

            public TableIndex(Token t) : this(t.Metadata, t) { }
            public TableIndex(Metadata m) { _m = m; Table = null; Index = 0; _val = null; }
            public TableIndex(Metadata m, int index, ITableRow[] table) { _m = m; Table = table; Index = index; _val = null; }
            public TableIndex(Metadata m, int coded_index, int tagbits, TableId[] tables)
            {
                Index = (int)(coded_index >> tagbits);
                Table = m.Tables[(int)tables[(int)(coded_index & (~(0xffffffff << tagbits)))]];
                _m = m;
                _val = null;
            }
            public TableIndex(Metadata m, ITableRow trow)
            { _m = m; Table = m.Tables[trow.TableId()]; Index = trow.GetRowNumber(); _val = trow; }
            public TableIndex(ITableRow trow) : this(trow.GetMetadata(), trow) { }
            public TableIndex(Metadata m, Token t)
            {
                Index = t.Value.GetRowNumber();
                Table = m.Tables[t.Value.TableId()];
                _m = m;
                _val = t.Value;
            }
            public TableIndex(TableIndex t)
            {
                Index = t.Index;
                Table = t.Table;
                _m = t._m;
                _val = null;
            }

            public static TableId[] TypeDefOrRef { get { return new TableId[] { TableId.TypeDef, TableId.TypeRef, TableId.TypeSpec }; } }
            public static TableId[] HasConstant { get { return new TableId[] { TableId.Field, TableId.Param, TableId.Property }; } }
            public static TableId[] HasCustomAttribute
            {
                get
                {
                    return new TableId[] { TableId.MethodDef, TableId.Field, TableId.TypeRef, TableId.TypeDef, TableId.Param, TableId.InterfaceImpl, TableId.MemberRef, TableId.Module, TableId.Property,
                TableId.Event, TableId.StandAloneSig, TableId.ModuleRef, TableId.TypeSpec, TableId.Assembly, TableId.AssemblyRef, TableId.File, TableId.ExportedType, TableId.ManifestResource };
                }
            }
            
            public int ToCodedIndex(TableId[] tables)
            {
                int _maxrows = 0;
                int n = tables.Length;
                int tagbits = Convert.ToInt32(Math.Log(Convert.ToDouble(n), 2.0));
                int rowno = -1;

                for (int i = 0; i < tables.Length; i++)
                {
                    if (_m.Tables[(int)tables[i]].Length > _maxrows)
                        _maxrows = _m.Tables[(int)tables[i]].Length;
                    if (tables[i] == TableId)
                        rowno = i;
                }
                if (rowno == -1)
                    throw new Exception("tables[] does not contain current TableId");

                int ret = (Index << tagbits) | rowno;
                return ret;
            }

            public Token ToToken(Metadata m)
            {
                return new Token(Index, Value.TableId(), m);
            }
            public Token ToToken()
            {
                return ToToken(_m);
            }
            public uint ToTokenUInt32()
            {
                return new Token(Index, Value.TableId(), _m).ToUInt32();
            }

            public TableIndex Copy()
            {
                return new TableIndex(_m, Index, Table);
            }

            public static int operator -(TableIndex lhs, TableIndex rhs)
            {
                if (lhs.Table != rhs.Table)
                    throw new ArgumentException();
                return lhs.Index - rhs.Index;
            }

            public static TableIndex operator +(TableIndex lhs, int i)
            {
                TableIndex ret = new TableIndex(lhs);
                lhs.Index += i;
                return lhs;
            }
            public static TableIndex operator -(TableIndex lhs, int i)
            {
                TableIndex ret = new TableIndex(lhs);
                lhs.Index -= i;
                return lhs;
            }


            public static TableIndex operator ++(TableIndex rhs)
            {
                rhs.Index++;
                return rhs;
            }

            public static TableIndex operator --(TableIndex rhs)
            {
                rhs.Index--;
                return rhs;
            }

            public static bool operator <=(TableIndex lhs, TableIndex rhs)
            {
                if ((lhs.Table == rhs.Table) && (lhs.Index <= rhs.Index)) return true; else return false;
            }

            public static bool operator >=(TableIndex lhs, TableIndex rhs)
            {
                if ((lhs.Table == rhs.Table) && (lhs.Index >= rhs.Index)) return true; else return false;
            }

            public static bool operator <(TableIndex lhs, TableIndex rhs)
            {
                if ((lhs.Table == rhs.Table) && (lhs.Index < rhs.Index)) return true; else return false;
            }

            public static bool operator >(TableIndex lhs, TableIndex rhs)
            {
                if ((lhs.Table == rhs.Table) && (lhs.Index > rhs.Index)) return true; else return false;
            }

            public static bool operator ==(TableIndex lhs, TableIndex rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(TableIndex lhs, TableIndex rhs)
            {
                if (lhs.Equals(rhs)) return false; else return true;
            }

            public override bool Equals(object obj)
            {
                if(obj is TableIndex) {
                    TableIndex rhs = (TableIndex)obj;
                    if ((this.Table == rhs.Table) && (this.Index == rhs.Index)) return true; else return false;
                }
                return false;
            }

            public override string ToString()
            {
                return Value.ToString();
            }

            public override int GetHashCode()
            {
                return (int)this.TableId & 0xff | (this.Index << 8);
            }
        }

        #region Table rows
        public interface ITableRow
        {
            int TableId();
            int GetRowNumber();
            Metadata GetMetadata();
        }

        public class TableRow
        {
            public Metadata m;
            public Assembler ass;
            public int RowNumber;
            public int GetRowNumber() { return RowNumber; }
            public Metadata GetMetadata() { return m; }

            public List<CustomAttributeRow> CustomAttrs = new List<CustomAttributeRow>();
        }

        public class AssemblyRow : TableRow, ITableRow
        {
            public enum AssemblyHashAlgorithm { None = 0x0, MD5 = 0x8003, SHA1 = 0x8004 }

            public int TableId() { return 0x20; }

            public AssemblyHashAlgorithm HashAlgId;
            public Version Version;
            public UInt32 Flags;

            public byte[] PublicKey;
            public String Name;
            public String Culture;
        }

        public class AssemblyRefRow : TableRow, ITableRow
        {
            public int TableId() { return 0x23; }

            public Version Version;
            public UInt32 Flags;
            public byte[] PublicKeyOrToken;
            public String Name;
            public String Culture;
            public byte[] HashValue;
        }

        public class ClassLayoutRow : TableRow, ITableRow
        {
            public int TableId() { return 0x0f; }

            public UInt16 PackingSize;
            public UInt32 ClassSize;
            public TableIndex Parent;
        }

        public class ConstantRow : TableRow, ITableRow
        {
            public int TableId() { return 0x0b; }

            public BaseType_Type Type;
            public TableIndex Parent;
            public byte[] Value;
        }

        public class CustomAttributeRow : TableRow, ITableRow
        {
            public int TableId() { return 0x0c; }

            public TableIndex Parent;
            public TableIndex Type;
            public byte[] Value;
        }

        public class DeclSecurityRow : TableRow, ITableRow
        {
            public int TableId() { return 0x0e; }

            public UInt16 Action;
            public TableIndex Parent;
            public byte[] PermissionSet;
        }

        public class EventMapRow : TableRow, ITableRow
        {
            public int TableId() { return 0x12; }

            public TableIndex Parent;
            public TableIndex EventList;
        }

        public class EventRow : TableRow, ITableRow
        {
            public int TableId() { return 0x14; }

            public UInt16 EventFlags;
            public String Name;
            public TableIndex EventType;
        }

        public class ExportedTypeRow : TableRow, ITableRow
        {
            public int TableId() { return 0x27; }

            public UInt32 Flags;
            public UInt32 TypeDefId;
            public String TypeName;
            public String TypeNamespace;
            public TableIndex Implementation;
        }

        public class FieldRow : TableRow, ITableRow
        {
            public int TableId() { return 0x04; }

            public UInt16 Flags;
            public String Name;
            public byte[] Signature;

            public FieldRVARow RVA;
            internal byte[] LiteralData;
            public ConstantRow Constant = null;

            internal bool RuntimeInternal;

            public Signature.Field fsig;

            public Signature.Field GetSignature()
            {
                if (fsig != null)
                    return fsig;
                return libtysila.Signature.ParseFieldSig(this.m, Signature, ass);
            }

            public TypeDefRow owning_type;

            public bool IsStatic { get { if ((Flags & 0x10) == 0x10) return true; return false; } }
        }

        public class FieldLayoutRow : TableRow, ITableRow
        {
            public int TableId() { return 0x10; }

            public UInt32 Offset;
            public TableIndex Field;
        }

        public class FieldMarshalRow : TableRow, ITableRow
        {
            public int TableId() { return 0x0d; }

            public TableIndex Parent;
            public byte[] NativeType;
        }

        public class FieldRVARow : TableRow, ITableRow
        {
            public int TableId() { return 0x1d; }

            public UInt32 RVA;
            public TableIndex Field;
        }

        public class FileRow : TableRow, ITableRow
        {
            public int TableId() { return 0x26; }

            public UInt32 Flags;
            public String Name;
            public byte[] HashValue;
        }

        public class GenericParamRow : TableRow, ITableRow
        {
            public int TableId() { return 0x2a; }

            public UInt16 Number;
            public UInt16 Flags;
            public TableIndex Owner;
            public String Name;
        }

        public class GenericParamConstraintRow : TableRow, ITableRow
        {
            public int TableId() { return 0x2c; }

            public TableIndex Owner;
            public TableIndex Constraint;
        }

        public class ImplMapRow : TableRow, ITableRow
        {
            public int TableId() { return 0x1c; }

            public UInt16 MappingFlags;
            public TableIndex MemberForwarded;
            public String ImportName;
            public TableIndex ImportScope;
        }

        public class InterfaceImplRow : TableRow, ITableRow
        {
            public int TableId() { return 0x09; }

            public TableIndex Class;
            public TableIndex Interface;
        }

        public class ManifestResourceRow : TableRow, ITableRow
        {
            public int TableId() { return 0x28; }

            public UInt32 Offset;
            public UInt32 Flags;
            public String Name;
            public TableIndex Implementation;
        }

        public class MemberRefRow : TableRow, ITableRow
        {
            public int TableId() { return 0x0a; }

            public TableIndex Class;
            public String Name;
            public byte[] Signature;
        }

        public class MethodDefRow : TableRow, ITableRow
        {
            public int TableId() { return 0x06; }

            public UInt32 RVA;
            public UInt16 ImplFlags;
            public UInt16 Flags;
            public String Name;
            public byte[] Signature;
            public Signature.BaseMethod msig;
            public TableIndex ParamList;

            public TypeDefRow owning_type;

            public bool IsEntryPoint = false;

            public bool ExcludedByArch = false;

            public MethodBody Body = new MethodBody();
            internal frontend.cil.CilGraph instrs = null;

            public string ReferenceAlias = null;
            public string CallConvOverride = null;
            public bool WeakLinkage = false;

            public Signature.LocalVars GetLocalVars(Assembler ass)
            {
                if (Body.LVars != null)
                    return Body.LVars;
                else
                    return libtysila.Signature.ParseLocalVarsSig(m, Body.LocalVars, ass);
            }

            public List<ParamRow> GetParamNames()
            {
                List<ParamRow> prs = new List<ParamRow>();

                for (TableIndex ti = ParamList; ti < Metadata.GetLastParam(m, this); ti++)
                {
                    ParamRow pr = ti.Value as ParamRow;
                    prs.Add(pr);
                }

                return prs;
            }

            public bool IsStatic { get { if ((Flags & 0x10) == 0x10) return true; return false; } }
            public bool IsFinal { get { if ((Flags & 0x20) == 0x20) return true; return false; } }
            public bool IsVirtual { get { if ((Flags & 0x40) == 0x40) return true; return false; } }
            public bool IsHideBySig { get { if ((Flags & 0x80) == 0x80) return true; return false; } }
            public bool IsNewSlot { get { if ((Flags & 0x100) == 0x100) return true; return false; } }
            public bool IsStrict { get { if ((Flags & 0x200) == 0x200) return true; return false; } }
            public bool IsAbstract { get { if ((Flags & 0x400) == 0x400) return true; return false; } }
            public bool IsSpecialName { get { if ((Flags & 0x800) == 0x800) return true; return false; } }
            public bool IsPinvokeImpl { get { if ((Flags & 0x2000) == 0x2000) return true; return false; } }
            public bool IsInternalCall { get { if ((ImplFlags & 0x1000) == 0x1000) return true; return false; } }

            public bool IgnoreAttribute = false;

            internal Signature.BaseMethod ActualSignature { get { return GetSignature(); } }

            public Signature.BaseMethod GetSignature()
            {
                if (msig != null)
                    return msig;
                return libtysila.Signature.ParseMethodDefSig(this.m, Signature, ass);
            }

            public override string ToString()
            {
                return Name;
            }
        }

        public class MethodBody
        {
            public UInt32 CodeRVA;
            public UInt32 CodeLength;
            public UInt32 MaxStack;
            public bool InitLocals;
            public byte[] LocalVars;
            public Signature.LocalVars LVars = null;

            public byte[] Body;

            public class EHClause
            {
                public uint Flags;
                public uint TryOffset;
                public uint TryLength;
                public uint HandlerOffset;
                public uint HandlerLength;
                public Token ClassToken;
                public uint FilterOffset;

                public int BlockId = -1;

                public bool IsCatch { get { if (Flags == 0x0) return true; return false; } }
                public bool IsFinally { get { if (Flags == 0x2) return true; return false; } }
                public bool IsFilter { get { if (Flags == 0x1) return true; return false; } }
                public bool IsFault { get { if (Flags == 0x4) return true; return false; } }
            }

            public List<EHClause> exceptions = new List<EHClause>();
        }

        public class MethodImplRow : TableRow, ITableRow
        {
            public int TableId() { return 0x19; }

            public TableIndex Class;
            public TableIndex MethodBody;
            public TableIndex MethodDeclaration;
        }

        public class MethodSemanticsRow : TableRow, ITableRow
        {
            public int TableId() { return 0x18; }

            public UInt16 Semantics;
            public TableIndex Method;
            public TableIndex Association;
        }

        public class MethodSpecRow : TableRow, ITableRow
        {
            public int TableId() { return 0x2b; }

            public TableIndex Method;
            public byte[] Instantiation;
        }

        public class ModuleRow : TableRow, ITableRow
        {
            public int TableId() { return 0x00; }

            public UInt16 Generation;
            public String Name;
            public Guid Mvid;
            public Guid EncId;
            public Guid EncBaseId;
        }

        public class ModuleRefRow : TableRow, ITableRow
        {
            public int TableId() { return 0x1a; }

            public String Name;
        }

        public class NestedClassRow : TableRow, ITableRow
        {
            public int TableId() { return 0x29; }

            public TableIndex NestedClass;
            public TableIndex EnclosingClass;
        }

        public class ParamRow : TableRow, ITableRow
        {
            public int TableId() { return 0x08; }

            public UInt16 Flags;
            public UInt16 Sequence;
            public String Name;
            public ConstantRow Constant = null;
        }

        public class PropertyRow : TableRow, ITableRow
        {
            public int TableId() { return 0x17; }

            public UInt16 Flags;
            public String Name;
            public byte[] Type;
            public ConstantRow Constant = null;
        }

        public class PropertyMapRow : TableRow, ITableRow
        {
            public int TableId() { return 0x15; }

            public TableIndex Parent;
            public TableIndex PropertyList;
        }

        public class UserStringHeapItem : TableRow, ITableRow
        {
            public int TableId() { return 0x70; }

            public string Value
            {
                get
                {
                    if (_msg != null)
                        return _msg;
                    return Encoding.Unicode.GetString(ByteString);
                }
            }
            public byte[] ByteString;
            public string _msg;

            public UserStringHeapItem(string msg) { _msg = msg; }
            public UserStringHeapItem() { _msg = null; }
        }

        public class StandAloneSigRow : TableRow, ITableRow
        {
            public int TableId() { return 0x11; }

            public byte[] Signature;
        }

        public class TypeDefRow : TableRow, ITableRow
        {
            public int TableId() { return 0x02; }

            public UInt32 Flags;

            public bool ExcludedByArch = false;

            internal string _ActualTypeName;
            internal string _ActualTypeNamespace;

            internal TypeDefRow _NestedParent;
            internal List<TypeDefRow> _NestedChildren = new List<TypeDefRow>();

            internal List<InterfaceImplRow> _InterfaceImpls = new List<InterfaceImplRow>();

            internal ClassLayoutRow Layout;

            public String TypeName
            {
                get
                {
                    if (_NestedParent == null)
                        return _ActualTypeName;
                    else
                        return _NestedParent.TypeName + "+" + _ActualTypeName;
                }
            }
            public String TypeNamespace
            {
                get
                {
                    if (_NestedParent == null)
                        return _ActualTypeNamespace;
                    else
                        return _NestedParent.TypeNamespace;
                }
            }
            public TableIndex Extends;
            public TableIndex FieldList;
            public TableIndex MethodList;
            public List<MethodDefRow> Methods = new List<MethodDefRow>();
            public List<FieldRow> Fields = new List<FieldRow>();
            public List<MethodImplRow> MethodImpls = new List<MethodImplRow>();
            public String TypeFullName { get { return TypeNamespace + "." + TypeName; } }

            List<FieldRow> _all_instance_fields = null;
            List<MethodDefRow> _all_virtual_methods = null;

            public bool IsAbstract { get { if ((Flags & 0x80) == 0x80) return true; return false; } }
            public bool IsInterface { get { if ((Flags & 0x20) == 0x20) return true; return false; } }
            public bool IsAutoLayout { get { if ((Flags & 0x18) == 0x00) return true; return false; } }
            public bool IsSequentialLayout { get { if ((Flags & 0x08) == 0x08) return true; return false; } }
            public bool IsExplicitLayout { get { if ((Flags & 0x10) == 0x10) return true; return false; } }
            public bool IsBeforeFieldInit { get { if ((Flags & 0x100000) == 0x100000) return true; return false; } }

            bool? _is_valuetype = null;
            bool? _is_enum = null;
            bool? _is_delegate = null;

            // Store some special types for fast access
            static TypeDefRow valuetype_row = null;
            static TypeDefRow enum_row = null;
            static TypeDefRow delegate_row = null;
            static TypeDefRow object_row = null;
            static TypeDefRow nullable_row = null;

            internal static TypeDefRow GetSystemDelegate(Assembler ass)
            {
                if (delegate_row == null)
                    delegate_row = Metadata.GetTypeDef("mscorlib", "System", "Delegate", ass);
                if (delegate_row == null)
                    throw new Exception("System.Delegate not found");
                return delegate_row;
            }

            internal static TypeDefRow GetSystemNullable(Assembler ass)
            {
                if (nullable_row == null)
                    nullable_row = Metadata.GetTypeDef("mscorlib", "System", "Nullable`1", ass);
                if (nullable_row == null)
                    throw new Exception("System.Nullable`1 not found");
                return nullable_row;
            }

            internal static TypeDefRow GetSystemEnum(Assembler ass)
            {
                if (enum_row == null)
                    enum_row = Metadata.GetTypeDef("mscorlib", "System", "Enum", ass);
                if (enum_row == null)
                    throw new Exception("System.Enum not found");
                return enum_row;
            }

            internal static TypeDefRow GetSystemObject(Assembler ass)
            {
                if (object_row == null)
                    object_row = Metadata.GetTypeDef("mscorlib", "System", "Object", ass);
                if (object_row == null)
                    throw new Exception("System.Object not found");
                return object_row;
            }

            internal static TypeDefRow GetSystemValueType(Assembler ass)
            {
                if (valuetype_row == null)
                    valuetype_row = Metadata.GetTypeDef("mscorlib", "System", "ValueType", ass);
                if (valuetype_row == null)
                    throw new Exception("System.ValueType not found");
                return valuetype_row;
            }

            public bool IsNullable
            {
                get
                {
                    return this == nullable_row;
                }
            }

            public bool IsValueType(Assembler ass)
            {
                if (_is_valuetype.HasValue)
                    return _is_valuetype.Value;

                if (valuetype_row == null)
                    valuetype_row = Metadata.GetTypeDef("mscorlib", "System", "ValueType", ass);
                if (valuetype_row == null)
                    throw new Exception("System.ValueType not found");
                if (enum_row == null)
                    enum_row = GetSystemEnum(ass);

                /* If this is System.Enum, it is not a value type
                 * In addition, if we do not extend anything, we cannot be a value type
                 */
                if ((this == GetSystemEnum(ass)) || (Extends.Value == null))
                {
                    _is_valuetype = false;
                    return false;
                }

                /* If we extend System.ValueType or System.Enum (and are not System.Enum itself - checked above)
                 * then we are a valuetype
                 */
                TypeDefRow extends = GetTypeDef(Extends.ToToken(), ass);
                if ((extends == valuetype_row) || (extends == enum_row))
                {
                    _is_valuetype = true;
                    return true;
                }

                _is_valuetype = extends.IsValueType(ass);
                return _is_valuetype.Value;
            }

            public bool IsEnum(Assembler ass)
            {
                if (_is_enum.HasValue)
                    return _is_enum.Value;

                if(enum_row == null)
                    enum_row = Metadata.GetTypeDef("mscorlib", "System", "Enum", ass);
                if(enum_row == null)
                    throw new Exception("System.Enum not found");

                if(this == enum_row)
                {
                    _is_enum = false;
                    return false;
                }

                TableIndex extends = Extends;
                while (extends.Value != null)
                {
                    TypeDefRow extends_tdr = GetTypeDef(extends.ToToken(), ass);
                    if (extends_tdr == enum_row)
                    {
                        _is_enum = true;
                        return true;
                    }

                    extends = extends_tdr.Extends;
                }

                _is_enum = false;
                return false;
            }

            public bool IsDelegate(Assembler ass)
            {
                if (_is_delegate.HasValue)
                    return _is_delegate.Value;

                if (delegate_row == null)
                    delegate_row = Metadata.GetTypeDef("mscorlib", "System", "Delegate", ass);
                if (delegate_row == null)
                    throw new Exception("System.Delegate not found");

                if (this == delegate_row)
                {
                    _is_delegate = true;
                    return true;
                }

                if (Extends.Value == null)
                {
                    _is_delegate = false;
                    return false;
                }

                _is_delegate = GetTypeDef(Extends.ToToken(), ass).IsDelegate(ass);
                return _is_delegate.Value;
            }

            public List<FieldRow> GetAllInstanceFields(Assembler ass)
            {
                if (_all_instance_fields != null)
                    return _all_instance_fields;

                List<FieldRow> ret = new List<FieldRow>();

                // If this is a value type, do not inherit from System.Object
                if (!IsValueType(ass))
                {
                    if (Extends.Value != null)
                        ret.AddRange(GetTypeDef(Extends.ToToken(), ass).GetAllInstanceFields(ass));
                }

                foreach (FieldRow fr in Fields)
                {
                    if (!fr.IsStatic)
                        ret.Add(fr);
                }

                // Add some special fields for System.Object
                if (this == GetSystemObject(ass))
                {
                    /* Add an Int32 __object_id field */
                    ret.Add(new FieldRow { Flags = 0x601, fsig = new Signature.Field(new Signature.Param(BaseType_Type.I4)), m = this.m, Name = "__object_id", owning_type = this, RuntimeInternal = true });

                    /* Add an Int64 __mutex_lock field */
                    ret.Add(new FieldRow { Flags = 0x601, fsig = new Signature.Field(new Signature.Param(BaseType_Type.I8)), m = this.m, Name = "__mutex_lock", owning_type = this, RuntimeInternal = true });
                }

                _all_instance_fields = ret;
                return ret;
            }

            public List<MethodDefRow> GetAllVirtualMethods(Assembler ass)
            {
                if (_all_virtual_methods != null)
                    return _all_virtual_methods;

                List<MethodDefRow> ret = new List<MethodDefRow>();
                List<MethodDefRow> base_methods = null;

                if (Extends.Value != null)
                {
                    base_methods = GetTypeDef(Extends.ToToken(), ass).GetAllVirtualMethods(ass);
                    ret.AddRange(base_methods);
                }

                foreach (MethodDefRow mdr in Methods)
                {
                    if (mdr.IsVirtual)
                    {
                        if (mdr.IsNewSlot)
                            ret.Add(mdr);
                        else
                        {
                            // Only add if there is not already a method with the same name and signature
                            bool can_add = true;
                            Signature.BaseMethod msig = mdr.GetSignature();

                            if (base_methods != null)
                            {
                                foreach (MethodDefRow base_mdr in base_methods)
                                {
                                    if (mdr.Name == base_mdr.Name)
                                    {
                                        if (Signature.BaseMethodSigCompare(msig, base_mdr.GetSignature(), ass))
                                        {
                                            can_add = false;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (can_add)
                                ret.Add(mdr);
                        }
                    }
                }

                _all_virtual_methods = ret;
                return ret;
            }

            public override string ToString()
            {
                return "TypeDefRow: " + this.TypeFullName;
            }

            public bool IsGeneric {
                get
                {
                    foreach (Metadata.GenericParamRow gpr in m.Tables[(int)Metadata.TableId.GenericParam])
                    {
                        if (gpr.Owner.Value == this)
                            return true;
                    }
                    return false;
                }
            }

            public bool IsNested { get { if (_NestedParent == null) return false; return true; } }
        }

        public class TypeRefRow : TableRow, ITableRow
        {
            public int TableId() { return 0x01; }

            public TableIndex ResolutionScope;
            public String TypeName;
            public String TypeNamespace;
            public String TypeFullName { get { return TypeNamespace + "." + TypeName; } }
        }

        public class TypeSpecRow : TableRow, ITableRow
        {
            public int TableId() { return 0x1b; }

            public byte[] Signature;
        }

        #endregion

        private ITableRow[] tab_templ = new ITableRow[] {
            new AssemblyRow(),
            new AssemblyRefRow(),
            new ClassLayoutRow(),
            new ConstantRow(),
            new CustomAttributeRow(),
            new DeclSecurityRow(),
            new EventMapRow(),
            new EventRow(),
            new ExportedTypeRow(),
            new FieldRow(),
            new FieldLayoutRow(),
            new FieldMarshalRow(),
            new FieldRVARow(),
            new FileRow(),
            new GenericParamRow(),
            new GenericParamConstraintRow(),
            new ImplMapRow(),
            new InterfaceImplRow(),
            new ManifestResourceRow(),
            new MemberRefRow(),
            new MethodDefRow(),
            new MethodImplRow(),
            new MethodSemanticsRow(),
            new MethodSpecRow(),
            new ModuleRow(),
            new ModuleRefRow(),
            new NestedClassRow(),
            new ParamRow(),
            new PropertyRow(),
            new PropertyMapRow(),
            new StandAloneSigRow(),
            new TypeDefRow(),
            new TypeRefRow(),
            new TypeSpecRow()
        };

        public ITableRow CreateTableRow(int table_id)
        {
            for (int i = 0; i < tab_templ.Length; i++)
            {
                if (tab_templ[i].TableId() == table_id)
                    return (ITableRow)tab_templ[i].GetType().InvokeMember(
                        null,
                        System.Reflection.BindingFlags.CreateInstance,
                        null,
                        null,
                        null);
            }
            return null;
        }

        public ITableRow[] CreateTableRowArray(int table_id, int length)
        {
            for (int i = 0; i < tab_templ.Length; i++)
            {
                if (tab_templ[i].TableId() == table_id)
                    return (ITableRow[])tab_templ[i].GetType().MakeArrayType().InvokeMember(
                        null,
                        System.Reflection.BindingFlags.CreateInstance,
                        null,
                        null,
                        new object[] { length });
            }
            return null;
        }

        // The metadata tables
        public ITableRow[][] Tables = new ITableRow[64][];

        public int GetMaxIndex(TableId[] tables) {
            int max = 0;
            foreach(int i in tables) {
                if (i >= 0)
                {
                    if (Tables[i].Length > max)
                        max = Tables[i].Length;
                }
            }
            return max;
        }

        public static TableIndex GetLastField(Metadata m, TypeDefRow td)
        {
            int cur_rowno = td.RowNumber - 1;

            if (cur_rowno >= (m.Tables[(int)TableId.TypeDef].Length - 1))
                return new TableIndex(m, m.Tables[(int)TableId.Field].Length + 1, m.Tables[(int)TableId.Field]);
            else
                return ((TypeDefRow)m.Tables[(int)TableId.TypeDef][cur_rowno + 1]).FieldList;
        }

        public static TableIndex GetLastParam(Metadata m, MethodDefRow md)
        {
            int cur_rowno = md.RowNumber - 1;

            if(cur_rowno >= (m.Tables[(int)TableId.MethodDef].Length - 1))
                return new TableIndex(m, m.Tables[(int)TableId.Param].Length + 1, m.Tables[(int)TableId.Param]);
            else
                return ((MethodDefRow)m.Tables[(int)TableId.MethodDef][cur_rowno + 1]).ParamList;
        }

        public static TableIndex GetLastMethod(TypeDefRow tdr) { return GetLastMethod(tdr.m, tdr); }
        public static TableIndex GetLastMethod(Metadata m, TypeDefRow td)
        {
            int cur_rowno = td.RowNumber - 1;

            if (cur_rowno >= (m.Tables[(int)TableId.TypeDef].Length - 1))
                return new TableIndex(m, m.Tables[(int)TableId.MethodDef].Length + 1, m.Tables[(int)TableId.MethodDef]);
            else
                return ((TypeDefRow)m.Tables[(int)TableId.TypeDef][cur_rowno + 1]).MethodList;
        }

        public static TypeDefRow GetOwningType(Metadata m, FieldRow f)
        {
            if (f == null)
                throw new ArgumentNullException("f");
            if (m == null)
                throw new ArgumentNullException("m");

            if (f.owning_type != null)
                return f.owning_type;

            TableIndex curf = new TableIndex(m, f.RowNumber, m.Tables[(int)TableId.Field]);
            for (int i = 0; i < m.Tables[(int)TableId.TypeDef].Length; i++)
            {
                TypeDefRow curt = m.Tables[(int)TableId.TypeDef][i] as TypeDefRow;

                if ((curf >= curt.FieldList) && (curf < GetLastField(m, curt)))
                    return curt;
            }
            return null;
        }

        public static TypeDefRow GetOwningType(Metadata m, MethodDefRow meth)
        {
            if (meth.m != m)
                throw new Exception();

            if (meth.owning_type != null)
                return meth.owning_type;            

            TableIndex curm = new TableIndex(m, meth.RowNumber, m.Tables[(int)TableId.MethodDef]);
            for (int i = 0; i < m.Tables[(int)TableId.TypeDef].Length; i++)
            {
                TypeDefRow curt = m.Tables[(int)TableId.TypeDef][i] as TypeDefRow;

                if ((curm >= curt.MethodList) && (curm < GetLastMethod(m, curt)))
                    return curt;
            }
            return null;
        }

        public static TypeDefRow GetOwningType(Metadata m, MemberRefRow meth, Assembler ass)
        {
            TypeDefRow tdr = GetTypeDef(meth.Class.ToToken(), ass);

            return tdr;
        }

        public static TypeDefRow GetOwningType(Metadata m, ITableRow meth, Assembler ass)
        {
            if (meth is MemberRefRow)
                return GetOwningType(m, (MemberRefRow)meth, ass);
            else if (meth is MethodDefRow)
                return GetOwningType(m, (MethodDefRow)meth);
            else if (meth is FieldRow)
                return GetOwningType(m, (FieldRow)meth, ass);
            throw new NotSupportedException();
        }

        public static TableIndex GetLastEvent(Metadata m, EventMapRow em)
        {
            int cur_rowno = em.RowNumber - 1;

            if (cur_rowno >= (m.Tables[(int)TableId.EventMap].Length - 1))
                return new TableIndex(m, m.Tables[(int)TableId.Event].Length + 1, m.Tables[(int)TableId.Event]);
            else
                return ((EventMapRow)m.Tables[(int)TableId.EventMap][cur_rowno + 1]).EventList;
        }

        public static TableIndex GetLastEvent(Metadata m, TypeDefRow td)
        {
            foreach (EventMapRow em in m.Tables[(int)TableId.EventMap])
            {
                if (em.Parent.Value == td)
                    return GetLastEvent(m, em);
            }
            return new TableIndex(m);
        }

        public static TableIndex GetFirstEvent(Metadata m, TypeDefRow td)
        {
            foreach (EventMapRow em in m.Tables[(int)TableId.EventMap])
            {
                if (em.Parent.Value == td)
                    return em.EventList;
            }
            return new TableIndex(m);
        }

        public static TableIndex GetFirstProperty(Metadata m, TypeDefRow td)
        {
            foreach (PropertyMapRow pm in m.Tables[(int)TableId.PropertyMap])
            {
                if (pm.Parent.Value == td)
                    return pm.PropertyList;
            }
            return new TableIndex(m);
        }

        public static TableIndex GetLastProperty(Metadata m, PropertyMapRow pm)
        {
            int cur_rowno = pm.RowNumber - 1;

            if (cur_rowno >= (m.Tables[(int)TableId.PropertyMap].Length - 1))
                return new TableIndex(m, m.Tables[(int)TableId.Property].Length + 1, m.Tables[(int)TableId.Property]);
            else
                return ((PropertyMapRow)m.Tables[(int)TableId.PropertyMap][cur_rowno + 1]).PropertyList;
        }

        public static TableIndex GetLastProperty(Metadata m, TypeDefRow td)
        {
            foreach (PropertyMapRow pm in m.Tables[(int)TableId.PropertyMap])
            {
                if (pm.Parent.Value == td)
                    return GetLastProperty(m, pm);
            }
            return new TableIndex(m);
        }

        public static List<MethodSemanticsRow> GetSemantics(Metadata m, TableIndex ti)
        {
            List<MethodSemanticsRow> ret = new List<MethodSemanticsRow>();

            foreach (MethodSemanticsRow ms in m.Tables[(int)TableId.MethodSemantics])
            {
                if (ms.Association == ti)
                    ret.Add(ms);
            }

            return ret;
        }

        public static List<GenericParamRow> GetGenericParams(Metadata m, TypeDefRow tdr)
        {
            List<GenericParamRow> ret = new List<GenericParamRow>();

            foreach (GenericParamRow gpr in m.Tables[(int)TableId.GenericParam])
            {
                if (gpr.Owner.Value == tdr)
                    ret.Add(gpr);
            }
            return ret;
        }

        public static Metadata.TypeDefRow GetTypeDef(ITableRow trow, Assembler ass)
        {
            if (trow is Metadata.TypeDefRow)
                return trow as Metadata.TypeDefRow;
            if (trow is Metadata.TypeRefRow)
                return ResolveTypeRef(trow as Metadata.TypeRefRow, ass);
            if (trow is Metadata.TypeSpecRow)
                return ResolveTypeSpec(trow as Metadata.TypeSpecRow, ass);
            throw new NotSupportedException();
        }

        public static Metadata.TypeDefRow GetTypeDef(Token reference, Assembler ass)
        { return GetTypeDef(reference.Value, ass); }

        public static Metadata.TypeDefRow GetTypeDef(string Assembly, string TypeNamespace, string TypeName, Assembler ass)
        {
            Metadata mnew = ass.FindAssembly(new AssemblyRefRow { Name = Assembly });
            return GetTypeDef(mnew, TypeNamespace, TypeName, ass);
        }

        public static Metadata.TypeDefRow GetTypeDef(Metadata assembly, string TypeNamespace, string TypeName, Assembler ass)
        {
            foreach (Metadata.TypeDefRow tdr in assembly.Tables[(int)Metadata.TableId.TypeDef])
            {
                if ((tdr.TypeName == TypeName) && (tdr.TypeNamespace == TypeNamespace))
                    return tdr;
            }
            return null;
        }

        public static TypeDefRow GetTypeDef(Signature.BaseOrComplexType bct, Assembler ass) { return GetTypeDef(bct, ass, true); }
        public static Metadata.TypeDefRow GetTypeDef(Signature.BaseOrComplexType bct, Assembler ass, bool request_types)
        {
            if (bct is Signature.BaseType)
            {
                Signature.BaseType bt = bct as Signature.BaseType;
                return ass.GetBasetypeTypedef(bt);
            }
            else if (bct is Signature.ZeroBasedArray)
            {
                Signature.ZeroBasedArray zba = bct as Signature.ZeroBasedArray;
                if (zba.ArrayType == null)
                {
                    Assembler.TypeToCompile arr_ttc = ass.CreateArray(new Signature.Param(zba, ass), 1, 
                        new Assembler.TypeToCompile { type = GetTypeDef(zba.ElemType, ass, request_types), 
                            tsig = new Signature.Param(zba.ElemType, ass), _ass = ass }, false);
                    zba.ArrayType = arr_ttc.type;
                    if(request_types)
                        ass.Requestor.RequestTypeInfo(arr_ttc);
                }
                return zba.ArrayType;
                //return GetTypeDef(new Signature.BaseType(BaseType_Type.Array), ass);
            }
            else if (bct is Signature.ComplexType)
            {
                Signature.ComplexType ct = bct as Signature.ComplexType;
                return GetTypeDef(ct.Type, ass);
            }
            else if (bct is Signature.ComplexArray)
            {
                Signature.ComplexArray ca = bct as Signature.ComplexArray;
                if (ca.ArrayType == null)
                {
                    Assembler.TypeToCompile arr_ttc =
                        ass.CreateArray(new Signature.Param(ca, ass), ca.Rank, 
                        new Assembler.TypeToCompile { type = GetTypeDef(ca.ElemType, ass, request_types), 
                            tsig = new Signature.Param(ca.ElemType, ass), _ass = ass }, false);
                    ca.ArrayType = arr_ttc.type;
                    if(request_types)
                        ass.Requestor.RequestTypeInfo(arr_ttc);
                }
                return ((Signature.ComplexArray)bct).ArrayType;
            }
            else if (bct is Signature.BoxedType)
                return GetTypeDef(((Signature.BoxedType)bct).Type, ass, request_types);
            else if (bct is Signature.GenericType)
                return GetTypeDef(((Signature.GenericType)bct).GenType, ass, request_types);
            else if (bct is Signature.ManagedPointer)
                return GetTypeDef(((Signature.ManagedPointer)bct).ElemType, ass, request_types);
            else if (bct is Signature.UnmanagedPointer)
                return GetTypeDef(((Signature.UnmanagedPointer)bct).BaseType, ass, request_types);
            else
                throw new NotSupportedException();
        }

        public static Metadata.MethodDefRow GetMethodDef(TableIndex reference, Assembler ass)
        {
            if (reference.Value is Metadata.MethodDefRow)
                return reference.Value as Metadata.MethodDefRow;
            if (reference.Value is Metadata.MemberRefRow)
                return ResolveMemberRef(reference.Value as Metadata.MemberRefRow, ass);
            return null;
        }

        public static Metadata.MethodDefRow GetMethodDef(Token reference, Assembler ass)
        {
            if (reference.Value is Metadata.MethodDefRow)
                return reference.Value as Metadata.MethodDefRow;
            if (reference.Value is Metadata.MemberRefRow)
                return ResolveMemberRef(reference.Value as Metadata.MemberRefRow, ass);
            return null;
        }

        public static Metadata.MethodDefRow GetMethodDef(Metadata m, string Name, TypeDefRow tdr, Signature.BaseMethod sig, Assembler ass)
        {
            foreach (Metadata.MethodDefRow mdr in tdr.Methods)
            {
                if (mdr.Name == Name)
                {
                    if (!Signature.BaseMethodSigCompare(mdr.GetSignature(), sig, ass))
                        continue;

                    return mdr;
                }

            }

            throw new Exception("Method not found");
        }

        public static MethodDefRow GetMethodDef(string Name, TypeDefRow tdr, Signature.BaseMethod sig, Signature.BaseOrComplexType parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            foreach (Metadata.MethodDefRow mdr in tdr.Methods)
            {
                if (mdr.Name == Name)
                {
                    if (sig.Method.GenParamCount != mdr.ActualSignature.Method.GenParamCount)
                        continue;

                    Signature.BaseMethod test_sig = Signature.ResolveGenericMember(mdr.ActualSignature, parent, containing_meth, ass);
                    if (!Signature.BaseMethodSigCompare(test_sig, sig.Method, ass))
                    {
                        AddCustomParameters(ref test_sig, mdr, ass);
                        if (!Signature.BaseMethodSigCompare(test_sig, sig.Method, ass))
                            continue;
                    }
                    return mdr;
                }
            }
            throw new Exception("Method not found");
        }

        private static void AddCustomParameters(ref Signature.BaseMethod test_sig, MethodDefRow mdr, Assembler ass)
        {
            foreach (Metadata.CustomAttributeRow car in mdr.CustomAttrs)
            {
                Assembler.MethodToCompile camtc = Metadata.GetMTC(car.Type, new Assembler.TypeToCompile(), null, ass);

                string caname = Mangler2.MangleMethod(camtc, ass);

                if (caname == "_ZX22ExtraArgumentAttributeM_0_7#2Ector_Rv_P3u1tii")
                {
                    if (car.Value[0] != 0x01)
                        throw new NotSupportedException();
                    if (car.Value[1] != 0x00)
                        throw new NotSupportedException();
                    int offset = 2;
                    int arg_no = (int)PEFile.Read32(car.Value, ref offset);
                    int arg_type = (int)PEFile.Read32(car.Value, ref offset);

                    if (!(test_sig is Signature.Method))
                        throw new Exception("ExtraArgumentAttribute is not allowed on generic methods");
                    Signature.Method meth_sig = test_sig.Method;

                    /* Create a new signature for the method for use in the compilation.
                     * 
                     * If we change the signature that is already stored, it will change the signature
                     * in the MethodToCompile object in Assembler.Requestor's dictionaries, without
                     * changing the hashcode internally stored for it.  This will lead to problems
                     * with insertion/removal of MethodToCompiles
                     */

                    Signature.Method new_sig = new Signature.Method
                    {
                        CallingConvention = meth_sig.CallingConvention,
                        ExplicitThis = meth_sig.ExplicitThis,
                        GenParamCount = meth_sig.GenParamCount,
                        HasThis = meth_sig.HasThis,
                        m = meth_sig.m,
                        ParamCount = arg_no + 1,
                        RetType = meth_sig.RetType,
                        Params = new List<Signature.Param>()
                    };
                    foreach (Signature.Param p in meth_sig.Params)
                        new_sig.Params.Add(p);
                    while (new_sig.Params.Count <= arg_no)
                        new_sig.Params.Add(null);
                    new_sig.Params[arg_no] = new Signature.Param((BaseType_Type)arg_type);
                    new_sig.Params[arg_no].name = "extra_" + arg_no.ToString();

                    test_sig = new_sig;
                }
            }
        }

        public static Metadata.FieldRow GetFieldDef(Token reference, Assembler ass)
        {
            if (reference.Value is Metadata.FieldRow)
                return reference.Value as Metadata.FieldRow;
            if (reference.Value is Metadata.MemberRefRow)
                return ResolveFieldRef(reference.Value as Metadata.MemberRefRow, ass);
            throw new NotSupportedException();
        }

        public static Metadata.FieldRow GetFieldDef(string Name, TypeDefRow tdr, Assembler ass)
        {
            foreach (Metadata.FieldRow fr in tdr.Fields)
            {
                if (fr.Name == Name)
                    return fr;
            }
            throw new Exception("Field not found");
        }

        public static ITableRow ResolveRef(ITableRow ref_, Assembler ass)
        {
            if (ref_ is Metadata.TypeDefRow)
                return ref_;
            if (ref_ is Metadata.MethodDefRow)
                return ref_;
            if (ref_ is Metadata.FieldRow)
                return ref_;
            if (ref_ is Metadata.TypeRefRow)
                return ResolveTypeRef(ref_ as Metadata.TypeRefRow, ass);
            if (ref_ is Metadata.TypeSpecRow)
                return ResolveTypeSpec(ref_ as Metadata.TypeSpecRow, ass);
            if (ref_ is Metadata.MemberRefRow)
            {
                if(((Metadata.MemberRefRow)ref_).Signature[0] == 0x6)
                    return ResolveFieldRef(ref_ as Metadata.MemberRefRow, ass);
                else
                    return ResolveMemberRef(ref_ as Metadata.MemberRefRow, ass);
            }
            return null;
        }

        public static FieldRow ResolveFieldRef(MemberRefRow memberRefRow, Assembler ass)
        {
            TypeDefRow tdr = GetTypeDef(memberRefRow.Class.ToToken(), ass);

            for (TableIndex ti = tdr.FieldList; ti < Metadata.GetLastField(tdr.m, tdr); ti++)
            {
                FieldRow mdr = ti.Value as FieldRow;

                if (mdr.Name == memberRefRow.Name)
                {
                    if (memberRefRow.Signature != null)
                    {
                        Signature.Field mrsig = Signature.ParseFieldSig(memberRefRow.m, memberRefRow.Signature, ass);
                        Signature.Field mdsig = Signature.ParseFieldSig(mdr.m, mdr.Signature, ass);

                        if (!Signature.FieldCompare(new Assembler.FieldToCompile { _ass = ass, field = mdr, fsig = mrsig.AsParam(ass) },
                            new Assembler.FieldToCompile { _ass = ass, field = mdr, fsig = mdsig.AsParam(ass) }, ass, false))
                        {
                            continue;
                        }
                    }

                    return mdr;
                }
            }

            return null;
        }

        private static MethodDefRow ResolveMemberRef(MemberRefRow memberRefRow, Assembler ass)
        {
            TypeDefRow tdr = GetTypeDef(memberRefRow.Class.ToToken(), ass);

            for (TableIndex ti = tdr.MethodList; ti < Metadata.GetLastMethod(tdr.m, tdr); ti++)
            {
                MethodDefRow mdr = ti.Value as MethodDefRow;

                if (mdr.Name == memberRefRow.Name)
                {
                    Signature.BaseMethod mrsig = Signature.ParseMethodDefSig(memberRefRow.m, memberRefRow.Signature, ass);
                    Signature.BaseMethod mdsig = Signature.ParseMethodDefSig(mdr.m, mdr.Signature, ass);

                    if(!Signature.BaseMethodSigCompare(mrsig, mdsig, ass))
                        continue;

                    return mdr;
                }
            }

            /* DEBUG stuff */
            tdr = GetTypeDef(memberRefRow.Class.ToToken(), ass);

            for (TableIndex ti = tdr.MethodList; ti < Metadata.GetLastMethod(tdr.m, tdr); ti++)
            {
                MethodDefRow mdr = ti.Value as MethodDefRow;

                if (mdr.Name == memberRefRow.Name)
                {
                    Signature.BaseMethod mrsig = Signature.ParseMethodDefSig(memberRefRow.m, memberRefRow.Signature, ass);
                    Signature.BaseMethod mdsig = Signature.ParseMethodDefSig(mdr.m, mdr.Signature, ass);

                    if (!Signature.BaseMethodSigCompare(mrsig, mdsig, ass))
                        continue;

                    return mdr;
                }
            }

            return null;
        }

        private static TypeDefRow ResolveTypeSpec(TypeSpecRow typeSpecRow, Assembler ass)
        {
            int r = 0;
            Signature.Param p = Signature.ParseParamSig(typeSpecRow.m, typeSpecRow.Signature, ref r, ass);
            return GetTypeDef(p.Type, ass);            
        }

        private static TypeDefRow ResolveTypeRef(TypeRefRow typeRefRow, Assembler ass)
        {
            if (typeRefRow.ResolutionScope.TableId == TableId.AssemblyRef)
            {
                // Load assembly and find type
                Metadata mnew = ass.FindAssembly((Metadata.AssemblyRefRow)typeRefRow.ResolutionScope.Value);
                foreach (Metadata.TypeDefRow tdr in mnew.Tables[(int)Metadata.TableId.TypeDef])
                {
                    if ((tdr._ActualTypeName == typeRefRow.TypeName) && (tdr._ActualTypeNamespace == typeRefRow.TypeNamespace))
                        return tdr;
                }
                return null;
            }
            else if (typeRefRow.ResolutionScope.TableId == TableId.TypeRef)
            {
                TypeDefRow parent = ResolveTypeRef(typeRefRow.ResolutionScope.Value as Metadata.TypeRefRow, ass);
                foreach (Metadata.TypeDefRow tdr in parent._NestedChildren)
                {
                    if ((tdr._ActualTypeName == typeRefRow.TypeName) && (tdr._ActualTypeNamespace == typeRefRow.TypeNamespace))
                        return tdr;
                }
                return null;
            }
            else
                throw new NotSupportedException();
        }

        public File File = null;
        public string ModuleName = "";

        public static Metadata LoadAssembly(string mod_name, Assembler ass, string output_name)
        {
            ass.DebugLine("libtysila.Metadata.LoadAssembly(" + mod_name + ",,)");

            if (ass.Loader == null)
                throw new Exception("Assembler.Loader is not defined");

            Assembler.FileLoader.FileLoadResults flr = ass.Loader.LoadFile(mod_name);

            if (flr == null)
                throw new Exception("Unable to locate referenced assembly: " + mod_name);

            File f = new PEFile(flr.FullFilename);
            f.Parse(flr.Stream);
            Metadata m = f.GetMetadata(ass);
            m.File = f;
            m.ModuleName = flr.ModuleName;
            m.StringTable = new StringTable(output_name + "_" + flr.ModuleName + "_stringtable");

            return m;
        }

        public static string GetTypeFullname(Token token)
        {
            if (token.Value is Metadata.TypeRefRow)
                return ((Metadata.TypeRefRow)token.Value).TypeFullName;
            else if (token.Value is Metadata.TypeDefRow)
                return ((Metadata.TypeDefRow)token.Value).TypeFullName;
            else
                throw new NotSupportedException();
        }

        internal static bool IsSpecialType(TypeDefRow tdr, Signature.BaseOrComplexType sig, Assembler ass)
        {
            if (tdr.TypeNamespace == "libsupcs")
            {
                return true;
            }
            foreach (CustomAttributeRow car in tdr.CustomAttrs)
            {
                Metadata.MethodDefRow camdr = Metadata.GetMethodDef(car.Type.ToToken(), ass);
                Metadata.TypeDefRow ca_tdr = Metadata.GetOwningType(camdr.m, camdr);
                Signature.Param ca_tsig = new Signature.Param(new Token(tdr), ass);
                Signature.BaseMethod bmeth = Signature.ParseMethodDefSig(camdr.m, camdr.Signature, ass);

                if ((ca_tdr.TypeNamespace == "libsupcs") && (ca_tdr.TypeName == "SpecialTypeAttribute") &&
                    (camdr.Name == ".ctor"))
                    return true;
            }
            return false;
        }


        public static Assembler.TypeToCompile GetTTC(string assembly, string typenamespace, string typename, Assembler ass)
        {
            Metadata.TypeDefRow tdr = GetTypeDef(assembly, typenamespace, typename, ass);
            return new Assembler.TypeToCompile { _ass = ass, type = tdr, tsig = new Signature.Param(tdr, ass) };
        }
        internal static Assembler.TypeToCompile GetTTC(TableIndex tableIndex, Assembler.TypeToCompile parent, Assembler ass)
        { return GetTTC(tableIndex, parent, null, ass); }
        internal static Assembler.TypeToCompile GetTTC(Token token, Assembler.TypeToCompile parent, Assembler ass)
        { return GetTTC(new TableIndex(token), parent, null, ass); }
        internal static Assembler.TypeToCompile GetTTC(Token token, Assembler.TypeToCompile parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            if (token is TTCToken)
                return ((TTCToken)token).ttc;
            return GetTTC(new TableIndex(token), parent, containing_meth, ass);
        }
        internal static Assembler.TypeToCompile GetTTC(TableIndex tableIndex, Assembler.TypeToCompile parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            // Return a TypeToCompile from a TypeDefOrRef coded index
            if (tableIndex.TableId == TableId.TypeDef)
                return new Assembler.TypeToCompile { _ass = ass, type = tableIndex.Value as Metadata.TypeDefRow, tsig = new Signature.Param(tableIndex.Value, ass) };
            else if (tableIndex.TableId == TableId.TypeRef)
                return new Assembler.TypeToCompile { _ass = ass, type = GetTypeDef(tableIndex.Value, ass), tsig = new Signature.Param(tableIndex.Value, ass) };
            else if (tableIndex.TableId == TableId.TypeSpec)
            {
                Signature.BaseOrComplexType bct = Signature.ParseTypeSig(((Metadata.TypeSpecRow)tableIndex.Value).m, ((Metadata.TypeSpecRow)tableIndex.Value).Signature, ass);
                Signature.Param gt = Signature.ResolveGenericParam(new Signature.Param(bct, ass), parent.tsig.Type, containing_meth, ass);
                //gt.IdentifyBasetype(ass);
                return new Assembler.TypeToCompile { _ass = ass, type = GetTypeDef(gt.Type, ass), tsig = gt };
            }
            else
                throw new NotImplementedException();
        }
        public static Assembler.TypeToCompile GetTTC(Signature.Param p, Assembler.TypeToCompile parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            Signature.Param gt = Signature.ResolveGenericParam(p, parent.tsig.Type, containing_meth, ass);
            return new Assembler.TypeToCompile { _ass = ass, type = GetTypeDef(gt.Type, ass), tsig = gt };
        }

        internal static Assembler.FieldToCompile GetFTC(TableIndex tableIndex, Assembler.TypeToCompile containing_type, Signature.BaseMethod containing_meth, Assembler ass)
        {
            // Return a FieldToCompile from a Field or FieldRef index
            if (tableIndex.TableId == TableId.Field)
            {
                Assembler.FieldToCompile ret = new Assembler.FieldToCompile();
                ret._ass = ass;
                ret.field = tableIndex.Value as Metadata.FieldRow;
                ret.fsig = Signature.ResolveGenericParam(ret.field.GetSignature(), containing_type.tsig.Type, containing_meth, ass);
                ret.definedin_type = GetOwningType(ret.field.m, ret.field);
                ret.definedin_tsig = Signature.ResolveGenericParam(new Signature.Param(ret.definedin_type, ass), containing_type.tsig.Type, containing_meth, ass);
                return ret;
            }
            else if (tableIndex.TableId == TableId.MemberRef)
            {
                MemberRefRow memberRefRow = tableIndex.Value as Metadata.MemberRefRow;

                Assembler.TypeToCompile ttc = GetTTC(memberRefRow.Class, containing_type, ass);

                for (TableIndex ti = ttc.type.FieldList; ti < Metadata.GetLastField(ttc.type.m, ttc.type); ti++)
                {
                    FieldRow mdr = ti.Value as FieldRow;

                    if (mdr.Name == memberRefRow.Name)
                    {
                        if (memberRefRow.Signature != null)
                        {
                            //Signature.Param mrsig = Signature.ResolveGenericParam(Signature.ParseFieldSig(memberRefRow.m, memberRefRow.Signature, ass), containing_type.tsig.Type,
                            //    containing_meth, ass);
                            //Signature.Param mdsig = Signature.ResolveGenericParam(Signature.ParseFieldSig(mdr.m, mdr.Signature, ass), containing_type.tsig.Type,
                            //    containing_meth, ass);

                            Signature.Param mrsig = Signature.ResolveGenericParam(Signature.ParseFieldSig(memberRefRow.m, memberRefRow.Signature, ass), ttc.tsig.Type,
                                containing_meth, ass);
                            Signature.Param mdsig = Signature.ResolveGenericParam(Signature.ParseFieldSig(mdr.m, mdr.Signature, ass), ttc.tsig.Type,
                                containing_meth, ass);

                            if (Signature.ParamCompare(mrsig, mdsig, ass))
                            {
                                Assembler.FieldToCompile ret = new Assembler.FieldToCompile();
                                ret.field = mdr;
                                ret.fsig = mdsig;
                                ret.definedin_type = ttc.type;
                                ret.definedin_tsig = Signature.ResolveGenericParam(ttc.tsig, containing_type.tsig.Type, containing_meth, ass);
                                ret._ass = ass;
                                //ret.definedin_tsig = Signature.ResolveGenericParam(new Signature.Param(ret.definedin_type, ass), containing_type.tsig.Type, containing_meth, ass);
                                return ret;
                            }
                        }
                    }
                }

                throw new Exception("Field not found");
            }
            else
                throw new NotImplementedException();
        }

        public static Assembler.MethodToCompile GetMTC(TableIndex tableIndex, Assembler.TypeToCompile parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            // Return a MethodToCompile from a MethodDefOrRef coded index
            if (parent.tsig == null)
                parent.tsig = new Signature.Param(ass) { Type = null };
            if (tableIndex.TableId == TableId.MethodDef)
            {
                Assembler.MethodToCompile ret = new Assembler.MethodToCompile(ass, tableIndex.ToToken(tableIndex.Metadata).ToUInt32());
                ret.meth = tableIndex.Value as Metadata.MethodDefRow;
                ret.msig = Signature.ResolveGenericMember(Signature.ParseMethodSig(ret.meth), parent.tsig.Type, containing_meth, ass);
                ret.type = GetOwningType(ret.meth.m, ret.meth);
                ret.tsigp = Signature.ResolveGenericParam(new Signature.Param(ret.type, ass), parent.tsig.Type, containing_meth, ass);
                ret.MetadataToken = tableIndex.ToToken().ToUInt32();
                ret.m = tableIndex.Metadata;
                return ret;
            }
            else if (tableIndex.TableId == TableId.MemberRef)
            {
                Metadata.MemberRefRow mrr = tableIndex.Value as Metadata.MemberRefRow;
                Assembler.TypeToCompile ttc = GetTTC(mrr.Class, parent, containing_meth, ass);
                Assembler.MethodToCompile mtc = new Assembler.MethodToCompile { _ass = ass, tsigp = ttc.tsig, type = ttc.type };
                mtc.msig = Signature.ResolveGenericMember(Signature.ParseMethodSig(mrr), ttc.tsig.Type, containing_meth, ass);

                /* Rewrite delegate constructors */
                if (ttc.type.IsDelegate(ass) && (mrr.Name == ".ctor") && (mtc.msig.Method.Params.Count == 2) && (Signature.ParamCompare(mtc.msig.Method.Params[1], new Signature.Param(BaseType_Type.I), ass)))
                    mtc.msig.Method.Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);
                mtc.meth = Metadata.GetMethodDef(mrr.Name, mtc.msig, ttc, containing_meth, ass);

                if (mtc.meth == null)
                {
                    mtc.meth = Metadata.GetMethodDef(mrr.Name, mtc.msig, ttc, containing_meth, ass);
                    throw new Exception("Cannot find method named " + mrr.Name + " within " + ttc.type.TypeFullName);
                }

                mtc.MetadataToken = tableIndex.ToToken().ToUInt32();
                mtc.m = tableIndex.Metadata;
                return mtc;
            }
            else if (tableIndex.TableId == TableId.MethodSpec)
            {
                Metadata.MethodSpecRow msr = tableIndex.Value as Metadata.MethodSpecRow;
                Assembler.MethodToCompile ret = new Assembler.MethodToCompile(ass, tableIndex.ToTokenUInt32());
                ret.meth = Metadata.GetMethodDef(msr.Method.ToToken(), ass);
                ret.msig = Signature.ResolveGenericMember(Signature.ParseMethodSpec(msr.m, msr.Instantiation, ass), parent.tsig.Type, containing_meth, ass);
                if (ret.msig is Signature.GenericMethod)
                {
                    Signature.GenericMethod gm = ret.msig as Signature.GenericMethod;
                    Assembler.MethodToCompile base_meth = GetMTC(msr.Method, parent, gm, ass);
                    gm.GenMethod = Signature.ResolveGenericMember(base_meth.msig, parent.tsig.Type, gm, ass) as Signature.Method;
                    //gm.GenMethod = base_meth.msig.Method;
                }
                ret.type = GetOwningType(ret.meth.m, ret.meth);
                ret.tsigp = Signature.ResolveGenericParam(new Signature.Param(ret.type, ass), parent.tsig.Type, null, ass);
                ret.MetadataToken = tableIndex.ToToken().ToUInt32();
                ret.m = tableIndex.Metadata;
                return ret;
            }
            else
                throw new NotImplementedException();
        }

        private static MethodDefRow GetMethodDef(string name, Signature.BaseMethod sig, Assembler.TypeToCompile ttc, Signature.BaseMethod containing_meth, Assembler ass)
        {
            foreach(Metadata.MethodDefRow mdr in ttc.type.Methods)
            //for (Metadata.TableIndex meth = ttc.type.MethodList; meth < Metadata.GetLastMethod(ttc.type); meth++)
            {
                //Metadata.MethodDefRow mdr = Metadata.GetMethodDef(meth.ToToken(), ass);
                if (mdr.Name == name)
                {
                    Signature.BaseMethod test = Signature.ParseMethodSig(mdr);
                    if (test.Method.GenParamCount == sig.Method.GenParamCount)
                    {
                        if (Signature.BaseMethodSigCompare(sig, Signature.ResolveGenericMember(Signature.ParseMethodSig(mdr), ttc.tsig.Type, containing_meth, ass), ass))
                            return mdr;
                    }
                }
            }
            return null;
        }


        // cache the response so the string comparison is only tested once - these methods will be called a lot
        public bool libsupcs_tested = false;
        public bool is_libsupcs = false;
        public bool libstdcs_tested = false;
        public bool is_libstdcs = false;

        public bool IsLibSupCs
        {
            get
            {
                if (libsupcs_tested)
                    return is_libsupcs;
                if (ModuleName == "libsupcs")
                    is_libsupcs = true;
                libsupcs_tested = true;
                return is_libsupcs;
            }
        }

        public bool IsLibStdCs
        {
            get
            {
                if (libstdcs_tested)
                    return is_libstdcs;
                if (ModuleName == "libstdcs")
                    is_libstdcs = true;
                libstdcs_tested = true;
                return is_libstdcs;
            }
        }

        // Return the entry point if it exists
        public Assembler.MethodToCompile? GetEntryPoint(Assembler ass)
        {
            if (File.GetStartToken() == 0)
                return null;
            Token t = new Token(File.GetStartToken(), this);
            MethodDefRow mdr = GetMethodDef(t, ass);
            Signature.BaseMethod msig = mdr.ActualSignature;
            TypeDefRow tdr = GetOwningType(this, mdr);
            Signature.Param tsig = new Signature.Param(tdr, ass);
            return new Assembler.MethodToCompile { _ass = ass, m = this, MetadataToken = File.GetStartToken(), meth = mdr, msig = msig, tsigp = tsig, type = tdr };
        }
    }
}
