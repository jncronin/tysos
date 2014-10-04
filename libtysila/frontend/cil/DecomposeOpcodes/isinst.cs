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
using System.Text;

namespace libtysila.frontend.cil.DecomposeOpcodes
{
    internal class isinst
    {
        internal static CilNode Decompose_isinst(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block, Assembler.MethodAttributes attrs)
        {
            timple.BaseNode first = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[0xfd23], inline_tok = n.il.inline_tok } });
            n.Remove();

            n.replaced_by = new List<CilNode> { (CilNode)first };
            return (CilNode)first;
        }

        internal static CilNode Decompose_castclass(CilNode n, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block, Assembler.MethodAttributes attrs)
        {
            timple.BaseNode i_1 = n.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.castclassex)], inline_tok = n.il.inline_tok } });
            timple.BaseNode i_2 = i_1.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.dup)] } });
            timple.BaseNode i_3 = i_2.InsertAfter(new CilNode { il = new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.DoubleOpcodes.throwfalse)], inline_int = Assembler.throw_InvalidCastException } });

            n.Remove();
            n.replaced_by = new List<CilNode> { (CilNode)i_1, (CilNode)i_2, (CilNode)i_3 };
            return (CilNode)i_1;
        }
    }
}
