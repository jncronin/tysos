#!/bin/sh

x86_64-elf-nm ../libtysila/libtysila.obj | grep -v " U " | grep -v "<PrivateImplementationDetails>" | awk '{ print $3 }' > libtysila_syms.txt

