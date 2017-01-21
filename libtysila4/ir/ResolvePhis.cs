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

namespace libtysila4.ir
{
    public class ResolvePhis
    {
        public static graph.Graph ResolvePhisPass(graph.Graph g, target.Target t)
        {
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as Opcode;

                int p_idx = 0;
                foreach(var p in n.Prev)
                {
                    var p_mcn = p.c as Opcode;

                    foreach(var phi in mcn.phis)
                    {
                        if (phi.defs == null || phi.defs.Length == 0)
                            continue;

                        var phi_dest = phi.defs[0];
                        var phi_src = phi.uses[p_idx];

                        if (phi_src.ssa_idx == -1)
                            continue;
                        if (phi_src.ssa_idx == phi_dest.ssa_idx)
                            continue;

                        List<Opcode> insts_loc = p_mcn.post_insts;

                        /* If we are adding phi resolutions to a branch,
                        we need to do the resolutions before the branch instruction
                        or they will never get executed.  This is safe because
                        br will actually assign to any variables */
                        if (p_mcn.oc == Opcode.oc_br)
                            insts_loc = p_mcn.pre_insts;

                        if(p_mcn.oc == Opcode.oc_brif)
                        {
                            /* If we are adding to a brif, then the default branch (Next1)
                            is to always fall through, so we can add to this,
                            otherwise we need to insert a new block that contains a simple
                            jump to the block and insert things there (we handle this later) */

                            if (p_mcn.n.Next1 != n)
                                insts_loc = p_mcn.pre_insts;
                        }

                        insts_loc.Add(
                            new Opcode
                            {
                                oc = ir.Opcode.oc_store,
                                uses = new Param[] { phi_src },
                                defs = new Param[] { phi_dest },
                                n = p_mcn.n
                            });
                    }

                    p_idx++;
                }

                mcn.phis.Clear();
            }

            List<graph.BaseNode> new_nodes = new List<graph.BaseNode>();
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as Opcode;
                if(mcn.oc == Opcode.oc_brif)
                {
                    var if_next = n.Next2;
                    var split_insts = mcn.pre_insts;
                    mcn.pre_insts = new List<Opcode>();
                    int bb = g.blocks.Count;

                    /* We don't need to insert a new block if
                    there are no phi instructions between this
                    node and the next */
                    if (split_insts.Count == 0)
                        continue;

                    /* Build a new node for the branch */
                    var new_oc = new Opcode
                    {
                        oc = Opcode.oc_br,
                        pre_insts = split_insts,
                        uses = new Param[] { },
                        defs = new Param[] { }
                    };
                    var new_oc_n = new graph.Node
                    {
                        c = new_oc,
                        bb = bb,
                        g = g
                    };

                    foreach (var phi in split_insts)
                        phi.n.bb = bb;

                    /* Patch up the block edges */
                    new_oc_n.AddPrev(n);
                    new_oc_n.AddNext(if_next);

                    if_next.ReplacePrev(n, new_oc_n);
                    n.ReplaceNext(if_next, new_oc_n);

                    /* Set up block info in the graph */
                    g.blocks.Add(new List<graph.BaseNode> { new_oc_n });
                    g.bbs_after.Add(new List<int> { if_next.bb });
                    g.bbs_before.Add(new List<int> { n.bb });
                    g.bb_starts.Add(new_oc_n);
                    g.bb_ends.Add(new_oc_n);

                    /* Change bbs_before/after for the old previous and next nodes */
                    for(int i = 0; i < g.bbs_after[n.bb].Count; i++)
                    {
                        if (g.bbs_after[n.bb][i] == if_next.bb)
                            g.bbs_after[n.bb][i] = bb;
                    }
                    for(int i = 0; i < g.bbs_before[if_next.bb].Count; i++)
                    {
                        if (g.bbs_before[if_next.bb][i] == n.bb)
                            g.bbs_before[if_next.bb][i] = bb;
                    }

                    new_nodes.Add(new_oc_n);
                }
            }

            g.LinearStream.AddRange(new_nodes);

            return g;
        }
    }
}
