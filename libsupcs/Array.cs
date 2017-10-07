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

namespace libsupcs
{
    class Array
    {
        [MethodAlias("_ZW6System5Array_8FastCopy_Rb_P5V5ArrayiV5Arrayii")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe bool FastCopy(void *srcArr, int srcIndex, void *dstArr, int dstIndex, int length)
        {
            /* This is often called with length == 0 when the
             * first item is added to a List<T> as the default
             * array within the list has length 0.
             */
            
            if (length == 0)
                return true;

            // Ensure both arrays are of rank 1
            int srcRank = *(int*)((byte*)srcArr + ArrayOperations.GetRankOffset());
            int dstRank = *(int*)((byte*)dstArr + ArrayOperations.GetRankOffset());
            if (srcRank != 1)
                return false;
            if (dstRank != 1)
                return false;

            // Ensure srcIndex is valid
            int srcLobound = **(int**)((byte*)srcArr + ArrayOperations.GetLoboundsOffset());
            int srcSize = **(int**)((byte*)srcArr + ArrayOperations.GetSizesOffset());
            srcIndex -= srcLobound;
            if (srcIndex + length > srcSize)
                return false;

            // Ensure destIndex is valid
            int dstLobound = **(int**)((byte*)dstArr + ArrayOperations.GetLoboundsOffset());
            int dstSize = **(int**)((byte*)dstArr + ArrayOperations.GetSizesOffset());
            dstIndex -= dstLobound;
            if (dstIndex + length > dstSize)
                return false;

            // Ensure both have same element type
            void* srcET = *(void**)((byte*)srcArr + ArrayOperations.GetElemTypeOffset());
            void* dstET = *(void**)((byte*)dstArr + ArrayOperations.GetElemTypeOffset());
            if (srcET != dstET)
                return false;

            // Get element size
            int elemSize = *(int*)((byte*)srcArr + ArrayOperations.GetElemSizeOffset());

            srcIndex *= elemSize;
            dstIndex *= elemSize;

            byte* srcAddr = *(byte**)((byte*)srcArr + ArrayOperations.GetInnerArrayOffset()) + srcIndex;
            byte* dstAddr = *(byte**)((byte*)dstArr + ArrayOperations.GetInnerArrayOffset()) + dstIndex;
            length *= elemSize;

            MemoryOperations.MemMove(dstAddr, srcAddr, length);

            return true;
        }

        [MethodAlias("_ZW6System5Array_13GetLowerBound_Ri_P2u1ti")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe int GetLowerBound(void *arr, int rank)
        {
            int arrRank = *(int*)((byte*)arr + ArrayOperations.GetRankOffset());
            if (rank < 0 || rank >= arrRank)
            {
                System.Diagnostics.Debugger.Break();
                throw new IndexOutOfRangeException();
            }

            int* lbPtr = *(int**)((byte*)arr + ArrayOperations.GetLoboundsOffset());
            return *(lbPtr + rank);
        }

        [MethodAlias("_ZW6System5Array_9GetLength_Ri_P2u1ti")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe int GetLength(void *arr, int rank)
        {
            if(arr == null)
            {
                System.Diagnostics.Debugger.Break();
                throw new ArgumentNullException();
            }
            int arrRank = *(int*)((byte*)arr + ArrayOperations.GetRankOffset());
            if (rank < 0 || rank >= arrRank)
            {
                System.Diagnostics.Debugger.Break();
                throw new IndexOutOfRangeException();
            }

            int* szPtr = *(int**)((byte*)arr + ArrayOperations.GetSizesOffset());
            return *(szPtr + rank);
        }

        [MethodAlias("_ZW6System5Array_7GetRank_Ri_P1u1t")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe int GetRank(void *arr)
        {
            return *(int*)((byte*)arr + ArrayOperations.GetRankOffset());
        }

        [MethodAlias("_ZW35System#2ERuntime#2ECompilerServices14RuntimeHelpers_15InitializeArray_Rv_P2U6System5Arrayu1I")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe void InitializeArray(void* arr, void* fld_handle)
        {
            System.Diagnostics.Debugger.Break();
        }

        [MethodAlias("_ZW6System5Array_12GetValueImpl_Ru1O_P2u1ti")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe void *GetValueImpl(void *arr, int pos)
        {
            /* Get the element type of the array */
            void* et = *(void**)((byte*)arr + ArrayOperations.GetElemTypeOffset());

            /* Get a pointer to the source data */
            var elem_size = *(int*)((byte*)arr + ArrayOperations.GetElemSizeOffset());
            void* ia = *(void**)((byte*)arr + ArrayOperations.GetInnerArrayOffset());
            void* sptr = (void*)((byte*)ia + pos * elem_size);

            /* Is this a value type? In which case we need to return a boxed value */
            void* extends = *(void**)((byte*)et + ClassOperations.GetVtblExtendsVtblPtrOffset());

            if (extends == OtherOperations.GetStaticObjectAddress("_Zu1L") ||
                extends == OtherOperations.GetStaticObjectAddress("_ZW6System4Enum"))
            {
                /* This is a value type.  We need to read the size of the element,
                 * create a new object of the appropriate size and copy the data
                 * into it */
                byte *ret = (byte*)MemoryOperations.GcMalloc(elem_size + ClassOperations.GetBoxedTypeDataOffset());
                *(void**)(ret + ClassOperations.GetVtblFieldOffset()) = et;
                *(ulong*)(ret + ClassOperations.GetMutexLockOffset()) = 0;

                /* Avoid calls to memcpy if possible */
                switch(elem_size)
                {
                    case 1:
                        *(byte*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(byte*)sptr;
                        return ret;
                    case 2:
                        *(ushort*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(ushort*)sptr;
                        return ret;
                    case 4:
                        *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(uint*)sptr;
                        return ret;
                    case 8:
                        if (OtherOperations.GetPointerSize() >= 8)
                        {
                            *(ulong*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(ulong*)sptr;
                            return ret;
                        }
                        else
                        {
                            *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(uint*)sptr;
                            *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset() + 4) = *(uint*)((byte*)sptr + 4);
                            return ret;
                        }
                    case 16:
                        if (OtherOperations.GetPointerSize() >= 8)
                        {
                            *(ulong*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(ulong*)sptr;
                            *(ulong*)(ret + ClassOperations.GetBoxedTypeDataOffset() + 8) = *(ulong*)((byte*)sptr + 8);
                            return ret;
                        }
                        else
                        {
                            *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset()) = *(uint*)sptr;
                            *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset() + 4) = *(uint*)((byte*)sptr + 4);
                            *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset() + 8) = *(uint*)((byte*)sptr + 8);
                            *(uint*)(ret + ClassOperations.GetBoxedTypeDataOffset() + 12) = *(uint*)((byte*)sptr + 12);
                            return ret;
                        }
                }

                /* Do data copy via memcpy */
                MemoryOperations.MemCpy(ret + ClassOperations.GetBoxedTypeDataOffset(),
                    sptr, elem_size);
                return ret;
            }
            else
            {
                /* Its a reference type, so just return the pointer */
                return *(void**)sptr;                 
            }
        }
    }
}
