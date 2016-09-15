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
using libtysila4.cil;

namespace libtysila4.ir
{
    partial class IrGraph
    {
        static Opcode[] ldfld(cil.CilNode start, target.Target t)
        {
            /* Get field token */
            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);

            metadata.MethodSpec fs;
            metadata.TypeSpec ts;
            if (m.GetFieldDefRow(table_id, row, out ts, out fs) == false)
                throw new MissingFieldException();

            switch(start.opcode.opcode1)
            {
                case cil.Opcode.SingleOpcodes.ldsfld:
                    {
                        var sfldoffset = layout.Layout.GetFieldOffset(ts, fs, t, true);
                        var sfldlabel = ts.m.MangleType(ts) + "S";

                        var fsig = fs.m.GetIntEntry(metadata.MetadataStream.tid_Field,
                            fs.mdrow, 2);
                        var sig_idx = fs.m.GetFieldSigTypeIndex((int)fsig);
                        int ft;
                        uint ft_tok;
                        ft = fs.m.GetType(ref sig_idx, out ft_tok);
                        var ft_ct = ir.Opcode.GetCTFromType(ft);

                        var r = new Opcode
                        {
                            oc = Opcode.oc_ldlabcontents,
                            uses = new Param[]
                            {
                                new Param { t = Opcode.vl_str, str = sfldlabel, v = sfldoffset }
                            },
                            defs = new Param[]
                            {
                                new Param { t = Opcode.vl_stack, v = 0, ct = ft_ct }
                            }
                        };
                        return new Opcode[] { r };
                    }

                case cil.Opcode.SingleOpcodes.ldsflda:
                    {
                        var sfldoffset = layout.Layout.GetFieldOffset(ts, fs, t, true);
                        var sfldlabel = ts.m.MangleType(ts) + "S";

                        var r = new Opcode
                        {
                            oc = Opcode.oc_ldlabaddr,
                            uses = new Param[]
                            {
                                new Param { t = Opcode.vl_str, str = sfldlabel, v = sfldoffset }
                            },
                            defs = new Param[]
                            {
                                new Param { t = Opcode.vl_stack, v = 0, ct = ir.Opcode.ct_ref }
                            }
                        };
                        return new Opcode[] { r };
                    }

                case cil.Opcode.SingleOpcodes.ldfld:
                case cil.Opcode.SingleOpcodes.ldflda:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }

        static Opcode[] stfld(cil.CilNode start, target.Target t)
        {
            /* Get field token */
            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);

            metadata.MethodSpec fs;
            metadata.TypeSpec ts;
            if (m.GetFieldDefRow(table_id, row, out ts, out fs) == false)
                throw new MissingFieldException();

            switch (start.opcode.opcode1)
            {
                case cil.Opcode.SingleOpcodes.stsfld:
                    {
                        var sfldoffset = layout.Layout.GetFieldOffset(ts, fs, t, true);
                        var sfldlabel = ts.m.MangleType(ts) + "S";

                        var r = new Opcode
                        {
                            oc = Opcode.oc_stlabcontents,
                            uses = new Param[]
                            {
                                new Param { t = Opcode.vl_stack, v = 0 }
                            },
                            defs = new Param[]
                            {
                                new Param { t = Opcode.vl_str, str = sfldlabel, v = sfldoffset }
                            },
                        };
                        return new Opcode[] { r };
                    }

                case cil.Opcode.SingleOpcodes.stfld:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }

    }
}
