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
    public class CalleePreserves
    {
        public static graph.Graph CalleePreservesPass(graph.Graph input, Target t)
        {
            /* First get a set of all defined registers */
            util.Set mreg_defs = new Set();
            foreach (var n in input.LinearStream)
            {
                var mcn = n.c as MCNode;

                foreach (var I in mcn.all_insts)
                {
                    foreach (var p in I.p)
                    {
                        if (p.IsMreg && p.IsDef)
                            mreg_defs.Union(p.mreg.mask);
                    }
                }
            }

            var cc = "sysv";

            var callee_preserves = t.cc_callee_preserves_map[cc];

            mreg_defs.Intersect(callee_preserves);

            foreach(var p in input.LinearStream)
            {
                var mcn = p.c as MCNode;
                for(int i = 0; i < mcn.insts.Count; i++)
                {
                    var I = mcn.insts[i];

                    if(I.p.Length > 0 && I.p[0].t == ir.Opcode.vl_str)
                    {
                        if(I.p[0].v == Generic.g_savecalleepreserves)
                        {
                            mcn.insts.RemoveAt(i);
                            foreach(var r in mreg_defs)
                            {
                                var reg = t.regs[r];
                                mcn.insts.Insert(i++, t.SaveRegister(reg));
                            }
                            i--;
                        }
                        else if(I.p[0].v == Generic.g_restorecalleepreserves)
                        {
                            mcn.insts.RemoveAt(i);
                            int count = 0;
                            foreach (var r in mreg_defs)
                            {
                                var reg = t.regs[r];

                                // no i++ here so that restores are done backwards
                                mcn.insts.Insert(i, t.RestoreRegister(reg));
                                count++;
                            }
                            i = i + count - 1;
                        }
                    }
                }
            }
            
            return input;
        }
    }
}
