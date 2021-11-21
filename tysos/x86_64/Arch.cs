﻿/* Copyright (C) 2011 - 2015 by John Cronin
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
using tysos.lib;

namespace tysos.x86_64
{
    partial class Arch : tysos.Arch
    {
        ulong bda_va;
        ulong pmem_bitmap_va;
        ulong vmem_temppage_va;
        ulong vga_fb_va;

        ulong buf_cur;
        ulong buf_end;

        ulong init_exit_address;

        const ulong heap_small_start = 0xffff800000000000;
        const ulong heap_small_end = 0xffffc00000000000;
        const ulong heap_long_start = 0xffffc00000000000;
        const ulong heap_long_end = 0xffffff8000000000;
        const ulong heap_small_cutoff = 512;

        bool multitasking = false;

        bool cpu_structure_setup = false;

        FirmwareConfiguration fwconf = null;
        List<tysos.lib.File.Property> ps;

        internal abstract unsafe class FirmwareConfiguration
        {
            internal abstract void* ACPI_20_table { get; }
            internal abstract void* ACPI_10_table { get; }
            internal abstract void* SMBIOS_table { get; }
        }

        internal override bool Multitasking
        {
            get { return multitasking; }
        }

        internal override string AssemblerArchitecture
        {
            get { return "x86_64-jit-tysos"; }
        }

        internal override ulong ExitAddress
        {
            get
            {
                if (Program.arch.CurrentCpu == null)
                    return init_exit_address;
                if (Program.arch.CurrentCpu.CurrentThread == null)
                    return init_exit_address;
                return Program.arch.CurrentCpu.CurrentThread.exit_address;
            }
        }

        internal override tysos.TaskSwitchInfo CreateTaskSwitchInfo()
        {
            x86_64.TaskSwitchInfo ret = new TaskSwitchInfo();
            ret.send_eoi = Program.stab.GetAddress("__lapic_eoi");
            return ret;
        }

        internal static ulong GetRecommendedChunkLength()
        {
            /* We need 1 page for the bda, one for the pmem bitmap, one for the 
             * virtual memory temporary page and one for the vga frame buffer,
             * and then some heap space */
            return 0x5000;
        }

        internal override ulong PageSize
        {
            get { return 0x1000; }
        }

        internal override int PointerSize
        {
            get { return 8; }
        }

        private bool IsUefiFreeMemory(Multiboot.MemoryMapType mtype)
        {
            switch (mtype)
            {
                case Multiboot.MemoryMapType.UEfiConventionalMemory:
                case Multiboot.MemoryMapType.UEfiBootServicesCode:
                case Multiboot.MemoryMapType.UEfiBootServicesData:
                case Multiboot.MemoryMapType.UEfiRuntimeServicesCode:
                case Multiboot.MemoryMapType.UEfiRuntimeServicesData:
                    return true;

                default:
                    return false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        [libsupcs.ReinterpretAsMethod]
        public unsafe static extern Cpu ReinterpretAsCpu(void* addr);

        internal unsafe override Cpu CurrentCpu
        {
            get
            {
                if (cpu_structure_setup == false)
                    return null;
                void* cpu_ptr = libsupcs.x86_64.Cpu.ReadGSData(0);
                return ReinterpretAsCpu(cpu_ptr);
            }
        }

        internal override void Init(UIntPtr chunk_vaddr, UIntPtr chunk_length, Multiboot.Header mboot)
        {
            /* Set up an x86_64 environment */

            /* Get the top of the stack
             * 
             * cur_rbp points to prev_rbp (rbp within Main)
             * [prev_rbp + 8] is the rip within tload
             */

            unsafe
            {
                ulong cur_rbp = libsupcs.x86_64.Cpu.RBP;
                ulong prev_rbp = *(ulong*)cur_rbp;
                init_exit_address = *(ulong*)(prev_rbp + 8);
            }

            // Enable sse (used for virtftnptr stores and loads) - see Intel 3a:13.1.4
            ulong cr4 = libsupcs.x86_64.Cpu.Cr4;
            cr4 |= 0x200;       // set cr4.OSFXSR
            libsupcs.x86_64.Cpu.Cr4 = cr4;
            cr4 |= 0x400;       // set cr4.OSXMMXCPT
            libsupcs.x86_64.Cpu.Cr4 = cr4;

            ulong cr0 = libsupcs.x86_64.Cpu.Cr0;
            cr0 |= 0x4;
            cr0 ^= 0x4;         // clear cr0.EM
            libsupcs.x86_64.Cpu.Cr0 = cr0;
            cr0 |= 0x2;         // set cr0.MP
            libsupcs.x86_64.Cpu.Cr0 = cr0;
            cr0 |= (1 << 16);   // set cr0.WP
            libsupcs.x86_64.Cpu.Cr0 = cr0;


            /* Set-up the MXCSR register
             * 
             * We want to unmask all exceptions except the precision exception which is thrown if the
             * result of an operation cannot be stored with complete precision in an xmm register,
             * e.g. 1/3 - this happens frequently and is expected, so we don't not want to raise a #XM
             * exception here.  The precision mask is bit 12.
             * 
             * We set the rounding mode (bits 13 and 14) to 00b which is round to even as the Math.Round
             * function expects this mode
             * 
             * All other bits are cleared.
             */

            uint mxcsr = 0x1000;
            libsupcs.x86_64.Cpu.Mxcsr = mxcsr;

            /* Set up the memory managers */
            bda_va = (ulong)chunk_vaddr;
            pmem_bitmap_va = bda_va + 0x1000;
            vmem_temppage_va = bda_va + 0x3000;
            vga_fb_va = bda_va + 0x4000;

            VirtMem = new VirtMem(null, vmem_temppage_va);
            PhysMem = new Pmem(pmem_bitmap_va, 0x2000);

            Multiboot.MachineMinorType_x86 bios = (Multiboot.MachineMinorType_x86)mboot.machine_minor_type;

            /* Set up the debug outputs */
            DebugOutput = new SerialDebug();
            if (mboot.has_vga && bios == Multiboot.MachineMinorType_x86.BIOS)
            {
                VirtMem.map_page(bda_va, 0x0);
                BootInfoOutput = new Vga(bda_va, vga_fb_va, VirtMem);
            }
            else
                BootInfoOutput = DebugOutput;

            /* Say hi */
            Formatter.WriteLine("Tysos x86_64 architecture initialising", DebugOutput);

            // Set up the physical memory allocator
            // First mark all pages as free, then unmark those which are used
            for (int i = 0; i < mboot.mmap.Length; i++)
            {
                Multiboot.MemoryMap cur_mmap = mboot.mmap[i];
                if (IsUefiFreeMemory(cur_mmap.type))
                {
                    for (ulong cur_page = cur_mmap.base_addr; (cur_page + 0x1000) <= (cur_mmap.base_addr + cur_mmap.length); cur_page += 0x1000)
                        PhysMem.ReleasePage(cur_page);
                }
            }
            for (int i = 0; i < mboot.mmap.Length; i++)
            {
                Multiboot.MemoryMap cur_mmap = mboot.mmap[i];
                if (!IsUefiFreeMemory(cur_mmap.type))
                {
                    for (ulong cur_page = cur_mmap.base_addr; cur_page < (cur_mmap.base_addr + cur_mmap.length); cur_page += 0x1000)
                        PhysMem.MarkUsed(cur_page);
                }
            }

            PageFault.pf_unwinder = new libsupcs.x86_64.Unwinder();
            
            // Display success
            Formatter.Write("Allocated: ", DebugOutput);
            Formatter.Write(PhysMem.GetFreeCount(), DebugOutput);
            Formatter.WriteLine(" 4 kiB pages", DebugOutput);
            VirtMem.SetPmem(PhysMem);

            Formatter.Write("x86_64: mboot.max_tysos: ", DebugOutput);
            Formatter.Write(mboot.max_tysos, "X", DebugOutput);
            Formatter.Write(", tysos_virtaddr: ", DebugOutput);
            Formatter.Write(mboot.tysos_virtaddr, "X", DebugOutput);
            Formatter.Write(", tysos_size: ", DebugOutput);
            Formatter.Write(mboot.tysos_size, "X", DebugOutput);
            Formatter.WriteLine(DebugOutput);

            /* Set up the virtual region allocator */
            if (mboot.max_tysos != 0)
                VirtualRegions = new Virtual_Regions(0, mboot.max_tysos);
            else
                VirtualRegions = new Virtual_Regions(mboot.tysos_virtaddr, mboot.tysos_size);

            Formatter.WriteLine("x86_64: Virtual region allocator started", DebugOutput);
            VirtualRegions.Dump(DebugOutput);

            /* Set up interrupts */
            ulong idt_start = VirtualRegions.Alloc(256 * 16, 0x1000, "idt");
            VirtMem.map_page(idt_start);
            Interrupts = new Interrupts(idt_start);
            Formatter.WriteLine("x86_64: IDT allocated", DebugOutput);

            /* Generate a blank page to use for all read page faults */
            ulong blank_page_vaddr = VirtualRegions.Alloc(0x1000, 0x1000, "blank page");
            VirtMem.blank_page_paddr = VirtMem.map_page(blank_page_vaddr);
            libsupcs.MemoryOperations.QuickClearAligned16(blank_page_vaddr, 0x1000);
            Formatter.Write("x86_64: blank page paddr: ", DebugOutput);
            Formatter.Write(VirtMem.blank_page_paddr, "X", DebugOutput);
            Formatter.Write(", vaddr: ", DebugOutput);
            Formatter.Write(blank_page_vaddr, "X", DebugOutput);
            Formatter.WriteLine(DebugOutput);

            unsafe
            {
                // Install the page fault handler
                Interrupts.InstallHandler(14, new Interrupts.ISREC(PageFault.PFHandler));

                // Install some default handlers
                Interrupts.InstallHandler(0, new Interrupts.ISR(Exceptions.DivideError_0_Handler));
                Interrupts.InstallHandler(1, new Interrupts.ISR(Exceptions.DebugError_1_Handler));
                Interrupts.InstallHandler(2, new Interrupts.ISR(Exceptions.NMIError_2_Handler));
                Interrupts.InstallHandler(3, new Interrupts.ISR(Exceptions.BreakPoint_3_Handler));
                Interrupts.InstallHandler(4, new Interrupts.ISR(Exceptions.OverflowError_4_Handler));
                Interrupts.InstallHandler(5, new Interrupts.ISR(Exceptions.BoundCheckError_5_Handler));
                Interrupts.InstallHandler(6, new Interrupts.ISR(Exceptions.InvalidOpcode_6_Handler));
                Interrupts.InstallHandler(7, new Interrupts.ISR(Exceptions.DeviceNotPresentError_7_Handler));
                Interrupts.InstallHandler(8, new Interrupts.ISREC(Exceptions.DoubleFault_8_Handler));
                Interrupts.InstallHandler(10, new Interrupts.ISREC(Exceptions.TSSError_10_Handler));
                Interrupts.InstallHandler(11, new Interrupts.ISREC(Exceptions.SegmentNotPresentError_11_Handler));
                Interrupts.InstallHandler(12, new Interrupts.ISREC(Exceptions.StackeFaultError_12_Handler));
                Interrupts.InstallHandler(13, new Interrupts.ISREC(Exceptions.GeneralProtection_13_Handler));
                Interrupts.InstallHandler(16, new Interrupts.ISR(Exceptions.FPUError_16_Handler));
                Interrupts.InstallHandler(17, new Interrupts.ISREC(Exceptions.AlignmentCheck_17_Handler));
                Interrupts.InstallHandler(18, new Interrupts.ISR(Exceptions.MachineCheckError_18_Handler));
                Interrupts.InstallHandler(19, new Interrupts.ISR(Exceptions.SIMD_19_Handler));
            }
            Formatter.WriteLine("x86_64: Default exception handlers installed", DebugOutput);

            /* Set up the tss and IST stacks */
            ulong tss;
            tss = VirtualRegions.Alloc(0x1000, 0x1000, "tss");
            ulong[] ists = new ulong[7];

            /* Create 2 ISTs with guard pages */
            int ist_count = 2;
            ulong ist_size = 0x2000;
            ulong ist_guard_size = 0x1000;
            for (int i = 0; i < ist_count; i++)
            {
                ulong ist_base = VirtualRegions.Alloc(ist_size + ist_guard_size, 0x1000, "IST");
                ists[i] = ist_base + ist_guard_size + ist_size;
                for (ulong addr = ist_base + ist_guard_size; addr < ist_base + ist_guard_size + ist_size; addr += 0x1000)
                    VirtMem.map_page(addr);
            }

            VirtMem.map_page(tss);
            Tss.Init(tss, ists);
            Formatter.WriteLine("x86_64: TSS installed", DebugOutput);

            // Now inform the page fault and double fault handlers to use their own stack
            unsafe
            {
                Interrupts.InstallHandler(14, new Interrupts.ISREC(PageFault.PFHandler), 1);
                Interrupts.InstallHandler(8, new Interrupts.ISREC(Exceptions.DoubleFault_8_Handler), 2);
            }
            Formatter.WriteLine("x86_64: Page fault and double fault handlers installed", DebugOutput);

            /* Set up the current cpu */
            Virtual_Regions.Region cpu_reg = VirtualRegions.AllocRegion(0x8000, 0x1000, "BSP cpu", 0, Virtual_Regions.Region.RegionType.CPU_specific, true);
            VirtMem.map_page(cpu_reg.start);
            x86_64_cpu bsp = new x86_64_cpu(cpu_reg);
            bsp.InitCurrentCpu();
            cpu_structure_setup = true;

            // Set up the page fault handler's stack switching mechanism
            /*ulong pfault_ist_block = VirtualRegions.Alloc(VirtMem.page_size * 7, VirtMem.page_size, "PageFault ISTs");
            List<ulong> pfault_ists = new List<ulong>();
            pfault_ists.Add(ist_block + VirtMem.page_size);
            for (int i = 0; i < 7; i++)
            {
                VirtMem.map_page(pfault_ist_block + (ulong)i * VirtMem.page_size);
                pfault_ists.Add(pfault_ist_block + (ulong)(i + 1) * VirtMem.page_size);
            }
            PageFault.cur_ist_idx = 0;
            PageFault.tss_addr = tss_start;
            PageFault.ist1_offset = (ulong)(((libsupcs.TysosField)typeof(tysos.x86_64.Tss.Tss_struct).GetField("ist1")).Offset);
            PageFault.ist_stack = pfault_ists;*/

            /* Test the gengc */
            Formatter.WriteLine("x86_64: testing gengc", Program.arch.DebugOutput);
            gc.gengc.heap = new gc.gengc();
            Formatter.Write("gengc object created @ ", Program.arch.DebugOutput);
            Formatter.Write(libsupcs.CastOperations.ReinterpretAsUlong(gc.gengc.heap), "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
            unsafe
            {
                gc.gengc.heap.Init((void*)heap_small_start, (void*)heap_long_end);

                /* Initial roots are the current stack, all static fields and the original heap */
                gc.gengc.heap.AddRoots((byte*)mboot.stack_low, (byte*)mboot.stack_high);
                gc.gengc.heap.AddRoots((byte*)mboot.tysos_static_start, (byte*)mboot.tysos_static_end);
                gc.gengc.heap.AddRoots((byte*)mboot.heap_start, (byte*)mboot.heap_end);
            }
            Formatter.WriteLine("heap initialized", Program.arch.DebugOutput);
            int[] test_array = new int[] { 8, 8, 8, 8, 8, 8, 8 };
            Formatter.WriteLine("test array created", Program.arch.DebugOutput);
            gc.gc.Heap = gc.gc.HeapType.GenGC;
            Formatter.WriteLine("heap set to gengc", Program.arch.DebugOutput);
            for (int i = 0; i < test_array.Length; i++)
            {
                Formatter.Write((ulong)i, Program.arch.DebugOutput);
                Formatter.Write(": size ", Program.arch.DebugOutput);
                Formatter.Write((ulong)test_array[i], Program.arch.DebugOutput);
                Formatter.Write(" returns ", Program.arch.DebugOutput);
                ulong addr = gc.gc.Alloc((ulong)test_array[i]);
                Formatter.Write(addr, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
            }

            // Now point the heap to its proper location
            //ulong heap_start = 0xFFFF800000000000;
            //ulong heap_len = 0xFFFFFF8000000000 - heap_start;
            //gc.heap.Init(heap_start, heap_len);
            /*Formatter.Write("x86_64: setting heap to final location: ", Program.arch.DebugOutput);
            Formatter.Write(heap_small_start, "X", Program.arch.DebugOutput);
            Formatter.Write(" - ", Program.arch.DebugOutput);
            Formatter.Write(heap_long_end, "X", Program.arch.DebugOutput);
            Formatter.Write(" ", Program.arch.DebugOutput);

#if NO_BOEHM
#if NO_TYSOSGC
            gc.gc.Heap = gc.gc.HeapType.Startup;
            gc.simple_heap.Init(heap_small_start, heap_long_end);
#else
            gc.gc.Heap = gc.gc.HeapType.TysosGC;
            gc.heap.Init(heap_small_cutoff, heap_small_start, heap_small_end, heap_long_start, heap_long_end);
#endif
#else
            gc.boehm.InitHeap(heap_small_start, heap_long_end);
            gc.gc.Heap = gc.gc.HeapType.BoehmGC;
#endif

            Formatter.WriteLine("done", Program.arch.DebugOutput);
            Formatter.Write("x86_64: new heap of type ", Program.arch.DebugOutput);
            Formatter.WriteLine(gc.gc.Heap.ToString(), Program.arch.DebugOutput); */

            /* Test a dictionary */
            var dtest = new Dictionary<int, int>(new Program.MyGenericEqualityComparer<int>());
            dtest[2] = 42;
            var dout = dtest[2];
            Formatter.Write("x86_64: dictionary test: ", DebugOutput);
            Formatter.Write((ulong)dout, DebugOutput);
            Formatter.WriteLine(DebugOutput);

            /* Test enum comparer */
            var enumcomp = new metadata.GenericEqualityComparerEnum<metadata.MetadataStream.TableId>();
            if (enumcomp.Equals(metadata.MetadataStream.TableId.Assembly, metadata.MetadataStream.TableId.Assembly))
                Formatter.WriteLine("x86_64: enumcomp test passed", DebugOutput);
            else
                Formatter.WriteLine("x86_64: enumcomp test failed", DebugOutput);

            /* Test an enum dictionary */
            var dtest2 = new Dictionary<metadata.MetadataStream.TableId, int>(new metadata.GenericEqualityComparerEnum<metadata.MetadataStream.TableId>());
            dtest2[metadata.MetadataStream.TableId.TypeDef] = 42;
            var dout2 = dtest2[metadata.MetadataStream.TableId.TypeDef];
            Formatter.Write("x86_64: enum dictionary test: ", DebugOutput);
            Formatter.Write((ulong)dout, DebugOutput);
            Formatter.WriteLine(DebugOutput);

            /* Test MetadataStream ctor */
            Formatter.WriteLine("x86_64: Testing metadata ctor", DebugOutput);
            var mtest = new metadata.MetadataStream();
            Formatter.WriteLine("x86_64: done", DebugOutput);

            /* Set-up the architecture for the JIT engine */
            jit.Jit.t = libtysila5.target.Target.targets["x86_64"];
            jit.Jit.t.Options["mcmodel"] = "large";
            jit.Jit.t.InitIntcalls();
            jit.Jit.bness = binary_library.Bitness.Bits64;

            /* Initialize firmware */
            switch (bios)
            {
                case Multiboot.MachineMinorType_x86.UEFI:
                    fwconf = new UEFI(VirtualRegions, VirtMem, mboot.virt_bda);
                    break;
                case Multiboot.MachineMinorType_x86.BIOS:
                    fwconf = new BIOS(VirtualRegions, VirtMem, mboot.virt_bda);
                    break;
                default:
                    throw new Exception("Unsupported firmware: " + bios.ToString());
            }

            /* Load ACPI tables */
            Acpi acpi = new Acpi(VirtualRegions, VirtMem, fwconf);
            //tysos.x86_64.Acpi acpi = new tysos.x86_64.Acpi(VirtualRegions, VirtMem,
            //    (bios == Multiboot.MachineMinorType_x86.BIOS) ? bda_va : mboot.virt_bda,
            //    bios);

            /* Disable the PIC if we have one */
            if ((acpi.Apic != null) && (acpi.Apic.Has8259))
                tysos.x86_64.PIC_8295a.Disable();


            /* Set up the local apic for the current processor */
            tysos.x86_64.LApic bsp_lapic = new tysos.x86_64.LApic(VirtualRegions, VirtMem);

            /* Calibrate the lapic */
            if (acpi.Hpet == null)
                bsp_lapic.CalibrateDirectly(133000000.0);
            else
            {
                tysos.x86_64.Hpet hpet = new tysos.x86_64.Hpet(VirtualRegions, VirtMem, acpi.Hpet.paddr);
                bsp_lapic.CalibrateTimerWithHpet(hpet, 1000000);
            }

            bsp_lapic.SetSpuriousVector(0x60);
            unsafe
            {
                Interrupts.InstallHandler(0x60, new Interrupts.ISR(tysos.x86_64.LApic.SpuriousApicInterrupt));
            }

            bsp_lapic.SetTimer(true, 100.0, 0x40);      // 10 ms timer
            //bsp_lapic.SetTimer(true, 0x144b50, 0x40);   // 10ms timer with 133 Mhz bus and divisor 1, interrupt vector 0x40
            unsafe
            {
                Interrupts.InstallHandler(0x40, new Interrupts.ISR(tysos.x86_64.LApic.TimerInterrupt));
            }
            SchedulerTimer = bsp_lapic;

            /* Set up the current cpu */
            bsp.CurrentLApic = bsp_lapic;

            Processors = new List<Cpu>();
            Processors.Add(Program.arch.CurrentCpu);

            /* Set up the task switcher */
            Switcher = new tysos.x86_64.TaskSwitcher();

            /* Now we have a working heap we can set up the rest of the physical memory */
            SetUpHighMemory(PhysMem, mboot);

            /* Finally, create a list of parameters for handing to the system device enumerator */
            if (acpi == null)
                throw new Exception("ACPI required");
            ps = new List<tysos.lib.File.Property>();
            ps.Add(new tysos.lib.File.Property { Name = "driver", Value = "acpipc" });
            foreach(Acpi.AcpiTable table in acpi.tables)
            {
                string tab_name = "table_" + table.signature.ToString("X8");
                ps.Add(new tysos.lib.File.Property { Name = tab_name, Value = new VirtualMemoryResource64(table.start_vaddr, table.length) });
            }
            ps.Add(new tysos.lib.File.Property { Name = "vmem", Value = new VirtualMemoryResource64(VirtualRegions.devs.start, VirtualRegions.devs.length) });
            ps.Add(new tysos.lib.File.Property { Name = "pmem", Value = new PhysicalMemoryResource64(0, UInt64.MaxValue) });
            ps.Add(new tysos.lib.File.Property { Name = "io", Value = new x86_64.IOResource(0, 0x10000) });

            Formatter.WriteLine("x86_64: Arch initialized", Program.arch.DebugOutput);
        }

        internal override List<File.Property> SystemProperties
        {
            get
            {
                return ps;
            }
        }

        private void SetUpHighMemory(Pmem PhysMem, Multiboot.Header mboot)
        {
            /* First generate a list of free memory > 256 MiB */
            List<Pmem.FreeRegion> free_regions = new List<Pmem.FreeRegion>();

            Formatter.WriteLine("x86_64: Multiboot memory map:", Program.arch.DebugOutput);
            foreach (Multiboot.MemoryMap mmap in mboot.mmap)
            {
                if (IsUefiFreeMemory(mmap.type))
                {
                    Pmem.FreeRegion fr = new Pmem.FreeRegion();
                    fr.start = mmap.base_addr;
                    fr.length = mmap.length;
                    free_regions.Add(fr);

                    Formatter.WriteLine("x86_64: FREE:  start: " + fr.start.ToString("X16") + "  length: " + fr.length.ToString("X16"), Program.arch.DebugOutput);
                }
            }

            /* Exclude the first 256 MiB */
            exclude_region(0x0, PhysMem.bmp_end, free_regions);

            /* Exclude space above this */
            foreach (Multiboot.MemoryMap mmap in mboot.mmap)
            {
                if (!IsUefiFreeMemory(mmap.type))
                {
                    exclude_region(mmap.base_addr, mmap.length, free_regions);
                    Formatter.WriteLine("x86_64: USED:  start: " + mmap.base_addr.ToString("X16") + "  length: " + mmap.length.ToString("X16"), Program.arch.DebugOutput);
                }
            }

            /* Trim the regions to start/finish on page boundaries */
            int i = 0;
            while(i < free_regions.Count)
            {
                Pmem.FreeRegion fr = free_regions[i];

                if ((fr.start & 0xfff) != 0x0)
                {
                    ulong next_page = (fr.start + 0x1000) & 0xfffffffffffff000;
                    if ((next_page - fr.start) > fr.length)
                        fr.length = 0;
                    else
                        fr.length -= (next_page - fr.start);
                    fr.start = next_page;
                }

                if ((fr.length & 0xfff) != 0x0)
                    fr.length = fr.length & 0xfffffffffffff000;

                if (fr.length == 0x0)
                    free_regions.RemoveAt(i);
                else
                    i++;
            }

            /* Now find some space to use as a buffer */
            ulong buf_len = 0x400000;
            ulong buf_addr = ulong.MaxValue;
            foreach(var r in free_regions)
            {
                if(buf_len <= r.length && r.start + buf_len <= 0x100000000UL)
                {
                    buf_addr = r.start;
                    r.start += buf_len;
                    r.length -= buf_len;
                    break;
                }
            }
            if(buf_addr != ulong.MaxValue)
            {
                buf_cur = buf_addr;
                buf_end = buf_addr + buf_len;

                Formatter.Write("x86_64: device physical buffer set up at ", DebugOutput);
                Formatter.Write(buf_addr, "X", DebugOutput);
                Formatter.Write(" - ", DebugOutput);
                Formatter.Write(buf_end, "X", DebugOutput);
                Formatter.WriteLine(DebugOutput);
            }

            PhysMem.SetFreeRegions(free_regions);
        }

        private void exclude_region(ulong exclude_start, ulong exclude_length, List<Pmem.FreeRegion> free_regions)
        {
            ulong exclude_last_byte = exclude_start - 1 + exclude_length;

            int i = 0;
            while (i < free_regions.Count)
            {
                Pmem.FreeRegion fr = free_regions[i];
                ulong test_start = fr.start;
                ulong test_last_byte = fr.start - 1 + fr.length;

                /* 6 possibilities exist:
                 * 
                 * 1) exclude region is below test region
                 *          -   exclude_last_byte < test_start
                 *          ->  do nothing
                 * 2) exclude region is above test region
                 *          -   exclude_start > test_last_byte
                 *          ->  do nothing
                 * 3) exclude region overlaps bottom of test region
                 *          -   exclude_last_byte >= test_start
                 *          -   exclude_last_byte < test_last_byte
                 *          -   exclude_start <= test_start
                 *          ->  shrink test region
                 *                  - test_start = exclude_last_byte + 1
                 * 4) exclude region overlaps top of test region
                 *          -   exclude_start <= test_last_byte
                 *          -   exclude_start > test_start
                 *          -   exclude_last_byte >= test_last_byte
                 *          ->  shrink test region
                 *                  - test_last_byte = exclude_start - 1
                 * 5) exclude region overlaps whole of test region
                 *          -   exclude_start <= test_start
                 *          -   exclude_last_byte >= test_last_byte
                 *          ->  delete test region
                 * 6) exclude region is contained within test region
                 *          -   exclude_start > test_start
                 *          -   exclude_start < test_last_byte
                 *          -   exclude_last_byte < test_last_byte
                 *          -   exclude_last_byte > test_start
                 *          ->  split test region
                 *                      - test1_start = test_start
                 *                      - test1_last_byte = exclude_start - 1
                 *                      - test2_start = exclude_last_byte + 1
                 *                      - test2_last_byte = test_last_byte
                 */

                if (exclude_last_byte < test_start)
                    i++;
                else if (exclude_start > test_last_byte)
                    i++;
                else if ((exclude_last_byte >= test_start) &&
                    (exclude_last_byte < test_last_byte) &&
                    (exclude_start <= test_start))
                {
                    test_start = exclude_last_byte + 1;
                    fr.start = test_start;
                    fr.length = test_last_byte - test_start + 1;
                    i++;
                }
                else if ((exclude_start <= test_last_byte) &&
                    (exclude_start > test_start) &&
                    (exclude_last_byte >= test_last_byte))
                {
                    test_last_byte = exclude_start - 1;
                    fr.start = test_start;
                    fr.length = test_last_byte - test_start + 1;
                    i++;
                }
                else if ((exclude_start <= test_start) &&
                    (exclude_last_byte >= test_last_byte))
                {
                    free_regions.RemoveAt(i);
                }
                else if ((exclude_start > test_start) &&
                    (exclude_start < test_last_byte) &&
                    (exclude_last_byte < test_last_byte) &&
                    (exclude_last_byte > test_start))
                {
                    ulong test1_start = test_start;
                    ulong test1_last_byte = exclude_start - 1;
                    ulong test2_start = exclude_last_byte + 1;
                    ulong test2_last_byte = test_last_byte;
                    fr.start = test1_start;
                    fr.length = test1_last_byte - test1_start + 1;

                    Pmem.FreeRegion fr2 = new Pmem.FreeRegion();
                    fr2.start = test2_start;
                    fr2.length = test2_last_byte - test2_start + 1;
                    free_regions.Add(fr2);

                    i++;
                }
            }
        }

        internal override void EnableMultitasking()
        {
            ((x86_64.x86_64_cpu)Program.arch.CurrentCpu).CurrentLApic.Enable();
            multitasking = true;
            libsupcs.x86_64.Cpu.Sti();
        }

        internal override void DisableMultitasking()
        {
            libsupcs.x86_64.Cpu.Cli();
            multitasking = false;
        }

        internal override libsupcs.Unwinder GetUnwinder()
        {
            libsupcs.Unwinder u = new libsupcs.x86_64.Unwinder();
            u.UnwindOne();
            return u;
        }

        internal override long GetNow()
        {
            if (((x86_64.x86_64_cpu)Program.arch.CurrentCpu).CurrentLApic == null)
                return 0;
            else
                return ((x86_64.x86_64_cpu)Program.arch.CurrentCpu).CurrentLApic.Ticks;
        }

        internal override ulong GetMonotonicCount
        {
            [libsupcs.Profile(false)]
            get
            {
                return libsupcs.x86_64.Cpu.Tsc;
            }
        }

        internal override ulong GetBuffer(ulong len)
        {
            // align len on a page size
            if((len & 0xfffUL) != 0)
            {
                len &= ~0xfffUL;
                len += 0x1000UL;
            }

            if (buf_cur + len > buf_end)
                return 0;

            ulong ret = buf_cur;
            buf_cur += len;
            return ret;
        }
    }
}
