AUTOMAKE_OPTIONS = foreign subdir-objects

CXXFLAGS = @CXXFLAGS@ -Ifatfs
CPPFLAGS = @CPPFLAGS@
LDFLAGS = @LDFLAGS@
LIBS = @LIBS@ @BOOST_SYSTEM_LIB@ @BOOST_FILESYSTEM_LIB@

bin_PROGRAMS = mkfat
mkfat_SOURCES = mkfat.cpp file.cpp fatfs/ff.cpp fatfs/diskio.cpp fatfs/unicode.cpp
noinst_HEADERS = fatfs/cc932.c fatfs/cc936.c fatfs/cc949.c fatfs/cc950.c fatfs/ccsbcs.c mkfat.h file.h fatfs/diskio.h fatfs/ff.h fatfs/ffconf.h fatfs/integer.h mkfat.vcxproj

