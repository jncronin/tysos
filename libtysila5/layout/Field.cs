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

namespace libtysila5.layout
{
    public partial class Layout
    {
        public static int GetFieldOffset(metadata.TypeSpec ts,
            metadata.MethodSpec fs, target.Target t, bool is_static = false)
        {
            /* Iterate through methods looking for requested
                one */
            var first_fdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 4);
            var last_fdef = ts.m.GetLastFieldDef(ts.tdrow);

            uint search_field_name = 0;
            if (fs != null)
            {
                search_field_name = fs.m.GetIntEntry(MetadataStream.tid_Field,
                    fs.mdrow, 1);
            }

            int cur_offset = 0;

            if (is_static == false && !ts.IsValueType)
            {
                // Add a vtable entry
                cur_offset += t.GetCTSize(ir.Opcode.ct_object);
            }

            for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                // Ensure field is static if requested
                var flags = ts.m.GetIntEntry(MetadataStream.tid_Field,
                    (int)fdef_row, 0);
                if (((flags & 0x10) == 0x10 && is_static == true) ||
                    ((flags & 0x10) == 0 && is_static == false))
                {
                    // Check on name if we are looking for a particular field
                    if (search_field_name != 0)
                    {
                        var fname = ts.m.GetIntEntry(MetadataStream.tid_Field,
                            (int)fdef_row, 1);
                        if (MetadataStream.CompareString(ts.m, fname,
                            fs.m, search_field_name))
                        {
                            return cur_offset;
                        }
                    }

                    // Increment by type size
                    var fsig = (int)ts.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 2);

                    var ft = ts.m.GetFieldType(ref fsig, ts.gtparams, null);
                    var ft_size = t.GetSize(ft);

                    cur_offset += ft_size;
                }
            }

            // Shouldn't get here if looking for a specific field
            if(search_field_name != 0)
                throw new MissingFieldException();

            // Else return size of complete type
            return cur_offset;
        }

        public static int GetFieldOffset(metadata.TypeSpec ts,
            string fname, target.Target t, bool is_static = false)
        {
            /* Iterate through methods looking for requested
                one */
            var first_fdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 4);
            var last_fdef = ts.m.GetLastFieldDef(ts.tdrow);

            int cur_offset = 0;

            if (is_static == false && !ts.IsValueType)
            {
                // Add a vtable entry
                cur_offset += t.GetCTSize(ir.Opcode.ct_object);
            }

            for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                // Ensure field is static if requested
                var flags = ts.m.GetIntEntry(MetadataStream.tid_Field,
                    (int)fdef_row, 0);
                if (((flags & 0x10) == 0x10 && is_static == true) ||
                    ((flags & 0x10) == 0 && is_static == false))
                {
                    // Check on name if we are looking for a particular field
                    if (fname != null)
                    {
                        var ffname = ts.m.GetIntEntry(MetadataStream.tid_Field,
                            (int)fdef_row, 1);
                        if (MetadataStream.CompareString(ts.m, ffname, fname))
                        {
                            return cur_offset;
                        }
                    }

                    // Increment by type size
                    var fsig = (int)ts.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 2);

                    var ft = ts.m.GetFieldType(ref fsig, ts.gtparams, null);
                    var ft_size = t.GetSize(ft);

                    cur_offset += ft_size;
                }
            }

            // Shouldn't get here if looking for a specific field
            if (fname != null)
                throw new MissingFieldException();

            // Else return size of complete type
            return cur_offset;
        }


        public static int GetTypeSize(metadata.TypeSpec ts,
            target.Target t, bool is_static = false)
        {
            switch(ts.stype)
            {
                case TypeSpec.SpecialType.None:
                    if(ts.Equals(ts.m.SystemRuntimeTypeHandle) ||
                        ts.Equals(ts.m.SystemRuntimeMethodHandle) ||
                        ts.Equals(ts.m.SystemRuntimeFieldHandle))
                    {
                        return is_static ? 0 : (t.GetPointerSize());
                    }
                    if (ts.m.classlayouts[ts.tdrow] != 0)
                    {
                        var size = ts.m.GetIntEntry(metadata.MetadataStream.tid_ClassLayout,
                            ts.m.classlayouts[ts.tdrow],
                            1);
                        return (int)size;
                    }
                    return GetFieldOffset(ts, (string)null, t, is_static);
                case TypeSpec.SpecialType.SzArray:
                    if (is_static)
                        return 0;
                    return GetArrayObjectSize(t);
                default:
                    throw new NotImplementedException();
            }
        }

        public static void OutputStaticFields(metadata.TypeSpec ts,
            target.Target t, binary_library.IBinaryFile of)
        {
            var os = of.GetDataSection();
            os.Align(t.GetCTSize(ir.Opcode.ct_object));

            ulong offset = (ulong)os.Data.Count;

            int cur_offset = 0;

            /* Iterate through methods looking for requested
                one */
            var first_fdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 4);
            var last_fdef = ts.m.GetLastFieldDef(ts.tdrow);

            for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                // Ensure field is static or not as required
                var flags = ts.m.GetIntEntry(MetadataStream.tid_Field,
                    (int)fdef_row, 0);
                if ((flags & 0x10) == 0x10)
                {
                    // Increment by type size
                    var fsig = (int)ts.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 2);

                    var ft = ts.m.GetFieldType(ref fsig, ts.gtparams, null);
                    var ft_size = t.GetSize(ft);

                    /* See if there is any data defined as an rva */
                    var rva = ts.m.fieldrvas[(int)fdef_row];
                    if (rva != 0)
                    {
                        var rrva = (int)ts.m.ResolveRVA(rva);
                        for (int i = 0; i < ft_size; i++)
                            os.Data.Add(ts.m.file.ReadByte(rrva++));
                    }
                    else
                    {
                        for (int i = 0; i < ft_size; i++)
                            os.Data.Add(0);
                    }

                    cur_offset += ft_size;
                }
            }

            if (cur_offset > 0)
            {
                /* Add symbol */

                var sym = of.CreateSymbol();
                sym.Name = ts.m.MangleType(ts) + "S";
                sym.DefinedIn = os;
                sym.Offset = offset;
                sym.Type = binary_library.SymbolType.Global;
                sym.ObjectType = binary_library.SymbolObjectType.Object;
                os.AddSymbol(sym);

                sym.Size = os.Data.Count - (int)offset;
            }
        }
    }
}
