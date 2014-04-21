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
#include <windowlist.h>
#include <assert.h>
#include <string.h>
#include <sys/param.h>

EFI_GRAPHICS_OUTPUT_PROTOCOL *GOP;
extern EFI_GUID GraphicsOutputProtocol;

WINDOW *EW_DESKTOP = NULL;

static int ew_can_blit = 0;

SIMPLE_INPUT_INTERFACE **ew_STIP_i = NULL;
int ew_STIP_n = 0;
EFI_SIMPLE_POINTER_PROTOCOL **ew_SPP_i = NULL;
int ew_SPP_n = 0;
EFI_ABSOLUTE_POINTER_PROTOCOL **ew_APP_i = NULL;
int ew_APP_n = 0;

static EFI_STATUS do_blit(void *buf, RECT *abs_rect, int w, int h, int bbw);

EFI_STATUS ew_init(EFI_HANDLE ImageHandle)
{
	/* Ensure the EFI_SYSTEM_TABLE *ST and EFI_BOOT_SERVICES *BS pointers are set */
	if(ST == NULL)
	{
		fprintf(stderr, "efiwindow: ew_init(): error: please set EFI_SYSTEM_TABLE *ST prior to calling efilibc_init()\n");
		return EFI_NOT_READY;
	}
	if(BS == NULL)
	{
		fprintf(stderr, "efiwindow: ew_init(): error: please set EFI_BOOT_SERVICES *BS prior to calling efilibc_init()\n");
		return EFI_NOT_READY;
	}

	/* Check arguments */
	if(ImageHandle == NULL)
	{
		fprintf(stderr, "efiwindow: ew_init(): error: ImageHandle cannot be NULL\n");
		return EFI_INVALID_PARAMETER;
	}

	/* Get the GOP device from the console out device */
	EFI_STATUS s = BS->OpenProtocol(ST->ConsoleOutHandle, &GraphicsOutputProtocol, (void **)&GOP, ImageHandle, NULL,
		EFI_OPEN_PROTOCOL_BY_HANDLE_PROTOCOL);
	if(s == EFI_UNSUPPORTED)
	{
		/* ConsoleOut does not support GOP - try to find all GOP devices and pick the first one */
		UINTN gop_handles_n;
		EFI_HANDLE *gop_handles;
		s = BS->LocateHandleBuffer(ByProtocol, &GraphicsOutputProtocol, NULL, &gop_handles_n, &gop_handles);
		if(s == EFI_NOT_FOUND)
		{
			fprintf(stderr, "efiwindow: ew_init(): error: no suitable Graphics Output Protocol device found\n");
			return s;
		}
		else if(EFI_ERROR(s))
		{
			fprintf(stderr, "efiwindow: ew_init(): error: default console out device does not support Graphics "
				"Output Protocol and LocateHandleBuffer failed: %i\n", s);
			return s;
		}

		for(UINTN gop_handle_i = 0; gop_handle_i < gop_handles_n; gop_handle_i++)
		{
			s = BS->OpenProtocol(gop_handles[gop_handle_i], &GraphicsOutputProtocol, (void **)&GOP, ImageHandle, NULL,
				EFI_OPEN_PROTOCOL_BY_HANDLE_PROTOCOL);
			if(s == EFI_SUCCESS)
				break;
		}
		if(EFI_ERROR(s))
		{
			fprintf(stderr, "efiwindow: ew_init(): error: unable to open Graphics Output Protocol on any of %i "
				"suitable devices: %i", gop_handles_n, s);
			return s;
		}
		BS->FreePool(gop_handles);
	}
	else if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: Could not get Graphics Output Protocol: %i\n", s);
		return s;
	}

	/* Get a list of input devices */

	/* First, determine the total number of devices of the relevant types:
		SimpleTextInputProtocol
		SimplePointerProtocol
		AbsolutePointerProtocol
	*/
	UINTN stip_n = 0, spp_n = 0, app_n = 0;
	s = BS->LocateHandle(ByProtocol, &TextInProtocol, NULL, &stip_n, NULL);
	if((s != EFI_SUCCESS) && (s != EFI_BUFFER_TOO_SMALL))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: LocateHandle(SimpleTextInputProtocol) failed: %i\n", s);
		return s;
	}
	s = BS->LocateHandle(ByProtocol, &SimplePointerProtocol, NULL, &spp_n, NULL);
	if((s != EFI_SUCCESS) && (s != EFI_BUFFER_TOO_SMALL))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: LocateHandle(SimplePointerProtocol) failed: %i\n", s);
		return s;
	}
	s = BS->LocateHandle(ByProtocol, &AbsolutePointerProtocol, NULL, &app_n, NULL);
	if((s != EFI_SUCCESS) && (s != EFI_BUFFER_TOO_SMALL))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: LocateHandle(AbsolutePointerProtocol) failed: %i\n", s);
		return s;
	}

	fprintf(stderr, "efiwindow: ew_init(): detected %i TextInputProtocol devices, %i SimplePointerProtocol devices and "
		"%i AbsolutePointerProtocol devices\n", stip_n / sizeof(EFI_HANDLE), spp_n / sizeof(EFI_HANDLE), app_n / sizeof(EFI_HANDLE));

	/* Now get a list of handles */
	EFI_HANDLE *stip_h, *spp_h, *app_h;
	stip_h = (EFI_HANDLE *)malloc(stip_n);
	spp_h = (EFI_HANDLE *)malloc(spp_n);
	app_h = (EFI_HANDLE *)malloc(app_n);
	if(stip_h == NULL || spp_h == NULL || app_h == NULL)
		return EFI_OUT_OF_RESOURCES;
	s = BS->LocateHandle(ByProtocol, &TextInProtocol, NULL, &stip_n, stip_h);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: LocateHandle(SimpleTextInputProtocol) failed: %i\n", s);
		return s;
	}
	s = BS->LocateHandle(ByProtocol, &SimplePointerProtocol, NULL, &spp_n, spp_h);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: LocateHandle(SimplePointerProtocol) failed: %i\n", s);
		return s;
	}
	s = BS->LocateHandle(ByProtocol, &AbsolutePointerProtocol, NULL, &app_n, app_h);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_init(): error: LocateHandle(AbsolutePointerProtocol) failed: %i\n", s);
		return s;
	}

	/* Now get the actual interface handles */
	ew_STIP_i = (SIMPLE_INPUT_INTERFACE **)malloc(stip_n / 8 * sizeof(SIMPLE_INPUT_INTERFACE *));
	ew_SPP_i = (EFI_SIMPLE_POINTER_PROTOCOL **)malloc(spp_n / 8 * sizeof(EFI_SIMPLE_POINTER_PROTOCOL *));
	ew_APP_i = (EFI_ABSOLUTE_POINTER_PROTOCOL **)malloc(app_n / 8 * sizeof(EFI_ABSOLUTE_POINTER_PROTOCOL *));
	if(ew_STIP_i == NULL || ew_SPP_i == NULL || ew_APP_i == NULL)
		return EFI_OUT_OF_RESOURCES;

	/* First for text input protocol */
	UINTN h_idx;
	for(h_idx = 0; h_idx < (stip_n / 8); h_idx++)
	{
		EFI_HANDLE h = stip_h[h_idx];
		s = BS->HandleProtocol(h, &TextInProtocol, (void **)&ew_STIP_i[ew_STIP_n++]);
		if(EFI_ERROR(s))
		{
			fprintf(stderr, "efiwindow: ew_init(): error: HandleProtocol(%#016llx, TextInProtocol) failed: %i\n", h, s);
			return s;
		}
		ew_STIP_i[ew_STIP_n - 1]->Reset(ew_STIP_i[ew_STIP_n - 1], 0);
	}

	/* Simple pointer protocol */
	for(h_idx = 0; h_idx < (spp_n / 8); h_idx++)
	{
		EFI_HANDLE h = spp_h[h_idx];

		/* Here we need to additionally ensure that the device does also not implement
		    AbsolutePointerProtocol as we'd prefer to use that interface if possible */
		UINTN app_idx;
		int duplicate = 0;
		for(app_idx = 0; app_idx < (app_n / 8); app_idx++)
		{
			if(h == app_h[app_idx])
			{
				duplicate = 1;
				break;
			}
		}

		if(duplicate)
			continue;

		s = BS->HandleProtocol(h, &SimplePointerProtocol, (void **)&ew_SPP_i[ew_SPP_n++]);
		if(EFI_ERROR(s))
		{
			fprintf(stderr, "efiwindow: ew_init(): error: HandleProtocol(%#016llx, SimplePointerProtocol) failed: %i\n", h, s);
			return s;
		}
		ew_SPP_i[ew_SPP_n - 1]->Reset(ew_SPP_i[ew_SPP_n - 1], 0);
	}

	/* Absolute Pointer Protocol */
	for(h_idx = 0; h_idx < (app_n / 8); h_idx++)
	{
		EFI_HANDLE h = app_h[h_idx];
		s = BS->HandleProtocol(h, &AbsolutePointerProtocol, (void **)&ew_APP_i[ew_APP_n++]);
		if(EFI_ERROR(s))
		{
			fprintf(stderr, "efiwindow: ew_init(): error: HandleProtocol(%#016llx, AbsolutePointerProtocol) failed: %i\n", h, s);
			return s;
		}
		ew_APP_i[ew_APP_n - 1]->Reset(ew_APP_i[ew_APP_n - 1], 0);
	}

	/* Report devices found */
	fprintf(stderr, "efiwindow: ew_init: handling %i text input devices, %i simple pointer devices and %i "
		"absolute pointer devices\n", ew_STIP_n, ew_SPP_n, ew_APP_n);
	
	/* Load cursors */
	ew_mouse_init();

	return EFI_SUCCESS;
}

EFI_STATUS ew_create_window(WINDOW **w, RECT *loc, WINDOW *parent, ew_paint_func paint_func, void *data, size_t data_size)
{
	return ew_create_window_int(w, loc, parent, paint_func, data, data_size, 1);
}

EFI_STATUS ew_create_window_int(WINDOW **w, RECT *loc, WINDOW *parent, ew_paint_func paint_func, void *data, size_t data_size, int add)
{
	fprintf(stderr, "ew_create_window(%#016llx, %#016llx, %#016llx, %#016llx, %#016llx)\n",
		w, loc, parent, paint_func, data);
	if((EW_DESKTOP == NULL) && (parent != NULL))
	{
		fprintf(stderr, "efiwindow: ew_create_window(): please call ew_set_mode() first\n");
		return EFI_NOT_READY;
	}
	if((EW_DESKTOP != NULL) && (parent == NULL))
	{
		fprintf(stderr, "efiwindow: ew_create_window(): 'parent' argument cannot be NULL\n");
		return EFI_INVALID_PARAMETER;
	}

	WINDOW *ret = (WINDOW *)malloc(sizeof(WINDOW));
	assert(ret);
	memset(ret, 0, sizeof(WINDOW));

	ret->show = 0;
	ret->paint_data = data;
	ret->data_size = data_size;

	if(paint_func == NULL)
		ret->paint = ew_paint_color;
	else
		ret->paint = paint_func;

	fprintf(stderr, "ew_create_window: parent: %#016llx\n", parent);
	if((parent != NULL) && add)
		ew_add_list(parent, ret);
	else
		ret->parent = parent;

	*w = ret;

	ew_resize_window(ret, loc);

	return EFI_SUCCESS;
}

EFI_STATUS ew_resize_window(WINDOW *w, RECT *loc)
{
	if((w == NULL) || (loc == NULL))
	{
		fprintf(stderr, "ew_resize_window: invalid arguments\n");
		return EFI_INVALID_PARAMETER;
	}

	w->loc.x = loc->x;
	w->loc.y = loc->y;
	w->loc.h = loc->h;
	w->loc.w = loc->w;

	size_t buf_size = w->loc.w * w->loc.h * EW_BB_PIXEL_SIZE;
	//fprintf(stderr, "ew_resize_window: w: %#016llx, loc: (%i,%i,%i,%i), buf_size: %i\n", w, w->loc.x, w->loc.y, w->loc.w, w->loc.h, buf_size);

	w->buf = realloc(w->buf, buf_size);
	assert(w->buf || (buf_size == 0));

	if(w->resize)
		w->resize(w);

	ew_repaint_rect(w, NULL);

	return EFI_SUCCESS;
}

EFI_STATUS ew_show(WINDOW *w)
{
	RECT r;
	r.x = 0;
	r.y = 0;
	r.w = w->loc.w;
	r.h = w->loc.h;
	w->show = 1;
	ew_invalidate_rect(w, &r);
	return EFI_SUCCESS;
}

EFI_STATUS ew_hide(WINDOW *w)
{
	RECT r;
	r.x = w->loc.x;
	r.y = w->loc.y;
	r.w = w->loc.w;
	r.h = w->loc.h;
	w->show = 0;
	ew_invalidate_rect(w->parent, &r);
	return EFI_SUCCESS;
}

/* Get the intersection of two rectangles */
static void ew_intersect_rect(RECT *a, RECT *b, RECT *out)
{
	int a_right = a->x + a->w;
	int b_right = b->x + b->w;
	int a_bottom = a->y + a->h;
	int b_bottom = b->y + b->h;

	out->x = -1;
	out->y = -1;
	out->w = -1;
	out->h = -1;

	int out_right = -1;
	int out_bottom = -1;

	if((((a->x < b_right) && (a_right > b->x)) || ((b->x < a_right) && (b_right > a->x))))
	{
		out->x = MAX(a->x, b->x);
		out_right = MIN(a_right, b_right);
	}

	if((((a->y < b_bottom) && (a_bottom > b->y)) || ((b->y < a_bottom) && (b_bottom > a->y))))
	{
		out->y = MAX(a->y, b->y);
		out_bottom = MIN(a_bottom, b_bottom);
	}

	if((out_right != -1) && (out->x != -1))
		out->w = out_right - out->x;
	else
		out->x = out->y = out->h = out->w = -1;

	if((out_bottom != -1) && (out->y != -1))
		out->h = out_bottom - out->y;
	else
		out->x = out->y = out->h = out->w = -1;
}

/* Convert a window-relative rectangle to a screen-relative one */
static void ew_get_absolute_rect(WINDOW *w, RECT *r, RECT *out)
{
	out->w = r->w;
	out->h = r->h;
	out->x = r->x;
	out->y = r->y;

	//fprintf(stderr, "ew_get_absolute_rect: w: %#016llx, w->parent: %#016llx, EW_DESKTOP: %#016llx\n", w, w->parent, EW_DESKTOP);

	WINDOW *cur_window = w;
	while(cur_window != EW_DESKTOP)
	{
		//fprintf(stderr, "ew_get_absolute_rect: cur_window: %#016llx, cur_window->parent: %#016llx\n", cur_window, cur_window->parent);
		out->x += cur_window->loc.x;
		out->y += cur_window->loc.y;
		cur_window = cur_window->parent;
	}
}

/* Determine if the window is to be shown (only if window and all parents have show set to 1) */
static int ew_get_absolute_show(WINDOW *w)
{
	WINDOW *cur_window = w;
	while(cur_window != EW_DESKTOP)
	{
		if(cur_window->show == 0)
			return 0;
		cur_window = cur_window->parent;
	}

	return cur_window->show;
}

static int ew_update_bb(WINDOW *w, void *data)
{
	if(w->show == 0)
		return 0;

	struct update_bb_data *ubbd = (struct update_bb_data *)data;
	RECT w_abs_rect;
	RECT w_rect;
	w_rect.x = 0;
	w_rect.y = 0;
	w_rect.w = w->loc.w;
	w_rect.h = w->loc.h;
	ew_get_absolute_rect(w, &w_rect, &w_abs_rect);
	RECT intersect_rect;
	ew_intersect_rect(&ubbd->absolute_rect, &w_abs_rect, &intersect_rect);

	int i, j;

	for(j = intersect_rect.y; j < (intersect_rect.y + intersect_rect.h); j++)
	{
		int w_x = intersect_rect.x - w_abs_rect.x;
		int w_y = j - w_abs_rect.y;

		EW_COLOR *src_line = (EW_COLOR *)&((uint8_t *)w->buf)[(w_x + w_y * w->loc.w) * EW_BB_PIXEL_SIZE];
		EW_COLOR *dest_line = (EW_COLOR *)&((uint8_t *)ubbd->bb)[(intersect_rect.x + j * ubbd->bbw) * EW_BB_PIXEL_SIZE];

		/*
		for(i = intersect_rect.x; i < (intersect_rect.x + intersect_rect.w); i++)
		{

			if((i >= 0) && (i < ubbd->bbw) && (j >= 0) && (j < ubbd->bbh))
			{
				uint32_t *src_color = (uint32_t *)&((uint8_t *)w->buf)[(w_x + w_y * w->loc.w) * sizeof(EFI_GRAPHICS_OUTPUT_BLT_PIXEL)];
				uint32_t *dest_color = (uint32_t *)&((uint8_t *)ubbd->bb)[(i + j * ubbd->bbw) * sizeof(EFI_GRAPHICS_OUTPUT_BLT_PIXEL)];

				*dest_color = ew_blend(*src_color, *dest_color);
			}
		}*/

		for(i = intersect_rect.x; i < (intersect_rect.x + intersect_rect.w); i++)
		{
			*dest_line = ew_blend(*src_line, *dest_line);
			dest_line++;
			src_line++;
		}
	}

	return 1;
}

EFI_STATUS ew_repaint_rect(WINDOW *w, RECT *r)
{
	if(w == NULL)
		return EFI_INVALID_PARAMETER;

	/* If r is NULL, repaint the entire window */
	RECT full_window;
	if(r == NULL)
	{
		full_window.x = 0; full_window.y = 0; full_window.w = w->loc.w; full_window.h = w->loc.h;
		r = &full_window;
	}

	if(w->paint != NULL)
		w->paint(w, r);

	/* The desktop only needs to be refreshed if the image is actually visible, else we're just
	 updating its backbuffer */
	if(ew_get_absolute_show(w))
		return ew_invalidate_rect(w, r);
	else
		return EFI_SUCCESS;
}

EFI_STATUS ew_invalidate_rect(WINDOW *w, RECT *r)
{
	PROFILE_START(ew_invalidate_rect);
	if(w == NULL)
		return EFI_INVALID_PARAMETER;

	/* If r is NULL, invalidate the whole window */
	RECT full_window;
	if(r == NULL)
	{
		full_window.x = 0; full_window.y = 0; full_window.w = w->loc.w; full_window.h = w->loc.h;
		r = &full_window;
	}

	/* Determine the absolute coordinates of the particular rectangle */
	RECT abs_rect;
	PROFILE_START(ew_get_absolute_rect);
	ew_get_absolute_rect(w, r, &abs_rect);
	PROFILE_END(ew_get_absolute_rect);

	/* Update the backbuffer */
	void *buf;
	int bbw, bbh;
	PROFILE_START(ew_get_backbuffer);
	ew_get_backbuffer(&buf);
	PROFILE_END(ew_get_backbuffer);
	PROFILE_START(ew_get_backbuffer_size);
	ew_get_backbuffer_size(&bbw, &bbh);
	PROFILE_END(ew_get_backbuffer_size);

	struct update_bb_data ubbd;
	ubbd.absolute_rect = abs_rect;
	ubbd.bb = buf;
	ubbd.bbw = bbw;
	ubbd.bbh = bbh;

	/* First clear the backbuffer */
	PROFILE_START(clear_backbuffer);
	int i, j;
	for(j = abs_rect.y; j < (abs_rect.y + abs_rect.h); j++)
	{
		for(i = abs_rect.x; i < (abs_rect.x + abs_rect.w); i++)
		{
			if((i >= 0) && (i < bbw) && (j >= 0) && (j < bbh))
			{
				uint32_t *bb_pixel = (uint32_t *)&((uint8_t *)buf)[(i + j * bbw) * sizeof(EFI_GRAPHICS_OUTPUT_BLT_PIXEL)];
				*bb_pixel = 0;
			}
		}
	}
	PROFILE_END(clear_backbuffer);

	/* Now traverse the window tree, writing the appropriate bit of each window's backbuffer to the main backbuffer */
	PROFILE_START(ew_leftmost_traversal);
	ew_leftmost_traversal(EW_DESKTOP, ew_update_bb, &ubbd);
	PROFILE_END(ew_leftmost_traversal);

	/* Display the mouse cursor */
	PROFILE_START(ew_display_mouse_cursor);
	ew_display_mouse_cursor(&ubbd);
	PROFILE_END(ew_display_mouse_cursor);

	/* Blit the relevant bit of the actual backbuffer to the screen */
	PROFILE_START(do_blit);
	EFI_STATUS s = do_blit(buf, &abs_rect, r->w, r->h, bbw);
	PROFILE_END(do_blit);
	PROFILE_END(ew_invalidate_rect);
	return s;
}

EFI_STATUS do_blit(void *buf, RECT *abs_rect, int w, int h, int bbw)
{
	if(ew_can_blit)
	{
		return GOP->Blt(GOP, (EFI_GRAPHICS_OUTPUT_BLT_PIXEL *)buf, EfiBltBufferToVideo, abs_rect->x, abs_rect->y,
			abs_rect->x, abs_rect->y, w, h, bbw * 4);
	}
	else
	{
		return ew_blit(buf, abs_rect->x, abs_rect->y, abs_rect->x, abs_rect->y, w, h, bbw * 4);
	}
}

EFI_STATUS ew_set_can_blit(int can_blit)
{
	if((can_blit != 0) && (can_blit != 1))
		return EFI_INVALID_PARAMETER;

	ew_can_blit = can_blit;
	return EFI_SUCCESS;
}

EFI_STATUS ew_paint_color(WINDOW *w, RECT *update)
{
	UINT32 pixel = (UINT32)(uintptr_t)w->paint_data;
	UINT32* bbuf = (UINT32 *)w->buf;
	
	int i, j;
	for(j = update->y; j < (update->y + update->h); j++)
	{
		for(i = update->x; i < (update->x + update->w); i++)
			bbuf[j * w->loc.w + i] = pixel;
	}

	return EFI_SUCCESS;
}

EFI_STATUS ew_paint_null(WINDOW *w, RECT *update)
{
	(void)w; (void)update;

	//fprintf(stderr, "ew_paint_null\n");

	return EFI_SUCCESS;
}

EFI_STATUS ew_get_data(WINDOW *w, void *buf, size_t *bufsize)
{
	if(w == NULL)
		return EFI_INVALID_PARAMETER;
	if(buf == NULL)
	{
		*bufsize = w->data_size;
		return EFI_BUFFER_TOO_SMALL;
	}
	memcpy(buf, w->paint_data, *bufsize);
	if(*bufsize < w->data_size)
	{
		*bufsize = w->data_size;
		return EFI_BUFFER_TOO_SMALL;
	}
	return EFI_SUCCESS;
}
