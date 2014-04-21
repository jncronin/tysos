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
    public class DeadCodeElimination
    {
        public static void DoElimination(TimpleGraph ssa, Liveness l)
        {
            /* Appel p. 446 for algorithm */

            util.Set<vara> W = new util.Set<vara>(l.defs.Keys);

            while (W.Count > 0)
            {
                vara v = W.ItemAtIndex(0);
                W.Remove(v);

                if (!l.uses.ContainsKey(v) || l.uses[v].Count == 0)
                {
                    TreeNode S = l.defs[v].ItemAtIndex(0) as TreeNode;

                    if (!(S is TimpleCallNode))
                    {
                        if (S is TimpleLabelNode)
                        {
                            /* Find the actual phi instruction within the label */
                            TimplePhiInstNode Sphi = null;
                            foreach (TimplePhiInstNode phi in ((TimpleLabelNode)S).Phis)
                            {
                                if (phi.R.Equals(v))
                                {
                                    Sphi = phi;
                                    break;
                                }
                            }

                            ((TimpleLabelNode)S).Phis.Remove(Sphi);
                            l.defs[Sphi.R].Remove(S);

                            foreach (vara xi in Sphi.uses)
                            {
                                l.uses[xi].Remove(S);
                                W.Add(xi);
                            }
                        }
                        else
                        {
                            ssa.RemoveNode(S, l);
                            foreach (vara xi in S.defs)
                                l.defs[xi].Remove(S);

                            foreach (vara xi in S.uses)
                            {
                                l.uses[xi].Remove(S);
                                W.Add(xi);
                            }
                        }
                    }
                }
            }
        }
    }
}
