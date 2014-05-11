/* Copyright (C) 2013 by John Cronin <jncronin@tysos.org>
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
#include "elf.h"

static int elf32_read_ehdr(FILE *fp, Elf32_Ehdr *ehdr);
static int elf64_read_ehdr(FILE *fp, Elf64_Ehdr *ehdr);

int elf_request_memory(uintptr_t base, uintptr_t size);

int elf_read_ehdr(FILE *fp, void **ehdr, int *elf_class, int arch, int endianness)
{
	fseek(fp, 0, SEEK_SET);

	uint8_t elf_id[20];
	if(fread(&elf_id, 1, 20, fp) != 20)
		return ELF_FILE_LOAD_ERROR;

	/* Confirm its an ELF file */
	if((elf_id[EI_MAG0] != 0x7f) || (elf_id[EI_MAG1] != 'E') ||
		(elf_id[EI_MAG2] != 'L') || (elf_id[EI_MAG3] != 'F'))
	{
		return ELF_NOT_ELF;
	}

	/* Determine the ELF class */
	uint8_t ec = elf_id[EI_CLASS];
	if(elf_class != NULL)
	{
		if(*elf_class == 0)
			*elf_class = ec;
		else if(*elf_class != ec)
			return ELF_BAD_CLASS;
	}

	*ehdr = (Elf32_Ehdr *)malloc(sizeof(Elf32_Ehdr));
	size_t bytes_to_read = sizeof(Elf32_Ehdr);
	size_t bytes_read = fread(*ehdr, 1, sizeof(Elf32_Ehdr), fp);
	if(bytes_to_read != bytes_read)
	{
		free(*ehdr);
		return ELF_FILE_LOAD_ERROR;
	}

	// Confirm its of the appropriate endianness
	if(elf_id[EI_DATA] != endianness)
		return ELF_BAD_ENDIAN;

	// Confirm its an executable file
	if(*(uint16_t *)&elf_id[16] != ET_EXEC)
		return ELF_NOT_EXEC;

	// Confirm its for the x86_64 architecture
	if(*(uint16_t *)&elf_id[18] != arch)
		return ELF_BAD_ARCH;

	fseek(fp, 0, SEEK_SET);

	/* Allocate the appropriate header */
	switch(ec)
	{
		case ELFCLASS32:
			*ehdr = malloc(sizeof(Elf32_Ehdr));
			if(ehdr == NULL)
				return ELF_NO_MEM;
			return elf32_read_ehdr(fp, (Elf32_Ehdr *)*ehdr);
			break;
		case ELFCLASS64:
			*ehdr = malloc(sizeof(Elf64_Ehdr));
			if(ehdr == NULL)
				return ELF_NO_MEM;
			return elf64_read_ehdr(fp, (Elf64_Ehdr *)*ehdr);
			break;
		default:
			return ELF_BAD_CLASS;
	}
}

int elf32_read_shdrs(FILE *fp, Elf32_Ehdr *ehdr, uint8_t **shdrs)
{
	size_t bytes_to_load = (size_t)(ehdr->e_shentsize * ehdr->e_shnum);
	fseek(fp, (long)ehdr->e_shoff, SEEK_SET);
	*shdrs = (uint8_t *)malloc(bytes_to_load);
	size_t bytes_read = fread(*shdrs, 1, bytes_to_load, fp);
	if(bytes_read != bytes_to_load)
	{
		free(*shdrs);
		return ELF_FILE_LOAD_ERROR;
	}
	return ELF_OK;
}

int elf64_read_shdrs(FILE *fp, Elf64_Ehdr *ehdr, uint8_t **shdrs)
{
	size_t bytes_to_load = (size_t)(ehdr->e_shentsize * ehdr->e_shnum);
	fseek(fp, (long)ehdr->e_shoff, SEEK_SET);
	*shdrs = (uint8_t *)malloc(bytes_to_load);
	size_t bytes_read = fread(*shdrs, 1, bytes_to_load, fp);
	if(bytes_read != bytes_to_load)
	{
		free(*shdrs);
		return ELF_FILE_LOAD_ERROR;
	}
	return ELF_OK;
}

int elf64_read_ehdr(FILE *fp, Elf64_Ehdr *ehdr)
{
	if(fread(ehdr, 1, sizeof(Elf64_Ehdr), fp) != sizeof(Elf64_Ehdr))
		return ELF_NOT_ELF;
	return ELF_OK;
}

int elf32_read_ehdr(FILE *fp, Elf32_Ehdr *ehdr)
{
	if(fread(ehdr, 1, sizeof(Elf32_Ehdr), fp) != sizeof(Elf32_Ehdr))
		return ELF_NOT_ELF;
	return ELF_OK;
}

int elf32_load_section(FILE *fp, Elf32_Shdr *shdr)
{
	/* Ensure we can read it */
	if(shdr->sh_addr == 0)
	{
		shdr->sh_addr = (Elf32_Addr)(uintptr_t)malloc((size_t)shdr->sh_size);
		if(shdr->sh_addr == 0)
			return ELF_NO_MEM;
	}
	else if(elf_request_memory((uintptr_t)shdr->sh_addr, (uintptr_t)shdr->sh_size) != 0)
		return ELF_ADDRESS_IN_USE;

	if(shdr->sh_type == SHT_NOBITS)
		memset((void*)(uintptr_t)shdr->sh_addr, 0, shdr->sh_size);
	else
	{
		if(!shdr->sh_offset)
			return ELF_NO_OFFSET;

		fseek(fp, (long)shdr->sh_offset, SEEK_SET);
		size_t bytes_to_read = (size_t)shdr->sh_size;
		size_t bytes_read = fread((void *)(uintptr_t)shdr->sh_addr,
				1, bytes_to_read, fp);
		if(bytes_to_read != bytes_read)
			return ELF_FILE_LOAD_ERROR;
	}
	return ELF_OK;
}

int elf64_load_section(FILE *fp, Elf64_Shdr *shdr)
{
	/* Ensure we can read it */
	if(shdr->sh_addr == 0)
	{
		shdr->sh_addr = (Elf64_Addr)(uintptr_t)malloc((size_t)shdr->sh_size);
		if(shdr->sh_addr == 0)
			return ELF_NO_MEM;
	}
	else if(elf_request_memory((uintptr_t)shdr->sh_addr, (uintptr_t)shdr->sh_size) != 0)
		return ELF_ADDRESS_IN_USE;

	if(shdr->sh_type == SHT_NOBITS)
		memset((void*)(uintptr_t)shdr->sh_addr, 0, shdr->sh_size);
	else
	{
		if(!shdr->sh_offset)
			return ELF_NO_OFFSET;

		fseek(fp, (long)shdr->sh_offset, SEEK_SET);
		size_t bytes_to_read = (size_t)shdr->sh_size;
		size_t bytes_read = fread((void *)(uintptr_t)shdr->sh_addr,
				1, bytes_to_read, fp);
		if(bytes_to_read != bytes_read)
			return ELF_FILE_LOAD_ERROR;
	}
	return ELF_OK;
}

int elf32_read_phdrs(FILE *fp, Elf32_Ehdr *ehdr, uint8_t **phdrs)
{
	size_t bytes_to_load = (size_t)(ehdr->e_phentsize * ehdr->e_phnum);
	fseek(fp, (long)ehdr->e_phoff, SEEK_SET);
	*phdrs = (uint8_t *)malloc(bytes_to_load);
	size_t bytes_read = fread(*phdrs, 1, bytes_to_load, fp);
	if(bytes_read != bytes_to_load)
	{
		free(*phdrs);
		return ELF_FILE_LOAD_ERROR;
	}
	return ELF_OK;
}

int elf64_read_phdrs(FILE *fp, Elf64_Ehdr *ehdr, uint8_t **phdrs)
{
	size_t bytes_to_load = (size_t)(ehdr->e_phentsize * ehdr->e_phnum);
	fseek(fp, (long)ehdr->e_phoff, SEEK_SET);
	*phdrs = (uint8_t *)malloc(bytes_to_load);
	size_t bytes_read = fread(*phdrs, 1, bytes_to_load, fp);
	if(bytes_read != bytes_to_load)
	{
		free(*phdrs);
		return ELF_FILE_LOAD_ERROR;
	}
	return ELF_OK;
}

int elf32_load_segment(FILE *fp, Elf32_Phdr *phdr)
{
	if(elf_request_memory((uintptr_t)phdr->p_filesz, (uintptr_t)phdr->p_memsz) != 0)
		return ELF_ADDRESS_IN_USE;

	uint32_t load_address = phdr->p_vaddr;
	if(phdr->p_filesz)
	{
		// Load the file image
		fseek(fp, (long)phdr->p_offset, SEEK_SET);
		size_t bytes_to_load = (size_t)phdr->p_filesz;
		size_t bytes_read = fread((void*)(uintptr_t)load_address, 1,
				bytes_to_load, fp);
		if(bytes_read != bytes_to_load)
			return ELF_FILE_LOAD_ERROR;
		load_address += phdr->p_filesz;
	}
	if(phdr->p_memsz - phdr->p_filesz)
	{
		// Zero out the rest of the memory image
		memset((void*)(uintptr_t)load_address, 0, phdr->p_memsz -
				phdr->p_filesz);
	}
	return ELF_OK;
}

int elf64_load_segment(FILE *fp, Elf64_Phdr *phdr)
{
	if(elf_request_memory((uintptr_t)phdr->p_filesz, (uintptr_t)phdr->p_memsz) != 0)
		return ELF_ADDRESS_IN_USE;

	uint32_t load_address = phdr->p_vaddr;
	if(phdr->p_filesz)
	{
		// Load the file image
		fseek(fp, (long)phdr->p_offset, SEEK_SET);
		size_t bytes_to_load = (size_t)phdr->p_filesz;
		size_t bytes_read = fread((void*)(uintptr_t)load_address, 1,
				bytes_to_load, fp);
		if(bytes_read != bytes_to_load)
			return ELF_FILE_LOAD_ERROR;
		load_address += phdr->p_filesz;
	}
	if(phdr->p_memsz - phdr->p_filesz)
	{
		// Zero out the rest of the memory image
		memset((void*)(uintptr_t)load_address, 0, phdr->p_memsz -
				phdr->p_filesz);
	}
	return ELF_OK;
}

static int _elf64_find_section(Elf64_Ehdr *ehdr, uint8_t *shdrs, int match_flags, char *name, Elf64_Word type, Elf64_Xword flags,
					   Elf64_Shdr **matched_shdrs, Elf64_Shdr *shstrtab)
{
	/* Count those sections which match, and optionally add them to the matched_shdrs array */
	Elf64_Half i;
	int count = 0;
	for(i = 0; i < ehdr->e_shnum; i++)
	{
		Elf64_Shdr *shdr;
		int eret = elf64_find_section_by_idx(NULL, ehdr, shdrs, i, &shdr);
		if(eret != ELF_OK)
			return eret;

		int match = 1;
		if(match_flags & MF_NAME)
		{
			/* Try and match name */
			char *sect_name = (char *)((uintptr_t)shstrtab->sh_addr + shdr->sh_name);
			if(strcmp(name, sect_name))
				match = 0;
		}
		if(match_flags & MF_TYPE)
		{
			/* Try and match type */
			if(shdr->sh_type != type)
				match = 0;
		}
		if(match_flags & MF_FLAGS)
		{
			/* Try and match all set flags */
			if((shdr->sh_flags & flags) != flags)
				match = 0;
		}

		if(match)
		{
			if(matched_shdrs != NULL)
				matched_shdrs[count] = shdr;
			count++;
		}
	}

	return count;
}

/* Find a section based on the value of match_flags */
int elf64_find_section(FILE *fp, Elf64_Ehdr *ehdr, uint8_t *shdrs, Elf64_Shdr *shstrtab, int match_flags, char *name, Elf64_Word type,
					   Elf64_Xword flags, Elf64_Shdr ***matched_shdrs, int *matched_count)
{
	/* Iterate through the sections looking for a match, this time just counting */
	int count = _elf64_find_section(ehdr, shdrs, match_flags, name, type, flags, NULL, shstrtab);

	/* Allocate memory for the buffer */
	*matched_shdrs = (Elf64_Shdr **)malloc(count * sizeof(Elf64_Shdr *));
	*matched_count = count;

	/* Now iterate through again and assign to the array */
	_elf64_find_section(ehdr, shdrs, match_flags, name, type, flags, *matched_shdrs, shstrtab);

	(void)fp;
	return ELF_OK;
}

/* Find a section based on index */
int elf64_find_section_by_idx(FILE *fp, Elf64_Ehdr *ehdr, uint8_t *shdrs, Elf64_Half idx, Elf64_Shdr **match)
{
	if(!ehdr || !shdrs || !match)
		return ELF_INVALID_PARAM;

	if(idx >= ehdr->e_shnum)
		return ELF_OUT_OF_RANGE;

	*match = (Elf64_Shdr *)&shdrs[idx * ehdr->e_shentsize];

	(void)fp;
	return ELF_OK;
}

int elf64_find_symbol(FILE *fp, Elf64_Ehdr *ehdr, Elf64_Sym *sym, const char *name)
{
	/* Iterate through the sections finding those marked as symtab */
	uint8_t *shdr_buf;
	int eret = elf64_read_shdrs(fp, ehdr, &shdr_buf);
	if(eret != ELF_OK)
		return eret;
	int found = 0;

	unsigned int i;
	for(i = 0; i < ehdr->e_shnum; i++)
	{
		Elf64_Shdr *shdr = (Elf64_Shdr *)&shdr_buf[i * ehdr->e_shentsize];

		if(shdr->sh_type == SHT_SYMTAB)
		{
			/* Load the symbol table */
			eret = elf64_load_section(fp, shdr);
			if(eret != ELF_OK)
				goto end_sect;

			/* Load its string table */
			if(shdr->sh_link == 0)
			{
				free((void *)shdr->sh_addr);
				shdr->sh_addr = 0;
				continue;
			}
			Elf64_Shdr *strtab = (Elf64_Shdr *)&shdr_buf[shdr->sh_link * ehdr->e_shentsize];
			eret = elf64_load_section(fp, strtab);
			if(eret != ELF_OK)
			{
				free((void *)shdr->sh_addr);
				goto end_sect;
			}
			
			/* Iterate through the symbols */
			unsigned int j;
			for(j = shdr->sh_info * shdr->sh_entsize; j < shdr->sh_size; j += shdr->sh_entsize)
			{
				Elf64_Sym *s = (Elf64_Sym *)(shdr->sh_addr + j);

				if(s->st_name != 0)
				{
					if(!strcmp((const char *)(strtab->sh_addr + s->st_name), name))
					{
						memcpy(sym, s, sizeof(Elf64_Sym));
						eret = ELF_OK;
						found = 1;
						goto end_sect;
					}
				}
			}

end_sect:
			if(shdr->sh_addr)
				free((void *)shdr->sh_addr);
			if(strtab->sh_addr)
				free((void *)strtab->sh_addr);
			if(found)
				break;
		}
	}

	free(shdr_buf);

	if(found)
		return ELF_OK;
	else
		return ELF_SYMBOL_NOT_FOUND;
}

int elf64_get_entry_point(FILE *fp, Elf64_Ehdr *ehdr, void **epoint)
{
	(void)fp;
	*epoint = (void **)ehdr->e_entry;
	return ELF_OK;
}