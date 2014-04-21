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

; Code to go to 64 bit execution

; enable pae (set cr4.pae to 1)
; load cr3 with pml4t
; set ia32_efer.lme to 1 (need to use rdmsr/wrmsr)
; set cr0.pg to 1
; load a 64-bit gdt with cs.l = 1 and cs.d = 0

%ifdef EFI
global _go64
global _gdt64
global _detect64
extern _kif
%else
global go64
global gdt64
global detect64
extern kif
%endif

IA32_EFER		EQU	0xc0000080
LME			EQU 	0x100

;int detect64()
%ifdef EFI
_detect64:
%else
detect64:
%endif
	mov	eax, 0x80000001
	xor	ebx, ebx
	xor	ecx, ecx
	xor	edx, edx

	cpuid

	and	edx, (1 << 29)
	shr	edx, 29

	ret	

;void go64(unsigned long int pml4t, unsigned long long int e_point)
%ifdef EFI
_go64:
%else
go64:
%endif
	push			ebp
	mov			ebp, esp
	
	mov			eax, cr4
	or			eax, 0x20
	mov			cr4, eax
	
	mov			eax, [ebp + 8]
	mov			cr3, eax
	
	mov			ecx, IA32_EFER
	rdmsr
	or			eax, LME
	wrmsr
	
	mov			eax, cr0
	or			eax, 0x80000000
	and			eax, 0x9fffffff
	mov			cr0, eax
	
; use our own gdt
	lgdt			[gdt64info]
	jmp			0x8:.owngdt
	
.owngdt:
[BITS 64]	
	mov			rax, [rbp + 12]
	xor			rdx, rdx
%ifdef EFI
	mov			edx, [_kif]
%else
	mov			edx, [kif]
%endif
	push			rdx
	xchg			bx, bx
	
	call			rax
	add			rsp, 8
	xchg			bx, bx
	jmp			$	

[BITS 32]

section .data
align 32
gdt64info:
        dw 			gdt64_end - gdt64 - 1
        dd 			gdt64		; linear address	
	
%ifdef EFI
%else
section .gdt
%endif	
align 32
_gdt64:
gdt64   dd      0,0     ; null
code    db      0xff, 0xff, 0x00, 0x00, 0x00, 10011010b, 10101111b, 0x00
data    db      0xff, 0xff, 0x00, 0x00, 0x00, 10010010b, 11001111b, 0x00
code3	db	0xff, 0xff, 0x00, 0x00, 0x00, 11111110b, 10101111b, 0x00
data3	db	0xff, 0xff, 0x00, 0x00, 0x00, 11110010b, 11001111b, 0x00
tss_ent	dd	0, 0, 0, 0
.end

times (2048 - .end + gdt64) db 0
gdt64_end:

