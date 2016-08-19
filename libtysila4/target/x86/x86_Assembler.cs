/* Copyright (C) 2016 by John Cronin
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
using libtysila4.ir;

namespace libtysila4.target.x86
{
    partial class x86_Assembler : Target
    {
        static x86_Assembler()
        {
            init_instrs();
        }

        protected override void MCLower(Opcode irnode, ref int next_temp_reg)
        {
            switch(irnode.oc)
            {
                case Opcode.oc_call:
                    LowerCall(irnode);
                    return;
                case Opcode.oc_ret:
                    LowerReturn(irnode);
                    return;

            }
            base.MCLower(irnode, ref next_temp_reg);
        }

        protected internal override bool IsMove(MCInst i)
        {
            if (i.p != null && i.p.Length > 0 &&
                i.p[0].t == Opcode.vl_str &&
                i.p[0].v == x86_mov &&
                i.p[1].IsStack && i.p[2].IsStack)
                return true;
            return false;
        }

        protected internal override Param GetMoveDest(MCInst i)
        {
            if (!IsMove(i))
                throw new NotSupportedException();
            return i.p[1];
        }

        protected internal override Param GetMoveSrc(MCInst i)
        {
            if (!IsMove(i))
                throw new NotSupportedException();
            return i.p[2];
        }

        protected internal override int GetCondCode(MCInst i)
        {
            if (i.p == null || i.p.Length < 2 ||
                i.p[1].t != Opcode.vl_cc)
                return Opcode.cc_always;
            return (int)i.p[1].v;
        }

        protected internal override bool IsBranch(MCInst i)
        {
            if (i.p != null && i.p.Length > 0 &&
                i.p[0].t == Opcode.vl_str &&
                (i.p[0].v == x86_br ||
                i.p[0].v == x86_bcc))
                return true;
            return false;
        }

        protected internal override void SetBranchDest(MCInst i, Param d)
        {
            if (!IsBranch(i))
                throw new NotSupportedException();
            if (i.p[0].v == x86_bcc)
                i.p[2] = d;
            else
                i.p[1] = d;
        }

        protected internal override Param GetBranchDest(MCInst i)
        {
            if (!IsBranch(i))
                throw new NotSupportedException();
            if (i.p[0].v == x86_bcc)
                return i.p[2];
            else
                return i.p[1];
        }
    }
}
