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
#include "tloadkif.h"
#include "elf.h"

int elf_request_memory(uintptr_t base, uintptr_t size);

EFI_STATUS allocate_fixed(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate_any(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS *paddr_out);

EFI_STATUS elf64_map_kernel(Elf64_Ehdr *ehdr)
{
	EFI_STATUS Status;

	if(ehdr->e_ident[EI_CLASS] != ELFCLASS64)
	{
		printf("Error: kernel is not an Elf64 file\n");
		return EFI_ABORTED;
	}

	for(int i = 0; i < ehdr->e_phnum; i++)
	{
		Elf64_Phdr *phdr;
		Status = elf64_get_phdr(ehdr, i, &phdr);
		if(Status != EFI_SUCCESS)
		{
			printf("failed to get segment header %i: \n", i, Status);
			return Status;
		}

		if(phdr->p_type == PT_LOAD)
		{
			printf("elf64: segment %i: offset: %x, vaddr: %x, filesz: %x, memsz: %x\n",
				i, phdr->p_offset, phdr->p_vaddr, phdr->p_filesz, phdr->p_memsz);

			EFI_PHYSICAL_ADDRESS cur_paddr = (EFI_PHYSICAL_ADDRESS)ehdr + (EFI_PHYSICAL_ADDRESS)phdr->p_offset;
			printf("paddr: %x\n", cur_paddr);

			if(cur_paddr & 0xfff)
			{
				printf("error: segment %i is not aligned on a page boundary\n", i);
				return EFI_ABORTED;
			}

			/* First directly map what we can */
			UINTPTR mappable_len = phdr->p_filesz & ~0xfffULL;
			UINTPTR mapped_len = 0;
			Status = allocate_fixed((UINTPTR)phdr->p_vaddr, mappable_len, cur_paddr);
			if(Status != EFI_SUCCESS)
			{
				printf("error: allocate_fixed failed: %i\n", Status);
				return Status;
			}

			/* Now handle the page containing some data and some bss (if any) */
			UINTPTR cur_vaddr = phdr->p_vaddr + mappable_len;
			cur_paddr += mappable_len;
			mapped_len += mappable_len;

			if(phdr->p_filesz > mappable_len)
			{
				EFI_PHYSICAL_ADDRESS extra_data_len = phdr->p_filesz - mappable_len;
				printf("%x data in transition phase\n", extra_data_len);
				EFI_PHYSICAL_ADDRESS transition_page;
				Status = BS->AllocatePages(AllocateAnyPages, EfiLoaderCode, 1, &transition_page);
				if(Status != EFI_SUCCESS)
				{
					printf("error: couldn't allocate transition page: %i\n", Status);
					return Status;
				}

				/* copy data to the transition page */
				memset((void *)transition_page, 0, 0x1000);
				memcpy((void *)transition_page, (void *)cur_paddr, (size_t)extra_data_len);

				/* map it */
				Status = allocate_fixed(cur_vaddr, 0x1000, cur_paddr);

				cur_paddr += 0x1000;
				cur_vaddr += 0x1000;
				mapped_len += 0x1000;
			}

			/* Now deal with the bss pages (if any) */
			if(phdr->p_memsz > mapped_len)
			{
				EFI_PHYSICAL_ADDRESS extra_bss_len = (EFI_PHYSICAL_ADDRESS)phdr->p_memsz - (EFI_PHYSICAL_ADDRESS)mapped_len;
				printf("%x data in pure bss section\n");
				if(extra_bss_len & 0xfff)
					extra_bss_len = (extra_bss_len + 0x1000) & ~0xfffULL;
				
				EFI_PHYSICAL_ADDRESS pbss;
				Status = BS->AllocatePages(AllocateAnyPages, EfiLoaderCode, extra_bss_len / 0x1000, &pbss);
				if(Status != EFI_SUCCESS)
				{
					printf("error: couldn't allocate bss pages: %i\n", Status);
					return Status;
				}

				memset((void *)pbss, 0, extra_bss_len);

				Status = allocate_fixed(cur_vaddr, extra_bss_len, pbss);
				if(Status != EFI_SUCCESS)
				{
					printf("error: couldn't map bss section: %i\n", Status);
					return Status;
				}
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