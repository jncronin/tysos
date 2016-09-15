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
using System.Text;
using libtysila4.cil;

namespace libtysila4.ir
{
    partial class IrGraph
    {
        static Opcode[] binnumops(CilNode start, target.Target t)
        {
            int oc = 0;

            switch(start.opcode.opcode1)
            {
                case cil.Opcode.SingleOpcodes.add:
                    oc = Opcode.oc_add;
                    break;
                case cil.Opcode.SingleOpcodes.sub:
                    oc = Opcode.oc_sub;
                    break;
            }

            Opcode r = new Opcode
            {
                oc = oc,
                uses = new Param[] { new Param { t = Opcode.vl_stack, v = 1 }, new Param { t = Opcode.vl_stack, v = 0 } },
                defs = new Param[] { new Param { t = Opcode.vl_stack, v = 0 } }
            };

            return new Opcode[] { r };
        }
    }
}
