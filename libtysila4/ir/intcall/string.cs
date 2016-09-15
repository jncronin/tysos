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

namespace libtysila4.ir.intcall
{
    partial class intcall
    {
        static Opcode[] str_getLength(Param[] defs, Param[] uses,
            cil.CilNode start, target.Target t)
        {
            /* String fields are:
                intptr vtable
                int length
                then the string */

            int len_offset = t.st.length_offset;

            /* Encode as:
                def <- ldind string, offset
            */

            var res = defs[0];
            var str = uses[1];

            res.ct = Opcode.ct_int32;

            var offset = new Param
            {
                t = Opcode.vl_c32,
                ct = Opcode.ct_int32,
                v = len_offset
            };

            return new Opcode[]
            {
                new Opcode { oc = Opcode.oc_ldind, defs = defs, uses = new Param[] { str, offset } }
            };
        }

        static Opcode[] str_getChars(Param[] defs, Param[] uses,
            cil.CilNode start, target.Target t)
        {
            /* String fields are:
                intptr vtable
                int length
                then the string */

            int str_offset = t.st.string_obj_len;

            /* Encode as:
                def <- ldind string, idx, 2, str_offset
            */

            var g = start.n.g;
            var res = defs[0];

            res.ct = Opcode.ct_int32;

            var poffset = new Param
            {
                ct = Opcode.ct_intptr,
                t = Opcode.vl_c32,
                v = str_offset
            };
            var number2 = new Param
            {
                ct = Opcode.ct_intptr,
                t = Opcode.vl_c32,
                v = 2
            };

            Opcode[] ret = new Opcode[]
            {
                new Opcode { oc = Opcode.oc_ldind, defs = new Param[] { res }, uses = new Param[] { uses[2], uses[1], number2, poffset } }
            };
            return ret;
        }
    }
}
