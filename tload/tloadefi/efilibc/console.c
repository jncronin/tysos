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

#include <efi.h>
#include <efilib.h>
#include <stdio.h>

static void reset_conin()
{
	static int conin_reset = 0;

	if(conin_reset == 0)
	{
		ST->ConIn->Reset(ST->ConIn, 0);
		conin_reset = 1;
	}
}

int puts(const char *s)
{
	while(*s != '\0')
		putc(*s++, stdout);
	putc('\n', stdout);
	return 0;
}

int putchar(int c)
{
	return putc(c, stdout);
}

size_t efilibc_console_fread(void *ptr, size_t size, size_t nmemb, void *data)
{
	reset_conin();

	size_t read = 0;
	uint8_t *buf = (uint8_t *)ptr;
	EFI_INPUT_KEY k;
	EFI_STATUS s;
	while((read < (size * nmemb)) && (s = ST->ConIn->ReadKeyStroke(ST->ConIn, &k)) != EFI_DEVICE_ERROR)
	{
		if((s == EFI_SUCCESS) && k.UnicodeChar != 0)
			buf[read++] = (uint8_t)k.UnicodeChar;
	}

	(void)data;
	return read / size;
}

size_t efilibc_console_fwrite(const void *ptr, size_t size, size_t nmemb, void *data)
{
	size_t written = 0;
	CHAR16 str[] = { 0, 0 };
	uint8_t *buf = (uint8_t *)ptr;
	while(written < (size * nmemb))
	{
		str[0] = (CHAR16)buf[written];
		
		if(ST->ConOut->OutputString(ST->ConOut, str) == EFI_SUCCESS)
		{
			if(buf[written] == '\n')
			{
				str[0] = '\r';
				ST->ConOut->OutputString(ST->ConOut, str);
			}
			written++;
		}
		else
			break;
	}

	(void)data;
	return written / size;
}
