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
    class obj
    {
        public static void tybel_ldobj(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_src = il.stack_after.Pop();

            libasm.hardware_location loc_src = il.stack_vars_after.Pop(ass);

            Assembler.TypeToCompile typeTok = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.Param p_T = typeTok.tsig;

            if (ass.Options.VerifiableCIL)
            {
                if (!(p_src.Type is Signature.ManagedPointer))
                    throw new Assembler.AssemblerException("ldobj: src is not a managed pointer (is instead " +
                        p_src.ToString() + ")", il.il, mtc);
                if (!ass.IsAssignableTo(p_T, p_src))
                    throw new Assembler.AssemblerException("ldobj: src (" + p_src.ToString() + ") is not verifier-assignable-to " +
                        "T (" + p_T.ToString() + ")", il.il, mtc);
            }

            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_T, ass);

            int size = ass.GetSizeOf(p_T);
            if (size <= ass.GetSizeOfPointer())
                ass.Peek(state, il.stack_vars_before, loc_dest, loc_src, size, il.il.tybel);
            else
            {
                libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                ass.Assign(state, il.stack_vars_before, t1, loc_src, Assembler.CliType.native_int, il.il.tybel);
                ass.Assign(state, il.stack_vars_before, loc_dest, new libasm.hardware_contentsof { base_loc = t1, size = size }, p_T.CliType(ass),
                    il.il.tybel);
            }

            il.stack_after.Push(p_T);
        }
        public static void tybel_stobj(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_src = il.stack_after.Pop();
            Signature.Param p_dest = il.stack_after.Pop();

            libasm.hardware_location loc_src = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_dest = il.stack_vars_after.Pop(ass);

            Assembler.TypeToCompile typeTok = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.Param p_T = typeTok.tsig;

            /* Ensure dest is a pointer to T */
            if (ass.Options.VerifiableCIL)
            {
                if (!(p_dest.Type is Signature.ManagedPointer) || !Signature.BCTCompare(((Signature.ManagedPointer)p_dest.Type).ElemType, p_T.Type, ass))
                    throw new Assembler.AssemblerException("stobj: dest (" + p_dest.ToString() + ") is not a pointer to " +
                        "T (" + p_T.ToString() + ")", il.il, mtc);
            }

            /* Ensure src is verifier-assignable-to T */
            if(!ass.IsAssignableTo(p_T, p_src))
                throw new Assembler.AssemblerException("stobj: src (" + p_src.ToString() + ") is not verifier-assignable-to " +
                    "T (" + p_T.ToString() + ")", il.il, mtc);

            int size = ass.GetSizeOf(p_T);
            if (size <= ass.GetSizeOfPointer())
                ass.Poke(state, il.stack_vars_before, loc_dest, loc_src, size, il.il.tybel);
            else
            {
                libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                ass.Assign(state, il.stack_vars_before, t1, loc_dest, Assembler.CliType.native_int, il.il.tybel);
                ass.Assign(state, il.stack_vars_before, new libasm.hardware_contentsof { base_loc = t1, size = size }, loc_src, p_T.CliType(ass),
                    il.il.tybel);
            }
        }

        public static void tybel_sizeof(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_dest = new Signature.Param(BaseType_Type.I4);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            Assembler.TypeToCompile typeTok = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.Param p_T = typeTok.tsig;

            int size = ass.GetPackedSizeOf(p_T);

            ass.Assign(state, il.stack_vars_before, loc_dest, size, Assembler.CliType.int32, il.il.tybel);

            il.stack_after.Push(p_dest);
        }

        public static void tybel_ldobj_addr(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            StringToken obj_name = il.il.inline_tok as StringToken;

            Signature.Param p_dest = new Signature.Param(BaseType_Type.I);
            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_dest, ass);

            ass.Assign(state, il.stack_vars_before, loc_dest, new libasm.hardware_addressoflabel(obj_name.str, true),
                Assembler.CliType.native_int, il.il.tybel);

            il.stack_after.Push(p_dest);
        }
    }
}
