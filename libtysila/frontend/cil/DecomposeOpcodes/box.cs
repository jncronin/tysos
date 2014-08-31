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
        internal static CilNode Decompose_box(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            // Don't box reference types
            Signature.Param unbox_type = n.il.stack_before.Peek();
            Metadata.TypeDefRow tdr = Metadata.GetTypeDef(unbox_type.Type, ass);
            if ((tdr != null) && tdr.IsValueType(ass))
            {
                /* We don't handle nullable types yet
                 * NB this is not a robust check */
                if (tdr.TypeNamespace == "System" && tdr.TypeName == "Nullable`1")
                    throw new NotImplementedException("Boxing on Nullable types not yet implemented");

                // newobj, dup, flip3, stfld
                Signature.Param box_type = new Signature.Param(new Signature.BoxedType { Type = unbox_type.Type }, ass);
                Assembler.TypeToCompile boxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = box_type, type = tdr };
                Assembler.FieldToCompile value_ftc = new Assembler.FieldToCompile
                {
                    _ass = ass,
                    definedin_tsig = boxed_ttc.tsig,
                    definedin_type = boxed_ttc.type,
                    field = new Metadata.FieldRow { Name = "m_value" },
                    fsig = unbox_type
                };

                timple.BaseNode i_1 = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.newobj)], inline_tok = new TTCToken { ttc = boxed_ttc } } });
                timple.BaseNode i_2 = i_1.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.dup)] } });
                timple.BaseNode i_3 = i_2.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[0xfd21] } });
                timple.BaseNode i_4 = i_3.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stfld)], inline_tok = new FTCToken { ftc = value_ftc } } });

                n.Remove();
                n.replaced_by = new List<CilNode> { (CilNode)i_1, (CilNode)i_2, (CilNode)i_3, (CilNode)i_4 };

                return (CilNode)i_1;
            }
            else
            {
                /* Reference type - convert to nop */
                timple.BaseNode i_1 = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.nop)] } });

                n.Remove();
                n.replaced_by = new List<CilNode> { (CilNode)i_1 };

                return (CilNode)i_1;
            }
        }

        internal static CilNode Decompose_unbox(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            throw new NotImplementedException();
            timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[0xfd23], inline_tok = n.il.inline_tok } });
            n.Remove();

            n.replaced_by = new List<CilNode> { (CilNode)first };
            return (CilNode)first;
        }

    }
}
