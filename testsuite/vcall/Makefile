all: vcall.exe iface.exe
	../../tysila2/bin/Release/tysila2.exe -L../../mono/corlib -c --irdump vcall.ir.txt -o vcall.obj vcall.exe
	x86_64-elf-objdump -d vcall.obj > vcall.txt
	../../tysila2/bin/Release/tysila2.exe -L../../mono/corlib -c --irdump iface.ir.txt -o iface.obj iface.exe
	x86_64-elf-objdump -d iface.obj > iface.txt

