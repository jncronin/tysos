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

namespace tysos
{
    public class Interrupts
    {
        tysos.Collections.StaticULongArray idt;

        ISR default_isr;
        ISREC default_isrec;
        ISR default_isr_lapic;

        public unsafe Interrupts(ulong idt_start)
        {
            idt = new tysos.Collections.StaticULongArray(idt_start, 256 * 16 / 8);
            idt.Clear(0);

            default_isr = DefaultHandler;
            default_isrec = DefaultHandlerErrorCode;
            default_isr_lapic = DefaultHandlerLapic;

            for (int i = 0; i < 256; i++)
                UninstallHandler(i);

            libsupcs.x86_64.Cpu.Lidt(idt_start, 256 * 16);
        }

        public void InstallHandler(int interrupt_no, Delegate handler) { InstallHandler(interrupt_no, handler, 0); }
        public void InstallHandler(int interrupt_no, Delegate handler, int ist)
        {
            if (handler == null)
                InstallHandler(interrupt_no, 0, ist);
            else
                InstallHandler(interrupt_no, (ulong)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(handler), ist);
        }

        public void InstallHandler(int interrupt_no, ulong handler_addr) { InstallHandler(interrupt_no, handler_addr, 0); }
        public void InstallHandler(int interrupt_no, ulong handler_addr, int ist)
        {
            // See Intel 3a:6.14.1

            if (handler_addr == 0)
            {
                idt[interrupt_no * 2] = 0;
                idt[interrupt_no * 2 + 1] = 0;
                return;
            }

            ulong low_qword, high_qword;

            low_qword = high_qword = 0x0;

            low_qword |= (handler_addr & 0xffff);
            low_qword |= (((handler_addr >> 16) & 0xffff) << 48);
            low_qword |= (0x8 << 16);

            ulong third_word = (ulong)(ist & 0x7);
            // type field is 0xe for an interrupt gate, 0xf for a trap gate
            third_word |= (0xe << 8);
            third_word |= (0x1 << 15);  // set present

            low_qword |= (third_word << 32);

            high_qword = ((handler_addr >> 32) & 0xffffffff);

            idt[interrupt_no * 2] = low_qword;
            idt[interrupt_no * 2 + 1] = high_qword;
        }

        public void UninstallHandler(int interrupt_no)
        {
            // Install the default handler

            // interrupts 8, 10, 11, 12, 13, 14 and 17 use an error code
            if ((interrupt_no == 8) || (interrupt_no == 10) || (interrupt_no == 11) ||
                (interrupt_no == 12) || (interrupt_no == 13) || (interrupt_no == 14) ||
                (interrupt_no == 17))
                InstallHandler(interrupt_no, (ulong)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(default_isrec));
            else if (interrupt_no >= 32)
                InstallHandler(interrupt_no, (ulong)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(default_isr_lapic));
            else
                InstallHandler(interrupt_no, (ulong)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(default_isr));

        }

        public unsafe delegate void ISREC(ulong error_code, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs);
        public unsafe delegate void ISR(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs);

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void DefaultHandler(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("A default handler was called", Program.arch.DebugOutput);
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void DefaultHandlerLapic(ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("A default handler which acknowledged the LAPIC was called", Program.arch.DebugOutput);
            ((x86_64.x86_64_cpu)Program.cur_cpu_data).CurrentLApic.SendEOI();
        }

        [libsupcs.ISR]
        [libsupcs.AlwaysCompile]
        public static unsafe void DefaultHandlerErrorCode(ulong error_code, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64* regs)
        {
            Formatter.WriteLine("A default handler with error code was called", Program.arch.DebugOutput);
        }
    }
}
