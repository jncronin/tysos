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
using libtysila4.ir;

namespace libtysila4.target.x86
{
    partial class x86_Assembler
    {
        void LowerConv(Opcode irnode, ref int next_temp_reg)
        {
            irnode.is_mc = true;
            irnode.mcinsts = new List<MCInst>();

            /* Parameters are:
                0: input
                1: dest size
                2: overflow flag
                3: unsigned
            */

            var input = irnode.uses[0];
            var destsize = irnode.uses[1].v;
            var ovf = irnode.uses[2].v;
            var un = irnode.uses[3].v;

            int oc = 0;
            int oc2 = 0;
            int ttype = 0;
            if (un == 1)
                throw new NotImplementedException();
            if (ovf == 1)
                throw new NotImplementedException();

            switch(input.ct)
            {
                case Opcode.ct_int32:
                    switch(destsize)
                    {
                        case 1:
                            oc = x86_movsxbd;
                            break;
                        case 2:
                            oc = x86_movsxwd;
                            break;
                        case 4:
                            oc = x86_mov_r32_rm32;
                            break;
                        case 8:
                            throw new NotImplementedException();
                        case -1:
                            oc = x86_movzxbd;
                            break;
                        case -2:
                            oc = x86_movzxwd;
                            break;
                        case -4:
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    oc2 = x86_mov_rm32_r32;
                    ttype = Opcode.ct_int32;
                    break;
                default:
                    throw new NotImplementedException();                   
            }

            var treg = next_temp_reg++;

            /* We do a conversion move to temporary register,
            then back to destination so we handle the case
            where both src and dest are memory locations.
            Register allocator should coalesce unnecessary moves */
            var treg_def = new Param { t = Opcode.vl_stack, ct = ttype, v = treg, ud = Param.UseDefType.Def };
            var treg_use = new Param { t = Opcode.vl_stack, ct = ttype, v = treg, ud = Param.UseDefType.Use };
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, v = oc, str = "conv_oc1" },
                    treg_def,
                    input,
                }
            });
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, v = oc2, str = "conv_oc2" },
                    irnode.defs[0],
                    treg_use,
                }
            });
            var new_defs = new List<Param>(irnode.defs);
            var new_uses = new List<Param>(irnode.uses);
            new_defs.Add(treg_def);
            new_uses.Add(treg_use);
            irnode.defs = new_defs.ToArray();
            irnode.uses = new_uses.ToArray();
        }
    }
}
