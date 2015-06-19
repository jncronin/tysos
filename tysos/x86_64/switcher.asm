global _ZN5tysos14tysos#2Ex86_6412TaskSwitcherM_0_16do_x86_64_switch_Rv_P4yU5tysos6Threadyy

_ZN5tysos14tysos#2Ex86_6412TaskSwitcherM_0_16do_x86_64_switch_Rv_P4yU5tysos6Threadyy:
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

	; cur_thread_pointer			= rdx
	; next_thread					= rsi
	; tsi_offset_within_thread		= rdx
	; rsp_offset_within_tsi			= rcx

	mov rax, [rdi]				; cur_thread
	
	cmp rax, 0					; if current thread is null, don't save the current rsp
	je .dontsave

	; if cur_thread != null
	mov rbx, [rax + rdx]		; cur_thread_tsi
	mov [rdx + rcx], rsp		; store rsp to rsp within current thread
.dontsave:

	; load rsp from the new thread
	mov rbx, [rsi + rdx]		; next_thread_tsi
	mov rsp, [rbx + rcx]		; load rsp from next thread

	; change the cur_thread pointer
	mov [rdi], rsi

	; restore state

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

