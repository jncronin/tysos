/*-------------------------------------------*/
/* Integer type definitions for FatFs module */
/*-------------------------------------------*/

#ifndef _FF_INTEGER
#define _FF_INTEGER

#ifdef _WIN32	/* FatFs development platform */

#include <windows.h>
#include <tchar.h>

#else			/* Embedded platform */

/* Modified for mkfat - on cygwin64 (at least), unsigned long is
	8 bytes, rather than the 4 expected in the fatfs sources */
#include <stdint.h>
typedef uint8_t			BYTE;
typedef int16_t			SHORT;
typedef uint16_t		WORD;
typedef uint16_t		WCHAR;
typedef int				INT;
typedef unsigned int	UINT;
typedef int32_t			LONG;
typedef uint32_t		DWORD;
#endif

#endif
