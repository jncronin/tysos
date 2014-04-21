#!/bin/sh
x86_64-elf-nm ../tysos/tysos.bin | grep " T " | awk '{ print $1" "$3 }' > tysos.sym

