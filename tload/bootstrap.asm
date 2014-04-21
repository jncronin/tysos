; Copyright (C) 2008 - 2011 by John Cronin
;
; Permission is hereby granted, free of charge, to any person obtaining a copy
; of this software and associated documentation files (the "Software"), to deal
; in the Software without restriction, including without limitation the rights
; to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
; copies of the Software, and to permit persons to whom the Software is
; furnished to do so, subject to the following conditions:

; The above copyright notice and this permission notice shall be included in
; all copies or substantial portions of the Software.

; THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
; IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
; FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
; AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
; LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
; OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
; THE SOFTWARE.

MODULEALIGN		equ		1<<0		; align modules on page boundaries
MEMINFO			equ		1<<1		; provide memory map
FLAGS			equ		MODULEALIGN | MEMINFO
MAGIC			equ		0x1BADB002	; MultiBoot magic
CHECKSUM		equ		-(MAGIC + FLAGS)

STACKSIZE		equ		0x8000		; 32kB of stack space

HEADERLEN		equ		90

global		loader
extern		kmain
global		mb_addr

section	.multiboot
align	4
mbheader:
	dd		MAGIC
	dd		FLAGS
	dd		CHECKSUM
	
section .text
loader:
; store multiboot info
	mov		[mb_addr], ebx
	mov		[mb_magic], eax
	
; use our own gdt
	lgdt	[gdtinfo]
	jmp		0x8:.owngdt
	
.owngdt:
	mov		eax,	0x10
	mov		ss,		eax
	mov		ds,		eax
	mov		es,		eax
	mov		fs,		eax
	mov		gs,		eax
	
	mov		esp,	stack + STACKSIZE		; top of stack in bss section
	
disableints:
	mov		al, 0xff
	out		0x21, al
	out		0xa1, al
	cli
	
	push	dword [mb_addr]
	push	dword [mb_magic]
	call	kmain
	
	cli
	hlt
	
	
	
section .data
align 32
gdtinfo:
        dw 		gdt_end - gdt - 1
        dd 		gdt		; linear address	
	
	
	
section .gdt
align 32
gdt    dd      0,0     ; null
code    db      0xff, 0xff, 0x00, 0x00, 0x00, 10011010b, 11001111b, 0x00
data    db      0xff, 0xff, 0x00, 0x00, 0x00, 10010010b, 11001111b, 0x00
code3	db		0xff, 0xff, 0x00, 0x00, 0x00, 11111110b, 11001111b, 0x00
data3	db		0xff, 0xff, 0x00, 0x00, 0x00, 11110010b, 11001111b, 0x00
tss_ent	dd		0, 0
.end

times (2048 - .end + gdt) db 0
gdt_end:


section .bss
align 32
stack: resb STACKSIZE
mb_magic: resd	1
mb_addr: resd	1

