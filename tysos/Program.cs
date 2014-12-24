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
        internal static Cpu cur_cpu_data;
        internal static InterruptMap imap;
        internal static Multiboot.Header mboot_header;
        internal static Ivfs vfs;

        internal static Dictionary<string, Process> running_processes;

        internal static Arch arch;

        internal static string[] kernel_cmd_line;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [libsupcs.ReinterpretAsMethod]
        public static extern Multiboot.Header ReinterpretAsMboot(ulong addr);

        [libsupcs.MethodAlias("kmain")]
        static void KMain(Multiboot.Header mboot)
        {
            // Disable profiling until we have enabled the arch.DebugOutput port
            do_profile = false;

            // Get the multiboot header
            mboot_header = mboot;

            /* Create a temporary heap, then initialize the architecture (default to x86_64 for now)
             * which will set up the permanent heap.  Then initialize the garbage collector */
            ulong heap_start = mboot.heap_start + tysos.x86_64.Arch.GetRecommendedChunkLength();
            //ulong heap_len = mboot.heap_end - heap_start;
            gc.gc.Heap = gc.gc.HeapType.Startup;
            gc.simple_heap.Init(heap_start, mboot.heap_end);

            arch = new tysos.x86_64.Arch();

            UIntPtr chunk_vaddr = new UIntPtr(mboot.heap_start);
            UIntPtr chunk_length = new UIntPtr(tysos.x86_64.Arch.GetRecommendedChunkLength());
            arch.Init(chunk_vaddr, chunk_length, mboot);

            while (true) ;

            // test dynamic types
            if (test_dynamic() == null)
                throw new Exception("test_dynamic failed");
            if (test_dynamic2() == null)
                throw new Exception("test_dynamic2 failed");

            // Say hi
            Formatter.WriteLine("Tysos v0.2.0", arch.BootInfoOutput);
            Formatter.WriteLine("Tysos v0.2.0", arch.DebugOutput);
            Formatter.Write("Command line: ", arch.DebugOutput);
            Formatter.WriteLine(mboot.cmdline, arch.DebugOutput);
            if (mboot.debug)
            {
                Formatter.Write("Kernel debug: ", arch.BootInfoOutput);
                Formatter.WriteLine("Kernel debug requested", arch.DebugOutput);

                if (arch.InitGDBStub())
                {
                    Formatter.WriteLine("enabled", arch.BootInfoOutput);
                    Formatter.WriteLine("Kernel debug started", arch.DebugOutput);
                }
                else
                {
                    Formatter.WriteLine("not supported by current architecture", arch.BootInfoOutput);
                    Formatter.WriteLine("Kernel debug not supported by current architecture", arch.DebugOutput);
                    mboot.debug = false;
                }
            }
         
            /* Map in the ELF image of the kernel, so we can load its symbols */
            ulong tysos_vaddr = map_in(mboot.tysos_paddr, mboot.tysos_size, "tysos binary");

            /* Set up a default environment */
            env = new Environment();
            env.env_vars.Add("OS", "tysos");
            env.env_vars.Add("OSVER", "v0.2.0");
            env.env_vars.Add("NUMBER_OF_PROCESSORS", "1");

#if NO_BOEHM
            gc.heap_arena.debug = true;
#endif  // NO_BOEHM


            /* Trigger a breakpoint to synchronize with gdb */
            if (mboot.debug)
            {
                Formatter.WriteLine("Synchronizing with debugger...", arch.BootInfoOutput);
                System.Diagnostics.Debugger.Break();
            }

            /* Parse the kernel command line */
            kernel_cmd_line = mboot.cmdline.Split(' ');

            /* Load up the symbol table for tysos */
            if (GetCmdLine("skip_kernel_syms") == false)
            {
                stab = new SymbolTable();
                Formatter.Write("Loading kernel symbols... ", arch.BootInfoOutput);
                Formatter.Write("Loading kernel symbols.  Tysos base: ", arch.DebugOutput);
                Formatter.Write(mboot.tysos_virtaddr, "X", arch.DebugOutput);
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
                }

                ElfReader.LoadSymbols(stab, tysos_vaddr, mboot.tysos_virtaddr, khash_addr);
                Formatter.WriteLine("done", arch.BootInfoOutput);

                /* Test symbol table */
                ulong gcmalloc_addr = stab.GetAddress("gcmalloc");
                Formatter.Write("Testing symbol table, address of gcmalloc: ", arch.DebugOutput);
                Formatter.Write(gcmalloc_addr, "X", arch.DebugOutput);
                Formatter.WriteLine(arch.DebugOutput);
            }

            /* Test the garbage collector */
            if (GetCmdLine("skip_test_gc") == false)
            {
                Formatter.Write("Testing garbage collector... ", arch.BootInfoOutput);
                gc.gc.DoCollection();
                Formatter.WriteLine("done", arch.BootInfoOutput);
            }

            /* Start the scheduler */
            cur_cpu_data.CurrentScheduler = new Scheduler();
            arch.SchedulerTimer.Callback = new Timer.TimerCallback(Scheduler.TimerProc);

            /* Store the process info */
            running_processes = new Dictionary<string, Process>(new MyGenericEqualityComparer<string>());


            /* Load tysila */
            Formatter.Write("libtysila version ", arch.DebugOutput);
            Formatter.Write(libtysila.Assembler.VersionString, arch.DebugOutput);
            Formatter.Write(", supported architectures: ", arch.DebugOutput);
            libtysila.Assembler.Architecture[] archs = libtysila.Assembler.ListArchitectures();
            for (int i = 0; i < archs.Length; i++)
            {
                if (i != 0)
                    Formatter.Write(", ", arch.DebugOutput);
                Formatter.Write(archs[i].ToString(), arch.DebugOutput);
            }
            Formatter.WriteLine(arch.DebugOutput);

            /* Test enum dictionaries */
            //test_hcp();
            //test_my_hcp();
            //test_dict();

            // Test the assembler
            // First start the garbage collector thread
            //CreateKernelThreads();

            // Now add the test thread
            Process test_thread = Process.Create("test_thread", stab.GetAddress("_ZN5tysos5tysos7ProgramM_0_19TestAssemblerThread_Rv_P0"), 0x8000, arch.VirtualRegions, stab, new object[] { });
            cur_cpu_data.CurrentScheduler.Reschedule(test_thread.startup_thread);
            test_thread.started = true;

            // Enable multitasking
            arch.EnableMultitasking();
            while (true) ;



            // Halt here
            libsupcs.OtherOperations.Halt();


            /* Load the gui */
            Process gui = LoadELFProgram("gui", mboot, stab, running_processes, 0x8000);
            gui.startup_thread.priority++;      // Elevate the priority of the gui

            /* Load the vfs */
            Process vfs = LoadELFProgram("vfs", mboot, stab, running_processes, 0x8000);
            vfs.startup_thread.priority++;      // Elevate the priority of the vfs

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

        private static void TestAssemblerThread()
        {
            /* First create the mini assembler for jit stubs etc */
            string assembler_arch = "x86_64-jit-tysos";
            Formatter.Write("Creating mini assembler... ", Program.arch.DebugOutput);
            libtysila.MiniAssembler mass = libtysila.MiniAssembler.GetMiniAssembler(assembler_arch);
            Formatter.WriteLine("done", Program.arch.DebugOutput);

            /* Create an x86_64 assembler */
            Formatter.Write("Initialising JIT assembler for " + assembler_arch + "... ", Program.arch.BootInfoOutput);
            Formatter.Write("Creating assembler... ", Program.arch.DebugOutput);
            libtysila.Assembler.MemberRequestor requestor = new jit.JitMemberRequestor();
            libtysila.Assembler.FileLoader loader = new jit.JitFileLoader();
            libtysila.IOutputFile output = new jit.JitOutput();
            libtysila.Assembler ass = libtysila.Assembler.CreateAssembler(libtysila.Assembler.ParseArchitectureString(assembler_arch), loader, requestor, null);
            requestor.Assembler = ass;
            ass.debugOutput = new DebugOutput();
            Formatter.WriteLine("done", Program.arch.DebugOutput);
            Formatter.WriteLine("done", Program.arch.BootInfoOutput);

            /* Test the JIT compiler */
            Formatter.WriteLine("Testing assembler: ", Program.arch.DebugOutput);
            libtysila.Metadata module = ass.FindAssembly("test_002");
            if (module != null)
            {
                Formatter.WriteLine("  Test_002 found", Program.arch.DebugOutput);
                libtysila.Assembler.MethodToCompile? mtc = module.GetEntryPoint(ass);
                if (mtc.HasValue)
                {
                    Formatter.WriteLine("  Entry point found", Program.arch.DebugOutput);
                    ass.AssembleMethod(mtc.Value, output, null);
                    Formatter.WriteLine("  Method assembled", Program.arch.DebugOutput);
                }
                else
                    Formatter.WriteLine("  Entry point not found", Program.arch.DebugOutput);
            }
            else
                Formatter.WriteLine("  Test_002 not found", Program.arch.DebugOutput);
        }

        private static void CreateKernelThreads()
        {
            Process kernel_pmem = Process.Create("kernel_pmem", stab.GetAddress("_ZN5tysos5tysos7PmemM_0_12TaskFunction_Rv_P1u1t"), 0x1000, arch.VirtualRegions, stab, new object[] { arch.PhysMem });
            //cur_cpu_data.CurrentScheduler.Reschedule(kernel_pmem.startup_thread);
            kernel_pmem.started = true;

            Process kernel_idle = Process.Create("kernel_idle", stab.GetAddress("_ZN5tysos5tysos7ProgramM_0_12IdleFunction_Rv_P0"), 0x1000, arch.VirtualRegions, stab, new object[] { });
            kernel_idle.startup_thread.priority = 0;
            cur_cpu_data.CurrentScheduler.Reschedule(kernel_idle.startup_thread);
            kernel_idle.started = true;

            /*Process kernel_gc = Process.Create("kernel_gc", stab.GetAddress("_ZN5tysos10tysos#2Egc2gcM_0_16CollectionThread_Rv_P0"), 0x10000, arch.VirtualRegions, stab, new object[] { });
            kernel_gc.startup_thread.priority = 10;
            cur_cpu_data.CurrentScheduler.Reschedule(kernel_gc.startup_thread);
            kernel_gc.started = true;*/
        }

        private static libtysila.Metadata.TypeDefRow[] test_dynamic()
        {
            System.Type tdr_type = typeof(libtysila.Metadata.TypeDefRow);
            return (libtysila.Metadata.TypeDefRow[])tdr_type.MakeArrayType().InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, new object[] { 1 });
        }

        private static libtysila.Metadata.ITableRow[] test_dynamic2()
        {
            System.Type tdr_type = typeof(libtysila.Metadata.TypeDefRow);
            return (libtysila.Metadata.ITableRow[])tdr_type.MakeArrayType().InvokeMember(null, System.Reflection.BindingFlags.CreateInstance, null, null, new object[] { 1 });
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


        private static void test_my_hcp()
        {
            MyEqComparer<libtysila.ThreeAddressCode.OpName> hcp = new MyEqComparer<libtysila.ThreeAddressCode.OpName>();

            int hc = hcp.GetHashCode(libtysila.ThreeAddressCode.OpName.label);
            Formatter.WriteLine("test_my_hcp: hash code 1: " + hc.ToString(), Program.arch.DebugOutput);
            hc = hcp.GetHashCode(libtysila.ThreeAddressCode.OpName.ldarga);
            Formatter.WriteLine("test_my_hcp: hash code 2: " + hc.ToString(), Program.arch.DebugOutput);
            hc = hcp.GetHashCode(libtysila.ThreeAddressCode.OpName.label);
            Formatter.WriteLine("test_my_hcp: hash code 3: " + hc.ToString(), Program.arch.DebugOutput);

            bool t1 = hcp.Equals(libtysila.ThreeAddressCode.OpName.label, libtysila.ThreeAddressCode.OpName.ldarga);
            Formatter.WriteLine("test_my_hcp: should be false: " + t1.ToString(), Program.arch.DebugOutput);

            bool t2 = hcp.Equals(libtysila.ThreeAddressCode.OpName.label, libtysila.ThreeAddressCode.OpName.label);
            Formatter.WriteLine("test_my_hcp: should be true: " + t2.ToString(), Program.arch.DebugOutput);
        }

        private static void test_hcp()
        {
            EqualityComparer<libtysila.ThreeAddressCode.OpName> hcp = EqualityComparer<libtysila.ThreeAddressCode.OpName>.Default;

            int hc = hcp.GetHashCode(libtysila.ThreeAddressCode.OpName.label);
            Formatter.WriteLine("test_hcp: hash code 1: " + hc.ToString(), Program.arch.DebugOutput);
            hc = hcp.GetHashCode(libtysila.ThreeAddressCode.OpName.ldarga);
            Formatter.WriteLine("test_hcp: hash code 2: " + hc.ToString(), Program.arch.DebugOutput);
            hc = hcp.GetHashCode(libtysila.ThreeAddressCode.OpName.label);
            Formatter.WriteLine("test_hcp: hash code 3: " + hc.ToString(), Program.arch.DebugOutput);

            bool t1 = hcp.Equals(libtysila.ThreeAddressCode.OpName.label, libtysila.ThreeAddressCode.OpName.ldarga);
            Formatter.WriteLine("test_hcp: should be false: " + t1.ToString(), Program.arch.DebugOutput);

            bool t2 = hcp.Equals(libtysila.ThreeAddressCode.OpName.label, libtysila.ThreeAddressCode.OpName.label);
            Formatter.WriteLine("test_hcp: should be true: " + t2.ToString(), Program.arch.DebugOutput);
        }

        private static void test_dict()
        {
            Dictionary<libtysila.ThreeAddressCode.OpName, int> d = new Dictionary<libtysila.ThreeAddressCode.OpName, int>();

            // Add an item
            d.Add(libtysila.ThreeAddressCode.OpName.label, 6);

            // Check an item
            int val = d[libtysila.ThreeAddressCode.OpName.label];
            Formatter.WriteLine("test_dict: " + val.ToString(), Program.arch.DebugOutput);
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
                cur_cpu_data.CurrentScheduler.Reschedule(p.startup_thread);
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
            foreach (Multiboot.Module module in modules)
            {
                if (module.name == name)
                    return module;
            }
            return null;
            //throw new System.Exception("Module: " + name + " not found");
        }

        internal static ulong map_in(Multiboot.Module mod)
        { return map_in(mod.base_addr, mod.length, mod.name); }

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

            if (do_profile || ((Program.cur_cpu_data != null) && (Program.cur_cpu_data.CurrentThread != null) && (Program.cur_cpu_data.CurrentThread.do_profile == true)))
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
            if (do_profile || ((Program.cur_cpu_data != null) && (Program.cur_cpu_data.CurrentThread != null) && (Program.cur_cpu_data.CurrentThread.do_profile == true)))
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
            Program.cur_cpu_data.CurrentScheduler.Deschedule(Program.cur_cpu_data.CurrentThread);
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

        [libsupcs.MethodAlias("putchar_debug")]
        [libsupcs.AlwaysCompile]
        [libsupcs.CallingConvention("gnu")]
        static byte putchar(byte c)
        {
            Program.arch.DebugOutput.Write((char)c);
            return c;
        }

        [libsupcs.MethodAlias("__get_cur_thread_id")]
        [libsupcs.AlwaysCompile]
        static int GetCurThreadId()
        {
            if (cur_cpu_data == null)
                return 1;
            else if (cur_cpu_data.CurrentThread == null)
                return 1;
            else
                return cur_cpu_data.CurrentThread.thread_id;
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

        class DebugOutput : libtysila.Assembler.DebugOutput
        {
            const int DBG_LEVEL = libtysila.Assembler.DebugOutput.DBG_DEBUG;

            public override void Write(string s, int level)
            {
                if (level <= DBG_LEVEL)
                    Formatter.Write(s, Program.arch.DebugOutput);
            }

            public override void WriteLine(string s, int level)
            {
                if (level <= DBG_LEVEL)
                    Formatter.WriteLine(s, Program.arch.DebugOutput);
            }
        }
    }
}
