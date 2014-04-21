all: delegate.exe
	../../tysila2/bin/Release/tysila2.exe -L../../mono/corlib -c --irdump delegate.ir.txt -o delegate.obj delegate.exe
	x86_64-elf-objdump -d delegate.obj > delegate.txt

