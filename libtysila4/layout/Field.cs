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
        public static int GetFieldOffset(metadata.TypeSpec ts,
            metadata.MethodSpec fs, target.Target t, bool is_static = false)
        {
            /* Iterate through methods looking for requested
                one */
            var first_fdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 4);
            var last_fdef = ts.m.GetLastFieldDef(ts.tdrow);
            var search_field_name = fs.m.GetIntEntry(MetadataStream.tid_Field,
                fs.mdrow, 1);

            int cur_offset = 0;

            if (is_static == false && !ts.IsValueType())
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
                    // Check on name
                    var fname = ts.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 1);
                    if (MetadataStream.CompareString(ts.m, fname,
                        fs.m, search_field_name))
                    {
                        return cur_offset;
                    }

                    // Increment by type size
                    var fsig = ts.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 2);

                    var sig_idx = ts.m.GetFieldSigTypeIndex((int)fsig);
                    int ft;
                    uint ft_tok;
                    ft = ts.m.GetType(ref sig_idx, out ft_tok);
                    var ft_size = t.GetSize(ft, ft_tok);

                    cur_offset += ft_size;
                }
            }

            // Shouldn't get here
            throw new MissingFieldException();
        }

        public static int GetTypeSize(metadata.TypeSpec ts,
            target.Target t, bool is_static = false)
        {
            /* Iterate through methods looking for requested
                one */
            var first_fdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 4);
            var last_fdef = ts.m.GetLastFieldDef(ts.tdrow);

            int cur_offset = 0;

            for (uint fdef_row = first_fdef; fdef_row < last_fdef; fdef_row++)
            {
                // Ensure field is static or not as required
                var flags = ts.m.GetIntEntry(MetadataStream.tid_Field,
                    (int)fdef_row, 0);
                if (((flags & 0x10) == 0x10 && is_static == true) ||
                    ((flags & 0x10) == 0 && is_static == false))
                {
                    // Increment by type size
                    var fsig = ts.m.GetIntEntry(MetadataStream.tid_Field,
                        (int)fdef_row, 2);

                    var sig_idx = ts.m.GetFieldSigTypeIndex((int)fsig);
                    int ft;
                    uint ft_tok;
                    ft = ts.m.GetType(ref sig_idx, out ft_tok);
                    var ft_size = t.GetSize(ft, ft_tok);

                    cur_offset += ft_size;
                }
            }

            if(is_static == false && !ts.IsValueType())
            {
                // Add a vtable entry
                cur_offset += t.GetCTSize(ir.Opcode.ct_object);
            }

            return cur_offset;
        }
    }
}
