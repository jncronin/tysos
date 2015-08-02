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
        void SelectSpill(tybel.Tybel.TybelCode code)
        {
            /* Count the distance between each def of a variable and its first use */
            Dictionary<vara, int> dists = new Dictionary<vara, int>();

            int max_dist = 0;
            vara m = vara.Void();

            foreach (vara v in spillWorklist)
            {
                IEnumerator<timple.BaseNode> def_enum = code.Liveness.defs[v].GetEnumerator();
                def_enum.MoveNext();
                timple.BaseNode def = def_enum.Current;

                int dist = -1;

                foreach (timple.BaseNode use in code.Liveness.uses[v])
                {
                    int cur_dist = GetDistance(def, use);
                    if (dist == -1)
                        dist = cur_dist;
                    else if (dist < cur_dist)
                        cur_dist = dist;
                }

                if (dist == -1)
                    throw new Exception();

                dists[v] = dist;

                if (dist > max_dist && v.VarType == vara.vara_type.Logical)
                {
                    max_dist = dist;
                    m = v;
                }
            }

            if (m.VarType != vara.vara_type.Logical)
                throw new Exception();

            spillWorklist.Remove(m);
            simplifyWorklist.Add(m);
            FreezeMoves(m);
        }

        private int GetDistance(timple.BaseNode from, timple.BaseNode to)
        {
            util.Set<timple.BaseNode> visited = new util.Set<timple.BaseNode>();
            int dist = 0;
            DFGetDistance(from, to, visited, ref dist);
            return dist;
        }

        private bool DFGetDistance(timple.BaseNode from, timple.BaseNode to, util.Set<timple.BaseNode> visited, ref int dist)
        {
            if (visited.Contains(from))
                return false;
            visited.Add(from);

            if (from == to)
                return true;

            dist++;

            int min_dist = -1;
            bool found = false;

            foreach (timple.BaseNode next in from.Next)
            {
                int cur_dist = dist;
                if (DFGetDistance(next, to, visited, ref cur_dist))
                {
                    found = true;
                    if (min_dist == -1)
                        min_dist = cur_dist;
                    else if (cur_dist < min_dist)
                        min_dist = cur_dist;
                }
            }

            if (found)
            {
                dist = min_dist;
                return true;
            }

            return false;
        }
    }
}
