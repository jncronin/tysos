TYBUILD=../tybuild/bin/Release/tybuild.exe

ELFHASH_EXE=bin/Release/elfhash.exe

.PHONY: clean $(TYBUILD)

all: $(ELFHASH_EXE)

$(ELFHASH_EXE): Hash.cs HashReader.cs Program.cs elfhash.csproj $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /tools:3_5 elfhash.csproj

$(TYBUILD):
	cd ../tybuild && make

clean:
	rm -rf obj bin

