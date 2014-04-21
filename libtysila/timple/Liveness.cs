/* Copyright (C) 2014 by John Cronin
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

namespace libtysila.timple
{
    /** <summary>Determines liveness data on a per-variable and per-node basis</summary> */
    public class Liveness
    {
        public Dictionary<BaseNode, util.Set<vara>> live_in = new Dictionary<BaseNode, util.Set<vara>>();
        public Dictionary<BaseNode, util.Set<vara>> live_out = new Dictionary<BaseNode, util.Set<vara>>();

        public Dictionary<vara, util.Set<BaseNode>> defs = new Dictionary<vara, util.Set<BaseNode>>();
        public Dictionary<vara, util.Set<BaseNode>> uses = new Dictionary<vara, util.Set<BaseNode>>();

        public static Liveness LivenessAnalysis(BaseGraph g)
        {
            Liveness l = new Liveness();

            bool changes = false;
            int iter = 0;

            IList<BaseNode> nodes = g.LinearStream;

            /* See Appel p. 214 */

            foreach (BaseNode n in nodes)
            {
                l.live_in[n] = new util.Set<vara>();
                l.live_out[n] = new util.Set<vara>();
            }

            do
            {
                changes = false;

                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    /* Iterate backwards for a more efficient algorithm */
                    BaseNode n = nodes[i];

                    /* On the first run through, set the global uses/defs chain too */
                    if (iter == 0)
                    {
                        foreach (vara use in n.uses)
                        {
                            if (!l.uses.ContainsKey(use))
                                l.uses[use] = new util.Set<BaseNode>();
                            l.uses[use].Add(n);
                        }

                        foreach (vara def in n.defs)
                        {
                            if (!l.defs.ContainsKey(def))
                                l.defs[def] = new util.Set<BaseNode>();
                            l.defs[def].Add(n);
                        }
                    }

                    /* Generate in and out sets */
                    util.Set<vara> inp = new util.Set<vara>(l.live_in[n]);
                    util.Set<vara> outp = new util.Set<vara>(l.live_out[n]);
                    l.live_in[n] = l.live_out[n].Except(n.defs).Union(n.uses);

                    l.live_out[n].Clear();
                    foreach (BaseNode s in n.Next)
                        l.live_out[n].AddRange(l.live_in[s]);

                    if (!inp.Equals(l.live_in[n]) || !outp.Equals(l.live_out[n]))
                        changes = true;
                }

                iter++;
            } while (changes);

            return l;
        }

        public void TrimEmpty()
        {
            List<vara> def_remove = new List<vara>();
            foreach (KeyValuePair<vara, util.Set<BaseNode>> def_kvp in defs)
            {
                if (def_kvp.Value.Count == 0)
                    def_remove.Add(def_kvp.Key);
            }

            List<vara> use_remove = new List<vara>();
            foreach (KeyValuePair<vara, util.Set<BaseNode>> use_kvp in uses)
            {
                if (use_kvp.Value.Count == 0)
                    use_remove.Add(use_kvp.Key);
            }

            foreach (vara def_r in def_remove)
                defs.Remove(def_r);

            foreach (vara use_r in use_remove)
                uses.Remove(use_r);
        }
    }
}
