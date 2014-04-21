#!/bin/sh

x86_64-elf-nm tysos.obj | grep -v " U " | grep -v "<PrivateImplementationDetails>" | awk '{ print $3 }' > tysos_syms.txt

