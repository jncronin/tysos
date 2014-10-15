/* Copyright (C) 2014 by John Cronin
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
using libtysila.frontend.cil;

namespace libtysila.x86_64.cil
{
    class loc
    {
        public static void tybel_ldloc(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int p = -1;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldloc_0:
                    p = 0;
                    break;
                case Opcode.SingleOpcodes.ldloc_1:
                    p = 1;
                    break;
                case Opcode.SingleOpcodes.ldloc_2:
                    p = 2;
                    break;
                case Opcode.SingleOpcodes.ldloc_3:
                    p = 3;
                    break;
                case Opcode.SingleOpcodes.ldloc_s:
                    p = (int)il.il.inline_uint;
                    break;
                case Opcode.SingleOpcodes.double_:
                    switch (il.il.opcode.opcode2)
                    {
                        case Opcode.DoubleOpcodes.ldloc:
                            p = (int)il.il.inline_uint;
                            break;
                    }
                    break;
            }
            if(p == -1)
                throw new Exception("Unimplemented ldloc opcode: " + il.ToString());

            libasm.hardware_location src = state.lv_locs[p];
            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(state.lvs[p], ass);

            ass.Assign(state, il.stack_vars_before, dest, src, state.lvs[p].CliType(ass), il.il.tybel);

            il.stack_after.Push(state.lvs[p]);
        }

        public static void tybel_ldloca(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int p = (int)il.il.inline_uint;

            libasm.hardware_location dest = il.stack_vars_after.GetAddressFor(new Signature.Param(Assembler.CliType.native_int), ass);
            libasm.hardware_location src = state.lv_locs[p];

            x86_64_Assembler.EncLea(ass as x86_64_Assembler, state, dest, src, il.il.tybel);

            il.stack_after.Push(new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = state.lvs[p].Type }, ass));
        }

        public static void tybel_stloc(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int p = -1;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.stloc_0:
                    p = 0;
                    break;
                case Opcode.SingleOpcodes.stloc_1:
                    p = 1;
                    break;
                case Opcode.SingleOpcodes.stloc_2:
                    p = 2;
                    break;
                case Opcode.SingleOpcodes.stloc_3:
                    p = 3;
                    break;
                case Opcode.SingleOpcodes.stloc_s:
                    p = (int)il.il.inline_uint;
                    break;
                case Opcode.SingleOpcodes.double_:
                    switch (il.il.opcode.opcode2)
                    {
                        case Opcode.DoubleOpcodes.stloc:
                            p = (int)il.il.inline_uint;
                            break;
                    }
                    break;
            }
            if(p == -1)
                throw new Exception("Unimplemented stloc opcode: " + il.ToString());

            il.stack_after.Pop();
            libasm.hardware_location src = il.stack_vars_after.Pop(ass);
            libasm.hardware_location dest = state.lv_locs[p];

            ass.Assign(state, il.stack_vars_before, dest, src, state.lvs[p].CliType(ass), il.il.tybel);
        }
    }
}
