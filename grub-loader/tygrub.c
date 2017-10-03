/* tygrub.c - Tysos loading module for grub, based on hello.c  */
/*
 *  GRUB  --  GRand Unified Bootloader
 *  Copyright (C) 2003,2007  Free Software Foundation, Inc.
 *  Copyright (C) 2003  NIIBE Yutaka <gniibe@m17n.org>
 *
 *  GRUB is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  GRUB is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with GRUB.  If not, see <http://www.gnu.org/licenses/>.
 */

#ifdef _MSC_VER
#define __attribute__
#endif

#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <sys/types.h>

// prevent including grub's elf.h
#define GRUB_ELF_H 1

#include <grub/types.h>
#include <grub/misc.h>
#include <grub/mm.h>
#include <grub/err.h>
#include <grub/dl.h>
#include <grub/extcmd.h>
#include <grub/i18n.h>
#include <grub/file.h>
#include <grub/relocator.h>
#include <grub/term.h>
#include <grub/video.h>
#include <grub/multiboot.h>

#include <efi.h>

#include <zlib.h>
#include <elf.h>

#include <pmem_alloc.h>

#define DEBUG

char modname[] __attribute__((section(".modname"))) = "tygrub";

char moddeps[] __attribute__((section(".moddeps"))) = "relocator\0video\0vbe\0multiboot\0gfxterm";

GRUB_MOD_LICENSE("GPLv3+");

struct grub_relocator *grub_tygrub_relocator = NULL;

EFI_STATUS load_kernel(const char *fname, void **fobj, size_t(**fread_func)(void *, void *, size_t),
	int(**fclose_func)(void *), off_t(**fseek_func)(void *, off_t offset, int whence));
EFI_STATUS allocate_fixed(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate_any(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS *paddr_out);
EFI_STATUS add_start_hole(UINTPTR base, UINTPTR len);
EFI_STATUS elf64_map_kernel(Elf64_Ehdr **ehdr, void *fobj, size_t(*fread_func)(void *, void *, size_t), off_t(*fseek_func)(void *, off_t, int));
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
EFI_PHYSICAL_ADDRESS sym_tab_paddr, sym_tab_size, sym_tab_entsize, str_tab_paddr, str_tab_size, static_start, static_end;

void(*trampoline_func)(uint64_t target, uint64_t cr3, uint64_t _mbheader, uint64_t halt_func,
	uint64_t kernel_stack, uint64_t gdt, uint64_t kif);
extern char trampoline, trampoline_end;
uint64_t __halt_func;

uint64_t mb_adjust;

UINTPTR kernel_low;
UINTPTR kernel_high;

extern int v_width;
extern int v_height;
extern int v_bpp;

struct cfg_module
{
	const char *name;
	const char *path;
	struct cfg_module *next;
};

EFI_PHYSICAL_ADDRESS align(EFI_PHYSICAL_ADDRESS v, EFI_PHYSICAL_ADDRESS alignment)
{
	if (align == 0)
	{
		printf("warning: requested alignment of 0\n");
		return v;
	}
	if (v % alignment)
		return v + alignment - (v % alignment);
	else
		return v;
}

void press_key()
{
	printf("Press any key to continue...");
	grub_getkey();
	printf("\n");
}

static char *sym_name(Elf64_Sym *sym)
{
	return (char *)(uintptr_t)(str_tab_paddr + (EFI_PHYSICAL_ADDRESS)sym->st_name);
}

uint64_t Get_Symbol_Addr(const char *name)
{
	static Elf64_Sym *last_sym = NULL;

//#ifdef DEBUG
//	printf("Get_Symbol_Addr: sym_tab_paddr: %p, sym_tab_size: %p, sym_tab_entsize: %p\n",
//		(uintptr_t)sym_tab_paddr, (uintptr_t)sym_tab_size, (uintptr_t)sym_tab_entsize);
//#endif

	if (last_sym != NULL)
	{
		if (!strcmp(sym_name(last_sym), name))
		{
//#ifdef DEBUG
//			printf("Get_Symbol_Addr: returning cache'd value %p\n", (uintptr_t)last_sym->st_value);
//#endif
			return (uint64_t)last_sym->st_value;
		}
	}

	for (unsigned int i = 0; i < ((unsigned int)sym_tab_size / (unsigned int)sym_tab_entsize); i++)
	{
		Elf64_Sym *cur_sym = (Elf64_Sym *)(uintptr_t)(sym_tab_paddr + i * sym_tab_entsize);
		if (!strcmp(sym_name(cur_sym), name))
		{
			last_sym = cur_sym;
//#ifdef DEBUG
//			printf("Get_Symbol_Addr: returning found value %p\n", (uintptr_t)cur_sym->st_value);
//#endif

			return (uint64_t)cur_sym->st_value;
		}
	}

	printf("error: Get_Symbol_Addr - cannot find %s\n", name);
	return 0;
}

/* mmap iteration function */
static int mmap_iter(grub_uint64_t addr, grub_uint64_t size,
	grub_memory_type_t type, void *data)
{
	struct Multiboot_MemoryMap ***mmap_ia_ptr =
		(struct Multiboot_MemoryMap ***)data;

	printf("mmap: addr: %p", addr);
	printf(", size: %p", size);
	printf(", type: %d\n", type);

	struct Multiboot_MemoryMap **mmap_ia = *mmap_ia_ptr;
	*mmap_ia = (struct Multiboot_MemoryMap *)kmalloc(sizeof(struct Multiboot_MemoryMap));
	Init_Multiboot_MemoryMap(mmap_ia[0]);
	(*mmap_ia)->__vtbl += mb_adjust;

	(*mmap_ia)->base_addr = addr;
	(*mmap_ia)->length = size;

	switch (type)
	{
		case GRUB_MEMORY_AVAILABLE:
			(*mmap_ia)->type = UEfiConventionalMemory;
			break;
		case GRUB_MEMORY_CODE:
			(*mmap_ia)->type = UEfiLoaderCode;
			break;
		case GRUB_MEMORY_ACPI:
			(*mmap_ia)->type = UEfiACPIReclaimMemory;
			break;
		default:
			(*mmap_ia)->type = UEfiUnusableMemory;
			break;
	}

	uintptr_t new_ptr = (uintptr_t)(*mmap_ia_ptr);
	new_ptr += sizeof(INTPTR);
	*mmap_ia_ptr = (struct Multiboot_MemoryMap **)new_ptr;

	return 0;
}

static grub_err_t
grub_cmd_tygrub(grub_extcmd_context_t ctxt __attribute__((unused)),
	int argc __attribute__((unused)),
	char **args __attribute__((unused)))
{
	EFI_STATUS Status;

	grub_printf("\n\n%s\n", _("Tygrub Starting"));

	/* Parse config */
	parse_cfg_file();

	/* Load up the kernel */
	void *fobj;
	size_t(*fread_func)(void *, void *, size_t);
	int(*fclose_func)(void *);
	off_t(*fseek_func)(void *, off_t offset, int whence);

	Status = load_kernel(cfg_get_kpath(), &fobj, &fread_func, &fclose_func, &fseek_func);
	if (Status != EFI_SUCCESS)
	{
		press_key();
		return Status;
	}

	/* Load the elf headers and map kernel */
	Elf64_Ehdr *ehdr;
	Status = elf64_map_kernel(&ehdr, fobj, fread_func, fseek_func);
	if (Status != EFI_SUCCESS)
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

	for (int i = 0; i < ehdr->e_shnum; i++)
	{
		Elf64_Shdr *shdr = (Elf64_Shdr *)((uintptr_t)shdrs + i * ehdr->e_shentsize);
		if (shdr->sh_type == SHT_SYMTAB)
		{
			sym_tab_paddr = (EFI_PHYSICAL_ADDRESS)(uintptr_t)malloc(shdr->sh_size);
			sym_tab_size = (EFI_PHYSICAL_ADDRESS)shdr->sh_size;
			sym_tab_entsize = (EFI_PHYSICAL_ADDRESS)shdr->sh_entsize;
			fseek_func(fobj, shdr->sh_offset, SEEK_SET);
			fread_func(fobj, (void *)(uintptr_t)sym_tab_paddr, shdr->sh_size);

			Elf64_Shdr *strtab = (Elf64_Shdr *)((uintptr_t)shdrs + shdr->sh_link * ehdr->e_shentsize);
			str_tab_paddr = (EFI_PHYSICAL_ADDRESS)(uintptr_t)malloc(strtab->sh_size);
			str_tab_size = (EFI_PHYSICAL_ADDRESS)strtab->sh_size;
			fseek_func(fobj, strtab->sh_offset, SEEK_SET);
			fread_func(fobj, (void *)(uintptr_t)str_tab_paddr, strtab->sh_size);
			break;
		}
	}
	if (sym_tab_paddr == 0)
	{
		printf("error: could not find kernel symbol table\n");
		press_key();
		return GRUB_ERR_READ_ERROR;
	}

	fclose_func(fobj);

	/* Allocate identity-mapped space for the KIF, and an extra page for the new gdt/idt */
	EFI_PHYSICAL_ADDRESS p_kif;
	Status = alloc_code(0x5000, &p_kif);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate physical space for the kif\n");
		press_key();
		return GRUB_ERR_OUT_OF_MEMORY;
	}
	UINTPTR v_kif = (UINTPTR)p_kif;
	Status = allocate_fixed(v_kif, 0x5000, p_kif);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate virtual space for the kif\n");
		press_key();
		return GRUB_ERR_OUT_OF_MEMORY;
	}

	mb_adjust = (uint64_t)v_kif - (uint64_t)p_kif;
	printf("mb_adjust: %x (v_kif: %x, p_kif: %x)\n", mb_adjust, v_kif, p_kif);
	struct Multiboot_Header *mbheader;
	kif_init(p_kif, 0x4000, &mbheader);
	printf("kif at: %x\n", mbheader);

	/* Write out new IDT, IDTR, GDT and GDTRs starting at kif + 0x4000
	First 2048 bytes (0x800) is IDT (=256 * 8), then
	1024 bytes (0x400) for GDT (starting at 0x6800) then
	IDTR at 0x6c00, GDTR at 0x6d00 */
	uintptr_t gdt = (uintptr_t)p_kif + 0x4000;
	memset((void *)gdt, 0, 0x1000);
	/* GDT entries are:
	0		NULL
	1		code0
	2		data0
	3		code3
	4		data3
	5-127	null
	*/
	uint64_t *gdt_entry = (uint64_t *)(gdt + 0x800);
	mbheader->gdt = (uint64_t)(uintptr_t)gdt_entry;
	*gdt_entry++ = 0x0;
	*gdt_entry++ = 0x00af9a000000ffffULL;
	*gdt_entry++ = 0x00cf92000000ffffULL;
	*gdt_entry++ = 0x00affe000000ffffULL;
	*gdt_entry++ = 0x00cff2000000ffffULL;

	*(uint16_t *)(gdt + 0xc00) = 2047;
	*(uint64_t *)(gdt + 0xc02) = gdt;
	*(uint16_t *)(gdt + 0xd00) = 1023;
	*(uint64_t *)(gdt + 0xd02) = gdt + 0x800;

	/* Load modules */
	struct cfg_module *mod;
	int mod_count = cfg_get_modcount();
	struct __array *mod_array;
	struct Multiboot_Module **mod_ia =
		(struct Multiboot_Module **)Create_Ref_Array(&mod_array, mod_count);
	(*mod_ia)->__vtbl += mb_adjust;
	int cur_mod_idx = 0;
	while ((mod = cfg_iterate_modules()))
	{
		UINTPTR mod_addr, mod_len;
		mod_ia[cur_mod_idx] = (struct Multiboot_Module *)kmalloc(sizeof(struct Multiboot_Module));
		load_module(mod->path, &mod_addr, (size_t *)&mod_len, (EFI_PHYSICAL_ADDRESS *)&mod_ia[cur_mod_idx]->base_addr);
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
	mbheader->modules = (uint64_t)(uintptr_t)mod_array + mb_adjust;

	/* Allocate space for the kernel initial heap */
	UINTPTR tysos_heap;
	UINTPTR tysos_heap_len = 0x100000;
	Status = allocate(tysos_heap_len, &tysos_heap, NULL);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate tysos heap\n");
		return GRUB_ERR_OUT_OF_MEMORY;
	}
	mbheader->heap_start = tysos_heap;
	mbheader->heap_end = tysos_heap + tysos_heap_len;
	printf("tysos heap: %x - %x\n", (uintptr_t)tysos_heap, (uintptr_t)tysos_heap + (uintptr_t)tysos_heap_len);

	/* Allocate space for the stack */
	UINTPTR kernel_stack;
	UINTPTR kernel_stack_len = 0x2000;
	Status = allocate(kernel_stack_len, &kernel_stack, NULL);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't allocate kernel stack\n");
		return GRUB_ERR_OUT_OF_MEMORY;
	}
	printf("kernel stack at %p-%p\n", (uintptr_t)kernel_stack,
		(uintptr_t)(kernel_stack + kernel_stack_len));

	Status = add_start_hole(kernel_stack, 0x1000);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't add 1 page hole at start of stack region\n");
		return GRUB_ERR_OUT_OF_MEMORY;
	}

	/* Allocate for the identity-mapped trampoline code */
	EFI_PHYSICAL_ADDRESS p_tramp;
	Status = alloc_code(0x1000, &p_tramp);
	if (Status != EFI_SUCCESS)
	{
		printf("error: could not allocate physical space for trampoline code: %i\n", Status);
		press_key();
		return GRUB_ERR_OUT_OF_MEMORY;
	}
	Status = allocate_fixed((uint64_t)p_tramp, 0x1000, p_tramp);
	if (Status != EFI_SUCCESS)
	{
		printf("error: could not allocate virtual space for trampoline code: %i\n", Status);
		press_key();
		return GRUB_ERR_OUT_OF_MEMORY;
	}
	EFI_PHYSICAL_ADDRESS tramp_start = (EFI_PHYSICAL_ADDRESS)(uintptr_t)&trampoline;
	EFI_PHYSICAL_ADDRESS tramp_end = (EFI_PHYSICAL_ADDRESS)(uintptr_t)&trampoline_end;
	printf("trampoline from %x to %x (copied to %x)\n", (uintptr_t)tramp_start, (uintptr_t)tramp_end, (uintptr_t)p_tramp);
	memcpy((void *)(uintptr_t)p_tramp, (void *)(uintptr_t)tramp_start, (size_t)((uintptr_t)tramp_end - (uintptr_t)tramp_start));
	trampoline_func = (void(*)(uint64_t, uint64_t, uint64_t, uint64_t, uint64_t, uint64_t, uint64_t))(uintptr_t)p_tramp;
	__halt_func = Get_Symbol_Addr("__halt");

	/* Build the kernel page tables */
	EFI_PHYSICAL_ADDRESS pml4t;
	Status = build_page_tables(&pml4t);
	if (Status != EFI_SUCCESS)
	{
		printf("error: couldn't build page tables\n");
		press_key();
		return GRUB_ERR_OUT_OF_MEMORY;
	}
	printf("pmlt4 at %x\n", pml4t);

	/* Build the kif */
	CreateString((struct System_String **)&mbheader->loader_name, "tygrub");
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

	mbheader->machine_major_type = x86_64;
	mbheader->machine_minor_type = BIOS;
	mbheader->virt_bda = 0;

	char *buf;
	buf = grub_xasprintf("%dx%dx%d,%dx%d,800x600x16,640x480x8,auto",
		v_width, v_height, v_bpp,
		v_width, v_height);
	grub_printf("video string: %s\n", buf);
	grub_err_t r = grub_video_set_mode(buf, 0, 0);
	grub_printf("video result: %d\n", r);

	struct grub_video_mode_info vmi;
	grub_err_t vmierr = grub_video_get_info(&vmi);

	grub_printf("video mode: %dx%dx%d (%d) (%d)\n",
		vmi.width, vmi.height, vmi.bpp, vmi.mode_type, vmierr);

	grub_uint32_t map_entries = grub_get_multiboot_mmap_count();
	struct __array *mmap_array;
	struct Multiboot_MemoryMap **mmap_ia =
		(struct Multiboot_MemoryMap **)Create_Ref_Array(&mmap_array, map_entries);
	(*mmap_ia)->__vtbl += mb_adjust;
	mbheader->mmap = (uint64_t)(uintptr_t)mmap_array + mb_adjust;

	grub_mmap_iterate(mmap_iter, &mmap_ia);

	mmap_array->inner_array += mb_adjust;
	mmap_array->lobounds += mb_adjust;
	mmap_array->sizes += mb_adjust;

	printf("Success - running trampoline function\n");
	trampoline_func((uint64_t)ehdr->e_entry,
		(uint64_t)pml4t, (uint64_t)(uintptr_t)mbheader + mb_adjust,
		(uint64_t)__halt_func,
		(uint64_t)(kernel_stack + kernel_stack_len),
		(uint64_t)gdt,
		(uint64_t)p_kif);

	while (1);

	return 0;
}

static grub_extcmd_t cmd;

GRUB_MOD_INIT(tygrub)
{
	cmd = grub_register_extcmd("tygrub", grub_cmd_tygrub, 0, 0,
		N_("Load tysos from config file."), 0);

	grub_relocator_unload(grub_tygrub_relocator);
	grub_tygrub_relocator = grub_relocator_new();
}

GRUB_MOD_FINI(tygrub)
{
	grub_unregister_extcmd(cmd);

	grub_relocator_unload(grub_tygrub_relocator);
	grub_tygrub_relocator = NULL;
}
