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

/* An extremely simple malloc */

#include "malloc.h"
#include "console.h"
#include "memmgr.h"
#include "kif.h"
#include "stdlib.h"

#define HEAP_SIZE		0x700000
#define HEAP_ALIGN		0x100000

unsigned long int heap_end = 0;
unsigned long int heap_start;

void *malloc(int n)
{
	static unsigned long int cur_end = 0;

	if(cur_end == 0) {
#ifdef _DEBUG_MALLOC
		puts("malloc: first call - setting up heap");
#endif
		heap_start = cur_end = (unsigned long int)malloc_align(HEAP_SIZE, HEAP_ALIGN, TLoad, 0);
#ifdef _DEBUG_MALLOC
		_puts("malloc: malloc_align returned ", 0);
		puthex(heap_start);
		puts("");
#endif
		heap_end = cur_end + HEAP_SIZE;

		if(cur_end == (unsigned long int)NULL) {
			puts("Error: unable to allocate heap");
			return NULL;
		}

#ifdef _DEBUG_MALLOC
		_puts("heap: ", 0);
		puthex(cur_end);
		_puts("-", 0);
		puthex(heap_end);
		_puts("", 1);
#endif

		add_linked_list(heap_start, HEAP_SIZE, TLoad);
	}

	if((unsigned long int)n > (heap_end - cur_end)) {
		puts("Error: out of heap space");
		return 0;
	}

	cur_end += n;
	return (void *)(cur_end - n);
}

void free(void __attribute__((unused)) *ptr)
{
}
