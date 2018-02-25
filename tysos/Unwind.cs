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
using System.Runtime.CompilerServices;

namespace tysos
{
    class Unwind
    {
        private Unwind() { }

        internal static void DumpUnwindInfo(object[] uinfo, IDebugOutput output)
        {
            foreach (object ueo in uinfo)
            {
                // We use this cast to avoid a dependency on UnwinderEntry[], the typeinfo
                //  for which is not produced by default in AOT mode for libsupcs
                libsupcs.Unwinder.UnwinderEntry ue = ueo as libsupcs.Unwinder.UnwinderEntry;

                if (ue != null)
                {
                    Formatter.Write((ulong)ue.ProgramCounter, "X", output);
                    Formatter.Write(": ", output);

                    if (ue.Symbol != null)
                    {
                        Formatter.Write(ue.Symbol, output);

                        if ((ulong)ue.Offset != 0)
                        {
                            Formatter.Write(" + ", output);
                            Formatter.Write((ulong)ue.Offset, "x", output);
                        }

                        Formatter.WriteLine(output);
                    }
                    /*else if(Program.stab != null)
                    {
                        ulong offset;
                        string meth = Program.stab.GetSymbolAndOffset((ulong)ue.ProgramCounter, out offset);
                        Formatter.Write(meth, output);
                        Formatter.Write(" + ", output);
                        Formatter.Write(offset, "X", output);
                        Formatter.WriteLine(output);
                    }*/
                    else
                        Formatter.WriteLine("unknown method", output);
                }
            }
        }
    }
}
