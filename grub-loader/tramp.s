/* Upon entry to trampoline, stack is:

	0x0(%esp)		return eip
	eax:edx			entry point
	0x4(%esp)		pml4t
	0xc(%esp)		mbheader
	0x14(%esp)		__halt_func
	0x1c(%esp)		kernel stack
	0x24(%esp)		gdt
	0x2c(%esp)		kif
*/

.globl trampoline
.globl trampoline_end
.code32
trampoline:
	cli

/* This follows the sequence in Intel 3A:9.8.5

	First, disable paging if it is enabled
*/

	mov		%cr0, %ecx
	andl	$0x7fffffff, %ecx
	mov		%ecx, %cr0

	// Enable PAE in cr4
	mov		%cr4, %ecx
	orl		$0x20, %ecx
	mov		%ecx, %cr4

	// Load cr3 with the pml4t
	movl	0x4(%esp), %ecx
	mov		%ecx, %cr3

	// Enable ia32_efer.lme
	/* First save important values.

		This assumes all addresses are 32-bit and that gdt = kif + 0x4000

		edx = epoint
		ebx = mbheader
		esi = halt func
		edi = kstack
	*/

	pushl	%eax		//epoint

	movl	0x10(%esp), %ebx
	movl	0x18(%esp), %esi
	movl	0x20(%esp), %edi

	movl	$0xc0000080, %ecx
	rdmsr
	orl		$0x100, %eax
	wrmsr
	rdmsr

	popl	%edx		//epoint

	// Enable paging
	mov		%cr0, %ecx
	orl		$0x80000000, %ecx
	mov		%ecx, %cr0

	// Load 64-bit gdt and idt
	mov		%ebx, %eax
	addl	$0x4c00, %eax
	lidt	(%eax)
	addl	$0x100, %eax
	lgdt	(%eax)

	// Set segment selectors and stack pointer
	movl	$0x10, %eax
	mov		%ax, %ds
	mov		%ax, %es
	mov		%ax, %fs
	mov		%ax, %gs
	mov		%ax, %ss
	movl	%edi, %esp

	// call next instruction to get its address in (%esp)
	.byte	0xe8, 0x00, 0x00, 0x00, 0x00
next_instr:

	// set up an iret block on the stack to load cs:rip
	movl	(%esp), %ecx
	movl	$(clear_pipe - next_instr), %eax
	addl	%ecx, %eax

	push	$0x10
	push	%edi
	push	$0x2
	push	$0x8
	push	%eax
	iret

clear_pipe:
.code64
	/* call the kernel then halt function

		mbheader (in ebx) -> rdi
		halt func (in esi) -> rbx (for saving)

		kernel is in edx
	*/

	movq	%rbx, %rdi
	movq	%rsi, %rbx

	callq	*%rdx
	callq	*%rbx

_spin:
	hlt
	pause
	.byte 0xeb, 0xfb	//(jmp -3)
trampoline_end:
