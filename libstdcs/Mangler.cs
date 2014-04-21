/* Copyright (C) 2013 by John Cronin
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

/* This is the runtime equivalent of libtysila/Mangler2.cs
 * 
 * It should produce identical output to that but takes as its input types from
 * libsupcs, rather than the types within libtysila.
 * 
 * See libtysila/Mangler2.cs for the mangling scheme
 */

using System;
using System.Collections.Generic;
using System.Text;
using libsupcs;

namespace libstdcs
{
    public class Mangler
    {
        class ManglerState
        {
            public string cur_module = null;
            public string cur_nspace = null;
        }

        public static string MangleMethod(System.Reflection.MethodInfo method)
        {
            StringBuilder sb = new StringBuilder("_Z");
            ManglerState ms = new ManglerState();

            MangleType(method.DeclaringType as TysosType, sb, ms);
            sb.Append("M_");
            if ((method.CallingConvention & System.Reflection.CallingConventions.VarArgs) == System.Reflection.CallingConventions.VarArgs)
                throw new Exception("Unsupported calling convention (VarArgs)");
            else
                sb.Append("0");

            sb.Append("_");
            AppendStringWithLength(sb, EncodeString(method.Name));

            sb.Append("_R");
            MangleType(method.ReturnType as TysosType, sb, ms);

            sb.Append("_P");
            System.Reflection.ParameterInfo[] ps = method.GetParameters();
            bool has_this = false;
            if ((method.CallingConvention & System.Reflection.CallingConventions.HasThis) == System.Reflection.CallingConventions.HasThis)
                has_this = true;
            if ((method.CallingConvention & System.Reflection.CallingConventions.ExplicitThis) == System.Reflection.CallingConventions.ExplicitThis)
                has_this = false;
            sb.Append(ps.Length + (has_this ? 1 : 0));
            if (has_this)
                sb.Append("u1t");
            foreach (System.Reflection.ParameterInfo p in ps)
                MangleType(p.ParameterType as TysosType, sb, ms);

            // TODO: determine if this is a generic method

            return sb.ToString();
        }

        static void MangleType(TysosType t, StringBuilder sb, ManglerState ms)
        {
            bool cont = true;
            while (cont)
            {
                if (t.IsBoxed)
                {
                    sb.Append("B");
                    t = t.GetUnboxedType();
                }
                else if (t.IsManagedPointer)
                {
                    sb.Append("R");
                    t = t.GetUnboxedType();
                }
                else if (t.IsUnmanagedPointer)
                {
                    sb.Append("P");
                    t = t.GetUnboxedType();
                }
                else if (t.IsZeroBasedArray)
                {
                    sb.Append("u1Z");
                    MangleType(t.GetElementType() as TysosType, sb, ms);
                    return;
                }
                else if (t.IsArray)
                {
                    sb.Append("u1A");
                    MangleType(t.GetElementType() as TysosType, sb, ms);
                    sb.Append(t.GetArrayRank());
                    sb.Append("_");
                    // TODO: work out how to extract array lobounds and sizes
                    return;
                }
                else
                    cont = false;
            }

            if (t.IsSimpleType)
            {
                switch (t.SimpleTypeElementType)
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
                    case 0xfe:
                        // VirtFtnPtr
                        sb.Append("u1V");
                        break;
                    case 0xfd:
                        // UninstantiatedGenericParam
                        sb.Append("u1P");
                        break;
                    case 0xfc:
                        // RefGenericParam
                        sb.Append("u1R");
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (t.IsGenericType)
            {
                MangleType(t.GetGenericTypeDefinition() as TysosType, sb, ms);
                sb.Append("_G");
                sb.Append(t.GetGenericArguments().Length);
                foreach (Type p in t.GetGenericArguments())
                    MangleType(p as TysosType, sb, ms);
            }
            else if (t.IsUninstantiatedGenericTypeParameter)
            {
                sb.Append("u1G");
                sb.Append(t.UgtpIdx);
            }
            else if (t.IsUninstantiatedGenericMethodParameter)
            {
                sb.Append("u1g");
                sb.Append(t.UgmpIdx);
            }
            else
            {
                string mod = EncodeString(t.Module.Name);
                string nspace = EncodeString(t.Namespace);
                string name = EncodeString(t.Name);

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
                else if (mod == "mscorlib")
                {
                    sb.Append("W");
                    AppendStringWithLength(sb, nspace);
                    AppendStringWithLength(sb, name);
                    ms.cur_module = mod;
                    ms.cur_nspace = nspace;
                }
                else if ((mod == "libsupcs") && (nspace == "libsupcs"))
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
