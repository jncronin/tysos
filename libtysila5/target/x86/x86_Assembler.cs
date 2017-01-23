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
using libtysila5.ir;
using metadata;

namespace libtysila5.target.x86
{
    partial class x86_Assembler : Target
    {
        static x86_Assembler()
        {
            init_instrs();
        }

        protected internal override Reg GetMoveDest(MCInst i)
        {
            return i.p[1].mreg;
        }

        protected internal override Reg GetMoveSrc(MCInst i)
        {
            return i.p[2].mreg;
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


        protected internal override void SetBranchDest(MCInst i, int d)
        {
            if (!IsBranch(i))
                throw new NotSupportedException();
            if (i.p[0].v == x86_jcc_rel32)
                i.p[2] = new Param { t = Opcode.vl_br_target, v = d };
            else
                i.p[1] = new Param { t = Opcode.vl_br_target, v = d };
        }

        protected internal override int GetBranchDest(MCInst i)
        {
            if (!IsBranch(i))
                throw new NotSupportedException();
            if (i.p[0].v == x86_jcc_rel32)
                return (int)i.p[2].v;
            else
                return (int)i.p[1].v;
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

        protected internal override IRelocationType GetDataToDataReloc()
        {
            return new binary_library.elf.ElfFile.Rel_386_32();
        }
    }
}
