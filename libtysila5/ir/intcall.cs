/* Copyright (C) 2017 by John Cronin
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
using System.Text;
using libtysila5.cil;
using metadata;
using libtysila5.util;

namespace libtysila5.ir
{
    public partial class ConvertToIR
    {
        delegate Stack<StackItem> intcall_delegate(CilNode n, Code c, Stack<StackItem> stack_before);
        static System.Collections.Generic.Dictionary<string, intcall_delegate> intcalls =
            new System.Collections.Generic.Dictionary<string, intcall_delegate>(
                new GenericEqualityComparer<string>());

        static void init_intcalls()
        {
            intcalls["_Zu1S_9get_Chars_Rc_P2u1ti"] = string_getChars;
            intcalls["_Zu1S_10get_Length_Ri_P1u1t"] = string_getLength;
        }

        static Stack<StackItem> string_getChars(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var char_offset = layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Start_Char, c);

            var stack_after = ldc(n, c, stack_before, 2);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.mul, Opcode.ct_int32);
            stack_after = conv(n, c, stack_after, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldc(n, c, stack_after, char_offset, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemChar);

            return stack_after;
        }

        private static Stack<StackItem> push_constant(CilNode n, Code c, Stack<StackItem> stack_before, int v, int ct = Opcode.ct_intptr)
        {
            Stack<StackItem> stack_after = new Stack<StackItem>(stack_before);
            StackItem si = new StackItem { _ct = ct, min_l = v, max_l = v };
            stack_after.Push(si);
            n.irnodes.Add(new CilNode.IRNode { parent = n, opcode = Opcode.oc_ldc, ctret = Opcode.ct_intptr, imm_l = v, stack_before = stack_before, stack_after = stack_after });
            return stack_after;
        }

        static Stack<StackItem> string_getLength(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var length_offset = layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Length, c);

            var stack_after = ldc(n, c, stack_before, length_offset, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemInt32);

            return stack_after;
        }
    }
}
