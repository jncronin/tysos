TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe
YASM=yasm
GCC=x86_64-elf-gcc

all: barebones.iso

kernel.exe: kernel.cs Makefile
	$(TYBUILD) /unsafe /r:libsupcs kernel.cs

kernel.o: kernel.exe
	$(TYSILA) --arch x86_64s-elf64-tysos --rename-epoint kmain -L../../mono/corlib -fno-rtti -o kernel.o kernel.exe

loader.o: loader.asm
	$(YASM) -felf64 -o loader.o loader.asm

kernel.bin: kernel.o loader.o linker.ld
	$(GCC) -fno-use-linker-plugin -nostdlib -Wl,-z,max-page-size=0x1000 -o kernel.bin -Wl,-T,linker.ld loader.o kernel.o

barebones.iso: kernel.bin grub.cfg grub_prefix.cfg
	mkdir -p iso/boot/grub
	cp grub.cfg iso/boot/grub
	cp kernel.bin iso
	grub-mkimage -o core.img -c grub_prefix.cfg -O i386-pc biosdisk multiboot configfile normal iso9660
	cat /usr/local/lib/grub/i386-pc/cdboot.img core.img > iso/boot/grub/eltorito.img
	xorriso -as mkisofs -R -f -b boot/grub/eltorito.img -no-emul-boot -boot-load-size 4 -boot-info-table -o barebones.iso iso
	rm core.img

clean:
	rm -rf *.o *.exe iso


