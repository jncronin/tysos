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
using libtysila4.util;

namespace libtysila4.ir
{
    public class DeadCodeElimination
    {
        public static graph.Graph DeadCodeEliminationPass(graph.Graph g, target.Target t)
        {
            Set W = new Set();
            W.set(g.uses.Keys);

            while(!W.Empty)
            {
                var v = W.get_first_set();
                W.unset(v);

                if(g.uses[v].Count == 0 ||
                    (g.uses[v].Count == 1 &&
                    g.uses[v].GetAny().Equals(g.defs[v].GetAny())))
                {
                    var Ss = g.defs[v];
                    if (Ss.Count != 1)
                        throw new Exception();

                    var S = Ss.GetAny();
                    var Si = S.inst;

                    if (!Si.HasSideEffects)
                    {
                        if (Si.uses != null)
                        {
                            foreach (var P in Si.uses)
                            {
                                if (P.IsStack)
                                {
                                    var xi = P.ssa_idx;

                                    var c = g.uses[xi].Count;
                                    g.uses[xi].Remove(S);
                                    if (g.uses[xi].Count != c - 1)
                                        System.Diagnostics.Debugger.Break();
                                    W.set(xi);
                                }
                            }
                        }

                        Si.oc = Opcode.oc_nop;
                        Si.uses = null;
                        Si.defs = null;
                    }
                }
            }

            return g;
        }
    }
}
