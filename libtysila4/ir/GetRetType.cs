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

namespace libtysila4.ir
{
    partial class Opcode
    {
        internal static int GetCTFromType(int type)
        {
            switch(type)
            {
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                case 0x08:
                case 0x09:
                    return ct_int32;

                case 0x0a:
                case 0x0b:
                    return ct_int64;

                case 0x0c:
                case 0x0d:
                    return ct_float;

                case 0x0e:
                    return ct_object;

                case 0x0f:
                    return ct_ref;

                case 0x11:
                    throw new NotImplementedException();

                case 0x12:
                case 0x14:
                case 0x16:
                    return ct_object;

                case 0x18:
                case 0x19:
                    return ct_intptr;

                case 0x1c:
                case 0x1d:
                    return ct_object;


                default:
                    throw new NotImplementedException();
            }
        }
        
        static int get_call_rettype(Opcode n)
        {
            // Determine the return type from the method signature
            var cs = n.uses[0];
            if (cs.t != Opcode.vl_call_target)
                throw new NotSupportedException();

            var msig = cs.v2;
            var rt_idx = cs.m.GetMethodDefSigRetTypeIndex((int)msig);
            uint tok;
            var rt = cs.m.GetType(ref rt_idx, out tok);
            var ct = GetCTFromType(rt);

            return ct;
        }

        static int get_store_pushtype(Opcode n)
        {
            // Determine from the type of operand 1
            var o1 = n.uses[0];

            switch(o1.t)
            {
                case vl_stack32:
                case vl_arg32:
                case vl_lv32:
                case vl_stack64:
                case vl_arg64:
                case vl_lv64:
                case vl_c32:
                case vl_c64:
                case vl_stack:
                case vl_arg:
                case vl_lv:
                case vl_c:
                    return o1.ct;
                    
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
