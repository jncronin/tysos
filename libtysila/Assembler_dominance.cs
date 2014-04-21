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
        void GenerateDominatorTree(TACList tacs)
        {
            int num_nodes = tacs.tacs.Count;
            int start = 0;

            bool[,] doms = new bool[num_nodes,num_nodes];

            /* Dominator of the start node is the start itself */
            for (int i = 0; i < num_nodes; i++)
            {
                if (i == start)
                    doms[start, i] = true;
                else
                    doms[start, i] = false;
            }

            /* For all other nodes, set all nodes as the dominators */
            for (int n = 0; n < num_nodes; n++)
            {
                if (n != start)
                {
                    for (int i = 0; i < num_nodes; i++)
                        doms[n, i] = true;
                }
            }

            /* Iteratively eliminate nodes that are not dominators */
            bool changes;
            do
            {
                changes = false;

                for (int n = 0; n < num_nodes; n++)
                {
                    if (n != start)
                    {
                        /* Dom(n) = { n } union with intersection over Dom(p) for all p in pred(n) */

                        /* First, get the predecessors of the node: pred(n) */
                        IList<int> pred = tacs.inst_pred[n];

                        /* Generate the intersection of the dominators of all members of pred(n), union with n */
                        List<int> dom = new List<int>();
                        for (int i = 0; i < num_nodes; i++)
                        {
                            if (i == n)
                                dom.Add(i);
                            else
                            {
                                bool isect = true;
                                foreach (int p in pred)
                                {
                                    /* is i a dominator of p? */
                                    if (doms[p, i] == false)
                                    {
                                        isect = false;
                                        break;
                                    }
                                }
                                if (isect)
                                    dom.Add(i);
                            }
                        }

                        /* Compare the new dominator list with the old one */

                        /* Assume the new list will be equal to, or a subset of the old one.
                         * 
                         * Therefore, if any points are set in the old list which are not set in the
                         * new one, we have a change */
                        bool change = false;
                        for (int i = 0; i < num_nodes; i++)
                        {
                            if (doms[n, i] && !dom.Contains(i))
                            {
                                change = true;
                                break;
                            }
                        }
                        if (change)
                        {
                            /* Set the dominator list to the new one */
                            for (int i = 0; i < num_nodes; i++)
                                doms[n, i] = false;
                            foreach (int i in dom)
                                doms[n, i] = true;
                            changes = true;
                        }
                    }
                }

            } while (changes);

            tacs.doms = doms;

            /* Now calculate immediate dominators.  These are defined as idom(n) such that:
             * 1) idom(n) != n
             * 2) idom(n) dominates n (already calculated)
             * 3) idom(n) does not dominate any other dominator of n
             * 
             * The start node does not have an immediate dominator
             */

            int[] idoms = new int[num_nodes];
            for (int n = 0; n < num_nodes; n++)
            {
                if (n == start)
                    idoms[n] = -1;
                else
                {
                    /* Iterate the dominators of n */
                    for (int idom = 0; idom < num_nodes; idom++)
                    {
                        if ((n != idom) && doms[n, idom])
                        {
                            /* now idom strictly dominates n.
                             * 
                             * Ensure it does not dominate any other dominator of n
                             */

                            bool allowed = true;
                            for (int otherdom = 0; otherdom < num_nodes; otherdom++)
                            {
                                if ((n != otherdom) && (otherdom != idom) && doms[n, otherdom])
                                {
                                    if (doms[otherdom, idom])
                                    {
                                        allowed = false;
                                        break;
                                    }
                                }   
                            }
                            if (allowed)
                            {
                                idoms[n] = idom;
                                break;
                            }
                        }
                    }
                }
            }

            tacs.idoms = idoms;

            /* Finally, calculate dominance frontiers (the set of all nodes which are the targets
             * of edges leaving the region dominated by a node).
             * 
             * See Appel p433
             * 
             */

            tacs.df = new bool[num_nodes, num_nodes];
            for (int i = 0; i < num_nodes; i++)
            {
                for (int j = 0; j < num_nodes; j++)
                    tacs.df[i, j] = false;
            }

            computeDF(tacs, start, num_nodes);
        }

        private void computeDF(TACList tacs, int n, int num_nodes)
        {
            /* for each node y in succ[n]
             *      if idom(y) != n
             *          DF.Add(y)
             */

            foreach (int y in tacs.inst_suc[n])
            {
                if ((tacs.idoms[y] != -1) && (tacs.idoms[y] != n))
                    tacs.df[n, y] = true;
            }

            /* foreach child c of n in the dominator tree (those with idom[c] = n)
             *      computeDF[c]
             *      for each element w of DF[c]
             *          if n does not dominate w
             *              DF.Add(w)
             */

            for (int c = 0; c < num_nodes; c++)
            {
                if (tacs.idoms[c] == n)
                {
                    computeDF(tacs, c, num_nodes);

                    for (int w = 0; w < num_nodes; w++)
                    {
                        if (tacs.df[c, w])
                        {
                            if (tacs.doms[w, n] == false)
                                tacs.df[n, w] = true;
                        }
                    }
                }
            }
        }
    }
}
