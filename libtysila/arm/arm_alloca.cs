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
        IEnumerable<OutputBlock> arm_alloca(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            /* The basic idea here is:
             * 
             * sub SP, op1
             * and SP, 0xfffffff0
             * mov dest, SP
             *
             * But we can't encode 0xfffffff0 as a constant in arm, therefore do:
             * mvn SCRATCH, 0xf     <- SCRATCH = !0xf = 0xfffffff0
             * sub SP, op1
             * and SP, SCRATCH
             * mov dest, SP
             */

            var sp = new var { hardware_loc = SP };
            var scratch = new var { hardware_loc = SCRATCH };
            List<OutputBlock> ret = new List<OutputBlock>();
            ret.AddRange(arm_not_i4_gpr_gpr_imm(ThreeAddressCode.Op.not_i4, scratch, var.Const(0xf), var.Null, null, state));
            ret.AddRange(arm_sub_i4_gpr_gpr(ThreeAddressCode.Op.sub_i4, sp, sp, op1, null, state));
            ret.AddRange(arm_and_i4_gpr_gpr(ThreeAddressCode.Op.and_i4, sp, sp, scratch, null, state));
            ret.AddRange(arm_assign_i4_gpr_gpr(ThreeAddressCode.Op.assign_i4, result, sp, var.Null, null, state));
            return ret;
        }
    }
}
