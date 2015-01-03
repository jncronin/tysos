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
    partial class initrth
    {
        public static void tybel_initrth(CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* Can be used to initialise one of RuntimeTypeHandle, RuntimeMethodHandle or RuntimeFieldHandle */
            string ti_name;
            int offset = 0;
            string sym_name;

            switch (il.il.opcode.opcode2)
            {
                case Opcode.DoubleOpcodes.init_rfh:
                    {
                        Assembler.FieldToCompile tok_ftc = Metadata.GetFTC(new Metadata.TableIndex(il.il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);
                        sym_name = Mangler2.MangleFieldInfoSymbol(tok_ftc, ass);
                        ti_name = Mangler2.MangleTypeInfo(tok_ftc.DefinedIn, ass);
                        Layout l2 = Layout.GetTypeInfoLayout(tok_ftc.DefinedIn, ass, false);
                        offset = l2.Symbols[sym_name];
                    }
                    break;

                case Opcode.DoubleOpcodes.init_rmh:
                    {
                        Assembler.MethodToCompile tok_mtc = Metadata.GetMTC(new Metadata.TableIndex(il.il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);
                        sym_name = Mangler2.MangleMethodInfoSymbol(tok_mtc, ass);
                        ti_name = Mangler2.MangleTypeInfo(tok_mtc.GetTTC(ass), ass);
                        Layout l2 = Layout.GetTypeInfoLayout(tok_mtc.GetTTC(ass), ass, false);
                        offset = l2.Symbols[sym_name];
                    }
                    break;

                case Opcode.DoubleOpcodes.init_rth:
                    Assembler.TypeToCompile tok_ttc = Metadata.GetTTC(new Metadata.TableIndex(il.il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);
                    ti_name = Mangler2.MangleTypeInfo(tok_ttc, ass);
                    ass.Requestor.RequestTypeInfo(tok_ttc);
                    break;

                default:
                    throw new NotSupportedException();
            }

            libasm.hardware_location loc_obj = il.stack_vars_before.GetAddressOf(il.stack_before.Count - 1, ass);
            Signature.Param p_obj = il.stack_before.Peek();

            Assembler.TypeToCompile rh_ttc = new Assembler.TypeToCompile(p_obj, ass);
            Layout l = Layout.GetLayout(rh_ttc, ass);

            int fld_offset = l.GetField("IntPtr value", false).offset;

            libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);

            ass.Assign(state, il.stack_vars_before, t1, loc_obj, Assembler.CliType.native_int, il.il.tybel);

            ass.Assign(state, il.stack_vars_before,
                new libasm.hardware_contentsof { base_loc = t1, const_offset = fld_offset, size = ass.GetSizeOfIntPtr() },
                new libasm.hardware_addressoflabel(ti_name, offset, true), Assembler.CliType.native_int, il.il.tybel);

            //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i,
            //    var.ContentsOf(i.stack_before[i.stack_before.Count - 1].contains_variable, fld_offset),
            //    var.AddrOfObject(ti_name, offset), var.Null));
        }
    }
}
