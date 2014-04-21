TYBUILD=../tybuild/bin/Release/tybuild.exe

ECMAEXTRACT=bin/Release/ecma-extract.exe
SOURCES=Program.cs Properties/AssemblyInfo.cs ecma-extract.csproj ecma-extract.sln

.PHONY: clean $(TYBUILD)

all: $(ECMAEXTRACT)

$(TYBUILD):
	cd ../tybuild && make

$(ECMAEXTRACT): $(TYBUILD) $(SOURCES)
	$(TYBUILD) /p:Configuration=Release /v

clean:
	rm -rf $(ECMAEXTRACT) obj bin 

