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
        static unsafe object InternalInvoke(IntPtr meth, Object[] parameters, TysosType rettype)
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

            object ret = asm_invoke(meth, p_length,
                (parameters == null) ? null : MemoryOperations.GetInternalArray(parameters),
                (plocs == null) ? null : MemoryOperations.GetInternalArray(plocs));

            // See if we have to box the return type
            if (rettype.IsValueType)
            {
                if (rettype.IsEnum)
                    rettype = rettype.GetUnboxedType();
                if (rettype == typeof(int))
                    ret = ReinterpretAsInt(ret);
                else if (rettype == typeof(uint))
                    ret = ReinterpretAsUInt(ret);
                else if (rettype == typeof(long))
                    ret = ReinterpretAsLong(ret);
                else if (rettype == typeof(ulong))
                    ret = ReinterpretAsULong(ret);
                else if (rettype == typeof(short))
                    ret = ReinterpretAsShort(ret);
                else if (rettype == typeof(ushort))
                    ret = ReinterpretAsUShort(ret);
                else if (rettype == typeof(byte))
                    ret = ReinterpretAsByte(ret);
                else if (rettype == typeof(sbyte))
                    ret = ReinterpretAsSByte(ret);
                else if (rettype == typeof(char))
                    ret = ReinterpretAsChar(ret);
                else if (rettype == typeof(IntPtr))
                    ret = ReinterpretAsIntPtr(ret);
                else if (rettype == typeof(UIntPtr))
                    ret = ReinterpretAsUIntPtr(ret);
                else if (rettype == typeof(bool))
                    ret = ReinterpretAsBoolean(ret);
                else if (rettype == typeof(void))
                    ret = null;
                else
                    throw new NotImplementedException("InternalInvoke: return type " + rettype.FullName + " not supported");

                return ret;
            }
            else
                return ret;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern int ReinterpretAsInt(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern uint ReinterpretAsUInt(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern long ReinterpretAsLong(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern ulong ReinterpretAsULong(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern short ReinterpretAsShort(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern ushort ReinterpretAsUShort(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern byte ReinterpretAsByte(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern sbyte ReinterpretAsSByte(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern char ReinterpretAsChar(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern IntPtr ReinterpretAsIntPtr(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern UIntPtr ReinterpretAsUIntPtr(object addr);
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern bool ReinterpretAsBoolean(object addr);
    }
}
