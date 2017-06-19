/* Copyright (C) 2011 by John Cronin
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
using System.Runtime.CompilerServices;

namespace tysos
{
    class Exceptions
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        [libsupcs.ReinterpretAsMethod]
        internal static extern libsupcs.TysosMethod ReinterpretAsTysosMethod(ulong addr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [libsupcs.ReinterpretAsMethod]
        internal static extern libsupcs.TysosMethod.EHClause ReinterpretAsEHClause(ulong addr);

        //[MethodImpl(MethodImplOptions.InternalCall)]
        //internal static extern void CallExceptionHandler(ulong addr, ulong rbp);

        [libsupcs.MethodAlias("sthrow")]
        [libsupcs.AlwaysCompile]
        static void SimpleThrow(int exception_id, libsupcs.TysosMethod methinfo)
        {
            if (exception_id == -1)
            {
                HandleFinally(methinfo);
                return;
            }
            if (exception_id == 1)
            {
                Formatter.WriteLine("OverflowException", Program.arch.BootInfoOutput);
                Formatter.WriteLine("OverflowException", Program.arch.DebugOutput);
            }
            else if (exception_id == 2)
            {
                Formatter.WriteLine("InvalidCastException", Program.arch.BootInfoOutput);
                Formatter.WriteLine("InvalidCastException", Program.arch.DebugOutput);
            }
            else if (exception_id == 3)
            {
                Formatter.WriteLine("NullReferenceException", Program.arch.BootInfoOutput);
                Formatter.WriteLine("NullReferenceException", Program.arch.DebugOutput);
            }
            else if (exception_id == 4)
            {
                Formatter.WriteLine("MissingMethodException", Program.arch.BootInfoOutput);
                Formatter.WriteLine("MissingMethodException", Program.arch.DebugOutput);
            }
            else if (exception_id == 5)
            {
                Formatter.WriteLine("IndexOutOfRangeException", Program.arch.BootInfoOutput);
                Formatter.WriteLine("IndexOutOfRangeException", Program.arch.DebugOutput);
            }
            else
            {
                Formatter.Write("Unknown exception:", Program.arch.BootInfoOutput);
                Formatter.Write((ulong)exception_id, "d", Program.arch.BootInfoOutput);
                Formatter.Write("Unknown exception:", Program.arch.DebugOutput);
                Formatter.Write((ulong)exception_id, "d", Program.arch.DebugOutput);
            }

            // Switch to protected heap and unwind stack
            bool old_cpu_alloc = false;
            if (Program.arch.CurrentCpu != null)
            {
                old_cpu_alloc = Program.arch.CurrentCpu.UseCpuAlloc;
                Program.arch.CurrentCpu.UseCpuAlloc = true;
            }
            Unwind.DumpUnwindInfo(Program.arch.GetUnwinder().Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            if (Program.arch.CurrentCpu != null)
                Program.arch.CurrentCpu.UseCpuAlloc = old_cpu_alloc;
            libsupcs.OtherOperations.Halt();
        }

        /* This isn't currently used - handling of leave -> finally clauses
         * happens at compile time.  It is left in as an example of a stack unwinding/exception
         * handling algorithm for when we handle proper exceptions
         */
        private static void HandleFinally(libsupcs.TysosMethod methinfo)
        {
            // Start unwinding

            // NB previous RIP is at [cur_rbp + 8]
            // previous rbp is at [cur_rbp]
            // previous methinfo is at [prev_rbp - 8]

            ulong cur_rbp = libsupcs.x86_64.Cpu.RBP;

            unsafe
            {
                ulong prev_rip = 0;
                do
                {
                    ulong prev_rbp = *(ulong*)(cur_rbp);
                    prev_rip = *(ulong*)(cur_rbp + 8);
                    ulong prev_methinfo_addr = *(ulong*)(prev_rbp - 8);

                    libsupcs.TysosMethod prev_methinfo = ReinterpretAsTysosMethod(prev_methinfo_addr);

                    ulong meth_base = (ulong)prev_methinfo.MethodAddress;

                    if (prev_methinfo.EHClauses != (IntPtr)0)
                    {
                        ulong cur_eh_clause_addr_ptr = (ulong)prev_methinfo.EHClauses;

                        while (*(ulong*)(cur_eh_clause_addr_ptr) != 0)
                        {
                            ulong cur_eh_clause_addr = *(ulong*)(cur_eh_clause_addr_ptr);
                            libsupcs.TysosMethod.EHClause cur_eh_clause = ReinterpretAsEHClause(cur_eh_clause_addr);

                            ulong try_start = meth_base + (ulong)cur_eh_clause.TryStart;
                            ulong try_end = meth_base + (ulong)cur_eh_clause.TryEnd;
                            ulong handler_start = meth_base + (ulong)cur_eh_clause.Handler;

                            if (cur_eh_clause.IsFinally)
                            {
                                if ((prev_rip >= try_start) && (prev_rip < try_end))
                                {
                                    throw new NotImplementedException();
                                    //CallExceptionHandler(handler_start, prev_rbp);
                                    return;
                                }
                            }

                            cur_eh_clause_addr_ptr += 8;
                        }
                    }

                    cur_rbp = prev_rbp;
                    prev_rip = *(ulong*)(cur_rbp + 8);
                } while (prev_rip != Program.arch.ExitAddress);
            }
        }

        [libsupcs.MethodAlias("throw")]
        [libsupcs.AlwaysCompile]
        static void Throw(System.Exception exception, libsupcs.TysosMethod methinfo)
        {
            Formatter.WriteLine("Exception thrown!", Program.arch.BootInfoOutput);
            Formatter.WriteLine(exception.ToString(), Program.arch.BootInfoOutput);
            if(methinfo != null)
                Formatter.WriteLine(" in method " + methinfo.DeclaringType.FullName + "." + methinfo.Name, Program.arch.BootInfoOutput);
            Formatter.WriteLine("Exception thrown!", Program.arch.DebugOutput);
            Formatter.WriteLine(exception.ToString(), Program.arch.DebugOutput);
            if(methinfo != null)
                Formatter.WriteLine(" in method " + methinfo.DeclaringType.FullName + "." + methinfo.Name, Program.arch.DebugOutput);

            /* Unwind the stack */
            PageFault.unwinding = true;
            Formatter.WriteLine("Stack trace: ", Program.arch.DebugOutput);

            // Switch to protected heap and unwind stack
            bool old_cpu_alloc = false;
            if (Program.arch.CurrentCpu != null)
            {
                old_cpu_alloc = Program.arch.CurrentCpu.UseCpuAlloc;
                Program.arch.CurrentCpu.UseCpuAlloc = true;
            }
            Unwind.DumpUnwindInfo(Program.arch.GetUnwinder().Init().UnwindOne().DoUnwind((UIntPtr)Program.arch.ExitAddress), Program.arch.DebugOutput);
            if (Program.arch.CurrentCpu != null)
                Program.arch.CurrentCpu.UseCpuAlloc = old_cpu_alloc;
            libsupcs.OtherOperations.Halt();
        }
    }
}
