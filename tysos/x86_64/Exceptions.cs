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
        public static void DivideError_0_Handler()
        {
            Formatter.WriteLine("Divide error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void DebugError_1_Handler()
        {
            Formatter.WriteLine("Debug error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void NMIError_2_Handler()
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
        public static void BreakPoint_3_Handler()
        {
            Formatter.WriteLine("Breakpoint", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void OverflowError_4_Handler()
        {
            Formatter.WriteLine("Overflow error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void BoundCheckError_5_Handler()
        {
            Formatter.WriteLine("Bound check error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void InvalidOpcode_6_Handler()
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
        public static void DeviceNotPresentError_7_Handler()
        {
            Formatter.WriteLine("Device not present error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void DoubleFault_8_Handler(ulong ec)
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
        [libsupcs.AlwaysCompile]
        public static void TSSError_10_Handler(ulong ec)
        {
            Formatter.WriteLine("TSS error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void SegmentNotPresentError_11_Handler(ulong ec)
        {
            Formatter.WriteLine("Segment not present error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void StackeFaultError_12_Handler(ulong ec)
        {
            Formatter.WriteLine("Stack Fault", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void GeneralProtection_13_Handler(ulong ec)
        {
            if (PageFault.unwinding)
            {
                Formatter.WriteLine("General protection fault during stack unwinding", Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }

            Formatter.WriteLine("General protection error", Program.arch.BootInfoOutput);
            Formatter.WriteLine("General protection error", Program.arch.DebugOutput);

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
        public static void FPUError_16_Handler()
        {
            Formatter.WriteLine("FPU error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void AlignmentCheck_17_Handler(ulong ec)
        {
            Formatter.WriteLine("Alignment check error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void MachineCheckError_18_Handler()
        {
            Formatter.WriteLine("Machine check error", Program.arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static void SIMD_19_Handler()
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
    }
}
