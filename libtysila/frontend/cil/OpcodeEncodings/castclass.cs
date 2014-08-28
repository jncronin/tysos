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

namespace libtysila.frontend.cil.OpcodeEncodings
{
    class castclass
    {
        public static void castclassex(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            Assembler.TypeToCompile dest_ttc;
            Signature.Param dest;
            vara v_src = il.stack_vars_before.Peek();

            if (il.inline_tok is TTCToken)
                dest_ttc = ((TTCToken)il.inline_tok).ttc;
            else
                dest_ttc = Metadata.GetTTC(il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);

            dest = dest_ttc.tsig;
            if ((dest_ttc.type != null) && (dest_ttc.type.IsValueType(ass)) && (!(dest.Type is Signature.BoxedType)))
                dest = new Signature.Param(new Signature.BoxedType(dest.Type), ass);

            /* See if we can do a compile-time cast */
            if (ass.can_cast(dest.Type, il.stack_before[il.stack_before.Count - 1].Type))
            {
                il.stack_after.Pop();
                il.stack_after.Push(dest);

                vara v_ret = vara.Logical(next_variable++, dest_ttc.tsig.CliType(ass));
                il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, v_ret.DataType, v_ret.vt_type),
                    v_ret, v_src, vara.Void()));

                il.stack_vars_after.Pop();
                il.stack_vars_after.Push(v_ret);

                return;
            }

            /* Else do a runtime dynamic cast */
            Layout dest_l = Layout.GetTypeInfoLayout(dest_ttc, ass, false);
            vara v_ret2 = vara.Logical(next_variable++, dest_ttc.tsig.CliType(ass));
            il.tacs.Add(new timple.TimpleCallNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.call, v_ret2.DataType, v_ret2.vt_type),
                v_ret2, vara.Label("castclassex", false), new vara[] { v_src, vara.Label(Mangler2.MangleTypeInfo(dest_ttc, ass), true) }, ass.msig_castclassex));

            il.stack_after.Pop();
            il.stack_after.Push(dest);
            il.stack_vars_after.Pop();
            il.stack_vars_after.Push(v_ret2);

            ass.Requestor.RequestTypeInfo(dest_ttc);
        }
    }
}
