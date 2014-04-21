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

#ifndef EWPROGRESS_H
#define EWPROGRESS_H

#define EW_PROGRESS_LEFTRIGHT		0
#define EW_PROGRESS_RIGHTLEFT		1
#define EW_PROGRESS_TOPBOTTOM		2
#define EW_PROGRESS_BOTTOMTOP		3

typedef struct _ew_progress
{
	EW_COLOR barcolor, backcolor, linecolor;
	int linewidth;
	int progress;
	int direction;
	FONT *caption_font;
	EW_COLOR caption_color;
	int caption_font_max_w;
	int caption_font_max_h;
} EW_PROGRESS;

EFI_STATUS ew_create_progress(WINDOW **w, RECT *loc, WINDOW *parent, EW_PROGRESS *info);
EFI_STATUS ew_set_progress_info(WINDOW *w, EW_PROGRESS *desc);
EFI_STATUS ew_get_progress_info(WINDOW *w, EW_PROGRESS *desc);

#endif
