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

#include "console.h"

#ifdef EFI
void *uncompress_bzipped_kernel(void *compressed_data, unsigned int len)
{
	puts("EFI: compressed kernel not supported in EFI mode");
	return (void *)0;
}

#else
#include "malloc.h"
#include "memmgr.h"
#define BZ_NO_STDIO
#include "bzip2-1.0.5/bzlib.h"
#include "tloadkif.h"

#define DEST_LEN	0xA00000

void bz_internal_error(int errcode)
{
	_puts("libbz2 internal error: ", 0);
	puthex(errcode);
	_puts("", 1);
}

void *uncompress_bzipped_kernel(void *compressed_data, unsigned int len)
{
	/* uncompress a kernel compressed with bzip */

	int ret;
	unsigned int dest_len = DEST_LEN;
	void *dest = malloc_align(dest_len, 0x100000, Tysos, 1);

	puts("Decompressing kernel...");

#ifdef _DEBUG
	_puts("comp_data: ", 0);
	puthex((unsigned long int)compressed_data);
	_puts("  length: ", 0);
	puthex(len);
	_puts("", 1);
	_puts("dest:      ", 0);
	puthex((unsigned long int)dest);
	_puts("  length: ", 0);
	puthex(dest_len);
	_puts("", 1);
#endif
	ret = BZ2_bzBuffToBuffDecompress((char *)dest, &dest_len, (char *)compressed_data, len, 0, 0);

	if(ret == BZ_OK)
		return dest;
	else {
		_puts("Error: decompressing error ", 0);
		puthex(ret);
		_puts("", 1);
		return compressed_data;
	}
}
#endif
