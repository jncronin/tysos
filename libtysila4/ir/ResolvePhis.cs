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
    public class ResolvePhis
    {
        public static graph.Graph ResolvePhisPass(graph.Graph g, target.Target t)
        {
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as Opcode;

                int p_idx = 0;
                foreach(var p in n.Prev)
                {
                    var p_mcn = p.c as Opcode;

                    foreach(var phi in mcn.phis)
                    {
                        if (phi.defs == null || phi.defs.Length == 0)
                            continue;

                        var phi_dest = phi.defs[0];
                        var phi_src = phi.uses[p_idx];

                        p_mcn.post_insts.Add(
                            new Opcode
                            {
                                oc = ir.Opcode.oc_store,
                                uses = new Param[] { phi_src },
                                defs = new Param[] { phi_dest }
                            });
                    }

                    p_idx++;
                }

                mcn.phis.Clear();
            }

            return g;
        }
    }
}
