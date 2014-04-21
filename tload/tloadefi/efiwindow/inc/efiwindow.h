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

#ifndef EFIWINDOW_H
#define EFIWINDOW_H

#include <efi.h>
#include <efilib.h>
#include <stdint.h>
#include <stddef.h>

extern EFI_GRAPHICS_OUTPUT_PROTOCOL *GOP;

EFI_STATUS ew_init(EFI_HANDLE ImageHandle);
EFI_STATUS ew_set_mode(int width, int height, int bpp);
EFI_STATUS ew_set_text_mode();
EFI_STATUS ew_get_backbuffer(void **buf);
EFI_STATUS ew_get_backbuffer_size(int *bbw, int *bbh);
EFI_STATUS ew_set_can_blit(int can_blit);
EFI_STATUS ew_blit(void *buf, int src_x, int src_y, int dest_x, int dest_y, int width, int height, int delta);

typedef uint32_t EW_COLOR;

typedef struct _ew_rect
{
	int x, y, w, h;
} RECT;

typedef struct _ew_window WINDOW;
typedef EFI_STATUS (*ew_paint_func)(WINDOW *w, RECT *update);
typedef EFI_STATUS (*ew_resize_func)(WINDOW *w);
EFI_STATUS ew_paint_color(WINDOW *w, RECT *update);
EFI_STATUS ew_paint_null(WINDOW *w, RECT *update);

typedef struct _ew_window
{
	RECT loc;
	int show;
	void *paint_data;
	WINDOW *next, *prev, *parent, *first_child, *last_child;
	ew_paint_func paint;
	ew_resize_func resize;
	void *buf;
	size_t data_size;
} WINDOW;

extern WINDOW *EW_DESKTOP;

EFI_STATUS ew_create_window(WINDOW **w, RECT *loc, WINDOW *parent, ew_paint_func paint, void *data, size_t datasize);
EFI_STATUS ew_resize_window(WINDOW *w, RECT *size);
EFI_STATUS ew_invalidate_rect(WINDOW *w, RECT *r);
EFI_STATUS ew_repaint_rect(WINDOW *w, RECT *r);
EFI_STATUS ew_show(WINDOW *w);
EFI_STATUS ew_hide(WINDOW *h);

EFI_STATUS ew_get_data(WINDOW *w, void *buf, size_t *bufsize);

#define EW_BLEND_ZERO			0
#define EW_BLEND_ONE			1
#define EW_BLEND_SRCCOLOR		2
#define EW_BLEND_DESTCOLOR		3
#define EW_BLEND_INVSRCCOLOR	4
#define EW_BLEND_INVDESTCOLOR	5
#define EW_BLEND_SRCALPHA		6
#define EW_BLEND_DESTALPHA		7
#define EW_BLEND_INVSRCALPHA	8
#define EW_BLEND_INVDESTALPHA	9

EFI_STATUS ew_set_blend_mode(int enable, int src_blend, int dest_blend);
uint32_t ew_blend(uint32_t src, uint32_t dest);

#define EW_BB_PIXEL_SIZE (sizeof(EFI_GRAPHICS_OUTPUT_BLT_PIXEL))
#define EW_BB_LOC(w, i, j) ((EW_COLOR *)&((uint8_t *)w->buf)[(i + j * w->loc.w) * EW_BB_PIXEL_SIZE])

/* These are pseudo-windows to define the mouse and keyboard for event purposes */
#define EW_MOUSE		((WINDOW *)1)
#define EW_KEYBOARD		((WINDOW *)2)

#ifdef IN_EW_LIB
struct update_bb_data
{
	RECT absolute_rect;
	void *bb;
	int bbw, bbh;
};

EFI_STATUS ew_create_window_int(WINDOW **w, RECT *loc, WINDOW *parent, ew_paint_func paint, void *data, size_t datasize, int add);
uint32_t ew_blend(uint32_t src, uint32_t dest);

#ifndef PROFILE
#define PROFILE_START(x)
#define PROFILE_END(x)
#else
#ifdef __GNUC__
#define RDTSC(x) __asm volatile ( "rdtsc" : "=A" (x) )
#define PROFILE_START(x)	uint64_t __profile_begin ## x; RDTSC(__profile_begin ## x);
#define PROFILE_END(x) uint64_t __profile_end ## x; RDTSC(__profile_end ## x); fprintf(stderr, "profile: %s took %ull ticks\n", #x, __profile_end ## x - __profile_begin ## x);
#else
#define PROFILE_START(x)
#define PROFILE_END(x)
#endif
#endif
#endif

#endif

#include <ewcolor.h>
#include <ewimage.h>
#include <ewbitmap.h>
#include <ewfont.h>
#include <ewrect.h>
#include <ewtextbox.h>
#include <ewprogress.h>
#include <ewmessageloop.h>
#include <ewmouse.h>
