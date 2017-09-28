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

#include <efi.h>
#include <efilib.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <pmem_alloc.h>

/* First and last virtual address to start allocating with allocate_any */
#define START_VMEM_ADDR		0x10000000ULL
#define END_VMEM_ADDR		0x800000000000ULL

struct vmem_map_entry
{
	struct vmem_map_entry *next;
	UINTPTR base;
	UINTPTR length;
	EFI_PHYSICAL_ADDRESS src;
};

struct vmem_map_entry *first = NULL;
struct vmem_map_entry *last = NULL;

static int intersects(UINTPTR base, UINTPTR length, struct vmem_map_entry *test)
{
	UINTPTR a_last = base + length - 1;
	UINTPTR test_last = test->base + test->length - 1;

	if(test_last < base)
		return 0;
	if(test->base > a_last)
		return 0;
	return 1;
}

static EFI_STATUS add(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src)
{
	struct vmem_map_entry *ent = (struct vmem_map_entry *)malloc(sizeof(struct vmem_map_entry));
	if(ent == NULL)
	{
		printf("add: malloc failed\n");
		return EFI_OUT_OF_RESOURCES;
	}

	ent->base = base;
	ent->length = length;
	ent->src = src;
	ent->next = NULL;

	if(first == NULL)
	{
		first = ent;
		last = ent;
	}
	else
	{
		last->next = ent;
		last = ent;
	}
	
	return EFI_SUCCESS;
}

static struct vmem_map_entry *get_intersect(UINTPTR base, UINTPTR length)
{
	struct vmem_map_entry *test = first;

	while(test != NULL)
	{
		if(intersects(base, length, test))
			return test;
		test = test->next;
	}

	return NULL;
}

EFI_PHYSICAL_ADDRESS get_pmem_for_vmem(UINTPTR vaddr)
{
	struct vmem_map_entry *isect = get_intersect(vaddr, 1);
	if(isect == NULL)
		return 0;
	return vaddr - isect->base + isect->src;
}

EFI_STATUS allocate_fixed(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src)
{
	if(get_intersect(base, length) != NULL)
		return EFI_OUT_OF_RESOURCES;

	return add(base, length, src);
}

EFI_STATUS allocate_any(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS src)
{
	UINTPTR base = START_VMEM_ADDR;

	while(base + length <= END_VMEM_ADDR)
	{
		struct vmem_map_entry *isect = get_intersect(base, length);
		if(isect == NULL)
		{
			EFI_STATUS s = add(base, length, src);
			if(s == EFI_SUCCESS && vaddr_out)
				*vaddr_out = base;
			return s;
		}

		base = isect->base + isect->length;
		if(base & 0xfff)
			base = (base + 0x1000) & ~0xfff;
	}
	return -1;
}

EFI_STATUS allocate(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS *paddr_out)
{
	UINTPTR align_length = length;
	if(align_length & 0xfff)
		align_length = (length + 0x1000) & ~0xfff;
	EFI_PHYSICAL_ADDRESS paddr;
	EFI_STATUS s = alloc_data(align_length, &paddr);
	if(s != EFI_SUCCESS)
	{
		printf("allocate: AllocatePages failed: %i\n", s);
		return s;
	}
	s = allocate_any(length, vaddr_out, paddr);
	if(s == EFI_SUCCESS && paddr_out)
		*paddr_out = paddr;
	return s;
}

static EFI_STATUS get_child_entry(EFI_PHYSICAL_ADDRESS parent, int idx, EFI_PHYSICAL_ADDRESS *child)
{
	uint64_t *pte = (uint64_t *)(uintptr_t)(parent + 8 * (EFI_PHYSICAL_ADDRESS)idx);
	if((*pte & 0x1) == 0)
	{
		/* Entry does not exist - create it */
		EFI_PHYSICAL_ADDRESS page;
		EFI_STATUS s = alloc_code(0x1000, &page);
		if(s != EFI_SUCCESS)
		{
			printf("get_child_entry: failed to allocate a free page: %i\n", s);
			return s;
		}
		memset((void *)(uintptr_t)page, 0, 0x1000);
		*pte = (uint64_t)page | 0x3ULL;
	}

	*child = (EFI_PHYSICAL_ADDRESS)(*pte & ~0xfffULL);

	//printf("get_child_entry: child %i of %x is %x\n", idx, parent, *child);

	return EFI_SUCCESS;
}

static EFI_STATUS build_pte_for_page(EFI_PHYSICAL_ADDRESS paddr, UINTPTR vaddr, EFI_PHYSICAL_ADDRESS pml4t)
{
	if(paddr & 0xfff)
	{
		printf("build_pte_for_page: error paddr %x is not page-aligned\n", paddr);
		return EFI_ABORTED;
	}
	if(vaddr & 0xfff)
	{
		printf("build_pte_for_page: error vaddr %x is not page-aligned\n", vaddr);
		return EFI_ABORTED;
	}

	UINTPTR pt_index = (vaddr >> 12) & 0x1ff;
	UINTPTR pd_index = (vaddr >> 21) & 0x1ff;
	UINTPTR pdpt_index = (vaddr >> 30) & 0x1ff;
	UINTPTR pmlt4_index = (vaddr >> 39) & 0x1ff;

	//printf("map p: %x to v: %x, pml4t_index: %i, pdpt_index: %i, pd_index: %i, pt_index: %i\n",
	//	paddr, vaddr, pmlt4_index, pdpt_index, pd_index, pt_index);

	EFI_PHYSICAL_ADDRESS pdpt, pd, pt;
	EFI_STATUS s;

	/* Iterate through the paging hierarchy to get the page table.
	   The get_child_entry function also creates the appropriate tables as it goes. */
	s = get_child_entry(pml4t, pmlt4_index, &pdpt);
	if(s != EFI_SUCCESS)
		return s;
	s = get_child_entry(pdpt, pdpt_index, &pd);
	if(s != EFI_SUCCESS)
		return s;
	s = get_child_entry(pd, pd_index, &pt);
	if(s != EFI_SUCCESS)
		return s;

	/* Now set the page table entry */
	uint64_t *pte = (uint64_t *)(uintptr_t)(pt + 8 * pt_index);
	*pte = (uint64_t)paddr | 0x3ULL;
	//printf("pte: address: %x, val: %x\n", pte, *pte);

	return EFI_SUCCESS;
}

static EFI_STATUS build_pte_for_region(struct vmem_map_entry *r, EFI_PHYSICAL_ADDRESS pml4t)
{
	UINTPTR cur_offset = 0x0;

	if(r->base & 0xfff || r->length & 0xfff || r->src & 0xfff)
	{
		printf("build_pte_for_region: unaligned base/length: base: %x, length: %x, src: %x\n",
			r->base, r->length, r->src);
		return EFI_ABORTED;
	}

	while(cur_offset < r->length)
	{
		EFI_STATUS s = build_pte_for_page(r->src + (EFI_PHYSICAL_ADDRESS)cur_offset, r->base + cur_offset, pml4t);
		if(s != EFI_SUCCESS)
		{
			printf("build_pte_for_page failed (paddr: %x, vaddr: %x): %i\n", r->src + (EFI_PHYSICAL_ADDRESS)cur_offset,
				r->base + cur_offset, s);
			return s;
		}
		cur_offset += 0x1000;
	}

	return EFI_SUCCESS;
}

EFI_STATUS build_page_tables(EFI_PHYSICAL_ADDRESS *pml4t_out)
{
	EFI_PHYSICAL_ADDRESS pml4t;
	EFI_STATUS s = alloc_code(0x1000, &pml4t);
	if(s != EFI_SUCCESS)
	{
		printf("build_page_tables: couldn't allocate pml4t (%i)\n", s);
		return s;
	}
	memset((void *)(uintptr_t)pml4t, 0, 0x1000);

	struct vmem_map_entry *cur_vmem = first;
	while(cur_vmem != NULL)
	{
		s = build_pte_for_region(cur_vmem, pml4t);
		if(s != EFI_SUCCESS)
		{
			printf("build_page_tables: build_pte_for_region(base: %x, length %x, src: %x) failed with %i\n",
				cur_vmem->base, cur_vmem->length, cur_vmem->src);
			return s;
		}
		printf("build_page_tables: region mapped (base: %p", cur_vmem->base);
		printf(" length: %p", cur_vmem->length);
		printf(" src: %p)\n", cur_vmem->src);

		cur_vmem = cur_vmem->next;
	}

	/* Point the last entry back to itself */
	uint64_t recursive_entry = (uint64_t)pml4t;
	recursive_entry |= 0x3;
	*(uint64_t *)(uintptr_t)(pml4t + 0xff8) = recursive_entry;

	if(pml4t_out)
		*pml4t_out = pml4t;
	return EFI_SUCCESS;
}