global loader
global sthrow
 
extern kmain
 
MODULEALIGN       equ     1<<0
MEMINFO           equ     1<<1
FLAGS             equ     MODULEALIGN | MEMINFO
MAGIC             equ     0x1BADB002
CHECKSUM          equ     -(MAGIC + FLAGS)
 
IA32_EFER         equ     0xC0000080
LME               equ     0x100
 
STACKSIZE         equ     0x1000
 
section .text
 
align 4
dd MAGIC
dd FLAGS
dd CHECKSUM
 
[BITS 32]
loader:
    mov         esp, stack + STACKSIZE
 
    ; set PAE in CR4
    mov         eax, cr4
    or          eax, 0x20
    mov         cr4, eax
 
    ; clear paging tables
    lea         edi, [pml4t]
    mov         ecx, 1024
    xor         eax, eax
    rep stosd
 
    lea         edi, [pdpt]
    mov         ecx, 1024
    rep stosd
 
    lea         edi, [pd]
    mov         ecx, 1024
    rep stosd
 
    ; identity map first 2 MiB
    lea         edi, [pml4t]
    lea         eax, [pdpt]
    or          eax, 3
    mov         [edi], eax
 
    lea         edi, [pdpt]
    lea         eax, [pd]
    or          eax, 3
    mov         [edi], eax
 
    mov dword   [pd], 0x83
 
    ; set up cr3 with the pml4t
    lea         eax, [pml4t]
    mov         cr3, eax
 
    ; enable long mode
    mov         ecx, IA32_EFER
    rdmsr
    or          eax, LME
    wrmsr
 
    ; enable paging and write-back cacheing
    mov         eax, cr0
    or          eax, 0x80000000
    and         eax, 0x9fffffff
    mov         cr0, eax
 
    ; load a 64 bit gdt
    lgdt        [gdt64info]
    jmp         0x8:.owngdt
 
.owngdt:
[BITS 64]
    call kmain
 
    cli
.halt:
    hlt
    jmp .halt
 
sthrow:
    hlt
    jmp sthrow
 
section .data
align 32
gdt64info:
    dw          gdt64_end - gdt64 - 1
    dd          gdt64
 
align 32
gdt64 dd        0, 0    ; null descriptor
code  db        0xff, 0xff, 0x00, 0x00, 0x00, 10011010b, 10101111b, 0x00
data  db        0xff, 0xff, 0x00, 0x00, 0x00, 10010010b, 11001111b, 0x00
gdt64_end:
 
section .bss
stack: resb STACKSIZE
pml4t: resb 0x1000
pdpt:  resb 0x1000
pd:    resb 0x1000
