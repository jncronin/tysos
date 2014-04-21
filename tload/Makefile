IDIR = .
ODIR = obj
OUTPUT = tload.bin

BZIP=bzip2-1.0.5
LIBBZ2=libbz2.a
PEKERNEL=../tysos/bin/Release/tysos.exe

TLOADKIF=tloadkif.h
TLOADKIFC=tloadkif.c

CC = i586-elf-gcc
AR = i586-elf-ar
YASM = yasm
TYSILA=../tysila2/bin/Release/tysila2.exe

CFLAGS = -I$(IDIR) -nostartfiles -Wall -Wextra -Wno-long-long -pedantic -g -DI386 -DKERNEL -D_DEBUG -ffreestanding
YASMFLAGS = -f elf -g stabs
TYSILAFLAGS = -L ../mono/corlib

MCOPY = mcopy
IMAGE = image

_DEPS = console.h mboot.h memmgr.h elf.h stdlib.h cmdline.h tloadkif.h malloc.h vmem.h
DEPS = $(patsubst %,$(IDIR)/%,$(_DEPS))

_OBJ = bootstrap.o main.o console.o memmgr.o stdlib.o cmdline.o elf.o vmem.o go64.o kif.o malloc.o decompress.o tloadkif.o
OBJ = $(patsubst %,$(ODIR)/%,$(_OBJ))

all: $(OUTPUT)

$(ODIR)/%.o: %.c $(DEPS)
	$(CC) -c -o $@ $< $(CFLAGS)
	
$(ODIR)/%.o: %.asm $(DEPS)
	$(YASM) $(YASMFLAGS) -o $@ $<

$(BZIP)/$(LIBBZ2): $(BZIP)/Makefile
	cd $(BZIP) && CC=$(CC) $(MAKE) $(LIBBZ2)

$(TLOADKIFC): $(TLOADKIF)

$(TLOADKIF): $(TYSILA) $(PEKERNEL)
	$(TYSILA) $(TYSILAFLAGS) --output-header $(TLOADKIF) --output-cinit $(TLOADKIFC) $(PEKERNEL)

$(OUTPUT): $(OBJ) linker.ld $(BZIP)/$(LIBBZ2) $(TLOADKIF)
	$(CC) -Wl,-Map,"tload.txt","-Tlinker.ld" -fno-use-linker-plugin -ffreestanding -nostdlib -o $@ $(OBJ) $(BZIP)/$(LIBBZ2)

.PHONY: clean
.PHONY: $(BZIP)/$(LIBBZ2)
.PHONY: $(TLOADKIF) $(TLOADKIFC)

clean:
	rm -f $(ODIR)/*.o
	rm -f $(OUTPUT)
	$(MAKE) -C $(BZIP) clean

