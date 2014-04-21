all:
	cd tybuild && make all
	cd tysila2 && make all
	cd libsupcs && make all
	cd libstdcs && make all
	cd tinygc && make all
	cd tysos && make bin/Release/tysos.exe
	cd mono && make all
	cd tysos && make all
	cd tload && make all
	cd testsuite/test_002 && make all
	cd iso_image && make all

.PHONY: clean

clean:
	cd tybuild && make clean
	cd tysila2 && make clean
	cd mono && make clean
	cd tysos && make clean
	cd processes && make clean
	cd tload && make clean
	cd testsuite/test_002 && make clean
	cd iso_image && make clean
	cd libtysila && make clean
	cd libsupcs && make clean
	cd libstdcs && make clean
	cd libgc && make clean
	cd elfhash && make clean

dist: clean
	cd .. && tar czf tysos.tar.gz --exclude-vcs tysos

