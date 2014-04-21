#include "c:/bochs-gdb-win32/bochsrc.bxrc"

ata0-master: type=cdrom, path=tysos.iso, status=inserted
boot: cdrom
debug: action=ignore
info: action=report
error: action=report
panic: action=report
#magic_break: enabled=1
gdbstub: enabled=1

