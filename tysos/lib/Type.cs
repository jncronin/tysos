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

namespace tysos.lib
{
    class Type
    {
        internal static object CreateObject(libsupcs.TysosType type)
        {
            /* Create an object of a certain type
             * 
             * If type is an unboxed value type, then we return the boxed equivalent
             * 
             * To do this, we get the VTable entry from the type then dereference it
             * to get the boxed type.  For reference types this does nothing apart from
             * return the original typeinfo again.
             * 
             * Then, set the __vtbl field to the VTable, set the __object_id field and
             * set the mutex lock field
             */

            unsafe
            {
                IntPtr vtable_object = type.VTable;
                IntPtr actual_type_info_ptr = *(IntPtr*)vtable_object;
                libsupcs.TysosType actual_type_info = libsupcs.TysosType.ReinterpretAsType(actual_type_info_ptr);

                /* The following is specific for x86_64 */
                ulong obj = gc.gc.Alloc((ulong)actual_type_info.GetClassSize());

                *(ulong*)obj = (ulong)vtable_object;
                *(int*)(obj + 8) = Program.GetNewObjId();
                *(ulong*)(obj + 12) = 0; // mutex_lock

                return libsupcs.CastOperations.ReinterpretAsObject(obj);
            }
        }

        internal static object PlacementNew(libsupcs.TysosType type, ulong obj)
        {
            unsafe
            {
                IntPtr vtable_object = type.VTable;
                IntPtr actual_type_info_ptr = *(IntPtr*)vtable_object;
                libsupcs.TysosType actual_type_info = libsupcs.TysosType.ReinterpretAsType(actual_type_info_ptr);

                /* The following is specific for x86_64 */
                *(ulong*)obj = (ulong)vtable_object;
                *(int*)(obj + 8) = Program.GetNewObjId();
                *(ulong*)(obj + 12) = 0; // mutex_lock

                return libsupcs.CastOperations.ReinterpretAsObject(obj);
            }
        }
    }
}
