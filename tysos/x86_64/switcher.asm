global _ZN11tysos#2Edll14tysos#2Ex86_6412TaskSwitcher_16do_x86_64_switch_Rv_P4yU5tysos6Threadyy:function

_ZN11tysos#2Edll14tysos#2Ex86_6412TaskSwitcher_16do_x86_64_switch_Rv_P4yU5tysos6Threadyy:
	; static void do_x86_64_switch(ulong cur_thread_pointer,
	;	Thread next_thread,
	;	ulong tsi_offset_within_thread,
	;	ulong rsp_offset_within_tsi);

	pushfq
	push rax
	push rbx
	push rcx
	push rdx
	push rsi
	push rdi
	push rbp
	push r8
	push r9
	push r10
	push r11
	push r12
	push r13
	push r14
	push r15

	; for now, just save the entire xmm registers.  Later, we will do it demand-based.
	sub rsp, 128
	movq [rsp], xmm0
	movq [rsp + 8], xmm1
	movq [rsp + 16], xmm2
	movq [rsp + 24], xmm3
	movq [rsp + 32], xmm4
	movq [rsp + 40], xmm5
	movq [rsp + 48], xmm6
	movq [rsp + 56], xmm7
	movq [rsp + 64], xmm8
	movq [rsp + 72], xmm9
	movq [rsp + 80], xmm10
	movq [rsp + 88], xmm11
	movq [rsp + 96], xmm12
	movq [rsp + 104], xmm13
	movq [rsp + 112], xmm14
	movq [rsp + 120], xmm15

	; cur_thread_pointer			= rdx
	; next_thread					= rsi
	; tsi_offset_within_thread		= rdx
	; rsp_offset_within_tsi			= rcx

	mov rax, [rdi]				; cur_thread
	
	cmp rax, 0					; if current thread is null, don't save the current rsp
	je .dontsave

	; if cur_thread != null
	mov rbx, [rax + rdx]		; cur_thread_tsi
	mov [rbx + rcx], rsp		; store rsp to rsp within current thread
.dontsave:

	; load rsp from the new thread
	mov rbx, [rsi + rdx]		; next_thread_tsi
	mov rsp, [rbx + rcx]		; load rsp from next thread

	; change the cur_thread pointer
	mov [rdi], rsi

	; restore state

	movq xmm0, [rsp]
	movq xmm1, [rsp + 8]
	movq xmm2, [rsp + 16]
	movq xmm3, [rsp + 24]
	movq xmm4, [rsp + 32]
	movq xmm5, [rsp + 40]
	movq xmm6, [rsp + 48]
	movq xmm7, [rsp + 56]
	movq xmm8, [rsp + 64]
	movq xmm9, [rsp + 72]
	movq xmm10, [rsp + 80]
	movq xmm11, [rsp + 88]
	movq xmm12, [rsp + 96]
	movq xmm13, [rsp + 104]
	movq xmm14, [rsp + 112]
	movq xmm15, [rsp + 120]
	add rsp, 128	

	pop r15
	pop r14
	pop r13
	pop r12
	pop r11
	pop r10
	pop r9
	pop r8
	pop rbp
	pop rdi
	pop rsi
	pop rdx
	pop rcx
	pop rbx
	pop rax
	popfq

	ret

