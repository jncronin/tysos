/* setjmp implementation - store registers in 'env' array

   int setjmp(jmp_buf env);
*/

.globl setjmp

setjmp:
leal 4(%esp), %eax
movl %eax, 0(%eax)
movl %ebx, 4(%eax)
movl %ecx, 8(%eax)
movl %edx, 12(%eax)
movl %esi, 16(%eax)
movl %edi, 20(%eax)
movl %ebp, 24(%eax)
movl %esp, 28(%eax)
movl $0, %eax
ret

