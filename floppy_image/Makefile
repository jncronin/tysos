TLOAD=../tload/tload.bin
KERNELBZ2=../tysos/tysos.bin.bz2
MENUCFG=menu.cfg

IMAGE=image

MCOPY=mcopy

all: $(TLOAD) $(KERNELBZ2) $(MENUCFG)
	$(MCOPY) -o -i $(IMAGE) $(TLOAD) $(KERNELBZ2) $(MENUCFG) ::/boot

clean:

	
