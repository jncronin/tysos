/* Copyright (C) 2012 by John Cronin
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

namespace libtysila.x86_64
{
    class x86_64_MiniAssembler : MiniAssembler
    {
        public override int GetSizeOfPointer()
        {
            return 8;
        }

        public override byte[] GetJITStub(UIntPtr meth_info)
        {
            throw new NotImplementedException();
        }

        public override byte[] GetCallsRefImplementation(UIntPtr meth_info, UIntPtr ref_impl_code)
        {
            // TODO: add in an enter opcode to store the meth_info information

            // For now just do mov rax, ref_impl_code; jmp rax
            byte[] ret = new byte[12];
            ret[0] = 0x48;
            ret[1] = 0xb8;
            ret[10] = 0xff;
            ret[11] = 0xe0;

            LSB_Assembler.SetByteArrayS(ret, 2, LSB_Assembler.ToByteArrayS(Convert.ToUInt64(ref_impl_code)), 0, 8);

            return ret;
        }
    }
}
