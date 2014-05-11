OUTPUT = tloadefi.bin

PEKERNEL=../../tysos/bin/Release/tysos.exe

EFILIBC=efilibc/efilibc.a
EFIWINDOW=efiwindow/efiwindow.a
ZLIB=zlib/libz.a
LIBPNG=libpng-1.6.9/libpng.a
LIBM=msun/libm.a

TLOADKIF=tloadkif.h
TLOADKIFC=tloadkif.c

CC = x86_64-w64-mingw32-gcc
AR = x86_64-w64-mingw32-ar
MKFAT=../mkfat/mkfat
TYSILA=../../tysila2/bin/Release/tysila2.exe
YASM=yasm

CFLAGS = -I. -Iefilibc/inc -Iefiwindow/inc -I../efi/inc -I../efi/inc/x86_64 -I../efi/inc/protocol -Iconfuse-2.7/src -Ilibpng-1.6.9 -Izlib -nostartfiles -Wall -Wextra -Wno-long-long -g -D_DEBUG -ffreestanding -mabi=ms -std=gnu11 -DHAVE_LIBPNG 
YASMFLAGS = -f win64
TYSILAFLAGS = -L ../../mono/corlib

OBJS = main.o elf.o vmem.o confuse-2.7/src/confuse.o confuse-2.7/src/lexer.o $(TLOADKIFC)

all: efi.iso

%.o: %.c $(DEPS) Makefile $(TLOADKIFC) libpng-1.6.9/pnglibconf.h
	$(CC) -c -o $@ $< $(CFLAGS)
	
%.o: %.asm Makefile
	$(YASM) $(YASMFLAGS) -o $@ $<

libpng-1.6.9/pnglibconf.h: $(LIBPNG)

$(EFILIBC):
	cd efilibc && make

$(EFIWINDOW):
	cd efiwindow && WITH_LIBPNG=../libpng-1.6.9 make

$(ZLIB):
	cd zlib && make libz.a

$(LIBPNG):
	cd libpng-1.6.9 && make libpng.a

$(LIBM):
	cd msun && make

$(TLOADKIFC): $(TLOADKIF)

$(TLOADKIF): $(TYSILA) $(PEKERNEL)
	$(TYSILA) $(TYSILAFLAGS) --output-header $(TLOADKIF) --output-cinit $(TLOADKIFC) $(PEKERNEL)

$(OUTPUT): $(OBJS) $(TLOADKIFC) $(EFILIBC) $(EFIWINDOW) $(ZLIB) $(LIBPNG) $(LIBM)
	$(CC) -fno-use-linker-plugin -nostdlib -Wl,-dll -shared -Wl,--subsystem,10 -e efi_main -o $@ $(OBJS) $(EFIWINDOW) $(LIBPNG) $(ZLIB) $(LIBM) $(EFILIBC)

efi.img: $(OUTPUT) $(MKFAT) boot.mnu tysos.png
	mkdir -p img/EFI/BOOT
	cp $(OUTPUT) img/EFI/BOOT/BOOTX64.EFI
	mkdir -p img/boot
	cp boot.mnu img/boot
	cp tysos.png img/boot
	cp tysos2.png img/boot
	cp tysos3.png img/boot
	cp unifont.psf img/boot
	cp -R modules img/EFI
	cp cursor-icon.png img/EFI/cursor.png
	$(MKFAT) -o efi.img img

efi.iso: efi.img
	mkdir -p iso
	cp efi.img iso
	xorriso -as mkisofs -R -f --efi-boot efi.img -no-emul-boot -o efi.iso iso

.PHONY: clean $(LIBPNG) $(ZLIB) $(EFILIBC) $(EFIWINDOW)

clean:
	rm -f $(OBJS) $(OUTPUT) $(TLOADKIF) $(TLOADKIFC)
	cd efilibc && make clean
	cd efiwindow && make clean
	cd zlib && make clean
	cd libpng-1.6.9 && make clean

