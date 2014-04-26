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
    public abstract class BaseGraph
    {
        public List<BaseNode> Starts = new List<BaseNode>();
        public List<BaseNode> Ends = new List<BaseNode>();
        public Dictionary<BaseNode, BaseNode> InnerToOuter = new Dictionary<BaseNode, BaseNode>();

        protected BaseGraph innergraph;
        public BaseGraph InnerGraph { get { return innergraph; } }

        public int Count
        {
            get
            {
                util.Set<BaseNode> visited = new util.Set<BaseNode>();
                int n = 0;
                foreach (BaseNode node in Starts)
                    DFCount(node, visited, ref n);
                return n;
            }
        }

        private void DFCount(BaseNode s, util.Set<BaseNode> visited, ref int n)
        {
            if (!visited.Contains(s))
            {
                visited.Add(s);

                foreach (BaseNode suc in s.Next)
                    DFCount(suc, visited, ref n);

                n++;
            }
        }

        public IList<BaseNode> LinearStream
        {
            get
            {
                BaseNode[] ret = new BaseNode[Count];
                util.Set<BaseNode> visited = new util.Set<BaseNode>();
                int n = Count - 1;
                foreach (BaseNode s in Starts)
                    DFS(s, ret, ref n, visited);
                return ret;
            }
        }

        private void DFS(BaseNode s, BaseNode[] ret, ref int n, util.Set<BaseNode> visited)
        {
            /* algorithm 17.5 from Appel */
            if (visited.Contains(s) == false)
            {
                visited.Add(s);

                /* we do the last successor first, as that is the non-default path, and should come last */
                for (int i = s.Next.Count - 1; i >= 0; i--)
                    DFS(s.Next[i], ret, ref n, visited);

                ret[n] = s;
                n--;
            }
        }
    }

    public class TimpleGraph : BaseGraph
    {
        //public List<TreeNode> Starts = new List<TreeNode>();
        //public List<TreeNode> Ends = new List<TreeNode>();
        //public int Count { get { return count; } }
        public Dictionary<int, Assembler.CliType> VarDataTypes = new Dictionary<int, Assembler.CliType>();

        public void AddStartNode(TreeNode n)
        {
            if (!Starts.Contains(n))
                Starts.Add(n);
            if (!Ends.Contains(n))
                Ends.Add(n);
        }

        public void AddTreeEdge(TreeNode from, TreeNode to)
        {
            if (Ends.Contains(from))
                Ends.Remove(from);

            if (!from.next.Contains(to))
                from.next.Add(to);

            if (!to.prev.Contains(from))
                to.prev.Add(from);

            if (to.next.Count == 0)
                Ends.Add(to);
        }

        public void RemoveNode(TreeNode n, Liveness l)
        {
            /* Remove the node */

            /* If we have multiple successors and predecessors, we cannot remove this node */
            if (n.prev.Count > 1 && n.next.Count > 1)
                throw new Exception("Attempt to remove node with multiple predecessors and successors");

            /* Fix up phis in the successors */
            foreach (TreeNode next in n.next)
            {
                if (next is TimpleLabelNode)
                {
                    TimpleLabelNode tln = next as TimpleLabelNode;

                    foreach (TimplePhiInstNode phi in tln.Phis)
                    {
                        if (phi.VarArgs.ContainsKey(n))
                        {
                            vara prev_vara = phi.VarArgs[n];
                            phi.VarArgs.Remove(n);

                            foreach (TreeNode prev in n.prev)
                                phi.VarArgs[prev] = prev_vara;

                            if ((n.prev.Count == 0) && (prev_vara.VarType == vara.vara_type.Logical))
                                l.uses[prev_vara].Remove(tln);
                        }
                    }
                }
            }

            foreach (TreeNode prev in n.prev)
            {
                prev.next.Remove(n);
                foreach (TreeNode next in n.next)
                {
                    if (!prev.next.Contains(next))
                        prev.next.Add(next);
                }
            }

            foreach (TreeNode next in n.next)
            {
                next.prev.Remove(n);
                foreach (TreeNode prev in n.prev)
                {
                    if (!next.prev.Contains(prev))
                        next.prev.Add(prev);
                }
            }
        }

        public void RemoveEdge(TreeNode from, TreeNode to)
        {
            from.next.Remove(to);
            to.prev.Remove(from);

            if (to is TimpleLabelNode)
            {
                TimpleLabelNode tln = to as TimpleLabelNode;

                foreach (TimplePhiInstNode phi in tln.Phis)
                {
                    if (phi.VarArgs.ContainsKey(from))
                        phi.VarArgs.Remove(from);
                }
            }
        }

        public static TimpleGraph BuildGraph(IList<TreeNode> tacs)
        {
            /* Build a timple graph from a linear list of instructions */
            TimpleGraph ret = new TimpleGraph();

            /* Identify block starts, and reset visited count */
            Dictionary<int, int> block_starts = new Dictionary<int, int>();
            for (int i = 0; i < tacs.Count; i++)
            {
                if (tacs[i] is TimpleLabelNode)
                {
                    TimpleLabelNode l = tacs[i] as TimpleLabelNode;
                    if (l.BlockId != -1)
                        block_starts.Add(l.BlockId, i);
                }
                tacs[i].visited = false;

                /* Store the types of the variables */
                if (tacs[i] is TimpleNode)
                {
                    TimpleNode tn = tacs[i] as TimpleNode;
                    if (tn.R.VarType == vara.vara_type.Logical)
                        ret.VarDataTypes[tn.R.LogicalVar] = tn.R.DataType;
                }
            }

            /* Add the root node */
            ret.AddStartNode(tacs[0]);
            AddChildren(ret, 0, tacs, block_starts);

            return ret;
        }

        private static void AddChildren(TimpleGraph g, int idx, IList<TreeNode> tacs, Dictionary<int, int> block_starts)
        {
            /* The child nodes are:
             * 1) the next in the list (idx + 1) if it exists and the instruction falls through
             * 2) the block target specified if this is a Br instruction
             */

            tacs[idx].visited = true;

            TreeNode next_in_list = null;
            int next_idx = 0;
            if (((idx + 1) < tacs.Count) && (ThreeAddressCode.OpFallsThrough(tacs[idx].Op)))
            {
                next_in_list = tacs[idx + 1];
                next_idx = idx + 1;
            }

            TreeNode block_target = null;
            int block_idx = 0;
            if (tacs[idx] is TimpleBrNode)
            {
                TimpleBrNode br = tacs[idx] as TimpleBrNode;
                block_target = tacs[block_starts[br.BlockTarget]];
                block_idx = block_starts[br.BlockTarget];
                if (block_target == next_in_list)
                    block_target = null;
            }

            if (next_in_list != null)
            {
                g.AddTreeEdge(tacs[idx], next_in_list);
                if (!next_in_list.visited)
                    AddChildren(g, next_idx, tacs, block_starts);
            }

            if (block_target != null)
            {
                g.AddTreeEdge(tacs[idx], block_target);
                if (!block_target.visited)
                    AddChildren(g, block_idx, tacs, block_starts);
            }
        }

        public TimpleGraph BuildParentGraph()
        {
            TimpleGraph ret = new TimpleGraph();
            return BuildParentGraph(ret);
        }

        public TimpleGraph BuildParentGraph(TimpleGraph dest)
        {
            TimpleGraph ret = dest;

            Dictionary<BaseNode, BaseNode> old_to_new = new Dictionary<BaseNode, BaseNode>();

            IList<BaseNode> nodes = LinearStream;
            foreach (TreeNode n in nodes)
            {
                TreeNode newnode = (TreeNode)n.MemberwiseClone();
                newnode.prev = new List<BaseNode>();
                newnode.next = new List<BaseNode>();
                newnode.InnerNode = n;
                old_to_new[n] = newnode;
            }

            util.Set<TreeNode> visited = new util.Set<TreeNode>();

            foreach (TreeNode n in Starts)
            {
                ret.AddStartNode((TreeNode)old_to_new[n]);
                DFAdd(ret, n, old_to_new, visited);
            }

            ret.InnerToOuter = old_to_new;

            return ret;
        }

        private void DFAdd(TimpleGraph ret, TreeNode n, Dictionary<BaseNode, BaseNode> old_to_new, util.Set<TreeNode> visited)
        {
            if (!visited.Contains(n))
            {
                visited.Add(n);

                foreach (TreeNode next in n.next)
                {
                    ret.AddTreeEdge((TreeNode)old_to_new[n], (TreeNode)old_to_new[next]);
                    DFAdd(ret, next, old_to_new, visited);
                }
            }
        }
    }
}
