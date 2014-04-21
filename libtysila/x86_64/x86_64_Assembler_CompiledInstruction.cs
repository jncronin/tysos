/* Copyright (C) 2011 by John Cronin
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
using libasm;

namespace libtysila
{
	partial class x86_64_Assembler
	{
        internal class x86_64_Instruction : CodeBlock.CompiledInstruction
        {
            public class Prefixes
            {
                public bool LOCK = false;
                public bool REPNE = false;
                public bool REP = false;
                public bool OP_SIZE = false;
                public bool ADDR_SIZE = false;

                public bool REX_W = false;
                public bool REX_R = false;
                public bool REX_X = false;
                public bool REX_B = false;
            }

            public string opcode;

            public class Opcode_def
            {
                public string name;
                public byte[] opcodes;

                public enum Encoding { modRM_reg, modRM_rm, imm8, imm16, imm32, imm64, RAX, none }
                public Encoding Operand1 = Encoding.none;
                public Encoding Operand2 = Encoding.none;
                public Encoding Operand3 = Encoding.none;
                public Encoding Operand4 = Encoding.none;
            }

            public class Opcodes
            {
                public static Opcode_def AAA = new Opcode_def { name = "aaa", opcodes = new byte[] { 0x37 } };
                public static Opcode_def ADD_04 = new Opcode_def { name = "add", opcodes = new byte[] { 0x04 }, Operand1 = Opcode_def.Encoding.RAX, Operand2 = Opcode_def.Encoding.imm8 };

            }

            public Prefixes Prefix = new Prefixes();
            public Opcode_def Opcode;
            public hardware_location Operand1, Operand2, Operand3, Operand4;
            public byte Scale = 0x00;
            public hardware_location Index;
            public hardware_location Base;
            public int Displacement;
            public int Immediate;

            public override IList<byte> GetCompiledRepresentation()
            {
                throw new NotImplementedException();
            }

            /* public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (Prefix.LOCK)
                    sb.Append("lock ");
                if (Prefix.REPNE)
                    sb.Append("repne ");
                if (Prefix.REP)
                    sb.Append("rep ");
                if (Prefix.REX_W)
                    sb.Append("rex.w ");
                if (Prefix.REX_R)
                    sb.Append("rex.r ");
                if (Prefix.REX_X)
                    sb.Append("rex.x ");
                if (Prefix.REX_B)
                    sb.Append("rex.b ");

                sb.Append(Opcode.name);

                return sb.ToString();
            } */

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(opcode);

                if (Operand1 != null)
                    sb.Append(" " + Operand1.ToString());
                if (Operand2 != null)
                    sb.Append(", " + Operand2.ToString());
                if (Operand3 != null)
                    sb.Append(", " + Operand3.ToString());
                if (Operand4 != null)
                    sb.Append(", " + Operand4.ToString());

                return sb.ToString();
            }
        }
	}
}
