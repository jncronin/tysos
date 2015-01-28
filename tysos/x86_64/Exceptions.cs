/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace tysos.x86_64
{
    class Exceptions
    {
        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void DivideError_0_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Divide error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void DebugError_1_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Debug error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void NMIError_2_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regsa)
        {
            Arch.GDB_Registers regs = Arch.get_registers((long)(-libsupcs.OtherOperations.GetUsedStackSize()));
            Formatter.WriteLine("NMI", Program.arch.BootInfoOutput);
            Formatter.WriteLine("NMI received", Program.arch.DebugOutput);
            Arch.dump_registers(regs);

            // Switch to protected heap and unwind stack
            if (Program.cur_cpu_data != null)
                Program.cur_cpu_data.UseCpuAlloc = true;
            Unwind.DumpUnwindInfo(((libsupcs.x86_64.Unwinder)Program.arch.GetUnwinder().Init()).UnwindOneWithErrorCode().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void BreakPoint_3_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Breakpoint", Program.arch.BootInfoOutput);
            DumpExceptionData(0, return_rip, return_cs, rflags, return_rsp, return_ss, regs);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void OverflowError_4_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Overflow error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void BoundCheckError_5_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Bound check error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void InvalidOpcode_6_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Invalid opcode", Program.arch.BootInfoOutput);

            /* Unwind the stack */
            Formatter.WriteLine("Invalid opcode", Program.arch.DebugOutput);
            PageFault.unwinding = true;
            Formatter.WriteLine("Stack trace: ", Program.arch.DebugOutput);

            // Switch to protected heap and unwind stack
            if (Program.cur_cpu_data != null)
                Program.cur_cpu_data.UseCpuAlloc = true;
            Unwind.DumpUnwindInfo(Program.arch.GetUnwinder().Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void DeviceNotPresentError_7_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Device not present error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.x86_64.Cpu.ISRErrorCode]
        [libsupcs.AlwaysCompile]
        public static unsafe void DoubleFault_8_Handler(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Double fault", Program.arch.BootInfoOutput);

            Formatter.WriteLine("Stack trace:", Program.arch.DebugOutput);

            // Switch to protected heap and unwind stack
            if (Program.cur_cpu_data != null)
                Program.cur_cpu_data.UseCpuAlloc = true;
            Unwind.DumpUnwindInfo(Program.arch.GetUnwinder().Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.x86_64.Cpu.ISRErrorCode]
        [libsupcs.AlwaysCompile]
        public static unsafe void TSSError_10_Handler(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("TSS error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.x86_64.Cpu.ISRErrorCode]
        [libsupcs.AlwaysCompile]
        public static unsafe void SegmentNotPresentError_11_Handler(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Segment not present error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.x86_64.Cpu.ISRErrorCode]
        [libsupcs.AlwaysCompile]
        public static unsafe void StackeFaultError_12_Handler(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Stack Fault", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.x86_64.Cpu.ISRErrorCode]
        [libsupcs.AlwaysCompile]
        public static unsafe void GeneralProtection_13_Handler(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            if (PageFault.unwinding)
            {
                Formatter.WriteLine("General protection fault during stack unwinding", Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }

            Formatter.WriteLine("General protection error", Program.arch.BootInfoOutput);
            Formatter.WriteLine("General protection error", Program.arch.DebugOutput);

            ulong rbp = libsupcs.x86_64.Cpu.RBP;
            ulong ecode, rip;
            unsafe
            {
                ecode = *(ulong*)(rbp + 8);
                rip = *(ulong*)(rbp + 16);
            }
            Formatter.Write("Error code: ", Program.arch.DebugOutput);
            Formatter.Write(ecode, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
            Formatter.Write("RIP: ", Program.arch.DebugOutput);
            Formatter.Write(rip, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            DumpExceptionData(ec, return_rip, return_cs, rflags, return_rsp, return_ss, regs);

            libsupcs.x86_64.Cpu.Break();

            Formatter.WriteLine("Stack trace:", Program.arch.DebugOutput);

            // Switch to protected heap and unwind stack
            if (Program.cur_cpu_data != null)
                Program.cur_cpu_data.UseCpuAlloc = true;
            else
                gc.gc.Heap = gc.gc.HeapType.Startup;
            Unwind.DumpUnwindInfo(((libsupcs.x86_64.Unwinder)Program.arch.GetUnwinder().Init()).UnwindOneWithErrorCode().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void FPUError_16_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("FPU error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.x86_64.Cpu.ISRErrorCode]
        [libsupcs.AlwaysCompile]
        public static unsafe void AlignmentCheck_17_Handler(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Alignment check error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void MachineCheckError_18_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Machine check error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void SIMD_19_Handler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            if (PageFault.unwinding)
                libsupcs.OtherOperations.Halt();
            
            Formatter.WriteLine("SIMD error", Program.arch.BootInfoOutput);
            Formatter.Write("SIMD error, MXCSR: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)libsupcs.x86_64.Cpu.Mxcsr, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            // Switch to protected heap and unwind stack
            if (Program.cur_cpu_data != null)
                Program.cur_cpu_data.UseCpuAlloc = true;
            Unwind.DumpUnwindInfo(Program.arch.GetUnwinder().Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }

        public static unsafe void DumpExceptionData(ulong ec, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("Exception information:", Program.arch.DebugOutput);

            DumpRegister("EC    ", ec);
            DumpRegister("RIP   ", return_rip);
            DumpRegister("CS    ", return_cs);
            DumpRegister("RFLAGS", rflags);
            DumpRegister("RSP   ", return_rsp);
            DumpRegister("SS    ", return_ss);
            DumpRegister("RAX   ", regs->rax);
            DumpRegister("RBX   ", regs->rbx);
            DumpRegister("RCX   ", regs->rcx);
            DumpRegister("RDX   ", regs->rdx);
            DumpRegister("RSI   ", regs->rsi);
            DumpRegister("RDI   ", regs->rdi);
            DumpRegister("R8    ", regs->r8);
            DumpRegister("R9    ", regs->r9);
            DumpRegister("R10   ", regs->r10);
            DumpRegister("R11   ", regs->r11);
            DumpRegister("R12   ", regs->r12);
            DumpRegister("R13   ", regs->r13);
            DumpRegister("R14   ", regs->r14);
            DumpRegister("R15   ", regs->r15);
            DumpRegister("XMM0  ", regs->xmm0);
            DumpRegister("XMM1  ", regs->xmm1);
            DumpRegister("XMM2  ", regs->xmm2);
            DumpRegister("XMM3  ", regs->xmm3);
            DumpRegister("XMM4  ", regs->xmm4);
            DumpRegister("XMM5  ", regs->xmm5);
            DumpRegister("XMM6  ", regs->xmm6);
            DumpRegister("XMM7  ", regs->xmm7);
            DumpRegister("XMM8  ", regs->xmm8);
            DumpRegister("XMM9  ", regs->xmm9);
            DumpRegister("XMM10 ", regs->xmm10);
            DumpRegister("XMM11 ", regs->xmm11);
            DumpRegister("XMM12 ", regs->xmm12);
            DumpRegister("XMM13 ", regs->xmm13);
            DumpRegister("XMM14 ", regs->xmm14);
            DumpRegister("XMM15 ", regs->xmm15);
        }

        static void DumpRegister(string reg, ulong val)
        {
            Formatter.Write(reg, Program.arch.DebugOutput);
            Formatter.Write(": ", Program.arch.DebugOutput);
            Formatter.Write(val, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
        }
    }
}
