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

namespace libtysila4.ir
{
    public class StackTracePass
    {
        public static graph.Graph TraceStackPass(graph.Graph g, target.Target t)
        {
            IrGraph ir = g as IrGraph;

            int next_vreg_id = g.next_vreg_id;

            /* First, identify types of local args */
            var sig_idx = ir._m.GetMethodDefSigRetTypeIndex(ir._mdef_sig);
            var p_count = ir._m.GetMethodDefSigParamCount(ir._mdef_sig);
            var p_count_this = ir._m.GetMethodDefSigParamCountIncludeThis(ir._mdef_sig);

            // skip rettype
            ir._m.GetTypeSpec(ref sig_idx, g.ms.gtparams, g.ms.gmparams);

            ir.largs = new int[p_count_this];
            int next_larg_idx = 0;
            if(p_count_this != p_count)
            {
                // TODO: use ref for this pointer in value types
                ir.largs[next_larg_idx++] = Opcode.ct_object;
            }
            for(int i = 0; i < p_count; i++)
            {
                bool is_req;
                uint token;
                while (ir._m.GetRetTypeCustomMod(ref sig_idx, out is_req, out token)) ;

                var pt = ir._m.GetTypeSpec(ref sig_idx, g.ms.gtparams, g.ms.gmparams);

                ir.largs[next_larg_idx++] = Opcode.GetCTFromType(pt);
            }


            /* And local vars */
            if (ir._lvar_sig_tok == 0)
                ir.lvars = new int[0];
            else
            {
                /* .locals directive should generate a token to 
                    the StandAloneSig table (0x11) */
                int table_id, row;
                ir._m.InterpretToken(ir._lvar_sig_tok, out table_id, out row);
                if (table_id != 0x11)
                    throw new Exception(".locals does not reference StandAloneSig (" + table_id.ToString("X2") + ")");

                var lvar_idx = (int)ir._m.GetIntEntry(table_id, row, 0);
                var lvar_count = ir._m.GetLocalVarCount(ref lvar_idx);
                ir.lvars = new int[lvar_count];
                for (int i = 0; i < lvar_count; i++)
                {
                    var pt_ts = ir._m.GetTypeSpec(ref lvar_idx, ir.ms.gtparams, ir.ms.gmparams);
                    ir.lvars[i] = Opcode.GetCTFromType(pt_ts);
                }
            }


            /* Lists of cil types that start and end basic blocks */
            List<int>[] bb_start_stacks = new List<int>[ir.blocks.Count];
            List<int>[] bb_end_stacks = new List<int>[ir.blocks.Count];

            /* List of vreg ids that start and end basic blocks */
            List<int>[] bb_start_vreg_stacks = new List<int>[ir.blocks.Count];
            List<int>[] bb_end_vreg_stacks = new List<int>[ir.blocks.Count];

            /* Trace all blocks, beginning at the start (BB = 0) */
            List<int> meth_start_stack = new List<int>();
            bb_start_stacks[0] = meth_start_stack;
            List<int> meth_start_vreg_stack = new List<int>();
            bb_start_vreg_stacks[0] = meth_start_vreg_stack;

            TraceBasicBlockStack(0, bb_start_stacks, bb_end_stacks, 
                bb_start_vreg_stacks, bb_end_vreg_stacks,
                ir, ref next_vreg_id, t);

            ir.next_vreg_id = next_vreg_id;

            return ir;  
        }

        private static void TraceBasicBlockStack(int bb_id, 
            List<int>[] bb_start_stacks, List<int>[] bb_end_stacks, 
            List<int>[] bb_start_vreg_stacks, List<int>[] bb_end_vreg_stacks,
            IrGraph g, ref int next_vreg_id, target.Target t)
        {
            // if already done, skip this block
            if (bb_end_stacks[bb_id] != null)
                return;

            List<int> bb_start_stack = bb_start_stacks[bb_id];
            List<int> bb_start_vreg_stack = bb_start_vreg_stacks[bb_id];

            var bb = g.blocks[bb_id];

            // Clone the start stack to use to iterate through
            List<int> cur_stack = new List<int>(bb_start_stack);
            List<int> cur_vreg_stack = new List<int>(bb_start_vreg_stack);
            foreach (var n in bb)
            {
                var c = n.c as Opcode;
                // Specially handle swap statement
                if (c.oc == Opcode.oc_swap)
                {
                    var tmp = cur_stack[cur_stack.Count - 1];
                    cur_stack[cur_stack.Count - 1] = cur_stack[cur_stack.Count - 2];
                    cur_stack[cur_stack.Count - 2] = tmp;

                    continue;
                }
                // First, identify the actual stack ids and var types of
                //  all items on the stack in the uses column

                int max_to_remove = -1;
                if (c.uses != null)
                {
                    foreach (var use in c.uses)
                    {
                        if (use.t == Opcode.vl_stack && use.stack_abs == false)
                        {
                            if (use.v > max_to_remove)
                                max_to_remove = (int)use.v;

                            int stack_id = cur_stack.Count - 1 - (int)use.v;
                            use.v = cur_vreg_stack[stack_id];
                            use.ct = cur_stack[stack_id];
                        }
                        else if (use.t == Opcode.vl_lv)
                            use.ct = g.lvars[use.v];
                        else if (use.t == Opcode.vl_arg)
                            use.ct = g.largs[use.v];
                    }

                    max_to_remove++;
                    while (max_to_remove > 0)
                    {
                        cur_stack.RemoveAt(cur_stack.Count - 1);
                        cur_vreg_stack.RemoveAt(cur_vreg_stack.Count - 1);
                        max_to_remove--;
                    }
                }

                if (c.empties_stack)
                {
                    cur_stack.Clear();
                    cur_vreg_stack.Clear();
                }

                if(c.defs != null)
                {
                    foreach(var def in c.defs)
                    {
                        if(def.t == Opcode.vl_stack && def.stack_abs == false)
                        {
                            if (def.v != 0)
                                throw new NotImplementedException();

                            // determine the type of argument pushed to the
                            //  stack by this instruction
                            int ct = def.ct;
                            if(ct == Opcode.ct_unknown)
                            {
                                ct = Opcode.oc_pushes_map[c.oc](c, t);
                                def.ct = ct;
                            }
                            def.v = next_vreg_id;

                            // needs to be expanded for things other than
                            //  st0
                            cur_stack.Add(ct);
                            cur_vreg_stack.Add(next_vreg_id++);

                        }
                        else if (def.t == Opcode.vl_lv)
                            def.ct = g.lvars[def.v];
                        else if (def.t == Opcode.vl_arg)
                            def.ct = g.largs[def.v];
                    }
                }
            }

            bb_end_stacks[bb_id] = cur_stack;
            bb_end_vreg_stacks[bb_id] = cur_vreg_stack;

            // Trace stacks through successor BBs
            foreach(var next in g.bbs_after[bb_id])
            {
                var next_start_stack = bb_start_stacks[next];
                if(next_start_stack == null)
                {
                    bb_start_stacks[next] = cur_stack;
                    bb_start_vreg_stacks[next] = cur_vreg_stack;
                    TraceBasicBlockStack(next, bb_start_stacks,
                        bb_end_stacks, bb_start_vreg_stacks,
                        bb_end_vreg_stacks, g, ref next_vreg_id, t);
                }
                else
                {
                    // ensure the stacks match
                    if (next_start_stack.Count != cur_stack.Count)
                        throw new Exception();
                    for(int i = 0; i < next_start_stack.Count; i++)
                    {
                        if (next_start_stack[i] != cur_stack[i])
                            throw new Exception();
                    }

                    var next_start_vreg_stack = bb_start_vreg_stacks[next];
                    if (next_start_vreg_stack.Count != cur_vreg_stack.Count)
                        throw new Exception();
                    for (int i = 0; i < next_start_vreg_stack.Count; i++)
                    {
                        if (next_start_vreg_stack[i] != cur_vreg_stack[i])
                            throw new Exception();  // TODO: fix up stacks here
                    }

                }
            }
        }
    }
}
