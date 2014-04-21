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

#ifndef MBOOT_H
#define MBOOT_H

struct mb_header {
	unsigned long int		flags;
	unsigned long int		mem_lower;
	unsigned long int		mem_upper;
	unsigned long int		boot_device;
	char *				cmdline;
	unsigned long int		mods_count;
	unsigned long int		mods_addr;
	unsigned long int		elf_num;
	unsigned long int		elf_size;
	unsigned long int		elf_addr;
	unsigned long int		elf_shndx;
	unsigned long int		mmap_length;
	unsigned long int		mmap_addr;
	unsigned long int		drives_length;
	unsigned long int		drives_addr;
	unsigned long int		config_table;
	unsigned long int		boot_loader_name;
	unsigned long int		apm_table;
	unsigned long int		vbe_control_info;
	unsigned long int		vbe_mode_info;
	unsigned short int		vbe_mode;
	unsigned short int		vbe_interface_seg;
	unsigned short int		vbe_interface_off;
	unsigned short int		vbe_interface_len;
};

struct mb_mmap {
	unsigned long long int		base_addr;
	unsigned long long int		length;
	unsigned long int		type;
};

struct mb_mod {
	unsigned long int		mod_start;
	unsigned long int		mod_end;
	char *				string;
	unsigned long int		reserved;
};

#endif
