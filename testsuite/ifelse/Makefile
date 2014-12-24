TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe

all: ifelse.o

ifelse.exe: ifelse.cs Makefile
	$(TYBUILD) /r:libsupcs ifelse.cs

ifelse.o: ifelse.exe $(TYSILA)
	$(TYSILA) --arch i586-elf-tysos -L../../mono/corlib -fno-rtti -o ifelse.o ifelse.exe

clean:
	rm -rf *.o *.exe iso


