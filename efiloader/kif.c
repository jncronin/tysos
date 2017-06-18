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
#include "tloadkif.h"

static EFI_PHYSICAL_ADDRESS kif_end = 0;
static EFI_PHYSICAL_ADDRESS kif_cur = 0;

struct Multiboot_Header *mboot;

EFI_PHYSICAL_ADDRESS align(EFI_PHYSICAL_ADDRESS v, EFI_PHYSICAL_ADDRESS alignment);
extern uint64_t mb_adjust;

void *kmalloc(size_t n)
{
	if(kif_cur + (EFI_PHYSICAL_ADDRESS)n > kif_end)
	{
		printf("error: kmalloc: out of space (cannot allocate %i bytes)\n", n);
		while (1);
		//return NULL;
	}
	void *ret = (void *)kif_cur;
	kif_cur += (EFI_PHYSICAL_ADDRESS)n;
	kif_cur = align(kif_cur, 8);

	return ret;
}

EFI_STATUS kif_init(EFI_PHYSICAL_ADDRESS p_kif, EFI_PHYSICAL_ADDRESS len, struct Multiboot_Header **mbheader)
{
	kif_cur = p_kif;
	kif_end = p_kif + len;

	mboot = (struct Multiboot_Header *)kmalloc(sizeof(struct Multiboot_Header));
	if(mboot == NULL)
		return EFI_OUT_OF_RESOURCES;
	Init_Multiboot_Header(mboot);
	mboot->__vtbl += mb_adjust;

	if(mbheader)
		*mbheader = mboot;

	return EFI_SUCCESS;
}

