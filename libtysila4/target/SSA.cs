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
using libtysila4.graph;
using libtysila4.util;

/* Converts to SSA form */
namespace libtysila4.target
{
    public class SSA
    {
        public static graph.Graph ConvertToSSAPass(graph.Graph input)
        {
            /* Determine all defined variables */
            Dictionary<int, util.Set> defsites =
                new Dictionary<int, util.Set>(new GenericEqualityComparer<int>());
            Dictionary<int, util.Set> Aorig =
                new Dictionary<int, util.Set>(new GenericEqualityComparer<int>());
            Dictionary<int, ir.Param> var_locs =
                new Dictionary<int, ir.Param>(new GenericEqualityComparer<int>());
            List<int> vars = new List<int>();
            foreach(var mcin in input.LinearStream)
            {
                var mci = mcin.c as MCNode;
                Aorig[mcin.bb] = new util.Set();
                foreach(var def in mci.defs)
                {
                    if(def.t == ir.Opcode.vl_stack ||
                        def.t == ir.Opcode.vl_stack32 ||
                        def.t == ir.Opcode.vl_stack64)
                    {
                        int a = (int)def.v;
                        util.Set defsites_a;
                        if(!defsites.TryGetValue(a, out defsites_a))
                        {
                            defsites_a = new util.Set(input.LinearStream.Count);
                            defsites[a] = defsites_a;
                            vars.Add(a);
                            var_locs[a] = def;
                        }
                        defsites_a.set(mcin.bb);
                        Aorig[mcin.bb].set(a);
                    }
                }
            }

            /* Appel 19.6 */
            Dictionary<int, util.Set> Aphi =
                new Dictionary<int, util.Set>(new GenericEqualityComparer<int>());
            foreach (var a in vars)
                Aphi[a] = new util.Set();

            foreach (var a in vars)
            {
                var W = defsites[a].Clone();

                while(!W.Empty)
                {
                    var n = W.get_first_set();
                    W.unset(n);

                    foreach(var y in input.DominanceGraph.df[n])
                    {
                        if(!Aphi[a].get(y))
                        {
                            // Insert phi function
                            var mcn = input.LinearStream[y].c as MCNode;
                            int preds = input.LinearStream[y].PrevCount;
                            MCInst phi = new MCInst();
                            phi.p = new ir.Param[preds + 2];
                            phi.p[0] = new ir.Param { t = ir.Opcode.vl_str, v = target.Generic.g_phi };
                            phi.p[1] = new ir.Param { t = var_locs[a].t, v = a, ud = ir.Param.UseDefType.Def };
                            for (int i = 0; i < preds; i++)
                                phi.p[2 + i] = new ir.Param { t = var_locs[a].t, v = a, ud = ir.Param.UseDefType.Def };

                            Aphi[a].set(y);
                            if (!Aorig[y].get(a))
                                W.set(y);
                        }
                    }
                }
            }

            /* Appel 19.7, p437

                We adapt it slightly so that instead of naming variables
                1_1, 1_2, 2_1 etc, they all get a new number */
            int next_ssa_var = 1;

            Dictionary<int, util.Stack<int>> Stack =
                new Dictionary<int, util.Stack<int>>(
                    new GenericEqualityComparer<int>());

            foreach(var a in vars)
            {
                Stack[a] = new util.Stack<int>();
                //Stack[a].Push(next_ssa_var++);
            }

            foreach (var n in input.DominanceGraph.Starts)
                Rename(n, input, ref next_ssa_var, Stack);

            input.next_vreg_id = next_ssa_var;

            return input;
        }

        private static void Rename(BaseNode Dn, Graph input, ref int next_ssa_var, Dictionary<int, util.Stack<int>> stack)
        {
            var n = ((DomNodeContents)Dn.c).SrcNode;

            foreach (var S in ((MCNode)n.c).all_insts)
            {
                bool is_phi = false;
                if (S.p.Length > 0 && S.p[0].t == ir.Opcode.vl_str &&
                    S.p[0].v == Generic.g_phi)
                    is_phi = true;

                if (!is_phi)
                {
                    foreach (var p in S.p)
                    {
                        if (p.ud == ir.Param.UseDefType.Use && p.IsStack)
                        {
                            var x = (int)p.v;
                            var i = stack[x].Peek();
                            p.ssa_idx = i;
                        }
                    }
                }
                foreach (var p in S.p)
                {
                    if (p.ud == ir.Param.UseDefType.Def && p.IsStack)
                    {
                        var a = (int)p.v;
                        var i = next_ssa_var++;
                        stack[a].Push(i);
                        p.ssa_idx = i;
                    }
                }
            }

            foreach (var Y in n.Next)
            {
                int j = 0;
                foreach (var prev in Y.Prev)
                {
                    if (prev == n)
                        break;
                    j++;
                }
                // n is now the jth predecessor of Y

                foreach(var phi in ((MCNode)Y.c).phis)
                {
                    var a = (int)phi.p[1].v;
                    phi.p[2 + j].ssa_idx = stack[a].Peek();
                }
            }

            foreach (var X in Dn.Next)
                Rename(X, input, ref next_ssa_var, stack);

            foreach (var S in ((MCNode)n.c).all_insts)
            {
                foreach (var p in S.p)
                {
                    if (p.ud == ir.Param.UseDefType.Def && p.IsStack)
                    {
                        var a = (int)p.v;
                        stack[a].Pop();
                    }
                }
            }
        }
    }
}
