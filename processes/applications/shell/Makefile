TYBUILD=../../../tybuild/bin/Release/tybuild.exe
TYSILA=../../../tysila2/bin/Release/tysila2.exe
MSCORLIB=../../../mono/corlib/mscorlib.obj
LD=x86_64-elf-ld

TYSILAFLAGS=-L../../../mono/corlib -L../../../tysos/bin/Release --whole-module --exclude $(MSCORLIB_SYMS) -q
LDFLAGS=-shared

APP_NAME=shell

PEFILE=bin/Release/$(APP_NAME).exe
OBJFILE=$(APP_NAME).obj
OUTPUT=$(APP_NAME).bin

MSCORLIB_SYMS=../../../mono/corlib/mscorlib_syms.txt

.PHONY: clean $(PEFILE)

all: $(OUTPUT)

$(TYSILA):
	cd ../../../tysila2 && make

$(MSCORLIB):
	cd ../../../mono/corlib && make

$(MSCORLIB_SYMS):
	cd ../../../mono/corlib && make mscorlib_syms.txt

$(OUTPUT): $(OBJFILE)
	$(LD) $(LDFLAGS) -o $(OUTPUT) $(OBJFILE)

$(OBJFILE): $(PEFILE) $(TYSILA) $(MSCORLIB_SYMS)
	$(TYSILA) $(TYSILAFLAGS) -o $(OBJFILE) $(PEFILE)

$(PEFILE): $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /v /tools:3_5

clean:
	rm -rf obj bin $(PEFILE) $(OBJFILE) $(OUTPUT)

