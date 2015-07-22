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

namespace tysos
{
    public class Syscalls
    {
        public class CPUFunctions
        {
            [libsupcs.Syscall]
            public static Cpu GetCurrentCpu() { return Program.cur_cpu_data; }
        }

        public class InterruptFunctions
        {
            [libsupcs.Syscall]
            public static void InstallHandler(int interrupt_no, Delegate handler) { Program.arch.Interrupts.InstallHandler(interrupt_no, handler, 0); }

            [libsupcs.Syscall]
            public static InterruptMap GetInterruptMap() { return Program.imap; }
        }

        public class ProcessFunctions
        {
            public enum SpecialProcessType
            { Vfs, Gui, Logger };

            [libsupcs.Syscall]
            public static bool RegisterSpecialProcess(ServerObject o, SpecialProcessType proc_type)
            {
                switch (proc_type)
                {
                    case SpecialProcessType.Vfs:
                        if (o is Ivfs)
                        {
                            Program.vfs = o as Ivfs;
                            return true;
                        }
                        Program.Vfs = o;
                        break;

                    case SpecialProcessType.Gui:
                        Program.Gui = o;
                        break;

                    case SpecialProcessType.Logger:
                        Program.Logger = o;
                        break;
                }
                return false;
            }

            [libsupcs.Syscall]
            public static ServerObject GetSpecialProcess(SpecialProcessType type)
            {
                switch(type)
                {
                    case SpecialProcessType.Gui:
                        return Program.Gui;
                    case SpecialProcessType.Logger:
                        return Program.Logger;
                    case SpecialProcessType.Vfs:
                        return Program.Vfs;
                    default:
                        return null;
                }
            }

            [libsupcs.Syscall]
            public static Process GetProcessByName(string s)
            {
                if (Program.running_processes.ContainsKey(s))
                    return Program.running_processes[s];
                else
                    return null;
            }

            [libsupcs.Syscall]
            public static Process ExecModule(string name)
            {
                return Program.LoadELFProgram(name, Program.mboot_header, Program.stab, Program.running_processes, 0x8000);
            }

            [libsupcs.Syscall]
            public static Process ExecModule(string name, bool start)
            {
                return Program.LoadELFProgram(name, Program.mboot_header, Program.stab, Program.running_processes, 0x8000, start);
            }

            [libsupcs.Syscall]
            public static void StartProcess(Process p)
            {
                if(!p.started)
                    Program.cur_cpu_data.CurrentScheduler.Reschedule(p.startup_thread);
            }

            [libsupcs.Syscall]
            public static Process GetCurrentProcess()
            {
                return Program.cur_cpu_data.CurrentThread.owning_process;
            }

            [libsupcs.Syscall]
            public static Thread GetCurrentThread()
            {
                return Program.cur_cpu_data.CurrentThread;
            }
        }

        public class SchedulerFunctions
        {
            [libsupcs.Syscall]
            [libsupcs.Uninterruptible]
            public static void Yield()
            {
                /* Yield the timeslice of the current thread */
                Thread cur = Program.cur_cpu_data.CurrentThread;
                Scheduler sched = Program.cur_cpu_data.CurrentScheduler;
                TaskSwitcher switcher = Program.arch.Switcher;

                if (sched == null)
                    throw new Exception("Cannot yield as scheduler not yet initialized");

                if (cur != null)
                    sched.Reschedule(cur);

                Thread next;
                lock (sched)
                {
                    next = sched.GetNextThread();
                }

                if ((next != cur) && (next != null))
                    switcher.Switch(next);
            }

            [libsupcs.Syscall]
            [libsupcs.Uninterruptible]
            public static void Block()
            {
                /* Block pending receipt of a message */
                Thread cur = Program.cur_cpu_data.CurrentThread;
                Scheduler sched = Program.cur_cpu_data.CurrentScheduler;
                TaskSwitcher switcher = Program.arch.Switcher;

                if (sched == null)
                    throw new Exception("Cannot block as scheduler not yet initialized");

                if (cur != null)
                    sched.Block(cur);
                
                Thread next;
                lock (sched)
                {
                     next = sched.GetNextThread();
                }

                if ((next != cur) && (next != null))
                    switcher.Switch(next);
            }

            [libsupcs.Syscall]
            [libsupcs.Uninterruptible]
            public static void Block(Event e)
            {
                /* Block on an event */
                Thread cur = Program.cur_cpu_data.CurrentThread;
                Scheduler sched = Program.cur_cpu_data.CurrentScheduler;
                TaskSwitcher switcher = Program.arch.Switcher;

                if (sched == null)
                    throw new Exception("Cannot block as scheduler not yet initialized");

                if (cur != null)
                    sched.Block(cur, e);

                Thread next;
                lock (sched)
                {
                    next = sched.GetNextThread();
                }

                if ((next != cur) && (next != null))
                    switcher.Switch(next);
            }

            [libsupcs.Syscall]
            [libsupcs.Uninterruptible]
            public static Thread GetCurrentThread()
            {
                return Program.cur_cpu_data.CurrentThread;
            }
        }

        public class DebugFunctions
        {
            [libsupcs.Syscall]
            public static void Write(char ch)
            { Program.arch.BootInfoOutput.Write(ch); Program.arch.BootInfoOutput.Flush(); }
            [libsupcs.Syscall]
            public static void Write(string s)
            { Program.arch.BootInfoOutput.Write(s); Program.arch.BootInfoOutput.Flush(); }
            [libsupcs.Syscall]
            public static void DebugWrite(char ch)
            { Program.arch.DebugOutput.Write(ch); Program.arch.DebugOutput.Flush(); }
            [libsupcs.Syscall]
            public static void DebugWrite(string s)
            { Program.arch.DebugOutput.Write(s); Program.arch.DebugOutput.Flush(); }

            [libsupcs.Syscall]
            public static void RedirectBootInfoOutput(IDebugOutput output)
            {
                if (output == null)
                    Program.arch.BootInfoOutput = new NullOutput();
                else
                    Program.arch.BootInfoOutput = output;
            }
            [libsupcs.Syscall]
            public static void RedirectDebugOutput(IDebugOutput output)
            {
                if (output == null)
                    Program.arch.DebugOutput = new NullOutput();
                else
                    Program.arch.DebugOutput = output;
            }
        }

        public class IPCFunctions
        {
            [libsupcs.Syscall]
            public static bool InitIPC()
            { return IPC.InitIPC(Program.cur_cpu_data.CurrentThread.owning_process); }
            [libsupcs.Syscall]
            public static IPCMessage ReadMessage()
            { if (Program.cur_cpu_data.CurrentThread.owning_process.ipc == null) return null; else return Program.cur_cpu_data.CurrentThread.owning_process.ipc.ReadMessage(); }
            [libsupcs.Syscall]
            public static bool SendMessage(Process dest, IPCMessage message)
            {
                if (dest == null)
                    return false;
                if (dest.ipc == null)
                {
                    if (IPC.InitIPC(dest) == false)
                        return false;
                }
                message.Source = Program.cur_cpu_data.CurrentThread;

                return dest.ipc.SendMessage(message);
            }

            [libsupcs.Syscall]
            public static bool SendMessage(Process dest, Messages.Message message, int type)
            {
                if (dest == null)
                    return false;
                if (dest.ipc == null)
                {
                    if (IPC.InitIPC(dest) == false)
                        return false;
                }

                IPCMessage msg = new IPCMessage();
                msg.Source = Program.cur_cpu_data.CurrentThread;
                msg.Type = type;
                msg.Message = message;

                return dest.ipc.SendMessage(msg);
            }
        }

        public class MemoryFunctions
        {
            public enum CacheType
            { Uncacheable, WriteCombining, WriteThrough, WriteBack, WriteProtected }

            [libsupcs.Syscall]
            public static ulong MapPhysicalMemory(ulong phys_addr, ulong size, CacheType cache_type, bool writeable)
            {
                bool cache_disable = false;
                bool write_through = false;

                switch (cache_type)
                {
                    case CacheType.Uncacheable:
                        cache_disable = true;
                        write_through = true;
                        break;
                }

                return Program.map_in(phys_addr, size, Program.cur_cpu_data.CurrentThread.owning_process.name, writeable, cache_disable, write_through);
            }
        }
    }
}
