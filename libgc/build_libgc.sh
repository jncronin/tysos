#!/bin/sh

CC=$ARCH-gcc
AR=$ARCH-ar
AS=$ARCH-as
RANLIB=$ARCH-ranlib

make clean

cd libatomic_ops/src
$CC -I../.. -ffreestanding -c -o atomic_ops.o atomic_ops.c
$CC -I../.. -ffreestanding -c -o atomic_ops_malloc.o atomic_ops_malloc.c
$CC -I../.. -ffreestanding -c -o atomic_ops_stack.o atomic_ops_stack.c
$CC -I../.. -ffreestanding -c -o atomic_ops_sysdeps.o atomic_ops_sysdeps.S
$AR cru libatomic_ops.a atomic_ops.o atomic_ops_malloc.o atomic_ops_stack.o atomic_ops_sysdeps.o
cd ../..
mkdir -p libatomic_ops-install/lib
cp libatomic_ops/src/libatomic_ops.a libatomic_ops-install/lib

CC=$CC AS=$AS CFLAGS='-DTYSOS -ffreestanding -DNO_CLOCK -DSMALL_CONFIG -I.' make gc.a
$RANLIB gc.a
cp gc.a libgc-$ARCH.a

