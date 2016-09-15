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
        static Opcode[] stind(CilNode start, target.Target t)
        {
            int dest_size = 0;

            switch(start.opcode.opcode1)
            {
                case cil.Opcode.SingleOpcodes.stind_i:
                case cil.Opcode.SingleOpcodes.stind_ref:
                    if (t.ptype == Opcode.ct_int32)
                        dest_size = 4;
                    else
                        dest_size = 8;
                    break;
                case cil.Opcode.SingleOpcodes.stind_i1:
                    dest_size = 1;
                    break;
                case cil.Opcode.SingleOpcodes.stind_i2:
                    dest_size = 2;
                    break;
                case cil.Opcode.SingleOpcodes.stind_i4:
                    dest_size = 4;
                    break;
                case cil.Opcode.SingleOpcodes.stind_i8:
                    dest_size = 8;
                    break;
                case cil.Opcode.SingleOpcodes.stind_r4:
                    dest_size = 14;
                    break;
                case cil.Opcode.SingleOpcodes.stind_r8:
                    dest_size = 18;
                    break;
                default:
                    throw new NotSupportedException();
            }

            Opcode r = new Opcode
            {
                oc = Opcode.oc_stind,
                uses = new Param[] {
                    new Param { t = Opcode.vl_stack, v = 1 },
                    new Param { t = Opcode.vl_stack, v = 0 },
                    new Param { t = Opcode.vl_c32, v = dest_size },
                },
            };

            return new Opcode[] { r };
        }
    }
}
