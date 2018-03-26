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
        internal static Multiboot.Header mboot_header;
        internal static ServerObject Vfs, Logger, Gui, Net;

        internal static Dictionary<string, Process> running_processes;

        internal static Arch arch;

        internal static System.Threading.Thread StartupThread;

        internal static string[] kernel_cmd_line;

        [libsupcs.FieldAlias("_tysos_hash")]
        static IntPtr tysos_hash;

        [libsupcs.MethodAlias("kmain")]
        [libsupcs.Profile(false)]
        public static void KMain(Multiboot.Header mboot)
        {
            // Disable profiling until we have enabled the arch.DebugOutput port
            do_profile = false;

            // Get the multiboot header
            mboot_header = mboot;

            /* Create a temporary heap, then initialize the architecture
             * which will set up the permanent heap.  Then initialize the garbage collector */
            ulong arch_data_length = 0;
            switch (mboot.machine_major_type)
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

            log_lock = new object();

            /* Set up the default startup thread */
            StartupThread = new System.Threading.Thread(null_func);

            /* Initialize the architecture */
            UIntPtr chunk_vaddr = new UIntPtr(mboot.heap_start);
            UIntPtr chunk_length = new UIntPtr(arch_data_length);

            switch (mboot.machine_major_type)
            {
                case (uint)Multiboot.MachineMajorType.x86_64:
                    //libsupcs.OtherOperations.AsmBreakpoint();
                    arch = new tysos.x86_64.Arch();
                    break;
            }
            arch.Init(chunk_vaddr, chunk_length, mboot);

            do_profile = true;

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
            Formatter.Write("mboot @ ", arch.DebugOutput);
            Formatter.Write(libsupcs.CastOperations.ReinterpretAsUlong(mboot), "X", arch.DebugOutput);
            Formatter.WriteLine(arch.DebugOutput);
            Formatter.Write("Loaded by ", arch.DebugOutput);
            Formatter.WriteLine(mboot.loader_name, arch.DebugOutput);
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
                Formatter.WriteLine("Synchronizing with debugger...", arch.DebugOutput);
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
                ulong sym_vaddr = Program.map_in(mboot.tysos_sym_tab_paddr, mboot.tysos_sym_tab_size,
                    "tysos_sym_tab");
                ulong str_vaddr = Program.map_in(mboot.tysos_str_tab_paddr, mboot.tysos_str_tab_size,
                    "tysos_str_tab");

                stab = new SymbolTable();
                Formatter.Write("Loading kernel symbols... ", arch.BootInfoOutput);
                Formatter.Write("Loading kernel symbols.  Tysos base: ", arch.DebugOutput);
                Formatter.Write(tysos_vaddr, "X", arch.DebugOutput);
                Formatter.WriteLine(arch.DebugOutput);

                var hr = new ElfReader.ElfHashTable((ulong)tysos_hash, sym_vaddr, mboot.tysos_sym_tab_entsize, str_vaddr,
                    null, 0, mboot.tysos_sym_tab_size);
                stab.symbol_providers.Add(hr);

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
            Formatter.Write("Starting scheduler... ", arch.DebugOutput);
            arch.CurrentCpu.CurrentScheduler = new Scheduler();
            if (GetCmdLine("ignore_timer") == false)
                arch.SchedulerTimer.Callback = new Timer.TimerCallback(Scheduler.TimerProc);
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Store the process info */
            running_processes = new Dictionary<string, Process>(new MyGenericEqualityComparer<string>());

            /* Add in threads for GC collections */
            Formatter.Write("Starting GC collection threads... ", arch.DebugOutput);
            Thread t_max = Thread.Create("gc_max_alloc", new System.Threading.ThreadStart(gc.gengc.MaxAllocCollectThreadProc),
                new object[] { });
            t_max.priority = 10;
            arch.CurrentCpu.CurrentScheduler.Reschedule(t_max);

            Thread t_min = Thread.Create("gc_min_alloc", new System.Threading.ThreadStart(gc.gengc.MinAllocCollectThreadProc),
                new object[] { });
            t_min.priority = 0;
            arch.CurrentCpu.CurrentScheduler.Reschedule(t_min);
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Init vfs signatures */
            lib.File.InitSigs();

            /* Build a list of available modules */
            Formatter.Write("Building module list... ", arch.DebugOutput);
            List<tysos.lib.File.Property> mods = new List<tysos.lib.File.Property>();
            foreach (Multiboot.Module mod in mboot.modules)
            {
                if (mod.length != 0)
                {
                    ulong vaddr = map_in(mod);
                    mods.Add(new tysos.lib.File.Property { Name = mod.name, Value = new VirtualMemoryResource64(vaddr, mod.length) });
                }
            }
            List<tysos.lib.File.Property> modfs_props = new List<lib.File.Property>();
            modfs_props.Add(new lib.File.Property { Name = "driver", Value = "modfs" });
            modfs_props.Add(new lib.File.Property { Name = "mods", Value = mods });
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Load the logger */
            Formatter.Write("Starting logger... ", arch.DebugOutput);
            Process logger = LoadELFModule("logger", mboot, stab, running_processes, 0x8000,
                new object[] { });

            Process debugprint = LoadELFModule("debugprint", mboot, stab, running_processes,
                0x8000, new object[] { });

            logger.Start();
            //debugprint.Start();
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Load the vfs */
            Formatter.Write("Starting vfs... ", arch.DebugOutput);
            Process vfs = LoadELFModule("vfs", mboot, stab, running_processes, 0x8000, new object[] { });
            vfs.Start();
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Load the gui */
            Formatter.Write("Starting gui... ", arch.DebugOutput);
            Process gui = LoadELFModule("gui", mboot, stab, running_processes, 0x8000, new object[] { });
            gui.Start();
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Load the network subsystem */
            Formatter.Write("Starting net... ", arch.DebugOutput);
            Process net = LoadELFModule("net", mboot, stab, running_processes, 0x8000, new object[] { });
            net.Start();
            Formatter.WriteLine("done", arch.DebugOutput);

            /* Load and mount the root fs */
            List<lib.File.Property> system_props = arch.SystemProperties;
            List<Resources.InterruptLine> interrupts = new List<Resources.InterruptLine>();
            // TODO: add interrupts for all cpus
            interrupts.AddRange(arch.CurrentCpu.Interrupts);
            system_props.Add(new lib.File.Property { Name = "interrupts", Value = interrupts });
            List<rootfs.rootfs_item> rootfs_items = new List<rootfs.rootfs_item>
            {
                new rootfs.rootfs_item { Name = "system", Props = system_props },
                new rootfs.rootfs_item { Name = "modules", Props = modfs_props }
            };

            /* Add a basic framebuffer device if set up by bootloader */
            if (mboot.fb_base != 0)
            {
                List<lib.File.Property> fb_props = new List<lib.File.Property>();
                fb_props.Add(new lib.File.Property { Name = "driver", Value = "framebuffer" });
                ulong fb_length = mboot.fb_stride * mboot.fb_h * mboot.fb_bpp / 8;
                if ((fb_length & 0xfffUL) != 0)
                {
                    fb_length += 0x1000UL;
                    fb_length &= ~0xfffUL;
                }
                fb_props.Add(new lib.File.Property { Name = "pmem", Value = new PhysicalMemoryResource64(mboot.fb_base, fb_length) });
                fb_props.Add(new lib.File.Property { Name = "height", Value = mboot.fb_h });
                fb_props.Add(new lib.File.Property { Name = "width", Value = mboot.fb_w });
                fb_props.Add(new lib.File.Property { Name = "stride", Value = mboot.fb_stride });
                fb_props.Add(new lib.File.Property { Name = "bpp", Value = mboot.fb_bpp });
                fb_props.Add(new lib.File.Property { Name = "pformat", Value = (int)mboot.fb_pixelformat });

                fb_props.Add(new lib.File.Property
                {
                    Name = "vmem",
                    Value = new VirtualMemoryResource64(arch.VirtualRegions.Alloc(fb_length, 0x1000, "framebuffer"), fb_length)
                });
                rootfs_items.Add(new rootfs.rootfs_item { Name = "framebuffer", Props = fb_props });
            }

            rootfs rootfs = new rootfs(rootfs_items);
            Process rootfs_p = Process.CreateProcess("rootfs",
                new System.Threading.ThreadStart(rootfs.MessageLoop), new object[] { rootfs });
            rootfs_p.Start();
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/", rootfs },
                new Type[] { typeof(string), typeof(ServerObject) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/modules" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/framebuffer" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system/pci_hostbridge_0" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system/pci_hostbridge_0/pcnet32_0" }, new Type[] { typeof(string) });
            /*ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system/pci_hostbridge_0/bga_0" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system/pci_hostbridge_0/pciide_0" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system/pci_hostbridge_0/pciide_0/ata_0" }, new Type[] { typeof(string) });
            ServerObject.InvokeRemoteAsync(vfs, "Mount", new object[] { "/system/pci_hostbridge_0/pciide_0/ata_0/device_0" }, new Type[] { typeof(string) });*/

            /* Load the modfs driver */
            Process modfs = LoadELFModule("modfs", mboot, stab, running_processes, 0x8000, new object[] { });
            modfs.Start();

            if (do_debug)
                System.Diagnostics.Debugger.Break();

            arch.EnableMultitasking();
            Syscalls.SchedulerFunctions.Yield();

            while (true) ;



            // Halt here
            libsupcs.OtherOperations.Halt();


            /* Create kernel threads */
            CreateKernelThreads();

            /* Dump the current virtual region table */
            arch.VirtualRegions.Dump(arch.DebugOutput);



            /* Start the processes */
            Formatter.WriteLine("Going multitasking...", arch.BootInfoOutput);
            arch.EnableMultitasking();
            while (true) ;
        }

        static string str_array_test(string[] val)
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
            Process kernel_pmem = Process.Create("kernel_pmem", stab.GetAddress("_ZN5tysos5tysos7Pmem_12TaskFunction_Rv_P1u1t"), 0x1000, arch.VirtualRegions, stab, new object[] { arch.PhysMem }, Program.arch.tysos_tls_length);
            //arch.CurrentCpu.CurrentScheduler.Reschedule(kernel_pmem.startup_thread);
            kernel_pmem.started = true;

            Process kernel_idle = Process.Create("kernel_idle", stab.GetAddress("_ZN5tysos5tysos7Program_12IdleFunction_Rv_P0"), 0x1000, arch.VirtualRegions, stab, new object[] { }, Program.arch.tysos_tls_length);
            kernel_idle.startup_thread.priority = 0;
            arch.CurrentCpu.CurrentScheduler.Reschedule(kernel_idle.startup_thread);
            kernel_idle.started = true;

            /*Process kernel_gc = Process.Create("kernel_gc", stab.GetAddress("_ZN5tysos10tysos#2Egc2gc_16CollectionThread_Rv_P0"), 0x10000, arch.VirtualRegions, stab, new object[] { });
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
            if (len != 0)
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
            if (mod == null || mod.length == 0)
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

            ulong e_point = ElfReader.LoadObject(arch.VirtualRegions, arch.VirtMem, stab, base_addr, base_addr, name, out var tls_size);

            Process p = Process.Create(name, e_point, stack_size, arch.VirtualRegions, stab, parameters, tls_size);

            Formatter.Write(name, arch.DebugOutput);
            Formatter.Write(" process created, entry point: ", arch.DebugOutput);
            Formatter.Write(e_point, "X", arch.DebugOutput);
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

        [libsupcs.MethodAlias("profile")]
        [libsupcs.AlwaysCompile]
        [libsupcs.Profile(false)]
        static unsafe void Profile(string meth_name, char* c_str, int c_str_len,
            int is_leave)
        {
            if (do_profile)
            {
                var state = libsupcs.OtherOperations.EnterUninterruptibleSection();

                Formatter.Write("PROFILE: ", arch.DebugOutput);

                if (is_leave == 0)
                    indent++;

                for (int i = 0; i < indent; i++)
                    Formatter.Write(" ", arch.DebugOutput);

                if (is_leave == 0)
                    Formatter.Write("ENTER: ", arch.DebugOutput);
                else
                    Formatter.Write("LEAVE: ", arch.DebugOutput);

                // Do this to avoid any calls to Length/Item on System.String
                for (int i = 0; i < c_str_len; i++)
                    Formatter.Write(c_str[i], arch.DebugOutput);

                Formatter.Write(" @ ", arch.DebugOutput);
                Formatter.Write(arch.GetMonotonicCount, arch.DebugOutput);
                Formatter.WriteLine(arch.DebugOutput);

                if (is_leave == 1)
                    indent--;

                libsupcs.OtherOperations.ExitUninterruptibleSection(state);
            }
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

        [libsupcs.MethodAlias("missing_function")]
        [libsupcs.AlwaysCompile]
        static void MissingFunction(string name)
        {
            if (arch != null && arch.DebugOutput != null)
            {
                Formatter.Write("Undefined function called: ", arch.DebugOutput);
                Formatter.Write(name, arch.DebugOutput);
                Formatter.WriteLine(arch.DebugOutput);
            }
            throw new Exception("Undefined function");
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
            if (arch != null && arch.BootInfoOutput != null)
                Formatter.WriteLine("System halted", arch.BootInfoOutput);
        }

        [libsupcs.MethodAlias("__cxa_pure_virtual")]
        [libsupcs.AlwaysCompile]
        static void CxaPureVirtual()
        {
            System.Diagnostics.Debugger.Break();
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
        [libsupcs.WeakLinkage]
        static byte putchar(byte c)
        {
            Program.arch.DebugOutput.Write((char)c);
            return c;
        }

        [libsupcs.MethodAlias("__get_cur_thread_id")]
        [libsupcs.AlwaysCompile]
        static internal int GetCurThreadId()
        {
            if (arch.CurrentCpu == null)
                return 1;
            else if (arch.CurrentCpu.CurrentThread == null)
                return 1;
            else
                return arch.CurrentCpu.CurrentThread.thread_id;
        }

        [libsupcs.MethodAlias("_ZW18System#2EThreading6Thread_19get_ManagedThreadId_Ri_P1u1t")]
        [libsupcs.AlwaysCompile]
        unsafe static int get_ManagedThreadId(System.Threading.Thread t)
        {
            if (arch == null || arch.CurrentCpu == null || arch.CurrentCpu.CurrentThread == null)
                return 1;

            var tt = Thread.GetTysosThread(t);
            return tt.thread_id;
        }

        [libsupcs.MethodAlias("_ZW18System#2EThreading6Thread_22GetCurrentThreadNative_RV6Thread_P0")]
        [libsupcs.AlwaysCompile]
        static unsafe System.Threading.Thread GetCurThread()
        {
            if(arch == null || arch.CurrentCpu == null || arch.CurrentCpu.CurrentThread == null)
                return StartupThread;

            return arch.CurrentCpu.CurrentThread.mt;
            /* TODO: Create a new System.Threading.Thread object */
            /*libsupcs.TysosType thread_type = libsupcs.TysosType.ReinterpretAsType(typeof(System.Threading.Thread));
            object t = libsupcs.MemoryOperations.GcMalloc(new IntPtr(thread_type.GetClassSize()));
            UIntPtr addr = libsupcs.CastOperations.ReinterpretAsUIntPtr(t);
            *(IntPtr*)(libsupcs.OtherOperations.Add(addr, libsupcs.ClassOperations.GetVtblFieldOffset())) = thread_type.VTable;
            *(int *)(libsupcs.OtherOperations.Add(addr, libsupcs.ClassOperations.GetObjectIdFieldOffset())) = GetNewObjId();
            libsupcs.TysosField tid_fld = libsupcs.TysosType.ReinterpretAsFieldInfo(thread_type.GetField("thread_id"));
            *(Int64*)(libsupcs.OtherOperations.Add(addr, new UIntPtr((uint)tid_fld.Offset))) = GetCurThreadId();
            return (System.Threading.Thread)t;*/
        }

        [libsupcs.MethodAlias("_ZX15OtherOperations_18GetFunctionAddress_Ru1I_P1u1S")]
        [libsupcs.AlwaysCompile]
        static ulong GetFunctionAddress(string name)
        {
            return stab.GetAddress(name);
        }

        [libsupcs.MethodAlias("_ZX15OtherOperations_22GetStaticObjectAddress_Ru1I_P1u1S")]
        [libsupcs.AlwaysCompile]
        static ulong GetStaticObjectAddress(string name)
        {
            return stab.GetAddress(name);
        }

        [libsupcs.MethodAlias("_ZW18System#2EThreading6Thread_23GetCachedCurrentCulture_RU22System#2EGlobalization11CultureInfo_P1u1t")]
        [libsupcs.AlwaysCompile]
        static System.Globalization.CultureInfo Thread_GetCachedCurrentCulture(System.Threading.Thread thread)
        {
            /* For now, we just return the invariant culture
             * This is required to get NumberFormatter to work to convert numbers to strings
             */

            return System.Globalization.CultureInfo.InvariantCulture;
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

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("jit_nameof")]
        unsafe static string GetNameOfAddress(void *addr, out void *offset)
        {
            ulong o;
            var ret = stab.GetSymbolAndOffset((ulong)addr, out o);
            offset = (void*)o;
            return ret;
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("jit_tm")]
        static IntPtr JitCompile(libsupcs.TysosMethod meth)
        {
            throw new Exception("JIT compilation of dynamic methods not supported");
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("jit_addrof")]
        static System.IntPtr GetAddressOfObject(string name)
        {
            var ret = stab.GetAddress(name);
            System.Diagnostics.Debugger.Log(0, "jit_addrof", "Request for " + name + ": returning " + ret.ToString("X"));
            return (IntPtr)ret;
        }

        static object log_lock;
        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("__log")]
        static void Log(int level, string category, string message)
        {
            if (arch == null || arch.DebugOutput == null)
                return;

            var state = libsupcs.OtherOperations.EnterUninterruptibleSection();

            if (category == null && arch.CurrentCpu != null && arch.CurrentCpu.CurrentProcess != null)
                category = arch.CurrentCpu.CurrentProcess.name;
            if (category == null && arch.CurrentCpu != null && arch.CurrentCpu.CurrentThread != null)
                category = arch.CurrentCpu.CurrentThread.name;
            if (category == null)
                category = "unknown";
            if (message == null)
                message = "<null>";

            Formatter.Write("[", arch.DebugOutput);
            Formatter.Write(arch.GetMonotonicCount, arch.DebugOutput);
            if (arch.CurrentCpu == null || arch.CurrentCpu.CurrentThread == null || arch.CurrentCpu.CurrentThread.name == null)
                Formatter.Write(": kernel(1)", arch.DebugOutput);
            else
            {
                Formatter.Write(": ", arch.DebugOutput);
                Formatter.Write(arch.CurrentCpu.CurrentThread.name, arch.DebugOutput);
                Formatter.Write("(", arch.DebugOutput);
                Formatter.Write((ulong)arch.CurrentCpu.CurrentThread.thread_id, arch.DebugOutput);
                Formatter.Write(")", arch.DebugOutput);
            }
            Formatter.Write("] ", arch.DebugOutput);
            Formatter.Write(category, arch.DebugOutput);
            Formatter.Write(": ", arch.DebugOutput);
            Formatter.Write(message, arch.DebugOutput);
            if (level == 0)
                Formatter.WriteLine(arch.DebugOutput);
            else
            {
                Formatter.Write(" (", arch.DebugOutput);
                Formatter.Write((ulong)level, arch.DebugOutput);
                Formatter.WriteLine(")", arch.DebugOutput);
            }

            libsupcs.OtherOperations.ExitUninterruptibleSection(state);
        }
    }
}