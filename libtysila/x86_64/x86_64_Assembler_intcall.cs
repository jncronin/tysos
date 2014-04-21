/* Copyright (C) 2012 by John Cronin
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
using libasm;

namespace libtysila
{
    partial class x86_64_Assembler
    {
        protected override bool Arch_enc_intcall(string mangled_name, InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state, bool provides, ref bool i_pushes_set)
        {
            if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_RSP_Rv_P1y")
            {
                // set_RSP(ulong v)
                i.tacs.Add(new MiscEx("set_RSP", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_, CliType.int64, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_Cr0_Ry_P0")
            {
                // ulong get_Cr0()
                i.tacs.Add(new MiscEx("get_Cr0", state.next_variable++, var.Null, var.Null, CliType.int64, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_Cr0_Rv_P1y")
            {
                // set_Cr0(ulong v)
                i.tacs.Add(new MiscEx("set_Cr0", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_, CliType.int64, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_Cr2_Ry_P0")
            {
                // ulong get_Cr2()
                i.tacs.Add(new MiscEx("get_Cr2", state.next_variable++, var.Null, var.Null, CliType.int64, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_Cr2_Rv_P1y")
            {
                // set_Cr2(ulong v)
                i.tacs.Add(new MiscEx("set_Cr2", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_, CliType.int64, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_Cr3_Ry_P0")
            {
                // ulong get_Cr3()
                i.tacs.Add(new MiscEx("get_Cr3", state.next_variable++, var.Null, var.Null, CliType.int64, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_Cr3_Rv_P1y")
            {
                // set_Cr3()
                i.tacs.Add(new MiscEx("set_Cr3", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_, CliType.int64, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_Cr4_Ry_P0")
            {
                // ulong get_Cr4()
                i.tacs.Add(new MiscEx("get_Cr4", state.next_variable++, var.Null, var.Null, CliType.int64, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_Cr4_Rv_P1y")
            {
                // set_Cr4()
                i.tacs.Add(new MiscEx("set_Cr4", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_, CliType.int64, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_4Lidt_Rv_P1u1U")
            {
                // Lidt(UIntPtr idt_ptr)
                i.tacs.Add(new MiscEx("Lidt", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_,
                    CliType.native_int, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_3Ltr_Rv_P1y")
            {
                // Ltr(ulong selector)
                return false;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_9set_Mxcsr_Rv_P1j")
            {
                // set_Mxcsr(uint v)
                return false;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_9get_Mxcsr_Rj_P0")
            {
                // uint get_Mxcsr()
                return false;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_6_Cpuid_Rv_P2jPj")
            {
                // ulong _Cpuid(uint req_no, uint *buf)
                return false;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_RBP_Ry_P0")
            {
                // ulong get_RBP()
                i.tacs.Add(new MiscEx("get_RBP", state.next_variable++, var.Null, var.Null, CliType.int64, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_RSP_Ry_P0")
            {
                // ulong get_RSP()
                i.tacs.Add(new MiscEx("get_RSP", state.next_variable++, var.Null, var.Null, CliType.int64, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_5RdMsr_Ry_P1j")
            {
                // ulong RdMsr(uint reg_no)
                return false;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7get_Tsc_Ry_P0")
            {
                // ulong get_Tsc()
                return false;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_3Sti_Rv_P0")
            {
                // Sti()
                i.tacs.Add(new MiscEx("Sti", var.Null, var.Null, var.Null, CliType.void_, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_3Cli_Rv_P0")
            {
                // Cli()
                i.tacs.Add(new MiscEx("Cli", var.Null, var.Null, var.Null, CliType.void_, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_RBP_Rv_P1y")
            {
                // set_RBP(ulong v)
                i.tacs.Add(new MiscEx("set_RBP", var.Null, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.void_, CliType.int64, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_5Break_Rv_P0")
            {
                // Break()
                i.tacs.Add(new MiscEx("Break", var.Null, var.Null, var.Null, CliType.void_, CliType.void_, CliType.void_, ThreeAddressCode.OpType.OtherOp));
                return true;
            }
            else if (mangled_name == "_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_6Invlpg_Rv_P1y")
            {
                // Invlpg(ulong vaddr)
                return false;
            }
            else if (mangled_name == "_ZW6System4MathM_0_5Round_Rd_P1d")
            {
                // double Round(double)
                i.tacs.Add(new MiscEx("round_double", state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null, CliType.F64, CliType.F64, CliType.void_, ThreeAddressCode.OpType.ConvOp));
                return true;
            }
            else
                return false;
        }

        internal IEnumerable<OutputBlock> x86_64_set_RSP(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            x86_64_assign(Rsp, op1.hardware_loc, ret);
            return ret;
        }

        internal IEnumerable<OutputBlock> x86_64_set_RBP(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            x86_64_assign(Rbp, op1.hardware_loc, ret);
            return ret;
        }

        internal IEnumerable<OutputBlock> x86_64_get_RSP(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            x86_64_assign(result.hardware_loc, Rsp, ret);
            return ret;
        }

        internal IEnumerable<OutputBlock> x86_64_get_RBP(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            x86_64_assign(result.hardware_loc, Rbp, ret);
            return ret;
        }

        internal IEnumerable<OutputBlock> x86_64_get_Cr0(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(0, result.hardware_loc, 3, false, 0, 0x0f, 0x20));
        }

        internal IEnumerable<OutputBlock> x86_64_set_Cr0(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(0, op1.hardware_loc, 3, false, 0, 0x0f, 0x22));
        }

        internal IEnumerable<OutputBlock> x86_64_get_Cr2(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(2, result.hardware_loc, 3, false, 0, 0x0f, 0x20));
        }

        internal IEnumerable<OutputBlock> x86_64_set_Cr2(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(2, op1.hardware_loc, 3, false, 0, 0x0f, 0x22));
        }

        internal IEnumerable<OutputBlock> x86_64_get_Cr3(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(3, result.hardware_loc, 3, false, 0, 0x0f, 0x20));
        }

        internal IEnumerable<OutputBlock> x86_64_set_Cr3(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(3, op1.hardware_loc, 3, false, 0, 0x0f, 0x22));
        }

        internal IEnumerable<OutputBlock> x86_64_get_Cr4(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(4, result.hardware_loc, 3, false, 0, 0x0f, 0x20));
        }

        internal IEnumerable<OutputBlock> x86_64_set_Cr4(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(4, op1.hardware_loc, 3, false, 0, 0x0f, 0x22));
        }

        internal IEnumerable<OutputBlock> x86_64_Sti(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new byte[] { 0xfb });
        }

        internal IEnumerable<OutputBlock> x86_64_Cli(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(new byte[] { 0xfa });
        }

        internal IEnumerable<OutputBlock> x86_64_Break(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // int3
            return OBList(new byte[] { 0xcc });
        }

        internal IEnumerable<OutputBlock> x86_64_Lidt(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* void Lidt(UIntPtr idt_ptr)
             * idt_ptr is in a gpr
             * lidt [idt_ptr]
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncOpcode(3, op1.hardware_loc, 0, false, 0, 0x0f, 0x01)));
            return ret;
        }

        internal IEnumerable<OutputBlock> x86_64_round_double_xmm_xmm(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* cvtsd2si rax, xmm
             * cvtsi2si xmm, rax
             */

            List<OutputBlock> ret = new List<OutputBlock>();
            ret.Add(new CodeBlock(EncOpcode(Rax, op1.hardware_loc, 3, true, 0, 0xf2, 0x0f, 0x2d)));
            ret.Add(new CodeBlock(EncOpcode(result.hardware_loc, Rax, 3, true, 0, 0xf2, 0x0f, 0x2a)));
            return ret;
        }
    }
}
