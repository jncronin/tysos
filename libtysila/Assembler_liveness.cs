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

namespace libtysila
{
    partial class Assembler
    {
        void LivenessAnalysis2(List<cfg_node> nodes, cfg_node end_node, AssemblerState state)
        {
            state.liveness_defs = new Dictionary<var, IList<ThreeAddressCode>>();
            state.liveness_in = new Dictionary<var, IList<ThreeAddressCode>>();
            state.liveness_out = new Dictionary<var, IList<ThreeAddressCode>>();
            state.liveness_uses = new Dictionary<var, IList<ThreeAddressCode>>();

            /* Iterate through each node, determining uses and defs at each function */
            foreach (cfg_node n in nodes)
            {
                DetermineUsesAndDefs(n, state);
            }

            /* Iterate backwards through the nodes, and then each instruction within the node */
            foreach (cfg_node n in nodes)
            {
                n.live_vars_at_end.Clear();
                n.live_vars_done = false;
            }

            LivenessAnalysis2(end_node, state);
        }

        private void DetermineUsesAndDefs(cfg_node node, AssemblerState state)
        {
            DetermineUsesAndDefs(GetTacList(node, state), state.liveness_uses, state.liveness_defs);
        }

        private void DetermineUsesAndDefs(TACList tacs)
        {
            tacs.liveness_uses = new Dictionary<var, IList<ThreeAddressCode>>();
            tacs.liveness_defs = new Dictionary<var, IList<ThreeAddressCode>>();
            DetermineUsesAndDefs(tacs.tacs, tacs.liveness_uses, tacs.liveness_defs);
        }

        private void DetermineUsesAndDefs(IList<ThreeAddressCode> tac_list, Dictionary<var, IList<ThreeAddressCode>> liveness_uses,
            Dictionary<var, IList<ThreeAddressCode>> liveness_defs)
        {
            for(int i = 0; i < tac_list.Count; i++)
            {
                ThreeAddressCode tac = tac_list[i];

                List<var> defs = new List<var>();
                List<var> uses = new List<var>();

                /* Do we have a def here? */
                if (tac.Result.type == var.var_type.LogicalVar)
                    defs.Add(tac.Result);

                /* Identify uses */
                if (tac.Result.contents_of)
                    uses.Add(tac.Result.ReferencedLogicalVar);
                uses.Add(tac.Operand1.ReferencedLogicalVar);
                uses.Add(tac.Operand2.ReferencedLogicalVar);

                if (tac is CallEx)
                {
                    CallEx ce = tac as CallEx;

                    foreach (var v in ce.Var_Args)
                        uses.Add(v.ReferencedLogicalVar);
                }
                if (tac is PhiEx2)
                {
                    PhiEx2 pe = tac as PhiEx2;

                    foreach (var v in pe.Var_Args)
                        uses.Add(v.ReferencedLogicalVar);
                }

                tac.uses = new List<var>();
                tac.defs = new List<var>();

                foreach (var v in defs)
                {
                    if (v.type == var.var_type.LogicalVar)
                    {
                        if (!liveness_defs.ContainsKey(v))
                            liveness_defs[v] = new List<ThreeAddressCode>();
                        liveness_defs[v].Add(tac);
                        tac.defs.Add(v);
                    }
                }

                foreach (var v in uses)
                {
                    if (v.type == var.var_type.LogicalVar)
                    {
                        if (!liveness_uses.ContainsKey(v))
                            liveness_uses[v] = new List<ThreeAddressCode>();
                        liveness_uses[v].Add(tac);
                        tac.uses.Add(v);
                    }
                }
            }
        }

        private IList<ThreeAddressCode> GetTacList(cfg_node node, AssemblerState state)
        {
            switch (state.CurPass)
            {
                case AssemblerState.Pass.Preoptimized:
                    return node.tacs;
                case AssemblerState.Pass.SSA:
                    return node.ssa_ir;
                case AssemblerState.Pass.Optimized:
                    return node.optimized_ir;
                default:
                    throw new NotSupportedException();
            }
        }

        void LivenessAnalysis2(cfg_node cur_node, AssemblerState state)
        {
            IList<ThreeAddressCode> tac_list = GetTacList(cur_node, state);

            for (int i = tac_list.Count - 1; i >= 0; i--)
            {
                ThreeAddressCode tac = tac_list[i];
            }

            foreach (cfg_node node in cur_node.ipred)
                LivenessAnalysis2(node, state);
        }
    }
}
