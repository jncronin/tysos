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

namespace tydb
{
    class obj
    {
        public string type;
        public object value;
        public ulong addr;
        public bool is_vt;

        public class field
        {
            public string name;
            public string type;
            public object value;
            public bool is_vt;
            public ulong ti_addr;
            public int offset;
            public ulong size;
        }

        internal static obj get_obj(ulong address)
        {
            bool is_obj = false;
            ulong vtbl_address;
            ulong ti_address = 0;

            // try and get a vtable at the contents of this address
            vtbl_address = mem.get_mem(address);
            ulong offset;
            string vtbl_name = mem.get_symbol(vtbl_address, out offset);
            if (vtbl_name.EndsWith("TI"))
            {
                // try and get a typeinfo at the contents of the vtbl
                ti_address = mem.get_mem(vtbl_address);
                string ti_name = mem.get_symbol(ti_address, out offset);
                if((offset == 0) && (ti_name.EndsWith("TI")))
                    is_obj = true;
            }

            if (is_obj)
            {
                // if an object try and interpret its typeinfo

                obj obj = new obj();
                libtysila.Layout ti_l = Program.arch.ass.GetTysosTypeLayout();
                libtysila.Layout fi_l = Program.arch.ass.GetTysosFieldLayout();
                string full_name = get_type_fullname(ti_address);

                string extends_name = get_type_fullname(mem.get_mem(ti_address + (ulong)ti_l.InstanceFieldOffsets["libsupcs.TysosType Extends"]));
                if (((extends_name == "[mscorlib]System.ValueType") && (full_name != "[mscorlib]System.Enum")) ||
                    (extends_name == "[mscorlib]System.EnumType"))
                    obj.is_vt = true;
                else
                    obj.is_vt = false;

                obj.type = full_name;
                obj.addr = address;

                if (full_name == "[mscorlib]System.String")
                    obj.value = get_string(address);
                else
                {
                    obj.value = get_fields(ti_address, address);
                    
                    /*ulong field_pointer = mem.get_mem(ti_address + (ulong)ti_l.InstanceFieldOffsets["IntPtr Fields"]);

                    if (field_pointer != 0)
                    {
                        List<obj.field> fields = new List<field>();

                        ulong cur_field = mem.get_mem(field_pointer);

                        while (cur_field != 0)
                        {
                            string field_name = get_string(mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["String _Name"]));
                            int flags = (int)mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["Int32 Flags"], 4);
                            int f_offset = (int)mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["Int32 offset"], 4);
                            string field_type = get_type_fullname(mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["tysos.TysosType _FieldType"]));
                            ulong field_ti = mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["tysos.TysosType _FieldType"]);

                            if ((flags & 0x10) == 0x0)
                            {
                                // is an instance field
                                int len = get_data_length(field_type);
                                ulong val = mem.get_mem(address + (ulong)f_offset, len);
                                bool is_vt = false;

                                string field_extends_name = get_type_fullname(mem.get_mem(field_ti + (ulong)ti_l.InstanceFieldOffsets["tysos.TysosType Extends"]));
                                if (((field_extends_name == "[mscorlib]System.ValueType") && (field_type != "[mscorlib]System.Enum")) ||
                                    (field_extends_name == "[mscorlib]System.EnumType"))
                                    is_vt = true;

                                fields.Add(new field { name = field_name, type = field_type, value = val, is_vt = is_vt, ti_addr = field_ti });
                            }

                            field_pointer += (ulong)Program.arch.address_size;
                            cur_field = mem.get_mem(field_pointer);
                        }

                        obj.value = fields.ToArray();
                    } */
                }

                return obj;
            }
            else
            {
                // if not an object, then interpret it as an unsigned integer in the native
                //  byte length

                ulong val = mem.get_mem(address, Program.arch.data_size);
                obj obj = new obj();
                obj.addr = address;

                switch (Program.arch.data_size)
                {
                    case 4:
                        obj.type = "[mscorlib]System.UInt32";
                        obj.value = (uint)val;
                        break;

                    case 8:
                    default:
                        obj.type = "[mscorlib]System.UInt64";
                        obj.value = (ulong)val;
                        break;
                }

                obj.is_vt = true;

                return obj;
            }
        }

        internal static field[] get_fields(ulong ti_address, ulong address)
        {
            libtysila.Layout ti_l = Program.arch.ass.GetTysosTypeLayout();
            libtysila.Layout fi_l = Program.arch.ass.GetTysosFieldLayout();

            ulong field_pointer = mem.get_mem(ti_address + (ulong)ti_l.InstanceFieldOffsets["IntPtr Fields"]);

            if (field_pointer != 0)
            {
                List<obj.field> fields = new List<field>();

                ulong cur_field = mem.get_mem(field_pointer);

                while (cur_field != 0)
                {
                    string field_name = get_string(mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["String _Name"]));
                    int flags = (int)mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["Int32 Flags"], 4);
                    int f_offset = (int)mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["Int32 offset"], 4);
                    string field_type = get_type_fullname(mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["tysos.TysosType _FieldType"]));
                    ulong field_ti = mem.get_mem(cur_field + (ulong)fi_l.InstanceFieldOffsets["tysos.TysosType _FieldType"]);

                    if ((flags & 0x10) == 0x0)
                    {
                        // is an instance field
                        int len = get_data_length(field_type);
                        ulong val = mem.get_mem(address + (ulong)f_offset, len);
                        bool is_vt = false;

                        string field_extends_name = get_type_fullname(mem.get_mem(field_ti + (ulong)ti_l.InstanceFieldOffsets["tysos.TysosType Extends"]));
                        if (((field_extends_name == "[mscorlib]System.ValueType") && (field_type != "[mscorlib]System.Enum")) ||
                            (field_extends_name == "[mscorlib]System.EnumType"))
                            is_vt = true;

                        ulong field_size = (ulong)Program.arch.data_size;
                        if(is_vt)
                            field_size = (ulong)mem.get_mem(field_ti + (ulong)ti_l.InstanceFieldOffsets["Int32 ClassSize"], 4);

                        fields.Add(new field { name = field_name, type = field_type, value = val, is_vt = is_vt, ti_addr = field_ti, offset = f_offset, size = field_size });
                    }

                    field_pointer += (ulong)Program.arch.address_size;
                    cur_field = mem.get_mem(field_pointer);
                }

                return fields.ToArray();
            }

            return new field[] { };
        }

        internal static int get_data_length(string field_type)
        {
            if (field_type == "[mscorlib]System.Int32")
                return 4;
            else if (field_type == "[mscorlib]System.UInt32")
                return 4;
            else if (field_type == "[mscorlib]System.Int64")
                return 8;
            else if (field_type == "[mscorlib]System.UInt64")
                return 8;
            else if ((field_type == "[mscorlib]System.Int16") || (field_type == "[mscorlib]System.UInt16") ||
                (field_type == "[mscorlib]System.Char"))
                return 2;
            else if ((field_type == "[mscorlib]System.Byte") || (field_type == "[mscorlib]System.SByte"))
                return 1;
            return Program.arch.address_size;
        }

        private static string get_string(ulong addr)
        {
            ulong data = addr + (ulong)Program.arch.ass.GetStringFieldOffset(libtysila.Assembler.StringFields.data_offset);
            ulong len_addr = addr + (ulong)Program.arch.ass.GetStringFieldOffset(libtysila.Assembler.StringFields.length);

            uint len = (uint)mem.get_mem(len_addr, 4);

            StringBuilder sb = new StringBuilder();
            for (uint i = 0; i < len; i++)
                sb.Append((char)mem.get_mem(data + i * 2, 2));
            return sb.ToString();
        }

        static string get_type_fullname(ulong ti_address)
        {
            if (ti_address == 0)
                return "";

            libtysila.Layout ti_l = Program.arch.ass.GetTysosTypeLayout();
            libtysila.Layout a_l = Program.arch.ass.GetTysosAssemblyLayout();
            string name = get_string(mem.get_mem(ti_address + (ulong)ti_l.InstanceFieldOffsets["String TypeName"]));
            string ns = get_string(mem.get_mem(ti_address + (ulong)ti_l.InstanceFieldOffsets["String TypeNamespace"]));
            string assembly_name = get_string(mem.get_mem(mem.get_mem(ti_address + (ulong)ti_l.InstanceFieldOffsets["System.Reflection.Assembly _Assembly"]) +
                (ulong)a_l.InstanceFieldOffsets["String assemblyName"]));
            string full_name = "[" + assembly_name + "]" + ns + "." + name;
            return full_name;
        }

        public override string ToString()
        {
            if (value.GetType() == typeof(field[]))
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(type);
                sb.Append(" {");
                sb.Append(Environment.NewLine);

                field[] fs = (field[])value;


                for (int i = 0; i < fs.Length; i++)
                {
                    field f = fs[i];
                    sb.Append("    ");
                    sb.Append(f.name);
                    sb.Append(" = ");

                    if (f.is_vt)
                        sb.Append(f.value);
                    else
                        sb.Append(f.type);

                    try
                    {
                        sb.Append(" (0x" + ((ulong)f.value).ToString("x" + (f.size * 2).ToString()) + ")");
                    }
                    catch (Exception)
                    { }

                    if (i < (fs.Length - 1))
                        sb.Append(",");
                    sb.Append(Environment.NewLine);
                }

                sb.Append("}");
                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(value.ToString());
                try
                {
                    sb.Append(" (0x" + ((ulong)value).ToString("x16") + ")");
                }
                catch (Exception)
                { }

                return sb.ToString();
            }
        }
    }
}
