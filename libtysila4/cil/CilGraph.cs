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
using System.Linq;
using System.Text;

namespace libtysila4.cil
{
    public class CilGraph : graph.Graph
    {
        internal metadata.MetadataStream _m;
        internal int _md_row;
        internal int _mdef_sig;
        internal uint _lvar_sig_tok;

        public List<int> offset_order = new List<int>();
        public Dictionary<int, CilNode> offset_map =
                new Dictionary<int, CilNode>(new libtysila4.GenericEqualityComparer<int>());

        public static CilGraph ReadCilStream(metadata.DataInterface di,
            metadata.MethodSpec ms, int boffset, int length,
            long lvar_sig_tok, bool has_exceptions = false)
        {
            CilGraph ret = new CilGraph();
            ret._m = ms.m;
            ret._md_row = ms.mdrow;
            ret._mdef_sig = (int)ms.m.GetIntEntry((int)metadata.MetadataStream.TableId.MethodDef,
                ms.mdrow, 4);
            ret._lvar_sig_tok = (uint)lvar_sig_tok;
            ret.ms = ms;

            Dictionary<int, List<int>> offsets_before =
                new Dictionary<int, List<int>>(new GenericEqualityComparer<int>());

            // Get a list of all local vars
            if (has_exceptions == false)
            {
                int table_id;
                int row;
                ms.m.InterpretToken((uint)lvar_sig_tok,
                    out table_id, out row);
                int idx = (int)ms.m.GetIntEntry(table_id, row, 0);
                int lv_count = ms.m.GetLocalVarCount(ref idx);
                for (int i = 0; i < lv_count; i++)
                    ret.lvars_for_simplifying.set(i);
            }

            // First, generate CilNodes for each instruction
            int offset = 0;
            while (offset < length)
            {
                CilNode n = new CilNode(ms, offset);

                /* Parse prefixes */
                bool cont = true;
                while (cont)
                {
                    if (di.ReadByte(offset + boffset) == 0xfe)
                    {
                        switch (di.ReadByte(offset + boffset + 1))
                        {
                            case 0x16:
                                n.constrained = true;
                                offset += 2;
                                //line.Prefixes.constrained_tok = new Token(LSB_Assembler.FromByteArrayU4S(code, offset), m);
                                offset += 4;
                                break;
                            case 0x19:
                                if ((di.ReadByte(offset + boffset + 2) & 0x01) == 0x01)
                                    n.no_typecheck = true;
                                if ((di.ReadByte(offset + boffset + 2) & 0x02) == 0x02)
                                    n.no_rangecheck = true;
                                if ((di.ReadByte(offset + boffset + 2) & 0x04) == 0x04)
                                    n.no_nullcheck = true;
                                offset += 3;
                                break;
                            case 0x1e:
                                n.read_only = true;
                                offset += 2;
                                break;
                            case 0x14:
                                n.tail = true;
                                offset += 2;
                                break;
                            case 0x12:
                                n.unaligned = true;
                                n.unaligned_alignment = di.ReadByte(offset + boffset + 2);
                                offset += 3;
                                break;
                            case 0x13:
                                n.volatile_ = true;
                                offset += 2;
                                break;
                            default:
                                cont = false;
                                break;
                        }
                    }
                    else
                        cont = false;
                }

                /* Parse opcode */
                if (di.ReadByte(offset + boffset) == (int)Opcode.SingleOpcodes.double_)
                {
                    offset++;
                    n.opcode = OpcodeList.Opcodes[0xfe00 + di.ReadByte(offset + boffset)];
                }
                else if (di.ReadByte(offset + boffset) == (int)Opcode.SingleOpcodes.tysila)
                {
                    //if (opts.AllowTysilaOpcodes)
                    //{
                        offset++;
                        n.opcode = OpcodeList.Opcodes[0xfd00 + di.ReadByte(offset + boffset)];
                    //}
                    //else
                    //    throw new UnauthorizedAccessException("Opcodes in the range 0xfd00 - 0xfdff are not allowed in user code");
                }
                else
                    n.opcode = OpcodeList.Opcodes[di.ReadByte(offset + boffset)];
                offset++;

                /* Parse immediate operands */
                switch (n.opcode.inline)
                {
                    case Opcode.InlineVar.InlineBrTarget:
                    case Opcode.InlineVar.InlineI:
                    case Opcode.InlineVar.InlineField:
                    case Opcode.InlineVar.InlineMethod:
                    case Opcode.InlineVar.InlineSig:
                    case Opcode.InlineVar.InlineString:
                    case Opcode.InlineVar.InlineTok:
                    case Opcode.InlineVar.InlineType:
                        n.inline_int = di.ReadInt(offset + boffset);
                        n.inline_uint = di.ReadUInt(offset + boffset);
                        n.inline_long = n.inline_int;
                        n.inline_val = new byte[4];
                        for (int i = 0; i < 4; i++)
                            n.inline_val[i] = di.ReadByte(offset + boffset);
                        offset += 4;
                        break;
                    case Opcode.InlineVar.InlineI8:
                        n.inline_int = di.ReadInt(offset + boffset);
                        n.inline_uint = di.ReadUInt(offset + boffset);
                        n.inline_long = di.ReadLong(offset + boffset);
                        n.inline_val = new byte[8];
                        for (int i = 0; i < 8; i++)
                            n.inline_val[i] = di.ReadByte(offset + boffset);
                        offset += 8;
                        break;
                    case Opcode.InlineVar.InlineR:
                        //line.inline_dbl = LSB_Assembler.FromByteArrayR8S(code, offset);
                        //line.inline_val = new byte[8];
                        //LSB_Assembler.SetByteArrayS(line.inline_val, 0, code, offset, 8);
                        throw new NotImplementedException();
                        offset += 8;
                        break;
                    case Opcode.InlineVar.InlineVar:
                        //line.inline_int = LSB_Assembler.FromByteArrayI2S(code, offset);
                        //line.inline_uint = LSB_Assembler.FromByteArrayU2S(code, offset);
                        //line.inline_val = new byte[2];
                        //LSB_Assembler.SetByteArrayS(line.inline_val, 0, code, offset, 2);
                        throw new NotImplementedException();
                        offset += 2;
                        break;
                    case Opcode.InlineVar.ShortInlineBrTarget:
                    case Opcode.InlineVar.ShortInlineI:
                    case Opcode.InlineVar.ShortInlineVar:
                        n.inline_int = di.ReadSByte(offset + boffset);
                        n.inline_uint = di.ReadByte(offset + boffset);
                        n.inline_long = n.inline_int;
                        n.inline_val = new byte[1];
                        n.inline_val[0] = di.ReadByte(offset + boffset);
                        offset += 1;
                        break;
                    case Opcode.InlineVar.ShortInlineR:
                        //line.inline_sgl = LSB_Assembler.FromByteArrayR4S(code, offset);
                        //line.inline_val = new byte[4];
                        //LSB_Assembler.SetByteArrayS(line.inline_val, 0, code, offset, 4);
                        throw new NotImplementedException();
                        offset += 4;
                        break;
                    case Opcode.InlineVar.InlineSwitch:
                        uint switch_len = di.ReadUInt(offset + boffset);
                        n.inline_int = (int)switch_len;
                        n.inline_long = n.inline_int;
                        offset += 4;
                        n.inline_array = new List<int>();
                        for (uint switch_it = 0; switch_it < switch_len; switch_it++)
                        {
                            n.inline_array.Add(di.ReadInt(offset + boffset));
                            offset += 4;
                        }
                        break;
                }

                /* Determine the next instruction in the stream */
                switch (n.opcode.ctrl)
                {
                    case Opcode.ControlFlow.BRANCH:
                        n.il_offsets_after.Add(offset + n.inline_int);
                        break;

                    case Opcode.ControlFlow.COND_BRANCH:
                        if (n.opcode.opcode1 == Opcode.SingleOpcodes.switch_)
                        {
                            foreach (int jmp_target in n.inline_array)
                                n.il_offsets_after.Add(offset + jmp_target);
                            n.il_offsets_after.Add(offset);
                        }
                        else
                        {
                            n.il_offsets_after.Add(offset);
                            n.il_offsets_after.Add(offset + n.inline_int);
                        }
                        break;

                    case Opcode.ControlFlow.NEXT:
                    case Opcode.ControlFlow.CALL:
                    case Opcode.ControlFlow.BREAK:
                        n.il_offsets_after.Add(offset);
                        break;
                }

                n.il_offset_after = offset;
                ret.offset_map[n.il_offset] = n;
                ret.offset_order.Add(n.il_offset);

                // Store this node as the offset_before whatever it
                //  references
                foreach(var offset_after in n.il_offsets_after)
                {
                    List<int> after_list;
                    if(!offsets_before.TryGetValue(offset_after,
                        out after_list))
                    {
                        after_list = new List<int>();
                        offsets_before[offset_after] = after_list;
                    }
                    if (!after_list.Contains(n.il_offset))
                        after_list.Add(n.il_offset);
                }
            }

            // Now build containing nodes for each CilNode
            foreach(var il_offset in ret.offset_order)
            {
                CilNode cil = ret.offset_map[il_offset];
                graph.BaseNode n;

                if (cil.il_offsets_after.Count > 1 ||
                    (offsets_before.ContainsKey(il_offset) &&
                    offsets_before[il_offset].Count > 1))
                {
                    n = new graph.MultiNode();
                }
                else
                    n = new graph.Node();
                n.c = cil;
                n.g = ret;

                if (cil.il_offsets_after.Count == 0)
                    ret.Ends.Add(n);

                ret.LinearStream.Add(n);
            }
            
            // Now patch up edges
            foreach(var il_offset in ret.offset_order)
            {
                List<int> befores;
                var n = ret.offset_map[il_offset].n;

                if (offsets_before.TryGetValue(il_offset, out befores))
                {
                    foreach (int before in befores)
                    {
                        var bn = ret.offset_map[before].n;
                        var bcn = bn.c as CilNode;

                        // Ensure the default fall-through for conditional branches goes
                        //  first
                        if (bcn.il_offset_after == il_offset)
                            bn.SetDefaultNext(n);
                        else
                            bn.AddNext(n);

                        n.AddPrev(bn);
                    }
                }
                else
                    ret.Starts.Add(n);
            }

            // We have changed the graph - patch up the basic block info
            ret.RefreshBasicBlocks();

            return ret;
        }
    }
}
