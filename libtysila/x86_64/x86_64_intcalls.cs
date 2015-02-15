/* Copyright (C) 2014 by John Cronin
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

using libtysila.frontend.cil;
using System;
using System.Collections.Generic;

namespace libtysila
{
    partial class x86_64_Assembler
    {
        protected internal override void InitArchIntCalls(Dictionary<string, frontend.cil.Opcode.TybelEncodeFunc> int_calls)
        {
            base.InitArchIntCalls(int_calls);

            int_calls["_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_7set_RSP_Rv_P1y"] = set_Rsp_v_y;
            int_calls["_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_4Lidt_Rv_P1u1U"] = Lidt_U;
            int_calls["_ZX12IoOperationsM_0_7PortInd_Rj_P1t"] = PortInd;
            int_calls["_ZX12IoOperationsM_0_7PortInw_Rt_P1t"] = PortInw;
            int_calls["_ZX12IoOperationsM_0_7PortInb_Rh_P1t"] = PortInb;
            int_calls["_ZX12IoOperationsM_0_7PortOut_Rv_P2tj"] = PortOutd;
            int_calls["_ZX12IoOperationsM_0_7PortOut_Rv_P2tt"] = PortOutw;
            int_calls["_ZX12IoOperationsM_0_7PortOut_Rv_P2th"] = PortOutb;
            int_calls["_ZN8libsupcs17libsupcs#2Ex86_643CpuM_0_5Break_Rv_P0"] = Break;
            int_calls["_ZW20System#2EDiagnostics8DebuggerM_0_5Break_Rv_P0"] = Break;
            int_calls["_ZX15OtherOperationsM_0_16GetReturnAddress_RPv_P0"] = GetReturnAddress;

            if(has_sse41)
                int_calls["_ZW6System4MathM_0_5Round_Rd_P1d"] = Math_Round;
        }

        static void set_Rsp_v_y(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_src = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rsp, loc_src, CliType.int64, il.il.tybel);
        }

        static void Lidt_U(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_src = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            if (!(loc_src is libasm.x86_64_gpr))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rax, loc_src, CliType.native_int, il.il.tybel);
                loc_src = x86_64_Assembler.Rax;
            }

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.LIDT, il.il.tybel, new libasm.hardware_contentsof { base_loc = loc_src });
        }

        static void PortInb(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_port = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U1);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            if (!((loc_port is libasm.const_location) && ass.FitsSByte(((libasm.const_location)loc_port).c)))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rdx, loc_port, CliType.int32, il.il.tybel);
                loc_port = x86_64_Assembler.Rdx;
            }

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.INB, il.il.tybel, x86_64_Assembler.Rax, loc_port);
            ass.Assign(state, il.stack_vars_before, loc_dest, x86_64_Assembler.Rax, CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void PortInw(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_port = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U2);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            if (!((loc_port is libasm.const_location) && ass.FitsSByte(((libasm.const_location)loc_port).c)))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rdx, loc_port, CliType.int32, il.il.tybel);
                loc_port = x86_64_Assembler.Rdx;
            }

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.INW, il.il.tybel, x86_64_Assembler.Rax, loc_port);
            ass.Assign(state, il.stack_vars_before, loc_dest, x86_64_Assembler.Rax, CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void PortInd(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_port = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            Signature.Param p_dest = new Signature.Param(BaseType_Type.U4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            if (!((loc_port is libasm.const_location) && ass.FitsSByte(((libasm.const_location)loc_port).c)))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rdx, loc_port, CliType.int32, il.il.tybel);
                loc_port = x86_64_Assembler.Rdx;
            }

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.INL, il.il.tybel, x86_64_Assembler.Rax, loc_port);
            ass.Assign(state, il.stack_vars_before, loc_dest, x86_64_Assembler.Rax, CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        static void PortOutb(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_val = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            libasm.hardware_location loc_port = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            if (!((loc_port is libasm.const_location) && ass.FitsSByte(((libasm.const_location)loc_port).c)))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rdx, loc_port, CliType.int32, il.il.tybel);
                loc_port = x86_64_Assembler.Rdx;
            }

            ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rax, loc_val, CliType.int32, il.il.tybel);

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.OUTB, il.il.tybel, loc_port, x86_64_Assembler.Rax);
        }

        static void PortOutw(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_val = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            libasm.hardware_location loc_port = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            if (!((loc_port is libasm.const_location) && ass.FitsSByte(((libasm.const_location)loc_port).c)))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rdx, loc_port, CliType.int32, il.il.tybel);
                loc_port = x86_64_Assembler.Rdx;
            }

            ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rax, loc_val, CliType.int32, il.il.tybel);

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.OUTW, il.il.tybel, loc_port, x86_64_Assembler.Rax);
        }

        static void PortOutd(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location loc_val = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            libasm.hardware_location loc_port = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            if (!((loc_port is libasm.const_location) && ass.FitsSByte(((libasm.const_location)loc_port).c)))
            {
                ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rdx, loc_port, CliType.int32, il.il.tybel);
                loc_port = x86_64_Assembler.Rdx;
            }

            ass.Assign(state, il.stack_vars_before, x86_64_Assembler.Rax, loc_val, CliType.int32, il.il.tybel);

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.OUTL, il.il.tybel, loc_port, x86_64_Assembler.Rax);
        }

        static void Break(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.INT3, il.il.tybel);
        }

        static void Math_Round(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* Assumes we have sse4.1 - use roundsd round to even */

            libasm.hardware_location loc_arg = il.stack_vars_after.Pop(ass);
            il.stack_after.Pop();

            Signature.Param p_ret = new Signature.Param(BaseType_Type.R8);
            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(p_ret, ass);

            ((x86_64_Assembler)ass).ChooseInstruction(x86_64.x86_64_asm.opcode.ROUNDSD, il.il.tybel, loc_ret, loc_arg, new libasm.const_location { c = 0 });

            il.stack_after.Push(p_ret);
        }

        static void GetReturnAddress(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_ret = new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = new Signature.BaseType(BaseType_Type.Void)}, ass);
            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(p_ret, ass);

            int ptr_size = ass.GetSizeOfPointer();
            ass.Assign(state, il.stack_vars_before, loc_ret,
                new libasm.hardware_contentsof { base_loc = x86_64_Assembler.Rbp, const_offset = ptr_size, size = ptr_size },
                CliType.native_int, il.il.tybel);

            il.stack_after.Push(p_ret);
        }
    }
}
