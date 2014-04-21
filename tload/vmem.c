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

#include "vmem.h"
#include "memmgr.h"
#include "elf.h"
#include "console.h"
#include "stdlib.h"
#include "mboot.h"
#include "kif.h"

#define GET_PML4T_ENTRY_NO(x)	(int)(((x) >> 39) & 0x1ff)
#define GET_PDPT_ENTRY_NO(x)	(int)(((x) >> 30) & 0x1ff)
#define GET_PD_ENTRY_NO(x)		(int)(((x) >> 21) & 0x1ff)
#define GET_PT_ENTRY_NO(x)		(int)(((x) >> 12) & 0x1ff)
#define GET_PAGE_OFFSET(x)		(int)((x) & 0xfff)

#define EXTRACT_PSTRUCT_PADDR(x)	(void *)((unsigned long int)((x) & 0xFFFFFFFFFF000LL))

#define MAKE_PSTRUCT_ENTRY(x)	((((unsigned long long int)(unsigned long int)(x)) & 0xFFFFFFFFFF000LL) | P_PRESENT)

#define ALL_FLAGS				(PF_X | PF_W | PF_R)

void *get_paging_struct(unsigned long long int *parent_table, int entry_no, unsigned long int p_flags, int type);
void *_get_paging_struct(unsigned long long int *parent_table, int entry_no, unsigned long int p_flags, int overwrite,
						 void *page, int type);

void *create_virtual_mapping(unsigned long long int virt_addr, unsigned long long int *pml4t, unsigned long int p_flags,
							 int type)
{
	return create_virtual_mapping_from_page(virt_addr, pml4t, p_flags, 0, NULL, type);
}

void *create_virtual_mapping_from_page(unsigned long long int virt_addr, unsigned long long int *pml4t, unsigned long int p_flags,
							  int page_exists, void *page, int type)
{
	/* Work down through the page structure hierarchy to determine if the page that contains virt_addr is defined,
	creating paging structures, and ultimately the page itself if necessary */

	int pml4t_entry_no, pdpt_entry_no, pd_entry_no, pt_entry_no, page_offset;
	unsigned long long int *pdpt, *pd, *pt;

	pml4t_entry_no = GET_PML4T_ENTRY_NO(virt_addr);
	pdpt_entry_no = GET_PDPT_ENTRY_NO(virt_addr);
	pd_entry_no = GET_PD_ENTRY_NO(virt_addr);
	pt_entry_no = GET_PT_ENTRY_NO(virt_addr);
	page_offset = GET_PAGE_OFFSET(virt_addr);

	pdpt = get_paging_struct(pml4t, pml4t_entry_no, ALL_FLAGS, PagingStructure);
	pd = get_paging_struct(pdpt, pdpt_entry_no, ALL_FLAGS, PagingStructure);
	pt = get_paging_struct(pd, pd_entry_no, ALL_FLAGS, PagingStructure);
	if(page_exists)
		page = _get_paging_struct(pt, pt_entry_no, p_flags, 1, page, type);
	else
		page = get_paging_struct(pt, pt_entry_no, p_flags, type);

	return (void *)((unsigned long int)page + page_offset);
}

void *get_paging_struct(unsigned long long int *parent_table, int entry_no, unsigned long int p_flags, int type)
{
	return _get_paging_struct(parent_table, entry_no, p_flags, 0, NULL, type);
}

void *_get_paging_struct(unsigned long long int *parent_table, int entry_no, unsigned long p_flags, int overwrite,
						 void *page, int type)
{
	/* return the physical address of the paging structure pointed to by entry 'entry_no' of 'parent_table'
	If the value is not present, then create a page to store it in and fill in the parent_table entry using the flags
	provided.
	If the value is already present, increase its flags with those set in p_flags */

	void *pstruct;

	if(parent_table == NULL) {
		puts("Error: passed a NULL as the parent table");
		return NULL;
	}

	if(overwrite) {
		pstruct = page;
		parent_table[entry_no] = MAKE_PSTRUCT_ENTRY(pstruct);
		add_linked_list((unsigned long long int)(unsigned long int)pstruct, PAGESIZE, type);
	} else {
		if(parent_table[entry_no] & P_PRESENT)
			pstruct = EXTRACT_PSTRUCT_PADDR(parent_table[entry_no]);
		else {
			pstruct = malloc_align(PAGESIZE, PAGESIZE, type, 1);
			if(pstruct == NULL) {
				puts("Error: unable to allocate a new page");
				return NULL;
			}
			memset(pstruct, 0, PAGESIZE);
			parent_table[entry_no] = MAKE_PSTRUCT_ENTRY(pstruct);

#ifdef _DEBUG4
			if((unsigned long int)parent_table != 0x104000) {
				_puts("n:", 0);
				puthex((unsigned long int)pstruct);
				_puts(" p:", 0);
				puthex((unsigned long int)parent_table);
				_puts(" e:", 0);
				puthex((unsigned long int)entry_no);
				_puts(" ", 1);
			}
#endif
		}
	}
	

	if(p_flags & PF_W)
		parent_table[entry_no] |= P_WRITE;

	return pstruct;
}
