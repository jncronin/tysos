global _ZN5tysos11tysos#2Elib7MonitorM_0_11spinunlockb_Rv_P1y:function
global _ZN5tysos11tysos#2Elib7MonitorM_0_9spinlockb_Rv_P1y:function

_ZN5tysos11tysos#2Elib7MonitorM_0_9spinlockb_Rv_P1y:
	xor rax, rax
	mov rbx, [rsp + 8]
	mov cl, 1

.doloop:
	lock cmpxchg byte [rbx], cl
	pause
	jnz .doloop

	ret

_ZN5tysos11tysos#2Elib7MonitorM_0_11spinunlockb_Rv_P1y:
	mov rbx, [rsp + 8]
	mov byte [rbx], 0
	ret


