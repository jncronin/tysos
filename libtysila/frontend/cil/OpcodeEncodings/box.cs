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
    class box
    {
        public static void tybel_box(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_val = il.stack_after.Pop();
            libasm.hardware_location loc_val = il.stack_vars_after.Pop(ass);
            Assembler.TypeToCompile typeTok = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.Param p_T = typeTok.tsig;

            if (typeTok.type.IsNullable)
                throw new NotImplementedException("box: nullable types not yet implemented");
            else
            {
                if (typeTok.type.IsValueType(ass))
                {
                    Signature.Param p_dest = new Signature.Param(new Signature.BoxedType { _ass = ass, Type = p_T.Type }, ass);
                    libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);
                    Assembler.TypeToCompile boxed_ttc = new Assembler.TypeToCompile(typeTok.type, p_dest, ass);
                    Layout l = Layout.GetTypeInfoLayout(boxed_ttc, ass, false);

                    libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                    libasm.hardware_location t2 = ass.GetTemporary2(state, Assembler.CliType.int32);
                    ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("gcmalloc", false), t1,
                        new libasm.hardware_location[] { new libasm.const_location { c = l.ClassSize } }, ass.callconv_gcmalloc, il.il.tybel);

                    if (l.has_vtbl)
                    {
                        ass.Assign(state, il.stack_vars_before,
                            new libasm.hardware_contentsof { base_loc = t1, const_offset = l.vtbl_offset, size = ass.GetSizeOfPointer() },
                            new libasm.hardware_addressoflabel(l.typeinfo_object_name, l.FixedLayout[Layout.ID_VTableStructure].Offset, true),
                            Assembler.CliType.native_int, il.il.tybel);
                    }
                    if (l.has_obj_id)
                    {
                        Stack temp_stack = il.stack_vars_before.Clone();
                        temp_stack.MarkUsed(t1);
                        ass.Call(state, temp_stack, new libasm.hardware_addressoflabel("getobjid", false), t2, new libasm.hardware_location[] { },
                            ass.callconv_getobjid, il.il.tybel);
                        ass.Assign(state, temp_stack,
                            new libasm.hardware_contentsof { base_loc = t1, const_offset = l.obj_id_offset, size = 4 },
                            t2, Assembler.CliType.int32, il.il.tybel);
                    }

                    int val_offset = l.GetField("m_value", false).offset;
                    int size = ass.GetSizeOf(p_T);
                    Assembler.CliType ct = p_T.CliType(ass);

                    if (ct == Assembler.CliType.vt && size > ass.GetSizeOfPointer())
                    {
                        t2 = ass.GetTemporary2(state, Assembler.CliType.native_int);
                        ass.LoadAddress(state, il.stack_vars_before, t2, loc_val, il.il.tybel);
                        ass.Assign(state, il.stack_vars_before,
                            new libasm.hardware_contentsof { base_loc = t1, const_offset = val_offset, size = ass.GetSizeOf(p_T) },
                            new libasm.hardware_contentsof { base_loc = t2, size = ass.GetSizeOf(p_T) }, p_T.CliType(ass), il.il.tybel);
                    }
                    else
                    {
                        ass.Assign(state, il.stack_vars_before,
                            new libasm.hardware_contentsof { base_loc = t1, const_offset = val_offset, size = ass.GetSizeOf(p_T) },
                            loc_val, p_T.CliType(ass), il.il.tybel);
                    }

                    ass.Assign(state, il.stack_vars_before, loc_dest, t1, Assembler.CliType.native_int, il.il.tybel);
                    il.stack_after.Push(p_dest);
                }
                else
                {
                    libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_T, ass);
                    ass.Assign(state, il.stack_vars_before, loc_dest, loc_val, p_T.CliType(ass), il.il.tybel);
                    il.stack_after.Push(p_T);
                }
            }
        }
    }
}
