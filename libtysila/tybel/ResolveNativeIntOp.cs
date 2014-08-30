/* Copyright (C) 2008 - 2012 by John Cronin
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

namespace libtysila
{
    partial class Assembler
    {
        protected virtual ThreeAddressCode.Op ResolveNativeIntOp(ThreeAddressCode.Op op)
        {
            Bitness bt = GetBitness();

            if (op.Operator == ThreeAddressCode.OpName.peek_u)
            {
                switch (bt)
                {
                    case Bitness.Bits32:
                        op.Operator = ThreeAddressCode.OpName.peek_u4;
                        break;
                    case Bitness.Bits64:
                        op.Operator = ThreeAddressCode.OpName.peek_u8;
                        break;
                }
            }
            else if (op.Operator == ThreeAddressCode.OpName.poke_u)
            {
                switch (bt)
                {
                    case Bitness.Bits32:
                        op.Operator = ThreeAddressCode.OpName.poke_u4;
                        break;
                    case Bitness.Bits64:
                        op.Operator = ThreeAddressCode.OpName.poke_u8;
                        break;
                }
            }

            if (op.OpType == ThreeAddressCode.OpType.ConvOp)
            {
                switch (op.Operator)
                {
                    case ThreeAddressCode.OpName.conv_i_i1sx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                op.Operator = ThreeAddressCode.OpName.conv_i4_i1sx;
                                break;
                            case Bitness.Bits64:
                                op.Operator = ThreeAddressCode.OpName.conv_i8_i1sx;
                                break;
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i_i2sx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                op.Operator = ThreeAddressCode.OpName.conv_i4_i2sx;
                                break;
                            case Bitness.Bits64:
                                op.Operator = ThreeAddressCode.OpName.conv_i8_i2sx;
                                break;
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i_i4sx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                op.Operator = ThreeAddressCode.OpName.assign;
                                break;
                            case Bitness.Bits64:
                                op.Operator = ThreeAddressCode.OpName.conv_i8_i4sx;
                                break;
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i_i8sx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                op.Operator = ThreeAddressCode.OpName.conv_i4_i8sx;
                                break;
                            case Bitness.Bits64:
                                op.Operator = ThreeAddressCode.OpName.assign;
                                break;
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i4_isx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign);
                            case Bitness.Bits64:
                                return ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i4_i8sx);
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i8_isx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i4sx);
                            case Bitness.Bits64:
                                return ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.assign);
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i4_uzx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign);
                            case Bitness.Bits64:
                                return ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i4_u8zx);
                        }
                        break;

                    case ThreeAddressCode.OpName.conv_i8_uzx:
                        switch (bt)
                        {
                            case Bitness.Bits32:
                                return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u4zx);
                            case Bitness.Bits64:
                                return ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.assign);
                        }
                        break;
                }
            }
            else if (op.Type == CliType.native_int || op.Type == CliType.O || op.Type == CliType.reference)
            {
                switch (bt)
                {
                    case Bitness.Bits32:
                        return new ThreeAddressCode.Op(op.Operator, CliType.int32, op.VT_Type);
                    case Bitness.Bits64:
                        return new ThreeAddressCode.Op(op.Operator, CliType.int64, op.VT_Type);
                }
            }
            return op;
        }
    }
}
