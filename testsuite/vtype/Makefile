TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe

all: vtype.o

vtype.exe: vtype.cs Makefile
	$(TYBUILD) /r:libsupcs vtype.cs

vtype.o: vtype.exe $(TYSILA)
	$(TYSILA) --arch i586-elf-tysos -L../../mono/corlib -fno-rtti -o vtype.o vtype.exe

clean:
	rm -rf *.o *.exe iso


