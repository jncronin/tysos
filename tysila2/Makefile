MSBUILD=/cygdrive/c/Windows/Microsoft.NET/Framework/v3.5/msbuild.exe
TYBUILD=../tybuild/bin/Release/tybuild.exe

.PHONY: clean

all:
	$(TYBUILD) /p:Configuration=Release /v /project:tysila2 /tools:3_5 /debug /Wc,warn:0

clean:
	rm -rf obj bin

