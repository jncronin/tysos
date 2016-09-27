/* Copyright (C) 2016 by John Cronin
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
using metadata;

namespace libtysila4.layout
{
    public partial class Layout
    {
        /* Vtable:

            TIPtr (just to a glorified TypeSpec)
            IFacePtr (to list of implemented interfaces)
            Extends (to base classes for quick castclassex)
            Method 0
            ...

            TI TODO

            IFaceList TODO


        */

        public void CreateVtableLayout(metadata.TypeSpec ts, target.Target t)
        {

        }

        public static int GetVTableOffset(metadata.MethodSpec ms)
        { return GetVTableOffset(ms.type, ms); }

        public static int GetVTableOffset(metadata.TypeSpec ts,
            metadata.MethodSpec ms)
        {
            var extends = ts.GetExtends();
            var vtbl_length = GetVTableMethLength(extends);
            var search_meth_name = ms.m.GetIntEntry(MetadataStream.tid_MethodDef,
                ms.mdrow, 3);

            /* Iterate through methods looking for requested
                one */
            var first_mdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 5);
            var last_mdef = ts.m.GetLastMethodDef(ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var flags = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 2);

                // Check on name
                var mname = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 3);
                if (MetadataStream.CompareString(ts.m, mname,
                    ms.m, search_meth_name))
                {
                    // TODO: check signature

                    if ((flags & 0x40) == 0x40)
                    {
                        // Virtual
                        return vtbl_length;
                    }
                    else
                    {
                        // Instance
                        return -1;
                    }
                }
                else
                {
                    // This is not the method we are looking for
                    // Virtual & NewSlot
                    if ((flags & 0x140) == 0x140)
                        vtbl_length++;
                }
            }

            return 0;   // fail
        }

        public static int GetVTableTIOffset(TypeSpec ts)
        {
            return GetVTableMethLength(ts);
        }

        private static int GetVTableMethLength(TypeSpec ts)
        {
            if (ts == null)
                return 3; // tiptr, ifaceptr, extends

            var extends = ts.GetExtends();
            var vtbl_length = GetVTableMethLength(extends);

            /* Iterate through methods adding 1 for each virtual
                one */
            var first_mdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 5);
            var last_mdef = ts.m.GetLastMethodDef(ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var flags = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 2);

                // Virtual & NewSlot
                if ((flags & 0x140) == 0x140)
                    vtbl_length++;
            }

            return vtbl_length;
        }
    }
}
