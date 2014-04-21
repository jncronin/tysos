TYBUILD=../tybuild/bin/Release/tybuild.exe

UNICODEDATA_CS=UnicodeData.cs
CHARCONVERSIONS_CS=create-category-table/char-conversions.cs

INTERMEDIATE=create-category-table/categorydump

UNICODESUPPORT_EXE=bin/Release/unicode_support.exe
CREATECATEGORYTABLE_EXE=create-category-table/create-category-table.exe

.PHONY: clean $(TYBUILD)

all: $(UNICODEDATA_CS) $(CHARCONVERSIONS_CS)

$(UNICODEDATA_CS): UnicodeData.txt $(UNICODESUPPORT_EXE)
	$(UNICODESUPPORT_EXE)

$(UNICODESUPPORT_EXE): unicode_support.cs unicode_support.sln unicode_support.csproj $(TYBUILD)
	$(TYBUILD) /p:Configuration=Release /tools:3_5 unicode_support.sln

$(CHARCONVERSIONS_CS): $(CREATECATEGORYTABLE_EXE)
	$(CREATECATEGORYTABLE_EXE) --dump $(INTERMEDIATE)
	$(CREATECATEGORYTABLE_EXE) --encode $(INTERMEDIATE) CategoryData $(CHARCONVERSIONS_CS)

$(CREATECATEGORYTABLE_EXE): create-category-table.sources create-category-table/create-category-table.cs $(TYBUILD)
	$(TYBUILD) /tools:3_5 create-category-table.sources

$(TYBUILD):
	cd ../tybuild && make

clean:
	rm -rf obj bin $(UNICODEDATA_CS) create-category-table/obj create-category-table/bin $(CHARCONVERSIONS_CS) $(INTERMEDIATE)

