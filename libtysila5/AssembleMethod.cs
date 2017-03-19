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
using libtysila5.util;

namespace libtysila5
{
    public partial class libtysila
    {
        public static bool AssembleMethod(metadata.MethodSpec ms,
            binary_library.IBinaryFile bf, target.Target t,
            StringBuilder debug_passes = null)
        {
            var ts = bf.GetTextSection();
            t.bf = bf;
            t.text_section = ts;

            var csite = ms.msig;
            var mdef = ms.mdrow;
            var m = ms.m;

            // Get mangled name for defining a symbol
            List<binary_library.ISymbol> meth_syms = new List<binary_library.ISymbol>();
            var mangled_name = m.MangleMethod(ms);
            var meth_sym = bf.CreateSymbol();
            meth_sym.Name = mangled_name;
            meth_sym.ObjectType = binary_library.SymbolObjectType.Function;
            meth_sym.Offset = (ulong)ts.Data.Count;
            meth_sym.Type = binary_library.SymbolType.Global;
            ts.AddSymbol(meth_sym);
            meth_syms.Add(meth_sym);

            if(ms.aliases != null)
            {
                foreach(var alias in ms.aliases)
                {
                    var alias_sym = bf.CreateSymbol();
                    alias_sym.Name = alias;
                    alias_sym.ObjectType = binary_library.SymbolObjectType.Function;
                    alias_sym.Offset = (ulong)ts.Data.Count;
                    alias_sym.Type = binary_library.SymbolType.Global;
                    ts.AddSymbol(alias_sym);
                    meth_syms.Add(alias_sym);
                }
            }

            if (debug_passes != null)
            {
                debug_passes.Append("Assembling method ");
                debug_passes.Append(mangled_name);
                debug_passes.Append(Environment.NewLine);
                debug_passes.Append(Environment.NewLine);
            }

            // Get signature if not specified
            if (csite == 0)
            {
                csite = (int)m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                    mdef, 4);
            }

            // Get method RVA
            var rva = m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                mdef, 0);
            if (rva == 0)
                return false;

            var meth = m.GetRVA(rva);

            var flags = meth.ReadByte(0);
            int max_stack = 0;
            long code_size = 0;
            long lvar_sig_tok = 0;
            int boffset = 0;
            List<metadata.ExceptionHeader> ehdrs = null;
            bool has_exceptions = false;

            if ((flags & 0x3) == 0x2)
            {
                // Tiny header
                code_size = flags >> 2;
                max_stack = 8;
                boffset = 1;
            }
            else if ((flags & 0x3) == 0x3)
            {
                // Fat header
                uint fat_flags = meth.ReadUShort(0) & 0xfffU;
                int fat_hdr_len = (meth.ReadUShort(0) >> 12) * 4;
                max_stack = meth.ReadUShort(2);
                code_size = meth.ReadUInt(4);
                lvar_sig_tok = meth.ReadUInt(8);
                boffset = fat_hdr_len;

                if ((flags & 0x8) == 0x8)
                {
                    has_exceptions = true;

                    ehdrs = new List<metadata.ExceptionHeader>();

                    int ehdr_offset = boffset + (int)code_size;
                    ehdr_offset = util.util.align(ehdr_offset, 4);

                    while (true)
                    {
                        int kind = meth.ReadByte(ehdr_offset);

                        if ((kind & 0x1) != 0x1)
                            throw new Exception("Invalid exception header");

                        bool is_fat = false;
                        if ((kind & 0x40) == 0x40)
                            is_fat = true;

                        int data_size = meth.ReadInt(ehdr_offset);
                        data_size >>= 8;
                        if (is_fat)
                            data_size &= 0xffffff;
                        else
                            data_size &= 0xff;

                        int clause_count;
                        if (is_fat)
                            clause_count = (data_size - 4) / 24;
                        else
                            clause_count = (data_size - 4) / 12;

                        ehdr_offset += 4;
                        for(int i = 0; i < clause_count; i++)
                        {
                            var ehdr = ParseExceptionHeader(meth,
                                ref ehdr_offset, is_fat, ms);
                            ehdr.EhdrIdx = i;
                            ehdrs.Add(ehdr);
                        }

                        if ((kind & 0x80) != 0x80)
                            break;
                    }
                }
            }
            else
                throw new Exception("Invalid method header flags");

            /* Parse CIL code */
            var cil = libtysila5.cil.CilParser.ParseCIL(meth,
                ms, boffset, (int)code_size, lvar_sig_tok,
                has_exceptions, ehdrs);

            /* Allocate local vars and args */
            t.AllocateLocalVarsArgs(cil);

            /* Convert to IR */
            cil.t = t;
            ir.ConvertToIR.DoConversion(cil);

            /* Allocate registers */
            ir.AllocRegs.DoAllocation(cil);


            /* Choose instructions */
            target.ChooseInstructions.DoChoosing(cil);

            ((target.x86.x86_Assembler)cil.t).AssemblePass(cil);

            foreach (var sym in meth_syms)
                sym.Size = ts.Data.Count - (int)sym.Offset;

            return true;
        }

        private static metadata.ExceptionHeader ParseExceptionHeader(metadata.DataInterface di,
            ref int ehdr_offset, bool is_fat,
            metadata.MethodSpec ms)
        {
            metadata.ExceptionHeader ehdr = new metadata.ExceptionHeader();
            int flags = 0;
            if(is_fat)
            {
                flags = di.ReadInt(ehdr_offset);
                ehdr.TryILOffset = di.ReadInt(ehdr_offset + 4);
                ehdr.TryLength = di.ReadInt(ehdr_offset + 8);
                ehdr.HandlerILOffset = di.ReadInt(ehdr_offset + 12);
                ehdr.HandlerLength = di.ReadInt(ehdr_offset + 16);
            }
            else
            {
                flags = di.ReadShort(ehdr_offset);
                ehdr.TryILOffset = di.ReadShort(ehdr_offset + 2);
                ehdr.TryLength = di.ReadSByte(ehdr_offset + 4);
                ehdr.HandlerILOffset = di.ReadShort(ehdr_offset + 5);
                ehdr.HandlerLength = di.ReadSByte(ehdr_offset + 7);
            }

            switch(flags)
            {
                case 0:
                    ehdr.EType = metadata.ExceptionHeader.ExceptionHeaderType.Catch;
                    uint mtoken;
                    if (is_fat)
                        mtoken = di.ReadUInt(ehdr_offset + 20);
                    else
                        mtoken = di.ReadUInt(ehdr_offset + 8);

                    int table_id, row;
                    ms.m.InterpretToken(mtoken, out table_id, out row);
                    ehdr.ClassToken = ms.m.GetTypeSpec(table_id, row,
                        ms.gtparams, ms.gmparams);
                    break;
                case 1:
                    ehdr.EType = metadata.ExceptionHeader.ExceptionHeaderType.Filter;
                    if (is_fat)
                        ehdr.FilterOffset = di.ReadInt(ehdr_offset + 20);
                    else
                        ehdr.FilterOffset = di.ReadInt(ehdr_offset + 8);
                    break;
                case 2:
                    ehdr.EType = metadata.ExceptionHeader.ExceptionHeaderType.Finally;
                    break;
                case 4:
                    ehdr.EType = metadata.ExceptionHeader.ExceptionHeaderType.Fault;
                    break;
                default:
                    throw new Exception("Invalid exception type: " + flags.ToString());
            }

            if (is_fat)
                ehdr_offset += 24;
            else
                ehdr_offset += 12;

            return ehdr;
        }
    }
}
