/* setjmp implementation - store registers in 'env' array

   int setjmp(jmp_buf env);
*/

.globl setjmp

setjmp:
leaq 8(%rsp), %rax
movq %rax, 0(%rax)
movq %rbx, 8(%rax)
movq %rcx, 16(%rax)
movq %rdx, 24(%rax)
movq %rsi, 32(%rax)
movq %rdi, 40(%rax)
movq %rbp, 48(%rax)
movq %rsp, 56(%rax)
movq %r8, 64(%rax)
movq %r9, 72(%rax)
movq %r10, 80(%rax)
movq %r11, 88(%rax)
movq %r12, 96(%rax)
movq %r13, 104(%rax)
movq %r14, 112(%rax)
movq %r15, 120(%rax)
movq $0, %rax
ret

