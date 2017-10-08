/* ===-- udivdi3.c - Implement __udivdi3 -----------------------------------===
 *
 *                    The LLVM Compiler Infrastructure
 *
 * This file is dual licensed under the MIT and the University of Illinois Open
 * Source Licenses. See LICENSE.TXT for details.
 *
 * ===----------------------------------------------------------------------===
 *
 * This file implements __udivdi3 for the compiler_rt library.
 *
 * ===----------------------------------------------------------------------===
 */

#define CHAR_BIT 8
typedef int si_int;
typedef unsigned su_int;
typedef long long di_int;
typedef unsigned long long du_int;

typedef union
{
	di_int all;
	struct
	{
#if _YUGA_LITTLE_ENDIAN
		su_int low;
		si_int high;
#else
		si_int high;
		su_int low;
#endif /* _YUGA_LITTLE_ENDIAN */
	}s;
} dwords;

typedef union
{
	du_int all;
	struct
	{
#if _YUGA_LITTLE_ENDIAN
		su_int low;
		su_int high;
#else
		su_int high;
		su_int low;
#endif /* _YUGA_LITTLE_ENDIAN */
	}s;
} udwords;

du_int
__udivmoddi4(du_int a, du_int b, du_int* rem);

/* Returns: a / b */

du_int
__udivdi3(du_int a, du_int b)
{
    return __udivmoddi4(a, b, 0);
}
