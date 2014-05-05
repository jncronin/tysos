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
    public class SSATree : TimpleGraph
    {
        public Dictionary<vara, util.Set<BaseNode>> Aa = new Dictionary<vara, util.Set<BaseNode>>();

        public static SSATree BuildSSATree(TimpleGraph g, DomTree d, Liveness l)
        {
            SSATree ssa = new SSATree();

            List<vara> vars = new List<vara>(l.defs.Keys);

            /* Algorithm is in Appel p. 435 */
            foreach (vara a in vars)
            {
                util.Set<BaseNode> W = l.defs[a];
                ssa.Aa[a] = new util.Set<BaseNode>();

                while (W.Count > 0)
                {
                    TreeNode n = W.ItemAtIndex(0) as TreeNode;
                    W.Remove(n);

                    foreach (TreeNode y in d.dfs[n])
                    {
                        if (!ssa.Aa[a].Contains(y))
                        {
                            ssa.Aa[a].Add(y);
                            if (!y.defs.Contains(a))
                                W.Add(y);
                        }
                    }
                }
            }

            g.BuildParentGraph(ssa);

            /* Add in phi functions */
            util.Set<TreeNode> visited_outer_labels = new util.Set<TreeNode>();
            foreach (vara a in vars)
            {
                ICollection<BaseNode> philocs = ssa.Aa[a];
                foreach (TreeNode phi in philocs)
                {
                    TimpleLabelNode label = phi as TimpleLabelNode;

                    if (label == null)
                        throw new Exception("Phi instruction at location that is not a label");

                    TimpleLabelNode outer_label = ssa.InnerToOuter[label] as TimpleLabelNode;
                    if (outer_label == null)
                        throw new Exception("Phi instruction at location that is not a label in outer stream");

                    if (!visited_outer_labels.Contains(outer_label))
                    {
                        outer_label.Phis = new List<TimplePhiInstNode>();
                        visited_outer_labels.Add(outer_label);
                    }
                    outer_label.Phis.Add(new TimplePhiInstNode(vara.Logical(a.LogicalVar, g.VarDataTypes[a.LogicalVar])));
                }
            }

            /* Rename variables (Appel p. 437) */
            Dictionary<int, int> Count = new Dictionary<int, int>();
            Dictionary<int, List<int>> Stack = new Dictionary<int, List<int>>();
            foreach (vara a in vars)
            {
                Count[a.LogicalVar] = 0;
                Stack[a.LogicalVar] = new List<int>();
                Stack[a.LogicalVar].Add(0);
            }

            foreach (TreeNode n in d.Starts)
                Rename(n, ssa, d, Count, Stack);

            return ssa;
        }

        private static void Rename(TreeNode n, SSATree ssa, DomTree d, Dictionary<int, int> Count, Dictionary<int, List<int>> Stack)
        {
            /* Get the node in the ssa form (the one we alter) */
            TreeNode S = (TreeNode)ssa.InnerToOuter[n.InnerNode];

            if (!(S is TimpleLabelNode))
            {
                /* Replace all uses of variable x in S with the x.(top of Stack[x]) */

                if (S is TimpleCallNode)
                {
                    TimpleCallNode tcn = S as TimpleCallNode;
                    if(tcn.R.VarType != vara.vara_type.Logical)
                        tcn.R = Replace(tcn.R, Stack);
                    tcn.O1 = Replace(tcn.O1, Stack);
                    for (int i = 0; i < tcn.VarArgs.Count; i++)
                        tcn.VarArgs[i] = Replace(tcn.VarArgs[i], Stack);
                }
                else if (S is TimpleNode)
                {
                    TimpleNode tn = S as TimpleNode;
                    if (tn.R.VarType != vara.vara_type.Logical)
                        tn.R = Replace(tn.R, Stack);
                    tn.O1 = Replace(tn.O1, Stack);
                    tn.O2 = Replace(tn.O2, Stack);
                }
            }

            /* If we define a new variable here, replace it and increment stack */
            if (S is TimpleNode)
            {
                TimpleNode tn = S as TimpleNode;
                if (tn.R.VarType == vara.vara_type.Logical)
                {
                    int i = Count[tn.R.LogicalVar] + 1;
                    Count[tn.R.LogicalVar] = i;
                    Stack[tn.R.LogicalVar].Add(i);
                    tn.R.SSA = i;
                }
            }
            else if (S is TimpleLabelNode)
            {
                foreach (TimplePhiInstNode phi in ((TimpleLabelNode)S).Phis)
                {
                    int i = Count[phi.R.LogicalVar] + 1;
                    Count[phi.R.LogicalVar] = i;
                    Stack[phi.R.LogicalVar].Add(i);
                    phi.R.SSA = i;
                }
            }

            /* Replace successor phi function definitions */
            foreach (TreeNode Yold in ((TreeNode)n.InnerNode).next)
            {
                TreeNode Y = (TreeNode)ssa.InnerToOuter[Yold];

                if (Y is TimpleLabelNode)
                {
                    /* Determine j, the operand of the phi functions we want */
                    int j = Yold.prev.IndexOf(n.InnerNode);

                    foreach (TimplePhiInstNode phi in ((TimpleLabelNode)Y).Phis)
                    {
                        int a = phi.R.LogicalVar;
                        vara renamed = vara.Logical(a, phi.R.DataType);
                        renamed.SSA = top(Stack[a]);
                        
                        if (renamed.SSA == 0)
                            phi.VarArgs[S] = vara.Const(null, phi.R.DataType);
                        else
                            phi.VarArgs[S] = renamed;
                    }
                }
            }

            /* Run rename on all children in the dominator tree */
            foreach (TreeNode X in n.next)
                Rename(X, ssa, d, Count, Stack);

            /* Foreach definition of a variable in the original S, pop the stack */
            foreach (vara def in S.defs)
                pop(Stack[def.LogicalVar]);
        }

        private static void pop(List<int> list)
        {
            list.RemoveAt(list.Count - 1);
        }

        private static int top(List<int> list)
        {
            return list[list.Count - 1];
        }

        private static vara Replace(vara v, Dictionary<int, List<int>> Stack)
        {
            vara renamed;

            switch (v.VarType)
            {
                case vara.vara_type.Logical:
                    renamed = vara.Logical(v.LogicalVar, v.DataType);
                    break;

                case vara.vara_type.ContentsOf:
                    renamed = vara.ContentsOf(v.LogicalVar, v.Offset, v.DataType);
                    break;

                case vara.vara_type.AddrOf:
                    renamed = vara.AddrOf(v.LogicalVar, v.Offset);
                    break;

                default:
                    return v;
            }

            renamed.SSA = top(Stack[v.LogicalVar]);
            return renamed;                    
        }

        public TimpleGraph ConvertFromSSA()
        {
            TimpleGraph g = new TimpleGraph();

            IList<BaseNode> nodes = LinearStream;
            foreach (TreeNode n in nodes)
            {
                TreeNode newnode = (TreeNode)n.MemberwiseClone();
                newnode.prev = new List<BaseNode>();
                newnode.next = new List<BaseNode>();
                newnode.InnerNode = n;

                if (newnode is TimpleLabelNode)
                    ((TimpleLabelNode)newnode).Phis = new List<TimplePhiInstNode>();

                g.InnerToOuter[n] = newnode;
            }

            util.Set<TreeNode> visited = new util.Set<TreeNode>();

            foreach (TreeNode n in Starts)
            {
                g.AddStartNode((TreeNode)g.InnerToOuter[n]);
                DFAdd(g, n, g.InnerToOuter, visited);
            }

            return g;
        }

        private void DFAdd(TimpleGraph ret, TreeNode n, Dictionary<BaseNode, BaseNode> old_to_new, util.Set<TreeNode> visited)
        {
            if (!visited.Contains(n))
            {
                visited.Add(n);

                foreach (TreeNode next in n.next)
                {
                    if (next is TimpleLabelNode)
                    {
                        /* Add instructions to convert phis to moves before/after the current instruction */
                        List<TreeNode> convert_instrs = new List<TreeNode>();
                        foreach (TimplePhiInstNode phi in ((TimpleLabelNode)next).Phis)
                        {
                            vara R = phi.R;
                            Assembler.CliType ct = phi.R.DataType;
                            ThreeAddressCode.Op op = Assembler.GetAssignTac(ct);
                            vara O1 = phi.VarArgs[n];
                            convert_instrs.Add(new TimpleNode(op, R, O1, vara.Void()));
                        }

                        /* If the current instruction is a branch, we need to insert the resolved phi before it
                         * so it is valid after the jump, otherwise insert after it (as the current instruction may
                         * be required for the phi)
                         */

                        if (n is TimpleBrNode)
                        {
                            foreach (TreeNode prev in n.prev)
                            {
                                TreeNode cur_prev = (TreeNode)old_to_new[prev];
                                ret.RemoveEdge((TreeNode)old_to_new[prev], (TreeNode)old_to_new[n]);

                                foreach (TreeNode conv in convert_instrs)
                                {
                                    TreeNode new_conv = (TreeNode)conv.MemberwiseClone();
                                    ret.AddTreeEdge(cur_prev, conv);
                                    cur_prev = conv;
                                }

                                ret.AddTreeEdge(cur_prev, (TreeNode)old_to_new[n]);
                            }
                            ret.AddTreeEdge((TreeNode)old_to_new[n], (TreeNode)old_to_new[next]);
                        }
                        else
                        {
                            TreeNode cur_prev = (TreeNode)old_to_new[n];
                            foreach (TreeNode conv in convert_instrs)
                            {
                                ret.AddTreeEdge(cur_prev, conv);
                                cur_prev = conv;
                            }
                            ret.AddTreeEdge(cur_prev, (TreeNode)old_to_new[next]);
                        }
                    }
                    else
                        ret.AddTreeEdge((TreeNode)old_to_new[n], (TreeNode)old_to_new[next]);

                    DFAdd(ret, next, old_to_new, visited);
                }
            }
        }
    }
}
