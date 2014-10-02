TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe
TEST=test_003

all: $(TEST).o

$(TEST).exe: $(TEST).cs Makefile
	$(TYBUILD) /r:libsupcs $(TEST).cs

$(TEST).o: $(TEST).exe $(TYSILA)
	$(TYSILA) --arch i586-elf-tysos -L../../mono/corlib -fno-rtti -o $(TEST).o $(TEST).exe

clean:
	rm -rf *.o *.exe iso


