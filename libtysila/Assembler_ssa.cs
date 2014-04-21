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

namespace libtysila
{
    partial class Assembler
    {
        TACList ConvertToSSA(TACList tacs)
        {
            /* First, insert phi functions.
             * 
             * According to Appel, we add phi functions at the start of a join-point 'z' for the
             * variable 'a' if all of the following are true:
             * 
             * 1) There is a block 'x' containing a definition of 'a'.
             * 2) There is a block 'y' (with y != x) containing a definition of 'a'.
             * 3) There is a non-empty path Pxz of edges from 'x' to 'z'.
             * 4) There is a non-empty path Pyz of edges from 'y' to 'z'.
             * 5) Paths Pxz and Pyz do not have any node in common other than 'z'
             * 6) The node 'z' does not appear in both Pxz and Pyz before the last node, although
             *     it may appear in one or the other
             */

            /* Iterate through each variable - use the 'uses' array (this will only look at variables
             * which are actually used, and also accounts for local args which are not necessarily
             * defined anywhere).
             * 
             * We are using the algorithm in Appel p 435 here (19.6) */
            List<var> var_list = new List<var>(tacs.liveness_uses.Keys);
            Dictionary<var, List<int>> Aa = new Dictionary<var, List<int>>();
            for (int i = 0; i < var_list.Count; i++)
                Aa[var_list[i]] = new List<int>();

            foreach (var a in var_list)
            {
                List<ThreeAddressCode> W = new List<ThreeAddressCode>(tacs.liveness_defs[a]);

                while (W.Count > 0)
                {
                    /* Remove and inspect node n */
                    int n = tacs.tacs.IndexOf(W[W.Count - 1]);
                    W.RemoveAt(W.Count - 1);

                    /* foreach y in DF[n] */
                    for (int y = 0; y < tacs.num_nodes; y++)
                    {
                        if (tacs.df[n, y])
                        {
                            /* if y is not in Aa[a] (set of nodes which must have phi functions for a) */
                            if (!Aa[a].Contains(y))
                            {
                                Aa[a].Add(y);

                                /* if a is not defined in y, add y to W */
                                if (!tacs.liveness_defs[a].Contains(tacs.tacs[y]))
                                    W.Add(tacs.tacs[y]);
                            }
                        }
                    }
                }
            }

            /* Now actually insert the phi functions, and create a new instruction stream
             * 
             * We maintain a list of old indexes -> new indexes and also a list of the indexes
             * of new phi instructions
             */

            int[] old_to_new = new int[tacs.num_nodes];
            List<int> phi_locs = new List<int>();
            List<int> phi_labels = new List<int>();

            List<ThreeAddressCode> ssa = new List<ThreeAddressCode>();
            for (int i = 0; i < tacs.num_nodes; i++)
            {
                ThreeAddressCode tac = tacs.tacs[i];

                old_to_new[i] = ssa.Count;

                ssa.Add(tac.Clone());

                for (int v = 0; v < var_list.Count; v++)
                {
                    var vres = var_list[v];
                    List<int> phis_for_v = Aa[vres];
                    if (phis_for_v.Contains(i))
                    {
                        /* We need to insert a phi function here.
                         * 
                         * If the current instruction is a label, insert after it, else fault */
                        if (!(tac is LabelEx))
                            throw new Exception("phi function at a place not marked by label");

                        phi_locs.Add(ssa.Count);
                        phi_labels.Add(old_to_new[i]);

                        /* The number of parameters of the phi function is the number of predecessors
                         * of the label node */

                        int param_count = tacs.inst_pred[i].Count;
                        PhiEx2 phi = new PhiEx2(vres, param_count);
                        ssa.Add(phi);
                    }
                }
            }

            TACList ssatl = new TACList(ssa);
            DetermineUsesAndDefs(ssatl);
            DetermineJoinPoints(ssatl);
            GenerateDominatorTree(ssatl);


            foreach (var v in var_list)
                ssatl.ssa_stack[v] = new List<int>();

            SSARename(ssatl, 0, -1, phi_locs, phi_labels);

            throw new NotImplementedException();
        }

        private void SSARename(TACList ssatl, int n, int prev_n, IList<int> phi_locs, IList<int> phi_labels)
        {
            ThreeAddressCode tac = ssatl.tacs[n];

            if (tac is PhiEx2)
            {
                /* determine the index we are coming from */
                int phi_idx = phi_locs.IndexOf(n);

            }
        }
    }
}
