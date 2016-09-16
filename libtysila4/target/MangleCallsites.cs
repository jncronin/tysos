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
    public class MangleCallsites
    {
        public static graph.Graph MangleCallsitesPass(graph.Graph g, Target t)
        {
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;

                foreach(var I in mcn.all_insts)
                {
                    foreach(var p in I.p)
                    {
                        if(p.t == ir.Opcode.vl_call_target)
                        {
                            p.t = ir.Opcode.vl_str;
                            if(p.str == null)
                                p.str = p.m.MangleMethod(p.ms);
                        }
                    }
                }
            }

            return g;
        }
    }
}
