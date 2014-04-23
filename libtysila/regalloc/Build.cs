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

namespace libtysila.regalloc
{
    partial class RegAlloc
    {
        void Build(tybel.Tybel.TybelCode code)
        {
            foreach (tybel.Node n in code.Code)
            {
                util.Set<vara> live = new util.Set<vara>(code.Liveness.live_out[n]);
                if (n.IsMove)
                {
                    live = live.Except(n.uses);

                    util.Set<vara> defs_and_uses = new util.Set<vara>(n.uses).Union(n.defs);
                    foreach (vara def_or_use in defs_and_uses)
                    {
                        if (!moveList.ContainsKey(def_or_use))
                            moveList[def_or_use] = new util.Set<timple.BaseNode>();
                        moveList[def_or_use].Add(n);
                    }

                    worklistMoves.Add(n);
                }

                live = live.Union(n.defs);

                foreach (vara d in n.defs)
                {
                    foreach (vara l in live)
                    {
                        AddEdge(l, d);
                    }
                }
            }
        }
    }
}
