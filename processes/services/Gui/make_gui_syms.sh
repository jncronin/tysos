#!/bin/sh

x86_64-elf-nm Gui.obj | grep -v " U " | grep -v "<PrivateImplementationDetails>" | awk '{ print $3 }' > gui_syms.txt

