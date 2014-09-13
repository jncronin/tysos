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

namespace libtysila.tybel
{
    partial class Tybel
    {
        public Tybel ResolveSpecialNodes(timple.Liveness l, Assembler ass, IList<libasm.hardware_location> las,
            IList<libasm.hardware_location> lvs)
        {
            Tybel ret = new Tybel();
            ret.innergraph = this;

            /* Clone the graph */
            foreach (timple.BaseNode n in LinearStream)
            {
                timple.BaseNode outer_n = n.MemberwiseClone();
                outer_n.InnerNode = n;
                ret.InnerToOuter[n] = outer_n;
            }
            foreach (timple.BaseNode n in LinearStream)
            {
                ret.InnerToOuter[n].Next = new List<timple.BaseNode>();
                foreach (timple.BaseNode next in n.Next)
                    ret.InnerToOuter[n].Next.Add(ret.InnerToOuter[next]);
                ret.InnerToOuter[n].Prev = new List<timple.BaseNode>();
                foreach (timple.BaseNode prev in n.Prev)
                    ret.InnerToOuter[n].Prev.Add(ret.InnerToOuter[prev]);
            }

            int next_var = 0;
            int next_block = 0;

            Dictionary<SpecialNode, List<vara>> stored_vars = new Dictionary<SpecialNode, List<vara>>();
            util.Set<timple.BaseNode> visited = new util.Set<timple.BaseNode>();

            foreach (timple.BaseNode start in Starts)
                ret.Starts.Add(ret.InnerToOuter[start]);

            foreach (timple.BaseNode start in Starts)
                ResolveSpecialNodes(ret, ret.InnerToOuter[start], l, stored_vars, visited, ass, ref next_var, ref next_block, las, lvs);

            foreach (timple.BaseNode end in Ends)
                ret.Ends.Add(ret.InnerToOuter[end]);
            
            return ret;
        }

        void ResolveSpecialNodes(Tybel ret, timple.BaseNode node, timple.Liveness l,
            Dictionary<SpecialNode, List<vara>> stored_vars, util.Set<timple.BaseNode> visited, Assembler ass, ref int next_var,
            ref int next_block, IList<libasm.hardware_location> las, IList<libasm.hardware_location> lvs)
        {
            if (visited.Contains(node))
                return;
            visited.Add(node);

            if (node is SpecialNode)
            {
                SpecialNode outer_sn = node as SpecialNode;

                switch (outer_sn.Type)
                {
                    case SpecialNode.SpecialNodeType.SaveLive:
                    case SpecialNode.SpecialNodeType.SaveLiveExcept:
                    case SpecialNode.SpecialNodeType.SaveLiveIntersect:
                        {
                            List<vara> stored = null;
                            switch (outer_sn.Type)
                            {
                                case SpecialNode.SpecialNodeType.SaveLive:
                                    stored = new List<vara>(l.live_out[outer_sn.SaveNode]);
                                    break;

                                case SpecialNode.SpecialNodeType.SaveLiveExcept:
                                    stored = new List<vara>(l.live_out[outer_sn.SaveNode].Except(outer_sn.VarList));
                                    break;

                                case SpecialNode.SpecialNodeType.SaveLiveIntersect:
                                    stored = new List<vara>(l.live_out[outer_sn.SaveNode].Intersect(outer_sn.VarList));
                                    break;
                            }

                            stored_vars[(SpecialNode)node.InnerNode] = stored;
                            timple.BaseNode cur_node = node;
                            foreach (vara v in stored)
                            {
                                IList<Node> save_instrs = ass.SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.save), vara.Void(), v, vara.Void()), ref next_var, ref next_block, las, lvs);

                                foreach (Node n in save_instrs)
                                {
                                    cur_node = cur_node.InsertAfter(n);
                                    n.InnerNode = node.InnerNode;
                                }
                            }
                            node.Remove();
                            return;
                        }
                        break;

                    case SpecialNode.SpecialNodeType.Restore:
                        {
                            List<vara> stored = stored_vars[(SpecialNode)((SpecialNode)node.InnerNode).SaveNode];

                            timple.BaseNode cur_node = node;
                            for (int i = stored.Count - 1; i >= 0; i--)
                            {
                                IList<Node> restore_instrs = ass.SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.restore), vara.Void(), stored[i], vara.Void()), ref next_var, ref next_block, las, lvs);

                                foreach (Node n in restore_instrs)
                                {
                                    cur_node = cur_node.InsertAfter(n);
                                    n.InnerNode = node.InnerNode;
                                }
                            }
                            node.Remove();
                            return;

                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else if (node is Node)
            {
                Node tn = node as Node;
                if (tn.IsUnconditionalJmp && tn.VarList.Count == 1 && tn.VarList[0].VarType == vara.vara_type.Label)
                {
                    string lab_target = tn.VarList[0].LabelVal;

                    if (node.Next.Count == 1)
                    {
                        int ls_idx = ret.LinearStream.IndexOf(node);
                        if((ls_idx + 1) < ret.LinearStream.Count)
                        {
                            if((ret.LinearStream[ls_idx + 1] is tybel.LabelNode) && (((LabelNode)ret.LinearStream[ls_idx + 1]).Label == lab_target))
                            {
                                node.Remove();
                                return;
                            }
                        }
                    }
                }
                if (tn.IsMove && tn.VarList[0].Equals(tn.VarList[1]))
                {
                    node.Remove();
                    return;
                }
            }

            util.Set<timple.BaseNode> visited_next = new util.Set<timple.BaseNode>();
            while (true)
            {
                timple.BaseNode next_node = null;
                foreach(timple.BaseNode next in node.Next)
                {
                    if(!visited_next.Contains(next))
                    {
                        next_node = next;
                        break;
                    }
                }
                if(next_node == null)
                    break;

                visited_next.Add(next_node);
                ResolveSpecialNodes(ret, next_node, l, stored_vars, visited, ass, ref next_var, ref next_block, las, lvs);
            }
        }


        void ResolveSpecialNodes2(Tybel ret, timple.BaseNode inner_node, timple.BaseNode outer_parent, timple.Liveness l,
            Dictionary<SpecialNode, List<vara>> stored_vars, util.Set<timple.BaseNode> visited, Assembler ass, ref int next_var,
            ref int next_block, IList<libasm.hardware_location> las, IList<libasm.hardware_location> lvs)
        {
            if (visited.Contains(inner_node))
                return;
            visited.Add(inner_node);

            tybel.Node outer_node = (tybel.Node)inner_node.MemberwiseClone();
            ret.InnerToOuter[inner_node] = outer_node;
            outer_node.InnerNode = inner_node;
            outer_node.next = new List<timple.BaseNode>();
            outer_node.prev = new List<timple.BaseNode>();

            if (outer_node is SpecialNode)
            {
                SpecialNode outer_sn = outer_node as SpecialNode;

                switch (outer_sn.Type)
                {
                    case SpecialNode.SpecialNodeType.SaveLive:
                    case SpecialNode.SpecialNodeType.SaveLiveExcept:
                    case SpecialNode.SpecialNodeType.SaveLiveIntersect:
                        {
                            List<vara> stored = null;
                            switch (outer_sn.Type)
                            {
                                case SpecialNode.SpecialNodeType.SaveLive:
                                    stored = new List<vara>(l.live_out[outer_sn.SaveNode]);
                                    break;

                                case SpecialNode.SpecialNodeType.SaveLiveExcept:
                                    stored = new List<vara>(l.live_out[outer_sn.SaveNode].Except(outer_sn.VarList));
                                    break;

                                case SpecialNode.SpecialNodeType.SaveLiveIntersect:
                                    stored = new List<vara>(l.live_out[outer_sn.SaveNode].Intersect(outer_sn.VarList));
                                    break;
                            }

                            stored_vars[(SpecialNode)inner_node] = stored;
                            foreach (vara v in stored)
                            {
                                IList<Node> save_instrs = ass.SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.save), vara.Void(), v, vara.Void()), ref next_var, ref next_block, las, lvs);

                                foreach (Node n in save_instrs)
                                {
                                    ret.AddEdge((Node)outer_parent, n);
                                    outer_parent = n;
                                }
                            }
                        }
                        break;

                    case SpecialNode.SpecialNodeType.Restore:
                        {
                            List<vara> stored = stored_vars[(SpecialNode)((SpecialNode)inner_node).SaveNode];

                            for (int i = stored.Count - 1; i >= 0; i--)
                            {
                                IList<Node> restore_instrs = ass.SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.restore), vara.Void(), stored[i], vara.Void()), ref next_var, ref next_block, las, lvs);

                                foreach (Node n in restore_instrs)
                                {
                                    ret.AddEdge((Node)outer_parent, n);
                                    outer_parent = n;
                                }
                            }

                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                bool skip = false;
                if (outer_node.IsUnconditionalJmp && outer_node.VarList.Count == 1 && outer_node.VarList[0].VarType == vara.vara_type.Label)
                {
                    string lab_target = outer_node.VarList[0].LabelVal;

                    if (inner_node.Next.Count == 1 && (inner_node.Next[0] is tybel.LabelNode) && (((LabelNode)inner_node.Next[0]).Label == lab_target))
                        skip = true;                       
                }

                if (skip == false)
                {
                    ret.AddEdge((Node)outer_parent, (Node)outer_node);
                    outer_parent = outer_node;
                }
            }

            foreach (timple.BaseNode next in inner_node.Next)
                ResolveSpecialNodes2(ret, next, outer_parent, l, stored_vars, visited, ass, ref next_var, ref next_block, las, lvs);
        }
    }
}
