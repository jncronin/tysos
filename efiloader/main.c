/* Copyright (C) 2014 by John Cronin
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

#include <efi.h>
#include <efilib.h>
#include <efilibc.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <sys/types.h>
#include "tloadkif.h"
#include "zlib.h"
#include "elf.h"

EFI_STATUS load_kernel(const char *fname, void **fobj, size_t (**fread_func)(void *, void *, size_t),
					   int (**fclose_func)(void *), off_t (**fseek_func)(void *, off_t offset, int whence));
EFI_STATUS allocate_fixed(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate_any(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS *paddr_out);
EFI_STATUS elf64_map_kernel(Elf64_Ehdr **ehdr, void *fobj, size_t (*fread_func)(void *, void *, size_t), off_t (*fseek_func)(void *, off_t, int));
EFI_STATUS load_module(const char *fname, UINTPTR *addr, size_t *length, EFI_PHYSICAL_ADDRESS *paddr);
EFI_STATUS build_page_tables(EFI_PHYSICAL_ADDRESS *pml4t_out);
EFI_STATUS kif_init(EFI_PHYSICAL_ADDRESS p_kif, EFI_PHYSICAL_ADDRESS len, struct Multiboot_Header **mbheader);
EFI_STATUS parse_cfg_file();
const char *cfg_get_kpath();
const char *cfg_get_kcmdline();
struct cfg_module *cfg_iterate_modules();
int cfg_get_modcount();
void *kmalloc(size_t n);

EFI_PHYSICAL_ADDRESS elf_kernel;
int elf_kernel_len;
EFI_SYSTEM_TABLE *ST;
EFI_BOOT_SERVICES *BS;
EFI_PHYSICAL_ADDRESS sym_tab_paddr, sym_tab_size, sym_tab_entsize, str_tab_paddr, str_tab_size, static_start, static_end;

UINTPTR kernel_low;
UINTPTR kernel_high;

UINTPTR tls_start;

extern int v_width;
extern int v_height;
extern int v_bpp;

extern EFI_GUID GraphicsOutputProtocol;

void (*trampoline_func)(uint64_t target, uint64_t cr3, uint64_t _mbheader, uint64_t halt_func,
	uint64_t kernel_stack);
extern char trampoline, trampoline_end;
uint64_t __halt_func;

uint64_t mb_adjust;

static inline uint64_t rdmsr(uint32_t msr_id)
{
	uint64_t msr_value;
	asm volatile ("rdmsr" : "=A" (msr_value) : "c" (msr_id));
	return msr_value;
}

static inline void outb(uint16_t port, uint8_t val)
{
	asm volatile ( "outb %0, %1" : : "a"(val), "Nd"(port) );
}

static inline uint8_t inb(uint16_t port)
{
	uint8_t ret;
	asm volatile ( "inb %1, %0" : "=a"(ret) : "Nd"(port) );
	return ret;
}

#define PORT 0x3f8		// COM1
void init_serial()
{
	outb(PORT + 1, 0x00);    // Disable all interrupts
	outb(PORT + 3, 0x80);    // Enable DLAB (set baud rate divisor)
	outb(PORT + 0, 0x03);    // Set divisor to 3 (lo byte) 38400 baud
	outb(PORT + 1, 0x00);    //                  (hi byte)
	outb(PORT + 3, 0x03);    // 8 bits, no parity, one stop bit
	outb(PORT + 2, 0xC7);    // Enable FIFO, clear them, with 14-byte threshold
	outb(PORT + 4, 0x0B);    // IRQs enabled, RTS/DSR set
}

int is_transmit_empty() {
	return inb(PORT + 5) & 0x20;
}
 
void write_serial(char a) {
	while (is_transmit_empty() == 0);
 
	outb(PORT,a);
}

static void lidt(void *base, uint16_t limit)
{
	struct
	{
		uint16_t limit;
		uint64_t base;
	} __attribute__((packed)) IDTR;

	IDTR.limit = limit;
	IDTR.base = (uint64_t)base;
	asm ( "lidt (%0)" : : "p"(&IDTR) );
}

static void lgdt(void *base, uint16_t limit)
{
	struct
	{
		uint16_t limit;
		uint64_t base;
	} __attribute__((packed)) GDTR;

	GDTR.limit = limit;
	GDTR.base = (uint64_t)base;
	asm ( "lgdt (%0)" : : "p"(&GDTR) );
}

size_t debug_serial_fwrite(const void *ptr, size_t size, size_t nmemb, void *data)
{
	(void)data;
	char *buf = (char *)ptr;
	size_t to_xmit = size * nmemb;
	while(to_xmit--)
		write_serial(*buf++);
	return nmemb;
}

struct cfg_module
{
	const char *name;
	const char *path;
	struct cfg_module *next;
};

EFI_PHYSICAL_ADDRESS align(EFI_PHYSICAL_ADDRESS v, EFI_PHYSICAL_ADDRESS alignment)
{
	if(v % alignment)
		return v + alignment - (v % alignment);
	else
		return v;
}

void press_key()
{
	printf("Press any key to continue...");
	getchar();
	printf("\n");
}

static char *sym_name(Elf64_Sym *sym)
{
	return (char *)(str_tab_paddr + (EFI_PHYSICAL_ADDRESS)sym->st_name);
}

uint64_t Get_Symbol_Addr(const char *name)
{
	static Elf64_Sym *last_sym = NULL;

	if(last_sym != NULL)
	{
		if(!strcmp(sym_name(last_sym), name))
			return (uint64_t)last_sym->st_value;
	}

	for(unsigned int i = 0; i < (sym_tab_size / sym_tab_entsize); i++)
	{
		Elf64_Sym *cur_sym = (Elf64_Sym *)(sym_tab_paddr + i * sym_tab_entsize);
		if(!strcmp(sym_name(cur_sym), name))
		{
			last_sym = cur_sym;
			return (uint64_t)cur_sym->st_value;
		}
	}

	printf("error: Get_Symbol_Addr - cannot find %s\n", name);
	return 0;
}

EFI_STATUS efi_main(EFI_HANDLE ImageHandle, EFI_SYSTEM_TABLE *SystemTable)
{
	EFI_STATUS Status;

	ST = SystemTable;
	BS = ST->BootServices;

	efilibc_init(ImageHandle);
	printf("Tysos efiloader\n");

	/* Parse config */
	parse_cfg_file();

	/* Load up the kernel */
	void *fobj;
	size_t (*fread_func)(void *, void *, size_t);
	int (*fclose_func)(void *);
	off_t (*fseek_func)(void *, off_t offset, int whence);

	Status = load_kernel(cfg_get_kpath(), &fobj, &fread_func, &fclose_func, &fseek_func);
	if(Status != EFI_SUCCESS)
	{
		press_key();
		return Status;
	}

	/* Load the elf headers and map kernel */
	Elf64_Ehdr *ehdr;
	Status = elf64_map_kernel(&ehdr, fobj, fread_func, fseek_func);
	if(Status != EFI_SUCCESS)
	{
		press_key();
		return Status;
	}

	/* Initialize the symbol + string tables */
	sym_tab_paddr = 0;
	str_tab_paddr = 0;
	void *shdrs = malloc(ehdr->e_shnum * ehdr->e_shentsize);
	fseek_func(fobj, ehdr->e_shoff, SEEK_SET);
	fread_func(fobj, shdrs, ehdr->e_shnum * ehdr->e_shentsize);

	for(int i = 0; i < ehdr->e_shnum; i++)
	{
		Elf64_Shdr *shdr = (Elf64_Shdr *)((EFI_PHYSICAL_ADDRESS)shdrs + i * ehdr->e_shentsize);
		if(shdr->sh_type == SHT_SYMTAB)
		{
			sym_tab_paddr = (EFI_PHYSICAL_ADDRESS)malloc(shdr->sh_size);
			if (sym_tab_paddr == 0)
			{
				printf("error: couldn't allocate space for kernel symbol table\n");
				return EFI_OUT_OF_RESOURCES;
			}
			sym_tab_size = (EFI_PHYSICAL_ADDRESS)shdr->sh_size;
			sym_tab_entsize = (EFI_PHYSICAL_ADDRESS)shdr->sh_entsize;
			fseek_func(fobj, shdr->sh_offset, SEEK_SET);
			fread_func(fobj, (void *)sym_tab_paddr, shdr->sh_size);

			Elf64_Shdr *strtab = (Elf64_Shdr *)((EFI_PHYSICAL_ADDRESS)shdrs + shdr->sh_link * ehdr->e_shentsize);
			str_tab_paddr = (EFI_PHYSICAL_ADDRESS)malloc(strtab->sh_size);
			str_tab_size = (EFI_PHYSICAL_ADDRESS)strtab->sh_size;
			fseek_func(fobj, strtab->sh_offset, SEEK_SET);
			fread_func(fobj, (void *)str_tab_paddr, strtab->sh_size);
			break;
		}
	}
	if(sym_tab_paddr == 0)
	{
		printf("error: could not find kernel symbol table\n");
		press_key();
		return EFI_ABORTED;
	}

	fclose_func(fobj);

	/* Check for mtrr support (testing rdmsr) */
	uint64_t mtrr_cap = rdmsr(0xfe);
	printf("mtrr_cap: %x\n", mtrr_cap);

	/* Allocate identity-mapped space at 0x2000 for the KIF, and an extra page for the new gdt/idt */
	UINTPTR v_kif = 0x2000;
	EFI_PHYSICAL_ADDRESS p_kif = 0x2000;
	Status = BS->AllocatePages(AllocateAddress, EfiLoaderData, 5, &p_kif);
	if(Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate physical space for the kif\n");
		press_key();
		return Status;
	}
	Status = allocate_fixed(0x2000, 0x5000, p_kif);
	if(Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate virtual space for the kif\n");
		press_key();
		return Status;
	}

	mb_adjust = (uint64_t)v_kif - (uint64_t)p_kif;
	printf("mb_adjust: %x (v_kif: %x, p_kif: %x)\n", mb_adjust, v_kif, p_kif);
	struct Multiboot_Header *mbheader;
	kif_init(p_kif, 0x4000, &mbheader);
	printf("kif at: %x\n", mbheader);

	/* Write out new IDT, IDTR, GDT and GDTRs starting at 0x6000
		First 2048 bytes (0x800) is IDT (=256 * 8), then
		1024 bytes (0x400) for GDT (starting at 0x6800) then
		IDTR at 0x6c00, GDTR at 0x6d00 */
	memset((void *)0x6000, 0, 0x1000);
	/* GDT entries are:
		0		NULL
		1		code0
		2		data0
		3		code3
		4		data3
		5-127	null
	*/
	uint64_t *gdt_entry = (uint64_t *)0x6800;
	mbheader->gdt = (uint64_t)(EFI_PHYSICAL_ADDRESS)gdt_entry;
	*gdt_entry++ = 0x0;
	*gdt_entry++ = 0x00af9a000000ffffULL;
	*gdt_entry++ = 0x00cf92000000ffffULL;
	*gdt_entry++ = 0x00affe000000ffffULL;
	*gdt_entry++ = 0x00cff2000000ffffULL;

	*(uint16_t *)0x6c00 = 2047;
	*(uint64_t *)0x6c02 = 0x6000;
	*(uint16_t *)0x6d00 = 1023;
	*(uint64_t *)0x6d02 = 0x6800;


	/* Load modules */
	struct cfg_module *mod;
	int mod_count = cfg_get_modcount();
	struct __array *mod_array;
	struct Multiboot_Module **mod_ia =
		(struct Multiboot_Module **)Create_Ref_Array(&mod_array, mod_count);
	(*mod_ia)->__vtbl += mb_adjust;
	int cur_mod_idx = 0;
	while((mod = cfg_iterate_modules()))
	{
		UINTPTR mod_addr, mod_len;
		mod_ia[cur_mod_idx] = (struct Multiboot_Module *)kmalloc(sizeof(struct Multiboot_Module));
		load_module(mod->path, &mod_addr, &mod_len, (EFI_PHYSICAL_ADDRESS *)&mod_ia[cur_mod_idx]->base_addr);
		Init_Multiboot_Module(mod_ia[cur_mod_idx]);
		mod_ia[cur_mod_idx]->virt_base_addr = mod_addr;
		mod_ia[cur_mod_idx]->length = mod_len;
		mod_ia[cur_mod_idx]->__vtbl += mb_adjust;

		CreateString((struct System_String **)&(mod_ia[cur_mod_idx]->name), mod->name);
		(*(struct System_String **)&(mod_ia[cur_mod_idx]->name))->__vtbl += mb_adjust;
		mod_ia[cur_mod_idx]->name += mb_adjust;

		printf("module: %s at %x\n", mod->name, mod_addr);
		cur_mod_idx++;
	}
	mod_array->inner_array += mb_adjust;
	mod_array->lobounds += mb_adjust;
	mod_array->sizes += mb_adjust;
	mbheader->modules = (uint64_t)mod_array + mb_adjust;

	/* Allocate space for the kernel initial heap */
	UINTPTR tysos_heap;
	UINTPTR tysos_heap_len = 0x100000;
	Status = allocate(tysos_heap_len, &tysos_heap, NULL);
	if(Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate tysos heap\n");
		return Status;
	}
	mbheader->heap_start = tysos_heap;
	mbheader->heap_end = tysos_heap + tysos_heap_len;
	printf("tysos heap: %x - %x\n", tysos_heap, tysos_heap + tysos_heap_len);

	/* Allocate space for the stack */
	UINTPTR kernel_stack;
	UINTPTR kernel_stack_len = 0x8000;
	Status = allocate(kernel_stack_len, &kernel_stack, NULL);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate kernel stack\n");
		return Status;
	}
	printf("kernel stack at %x\n", kernel_stack);

	/* Allocate space at address 0x1000 for the identity-mapped trampoline code */
	EFI_PHYSICAL_ADDRESS p_tramp = 0x1000;
	Status = BS->AllocatePages(AllocateAddress, EfiLoaderCode, 1, &p_tramp);
	if(Status != EFI_SUCCESS)
	{
		printf("error: could not allocate physical space for trampoline code: %i\n", Status);
		press_key();
		return Status;
	}
	Status = allocate_fixed(0x1000, 0x1000, 0x1000);
	if(Status != EFI_SUCCESS)
	{
		printf("error: could not allocate virtual space for trampoline code: %i\n", Status);
		press_key();
		return Status;
	}
	EFI_PHYSICAL_ADDRESS tramp_start = (EFI_PHYSICAL_ADDRESS)&trampoline;
	EFI_PHYSICAL_ADDRESS tramp_end = (EFI_PHYSICAL_ADDRESS)&trampoline_end;
	printf("trampoline from %x to %x\n", tramp_start, tramp_end);
	memcpy((void *)p_tramp, (void *)tramp_start, tramp_end - tramp_start);
	trampoline_func = (void (*)(uint64_t, uint64_t, uint64_t, uint64_t, uint64_t))0x1000;
	__halt_func = Get_Symbol_Addr("__halt");

	/* Build the kernel page tables */
	EFI_PHYSICAL_ADDRESS pml4t;
	Status = build_page_tables(&pml4t);
	if(Status != EFI_SUCCESS)
	{
		printf("error: couldn't build page tables\n");
		press_key();
		return Status;
	}
	printf("pmlt4 at %x\n", pml4t);

	/* Build the kif */
	CreateString((struct System_String **)&mbheader->loader_name, "efiloader");
	(*((struct System_String **)&mbheader->loader_name))->__vtbl += mb_adjust;
	mbheader->loader_name += mb_adjust;
	CreateString((struct System_String **)&mbheader->cmdline, cfg_get_kcmdline());
	(*((struct System_String **)&mbheader->cmdline))->__vtbl += mb_adjust;
	mbheader->cmdline += mb_adjust;
	mbheader->has_vga = 0;
	mbheader->tysos_paddr = (uint64_t)elf_kernel;
	mbheader->tysos_str_tab_paddr = str_tab_paddr;
	mbheader->tysos_str_tab_size = str_tab_size;
	mbheader->tysos_sym_tab_entsize = sym_tab_entsize;
	mbheader->tysos_sym_tab_paddr = sym_tab_paddr;
	mbheader->tysos_sym_tab_size = sym_tab_size;
	mbheader->tysos_virtaddr = kernel_low;
	mbheader->tysos_size = kernel_high - kernel_low;
	mbheader->tysos_static_start = static_start & ~0x7ULL;
	mbheader->tysos_static_end = static_end;
	mbheader->stack_low = kernel_stack;
	mbheader->stack_high = kernel_stack + kernel_stack_len;
	
#ifdef __x86_64
	mbheader->machine_major_type = x86_64;
#else
#ifdef __i386
	mbheader->machine_major_type = x86;
#else
#error Unknown machine type
#endif
#endif
	mbheader->machine_minor_type = UEFI;
	mbheader->virt_bda = (uint64_t)(uintptr_t)ST;

	/* Try and set a video mode */
	printf("Setting video mode %ix%ix%i\n", v_width, v_height, v_bpp);
	EFI_GRAPHICS_OUTPUT_PROTOCOL *gop_handle = NULL;
	int v_set = 0;
	Status = BS->OpenProtocol(ST->ConsoleOutHandle, &GraphicsOutputProtocol,
		(void**)&gop_handle, ImageHandle, NULL, EFI_OPEN_PROTOCOL_BY_HANDLE_PROTOCOL);
	if (Status == EFI_UNSUPPORTED)
	{
		/* ConsoleOut does not support GOP - try and find all GOP devices */
		UINTN gop_handles_n;
		EFI_HANDLE *gop_handles;
		Status = BS->LocateHandleBuffer(ByProtocol, &GraphicsOutputProtocol,
			NULL, &gop_handles_n, &gop_handles);
		if (Status == EFI_SUCCESS)
		{
			for (UINTN gop_handle_i = 0; gop_handle_i < gop_handles_n; gop_handle_i++)
			{
				Status = BS->OpenProtocol(gop_handles[gop_handle_i],
					&GraphicsOutputProtocol, (void **)&gop_handle, ImageHandle,
					NULL, EFI_OPEN_PROTOCOL_BY_HANDLE_PROTOCOL);
				if (Status == EFI_SUCCESS)
					break;
			}
		}
		BS->FreePool(gop_handles);
	}
	if (gop_handle)
	{
		UINT32 maxmode = gop_handle->Mode->MaxMode;
		for (UINT32 mode_i = 0; mode_i < maxmode; mode_i++)
		{
			EFI_GRAPHICS_OUTPUT_MODE_INFORMATION *mod_info;
			UINTN mod_info_sz;
			Status = gop_handle->QueryMode(gop_handle, mode_i, &mod_info_sz,
				&mod_info);
			v_set = 1;
			if (Status == EFI_SUCCESS)
			{
				/* See if the current mode information fits our requirements */
				if (mod_info->PixelFormat == PixelBltOnly)
					v_set = 0;
				if (mod_info->HorizontalResolution != (UINT32)v_width)
					v_set = 0;
				if (mod_info->VerticalResolution != (UINT32)v_height)
					v_set = 0;
				if (v_bpp != 32)
					v_set = 0;
				if (mod_info->PixelFormat != PixelBlueGreenRedReserved8BitPerColor &&
					mod_info->PixelFormat != PixelRedGreenBlueReserved8BitPerColor)
					v_set = 0;			
			}
			BS->FreePool(mod_info);

			if (v_set)
			{
				Status = gop_handle->SetMode(gop_handle, mode_i);
				if (Status == EFI_SUCCESS)
				{
					init_serial();
					efilibc_register_stderr_fwrite(debug_serial_fwrite, NULL);
					efilibc_register_stdout_fwrite(debug_serial_fwrite, NULL);

					switch (gop_handle->Mode->Info->PixelFormat)
					{
					case	 PixelBlueGreenRedReserved8BitPerColor:
						mbheader->fb_pixelformat = 2;
						break;
					case	 PixelRedGreenBlueReserved8BitPerColor:
						mbheader->fb_pixelformat = 1;
						break;
					default:
						break;
					}
					mbheader->fb_bpp = 32;
					mbheader->fb_w = gop_handle->Mode->Info->HorizontalResolution;
					mbheader->fb_h = gop_handle->Mode->Info->VerticalResolution;
					mbheader->fb_stride = gop_handle->Mode->Info->PixelsPerScanLine;
					mbheader->fb_base = (uint64_t)gop_handle->Mode->FrameBufferBase;

					printf("Set video mode %ix%ix%i @ %x\n",
						mbheader->fb_w, mbheader->fb_h, mbheader->fb_bpp,
						mbheader->fb_base);

					break;
				}
			}
		}
	}


	//press_key();

	/* Get the efi memory map */
	UINTN map_size = 0;
	EFI_PHYSICAL_ADDRESS mmap;
	UINTN map_key;
	UINTN desc_size;
	UINT32 desc_ver;
	Status = BS->GetMemoryMap(&map_size, (EFI_MEMORY_DESCRIPTOR *)mmap, &map_key, &desc_size, &desc_ver);
	if(Status != EFI_BUFFER_TOO_SMALL)
	{
		printf("error: GetMemoryMap failed: %i\n", Status);
		return Status;
	}
	map_size += map_size / 2;	// Increase the map size a bit to account for the following allocation
	Status = BS->AllocatePool(EfiLoaderData, map_size, (void **)&mmap);
	if(Status != EFI_SUCCESS)
	{
		printf("error: could not allocate space for EFI memory map: %i\n", Status);
		return Status;
	}
	Status = BS->GetMemoryMap(&map_size, (EFI_MEMORY_DESCRIPTOR *)mmap, &map_key, &desc_size, &desc_ver);
	if(Status != EFI_SUCCESS)
	{
		printf("error: GetMemoryMap failed: %i\n", Status);
		return Status;
	}
	if(desc_ver != 1)
	{
		printf("error: unknown descriptor format: %i\n", desc_ver);
		return EFI_ABORTED;
	}
	Status = BS->ExitBootServices(ImageHandle, map_key);
	if(Status != EFI_SUCCESS)
	{
		printf("error: ExitBootServices failed: %i\n", Status);
		return Status;
	}

	efilibc_register_external_malloc(kmalloc);
	init_serial();
	efilibc_register_stderr_fwrite(debug_serial_fwrite, NULL);
	efilibc_register_stdout_fwrite(debug_serial_fwrite, NULL);
	printf("ExitBootServices complete - switching to native serial port driver\n");

	lidt((void *)0, 0);
	lgdt((void *)0, 0);

	UINTN map_entries = map_size / desc_size;
	printf("EFI memory map has %i entries\n", map_entries);
	struct __array *mmap_array;
	struct Multiboot_MemoryMap **mmap_ia =
		(struct Multiboot_MemoryMap **)Create_Ref_Array(&mmap_array, map_entries);
	(*mmap_ia)->__vtbl += mb_adjust;
	mbheader->mmap = (uint64_t)mmap_array + mb_adjust;

	for(UINTN map_idx = 0; map_idx < map_entries; map_idx++)
	{
		mmap_ia[map_idx] = (struct Multiboot_MemoryMap *)kmalloc(sizeof(struct Multiboot_MemoryMap));
		Init_Multiboot_MemoryMap(mmap_ia[map_idx]);
		mmap_ia[map_idx]->__vtbl += mb_adjust;

		EFI_MEMORY_DESCRIPTOR *efi_md = (EFI_MEMORY_DESCRIPTOR *)(mmap + map_idx * desc_size);
		printf("Mapping idx %i: base_addr: %x, length: %x, type: %i\n", map_idx, efi_md->PhysicalStart, efi_md->NumberOfPages * 0x1000, efi_md->Type);

		mmap_ia[map_idx]->base_addr = efi_md->PhysicalStart;
		mmap_ia[map_idx]->length = efi_md->NumberOfPages * 0x1000;
		mmap_ia[map_idx]->type = (int32_t)efi_md->Type;
	}
	mmap_array->inner_array += mb_adjust;
	mmap_array->lobounds += mb_adjust;
	mmap_array->sizes += mb_adjust;

	printf("Success - running trampoline function\n");
	trampoline_func((uint64_t)ehdr->e_entry, (uint64_t)pml4t, (uint64_t)mbheader + mb_adjust, (uint64_t)__halt_func,
		(uint64_t)(kernel_stack + kernel_stack_len));
	while(1);

	return EFI_SUCCESS;
}
