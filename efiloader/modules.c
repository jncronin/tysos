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
#include <errno.h>
#include <stdint.h>
#include "tloadkif.h"

EFI_STATUS allocate_fixed(UINTPTR base, UINTPTR length, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate_any(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS src);
EFI_STATUS allocate(UINTPTR length, UINTPTR *vaddr_out, EFI_PHYSICAL_ADDRESS *paddr_out);
EFI_STATUS load_file(FILE *f, long flen, EFI_PHYSICAL_ADDRESS paddr);

EFI_STATUS load_module(const char *fname, UINTPTR *vaddr, UINTPTR *length, EFI_PHYSICAL_ADDRESS *paddr_out)
{
	FILE *fmod = fopen(fname, "r");
	if(fmod == NULL)
	{
		printf("load_module: cannot load %s (%d)\n", fname, errno);
		*vaddr = 0;
		*length = 0;
		*paddr_out = 0;
		return EFI_NOT_FOUND;
	}

	fseek(fmod, 0, SEEK_END);
	long flen = ftell(fmod);
	fseek(fmod, 0, SEEK_SET);

	long flen_align = flen;
	if(flen_align & 0xfff)
		flen_align = (flen_align + 0x1000) & ~0xfffULL;

	EFI_PHYSICAL_ADDRESS paddr;
	EFI_STATUS s = allocate(flen_align, vaddr, &paddr);
	if(s != EFI_SUCCESS)
	{
		printf("load_module: cannot allocate %d bytes for %s (%d)\n", (size_t)flen_align, fname, (size_t)s);
		return s;
	}

	printf("load_module: load %s (length %d) to paddr %p, vaddr %p\n", fname, (size_t)flen, (intptr_t)paddr, (intptr_t)(vaddr ? *vaddr : 0x0));
	load_file(fmod, flen, paddr);
	printf(" done.\n");

	fclose(fmod);

	if(length)
		*length = (UINTPTR)flen;
	if(paddr_out)
		*paddr_out = paddr;

	return EFI_SUCCESS;
}
