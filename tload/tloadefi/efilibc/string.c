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

#include <string.h>
#include <stdint.h>
#include <ctype.h>
#include <stdio.h>
#include <errno.h>
#include <stdlib.h>
#include <wchar.h>

size_t strlen(const char *s)
{
	size_t ret = 0;
	while(*s++ != '\0')
		ret++;
	return ret;
}

size_t strnlen(const char *s, size_t maxlen)
{
	size_t ret = 0;
	while((*s++ != '\0') && (ret < maxlen))
		ret++;
	return ret;
}

size_t wcslen(const wchar_t *s)
{
	size_t ret = 0;
	while(*s++ != 0)
		ret++;
	return ret;
}

int strcmp(const char *s1, const char *s2)
{
	while(*s1 || *s2)
	{
		char s = *s1++ - *s2++;
			if(s != 0)
		return (int)s;
	}
	return 0;
}

int memcmp(const void *s1, const void *s2, size_t n)
{
	uint8_t *a = (uint8_t *)s1;
	uint8_t *b = (uint8_t *)s2;

	size_t i;
	for(i = 0; i < n; i++)
	{
		uint8_t v = a[i] - b[i];
		if(v != 0)
			return (int)v;
	}

	return 0;
}

void *memset(void *s, int c, size_t n)
{
	while(n--)
		((uint8_t *)s)[n] = (uint8_t)c;
	return s;
}

void *memcpy(void *dest, const void *src, size_t n)
{
	while(n--)
		((uint8_t *)dest)[n] = ((uint8_t *)src)[n];
	return dest;
}

size_t mbstowcs(wchar_t *dest, const char *src, size_t n)
{
	int conv = 0;
	if(dest == NULL)
		return strlen(src);
	while(n--)
	{
		int end = 0;
		if(*src == '\0')
			end = 1;
		*dest++ = (wchar_t)*src++;
		conv++;
		if(end)
			break;
	}
	return conv;
}

size_t wcstombs(char *dest, const wchar_t *src, size_t n)
{
	size_t conv = 0;
	if(dest == NULL)
		return wcslen(src);

	while(n--)
	{
		int end = 0;
		if(*src == 0)
			end = 1;
		*dest++ = (wchar_t)*src++;
		conv++;
		if(end)
			break;
	}
	return conv;
}

char *strchr(const char *s, int c)
{
	while(1)
	{
		if(*s == (char)c)
			return (char *)s;
		if(*s == '\0')
			return NULL;
		s++;
	}
}

char *getenv(const char *name)
{
	(void)name;
	return NULL;
}

char *strcpy(char *dest, const char *src)
{
	char *d = dest;
	while(*src != '\0')
		*d++ = *src++;
	*d = '\0';
	return dest;
}

char *strncpy(char *dest, const char *src, size_t n)
{
	char *d = dest;
	while((*src != '\0') && n--)
		*d++ = *src++;
	if(n)
		*d = '\0';
	return dest;
}

int tolower(int c)
{
	if((c >= 'A') && (c <= 'Z'))
		return c - 'A' + 'a';
	else
		return c;
}

int toupper(int c)
{
	if((c >= 'a') && (c <= 'z'))
		return c - 'a' + 'A';
	else
		return c;
}

int isdigit(int c)
{
	if((c >= '0') && (c <= '9'))
		return 1;
	else
		return 0;
}

int isalpha(int c)
{
	return isupper(c) || islower(c);
}

int isalnum(int c)
{
	return isalpha(c) || isdigit(c);
}

int isspace(int c)
{
	if((c == '\f') || (c == '\n') || (c == '\r') || (c == '\t') || (c == '\v') || (c == ' '))
		return 1;
	else
		return 0;
}

int isupper(int c)
{
	if((c >= 'A') && (c <= 'Z'))
		return 1;
	else
		return 0;
}

int islower(int c)
{
	if((c >= 'a') && (c <= 'z'))
		return 1;
	else
		return 0;
}

int isgraph(int c)
{
	if((c >= '!') && (c <= '~'))
		return 1;
	else
		return 0;
}

int isprint(int c)
{
	if((c >= ' ') && (c <= '~'))
		return 1;
	else
		return 0;
}

size_t strspn(const char *s, const char *accept)
{
	size_t ret = 0;

	while(*s != '\0')
	{
		int found = 0;

		const char *a = accept;
		while(*a != '\0')
		{
			if(*s == *a)
			{
				found = 1;
				break;
			}
			a++;
		}

		if(found)
			ret++;
		else
			break;

		s++;
	}

	return ret;
}

size_t strcspn(const char *s, const char *reject)
{
	size_t ret = 0;

	while(*s != '\0')
	{
		int found = 0;

		const char *r = reject;
		while(*r != '\0')
		{
			if(*s == *r)
			{
				found = 1;
				break;
			}
			r++;
		}

		if(found)
			break;
		else
			ret++;

		s++;
	}

	return ret;
}

char *strerror(int errnum)
{
	switch(errnum)
	{
	case 0:
		return "No error";
	case EDOM:
		return "Mathematics argument out of domain of function";
	case ERANGE:
		return "Result too large";
	case EILSEQ:
		return "Illegal byte sequence";
	case EEXIST:
		return "File exists";
	case EACCES:
		return "Permission denied";
	case EFAULT:
		return "Bad address";
	case ENOENT:
		return "No such file or directory";
	case EINVAL:
		return "Invalid argument";
	case ENOMEM:
		return "Not enough space";
	case EINTR:
		return "Interrupted function";
	case EBADF:
		return "Bad file descriptor";
	case EMFILE:
		return "File descriptor value too large";
	default:
		{
			char *ret = (char *)malloc(30);
			if(ret == NULL)
			{
				errno = ERANGE;
				return strerror(ERANGE);
			}
			sprintf(ret, "Error %d occurred", errnum);
			errno = EINVAL;
			return ret;
		}
	}
}

void *memchr(const void *s, int c, size_t n)
{
	uint8_t *src = (uint8_t *)s;

	while(n--)
	{
		if(*src == (uint8_t)c)
			return (void *)src;
		src++;
	}

	return NULL;
}

char *strcat(char *dest, const char *src)
{
	size_t dlen = strlen(dest);
	strcpy(&dest[dlen], src);
	return dest;
}
