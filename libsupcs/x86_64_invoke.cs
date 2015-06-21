/* Copyright (C) 2015 by John Cronin
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

namespace libsupcs.x86_64
{
    [ArchDependent("x86_64")]
    class x86_64_invoke
    {
        [MethodReferenceAlias("__x86_64_invoke")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        static unsafe extern object asm_invoke(IntPtr meth, int p_length,
            void* parameters, void* plocs);

        [MethodAlias("__invoke")]
        [Bits64Only]
        static unsafe object InternalInvoke(IntPtr meth, Object[] parameters)
        {
            /* Build an array of the call locations of each parameter
             * 
             * 0 - INTEGER (pass as-is)
             * 1 - INTEGER (unbox in asm)
             * 2 - SSE (unbox in asm)
             * 3 - MEMORY (upper 24 bits give length of object)
             * 4 - INTEGER (unbox low 32 bits in asm)
             */

            uint[] plocs = null;
            int p_length = 0;

            if (parameters != null)
            {
                p_length = parameters.Length;

                plocs = new uint[p_length];

                for (int i = 0; i < p_length; i++)
                {
                    TysosType p_type = TysosType.ReinterpretAsType(**(void***)CastOperations.ReinterpretAsPointer(parameters[i]));
                    if (p_type.IsBoxed)
                    {
                        int size = p_type.GetUnboxedType().GetClassSize();
                        if (size > 8)
                        {
                            if (size > 0xffffff)
                            {
                                throw new Exception("x86_64.Invoke: value type (" +
                                    p_type.FullName + ") is too large (" + size.ToString() +
                                    " bytes)");
                            }
                            uint val = ((uint)size) << 8;
                            val |= 3;
                            plocs[i] = val;
                        }
                        else
                        {
                            if (p_type.Equals(typeof(float)) || p_type.Equals(typeof(double)))
                            {
                                plocs[i] = 2;
                            }
                            else if(size > 4)
                            {
                                plocs[i] = 1;
                            }
                            else
                            {
                                plocs[i] = 4;
                            }
                        }
                    }
                    else
                    {
                        plocs[i] = 0;
                    }
                }
            }

            return asm_invoke(meth, p_length,
                (parameters == null) ? null : MemoryOperations.GetInternalArray(parameters),
                (plocs == null) ? null : MemoryOperations.GetInternalArray(plocs));
        }
    }
}
