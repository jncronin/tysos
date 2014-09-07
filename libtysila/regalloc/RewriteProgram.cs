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
        void RewriteProgram(tybel.Tybel.TybelCode code, Assembler ass, Assembler.MethodAttributes attrs)
        {
            /* Allocate memory locations for each v in spilledNodes */
            Dictionary<vara, libasm.hardware_location> memlocs = new Dictionary<vara, libasm.hardware_location>();

            util.Set<vara> newTemps = new util.Set<vara>();

            ass.MachineRegistersResetStack(attrs.SpillStackLocs);

            foreach (vara v in spilledNodes)
            {
                util.Set<libasm.hardware_location> possible_locs = ass.MachineRegistersForDataType(v.DataType, true, attrs.SpillStackLocs);

                if (possible_locs.Count == 0)
                    throw new Exception("Unable to allocate memory location for variable " + v.ToString());

                libasm.hardware_location sn_loc = possible_locs.ItemAtIndex(0);
                memlocs[v] = sn_loc;

                /* Rewrite each definition of v to define vi instead, where vi is a new
                 * variable, and insert a store from vi to sn_loc after each definition */
                foreach(tybel.Node n in code.Liveness.defs[v])
                {
                    vara vi = vara.Logical(code.NextVar++, v.DataType);
                    newTemps.Add(vi);

                    for (int i = 0; i < n.VarList.Count; i++)
                    {
                        if (n.VarList[i].Equals(v))
                            n.VarList[i] = vi;
                    }

                    timple.TimpleNode tn = new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, v.DataType, v.vt_type),
                        vara.MachineReg(sn_loc, v.DataType), vi, vara.Void());
                    IList<tybel.Node> store_nodes = ass.SelectInstruction(tn, ref code.NextVar, ref code.NextBlock, null, null);

                    tybel.Node cur_n = n;
                    foreach (tybel.Node cur_store_n in store_nodes)
                    {
                        cur_n.InsertAfter(cur_store_n);
                        cur_n = cur_store_n;
                    }
                }

                /* Likewise, rewrite each use of v to use vi instead, where vi is a new
                 * variable, and insert a fetch from sn_loc to vi before each use */
                foreach (tybel.Node n in code.Liveness.uses[v])
                {
                    vara vi = vara.Logical(code.NextVar++, v.DataType);
                    newTemps.Add(vi);

                    for (int i = 0; i < n.VarList.Count; i++)
                    {
                        if (n.VarList[i].Equals(v))
                            n.VarList[i] = vi;
                    }

                    timple.TimpleNode tn = new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, v.DataType, v.vt_type),
                        vi, vara.MachineReg(sn_loc, v.DataType), vara.Void());
                    IList<tybel.Node> fetch_nodes = ass.SelectInstruction(tn, ref code.NextVar, ref code.NextBlock, null, null);

                    tybel.Node cur_n = n;
                    for(int i = fetch_nodes.Count - 1; i >= 0; i--)
                    {
                        tybel.Node cur_store_n = fetch_nodes[i];
                        cur_n.InsertBefore(cur_store_n);
                        cur_n = cur_store_n;
                    }
                }
            }

            initial.Clear();
            initial.AddRange(coloredNodes);
            initial.AddRange(coalescedNodes);
            initial.AddRange(newTemps);
            code.Liveness = timple.Liveness.LivenessAnalysis(code.CodeGraph);
        }
    }
}
