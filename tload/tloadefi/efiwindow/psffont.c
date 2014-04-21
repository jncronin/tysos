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
#include <errno.h>
#include <string.h>
#include <stdlib.h>
#include <assert.h>
#include <efi.h>

struct _psffont
{
	FONT f;
	
	void *fbuf;
	int c_w, c_h;
	int charsize;
	int length;
};

#define PSF1_MAGIC0     0x36
#define PSF1_MAGIC1     0x04

#define PSF1_MODE512    0x01
#define PSF1_MODEHASTAB 0x02
#define PSF1_MODEHASSEQ 0x04
#define PSF1_MAXMODE    0x05

#define PSF1_SEPARATOR  0xFFFF
#define PSF1_STARTSEQ   0xFFFE

struct psf1_header {
	unsigned char magic[2];     /* Magic number */
	unsigned char mode;         /* PSF font mode */
	unsigned char charsize;     /* Character size */
};

#define PSF2_MAGIC0     0x72
#define PSF2_MAGIC1     0xb5
#define PSF2_MAGIC2     0x4a
#define PSF2_MAGIC3     0x86

/* bits used in flags */
#define PSF2_HAS_UNICODE_TABLE 0x01

/* max version recognized so far */
#define PSF2_MAXVERSION 0

/* UTF8 separators */
#define PSF2_SEPARATOR  0xFF
#define PSF2_STARTSEQ   0xFE

struct psf2_header {
	unsigned char magic[4];
	unsigned int version;
	unsigned int headersize;    /* offset of bitmaps in file */
	unsigned int flags;
	unsigned int length;        /* number of glyphs */
	unsigned int charsize;      /* number of bytes for each character */
	unsigned int height, width; /* max dimensions of glyphs */
	/* charsize = height * ((width + 7) / 8) */
};

static EFI_STATUS psf_draw_glyph(FONT *f, WINDOW *w, CHAR16 c, int x, int y, int c_w, int c_h, EW_COLOR forecolor, EW_COLOR backcolor);

EFI_STATUS ew_load_psf_font(FONT **font, char *fname)
{
	if((font == NULL) || (fname == NULL))
		return EFI_INVALID_PARAMETER;

	FILE *fd = fopen(fname, "r");
	if(fd == NULL)
	{
		fprintf(stderr, "load_psffont: could not open %s: %s\n", fname, strerror(errno));
		return EFI_INVALID_PARAMETER;
	}

	struct _psffont *f = (struct _psffont *)malloc(sizeof(struct _psffont));
	if(f == NULL)
		return EFI_OUT_OF_RESOURCES;

	uint8_t *hdr = (uint8_t *)malloc(sizeof(struct psf2_header));
	if(hdr == NULL)
		return EFI_OUT_OF_RESOURCES;

	if(fread(hdr, 1, sizeof(struct psf2_header), fd) != sizeof(struct psf2_header))
	{
		fprintf(stderr, "load_psffont: failed to read header\n");
		free(hdr);
		return EFI_ABORTED;
	}

	/* determine type */
	int type = 0;
	struct psf1_header *h1 = (struct psf1_header *)hdr;
	struct psf2_header *h2 = (struct psf2_header *)hdr;

	if((h2->magic[0] == PSF1_MAGIC0) && (h2->magic[1] == PSF1_MAGIC1))
		type = 1;
	else if((h2->magic[0] == PSF2_MAGIC0) && (h2->magic[1] == PSF2_MAGIC1) &&
		(h2->magic[2] == PSF2_MAGIC2) && (h2->magic[3] == PSF2_MAGIC3))
		type = 2;
	else
	{
		fprintf(stderr, "load_psffont: invalid magic number: %02x %02x %02x %02x\n",
			h2->magic[0], h2->magic[1], h2->magic[2], h2->magic[3]);
		free(hdr);
		return EFI_ABORTED;
	}

	/* load the font */
	switch(type)
	{
		case 1:
			f->charsize = h1->charsize;
			f->c_w = 8;
			f->c_h = h1->charsize;
			f->length = 256;
			if(h1->mode & PSF1_MODE512)
				f->length = 512;
			fseek(fd, sizeof(struct psf1_header), SEEK_SET);
			break;
		case 2:
			f->charsize = h2->charsize;
			f->c_w = h2->width;
			f->c_h = h2->height;
			f->length = h2->length;
			fseek(fd, h2->headersize, SEEK_SET);
			break;
	}
	size_t to_read = f->length * f->charsize;
	f->fbuf = malloc(to_read);
	if(f->fbuf == NULL)
	{
		fprintf(stderr, "load_psffont: could not allocate memory (%i bytes) for font\n",
			to_read);
		free(hdr);
		return EFI_OUT_OF_RESOURCES;
	}
	size_t read;
	if((read = fread(f->fbuf, 1, to_read, fd)) != to_read)
	{
		fprintf(stderr, "load_psffont: error loading font: wanted %i, only read %i\n",
			to_read, read);
		free(hdr);
		return EFI_ABORTED;
	}
	fclose(fd);

	f->f.draw_glyph = psf_draw_glyph;
	f->f.natural_w = f->c_w;
	f->f.natural_h = f->c_h;
	free(hdr);

	*font = (FONT *)f;
	return EFI_SUCCESS;
}

EFI_STATUS psf_draw_glyph(FONT *f, WINDOW *w, CHAR16 c, int x, int y, int c_w, int c_h, EW_COLOR forecolor, EW_COLOR backcolor)
{
	struct _psffont *pf = (struct _psffont *)f;

	if(c > pf->length)
		c = ' ';

	int d_y, d_x;
	/* Iterate through destination pixels */
	for(d_y = 0; d_y < c_h; d_y++)
	{
		for(d_x = 0; d_x < c_w; d_x++)
		{
			/* The actual screen coordinates to output to */
			int scr_x = x + d_x;
			int scr_y = y + d_y;

			/* The source coordinates */
			int s_x = (d_x * pf->c_w) / c_w;
			int s_y = (d_y * pf->c_h) / c_h;

			/* Find the character in the font buffer */
			uint8_t *src = (uint8_t *)((uintptr_t)pf->fbuf + c * pf->charsize);

			/* The bit within the character */
			int s_bit = s_y * pf->c_w + s_x;
			while(s_bit > 8)
			{
				s_bit -= 8;
				src++;
			}
			uint8_t pixel = (*src >> (7 - s_bit)) & 0x1;

			uint32_t *dest = (uint32_t *)&((uint8_t *)w->buf)[(scr_x + scr_y * w->loc.w) * EW_BB_PIXEL_SIZE];
			if(pixel)
				*dest = forecolor;
			else
				*dest = backcolor;
		}
	}

	return 0;
}
