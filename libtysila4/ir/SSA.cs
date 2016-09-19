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
using libtysila4.graph;
using libtysila4.util;

/* Converts to SSA form */
namespace libtysila4.ir
{
    public class SSA
    {
        public static graph.Graph ConvertToSSAPass(graph.Graph input, target.Target t)
        {
            /* Determine all defined variables */
            Dictionary<int, util.Set> defsites =
                new Dictionary<int, util.Set>(new GenericEqualityComparer<int>());
            Dictionary<int, int> ctypes =
                new Dictionary<int, int>(new GenericEqualityComparer<int>());
            Dictionary<int, util.Set> Aorig =
                new Dictionary<int, util.Set>(new GenericEqualityComparer<int>());
            Dictionary<int, ir.Param> var_locs =
                new Dictionary<int, ir.Param>(new GenericEqualityComparer<int>());
            List<int> vars = new List<int>();
            int oc_idx = 0;
            List<Opcode> ocs = new List<Opcode>();

            foreach(var n in input.LinearStream)
            {
                var o = n.c as Opcode;
                o.oc_idx = oc_idx;
                ocs.Add(o);

                Aorig[oc_idx] = new Set();

                if (o.defs != null)
                {
                    foreach (var p in o.defs)
                    {
                        if (p.IsStack)
                        {
                            int a = (int)p.v;
                            util.Set defsites_a;
                            if (!defsites.TryGetValue(a, out defsites_a))
                            {
                                defsites_a = new util.Set(input.LinearStream.Count);
                                defsites[a] = defsites_a;
                                vars.Add(a);
                                var_locs[a] = p;
                            }

                            int ct;
                            if (!ctypes.TryGetValue(a, out ct))
                                ct = ir.Opcode.ct_unknown;
                            defsites_a.set(oc_idx);
                            Aorig[oc_idx].set(a);

                            if (ct == ir.Opcode.ct_unknown)
                                ct = p.ct;
                            ctypes[a] = ct;
                        }
                    }
                }
                oc_idx++;
            }

            /* Appel 19.6 */
            Dictionary<int, util.Set> Aphi =
                new Dictionary<int, util.Set>(new GenericEqualityComparer<int>());
            foreach (var a in vars)
                Aphi[a] = new util.Set();

            foreach (var a in vars)
            {
                var W = defsites[a].Clone();

                while(!W.Empty)
                {
                    var n = W.get_first_set();
                    W.unset(n);

                    foreach(var y in input.DominanceGraph.df[ocs[n].n.bb])
                    {
                        var y2 = (input.bb_starts[y].c as Opcode).oc_idx;
                        if(!Aphi[a].get(y2))
                        {
                            // Insert phi function
                            var o = input.LinearStream[y2].c as Opcode;
                            int preds = input.LinearStream[y2].PrevCount;

                            Opcode phi = new Opcode();
                            phi.oc = ir.Opcode.oc_phi;
                            phi.defs = new Param[1];
                            phi.uses = new Param[preds];
                            phi.defs[0] = new ir.Param { t = var_locs[a].t, v = a, ud = ir.Param.UseDefType.Def, ct = ctypes[a] };
                            for (int i = 0; i < preds; i++)
                                phi.uses[i] = new ir.Param { t = var_locs[a].t, v = a, ud = ir.Param.UseDefType.Use };

                            o.phis.Add(phi);

                            Aphi[a].set(y2);
                            if (!Aorig[y].get(a))
                                W.set(y2);
                        }
                    }
                }
            }

            /* Appel 19.7, p437

                We adapt it slightly so that instead of naming variables
                1_1, 1_2, 2_1 etc, they all get a new number */
            int next_ssa_var = 1;

            Dictionary<int, util.Stack<int>> Stack =
                new Dictionary<int, util.Stack<int>>(
                    new GenericEqualityComparer<int>());

            foreach(var a in vars)
            {
                Stack[a] = new util.Stack<int>();
                //Stack[a].Push(next_ssa_var++);
            }

            foreach (var n in input.DominanceGraph.Starts)
                Rename(n, input, ref next_ssa_var, Stack);

            input.next_vreg_id = next_ssa_var;

            return input;
        }

        private static void Rename(BaseNode Dn, Graph input, ref int next_ssa_var, Dictionary<int, util.Stack<int>> stack)
        {
            var n = ((DomNodeContents)Dn.c).SrcNode;

            /* n is the first node in this block */
            var bb_id = n.bb;

            for (var o_idx = 0; o_idx < input.blocks[bb_id].Count; o_idx++)
            {
                var o = input.blocks[bb_id][o_idx].c as Opcode;

                int mc_idx = 0;

                foreach (var S in o.all_insts)
                {
                    bool is_phi = false;
                    if (S.oc == ir.Opcode.oc_phi)
                        is_phi = true;

                    var oc_id = new Opcode.OpcodeId();
                    oc_id.g = input;
                    oc_id.ls_idx = o.oc_idx;

                    var mc_idx_adj = mc_idx;
                    var oc_type = 1;
                    if (mc_idx < o.phis.Count)
                    {
                        mc_idx_adj -= o.phis.Count;
                        oc_type = 0;
                    }
                    oc_id.mc_idx = mc_idx;
                    oc_id.oc_type = oc_type;

                    if (!is_phi)
                    {
                        if (S.uses != null)
                        {
                            foreach (var p in S.uses)
                            {
                                if (p.IsStack)
                                {
                                    var x = (int)p.v;
                                    var i = stack[x].Peek();
                                    p.ssa_idx = i;

                                    Set<Opcode.OpcodeId> uses;
                                    if(!input.uses.TryGetValue(i, out uses))
                                    {
                                        uses = new Set<Opcode.OpcodeId>();
                                        input.uses[i] = uses;
                                        input.defs[i] = new Set<Opcode.OpcodeId>();
                                    }
                                    uses.Add(oc_id);
                                }
                            }
                        }
                    }
                    if (S.defs != null)
                    {
                        foreach (var p in S.defs)
                        {
                            if (p.IsStack)
                            {
                                var a = (int)p.v;
                                var i = next_ssa_var++;
                                stack[a].Push(i);
                                p.ssa_idx = i;

                                Set<Opcode.OpcodeId> defs;
                                if (!input.defs.TryGetValue(i, out defs))
                                {
                                    defs = new Set<Opcode.OpcodeId>();
                                    input.defs[i] = defs;
                                    input.uses[i] = new Set<Opcode.OpcodeId>();
                                }
                                defs.Add(oc_id);
                            }
                        }
                    }

                    mc_idx++;
                }

                /* Special handling of the last instruction in each bb */
                if (o_idx == input.blocks[bb_id].Count - 1)
                {
                    foreach (var Y in input.blocks[bb_id][o_idx].Next)
                    {
                        int j = 0;
                        foreach (var prev in Y.Prev)
                        {
                            if (prev == input.blocks[bb_id][o_idx])
                                break;
                            j++;
                        }
                        // n is now the jth predecessor of Y

                        var oy = Y.c as Opcode;
                        mc_idx = 0;
                        foreach (var phi in oy.phis)
                        {
                            var a = (int)phi.defs[0].v;
                            var i = stack[a].Peek();
                            phi.uses[j].ssa_idx = i;

                            var oc_id = new Opcode.OpcodeId();
                            oc_id.g = input;
                            oc_id.ls_idx = oy.oc_idx;

                            oc_id.mc_idx = mc_idx;
                            oc_id.oc_type = 0;

                            Set<Opcode.OpcodeId> uses;
                            if (!input.uses.TryGetValue(i, out uses))
                            {
                                uses = new Set<Opcode.OpcodeId>();
                                input.uses[i] = uses;
                                input.defs[i] = new Set<Opcode.OpcodeId>();
                            }
                            uses.Add(oc_id);

                            mc_idx++;
                        }
                    }

                    foreach (var X in Dn.Next)
                        Rename(X, input, ref next_ssa_var, stack);

                    foreach (var S in o.all_insts)
                    {
                        if (S.defs != null)
                        {
                            foreach (var p in S.defs)
                            {
                                if (p.IsStack)
                                {
                                    var a = (int)p.v;
                                    stack[a].Pop();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
