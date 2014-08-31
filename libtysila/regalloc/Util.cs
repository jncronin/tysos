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
        void AddEdge(vara u, vara v)
        {
            if ((u.VarType == vara.vara_type.MachineReg) && (u.MachineRegVal is libasm.hardware_contentsof))
                return;
            if ((v.VarType == vara.vara_type.MachineReg) && (v.MachineRegVal is libasm.hardware_contentsof))
                return;

            if(!adjSet.Contains(new InterferenceEdge(u, v)) && !u.Equals(v))
            {
                adjSet.Add(new InterferenceEdge(u, v));

                if (!precolored.Contains(u))
                {
                    adjList[u].Add(v);
                    degree[u] = degree[u] + 1;
                }

                if (!precolored.Contains(v))
                {
                    adjList[v].Add(u);
                    degree[v] = degree[v] + 1;
                }
            }
        }

        bool MoveRelated(vara n)
        {
            return NodeMoves(n).Count != 0;
        }

        ICollection<timple.BaseNode> NodeMoves(vara n)
        {
            return new util.Set<timple.BaseNode>(activeMoves).Union(worklistMoves).Intersect(moveList[n]);
        }

        ICollection<vara> Adjacent(vara n)
        {
            return adjList[n].Except(coalescedNodes.Union(selectStack));
        }

        void DecrementDegree(vara m)
        {
            int d = degree[m];
            degree[m] = d - 1;

            if (d == K)
            {
                util.Set<vara> moves = new util.Set<vara>(Adjacent(m));
                moves.Add(m);
                EnableMoves(moves);

                spillWorklist.Remove(m);
                if (MoveRelated(m))
                    freezeWorklist.Add(m);
                else
                    simplifyWorklist.Add(m);
            }
        }

        void EnableMoves(ICollection<vara> nodes)
        {
            foreach (vara n in nodes)
            {
                foreach (timple.BaseNode m in NodeMoves(n))
                {
                    if (activeMoves.Contains(m))
                    {
                        activeMoves.Remove(m);
                        worklistMoves.Add(m);
                    }
                }
            }
        }

        void AddWorkList(vara u)
        {
            if ((!precolored.Contains(u)) && (!MoveRelated(u)) && (degree[u] < K))
            {
                freezeWorklist.Remove(u);
                simplifyWorklist.Add(u);
            }
        }

        bool OK(vara t, vara r)
        {
            if (degree[t] < K)
                return true;

            if (precolored.Contains(t))
                return true;

            if (adjSet.Contains(new InterferenceEdge(t, r)))
                return true;

            return false;
        }

        bool Conservative(IEnumerable<vara> nodes)
        {
            int k = 0;
            foreach (vara n in nodes)
            {
                if (degree[n] >= K)
                    k++;
            }
            return k < K;
        }

        vara GetAlias(vara n)
        {
            vara alias = GetAlias2(n);
            return alias;
        }

        vara GetAlias2(vara n)
        {
            if (coalescedNodes.Contains(n))
                return GetAlias2(alias[n]);
            else
                return n;
        }

        void Combine(vara u, vara v)
        {
            if (freezeWorklist.Contains(v))
                freezeWorklist.Remove(v);
            else
                spillWorklist.Remove(v);

            coalescedNodes.Add(v);
            alias[v] = u;
            moveList[u] = moveList[u].Union(moveList[v]);

            foreach (vara t in Adjacent(v))
            {
                AddEdge(t, u);
                DecrementDegree(t);
            }

            if ((degree[u] >= K) && freezeWorklist.Contains(u))
            {
                freezeWorklist.Remove(u);
                spillWorklist.Add(u);
            }
        }
    }
}
