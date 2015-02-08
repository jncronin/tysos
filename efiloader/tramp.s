.globl trampoline
.globl trampoline_end
trampoline:
	movq	0x28(%rsp), %rbx	/* stack is return address, 32 bytes of shadow space, 5th param */

	movl	$0x6c00, %eax
	lidt	(%rax)
	movl	$0x6d00, %eax
	lgdt	(%rax)


	movl	$0x10, %eax
	mov		%ax, %ds
	mov		%ax, %es
	mov		%ax, %fs
	mov		%ax, %gs
	mov		%ax, %ss
	movq	$0x2000, %rsp

	movl	$(clear_pipe - trampoline), %eax
	addl	$0x1000, %eax

	push	$0x10
	push	$0x2000
	push	$0x2
	push	$0x8
	push	%rax
	rex.w iret
clear_pipe:
	movq	%rdx, %cr3
	movq	%rbx, %rsp
	movq	%r8, %rdi
	pushq	%r9
	callq	*%rcx
	pop		%r9
	callq	*%r9
_spin:
	hlt
	pause
	jmp		_spin
trampoline_end:
