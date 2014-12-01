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
		return NULL;
	}
	void *ret = (void *)kif_cur;
	kif_cur += (EFI_PHYSICAL_ADDRESS)n;
	kif_cur = align(kif_cur, 8);

	return ret;
}

void kCreateString(struct System_String **obj, const char *s)
{
    int l = strlen(s);
    int i;
    uint16_t *p;
    *obj = (struct System_String *)kmalloc(sizeof(struct System_String) + l * sizeof(uint16_t));
    Init_System_String(*obj);
    (*obj)->length = l;
    p = &((*obj)->start_char);
    for(i = 0; i < l; i++)
        p[i] = (uint16_t)s[i];
	(*obj)->__vtbl += mb_adjust;
}

void kCreateRefArray(struct __array **arr_obj, int len)
{
    *arr_obj = (struct __array *)kmalloc(sizeof(struct __array));
    (*arr_obj)->__object_id = 0;		// TODO
    (*arr_obj)->rank = 1;
    (*arr_obj)->elem_size = sizeof(uint64_t);
	(*arr_obj)->lobounds = (INTPTR)kmalloc(sizeof(int32_t));
	(*arr_obj)->sizes = (INTPTR)kmalloc(sizeof(int32_t));
	(*arr_obj)->inner_array = (INTPTR)kmalloc(len * sizeof(INTPTR));
	(*arr_obj)->inner_array_length = len;

	*(int32_t *)(INTPTR)(*arr_obj)->lobounds = 0;
	*(int32_t *)(INTPTR)(*arr_obj)->sizes = len;
	memset((void *)(*arr_obj)->inner_array, 0, len * sizeof(INTPTR));

	(*arr_obj)->__vtbl += mb_adjust;
	(*arr_obj)->lobounds += mb_adjust;
	(*arr_obj)->sizes += mb_adjust;
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

