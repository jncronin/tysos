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
    }

    /**<summary>Represents a generic way of specifying all types
    (whether generic or not)</summary>*/
    public class TypeSpec : Spec, IEquatable<TypeSpec>
    {
        public metadata.MetadataStream m;
        public int tdrow;

        public TypeSpec[] gtparams;

        public enum SpecialType { None, SzArray, Array, Ptr, MPtr, Var, MVar };
        public int idx; // var/mvar index for uninstantiated generic types/methods
        public SpecialType stype;

        /* array/szarray/ptr/mptr base type */
        public TypeSpec other;

        /* array shape */
        public int arr_rank;
        public int[] arr_sizes;
        public int[] arr_lobounds;

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

                if (IsValueType())
                    return 0x11;

                return 0x12;
            }
        }

        public TypeSpec GetExtends()
        {
            int table_id, etdrow;
            m.GetCodedIndexEntry(MetadataStream.tid_TypeDef,
                tdrow, 3, m.TypeDefOrRef, out table_id,
                out etdrow);

            if (etdrow == 0)
                return null;

            var ret = m.GetTypeSpec(table_id, etdrow, gtparams, null);

            return ret;
        }

        /**<summary>Is the type a descendent of System.ValueType?</summary> */
        public bool IsValueType()
        {
            var extends = GetExtends();
            if (extends == null)
                return false;
            if (extends.m.simple_type_idx == null)
                return false;
            return (extends.m.simple_type_idx[extends.tdrow] == 0x11);
        }

        /**<summary>Is this a superclass of another type?</summary> */
        public bool IsSuperclassOf(TypeSpec other)
        {
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

        public string MangleType()
        {
            return m.MangleType(this);
        }

        public bool IsGeneric
        {
            get
            {
                return m.gtparams[tdrow] != 0;
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
                    if (this.other == null && other.other == null)
                        return true;
                    if (this.other == null)
                        return false;
                    return this.other.Equals(other.other);

                case SpecialType.SzArray:
                    return this.other.Equals(other.other);

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
    }
}
