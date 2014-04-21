TYBUILD=../tybuild/bin/Release/tybuild.exe
TYSILA=../tysila2/bin/Release/tysila2.exe

AR=x86_64-elf-ar
RANLIB=x86_64-elf-ranlib

ARM_AR=arm-none-eabi-ar
ARM_RANLIB=arm-none-eabi-ranlib

PELIBSTDCS=bin/Release/libstdcs.dll
LIBSTDCSOBJ=libstdcs.obj
LIBSTDCSA=libstdcs.a

ARM_LIBSTDCSOBJ = libstdcs.arm.obj
ARM_LIBSTDCSA = libstdcs.arm.a

MSCORLIBDLL=../mono/corlib/mscorlib.dll

TYSILAFLAGS += -q -c -g -L../mono/corlib
TYBUILDFLAGS += /p:Configuration=Release /v /unsafe /tools:3_5 /Wc,warn:0

.PHONY: clean

all: $(LIBSTDCSA)

$(TYBUILD):
	cd ../tybuild && make

$(TYSILA2):
	cd ../tysila2 && make

$(MSCORLIBDLL):
	cd ../mono/corlib && make mscorlib.dll

$(PELIBSTDCS): $(TYBUILD)
	$(TYBUILD) $(TYBUILDFLAGS)

$(LIBSTDCSOBJ): $(PELIBSTDCS) $(TYSILA) $(MSCORLIBDLL)
	$(TYSILA) $(TYSILAFLAGS) -o $(LIBSTDCSOBJ) $(PELIBSTDCS)

$(ARM_LIBSTDCSOBJ): $(PELIBSTDCS) $(TYSILA) $(MSCORLIBDLL)
	$(TYSILA) $(TYSILAFLAGS) --arch arm-elf-tysos -o $(ARM_LIBSTDCSOBJ) $(PELIBSTDCS)

$(LIBSTDCSA): $(LIBSTDCSOBJ)
	$(AR) -cru $(LIBSTDCSA) $(LIBSTDCSOBJ)
	$(RANLIB) $(LIBSTDCSA)

$(ARM_LIBSTDCSA): $(ARM_LIBSTDCSOBJ)
	$(ARM_AR) -cru $(ARM_LIBSTDCSA) $(ARM_LIBSTDCSOBJ)
	$(ARM_RANLIB) $(ARM_LIBSTDCSA)

clean:
	rm -rf obj bin $(PELIBSTDCS) $(LIBSTDCSOBJ) $(LIBSTDCSA)

