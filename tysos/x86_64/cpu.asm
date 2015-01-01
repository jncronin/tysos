global _ZN5tysos11tysos#2Elib7MonitorM_0_11spinunlockb_Rv_P1y:function
global _ZN5tysos11tysos#2Elib7MonitorM_0_9spinlockb_Rv_P1y:function

_ZN5tysos11tysos#2Elib7MonitorM_0_9spinlockb_Rv_P1y:
	xor rax, rax
	mov cl, 1

.doloop:
	lock cmpxchg byte [rdi], cl
	pause
	jnz .doloop

	ret

_ZN5tysos11tysos#2Elib7MonitorM_0_11spinunlockb_Rv_P1y:
	mov byte [rdi], 0
	ret


