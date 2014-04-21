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

#include <efiwindow.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>

static EFI_STATUS rect_paint(WINDOW *w, RECT *update);

EFI_STATUS ew_create_rect(WINDOW **w, RECT *loc, WINDOW *parent, EW_RECT *info)
{
	if((w == NULL) || (loc == NULL) || (parent == NULL) || (info == NULL))
		return EFI_INVALID_PARAMETER;

	EW_RECT *desc = (EW_RECT *)malloc(sizeof(EW_RECT));
	if(desc == NULL)
		return EFI_OUT_OF_RESOURCES;
	memcpy(desc, info, sizeof(EW_RECT));

	return ew_create_window(w, loc, parent, rect_paint, desc, sizeof(EW_RECT));
}

EFI_STATUS ew_set_rect_info(WINDOW *w, EW_RECT *desc)
{
	if((w == NULL) || (desc == NULL))
		return EFI_INVALID_PARAMETER;

	memcpy(w->paint_data, desc, sizeof(EW_RECT));

	ew_invalidate_rect(w, NULL);

	return EFI_SUCCESS;
}

EFI_STATUS ew_get_rect_info(WINDOW *w, EW_RECT *desc)
{
	if((w == NULL) || (desc == NULL))
		return EFI_INVALID_PARAMETER;

	memcpy(desc, w->paint_data, sizeof(EW_RECT));

	return EFI_SUCCESS;
}

EFI_STATUS rect_paint(WINDOW *w, RECT *update)
{
	int i, j;

	//fprintf(stderr, "rect_paint\n");

	EW_RECT *r = (EW_RECT *)w->paint_data;

	for(j = update->y; j < (update->y + update->h); j++)
	{
		for(i = update->x; i < (update->x + update->w); i++)
		{
			EW_COLOR col;
			/* Is this within the border? */
			if((i >= r->linewidth) && (j >= r->linewidth) && (i < (w->loc.w - r->linewidth)) && (j < (w->loc.h - r->linewidth)))
				col = r->color;
			else
				col = r->linecolor;

			*EW_BB_LOC(w, i, j) = col;			
		}
	}

	return EFI_SUCCESS;
}
