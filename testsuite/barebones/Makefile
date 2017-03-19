TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/tysila4/bin/Debug/tysila4.exe
YASM=yasm
GCC=i686-elf-gcc

KERNEL_OBJ=kernel.o

all: barebones.iso

kernel.exe: kernel.cs Makefile
	$(TYBUILD) /unsafe /r:libsupcs kernel.cs

kernel.o: kernel.exe $(TYSILA)
	$(TYSILA) -t x86 -e kmain -L../../mono/corlib -fno-rtti -o kernel.o kernel.exe

loader.o: loader.asm
	$(YASM) -felf -o loader.o loader.asm

kernel.bin: $(KERNEL_OBJ) loader.o linker.ld
	$(GCC) -fno-use-linker-plugin -nostdlib -Wl,-z,max-page-size=0x1000 -o kernel.bin -Wl,-T,linker.ld loader.o $(KERNEL_OBJ)

barebones.iso: kernel.bin grub.cfg grub_prefix.cfg
	mkdir -p iso/boot/grub
	cp grub.cfg iso/boot/grub
	cp kernel.bin iso
	grub-mkrescue -o myos.iso iso

clean:
	rm -rf *.o *.exe iso


