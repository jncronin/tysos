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

#include <windowlist.h>
#include <stdio.h>

void ew_add_list(WINDOW *parent, WINDOW *w)
{
	if(parent->last_child == NULL)
	{
		parent->first_child = parent->last_child = w;
		w->next = w->prev = NULL;
	}
	else
	{
		w->prev = parent->last_child;
		w->next = NULL;
		parent->last_child->next = w;
		parent->last_child = w;
	}
	w->parent = parent;
}

void ew_leftmost_traversal(WINDOW *cur_w, traversal_func func, void *data)
{
	if(func(cur_w, data) == 0)
		return;

	WINDOW *cur_child = cur_w->first_child;
	while(cur_child != NULL)
	{
		ew_leftmost_traversal(cur_child, func, data);
		cur_child = cur_child->next;
	}
}
