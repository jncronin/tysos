all: idle.bin

.PHONY: clean

idle.o: idle.asm
	yasm -f elf64 -o idle.o idle.asm

idle.bin: idle.o
	x86_64-elf-ld -pie -o idle.bin idle.o

clean:
	rm -f idle.o idle.bin

