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

namespace libtysila.frontend.cil.DecomposeOpcodes
{
    internal class box
    {
        internal static CilNode Decompose_unboxany(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block, Assembler.MethodAttributes attrs)
        {
            Signature.Param boxed_type = null;
            if (n.il.stack_before != null)
                boxed_type = n.il.stack_before.Peek();
            else
                boxed_type = n.stack_before.Peek();

            Assembler.TypeToCompile T;
            if (n.il.inline_tok is TTCToken)
                T = ((TTCToken)n.il.inline_tok).ttc;
            else
                T = Metadata.GetTTC(n.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);

            /* If it is not a value type then just do castclass */
            if (T.type.IsValueType(ass) == false)
                return isinst.Decompose_castclass(n, ass, mtc, ref next_block, attrs);

            /* Build the field object */
            Signature.Param boxed_tsig = new Signature.Param(new Signature.BoxedType(T.tsig.Type), ass);
            Assembler.TypeToCompile boxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(new Signature.BoxedType(T.tsig.Type), ass), type = T.type };
            Assembler.FieldToCompile value_ftc = new Assembler.FieldToCompile
            {
                _ass = ass,
                definedin_tsig = boxed_ttc.tsig,
                definedin_type = boxed_ttc.type,
                field = new Metadata.FieldRow { Name = "m_value" },
                fsig = T.tsig
            };

            /* First, check the item on the stack is of the appropriate type, then
             * load the field value
             * 
             *                                          ..., obj
             * dup                                      ..., obj, obj
             * castclassex                              ..., obj, result
             * throwfalse                               ..., obj
             * ldfld                                    ..., value
             */
            CilNode i_1 = new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.dup)] } };
            CilNode i_2 = new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.castclassex)], inline_tok = n.il.inline_tok } };
            CilNode i_3 = new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.throwfalse)], inline_int = Assembler.throw_InvalidCastException } };
            CilNode i_4 = new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldfld)], inline_tok = new FTCToken { ftc = value_ftc } } };

            n.replaced_by = new List<CilNode> { i_1, i_2, i_3, i_4 };

            return i_1;
        }

        internal static CilNode Decompose_unbox(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block, Assembler.MethodAttributes attrs)
        {
            Signature.Param boxed_type = null;
            if (n.il.stack_before != null)
                boxed_type = n.il.stack_before.Peek();
            else
                boxed_type = n.stack_before.Peek();

            if (!(boxed_type.Type is Signature.BoxedType))
                throw new InvalidCastException("unbox specified without boxed value type on the stack");
            Signature.BoxedType bt = boxed_type.Type as Signature.BoxedType;

            Assembler.TypeToCompile T;
            if (n.il.inline_tok is TTCToken)
                T = ((TTCToken)n.il.inline_tok).ttc;
            else
                T = Metadata.GetTTC(n.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);

            if (T.type.IsValueType(ass) == false)
                throw new InvalidCastException("unbox specified with T being non-value type");

            if (ass.Options.VerifiableCIL)
            {
                if (!Signature.ParamCompare(boxed_type, T.tsig, ass))
                    throw new InvalidCastException("unbox: incompatible types between stack type and inline token: " +
                        boxed_type.ToString() + " and " + T.tsig.ToString());
            }

            Assembler.TypeToCompile boxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = boxed_type, type = T.type };
            Assembler.FieldToCompile value_ftc = new Assembler.FieldToCompile
            {
                _ass = ass,
                definedin_tsig = boxed_ttc.tsig,
                definedin_type = boxed_ttc.type,
                field = new Metadata.FieldRow { Name = "m_value" },
                fsig = T.tsig
            };

            CilNode t_1 = new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldflda)], inline_tok = new FTCToken { ftc = value_ftc } } };
            n.replaced_by = new List<CilNode> { t_1 };
            return t_1;
        }
    }
}
