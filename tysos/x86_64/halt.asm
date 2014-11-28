global __ty_gettype
global gcmalloc
global __halt:function
global __ty_strcpy
global _ZX15OtherOperationsM_0_4Exit_Rv_P0:function

extern __display_halt

__ty_strcpy:
	jmp $

__ty_gettype:
	jmp $

__halt:
	call __display_halt
.halt:
	xchg bx, bx
	hlt
	jmp .halt

_ZX15OtherOperationsM_0_4Exit_Rv_P0:
.halt:
	hlt
	jmp .halt

