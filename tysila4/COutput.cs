/* Copyright (C) 2017 by John Cronin
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

using metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using libtysila5.target;


namespace tysila4
{
    class COutput
    {
        internal static void WriteHeader(MetadataStream m, Target ass, string output_header,
            string output_cinit)
        {
            FileStream fs = new FileStream(output_header, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);

            StreamWriter hmsw = new StreamWriter(new MemoryStream());
            StreamWriter cmsw = new StreamWriter(new MemoryStream());

            StreamWriter oci = null;
            System.IO.FileInfo header_fi = new FileInfo(output_header);
            if (output_cinit != null)
            {
                oci = new StreamWriter(new FileStream(output_cinit, FileMode.Create, FileAccess.Write));
                oci.WriteLine("#include \"" + header_fi.Name + "\"");
                oci.WriteLine("#include <string.h>");
                oci.WriteLine("#include <stdlib.h>");
                oci.WriteLine("#include <stdint.h>");
                oci.WriteLine();
                oci.WriteLine("INTPTR Get_Symbol_Addr(const char *name);");
                oci.WriteLine();
            }

            List<string> advance_defines = new List<string>();
            List<string> external_defines = new List<string>();
            List<string> func_headers = new List<string>();

            EmitType(m.GetSimpleTypeSpec(0x1c), hmsw, cmsw, advance_defines,
                external_defines, func_headers, ass);
            EmitType(m.GetSimpleTypeSpec(0xe), hmsw, cmsw, advance_defines,
                external_defines, func_headers, ass);
            EmitArrayInit(hmsw, cmsw, func_headers, ass, m);

            for(int i = 1; i <= m.table_rows[MetadataStream.tid_CustomAttribute]; i++)
            {
                int type_tid, type_row;
                m.GetCodedIndexEntry(MetadataStream.tid_CustomAttribute,
                    i, 1, m.CustomAttributeType, out type_tid,
                    out type_row);

                MethodSpec ca_ms;
                m.GetMethodDefRow(type_tid, type_row, out ca_ms);
                var ca_ms_name = ca_ms.MangleMethod();

                if (ca_ms_name == "_ZN14libsupcs#2Edll8libsupcs22OutputCHeaderAttribute_7#2Ector_Rv_P1u1t")
                {
                    int parent_tid, parent_row;
                    m.GetCodedIndexEntry(MetadataStream.tid_CustomAttribute,
                        i, 0, m.HasCustomAttribute, out parent_tid,
                        out parent_row);

                    if(parent_tid == MetadataStream.tid_TypeDef)
                    {
                        var ts = m.GetTypeSpec(parent_tid, parent_row);

                        EmitType(ts, hmsw, cmsw, advance_defines,
                            external_defines, func_headers, ass);
                    }
                }
            }

            sw.WriteLine("#include <stdint.h>");
            sw.WriteLine();
            sw.WriteLine("#ifdef INTPTR");
            sw.WriteLine("#undef INTPTR");
            sw.WriteLine("#endif");
            sw.WriteLine("#ifdef UINTPTR");
            sw.WriteLine("#undef UINTPTR");
            sw.WriteLine("#endif");
            sw.WriteLine();
            sw.WriteLine("#define INTPTR " + ((ass.GetPointerSize() == 4) ? ass.GetCType(m.SystemInt32) : ass.GetCType(m.SystemInt64)));
            sw.WriteLine("#define UINTPTR " + ((ass.GetPointerSize() == 4) ? ass.GetCType(m.GetSimpleTypeSpec(0x09)) : ass.GetCType(m.GetSimpleTypeSpec(0x0b))));
            sw.WriteLine();
            EmitArrayType(sw, ass, m);
            foreach (string s in advance_defines)
                sw.WriteLine(s);
            sw.WriteLine();
            if (oci != null)
            {
                foreach (string s2 in func_headers)
                    sw.WriteLine(s2);
                sw.WriteLine();
            }
            hmsw.Flush();
            StreamReader hmsr = new StreamReader(hmsw.BaseStream);
            hmsr.BaseStream.Seek(0, SeekOrigin.Begin);
            string hs = hmsr.ReadLine();
            while (hs != null)
            {
                sw.WriteLine(hs);
                hs = hmsr.ReadLine();
            }

            sw.Close();

            if (oci != null)
            {
                foreach (string s in external_defines)
                    oci.WriteLine(s);
                oci.WriteLine();

                cmsw.Flush();
                StreamReader cmsr = new StreamReader(cmsw.BaseStream);
                cmsr.BaseStream.Seek(0, SeekOrigin.Begin);
                string cs = cmsr.ReadLine();
                while (cs != null)
                {
                    oci.WriteLine(cs);
                    cs = cmsr.ReadLine();
                }
                oci.Close();
            }
        }

        private static void EmitType(TypeSpec tdr, StreamWriter hmsw, StreamWriter cmsw,
            List<string> advance_defines, List<string> external_defines, List<string> header_funcs, Target ass)
        {
            //Layout l = Layout.GetTypeInfoLayout(new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(tdr, ass), type = tdr }, ass, false);
            //Layout l = tdr.GetLayout(new Signature.Param(new Token(tdr), ass).Type, ass, null);

            var tname = tdr.m.GetStringEntry(MetadataStream.tid_TypeDef,
                tdr.tdrow, 1);
            var tns = tdr.m.GetStringEntry(MetadataStream.tid_TypeDef,
                tdr.tdrow, 2);

            int next_rsvd = 0;

            int align = libtysila5.layout.Layout.GetTypeAlignment(tdr, ass, false);

            if (!tdr.IsEnum)
            {
                hmsw.WriteLine("struct " + tns + "_" + tname + " {");
                advance_defines.Add("struct " + tns + "_" + tname + ";");

                List<TypeSpec> fields = new List<TypeSpec>();
                List<string> fnames = new List<string>();
                libtysila5.layout.Layout.GetFieldOffset(tdr, null, ass, false,
                    fields, fnames);

                for (int i = 0; i < fields.Count; i++)
                {
                    int bytesize;
                    hmsw.WriteLine("    " + ass.GetCType(fields[i], out bytesize) + " " + fnames[i] + ";");

                    // Pad out to align size
                    bytesize = align - bytesize;
                    while ((bytesize % 2) != 0)
                    {
                        hmsw.WriteLine("    uint8_t __reserved" + (next_rsvd++).ToString() + ";");
                        bytesize--;
                    }
                    while ((bytesize % 4) != 0)
                    {
                        hmsw.WriteLine("    uint16_t __reserved" + (next_rsvd++).ToString() + ";");
                        bytesize -= 2;
                    }
                    while(bytesize != 0)
                    {
                        hmsw.WriteLine("    uint32_t __reserved" + (next_rsvd++).ToString() + ";");
                        bytesize -= 4;
                    }
                }


                //if (packed_structs)
                //    hmsw.WriteLine("} __attribute__((__packed__));");
                //else
                    hmsw.WriteLine("};");
            }
            else
            {
                // Identify underlying type
                var utype = tdr.UnderlyingType;
                bool needs_comma = false;
                hmsw.WriteLine("enum " + tns + "_" + tname + " {");

                var first_fdef = tdr.m.GetIntEntry(MetadataStream.tid_TypeDef,
                    tdr.tdrow, 4);
                var last_fdef = tdr.m.GetLastFieldDef(tdr.tdrow);

                for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
                {
                    // Ensure field is static
                    var flags = tdr.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 0);
                    if ((flags & 0x10) == 0x10)
                    {
                        for(int cridx = 1; cridx <= tdr.m.table_rows[MetadataStream.tid_Constant]; cridx++)
                        {
                            int crpar_tid, crpar_row;
                            tdr.m.GetCodedIndexEntry(MetadataStream.tid_Constant,
                                cridx, 1, tdr.m.HasConstant, out crpar_tid, out crpar_row);

                            if(crpar_tid == MetadataStream.tid_Field &&
                                crpar_row == fdef_row)
                            {
                                var value = (int)tdr.m.GetIntEntry(MetadataStream.tid_Constant,
                                    cridx, 2);

                                if (tdr.m.SigReadUSCompressed(ref value) != 4)
                                    throw new NotSupportedException("Constant value not int32");

                                var v = tdr.m.sh_blob.di.ReadInt(value);
                                var fname = tdr.m.GetStringEntry(MetadataStream.tid_Field,
                                    (int)fdef_row, 1);

                                if (needs_comma)
                                    hmsw.WriteLine(",");
                                hmsw.Write("    " + fname + " = " + v.ToString());
                                needs_comma = true;
                            }
                        }
                    }
                }
                hmsw.WriteLine();
                hmsw.WriteLine("};");
            }

            hmsw.WriteLine();

            //if (output_cinit != null)
            //{
                if (!tdr.IsValueType)
                {
                    string init_func = "void Init_" + tns + "_" + tname + "(struct " +
                        tns + "_" + tname + " *obj)";
                    cmsw.WriteLine(init_func);
                    header_funcs.Add(init_func + ";");
                    cmsw.WriteLine("{");

                    cmsw.WriteLine("    obj->__vtbl = Get_Symbol_Addr(\"" + tdr.MangleType() + "\");");
                    cmsw.WriteLine("    obj->__mutex_lock = 0;");

                    cmsw.WriteLine("}");
                    cmsw.WriteLine();

                    if(tdr.Equals(tdr.m.SystemString))
                        EmitStringInit(tdr, hmsw, cmsw, advance_defines, external_defines, header_funcs, ass);
                }
            //}
        }

        private static void EmitArrayInit(StreamWriter hmsw, StreamWriter cmsw, List<string> header_funcs,
            Target ass, MetadataStream m)
        {
            EmitArrayInit(m.SystemObject, "Ref", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.SystemChar, "Char", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.SystemIntPtr, "I", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.SystemInt8, "I1", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.SystemInt16, "I2", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.SystemInt32, "I4", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.SystemInt64, "I8", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.GetSimpleTypeSpec(0x19), "U", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.GetSimpleTypeSpec(0x05), "U1", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.GetSimpleTypeSpec(0x07), "U2", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.GetSimpleTypeSpec(0x09), "U4", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(m.GetSimpleTypeSpec(0x0b), "U8", hmsw, cmsw, header_funcs, ass);
        }

        private static void EmitArrayType(StreamWriter hmsw, Target ass, MetadataStream m)
        {
            hmsw.WriteLine("struct __array");
            hmsw.WriteLine("{");
            hmsw.WriteLine("    INTPTR           __vtbl;");
            hmsw.WriteLine("    int64_t          __mutex_lock;");
            hmsw.WriteLine("    INTPTR           elemtype;");
            hmsw.WriteLine("    INTPTR           lobounds;");
            hmsw.WriteLine("    INTPTR           sizes;");
            hmsw.WriteLine("    INTPTR           inner_array;");
            hmsw.WriteLine("    INTPTR           rank;");
            hmsw.WriteLine("    int32_t          elem_size;");
            //if (packed_structs)
            //    hmsw.WriteLine("} __attribute__((__packed__));");
            //else
                hmsw.WriteLine("};");
            hmsw.WriteLine();
        }

        private static void EmitArrayInit(TypeSpec ts, string tname, StreamWriter hmsw, StreamWriter cmsw,
            List<string> header_funcs, Target ass)
        {
            string typestr = ass.GetCType(ts);
            string init_func_name = "void* Create_" + tname + "_Array(struct __array **arr_obj, int32_t length)";

            /* Arrays have four pieces of memory allocated:
             * - The array superblock
             * - The lobounds array
             * - The sizes array
             * - The actual array data
             * 
             * We do not allocate the last 3, as they may need to be placed at a different virtual address
             * when relocated - let the implementation decide this
             * 
             * Code is:
             * 
             * struct __array
             * {
             *     intptr           __vtbl;
             *     int32_t          __object_id;
             *     int64_t          __mutex_lock;
             *     int32_t          rank;
             *     int32_t          elem_size;
             *     intptr           lobounds;
             *     intptr           sizes;
             *     intptr           inner_array;
             * } __attribute__((__packed__));
             * 
             * void Create_X_Array(__array **arr_obj, int32_t num_elems)
             * {
             *     *arr_obj = (__array *)malloc(sizeof(arr_obj));
             *     (*arr_obj)->rank = 1;
             * }
             */

            //int elem_size = ass.GetPackedSizeOf(new Signature.Param(baseType_Type));

            header_funcs.Add(init_func_name + ";");
            cmsw.WriteLine(init_func_name);
            cmsw.WriteLine("{");
            cmsw.WriteLine("    *arr_obj = (struct __array *)malloc(sizeof(struct __array));");
            cmsw.WriteLine("    (*arr_obj)->__vtbl = Get_Symbol_Addr(\"" + ts.SzArray.MangleType() + "\");");
            cmsw.WriteLine("    (*arr_obj)->__mutex_lock = 0;");
            cmsw.WriteLine("    (*arr_obj)->rank = 1;");
            cmsw.WriteLine("    (*arr_obj)->elem_size = sizeof(" + typestr + ");");
            cmsw.WriteLine("    (*arr_obj)->elemtype = Get_Symbol_Addr(\"" + ts.MangleType() + "\");");
            cmsw.WriteLine("    (*arr_obj)->lobounds = (INTPTR)(intptr_t)malloc(sizeof(int32_t));");
            cmsw.WriteLine("    (*arr_obj)->sizes = (INTPTR)(intptr_t)malloc(sizeof(int32_t));");
            cmsw.WriteLine("    (*arr_obj)->inner_array = (INTPTR)(intptr_t)malloc(length * sizeof(int32_t));");
            cmsw.WriteLine("    *(int32_t *)(intptr_t)((*arr_obj)->lobounds) = 0;");
            cmsw.WriteLine("    *(int32_t *)(intptr_t)((*arr_obj)->sizes) = length;");
            cmsw.WriteLine("    return((void *)(intptr_t)((*arr_obj)->inner_array));");
            cmsw.WriteLine("}");
            cmsw.WriteLine();
        }

        private static void EmitStringInit(TypeSpec tdr, StreamWriter hmsw, StreamWriter cmsw,
            List<string> advance_defines, List<string> external_defines, List<string> header_funcs, Target ass)
        {
            // Emit a string creation instruction of the form:
            // void CreateString(System_String **obj, const char *s)

            string init_func = "void CreateString(struct System_String **obj, const char *s)";
            header_funcs.Add(init_func + ";");

            cmsw.WriteLine(init_func);
            cmsw.WriteLine("{");
            cmsw.WriteLine("    int l = strlen(s);");
            cmsw.WriteLine("    int i;");
            cmsw.WriteLine("    " + ass.GetCType(tdr.m.SystemChar) + " *p;");
            cmsw.WriteLine("    *obj = (struct System_String *)malloc(sizeof(struct System_String) + l * sizeof(" +
                ass.GetCType(tdr.m.SystemChar) + "));");
            cmsw.WriteLine("    Init_System_String(*obj);");
            cmsw.WriteLine("    (*obj)->length = l;");
            cmsw.WriteLine("    p = &((*obj)->start_char);");
            //cmsw.WriteLine("    p = (" + ass.GetCType(BaseType_Type.Char) +
            //    " *)(*obj + sizeof(struct System_String));");
            cmsw.WriteLine("    for(i = 0; i < l; i++)");
            cmsw.WriteLine("        p[i] = (" + ass.GetCType(tdr.m.SystemChar) + ")s[i];");
            cmsw.WriteLine("}");
            cmsw.WriteLine();
        }
    }
}
