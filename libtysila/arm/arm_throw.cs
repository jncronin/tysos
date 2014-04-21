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
using libasm;

namespace libtysila.arm
{
    partial class arm_Assembler
    {
        IEnumerable<OutputBlock> arm_throw(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();

            string target = "throw";
            CallConv cc;
            if (op1.type == var.var_type.Const)
            {
                target = "sthrow";
                cc = callconv_sthrow;
            }
            else
                cc = callconv_throw;

            hardware_location methinfo = state.methinfo_pointer;
            if (methinfo == null)
                methinfo = new const_location { c = 0 };

            cond c = cond.Always;
            switch (op)
            {
                case ThreeAddressCode.Op.throw_ovf:
                    c = cond.Overflow;
                    break;
                case ThreeAddressCode.Op.throw_ovf_un:
                    c = cond.CarrySet;
                    break;
                case ThreeAddressCode.Op.throweq:
                    c = cond.Equal;
                    break;
                case ThreeAddressCode.Op.throwg_un:
                    c = cond.UnsignedHigher;
                    break;
                case ThreeAddressCode.Op.throwge_un:
                    c = cond.CarrySet;
                    break;
                case ThreeAddressCode.Op.throwne:
                    c = cond.NotEqual;
                    break;
            }

            // Do a call if cond to throw(op1.hardware_location, methinfo)
            
            // First assign to the registers: r0 = op1.hardware_loc, r1 = methinfo (or others depending on calling convention)
            // If they are the other way round, we need to swap them
            hardware_location r0 = cc.Arguments[0].ValueLocation;
            hardware_location r1 = cc.Arguments[1].ValueLocation;
            if (op1.hardware_loc.Equals(r0) && methinfo.Equals(r1))
            {
                // op1 is in R1 and methinfo is in R0
                // assign op1 to a scratch register, then methinfo to R1, then scratch to r0
                arm_assign(R12, op1.hardware_loc, ret, state, c);
                arm_assign(r1, methinfo, ret, state, c);
                arm_assign(r0, R12, ret, state, c);
            }
            else if (op1.hardware_loc.Equals(r1))
            {
                // op1 is in R1 and methinfo isn't in R0
                // assign op1 to R0 first
                arm_assign(r0, op1.hardware_loc, ret, state, c);
                arm_assign(r1, methinfo, ret, state, c);
            }
            else
            {
                // op1 isnt in R1
                // assign methinfo to R1 first
                arm_assign(r1, methinfo, ret, state, c);
                arm_assign(r0, op1.hardware_loc, ret, state, c);
            }

            // Now do the call
            ret.Add(new RelocationBlock { RelType = 29, Target = target, Size = 3, Value = -2 });       // R_ARM_JUMP24 (required for BLc as per ARM ELF doc), addend shifted by 2
            ret.Add(new CodeBlock { Code = new byte[] { (byte)(((uint)c << 4) | 0xb) }});                // BLc

            return ret;
        }
    }
}
