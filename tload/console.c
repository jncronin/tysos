/* Copyright (C) 2008 - 2011 by John Cronin
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

/* console output functions */

#include "inline.c"
#include "console.h"

static int x = 0;
static int y = 0;

const char hex_digits[] = "0123456789ABCDEF";

unsigned short int * const vmem = (unsigned short int *)0xb8000;

#define DEF_FMT			0x07
#define MAX_X			80
#define MAX_Y			25

void _putc(unsigned char ch);
void _putcf(unsigned char ch, unsigned char fmt);
void _scrup(int lines);
void _updatecursor();

int clear()
{
	int i;
	for(i = 0; i < MAX_X * MAX_Y; i++)
		vmem[i] = (DEF_FMT << 8);
	return 0;
}

int putchar(int c)
{
	_putc((unsigned char)c);
	_updatecursor();
	return 0;
}

int _puts(const char *s, int newline)
{
	while(*s != '\0') {
		_putc((unsigned char)*s);
		s++;
	}
	if(newline != 0)
	{
		x = 0;
		y++;
		if(y >= MAX_Y) {
			_scrup(1);
			y = MAX_Y - 1;
		}
		_updatecursor();
	}
	return 0;
}

int puts(const char *s)
{
	return _puts(s, 1);
}

void _updatecursor()
{
   unsigned short position=(y*MAX_X) + x;

   /* cursor LOW port to vga INDEX register */
   outb(0x3D4, 0x0F);
   outb(0x3D5, (unsigned char)(position&0xFF));
   /* cursor HIGH port to vga INDEX register */
   outb(0x3D4, 0x0E);
   outb(0x3D5, (unsigned char )((position>>8)&0xFF));
}

void _putc(unsigned char ch)
{
	_putcf(ch, DEF_FMT);
}

void _putcf(unsigned char ch, unsigned char fmt)
{
	vmem[x + MAX_X * y] = (fmt << 8) | ch;

	x++;
	if(x >= MAX_X) {
		x = 0;
		y++;
	}
	if(y >= MAX_Y) {
		_scrup(1);
		y = MAX_Y - 1;
	}
}

void _scrup(int lines)
{
	/* scroll up a number of lines */

	/* the number of lines we need to move are MAX_Y - lines */
	/* we start moving from the lines'th line and move that to the first line and so on */
	/* then clear the last lines */

	int i, j;

	for(i = 0; i < MAX_Y; i++) {
		for(j = 0; j < MAX_X; j++) {
			if(i < (MAX_Y - lines))
				vmem[j + MAX_X * i] = vmem[j + MAX_X * (lines + i)];
			else
				vmem[j + MAX_X * i] = (DEF_FMT << 8);
		}
	}
}

int puthex(unsigned long val)
{
	_putc('0');
	_putc('x');

	return _puthex(val);
}

int _puthex(unsigned long val)
{
	int digit;

	for(digit = 7; digit >= 0; digit--)
		_putc(hex_digits[(val >> (digit * 4)) & 0xf]);

	_updatecursor();
	return 0;
}

