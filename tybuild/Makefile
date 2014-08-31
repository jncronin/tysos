GCC=gcc

SOURCES=project.cs solution.cs tybuild.cs
EXTRA_SOURCES=tybuild.csproj tybuild.sln

TYBUILD=bin/Release/tybuild.exe
EXECCOMPILER=./execcompiler.exe

LIBSUPCS=../libsupcs/bin/Release/libsupcs.dll
LIBSTDCS=../libstdcs/bin/Release/libstdcs.dll

LOCAL_LIBSUPCS=bin/Release/libsupcs.dll
LOCAL_LIBSTDCS=bin/Release/libstdcs.dll

all: $(TYBUILD) $(LOCAL_LIBSUPCS) $(LOCAL_LIBSTDCS)

$(LIBSUPCS): $(TYBUILD)
	cd ../libsupcs && make bin/Release/libsupcs.dll

$(LIBSTDCS): $(TYBUILD)
	cd ../libstdcs && make bin/Release/libstdcs.dll

$(LOCAL_LIBSUPCS): $(LIBSUPCS)
	cp $(LIBSUPCS) $(LOCAL_LIBSUPCS)

$(LOCAL_LIBSTDCS): $(LIBSTDCS)
	cp $(LIBSTDCS) $(LOCAL_LIBSTDCS)

$(EXECCOMPILER): execcompiler.c
	$(GCC) -g -o $(EXECCOMPILER) -Wall -pedantic execcompiler.c

$(TYBUILD): $(SOURCES) $(EXTRA_SOURCES) $(EXECCOMPILER)
	mkdir -p bin/Release
	$(EXECCOMPILER)

.PHONY: clean

clean:
	rm -rf $(TYBUILD) obj bin


