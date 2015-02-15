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

/* System.String internal calls */

namespace libsupcs
{
    class String
    {
        [MethodAlias("_Zu1SM_0_14InternalStrcpy_Rv_P5u1Siu1Sii")]
        static unsafe bool InternalStrcpy(byte *dest, int destPos, byte *src, int srcPos, int count)
        {
            /* Get size of src and dest */
            int srcLength = *(int*)(src + StringOperations.GetLengthOffset());
            int destLength = *(int*)(dest + StringOperations.GetLengthOffset());

            /* Ensure the source and destination are big enough */
            if (destPos < 0)
                return false;
            if (srcPos < 0)
                return false;
            if (count < 0)
                return false;
            if (destPos + count > destLength)
                return false;
            if (srcPos + count > srcLength)
                return false;

            /* Do the copy */
            MemoryOperations.MemCpy((void*)(dest + StringOperations.GetDataOffset() + destPos * 2),
                (void*)(src + StringOperations.GetDataOffset() + srcPos * 2), count * 2);

            return true;
        }

        [MethodAlias("_Zu1SM_0_14InternalStrcpy_Rv_P3u1Siu1S")]
        static unsafe bool InternalStrcpy(byte* dest, int destPos, byte* src)
        {
            /* Get size of src and dest */
            int srcLength = *(int*)(src + StringOperations.GetLengthOffset());
            int destLength = *(int*)(dest + StringOperations.GetLengthOffset());
            int srcPos = 0;
            int count = srcLength;

            /* Ensure the source and destination are big enough */
            if (destPos < 0)
                return false;
            if (destPos + count > destLength)
                return false;

            /* Do the copy */
            MemoryOperations.MemCpy((void*)(dest + StringOperations.GetDataOffset() + destPos * 2),
                (void*)(src + StringOperations.GetDataOffset() + srcPos * 2), count * 2);

            return true;
        }
    }
}
