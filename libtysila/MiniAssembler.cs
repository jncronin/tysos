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

/* This is an abstract class whose subclasses perform basic assembler instructions
 * for a given architecture - it can be substantiated without any output methods,
 * for example and does not produce relocations */

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    public abstract class MiniAssembler
    {
        public static MiniAssembler GetMiniAssembler(string arch)
        {
            Assembler _ass = Assembler.CreateAssembler(Assembler.ParseArchitectureString(arch), null, null, new Assembler.AssemblerOptions { MiniAssembler = true });
            return _ass.GetMiniAssembler();
        }

        public abstract int GetSizeOfPointer();
        public abstract byte[] GetJITStub(UIntPtr meth_info);
        public abstract byte[] GetCallsRefImplementation(UIntPtr meth_info, UIntPtr ref_impl_code);
    }
}
