_ZX14CastOperationsM_0_9GetArg0U8_Ry_P0:
_halt:
mov rdi, [rsp]
call __undefined_func
call __display_halt
.haltloop:
xchg bx, bx
hlt
jmp .haltloop

