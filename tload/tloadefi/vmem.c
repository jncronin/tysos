/* Copyright (C) 2014 by John Cronin
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

/* Manage the virtual memory of the system.

We already start up in 64-bit long mode, so use the current paging setup
and build from there.

*/

#include <stdint.h>

static uint64_t cr4;
uint64_t get_cr4()
{
	uint64_t ret;
	asm volatile ( "mov %%cr4, %0" : "=r"(ret) );
	return ret;
}

uint64_t get_cr2()
{
	uint64_t ret;
	asm volatile ( "mov %%cr2, %0" : "=r"(ret) );
	return ret;
}

uint64_t get_rbp()
{
	uint64_t ret;
	asm volatile ( "mov %%rbp, %0" : "=r"(ret) );
	return ret;
}

/* Set up a recursive mapping for the page table structures */
void setup_recursive_map()
{


}

void create_virtual_mapping_from_page()
{ }

void create_virtual_mapping()
{ }
