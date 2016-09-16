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
using libtysila4.util;

namespace libtysila4.target
{
    public class SimplifyLocalVars
    {
        public static graph.Graph SimplifyLocalVarsPass(graph.Graph g, Target t)
        {
            int lvar_count = g.lvars_for_simplifying.get_last_set() + 1;

            int[] lv_st_map = new int[lvar_count];
            for (int i = 0; i < lvar_count; i++)
                lv_st_map[i] = -1;

            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;

                for(int i = 0; i < mcn.insts.Count; i++)
                {
                    var I = mcn.insts[i];

                    foreach(var p in I.p)
                    {
                        if (p.IsLV && g.lvars_for_simplifying.get((int)p.v))
                        {
                            // This var is suitable for simplifying

                            int new_st;
                            if (lv_st_map[p.v] == -1)
                            {
                                new_st = g.next_vreg_id++;
                                lv_st_map[p.v] = new_st;
                            }
                            else
                                new_st = lv_st_map[p.v];


                            switch(p.t)
                            {
                                case ir.Opcode.vl_lv:
                                    p.t = ir.Opcode.vl_stack;
                                    p.v = new_st;
                                    break;
                                case ir.Opcode.vl_lv32:
                                    p.t = ir.Opcode.vl_stack32;
                                    p.v = new_st;
                                    break;
                                case ir.Opcode.vl_lv64:
                                    p.t = ir.Opcode.vl_stack64;
                                    p.v = new_st;
                                    break;
                            }
                        }
                    }
                }
            }

            return g;
        }
    }
}
