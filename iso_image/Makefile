TLOAD=../tload/tload.bin
KERNELBZ2=../tysos/tysos.bin.bz2
KERNEL=../tysos/tysos.bin
KERNELHASH=../tysos/tysos.bin.hash
LIBTYSILASO=../libtysila/libtysila.so

ACPI_PC=../processes/drivers/ACPI_PC/ACPI_PC.bin
GUI=../processes/services/Gui/Gui.bin
PS2K=../processes/drivers/PS2_Keyboard/PS2_Keyboard.bin
VGA=../processes/drivers/Vga/Vga.bin
CONSOLE=../processes/applications/Console/Console.bin
TYSHELL=../processes/applications/shell/shell.bin
VFS=../processes/services/vfs/vfs.bin

TEST_JIT=../testsuite/test_002/bin/Release/test_002.exe ../mono/corlib/mscorlib.dll ../libsupcs/bin/Release/libsupcs.dll

PROCESSES=$(ACPI_PC) $(GUI) $(PS2K) $(VGA) $(CONSOLE) $(TYSHELL) $(VFS)
GRUB_CFG=grub.cfg

IMAGE=tysos.iso

GRUB=grub-mkrescue

$(IMAGE): $(TLOAD) $(KERNEL) $(KERNELHASH) $(GRUB_CFG) $(TEST_JIT) tysos.sym Makefile grub_prefix.cfg
	mkdir -p iso/boot/grub
	cp $(TLOAD) $(KERNEL) $(KERNELHASH) $(TEST_JIT) iso
	cp $(GRUB_CFG) iso/boot/grub
	grub-mkimage -o core.img -c grub_prefix.cfg -O i386-pc biosdisk multiboot configfile normal iso9660
	if [ -f /usr/lib/grub/i386-pc/cdboot.img ]; then export CDBOOT=/usr/lib/grub/i386-pc/cdboot.img; elif [ -f /usr/local/lib/grub/i386-pc/cdboot.img ]; then CDBOOT=/usr/local/lib/grub/i386-pc/cdboot.img; else CDBOOT=cdboot.img; fi; echo cdboot.img is $$CDBOOT; cat $$CDBOOT core.img > iso/boot/grub/eltorito.img
	rm core.img
	xorriso -as mkisofs -R -f -b boot/grub/eltorito.img -no-emul-boot -boot-load-size 4 -boot-info-table -o tysos.iso iso

all: $(IMAGE)

tysos.sym: $(KERNEL)
	./make_tysos_sym.sh

clean:

	
