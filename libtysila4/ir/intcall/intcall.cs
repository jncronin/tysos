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
        delegate Opcode[] intcall_delegate(Param[] defs, Param[] uses,
            cil.CilNode start, target.Target t);

        static Dictionary<string, intcall_delegate> intcalls =
            new Dictionary<string, intcall_delegate>(
                new GenericEqualityComparer<string>());

        internal static Opcode[] do_intcall(string mname,
            Param[] defs, Param[] uses,
            cil.CilNode start, target.Target t)
        {
            intcall_delegate i;
            if (intcalls.TryGetValue(mname, out i))
                return i(defs, uses, start, t);
            else
                return null;
        }

        static intcall()
        {
            intcalls["_Zu1S_9get_Chars_Rc_P2u1ti"] = str_getChars;
            intcalls["_Zu1S_10get_Length_Ri_P1u1t"] = str_getLength;
        }
    }
}
