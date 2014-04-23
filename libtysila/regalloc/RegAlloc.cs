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

/* See Appel p. 244+ for the algorithm */

using System;
using System.Collections.Generic;

namespace libtysila.regalloc
{
    public partial class RegAlloc
    {
        util.Set<vara> precolored = new util.Set<vara>();
        util.Set<vara> initial = new util.Set<vara>();
        util.Set<vara> simplifyWorklist = new util.Set<vara>();
        util.Set<vara> freezeWorklist = new util.Set<vara>();
        util.Set<vara> spillWorklist = new util.Set<vara>();
        util.Set<vara> spilledNodes = new util.Set<vara>();
        util.Set<vara> coalescedNodes = new util.Set<vara>();
        util.Set<vara> coloredNodes = new util.Set<vara>();
        util.Stack<vara> selectStack = new util.Stack<vara>();

        util.Set<timple.BaseNode> coalescedMoves = new util.Set<timple.BaseNode>();
        util.Set<timple.BaseNode> constrainedMoves = new util.Set<timple.BaseNode>();
        util.Set<timple.BaseNode> frozenMoves = new util.Set<timple.BaseNode>();
        util.Set<timple.BaseNode> worklistMoves = new util.Set<timple.BaseNode>();
        util.Set<timple.BaseNode> activeMoves = new util.Set<timple.BaseNode>();

        util.Set<InterferenceEdge> adjSet = new util.Set<InterferenceEdge>();
        Dictionary<vara, util.Set<vara>> adjList = new Dictionary<vara, util.Set<vara>>();
        Dictionary<vara, util.Set<timple.BaseNode>> moveList = new Dictionary<vara, util.Set<timple.BaseNode>>();
        Dictionary<vara, vara> alias = new Dictionary<vara, vara>();
        Dictionary<vara, int> color = new Dictionary<vara, int>();
        Dictionary<vara, int> degree = new Dictionary<vara, int>();

        int K;

        public Dictionary<vara, int> Main(tybel.Tybel.TybelCode code, int k)
        {
            K = k;

            /* init structures */
            foreach (vara v in code.Liveness.defs.Keys)
            {
                adjList[v] = new util.Set<vara>();
                moveList[v] = new util.Set<timple.BaseNode>();
                degree[v] = 0;

                if (v.VarType == vara.vara_type.MachineReg)
                    precolored.Add(v);
            }

            Build(code);
            MakeWorklist(code);
            do
            {
                if (simplifyWorklist.Count != 0)
                    Simplify(code);
                else if (worklistMoves.Count != 0)
                    Coalesce(code);
                else if (freezeWorklist.Count != 0)
                    Freeze(code);
                else if (spillWorklist.Count != 0)
                    SelectSpill(code);
            } while ((simplifyWorklist.Count != 0) || (worklistMoves.Count != 0) ||
            (freezeWorklist.Count != 0) || (spillWorklist.Count != 0));

            AssignColors(code);

            if (spilledNodes.Count != 0)
                throw new NotImplementedException();

            return color;
        }

        public struct InterferenceEdge
        {
            public vara u, v;
            public InterferenceEdge(vara U, vara V)
            { u = U; v = V; }
            public override bool Equals(object obj)
            {
                if (!(obj is InterferenceEdge))
                    return false;
                InterferenceEdge other = (InterferenceEdge)obj;
                if (u.Equals(other.u) && v.Equals(other.v))
                    return true;
                if (u.Equals(other.v) && v.Equals(other.u))
                    return true;
                return false;
            }
            public override int GetHashCode()
            {
                return u.GetHashCode() ^ v.GetHashCode();
            }
        }
    }
}
