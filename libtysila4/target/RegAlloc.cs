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


// Uses the algorithm from Appel ch11, p244
using System;
using System.Collections.Generic;
using System.Text;
using libtysila4.util;

namespace libtysila4.target
{
    public class RegAlloc
    {
        public static graph.Graph RegAllocPass(graph.Graph input, Target t)
        {
            RegAlloc r = new RegAlloc(input, t);
            r.K = 6;        // TODO: make unique per arch

            // TODO - make unique per arch
            Dictionary<int, Target.Reg> mreg_vals =
                new Dictionary<int, Target.Reg>(
                    new GenericEqualityComparer<int>());
            var x86_ass = ((x86.x86_Assembler)Target.targets["x86"]);
            mreg_vals[0] = x86_ass.r_eax;
            mreg_vals[1] = x86_ass.r_ebx;
            mreg_vals[2] = x86_ass.r_ecx;
            mreg_vals[3] = x86_ass.r_edx;
            mreg_vals[4] = x86_ass.r_edi;
            mreg_vals[5] = x86_ass.r_esi;

            Dictionary<Target.Reg, int> mreg_vals_rev =
                new Dictionary<Target.Reg, int>(
                    new GenericEqualityComparer<Target.Reg>());
            foreach (var kvp in mreg_vals)
            {
                var vreg_id = input.next_vreg_id++;
                mreg_vals_rev[kvp.Value] = vreg_id;
                r.color[vreg_id] = kvp.Key;
            }

            // Convert preallocated mahine regs to vregs
            foreach(var n in input.LinearStream)
            {
                var mcn = n.c as MCNode;
                foreach(var I in mcn.all_insts)
                {
                    foreach(var p in I.p)
                    {
                        if(p.t == ir.Opcode.vl_mreg)
                        {
                            int vreg_id;
                            if (mreg_vals_rev.TryGetValue(p.mreg, out vreg_id))
                            {
                                p.t = ir.Opcode.vl_stack;
                                p.ssa_idx = vreg_id;
                            }
                        }
                    }
                }
            }


            r.Main();

            foreach(var n in r.renumbered_insts)
            {
                foreach(var p in n.p)
                {
                    if(p.IsStack)
                    {
                        var c = r.color[p.ssa_idx];
                        p.t = ir.Opcode.vl_mreg;
                        p.mreg = mreg_vals[c];
                    }
                }
            }

            return input;
        }

        void Main()
        {
            BuildInitial();
            nodes = initial.Clone();

            while (true)
            {
                /* First do liveness analysis - A potential optimisation
                is to update the gen/kill lists whenever we update the
                code, thus all we need to do is the liveness analysis
                on each pass.  */
                Liveness.DoGenKill(g, t);
                Liveness.LivenessAnalysis(g, t);

                /* Give each instruction (rather than block) a unique index */
                RelabelInsts();

                Build();
                MakeWorklist();

                do
                {
                    if (!simplifyWorklist.Empty)
                        Simplify();
                    else if (!worklistMoves.Empty)
                        Coalesce();
                    else if (!freezeWorklist.Empty)
                        Freeze();
                    else if (!spillWorklist.Empty)
                        SelectSpill();
                } while (!simplifyWorklist.Empty ||
                    !worklistMoves.Empty ||
                    !freezeWorklist.Empty ||
                    !spillWorklist.Empty);

                AssignColors();

                if (spilledNodes.Empty)
                    break;
                RewriteProgram(spilledNodes); 
            };
        }

        private void BuildInitial()
        {
            initial.Clear();
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;
                foreach(var I in mcn.all_insts)
                {
                    foreach(var p in I.p)
                    {
                        if (p.IsStack)
                            initial.set(p.ssa_idx);
                    }
                }
            }
        }

        private void RelabelInsts()
        {
            int next_idx = 0;
            renumbered_insts.Clear();
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;
                foreach(var I in mcn.all_insts)
                {
                    int idx = next_idx++;
                    I.idx = idx;
                    renumbered_insts.Add(I);
                }
            }
        }

        private void RewriteProgram(Set spilledNodes)
        {
            throw new NotImplementedException();
        }

        private void SelectSpill()
        {
            throw new NotImplementedException();
        }

        private void Freeze()
        {
            var u = freezeWorklist.get_first_set();
            freezeWorklist.unset(u);
            simplifyWorklist.set(u);
            FreezeMoves(u);
        }

        private void FreezeMoves(int u)
        {
            foreach (var m in worklistMoves)
            {
                var I = renumbered_insts[m];

                // TODO - check these are the right way round
                var x = t.GetMoveDest(I).ssa_idx;
                var y = t.GetMoveSrc(I).ssa_idx;

                int v;

                if (GetAlias(y) == GetAlias(u))
                    v = GetAlias(x);
                else
                    v = GetAlias(y);

                activeMoves.unset(m);
                frozenMoves.set(m);

                if(NodeMoves(v).Empty && degree[v] < K)
                {
                    freezeWorklist.unset(v);
                    simplifyWorklist.set(v);
                }
            }
        }

        private void Coalesce()
        {
            var m = worklistMoves.get_first_set();

            var I = renumbered_insts[m];

            // TODO - check these are the right way round
            var x = t.GetMoveDest(I).ssa_idx;
            var y = t.GetMoveSrc(I).ssa_idx;

            x = GetAlias(x);
            y = GetAlias(y);

            int u, v;
            if(precolored.get(y))
            {
                u = y;
                v = x;
            }
            else
            {
                u = x;
                v = y;
            }

            worklistMoves.unset(m);
            if(u == v)
            {
                coalescedMoves.set(m);
                AddWorkList(u);
            }
            else if(precolored.get(v) || dictSetGet(u, v, adjSet))
            {
                constrainedMoves.set(m);
                AddWorkList(u);
                AddWorkList(v);
            }
            else
            {
                bool is_ok = true;
                if (precolored.get(u))
                { 
                    foreach(var t in Adjacent(v))
                    {
                        if (!OK(t, u))
                        {
                            is_ok = false;
                            break;
                        }
                    }
                }
                else
                {
                    var adj_uv = Adjacent(u).Clone();
                    adj_uv.Union(Adjacent(v));

                    if (!Conservative(adj_uv))
                        is_ok = false;
                }

                if (is_ok)
                {
                    coalescedMoves.set(m);
                    Combine(u, v);
                    AddWorkList(u);
                }
                else
                    activeMoves.set(m);
            }
        }

        private void Combine(int u, int v)
        {
            if (freezeWorklist.get(v))
                freezeWorklist.unset(v);
            else
                spillWorklist.unset(v);

            coalescedNodes.set(v);
            if (alias.ContainsKey(v))
                throw new Exception();
            alias[v] = u;

            moveList[u].Union(moveList[v]);
            EnableMoves(v);

            foreach(var t in Adjacent(v))
            {
                AddEdge(t, u);
                DecrementDegree(t);
            }
            if(getDegree(u) >= K && freezeWorklist.get(u))
            {
                freezeWorklist.unset(u);
                spillWorklist.set(u);
            }
        }

        private void EnableMoves(int n)
        {
            foreach (var m in NodeMoves(n))
            {
                if (activeMoves.get(m))
                {
                    activeMoves.unset(m);
                    worklistMoves.set(m);
                }
            }
        }

        private bool Conservative(Set adj_uv)
        {
            var k = 0;
            foreach(var n in adj_uv)
            {
                if (getDegree(n) >= K)
                    k++;
            }
            return k < K;
        }

        private bool OK(int t, int r)
        {
            throw new NotImplementedException();
        }

        private void AddWorkList(int u)
        {
            if(!precolored.get(u) && !MoveRelated(u) && getDegree(u) < K)
            {
                freezeWorklist.unset(u);
                simplifyWorklist.set(u);
            }
        }

        private void Simplify()
        {
            var n = simplifyWorklist.get_first_set();
            simplifyWorklist.unset(n);
            selectStack.Push(n);
            foreach (var m in Adjacent(n))
                DecrementDegree(m);
        }

        private void DecrementDegree(int m)
        {
            var d = getDegree(m);
            degree[m] = d - 1;

            if(d == K)
            {
                var m_adjm = Adjacent(m);
                m_adjm.set(m);
                EnableMoves(m_adjm);
                spillWorklist.unset(m);
                if (MoveRelated(m))
                    freezeWorklist.set(m);
                else
                    simplifyWorklist.set(m);
            }
        }

        private void EnableMoves(Set m_adjm)
        {
            foreach(var n in nodes)
            {
                EnableMoves(n);
            }
        }

        private util.Set Adjacent(int n)
        {
            Set adjList_n;
            if(!adjList.TryGetValue(n, out adjList_n))
            {
                return new Set();
            }
            var tmp = adjList_n.Clone();
            foreach (var ss in selectStack)
                tmp.unset(ss);
            tmp.unset(coalescedNodes);

            return tmp;
        }

        private void AssignColors()
        {
            var okColorsTemplate = new Set(K);
            for (var i = 0; i < K; i++)
                okColorsTemplate.set(i);

            while (selectStack.Count > 0)
            {
                var n = selectStack.Pop();
                var okColors = okColorsTemplate.Clone();

                Set adjList_n;
                if (adjList.TryGetValue(n, out adjList_n))
                {
                    foreach (var w in adjList[n])
                    {
                        var tmp = coloredNodes.Clone();
                        tmp.Union(precolored);

                        var alias_w = GetAlias(w);
                        if (tmp.get(alias_w))
                            okColors.unset(color[alias_w]);
                    }
                }

                if (okColors.Empty)
                    spilledNodes.set(n);
                else
                {
                    coloredNodes.set(n);
                    var c = okColors.get_first_set();
                    color[n] = c;
                }
            }

            foreach (var n in coalescedNodes)
                color[n] = color[GetAlias(n)];
        }

        private int GetAlias(int n)
        {
            if (coalescedNodes.get(n))
                return GetAlias(alias[n]);
            else
                return n;
        }

        private void MakeWorklist()
        {
            var new_initial = initial.Clone();
            foreach(var n in initial)
            {
                new_initial.unset(n);

                if (getDegree(n) >= K)
                    spillWorklist.set(n);
                else if (MoveRelated(n))
                    freezeWorklist.set(n);
                else
                    simplifyWorklist.set(n);
            }
            initial = new_initial;
        }

        private bool MoveRelated(int n)
        {
            return !NodeMoves(n).Empty;
        }

        private Set NodeMoves(int n)
        {
            var tmp = activeMoves.Clone();
            tmp.Union(worklistMoves);

            Set moveList_n;
            if(!moveList.TryGetValue(n, out moveList_n))
            {
                moveList_n = new Set();
                moveList[n] = moveList_n;
            }
            tmp.Intersect(moveList_n);
            return tmp;
        }

        private void Build()
        {
            foreach (var b in g.LinearStream)
            {
                var mcn = b.c as MCNode;
                var live = mcn.live_out.Clone();

                foreach(var I in mcn.all_insts_rev)
                {
                    if(t.IsMoveVreg(I))
                    {
                        // live <- live\use(I)
                        foreach(var p in I.p)
                        {
                            if (p.IsStack && p.ud == ir.Param.UseDefType.Use)
                                live.unset(p.ssa_idx);
                        }

                        foreach(var p in I.p)
                        {
                            if(p.IsStack && (p.ud == ir.Param.UseDefType.Use ||
                                p.ud == ir.Param.UseDefType.Def))
                            {
                                var n = p.ssa_idx;
                                util.Set moveList_n;
                                if(!moveList.TryGetValue(n, out moveList_n))
                                {
                                    moveList_n = new Set();
                                    moveList[n] = moveList_n;
                                }
                                moveList_n.set(I.idx);
                            }
                        }

                        worklistMoves.set(I.idx);
                    }

                    foreach(var p in I.p)
                    {
                        if(p.IsStack && p.ud == ir.Param.UseDefType.Def)
                            live.set(p.ssa_idx);
                    }

                    foreach(var p in I.p)
                    {
                        if(p.IsStack && p.ud == ir.Param.UseDefType.Def)
                        {
                            foreach(var l in live)
                            {
                                AddEdge(l, p.ssa_idx);
                            }
                        }
                    }

                    foreach(var p in I.p)
                    {
                        if (p.IsStack && p.ud == ir.Param.UseDefType.Def)
                            live.unset(p.ssa_idx);
                    }
                    foreach(var p in I.p)
                    {
                        if (p.IsStack && p.ud == ir.Param.UseDefType.Use)
                            live.set(p.ssa_idx);
                    }
                }
            }
        }

        private void AddEdge(int u, int v)
        {
            if((u != v) && !dictSetGet(u,v, adjSet))
            {
                dictSetSet(u, v, adjSet);
                dictSetSet(v, u, adjSet);

                if(!precolored.get(u))
                {
                    dictSetSet(u, v, adjList);
                    degree[u] = getDegree(u) + 1;
                }
                if(!precolored.get(v))
                {
                    dictSetSet(v, u, adjList);
                    degree[v] = getDegree(v) + 1;
                }
            }
        }

        public RegAlloc(graph.Graph graph, Target target)
        {
            g = graph;
            t = target;
        }

        graph.Graph g;
        Target t;
        List<MCInst> renumbered_insts = new List<MCInst>();

        Dictionary<int, util.Set> moveList =
            new Dictionary<int, Set>(
                new GenericEqualityComparer<int>());

        Dictionary<int, util.Set> adjSet =
            new Dictionary<int, Set>(
                new GenericEqualityComparer<int>());

        Dictionary<int, util.Set> adjList =
            new Dictionary<int, Set>(
                new GenericEqualityComparer<int>());

        Dictionary<int, int> degree =
            new Dictionary<int, int>(
                new GenericEqualityComparer<int>());

        Dictionary<int, int> alias =
            new Dictionary<int, int>(
                new GenericEqualityComparer<int>());

        Dictionary<int, int> color =
            new Dictionary<int, int>(
                new GenericEqualityComparer<int>());

        int K;
        util.Set precolored = new Set();
        util.Set initial = new Set();
        util.Set nodes;
        util.Set spilledNodes = new util.Set();
        util.Set coalescedNodes = new util.Set();
        util.Set coloredNodes = new util.Set();
        util.Set simplifyWorklist = new util.Set();
        util.Set worklistMoves = new util.Set();
        util.Set activeMoves = new util.Set();
        util.Set frozenMoves = new util.Set();
        util.Set coalescedMoves = new Set();
        util.Set constrainedMoves = new Set();
        util.Set freezeWorklist = new util.Set();
        util.Set spillWorklist = new util.Set();
        util.Stack<int> selectStack = new util.Stack<int>();

        void dictSetSet(int u, int v, Dictionary<int, Set> s)
        {
            Set s_entry;
            if(!s.TryGetValue(u, out s_entry))
            {
                s_entry = new Set();
                s[u] = s_entry;
            }
            s_entry.set(v);
        }

        bool dictSetGet(int u, int v, Dictionary<int, Set> s)
        {
            Set s_entry;
            if (!s.TryGetValue(u, out s_entry))
            {
                return false;
            }
            return s_entry.get(v);
        }

        int getDegree(int v)
        {
            int ret;
            if (degree.TryGetValue(v, out ret))
                return ret;
            return 0;
        }
    }
}
