global sthrow
weak _Zu1O_7#2Ector_Rv_P1u1t
weak _Zu1S
weak __cxa_pure_virtual

extern kmain

MODULEALIGN		equ	1<<0
MEMINFO			equ	1<<1
FLAGS			equ	MODULEALIGN | MEMINFO
MAGIC			equ	0x1badb002
CHECKSUM		equ	-(MAGIC + FLAGS)

section .text

align 4
dd MAGIC
dd FLAGS
dd CHECKSUM

sthrow:
	xchg bx, bx
	hlt
	jmp sthrow



_Zu1O_7#2Ector_Rv_P1u1t:
	ret

__cxa_pure_virtual:
	ret

section .data
_Zu1S:
	dd 0, 0, 0			; TIPtr, IFacePtr, Extends
	dd __cxa_pure_virtual
	dd __cxa_pure_virtual
	dd __cxa_pure_virtual
	dd __cxa_pure_virtual

