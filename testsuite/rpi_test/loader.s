.globl sthrow
.globl __zeromem_vii4

sthrow:
halt:
	wfe
	b halt

__zeromem_vii4:
	mov r2, #0
	add r3, r0, r1

	b .test

.loop:
	stmia r0!, { r2 }

.test:
	cmp r0, r3
	blo .loop

	mov pc, lr


