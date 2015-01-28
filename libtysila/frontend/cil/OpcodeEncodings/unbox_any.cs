/* Copyright (C) 2015 by John Cronin
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
    class unbox_any
    {
        public static void tybel_unbox_any(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* obj -> value or obj
             * 
             * If obj is a boxed value type, extract the value contain within it and push it
             * to the stack.  Else perform castclass <typeTok>
             */

            Assembler.TypeToCompile T = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            libasm.hardware_location loc_obj = il.stack_vars_after.Pop(ass);
            Signature.Param p_obj = il.stack_after.Pop();
            Stack in_use = il.stack_vars_before.Clone();

            libasm.hardware_location t1 = ass.GetTemporary(state);
            libasm.hardware_location t2 = ass.GetTemporary2(state);
            
            Signature.Param p_ret = new Signature.Param(T.tsig.Type, ass);
            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(p_ret, ass);

            if (!(loc_obj is libasm.register))
            {
                ass.Assign(state, in_use, t1, loc_obj, Assembler.CliType.native_int, il.il.tybel);
                in_use.MarkUsed(t1);
                loc_obj = t1;
            }

            int L_is_boxed = state.next_blk++;
            int L_end = state.next_blk++;

            string sL_is_boxed = "L" + L_is_boxed.ToString();
            string sL_end = "L" + L_end.ToString();

            if (p_obj.Type is Signature.BoxedType)
            {
                ass.Br(state, in_use, new libasm.hardware_addressoflabel(sL_is_boxed, false),
                    il.il.tybel);
            }
            else
            {
                /* We have to do a runtime test to determine if this is a boxed type */

                // TODO, check the type is compatible with T

                // First, get the type info (double dereference)
                ass.Assign(state, in_use, t2,
                    new libasm.hardware_contentsof { base_loc = loc_obj },
                    Assembler.CliType.native_int, il.il.tybel);
                in_use.MarkUsed(t2);
                ass.Assign(state, in_use, t2,
                    new libasm.hardware_contentsof { base_loc = t2 },
                    Assembler.CliType.native_int, il.il.tybel);

                // Make a call to libsupcs.TysosType.IsBoxed
                string is_boxed_meth = "_ZX9TysosTypeM_0_11get_IsBoxed_Rb_P1u1t";
                CallConv cc_is_boxed = ass.MakeStaticCall("default",
                    new Signature.Param(Assembler.CliType.native_int),
                    new List<Signature.Param> { new Signature.Param(Assembler.CliType.native_int) },
                    ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.call));

                ass.Call(state, in_use,
                    new libasm.hardware_addressoflabel(is_boxed_meth, false),
                    t2, new libasm.hardware_location[] { t2 }, cc_is_boxed, il.il.tybel);

                // Jump to the is_boxed label if it is boxed (IsBoxed != 0)
                ass.BrIf(state, in_use, new libasm.hardware_addressoflabel(sL_is_boxed, false),
                    t2, new libasm.const_location { c = 0 }, ThreeAddressCode.OpName.bne,
                    Assembler.CliType.native_int, il.il.tybel);
            }

            // This is executed if the object is not a boxed type
            Layout dest_l = Layout.GetTypeInfoLayout(T, ass, false);
            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("castclassex", false), loc_ret,
                new libasm.hardware_location[] { loc_obj, new libasm.hardware_addressoflabel(Mangler2.MangleTypeInfo(T, ass), dest_l.FixedLayout[Layout.ID_VTableStructure].Offset, true) },
                ass.callconv_castclassex, il.il.tybel);
            ass.Br(state, in_use, new libasm.hardware_addressoflabel(sL_end, false), il.il.tybel);

            // This is executed if the object is a boxed type
            il.il.tybel.Add(new tybel.LabelNode(sL_is_boxed, true));

            /* Build a prototype {boxed}Int32 instance to get the offset of m_value */
            Signature.Param p_boxed = new Signature.Param(new Signature.BoxedType { _ass = ass, Type = new Signature.BaseType(BaseType_Type.I4) }, ass);
            Assembler.TypeToCompile boxed_ttc = new Assembler.TypeToCompile(p_boxed, ass);
            Layout l_boxed = Layout.GetTypeInfoLayout(boxed_ttc, ass, false);

            // Load the source address to t1, dest address to t2
            int value_offset = l_boxed.GetField("m_value", false).offset;
            int obj_size = dest_l.ClassSize;

            ass.LoadAddress(state, in_use, t1,
                new libasm.hardware_contentsof { base_loc = loc_obj, const_offset = value_offset, size = obj_size },
                il.il.tybel);
            if (loc_ret is libasm.register)
            {
                ass.Peek(state, in_use, loc_ret, t1, obj_size, il.il.tybel);
            }
            else if (loc_ret is libasm.hardware_stackloc)
            {
                ass.LoadAddress(state, in_use, t2, loc_ret, il.il.tybel);

                // Copy the data
                ass.MemCpy(state, in_use, t2, t1, new libasm.const_location { c = obj_size },
                    il.il.tybel);
            }
            else throw new NotImplementedException();

            // End of the function
            il.il.tybel.Add(new tybel.LabelNode(sL_end, true));

            il.stack_after.Push(p_ret);
        }
    }
}
