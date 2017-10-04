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
#include <tloadkif.h>
#include <stdio.h>
#include <stdint.h>

/* A list of assigned physical memory locations */
#define MAX_PMEM_LOCS		512
uintptr_t pmem_base[MAX_PMEM_LOCS];
uintptr_t pmem_len[MAX_PMEM_LOCS];

int cur_pmem_ptr = 0;

#ifdef TYGRUB
#include <grub/relocator.h>
extern struct grub_relocator *grub_tygrub_relocator;

#ifdef DEBUG
struct grc
{
	struct grc *next;
	grub_phys_addr_t src;
	void *srcv;
	grub_phys_addr_t target;
	grub_size_t size;
	void *subchunks;
	unsigned nsubchunks;
};
#endif

EFI_STATUS alloc_code(UINTPTR len, EFI_PHYSICAL_ADDRESS *paddr)
{
	grub_relocator_chunk_t out;
	grub_err_t r = grub_relocator_alloc_chunk_align(grub_tygrub_relocator,
		&out, 0x100000, (0xffffffff - len) + 1,
		len, 0x1000, GRUB_RELOCATOR_PREFERENCE_NONE, 1);

	if (r == GRUB_ERR_NONE)
	{
		*paddr = (EFI_PHYSICAL_ADDRESS)(uintptr_t)get_virtual_current_address(out);
#ifdef DEBUG
		printf("vmem: returning aligned chunk %p\n", *paddr);
		struct grc *gp = (struct grc *)out;
		printf("src: %p, srcv: %p, target: %p, size: %p, nsubchunks: %d\n",
			gp->src, gp->srcv, gp->target, gp->size, gp->nsubchunks);
#endif
		pmem_base[cur_pmem_ptr] = (uintptr_t)*paddr;
		pmem_len[cur_pmem_ptr++] = (uintptr_t)len;
		return EFI_SUCCESS;
	}
	else
	{
		printf("vmem: error %d allocating chunk\n", r);
		*paddr = 0;
		return EFI_ABORTED;
	}
}
EFI_STATUS alloc_data(UINTPTR len, EFI_PHYSICAL_ADDRESS *paddr)
{
	return alloc_code(len, paddr);
}
#else
#include <efilib.h>

EFI_STATUS alloc_code(UINTPTR len, EFI_PHYSICAL_ADDRESS *paddr)
{
	return BS->AllocatePages(AllocateAnyPages, EfiLoaderCode, len / 4096, paddr);
}
EFI_STATUS alloc_data(UINTPTR len, EFI_PHYSICAL_ADDRESS *paddr)
{
	return BS->AllocatePages(AllocateAnyPages, EfiLoaderData, len / 4096, paddr);
}
#endif
