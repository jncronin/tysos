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
    public class PreserveRegistersAroundCall
    {
        public static graph.Graph PreserveRegistersAroundCallPass(graph.Graph input, object target)
        {
            var t = Target.targets[target as string];
            var cc = "sysv";

            // TODO: make cc_ dictionaries be part of Target
            var caller_preserves = x86.x86_Assembler.cc_caller_preserves_map[cc];

            /* store the set of registers pushed by the last
            precall for use at the postcall */
            util.Set to_push = new Set();

            foreach(var p in input.LinearStream)
            {
                var mcn = p.c as MCNode;
                for(int i = 0; i < mcn.insts.Count; i++)
                {
                    var I = mcn.insts[i];

                    if(I.p.Length > 0 && I.p[0].t == ir.Opcode.vl_str)
                    {
                        if (I.p[0].v == Generic.g_precall)
                        {
                            to_push = I.mreg_live_in.Clone();
                            to_push.Intersect(caller_preserves);
                            mcn.insts.RemoveAt(i);
                            foreach(var r in to_push)
                            {
                                var reg = t.regs[r];
                                mcn.insts.Insert(i++, t.SaveRegister(reg));
                            }
                            i--;
                        }
                        else if (I.p[0].v == Generic.g_postcall)
                        {
                            mcn.insts.RemoveAt(i);
                            int count = 0;
                            foreach (var r in to_push)
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
