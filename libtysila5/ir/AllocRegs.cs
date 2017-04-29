/* Copyright (C) 2017 by John Cronin
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
using System.Text;
using libtysila5.cil;
using metadata;
using libtysila5.util;

namespace libtysila5.ir
{
    public class AllocRegs
    {
        public static void DoAllocation(Code c)
        {
            foreach(var n in c.ir)
            {
                DoAllocation(c, n.stack_before, c.t);
                DoAllocation(c, n.stack_after, c.t);
            }
        }

        static target.Target.Reg alloc_x86_reg(Code c, target.Target.Reg[] regs, ref int cur_reg, ref int cur_stack,
            int rsize = 4)
        {
            var x = c.t as target.x86.x86_Assembler;

            if (cur_reg < regs.Length)
            {
                var ret = regs[cur_reg++];
                c.regs_used |= ret.mask;
                return ret;
            }
            else
            {
                cur_stack -= rsize;
                return new target.Target.ContentsReg { basereg = target.x86.x86_Assembler.r_ebp, disp = cur_stack, size = 4 };
            }
        }

        private static void DoAllocation(Code c, Stack<StackItem> stack, target.Target t)
        {
            long alloced = 0;

            foreach(var si in stack)
            {
                long avail = t.ct_regs[si.ct] & ~alloced;

                if (avail != 0)
                {
                    // We have a valid allocation to use
                    int idx = 0;
                    while ((avail & 0x1) == 0)
                    {
                        idx++;
                        avail >>= 1;
                    }
                    var reg = t.regs[idx];
                    alloced |= (1L << idx);
                    si.reg = reg;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        private static void DoAllocation(Code c, Stack<StackItem> stack)
        {
            // simple algorithm for x86 for now

            var x = c.t as target.x86.x86_Assembler;

            target.Target.Reg[] r32 = new target.Target.Reg[]
            {
                target.x86.x86_Assembler.r_esi,
                target.x86.x86_Assembler.r_edi,
                target.x86.x86_Assembler.r_ecx,
                target.x86.x86_Assembler.r_ebx
            };
            target.Target.Reg[] rf = new target.Target.Reg[]
            {
                target.x86.x86_Assembler.r_xmm0,
                target.x86.x86_Assembler.r_xmm1,
                target.x86.x86_Assembler.r_xmm2,
                target.x86.x86_Assembler.r_xmm3,
                target.x86.x86_Assembler.r_xmm4,
                target.x86.x86_Assembler.r_xmm5,
                target.x86.x86_Assembler.r_xmm6
            };
            int cur_reg = 0;
            int cur_rf = 0;
            int cur_stack = 0;

            foreach(var si in stack)
            {
                switch(si.ct)
                {
                    case Opcode.ct_int32:
                    case Opcode.ct_intptr:
                    case Opcode.ct_object:
                    case Opcode.ct_ref:
                        si.reg = alloc_x86_reg(c, r32, ref cur_reg, ref cur_stack);
                        break;
                    case Opcode.ct_float:
                        si.reg = alloc_x86_reg(c, rf, ref cur_rf, ref cur_stack, 8);
                        break;
                    case Opcode.ct_int64:
                        si.reg = new target.Target.DoubleReg(
                            alloc_x86_reg(c, r32, ref cur_reg, ref cur_stack),
                            alloc_x86_reg(c, r32, ref cur_reg, ref cur_stack));
                        break;
                    case Opcode.ct_vt:
                        {
                            var vt_size = c.t.GetSize(si.ts);
                            if (vt_size <= 4 && si.has_address_taken == false)
                                si.reg = alloc_x86_reg(c, r32, ref cur_reg, ref cur_stack);
                            else if (vt_size <= 8 && si.has_address_taken == false)
                                si.reg = new target.Target.DoubleReg(
                                    alloc_x86_reg(c, r32, ref cur_reg, ref cur_stack),
                                    alloc_x86_reg(c, r32, ref cur_reg, ref cur_stack));
                            else
                            {
                                vt_size = util.util.align(vt_size, 4);
                                cur_stack -= vt_size;
                                si.reg = new target.Target.ContentsReg { basereg = target.x86.x86_Assembler.r_ebp, disp = cur_stack, size = vt_size };
                            }
                            break;
                        }

                    default:
                        throw new NotImplementedException(ir.Opcode.ct_names[si.ct]);
                }
            }
        }
    }
}
