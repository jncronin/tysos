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
#include <sys/param.h>

struct _ew_progress_int
{
	EW_PROGRESS info;
	WINDOW *outer, *bar;
	WINDOW *caption;
};

static void set_outer_bar_rect(WINDOW *progress, RECT *outer_rect)
{
	outer_rect->x = 0;
	outer_rect->y = 0;
	outer_rect->w = progress->loc.w;
	outer_rect->h = progress->loc.h;
}

static void set_caption_rect(WINDOW *progress, EW_PROGRESS *pi, RECT *capt_rect)
{
	if(pi->caption_font == NULL)
		return;

	/* Decide on the size of the caption

	Define a box 2 pixels within the line.  Scale the font by a integral multiple, choose scale 1 if too small

	*/

	int box_w = progress->loc.w - pi->linewidth * 2 - 4;
	int box_h = progress->loc.h - pi->linewidth * 2 - 4;

	int x_scale = box_w / pi->caption_font->natural_w;
	int y_scale = box_h / pi->caption_font->natural_h;
	if(x_scale == 0)
		x_scale = 1;
	if(y_scale == 0)
		y_scale = 1;
	int f_scale = MIN(x_scale, y_scale);
	if(pi->caption_font_max_w != 0)
	{
		int max_xscale = pi->caption_font_max_w / pi->caption_font->natural_w;
		f_scale = MIN(f_scale, max_xscale);
	}
	if(pi->caption_font_max_h != 0)
	{
		int max_yscale = pi->caption_font_max_h / pi->caption_font->natural_h;
		f_scale = MIN(f_scale, max_yscale);
	}
	
	capt_rect->w = 4 * f_scale * pi->caption_font->natural_w;	// 4 chars - enough for '100%'
	capt_rect->h = 1 * f_scale * pi->caption_font->natural_h;
	capt_rect->x = (progress->loc.w - capt_rect->w) / 2;
	capt_rect->y = (progress->loc.h - capt_rect->h) / 2;

	fprintf(stderr, "set_caption_rect: progress->w: %i, progress->h: %i, linewidth: %i, box_w: %i, box_h %i, "
		"x_scale: %i, y_scale: %i, f_scale: %i, natural_w: %i, natural_h: %i, capt_rect: (%i,%i,%i,%i)\n",
		progress->loc.w, progress->loc.h, pi->linewidth, box_w, box_h, x_scale, y_scale, f_scale,
		pi->caption_font->natural_w, pi->caption_font->natural_h, capt_rect->x, capt_rect->y,
		capt_rect->w, capt_rect->h);
}

static void set_inner_bar_rect(WINDOW *progress, RECT *inner_rect, int v, int dir)
{
	switch(dir)
	{
		case EW_PROGRESS_LEFTRIGHT:
			inner_rect->x = 0;
			inner_rect->y = 0;
			inner_rect->w = (progress->loc.w * v) / 100;
			inner_rect->h = progress->loc.h;
			break;
		case EW_PROGRESS_RIGHTLEFT:
			inner_rect->x = (progress->loc.w * (100 - v)) / 100;
			inner_rect->y = 0;
			inner_rect->w = progress->loc.w - inner_rect->x;
			inner_rect->h = progress->loc.h;
			break;
		case EW_PROGRESS_TOPBOTTOM:
			inner_rect->x = 0;
			inner_rect->y = 0;
			inner_rect->w = progress->loc.w;
			inner_rect->h = (progress->loc.h * v) / 100;
			break;
		case EW_PROGRESS_BOTTOMTOP:
			inner_rect->x = 0;
			inner_rect->y = (progress->loc.h * (100 - v)) / 100;
			inner_rect->w = progress->loc.w;
			inner_rect->h = progress->loc.h - inner_rect->y;
			break;
	}
}

EFI_STATUS ew_create_progress(WINDOW **w, RECT *loc, WINDOW *parent, EW_PROGRESS *info)
{
	if((w == NULL) || (loc == NULL) || (parent == NULL) || (info == NULL))
		return EFI_INVALID_PARAMETER;

	struct _ew_progress_int *desc = (struct _ew_progress_int *)malloc(sizeof(struct _ew_progress_int));
	if(desc == NULL)
		return EFI_OUT_OF_RESOURCES;
	memset(desc, 0, sizeof(struct _ew_progress_int));
	memcpy(desc, info, sizeof(EW_PROGRESS));

	EFI_STATUS s = ew_create_window(w, loc, parent, ew_paint_null, desc, sizeof(struct _ew_progress_int));
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_create_progress: ew_create_window() failed: %i\n", s);
		return s;
	}

	/* Create the bars */
	RECT outer_rect, inner_rect;
	set_outer_bar_rect(*w, &outer_rect);
	set_inner_bar_rect(*w, &inner_rect, info->progress, info->direction);
	EW_RECT outer_info = { info->backcolor, info->linecolor, info->linewidth };
	EW_RECT inner_info = { info->barcolor, info->linecolor, info->linewidth };

	s = ew_create_rect(&desc->outer, &outer_rect, *w, &outer_info);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_create_progress: ew_create_rect(outer) failed: %i\n", s);
		return s;
	}
	s = ew_create_rect(&desc->bar, &inner_rect, *w, &inner_info);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_create_progress: ew_create_rect(inner) failed: %i\n", s);
		return s;
	}

	ew_show(desc->outer);
	ew_show(desc->bar);

	/* Create caption */
	if(info->caption_font != NULL)
	{
		RECT capt_rect;
		set_caption_rect(*w, info, &capt_rect);
		EW_TEXTBOX caption_info = { info->caption_font, 4, 1, info->caption_color, EW_CLEAR, EW_TEXTBOX_ALIGN_TOP | EW_TEXTBOX_ALIGN_LEFT, 1 };
		s = ew_create_textbox(&desc->caption, &capt_rect, *w, &caption_info);
		if(EFI_ERROR(s))
		{
			fprintf(stderr, "efiwindow: ew_create_progress: ew_create_textbox failed: %i\n", s);
			return s;
		}

		ew_show(desc->caption);
	}

	return EFI_SUCCESS;
}

EFI_STATUS ew_set_progress_info(WINDOW *w, EW_PROGRESS *desc)
{
	if((w == NULL) || (desc == NULL))
		return EFI_INVALID_PARAMETER;

	memcpy(w->paint_data, desc, sizeof(EW_PROGRESS));

	struct _ew_progress_int *pi = (struct _ew_progress_int *)w->paint_data;
	RECT bar;
	set_inner_bar_rect(w, &bar, desc->progress, desc->direction);
	ew_resize_window(pi->bar, &bar);

	if(pi->caption)
	{
		char tbuf[5];
		sprintf(tbuf, "%3i%%", desc->progress);
		ew_set_textbox_text(pi->caption, tbuf, EW_TEXTBOX_WHOLE_STRING);
	}

	return EFI_SUCCESS;
}

EFI_STATUS ew_get_progress_info(WINDOW *w, EW_PROGRESS *desc)
{
	if((w == NULL) || (desc == NULL))
		return EFI_INVALID_PARAMETER;

	memcpy(desc, w->paint_data, sizeof(EW_PROGRESS));

	return EFI_SUCCESS;
}
