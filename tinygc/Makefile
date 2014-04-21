CFLAGS += -ffreestanding -I. -g -O2
X8664CFLAGS += -mno-red-zone -mno-sse -mno-sse2 -mno-mmx -mno-3dnow -mno-sse3
I386CFLAGS += -mno-sse -mno-sse2 -mno-mmx -mno-3dnow -mno-sse3
LDFLAGS += -fno-use-linker-plugin

ARMCC = arm-none-eabi-gcc
X8664CC = x86_64-elf-gcc
I386CC = i586-elf-gcc

ARMOBJ = gc.arm.o
X8664OBJ = gc.x86_64.o
I386OBJ = gc.i386.o

all: $(ARMOBJ) $(X8664OBJ) $(I386OBJ)

$(ARMOBJ): dlmalloc.arm.o tinygc.arm.o setjmp.arm.o stdlib.arm.o sbrk.arm.o printf.arm.o
	$(ARMCC) -Wl,-r $(LDFLAGS) -nostdlib -o $@ $^

$(X8664OBJ): dlmalloc.x86_64.o tinygc.x86_64.o setjmp.x86_64.o stdlib.x86_64.o sbrk.x86_64.o printf.x86_64.o
	$(X8664CC) -Wl,-r $(LDFLAGS) -nostdlib -o $@ $^

$(I386OBJ): dlmalloc.i386.o tinygc.i386.o setjmp.i386.o stdlib.i386.o sbrk.i386.o printf.i386.o
	$(I386CC) -Wl,-r $(LDFLAGS) -nostdlib -o $@ $^

%.arm.o: %.arm.s Makefile
	$(ARMCC) $(CFLAGS) -c $< -o $@

%.arm.o: %.c Makefile
	$(ARMCC) $(CFLAGS) -c $< -o $@


%.x86_64.o: %.x86_64.s Makefile
	$(X8664CC) $(CFLAGS) -c $< -o $@

%.x86_64.o: %.c Makefile
	$(X8664CC) $(CFLAGS) $(X8664CFLAGS) -c $< -o $@


%.i386.o: %.i386.s Makefile
	$(I386CC) $(CFLAGS) -c $< -o $@

%.i386.o: %.c Makefile
	$(I386CC) $(CFLAGS) $(I386CFLAGS) -c $< -o $@

.PHONY: clean

clean:
	$(RM) -f *.o

