TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe
MSCORLIB=../../mono/corlib/mscorlib.dll
ARM_LD = arm-none-eabi-ld
ARM_AS = arm-none-eabi-as

RPI_TEST_EXE = bin/Release/rpi_test.exe

all: rpi_test.bin

$(RPI_TEST_EXE): Program.cs
	$(TYBUILD) /unsafe /p:Configuration=Release

rpi_test.bin: rpi_test.obj linker.ld loader.o
	$(ARM_LD) -Ttext 0x100000 -o rpi_test.bin loader.o rpi_test.obj

rpi_test.obj: $(RPI_TEST_EXE) $(MSCORLIB)
	cp $(MSCORLIB) mscorlib.dll
	$(TYSILA) --arch arm-elf-tysos -fno-rtti -o rpi_test.obj $(RPI_TEST_EXE)

loader.o: loader.s
	$(ARM_AS) -o loader.o loader.s

clean:
	rm -rf bin obj loader.o rpi_test.obj rpi_test.bin

