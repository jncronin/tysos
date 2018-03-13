/* Copyright (C) 2014 by John Cronin <jncronin@tysos.org>
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

#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <efi.h>
#include <efilib.h>
#include <sys/types.h>

#include <pmem_alloc.h>

#include "elf.h"

int elf_request_memory(uintptr_t base, uintptr_t size);

EFI_STATUS allocate_fixed(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate_any(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS *paddr_out);
EFI_PHYSICAL_ADDRESS get_pmem_for_vmem(UINTPTR vaddr);

extern UINTPTR kernel_low;
extern UINTPTR kernel_high;
extern EFI_PHYSICAL_ADDRESS static_start;
extern EFI_PHYSICAL_ADDRESS static_end;

EFI_STATUS elf64_map_kernel(Elf64_Ehdr **ehdr, void *fobj, size_t (*fread_func)(void *fobj, void *buf, size_t len),
							off_t (*fseek_func)(void *fobj, off_t offset, int whence))
{
	EFI_STATUS Status;

	kernel_low = ~(UINTPTR)0;
	kernel_high = 0;

	static_start = 0;
	static_end = 0;

	/* Load up the elf header */
	*ehdr = (Elf64_Ehdr *)malloc(sizeof(Elf64_Ehdr));
	fread_func(fobj, (void *)*ehdr, sizeof(Elf64_Ehdr));

	if((*ehdr)->e_ident[EI_CLASS] != ELFCLASS64)
	{
		printf("Error: kernel is not an Elf64 file\n");
		return EFI_ABORTED;
	}

	/* Load up the program headers */
	void *phdrs = malloc((*ehdr)->e_phnum * (*ehdr)->e_phentsize);
	fseek_func(fobj, (*ehdr)->e_phoff, SEEK_SET);
	fread_func(fobj, phdrs, (*ehdr)->e_phnum * (*ehdr)->e_phentsize);

	/* Iterate through them, loading as necessary */
	for(int i = 0; i < (*ehdr)->e_phnum; i++)
	{
		Elf64_Phdr *phdr = (Elf64_Phdr *)(uintptr_t)((EFI_PHYSICAL_ADDRESS)(uintptr_t)phdrs + i * (*ehdr)->e_phentsize);
		if(phdr->p_type == PT_LOAD)
		{
			EFI_PHYSICAL_ADDRESS page_offset = phdr->p_vaddr & 0xfff;
			UINTPTR vpage_start = phdr->p_vaddr & ~0xfffULL;
			UINTPTR v_length = phdr->p_vaddr + phdr->p_memsz - vpage_start;
			if(v_length & 0xfff)
				v_length = (v_length + 0x1000) & ~0xfffULL;

			UINTPTR seg_low = phdr->p_vaddr;
			UINTPTR seg_high = phdr->p_vaddr + phdr->p_memsz;
			if(seg_low < kernel_low)
				kernel_low = seg_low;
			if(seg_high > kernel_high)
				kernel_high = seg_high;

			int coffset = (int)fseek_func(fobj, phdr->p_offset, SEEK_SET);
			printf("seeked to offset %x\n", coffset);

			/* Is the first page mapped? (segments pages can overlap) */
			EFI_PHYSICAL_ADDRESS first_page_paddr = get_pmem_for_vmem(vpage_start);
			UINTPTR next_page_vaddr = vpage_start;
			size_t seg_offset = 0;
			printf("first_page_paddr: %p, page_offset: %p, vpage_start: %p, v_length: %p\n", (uintptr_t)first_page_paddr, (uintptr_t)page_offset, (uintptr_t)vpage_start, (uintptr_t)v_length);
			if(first_page_paddr)
			{
				size_t to_load = 0x1000 - page_offset;
				if(to_load > phdr->p_filesz)
					to_load = phdr->p_filesz;
				size_t to_load_r = fread_func(fobj, (void *)(uintptr_t)(first_page_paddr + page_offset), to_load);
				printf("first_page: requested %x, loaded %x\n", (int)to_load, (int)to_load_r);
				seg_offset += to_load;
				
				//size_t to_zero = 0x1000 - page_offset - to_load;
				//memset((void *)(uintptr_t)(first_page_paddr + page_offset + seg_offset), 0, to_zero);
				//seg_offset += to_load;
				
				next_page_vaddr += 0x1000;
				v_length -= 0x1000;
				page_offset = 0;
			}

			/* Now allocate the rest of the segment */
			EFI_PHYSICAL_ADDRESS next_paddr;
			if (v_length)
			{
				Status = alloc_code(v_length, &next_paddr);
				if (Status != EFI_SUCCESS)
				{
					printf("error allocating pages for segment: %i\n", Status);
					return Status;
				}
				Status = allocate_fixed(next_page_vaddr, v_length, next_paddr);
				if (Status != EFI_SUCCESS)
				{
					printf("error allocating virtual space for segment: %i\n", Status);
					return Status;
				}

				size_t rest_to_load = phdr->p_filesz - seg_offset;
				EFI_PHYSICAL_ADDRESS cur_paddr = next_paddr + page_offset;
				size_t rest_to_load_r = fread_func(fobj, (void *)(uintptr_t)cur_paddr, rest_to_load);
				printf("rest: requested %x, loaded %x\n", (int)rest_to_load, (int)rest_to_load_r);
				cur_paddr += rest_to_load;
				size_t rest_to_zero = phdr->p_memsz - phdr->p_filesz;
				memset((void *)(uintptr_t)cur_paddr, 0, rest_to_zero);
			}

			/* Find all writeable sections - we need to add them as GC roots */
			if ((phdr->p_flags & PF_W) != 0)
			{
				printf("elf: writeable section found from %x to %x\n",
					phdr->p_vaddr, phdr->p_vaddr + phdr->p_memsz);
				if (static_start != 0)
				{
					printf("error: more than one writeable section found in file\n");
					return EFI_ABORTED;
				}
				static_start = (EFI_PHYSICAL_ADDRESS)phdr->p_vaddr;
				static_end = (EFI_PHYSICAL_ADDRESS)(phdr->p_vaddr + phdr->p_memsz);
			}
		}
	}

	return EFI_SUCCESS;
}

EFI_STATUS elf64_get_shdr(Elf64_Ehdr *ehdr, int idx, Elf64_Shdr **shdr)
{
	if(shdr == NULL)
		return EFI_INVALID_PARAMETER;
	if(idx < 0 || idx >= ehdr->e_shnum)
		return EFI_INVALID_PARAMETER;

	*shdr = (Elf64_Shdr *)((uintptr_t)ehdr + (uintptr_t)ehdr->e_shoff + (uintptr_t)(idx * ehdr->e_shentsize));
	return EFI_SUCCESS;
}

EFI_STATUS elf64_get_phdr(Elf64_Ehdr *ehdr, int idx, Elf64_Phdr **phdr)
{
	if(phdr == NULL)
		return EFI_INVALID_PARAMETER;
	if(idx < 0 || idx >= ehdr->e_shnum)
		return EFI_INVALID_PARAMETER;

	*phdr = (Elf64_Phdr *)((uintptr_t)ehdr + (uintptr_t)ehdr->e_phoff + (uintptr_t)(idx * ehdr->e_phentsize));
	return EFI_SUCCESS;
}