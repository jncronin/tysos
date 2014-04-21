GCC=gcc

SOURCES=project.cs solution.cs tybuild.cs
EXTRA_SOURCES=tybuild.csproj tybuild.sln

TYBUILD=bin/Release/tybuild.exe
EXECCOMPILER=./execcompiler.exe

all: $(TYBUILD)

$(EXECCOMPILER): execcompiler.c
	$(GCC) -g -o $(EXECCOMPILER) -Wall -pedantic execcompiler.c

$(TYBUILD): $(SOURCES) $(EXTRA_SOURCES) $(EXECCOMPILER)
	mkdir -p bin/Release
	$(EXECCOMPILER)

.PHONY: clean

clean:
	rm -rf $(TYBUILD) obj bin


