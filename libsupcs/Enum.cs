﻿/* Copyright (C) 2011 by John Cronin
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
    class Enum
    {
        struct MonoEnumInfo
        {
            internal System.Type utype;
            internal System.Array values;
            internal string[] names;
        }

        [MethodAlias("_ZW6System4Enum_9get_value_Ru1O_P1u1t")]
        [WeakLinkage]
        [AlwaysCompile]
        static unsafe void* get_value(void *obj)
        {
            /* we can copy the entire enum object to an instance
             * of the underlying type and just change the
             * vtbl
             */

            byte* enum_vtbl = *(byte**)obj;
            int enum_size = *(int*)(enum_vtbl + ClassOperations.GetVtblTypeSizeOffset());

            void** enum_ti = *(void***)enum_vtbl;
            void* ut_vtbl = *(enum_ti + 1);

            void* new_obj = MemoryOperations.GcMalloc(enum_size);
            MemoryOperations.MemCpy(new_obj, obj, enum_size);

            *(void**)new_obj = ut_vtbl;
            *(void**)((byte*)new_obj + ClassOperations.GetMutexLockOffset()) = null;

            return new_obj;
        }

        static internal unsafe TysosType GetUnderlyingEnumType(TysosType enum_type)
        {
            return GetUnderlyingEnumType(CastOperations.ReinterpretAsPointer(enum_type));
        }

        static internal unsafe TysosType GetUnderlyingEnumType(void *enum_type)
        {
            return TysosType.internal_from_handle(GetUnderlyingEnumTypeVtbl(enum_type));
        }

        static internal unsafe void* GetUnderlyingEnumTypeVtbl(void* enum_type)
        {
            void* enum_vtbl = *(void**)((byte*)enum_type + ClassOperations.GetSystemTypeImplOffset());
            void** enum_ti = *(void***)enum_vtbl;
            return *(enum_ti + 1);
        }


        [WeakLinkage]
        [MethodAlias("_ZW6System12MonoEnumInfo_13get_enum_info_Rv_P2V4TypeRV12MonoEnumInfo")]
        [AlwaysCompile]
        static unsafe void get_enum_info(TysosType enumType, out MonoEnumInfo info)
        {
            void* et_obj = CastOperations.ReinterpretAsPointer(enumType);

            MonoEnumInfo ret = new MonoEnumInfo();

            ret.utype = GetUnderlyingEnumType(et_obj);

            if (!ret.utype.Equals(typeof(int)))
            {
                throw new Exception("get_enum_info: currently only enums with an underlying type of int are supported");
            }

            var ets = enumType.tspec;

            /* Iterate through methods looking for requested
                  one */
            var first_fdef = ets.m.GetIntEntry(metadata.MetadataStream.tid_TypeDef,
                ets.tdrow, 4);
            var last_fdef = ets.m.GetLastFieldDef(ets.tdrow);

            // First iterate to get number of satic fields
            int static_fields = 0;
            for(uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                var flags = ets.m.GetIntEntry(metadata.MetadataStream.tid_Field,
                    (int)fdef_row, 0);
                if ((flags & 0x10) == 0x10)
                    static_fields++;
            }

            ret.names = new string[static_fields];
            int[] values = new int[static_fields];

            System.Diagnostics.Debugger.Log(0, "libsupcs", "Enum.get_enum_info: found " + static_fields.ToString() + " entries");

            static_fields = 0;
            // Iterate again to get the details
            for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                var flags = ets.m.GetIntEntry(metadata.MetadataStream.tid_Field,
                    (int)fdef_row, 0);
                if ((flags & 0x10) == 0x10)
                {
                    var name = ets.m.GetStringEntry(metadata.MetadataStream.tid_Field,
                        (int)fdef_row, 1);
                    ret.names[static_fields] = name;

                    System.Diagnostics.Debugger.Log(0, "libsupcs", "Enum.get_enum_info: field: " + name);

                    var const_row = ets.m.const_field_owners[fdef_row];
                    if(const_row != 0)
                    {
                        var const_offset = (int)ets.m.GetIntEntry(metadata.MetadataStream.tid_Constant,
                            const_row, 2);

                        ets.m.SigReadUSCompressed(ref const_offset);
                        var const_val = ets.m.sh_blob.di.ReadInt(const_offset);
                        values[static_fields] = const_val;

                        System.Diagnostics.Debugger.Log(0, "libsupcs", "Enum.get_enum_info: value: " + const_val.ToString());
                    }

                    static_fields++;
                }
            }

            ret.values = values;
            info = ret;
        }

        [MethodAlias("_ZW6System4Enum_8ToObject_Ru1O_P2V4Typeu1O")]
        [AlwaysCompile]
        static unsafe object to_object(TysosType enum_type, object value)
        {
            /* Convert value (of an integral type) to an enum of type 'enum_type'
             * and return its boxed value
             */

            if (enum_type == null)
                throw new ArgumentNullException("enumType");
            if (value == null)
                throw new ArgumentException("value");

            /* The incoming value type 'value' is already boxed.
             * 
             * We can therefore just copy its data to a new enum object
             */

            void* value_obj = CastOperations.ReinterpretAsPointer(value);
            void* value_vtbl = *(void**)value_obj;
            int value_csize = *(int*)((byte*)value_vtbl + ClassOperations.GetVtblTypeSizeOffset());

            void* ret_obj = MemoryOperations.GcMalloc(value_csize);
            MemoryOperations.MemCpy(ret_obj, value_obj, value_csize);

            void* ret_vtbl = *(void**)((byte*)CastOperations.ReinterpretAsPointer(enum_type) + ClassOperations.GetSystemTypeImplOffset());
            *(void**)ret_obj = ret_vtbl;
            *(void**)((byte*)ret_obj + ClassOperations.GetMutexLockOffset()) = null;

            return CastOperations.ReinterpretAsObject(ret_obj);
        }
    }
}