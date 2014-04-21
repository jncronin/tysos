TYBUILD=../../tybuild/bin/Release/tybuild.exe

PEFILE=bin/Release/test_002.exe

.PHONY: clean $(PEFILE)

all: $(PEFILE)

$(PEFILE): $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /v /tools:3_5

clean:
	rm -rf obj bin $(PEFILE)

