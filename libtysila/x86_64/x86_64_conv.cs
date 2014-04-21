/* Copyright (C) 2008 - 2011 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using libasm;

namespace libtysila
{
    partial class x86_64_Assembler
    {
        IEnumerable<OutputBlock> x86_64_conv_i4u2zx_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0x0f, 0xb7)); }

        IEnumerable<OutputBlock> x86_64_conv_i48_i2sx_dgpr_sgprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, (op == ThreeAddressCode.Op.conv_i4_i2sx) ? false : true, 0, 0x0f, 0xbf)); }

        IEnumerable<OutputBlock> x86_64_conv_i48_i1sx_dgpr_sgprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, (op == ThreeAddressCode.Op.conv_i4_i2sx) ? false : true, 0, 0x0f, 0xbe)); }

        IEnumerable<OutputBlock> x86_64_conv_i4u1zx_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, true, 0x0f, 0xb6));
        }

        IEnumerable<OutputBlock> x86_64_conv_i4i8sx_gpr_gprmem(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            if (ia == IA.i586)
            {
                if (op == ThreeAddressCode.Op.conv_i4_i8sx)
                    throw new NotSupportedException();
                return new List<OutputBlock>();
            }
            return OBList(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, (ia == IA.x86_64), 0, 0x63));
        }

        IEnumerable<OutputBlock> x86_64_conv_i4u8zx_sgprmem_dgpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* x86_64 automatically zero extends whatever is loaded into the low dword of an extended register */
            return OBList(new CodeBlock(EncOpcode(result.hardware_loc, op1.hardware_loc, 3, false, 0, 0x8b),
                new x86_64_Instruction { opcode = "mov", Operand1 = result.hardware_loc, Operand2 = op1.hardware_loc }));
        }
    }
}
