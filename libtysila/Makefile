TYBUILD=../tybuild/bin/Release/tybuild.exe
TYSILA=../tysila2/bin/Release/tysila2.exe
LD=x86_64-elf-ld

PELIBTYSILA=bin/Release/libtysila.dll
LIBTYSILAOBJ=libtysila.obj

MSCORLIB=../mono/corlib/mscorlib.obj
PEKERNEL=../tysos/bin/Release/tysos.exe

MSCORLIB_SYMS=../mono/corlib/mscorlib_syms.txt
TYSOS_SYMS=../tysos/tysos_syms.txt

TYSILAFLAGS=-L../mono/corlib -q -d --whole-module -g
LDFLAGS=-shared

.PHONY: clean

all: $(LIBTYSILAOBJ)

$(TYBUILD):
	cd ../tybuild && make

$(TYSILA2):
	cd ../tysila2 && make

$(MSCORLIB_SYMS): $(MSCORLIB)
	cd ../mono/corlib && ./make_mscorlib_syms.sh

$(TYSOS_SYMS): $(PEKERNEL)
	cd ../tysos && ./make_tysos_syms.sh

$(PELIBTYSILA): $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /v /unsafe /tools:3_5 /Wc,warn:0

$(LIBTYSILAOBJ): $(PELIBTYSILA) $(MSCORLIB_SYMS) $(TYSILA)
	$(TYSILA) $(TYSILAFLAGS) -o $(LIBTYSILAOBJ) --exclude $(MSCORLIB_SYMS) $(PELIBTYSILA)

clean:
	rm -rf obj bin $(PELIBTYSILA) $(LIBTYSILAOBJ) $(LIBTYSILASO)

