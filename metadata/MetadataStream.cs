/* Copyright (C) 2016 by John Cronin
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

namespace metadata
{
    public partial class MetadataStream
    {
        public string VersionString;

        internal string assemblyName;

        public PEFile.StreamHeader sh_string, sh_guid, sh_blob, sh_us, sh_tables;
        internal PEFile pef;
        public AssemblyLoader al;
        public DataInterface file;

        public long ResolveRVA(long RVA) { return pef.ResolveRVA(RVA); }
        public PEFile GetPEFile() { return pef; }

        public bool wide_string = false;
        public bool wide_guid = false;
        public bool wide_blob = false;

        public uint entry_point_token = 0;

        public bool[] valid_tables = new bool[64];
        public int[] table_rows = new int[64];
        public int[] table_offsets = new int[64];
        public int[] table_entry_size = new int[64];
        public List<int>[] table_column_sizes = new List<int>[64];
        public List<int>[] table_column_offsets = new List<int>[64];
        internal List<FieldTemplate>[] table_templates = new List<FieldTemplate>[64];

        public MetadataStream[] referenced_assemblies;

        public int[] methoddef_owners;
        public int[] fielddef_owners;
        public uint[] fieldrvas;
        public int[] classlayouts;
        public int[] gtparams;
        public int[] gmparams;
        public int[] const_field_owners;
        public bool[] thread_local_fields;

        public int[] simple_type_idx;
        public int[] simple_type_rev_idx;
        public bool is_corlib = false;

        public int[] nested_types;
        public int[] next_nested_type;
        public int[] nested_parent;

        public int[] td_custom_attrs;
        public int[] md_custom_attrs;
        public int[] fd_custom_attrs;
        public int[] next_ca;

        public string[] td_extends_override;

        public string AssemblyName { get { if (assemblyName == null) return "unnamed"; else return assemblyName; } }
        public string FullName { get { return AssemblyName + " " + AssemblyVersionString; } }

        public override string ToString()
        {
            return FullName;
        }

        /* Fast access to built-in types */
        public class BuiltInType
        {
            public string name, nspace, mod;
            TypeSpec v;
            internal AssemblyLoader al;

            public TypeSpec Type
            {
                get
                {
                    if (v == null)
                    {
                        var m = al.GetAssembly(mod);
                        v = m.GetTypeSpec(nspace, name);
                        if (v == null)
                            throw new Exception("Cannot find " + nspace + "." +
                                name + " in " + mod);
                    }
                    return v;
                }
            }

            public static implicit operator TypeSpec(BuiltInType b) { return b.Type; }
        }

        public void LoadBuiltinTypes()
        {
            SystemObject = GetBuiltin("Object");
            SystemString = GetBuiltin("String");
            SystemInt8 = GetBuiltin("SByte");
            SystemInt16 = GetBuiltin("Int16");
            SystemChar = GetBuiltin("Char");
            SystemInt32 = GetBuiltin("Int32");
            SystemInt64 = GetBuiltin("Int64");
            SystemIntPtr = GetBuiltin("IntPtr");
            SystemRuntimeTypeHandle = GetBuiltin("RuntimeTypeHandle");
            SystemRuntimeMethodHandle = GetBuiltin("RuntimeMethodHandle");
            SystemRuntimeFieldHandle = GetBuiltin("RuntimeFieldHandle");
            SystemEnum = GetBuiltin("Enum");
            SystemValueType = GetBuiltin("ValueType");
            SystemVoid = GetBuiltin("Void");
            SystemArray = GetBuiltin("Array");
            SystemByte = GetBuiltin("Byte");
            SystemUInt16 = GetBuiltin("UInt16");
            SystemUInt32 = GetBuiltin("UInt32");
            SystemUInt64 = GetBuiltin("UInt64");
            SystemDelegate = GetBuiltin("Delegate");
            SystemTypedByRef = GetBuiltin("TypedReference");
            SystemDouble = GetBuiltin("Double");
            SystemSingle = GetBuiltin("Single");
            SystemBool = GetBuiltin("Bool");
        }

        internal BuiltInType GetBuiltin(string name, string nspace = "System", string mod = "mscorlib")
        { return new BuiltInType { mod = mod, name = name, nspace = nspace, al = al }; }

        public BuiltInType SystemObject;
        public BuiltInType SystemString;
        public BuiltInType SystemInt8;
        public BuiltInType SystemInt16;
        public BuiltInType SystemChar;
        public BuiltInType SystemInt32;
        public BuiltInType SystemInt64;
        public BuiltInType SystemIntPtr;
        public BuiltInType SystemRuntimeTypeHandle;
        public BuiltInType SystemRuntimeMethodHandle;
        public BuiltInType SystemRuntimeFieldHandle;
        public BuiltInType SystemEnum;
        public BuiltInType SystemValueType;
        public BuiltInType SystemVoid;
        public BuiltInType SystemArray;
        public BuiltInType SystemByte;
        public BuiltInType SystemUInt16;
        public BuiltInType SystemUInt32;
        public BuiltInType SystemUInt64;
        public BuiltInType SystemDelegate;
        public BuiltInType SystemTypedByRef;
        public BuiltInType SystemBool;
        public BuiltInType SystemDouble;
        public BuiltInType SystemSingle;

        /* Consts for fast table indexing */
        public const int tid_Assembly = 0x20;
        public const int tid_AssemblyOS = 0x22;
        public const int tid_AssemblyProcessor = 0x21;
        public const int tid_AssemblyRef = 0x23;
        public const int tid_AssemblyRefOS = 0x25;
        public const int tid_AssemblyRefProcessor = 0x24;
        public const int tid_ClassLayout = 0x0f;
        public const int tid_Constant = 0x0b;
        public const int tid_CustomAttribute = 0x0c;
        public const int tid_DeclSecurity = 0x0e;
        public const int tid_EventMap = 0x12;
        public const int tid_Event = 0x14;
        public const int tid_ExportedType = 0x27;
        public const int tid_Field = 0x04;
        public const int tid_FieldLayout = 0x10;
        public const int tid_FieldMarshal = 0x0d;
        public const int tid_FieldRVA = 0x1d;
        public const int tid_File = 0x26;
        public const int tid_GenericParam = 0x2a;
        public const int tid_GenericParamConstaint = 0x2c;
        public const int tid_ImplMap = 0x1c;
        public const int tid_InterfaceImpl = 0x09;
        public const int tid_ManifestResource = 0x28;
        public const int tid_MemberRef = 0x0a;
        public const int tid_MethodDef = 0x06;
        public const int tid_MethodImpl = 0x19;
        public const int tid_MethodSemantics = 0x18;
        public const int tid_MethodSpec = 0x2b;
        public const int tid_Module = 0x00;
        public const int tid_ModuleRef = 0x1a;
        public const int tid_NestedClass = 0x29;
        public const int tid_Param = 0x08;
        public const int tid_Property = 0x17;
        public const int tid_PropertyMap = 0x15;
        public const int tid_StandAloneSig = 0x11;
        public const int tid_TypeDef = 0x02;
        public const int tid_TypeRef = 0x01;
        public const int tid_TypeSpec = 0x1b;

        /* Class that stores a type name and namespace for quick
        lookup in a hash table */
        class nn : IEquatable<nn>
        {
            public string name, nspace;

            public bool Equals(nn other)
            {
                if (other == null)
                    return false;
                if (name != other.name)
                    return false;
                return name == other.name;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as nn);
            }

            public override int GetHashCode()
            {
                return name.GetHashCode() ^ (nspace.GetHashCode() << 16);
            }
        }

        Dictionary<nn, TypeSpec> typedef_db = new Dictionary<nn, TypeSpec>(
            new GenericEqualityComparer<nn>());

        /* Templates used for exploring metadata - compiler should use direct
            accesses via GetInt etc to prevent creating loads of objects */
        public class FieldEntry
        {
            public MetadataStream m;
            public int Table, Row, Col;

            public enum FieldType
            {
                String,
                Blob,
                Guid,
                Int,
                TableIndex
            }

            public FieldType EntryType;
            public string StringValue;
            public uint BlobOffset;
            public Guid GuidValue;
            public uint IntValue;
            public int TableRefRow, TableRefId;

            public override string ToString()
            {
                switch (EntryType)
                {
                    case FieldType.Blob:
                        return "Blob: 0x" + BlobOffset.ToString("X");
                    case FieldType.Guid:
                        return GuidValue.ToString();
                    case FieldType.Int:
                        return IntValue.ToString("X");
                    case FieldType.String:
                        return StringValue;
                    case FieldType.TableIndex:
                        return "{ " + TableRefId.ToString("X2") + ": " +
                            TableRefRow.ToString() + " }";
                    default:
                        return base.ToString();
                }
            }
        }

        internal class FieldTemplate
        {
            public enum FieldType
            {
                String,
                Blob,
                Guid,
                Int16,
                Int32,
                SimpleIndex,
                CodedIndex
            }

            public FieldType EntryType;
            public CodedIndexTemplate CodedTemplate;
            public int SimpleIndex;
        }

        public int GetRowCount(int table_id)
        {
            return table_rows[table_id];
        }

        public int GetColCount(int table_id)
        {
            return table_column_sizes[table_id].Count;
        }

        public int GetTableCount()
        {
            return 64;
        }

        public FieldEntry GetFieldEntry(int table_id, int row, int col)
        {
            FieldEntry ret = new FieldEntry();
            ret.m = this;
            ret.Row = row;
            ret.Col = col;
            ret.Table = table_id;

            // Get the template for the field
            var templ = table_templates[table_id][col];

            // Interpret it according to the template
            switch (templ.EntryType)
            {
                case FieldTemplate.FieldType.Blob:
                    ret.EntryType = FieldEntry.FieldType.Blob;
                    ret.BlobOffset = GetIntEntry(table_id, row, col);
                    break;
                case FieldTemplate.FieldType.CodedIndex:
                    ret.EntryType = FieldEntry.FieldType.TableIndex;
                    GetCodedIndexEntry(table_id, row, col,
                        templ.CodedTemplate, out ret.TableRefId,
                        out ret.TableRefRow);
                    break;
                case FieldTemplate.FieldType.Guid:
                    ret.EntryType = FieldEntry.FieldType.Guid;
                    ret.GuidValue = GetGuidEntry(table_id, row, col);
                    break;
                case FieldTemplate.FieldType.Int16:
                case FieldTemplate.FieldType.Int32:
                    ret.EntryType = FieldEntry.FieldType.Int;
                    ret.IntValue = GetIntEntry(table_id, row, col);
                    break;
                case FieldTemplate.FieldType.SimpleIndex:
                    ret.EntryType = FieldEntry.FieldType.TableIndex;
                    ret.TableRefId = templ.SimpleIndex;
                    ret.TableRefRow = (int)GetIntEntry(table_id, row, col);
                    break;
                case FieldTemplate.FieldType.String:
                    ret.EntryType = FieldEntry.FieldType.String;
                    ret.StringValue = GetStringEntry(table_id, row, col);
                    break;
            }

            return ret;
        }

        // These are the functions likely called by the compiler

        // Raw get function
        public virtual uint GetIntEntry(int table_id, int row, int col)
        {
            if (row == 0)
                return 0;

            // Get the column size and offset
            var size = table_column_sizes[table_id][col];
            var offset = table_column_offsets[table_id][col];

            // Get the offset of the table and row
            var table_offset = table_offsets[table_id];
            var row_offset = table_offset + (row - 1) * table_entry_size[table_id];

            // Get the value contained within the table
            uint val = sh_tables.di.ReadUInt(row_offset + offset);
            switch (size)
            {
                case 1:
                    val &= 0xffU;
                    break;
                case 2:
                    val &= 0xffffU;
                    break;
            }

            return val;
        }

        // Formatted functions
        public string GetStringEntry(int table_id, int row, int col)
        {
            uint val = GetIntEntry(table_id, row, col);
            return GetString((int)val);
        }

        public string GetString(int addr)
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                byte b = sh_string.di.ReadByte(addr);
                if (b == 0)
                    break;
                sb.Append((char)b);
                addr++;
            }

            return sb.ToString();
        }

        public string GetUserString(int addr)
        {
            StringBuilder sb = new StringBuilder();

            int len = (int)SigReadUSCompressed(ref addr, true);
            len--;

            for (int i = 0; i < len; i += 2)
                sb.Append((char)sh_us.di.ReadUShort(addr + i));

            return sb.ToString();
        }

        public Guid GetGuidEntry(int table_id, int row, int col)
        {
            uint val = GetIntEntry(table_id, row, col);
            if (val == 0)
                return Guid.Empty;
            else
            {
                byte[] g = new byte[16];
                for (int i = 0; i < 16; i++)
                    g[i] = sh_guid.di.ReadByte(((int)val - 1) * 16 + i);

                return new Guid(g);
            }
        }

        public void GetCodedIndexEntry(int table_id, int row, int col,
            CodedIndexTemplate templ, out int ref_id, out int ref_row)
        {
            GetCodedIndexEntry(GetIntEntry(table_id, row, col),
                templ, out ref_id, out ref_row);
        }
        public void GetCodedIndexEntry(uint val,
            CodedIndexTemplate templ, out int ref_id, out int ref_row)
        {
            uint index = val >> templ.TagBits;
            uint table = val & ~(0xffffffffU << templ.TagBits);

            ref_row = (int)index;
            ref_id = templ.Members[(int)table];
        }
        public uint MakeCodedIndexEntry(int table_id, int table_row,
            CodedIndexTemplate templ)
        {
            uint encoded_table = 0;
            for(int i = 0; i < templ.Members.Length; i++)
            {
                if(templ.Members[i] == table_id)
                {
                    encoded_table = (uint)i;
                    break;
                }
            }
            uint val = (((uint)table_row) << templ.TagBits) | encoded_table;
            return val;
        }

        public DataInterface GetRVA(long RVA)
        {
            long offset = pef.ResolveRVA(RVA);
            return file.Clone((int)offset);
        }

        public static bool CompareSignature(MethodSpec a, MethodSpec b)
        {
            return CompareSignature(a.m, a.msig, a.gtparams, a.gmparams,
                b.m, b.msig, b.gtparams, b.gmparams);
        }

        public static bool CompareSignature(MethodSpec a,
            MetadataStream mb, int msigb,
            TypeSpec[] gtparamsb, TypeSpec[] gmparamsb)
        {
            return CompareSignature(a.m, a.msig, a.gtparams, a.gmparams,
                mb, msigb, gtparamsb, gmparamsb);
        }

        public static bool CompareSignature(MetadataStream ma,
            int msiga,
            TypeSpec[] gtparamsa, TypeSpec[] gmparamsa,
            MetadataStream mb, int msigb,
            TypeSpec[] gtparamsb, TypeSpec[] gmparamsb)
        {
            // Quick check
            if(ma == mb && msiga == msigb)
            {
                if (gtparamsa == gtparamsb && gmparamsa == gmparamsb)
                    return true;
            }

            // Param count
            var pca = ma.GetMethodDefSigParamCount(msiga);
            var pcb = mb.GetMethodDefSigParamCount(msigb);
            if (pca != pcb)
                return false;

            // Explicit this and calling convention
            int cca, ccb;
            if (ma.GetMethodDefSigHasNonExplicitThis(msiga, out cca) !=
                mb.GetMethodDefSigHasNonExplicitThis(msigb, out ccb))
                return false;
            if (cca != ccb)
                return false;

            // Return type
            /*msiga = ma.GetMethodDefSigRetTypeIndex(msiga);
            msigb = mb.GetMethodDefSigRetTypeIndex(msigb);
            var rta = ma.GetTypeSpec(ref msiga, gtparamsa, gmparamsa);
            var rtb = mb.GetTypeSpec(ref msigb, gtparamsb, gmparamsb);
            if (rta == null && rtb != null)
                return false;
            if (rtb == null && rta != null)
                return false;
            if (rta != null && !rta.Equals(rtb))
                return false;*/

            // Params
            for(int i = 0; i < pca; i++)
            {
                var pa = ma.GetTypeSpec(ref msiga, gtparamsa, gmparamsa);
                var pb = mb.GetTypeSpec(ref msigb, gtparamsb, gmparamsb);

                if(pa.stype == TypeSpec.SpecialType.MVar)
                {
                    if (pb.stype != TypeSpec.SpecialType.MVar)
                        return false;
                    return pa.idx == pb.idx;
                }

                if (!pa.Equals(pb))
                    return false;
            }

            return true;
        }

        public static bool CompareString(MetadataStream ma,
            uint sa, MetadataStream mb, uint sb)
        {
            if (ma == mb)
            {
                if (sa == sb)
                    return true;
                // ?strings are unique in each string table
                // so if sa != sb can return false?
            }

            // Strip 0x07 header if necessary
            var sai = (int)(sa & 0x00ffffffU);
            var sbi = (int)(sb & 0x00ffffffU);

            while(true)
            {
                var ca = ma.sh_string.di.ReadByte(sai++);
                var cb = mb.sh_string.di.ReadByte(sbi++);

                if (ca != cb)
                    return false;
                if (ca == 0)
                    return true;
            }
        }

        public static bool CompareString(MetadataStream ma,
            uint sa, string sb)
        {
            var sai = (int)(sa & 0x00ffffffU);
            
            for(int i = 0; i < sb.Length; i++)
            {
                var ca = ma.sh_string.di.ReadByte(sai++);
                if (ca != sb[i])
                    return false;
            }

            var endbyte = ma.sh_string.di.ReadByte(sai);
            return endbyte == 0;
        }

        public void InterpretToken(uint token, out int table_id,
            out int row)
        {
            table_id = (int)(token >> 24);
            row = (int)(token & 0xffffffU);
        }

        public TypeSpec GetTypeSpec(string nspace, string name, TypeSpec[] gtparams = null)
        {
            TypeSpec ts;
            var test = new nn { name = name, nspace = nspace };
            if (typedef_db.TryGetValue(test, out ts))
            {
                var new_ts = new TypeSpec();
                new_ts.m = this;
                new_ts.tdrow = ts.tdrow;
                new_ts.gtparams = gtparams;
                return new_ts;
            }

            for (int i = 1; i <= table_rows[tid_TypeDef]; i++)
            {
                var cur_name = GetIntEntry(tid_TypeDef, i, 1);
                if (CompareString(this, cur_name, name))
                {
                    var cur_nspace = GetIntEntry(tid_TypeDef, i, 2);

                    if (CompareString(this, cur_nspace, nspace))
                    {
                        ts = new TypeSpec();
                        ts.m = this;
                        ts.tdrow = i;
                        ts.gtparams = gtparams;
                        typedef_db[test] = ts;
                        return ts;
                    }
                }
            }

            return null;
        }

        public TypeSpec GetTypeSpec(uint token, TypeSpec[] gtparams = null, TypeSpec[] gmparams = null)
        {
            int table_id, row;
            InterpretToken(token, out table_id, out row);
            return GetTypeSpec(table_id, row, gtparams, gmparams);
        }

        public bool GetTypeDefRow(int table_id, int row, out TypeSpec ts)
        {
            throw new NotImplementedException();
            switch(table_id)
            {
                case (int)TableId.TypeDef:
                    {
                        TypeSpec ret = new TypeSpec();
                        ret.m = this;
                        ret.tdrow = row;
                        ts = ret;
                        return true;
                    }

                case (int)TableId.TypeRef:
                    return GetTypeRefRow(row, out ts);

                case (int)TableId.TypeSpec:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }

        private bool GetTypeRefRow(int row, out TypeSpec ts)
        {
            int rs_tableid;
            int rs_row;
            GetCodedIndexEntry(tid_TypeRef, row, 0,
                ResolutionScope, out rs_tableid, out rs_row);
            var typename = GetStringEntry(tid_TypeRef, row, 1);
            var typenamespace = GetStringEntry(tid_TypeRef, row, 2);

            switch (rs_tableid)
            {
                case (int)TableId.Module:
                    throw new NotImplementedException();
                case (int)TableId.ModuleRef:
                    throw new NotImplementedException();
                case (int)TableId.AssemblyRef:
                    {
                        var ass_name = GetStringEntry(rs_tableid, rs_row, 6);
                        var other_ass = al.GetAssembly(ass_name);

                        var tforward_key = other_ass.AssemblyName + "!" + typenamespace + "." + typename;
                        if (al.TypeForwarders.ContainsKey(tforward_key))
                            other_ass = al.GetAssembly(al.TypeForwarders[tforward_key]);

                        ts = other_ass.GetTypeSpec(typenamespace, typename);

                        return true;
                    }
                case (int)TableId.TypeRef:
                    {
                        var enc_ts = GetTypeSpec(rs_tableid, rs_row);

                        // find a particular nested type
                        int cur_nested = enc_ts.m.nested_types[enc_ts.tdrow];
                        while (cur_nested != 0)
                        {
                            // only compare the type name (namespace will be null)
                            var nested_name = enc_ts.m.GetStringEntry(tid_TypeDef,
                                cur_nested, 1);
                            if (nested_name.Equals(typename))
                            {
                                var nested_ts = enc_ts.m.GetTypeSpec(tid_TypeDef,
                                    cur_nested);
                                ts = nested_ts;
                                return true;
                            }

                            cur_nested = enc_ts.m.next_nested_type[cur_nested];
                        }

                        throw new NotSupportedException();
                    }
            }
            throw new NotImplementedException();
        }

        /**<summary>Slow method that compares against a string, for use only to
        find special fields (e.g members of System.String).  For
        runtime lookups use the faster methods that search using
        a string index</summary> */
        public MethodSpec GetFieldDefRow(string name, TypeSpec ts)
        {
            var first_fdef = ts.m.GetIntEntry(tid_TypeDef,
                ts.tdrow, 4);
            var last_fdef = ts.m.GetLastFieldDef(ts.tdrow);

            for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                // Check on name
                var fname = ts.m.GetIntEntry(tid_Field,
                    (int)fdef_row, 1);
                var fnames = ts.m.GetStringEntry(tid_Field,
                    (int)fdef_row, 1);
                if(CompareString(ts.m, fname, name))
                {
                    var fs = new MethodSpec
                    {
                        m = ts.m,
                        mdrow = (int)fdef_row,
                        msig = (int)ts.m.GetIntEntry(tid_Field, (int)fdef_row, 2),
                        type = ts,
                        is_field = true
                    };
                    return fs;
                }
            }

            return null;
        }

        public bool GetFieldDefRow(int table_id, int row, out TypeSpec ts,
            out MethodSpec fs)
        {
            switch(table_id)
            {
                case (int)TableId.Field:
                    fs = new MethodSpec();
                    fs.m = this;
                    fs.msig = (int)GetIntEntry(table_id, row, 2);
                    fs.mdrow = row;
                    fs.is_field = true;
                    ts = new TypeSpec();
                    ts.m = this;
                    ts.tdrow = fielddef_owners[row];
                    return true;
                case (int)TableId.MemberRef:
                    //GetMethodDefRow(table_id, row, out fs,
                    //    gtparams, gmparams);
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException();
            }
        }

        public MethodSpec GetMethodSpec(uint token,
            TypeSpec[] gtparams = null, TypeSpec[] gmparams = null)
        {
            int table_id, row;
            InterpretToken(token, out table_id, out row);
            MethodSpec ms;
            GetMethodDefRow(table_id, row, out ms, gtparams,
                gmparams);
            return ms;
        }

        public bool GetMethodDefRow(int table_id, int row, 
            out MethodSpec ms, TypeSpec[] gtparams = null,
            TypeSpec[] gmparams = null)
        {
            switch (table_id)
            {
                case (int)TableId.MethodDef:
                    ms = new MethodSpec();
                    ms.m = this;
                    ms.msig = (int)GetIntEntry(table_id, row, 4);
                    ms.mdrow = row;
                    ms.type = null;
                    ms.type = new metadata.TypeSpec
                    {
                        m = ms.m,
                        tdrow = ms.m.methoddef_owners[ms.mdrow]
                    };

                    return true;
                case (int)TableId.MemberRef:
                    {
                        int class_tableid = 0;
                        int class_row = 0;
                        GetCodedIndexEntry(table_id, row, 0,
                            MemberRefParent, out class_tableid, out class_row);
                        var name = GetStringEntry(table_id, row, 1);
                        var sig = GetIntEntry(table_id, row, 2);

                        // determine if referencing a field or method
                        var sig_check = (int)sig;
                        SigReadUSCompressed(ref sig_check);
                        var cc = sh_blob.di.ReadByte(sig_check);
                        bool is_field = false;
                        if (cc == 0x06)
                            is_field = true;

                        switch(class_tableid)
                        {
                            case (int)TableId.MethodDef:
                                throw new NotImplementedException();
                            case (int)TableId.ModuleRef:
                                throw new NotImplementedException();
                            case (int)TableId.TypeDef:
                                throw new NotImplementedException();
                            case (int)TableId.TypeRef:
                                {
                                    TypeSpec ts;
                                    if (GetTypeRefRow(class_row, out ts) == false)
                                        throw new TypeLoadException();

                                    if (is_field)
                                    {
                                        ms = ts.m.GetFieldDefRow(name, ts);
                                        return true;
                                    }
                                    else
                                    {
                                        ms = new MethodSpec();
                                        ms.m = ts.m;
                                        ms.type = ts;
                                        var mdrow = ts.m.GetMethodDefRow(ts, name, (int)sig, this);
                                        ms.mdrow = mdrow;
                                        ms.msig = (int)ts.m.GetIntEntry(tid_MethodDef, mdrow, 4);
                                    }
                                    return true;
                                }
                            case (int)TableId.TypeSpec:
                                {
                                    var sig_idx = (int)GetIntEntry(tid_TypeSpec, class_row, 0);

                                    SigReadUSCompressed(ref sig_idx);

                                    var newts = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                                    switch(newts.stype)
                                    {
                                        case TypeSpec.SpecialType.None:
                                            if (is_field)
                                            {
                                                ms = GetFieldDefRow(name, newts);
                                                return true;
                                            }
                                            else
                                            {
                                                var mdrow = newts.m.GetMethodDefRow(newts, name, (int)sig, this, true, gmparams);
                                                var msig = (int)newts.m.GetIntEntry(tid_MethodDef, mdrow, 4);

                                                ms = new metadata.MethodSpec
                                                {
                                                    m = newts.m,
                                                    mdrow = mdrow,
                                                    msig = msig,
                                                    type = newts
                                                };
                                                return true;
                                            }
                                        case TypeSpec.SpecialType.Array:
                                            if(is_field)
                                            {
                                                throw new NotImplementedException();
                                            }
                                            else
                                            {
                                                ms = new MethodSpec
                                                {
                                                    m = this,
                                                    msig = (int)sig,
                                                    name_override = name,
                                                    type = newts
                                                };
                                                return true;
                                            }
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                throw new NotImplementedException();
                        }
                                                
                        throw new NotImplementedException();
                    }
                case (int)TableId.MethodSpec:
                    {
                        // build new gmparams from current ones
                        var isig_idx = (int)GetIntEntry(tid_MethodSpec,
                            row, 1);
                        SigReadUSCompressed(ref isig_idx);
                        var gi = sh_blob.di.ReadByte(isig_idx++);
                        if (gi != 0x0a)
                            throw new Exception("Invalid MethodSpec signature");
                        var gmcount = SigReadUSCompressed(ref isig_idx);

                        TypeSpec[] new_gmparams = new TypeSpec[gmcount];
                        for(var gm_idx = 0; gm_idx < gmcount; gm_idx++)
                        {
                            var gmts = GetTypeSpec(ref isig_idx, gtparams, gmparams);
                            new_gmparams[gm_idx] = gmts;
                        }                        

                        // now get instantiated method
                        int Method_tid, Method_row;
                        GetCodedIndexEntry(tid_MethodSpec, row, 0,
                            MethodDefOrRef, out Method_tid, out Method_row);
                                         
                        MethodSpec base_method;
                        GetMethodDefRow(Method_tid, Method_row,
                            out base_method, gtparams, new_gmparams);
                        base_method.gmparams = new_gmparams;

                        ms = base_method;
                        return true;
                    }

                case (int)TableId.Field:
                    ms = new MethodSpec
                    {
                        m = this,
                        mdrow = row,
                        msig = (int)GetIntEntry(table_id, row, 2),
                        type = new TypeSpec
                        {
                            m = this,
                            tdrow = fielddef_owners[row]
                        },
                        is_field = true
                    };
                    return true;
            }
            ms = null;
            return false;
        }

        public TypeSpec GetTypeSpec(int table_id, int row,
            TypeSpec[] gtparams = null, TypeSpec[] gmparams = null)
        {
            switch(table_id)
            {
                case tid_TypeDef:
                    var ret = new TypeSpec
                    {
                        m = this,
                        tdrow = row,
                    };
                    if (ret.IsGeneric)
                        ret.gtparams = gtparams;
                    return ret;

                case tid_TypeRef:
                    {
                        int rs_id, rs_row;
                        GetCodedIndexEntry(tid_TypeRef, row, 0, ResolutionScope, out rs_id, out rs_row);
                        var other_name = GetStringEntry(tid_TypeRef, row, 1);
                        var other_namespace = GetStringEntry(tid_TypeRef, row, 2);

                        switch(rs_id)
                        {
                            case tid_AssemblyRef:
                                var other_m = referenced_assemblies[rs_row - 1];

                                /* Is there a typeforwarder for this? */
                                var tforward_key = other_m.AssemblyName + "!" + other_namespace + "." + other_name;
                                if (al.TypeForwarders.ContainsKey(tforward_key))
                                    other_m = al.GetAssembly(al.TypeForwarders[tforward_key]);
                                var other_ts = other_m.GetTypeSpec(other_namespace, other_name);
                                return other_ts;

                            case tid_TypeRef:
                                {
                                    var enc_ts = GetTypeSpec(rs_id, rs_row, gtparams, gmparams);

                                    // find a particular nested type
                                    int cur_nested = enc_ts.m.nested_types[enc_ts.tdrow];
                                    while(cur_nested != 0)
                                    {
                                        // only compare the type name (namespace will be null)
                                        var nested_name = enc_ts.m.GetStringEntry(tid_TypeDef,
                                            cur_nested, 1);
                                        if (nested_name.Equals(other_name))
                                        {
                                            var nested_ts = enc_ts.m.GetTypeSpec(tid_TypeDef,
                                                cur_nested, gtparams, gmparams);
                                            return nested_ts;
                                        }

                                        cur_nested = enc_ts.m.next_nested_type[cur_nested];
                                    }

                                    throw new NotSupportedException();
                                }

                            default:
                                throw new NotImplementedException();
                        }
                    }
                    throw new NotSupportedException();

                case tid_TypeSpec:
                    {
                        var sig = (int)GetIntEntry(tid_TypeSpec, row, 0);
                        SigReadUSCompressed(ref sig);
                        return GetTypeSpec(ref sig, gtparams, gmparams);
                    }

                default:
                    return null;
            }
        }

        public TypeSpec GetSimpleTypeSpec(int stype)
        {
            TypeSpec ts = new TypeSpec();

            switch(stype)
            {
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                case 0x0d:
                case 0x0e:
                case 0x16:
                case 0x18:
                case 0x19:
                case 0x1c:
                    ts.m = al.GetAssembly("mscorlib");
                    ts.tdrow = ts.m.simple_type_rev_idx[stype];
                    break;
                default:
                    throw new NotSupportedException("Unknown simple type: " + stype.ToString());
            }
            return ts;
        }

        public int MajorVersion
        {
            get
            {
                return (int)GetIntEntry(tid_Assembly, 1, 1);
            }
        }

        public int MinorVersion
        {
            get
            {
                return (int)GetIntEntry(tid_Assembly, 1, 2);
            }
        }

        public int BuildVersion
        {
            get
            {
                return (int)GetIntEntry(tid_Assembly, 1, 3);
            }
        }

        public int RevisionVersion
        {
            get
            {
                return (int)GetIntEntry(tid_Assembly, 1, 4);
            }
        }

        public string AssemblyVersionString
        {
            get
            {
                return MajorVersion.ToString() + "." +
                    MinorVersion.ToString() + "." +
                    BuildVersion.ToString() + "." +
                    RevisionVersion.ToString();
            }
        }

        public TypeSpec GetTypeSpec(ref int sig_idx, TypeSpec[] gtparams, TypeSpec[] gmparams)
        {
            TypeSpec ts = new TypeSpec();
            ts.m = this;

            bool is_generic = false;

            var b = sh_blob.di.ReadByte(sig_idx++);

            // CMOD_REQD/OPT
            while (b == 0x1f || b == 0x20)
                b = sh_blob.di.ReadByte(sig_idx++);

            if(b == 0x15)
            {
                is_generic = true;
                b = sh_blob.di.ReadByte(sig_idx++);
            }

            switch (b)
            {
                case 0x01:
                    // VOID
                    return null;
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                case 0x0d:
                case 0x0e:
                case 0x16:
                case 0x18:
                case 0x19:
                case 0x1c:
                    ts.m = al.GetAssembly("mscorlib");
                    ts.tdrow = ts.m.simple_type_rev_idx[b];
                    break;

                case 0x0f:
                    ts.m = al.GetAssembly("mscorlib");
                    ts.stype = TypeSpec.SpecialType.Ptr;
                    ts.other = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                    break;

                case 0x10:
                    ts.m = al.GetAssembly("mscorlib");
                    ts.stype = TypeSpec.SpecialType.MPtr;
                    ts.other = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                    break;

                case 0x11:
                case 0x12:
                case 0x91:
                case 0x92:
                    MetadataStream cur_m = this;
                    if (b > 0x90)
                    {
                        // Tysila specific extensions to metadata where what follows
                        // is encoded length followed by metadata name
                        var mlen = SigReadUSCompressed(ref sig_idx);
                        StringBuilder sb = new StringBuilder();
                        for (uint i = 0; i < mlen; i++)
                            sb.Append((char)sh_blob.di.ReadByte(sig_idx++));

                        cur_m = al.GetAssembly(sb.ToString());
                    }
                    // CLASS or vtype
                    var tok = SigReadUSCompressed(ref sig_idx);
                    int tid, trow;
                    GetCodedIndexEntry(tok, TypeDefOrRef,
                        out tid, out trow);

                    ts = cur_m.GetTypeSpec(tid, trow, gtparams, gmparams);
                    break;

                case 0x13:
                    // VAR
                    var gtidx = SigReadUSCompressed(ref sig_idx);
                    if (gtparams == null)
                        ts = new TypeSpec { stype = TypeSpec.SpecialType.Var, idx = (int)gtidx };
                    else
                        ts = gtparams[gtidx];
                    break;

                case 0x1e:
                    // MVAR
                    var gmidx = SigReadUSCompressed(ref sig_idx);
                    if (gmparams == null)
                        ts = new TypeSpec { stype = TypeSpec.SpecialType.MVar, idx = (int)gmidx };
                    else
                        ts = gmparams[gmidx];
                    break;

                case 0x1d:
                    ts.m = al.GetAssembly("mscorlib");
                    ts.stype = TypeSpec.SpecialType.SzArray;

                    ts.other = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                    break;

                case 0x14:
                    ts.m = al.GetAssembly("mscorlib");
                    ts.stype = TypeSpec.SpecialType.Array;

                    ts.other = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                    ts.arr_rank = (int)SigReadUSCompressed(ref sig_idx);
                    
                    int boundsCount = (int)SigReadUSCompressed(ref sig_idx);
                    ts.arr_sizes = new int[boundsCount];
                    for(int i = 0; i < boundsCount; i++)
                        ts.arr_sizes[i] = (int)SigReadUSCompressed(ref sig_idx);

                    int loCount = (int)SigReadUSCompressed(ref sig_idx);
                    ts.arr_lobounds = new int[loCount];
                    for (int i = 0; i < loCount; i++)
                        ts.arr_lobounds[i] = (int)SigReadUSCompressed(ref sig_idx);

                    break;

                case 0x45:
                    ts = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                    ts.Pinned = true;
                    break;

                default:
                    throw new NotImplementedException();
            }

            if(is_generic)
            {
                var gen_count = SigReadUSCompressed(ref sig_idx);
                ts.gtparams = new TypeSpec[gen_count];
                for(uint i = 0; i < gen_count; i++)
                {
                    ts.gtparams[i] = GetTypeSpec(ref sig_idx, gtparams, gmparams);
                }
            }

            return ts;
        }

        public uint GetLastMethodDef(int tdrow)
        {
            uint last_mdef;
            if (tdrow == table_rows[tid_TypeDef])
                last_mdef = (uint)table_rows[tid_MethodDef] + 1;
            else
                last_mdef = GetIntEntry(tid_TypeDef, tdrow + 1, 5);
            return last_mdef;
        }

        public uint GetLastFieldDef(int tdrow)
        {
            uint last_fdef;
            if (tdrow == table_rows[tid_TypeDef])
                last_fdef = (uint)table_rows[tid_Field] + 1;
            else
                last_fdef = GetIntEntry(tid_TypeDef, tdrow + 1, 4);
            return last_fdef;
        }

        public MethodSpec GetMethodSpec(TypeSpec ts, string name, int sig = 0, MetadataStream sig_m = null, bool throw_on_error = true)
        {
            var first_mdef = ts.m.GetIntEntry(tid_TypeDef, ts.tdrow, 5);
            var last_mdef = ts.m.GetLastMethodDef(ts.tdrow);

            var ret = new MethodSpec { type = ts, m = ts.m };

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var meth_name = ts.m.GetStringEntry(tid_MethodDef, (int)mdef_row, 3);

                if (meth_name.Equals(name))
                {
                    var cur_sig = (int)ts.m.GetIntEntry(tid_MethodDef, (int)mdef_row, 4);

                    if (sig_m == null)
                    {
                        ret.msig = cur_sig;
                        ret.mdrow = (int)mdef_row;
                        ret.type = ts;
                        return ret;
                    }
                    // compare signatures

                    if (CompareSignature(ts.m, cur_sig, ts.gtparams, null,
                        sig_m, sig, ts.gtparams, null))
                    {
                        ret.msig = cur_sig;
                        ret.mdrow = (int)mdef_row;
                        return ret;
                    }
                }
            }

            if (throw_on_error)
            {
                throw new NotImplementedException("cannot find " +
                    sig_m.MangleMethod(ts, name, sig));
            }
            else
                return null;
        }

        public int GetMethodDefRow(TypeSpec ts, string name, int sig, MetadataStream sig_m, bool throw_on_error = true, TypeSpec[] gmparams = null)
        {
            var first_mdef = ts.m.GetIntEntry(tid_TypeDef, ts.tdrow, 5);
            var last_mdef = ts.m.GetLastMethodDef(ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var meth_name = ts.m.GetStringEntry(tid_MethodDef, (int)mdef_row, 3);

                if (meth_name.Equals(name))
                {
                    if (sig_m == null)
                        return (int)mdef_row;
                    // compare signatures
                    var cur_sig = (int)ts.m.GetIntEntry(tid_MethodDef, (int)mdef_row, 4);

                    if (CompareSignature(ts.m, cur_sig, ts.gtparams, gmparams,
                        sig_m, sig, ts.gtparams, gmparams))
                        return (int)mdef_row;
                }
            }

            if (throw_on_error)
            {
                throw new NotImplementedException("cannot find " +
                    sig_m.MangleMethod(ts, name, sig));
            }
            else
                return -1;
        }

        /*private int GetMethodDefRow(int tdrow, string name, int sig, MetadataStream sig_m)
        {
            var first_mdef = GetIntEntry(tid_TypeDef, tdrow, 5);
            var last_mdef = GetLastMethodDef(tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var meth_name = GetStringEntry(tid_MethodDef, (int)mdef_row, 3);

                if (meth_name.Equals(name))
                {
                    // compare signatures
                    var cur_sig = (int)GetIntEntry(tid_MethodDef, (int)mdef_row, 4);

                    if (CompareSignature(this, cur_sig, null, null,
                        sig_m, sig, null, null))
                            return (int)mdef_row;
                }
            }

            throw new NotImplementedException("cannot find " +
                sig_m.MangleMethod(new TypeSpec { tdrow = tdrow, m = this },
                name, sig));

        }*/
        private int GetMethodDefRow(int tdrow, string name)
        {
            throw new NotImplementedException("Deprecated method");

            return 0;
        }

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

        public class CodedIndexTemplate
        {
            public int TagBits;
            public int[] Members;
        }

        public CodedIndexTemplate TypeDefOrRef, HasConstant,
            HasCustomAttribute, HasFieldMarshal, HasDeclSecurity,
            MemberRefParent, HasSemantics, MethodDefOrRef,
            MemberForwarded, Implementation, CustomAttributeType,
            ResolutionScope, TypeOrMethodDef;

        public Dictionary<TableId, int> TableIDs = new Dictionary<TableId, int>(
            new GenericEqualityComparerEnum<TableId>());

        public MetadataStream(string name = null)
        {
            assemblyName = name;

            TableIDs[TableId.NotUsed] = -1;
            TableIDs[TableId.Assembly] = 0x20;
            TableIDs[TableId.AssemblyOS] = 0x22;
            TableIDs[TableId.AssemblyProcessor] = 0x21;
            TableIDs[TableId.AssemblyRef] = 0x23;
            TableIDs[TableId.AssemblyRefOS] = 0x25;
            TableIDs[TableId.AssemblyRefProcessor] = 0x24;
            TableIDs[TableId.ClassLayout] = 0x0f;
            TableIDs[TableId.Constant] = 0x0b;
            TableIDs[TableId.CustomAttribute] = 0x0c;
            TableIDs[TableId.DeclSecurity] = 0x0e;
            TableIDs[TableId.EventMap] = 0x12;
            TableIDs[TableId.Event] = 0x14;
            TableIDs[TableId.ExportedType] = 0x27;
            TableIDs[TableId.Field] = 0x04;
            TableIDs[TableId.FieldLayout] = 0x10;
            TableIDs[TableId.FieldMarshal] = 0x0d;
            TableIDs[TableId.FieldRVA] = 0x1d;
            TableIDs[TableId.File] = 0x26;
            TableIDs[TableId.GenericParam] = 0x2a;
            TableIDs[TableId.GenericParamConstaint] = 0x2c;
            TableIDs[TableId.ImplMap] = 0x1c;
            TableIDs[TableId.InterfaceImpl] = 0x09;
            TableIDs[TableId.ManifestResource] = 0x28;
            TableIDs[TableId.MemberRef] = 0x0a;
            TableIDs[TableId.MethodDef] = 0x06;
            TableIDs[TableId.MethodImpl] = 0x19;
            TableIDs[TableId.MethodSemantics] = 0x18;
            TableIDs[TableId.MethodSpec] = 0x2b;
            TableIDs[TableId.Module] = 0x00;
            TableIDs[TableId.ModuleRef] = 0x1a;
            TableIDs[TableId.NestedClass] = 0x29;
            TableIDs[TableId.Param] = 0x08;
            TableIDs[TableId.Property] = 0x17;
            TableIDs[TableId.PropertyMap] = 0x15;
            TableIDs[TableId.StandAloneSig] = 0x11;
            TableIDs[TableId.TypeDef] = 0x02;
            TableIDs[TableId.TypeRef] = 0x01;
            TableIDs[TableId.TypeSpec] = 0x1b;

            TypeDefOrRef = new CodedIndexTemplate
            {
                TagBits = 2,
                Members = new int[]
                {
                    TableIDs[TableId.TypeDef],
                    TableIDs[TableId.TypeRef],
                    TableIDs[TableId.TypeSpec]
                }
            };

            HasConstant = new CodedIndexTemplate
            {
                TagBits = 2,
                Members = new int[]
                {
                    TableIDs[TableId.Field],
                    TableIDs[TableId.Param],
                    TableIDs[TableId.Property]
                }
            };

            HasCustomAttribute = new CodedIndexTemplate
            {
                TagBits = 5,
                Members = new int[]
                {
                    TableIDs[TableId.MethodDef],
                    TableIDs[TableId.Field],
                    TableIDs[TableId.TypeRef],
                    TableIDs[TableId.TypeDef],
                    TableIDs[TableId.Param],
                    TableIDs[TableId.InterfaceImpl],
                    TableIDs[TableId.MemberRef],
                    TableIDs[TableId.Module],
                    TableIDs[TableId.DeclSecurity],
                    TableIDs[TableId.Property],
                    TableIDs[TableId.Event],
                    TableIDs[TableId.StandAloneSig],
                    TableIDs[TableId.ModuleRef],
                    TableIDs[TableId.TypeSpec],
                    TableIDs[TableId.Assembly],
                    TableIDs[TableId.AssemblyRef],
                    TableIDs[TableId.File],
                    TableIDs[TableId.ExportedType],
                    TableIDs[TableId.ManifestResource],
                    TableIDs[TableId.GenericParam],
                    TableIDs[TableId.GenericParamConstaint],
                    TableIDs[TableId.MethodSpec]
                }
            };

            HasFieldMarshal = new CodedIndexTemplate
            {
                TagBits = 1,
                Members = new int[]
                {
                    TableIDs[TableId.Field],
                    TableIDs[TableId.Param]
                }
            };

            HasDeclSecurity = new CodedIndexTemplate
            {
                TagBits = 2,
                Members = new int[]
                {
                    TableIDs[TableId.TypeDef],
                    TableIDs[TableId.MethodDef],
                    TableIDs[TableId.Assembly]
                }
            };

            MemberRefParent = new CodedIndexTemplate
            {
                TagBits = 3,
                Members = new int[]
                {
                    TableIDs[TableId.TypeDef],
                    TableIDs[TableId.TypeRef],
                    TableIDs[TableId.ModuleRef],
                    TableIDs[TableId.MethodDef],
                    TableIDs[TableId.TypeSpec]
                }
            };

            HasSemantics = new CodedIndexTemplate
            {
                TagBits = 1,
                Members = new int[]
                {
                    TableIDs[TableId.Event],
                    TableIDs[TableId.Property]
                }
            };

            MethodDefOrRef = new CodedIndexTemplate
            {
                TagBits = 1,
                Members = new int[]
                {
                    TableIDs[TableId.MethodDef],
                    TableIDs[TableId.MemberRef]
                }
            };

            MemberForwarded = new CodedIndexTemplate
            {
                TagBits = 1,
                Members = new int[]
                {
                    TableIDs[TableId.Field],
                    TableIDs[TableId.MethodDef]
                }
            };

            Implementation = new CodedIndexTemplate
            {
                TagBits = 2,
                Members = new int[]
                {
                    TableIDs[TableId.File],
                    TableIDs[TableId.AssemblyRef],
                    TableIDs[TableId.ExportedType]
                }
            };

            CustomAttributeType = new CodedIndexTemplate
            {
                TagBits = 3,
                Members = new int[]
                {
                    TableIDs[TableId.NotUsed],
                    TableIDs[TableId.NotUsed],
                    TableIDs[TableId.MethodDef],
                    TableIDs[TableId.MemberRef],
                    TableIDs[TableId.NotUsed]
                }
            };

            ResolutionScope = new CodedIndexTemplate
            {
                TagBits = 2,
                Members = new int[]
                {
                    TableIDs[TableId.Module],
                    TableIDs[TableId.ModuleRef],
                    TableIDs[TableId.AssemblyRef],
                    TableIDs[TableId.TypeRef]
                }
            };

            TypeOrMethodDef = new CodedIndexTemplate
            {
                TagBits = 1,
                Members = new int[]
                {
                    TableIDs[TableId.TypeDef],
                    TableIDs[TableId.MethodDef]
                }
            };
        }

        /**<summary>Patch up field rvas</summary> */
        internal void PatchFieldRVAs()
        {
            fieldrvas = new uint[table_rows[tid_Field] + 1];

            for (int i = 1; i <= (table_rows[tid_FieldRVA]); i++)
            {
                var rva = GetIntEntry(tid_FieldRVA, i, 0);
                var field = GetIntEntry(tid_FieldRVA, i, 1);

                fieldrvas[field] = rva;
            }
        }

        internal void PatchFieldConstants()
        {
            const_field_owners = new int[table_rows[tid_Field] + 1];

            for (int i = 1; i <= table_rows[tid_Constant]; i++)
            {
                int parent_tid, parent_row;
                GetCodedIndexEntry(tid_Constant,
                    i, 1, HasConstant, out parent_tid, out parent_row);

                if (parent_tid == tid_Field)
                    const_field_owners[parent_row] = i;
            }

        }

        /**<summary>Patch up class layouts</summary> */
        internal void PatchClassLayouts()
        {
            classlayouts = new int[table_rows[tid_TypeDef] + 1];

            for(int i = 1; i <= table_rows[tid_ClassLayout]; i++)
            {
                var tdrow = GetIntEntry(tid_ClassLayout, i, 2);
                classlayouts[tdrow] = i;
            }
        }

        /**<summary>Patch up generic types</summary> */
        internal void PatchGTypes()
        {
            gtparams = new int[table_rows[tid_TypeDef] + 1];
            gmparams = new int[table_rows[tid_MethodDef] + 1];

            for (int i = 1; i <= (table_rows[tid_GenericParam]); i++)
            {
                int tid, row;
                GetCodedIndexEntry(tid_GenericParam, i, 2,
                    TypeOrMethodDef, out tid, out row);

                if (tid == tid_TypeDef)
                    gtparams[row] = gtparams[row] + 1;
                else if (tid == tid_MethodDef)
                    gmparams[row] = gmparams[row] + 1;
                else
                    throw new NotSupportedException();
            }
        }

        /**<summary>Patch up field def owners</summary> */
        internal void PatchFieldDefOwners()
        {
            fielddef_owners = new int[table_rows[tid_Field] + 1];

            uint prev_start = 0;
            for (int i = 1; i <= (table_rows[tid_TypeDef] + 1); i++)
            {
                uint cur_start;
                if (i == table_rows[tid_TypeDef] + 1)
                    cur_start = (uint)table_rows[tid_Field] + 1;
                else
                    cur_start = GetIntEntry(tid_TypeDef, i, 4);
                if (i != 0)
                {
                    for (uint j = prev_start; j < cur_start; j++)
                        fielddef_owners[j] = i - 1;
                }
                prev_start = cur_start;
            }
        }

        /**<summary>Patch up method def owners</summary> */
        internal void PatchMethodDefOwners()
        {
            methoddef_owners = new int[table_rows[tid_MethodDef] + 1];

            uint prev_start = 0;
            for (int i = 1; i <= (table_rows[tid_TypeDef] + 1); i++)
            {
                uint cur_start;
                if (i == table_rows[tid_TypeDef] + 1)
                    cur_start = (uint)table_rows[tid_MethodDef] + 1;
                else
                    cur_start = GetIntEntry(tid_TypeDef, i, 5);
                if (i != 0)
                {
                    for (uint j = prev_start; j < cur_start; j++)
                        methoddef_owners[j] = i - 1;
                }
                prev_start = cur_start;
            }
        }

        internal void PatchNestedTypes()
        {
            nested_types = new int[table_rows[tid_TypeDef] + 1];
            next_nested_type = new int[table_rows[tid_TypeDef] + 1];
            nested_parent = new int[table_rows[tid_TypeDef] + 1];

            for (int i = 1; i <= table_rows[tid_NestedClass]; i++)
            {
                var NestedClass = (int)GetIntEntry(tid_NestedClass, i, 0);
                var EnclosingClass = (int)GetIntEntry(tid_NestedClass, i, 1);

                next_nested_type[NestedClass] = nested_types[EnclosingClass];
                nested_types[EnclosingClass] = NestedClass;
                nested_parent[NestedClass] = EnclosingClass;
            }
        }

        internal void PatchCustomAttrs()
        {
            td_custom_attrs = new int[table_rows[tid_TypeDef] + 1];
            md_custom_attrs = new int[table_rows[tid_MethodDef] + 1];
            fd_custom_attrs = new int[table_rows[tid_Field] + 1];
            td_extends_override = new string[table_rows[tid_TypeDef] + 1];
            next_ca = new int[table_rows[tid_CustomAttribute] + 1];
            thread_local_fields = new bool[table_rows[tid_Field] + 1];

            for(int i = 0; i <= table_rows[tid_CustomAttribute]; i++)
            {
                int parent_tid, parent_row, type_tid, type_row;
                GetCodedIndexEntry(tid_CustomAttribute, i, 0,
                    HasCustomAttribute, out parent_tid,
                    out parent_row);

                switch(parent_tid)
                {
                    case tid_TypeDef:
                        next_ca[i] = td_custom_attrs[parent_row];
                        td_custom_attrs[parent_row] = i;

                        if (assemblyName != "mscorlib")
                        {
                            // Determine if this is a typedef extends override
                            GetCodedIndexEntry(tid_CustomAttribute,
                                i, 1, CustomAttributeType, out type_tid,
                                out type_row);

                            MethodSpec ca_ms;
                            GetMethodDefRow(type_tid, type_row, out ca_ms);
                            var ca_ms_name = ca_ms.MangleMethod();

                            if (ca_ms_name == "_ZN14libsupcs#2Edll8libsupcs20NoBaseClassAttribute_7#2Ector_Rv_P1u1t")
                            {
                                td_extends_override[parent_row] = "";
                            }
                            else if (ca_ms_name == "_ZN14libsupcs#2Edll8libsupcs24ExtendsOverrideAttribute_7#2Ector_Rv_P2u1tu1S")
                            {
                                var sig_idx = GetCustomAttrSigIdx(i);
                                var extends_name = ReadCustomAttrString(ref sig_idx);
                                td_extends_override[parent_row] = extends_name;
                            }
                        }

                        break;
                    case tid_MethodDef:
                        next_ca[i] = md_custom_attrs[parent_row];
                        md_custom_attrs[parent_row] = i;
                        break;
                    case tid_Field:
                        next_ca[i] = fd_custom_attrs[parent_row];
                        fd_custom_attrs[parent_row] = i;

                        // Determine if this is a thread static attribute
                        if (assemblyName != "mscorlib")
                        {
                            GetCodedIndexEntry(tid_CustomAttribute,
                                i, 1, CustomAttributeType, out type_tid,
                                out type_row);

                            MethodSpec ca_ms;
                            GetMethodDefRow(type_tid, type_row, out ca_ms);
                            var ca_ms_name = ca_ms.MangleMethod();

                            if (ca_ms_name == "_ZW6System21ThreadStaticAttribute_7#2Ector_Rv_P1u1t")
                            {
                                thread_local_fields[parent_row] = true;
                            }
                        }

                        break;
                }
            }
        }

        /**<summary>Patch up simple types</summary> */
        internal void PatchSimpleTypes()
        {
            simple_type_idx = new int[table_rows[tid_TypeDef] + 1];
            simple_type_rev_idx = new int[256];
            for (int i = 1; i <= table_rows[tid_TypeDef]; i++)
            {
                var nspace = GetStringEntry(tid_TypeDef, i, 2);
                if (nspace == "System")
                {
                    var typename = GetStringEntry(tid_TypeDef, i, 1);

                    if (typename == "Boolean")
                        simple_type_idx[i] = 0x02;
                    else if (typename == "Char")
                        simple_type_idx[i] = 0x03;
                    else if (typename == "SByte")
                        simple_type_idx[i] = 0x04;
                    else if (typename == "Byte")
                        simple_type_idx[i] = 0x05;
                    else if (typename == "Int16")
                        simple_type_idx[i] = 0x06;
                    else if (typename == "UInt16")
                        simple_type_idx[i] = 0x07;
                    else if (typename == "Int32")
                        simple_type_idx[i] = 0x08;
                    else if (typename == "UInt32")
                        simple_type_idx[i] = 0x09;
                    else if (typename == "Int64")
                        simple_type_idx[i] = 0x0a;
                    else if (typename == "UInt64")
                        simple_type_idx[i] = 0x0b;
                    else if (typename == "Single")
                        simple_type_idx[i] = 0x0c;
                    else if (typename == "Double")
                        simple_type_idx[i] = 0x0d;
                    else if (typename == "String")
                        simple_type_idx[i] = 0x0e;
                    else if (typename == "TypedReference")
                        simple_type_idx[i] = 0x16;
                    else if (typename == "IntPtr")
                        simple_type_idx[i] = 0x18;
                    else if (typename == "UIntPtr")
                        simple_type_idx[i] = 0x19;
                    else if (typename == "Object")
                        simple_type_idx[i] = 0x1c;
                    else if (typename == "ValueType")
                        simple_type_idx[i] = 0x11;
                    else
                        simple_type_idx[i] = -1;
                }
                else simple_type_idx[i] = -1;

                if (simple_type_idx[i] != -1)
                    simple_type_rev_idx[simple_type_idx[i]] = i;
            }
        }

        public IEnumerable<string> GetFieldAliases(int fdrow)
        {
            if (fd_custom_attrs == null)
                yield break;

            int cur_ca = fd_custom_attrs[fdrow];

            while(cur_ca != 0)
            {
                int type_tid, type_row;
                GetCodedIndexEntry(tid_CustomAttribute,
                    cur_ca, 1, CustomAttributeType, out type_tid,
                    out type_row);

                MethodSpec ca_ms;
                GetMethodDefRow(type_tid, type_row, out ca_ms);
                var ca_ms_name = ca_ms.MangleMethod();

                if (ca_ms_name == "_ZN14libsupcs#2Edll8libsupcs19FieldAliasAttribute_7#2Ector_Rv_P2u1tu1S")
                {
                    int val_idx = (int)GetIntEntry(MetadataStream.tid_CustomAttribute,
                        cur_ca, 2);

                    SigReadUSCompressed(ref val_idx);
                    var prolog = sh_blob.di.ReadUShort(val_idx);
                    if (prolog == 0x0001)
                    {
                        val_idx += 2;

                        var str_len = SigReadUSCompressed(ref val_idx);
                        StringBuilder sb = new StringBuilder();
                        for (uint i = 0; i < str_len; i++)
                        {
                            sb.Append((char)sh_blob.di.ReadByte(val_idx++));
                        }
                        yield return sb.ToString();
                    }
                }

                cur_ca = next_ca[cur_ca];
            }

            yield break;
        }
    }
}
