OUTPUT = efiwindow.a

EFI_CC ?= x86_64-w64-mingw32-gcc
EFI_AR ?= x86_64-w64-mingw32-ar
EFILIBC ?= ../efilibc

CFLAGS = -Iinc -I$(EFILIBC)/inc -I$(EFILIBC)/efi -I$(EFILIBC)/efi/inc -I$(EFILIBC)/efi/inc/x86_64 -I$(EFILIBC)/efi/inc/protocol -Wall -Wextra -Wno-long-long -g -ffreestanding -std=c11 -DIN_EW_LIB -O3

ifneq ($(strip $(WITH_LIBPNG)),)
	PNG_CFLAGS = -DHAVE_LIBPNG -I$(WITH_LIBPNG)
	CFLAGS += $(PNG_CFLAGS)
endif

OBJS = efiwindow.o screen.o windowlist.o bitmap.o psffont.o ewrect.o textbox.o progress.o ewmessage.o image.o mouse.o

all: $(OUTPUT)

%.o: %.c Makefile
	$(EFI_CC) -c -o $@ $< $(CFLAGS)
	
$(OUTPUT): $(OBJS)
	$(EFI_AR) rcs $(OUTPUT) $(OBJS)

.PHONY: clean

clean:
	rm -f *.o $(OUTPUT)

