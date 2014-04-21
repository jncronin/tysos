#!/bin/sh

x86_64-elf-nm mscorlib.obj | grep -v " U " | awk '{ print $3 }' > mscorlib_syms.txt

