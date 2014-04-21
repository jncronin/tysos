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

#include "mboot.h"
#include "stdint.h"
#include "kif.h"
#include "malloc.h"
#include "memmgr.h"
#include "vmem.h"
#include "console.h"
#include "stdlib.h"

struct Multiboot_Header *kif;
extern struct linked_mmap *first;
extern char tysos_heap_start, tysos_heap_end, bda_addr;

#ifdef EFI
#define TYSOS_HEAP_SIZE 0x100000
char tysos_heap[TYSOS_HEAP_SIZE];
#endif

void *make_kif()
{
	int mmap_items, i;
	struct __array *mmap_array_obj;
	struct linked_mmap *cur_linked_mmap;

	kif = (struct Multiboot_Header *)malloc(sizeof(struct Multiboot_Header));

	/* Build the managed array of MemoryMap objects */
	mmap_items = count_linked_list();
	Create_Ref_Array(&mmap_array_obj);
	mmap_array_obj->lobounds = (unsigned long long int)(unsigned long int)malloc(sizeof(unsigned long int));
	mmap_array_obj->sizes = (unsigned long long int)(unsigned long int)malloc(sizeof(unsigned long int));
	mmap_array_obj->inner_array = (unsigned long long int)(unsigned long int)malloc(mmap_items * sizeof(unsigned long long int));

	*((unsigned long int *)(unsigned long int)mmap_array_obj->lobounds) = 0;
	*((unsigned long int *)(unsigned long int)mmap_array_obj->sizes) = mmap_items;

	cur_linked_mmap = first;
	for(i = 0; i < mmap_items; i++) {
		((unsigned long long int *)(unsigned long int)mmap_array_obj->inner_array)[i] = (unsigned long long int)(unsigned long int)&cur_linked_mmap->mmap;
		cur_linked_mmap = cur_linked_mmap->next;
	}

	/* Link it into the kif */
	kif->mmap = (unsigned long long int)(unsigned long int)mmap_array_obj;

	/* The loader needs to provide a space which the kernel can use as its default heap, because it is already running
	in paged mode and has little knowledge of the layout of the virtual address space */
#ifdef EFI
	kif->heap_start = (unsigned long long int)(unsigned long int)tysos_heap;
	kif->heap_end = kif->heap_start + TYSOS_HEAP_SIZE;
#else
	kif->heap_start = (unsigned long long int)(unsigned long int)&tysos_heap_start;
	kif->heap_end = (unsigned long long int)(unsigned long int)&tysos_heap_end;
#endif

	/* We also need to provide the virtual address of the level 4 page table */
	kif->virt_master_paging_struct = VA_PML4T;

	/* Inform tysos where we've mapped the bios data area */
#ifdef EFI
	kif->virt_bda = 0;
#else
	kif->virt_bda = (unsigned long long int)(unsigned long int)&bda_addr;
#endif

#ifdef _DEBUG
	_puts("kif: ", 0);
	puthex((unsigned long int)kif);
	_puts("  mmap_array: ", 0);
	puthex((unsigned long int)mmap_array_obj);
	_puts("", 1);
	_puts("heap_start: ", 0);
	puthex((unsigned long int)kif->heap_start);
	_puts("  heap_end: ", 0);
	puthex((unsigned long int)kif->heap_end);
	_puts("", 1);
#endif

	return kif;
}

void write_kif_modules(struct Multiboot_Header *kif, struct mb_header *mboot)
{
	struct __array *modules_array;
	unsigned long int i;

	/* set up the array */
	Create_Ref_Array(&modules_array);
	modules_array->lobounds = (unsigned long long int)(unsigned long int)malloc(sizeof(unsigned long int));
	modules_array->sizes = (unsigned long long int)(unsigned long int)malloc(sizeof(unsigned long int));
	modules_array->inner_array = (unsigned long long int)(unsigned long int)malloc(mboot->mods_count * sizeof(unsigned long long int));

	*((unsigned long int *)(unsigned long int)modules_array->lobounds) = 0;
	*((unsigned long int *)(unsigned long int)modules_array->sizes) = mboot->mods_count;
	kif->modules = (unsigned long long int)(unsigned long int)modules_array;

	for(i = 0; i < mboot->mods_count; i++)
	{
		struct mb_mod *cur_mod = (struct mb_mod *)(mboot->mods_addr + i * 16);	/* mboot 0.6.96 defines module structures as 16 bytes */
		struct Multiboot_Module *cur_dest;

		cur_dest = (struct Multiboot_Module *)malloc(sizeof(struct Multiboot_Module));
		Init_Multiboot_Module(cur_dest);

		((unsigned long long int *)(unsigned long int)modules_array->inner_array)[i] = (unsigned long long int)(unsigned long int)cur_dest;

		cur_dest->base_addr = (unsigned long long int)cur_mod->mod_start;
		cur_dest->length = (unsigned long long int)(cur_mod->mod_end - cur_mod->mod_start);

		if(cur_mod->string != NULL)
		{
			struct System_String *dest_str;

			CreateString(&dest_str, cur_mod->string);
			cur_dest->name = (unsigned long long int)(unsigned long int)dest_str;
		}
	}
}
