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
using System.Runtime.InteropServices;

namespace Multiboot
{
    [libsupcs.OutputCHeader]
    public class Header
    {
        public MemoryMap[] mmap;
        public Module[] modules;

        public ulong heap_start;
        public ulong heap_end;

        public ulong virt_master_paging_struct;

        public ulong virt_bda;

        public ulong max_tysos;
        public ulong gdt;

        public ulong tysos_paddr;
        public ulong tysos_size;
        public ulong tysos_virtaddr;
        public ulong tysos_sym_tab_paddr;
        public ulong tysos_sym_tab_size;
        public ulong tysos_sym_tab_entsize;
        public ulong tysos_str_tab_paddr;
        public ulong tysos_str_tab_size;

        public string cmdline;
        public bool debug;
        public string loader_name;

        public uint machine_major_type;
        public uint machine_minor_type;

        public bool has_vga;

        public ulong fb_base;
        public uint fb_w;
        public uint fb_h;
        public uint fb_stride;
        public uint fb_bpp;
        public PixelFormat fb_pixelformat;
    }

    [libsupcs.OutputCHeader]
    public class MemoryMap
    {
        public ulong base_addr;
        public ulong virt_addr;
        public ulong length;
        public MemoryMapType type;
    }

    [libsupcs.OutputCHeader]
    public class Module
    {
        public ulong base_addr;
        public ulong length;
        public String name;
    }

    [libsupcs.OutputCHeader]
    public enum PixelFormat
    {
        RGB = 1,
        BGR = 2
    }

    [libsupcs.OutputCHeader]
    public enum MemoryMapType
    {
        Available = 1,
        TLoad = 1002,
        Tysos = 1003,
        PagingStructure = 1004,
        Module = 1005,
        VideoHardware = 1006,
        BiosDataArea = 1007,
        UEfiLoaderCode = 1,
        UEfiLoaderData = 2,
        UEfiBootServicesCode = 3,
        UEfiBootServicesData = 4,
        UEfiRuntimeServicesCode = 5,
        UEfiRuntimeServicesData = 6,
        UEfiConventionalMemory = 7,
        UEfiUnusableMemory = 8,
        UEfiACPIReclaimMemory = 9,
        UEfiACPIMemoryNVS = 10,
        UEfiMemoryMappedIO = 11,
        UEfiMemoryMappedIOPortSpace = 12,
        UEfiPalCode = 13,
    }

    [libsupcs.OutputCHeader]
    public enum MachineMajorType
    {
        Unknown = 0,
        x86_64 = 1,
        x86 = 2,
        arm = 3
    }

    [libsupcs.OutputCHeader]
    public enum MachineMinorType_ARM
    {
        bcm2708 = 3138
    }
}
