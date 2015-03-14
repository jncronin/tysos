AUTOMAKE_OPTIONS = foreign subdir-objects

CXXFLAGS = @CXXFLAGS@
CPPFLAGS = @CPPFLAGS@
CFLAGS = @CFLAGS@ -Wall -Wextra -pedantic -Werror -std=gnu99
LDFLAGS = @LDFLAGS@
LIBS = @LIBS@

bin_PROGRAMS = mkgpt
mkgpt_SOURCES = mkgpt.c crc32.c fstypes.c guid.c
noinst_HEADERS = fstypes.h guid.h part.h mkgpt.vcxproj README

