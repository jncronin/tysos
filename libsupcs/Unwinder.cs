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

namespace libsupcs
{
    public abstract class Unwinder
    {
        public abstract UIntPtr GetInstructionPointer();
        public abstract Unwinder UnwindOne();
        public abstract Unwinder UnwindOne(libsupcs.TysosMethod cur_method);
        public abstract libsupcs.TysosMethod GetMethodInfo();
        public abstract Unwinder Init();
        public abstract bool CanContinue();
        public virtual object[] DoUnwind(UIntPtr exit_address) { return DoUnwind(this, exit_address); }

        public class UnwinderEntry
        {
            public UIntPtr ProgramCounter;
            public TysosMethod Method;
        }

        internal static object[] DoUnwind(Unwinder u, UIntPtr exit_address)
        {
            System.Collections.ArrayList ret = new System.Collections.ArrayList();

            while (u.CanContinue() && (u.GetInstructionPointer() != exit_address))
            {
                //TysosMethod meth = u.GetMethodInfo();
                TysosMethod meth = null;
                ret.Add(new UnwinderEntry { ProgramCounter = u.GetInstructionPointer(), Method = meth });
                if (meth == null)
                    u.UnwindOne();
                else
                    u.UnwindOne(meth);
            }

            return ret.ToArray();
        }
    }
}
