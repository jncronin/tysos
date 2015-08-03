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

using tysos.Collections;
using System.Collections.Generic;
using System.Text;
using System;

namespace tysos
{
    public class Program
    {
        internal static Environment env;
        internal static SymbolTable stab;
        internal static InterruptMap imap;
        internal static Multiboot.Header mboot_header;
        internal static ServerObject Vfs, Logger, Gui;

        internal static Dictionary<string, Process> running_processes;

        internal static Arch arch;

        internal static System.Threading.Thread StartupThread;

        internal static string[] kernel_cmd_line;

        [libsupcs.MethodAlias("kmain")]
        static void KMain(Multiboot.Header mboot)
        {
            // Disable profiling until we have enabled the arch.DebugOutput port
            do_profile = false;

            // Get the multiboot header
            mboot_header = mboot;

            /* Create a temporary heap, then initialize the architecture
             * which will set up the permanent heap.  Then initialize the garbage collector */
            ulong arch_data_length = 0;
            switch(mboot.machine_major_type)
            {
                case (uint)Multiboot.MachineMajorType.x86_64:
                    arch_data_length = tysos.x86_64.Arch.GetRecommendedChunkLength();
                    break;
                default:
                    return;
            }

            ulong heap_start = mboot.heap_start + arch_data_length;
            //ulong heap_len = mboot.heap_end - heap_start;
            gc.gc.Heap = gc.gc.HeapType.Startup;
            gc.simple_heap.Init(heap_start, mboot.heap_end);


            /* Set up the default startup thread */
            StartupThread = new System.Threading.Thread(null_func);

            /* Initialize the architecture */
            UIntPtr chunk_vaddr = new UIntPtr(mboot.heap_start);
            UIntPtr chunk_length = new UIntPtr(arch_data_length);

            switch(mboot.machine_major_type)
            {
                case (uint)Multiboot.MachineMajorType.x86_64:
                    arch = new tysos.x86_64.Arch();
                    break;
            }
            arch.Init(chunk_vaddr, chunk_length, mboot);

            /* Parse the kernel command line */
            kernel_cmd_line = mboot.cmdline.Split(' ');

            //while (true) ;

            // test dynamic types
            //if (test_dynamic() == null)
            //    throw new Exception("test_dynamic failed");
            //if (test_dynamic2() == null)
            //    throw new Exception("test_dynamic2 failed");

            // Say hi
            Formatter.WriteLine("Tysos v0.2.0", arch.BootInfoOutput);
            Formatter.WriteLine("Tysos v0.2.0", arch.DebugOutput);
            Formatter.Write("Command line: ", arch.DebugOutput);
            Formatter.WriteLine(mboot.cmdline, arch.DebugOutput);
            bool do_debug = false;
            if (GetCmdLine("debug"))
            {
                Formatter.Write("Kernel debug: ", arch.BootInfoOutput);
                Formatter.WriteLine("Kernel debug requested", arch.DebugOutput);

                if (arch.InitGDBStub())
                {
                    Formatter.WriteLine("enabled", arch.BootInfoOutput);
                    Formatter.WriteLine("Kernel debug started", arch.DebugOutput);
                    do_debug = true;
                }
                else
                {
                    Formatter.WriteLine("not supported by current architecture", arch.BootInfoOutput);
                    Formatter.WriteLine("Kernel debug not supported by current architecture", arch.DebugOutput);
                }
            }

         
            /* Map in the ELF image of the kernel, so we can load its symbols */
            ulong tysos_vaddr = map_in(mboot.tysos_paddr, mboot.tysos_size, "tysos binary");

            /* Trigger a breakpoint to synchronize with gdb */
            if (do_debug)
            {
                Formatter.WriteLine("Synchronizing with debugger...", arch.BootInfoOutput);
                System.Diagnostics.Debugger.Break();
            }
            
            /* Set up a default environment */
            env = new Environment();
            env.env_vars.Add("OS", "tysos");
            env.env_vars.Add("OSVER", "v0.2.0");
            env.env_vars.Add("NUMBER_OF_PROCESSORS", "1");

#if NO_BOEHM
            gc.heap_arena.debug = true;
#endif  // NO_BOEHM

            /* Load up the symbol table for tysos */
            if (GetCmdLine("skip_kernel_syms") == false)
            {
                stab = new SymbolTable();
                Formatter.Write("Loading kernel symbols... ", arch.BootInfoOutput);
                Formatter.Write("Loading kernel symbols.  Tysos base: ", arch.DebugOutput);
                Formatter.Write(tysos_vaddr, "X", arch.DebugOutput);
                Formatter.WriteLine(arch.DebugOutput);

                /* Look for a hash file */
                Multiboot.Module khash = find_module(mboot.modules, "tysos.hash");
                ulong khash_addr = 0;
                if (khash != null)
                {
                    khash_addr = map_in(khash);
                    Formatter.Write("Kernel hash table found, mapped to address: ", arch.DebugOutput);
                    Formatter.Write(khash_addr, "X", arch.DebugOutput);
                    Formatter.WriteLine(arch.DebugOutput);
                    throw new NotImplementedException();
                }

                ElfReader.LoadSymbols(stab, mboot);
                //ElfReader.LoadSymbols(stab, tysos_vaddr, mboot.tysos_virtaddr, khash_addr);
                Formatter.WriteLine("done", arch.BootInfoOutput);
            }

            /* Test the garbage collector */
            if (GetCmdLine("skip_test_gc") == false)
            {
                Formatter.Write("Testing garbage collector... ", arch.BootInfoOutput);
                gc.gc.DoCollection();
                Formatter.WriteLine("done", arch.BootInfoOutput);
            }

            /* Start the scheduler */
            arch.CurrentCpu.CurrentScheduler = new Scheduler();
            arch.SchedulerTimer.Callback = new Timer.TimerCallback(Scheduler.TimerProc);

            /* Store the process info */
            running_processes = new Dictionary<string, Process>(new MyGenericEqualityComparer<string>());

            /* Init vfs signatures */
            lib.File.InitSigs();

            /* Build a list of available modules */
            List<tysos.lib.File.Property> mods = new List<tysos.lib.File.Property>();
            foreach (Multiboot.Module mod in mboot.modules)
            {
                ulong vaddr = map_in(mod);
                mods.Add(new tysos.lib.File.Property { Name = mod.name, Value = new VirtualMemoryResource64(vaddr, mod.length) });
            }
            List<tysos.lib.File.Property> modfs_props = new List<lib.File.Property>();
            modfs_props.Add(new lib.File.Property { Name = "device", Value = "modfs" });
            modfs_props.Add(new lib.File.Property { Name = "mods", Value = mods });

            /* Load the logger */
            Process logger = LoadELFModule("logger", mboot, stab, running_processes, 0x8000,
                new object[] { });

            Process debugprint = LoadELFModule("debugprint", mboot, stab, running_processes, 
                0x8000, new object[] { });

            logger.Start();
            //debugprint.Start();

            /* Load the vfs */
            Process vfs = LoadELFModule("vfs", mboot, stab, running_processes, 0x8000, new object[] { });
            vfs.Start();

            /* Load and mount the root fs */
            rootfs rootfs = new rootfs(new List<rootfs.rootfs_item>
            {
                new rootfs.rootfs_item { Name = "system", Props = arch.SystemProperties },
                new rootfs.rootfs_item { Name = "modules", Props = modfs_props }
            });
            Process rootfs_p = Process.CreateProcess("rootfs",
                new System.Threading.ThreadStart(rootfs.MessageLoop), new object[] { rootfs });
            rootfs_p.Start();
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/", rootfs },
                new Type[] { typeof(string), typeof(ServerObject) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/modules", "/modules", "modfs" },
                new Type[] { typeof(string), typeof(string), typeof(string) });

            /* Load the modfs driver */
            Process modfs = LoadELFModule("modfs", mboot, stab, running_processes, 0x8000, new object[] { });
            modfs.Start();

            /* Load a filesystem dump tester */
            LoadELFModule("fsdump", mboot, stab, running_processes, 0x8000, new object[] { }).Start();

            arch.EnableMultitasking();

            while (true) ;



            // Halt here
            libsupcs.OtherOperations.Halt();


            /* Load the gui */
            Process gui = LoadELFProgram("gui", mboot, stab, running_processes, 0x8000);
            gui.startup_thread.priority++;      // Elevate the priority of the gui

            /* Load the ACPI setup program */
            LoadELFProgram("ACPI_PC", mboot, stab, running_processes, 0x10000);

            /* Load the console application */
            LoadELFProgram("Console", mboot, stab, running_processes, 0x8000);

            /* Create kernel threads */
            CreateKernelThreads();

            /* Dump the current virtual region table */
            arch.VirtualRegions.Dump(arch.DebugOutput);

            /* Start the processes */
            Formatter.WriteLine("Going multitasking...", arch.BootInfoOutput);
            arch.EnableMultitasking();
            while (true) ;
        }

        static string str_array_test(string [] val)
        {
            return val[0];
        }

        private static object generic_castclass_test(object list)
        {
            return list as IList<string>;
        }

        private static void null_func(object obj)
        {
            throw new NotImplementedException();
        }

        private static void CreateKernelThreads()
        {
            Process kernel_pmem = Process.Create("kernel_pmem", stab.GetAddress("_ZN5tysos5tysos7PmemM_0_12TaskFunction_Rv_P1u1t"), 0x1000, arch.VirtualRegions, stab, new object[] { arch.PhysMem });
            //arch.CurrentCpu.CurrentScheduler.Reschedule(kernel_pmem.startup_thread);
            kernel_pmem.started = true;

            Process kernel_idle = Process.Create("kernel_idle", stab.GetAddress("_ZN5tysos5tysos7ProgramM_0_12IdleFunction_Rv_P0"), 0x1000, arch.VirtualRegions, stab, new object[] { });
            kernel_idle.startup_thread.priority = 0;
            arch.CurrentCpu.CurrentScheduler.Reschedule(kernel_idle.startup_thread);
            kernel_idle.started = true;

            /*Process kernel_gc = Process.Create("kernel_gc", stab.GetAddress("_ZN5tysos10tysos#2Egc2gcM_0_16CollectionThread_Rv_P0"), 0x10000, arch.VirtualRegions, stab, new object[] { });
            kernel_gc.startup_thread.priority = 10;
            arch.CurrentCpu.CurrentScheduler.Reschedule(kernel_gc.startup_thread);
            kernel_gc.started = true;*/
        }

        static unsafe libsupcs.TysosMethod GetCurrentMethod()
        {
            ulong ret_address = (ulong)libsupcs.OtherOperations.GetReturnAddress();
            if (Program.stab == null)
                return null;

            ulong offset;
            string sym = Program.stab.GetSymbolAndOffset(ret_address, out offset);
            ulong len = Program.stab.GetLength(sym);
            if(len != 0)
            {
                if (offset >= len)
                    return null;
            }

            ulong meth_addr = Program.stab.GetAddress(sym);
            ulong mi_addr = meth_addr - (ulong)sizeof(void*);

            return libsupcs.TysosType.ReinterpretAsMethodInfo(*(void**)mi_addr);
        }

        class MyEqComparer<T>
        {
            public int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }

            public bool Equals(T x, T y)
            {
                //if (x == null)
                //    return y == null;

                return x.Equals(y);
            }
        }


        private static void test_methinfo()
        {
            System.Reflection.MethodInfo[] meths = typeof(Program).GetMethods();
            Formatter.Write("test_methinfo: returned ", Program.arch.DebugOutput);
            Formatter.Write((ulong)meths.Length, Program.arch.DebugOutput);
            Formatter.WriteLine(" methods", Program.arch.DebugOutput);
            foreach (System.Reflection.MethodInfo meth in meths)
            {
                Formatter.Write(" - ", Program.arch.DebugOutput);
                Formatter.WriteLine(meth.Name, Program.arch.DebugOutput);
            }

            test_invoke();
        }

        private static void test_invoke()
        {
            System.Reflection.MethodInfo stub = typeof(Program).GetMethod("invoke_stub", System.Reflection.BindingFlags.Static);
            if (stub != null)
            {
                Formatter.WriteLine("test_invoke: found stub method - invoking", Program.arch.DebugOutput);
                stub.Invoke(null, null);
                Formatter.WriteLine("test_invoke: completed stub", Program.arch.DebugOutput);
            }
            else
                Formatter.WriteLine("test_invoke: stub not found", Program.arch.DebugOutput);
        }

        private static void invoke_stub()
        {
            Formatter.WriteLine("invoke_stub: in stub", Program.arch.DebugOutput);
        }

        internal static void LoadELFLibrary(string name, Multiboot.Header mboot, SymbolTable stab)
        {
            Formatter.Write("Loading library: ", arch.BootInfoOutput);
            Formatter.Write(name, arch.BootInfoOutput);
            Formatter.Write("... ", arch.BootInfoOutput);

            Multiboot.Module mod = find_module(mboot.modules, name);
            ulong mod_vaddr = map_in(mod);
            ulong load_vaddr = ElfReader.LoadModule(arch.VirtualRegions, arch.VirtMem, stab, mod_vaddr, mod.base_addr, name);
            ElfReader.LoadSymbols(stab, mod_vaddr, load_vaddr);

            Formatter.WriteLine("done", arch.BootInfoOutput);
            Formatter.Write(name, arch.DebugOutput);
            Formatter.Write(" library loaded, load address: ", arch.DebugOutput);
            Formatter.Write(load_vaddr, "X", arch.DebugOutput);
            Formatter.WriteLine(arch.DebugOutput);
        }

        internal static Process LoadELFModule(string name, Multiboot.Header mboot, SymbolTable stab, Dictionary<string, Process> running_processes, ulong stack_size,
            object[] parameters)
        {
            Multiboot.Module mod = find_module(mboot.modules, name);
            if (mod == null)
                throw new Exception("Module: " + name + " not found");
            ulong mod_vaddr = map_in(mod);
            return LoadELFModule(mod_vaddr, mod.length, name, mboot, stab, running_processes, stack_size, parameters);
        }

        internal static Process LoadELFModule(ulong base_addr, ulong len, string name, Multiboot.Header mboot, SymbolTable stab, Dictionary<string, Process> running_processes, ulong stack_size,
            object[] parameters)
        {
            Formatter.Write("Loading module: ", arch.BootInfoOutput);
            Formatter.Write(name, arch.BootInfoOutput);
            Formatter.Write(" from ", arch.BootInfoOutput);
            Formatter.Write(base_addr, "X", arch.BootInfoOutput);
            Formatter.WriteLine(arch.BootInfoOutput);

            ulong e_point = ElfReader.LoadObject(arch.VirtualRegions, arch.VirtMem, stab, base_addr, base_addr, name);

            Process p = Process.Create(name, e_point, stack_size, arch.VirtualRegions, stab, parameters);
            running_processes.Add(name, p);

            Formatter.Write(name, arch.DebugOutput);
            Formatter.Write(" process created, entry point: ", arch.DebugOutput);
            Formatter.Write(e_point, "X", arch.DebugOutput);
            Formatter.WriteLine(arch.DebugOutput);

            return p;
        }

        internal static Process LoadELFProgram(string name, Multiboot.Header mboot, SymbolTable stab, Dictionary<string, Process> running_processes, ulong stack_size)
        { return LoadELFProgram(name, mboot, stab, running_processes, stack_size, true); }
        internal static Process LoadELFProgram(string name, Multiboot.Header mboot, SymbolTable stab, Dictionary<string, Process> running_processes, ulong stack_size, bool start)
        {
            Formatter.Write("Loading program: ", arch.BootInfoOutput);
            Formatter.Write(name, arch.BootInfoOutput);
            Formatter.Write("... ", arch.BootInfoOutput);

            Multiboot.Module mod = find_module(mboot.modules, name);
            ulong mod_vaddr = map_in(mod);
            ulong load_vaddr = ElfReader.LoadModule(arch.VirtualRegions, arch.VirtMem, stab, mod_vaddr, mod.base_addr, name);
            ulong e_point = ElfReader.GetEntryPoint(mod_vaddr, load_vaddr);
            Process p = Process.Create(name, e_point, stack_size, arch.VirtualRegions, stab, new object[] {});

            ElfReader.LoadSymbols(stab, mod_vaddr, load_vaddr);

            running_processes.Add(name, p);
            
            if(start)
                arch.CurrentCpu.CurrentScheduler.Reschedule(p.startup_thread);
            p.started = start;

            Formatter.WriteLine("done", arch.BootInfoOutput);
            Formatter.Write(name, arch.DebugOutput);
            Formatter.Write(" process created, load address: ", arch.DebugOutput);
            Formatter.Write(load_vaddr, "X", arch.DebugOutput);
            Formatter.WriteLine(arch.DebugOutput);

            return p;
        }

        internal static Multiboot.Module find_module(Multiboot.Module[] modules, string name)
        {
            if (modules == null)
                throw new Exception("find_module: 'modules' is null");
            foreach (Multiboot.Module module in modules)
            {
                if (module.name == name)
                    return module;
            }
            return null;
            //throw new System.Exception("Module: " + name + " not found");
        }

        internal static ulong map_in(Multiboot.Module mod)
        {
            if (mod.virt_base_addr != 0)
                return mod.virt_base_addr;
            mod.virt_base_addr = map_in(mod.base_addr, mod.length, mod.name);
            return mod.virt_base_addr;
        }

        internal static ulong map_in(ulong paddr, ulong size, string name)
        { return map_in(paddr, size, name, true, false, false); }

        internal static ulong map_in(ulong paddr, ulong size, string name, bool writeable, bool cache_disable, bool write_through)
        {
            ulong page_offset = paddr & 0xfff;
            ulong start = paddr - page_offset;
            ulong end = util.align(paddr + size, 0x1000);
            ulong length = end - start;
            ulong start_vaddr = arch.VirtualRegions.Alloc(length, 0x1000, name);
            ulong vaddr = start_vaddr + page_offset;
            for (ulong i = 0; i < length; i += 0x1000)
                arch.VirtMem.map_page(start_vaddr + i, start + i, writeable, cache_disable, write_through);
            return vaddr;
        }

        /* The following class is taken from Mono.  mono/corlib/System.Collections.Generic/EqualityComparer.cs
         * Authors: Ben Maurer (bmaurer@ximian.com), Copyright (C) 2004 Novell, Inc under the same license as this file
         * 
         * We need to use our own version of this as EqualityComparer<T> has a static constructor which calls
         * System.Activator, which we don't have yet */
        public class MyGenericEqualityComparer<T> : EqualityComparer<T> where T : System.IEquatable<T>
        {
            public override int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }

            public override bool Equals(T x, T y)
            {
                if (x == null)
                    return y == null;

                return x.Equals(y);
            }
        }

        public class MyComparer<T> : IComparer<T> where T : System.IComparable<T>
        {
            public int Compare(T x, T y)
            {
                return x.CompareTo(y);
            }
        }

        static int indent = 0;
        static bool do_profile = false;

        [libsupcs.MethodAlias("__profile")]
        [libsupcs.AlwaysCompile]
        [libsupcs.Profile(false)]
        static void Profile(string meth_name)
        {
            indent++;

            if (do_profile || ((Program.arch.CurrentCpu != null) && (Program.arch.CurrentCpu.CurrentThread != null) && (Program.arch.CurrentCpu.CurrentThread.do_profile == true)))
            {
                for (int i = 0; i < indent; i++)
                    Program.arch.DebugOutput.Write(' ');
                Program.arch.DebugOutput.Write("Enter: ");
                Program.arch.DebugOutput.Write(meth_name);
                Program.arch.DebugOutput.Write('\n');
            }
        }

        [libsupcs.MethodAlias("__endprofile")]
        [libsupcs.AlwaysCompile]
        [libsupcs.Profile(false)]
        static void EndProfile(string meth_name)
        {
            if (do_profile || ((Program.arch.CurrentCpu != null) && (Program.arch.CurrentCpu.CurrentThread != null) && (Program.arch.CurrentCpu.CurrentThread.do_profile == true)))
            {
                for (int i = 0; i < indent; i++)
                    Program.arch.DebugOutput.Write(' ');
                Program.arch.DebugOutput.Write("Leave: ");
                Program.arch.DebugOutput.Write(meth_name);
                Program.arch.DebugOutput.Write('\n');
            }

            indent--;
        }

        static int next_obj_id = 0x1000;
        [libsupcs.MethodAlias("__get_new_obj_id")]
        [libsupcs.MethodAlias("getobjid")]
        [libsupcs.AlwaysCompile]
        internal static int GetNewObjId()
        {
            // TODO: implement locking on this
            // The unchecked keyword allows the value to wrap round on overflow instead of throwing an exception
            unchecked
            {
                return next_obj_id++;
            }
        }

        [libsupcs.MethodAlias("__undefined_func")]
        [libsupcs.AlwaysCompile]
        static void UndefinedFunc(ulong addr)
        {
            Formatter.Write("Undefined function called from: 0x", arch.BootInfoOutput);
            Formatter.Write(addr, "X", arch.BootInfoOutput);
            Formatter.WriteLine(arch.BootInfoOutput);

            Formatter.Write("Undefined function called from: 0x", arch.DebugOutput);
            Formatter.Write(addr, "X", arch.DebugOutput);
            Formatter.WriteLine(arch.DebugOutput);

            throw new Exception("Undefined function");
        }

        [libsupcs.MethodAlias("__display_halt")]
        [libsupcs.AlwaysCompile]
        static void DisplayHalt()
        {
            Formatter.WriteLine("System halted", arch.BootInfoOutput);
        }

        [libsupcs.MethodAlias("__cxa_pure_virtual")]
        [libsupcs.AlwaysCompile]
        static void CxaPureVirtual()
        {
            Formatter.WriteLine("Pure virtual function called!", arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        [libsupcs.MethodAlias("__exit")]
        [libsupcs.AlwaysCompile]
        static void Exit()
        {
            Program.arch.CurrentCpu.CurrentScheduler.Deschedule(Program.arch.CurrentCpu.CurrentThread);
            libsupcs.OtherOperations.Exit();
        }

        [libsupcs.MethodAlias("exit")]
        [libsupcs.AlwaysCompile]
        [libsupcs.CallingConvention("gnu")]
        static void GC_Exit(int retno)
        {
            Formatter.Write("exit(", arch.BootInfoOutput);
            Formatter.Write((ulong)retno, arch.BootInfoOutput);
            Formatter.WriteLine(") called!", arch.BootInfoOutput);
            libsupcs.OtherOperations.Halt();
        }

        /* The following may be overwritten in native code somewhere */
        [libsupcs.MethodAlias("putchar_debug")]
        [libsupcs.AlwaysCompile]
        [libsupcs.CallingConvention("gnu")]
        [libsupcs.WeakLinkage]
        static byte putchar(byte c)
        {
            Program.arch.DebugOutput.Write((char)c);
            return c;
        }

        [libsupcs.MethodAlias("__get_cur_thread_id")]
        [libsupcs.AlwaysCompile]
        static int GetCurThreadId()
        {
            if (arch.CurrentCpu == null)
                return 1;
            else if (arch.CurrentCpu.CurrentThread == null)
                return 1;
            else
                return arch.CurrentCpu.CurrentThread.thread_id;
        }

        [libsupcs.MethodAlias("_ZW18System#2EThreading6ThreadM_0_22CurrentThread_internal_RV6Thread_P0")]
        [libsupcs.AlwaysCompile]
        static unsafe System.Threading.Thread GetCurThread()
        {
            return StartupThread;
            /* Create a new System.Threading.Thread object */
            /*libsupcs.TysosType thread_type = libsupcs.TysosType.ReinterpretAsType(typeof(System.Threading.Thread));
            object t = libsupcs.MemoryOperations.GcMalloc(new IntPtr(thread_type.GetClassSize()));
            UIntPtr addr = libsupcs.CastOperations.ReinterpretAsUIntPtr(t);
            *(IntPtr*)(libsupcs.OtherOperations.Add(addr, libsupcs.ClassOperations.GetVtblFieldOffset())) = thread_type.VTable;
            *(int *)(libsupcs.OtherOperations.Add(addr, libsupcs.ClassOperations.GetObjectIdFieldOffset())) = GetNewObjId();
            libsupcs.TysosField tid_fld = libsupcs.TysosType.ReinterpretAsFieldInfo(thread_type.GetField("thread_id"));
            *(Int64*)(libsupcs.OtherOperations.Add(addr, new UIntPtr((uint)tid_fld.Offset))) = GetCurThreadId();
            return (System.Threading.Thread)t;*/
        }

        [libsupcs.MethodAlias("_ZX15OtherOperationsM_0_18GetFunctionAddress_Ru1I_P1u1S")]
        [libsupcs.AlwaysCompile]
        static ulong GetFunctionAddress(string name)
        {
            return stab.GetAddress(name);
        }

        [libsupcs.MethodAlias("_ZX15OtherOperationsM_0_22GetStaticObjectAddress_Ru1I_P1u1S")]
        [libsupcs.AlwaysCompile]
        static ulong GetStaticObjectAddress(string name)
        {
            return stab.GetAddress(name);
        }

        [libsupcs.MethodAlias("_ZW18System#2EThreading6ThreadM_0_23GetCachedCurrentCulture_RU22System#2EGlobalization11CultureInfo_P1u1t")]
        [libsupcs.AlwaysCompile]
        static System.Globalization.CultureInfo Thread_GetCachedCurrentCulture(System.Threading.Thread thread)
        {
            /* For now, we just return the invariant culture
             * This is required to get NumberFormatter to work to convert numbers to strings
             */

            return System.Globalization.CultureInfo.InvariantCulture;
        }

        [libsupcs.MethodAlias("__floatundidf")]
        static void floatundidf()
        {
            throw new NotImplementedException("__floatundidf");
        }

        [libsupcs.MethodAlias("__negdf2")]
        static void negdf2()
        {
            throw new NotImplementedException("__negdf2");
        }

        [libsupcs.MethodAlias("__negsf2")]
        static void negsf2()
        {
            throw new NotImplementedException("__negsf2");
        }

        internal static void IdleFunction()
        {
            while (true) ;
        }

        internal static void Panic(string message)
        {
            Formatter.WriteLine(message, arch.DebugOutput);
            Formatter.WriteLine("System Halted", arch.DebugOutput);
            libsupcs.OtherOperations.Halt();
        }

        internal static bool IsCompiled(string obj)
        {
            return stab.sym_to_offset.ContainsKey(obj);
        }

        internal static bool GetCmdLine(string opt)
        {
            if (kernel_cmd_line == null)
                return false;

            foreach (string c in kernel_cmd_line)
            {
                if (c == opt)
                    return true;
            }
            return false;
        }

        [libsupcs.MethodAlias("jit_tm")]
        static IntPtr JitCompile(libsupcs.TysosMethod meth)
        {
            throw new Exception("JIT compilation of dynamic methods not supported");
        }

        [libsupcs.MethodAlias("jit_addrof")]
        static System.IntPtr GetAddressOfObject(string name)
        {
            throw new Exception("Request for address of " + name + ": not yet supported");
        }

        [libsupcs.MethodAlias("__log")]
        static void Log(int level, string category, string message)
        {
            Formatter.WriteLine(message, Program.arch.DebugOutput);
        }
    }
}
