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
    internal class ldtoken
    {
        internal static CilNode Decompose_ldtoken(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Assembler.MethodAttributes attrs)
        {
            /* First decide on the type of handle to create: field, type or method */
            Metadata.TypeDefRow th_type;
            Opcode init_opcode;

            switch (n.il.inline_tok.Value.TableId())
            {
                case (int)Metadata.TableId.MethodDef:
                case (int)Metadata.TableId.MethodSpec:
                    th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeMethodHandle", ass);
                    init_opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.init_rmh)];
                    break;

                case (int)Metadata.TableId.TypeDef:
                case (int)Metadata.TableId.TypeRef:
                case (int)Metadata.TableId.TypeSpec:
                    th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeTypeHandle", ass);
                    init_opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.init_rth)];
                    break;

                case (int)Metadata.TableId.Field:
                    th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeFieldHandle", ass);
                    init_opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.init_rfh)];
                    break;

                case (int)Metadata.TableId.MemberRef:
                    {
                        Metadata.ITableRow ref_type = Metadata.ResolveRef(n.il.inline_tok.Value, ass);
                        if (ref_type is Metadata.MethodDefRow)
                        {
                            th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeMethodHandle", ass);
                            init_opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.init_rmh)];
                        }
                        else if (ref_type is Metadata.FieldRow)
                        {
                            th_type = Metadata.GetTypeDef("mscorlib", "System", "RuntimeFieldHandle", ass);
                            init_opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.init_rfh)];
                        }
                        else
                            throw new NotSupportedException();
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            // newobj, init_rth/rfh/rmh
            Assembler.TypeToCompile rth_ttc = new Assembler.TypeToCompile();
            rth_ttc.type = th_type;
            rth_ttc.tsig = new Signature.Param(rth_ttc.type, ass);

            CilNode i_1 = new CilNode
            {
                il = new InstructionLine
                {
                    opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.newobj)],
                    inline_tok = new TTCToken { ttc = rth_ttc }
                }
            };

            CilNode i_2 = new CilNode
            {
                il = new InstructionLine
                {
                    opcode = init_opcode,
                    inline_tok = n.il.inline_tok
                }
            };

            n.replaced_by = new List<CilNode> { i_1, i_2 };

            return i_1;
        }
    }
}
