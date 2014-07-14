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

#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#ifdef WINDOWS
#include <windows.h>
#include <wincrypt.h>
#endif

#include "guid.h"

#define GUID_FMT "%08X-%04hX-%04hX-%02hhX%02hhX-%02hhX%02hhX%02hhX%02hhX%02hhX%02hhX"

static int rnd_init = 0;

int guid_to_string(char *str, GUID *guid)
{
	if(guid == NULL)
	{
		fprintf(stderr, "guid_to_string: guid is null\n");
		return -1;
	}
	if(str == NULL)
	{
		fprintf(stderr, "guid_to_string: str is null\n");
		return -1;
	}

	sprintf(str, GUID_FMT, guid->data1, guid->data2, guid->data3, guid->data4[0], guid->data4[1],
		guid->data4[2], guid->data4[3], guid->data4[4], guid->data4[5], guid->data4[6], guid->data4[7]);

	return 0;
}

int string_to_guid(GUID *guid, char *str)
{
	if(guid == NULL)
	{
		fprintf(stderr, "string_to_guid: guid is null\n");
		return -1;
	}
	if(str == NULL)
	{
		fprintf(stderr, "string_to_guid: str is null\n");
		return -1;
	}

	sscanf(str, GUID_FMT, &guid->data1, &guid->data2, &guid->data3, &guid->data4[0], &guid->data4[1],
		&guid->data4[2], &guid->data4[3], &guid->data4[4], &guid->data4[5], &guid->data4[6], &guid->data4[7]);

	return 0;
}

int guid_to_bytestring(uint8_t *bytes, GUID *guid)
{
	int i;

	if(guid == NULL)
	{
		fprintf(stderr, "guid_to_bytestring: guid is null\n");
		return -1;
	}
	if(bytes == NULL)
	{
		fprintf(stderr, "guid_to_bytestring: bytes is null\n");
		return -1;
	}

	*(uint32_t *)&bytes[0] = guid->data1;
	*(uint16_t *)&bytes[4] = guid->data2;
	*(uint16_t *)&bytes[6] = guid->data3;
	for(i = 0; i < 8; i++)
		bytes[8 + i] = guid->data4[i];

	return 0;
}

int guid_is_zero(GUID *guid)
{
	int i;

	if(guid->data1 != 0)
		return 0;
	if(guid->data2 != 0)
		return 0;
	if(guid->data3 != 0)
		return 0;
	for(i = 0; i < 8; i++)
	{
		if(guid->data4[i] != 0)
			return 0;
	}
	return 1;
}

void init_rnd()
{
	/* Attempt to initialise the random number generator */
	FILE *rnd = fopen("/dev/random", "r");
	if(rnd != NULL)
	{
		unsigned int seed;
		if(fread(&seed, 1, sizeof(unsigned int), rnd) == sizeof(unsigned int))
		{
			srand(seed);
			fclose(rnd);
			rnd_init = 1;
			return;
		}

		fclose(rnd);
	}

#ifdef WINDOWS
	{
		HCRYPTPROV prov = 0;
		if(!CryptAcquireContext(&prov, NULL, NULL, PROV_INTEL_SEC, CRYPT_VERIFYCONTEXT))
		{
			if(!CryptAcquireContext(&prov, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT))
				prov = 0;
		}

		if(prov != 0)
		{
			unsigned int seed;
			CryptGenRandom(prov, sizeof(unsigned int), (BYTE *)&seed);
			srand(seed);
			rnd_init = 1;
			return;
		}
	}
#endif

	{
		time_t t = time(NULL);
		struct tm *tmptr = gmtime(&t);
		int seed = tmptr->tm_hour;
		seed <<= 3;
		seed ^= tmptr->tm_isdst;
		seed <<= 3;
		seed ^= tmptr->tm_mday;
		seed <<= 3;
		seed ^= tmptr->tm_min;
		seed <<= 3;
		seed ^= tmptr->tm_mon;
		seed <<= 3;
		seed ^= tmptr->tm_sec;
		seed <<= 3;
		seed ^= tmptr->tm_wday;
		seed <<= 3;
		seed ^= tmptr->tm_yday;
		seed <<= 3;
		seed ^= tmptr->tm_year;

		srand((unsigned int)seed);
		rnd_init = 1;
		return;
	}
}

uint8_t rnd_byte()
{
	if(rnd_init == 0)
		init_rnd();

	return (uint8_t)(rand() & 0xff);
}

int random_guid(GUID *guid)
{
	int i;

	guid->data1 = (uint32_t)rnd_byte() | (((uint32_t)rnd_byte()) << 8) |
		(((uint32_t)rnd_byte()) << 16) | (((uint32_t)rnd_byte()) << 24);
	guid->data2 = (uint16_t)rnd_byte() | (((uint16_t)rnd_byte()) << 8);
	guid->data3 = (uint16_t)rnd_byte() | (((uint16_t)((rnd_byte() & 0x0f) | 0x40)) << 8);
	for(i = 0; i < 8; i++)
		guid->data4[i] = rnd_byte();

	return 0;
}
