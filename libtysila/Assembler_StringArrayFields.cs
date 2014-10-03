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

namespace libtysila
{
    partial class Assembler
    {
#if false
        internal enum ArrayFields { rank, lobounds, sizes, elem_size, inner_array, elemtype, inner_array_length, array_type_size, getvalueimpl_vtbl_offset };
        bool array_fields_calculated = false;
        int array_rank_offset, array_lobounds_offset, array_sizes_offset, array_elem_size_offset, array_inner_array_offset, array_type_size, array_getvalueimpl_vtbl_offset;
        int array_elemtype_offset, array_inner_array_length_offset;

        internal int GetArrayFieldOffset(ArrayFields field)
        {
            if (!array_fields_calculated)
            {
                // Generate a simple complex array to interrogate about its fields

                /*TypeToCompile simple_array = CreateArray(new Signature.Param(new Signature.ComplexArray { _ass = this, ElemType = new Signature.BaseType(BaseType_Type.I4), Rank = 1, LoBounds = new int[] { }, Sizes = new int[] { } },
                    this), 1, Metadata.GetTTC("mscorlib", "System", "Int32", this));*/
                TypeToCompile simple_array;
                throw new NotImplementedException();
                Layout l = Layout.GetLayout(simple_array, this, false);

                array_rank_offset = l.GetFirstInstanceField("__rank").offset;
                array_lobounds_offset = l.GetFirstInstanceField("__lobounds").offset;
                array_sizes_offset = l.GetFirstInstanceField("__sizes").offset;
                array_elem_size_offset = l.GetFirstInstanceField("__elemsize").offset;
                array_inner_array_offset = l.GetFirstInstanceField("__inner_array").offset;
                array_elemtype_offset = l.GetFirstInstanceField("__elemtype").offset;
                array_inner_array_length_offset = l.GetFirstInstanceField("__inner_array_length").offset;
                array_type_size = l.ClassSize;

                array_getvalueimpl_vtbl_offset = l.GetVirtualMethod("GetValueImpl").offset;

                array_fields_calculated = true;
            }

            switch (field)
            {
                case ArrayFields.elem_size:
                    return array_elem_size_offset;
                case ArrayFields.inner_array:
                    return array_inner_array_offset;
                case ArrayFields.lobounds:
                    return array_lobounds_offset;
                case ArrayFields.rank:
                    return array_rank_offset;
                case ArrayFields.sizes:
                    return array_sizes_offset;
                case ArrayFields.elemtype:
                    return array_elemtype_offset;
                case ArrayFields.inner_array_length:
                    return array_inner_array_length_offset;
                case ArrayFields.array_type_size:
                    return array_type_size;
                case ArrayFields.getvalueimpl_vtbl_offset:
                    return array_getvalueimpl_vtbl_offset;
            }

            throw new NotSupportedException();
        }
#endif

        public enum StringFields { length, data_offset, vtbl, objid, mutex_lock };
        bool string_fields_calculated = false;
        int string_length_offset, string_data_offset, string_vtbl_offset, string_objid_offset, string_mutexlock_offset;
        Layout string_layout;

        internal string GetStringTypeInfoObjectName()
        {
            GetStringFieldOffset(StringFields.vtbl);
            return string_layout.typeinfo_object_name;
        }

        public int GetStringFieldOffset(StringFields field)
        {
            if (!string_fields_calculated)
            {
                TypeToCompile str_ttc = new TypeToCompile { _ass = this, tsig = new Signature.Param(BaseType_Type.String), type = Metadata.GetTypeDef(new Signature.BaseType(BaseType_Type.String), this) };
                Layout l = Layout.GetLayout(str_ttc, this, false);
                string_layout = l;

                string_length_offset = l.GetFirstInstanceField("length").offset;
                string_data_offset = l.GetFirstInstanceField("start_char").offset;
                string_vtbl_offset = l.GetFirstInstanceField("__vtbl").offset;
                string_objid_offset = l.GetFirstInstanceField("__object_id").offset;
                string_mutexlock_offset = l.GetFirstInstanceField("__mutex_lock").offset;
            }

            switch (field)
            {
                case StringFields.data_offset:
                    return string_data_offset;
                case StringFields.length:
                    return string_length_offset;
                case StringFields.objid:
                    return string_objid_offset;
                case StringFields.vtbl:
                    return string_vtbl_offset;
                case StringFields.mutex_lock:
                    return string_mutexlock_offset;
            }

            throw new NotSupportedException();
        }
    }
}
