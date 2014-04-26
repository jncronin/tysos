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

        public Dictionary<vara, vara> Main(tybel.Tybel.TybelCode code, Assembler ass)
        {
            util.Set<libasm.hardware_location> all_regs = ass.MachineRegisters;
            K = all_regs.Count;

            /* Precolor machine regs */
            int c = 0;
            Dictionary<int, libasm.hardware_location> c_hloc = new Dictionary<int, libasm.hardware_location>();
            foreach (libasm.hardware_location hloc in all_regs)
            {
                vara v = vara.MachineReg(hloc);
                precolored.Add(v);
                adjList[v] = new util.Set<vara>();
                moveList[v] = new util.Set<timple.BaseNode>();
                degree[v] = 0;
                color[v] = c++;
                c_hloc[color[v]] = hloc;
            }

            /* init structures */
            List<InterferenceEdge> preset_ifedges = new List<InterferenceEdge>();
            foreach (vara v in code.Liveness.defs.Keys)
            {
                adjList[v] = new util.Set<vara>();
                moveList[v] = new util.Set<timple.BaseNode>();
                degree[v] = 0;

                if (v.VarType == vara.vara_type.Logical)
                {
                    /* Depending on type of variable, add interference edges to
                     * the hardware registers it cannot access */

                    foreach (libasm.hardware_location reg in all_regs.Except(ass.MachineRegistersForDataType(v.DataType)))
                        preset_ifedges.Add(new InterferenceEdge(v, vara.MachineReg(reg)));
                }
            }

            foreach (InterferenceEdge edge in preset_ifedges)
            {
                AddEdge(edge.u, edge.v);
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

            Dictionary<vara, vara> ret = new Dictionary<vara, vara>();
            foreach (vara v in code.Liveness.defs.Keys)
            {
                if (v.VarType == vara.vara_type.Logical)
                    ret[v] = vara.MachineReg(c_hloc[color[v]]);
            }

            return ret;
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

            public override string ToString()
            {
                return u.ToString() + ", " + v.ToString();
            }
        }
    }
}

namespace libtysila
{
    partial class Assembler
    {
        public abstract util.Set<libasm.hardware_location> MachineRegisters { get; }
        public abstract util.Set<libasm.hardware_location> MachineRegistersForDataType(CliType dt);
    }
}
