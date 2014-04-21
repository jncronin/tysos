TYBUILD=../../../tybuild/bin/Release/tybuild.exe
TYSILA=../../../tysila2/bin/Release/tysila2.exe
MSCORLIB=../../../mono/corlib/mscorlib.obj
LD=x86_64-elf-ld

TYSILAFLAGS=-L../../../mono/corlib -L../../../tysos/bin/Release --whole-module --exclude $(MSCORLIB_SYMS) --exclude $(TYSOS_SYMS) --exclude $(GUI_SYMS) -q
LDFLAGS=-shared

PEFILE=bin/Release/Vga.exe
OBJFILE=Vga.obj
OUTPUT=Vga.bin

MSCORLIB_SYMS=../../../mono/corlib/mscorlib_syms.txt
TYSOS_SYMS=../../../tysos/tysos_syms.txt
GUI_SYMS=../../services/Gui/gui_syms.txt

.PHONY: clean $(PEFILE) $(TYSOS_SYMS) $(GUI_SYMS)

all: $(OUTPUT)

$(TYSILA):
	cd ../../../tysila2 && make

$(MSCORLIB):
	cd ../../../mono/corlib && make

$(MSCORLIB_SYMS):
	cd ../../../mono/corlib && make mscorlib_syms.txt

$(TYSOS_SYMS):
	cd ../../../tysos && make tysos_syms.txt

$(GUI_SYMS):
	cd ../../services/Gui && make gui_syms.txt

$(OUTPUT): $(OBJFILE)
	$(LD) $(LDFLAGS) -o $(OUTPUT) $(OBJFILE)

$(OBJFILE): $(PEFILE) $(TYSILA) $(MSCORLIB_SYMS) $(TYSOS_SYMS) $(GUI_SYMS)
	$(TYSILA) $(TYSILAFLAGS) -o $(OBJFILE) $(PEFILE)

$(PEFILE): $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /v /tools:3_5

clean:
	rm -rf obj bin $(PEFILE) $(OBJFILE) $(OUTPUT)

