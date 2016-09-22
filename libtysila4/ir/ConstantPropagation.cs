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
using libtysila4.util;
using metadata;

namespace libtysila4.ir
{
    public class ConstantPropagation
    {
        public static graph.Graph ConstantPropagationPass(graph.Graph g, target.Target t)
        {
            int[] vreg_stypes = new int[g.next_vreg_id];
            metadata.TypeSpec[] vreg_types = new metadata.TypeSpec[g.next_vreg_id];
            long[] vreg_intvals = new long[g.next_vreg_id];
            ulong[] vreg_uintvals = new ulong[g.next_vreg_id];
            bool[] vreg_hasvals = new bool[g.next_vreg_id];

            int start_changes = 0;
            int changes = 0;

            do
            {
                start_changes = changes;
                foreach (var i in g.LinearStream)
                {
                    var no = i.c as Opcode;

                    int mc_idx = 0;

                    foreach (var o in no.all_insts)
                    {
                        if (o.uses != null)
                        {
                            foreach (var use in o.uses)
                            {
                                if (use.IsStack && vreg_stypes[use.ssa_idx] != 0)
                                {
                                    SetParamFromCache(use, vreg_stypes, vreg_types,
                                        vreg_intvals, vreg_uintvals, vreg_hasvals,
                                        no, mc_idx, g);
                                }
                            }
                        }
                        if (o.defs != null)
                        {
                            foreach (var def in o.defs)
                            {
                                if (def.IsStack && def.cf_stype == 0)
                                {
                                    CalcConstantVal(o, vreg_stypes, vreg_types,
                                        vreg_intvals, vreg_uintvals, vreg_hasvals,
                                        ref changes);
                                }
                            }
                        }
                        mc_idx++;
                    }
                }
            } while (changes != start_changes);

            return g;
        }

        private static void CalcConstantVal(Opcode o, int[] vreg_stypes,
            TypeSpec[] vreg_types, long[] vreg_intvals,
            ulong[] vreg_uintvals, bool[] vreg_hasvals, ref int changes)
        {
            switch(o.oc)
            {
                case Opcode.oc_phi:
                    // If this is def <- phi(c, c, c, c) where c is all the
                    //  same, can use def as a constant
                    if(o.uses.Length > 0)
                    {
                        var cmp_use = o.uses[0];

                        bool all_same = true;
                        for(int i = 1; i < o.uses.Length; i++)
                        {
                            if(!CompareParamVal(cmp_use, o.uses[i]))
                            {
                                all_same = false;
                                break;
                            }
                        }
                        if(all_same)
                        {
                            o.defs[0].cf_stype = cmp_use.cf_stype;
                            o.defs[0].cf_type = cmp_use.cf_type;
                            o.defs[0].cf_intval = cmp_use.cf_intval;
                            o.defs[0].cf_uintval = cmp_use.cf_uintval;
                            o.defs[0].cf_hasval = cmp_use.cf_hasval;
                        }
                    }
                    break;

                case Opcode.oc_store:
                    if(o.uses[0].IsConstant)
                    {
                        switch(o.uses[0].ct)
                        {
                            case Opcode.ct_int32:
                                o.defs[0].cf_stype = 0x08;
                                break;
                            case Opcode.ct_int64:
                                o.defs[0].cf_stype = 0x0a;
                                break;
                            case Opcode.ct_intptr:
                                o.defs[0].cf_stype = 0x18;
                                break;
                            case Opcode.ct_object:
                                o.defs[0].cf_stype = 0x1c;
                                break;
                            case Opcode.ct_ref:
                                o.defs[0].cf_stype = 0x0f;
                                break;
                        }
                        o.defs[0].cf_intval = o.uses[0].v;
                        o.defs[0].cf_hasval = true;
                    }
                    else
                    {
                        o.defs[0].cf_stype = o.uses[0].cf_stype;
                        o.defs[0].cf_type = o.uses[0].cf_type;
                        o.defs[0].cf_intval = o.uses[0].cf_intval;
                        o.defs[0].cf_uintval = o.uses[0].cf_uintval;
                        o.defs[0].cf_hasval = o.uses[0].cf_hasval;
                    }
                    break;

                case Opcode.oc_add:
                case Opcode.oc_sub:
                    if(o.uses[0].cf_hasval && o.uses[1].cf_hasval)
                    {
                        throw new NotImplementedException();
                    }
                    break;

                case Opcode.oc_conv:
                    if(o.uses[0].cf_hasval)
                    {
                        throw new NotImplementedException();
                    }
                    break;

                case Opcode.oc_cmp:
                    if (o.uses[0].cf_hasval && o.uses[1].cf_hasval)
                    {
                        throw new NotImplementedException();
                    }
                    break;

                case Opcode.oc_ldstr:
                    o.defs[0].cf_hasval = false;
                    o.defs[0].cf_stype = 0x0e;
                    break;

                case Opcode.oc_ldind:
                case Opcode.oc_ldindzb:
                case Opcode.oc_ldindzw:
                case Opcode.oc_ldlabcontents:
                    break;

                default:
                    throw new NotImplementedException();
            }

            vreg_stypes[o.defs[0].ssa_idx] = o.defs[0].cf_stype;
            vreg_types[o.defs[0].ssa_idx] = o.defs[0].cf_type;
            vreg_intvals[o.defs[0].ssa_idx] = o.defs[0].cf_intval;
            vreg_uintvals[o.defs[0].ssa_idx] = o.defs[0].cf_uintval;
            vreg_hasvals[o.defs[0].ssa_idx] = o.defs[0].cf_hasval;

            if (o.defs[0].cf_stype != 0)
                changes++;
        }

        private static bool CompareParamVal(Param a,
            Param b)
        {
            if (a.cf_stype != b.cf_stype)
                return false;
            if (a.cf_type != b.cf_type)
                return false;
            if (a.cf_intval != b.cf_intval)
                return false;
            if (a.cf_uintval != b.cf_uintval)
                return false;
            if (a.cf_hasval != b.cf_hasval)
                return false;
            return true;
        }

        private static void SetParamFromCache(Param use,
            int[] vreg_stypes, TypeSpec[] vreg_types,
            long[] vreg_intvals, ulong[] vreg_uintvals,
            bool[] vreg_hasvals,
            Opcode no, int mc_idx, graph.Graph g)
        {
            use.cf_stype = vreg_stypes[use.ssa_idx];
            use.cf_type = vreg_types[use.ssa_idx];
            use.cf_intval = vreg_intvals[use.ssa_idx];
            use.cf_uintval = vreg_uintvals[use.ssa_idx];
            use.cf_hasval = vreg_hasvals[use.ssa_idx];

            if(use.cf_hasval)
            {
                switch(use.cf_stype)
                {
                    case 0x04:
                    case 0x06:
                    case 0x08:
                    case 0x02:
                    case 0x03:
                        use.t = Opcode.vl_c32;
                        use.ct = Opcode.ct_int32;
                        use.v = use.cf_intval;
                        break;

                    case 0x05:
                    case 0x07:
                    case 0x09:
                        use.t = Opcode.vl_c32;
                        use.ct = Opcode.ct_int32;
                        use.v = unchecked((long)use.cf_uintval);
                        break;

                    case 0x0a:
                        use.t = Opcode.vl_c64;
                        use.ct = Opcode.ct_int64;
                        use.v = use.cf_intval;
                        break;

                    case 0x0b:
                        use.t = Opcode.vl_c64;
                        use.ct = Opcode.ct_int64;
                        use.v = unchecked((long)use.cf_uintval);
                        break;

                    case 0x0e:
                    case 0x0f:
                    case 0x12:
                    case 0x1b:
                    case 0x1c:
                        use.t = Opcode.vl_c;
                        use.v = use.cf_intval;
                        break;

                    case 0x18:
                        use.t = Opcode.vl_c;
                        use.ct = Opcode.ct_intptr;
                        use.v = use.cf_intval;
                        break;

                    case 0x19:
                        use.t = Opcode.vl_c;
                        use.ct = Opcode.ct_intptr;
                        use.v = unchecked((long)use.cf_uintval);
                        break;
                }
            }

            if(use.IsConstant)
            {
                // Remove from uses
                Opcode.OpcodeId oid = new Opcode.OpcodeId();
                oid.g = g;
                oid.ls_idx = no.oc_idx;
                oid.mc_idx = mc_idx;
                oid.oc_type = 1;
                if (mc_idx < no.phis.Count)
                {
                    oid.oc_type = 0;
                }
                else
                    oid.mc_idx -= no.phis.Count;

                int c = g.uses[use.ssa_idx].Count;
                g.uses[use.ssa_idx].Remove(oid);
                if (g.uses[use.ssa_idx].Count != c - 1)
                    System.Diagnostics.Debugger.Break();
            }
        }
    }
}
