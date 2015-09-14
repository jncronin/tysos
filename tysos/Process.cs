/* Copyright (C) 2011-2015 by John Cronin
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
    public class Process
    {
        public List<Thread> threads = new List<Thread>();
        public Thread startup_thread;
        public string name;
        internal lib.File stdin, stdout, stderr;
        internal bool started = false;
        public bool Started { get { return started; } }
        public ServerObject MessageServer;

        public bool MessagePending { get { if (ipc == null) return false; return ipc.PeekMessage() != null; } }

        internal string current_directory = "/";
        public string CurrentDirectory { get { return current_directory; } }

        internal static Process Create(string name, ulong e_point, ulong stack_size, Virtual_Regions vreg, SymbolTable stab, object[] parameters)
        {
            Process p = new Process();

            p.startup_thread = Thread.Create(name + "(Thread 1)", e_point, stack_size, vreg, stab, parameters);
            p.startup_thread.owning_process = p;
            p.threads.Add(p.startup_thread);
            p.name = name;

            if (Program.running_processes != null)
                Program.running_processes[name] = p;

            return p;
        }

        public static Process CreateProcess(string name, Delegate e_point, object[] parameters)
        {
            return Create(name,
                (ulong)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(e_point),
                0x8000, Program.arch.VirtualRegions, Program.stab, parameters);
        }

        internal Virtual_Regions.Region ipc_region;
        internal IPC ipc;

        public static Process CreateProcess(lib.File file, string name, object [] parameters)
        {
            /* Create a process from an ELF module in a file object */

            ulong epoint = elf.ElfFileReader.LoadObject(Program.arch.VirtualRegions,
                Program.arch.VirtMem, Program.stab, file, name);

            return Create(name, epoint, 0x8000, Program.arch.VirtualRegions, Program.stab,
                parameters);
        }

        public void Start()
        {
            if (started)
                return;
            Program.arch.CurrentCpu.CurrentScheduler.Reschedule(startup_thread);
            started = true;
        }
    }

    public class Thread
    {
        internal static int next_thread_id = 2;

        internal const long DEF_SLICE = 10000000;     // 10 ms
        internal const int DEF_PRIORITY = 5;

        internal const int LOC_CURRENT = -1;
        internal const int LOC_SLEEPING = -2;
        internal const int LOC_BLOCKING = -3;
        internal const int LOC_RELEASED = -4;

        internal int location = LOC_RELEASED;
        internal int priority = DEF_PRIORITY;
        internal long time_to_run;
        internal long default_slice = DEF_SLICE;

        internal int thread_id;
        internal TaskSwitchInfo saved_state;
        public Process owning_process;
        internal Virtual_Regions.Region sse;
        internal Virtual_Regions.Region stack;

        internal string name;

        public string ProcessName { get
            {
                if (owning_process != null && owning_process.name != null)
                    return owning_process.name;
                else if (name != null)
                    return name;
                else
                    return "Unnamed thread";
            }
        }

        public bool do_profile = false;

        internal List<Event> BlockingOn = new List<Event>();

        internal ulong exit_address;

        internal static Thread Create(string name, ulong e_point, ulong stack_size, Virtual_Regions vreg, SymbolTable stab, object[] parameters)
        {
            Thread t = new Thread();

            t.thread_id = next_thread_id++;

            t.saved_state = Program.arch.CreateTaskSwitchInfo();
            t.stack = vreg.AllocRegion(stack_size, 0x1000, name + "_Stack", 0x1000, Virtual_Regions.Region.RegionType.Stack, true);
            t.saved_state.Init(new UIntPtr(e_point), t.stack, new UIntPtr(stab.GetAddress("__exit")), parameters);

            t.name = name;

            t.exit_address = stab.GetAddress("__exit");

            return t;
        }

        internal static Thread Create(string name, Delegate e_point, object[] parameters)
        {
            return Create(name, (ulong)System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(e_point),
                0x8000, Program.arch.VirtualRegions, Program.stab, parameters);
        }

        public static Thread CurrentThread
        {
            get
            {
                return Program.arch.CurrentCpu.CurrentThread;
            }
        }
    }
}
