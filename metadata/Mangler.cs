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

/* Changed Aug 2011 to use an entirely different mangling system based loosely
* on the Itanium C++ ABI
* 
* We use the following EBNF:
* 
* <name>: _Z<type name><object name>
*         _M<count><module name>              # module info for <module name> (a string, with length in count)
*         _A<count><assembly name>            # assembly info for <assembly name> (a string, with length in count)
* 
* <type name>: <prefix><nested-name><generic-inst>
* 
* <prefix>:    P               # unmanaged pointer to
*              R               # reference to
* 
* <nested-name>:   N<module count><module><nspace count><nspace><name count><type name>
*                      # type specified by module, name space and name
*                  U<nspace count><nspace><name count><type name>
*                      # type specified by most recently used module, name space and name
*                  V<name count><type name>
*                      # type specified by most recently used module and name space, and name
*                  W<nspace count><nspace><name count><type name>
*                      # type within mscorlib, specified by name space and name
*                  X<name count><type name>
*                      # type within libsupcs, in namespace libsupcs, specified by name
*                  <predefined-name>
*                  
* where <module count>, <nspace count> and <name count> are of type <integer>
* <integer>: integer in decimal format
*                  
* <predefined-name>:   v               # void
*                      c               # Char
*                      b               # Boolean
*                      a               # I1
*                      h               # U1
*                      s               # I2
*                      t               # U2
*                      i               # I4
*                      j               # U4
*                      x               # I8
*                      y               # U8
*                      f               # R4
*                      d               # R8
*                      u1I             # I
*                      u1U             # U
*                      u1S             # String
*                      u1T             # TypedByRef
*                      u1O             # Object
*                      u1L             # ValueType
*                      u1V             # VirtFtnPtr
*                      u1P             # Uninstantiated generic param
*                      u1p             # Uninstantiated generic method param
*                      u1A<array-def>  # ComplexArray
*                      u1Z<elem-type>  # ZeroBasedArray, <elem-type> is of type <type name>
*                      u1t             # this pointer
*                      u1G<integer>    # GenericParam, followed by ParamNumber
*                      u1g<integer>    # GenericMethodParam, followed by ParamNumber
*                      u1R             # Ref - used to denote a generic reference type in a coalesced generic method
*                      
* <array-def>: <type name><rank>_<lobound 0>_ ... <lobound n-1>__<size 0>_ ... <size n-1>_
* <type name> is the base type of the array of type <string>
* <rank>, <lobound n>, <size n> are all of type <integer>
* 
* <object name>:   TV                                      # Virtual table
*                  TI                                      # Typeinfo structure
*                  MI<method-def>                          # Method info for a certain method
*                  FI<name count><name><field-type>        # Field info for a certain field
*                  M_<method-def>                          # Executable code for a certain method
*                  S                                       # Static data for a type
*                  
* <method-def>:    <flags>_<name count><name><ret-type><params><generic-meth-inst>
* <flags>:         0       CallConv.Default
*                  1       CallConv.VarArg
*                  2       CallConv.Generic
* 
* <ret-type>:      _R<type name>
* <field-type>:    _R<type name>
* 
* where <name count> is of type <integer>
* 
* <params>: _P<param count><param 1><param 2><param 3>...<param n>
* where <param> is of type <type name> and <param count> is of type <integer>
* 
* <generic-inst>:  <nothing>
*                  _G<param count><gparam 1><gparam 2><gparam 3>...<gparam n>
* <generic-meth-inst>: <nothing>
*                      _g<param count><gparam 1><gparam 2><gparam 3>...<gparam n>
*                     
* where <gparam> is of type <type name>
* 
* <string>:    ASCII encoding of a string
*              Characters which cannot be directly represented in ELF symbols are represented as #XX
*              where XX is the hexadecimal encoding (using capitals) of the character
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace metadata
{
    partial class MetadataStream
    {
        class ManglerState
        {
            public string cur_module = null;
            public string cur_nspace = null;
        }

        public string MangleType(TypeSpec ts)
        {
            StringBuilder sb = new StringBuilder("_Z");
            ManglerState ms = new ManglerState();

            MangleType(ts, sb, ms);

            return sb.ToString();
        }

        public TypeSpec DemangleType(string s)
        {
            ManglerState ms = new ManglerState();

            int i = 0;
            if (!Munch(s, ref i, "_Z"))
                throw new ArgumentException();
            return DemangleType(s, ref i, ms);
        }

        private TypeSpec DemangleType(string s, ref int i, ManglerState ms)
        {
            string module = ms.cur_module, nspace = ms.cur_nspace, name;

            if (Munch(s, ref i, "P"))
            {
                return new TypeSpec
                {
                    stype = TypeSpec.SpecialType.Ptr,
                    other = DemangleType(s, ref i, ms)
                };
            }
            else if (Munch(s, ref i, "R"))
            {
                return new TypeSpec
                {
                    stype = TypeSpec.SpecialType.MPtr,
                    other = DemangleType(s, ref i, ms)
                };
            }
            else if (Munch(s, ref i, "u1Z"))
            {
                return new TypeSpec
                {
                    stype = TypeSpec.SpecialType.SzArray,
                    other = DemangleType(s, ref i, ms)
                };
            }
            else if (Munch(s, ref i, "u1A"))
            {
                throw new NotImplementedException();
            }
            else if (Munch(s, ref i, "v"))
                return SystemVoid;
            else if (Munch(s, ref i, "c"))
                return SystemChar;
            else if (Munch(s, ref i, "b"))
                throw new NotImplementedException(); //bool
            else if (Munch(s, ref i, "a"))
                return SystemInt8;
            else if (Munch(s, ref i, "h"))
                return SystemByte;
            else if (Munch(s, ref i, "s"))
                return SystemInt16;
            else if (Munch(s, ref i, "t"))
                return SystemUInt16;
            else if (Munch(s, ref i, "i"))
                return SystemInt32;
            else if (Munch(s, ref i, "j"))
                return SystemUInt32;
            else if (Munch(s, ref i, "x"))
                return SystemInt64;
            else if (Munch(s, ref i, "y"))
                return SystemUInt64;
            else if (Munch(s, ref i, "f"))
                throw new NotImplementedException(); //r4
            else if (Munch(s, ref i, "d"))
                throw new NotImplementedException(); //r8
            else if (Munch(s, ref i, "u1I"))
                return SystemIntPtr;
            else if (Munch(s, ref i, "u1U"))
                throw new NotImplementedException(); //uintptr
            else if (Munch(s, ref i, "u1S"))
                return SystemString;
            else if (Munch(s, ref i, "u1O"))
                return SystemObject;
            else if (Munch(s, ref i, "u1L"))
                return SystemValueType;
            else if(Munch(s, ref i, "N"))
            {
                module = MunchString(s, ref i);
                nspace = MunchString(s, ref i);
                name = MunchString(s, ref i);
            }
            else if(Munch(s, ref i, "U"))
            {
                nspace = MunchString(s, ref i);
                name = MunchString(s, ref i);
            }
            else if(Munch(s, ref i, "V"))
            {
                name = MunchString(s, ref i);
            }
            else if(Munch(s, ref i, "W"))
            {
                module = "mscorlib";
                nspace = MunchString(s, ref i);
                name = MunchString(s, ref i);
            }
            else if(Munch(s, ref i, "X"))
            {
                module = "libsupcs";
                nspace = "libsupcs";
                name = MunchString(s, ref i);
            }
            else
                throw new NotImplementedException();

            if (Munch(s, ref i, "_G"))
                throw new NotImplementedException();

            ms.cur_module = module;
            ms.cur_nspace = nspace;

            var mod = al.GetAssembly(module);
            return mod.GetTypeSpec(nspace, name);
        }

        private string MunchString(string s, ref int i)
        {
            int digit_count = 0;
            while ((i + digit_count < s.Length) && (char.IsDigit(s[i + digit_count])))
                digit_count++;

            int digits = int.Parse(s.Substring(i, digit_count));
            i += digit_count;

            if (i + digits > s.Length)
                throw new ArgumentException();

            var ret = s.Substring(i, digits);
            i += digits;
            return DecodeString(ret);
        }

        private string DecodeString(string s)
        {
            if (!s.Contains("#"))
                return s;

            StringBuilder ret = new StringBuilder();

            int i = 0;
            while(i < s.Length)
            {
                if(s[i] == '#')
                {
                    i++;
                    if (i + 2 > s.Length)
                        throw new ArgumentException();

                    var val = int.Parse(s.Substring(i, 2),
                        System.Globalization.NumberStyles.HexNumber);
                    ret.Append((char)val);
                    i += 2;
                }
                else
                {
                    ret.Append(s[i]);
                    i++;
                }
            }
            return ret.ToString();
        }

        private bool Munch(string s, ref int i, string v)
        {
            if (v.Length + i > s.Length)
                return false;
            if(s.Substring(i, v.Length).Equals(v))
            {
                i += v.Length;
                return true;
            }
            return false;
        }

        void MangleType(TypeSpec ts, StringBuilder sb,
            ManglerState ms)
        {
            if(ts == null)
            {
                sb.Append("v");
                return;
            }
            switch(ts.stype)
            {
                case TypeSpec.SpecialType.None:
                    ts.m.MangleTypeDef(ts.tdrow, sb, ms);
                    if (ts.IsGeneric && ts.gtparams != null)
                    {
                        sb.Append("_G");
                        sb.Append(ts.gtparams.Length.ToString());
                        foreach (var gtparam in ts.gtparams)
                            MangleType(gtparam, sb, ms);
                    }
                    break;

                case TypeSpec.SpecialType.SzArray:
                    sb.Append("u1Z");
                    MangleType(ts.other, sb, ms);
                    break;

                case TypeSpec.SpecialType.Ptr:
                    sb.Append("P");
                    MangleType(ts.other, sb, ms);
                    break;

                case TypeSpec.SpecialType.MPtr:
                    sb.Append("R");
                    MangleType(ts.other, sb, ms);
                    break;

                case TypeSpec.SpecialType.Var:
                    sb.Append("u1P");
                    sb.Append(ts.idx.ToString());
                    break;

                case TypeSpec.SpecialType.MVar:
                    sb.Append("u1p");
                    sb.Append(ts.idx.ToString());
                    break;

                case TypeSpec.SpecialType.Boxed:
                    MangleType(ts.other, sb, ms);
                    break;

                case TypeSpec.SpecialType.Array:
                    sb.Append("u1A");
                    MangleType(ts.other, sb, ms);
                    sb.Append(ts.arr_rank.ToString());
                    foreach(var lb in ts.arr_lobounds)
                    {
                        sb.Append("_");
                        sb.Append(lb.ToString());
                    }
                    sb.Append("_");
                    foreach(var s in ts.arr_sizes)
                    {
                        sb.Append("_");
                        sb.Append(s.ToString());
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public string MangleMethod(MethodSpec m)
        { return MangleMethod(m, false); }

        public string MangleMethodSpec(MethodSpec m)
        { return MangleMethod(m, true); }

        private string MangleMethod(TypeSpec t, string mname, int msig,
            TypeSpec[] gtparams = null, TypeSpec[] gmparams = null,
            bool is_spec = false, bool is_boxed = false)
        {
            StringBuilder sb = new StringBuilder("_Z");
            ManglerState ms = new ManglerState();

            if (msig == 0)
                throw new Exception("invalid method signature");

            if (gtparams == null)
                gtparams = t.gtparams;

            /* Get declaring type */
            MangleType(t, sb, ms);

            sb.Append("_");

            /* Get method name */
            AppendStringWithLength(sb, mname);

            /* Generic method params */
            if(gmparams != null)
            {
                sb.Append("_g");
                sb.Append(gmparams.Length.ToString());
                foreach (var gmp in gmparams)
                    MangleType(gmp, sb, ms);
            }

            if (is_spec)
                sb.Append("_MS");

            /* Return type */
            sb.Append("_R");
            int ret_idx = GetMethodDefSigRetTypeIndex(msig);
            var ret_ts = GetTypeSpec(ref ret_idx, gtparams,
                gmparams);
            MangleType(ret_ts, sb, ms);

            /* Params */
            sb.Append("_P");
            var pcount = GetMethodDefSigParamCount(msig);
            var pcountthis = GetMethodDefSigParamCountIncludeThis(msig);
            sb.Append(pcountthis);
            if (pcountthis != pcount)
                sb.Append("u1t");
            for (int i = 0; i < pcount; i++)
            {
                var p_ts = GetTypeSpec(ref ret_idx, gtparams, gmparams);
                MangleType(p_ts, sb, ms);
            }

            if (is_boxed)
                sb.Append("B");

            return sb.ToString();
        }

        private string MangleMethod(MethodSpec m, bool is_spec)
        {
            if (m.mangle_override != null)
                return m.mangle_override;

            return m.m.MangleMethod(m.type,
                m.name_override != null ? m.name_override : EncodeString(m.m.GetStringEntry(tid_MethodDef, m.mdrow, 3)),
                m.msig, m.gtparams, m.gmparams, is_spec, m.is_boxed);
        }

        private void MangleTypeSig(ref int type_idx, StringBuilder sb, ManglerState ms)
        {
            uint p_token;
            int p_type = GetType(ref type_idx, out p_token);

            switch(p_type)
            {
                case 0x1d:
                    /* SZARRAY */
                    sb.Append("u1Z");
                    MangleTypeSig(ref type_idx, sb, ms);
                    break;

                default:
                    MangleTypeSig(p_type, p_token, sb, ms);
                    break;
            }
        }

        private void MangleTypeSig(int ret_type, uint ret_token, StringBuilder sb, ManglerState ms)
        {
            switch(ret_type)
            {
                case 1:
                    // void
                    sb.Append("v");
                    break;
                case 2:
                    // bool
                    sb.Append("b");
                    break;
                case 3:
                    // char
                    sb.Append("c");
                    break;
                case 4:
                    // i1
                    sb.Append("a");
                    break;
                case 5:
                    // u1
                    sb.Append("h");
                    break;
                case 6:
                    // i2
                    sb.Append("s");
                    break;
                case 7:
                    // u2
                    sb.Append("t");
                    break;
                case 8:
                    // i4
                    sb.Append("i");
                    break;
                case 9:
                    // u4
                    sb.Append("j");
                    break;
                case 0xa:
                    // i8
                    sb.Append("x");
                    break;
                case 0xb:
                    // u8
                    sb.Append("y");
                    break;
                case 0xc:
                    // r4
                    sb.Append("f");
                    break;
                case 0xd:
                    // r8
                    sb.Append("d");
                    break;
                case 0xe:
                    // string
                    sb.Append("u1S");
                    break;
                case 0x11:
                    // ValueType
                    sb.Append("u1L");
                    break;
                case 0x18:
                    // intptr
                    sb.Append("u1I");
                    break;
                case 0x19:
                    // uintptr
                    sb.Append("u1U");
                    break;
                case 0x1c:
                    // object
                    sb.Append("u1O");
                    break;
                case 0x16:
                    // TypedByRef
                    sb.Append("u1T");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void MangleTypeDef(int td_idx, StringBuilder sb, ManglerState ms)
        {
            // Is this a simple type?
            if(is_corlib && simple_type_idx[td_idx] != -1)
            {
                MangleTypeSig(simple_type_idx[td_idx], 0, sb, ms);
                return;
            }

            // Get module, type name
            string mod = EncodeString(GetStringEntry(tid_Module, 1, 1));
            if(mod == "CommonLanguageRuntimeLibrary")
            {
                /* This is the framework-agnostic .netstandard 2.0 way of referring to
                 * either mscorlib.dll or System.Private.CoreLib.dll
                 * 
                 * We rename the latter to the former in the build script, so do the
                 * same here */
                mod = "mscorlib#2Edll";
            }

            // Get namespace of outermost enclosing type
            int outermost = td_idx;
            while (nested_parent[outermost] != 0)
                outermost = nested_parent[outermost];
            string nspace = EncodeString(GetStringEntry(tid_TypeDef, outermost, 2));

            StringBuilder name_sb = new StringBuilder();
            AppendEnclosingType(td_idx, name_sb);
            string name = EncodeString(name_sb.ToString());

            if (mod == ms.cur_module)
            {
                if (nspace == ms.cur_nspace)
                {
                    sb.Append("V");
                    AppendStringWithLength(sb, name);
                }
                else
                {
                    sb.Append("U");
                    AppendStringWithLength(sb, nspace);
                    AppendStringWithLength(sb, name);
                    ms.cur_nspace = nspace;
                }
            }
            else if (mod == "mscorlib#2Edll")
            {
                sb.Append("W");
                AppendStringWithLength(sb, nspace);
                AppendStringWithLength(sb, name);
                ms.cur_module = mod;
                ms.cur_nspace = nspace;
            }
            else if ((mod == "libsupcs#2E") && (nspace == "libsupcs#2E"))
            {
                sb.Append("X");
                AppendStringWithLength(sb, name);
                ms.cur_module = mod;
                ms.cur_nspace = nspace;
            }
            else
            {
                sb.Append("N");
                AppendStringWithLength(sb, mod);
                AppendStringWithLength(sb, nspace);
                AppendStringWithLength(sb, name);
                ms.cur_module = mod;
                ms.cur_nspace = nspace;
            }
        }

        private void AppendEnclosingType(int td_idx, StringBuilder name_sb)
        {
            if(nested_parent[td_idx] != 0)
            {
                AppendEnclosingType(nested_parent[td_idx], name_sb);
                name_sb.Append("+");
            }
            name_sb.Append(GetStringEntry(tid_TypeDef, td_idx, 1));
        }

        private static void AppendStringWithLength(StringBuilder sb, string s)
        {
            sb.Append(s.Length.ToString());
            sb.Append(s);
        }

        private static string EncodeString(string p)
        {
            /* Many tools cannot deal with +, - and . characters in labels.
             * 
             * Therefore we encode them to # followed by the hex value (in capitals) of the ascii code of the character
             * We encode # itself too
             */

            if (p.Contains("+") || p.Contains("-") || p.Contains(".") || p.Contains("#"))
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in p)
                {
                    if ((c == '+') || (c == '-') || (c == '.') || (c == '#'))
                    {
                        byte[] enc = ASCIIEncoding.ASCII.GetBytes(new char[] { c });
                        sb.Append("#");
                        sb.Append(enc[0].ToString("X2"));
                    }
                    else
                        sb.Append(c);
                }
                return sb.ToString();
            }
            return p;
        }

    }
}
