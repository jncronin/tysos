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
using libtysila4.ir;

namespace libtysila4.target.x86
{
    partial class x86_Assembler
    {
        internal static graph.Graph SimplifyImmediates(graph.Graph g, Target t)
        {
            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;

                foreach(var I in mcn.all_insts)
                {
                    if (I.p.Length == 0)
                        continue;

                    switch(I.p[0].v)
                    {
                        case x86_add_rm32_imm32:
                        case x86_sub_r32_rm32:
                        case x86_sub_rm32_imm32:
                        case x86_cmp_rm32_imm32:
                            {
                                var reg = I.p[1];

                                Param imm;
                                if (I.p[0].v == x86_cmp_rm32_imm32)
                                    imm = I.p[2];
                                else
                                    imm = I.p[3];

                                int size = 4;
                                if (imm.v >= -128 && imm.v <= 127)
                                    size = 1;
                                else if (imm.v >= -32768 && imm.v <= 32767)
                                    size = 2;

                                if(size == 1)
                                {
                                    switch(I.p[0].v)
                                    {
                                        case x86_add_rm32_imm32:
                                            I.p[0].v = x86_add_rm32_imm8;
                                            break;
                                        case x86_sub_r32_rm32:
                                        case x86_sub_rm32_imm32:
                                            I.p[0].v = x86_sub_rm32_imm8;
                                            break;
                                        case x86_cmp_rm32_imm32:
                                            I.p[0].v = x86_cmp_rm32_imm8;
                                            break;
                                    }
                                }
                            }
                            break;

                        case x86_mov_rm32_imm32:
                            if(I.p[1].IsMreg && I.p[1].mreg.type == 0)
                            {
                                I.p = new Param[]
                                {
                                    new Param { t = Opcode.vl_str, str = "xor_r32_rm32", v = x86_xor_r32_rm32 },
                                    I.p[1],
                                    I.p[1]
                                };
                            }
                            break;
                    }
                }
            }

            return g;
        }
    }
}
