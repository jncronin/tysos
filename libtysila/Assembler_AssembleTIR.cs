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
        /* A list of TIR instructions, with supporting metadata */
        internal class TACList
        {
            public Dictionary<var, IList<ThreeAddressCode>> liveness_defs, liveness_in, liveness_out, liveness_uses;
            public Dictionary<int, IList<int>> blocks_pred, blocks_suc;
            public IList<ThreeAddressCode> tacs;
            public IList<int> start_nodes, end_nodes;
            public Dictionary<int, ThreeAddressCode> block_starts;
            public Dictionary<int, IList<int>> inst_pred, inst_suc;
            public bool[,] doms;
            public int[] idoms;
            public bool[,] df;
            public Dictionary<var, IList<int>> ssa_stack;

            public int num_nodes { get { return tacs.Count; } }

            public TACList(IList<ThreeAddressCode> ts)
            {
                tacs = new List<ThreeAddressCode>(ts);
                liveness_defs = new Dictionary<var, IList<ThreeAddressCode>>();
                liveness_in = new Dictionary<var, IList<ThreeAddressCode>>();
                liveness_out = new Dictionary<var, IList<ThreeAddressCode>>();
                liveness_uses = new Dictionary<var, IList<ThreeAddressCode>>();
                blocks_pred = new Dictionary<int, IList<int>>();
                blocks_suc = new Dictionary<int, IList<int>>();
            }
        }

        private void DetermineJoinPoints(TACList t)
        {
            /* Determine the successors and predecessors of each block */

            t.blocks_pred = new Dictionary<int, IList<int>>();
            t.blocks_suc = new Dictionary<int, IList<int>>();
            t.start_nodes = new List<int>();
            t.end_nodes = new List<int>();
            t.block_starts = new Dictionary<int, ThreeAddressCode>();
            t.inst_pred = new Dictionary<int, IList<int>>();
            t.inst_suc = new Dictionary<int, IList<int>>();

            Dictionary<int, int> block_start_insts = new Dictionary<int, int>();

            int cur_block = -1;
            /* First, identify successors */
            for(int i = 0; i < t.tacs.Count; i++)
            {
                ThreeAddressCode tac = t.tacs[i];
                
                if (tac is LabelEx)
                {
                    cur_block = ((LabelEx)tac).Block_id;
                    t.block_starts[cur_block] = tac;
                    block_start_insts[cur_block] = i;
                }
                if (tac is BrEx)
                {
                    if(!t.blocks_suc.ContainsKey(cur_block))
                        t.blocks_suc[cur_block] = new List<int>();
                    t.blocks_suc[cur_block].Add(((BrEx)tac).Block_Target);
                }

                tac.block_id = cur_block;
            }

            /* Now, identify predecessors from the successor list */
            foreach (KeyValuePair<int, IList<int>> kvp in t.blocks_suc)
            {
                int pred = kvp.Key;
                foreach (int suc in kvp.Value)
                {
                    if (!t.blocks_pred.ContainsKey(suc))
                        t.blocks_pred[suc] = new List<int>();

                    if (!t.blocks_pred[suc].Contains(pred))
                        t.blocks_pred[suc].Add(pred);
                }

                /* Add the key to the predecessor list if it doesn't already exist */
                if (!t.blocks_pred.ContainsKey(pred))
                    t.blocks_pred[pred] = new List<int>();
            }

            /* Ensure each key in the predecessor list also exists in the successor list, and also determine
             *  start nodes  
             */
            foreach (KeyValuePair<int, IList<int>> kvp in t.blocks_pred)
            {
                if (!t.blocks_suc.ContainsKey(kvp.Key))
                    t.blocks_suc[kvp.Key] = new List<int>();
                if (kvp.Value.Count == 0)
                    t.start_nodes.Add(kvp.Key);
            }

            /* Identify end nodes */
            foreach (KeyValuePair<int, IList<int>> kvp in t.blocks_suc)
            {
                if (kvp.Value.Count == 0)
                    t.end_nodes.Add(kvp.Key);
            }

            /* Now, loop through again, generating the successors of each instruction */
            for (int i = 0; i < t.tacs.Count; i++)
            {
                ThreeAddressCode tac = t.tacs[i];

                t.inst_suc[i] = new List<int>();
                t.inst_pred[i] = new List<int>();

                /* The successors will always include the next instruction, unless:
                 * 1) it is the last instruction
                 * 2) it is a br/br_ehclause/throw instruction
                 */

                if ((i != (t.tacs.Count - 1)) && tac.FallsThrough)
                    t.inst_suc[i].Add(i + 1);

                /* If it is a branch instruction, then add the first instruction of the target block */
                if (tac is BrEx)
                    t.inst_suc[i].Add(block_start_insts[((BrEx)tac).Block_Target]);
            }

            /* Now, loop through again, generating the predecessors of each instruction */
            for (int i = 0; i < t.tacs.Count; i++)
            {
                foreach (int j in t.inst_suc[i])
                    t.inst_pred[j].Add(i);
            }
        }
    }
}
