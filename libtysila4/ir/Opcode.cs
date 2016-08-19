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

namespace libtysila4.ir
{
    public partial class Opcode : graph.NodeContents
    {
        public int oc = oc_null;
        public int cc = cc_always;
        /* parameters */
        public Param[] uses;
        public Param[] defs;

        public bool is_mc = false;

        public List<target.MCInst> mcinsts;

        string IrString()
        {
            StringBuilder sb = new StringBuilder();
            if (oc_names.ContainsKey(oc))
                sb.Append(oc_names[oc]);
            else
                sb.Append(oc.ToString());

            if (cc != cc_always)
            {
                sb.Append("(");
                sb.Append(cc_names[cc]);
                sb.Append(")");
            }
            sb.Append(" ");
            if (uses != null && uses.Length > 0)
            {
                sb.Append("{ ");
                for (int i = 0; i < uses.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(uses[i].ToString());
                }
                sb.Append(" }");
            }
            else sb.Append("{empty}");
            sb.Append(" -> ");
            if (defs != null && defs.Length > 0)
            {
                sb.Append("{ ");
                for (int i = 0; i < defs.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(defs[i].ToString());
                }
                sb.Append(" }");
            }
            else sb.Append("{empty}");
            return sb.ToString();
        }

        string MCString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var m in mcinsts)
            {
                MCString(m, sb);
                sb.Append(";  \n");
            }

            return sb.ToString();
        }

        void MCString(target.MCInst m, StringBuilder sb)
        { 
            for(int i = 0; i < m.p.Length; i++)
            {
                var p = m.p[i];

                if (i != 0)
                    sb.Append(", ");

                sb.Append(p.ToString());
            }
        }

        public override string ToString()
        {
            if (is_mc)
                return MCString();
            else
                return IrString();
        }

        static Dictionary<int, string> oc_names;
        public static Dictionary<int, string> cc_names;
        public static Dictionary<int, int> cc_invert_map;
        public static Dictionary<int, string> ct_names;
        public static Dictionary<cil.Opcode.SingleOpcodes, int> cc_single_map;
        public static Dictionary<cil.Opcode.DoubleOpcodes,int> cc_double_map;
        public static Dictionary<int, GetDefTypeHandler> oc_pushes_map
            = new Dictionary<int, GetDefTypeHandler>(
                new GenericEqualityComparer<int>());
        public static Dictionary<int, string> vl_names
            = new Dictionary<int, string>(
                new GenericEqualityComparer<int>());

        public delegate int GetDefTypeHandler(Opcode start);


        static Opcode()
        {
            // Pull in mappings defined in IrOpcodes.td
            oc_names = new Dictionary<int, string>();
            cc_names = new Dictionary<int, string>();
            ct_names = new Dictionary<int, string>();
            cc_single_map = new Dictionary<cil.Opcode.SingleOpcodes, int>();
            cc_double_map = new Dictionary<cil.Opcode.DoubleOpcodes, int>();
            cc_invert_map = new Dictionary<int, int>();
            
            init_oc();
            init_cc();
            init_ct();
            init_cc_single_map();
            init_cc_double_map();
            init_cc_invert();
            init_oc_pushes_map();
            init_vl();
        }
    }

    public class Param
    {
        public int t;
        public long v;
        public long v2;
        public string str;
        public target.Target.Reg mreg;
        public metadata.MetadataStream m;
        public int ct = Opcode.ct_unknown;
        public int ssa_idx = -1;

        public enum UseDefType { Unknown, Use, Def };
        public UseDefType ud = UseDefType.Unknown;

        public bool IsStack { get { return t == Opcode.vl_stack || t == Opcode.vl_stack32 || t == Opcode.vl_stack64; } }

        /** <summary>Decorate the current type to include bitness</summary> */
        public int DecoratedType(target.Target tgt)
        {
            int new_ct = ct;

            if (new_ct == Opcode.ct_intptr)
                new_ct = tgt.ptype;

            switch(t)
            {
                case Opcode.vl_arg:
                    if (new_ct == Opcode.ct_int32)
                        return Opcode.vl_arg32;
                    else if (new_ct == Opcode.ct_int64)
                        return Opcode.vl_arg64;
                    return t;

                case Opcode.vl_stack:
                    if (new_ct == Opcode.ct_int32)
                        return Opcode.vl_stack32;
                    else if (new_ct == Opcode.ct_int64)
                        return Opcode.vl_stack64;
                    return t;

                case Opcode.vl_lv:
                    if (new_ct == Opcode.ct_int32)
                        return Opcode.vl_lv32;
                    else if (new_ct == Opcode.ct_int64)
                        return Opcode.vl_lv64;
                    return t;

                case Opcode.vl_c:
                    if (new_ct == Opcode.ct_int32)
                        return Opcode.vl_c32;
                    else if (new_ct == Opcode.ct_int64)
                        return Opcode.vl_c64;
                    return t;

                default:
                    return t;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            switch(t)
            {
                case Opcode.vl_c:
                case Opcode.vl_c32:
                case Opcode.vl_c64:
                    sb.Append("$" + v.ToString());
                    break;
                case Opcode.vl_arg:
                case Opcode.vl_arg32:
                case Opcode.vl_arg64:
                    sb.Append("la" + v.ToString());
                    break;
                case Opcode.vl_lv:
                case Opcode.vl_lv32:
                case Opcode.vl_lv64:
                    sb.Append("lv" + v.ToString());
                    break;
                case Opcode.vl_stack:
                case Opcode.vl_stack32:
                case Opcode.vl_stack64:
                    if (ssa_idx != -1)
                        sb.Append("vreg" + ssa_idx.ToString());
                    else
                        sb.Append("st" + v.ToString());
                    break;
                case Opcode.vl_call_target:
                    sb.Append("callsite(" + v.ToString() + " [" + v2.ToString() + "])");
                    break;
                case Opcode.vl_cc:
                    sb.Append(Opcode.cc_names[(int)v]);
                    break;
                case Opcode.vl_str:
                    sb.Append(str);
                    break;
                case Opcode.vl_br_target:
                    sb.Append("bb" + v.ToString());
                    break;
                case Opcode.vl_mreg:
                    sb.Append("%" + mreg.ToString());
                    break;
                default:
                    return "{null}";
            }
            
            switch(ct)
            {
                case Opcode.ct_int32:
                case Opcode.ct_int64:
                case Opcode.ct_intptr:
                case Opcode.ct_object:
                case Opcode.ct_ref:
                case Opcode.ct_float:
                    sb.Append(": ");
                    sb.Append(Opcode.ct_names[ct]);
                    break;
            }

            if (ud == UseDefType.Use)
                sb.Append(" (use) ");
            else if (ud == UseDefType.Def)
                sb.Append(" (def) ");

            sb.Append("}");
            return sb.ToString();
        }
    }
}
