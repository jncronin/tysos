.assembly Test { }

.method static public int32 main() cil managed
{
        .entrypoint

        ldc.i4          0
    L1: ldc.i4          1
        add
        dup
        ldc.i4          10
        ble             L1
        ret
}


