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
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static EFI_STATUS bitmap_paint(WINDOW *w, RECT *update);

struct _ew_bitmap
{
	int stretch;
	EW_IMAGE img;
};

EFI_STATUS ew_create_bitmap(WINDOW **w, RECT *loc, WINDOW *parent, EW_IMAGE *img, int stretch)
{
	if((w == NULL) || (loc == NULL) || (img == NULL) || (parent == NULL) || (img->buf == NULL))
	{
		fprintf(stderr, "efiwindow: ew_create_bitmap: invalid param: w: %#016llx, loc: %#016llx, parent: %#016llx, img: %#016llx\n",
			w, loc, parent, img);
		return EFI_INVALID_PARAMETER;
	}

	struct _ew_bitmap *data = (struct _ew_bitmap *)malloc(sizeof(struct _ew_bitmap));
	if(data == NULL)
		return EFI_OUT_OF_RESOURCES;

	memset(data, 0, sizeof(struct _ew_bitmap));
	data->stretch = stretch;
	memcpy(&data->img, img, sizeof(EW_IMAGE));

	if(img->pixel_element_size != 8)
	{
		fprintf(stderr, "efiwindow: ew_create_bitmap: only 8 bits per channel images supported\n");
		return EFI_INVALID_PARAMETER;
	}

	return ew_create_window(w, loc, parent, bitmap_paint, data, sizeof(struct _ew_bitmap));
}

EFI_STATUS bitmap_paint(WINDOW *w, RECT *update)
{
	int i, j;
	struct _ew_bitmap *eb = (struct _ew_bitmap *)w->paint_data;

	for(j = update->y; j < (update->y + update->h); j++)
	{
		for(i = update->x; i < (update->x + update->w); i++)
		{
			int src_x, src_y;

			/* Determine the source coordinates */
			switch(eb->stretch)
			{
				case EW_BITMAP_STRETCH_FILL:
					src_x = (eb->img.width * i) / w->loc.w;
					src_y = (eb->img.height * j) / w->loc.h;
					break;

				case EW_BITMAP_STRETCH_CENTER:
					src_x = i - (w->loc.w - eb->img.width) / 2;
					src_y = j - (w->loc.h - eb->img.height) / 2;
					break;

				case EW_BITMAP_STRETCH_TILE:
					src_x = i % eb->img.width;
					src_y = j % eb->img.height;
					break;

				case EW_BITMAP_STRETCH_NONE:
				default:
					src_x = i;
					src_y = j;
					break;
			};

			uint32_t *dest_pixel = (uint32_t *)&((uint8_t *)w->buf)[(i + j * w->loc.w) * sizeof(EFI_GRAPHICS_OUTPUT_BLT_PIXEL)];
			EW_COLOR blank = 0x0;
			uint32_t color;
			
			if((src_x >= 0) && (src_x < (int)eb->img.width) && (src_y >= 0) && (src_y < (int)eb->img.height))
				color = ew_get_image_pixel(&eb->img, src_x, src_y);
			else
				color = blank;

			*dest_pixel = color;
		}
	}

	return EFI_SUCCESS;
}
