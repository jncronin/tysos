global _ZX11TysosMethodM_0_14InternalInvoke_Ru1O_P3u1IiPv

; static object InternalInvoke(IntPtr meth, int param_count, void* params)
_ZX11TysosMethodM_0_14InternalInvoke_Ru1O_P3u1IiPv:
	push rbp
	mov rbp, rsp

	mov rax, [rsp + 16]
	mov rcx, [rsp + 24]
	mov rdi, [rsp + 32]

	cmp rcx, 0
	jz .docall

; move to the last parameter - pushing in rtl order
	mov rdx, rcx
	shl rdx, 3
	add rdi, rdx

.startloop:
	sub rdi, 8
	push qword [rdi]
	loop .startloop

.docall:
	call rax
	leave
	ret

