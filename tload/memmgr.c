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

/* a simple memory manager */

#include "memmgr.h"
#include "console.h"
#include "vmem.h"
#include "stdlib.h"
#include "malloc.h"
#include "mboot.h"
#include "kif.h"

unsigned long int bitmap[0x8000];		/* enough for 1 bit per 4kb page == 32 pages */

struct linked_mmap *first = NULL;
struct linked_mmap *last = NULL;

/* each dword contains 32 bits, so can reference 128kB of memory (=0x20000 bytes) */

#define PAGE_INDEX(x)		((x) >> 12)

#define DWORD_INDEX(x)		(PAGE_INDEX(x) >> 5)
#define BIT_INDEX(x)		(PAGE_INDEX(x) & 0x1f)

void _set(unsigned long int addr);
void _unset(unsigned long int addr);
int _isset(unsigned long int addr);
void *_malloc_fixed(unsigned long int addr, unsigned long int length, int type, int add_list);

#ifdef EFI
void efi_pause();

void dump_bitmap()
{
	char digits[] = "0123456789ABCDEF";

	int digits_per_line = 20;
	int lines_per_break = 20;

	int cur_digit = 0, cur_line = 0, cur_byte;

	for(cur_byte = 0; cur_byte < (0x8000 * 4); cur_byte++)
	{
		unsigned long int uli = bitmap[cur_byte / 4];
		uli >> ((cur_byte % 4) * 8);
		unsigned long int unib = (uli >> 4) & 0xf;
		unsigned long int lnib = uli & 0xf;
		putchar((int)digits[unib]);
		putchar((int)digits[lnib]);
		putchar(' ');

		cur_digit++;
		if(cur_digit == digits_per_line)
		{
			puts("");
			cur_digit = 0;
			cur_line++;

			if(cur_line == lines_per_break)
			{
				efi_pause();
				cur_line = 0;
			}
		}
	}
}
#endif

int is_free(unsigned long int addr, unsigned long int length)
{
	unsigned long int isfree = 1;
	unsigned long int block_offset;

	for(block_offset = 0; block_offset < length; block_offset += PAGESIZE) {
		if(_isset(addr + block_offset)) {
			isfree = 0;
			break;
		}
	}

	return isfree;
}

void *malloc_fixed(unsigned long int addr, unsigned long int length, int type, int add_list)
{
	if(!is_free(addr, length))
		return NULL;

	return _malloc_fixed(addr, length, type, add_list);
}

void *_malloc_fixed(unsigned long int addr, unsigned long int length, int type, int add_list)
{
	unsigned long int cur_set_page;

	for(cur_set_page = addr; cur_set_page < (addr + length); cur_set_page += PAGESIZE)
		_set(cur_set_page);
	if(add_list)
		add_linked_list((unsigned long long int)addr, (unsigned long long int)length, type);
	return (void *)addr;
}

void *malloc_align(unsigned long int length, unsigned int align, int type, int add_list)
{
	unsigned long int test = 0;
	unsigned long int found = 0;

	if((length % PAGESIZE) || (align % PAGESIZE)) {
		puts("Error: can only process multiple of page sized and aligned requests");
		return NULL;
	}

	while(1) {
		if(is_free(test, length)) {
			found = 1;
			break;
		}

		if(test >= (unsigned long int)(-align))
			break;

		test += align;
	}

	if(found)
		return _malloc_fixed(test, length, type, add_list);

	return NULL;
}

void freepage(void *p)
{
	_unset((unsigned long int)p);
}

unsigned long long int mark_used(unsigned long long int start, unsigned long long int length)
{
	unsigned long long int cur;

	for(cur = start; cur < (start + length); cur += PAGESIZE)
		_set((unsigned long int)cur);
	return start;
}

unsigned long long int mark_free(unsigned long long int start, unsigned long long int length)
{
	unsigned long long int cur;

	for(cur = start; cur < (start + length); cur += PAGESIZE)
		_unset((unsigned long int)cur);
	return start;
}

void _set(unsigned long addr)
{
	unsigned long int setval;

	setval = 1 << BIT_INDEX(addr);
	bitmap[DWORD_INDEX(addr)] |= setval;
}

void _unset(unsigned long addr)
{
	unsigned long int setval;

	setval = 1 << BIT_INDEX(addr);
	bitmap[DWORD_INDEX(addr)] &= (~setval);
}

int _isset(unsigned long addr) {
	return (bitmap[DWORD_INDEX(addr)] >> BIT_INDEX(addr)) & 0x1;
}

void add_linked_list(unsigned long long int addr, unsigned long long int length, int type)
{
	struct linked_mmap *new_mmap;

#ifdef _DEBUG_MEMMGR
	if(type == 1)
		_puts("Adding free: ", 0);
	else
		_puts("Adding used: ", 0);

	puthex(addr);
	_puts("-", 0);
	puthex(addr + length);
	_puts("", 1);
#endif
	
	/* First see if we can coalesce this request with an existing entry */
	if(last != NULL)
	{
		struct linked_mmap *cur_mmap = last;

		/* Iterate from last to first as it is most likely that this region will coalesce with
		   the region added last */
		do
		{
			if(cur_mmap->mmap.type == type)
			{
				if(cur_mmap->mmap.base_addr == (addr + length))
				{
					cur_mmap->mmap.base_addr = addr;
					cur_mmap->mmap.length += length;
					return;
				}
				else if((cur_mmap->mmap.base_addr + cur_mmap->mmap.length) == addr)
				{
					cur_mmap->mmap.length += length;
					return;
				}
			}

			cur_mmap = cur_mmap->prev;
		} while(cur_mmap != first);
	}

	new_mmap = (struct linked_mmap *)malloc(sizeof(struct linked_mmap));

	new_mmap->mmap.base_addr = addr;
	new_mmap->mmap.length = length;
	new_mmap->mmap.type = type;

	new_mmap->next = NULL;
	if(first == NULL)
		first = new_mmap;

	if(last == NULL)
		last = new_mmap;
	else {
		last->next = new_mmap;
		new_mmap->prev = last;
		last = new_mmap;
	}
}

int count_linked_list()
{
	int i = 0;

	struct linked_mmap *cur = first;

	while(cur != NULL) {
		cur = cur->next;
		i++;
	}

	return i;
}

void memmgr_init_linked_list()
{
	struct linked_mmap *cur_mmap = first;

	while(cur_mmap != NULL)
	{
		Init_Multiboot_MemoryMap(&cur_mmap->mmap);
		cur_mmap = cur_mmap->next;
	}
}
