TYBUILD=../../../tybuild/bin/Release/tybuild.exe
TYSILA=../../../tysila2/bin/Release/tysila2.exe
MSCORLIB=../../../mono/corlib/mscorlib.obj
LD=x86_64-elf-ld

TYSILAFLAGS=-L../../../mono/corlib -L../../../tysos/bin/Release -c
LDFLAGS=-shared

PEFILE=bin/Release/TestProcess.exe
OBJFILE=TestProcess.obj
OUTPUT=TestProcess.bin

.PHONY: clean $(PEFILE)

all: $(OUTPUT)

$(TYSILA):
	cd ../../../tysila2 && make

$(MSCORLIB):
	cd ../../../mono/corlib && make

$(OUTPUT): $(OBJFILE)
	$(LD) $(LDFLAGS) -o $(OUTPUT) $(OBJFILE)

$(OBJFILE): $(PEFILE) $(TYSILA)
	$(TYSILA) $(TYSILAFLAGS) -o $(OBJFILE) $(PEFILE)

$(PEFILE): $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /v

clean:
	rm -rf obj bin $(PEFILE) $(OBJFILE) $(OUTPUT)

