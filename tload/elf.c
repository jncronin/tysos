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

#include "elf.h"
#include "console.h"
#include "vmem.h"
#include "stdlib.h"
#include "mboot.h"
#include "kif.h"
#include "memmgr.h"
#include <stdint.h>

#define DEFAULT_PIE_ALIGN		0x100000
#define DEFAULT_NONPIE_ALIGN	0x1000

static unsigned long int pie_base;
static void *k;
Elf64_Ehdr *ehdr;
Elf64_Shdr *strtab = NULL;
static Elf64_Shdr *shstrtab = NULL;
Elf64_Shdr *symtab = NULL;

static char *dynstrtab = NULL;
static Elf64_Sym *dynsymtab = NULL;

static Elf64_Dyn *dyntab = NULL;

int load_elf64_kernel(void *kernel, unsigned long long int *pml4t, unsigned long long int *e_point,
					  unsigned long long int *load_offset, unsigned long long int *kernel_vsize)
{
	int is_pie = 0;
	unsigned long long int image_start = 0xffffffff, image_end = 0;
	unsigned long int image_length;
	Elf64_Half phdr_idx;
	Elf64_Half shdr_idx;

	*e_point = 0;
	ehdr = (Elf64_Ehdr *)kernel;
	k = kernel;
	
	/* check this is an elf64 file */
	if((ehdr->e_ident[EI_MAG0] != 0x7f) || (ehdr->e_ident[EI_MAG1] != 'E') || (ehdr->e_ident[EI_MAG2] != 'L') ||
		(ehdr->e_ident[EI_MAG3] != 'F')) {
			puts("Error: not an ELF file");
			return -1;
	}
	if(ehdr->e_ident[EI_CLASS] != ELFCLASS64) {
		puts("Error: not an ELF64 file");
		return -1;
	}
	if(ehdr->e_ident[EI_DATA] != ELFDATA2LSB) {
		puts("Error: not an ELF64 LSB file");
		return -1;
	}

	/* determine the start and end addresses of the image, and thus its length */
	for(phdr_idx = 0; phdr_idx < ehdr->e_phnum; phdr_idx++) {
		Elf64_Phdr *phdr = (Elf64_Phdr *)(phdr_idx * ehdr->e_phentsize + (unsigned long int)ehdr->e_phoff +
			(unsigned long int)kernel);

		if(phdr->p_type == PT_LOAD) {
			if(phdr->p_vaddr < image_start)
				image_start = phdr->p_vaddr;
			if((phdr->p_vaddr + phdr->p_memsz) > image_end)
				image_end = phdr->p_vaddr + phdr->p_memsz;
		}
	}
	image_length = (unsigned long int)(image_end - image_start);
	if(image_length % DEFAULT_PIE_ALIGN)
		image_length = image_length - (image_length % DEFAULT_PIE_ALIGN) + DEFAULT_PIE_ALIGN;
#ifdef _DEBUG
	_puts("tysos: s: ", 0);
	puthex(image_start);
	_puts("  e: ", 0);
	puthex(image_end);
	_puts("  l: ", 0);
	puthex(image_length);
	_puts("", 1);
#endif

	/* determine whether its position-independent or not */
	if(ehdr->e_type == ET_EXEC)
		is_pie = 0;
	else if(ehdr->e_type == ET_DYN) {
		unsigned long int pie_excess;
		is_pie = 1;
		pie_base = (unsigned long int)malloc_align(image_length, DEFAULT_PIE_ALIGN, Tysos, 1);
		if(pie_base == (unsigned long int)NULL) {
			puts("Error: unable to allocate memory for kernel");
			return -1;
		}
		pie_excess = pie_base % DEFAULT_PIE_ALIGN;
		if(pie_excess != 0) {
			pie_base -= pie_excess;
			pie_base += DEFAULT_PIE_ALIGN;
		}

#ifdef _DEBUG
		_puts("load: ", 0);
		puthex(pie_base);
		_puts("-", 0);
		puthex(pie_base + image_length);
		_puts("", 1);
#endif
	}
	else {
		puts("Error: unknown file type (not ET_EXEC or ET_DYN)");
		return -1;
	}

	/* ensure its for the x86_64 processor */
	if(ehdr->e_machine != EM_X86_64) {
		puts("Error: not an x86_64 executable");
		return -1;
	}

	/* Iterate through the program headers, loading what is necessary */
	for(phdr_idx = 0; phdr_idx < ehdr->e_phnum; phdr_idx++) {
		Elf64_Phdr *phdr = (Elf64_Phdr *)(phdr_idx * ehdr->e_phentsize + (unsigned long int)ehdr->e_phoff +
			(unsigned long int)kernel);

		if((phdr->p_type == PT_LOAD) && (phdr->p_memsz > 0)) {
			/* seg_addr holds the current location within the segment.  It may be negative indicating a
				position before the segment */
			int64_t seg_addr;

			/* seg_v_start is the virtual load address of the segment */
			uint64_t seg_v_start;

			/* seg_v_start_align is seg_v_start aligned down to a multiple of PAGESIZE */
			uint64_t seg_v_start_align;

			/* seg_data_end is the virtual end address of the data within a segment */
			uint64_t seg_data_end;

			/* seg_bss_end is the virtual end address of the bss section */
			uint64_t seg_bss_end;

			/* seg_p_start is the physical location of the segment */
			uint64_t seg_p_start;

			seg_p_start = (uint64_t)(uintptr_t)kernel + phdr->p_offset;
			seg_v_start = phdr->p_vaddr;
			seg_v_start_align = seg_v_start & ~(PAGESIZE - 1);
			seg_data_end = phdr->p_vaddr + phdr->p_filesz;
			seg_bss_end = phdr->p_vaddr + phdr->p_memsz;

			seg_addr = -(seg_v_start - seg_v_start_align);

#ifdef _DEBUG
			_puts("Section: ", 0);
			puthex(phdr_idx);
			_puts(", seg_addr: ", 0);
			puthex(seg_addr);
			_puts(", seg_v_start: ", 0);
			puthex(seg_v_start);
			_puts(", seg_v_start_align: ", 0);
			puthex(seg_v_start_align);
			_puts(", seg_data_end: ", 0);
			puthex(seg_data_end);
			_puts(", seg_bss_end: ", 0);
			puthex(seg_bss_end);
			_puts(", seg_p_start: ", 0);
			puthex(seg_p_start);
			_puts("", 1);
#endif

			while(seg_addr < (int64_t)phdr->p_memsz)
			{
				/* At this point, seg_addr + seg_v_start should be page aligned.  We can perform a direct mapping if:
					1) seg_addr >= 0
					2) seg_addr + PAGESIZE < p_filesz
					3) seg_p_start + seg_addr is page aligned

				   Else, we have to map a new page and copy the appropriate bits
				*/

				uint64_t v_page_start = (uint64_t)((int64_t)seg_v_start + seg_addr);
				uint64_t p_page_start = (uint64_t)((int64_t)seg_p_start + seg_addr);

				if((seg_addr >= 0) && ((seg_addr + PAGESIZE) < (int64_t)phdr->p_filesz) &&
					((p_page_start & (PAGESIZE - 1)) == 0))
				{
					/* We can map a page directly from p_page_start to v_page_start */
					create_virtual_mapping_from_page(v_page_start, pml4t, phdr->p_flags, 1, (void*)(uintptr_t)p_page_start, Tysos);
					seg_addr += PAGESIZE;
				}
				else
				{
					/* We have to create a new page, then copy the data to it.
						If seg_addr < 0, do nothing
						Else If seg_addr < p_filesz, copy the data
						Else If seg_addr < p_memsz, write a zero
						Else do nothing (beyond the segment)
					*/

					int64_t page_index;
					unsigned char *new_page = (unsigned char *)create_virtual_mapping(v_page_start, pml4t, phdr->p_flags, Tysos);
					unsigned char *old_page = (unsigned char *)(uintptr_t)p_page_start;

					for(page_index = 0; page_index < PAGESIZE; page_index++, seg_addr++)
					{
						if(seg_addr < 0)
							continue;
						else if (seg_addr < (int64_t)phdr->p_filesz)
							new_page[page_index] = old_page[page_index];
						else if (seg_addr < (int64_t)phdr->p_memsz)
							new_page[page_index] = 0;
					}
				}
			}
		}
	}

	/* Identify the section name string table */
	shstrtab = (Elf64_Shdr *)(ehdr->e_shstrndx * ehdr->e_shentsize + (unsigned long int)ehdr->e_shoff + (unsigned long int)kernel);

	/* Identify the symbol string table and symbol tables */
	for(shdr_idx = 0; shdr_idx < ehdr->e_shnum; shdr_idx++) {
		Elf64_Shdr *shdr = (Elf64_Shdr *)(shdr_idx * ehdr->e_shentsize + (unsigned long int)ehdr->e_shoff +
			(unsigned long int)kernel);
		char *sect_name = (char *)(shdr->sh_name + (unsigned long int)shstrtab->sh_offset + (unsigned long int)kernel);

		if(shdr->sh_type == SHT_SYMTAB)
			symtab = shdr;
		if(!strcmp(".strtab", sect_name))
			strtab = shdr;
	}

	/* Identity the dynamic section */
	if(is_pie) {
		Elf64_Dyn *cur_dt;

		for(phdr_idx = 0; phdr_idx < ehdr->e_phnum; phdr_idx++) {
			Elf64_Phdr *phdr = (Elf64_Phdr *)(phdr_idx * ehdr->e_phentsize + (unsigned long int)ehdr->e_phoff +
				(unsigned long int)kernel);

			if(phdr->p_type == PT_DYNAMIC)
			{
				dyntab = (Elf64_Dyn *)(unsigned long int)(phdr->p_offset + (unsigned long int)kernel);
				break;
			}
		}

		if(dyntab == NULL)
		{
			puts("Error: unable to find dynamic section");
			return -1;
		}

		cur_dt = dyntab;

		while(cur_dt->d_tag != DT_NULL)
		{
			if(cur_dt->d_tag == DT_STRTAB)
				dynstrtab = (char *)(unsigned long int)(cur_dt->d_un.d_ptr + (unsigned long int)kernel);
			else if(cur_dt->d_tag == DT_SYMTAB)
				dynsymtab = (Elf64_Sym *)(unsigned long int)(cur_dt->d_un.d_ptr + (unsigned long int)kernel);

			cur_dt++;
		}

#ifdef _DEBUG
		_puts("dynstrtab: ", 0);
		puthex((unsigned long int)dynstrtab);
		_puts("  dynsymtab: ", 0);
		puthex((unsigned long int)dynsymtab);
		puts("");
#endif
	}

	/* Fix up dynamic relocations */
	if(is_pie) {
		for(shdr_idx = 0; shdr_idx < ehdr->e_shnum; shdr_idx++) {
			Elf64_Shdr *shdr = (Elf64_Shdr *)(shdr_idx * ehdr->e_shentsize + (unsigned long int)ehdr->e_shoff +
				(unsigned long int)kernel);

			if(shdr->sh_type == SHT_RELA) {
				Elf64_Off cur_off;

				for(cur_off = 0; cur_off < shdr->sh_size; cur_off += shdr->sh_entsize) {
					Elf64_Rela *rela = (Elf64_Rela *)(unsigned long int)(cur_off + shdr->sh_offset + (unsigned long int)kernel);

					if(ELF64_R_TYPE(rela->r_info) == R_X86_64_RELATIVE) {
						/* We need to set the 8-byte field at virtual address DEFAULT_PIE_OFFSET + rela->r_offset to
						DEFAULT_PIE_OFFSET + rela->r_addend */

						*((unsigned long long int *)(create_virtual_mapping(pie_base + rela->r_offset,
							pml4t, PF_W | PF_R, Tysos))) = pie_base + rela->r_addend;
					}
					else if(ELF64_R_TYPE(rela->r_info) == R_X86_64_64) {
						/* We need to set the 8-byte field at virtual address DEFAULT_PIE_OFFSET + rela->r_offset to S + A
						   
						   Where S = address of the symbol within the relocation ( = pie_base + symbol_offset)
						     (according to ELF64 docs - section 6 (Symbol table) - in executable and shared object files
							  the st_value field of a symbol contains the virtual address - i.e. it does not need to be added
							  to the section offset)
						   and A is the addend within the relocation */

						Elf64_Sym *sym = &dynsymtab[ELF64_R_SYM(rela->r_info)];
						char *sym_name = &dynstrtab[sym->st_name];
						Elf64_Half sym_sect_id = sym->st_shndx;
						unsigned long long int S;
						
						if(sym_sect_id == SHN_UNDEF)
						{
#ifdef _DEBUG
							puts("Error: attempting to relocate SHN_UNDEF symbol");
							puts(sym_name);
							__asm__ ("xchg %bx, %bx");
#endif
							continue;
						}

						S = pie_base + sym->st_value;

						*((unsigned long long int *)(create_virtual_mapping(pie_base + rela->r_offset,
							pml4t, PF_W | PF_R, Tysos))) = S + rela->r_addend;

#ifdef _DEBUG2
						_puts("Relocating: 0x", 0);
						_puthex((unsigned long int)((pie_base + rela->r_offset) >> 32));
						_puthex((unsigned long int)((pie_base + rela->r_offset) & 0xffffffff));
						puts("");
						_puts(sym_name, 0);
						_puts(" + ", 0);
						puthex(rela->r_addend);
						_puts(" to: 0x", 0);
						_puthex((unsigned long int)((S + rela->r_addend) >> 32));
						_puthex((unsigned long int)((S + rela->r_addend) & 0xffffffff));
						puts("");
						puts("");
						__asm__ ("xchg %bx, %bx");
#endif
					}
				}
			}
		}
	}

	/* Identify the entry point, first by looking for the symbol "_start" and then defaulting to ehdr->e_entry */
	if(symtab == NULL)
	{
#ifdef _DEBUG
		puts("Symbol table not found");
#endif
	}
	else if(strtab == NULL)
	{
#ifdef _DEBUG
		puts("String table not found");
#endif
	}
	else
	{
		Elf64_Off cur_off;
#ifdef _DEBUG
		_puts("symtab: ", 0);
		puthex((unsigned long int)symtab);
		_puts("  strtab: ", 0);
		puthex((unsigned long int)strtab);
		puts("");
#endif

		for(cur_off = 0; cur_off < symtab->sh_size; cur_off += symtab->sh_entsize) {
			Elf64_Sym *cur_sym = (Elf64_Sym *)(unsigned long int)(cur_off + symtab->sh_offset + (unsigned long int)kernel);
			char *sym_name = (char *)(cur_sym->st_name + (unsigned long int)strtab->sh_offset + (unsigned long int)kernel);
			if(!strcmp("_start", sym_name))
				*e_point = cur_sym->st_value;
		}	
	}
	if(*e_point == 0)
		*e_point = ehdr->e_entry;
	if(is_pie) {
		*e_point += pie_base;
		*load_offset = (unsigned long long int)pie_base;
	} else
		*load_offset = 0x0LL;

	*kernel_vsize = (unsigned long long int)image_length;

#ifdef _DEBUG
	_puts("epoint: ", 0);
	puthex((unsigned long int)*e_point);
	_puts("  load_offset: ", 0);
	puthex((unsigned long int)*load_offset);
	_puts("", 1);
#endif

	return 0;
}

uint64_t Get_Symbol_Addr(const char *name)
{
	Elf64_Off cur_off;
	static Elf64_Sym *prev_matching_sym = (Elf64_Sym *)0;
	if(symtab == NULL)
		puts("Get_Symbol_Addr failed: symtab not loaded");
	if(strtab == NULL)
		puts("Get_Symbol_Addr failed: strtab not loaded");

	/* Work on the premise that we often ask for the same symbol repeatedly, therefore cache
	   the last symbol which was found and test it first this time */

	if(prev_matching_sym != (Elf64_Sym *)0)
	{
		char *sym_name = (char *)(prev_matching_sym->st_name + (unsigned long int)strtab->sh_offset + (unsigned long int)k);
		if(!strcmp(name, sym_name))
			return prev_matching_sym->st_value;
	}

	for(cur_off = 0; cur_off < symtab->sh_size; cur_off += symtab->sh_entsize) {
		Elf64_Sym *cur_sym = (Elf64_Sym *)(unsigned long int)(cur_off + symtab->sh_offset + (unsigned long int)k);
		char *sym_name = (char *)(cur_sym->st_name + (unsigned long int)strtab->sh_offset + (unsigned long int)k);
		if(!strcmp(name, sym_name))
		{
			prev_matching_sym = cur_sym;
			return cur_sym->st_value;
		}
	}
	return 0;
}
