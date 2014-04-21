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

	mov rax, [rsp + 136]		; cur_thread_pointer
	mov rbx, [rsp + 144]		; next_thread
	mov rcx, [rsp + 152]		; tsi_offset_within_thread
	mov rdx, [rsp + 160]		; rsp_offset_within_tsi

	mov rsi, [rax]				; cur_thread
	
	cmp rsi, 0					
	je .dontsave

	; if cur_thread != null
	mov rdi, [rsi + rcx]		; cur_thread_tsi
	mov [rdi + rdx], rsp		; store rsp to rsp within current thread
.dontsave:

	; load rsp from the new thread
	mov rdi, [rbx + rcx]		; next_thread_tsi
	mov rsp, [rdi + rdx]		; load rsp from next thread

	; change the cur_thread pointer
	mov [rax], rbx

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

