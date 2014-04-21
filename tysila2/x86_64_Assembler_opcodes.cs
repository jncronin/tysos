using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tysila
{
    partial class x86_64_Assembler
    {
        protected override Instruction GetInstruction(bool double_opcode, int opcode)
        {
            if (double_opcode)
                return double_opcodes[opcode];
            else
                return single_opcodes[opcode];
        }

        Instruction[] single_opcodes = new Instruction[] {
            new Instruction { func = null, parameters = null },                  // 0x00
            new Instruction { func = null, parameters = null },                  // 0x01
            new Instruction { func = null, parameters = null },                  // 0x02
            new Instruction { func = null, parameters = null },                  // 0x03
            new Instruction { func = null, parameters = null },                  // 0x04
            new Instruction { func = null, parameters = null },                  // 0x05
            new Instruction { func = null, parameters = null },                  // 0x06
            new Instruction { func = null, parameters = null },                  // 0x07
            new Instruction { func = null, parameters = null },                  // 0x08
            new Instruction { func = null, parameters = null },                  // 0x09
            new Instruction { func = null, parameters = null },                  // 0x0a
            new Instruction { func = null, parameters = null },                  // 0x0b
            new Instruction { func = null, parameters = null },                  // 0x0c
            new Instruction { func = null, parameters = null },                  // 0x0d
            new Instruction { func = null, parameters = null },                  // 0x0e
            new Instruction { func = null, parameters = null },                  // 0x0f

            new Instruction { func = null, parameters = null },                  // 0x10
            new Instruction { func = null, parameters = null },                  // 0x11
            new Instruction { func = null, parameters = null },                  // 0x12
            new Instruction { func = null, parameters = null },                  // 0x13
            new Instruction { func = null, parameters = null },                  // 0x14
            new Instruction { func = null, parameters = null },                  // 0x15
            new Instruction { func = null, parameters = null },                  // 0x16
            new Instruction { func = null, parameters = null },                  // 0x17
            new Instruction { func = null, parameters = null },                  // 0x18
            new Instruction { func = null, parameters = null },                  // 0x19
            new Instruction { func = null, parameters = null },                  // 0x1a
            new Instruction { func = null, parameters = null },                  // 0x1b
            new Instruction { func = null, parameters = null },                  // 0x1c
            new Instruction { func = null, parameters = null },                  // 0x1d
            new Instruction { func = null, parameters = null },                  // 0x1e
            new Instruction { func = null, parameters = null },                  // 0x1f

            new Instruction { func = null, parameters = null },                  // 0x20
            new Instruction { func = null, parameters = null },                  // 0x21
            new Instruction { func = null, parameters = null },                  // 0x22
            new Instruction { func = null, parameters = null },                  // 0x23
            new Instruction { func = null, parameters = null },                  // 0x24
            new Instruction { func = null, parameters = null },                  // 0x25
            new Instruction { func = null, parameters = null },                  // 0x26
            new Instruction { func = null, parameters = null },                  // 0x27
            new Instruction { func = null, parameters = null },                  // 0x28
            new Instruction { func = null, parameters = null },                  // 0x29
            new Instruction { func = null, parameters = null },                  // 0x2a
            new Instruction { func = null, parameters = null },                  // 0x2b
            new Instruction { func = null, parameters = null },                  // 0x2c
            new Instruction { func = null, parameters = null },                  // 0x2d
            new Instruction { func = null, parameters = null },                  // 0x2e
            new Instruction { func = null, parameters = null },                  // 0x2f

            new Instruction { func = null, parameters = null },                  // 0x30
            new Instruction { func = null, parameters = null },                  // 0x31
            new Instruction { func = null, parameters = null },                  // 0x32
            new Instruction { func = null, parameters = null },                  // 0x33
            new Instruction { func = null, parameters = null },                  // 0x34
            new Instruction { func = null, parameters = null },                  // 0x35
            new Instruction { func = null, parameters = null },                  // 0x36
            new Instruction { func = null, parameters = null },                  // 0x37
            new Instruction { func = null, parameters = null },                  // 0x38
            new Instruction { func = null, parameters = null },                  // 0x39
            new Instruction { func = null, parameters = null },                  // 0x3a
            new Instruction { func = null, parameters = null },                  // 0x3b
            new Instruction { func = null, parameters = null },                  // 0x3c
            new Instruction { func = null, parameters = null },                  // 0x3d
            new Instruction { func = null, parameters = null },                  // 0x3e
            new Instruction { func = null, parameters = null },                  // 0x3f

            new Instruction { func = null, parameters = null },                  // 0x40
            new Instruction { func = null, parameters = null },                  // 0x41
            new Instruction { func = null, parameters = null },                  // 0x42
            new Instruction { func = null, parameters = null },                  // 0x43
            new Instruction { func = null, parameters = null },                  // 0x44
            new Instruction { func = null, parameters = null },                  // 0x45
            new Instruction { func = null, parameters = null },                  // 0x46
            new Instruction { func = null, parameters = null },                  // 0x47
            new Instruction { func = null, parameters = null },                  // 0x48
            new Instruction { func = null, parameters = null },                  // 0x49
            new Instruction { func = null, parameters = null },                  // 0x4a
            new Instruction { func = null, parameters = null },                  // 0x4b
            new Instruction { func = null, parameters = null },                  // 0x4c
            new Instruction { func = null, parameters = null },                  // 0x4d
            new Instruction { func = null, parameters = null },                  // 0x4e
            new Instruction { func = null, parameters = null },                  // 0x4f

            new Instruction { func = null, parameters = null },                  // 0x50
            new Instruction { func = null, parameters = null },                  // 0x51
            new Instruction { func = null, parameters = null },                  // 0x52
            new Instruction { func = null, parameters = null },                  // 0x53
            new Instruction { func = null, parameters = null },                  // 0x54
            new Instruction { func = null, parameters = null },                  // 0x55
            new Instruction { func = null, parameters = null },                  // 0x56
            new Instruction { func = null, parameters = null },                  // 0x57
            new Instruction { func = null, parameters = null },                  // 0x58
            new Instruction { func = null, parameters = null },                  // 0x59
            new Instruction { func = null, parameters = null },                  // 0x5a
            new Instruction { func = null, parameters = null },                  // 0x5b
            new Instruction { func = null, parameters = null },                  // 0x5c
            new Instruction { func = null, parameters = null },                  // 0x5d
            new Instruction { func = null, parameters = null },                  // 0x5e
            new Instruction { func = null, parameters = null },                  // 0x5f

            new Instruction { func = null, parameters = null },                  // 0x60
            new Instruction { func = null, parameters = null },                  // 0x61
            new Instruction { func = null, parameters = null },                  // 0x62
            new Instruction { func = null, parameters = null },                  // 0x63
            new Instruction { func = null, parameters = null },                  // 0x64
            new Instruction { func = null, parameters = null },                  // 0x65
            new Instruction { func = null, parameters = null },                  // 0x66
            new Instruction { func = null, parameters = null },                  // 0x67
            new Instruction { func = null, parameters = null },                  // 0x68
            new Instruction { func = null, parameters = null },                  // 0x69
            new Instruction { func = null, parameters = null },                  // 0x6a
            new Instruction { func = null, parameters = null },                  // 0x6b
            new Instruction { func = null, parameters = null },                  // 0x6c
            new Instruction { func = null, parameters = null },                  // 0x6d
            new Instruction { func = null, parameters = null },                  // 0x6e
            new Instruction { func = null, parameters = null },                  // 0x6f

            new Instruction { func = null, parameters = null },                  // 0x70
            new Instruction { func = null, parameters = null },                  // 0x71
            new Instruction { func = null, parameters = null },                  // 0x72
            new Instruction { func = null, parameters = null },                  // 0x73
            new Instruction { func = null, parameters = null },                  // 0x74
            new Instruction { func = null, parameters = null },                  // 0x75
            new Instruction { func = null, parameters = null },                  // 0x76
            new Instruction { func = null, parameters = null },                  // 0x77
            new Instruction { func = null, parameters = null },                  // 0x78
            new Instruction { func = null, parameters = null },                  // 0x79
            new Instruction { func = null, parameters = null },                  // 0x7a
            new Instruction { func = null, parameters = null },                  // 0x7b
            new Instruction { func = null, parameters = null },                  // 0x7c
            new Instruction { func = null, parameters = null },                  // 0x7d
            new Instruction { func = null, parameters = null },                  // 0x7e
            new Instruction { func = null, parameters = null },                  // 0x7f

            new Instruction { func = null, parameters = null },                  // 0x80
            new Instruction { func = null, parameters = null },                  // 0x81
            new Instruction { func = null, parameters = null },                  // 0x82
            new Instruction { func = null, parameters = null },                  // 0x83
            new Instruction { func = null, parameters = null },                  // 0x84
            new Instruction { func = null, parameters = null },                  // 0x85
            new Instruction { func = null, parameters = null },                  // 0x86
            new Instruction { func = null, parameters = null },                  // 0x87
            new Instruction { func = null, parameters = null },                  // 0x88
            new Instruction { func = null, parameters = null },                  // 0x89
            new Instruction { func = null, parameters = null },                  // 0x8a
            new Instruction { func = null, parameters = null },                  // 0x8b
            new Instruction { func = null, parameters = null },                  // 0x8c
            new Instruction { func = null, parameters = null },                  // 0x8d
            new Instruction { func = null, parameters = null },                  // 0x8e
            new Instruction { func = null, parameters = null },                  // 0x8f

            new Instruction { func = null, parameters = null },                  // 0x90
            new Instruction { func = null, parameters = null },                  // 0x91
            new Instruction { func = null, parameters = null },                  // 0x92
            new Instruction { func = null, parameters = null },                  // 0x93
            new Instruction { func = null, parameters = null },                  // 0x94
            new Instruction { func = null, parameters = null },                  // 0x95
            new Instruction { func = null, parameters = null },                  // 0x96
            new Instruction { func = null, parameters = null },                  // 0x97
            new Instruction { func = null, parameters = null },                  // 0x98
            new Instruction { func = null, parameters = null },                  // 0x99
            new Instruction { func = null, parameters = null },                  // 0x9a
            new Instruction { func = null, parameters = null },                  // 0x9b
            new Instruction { func = null, parameters = null },                  // 0x9c
            new Instruction { func = null, parameters = null },                  // 0x9d
            new Instruction { func = null, parameters = null },                  // 0x9e
            new Instruction { func = null, parameters = null },                  // 0x9f

            new Instruction { func = null, parameters = null },                  // 0xa0
            new Instruction { func = null, parameters = null },                  // 0xa1
            new Instruction { func = null, parameters = null },                  // 0xa2
            new Instruction { func = null, parameters = null },                  // 0xa3
            new Instruction { func = null, parameters = null },                  // 0xa4
            new Instruction { func = null, parameters = null },                  // 0xa5
            new Instruction { func = null, parameters = null },                  // 0xa6
            new Instruction { func = null, parameters = null },                  // 0xa7
            new Instruction { func = null, parameters = null },                  // 0xa8
            new Instruction { func = null, parameters = null },                  // 0xa9
            new Instruction { func = null, parameters = null },                  // 0xaa
            new Instruction { func = null, parameters = null },                  // 0xab
            new Instruction { func = null, parameters = null },                  // 0xac
            new Instruction { func = null, parameters = null },                  // 0xad
            new Instruction { func = null, parameters = null },                  // 0xae
            new Instruction { func = null, parameters = null },                  // 0xaf

            new Instruction { func = null, parameters = null },                  // 0xb0
            new Instruction { func = null, parameters = null },                  // 0xb1
            new Instruction { func = null, parameters = null },                  // 0xb2
            new Instruction { func = null, parameters = null },                  // 0xb3
            new Instruction { func = null, parameters = null },                  // 0xb4
            new Instruction { func = null, parameters = null },                  // 0xb5
            new Instruction { func = null, parameters = null },                  // 0xb6
            new Instruction { func = null, parameters = null },                  // 0xb7
            new Instruction { func = null, parameters = null },                  // 0xb8
            new Instruction { func = null, parameters = null },                  // 0xb9
            new Instruction { func = null, parameters = null },                  // 0xba
            new Instruction { func = null, parameters = null },                  // 0xbb
            new Instruction { func = null, parameters = null },                  // 0xbc
            new Instruction { func = null, parameters = null },                  // 0xbd
            new Instruction { func = null, parameters = null },                  // 0xbe
            new Instruction { func = null, parameters = null },                  // 0xbf

            new Instruction { func = null, parameters = null },                  // 0xc0
            new Instruction { func = null, parameters = null },                  // 0xc1
            new Instruction { func = null, parameters = null },                  // 0xc2
            new Instruction { func = null, parameters = null },                  // 0xc3
            new Instruction { func = null, parameters = null },                  // 0xc4
            new Instruction { func = null, parameters = null },                  // 0xc5
            new Instruction { func = null, parameters = null },                  // 0xc6
            new Instruction { func = null, parameters = null },                  // 0xc7
            new Instruction { func = null, parameters = null },                  // 0xc8
            new Instruction { func = null, parameters = null },                  // 0xc9
            new Instruction { func = null, parameters = null },                  // 0xca
            new Instruction { func = null, parameters = null },                  // 0xcb
            new Instruction { func = null, parameters = null },                  // 0xcc
            new Instruction { func = null, parameters = null },                  // 0xcd
            new Instruction { func = null, parameters = null },                  // 0xce
            new Instruction { func = null, parameters = null },                  // 0xcf

            new Instruction { func = null, parameters = null },                  // 0xd0
            new Instruction { func = null, parameters = null },                  // 0xd1
            new Instruction { func = null, parameters = null },                  // 0xd2
            new Instruction { func = null, parameters = null },                  // 0xd3
            new Instruction { func = null, parameters = null },                  // 0xd4
            new Instruction { func = null, parameters = null },                  // 0xd5
            new Instruction { func = null, parameters = null },                  // 0xd6
            new Instruction { func = null, parameters = null },                  // 0xd7
            new Instruction { func = null, parameters = null },                  // 0xd8
            new Instruction { func = null, parameters = null },                  // 0xd9
            new Instruction { func = null, parameters = null },                  // 0xda
            new Instruction { func = null, parameters = null },                  // 0xdb
            new Instruction { func = null, parameters = null },                  // 0xdc
            new Instruction { func = null, parameters = null },                  // 0xdd
            new Instruction { func = null, parameters = null },                  // 0xde
            new Instruction { func = null, parameters = null },                  // 0xdf

            new Instruction { func = null, parameters = null },                  // 0xe0
            new Instruction { func = null, parameters = null },                  // 0xe1
            new Instruction { func = null, parameters = null },                  // 0xe2
            new Instruction { func = null, parameters = null },                  // 0xe3
            new Instruction { func = null, parameters = null },                  // 0xe4
            new Instruction { func = null, parameters = null },                  // 0xe5
            new Instruction { func = null, parameters = null },                  // 0xe6
            new Instruction { func = null, parameters = null },                  // 0xe7
            new Instruction { func = null, parameters = null },                  // 0xe8
            new Instruction { func = null, parameters = null },                  // 0xe9
            new Instruction { func = null, parameters = null },                  // 0xea
            new Instruction { func = null, parameters = null },                  // 0xeb
            new Instruction { func = null, parameters = null },                  // 0xec
            new Instruction { func = null, parameters = null },                  // 0xed
            new Instruction { func = null, parameters = null },                  // 0xee
            new Instruction { func = null, parameters = null },                  // 0xef
        };

        Instruction[] double_opcodes = new Instruction[] {
            new Instruction { func = null, parameters = null },                  // 0xfe 00
            new Instruction { func = null, parameters = null },                  // 0xfe 01
            new Instruction { func = null, parameters = null },                  // 0xfe 02
            new Instruction { func = null, parameters = null },                  // 0xfe 03
            new Instruction { func = null, parameters = null },                  // 0xfe 04
            new Instruction { func = null, parameters = null },                  // 0xfe 05
            new Instruction { func = null, parameters = null },                  // 0xfe 06
            new Instruction { func = null, parameters = null },                  // 0xfe 07
            new Instruction { func = null, parameters = null },                  // 0xfe 08
            new Instruction { func = null, parameters = null },                  // 0xfe 09
            new Instruction { func = null, parameters = null },                  // 0xfe 0a
            new Instruction { func = null, parameters = null },                  // 0xfe 0b
            new Instruction { func = null, parameters = null },                  // 0xfe 0c
            new Instruction { func = null, parameters = null },                  // 0xfe 0d
            new Instruction { func = null, parameters = null },                  // 0xfe 0e
            new Instruction { func = null, parameters = null },                  // 0xfe 0f

            new Instruction { func = null, parameters = null },                  // 0xfe 10
            new Instruction { func = null, parameters = null },                  // 0xfe 11
            new Instruction { func = null, parameters = null },                  // 0xfe 12
            new Instruction { func = null, parameters = null },                  // 0xfe 13
            new Instruction { func = null, parameters = null },                  // 0xfe 14
            new Instruction { func = null, parameters = null },                  // 0xfe 15
            new Instruction { func = null, parameters = null },                  // 0xfe 16
            new Instruction { func = null, parameters = null },                  // 0xfe 17
            new Instruction { func = null, parameters = null },                  // 0xfe 18
            new Instruction { func = null, parameters = null },                  // 0xfe 19
            new Instruction { func = null, parameters = null },                  // 0xfe 1a
            new Instruction { func = null, parameters = null },                  // 0xfe 1b
            new Instruction { func = null, parameters = null },                  // 0xfe 1c
            new Instruction { func = null, parameters = null },                  // 0xfe 1d
            new Instruction { func = null, parameters = null },                  // 0xfe 1e
            new Instruction { func = null, parameters = null },                  // 0xfe 1f
        };
    }
}
