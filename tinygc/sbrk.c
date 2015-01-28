/* A basic sbrk implementation.
 *
 * It is implemented in C to support the returning of (void *)-1 and
 * setting errno to ENOMEM on error.
 */

#include <errno.h>
#include <stdint.h>

void printf(const char *fmt, ...);

static uintptr_t initial_brk = 0;
static uintptr_t cur_brk = 0;
static uintptr_t max_brk = 0;

void initheap(uintptr_t start, uintptr_t end)
{
	cur_brk = start;
	initial_brk = start;
	max_brk = end;

	printf("SBRK: initheap(0x%zx, 0x%zx) called\n", start, end);
}

void *sbrk(intptr_t increment)
{
#ifdef DEBUG_SBRK
	printf("SBRK: sbrk(0x%zx) called\n", increment);
#endif

	if(increment == 0)
		return (void*)cur_brk;
	else if (increment > 0)
	{
		if((max_brk - cur_brk) > (uintptr_t)increment)
		{
			uintptr_t ret = cur_brk;
			cur_brk += (uintptr_t)increment;
#ifdef DEBUG_SBRK
			printf("SBRK: returning 0x%zx\n", ret);
#endif
			return (void*)ret;
		}
		else
		{
#ifdef DEBUG_SBRK
			printf("SBRK: failed on request for 0x%zx bytes\n", increment);
#endif
			errno = ENOMEM;
			return (void*)-1;
		}
	}
	else
	{
		if((cur_brk - initial_brk) > (uintptr_t)(-increment))
		{
			uintptr_t ret = cur_brk;
			cur_brk -= (uintptr_t)(-increment);
			return (void*)ret;
		}
		else
		{
#ifdef DEBUG_SBRK
			printf("SBRK: failed on request to release 0x%zx bytes\n",
					-increment);
#endif
			errno = ENOMEM;
			return (void*)-1;
		}
	}
}

