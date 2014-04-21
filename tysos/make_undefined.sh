#!/bin/sh

cat _undefined_top.asm > undefined.asm
awk '{ printf ("weak %s\n", $0) }' unimplemented.txt >> undefined.asm
echo "" >> undefined.asm
awk '{ printf ("%s:\n", $0) }' unimplemented.txt >> undefined.asm
cat _undefined.asm >> undefined.asm

