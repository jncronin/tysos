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
using System.Collections.Generic;
using System.Text;
using libtysila5.cil;

namespace libtysila5.target.x86
{
    partial class x86_Assembler
    {
        public override List<MCInst> ChooseInstruction(List<CilNode.IRNode> nodes, int start, int count, Code c)
        {
            if (count == 1)
            {
                var n = nodes[start];
                switch (n.opcode)
                {
                    case ir.Opcode.oc_ldc:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr)
                            return new List<MCInst> { inst(x86_mov_rm32_imm32, n.stack_after.Peek().reg, (int)n.imm_l, n) };
                        break;

                    case ir.Opcode.oc_stloc:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr)
                        {
                            var src = n.stack_before.Peek().reg;

                            var r = new List<MCInst>();
                            if (src is ContentsReg)
                            {
                                // first store to rax
                                r.Add(inst(x86_mov_r32_rm32, r_eax, src, n));
                                src = r_eax;
                            }
                            r.Add(inst(x86_mov_rm32_r32, GetLVLocation((int)n.imm_l, 4), src, n));
                            return r;
                        }
                        break;

                    case ir.Opcode.oc_ldloc:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr)
                        {
                            var dest = n.stack_after.Peek().reg;

                            var r = new List<MCInst>();
                            if (dest is ContentsReg)
                            {
                                // first load to rax
                                r.Add(inst(x86_mov_r32_rm32, r_eax, GetLVLocation((int)n.imm_l, 4), n));
                                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                            }
                            else
                            {
                                r.Add(inst(x86_mov_r32_rm32, dest, GetLVLocation((int)n.imm_l, 4), n));
                            }
                            return r;
                        }
                        break;

                    case ir.Opcode.oc_add:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr)
                        {
                            var srca = n.stack_before.Peek(1).reg;
                            var srcb = n.stack_before.Peek().reg;
                            var dest = n.stack_after.Peek().reg;

                            // try and optimize
                            if (!(srca is ContentsReg) && srca.Equals(dest))
                            {
                                return new List<MCInst> { inst(x86_add_r32_rm32, srca, srcb, n) };
                            }
                            else if (!(srcb is ContentsReg) && srca.Equals(dest))
                            {
                                return new List<MCInst> { inst(x86_add_rm32_r32, srca, srcb, n) };
                            }
                            else
                            {
                                // complex way, do calc in rax, then store
                                List<MCInst> r = new List<MCInst>();
                                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                                r.Add(inst(x86_add_r32_rm32, r_eax, srcb, n));
                                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                                return r;
                            }
                        }
                        break;

                    case ir.Opcode.oc_cmp:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr)
                        {
                            var srca = n.stack_before.Peek(1).reg;
                            var srcb = n.stack_before.Peek().reg;
                            var dest = n.stack_after.Peek().reg;

                            List<MCInst> r = new List<MCInst>();
                            if (!(srca is ContentsReg))
                                r.Add(inst(x86_cmp_r32_rm32, srca, srcb, n));
                            else if (!(srcb is ContentsReg))
                                r.Add(inst(x86_cmp_rm32_r32, srca, srcb, n));
                            else
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                                r.Add(inst(x86_cmp_r32_rm32, r_eax, srcb, n));
                            }
                            r.Add(inst(x86_set_rm32, dest, new ir.Param { t = ir.Opcode.vl_cc, v = (int)n.imm_ul }, n));

                            return r;
                        }
                        break;

                    case ir.Opcode.oc_brif:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr)
                        {
                            var srca = n.stack_before.Peek(1).reg;
                            var srcb = n.stack_before.Peek().reg;

                            List<MCInst> r = new List<MCInst>();
                            if (!(srca is ContentsReg))
                                r.Add(inst(x86_cmp_r32_rm32, srca, srcb, n));
                            else if (!(srcb is ContentsReg))
                                r.Add(inst(x86_cmp_rm32_r32, srca, srcb, n));
                            else
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, srca, n));
                                r.Add(inst(x86_cmp_r32_rm32, r_eax, srcb, n));
                            }
                            r.Add(inst_jmp(x86_jcc_rel32, (int)n.imm_l, (int)n.imm_ul, n));

                            return r;
                        }
                        break;


                    case ir.Opcode.oc_conv:
                        if (n.ct == ir.Opcode.ct_int32)
                        {
                            var si = n.stack_after.Peek();
                            var stype = si.ts.SimpleType;

                            switch (stype)
                            {
                                case 0x08:
                                case 0x09:
                                case 0x18:
                                case 0x19:
                                    // nop
                                    return new List<MCInst>();
                            }
                        }
                        break;

                    case ir.Opcode.oc_stind:
                        if (n.ct == ir.Opcode.ct_int32)
                        {
                            var addr = n.stack_before.Peek(1).reg;
                            var val = n.stack_before.Peek().reg;

                            List<MCInst> r = new List<MCInst>();
                            if (addr is ContentsReg)
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_eax, addr, n));
                                addr = r_eax;
                            }
                            if (val is ContentsReg)
                            {
                                r.Add(inst(x86_mov_r32_rm32, r_edx, val, n));
                                val = r_edx;
                            }

                            switch (n.vt_size)
                            {
                                case 1:
                                    r.Add(inst(x86_mov_rm8disp_r32, addr, val, n));
                                    break;
                                case 2:
                                    r.Add(inst(x86_mov_rm16disp_r32, addr, val, n));
                                    break;
                                case 4:
                                    r.Add(inst(x86_mov_rm32disp_r32, addr, val, n));
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            return r;
                        }
                        break;

                    case ir.Opcode.oc_br:
                        return new List<MCInst> { inst_jmp(x86_jmp_rel32, (int)n.imm_l, n) };

                    case ir.Opcode.oc_ldlabaddr:
                        if (n.ct == ir.Opcode.ct_int32 || n.ct == ir.Opcode.ct_intptr || n.ct == ir.Opcode.ct_object)
                        {
                            var dest = n.stack_after.Peek().reg;

                            List<MCInst> r = new List<MCInst>();
                            if (dest is ContentsReg)
                            {
                                r.Add(inst(x86_lea_r32, r_eax, new ir.Param { t = ir.Opcode.vl_str, str = n.imm_lab, v = n.imm_l }, n));
                                r.Add(inst(x86_mov_rm32_r32, dest, r_eax, n));
                            }
                            else
                                r.Add(inst(x86_lea_r32, dest, new ir.Param { t = ir.Opcode.vl_str, str = n.imm_lab, v = n.imm_l }, n));
                            return r;
                        }
                        break;

                    case ir.Opcode.oc_call:
                        {
                            var r = handle_call(n, c);
                            if (r != null)
                                return r;
                        }
                        return null;
                }
            }
            else if (count == 2)
            {
                var n1 = nodes[start];
                var n2 = nodes[start + 1];

                if (n1.opcode == ir.Opcode.oc_ldc &&
                    n2.opcode == ir.Opcode.oc_stloc)
                {
                    if (n1.ct == ir.Opcode.ct_int32)
                        return new List<MCInst> { inst(x86_mov_rm32_imm32, GetLVLocation((int)n2.imm_l, 4), (int)n1.imm_l, n1) };
                    throw new NotImplementedException();
                }
            }
            return null;
        }

        private List<MCInst> handle_call(CilNode.IRNode n, Code c)
        {
            List<MCInst> r = new List<MCInst>();
            var call_ms = n.imm_ms;

            /* Determine which registers we need to save */
            var caller_preserves = c.t.cc_caller_preserves_map["sysv"];
            ulong defined = 0;
            foreach (var si in n.stack_after)
                defined |= si.reg.mask;
            var to_push = new util.Set();
            to_push.Union(defined);
            to_push.Intersect(caller_preserves);
            List<Reg> push_list = new List<Reg>();
            while (!to_push.Empty)
            {
                var first_set = to_push.get_first_set();
                push_list.Add(c.t.regs[first_set]);
                to_push.unset(first_set);
            }

            foreach (var push_reg in push_list)
                r.Add(inst(x86_push_r32, push_reg, n));

            /* Push arguments */
            var sig_idx = call_ms.msig;
            var pcount = call_ms.m.GetMethodDefSigParamCountIncludeThis(sig_idx);

            int push_length = 0;

            for(int i = 0; i < pcount; i++)
            {
                var to_pass = n.stack_before.Peek(i).reg;

                if (to_pass is ContentsReg)
                {
                    ContentsReg cr = to_pass as ContentsReg;
                    if (cr.size != 4)
                        throw new NotImplementedException();
                    r.Add(inst(x86_push_rm32, to_pass, n));
                    push_length += 4;
                }
                else
                {
                    r.Add(inst(x86_push_r32, to_pass, n));
                    push_length += 4;
                }
            }

            // Do the call
            var target = call_ms.m.MangleMethod(call_ms);
            r.Add(inst(x86_call_rel32, new ir.Param { t = ir.Opcode.vl_call_target, str = target }, n));

            // Restore stack
            var add_oc = x86_add_rm32_imm32;
            if (push_length < 128)
                add_oc = x86_add_rm32_imm8;
            r.Add(inst(add_oc, r_esp, push_length, n));

            // Restore saved registers
            for (int i = push_list.Count - 1; i >= 0; i--)
                r.Add(inst(x86_pop_r32, push_list[i], n));

            return r;
        }

        private MCInst inst_jmp(int idx, int jmp_target, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_br_target, v = jmp_target },
                },
                parent = p
            };
        }

        private MCInst inst_jmp(int idx, int jmp_target, int cc, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_br_target, v = jmp_target },
                    new ir.Param { t = ir.Opcode.vl_cc, v = cc }
                },
                parent = p
            };
        }

        MCInst inst(int idx, Reg v1, int v2, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_mreg, mreg = v1 },
                    new ir.Param { t = ir.Opcode.vl_c32, v = v2 }
                },
                parent = p
            };
        }

        MCInst inst(int idx, Reg v1, ir.Param v2, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_mreg, mreg = v1 },
                    v2
                },
                parent = p
            };
        }

        MCInst inst(int idx, int v1, Reg v2, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_c32, v = v1 },
                    new ir.Param { t = ir.Opcode.vl_mreg, mreg = v2 }
                },
                parent = p
            };
        }


        MCInst inst(int idx, Reg v1, Reg v2, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    new ir.Param { t = ir.Opcode.vl_mreg, mreg = v1 },
                    new ir.Param { t = ir.Opcode.vl_mreg, mreg = v2 }
                },
                parent = p
            };
        }

        MCInst inst(int idx, ir.Param v1, CilNode.IRNode p)
        {
            return new MCInst
            {
                p = new ir.Param[]
                {
                    new ir.Param { t = ir.Opcode.vl_str, v = idx, str = insts[idx] },
                    v1
                },
                parent = p
            };
        }
    }
}
