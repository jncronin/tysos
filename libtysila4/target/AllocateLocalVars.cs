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

namespace libtysila4.target
{
    public class AllocateLocalVars
    {
        public static graph.Graph AllocateLocalVarsPass(graph.Graph g, Target t)
        {
            /* Generate list of locations of local vars */
            int table_id;
            int row;
            g.cg._m.InterpretToken(g.cg._lvar_sig_tok,
                out table_id, out row);
            int idx = (int)g.cg._m.GetIntEntry(table_id, row, 0);
            int lv_count = g.cg._m.GetLocalVarCount(ref idx);

            int[] lv_locs = new int[lv_count];
            int cur_loc = 0;
            for(int i = 0; i < lv_count; i++)
            {
                var type = g.cg._m.GetTypeSpec(ref idx, g.ms.gtparams,
                    g.ms.gmparams);
                int t_size = t.GetSize(type);
                lv_locs[i] = cur_loc;

                cur_loc += t_size;

                // align to pointer size
                int diff = cur_loc % t.GetPointerSize();
                if (diff != 0)
                    cur_loc = cur_loc - diff + t.GetPointerSize();
            }

            /* Do the same for local args */
            int la_count = g.cg._m.GetMethodDefSigParamCountIncludeThis(
                g.cg._mdef_sig);
            int[] la_locs = new int[la_count];
            int la_count2 = g.cg._m.GetMethodDefSigParamCount(
                g.cg._mdef_sig);
            int laidx = 0;
            if(la_count != la_count2)
            {
                var this_size = t.GetCTSize(ir.Opcode.ct_object);
                la_locs[laidx++] = cur_loc;
                cur_loc += this_size;
            }
            idx = g.cg._m.GetMethodDefSigRetTypeIndex(
                g.cg._mdef_sig);
            // pass by rettype
            g.cg._m.GetTypeSpec(ref idx, g.ms.gtparams, g.ms.gmparams);

            var cc = t.cc_map["sysv"];
            int stack_loc = 0;
            var la_phys_locs = t.GetRegLocs(new ir.Param
            {
                m = g.cg._m,
                ms = g.ms,
            }, ref stack_loc, cc);
            g.incoming_args = la_phys_locs;

            for(int i = 0; i < la_count2; i++)
            {
                var mreg = la_phys_locs[i];
                if (mreg.type == Target.rt_stack)
                    la_locs[laidx++] = -1 - mreg.stack_loc;
                else
                {
                    var type = g.cg._m.GetTypeSpec(ref idx, g.ms.gtparams, g.ms.gmparams);
                    var la_size = t.GetSize(type);
                    la_locs[laidx++] = cur_loc;
                    cur_loc += la_size;
                }
            }

            /* Iterate through code, changing as required */
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;

                for(int i = 0; i < mcn.insts.Count; i++)
                {
                    var I = mcn.insts[i];
                    if(I.p.Length > 0 && I.p[0].t == ir.Opcode.vl_str && I.p[0].v == Generic.g_setupstack)
                    {
                        mcn.insts.RemoveAt(i);
                        var insts = t.SetupStack(cur_loc);
                        foreach(var inst in insts)
                        {
                            mcn.insts.Insert(i, inst);
                            i++;
                        }

                        for(int j = 0; j < la_count2; j++)
                        {
                            var dest = la_locs[j];
                            var src = la_phys_locs[j];

                            if(src.type != Target.rt_stack)
                            {
                                var moves = t.CreateMove(src, t.GetLVLocation(dest, cur_loc));
                                foreach(var move in moves)
                                {
                                    mcn.insts.Insert(i, move);
                                    i++;
                                }
                            }
                        }

                        i--;
                    }
                    else
                    {
                        foreach(var p in I.p)
                        {
                            if(p.IsLV)
                            {
                                p.t = ir.Opcode.vl_mreg;
                                p.mreg = t.GetLVLocation(lv_locs[(int)p.v], cur_loc);
                            }
                            else if(p.IsLA)
                            {
                                p.t = ir.Opcode.vl_mreg;
                                p.mreg = t.GetLVLocation(la_locs[(int)p.v], cur_loc);
                            }
                        }
                    }
                }

            }

            return g;
        }
    }
}
