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
            switch (GetBitness())
            {
                case Bitness.Bits32:
                    switch (op)
                    {
                        case ThreeAddressCode.Op.assign_i:
                            return ThreeAddressCode.Op.assign_i4;
                        case ThreeAddressCode.Op.call_i:
                            return ThreeAddressCode.Op.call_i4;
                    }
                    break;

                case Bitness.Bits64:
                    switch (op)
                    {
                        case ThreeAddressCode.Op.assign_i:
                            return ThreeAddressCode.Op.assign_i8;
                        case ThreeAddressCode.Op.call_i:
                            return ThreeAddressCode.Op.call_i8;
                    }
                    break;
            }

            return op;
        }

    }
}
