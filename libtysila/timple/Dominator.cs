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
    public class DomTree : TimpleGraph
    {
        /** <summary>Mapping from node to a set of those nodes which dominate it</summary> */
        Dictionary<BaseNode, util.Set<BaseNode>> doms = new Dictionary<BaseNode, util.Set<BaseNode>>();

        /** <summary>Mapping from node to a set of nodes which it dominates</summary> */
        Dictionary<BaseNode, util.Set<BaseNode>> dominates = new Dictionary<BaseNode, util.Set<BaseNode>>();

        /** <summary>Mapping from node to its immediate dominator</summary> */
        Dictionary<BaseNode, BaseNode> idoms = new Dictionary<BaseNode, BaseNode>();

        /** <summary>Mapping from node to the immediate children in the dominance tree</summary> */
        Dictionary<BaseNode, util.Set<BaseNode>> ichild = new Dictionary<BaseNode, util.Set<BaseNode>>();

        /** <summary>Dominance frontiers of each node</summary> */
        public Dictionary<BaseNode, util.Set<BaseNode>> dfs = new Dictionary<BaseNode, util.Set<BaseNode>>();

        public static DomTree BuildDomTree(TimpleGraph g)
        {
            /* Build a dominator tree from the graph g */
            DomTree ret = new DomTree();
            ret.innergraph = g;

            IList<BaseNode> nodes = g.LinearStream;

            /* Set the dominators of start nodes to be the node itself, and all other nodes to have
             * all other nodes be its dominators.  Also, initialize the data structures */
            foreach (TreeNode n in nodes)
            {
                ret.doms[n] = new util.Set<BaseNode>();
                ret.dominates[n] = new util.Set<BaseNode>();
                ret.ichild[n] = new util.Set<BaseNode>();
                ret.dfs[n] = new util.Set<BaseNode>();

                if (g.Starts.Contains(n))
                    ret.doms[n].Add(n);
                else
                    ret.doms[n].AddRange(nodes);
            }

            /* Iteratively eliminate nodes that are not dominators */
            bool changes = false;
            do
            {
                changes = false;

                foreach (TreeNode n in nodes)
                {
                    if (!g.Starts.Contains(n))
                    {
                        /* Dom(n) = {n} U intersection of dominators of all predecessors of n */
                        util.Set<BaseNode> isect = new util.Set<BaseNode>();
                        isect.AddRange(ret.doms[n.prev[0]]);
                        for (int i = 1; i < n.prev.Count; i++)
                        {
                            isect = isect.Intersect(ret.doms[n.prev[i]]);
                        }
                        isect.Add(n);
                        
                        /* If this new dominator set is different from the currently saved one, update */
                        if(!isect.Equals(ret.doms[n]))
                        {
                            ret.doms[n] = isect;
                            changes = true;
                        }
                    }
                }
            } while (changes);

            /* Generate a mapping of those nodes which a particular node dominates */
            foreach (TreeNode n in nodes)
            {
                foreach (TreeNode dom in ret.doms[n])
                    ret.dominates[dom].Add(n);
            }

            /* Generate immediate dominators:
             * 
             * These are dominators which:
             * 1) are not the same node as that which it dominates
             * 2) do not dominate any other dominator of the node
             */
            foreach (TreeNode n in nodes)
            {
                ret.idoms[n] = null;

                util.Set<BaseNode> doms_n = ret.doms[n];
                foreach (TreeNode idom in doms_n)
                {
                    /* Skip if its the same as the node in question (rule 1 above) */
                    if (idom == n)
                        continue;

                    /* Ensure it does not dominate any other dominator of the node */
                    util.Set<BaseNode> test = doms_n.Intersect(ret.dominates[idom]);

                    if (test.Count == 2) /* Account for the n node and the idom node */
                        ret.idoms[n] = idom;
                }
            }

            /* Build the mappings to immediate children */
            foreach (TreeNode n in nodes)
            {
                if (ret.idoms[n] != null)
                    ret.ichild[ret.idoms[n]].Add(n);
            }

            /* Add to the tree */
            foreach (TreeNode n in g.Starts)
            {
                ParentNode dtn = new ParentNode(n);
                ret.AddStartNode(dtn);
                AddDomNode(ret, dtn);
            }

            /* Calculate dominance frontiers */
            foreach (TreeNode n in ret.Starts)
            {
                computeDF(ret, n);
            }

            return ret;
        }

        private static void computeDF(DomTree ret, TreeNode n)
        {
            /* Appel p. 434 */
            util.Set<BaseNode> S = new util.Set<BaseNode>();

            foreach (TreeNode y in n.InnerNode.next)
            {
                if (ret.idoms[y] != n.InnerNode)
                    S.Add(y);
            }

            foreach (ParentNode c in n.next)
            {
                computeDF(ret, c);

                foreach (TreeNode w in ret.dfs[c.InnerNode])
                {
                    if (!ret.doms[w].Contains(n.InnerNode))
                        S.Add(w);
                }
            }

            ret.dfs[n.InnerNode] = S;
        }

        private static void AddDomNode(DomTree ret, TreeNode n)
        {
            foreach (TreeNode child in ret.ichild[n.InnerNode])
            {
                TreeNode dtn = new ParentNode(child);
                ret.AddTreeEdge(n, dtn);
                AddDomNode(ret, dtn);
            }
        }
    }
}
