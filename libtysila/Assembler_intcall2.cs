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

namespace libtysila
{
    partial class Assembler
    {
        public class InstructionLine
        {
        }

        protected virtual bool Arch_enc_intcall(string mangled_name, InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state, bool provides, ref bool i_pushes_set)
        {
            return false;
        }

        private bool provides_intcall(MethodToCompile mtc)
        {
            InstructionLine instr = new InstructionLine();
            return enc_intcall(instr, mtc.meth, mtc.msig, mtc.type, mtc.tsigp, new AssemblerState(), true);
        }

        private bool enc_intcall(InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state)
        { return enc_intcall(i, mdr, msig, tdr, tsig, state, false); }
        private bool enc_intcall(InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state, bool provides)
        {
            return false;
        }
    }
}
