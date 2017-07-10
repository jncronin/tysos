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
    /**<summary>Represents a generic way of specifying all methods
    (whether generic or not)</summary>*/
    public class MethodSpec : Spec, IEquatable<MethodSpec>
    {
        public metadata.MetadataStream m;
        public int mdrow;
        public int msig;
        public bool is_field;
        public bool is_boxed;   // is it a virtual method on a boxed type?

        public TypeSpec[] gmparams;
        public TypeSpec type;

        public List<string> aliases;

        public string mangle_override = null;
        public string name_override = null;

        public override MetadataStream Metadata
        { get { return m; } }

        public TypeSpec[] gtparams
        {
            get
            {
                if (type == null)
                    return null;
                return type.gtparams;
            }
        }

        public bool Equals(MethodSpec other)
        {
            if (!m.Equals(other.m))
                return false;
            if (!type.Equals(other.type))
                return false;
            if (mdrow != other.mdrow)
                return false;
            if (msig != other.msig)
                return false;

            if (gmparams == null && other.gmparams == null)
                return true;
            if (gmparams == null)
                return false;
            if (other.gmparams == null)
                return false;
            if (gmparams.Length != other.gmparams.Length)
                return false;
            for (int i = 0; i < gmparams.Length; i++)
            {
                if (!gmparams[i].Equals(other.gmparams[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hc = m.GetHashCode();
            hc <<= 1;
            hc ^= mdrow.GetHashCode();
            hc <<= 1;
            hc ^= msig.GetHashCode();

            if (type != null)
            {
                hc <<= 1;
                hc ^= type.GetHashCode();
            }
            if (gmparams != null)
            {
                hc ^= gmparams.Length.GetHashCode();
                foreach (var gmparam in gmparams)
                {
                    hc <<= 1;
                    hc ^= gmparam.GetHashCode();
                }
            }

            return hc;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MethodSpec);
        }

        public bool IsGeneric
        {
            get
            {
                return m.gmparams[mdrow] != 0;
            }
        }

        public bool IsGenericTemplate
        {
            get
            {
                if (!IsGeneric)
                    return false;
                return gmparams == null;
            }
        }

        /**<summary>Returns whether the method is virtual</summary> */
        public bool IsVirtual
        {
            get
            {
                var flags = m.GetIntEntry(MetadataStream.tid_MethodDef,
                    mdrow, 2);
                return (flags & 0x40) != 0;
            }
        }

        public override string ToString()
        {
            if (m == null)
                return "MethodSpec";
            return m.MangleMethod(this);
        }

        public string MangleMethod()
        {
            if (mangle_override != null)
                return mangle_override;
            return m.MangleMethod(this);
        }

        public override string Name
        {
            get
            {
                if (name_override != null)
                    return name_override;
                return m.GetStringEntry(MetadataStream.tid_MethodDef,
                    mdrow, 3);
            }
        }

        public FullySpecSignature FieldSignature
        {
            get
            {
                List<byte> sig = new List<byte>();
                List<MetadataStream> mods = new List<MetadataStream>();

                var sig_idx = (int)m.GetIntEntry(MetadataStream.tid_Field,
                    mdrow, 2);

                // Parse blob length
                m.SigReadUSCompressed(ref sig_idx);

                byte fld = m.sh_blob.di.ReadByte(sig_idx++);
                if (fld != 0x6)
                    throw new Exception("Bad field signature");

                sig.Add(fld);

                var ts = m.GetTypeSpec(ref sig_idx, gtparams, gmparams);
                ts.AddSignature(sig, mods);

                return new FullySpecSignature
                {
                    Modules = mods,
                    Signature = sig,
                    OriginalSpec = this,
                    Type = FullySpecSignature.FSSType.Field
                };
            }
        }

        public FullySpecSignature MethodSignature
        {
            get
            {
                List<byte> sig = new List<byte>();
                List<MetadataStream> mods = new List<MetadataStream>();
                throw new NotImplementedException();
            }
        }

        public FullySpecSignature Signature
        {
            get
            {
                if (is_field)
                    return FieldSignature;
                else
                    return MethodSignature;
            }
        }

        public bool HasCustomAttribute(string ctor)
        {
            return GetCustomAttribute(ctor) != -1;
        }

        public int GetCustomAttribute(string ctor)
        {
            if (m.md_custom_attrs == null)
                return -1;
            int cur_ca = m.md_custom_attrs[mdrow];

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
                    return cur_ca;

                cur_ca = m.next_ca[cur_ca];
            }

            return -1;
        }

        public override IEnumerable<int> CustomAttributes(string ctor = null)
        {
            if (m.md_custom_attrs == null)
                yield break;
            int cur_ca = m.md_custom_attrs[mdrow];

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

        string cc = null;
        public string CallingConvention
        {
            get
            {
                if (cc != null)
                    return cc;
                cc = "default";

                var cc_ca = GetCustomAttribute("_ZN14libsupcs#2Edll8libsupcs26CallingConventionAttribute_7#2Ector_Rv_P2u1tu1S");
                if (cc_ca != -1)
                {
                    // This is a call to a method that has a different name
                    int val_idx = (int)m.GetIntEntry(MetadataStream.tid_CustomAttribute,
                        cc_ca, 2);

                    m.SigReadUSCompressed(ref val_idx);
                    var prolog = m.sh_blob.di.ReadUShort(val_idx);
                    if (prolog == 0x0001)
                    {
                        val_idx += 2;

                        var str_len = m.SigReadUSCompressed(ref val_idx);
                        StringBuilder sb = new StringBuilder();
                        for (uint i = 0; i < str_len; i++)
                        {
                            sb.Append((char)m.sh_blob.di.ReadByte(val_idx++));
                        }
                        cc = sb.ToString();
                    }
                }

                return cc;
            }
        }

        public IEnumerable<string> MethodAliases
        {
            get
            {
                if(aliases != null)
                {
                    foreach (var alias in aliases)
                        yield return alias;
                }

                int cur_ca = m.md_custom_attrs[mdrow];

                while(cur_ca != 0)
                {
                    int type_tid, type_row;
                    m.GetCodedIndexEntry(MetadataStream.tid_CustomAttribute,
                        cur_ca, 1, m.CustomAttributeType, out type_tid,
                        out type_row);

                    MethodSpec ca_ms;
                    m.GetMethodDefRow(type_tid, type_row, out ca_ms);
                    var ca_ms_name = ca_ms.MangleMethod();

                    if (ca_ms_name == "_ZN14libsupcs#2Edll8libsupcs20MethodAliasAttribute_7#2Ector_Rv_P2u1tu1S")
                    {
                        int val_idx = (int)m.GetIntEntry(MetadataStream.tid_CustomAttribute,
                            cur_ca, 2);

                        m.SigReadUSCompressed(ref val_idx);
                        var prolog = m.sh_blob.di.ReadUShort(val_idx);
                        if (prolog == 0x0001)
                        {
                            val_idx += 2;

                            var str_len = m.SigReadUSCompressed(ref val_idx);
                            StringBuilder sb = new StringBuilder();
                            for (uint i = 0; i < str_len; i++)
                            {
                                sb.Append((char)m.sh_blob.di.ReadByte(val_idx++));
                            }
                            yield return sb.ToString();
                        }
                    }

                    cur_ca = m.next_ca[cur_ca];
                }

                yield break;
            }
        }

        public override bool IsInstantiatedGenericType
        {
            get
            {
                return type.IsInstantiatedGenericType;
            }
        }

        public override bool IsInstantiatedGenericMethod
        {
            get
            {
                if (IsGeneric && !IsGenericTemplate)
                    return true;
                return false;
            }
        }

        public override bool IsArray
        {
            get
            {
                return type.IsArray;
            }
        }
    }
}
