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
using libtysila5.util;
using metadata;

namespace libtysila5.target.x86
{
    partial class x86_Assembler : Target
    {
        static x86_Assembler()
        {
            init_instrs();
        }

        public override void InitIntcalls()
        {
            ConvertToIR.intcalls["_ZN14libsupcs#2Edll8libsupcs12IoOperations_7PortOut_Rv_P2th"] = portout_byte;
            ConvertToIR.intcalls["_ZN14libsupcs#2Edll8libsupcs12IoOperations_7PortOut_Rv_P2tt"] = portout_word;
            ConvertToIR.intcalls["_ZN14libsupcs#2Edll8libsupcs12IoOperations_7PortOut_Rv_P2tj"] = portout_dword;
            ConvertToIR.intcalls["_ZN14libsupcs#2Edll8libsupcs12IoOperations_7PortInb_Rh_P1t"] = portin_byte;
            ConvertToIR.intcalls["_ZN14libsupcs#2Edll8libsupcs12IoOperations_7PortInw_Rt_P1t"] = portin_word;
            ConvertToIR.intcalls["_ZN14libsupcs#2Edll8libsupcs12IoOperations_7PortInd_Rj_P1t"] = portin_dword;
        }

        private static util.Stack<StackItem> portin_byte(cil.CilNode n, Code c, util.Stack<StackItem> stack_before)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Push(new StackItem { ts = c.ms.m.SystemByte, min_ul = 0, max_ul = 255 });

            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = ir.Opcode.oc_x86_portin, vt_size = 1, stack_before = stack_before, stack_after = stack_after, arg_a = 0, res_a = 0 });

            return stack_after;
        }

        private static util.Stack<StackItem> portin_word(cil.CilNode n, Code c, util.Stack<StackItem> stack_before)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Push(new StackItem { ts = c.ms.m.SystemUInt16, min_ul = 0, max_ul = ushort.MaxValue });

            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = ir.Opcode.oc_x86_portin, vt_size = 2, stack_before = stack_before, stack_after = stack_after, arg_a = 0, res_a = 0 });

            return stack_after;
        }

        private static util.Stack<StackItem> portin_dword(cil.CilNode n, Code c, util.Stack<StackItem> stack_before)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Push(new StackItem { ts = c.ms.m.SystemUInt32, min_ul = 0, max_ul = uint.MaxValue });

            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = ir.Opcode.oc_x86_portin, vt_size = 4, stack_before = stack_before, stack_after = stack_after, arg_a = 0, res_a = 0 });

            return stack_after;
        }

        private static util.Stack<StackItem> portout_byte(cil.CilNode n, Code c, util.Stack<StackItem> stack_before)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Pop();

            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = ir.Opcode.oc_x86_portout, vt_size = 1, stack_before = stack_before, stack_after = stack_after, arg_a = 1, arg_b = 0 });

            return stack_after;
        }

        private static util.Stack<StackItem> portout_word(cil.CilNode n, Code c, util.Stack<StackItem> stack_before)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Pop();

            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = ir.Opcode.oc_x86_portout, vt_size = 2, stack_before = stack_before, stack_after = stack_after, arg_a = 1, arg_b = 0 });

            return stack_after;
        }

        private static util.Stack<StackItem> portout_dword(cil.CilNode n, Code c, util.Stack<StackItem> stack_before)
        {
            var stack_after = new util.Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Pop();

            n.irnodes.Add(new cil.CilNode.IRNode { parent = n, opcode = ir.Opcode.oc_x86_portout, vt_size = 4, stack_before = stack_before, stack_after = stack_after, arg_a = 1, arg_b = 0 });

            return stack_after;
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
        protected internal override Reg GetLVLocation(int lv_loc, int lv_size, Code c)
        {
            if (Opcode.GetCTFromType(c.ret_ts) == Opcode.ct_vt)
                lv_loc += 4;

            int disp = 0;
            disp = -lv_size - lv_loc;
            return new ContentsReg
            {
                basereg = r_ebp,
                disp = disp,
                size = lv_size
            };
        }

        /* Access to variables on the incoming stack is encoded as -address - 1 */
        protected internal override Reg GetLALocation(int la_loc, int la_size, Code c)
        {
            if (Opcode.GetCTFromType(c.ret_ts) == Opcode.ct_vt)
                la_loc += 4;

            return new ContentsReg
            {
                basereg = r_ebp,
                disp = la_loc + 8,
                size = la_size
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

        protected internal override IRelocationType GetDataToCodeReloc()
        {
            return new binary_library.elf.ElfFile.Rel_386_32();
        }

        public override Reg AllocateStackLocation(Code c, int size, ref int cur_stack)
        {
            size = util.util.align(size, psize);
            cur_stack -= size;

            return new ContentsReg { basereg = r_ebp, disp = cur_stack, size = size };
        }
    }
}

namespace libtysila5.target.x86_64
{
    partial class x86_64_Assembler : x86.x86_Assembler
    {
        public override int GetCCClassFromCT(int ct, int size, TypeSpec ts, string cc)
        {
            if (cc == "sysv")
            {
                switch (ct)
                {
                    case Opcode.ct_vt:
                        // breaks spec - need to only use MEMORY for those more than 32 bytes
                        //  but we don't wupport splitting arguments up yet
                        return sysvc_MEMORY;
                }
            }

            return base.GetCCClassFromCT(ct, size, ts, cc);
        }
    }
}
