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
        void Coalesce(tybel.Tybel.TybelCode code)
        {
            timple.BaseNode m = worklistMoves.ItemAtIndex(0);

            IEnumerator<vara> enum_x = m.defs.GetEnumerator();
            enum_x.MoveNext();
            vara x = GetAlias(enum_x.Current);

            IEnumerator<vara> enum_y = m.uses.GetEnumerator();
            enum_y.MoveNext();
            vara y = GetAlias(enum_y.Current);

            vara u, v;
            if (precolored.Contains(y))
            {
                u = y;
                v = x;
            }
            else
            {
                u = x;
                v = y;
            }

            worklistMoves.Remove(m);

            /* Calculate part of the following in advance */
            bool all_ok = true;
            if (precolored.Contains(v))
            {
                foreach (vara t in Adjacent(v))
                {
                    if (OK(t, u) == false)
                    {
                        all_ok = false;
                        break;
                    }
                }
            }

            if (u.Equals(v))
            {
                coalescedMoves.Add(m);
                AddWorkList(u);
            }
            else if (precolored.Contains(v) || adjSet.Contains(new InterferenceEdge(u, v)))
            {
                constrainedMoves.Add(m);
                AddWorkList(u);
                AddWorkList(v);
            }
            else if ((precolored.Contains(v) && all_ok) ||
                ((!precolored.Contains(v)) && Conservative(new util.Set<vara>(Adjacent(u)).Union(Adjacent(v)))))
            {
                coalescedMoves.Add(m);
                Combine(u, v);
                AddWorkList(u);
            }
            else
                activeMoves.Add(m);
        }
    }
}
