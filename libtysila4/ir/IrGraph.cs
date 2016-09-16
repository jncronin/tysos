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

using libtysila4.cil;
using System;
using System.Collections.Generic;

namespace libtysila4.ir
{
    public partial class IrGraph : graph.Graph
    {
        internal metadata.MetadataStream _m;
        internal int _md_row;
        internal int _mdef_sig;
        internal uint _lvar_sig_tok;
        internal int[] lvars, largs;

        static SpecialMethods special_meths;

        public static graph.Graph LowerCilGraph(graph.Graph g, target.Target t)
        {
            CilGraph cg = g as CilGraph;

            /* This works by examining the CIL stream looking for the
            largest linear set of nodes that match a pattern,
            either at the cil opcode level or the simple opcode level.

            The requirement is that only the first and/or last members
            of the linear set are of type MultiNode - all others must
            be simple nodes.  The start node is required to only have
            a single successor, and the end node is required to only
            have a single predecessor. */

            IrGraph ret = new IrGraph();
            ret._m = cg._m;
            ret._md_row = cg._md_row;
            ret._mdef_sig = cg._mdef_sig;
            ret._lvar_sig_tok = cg._lvar_sig_tok;
            ret.cg = cg;
            ret.ms = cg.ms;
            ret.lvars_for_simplifying = cg.lvars_for_simplifying;

            if (special_meths == null)
                special_meths = new SpecialMethods(cg._m);

            Dictionary<int, Opcode[]> cil_to_ir_map = new Dictionary<int, Opcode[]>();

            // Now splice up basic blocks as much as possible
            List<List<Opcode>> ir_code = new List<List<Opcode>>();
            foreach(var bb in cg.blocks)
            {
                List<Opcode> cur_ir_code = new List<Opcode>();
                ir_code.Add(cur_ir_code);
                int start_idx = 0;

                if (bb.Count > 0 && cg.Starts.Contains(bb[0]))
                    cur_ir_code.Add(new Opcode { oc = Opcode.oc_enter });
                while (start_idx < bb.Count)
                {
                    // Build the longest stretch of simple opcodes from
                    //  here

                    int trial_length = bb.Count - start_idx;
                    Handler h = null;
                    while (trial_length > 0)
                    {
                        cil.Opcode.SimpleOpcode[] stretch =
                            new cil.Opcode.SimpleOpcode[trial_length];

                        for (int i = 0; i < trial_length; i++)
                            stretch[i] = ((CilNode)bb[start_idx + i].c).opcode.sop;

                        OCList<cil.Opcode.SimpleOpcode> oclist =
                            new OCList<cil.Opcode.SimpleOpcode>(stretch);

                        if (simple_map.TryGetValue(oclist, out h))
                        {
                            var l = h((CilNode)bb[start_idx].c, t);

                            if (l != null)
                            {
                                /* Populate use/def members of params */
                                foreach (var oc in l)
                                {
                                    if (oc.uses != null)
                                    {
                                        foreach (var p in oc.uses)
                                            p.ud = Param.UseDefType.Use;
                                    }
                                    if (oc.defs != null)
                                    {
                                        foreach (var p in oc.defs)
                                            p.ud = Param.UseDefType.Def;
                                    }
                                }
                                cur_ir_code.AddRange(l);
                                break;
                            }
                        }

                        trial_length--;
                    }

                    if (h == null)
                        throw new Exception("Unable to lower " + bb[start_idx].ToString());

                    start_idx += trial_length;
                }
            }

            // String the new blocks together
            for(int i = 0; i < ir_code.Count; i++)
            {
                var bb = ir_code[i];

                List<graph.BaseNode> cur_block = new List<graph.BaseNode>();

                for(int j = 0; j < bb.Count; j++)
                {
                    graph.BaseNode n;

                    if (j == 0 || j == bb.Count - 1)
                        n = new graph.MultiNode();
                    else
                        n = new graph.Node();

                    if (j == 0)
                        ret.bb_starts.Add(n);
                    if (j == bb.Count - 1)
                        ret.bb_ends.Add(n);

                    n.c = bb[j];

                    if(j >= 1)
                    {
                        n.AddPrev(bb[j - 1].n);
                        bb[j - 1].n.AddNext(n);
                    }

                    bb[j].n = n;
                    n.bb = i;
                    ret.LinearStream.Add(n);
                    cur_block.Add(n);
                }

                ret.blocks.Add(cur_block);
            }

            // Now rebuild a graph using the new blocks
            for(int i = 0; i < ret.blocks.Count; i++)
            {
                var bb = ret.blocks[i];

                var first = ret.bb_starts[i];
                var last = ret.bb_ends[i];

                List<int> cbb_bbs_before = new List<int>();
                List<int> cbb_bbs_after = new List<int>();

                if (cg.bbs_before[i].Count == 0)
                    ret.Starts.Add(first);
                else
                {
                    foreach (var prev in cg.bbs_before[i])
                    {
                        first.AddPrev(ret.bb_ends[prev]);
                        cbb_bbs_before.Add(prev);
                    }                        
                }

                if (cg.bbs_after[i].Count == 0)
                    ret.Ends.Add(last);
                else
                {
                    foreach (var next in cg.bbs_after[i])
                    {
                        last.AddNext(ret.bb_starts[next]);
                        cbb_bbs_after.Add(next);
                    }
                }

                ret.bbs_after.Add(cbb_bbs_after);
                ret.bbs_before.Add(cbb_bbs_before);
            }

            return ret; 
        }

        private static Param GetParam(CilNode start)
        {
            if (start.HasLocalVarLoc)
                return new Param { t = Opcode.vl_lv, v = start.GetLocalVarLoc() };
            else if (start.HasConstant)
                return new Param { t = Opcode.vl_c, v = start.GetConstant(), ct = start.GetConstantType() };
            else if (start.HasLocalArgLoc)
                return new Param { t = Opcode.vl_arg, v = start.GetLocalArgLoc() };
            throw new NotImplementedException();
        }
    }
}
