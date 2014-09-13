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

/* tybel = tysila back-end language */

using System;
using System.Collections.Generic;

namespace libtysila.tybel
{
    public partial class Tybel : timple.BaseGraph
    {
        public Dictionary<timple.TreeNode, IList<Node>> TimpleMap;
        public int NextVar;
        public int NextBlock;

        public static Tybel GenerateTybel(timple.Optimizer.OptimizeReturn code, Assembler ass, IList<libasm.hardware_location> las,
            IList<libasm.hardware_location> lvs, Assembler.MethodAttributes attrs, ref int next_var, ref int next_block)
        {
            /* Choose instructions */
            libtysila.tybel.Tybel tybel = libtysila.tybel.Tybel.BuildGraph(code, ass, las, lvs, ref next_var, ref next_block);
            libtysila.timple.Liveness tybel_l = libtysila.timple.Liveness.LivenessAnalysis(tybel);

            /* Prepare register allocator */
            libtysila.regalloc.RegAlloc r = new libtysila.regalloc.RegAlloc();
            Dictionary<vara, vara> regs = r.Main(new libtysila.tybel.Tybel.TybelCode(tybel, tybel_l, next_var, next_block), ass, attrs);

            /* Generate stackloc mappings */
            Dictionary<libasm.hardware_location, libasm.hardware_location> stackloc_map = ass.AllocateStackLocations(attrs);
            CombineStackLocMap(regs, stackloc_map);

            /* Rename registers in the code */
            libtysila.tybel.Tybel tybel_2 = tybel.RenameRegisters(regs);

            /* Resolve special instructions */
            libtysila.timple.Liveness tybel2_l = libtysila.timple.Liveness.LivenessAnalysis(tybel_2, true);
            libtysila.tybel.Tybel tybel_3 = tybel_2.ResolveSpecialNodes(tybel2_l, ass, las, lvs);

            return tybel_3;
        }

        private static void CombineStackLocMap(Dictionary<vara, vara> regs, Dictionary<libasm.hardware_location, libasm.hardware_location> stackloc_map)
        {
            List<vara> reg_list = new List<vara>(regs.Keys);
            foreach (vara r in reg_list)
            {
                if (regs[r].MachineRegVal is libasm.hardware_stackloc)
                {
                    vara r2 = vara.MachineReg(stackloc_map[regs[r].MachineRegVal], regs[r].DataType);
                    regs[r] = r2;
                }
            }
            foreach(KeyValuePair<libasm.hardware_location, libasm.hardware_location> kvp in stackloc_map)
                regs[vara.MachineReg(kvp.Key)] = vara.MachineReg(kvp.Value);
        }

        private static Tybel BuildGraph(timple.Optimizer.OptimizeReturn code, Assembler ass, IList<libasm.hardware_location> las,
            IList<libasm.hardware_location> lvs, ref int next_var, ref int next_block)
        {
            Tybel ret = new Tybel();
            ret.TimpleMap = new Dictionary<timple.TreeNode, IList<Node>>();
            ret.NextVar = next_var;
            ret.NextBlock = next_block;

            /* Determine the greatest logical var in use */
            foreach (vara v in code.Liveness.defs.Keys)
            {
                if (v.LogicalVar >= ret.NextVar)
                    ret.NextVar = v.LogicalVar + 1;
            }

            /* Determine those variables which need memory locations */
            util.Set<vara> memlocs = new util.Set<vara>();
            foreach (vara v in code.Liveness.defs.Keys)
            {
                foreach (timple.BaseNode n in code.Liveness.defs[v])
                {
                    if (n is timple.TimpleNode)
                    {
                        timple.TimpleNode tn = n as timple.TimpleNode;
                        if (tn.O1.VarType == vara.vara_type.AddrOf)
                        {
                            memlocs.Add(vara.Logical(tn.O1.LogicalVar, tn.O1.SSA, Assembler.CliType.none));
                            tn.O1.needs_memloc = true;
                        }
                        if (tn.O2.VarType == vara.vara_type.AddrOf)
                        {
                            memlocs.Add(vara.Logical(tn.O2.LogicalVar, tn.O2.SSA, Assembler.CliType.none));
                            tn.O2.needs_memloc = true;
                        }
                    }
                    if (n is timple.TimpleCallNode)
                    {
                        timple.TimpleCallNode tcn = n as timple.TimpleCallNode;
                        for(int i = 0; i < tcn.VarArgs.Count; i++)
                        {
                            if (tcn.VarArgs[i].VarType == vara.vara_type.AddrOf)
                            {
                                memlocs.Add(vara.Logical(tcn.VarArgs[i].LogicalVar, tcn.VarArgs[i].SSA, Assembler.CliType.none));
                                vara v2 = tcn.VarArgs[i];
                                v2.needs_memloc = true;
                                tcn.VarArgs[i] = v2;
                            }
                        }
                    }
                }
            }
            
            foreach (timple.TreeNode n in code.Code)
            {
                /* Convert operations on native ints to the appropriate instruction
                 * depending on the bitness of the architecture */
                SelectBitness(n, ass);

                /* Set all needs_memloc vars as required */
                if (n is timple.TimpleNode)
                {
                    timple.TimpleNode tn = n as timple.TimpleNode;

                    if (tn.R.VarType == vara.vara_type.Logical && memlocs.Contains(vara.Logical(tn.R.LogicalVar, tn.R.SSA, Assembler.CliType.none)))
                        tn.R.needs_memloc = true;
                    if (tn.O1.VarType == vara.vara_type.Logical && memlocs.Contains(vara.Logical(tn.O1.LogicalVar, tn.O1.SSA, Assembler.CliType.none)))
                        tn.O1.needs_memloc = true;
                    if (tn.O2.VarType == vara.vara_type.Logical && memlocs.Contains(vara.Logical(tn.O2.LogicalVar, tn.O2.SSA, Assembler.CliType.none)))
                        tn.O2.needs_memloc = true;
                }
                if (n is timple.TimpleCallNode)
                {
                    timple.TimpleCallNode tcn = n as timple.TimpleCallNode;
                    for (int i = 0; i < tcn.VarArgs.Count; i++)
                    {
                        if (tcn.VarArgs[i].VarType == vara.vara_type.Logical && memlocs.Contains(vara.Logical(tcn.VarArgs[i].LogicalVar, tcn.VarArgs[i].SSA, Assembler.CliType.none)))
                        {
                            vara v2 = tcn.VarArgs[i];
                            v2.needs_memloc = true;
                            tcn.VarArgs[i] = v2;
                        }
                    }
                }

                /* Generate tybel instructions */
                IList<Node> tybel = ass.SelectInstruction(n, ref ret.NextVar, ref ret.NextBlock, las, lvs);

                if (tybel == null)
                {
                    /* The instruction cannot be encoded as-is.
                     * 
                     * Rewrite it */
                    List<timple.TreeNode> new_code = RewriteInst(n, ref ret.NextVar);
                    if(new_code == null)
                        throw new Exception("Unable to encode " + n.ToString());
                    tybel = new List<Node>();
                    foreach (timple.TreeNode new_n in new_code)
                    {
                        IList<Node> new_tybel = ass.SelectInstruction(new_n, ref ret.NextVar, ref ret.NextBlock, las, lvs);
                        if(new_tybel == null)
                            throw new Exception("Unable to encode " + new_n.ToString() + " (rewritten from " + n.ToString() + ")");
                        ((List<Node>)tybel).AddRange(new_tybel);
                    }
                }

                ret.TimpleMap[n] = tybel;
                foreach (Node t_n in tybel)
                    t_n.InnerNode = n;
            }

            util.Set<timple.TreeNode> visited = new util.Set<timple.TreeNode>();
            foreach (timple.TreeNode n in code.CodeTree.Starts)
            {
                ret.Starts.Add(ret.TimpleMap[n][0]);
                ret.DFAdd(n, visited);
            }

            next_var = ret.NextVar;
            next_block = ret.NextBlock;
            return ret;
        }

        internal static List<timple.TreeNode> RewriteInst(timple.TreeNode n, ref int next_var)
        {
            /* Rewrite an instruction of form R op A, B to:
             *   new_R op A, B
             *   R = new_R
             * Where new_R is a logical value */

            if (n is timple.TimpleNode)
            {
                timple.TimpleNode tn = n as timple.TimpleNode;

                if (tn.R.VarType != vara.vara_type.Void)
                {
                    vara new_R = vara.Logical(next_var++, tn.R.DataType);

                    List<timple.TreeNode> ret = new List<timple.TreeNode>();
                    timple.TimpleNode line1 = new timple.TimpleNode(n.Op, new_R, tn.O1, tn.O2);
                    timple.TimpleNode line2 = new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, tn.R.DataType, tn.R.vt_type),
                        tn.R, new_R, vara.Void());

                    ret.Add(line1);
                    ret.Add(line2);

                    return ret;
                }
            }

            return null;
        }

        private static void SelectBitness(timple.TreeNode n, Assembler ass)
        {
            switch (ass.GetBitness())
            {
                case Assembler.Bitness.Bits32:
                    n.Op = ThreeAddressCode.Get32BitOp(n.Op);
                    break;

                case Assembler.Bitness.Bits64:
                    n.Op = ThreeAddressCode.Get64BitOp(n.Op);
                    break;
            }
        }

        private void DFAdd(timple.TreeNode n, util.Set<timple.TreeNode> visited)
        {
            if (visited.Contains(n))
                return;

            visited.Add(n);

            for (int i = 1; i < TimpleMap[n].Count; i++)
                AddEdge(TimpleMap[n][i - 1], TimpleMap[n][i]);

            Node last = TimpleMap[n][TimpleMap[n].Count - 1];

            if (n.next.Count == 0)
                Ends.Add(last);
            else
            {
                foreach (timple.TreeNode next in n.next)
                {
                    AddEdge(last, TimpleMap[next][0]);
                    DFAdd(next, visited);
                }
            }
        }

        private void AddEdge(Node parent, Node c)
        {
            if (c.next == null)
                c.next = new List<timple.BaseNode>();
            if (c.prev == null)
                c.prev = new List<timple.BaseNode>();

            if (parent == null)
                Starts.Add(c);
            else
            {
                if (parent.next == null)
                    parent.next = new List<timple.BaseNode>();
                if (parent.prev == null)
                    parent.prev = new List<timple.BaseNode>();

                parent.Next.Add(c);
                c.Prev.Add(parent);
            }
        }

        public class TybelCode
        {
            public timple.BaseGraph CodeGraph;
            public IList<timple.BaseNode> Code { get { return CodeGraph.LinearStream; } }
            public timple.Liveness Liveness;

            public TybelCode(timple.BaseGraph code, timple.Liveness l, int next_var, int next_block)
            { CodeGraph = code; Liveness = l; NextVar = next_var; NextBlock = next_block; }

            public int NextVar;
            public int NextBlock;
        }
    }
}
