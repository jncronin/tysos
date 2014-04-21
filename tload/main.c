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

/* main entry point from assembler */

/* At this point we are executing in 32-bit mode, there is a gdt set up by the 
 * assembler code and a stack
 * We need to identify the used areas of memory, then identify the kernel 
 * module and load it to a free set of pages.
 * We then need to set up the 64-bit paging structures and map the kernel to 
 * where it should be.
 * Finally, enable paging and jump to 64-bit kernel */

#include "console.h"
#include "mboot.h"
#include "memmgr.h"
#include "elf.h"
#include "cmdline.h"
#include "stdlib.h"
#include "vmem.h"
#include "kif.h"
#include "malloc.h"

#define MB_ID		0x2BADB002
#define DEFAULT_KERNEL		"kernel"

#ifdef EFI
#include <efi.h>
#include <efilib.h>

EFI_SYSTEM_TABLE *gST;
UINTN efi_map_key;
#define MEMMAP_SIZE 0x1000
char efi_memmap[MEMMAP_SIZE];

void setup_mm();
int identity_map_tload(EFI_LOADED_IMAGE *li, unsigned long long int *pml4t);
#else
void setup_mm(struct mb_header *mb_header);
int identity_map_tload(struct mb_header *mb_header, unsigned long long int *pml4t);
#endif

const char *profile(const char *msg);
extern void go64(unsigned long int pml4t, unsigned long long int e_point);
extern int detect64();
extern void *uncompress_bzipped_kernel(void *compressed_data, unsigned int len);

extern char *kernel_name;
extern char bda_addr;
extern char gdt64;

extern unsigned long int heap_end;
extern unsigned long int heap_start;

extern char tload_start;
extern char tload_end;

extern struct Multiboot_Header *kif;
extern Elf64_Shdr *strtab;
extern Elf64_Shdr *symtab;

extern int tysos_debug;

const char def_k_name[] = DEFAULT_KERNEL;

#ifdef EFI
// Load 512 kiB chunks
#define EFI_LOAD_FILE_CHUNK_SIZE 0x80000
/* Load a file from the filesystem beginning at root */
EFI_STATUS efi_load_file(EFI_FILE *root, CHAR16 *fname, void **file, size_t *fsize, void (*report_progress)(int percent))
{
	EFI_STATUS s;

	/* Attempt to open the file */
	EFI_FILE *f;
	s = root->Open(root, &f, fname, EFI_FILE_MODE_READ, 0);
	if(EFI_ERROR(s))
		return s;

	/* Get its info to determine its size */
	EFI_FILE_INFO *fi = NULL;
	UINTN fi_size = 0;
	s = f->GetInfo(f, &GenericFileInfo, &fi_size, fi);
	if(s != EFI_BUFFER_TOO_SMALL)
		return s;
	fi = (EFI_FILE_INFO *)malloc(fi_size);
	s = f->GetInfo(f, &GenericFileInfo, &fi_size, fi);
	if(EFI_ERROR(s))
	{
		free(fi);
		return s;
	}

	/* Get the file size, aligned up */
	unsigned long flen = (unsigned long)fi->FileSize;
	UINTN flen_unalign = flen;
	free(fi);
	if(flen % 4096)
	{
		flen -= (flen % 4096);
		flen += 4096;
	}

	/* Get memory for the file */
	void *buf = malloc_align(flen, 4096, Tysos, 1);
	if(buf == NULL)
		return EFI_OUT_OF_RESOURCES;

	/* Load the file in chunks, reporting progress as we go */
	UINTN cur_file_pointer = 0;
	while (cur_file_pointer < flen_unalign)
	{
		UINTN bytes_to_read = EFI_LOAD_FILE_CHUNK_SIZE;
		if((cur_file_pointer + bytes_to_read) > flen_unalign)
			bytes_to_read = flen_unalign - cur_file_pointer;
		UINTN bytes_read = bytes_to_read;

		if(report_progress != NULL)
			report_progress((int)((double)cur_file_pointer / flen_unalign * 100.0));

		s = f->Read(f, &bytes_to_read, &(((uint8_t *)buf)[cur_file_pointer]));
		if(EFI_ERROR(s))
			return s;
		if(bytes_to_read != bytes_read)
			return EFI_END_OF_FILE;

		cur_file_pointer += bytes_read;
	}

	/* Close and return */
	*fsize = (size_t)flen_unalign;
	*file = buf;
	return f->Close(f);
}

char efp_digits[] = "0123456789";
void efi_file_progress(int percent)
{
	/* Backspace 4 times */
	putchar(0x08);
	putchar(0x08);
	putchar(0x08);
	putchar(0x08);

	if(percent == 100)
		puts("100%");
	else
	{
		int digits = percent % 10;
		int tens = percent / 10;

		putchar(' ');
		putchar(efp_digits[tens]);
		putchar(efp_digits[digits]);
		putchar('%');
	}
}

void efi_pause()
{
	UINTN index;
	EFI_INPUT_KEY key;
	gST->BootServices->WaitForEvent(1, &gST->ConIn->WaitForKey, &index);
	gST->ConIn->ReadKeyStroke(gST->ConIn, &key);
}
#endif


#ifdef EFI
EFI_STATUS efi_main(EFI_HANDLE ImageHandle, EFI_SYSTEM_TABLE *SystemTable)
#else
int kmain(unsigned long int mbid, struct mb_header *mb_header)
#endif
{
	void *kmod;
	void *pml4t;
	unsigned long long int e_point;
	unsigned long long int kernel_base;
	unsigned long int kmod_length;
	unsigned long long int kernel_vsize;
	struct System_String *cmd_line;

#ifdef EFI
	gST = SystemTable;
#endif

	profile("Start");

	/* say hi */
	//clear();
	puts("TLoad starting...");

	/* Ensure this is a 64-bit processor */
	if(!detect64())
	{
		puts("Error: 64-bit processor not detected.  Boot cannot continue.");
		while(1);
	}

#ifdef EFI
	puts("EFI boot detected.");

	setup_mm();

	/* Attempt to load the kernel */

	/* First, get the filesystem tload was loaded from */
	EFI_FILE *fi;
	EFI_FILE_IO_INTERFACE *sfs;
	EFI_LOADED_IMAGE *li;
	EFI_STATUS s;

	s = gST->BootServices->HandleProtocol(ImageHandle, &LoadedImageProtocol, (void **)&li);
	if(EFI_ERROR(s))
	{
		puts("EFI: HandleProtocol(LoadedImageProtocol) failed");
		efi_pause();
		return s;
	}

	s = gST->BootServices->HandleProtocol(li->DeviceHandle, &FileSystemProtocol, (void **)&sfs);
	if(EFI_ERROR(s))
	{
		puts("EFI: HandleProtocol(FileSystemProtocol) failed");
		efi_pause();
		return s;
	}

	/* Obtain root directory */
	EFI_FILE *root;
	s = sfs->OpenVolume(sfs, &root);
	if(EFI_ERROR(s))
	{
		puts("EFI: OpenVolume failed for root device");
		efi_pause();
		return s;
	}

	/* Now try and open boot/tysos.bin */
	_puts("EFI: loading kernel...  00%", 0);
	s = efi_load_file(root, u"\\boot\\tysos.bin", &kmod, (size_t *)&kmod_length, efi_file_progress);
	if(EFI_ERROR(s))
	{
		puts("");
		_puts("EFI: couldn't open kernel: ", 0);
		puthex(s);
		puts("");
		efi_pause();
		return s;
	}
	putchar(0x08); putchar(0x08); putchar(0x08); putchar(0x08);
	puts(" done");
#else
	/* Check that the multiboot id is correct */
	if(mbid != MB_ID) {
		puts("Invalid multiboot magic number");
		return -1;
	}

#ifdef _DEBUG
	_puts("  Command line: ", 0);
	puts(mb_header->cmdline);
	_puts("  Multiboot flags: ", 0);
	puthex(mb_header->flags);
	_puts("", 1);
#endif

	/* Set up the memory manager */
	setup_mm(mb_header);
	profile("setup_mm");

	/* Parse command line */
	parse_cmd_line(mb_header->cmdline);
	profile("parse_cmd_line");

	/* Find the kernel module */
	if(kernel_name == NULL)
		kernel_name = (char *)def_k_name;
#ifdef _DEBUG
	_puts("Searching for kernel: ", 0);
	puts(kernel_name);
#endif
	kmod = NULL;
	if(mb_header->flags & 0x8) {
		unsigned int i;
		struct mb_mod *mods = (struct mb_mod *)mb_header->mods_addr;
		for(i = 0; i < mb_header->mods_count; i++) {
#ifdef _DEBUG
			_puts("  Trying module: ", 0);
			puts(mods[i].string);
#endif
			if(!strcmp(kernel_name, mods[i].string)) {
				kmod = (void *)mods[i].mod_start;
				kmod_length = mods[i].mod_end - mods[i].mod_start;
			}
		}
	}

	if(kmod == NULL) {
		puts("Error: could not find kernel module");
		return -1;
	}

	profile("find kernel module");
#endif

	/* Create a page map level 4 table */
	pml4t = malloc_align(4096, 4096, PagingStructure, 1);
	if(pml4t == NULL) {
		puts("Error: could not allocate page map level 4 table");
		return -1;
	}
	memset(pml4t, 0, 4096);
	/* point it back at itself */
	((unsigned long long int *)pml4t)[511] = ((unsigned long long int)(unsigned long int)pml4t) | 0x3LL;

	profile("create pml4t");

	/* Load the kernel */
	if(*(char *)kmod == 'B')
		kmod = uncompress_bzipped_kernel(kmod, kmod_length);
	if(load_elf64_kernel(kmod, pml4t, &e_point, &kernel_base, &kernel_vsize) < 0) {
		puts("Error: interpreting elf64 kernel");
#ifdef EFI
		efi_pause();
#endif
		return -1;
	}

	profile("load kernel");

	/* Identity map tload */
#ifdef EFI
	if(identity_map_tload(li, pml4t) < 0) {
#else
	if(identity_map_tload(mb_header, pml4t) < 0) {
#endif
		puts("Error: could not identity map tload");
		return -1;
	}

	profile("identity map tload");

#ifndef EFI
	/* Map the bios data area (physical 0x0) to somewhere away from there so we can both still use it and catch
	NULL pointers */
	create_virtual_mapping_from_page((unsigned long long int)(unsigned long int)&bda_addr, pml4t, PF_R | PF_W, 1,
		(void *)0x0, BiosDataArea);

	profile("map bda");
#endif

	/* Identity map the vga */
	create_virtual_mapping_from_page(0xb8000, pml4t, PF_R | PF_W, 1, (void *)0xb8000, VideoHardware);
	profile("map vga");

#ifdef EFI
#else
	/* Generate the kernel information format header */
	memmgr_init_linked_list();
	profile("memmgr_init_linked_list");
	make_kif();
	kif->max_tysos = kernel_base + kernel_vsize;
	kif->gdt = (unsigned long long int)(unsigned long int)&gdt64;
	kif->tysos_paddr = (unsigned long long int)(unsigned long int)kmod;
	kif->tysos_size = (unsigned long long int)(unsigned long int)kmod_length;
	kif->tysos_virtaddr = (unsigned long long int)(unsigned long int)kernel_base;
	kif->tysos_sym_tab_paddr = (unsigned long long int)(unsigned long int)kmod + symtab->sh_offset;
	kif->tysos_sym_tab_size = symtab->sh_size;
	kif->tysos_sym_tab_entsize = symtab->sh_entsize;
	kif->tysos_str_tab_paddr = (unsigned long long int)(unsigned long int)kmod + strtab->sh_offset;
	kif->tysos_str_tab_size = strtab->sh_size;
	kif->debug = (uint8_t)tysos_debug;
	write_kif_modules(kif, mb_header);
	CreateString(&cmd_line, mb_header->cmdline);
	kif->cmdline = (unsigned long long int)(unsigned long int)cmd_line;
	profile("make_kif");
#endif

	/* Go 64 bit and jump to the kernel */
#ifdef _DEBUG
	_puts("Enabling long mode and jumping to ", 0);
	puthex(e_point);
	puts("");
#endif

#ifdef _HALT
	puts("Halting before entering kernel (do not define _HALT to stop this happening)");
	while(1);
#endif

#ifdef EFI
	s = gST->BootServices->ExitBootServices(ImageHandle, efi_map_key);
	if(s == EFI_INVALID_PARAMETER)
	{
		/* We probably shouldn't do this, but most of the important memory is set up anyway.
		    The only new memory should be those for the EFI_FILE structures etc */
		UINTN mmapsize = MEMMAP_SIZE;
		UINTN descsize, descver;
		s = gST->BootServices->GetMemoryMap(&mmapsize, (EFI_MEMORY_DESCRIPTOR *)efi_memmap,
			&efi_map_key, &descsize, &descver);
		if(EFI_ERROR(s))
		{
			_puts("EFI: GetMemoryMap failed (", 0);
			puthex(s);
			puts("");
			efi_pause();
			return s;
		}
		s = gST->BootServices->ExitBootServices(ImageHandle, efi_map_key);
	}
	if(EFI_ERROR(s))
	{
		_puts("EFI: ExitBootServices failed (", 0);
		puthex(s);
		puts(")");
		efi_pause();
		return s;
	}
#endif
	go64((unsigned long int)pml4t, e_point);

	return 0;
}

#ifdef EFI

void dump_bitmap();

void setup_mm()
{
	/* Set up the memory manager with details of the memory map provided by EFI */

	/* loop through twice, first setting up the bitmap then the linked list which we'll pass to tysos.  This is because
	  the linked list requires a heap, which requires the bitmap set up to find space for a heap */

	EFI_STATUS s;

	/* Mark the entire bitmap as used */
	mark_used(0, 0x100000000);

	/* First get the EFI memory map */
	UINTN mmapsize = MEMMAP_SIZE;
	UINTN descsize;
	UINT32 descver;
	s = gST->BootServices->GetMemoryMap(&mmapsize, (EFI_MEMORY_DESCRIPTOR *)efi_memmap, &efi_map_key, &descsize, &descver);
	if(s != EFI_SUCCESS)
	{
		puts("EFI: setup_mm: GetMemoryMap() failed");
		return;
	}

	/* Determine the number of entries */
	int e_entries = mmapsize / descsize;
	int i;

	/* Loop first to set up the bitmap */
	for(i = 0; i < e_entries; i++)
	{
		EFI_MEMORY_DESCRIPTOR *desc = (EFI_MEMORY_DESCRIPTOR *)((UINTN)efi_memmap + i * descsize);

		if(desc->Type == EfiConventionalMemory)
			mark_free((unsigned long long)desc->PhysicalStart, (unsigned long long)(desc->NumberOfPages * 4096));
		else
			mark_used((unsigned long long)desc->PhysicalStart, (unsigned long long)(desc->NumberOfPages * 4096));
	}

	/* Mark first page used so we never return it from malloc_align */
	mark_used(0, 4096);

	/* Loop again to set up the linked list */
	for(i = 0; i < e_entries; i++)
	{
		EFI_MEMORY_DESCRIPTOR *desc = (EFI_MEMORY_DESCRIPTOR *)((UINTN)efi_memmap + i * descsize);
		int type = Tysos;

		if(desc->Type == EfiConventionalMemory)
			type = Available;

		add_linked_list((unsigned long long)desc->PhysicalStart, (unsigned long long)(desc->NumberOfPages * 4096),
			type);
	}
}
#else
void setup_mm(struct mb_header *mb_header)
{
	/* Set up the memory manager with details of the memory map provided by grub as well as the locations of all the
	modules and elf sections loaded */

	/* loop through twice, first setting up the bitmap then the linked list which we'll pass to tysos.  This is because
	  the linked list requires a heap, which requires the bitmap set up to find space for a heap */

	/* mmap */
	if(mb_header->flags & 0x40) {
		unsigned long int *cur_offset;
		
		cur_offset = (unsigned long int *)mb_header->mmap_addr;

#ifdef _DEBUG
		_puts("mmap_addr: ", 0);
		puthex(mb_header->mmap_addr);
		_puts("  mmap_length: ", 0);
		puthex(mb_header->mmap_length);
		_puts("", 1);
#endif

		while((unsigned long int)cur_offset < (mb_header->mmap_addr + mb_header->mmap_length)) {
			struct mb_mmap *cur_mmap;
			int cur_size = *cur_offset;
			cur_offset++;

			cur_mmap = (struct mb_mmap *)cur_offset;

#ifdef _DEBUG
			_puts("Mmap type: ", 0);
			puthex(cur_mmap->type);
			_puts(" ", 0);
			puthex(cur_mmap->base_addr);
			_puts("-", 0);
			puthex(cur_mmap->base_addr + cur_mmap->length);
			_puts("", 1);
#endif
			if(cur_mmap->type == 1)
				mark_free(cur_mmap->base_addr, cur_mmap->length);
			else
				mark_used(cur_mmap->base_addr, cur_mmap->length);

			cur_offset = (unsigned long int *)(((unsigned long int)cur_offset) + cur_size);
		}
	} else if(mb_header->flags & 0x1) {
		puts("Warning: multiboot header does not specify a memory map");
		mark_free(0x100000, mb_header->mem_upper * 1024);
	} else {
		puts("Warning: no memory information specified in multiboot header");
	}

	/* Always mark the first 1MB as used */
	mark_used(0, 0x100000);

	/* Modules */
	if(mb_header->flags & 0x8) {
		unsigned int i;
		struct mb_mod *mods = (struct mb_mod *)mb_header->mods_addr;
		for(i = 0; i < mb_header->mods_count; i++) {
			mark_used((unsigned long long int)mods[i].mod_start, (unsigned long long int)(mods[i].mod_end -
				mods[i].mod_start));
#ifdef _DEBUG
			_puts("mod: ", 0);
			_puts(mods[i].string, 0);
			_puts("  s: ", 0);
			puthex(mods[i].mod_start);
			_puts("  e: ", 0);
			puthex(mods[i].mod_end);
			_puts("", 1);
#endif
		}
	}

	/* Elf sections */
	if(mb_header->flags & 0x20) {
		unsigned int i;

#ifdef _DEBUG
		_puts("elf section count: ", 0);
		_puthex(mb_header->elf_num);
		_puts("", 1);
#endif

		for(i = 0; i < mb_header->elf_num; i++) {
			Elf32_Shdr *shdr = (Elf32_Shdr *)(mb_header->elf_addr + i * mb_header->elf_size);
			mark_used((unsigned long long int)shdr->sh_addr, (unsigned long long int)shdr->sh_size);
#ifdef _DEBUG
			_puts("elf section: s: ", 0);
			_puthex(shdr->sh_addr);
			_puts(" e: ", 0);
			_puthex(shdr->sh_addr + shdr->sh_size);
			_puts("", 1);
#endif
		}
	}

	/* tload itself */
	{
		unsigned long long int tl_start = (unsigned long long int)(unsigned long int)&tload_start;
		unsigned long long int tl_end = (unsigned long long int)(unsigned long int)&tload_end;
		mark_used(tl_start, tl_end - tl_start);

#ifdef _DEBUG
		_puts("tload: s: ", 0);
		_puthex((unsigned long int)tl_start);
		_puts(" e: ", 0);
		_puthex((unsigned long int)tl_end);
		_puts("", 1);
#endif
	}

	/* loop again to set up the linked list */

	/* mmap */
	if(mb_header->flags & 0x40) {
		unsigned long int *cur_offset;
		
		cur_offset = (unsigned long int *)mb_header->mmap_addr;

		while((unsigned long int)cur_offset < (mb_header->mmap_addr + mb_header->mmap_length)) {
			struct mb_mmap *cur_mmap;
			int cur_size = *cur_offset;
			cur_offset++;

			cur_mmap = (struct mb_mmap *)cur_offset;

			add_linked_list((unsigned long long int)cur_mmap->base_addr, (unsigned long long int)cur_mmap->length,
				(int)cur_mmap->type);

			cur_offset = (unsigned long int *)(((unsigned long int)cur_offset) + cur_size);
		}
	}

	/* Modules */
	if(mb_header->flags & 0x8) {
		unsigned int i;
		struct mb_mod *mods = (struct mb_mod *)mb_header->mods_addr;
		for(i = 0; i < mb_header->mods_count; i++) {
			add_linked_list((unsigned long long int)mods[i].mod_start, (unsigned long long int)(mods[i].mod_end -
				mods[i].mod_start), Module);
		}
	}

	/* Elf sections */
	if(mb_header->flags & 0x20) {
		unsigned int i;

		for(i = 0; i < mb_header->elf_num; i++) {
			Elf32_Shdr *shdr = (Elf32_Shdr *)(mb_header->elf_addr + i * mb_header->elf_size);
			add_linked_list((unsigned long long int)shdr->sh_addr, (unsigned long long int)shdr->sh_size,
				TLoad);
		}
	}

	/* tload itself */
	{
		unsigned long long int tl_start = (unsigned long long int)(unsigned long int)&tload_start;
		unsigned long long int tl_end = (unsigned long long int)(unsigned long int)&tload_end;
		add_linked_list(tl_start, tl_end - tl_start, TLoad);
	}
}
#endif

#ifdef EFI
int identity_map_tload(EFI_LOADED_IMAGE *li, unsigned long long int *pml4t)
{
	unsigned long int tl_start = (unsigned long int)li->ImageBase;
	unsigned long int tl_end = tl_start + (unsigned long int)li->ImageSize;
	unsigned long int cur_page = tl_start & (~(PAGESIZE - 1));

#ifdef _DEBUG
	_puts("EFI: identity mapping tload from ", 0);
	puthex(tl_start);
	_puts(" to ", 0);
	puthex(tl_end);
	puts("");
#endif

	while(cur_page < tl_end)
	{
		create_virtual_mapping_from_page(cur_page, pml4t, PF_R | PF_W | PF_X, 1, (void *)cur_page, TLoad);
		cur_page += PAGESIZE;
	}

	return 0;
}
#else
int identity_map_tload(struct mb_header *mb_header, unsigned long long int *pml4t)
{
	/* Identity map the bootloader so that the bootloader is still mapped once we jump to long mode */
	unsigned long int i;

	for(i = 0; i < mb_header->elf_num; i++) {
		Elf32_Shdr *shdr;
		unsigned long int base;

		shdr = (Elf32_Shdr *)(i * mb_header->elf_size + mb_header->elf_addr);
		base = shdr->sh_addr & (~(PAGESIZE - 1));
		while(base < (shdr->sh_addr + shdr->sh_size)) {
			create_virtual_mapping_from_page(base, pml4t, PF_R | PF_W | PF_X, 1, (void *)base, TLoad);
			base += PAGESIZE;
		}
	}

	/* Identity map the tload heap */
	{
		unsigned long int base;
		
		for(base = heap_start; base < heap_end; base += PAGESIZE)
			create_virtual_mapping_from_page(base, pml4t, PF_R | PF_W | PF_X, 1, (void *)base, TLoad);
	}

	/* Identity map using an alternate algorithm */
	{
		unsigned long int tl_start = (unsigned long int)&tload_start;
		unsigned long int tl_end = (unsigned long int)&tload_end;
		unsigned long int cur_page = tl_start & (~(PAGESIZE - 1));

		while(cur_page < tl_end)
		{
			create_virtual_mapping_from_page(cur_page, pml4t, PF_R | PF_W | PF_X, 1, (void *)cur_page, TLoad);
			cur_page += PAGESIZE;
		}
	}

	return 0;
}
#endif

#ifdef _PROFILE
const char *profile(const char *msg)
{
	static unsigned long int prev_high = 0;
	static unsigned long int prev_low = 0;

	unsigned long int high = 0;
	unsigned long int low = 0;

	unsigned long int delta_high = 0;
	unsigned long int delta_low = 0;

	__asm__ __volatile__ ("rdtsc" : "=d"(high), "=a"(low));

	delta_high = high - prev_high;
	delta_low = low - prev_low;

	prev_high = high;
	prev_low = low;

	_puts(msg, 1);
	_puts("Abs: 0x", 0);
	_puthex(high);
	_puthex(low);
	_puts("  Delta: 0x", 0);
	_puthex(delta_high);
	_puthex(delta_low);
	_puts("", 1);

	return msg;
}
#else
const char * profile(const char *msg)
{
	return msg;		/* prevent gcc complaining about unused parameters in a portable way */
}
#endif
