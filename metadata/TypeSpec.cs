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
    /**<summary>Base Spec class for TypeSpec and MethodSpec to help
     * the requestor exclude those methods/types not in the
     * current assembly</summary> */
    public abstract class Spec
    {
        public abstract MetadataStream Metadata { get; }

        public abstract bool IsInstantiatedGenericType { get; }
        public abstract bool IsInstantiatedGenericMethod { get; }
        public abstract bool IsArray { get; }
        public abstract string Name { get; }

        public class FullySpecSignature
        {
            public List<byte> Signature;
            public List<MetadataStream> Modules;
            public Spec OriginalSpec;
        }

        public abstract IEnumerable<int> CustomAttributes(string ctor = null);

        public IList<string> CustomAttributeNames
        {
            get
            {
                List<string> ret = new List<string>();
                foreach(var idx in CustomAttributes())
                {
                    int type_tid, type_row;
                    Metadata.GetCodedIndexEntry(MetadataStream.tid_CustomAttribute,
                        idx, 1, Metadata.CustomAttributeType, out type_tid,
                        out type_row);

                    MethodSpec ca_ms;
                    Metadata.GetMethodDefRow(type_tid, type_row, out ca_ms);
                    var ca_ms_name = ca_ms.MangleMethod();

                    ret.Add(ca_ms_name);
                }
                return ret;
            }
        }
    }

    /**<summary>Represents a generic way of specifying all types
    (whether generic or not)</summary>*/
    public class TypeSpec : Spec, IEquatable<TypeSpec>
    {
        public metadata.MetadataStream m;
        public int tdrow;

        public TypeSpec[] gtparams;

        public enum SpecialType { None, SzArray, Array, Ptr, MPtr, Var, MVar, Boxed };
        public int idx; // var/mvar index for uninstantiated generic types/methods
        public SpecialType stype;

        /* array/szarray/ptr/mptr base type */
        public TypeSpec other;

        /* array shape */
        public int arr_rank;
        public int[] arr_sizes;
        public int[] arr_lobounds;

        public bool Pinned;

        public override MetadataStream Metadata
        { get { return m; } }

        public int SimpleType
        {
            get
            {
                if (m.simple_type_idx == null)
                    return 0;
                var ret = m.simple_type_idx[tdrow];
                if (ret <= 0)
                    return 0;
                return ret;
            }
        }

        public int ElemType
        {
            get
            {
                var stype = SimpleType;
                if (stype != 0)
                    return stype;

                if (IsValueType)
                    return 0x11;

                return 0x12;
            }
        }

        public TypeSpec GetExtends()
        {
            switch(stype)
            {
                case SpecialType.Array:
                case SpecialType.SzArray:
                    return m.SystemArray;

                case SpecialType.Boxed:
                    return m.SystemValueType;
            }

            if(m.td_extends_override[tdrow] != null)
            {
                if (m.td_extends_override[tdrow] == "")
                    return null;
                else
                    return m.DemangleType(m.td_extends_override[tdrow]);
            }

            int table_id, etdrow;
            m.GetCodedIndexEntry(MetadataStream.tid_TypeDef,
                tdrow, 3, m.TypeDefOrRef, out table_id,
                out etdrow);

            if (etdrow == 0)
                return null;

            var ret = m.GetTypeSpec(table_id, etdrow, gtparams, null);

            return ret;
        }

        /**<summary>Is the type a boxed  value type?</summary> */
        public bool IsBoxed
        {
            get
            {
                return stype == SpecialType.Boxed;
            }
        }

        /**<summary>Is the type a descendent of System.ValueType?</summary> */
        public bool IsValueType
        {
            get
            {
                switch (stype)
                {
                    case SpecialType.Array:
                    case SpecialType.Boxed:
                    case SpecialType.SzArray:
                        return false;
                    case SpecialType.MPtr:
                    case SpecialType.Ptr:
                        return true;
                }
                var extends = GetExtends();
                if (extends == null)
                    return false;
                if (extends.Equals(m.SystemEnum))
                    return true;
                if (this.Equals(m.SystemEnum))
                    return false;
                if (extends.m.simple_type_idx == null)
                    return false;
                return (extends.m.simple_type_idx[extends.tdrow] == 0x11);
            }
        }

        /**<summary>Is this a superclass of another type?</summary> */
        public bool IsSuperclassOf(TypeSpec other)
        {
            if (stype == SpecialType.SzArray && other.stype == SpecialType.SzArray)
                return this.other.IsSuperclassOf(other.other);
            if(SimpleType == 0x1c)
            {
                if (other.stype == SpecialType.Array ||
                    other.stype == SpecialType.SzArray)
                    return true;
            }

            var other_extends = other.GetExtends();

            if (other_extends == null)
                return false;
            if (other_extends.Equals(this))
                return true;
            return IsSuperclassOf(other_extends);
        }

        /**<summary>Is this a subclass of another type?</summary> */
        public bool IsSubclassOf(TypeSpec other)
        {
            return other.IsSuperclassOf(this);
        }

        /**<summary>Convert to a managed pointer</summary> */
        public TypeSpec ManagedPointer
        {
            get
            {
                return new TypeSpec { m = m, stype = SpecialType.MPtr, other = this };
            }
        }

        /**<summary>Convert to an unmanaged pointer</summary> */
        public TypeSpec Pointer
        {
            get
            {
                return new TypeSpec { m = m, stype = SpecialType.Ptr, other = this };
            }
        }

        /**<summary>Convert to a zero-based array</summary> */
        public TypeSpec SzArray
        {
            get
            {
                return new TypeSpec { m = m, stype = SpecialType.SzArray, other = this };
            }
        }

        /**<summary>Convert to a boxed instance if this is a value type</summary> */
        public TypeSpec Box
        {
            get
            {
                if (!IsValueType)
                    return this;
                return new TypeSpec { m = m, stype = SpecialType.Boxed, other = this };
            }
        }

        /**<summary>Unbox if this is a boxed value type</summary> */
        public TypeSpec Unbox
        {
            get
            {
                if (stype != SpecialType.Boxed)
                    return this;
                return other;
            }
        }

        List<TypeSpec> ii = null;
        /**<summary>Return a list of all implemented interfaces</summary> */
        public List<TypeSpec> ImplementedInterfaces
        {
            get
            {
                if (ii != null)
                    return ii;

                ii = new List<TypeSpec>();
                
                for(int i = 1; i <= m.table_rows[MetadataStream.tid_InterfaceImpl]; i++)
                {
                    var Class = m.GetIntEntry(MetadataStream.tid_InterfaceImpl, i, 0);
                    if(Class == tdrow)
                    {
                        int ref_id, ref_row;
                        m.GetCodedIndexEntry(MetadataStream.tid_InterfaceImpl,
                            i, 1, m.TypeDefOrRef, out ref_id, out ref_row);

                        var iface = m.GetTypeSpec(ref_id, ref_row, gtparams);
                        ii.Add(iface);
                    }
                }

                return ii;
            }
        }

        public bool IsSigned
        {
            get
            {
                if(stype == SpecialType.None)
                {
                    switch(SimpleType)
                    {
                        case 0x04:
                        case 0x06:
                        case 0x08:
                        case 0x0a:
                        case 0x18:
                            return true;
                    }
                }
                return false;
            }
        }

        public string MangleType()
        {
            return m.MangleType(this);
        }

        public override bool IsInstantiatedGenericType
        {
            get
            {
                if (stype == SpecialType.Boxed)
                    return other.IsInstantiatedGenericType;
                if (IsGeneric && !IsGenericTemplate)
                    return true;
                return false;
            }
        }

        public override bool IsInstantiatedGenericMethod
        {
            get
            {
                return false;
            }
        }

        public bool IsGeneric
        {
            get
            {
                if (m.gtparams == null)
                    return false;
                return m.gtparams[tdrow] != 0;
            }
        }

        public override bool IsArray
        {
            get
            {
                if (stype == SpecialType.Array ||
                    stype == SpecialType.SzArray)
                    return true;
                return false;
            }
        }

        public bool IsGenericTemplate
        {
            get
            {
                if (!IsGeneric)
                    return false;
                return gtparams == null;
            }
        }

        public FullySpecSignature Signature
        {
            get
            {
                List<byte> sig = new List<byte>();
                List<MetadataStream> mods = new List<MetadataStream>();
                AddSignature(sig, mods);
                return new FullySpecSignature
                {
                    Modules = mods,
                    Signature = sig,
                    OriginalSpec = this
                };
            }
        }

        public bool IsInterface
        {
            get
            {
                switch(stype)
                {
                    case SpecialType.None:
                        var flags = m.GetIntEntry(MetadataStream.tid_TypeDef,
                            tdrow, 0);
                        return (flags & 0x20) == 0x20;

                    default:
                        return false;
                }                
            }
        }

        public TypeSpec ReducedType
        {
            get
            {
                var t = UnderlyingType;
                if (t.stype != SpecialType.None)
                    return this;

                switch(t.SimpleType)
                {
                    case 0x04:
                    case 0x05:
                        return m.SystemInt8;
                    case 0x06:
                    case 0x07:
                        return m.SystemInt16;
                    case 0x08:
                    case 0x09:
                        return m.SystemInt32;
                    case 0x0a:
                    case 0x0b:
                        return m.SystemInt64;
                    case 0x18:
                    case 0x19:
                        return m.SystemIntPtr;
                }
                return this;
            }
        }

        public TypeSpec VerificationType
        {
            get
            {
                switch(ReducedType.SimpleType)
                {
                    case 0x02:
                    case 0x04:
                        return m.SystemInt8;
                    case 0x03:
                    case 0x06:
                        return m.SystemInt16;
                    case 0x08:
                        return m.SystemInt32;
                    case 0x0a:
                        return m.SystemInt64;
                    case 0x18:
                        return m.SystemIntPtr;
                }
                if(stype == SpecialType.MPtr)
                {
                    switch(other.ReducedType.SimpleType)
                    {
                        case 0x02:
                        case 0x04:
                            return new TypeSpec { m = m.SystemIntPtr.Type.m, stype = SpecialType.MPtr, other = m.SystemInt8 };
                        case 0x03:
                        case 0x06:
                            return new TypeSpec { m = m.SystemIntPtr.Type.m, stype = SpecialType.MPtr, other = m.SystemInt16 };
                        case 0x08:
                            return new TypeSpec { m = m.SystemIntPtr.Type.m, stype = SpecialType.MPtr, other = m.SystemInt32 };
                        case 0x0a:
                            return new TypeSpec { m = m.SystemIntPtr.Type.m, stype = SpecialType.MPtr, other = m.SystemInt64 };
                        case 0x18:
                            return new TypeSpec { m = m.SystemIntPtr.Type.m, stype = SpecialType.MPtr, other = m.SystemIntPtr };
                    }
                }
                return this;
            }
        }

        public TypeSpec IntermediateType
        {
            get
            {
                var v = VerificationType;
                if(v.stype == SpecialType.None)
                {
                    switch(v.SimpleType)
                    {
                        case 0x04:
                        case 0x06:
                        case 0x08:
                            return m.SystemInt32;
                        case 0x0c:
                        case 0x0d:
                            return m.GetSimpleTypeSpec(0x0d);
                    }
                }
                return v;
            }
        }

        public bool IsEnum
        {
            get
            {
                if (stype != SpecialType.None)
                    return false;
                var e = GetExtends();
                if (e == null)
                    return false;
                return e.Equals(m.SystemEnum);
            }
        }

        public bool IsDelegate
        {
            get
            {
                if (stype != SpecialType.None)
                    return false;
                var flags = m.GetIntEntry(MetadataStream.tid_TypeDef,
                    tdrow, 0);
                if ((flags & 0x100) != 0x100)
                    return false;

                var e = GetExtends();
                while(e != null)
                {
                    if (e.Equals(m.SystemDelegate))
                        return true;
                    e = e.GetExtends();
                }
                return false;
            }
        }

        public TypeSpec UnderlyingType
        {
            get
            {
                if(stype == SpecialType.None)
                {
                    var e = GetExtends();
                    if (e == null)
                        return this;
                    if (e.Equals(m.SystemEnum))
                    {
                        var field_idx = (int)m.GetIntEntry(MetadataStream.tid_TypeDef,
                            tdrow, 4);

                        // find first instance field
                        while (true)
                        {
                            var fflags = m.GetIntEntry(MetadataStream.tid_Field, field_idx,
                                0);
                            if ((fflags & 0x10) == 0)
                            {
                                var fsig = (int)m.GetIntEntry(MetadataStream.tid_Field, field_idx,
                                    2);
                                m.SigReadUSCompressed(ref fsig);
                                fsig++;
                                var value_ts = m.GetTypeSpec(ref fsig, gtparams, null);
                                return value_ts;
                            }
                            field_idx++;
                        }
                    }
                }
                return this;
            }
        }

        public void AddSignature(List<byte> sig, List<MetadataStream> mods)
        {
            switch(stype)
            {
                case SpecialType.None:
                    {
                        bool has_gtparams = true;
                        if (gtparams == null || gtparams.Length == 0)
                            has_gtparams = false;

                        if (has_gtparams)
                            sig.Add(0x15);

                        // emit as simple typedef signature
                        var simple = SimpleType;
                        if(simple != 0)
                        {
                            sig.Add((byte)simple);
                        }
                        else
                        {
                            if (IsValueType)
                                sig.Add(0x31);
                            else
                                sig.Add(0x32);
                            uint mod_tok = (uint)GetModIdx(m, mods);
                            sig.AddRange(MetadataStream.SigWriteUSCompressed(mod_tok));
                            uint tok = m.MakeCodedIndexEntry(MetadataStream.tid_TypeDef,
                                tdrow, m.TypeDefOrRef);
                            sig.AddRange(MetadataStream.SigWriteUSCompressed(tok));
                        }

                        if(has_gtparams)
                        {
                            sig.AddRange(MetadataStream.SigWriteUSCompressed((uint)gtparams.Length));
                            foreach(var x in gtparams)
                                x.AddSignature(sig, mods);
                        }
                    }
                    break;

                case SpecialType.SzArray:
                    sig.Add(0x1d);
                    other.AddSignature(sig, mods);
                    break;

                case SpecialType.Boxed:
                    other.AddSignature(sig, mods);
                    break;

                case SpecialType.Array:
                    sig.Add(0x14);
                    other.AddSignature(sig, mods);
                    sig.AddRange(MetadataStream.SigWriteUSCompressed((uint)arr_rank));
                    sig.AddRange(MetadataStream.SigWriteUSCompressed((uint)arr_sizes.Length));
                    foreach(var item in arr_sizes)
                        sig.AddRange(MetadataStream.SigWriteUSCompressed((uint)item));
                    sig.AddRange(MetadataStream.SigWriteUSCompressed((uint)arr_lobounds.Length));
                    foreach (var item in arr_lobounds)
                        sig.AddRange(MetadataStream.SigWriteUSCompressed((uint)item));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private int GetModIdx(MetadataStream m, List<MetadataStream> mods)
        {
            for(int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Equals(m))
                    return i;
            }
            var ret = mods.Count;
            mods.Add(m);
            return ret;
        }

        public bool Equals(TypeSpec other)
        {
            if (stype != other.stype)
                return false;

            if (!m.Equals(other.m))
                return false;

            switch(stype)
            {
                case SpecialType.None:
                    if (tdrow != other.tdrow)
                        return false;
                    if (gtparams == null && other.gtparams == null)
                        return true;
                    if (gtparams == null)
                        return false;
                    if (other.gtparams == null)
                        return false;
                    if (gtparams.Length != other.gtparams.Length)
                        return false;
                    for(int i = 0; i < gtparams.Length; i++)
                    {
                        if (!gtparams[i].Equals(other.gtparams[i]))
                            return false;
                    }
                    return true;

                case SpecialType.Var:
                case SpecialType.MVar:
                    return idx == other.idx;

                case SpecialType.Ptr:
                case SpecialType.MPtr:
                case SpecialType.Boxed:
                    if (this.other == null && other.other == null)
                        return true;
                    if (this.other == null)
                        return false;
                    return this.other.Equals(other.other);

                case SpecialType.SzArray:
                    return this.other.Equals(other.other);

                case SpecialType.Array:
                    if (!this.other.Equals(other.other))
                        return false;
                    if (this.arr_rank != other.arr_rank)
                        return false;
                    if (this.arr_lobounds.Length != other.arr_lobounds.Length)
                        return false;
                    if (this.arr_sizes.Length != other.arr_sizes.Length)
                        return false;
                    for(int i = 0; i < this.arr_lobounds.Length; i++)
                    {
                        if (this.arr_lobounds[i] != other.arr_lobounds[i])
                            return false;
                    }
                    for (int i = 0; i < this.arr_sizes.Length; i++)
                    {
                        if (this.arr_sizes[i] != other.arr_sizes[i])
                            return false;
                    }
                    return true;

                default:
                    throw new NotImplementedException();
            }
        }

        public override int GetHashCode()
        {
            int hc = stype.GetHashCode();
            hc <<= 1;
            hc ^= tdrow.GetHashCode();
            if(other != null)
            {
                hc <<= 1;
                hc ^= other.GetHashCode();
            }
            hc <<= 1;
            hc ^= arr_rank.GetHashCode();
            hc <<= 1;
            if (arr_sizes != null)
                hc ^= arr_sizes.Length.GetHashCode();
            hc <<= 1;
            if (arr_lobounds != null)
                hc ^= arr_lobounds.Length.GetHashCode();
            hc <<= 1;
            hc ^= idx;

            hc <<= 1;
            if(gtparams != null)
            {
                hc ^= gtparams.Length.GetHashCode();
                foreach(var gtparam in gtparams)
                {
                    hc <<= 1;
                    hc ^= gtparam.GetHashCode();
                }
            }

            return hc;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TypeSpec);
        }

        public override string ToString()
        {
            if (m == null)
                return "TypeSpec";
            return m.MangleType(this);
        }

        public bool IsVerifierAssignableTo(TypeSpec u)
        {
            // CIL III 1.8.1.2.3
            if (Equals(u))
                return true;
            if (IsAssignableTo(u))
                return true;


            // TODO: more required here
            return false;
        }

        public bool IsAssignableTo(TypeSpec u)
        {
            if (Equals(u))
                return true;
            var v = IntermediateType;
            var w = u.IntermediateType;

            if (v.Equals(w))
                return true;

            if (v.Equals(m.SystemInt32) && w.Equals(m.SystemIntPtr))
                return true;
            if (w.Equals(m.SystemInt32) && v.Equals(m.SystemIntPtr))
                return true;

            return IsAssignmentCompatibleWith(u);
        }

        public bool IsAssignmentCompatibleWith(TypeSpec to_type)
        {
            // CIL 8.7.1
            if (Equals(to_type))
                return true;
            if (IsSubclassOf(to_type))
                return true;
            // TODO: does this implement interface to_type?
            if (stype == SpecialType.SzArray &&
                to_type.stype == SpecialType.SzArray &&
                other.IsArrayElementCompatibleWith(to_type.other))
                return true;
            // TODO: if this is SzArray<V> and to_type is IList<W> and V.IsArrayElementCompatible(W)

            return false;
        }

        private bool IsArrayElementCompatibleWith(TypeSpec w)
        {
            var v = this.UnderlyingType;
            w = w.UnderlyingType;
            if (IsAssignmentCompatibleWith(w))
                return true;
            return VerificationType.Equals(w.VerificationType);
        }

        public bool HasCustomAttribute(string ctor)
        {
            int cur_ca = m.td_custom_attrs[tdrow];

            while (cur_ca != 0)
            {
                int type_tid, type_row;
                m.GetCodedIndexEntry(MetadataStream.tid_CustomAttribute,
                    cur_ca, 1, m.CustomAttributeType, out type_tid,
                    out type_row);

                MethodSpec ca_ms;
                m.GetMethodDefRow(type_tid, type_row, out ca_ms);
                var ca_ms_name = ca_ms.MangleMethod();

                if (ca_ms_name.Equals(ctor))
                    return true;

                cur_ca = m.next_ca[cur_ca];
            }

            return false;
        }

        public override IEnumerable<int> CustomAttributes(string ctor = null)
        {
            int cur_ca = m.td_custom_attrs[tdrow];

            while (cur_ca != 0)
            {
                if (ctor == null)
                    yield return cur_ca;
                else
                {
                    int type_tid, type_row;
                    m.GetCodedIndexEntry(MetadataStream.tid_CustomAttribute,
                        cur_ca, 1, m.CustomAttributeType, out type_tid,
                        out type_row);

                    MethodSpec ca_ms;
                    m.GetMethodDefRow(type_tid, type_row, out ca_ms);
                    var ca_ms_name = ca_ms.MangleMethod();

                    if (ca_ms_name.Equals(ctor))
                        yield return cur_ca;
                }

                cur_ca = m.next_ca[cur_ca];
            }

            yield break;
        }

        public override string Name
        {
            get
            {
                return m.GetStringEntry(MetadataStream.tid_TypeDef,
                    tdrow, 1);
            }
        }
    }
}
