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

namespace libtysila4.graph
{
    public class DominanceGraph : Graph
    {
        public Dictionary<int, util.Set> dom =
            new Dictionary<int, util.Set>(
                new GenericEqualityComparer<int>());
        public Dictionary<int, util.Set> df =
            new Dictionary<int, util.Set>(
                new GenericEqualityComparer<int>());
        public Dictionary<int, int> idom =
            new Dictionary<int, int>(
                new GenericEqualityComparer<int>());

        public static graph.Graph GenerateDominanceGraph(graph.Graph input, target.Target t)
        {
            var dg = new DominanceGraph();

            // This assumes each node is a basic block

            // First build a set containing all blocks set
            var all_set = new util.Set();
            foreach (var n in input.bb_starts)
                all_set.set(n.bb);
            var nodes_visited = new Set();

            // Appel 18.1
            foreach(var n in input.bb_starts)
            {
                if (input.Starts.Contains(n))
                {
                    dg.dom[n.bb] = new util.Set();
                    dg.dom[n.bb].set(n.bb);
                    nodes_visited.set(n.bb);
                }
                else
                    dg.dom[n.bb] = all_set.Clone();
            }

            bool changes;
            do
            {
                changes = false;
                foreach (var n in input.bb_starts)
                {
                    HandleNode(n, dg.dom, ref changes);
                }
            } while (changes);

            input.DominanceGraph = dg;

            // Calculate immediate dominators
            foreach(var n in input.bb_starts)
            {
                var Ds_n = dg.dom[n.bb];

                foreach(var D_n in Ds_n)
                {
                    if (D_n == n.bb)
                        continue;

                    bool dominates_other = false;
                    foreach(var other_D_n in Ds_n)
                    {
                        if (other_D_n == D_n)
                            continue;
                        if (other_D_n == n.bb)
                            continue;
                        var Ds_other_D_n = dg.dom[other_D_n];
                        if(Ds_other_D_n.get(D_n))
                        {
                            dominates_other = true;
                            break;
                        }
                    }
                    if(dominates_other == false)
                    {
                        dg.idom[n.bb] = D_n;
                        break;
                    }
                }
            }

            /* Build dominator tree */
            Dictionary<int, graph.BaseNode> dt_idx =
                new Dictionary<int, BaseNode>(
                    new GenericEqualityComparer<int>());

            foreach(var n in input.bb_starts)
            {
                dt_idx[n.bb] = new graph.MultiNode();
                dt_idx[n.bb].bb = n.bb;
                dt_idx[n.bb].c = new DomNodeContents { SrcNode = n };
            }
            foreach(var n in input.bb_starts)
            {
                int idom;
                if(dg.idom.TryGetValue(n.bb, out idom))
                {
                    dt_idx[n.bb].AddPrev(dt_idx[idom]);
                    dt_idx[idom].AddNext(dt_idx[n.bb]);
                }
            }
            foreach (var n in input.Starts)
                dg.Starts.Add(dt_idx[n.bb]);

            /* Compute dominance frontiers */
            foreach (var n in dg.Starts)
                computeDF(n, dg, input);

            return input;
        }

        private static void computeDF(BaseNode n, DominanceGraph dg, Graph input)
        {
            // Appel page 434
            var S = new util.Set();

            foreach(var y in input.bbs_after[n.bb])
            {
                int idom;
                if(dg.idom.TryGetValue(y, out idom) &&
                    idom != n.bb)
                {
                    S.set(y);
                }
            }
            foreach(var c in n.Next)
            {
                computeDF(c, dg, input);
                foreach(var w in dg.df[c.bb])
                {
                    if (!dg.dom[w].get(n.bb) || n.bb == w)
                        S.set(w);
                }
            }

            dg.df[n.bb] = S;
        }

        private static void HandleNode(BaseNode n, Dictionary<int, Set> dom, ref bool changes)
        {
            // Appel 18.1
            // D[n] = {n} | (D[pred1] & D[pred2] & ... & D[predn])

            var old_d = dom[n.bb];

            int i = 0;
            Set d = null;
            if (n.PrevCount == 0)
                d = new Set();
            else
            {
                foreach (var p in n.Prev)
                {
                    if (i == 0)
                        d = dom[p.bb].Clone();
                    else
                        d.Intersect(dom[p.bb]);
                    i++;
                }
            }
            d.set(n.bb);

            dom[n.bb] = d;

            if (d.Equals(old_d) == false)
                changes = true;
        }
    }

    public class DomNodeContents : NodeContents
    {
        public BaseNode SrcNode;

        public override string ToString()
        {
            return "DomNode: " + SrcNode.ToString();
        }
    }
}

