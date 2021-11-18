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
    class PageFault
    {
        internal static bool unwinding = false;

        internal static List<ulong> ist_stack = null;
        internal static ulong tss_addr;
        internal static int cur_ist_idx = 0;
        internal static ulong ist1_offset = 0;
        internal static bool unwind_fail = false;

        internal static libsupcs.Unwinder pf_unwinder;

        [libsupcs.CallingConvention("isrec")]
        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("__pfault")]
        public unsafe static void PFHandler(ulong error_code, ulong return_rip, ulong return_cs,
            ulong rflags, ulong return_rsp, ulong return_ss, libsupcs.x86_64.Cpu.InterruptRegisters64 *regs)
        {
            ulong fault_address = libsupcs.x86_64.Cpu.Cr2;

            // Adjust the tss segment's ist1 pointer
            if (ist_stack != null)
            {
                cur_ist_idx++;

                if (cur_ist_idx >= 4)
                {
                    Formatter.Write("kernel: Warning, increasing number of page fault virtual stacks being used, fault_address: ", Program.arch.DebugOutput);
                    Formatter.Write(fault_address, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                }
                if (cur_ist_idx >= ist_stack.Count)
                {
                    /* Catch page faults in this final unwind */
                    if (unwind_fail)
                    {
                        Formatter.WriteLine("kernel: Page fault: stack unwind failed", Program.arch.DebugOutput);
                        libsupcs.OtherOperations.Halt();
                    }
                    unwind_fail = true;

                    Formatter.Write("Page fault.  Address: 0x", Program.arch.DebugOutput);
                    Formatter.Write(fault_address, "X", Program.arch.DebugOutput);

                    Formatter.WriteLine("Page fault handler has run out of virtual stacks!", Program.arch.BootInfoOutput);
                    Formatter.WriteLine("kernel: Page fault handler has run out of virtual stacks!", Program.arch.DebugOutput);
                    Formatter.WriteLine("kernel: Stack dump", Program.arch.DebugOutput);

                    // Switch to protected heap
                    if (Program.arch.CurrentCpu != null)
                        Program.arch.CurrentCpu.UseCpuAlloc = true;
                    else
                        tysos.gc.gc.Heap = gc.gc.HeapType.Startup;
                    Unwind.DumpUnwindInfo(pf_unwinder.Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
                    libsupcs.OtherOperations.Halt();
                }

                unsafe
                {
                    *(ulong*)(tss_addr + ist1_offset) = ist_stack[cur_ist_idx];
                }
            }

            if (Program.arch.VirtualRegions.heap.contains(fault_address))
            {
                // If the faulting address is in the heap, map a new page
                //Program.arch.VirtMem.map_page(fault_address);
                do_map(fault_address, error_code);
            }
            else
            {
                /* Check if it something we can allocate, i.e. stack space or SSE storage space or cpu-specific space */
                Virtual_Regions.Region cur_reg = Program.arch.VirtualRegions.list_start;
                bool success = false;
                while ((cur_reg != null) && !success)
                {
                    if (cur_reg.contains(fault_address))
                    {
                        if ((cur_reg.type == Virtual_Regions.Region.RegionType.SSE_state) ||
                            (cur_reg.type == Virtual_Regions.Region.RegionType.CPU_specific) ||
                            (cur_reg.type == Virtual_Regions.Region.RegionType.IPC) ||
                            cur_reg.type == Virtual_Regions.Region.RegionType.ModuleSection)
                        {
                            //Program.arch.VirtMem.map_page(fault_address);
                            do_map(fault_address, error_code);
                            success = true;
                        }
                        else if (cur_reg.type == Virtual_Regions.Region.RegionType.Stack)
                        {
                            /* Check for stack overflow into the guard page */
                            if (fault_address < (cur_reg.start + cur_reg.stack_protect))
                            {
                                Formatter.Write("Page fault.  Address: 0x", Program.arch.BootInfoOutput);
                                Formatter.Write(fault_address, "X", Program.arch.BootInfoOutput);
                                Formatter.WriteLine(Program.arch.BootInfoOutput);
                                Formatter.WriteLine("Stack overflow!", Program.arch.BootInfoOutput);

                                Formatter.Write("Page fault.  Address: 0x", Program.arch.DebugOutput);
                                Formatter.Write(fault_address, "X", Program.arch.DebugOutput);
                                Formatter.WriteLine(Program.arch.DebugOutput);
                                Formatter.WriteLine("Stack overflow!", Program.arch.DebugOutput);

                                /* Unwind the stack */
                                if (pf_unwinder != null)
                                {
                                    if (Program.arch.CurrentCpu != null)
                                        Program.arch.CurrentCpu.UseCpuAlloc = true;
                                    else
                                        tysos.gc.gc.Heap = gc.gc.HeapType.Startup;
                                    Formatter.WriteLine("Stack trace: ", Program.arch.DebugOutput);
                                    pf_unwinder.Init();
                                    Unwind.DumpUnwindInfo(((libsupcs.x86_64.Unwinder)pf_unwinder).UnwindOneWithErrorCode().DoUnwind((UIntPtr)Program.arch.ExitAddress, false),
                                        Program.arch.DebugOutput);
                                }
                                
                                libsupcs.OtherOperations.Halt();
                            }
                            else
                            {
                                //Program.arch.VirtMem.map_page(fault_address);
                                do_map(fault_address, error_code);
                                success = true;
                            }
                        }
                    }

                    cur_reg = cur_reg.next;
                }

                if (!success)
                {
                    if (unwinding)
                    {
                        // If we are in the middle of unwinding a previous page fault then 
                        //  any new fault is caught when we reach the end of the stack, so we
                        //  should stop.

                        Formatter.Write("Invalid page fault whilst unwinding.  Address: 0x", Program.arch.DebugOutput);
                        Formatter.Write(fault_address, "X", Program.arch.DebugOutput);
                        Formatter.WriteLine(Program.arch.DebugOutput);
                        Formatter.Write("Invalid page fault whilst unwinding.  Address: 0x", Program.arch.BootInfoOutput);
                        Formatter.Write(fault_address, "X", Program.arch.BootInfoOutput);
                        Formatter.WriteLine(Program.arch.BootInfoOutput);

                        libsupcs.OtherOperations.Halt();
                    }

                    Formatter.Write("Page fault.  Address: 0x", Program.arch.BootInfoOutput);
                    Formatter.Write(fault_address, "X", Program.arch.BootInfoOutput);
                    Formatter.WriteLine(Program.arch.BootInfoOutput);
                    Formatter.WriteLine("Address not allowed!", Program.arch.BootInfoOutput);

                    Formatter.Write("Page fault.  Address: 0x", Program.arch.DebugOutput);
                    Formatter.Write(fault_address, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(" - address not allowed!", Program.arch.DebugOutput);

                    // Extract error code and return eip
                    ulong rbp = libsupcs.x86_64.Cpu.RBP;
                    ulong ec, rip;
                    unsafe
                    {
                        ec = *(ulong*)(rbp + 8);
                        rip = *(ulong*)(rbp + 16);
                        rbp = *(ulong*)rbp;
                    }
                    Formatter.Write("Error code: ", Program.arch.DebugOutput);
                    Formatter.Write(ec, Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                    Formatter.Write("RIP: ", Program.arch.DebugOutput);
                    Formatter.Write(rip, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);

                    tysos.x86_64.Exceptions.DumpExceptionData(error_code, return_rip, return_cs, rflags,
                        return_rsp, return_ss, rbp, regs);


                    //Program.arch.VirtualRegions.Dump(Program.arch.DebugOutput);

                    if (pf_unwinder != null)
                    {
                        if (Program.arch.CurrentCpu != null)
                            Program.arch.CurrentCpu.UseCpuAlloc = true;
                        else
                            tysos.gc.gc.Heap = gc.gc.HeapType.Startup;

                        unwinding = true;
                        Formatter.WriteLine("Stack trace: ", Program.arch.DebugOutput);
                        pf_unwinder.Init();
                        //Unwind.DumpUnwindInfo(((libsupcs.x86_64.Unwinder)pf_unwinder).UnwindOneWithErrorCode().DoUnwind((UIntPtr)Program.arch.ExitAddress),
                        //    Program.arch.DebugOutput);
                        Unwind.DumpUnwindInfo(pf_unwinder.DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
                    }

                    /* Unwind the stack */
                    /*Unwind u2 = new Unwind();

                    DumpIP(u2.UnwindOneWithErrorCode());
                    ulong next_rip2;
                    do
                    {
                        next_rip2 = u2.UnwindOne();
                        DumpIP(next_rip2);
                    } while (next_rip2 != 0);*/

                    libsupcs.OtherOperations.Halt();
                }
            }

            // Restore the tss segment's ist1 pointer
            if (ist_stack != null)
            {
                cur_ist_idx--;

                unsafe
                {
                    *(ulong*)(tss_addr + ist1_offset) = ist_stack[cur_ist_idx];
                }
            }
        }

        static void do_map(ulong vaddr, ulong ec)
        {
            /* If the error was a read, we only need to map a pre-existing blank
             * page (to save on physical memory space), otherwise map a new page */

            if((ec & 0x2) == 0)
            {
                // was read - use the blank page
                Program.arch.VirtMem.map_page(vaddr, VirtMem.blank_page);
            }
            else
            {
                // was write - request a new page
                Program.arch.VirtMem.map_page(vaddr);
            }
        }

        internal static void DumpIP(ulong rip)
        {
            string sym_name = "Unknown";
            ulong offset = rip;

            if (rip == 0)
            {
                Formatter.WriteLine("End of stack trace", Program.arch.DebugOutput);
                //Formatter.WriteLine("End of stack trace", Program.arch.BootInfoOutput);
                return;
            }

            if (Program.stab != null)
                sym_name = Program.stab.GetSymbolAndOffset(rip, out offset);

            Formatter.Write(rip, "X", Program.arch.DebugOutput);
            Formatter.Write(": ", Program.arch.DebugOutput);
            Formatter.Write(sym_name, Program.arch.DebugOutput);
            Formatter.Write(" + ", Program.arch.DebugOutput);
            Formatter.Write(offset, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);

            /*Formatter.Write(rip, "X", Program.arch.BootInfoOutput);
            Formatter.Write(": ", Program.arch.BootInfoOutput);
            Formatter.Write(sym_name, Program.arch.BootInfoOutput);
            Formatter.Write(" + ", Program.arch.BootInfoOutput);
            Formatter.Write(offset, "X", Program.arch.BootInfoOutput);
            Formatter.WriteLine(Program.arch.BootInfoOutput);*/
        }
    }
}
