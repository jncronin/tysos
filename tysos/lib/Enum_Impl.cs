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

namespace tysos
{
    class Enum_Impl
    {
        struct MonoEnumInfo
        {
            internal System.Type utype;
            internal System.Array values;
            internal string[] names;
        }

        static libsupcs.TysosType GetUnderlyingEnumType(System.Type enum_type)
        {
            /* Get the underlying type of an enum */

            System.Reflection.FieldInfo value_field = enum_type.GetField("value__");
            if (value_field == null)
                throw new Exception("value__ not found within " + enum_type.ToString());

            return value_field.FieldType as libsupcs.TysosType;
        }

        [libsupcs.MethodAlias("_ZW6System4EnumM_0_9get_value_Ru1O_P1u1t")]
        [libsupcs.AlwaysCompile]
        static object System_Enum_get_value(System.Enum enum_type)
        {
            /* Return a boxed implementation of the value of the enum */

            libsupcs.TysosType field_type = GetUnderlyingEnumType(enum_type.GetType());

            /* Now we calculate the offset of the value__ field
             * It is at the offset of m_value + the offset of value__ */

            // For now, just set it to 16 (the current offset of m_value)
            ulong field_offset = 16;

            unsafe
            {
                if (field_type == typeof(byte))
                    return *(byte*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(sbyte))
                    return *(sbyte*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(ushort))
                    return *(ushort*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(short))
                    return *(short*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(uint))
                    return *(uint*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(int))
                    return *(int*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(ulong))
                    return *(ulong*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
                else if (field_type == typeof(long))
                    return *(long*)(libsupcs.CastOperations.ReinterpretAsUlong(enum_type) + field_offset);
            }

            throw new System.Exception("Invalid underlying enum type: " + field_type.FullName);
        }

        [libsupcs.MethodAlias("_ZW6System12MonoEnumInfoM_0_13get_enum_info_Rv_P2V4TypeRV12MonoEnumInfo")]
        [libsupcs.AlwaysCompile]
        static void get_enum_info(Type enumType, out MonoEnumInfo info)
        {
            MonoEnumInfo ret = new MonoEnumInfo();

            ret.utype = GetUnderlyingEnumType(enumType);

            if (ret.utype != typeof(int))
                throw new Exception("get_enum_info: currently only enums with an underlying type of int are supported");

            System.Reflection.FieldInfo[] fields = enumType.GetFields(System.Reflection.BindingFlags.Static);
            ret.names = new string[fields.Length];

            int[] values = new int[fields.Length];

            for (int i = 0; i < fields.Length; i++)
            {
                libsupcs.TysosField field = fields[i] as libsupcs.TysosField;

                ret.names[i] = field.Name;

                unsafe
                {
                    values[i] = *(int*)field.Constant_data;
                }
            }

            ret.values = values;
            info = ret;
        }

        [libsupcs.MethodAlias("_ZW6System4EnumM_0_8ToObject_Ru1O_P2V4Typeu1O")]
        [libsupcs.AlwaysCompile]
        static object to_object(libsupcs.TysosType enum_type, object value)
        {
            /* Convert value (of an integral type) to an enum of type 'enum_type'
             * and return its boxed value
             */

            if (enum_type == null)
                throw new ArgumentNullException("enumType");
            if (value == null)
                throw new ArgumentException("value");

            unsafe
            {
                /* Create a new object of type enum_type */
                object new_obj = lib.Type.CreateObject(enum_type);
                ulong obj_addr = libsupcs.CastOperations.ReinterpretAsUlong(new_obj);

                // The following is specific for x86_64
                if (value.GetType() == typeof(byte))
                    *(byte*)(obj_addr + 16) = (byte)value;
                else if (value.GetType() == typeof(sbyte))
                    *(sbyte*)(obj_addr + 16) = (sbyte)value;
                else if (value.GetType() == typeof(ushort))
                    *(ushort*)(obj_addr + 16) = (ushort)value;
                else if (value.GetType() == typeof(short))
                    *(short*)(obj_addr + 16) = (short)value;
                else if (value.GetType() == typeof(uint))
                    *(uint*)(obj_addr + 16) = (uint)value;
                else if (value.GetType() == typeof(int))
                    *(int*)(obj_addr + 16) = (int)value;
                else if (value.GetType() == typeof(ulong))
                    *(ulong*)(obj_addr + 16) = (ulong)value;
                else if (value.GetType() == typeof(long))
                    *(long*)(obj_addr + 16) = (long)value;
                else
                    throw new Exception("Enum.to_object: invalid type of 'value': " + value.GetType().ToString());

                return new_obj;
            }
        }
    }
}
