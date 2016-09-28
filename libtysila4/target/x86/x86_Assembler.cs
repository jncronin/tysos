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
using binary_library;
using libtysila4.graph;
using libtysila4.ir;
using metadata;

namespace libtysila4.target.x86
{
    partial class x86_Assembler : Target
    {
        static x86_Assembler()
        {
            init_instrs();
        }

        protected override void MCLower(Opcode irnode, ref int next_temp_reg)
        {
            switch(irnode.oc)
            {
                case Opcode.oc_call:
                case Opcode.oc_callvirt:
                    LowerCall(irnode, ref next_temp_reg);
                    return;
                case Opcode.oc_endfinally:
                    LowerEndfinally(irnode);
                    return;
                case Opcode.oc_ret:
                    LowerReturn(irnode);
                    return;
                case Opcode.oc_enter:
                    LowerEnter(irnode, ref next_temp_reg);
                    return;
                case Opcode.oc_conv:
                    LowerConv(irnode, ref next_temp_reg);
                    return;
                case Opcode.oc_stind:
                    LowerStind(irnode, ref next_temp_reg);
                    return;
                case Opcode.oc_ldlabcontents:
                    LowerLdLabContents(irnode, ref next_temp_reg);
                    return;
                case Opcode.oc_stlabcontents:
                    LowerStLabContents(irnode, ref next_temp_reg);
                    return;
                case Opcode.oc_zeromem:
                    LowerZeromem(irnode, ref next_temp_reg);
                    return;
            }
            base.MCLower(irnode, ref next_temp_reg);
        }

        private void LowerEnter(Opcode irnode, ref int next_temp_reg)
        {
            irnode.mcinsts = new List<MCInst>();
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "push", v = x86_push_r32 },
                    new Param { t = Opcode.vl_mreg, mreg = r_ebp }
                }
            });
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "mov", v = x86_mov_r32_rm32 },
                    new Param { t = Opcode.vl_mreg, mreg = r_ebp },
                    new Param { t = Opcode.vl_mreg, mreg = r_esp }
                }
            });

            base.MCLower(irnode, ref next_temp_reg);
        }

        protected internal override bool IsMoveVreg(MCInst i)
        {
            if (i.p != null && i.p.Length > 0 &&
                i.p[0].t == Opcode.vl_str &&
                (i.p[0].v == x86_mov_rm32_r32 ||
                i.p[0].v == x86_mov_r32_rm32) &&
                i.p[1].IsStack && i.p[2].IsStack)
                return true;
            return false;
        }

        protected internal override bool IsMoveMreg(MCInst i)
        {
            if (i.p != null && i.p.Length > 0 &&
                i.p[0].t == Opcode.vl_str &&
                (i.p[0].v == x86_mov_rm32_r32 ||
                i.p[0].v == x86_mov_r32_rm32) &&
                i.p[1].t == Opcode.vl_mreg &&
                i.p[2].t == Opcode.vl_mreg)
                return true;
            return false;
        }

        protected internal override Param GetMoveDest(MCInst i)
        {
            if (!IsMoveVreg(i) && !IsMoveMreg(i))
                throw new NotSupportedException();
            return i.p[1];
        }

        protected internal override Param GetMoveSrc(MCInst i)
        {
            if (!IsMoveVreg(i) && !IsMoveMreg(i))
                throw new NotSupportedException();
            return i.p[2];
        }

        protected internal override int GetCondCode(MCInst i)
        {
            if (i.p == null || i.p.Length < 2 ||
                i.p[1].t != Opcode.vl_cc)
                return Opcode.cc_always;
            return (int)i.p[1].v;
        }

        protected internal override bool IsBranch(MCInst i)
        {
            if (i.p != null && i.p.Length > 0 &&
                i.p[0].t == Opcode.vl_str &&
                (i.p[0].v == x86_jmp_rel32 ||
                i.p[0].v == x86_jcc_rel32))
                return true;
            return false;
        }

        protected internal override bool IsCall(MCInst i)
        {
            if (i.p != null && i.p.Length > 0 &&
                i.p[0].t == Opcode.vl_str &&
                (i.p[0].v == x86_call_rel32))
                return true;
            return false;
        }


        protected internal override void SetBranchDest(MCInst i, Param d)
        {
            if (!IsBranch(i))
                throw new NotSupportedException();
            if (i.p[0].v == x86_jcc_rel32)
                i.p[2] = d;
            else
                i.p[1] = d;
        }

        protected internal override Param GetBranchDest(MCInst i)
        {
            if (!IsBranch(i))
                throw new NotSupportedException();
            if (i.p[0].v == x86_jcc_rel32)
                return i.p[2];
            else
                return i.p[1];
        }

        protected internal override MCInst SaveRegister(Reg r)
        {
            MCInst ret = new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "push", v = x86_push_r32 },
                    new Param { t = Opcode.vl_mreg, mreg = r }
                }
            };
            return ret;
        }

        protected internal override MCInst RestoreRegister(Reg r)
        {
            MCInst ret = new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "pop", v = x86_pop_r32 },
                    new Param { t = Opcode.vl_mreg, mreg = r }
                }
            };
            return ret;
        }

        /* Access to variables on the incoming stack is encoded as -address - 1 */
        protected internal override Reg GetLVLocation(int lv_loc, int lv_size)
        {
            int disp = 0;
            if(lv_loc < 0)
                disp = -lv_loc - 1 + 8;
            else
                disp = -lv_size + lv_loc;
            return new ContentsReg
            {
                basereg = r_ebp,
                disp = disp
            };
        }

        protected internal override MCInst[] CreateMove(Reg src, Reg dest)
        {
            throw new NotImplementedException();
        }

        protected internal override MCInst[] CreateMove(Param src, Param dest)
        {
            var ct = dest.ct;

            switch(ct)
            {
                case Opcode.ct_object:
                case Opcode.ct_ref:
                case Opcode.ct_int32:
                case Opcode.ct_intptr:
                    return new MCInst[]
                    {
                        new MCInst
                        {
                            p = new Param[]
                            {
                                new Param { t = Opcode.vl_str, v = x86_mov_r32_rm32, str = "mov_r32_rm32" },
                                dest,
                                src
                            }
                        }
                    };

                default:
                    throw new NotImplementedException();
            }
        }

        protected internal override MCInst[] SetupStack(int lv_size)
        {
            if (lv_size == 0)
                return new MCInst[0];
            else
                return new MCInst[]
                {
                    new MCInst { p = new Param[]
                    {
                        new Param { t = Opcode.vl_str, str = "sub", v = x86_sub_rm32_imm32 },
                        new Param { t = Opcode.vl_mreg, mreg = r_esp },
                        new Param { t = Opcode.vl_mreg, mreg = r_esp },
                        new Param { t = Opcode.vl_c, v = lv_size }
                    } }
                };
        }

        public override IEnumerable<Graph.PassDelegate> GetOutputMCPasses()
        {
            return new Graph.PassDelegate[]
            {
                SimplifyImmediates,
                Assemble.AssemblePass,
            };
        }

        protected internal override IRelocationType GetDataToDataReloc()
        {
            return new binary_library.elf.ElfFile.Rel_386_32();
        }
    }
}
