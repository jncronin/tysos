TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe

all: static_fields.o

static_fields.exe: static_fields.cs Makefile
	$(TYBUILD) /r:libsupcs static_fields.cs

static_fields.o: static_fields.exe $(TYSILA)
	$(TYSILA) --arch i586-elf-tysos -L../../mono/corlib -fno-rtti -o static_fields.o static_fields.exe

clean:
	rm -rf *.o *.exe iso


