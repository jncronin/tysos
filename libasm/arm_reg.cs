/* Copyright (C) 2013 by John Cronin
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

namespace libasm
{
    public class arm_gpr : register, IEquatable<hardware_location>
    {
        public enum RegId
        {
            r0 = 0, r1 = 1, r2 = 2, r3 = 3, r4 = 4, r5 = 5,
            r6 = 6, r7 = 7, r8 = 8, r9 = 9, r10 = 10, r11 = 11, r12 = 12,
            sp = 13, lr = 14, pc = 15
        };

        public RegId reg;

        public override string ToString()
        {
            return reg.ToString();
        }

        public override bool Equals(hardware_location other)
        {
            if (!(other is arm_gpr))
                return false;
            return this.reg == ((arm_gpr)other).reg;
        }

        public override int GetHashCode()
        {
            return reg.GetHashCode();
        }

        public override var_semantic GetSemantic()
        {
            return new var_semantic { needs_int32 = true, needs_intptr = true };
        }
    }
}
