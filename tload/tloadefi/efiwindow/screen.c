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

static void *bb = NULL;
static int w, h, bpp;
static int bbpages;
static int fb_stride;
static void *fb = NULL;
static EFI_GRAPHICS_PIXEL_FORMAT fb_format;
static int blend_enable = 0;
static int srcblend = EW_BLEND_ONE;
static int destblend = EW_BLEND_ZERO;

#define make_bgr_pixel(src) \
	(((UINT32)src->Blue) | ((UINT32)src->Green << 8) | ((UINT32)src->Red << 16))
#define make_rgb_pixel(src) \
	(((UINT32)src->Red) | ((UINT32)src->Green << 8) | ((UINT32)src->Blue << 16))
#define extract_blue(src) ((src) & 0xff)
#define extract_green(src) ((src) >> 8 & 0xff)
#define extract_red(src) ((src) >> 16 & 0xff)
#define extract_alpha(src) ((src) >> 24 & 0xff)

static void free_bb()
{
	if(bb != NULL)
		BS->FreePages((EFI_PHYSICAL_ADDRESS)(uintptr_t)bb, bbpages);
}

EFI_STATUS ew_set_mode(int width, int height, int bits_per_pixel)
{
	if(GOP == NULL)
	{
		fprintf(stderr, "efiwindow: ew_set_mode(): error: please call ew_init() first\n");
		return EFI_NOT_READY;
	}

	if(bits_per_pixel != 32)
		return EFI_INVALID_PARAMETER;

	/* Iterate through the supported modes looking for one that fits */
	UINT32 i;
	EFI_STATUS s;
	for(i = 0; i < GOP->Mode->MaxMode; i++)
	{
		EFI_GRAPHICS_OUTPUT_MODE_INFORMATION *mi;
		UINTN isize;
		s = GOP->QueryMode(GOP, i, &isize, &mi);
		if(EFI_ERROR(s))
			continue;
		if(isize < sizeof(EFI_GRAPHICS_OUTPUT_MODE_INFORMATION))
			continue;
		fprintf(stderr, "Mode %03i: %ix%i PF=%i\n", i, mi->HorizontalResolution, mi->VerticalResolution,
			mi->PixelFormat);
		if((mi->HorizontalResolution == (UINT32)width) && (mi->VerticalResolution == (UINT32)height) &&
			((mi->PixelFormat == PixelRedGreenBlueReserved8BitPerColor) ||
			(mi->PixelFormat == PixelBlueGreenRedReserved8BitPerColor)))
			break;
	}

	if(i == GOP->Mode->MaxMode)
		return EFI_UNSUPPORTED;

	/* Allocate a back buffer */
	free_bb();
	w = width; h = height; bpp = bits_per_pixel;
	bbpages = w * h * bpp / 8;
	if(bbpages & 0xfff)
	{
		bbpages += 0x1000;
		bbpages &= ~0xfff;
	}
	bbpages /= 0x1000;
	s = BS->AllocatePages(AllocateAnyPages, EfiLoaderData, bbpages, (EFI_PHYSICAL_ADDRESS *)&bb);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_set_mode(): failed to allocate back buffer pages (requested %i pages)\n",
			bbpages);
		return s;
	}

	s = GOP->SetMode(GOP, i);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_set_mode(): failed to set mode: %i\n", s);
		return s;
	}

	/* Create the desktop window */
	RECT desktop_rect;
	desktop_rect.x = 0;
	desktop_rect.y = 0;
	desktop_rect.w = w;
	desktop_rect.h = h;
	//s = ew_create_window(&EW_DESKTOP, &desktop_rect, NULL, ew_paint_color, (void *)0xff00ff00);
	s = ew_create_window(&EW_DESKTOP, &desktop_rect, NULL, ew_paint_null, NULL, 0);
	if(EFI_ERROR(s))
	{
		fprintf(stderr, "efiwindow: ew_set_mode(): ew_create_window() failed: %i\n", s);
		return s;
	}

	/* Store some info for our our blit function */
	fb_stride = GOP->Mode->Info->PixelsPerScanLine;
	fb_format = GOP->Mode->Info->PixelFormat;
	fb = (void *)GOP->Mode->FrameBufferBase;

	ew_show(EW_DESKTOP);
	fprintf(stderr, "efiwindow: ew_set_mode(): desktop window created (%016llx)\n", EW_DESKTOP);

	/* Set up mouse cursor */
	ew_set_cursor_limits(w, h);
	ew_select_cursor_id(EW_CURSOR_ARROW);


	return EFI_SUCCESS;
}

EFI_STATUS ew_set_text_mode()
{
	EFI_STATUS s = ST->ConOut->SetMode(ST->ConOut, 0);
	if(EFI_ERROR(s))
		return s;
	free_bb();
	return EFI_SUCCESS;
}

EFI_STATUS ew_get_backbuffer(void **buf)
{
	*buf = bb;
	return EFI_SUCCESS;
}

EFI_STATUS ew_get_backbuffer_size(int *bbw, int *bbh)
{
	*bbw = w;
	*bbh = h;
	return EFI_SUCCESS;
}

/* Blit from the buffer specified in 'buf' to the screen.
	'buf' is in the format of B8G8R8N8
*/
EFI_STATUS ew_blit(void *buf, int src_x, int src_y, int dest_x, int dest_y, int width, int height, int delta)
{
	int i, j;

	for(j = 0; j < height; j++)
	{
		for(i = 0; i < width; i++)
		{
			EFI_GRAPHICS_OUTPUT_BLT_PIXEL *src_pixel = (EFI_GRAPHICS_OUTPUT_BLT_PIXEL *)&((uint8_t *)buf)[(i + src_x) * 4 + (j + src_y) * delta];
			uint32_t *dest_pixel = (uint32_t *)&((uint8_t *)fb)[(i + dest_x) * 4 + (j + dest_y) * fb_stride * 4];

			/*fprintf(stderr, "src_x: %i, src_y: %i, dest_x: %i, dest_y: %i, width: %i, height: %i, delta: %i, i: %i, j: %i\n  "
				"src_pixel: %#016llx, dest_pixel: %#016llx\n", src_x, src_y, dest_x, dest_y, width, height, delta, i, j,
				src_pixel, dest_pixel);*/
			
			switch(fb_format)
			{
				case PixelBlueGreenRedReserved8BitPerColor:
					*dest_pixel = make_bgr_pixel(src_pixel);
					//*dest_pixel = 0x0000ff00;
					break;
				case PixelRedGreenBlueReserved8BitPerColor:
					*dest_pixel = make_rgb_pixel(src_pixel);
					//*dest_pixel = 0x0000ff00;
					break;
				default:
					break;
			}
		}
	}

	return EFI_SUCCESS;
}

EFI_STATUS ew_set_blend_mode(int enable, int src_blend, int dest_blend)
{
	blend_enable = enable;
	srcblend = src_blend;
	destblend = dest_blend;

	return EFI_SUCCESS;
}

struct double_pixel
{
	double r, g, b, a;
};

static struct double_pixel pixel_blend(struct double_pixel incoming, struct double_pixel src, struct double_pixel dest, int blend_mode)
{
	struct double_pixel ret;
	switch(blend_mode)
	{
		case EW_BLEND_ZERO:
			ret.r = 0;
			ret.g = 0;
			ret.b = 0;
			ret.a = 0;
			return ret;
		case EW_BLEND_SRCCOLOR:
			ret.r = incoming.r * src.r;
			ret.g = incoming.g * src.g;
			ret.b = incoming.b * src.b;
			ret.a = incoming.a * src.a;
			return ret;
		case EW_BLEND_INVSRCCOLOR:
			ret.r = incoming.r * (1.0 - src.r);
			ret.g = incoming.g * (1.0 - src.g);
			ret.b = incoming.b * (1.0 - src.b);
			ret.a = incoming.a * (1.0 - src.a);
			return ret;
		case EW_BLEND_SRCALPHA:
			ret.r = incoming.r * src.a;
			ret.g = incoming.g * src.a;
			ret.b = incoming.b * src.a;
			ret.a = incoming.a * src.a;
			return ret;
		case EW_BLEND_INVSRCALPHA:
			ret.r = incoming.r * (1.0 - src.a);
			ret.g = incoming.g * (1.0 - src.a);
			ret.b = incoming.b * (1.0 - src.a);
			ret.a = incoming.a * (1.0 - src.a);
			return ret;
		case EW_BLEND_DESTCOLOR:
			ret.r = incoming.r * dest.r;
			ret.g = incoming.g * dest.g;
			ret.b = incoming.b * dest.b;
			ret.a = incoming.a * dest.a;
			return ret;
		case EW_BLEND_INVDESTCOLOR:
			ret.r = incoming.r * (1.0 - dest.r);
			ret.g = incoming.g * (1.0 - dest.g);
			ret.b = incoming.b * (1.0 - dest.b);
			ret.a = incoming.a * (1.0 - dest.a);
			return ret;
		case EW_BLEND_DESTALPHA:
			ret.r = incoming.r * dest.a;
			ret.g = incoming.g * dest.a;
			ret.b = incoming.b * dest.a;
			ret.a = incoming.a * dest.a;
			return ret;
		case EW_BLEND_INVDESTALPHA:
			ret.r = incoming.r * (1.0 - dest.a);
			ret.g = incoming.g * (1.0 - dest.a);
			ret.b = incoming.b * (1.0 - dest.a);
			ret.a = incoming.a * (1.0 - dest.a);
			return ret;
		case EW_BLEND_ONE:
		default:
			ret.r = incoming.r;
			ret.g = incoming.g;
			ret.b = incoming.b;
			ret.a = incoming.a;
			return ret;
	}
}

uint32_t ew_blend(uint32_t src, uint32_t dest)
{
	if(blend_enable == 0)
		return src;

	struct double_pixel srcp, destp;
	srcp.r = (double)extract_red(src) / 255.0;
	srcp.g = (double)extract_green(src) / 255.0;
	srcp.b = (double)extract_blue(src) / 255.0;
	srcp.a = (double)extract_alpha(src) / 255.0;

	destp.r = (double)extract_red(dest) / 255.0;
	destp.g = (double)extract_green(dest) / 255.0;
	destp.b = (double)extract_blue(dest) / 255.0;
	destp.a = (double)extract_alpha(dest) / 255.0;

	struct double_pixel out_s, out_d;
	out_s = pixel_blend(srcp, srcp, destp, srcblend);
	out_d = pixel_blend(destp, srcp, destp, destblend);

	struct double_pixel out;
	out.r = out_s.r + out_d.r;
	out.g = out_s.g + out_d.g;
	out.b = out_s.b + out_d.b;
	out.a = out_s.a + out_d.a;

	if(out.r > 1.0)
		out.r = 1.0;
	if(out.g > 1.0)
		out.g = 1.0;
	if(out.b > 1.0)
		out.b = 1.0;
	if(out.a > 1.0)
		out.a = 1.0;

	uint32_t ret = (uint32_t)(out.b * 255.0) | (uint32_t)(out.g * 255.0) << 8 | (uint32_t)(out.r * 255.0) << 16 | (uint32_t)(out.a * 255.0) << 24;

	return ret;
}
