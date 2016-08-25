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

namespace libtysila4.target
{
    public class Liveness
    {
        public static void DoGenKill(graph.Graph g)
        {
            foreach (var n in g.LinearStream)
                DoGenKill(n.c as MCNode);
        }

        private static void DoGenKill(MCNode mcn)
        {
            mcn.gen.Clear();
            mcn.kill.Clear();

            util.Set cur_used = new util.Set();
            util.Set cur_defd = new util.Set();

            foreach(var mci in mcn.all_insts)
            {
                /* First update gen/kill sets */
                foreach(var p in mci.p)
                {
                    if (p.IsStack)
                    {
                        int x = p.ssa_idx;
                        if (p.ud == ir.Param.UseDefType.Use)
                        {
                            // gens are those variables used before assignment
                            if (!cur_defd.get(x))
                                mcn.gen.set(x);
                        }
                        else if(p.ud == ir.Param.UseDefType.Def)
                        {
                            // kills are those defined before use
                            if (!cur_used.get(x))
                                mcn.kill.set(x);
                        }
                    }
                }

                /* Now add those used or defined to temp set
                        - doing 2 stage approach stops us
                          breaking when a var is used and defd
                          in the same instruction - we
                          complete the setup of gen/kill for the
                          entire instruction, then add to the
                          cur_used/defd sets */
                foreach(var p in mci.p)
                {
                    if(p.IsStack)
                    {
                        int x = p.ssa_idx;
                        if (p.ud == ir.Param.UseDefType.Use)
                            cur_used.set(x);
                        else if (p.ud == ir.Param.UseDefType.Def)
                            cur_defd.set(x);
                    }
                }
            }
        }

        public static void MRegLivenessAnalysis(graph.Graph g, object target)
        {
            var t = Target.targets[target as string];

            foreach (var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;
                mcn.live_in.Clear();
                mcn.live_out.Clear();
            }

            bool changes_made = false;
            do
            {
                changes_made = false;

                for (int i = g.LinearStream.Count - 1; i >= 0; i--)
                {
                    var n = g.LinearStream[i];
                    var mcn = n.c as MCNode;

                    util.Set cur = mcn.live_in.Clone();

                    foreach(var I in mcn.all_insts_rev)
                    {
                        if(t.NeedsMregLiveness(I))
                            I.mreg_live_out = cur.Clone();

                        foreach(var p in I.p)
                        {
                            if (p.IsMreg && p.IsDef)
                                cur.unset(p.mreg.id);
                        }
                        foreach(var p in I.p)
                        {
                            if (p.IsMreg && p.IsUse)
                                cur.set(p.mreg.id);
                        }

                        if (t.NeedsMregLiveness(I))
                            I.mreg_live_in = cur.Clone();
                    }


                    if (!cur.Equals(mcn.live_in))
                        changes_made = true;
                    mcn.live_in = cur;
                }
            } while (changes_made);
        }

        public static void LivenessAnalysis(graph.Graph g)
        {
            // Appel 10.4 (p214) adjusted to go backwards for efficiency

            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;
                mcn.live_in.Clear();
                mcn.live_out.Clear();
            }

            bool changes_made = false;
            do
            {
                changes_made = false;

                for(int i = g.LinearStream.Count - 1; i >= 0; i--)
                {
                    var n = g.LinearStream[i];
                    var mcn = n.c as MCNode;

                    util.Set in_ = mcn.live_in.Clone();
                    util.Set out_ = mcn.live_out.Clone();

                    util.Set new_in = mcn.live_out.Clone();
                    new_in.unset(mcn.kill);
                    new_in.Union(mcn.gen);

                    mcn.live_in = new_in;

                    util.Set new_out = new util.Set();
                    foreach (var s in n.Next)
                    {
                        var mcns = s.c as MCNode;
                        new_out.Union(mcns.live_in);
                    }
                    mcn.live_out = new_out;

                    if (!in_.Equals(new_in) || !out_.Equals(new_out))
                        changes_made = true;
                }
            } while (changes_made);
        }
    }
}
