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
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include <ctype.h>
#include <sys/param.h>

struct console_data
{
	EW_TEXTBOX info;
	int c_w, c_h;
	int cur_x, cur_y;
	char *text_buf;
	WINDOW *w;
	int dirty_x, dirty_y;
	int x_offset, y_offset, ta_w, ta_h;
};

EFI_STATUS textbox_paint(WINDOW *w, RECT *update);

static void resize_tb(struct console_data *cd, RECT *loc, int cols, int rows)
{
	cd->info.rows = rows;
	cd->info.cols = cols;
	//cd->c_w = loc->w / cd->info.cols;
	//cd->c_h = loc->h / cd->info.rows;

	double xscale = (double)loc->w / (double)cd->info.cols / (double)cd->info.f->natural_w;
	double yscale = (double)loc->h / (double)cd->info.rows / (double)cd->info.f->natural_h;
	double f_scale = MIN(xscale, yscale);

	if(cd->info.integral_multiples)
	{
		cd->c_w = cd->info.f->natural_w * (int)f_scale;
		cd->c_h = cd->info.f->natural_h * (int)f_scale;
	}
	else
	{
		cd->c_w = (int)((double)cd->info.f->natural_w * f_scale);
		cd->c_h = (int)((double)cd->info.f->natural_h * f_scale);
	}

	cd->x_offset = (loc->w - cd->c_w * cols) / 2;
	cd->y_offset = (loc->h - cd->c_h * rows) / 2;
	cd->ta_w = cd->c_w * cols;
	cd->ta_h = cd->c_h * rows;

	if(cd->cur_x >= cols)
		cd->cur_x = cols - 1;
	if(cd->cur_y >= rows)
		cd->cur_y = rows - 1;

	cd->text_buf = (char *)realloc(cd->text_buf, rows * cols * sizeof(char));
}

EFI_STATUS ew_create_textbox(WINDOW **w, RECT *loc, WINDOW *parent, EW_TEXTBOX *info)
{
	if((w == NULL) || (loc == NULL) || (parent == NULL) || (info == NULL))
		return EFI_INVALID_PARAMETER;

	EFI_STATUS s;

	struct console_data *cd = (struct console_data *)malloc(sizeof(struct console_data));
	if(cd == NULL)
		return EFI_OUT_OF_RESOURCES;
	memset(cd, 0, sizeof(struct console_data));
	memcpy(cd, info, sizeof(EW_TEXTBOX));

	cd->text_buf = (char *)malloc(info->cols * info->rows * sizeof(char));
	assert(cd->text_buf);
	memset(cd->text_buf, ' ', info->cols * info->rows * sizeof(char));

	resize_tb(cd, loc, info->cols, info->rows);

	fprintf(stderr, "creating textbox window\n");

	s = ew_create_window(&cd->w, loc, parent, textbox_paint, cd, sizeof(struct console_data));
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_create_textbox: ew_create_window() failed: %i\n", s);
		return -1;
	}
	fprintf(stderr, "textbox window created (%#016llx), parent = %#016llx\n", cd->w, cd->w->parent);
	*w = cd->w;

	return EFI_SUCCESS;
}

EFI_STATUS textbox_paint(WINDOW *w, RECT *update)
{
	struct console_data *cd = (struct console_data *)w->paint_data;

	int i, j;

	for(j = update->y; j < (update->y + update->h); j++)
	{
		for(i = update->x; i < (update->x + update->w); i++)
		{
			/* Are we in the border? */
			if((i >= cd->x_offset) && (i < (cd->x_offset + cd->ta_w)) && (j >= cd->y_offset) && (j < (cd->y_offset + cd->ta_h)))
			{
				/* Not in border, in the text instead */
				int ta_x = i - cd->x_offset;
				int ta_y = j - cd->y_offset;

				/* Are we at a glyph origin? */
				if((ta_x % cd->c_w == 0) && (ta_y % cd->c_h == 0))
				{
					/* We're at a glyph origin */
					int char_x = ta_x / cd->c_w;
					int char_y = ta_y / cd->c_h;
					cd->info.f->draw_glyph(cd->info.f, w, (CHAR16)cd->text_buf[char_x + char_y * cd->info.cols], i, j,
						cd->c_w, cd->c_h, cd->info.forecolor, cd->info.backcolor);
				}
				/* If not at a glyph origin then do nothing */
			}
			else
			{
				/* In border - paint background */
				EW_COLOR *dest = (EW_COLOR *)&((uint8_t *)w->buf)[(i + j * w->loc.w) * EW_BB_PIXEL_SIZE];
				*dest = cd->info.backcolor;
			}
		}
	}

	return EFI_SUCCESS;
}

static void invalidate_loc(int x, int y, struct console_data *cd, RECT *fwrite_invalidate)
{
	int p_x = x * cd->c_w + cd->x_offset;
	int p_y = y * cd->c_h + cd->y_offset;
	
	if(fwrite_invalidate->x == -1)
		fwrite_invalidate->x = p_x;
	if(fwrite_invalidate->y == -1)
		fwrite_invalidate->y = p_y;

	if(p_x < fwrite_invalidate->x)
		fwrite_invalidate->x = p_x;
	if(p_y < fwrite_invalidate->y)
		fwrite_invalidate->y = p_y;

	if((p_x + cd->c_w) > (fwrite_invalidate->x + fwrite_invalidate->w))
		fwrite_invalidate->w = p_x + cd->c_w - fwrite_invalidate->x;
	if((p_y + cd->c_h) > (fwrite_invalidate->y + fwrite_invalidate->h))
		fwrite_invalidate->h = p_y + cd->c_h - fwrite_invalidate->y;
}

static void scr_up(struct console_data *cd, RECT *fwrite_invalidate)
{
	int j;

	for(j = 0; j < cd->info.rows; j++)
	{
		if(j == (cd->info.rows - 1))
			memset(&cd->text_buf[j * cd->info.cols], ' ', cd->info.cols);
		else
			memcpy(&cd->text_buf[j * cd->info.cols], &cd->text_buf[(j + 1) * cd->info.cols], cd->info.cols);
	}

	fwrite_invalidate->x = 0;
	fwrite_invalidate->y = 0;
	fwrite_invalidate->w = cd->info.cols * cd->c_w;
	fwrite_invalidate->h = cd->info.rows * cd->c_h;
}

static void newline(struct console_data *cd, RECT *fwrite_invalidate)
{
	cd->cur_x = 0;
	cd->cur_y++;
	if(cd->cur_y == cd->info.rows)
	{
		scr_up(cd, fwrite_invalidate);
		cd->cur_y = cd->info.rows - 1;
	}
}

size_t ew_textbox_fwrite(const void *ptr, size_t size, size_t nmemb, void *data)
{
	int bytes = size * nmemb;
	int i = 0;

	/* fprintf(stderr, "ew_textbox_fwrite: ptr: %#016llx, size: %i, nmemb: %i, data: %#016llx\n",
		ptr, size, nmemb, data); */
	
	struct console_data *cd = (struct console_data *)data;

	RECT fwrite_invalidate;
	fwrite_invalidate.x = -1;
	fwrite_invalidate.y = -1;
	fwrite_invalidate.w = 0;
	fwrite_invalidate.h = 0;

	for(i = 0; i < bytes; i++)
	{
		char c = (char)*(uint8_t *)((uintptr_t)ptr + i);
		if(isprint(c))
		{
			if(cd->cur_x == cd->info.cols)
				newline(cd, &fwrite_invalidate);

			cd->text_buf[cd->cur_x + cd->cur_y * cd->info.cols] = c;
			invalidate_loc(cd->cur_x, cd->cur_y, cd, &fwrite_invalidate);

			cd->cur_x++;

			/* Update the dirty rectangle if necessary */
			if(cd->cur_x > cd->dirty_x)
				cd->dirty_x = cd->cur_x;
		}
		else if(c == '\n')
			newline(cd, &fwrite_invalidate);

		/* Update dirty rectangle y */
		if(cd->cur_y > cd->dirty_y)
			cd->dirty_y = cd->cur_y;
	}

	if((fwrite_invalidate.x != -1) && (fwrite_invalidate.y != -1))
		ew_invalidate_rect(cd->w, &fwrite_invalidate);
	
	return nmemb;
}

EFI_STATUS ew_set_textbox_text(WINDOW *w, const char *buf, size_t nchars)
{
	if((w == NULL) || (buf == NULL))
		return EFI_INVALID_PARAMETER;

	/* Clear the dirty rectangle */
	struct console_data *cd = (struct console_data *)w->paint_data;
	size_t dirty_length = cd->dirty_y * cd->info.cols + cd->dirty_x;
	memset(cd->text_buf, ' ', dirty_length);
	RECT dirty_rect = { 0, 0, cd->dirty_x * cd->c_w, cd->dirty_y * cd->c_h };
	ew_invalidate_rect(w, &dirty_rect);

	/* Locate the text according to alignment rules - assumes single line text */
	if(nchars == EW_TEXTBOX_WHOLE_STRING)
		nchars = strlen(buf);
	switch(cd->info.align & EW_TEXTBOX_ALIGN_HORIZ_MASK)
	{
		case EW_TEXTBOX_ALIGN_LEFT:
			cd->cur_x = 0;
			break;
		case EW_TEXTBOX_ALIGN_CENTER:
			cd->cur_x = (cd->info.cols - nchars) / 2;
			break;
		case EW_TEXTBOX_ALIGN_RIGHT:
			cd->cur_x = cd->info.cols - nchars;
			break;
	}
	switch(cd->info.align & EW_TEXTBOX_ALIGN_VERT_MASK)
	{
		case EW_TEXTBOX_ALIGN_TOP:
			cd->cur_y = 0;
			break;
		case EW_TEXTBOX_ALIGN_VCENTER:
			cd->cur_y = (cd->info.cols - 1) / 2;
			break;
		case EW_TEXTBOX_ALIGN_BOTTOM:
			cd->cur_y = cd->info.cols - 1;
			break;
	}

	/* Write out the text */
	ew_textbox_fwrite(buf, 1, nchars, cd);

	return EFI_SUCCESS;
}

EFI_STATUS ew_get_textbox_text(WINDOW *w, char *buf, size_t nchars)
{
	if((w == NULL) || (buf == NULL))
		return EFI_INVALID_PARAMETER;
	if(nchars == EW_TEXTBOX_WHOLE_STRING)
		return EFI_INVALID_PARAMETER;

	struct console_data *cd = (struct console_data *)w->paint_data;
	size_t buf_len = strnlen(cd->text_buf, cd->info.cols * cd->info.rows);
	size_t to_copy = nchars;
	if(buf_len < to_copy)
		to_copy = buf_len;

	memcpy(buf, cd->text_buf, to_copy);

	/* Null terminate if there is room */
	if(to_copy < nchars)
		((char *)buf)[to_copy] = 0;

	return EFI_SUCCESS;
}

EFI_STATUS ew_get_textbox_length(WINDOW *w, size_t *nchars)
{
	if((w == NULL) || (nchars == NULL))
		return EFI_INVALID_PARAMETER;

	struct console_data *cd = (struct console_data *)w->paint_data;
	*nchars = strnlen(cd->text_buf, cd->info.cols * cd->info.rows);

	return EFI_SUCCESS;
}
