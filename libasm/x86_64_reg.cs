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
    public class x86_64_gpr : register, IEquatable<hardware_location>
    {
        public enum RegId
        {
            rax = 0, rcx = 1, rdx = 2, rbx = 3, rsp = 4, rbp = 5, rsi = 6, rdi = 7,
            r8 = 8, r9 = 9, r10 = 10, r11 = 11, r12 = 12, r13 = 13, r14 = 14, r15 = 15
        };

        public RegId reg;

        public override string ToString()
        {
            if (bits32)
                return reg.ToString().Replace('r', 'e');
            else
                return reg.ToString();
        }

        public bool is_extended { get { if (((int)reg) >= 8) return true; return false; } }
        public byte base_val { get { return (byte)(((int)reg) % 8); } }
        public bool bits32 = false;

        public override bool Equals(hardware_location other)
        {
            if (!(other is x86_64_gpr))
                return false;
            if (this.reg != ((x86_64_gpr)other).reg)
                return false;
            if (this.bits32 != ((x86_64_gpr)other).bits32)
                return false;
            return true;
        }

        public override var_semantic GetSemantic()
        {
            return new var_semantic { needs_int32 = true, needs_int64 = true, needs_intptr = true };
        }

        public override int GetHashCode()
        {
            return (reg.GetHashCode() << 16) ^ bits32.GetHashCode();
        }
    }

    public class x86_64_xmm : register, IEquatable<hardware_location>
    {
        public enum XmmId
        {
            xmm0 = 0, xmm1 = 1, xmm2 = 2, xmm3 = 3, xmm4 = 4, xmm5 = 5, xmm6 = 6, xmm7 = 7,
            xmm8 = 8, xmm9 = 9, xmm10 = 10, xmm11 = 11, xmm12 = 12, xmm13 = 13, xmm14 = 14, xmm15 = 15
        };

        public XmmId xmm;

        public override string ToString()
        {
            return xmm.ToString();
        }

        public bool is_extended { get { if (((int)xmm) >= 8) return true; return false; } }
        public int base_val { get { return ((int)xmm) % 8; } }

        public override bool Equals(hardware_location other)
        {
            if (!(other is x86_64_xmm))
                return false;
            if (this.xmm == ((x86_64_xmm)other).xmm)
                return true;
            return false;
        }

        public override var_semantic GetSemantic()
        {
            return new var_semantic { needs_float32 = true, needs_float64 = true };
        }
    }
}
