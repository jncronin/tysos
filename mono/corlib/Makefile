TYBUILD=../../tybuild/bin/Release/tybuild.exe
TYSILA=../../tysila2/bin/Release/tysila2.exe

PECORLIB=mscorlib.dll
CORLIBOBJ=mscorlib.obj
MSCORLIBSYMS=mscorlib_syms.txt
ORIGSOURCES=corlib.dll.sources
SOURCESOPTIONS=corlib.dll.sources.options
SOURCELIST=corlib.dll.ty.sources

.PHONY: clean

all: $(MSCORLIBSYMS)

$(TYBUILD):
	cd ../../tybuild && make

$(TYSILA2):
	cd ../../tysila2 && make

$(SOURCELIST): $(ORIGSOURCES) $(SOURCESOPTIONS)
	cat $(SOURCESOPTIONS) > $(SOURCELIST)
	echo '#assemblyname $(PECORLIB)' >> $(SOURCELIST)
	echo '' >> $(SOURCELIST)
	cat $(ORIGSOURCES) >> $(SOURCELIST)

$(PECORLIB): $(SOURCELIST) $(TYBUILD)
	$(TYBUILD) /t $(SOURCELIST)

$(CORLIBOBJ): $(PECORLIB)
	$(TYSILA) -o $(CORLIBOBJ) -q -c --unimplemented mscorlib-unimplemented.txt $(PECORLIB)

$(MSCORLIBSYMS): $(CORLIBOBJ) make_mscorlib_syms.sh
	./make_mscorlib_syms.sh

clean:
	rm -rf $(PECORLIB) $(SOURCELIST) $(MSCORLIBSYMS)

