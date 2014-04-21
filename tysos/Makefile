MSBUILD=/cygdrive/c/Windows/Microsoft.NET/Framework/v3.5/msbuild.exe
TYBUILD=../tybuild/bin/Release/tybuild.exe
TYSILA=../tysila2/bin/Release/tysila2.exe
ELFHASH=../elfhash/bin/Release/elfhash.exe
PEKERNEL=bin/Release/tysos.exe
MSCORLIB=../mono/corlib/mscorlib.obj
LIBTYSILA=../libtysila/libtysila.obj
LIBSUPCS=../libsupcs/libsupcs.a
LIBSTDCS=../libstdcs/libstdcs.a
LD=x86_64-elf-ld
BZIP2=bzip2

UNIMPL_LABELS=../mono/corlib/mscorlib-unimplemented.txt
MSCORLIB_SYMS=../mono/corlib/mscorlib_syms.txt
TYSOS_SYMS=tysos_syms.txt
LIBTYSILA_SYMS=libtysila_syms.txt
GUI_SYMS=gui_syms.txt
UNICODEDATA_CS=lib/UnicodeData.cs

KERNELOBJ=tysos.obj
KERNEL=tysos.bin
KERNELBZ2=tysos.bin.bz2

BOEHM_ARM=../libgc/libgc-arm-none-eabi.a
BOEHM_i586=../libgc/libgc-i586-pc-tysos.a
BOEHM_x86_64=../libgc/libgc-x86_64-elf.a

TINYGC_ARM=../tinygc/gc.arm.o
TINYGC_i586=../tinygc/gc.i586.o
TINYGC_x86_64=../tinygc/gc.x86_64.o

GC_ARM=$(TINYGC_ARM)
GC_i586=$(TINYGC_i586)
GC_x86_64=$(TINYGC_x86_64)

;LDFLAGS=-pie
LDFLAGS=-Ttext 0x40000000

LDADD=
BZIP2FLAGS=-kf
TYSILAFLAGS=-L../mono/corlib -q -d --whole-module --irdump tysos.ir.txt -g --include extra_objects.txt
#TYSILAFLAGS=-L../mono/corlib -q -d --extra-type _m8_mscorlib_N6_System_T6_Object_S2_1C --extra-method _m8_mscorlib_N6_System_T6_String_S2_0E_M10_get_Length_S6_200008 --extra-method _m8_mscorlib_N6_System_T6_Object_S2_1C_M5_.ctor_S6_200001 --irdump tysos.ir.txt

.PHONY: clean $(BOEHM_ARM) $(BOEHM_i586) $(BOEHM_x86_64)

all: $(KERNEL)

$(TYSILA):
	cd ../tysila2 && make

$(ELFHASH):
	cd ../elfhash && make

$(MSCORLIB):
	cd ../mono/corlib && make

$(LIBTYSILA):
	cd ../libtysila && make

$(LIBSUPCS):
	cd ../libsupcs && make

$(LIBSTDCS):
	cd ../libstdcs && make

$(MSCORLIB_SYMS): $(MSCORLIB)
	cd ../mono/corlib && make

$(TYSOS_SYMS): $(PEKERNEL)
	./make_tysos_syms.sh

$(LIBTYSILA_SYMS): $(LIBTYSILA)
	./make_libtysila_syms.sh

$(GUI_SYMS):
	./make_gui_syms.sh

$(KERNEL): $(KERNELOBJ) halt.o cpu.o undefined.o x86_64/switcher.o x86_64/exceptions.o $(MSCORLIB) $(LIBTYSILA) $(LIBSUPCS) $(LIBSTDCS) $(GC_x86_64) $(ELFHASH)
	$(LD) $(LDFLAGS) -o $(KERNEL) $(KERNELOBJ) halt.o cpu.o undefined.o x86_64/switcher.o x86_64/exceptions.o $(LIBTYSILA) $(MSCORLIB) $(LIBSTDCS) $(LIBSUPCS) $(GC_x86_64) $(LDADD)
	x86_64-elf-objdump -d $(KERNEL) > ../tysos.txt
	x86_64-elf-objdump -d $(KERNELOBJ) > ../tysos.obj.txt
	$(ELFHASH) -v1 -32 $(KERNEL)

halt.o: halt.asm
	yasm -f elf64 -o halt.o halt.asm

cpu.o: cpu.asm
	yasm -f elf64 -o cpu.o cpu.asm

x86_64/switcher.o: x86_64/switcher.asm
	yasm -f elf64 -o x86_64/switcher.o x86_64/switcher.asm

x86_64/exceptions.o: x86_64/exceptions.asm
	yasm -f elf64 -o x86_64/exceptions.o x86_64/exceptions.asm

undefined.asm: $(UNIMPL_LABELS)
	cp $(UNIMPL_LABELS) ./unimplemented.txt
	./make_undefined.sh

undefined.o: undefined.asm
	yasm -f elf64 -o undefined.o undefined.asm

$(KERNELOBJ): $(TYSILA) $(PEKERNEL) $(MSCORLIB_SYMS) $(LIBTYSILA_SYMS)
	$(TYSILA) $(TYSILAFLAGS) -o $(KERNELOBJ) --exclude $(MSCORLIB_SYMS) --exclude $(LIBTYSILA_SYMS) $(PEKERNEL)

$(PEKERNEL): $(TYBUILD) $(UNICODEDATA_CS)
	$(TYBUILD) /p:Configuration=Release /v /unsafe /tools:3_5 /Wc,warn:0

$(UNICODEDATA_CS):
	cd ../unicode_support && make && cd ../tysos
	cp ../unicode_support/UnicodeData.cs lib
	cp ../unicode_support/create-category-table/char-conversions.cs lib

clean:
	rm -rf obj bin $(PEKERNEL) $(KERNELOBJ) $(KERNEL) $(KERNELBZ2)

$(BOEHM_ARM):
	cd ../libgc && make libgc-arm-none-eabi.a

$(BOEHM_i586):
	cd ../libgc && make libgc-i586-pc-tysos.a

$(BOEHM_x86_64):
	cd ../libgc && make libgc-x86_64-elf.a

$(TINYGC_ARM):
	cd ../tinygc && make $@

$(TINYGC_i586):
	cd ../tinygc && make $@

$(TINYGC_x86_64):
	cd ../tinygc && make $@

