/* Copyright (C) 2008 - 2014 by John Cronin
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

namespace libtysila.frontend.cil
{
    public class CilGraph : timple.BaseGraph
    {
        public static CilGraph BuildGraph(Metadata.MethodBody method, Metadata m, Assembler.AssemblerOptions opts)
        {
            CilGraph ret = new CilGraph();

            /* First, generate a mapping between offset and CilNodes */
            Dictionary<int, CilNode> offset_map = new Dictionary<int, CilNode>();
            List<int> starts = new List<int>();
            int offset = 0;

            /* Add start nodes: first instruction and any exception clause */
            starts.Add(0);
            if (method.exceptions != null)
            {
                foreach (Metadata.MethodBody.EHClause ehclause in method.exceptions)
                {
                    starts.Add((int)ehclause.HandlerOffset);
                }
            }

            /* Parse method body */
            ParseCode(method.Body, ref offset, offset_map, m, opts);
           
            /* Add the instructions to the graph */
            util.Set<int> visited = new util.Set<int>();
            foreach (int start in starts)
                DFAdd(ret, start, null, offset_map, visited);

            /* Add exception handler start node hints */
            if (method.exceptions != null)
            {
                foreach (Metadata.MethodBody.EHClause ehclause in method.exceptions)
                    offset_map[(int)ehclause.HandlerOffset].ehclause_start = ehclause;
            }

            /* Build our linear stream in the original order of the IL */
            foreach (CilNode n in offset_map.Values)
                ret.linear_stream.Add(n);

            return ret;
        }

        internal List<timple.BaseNode> linear_stream = new List<timple.BaseNode>();
        public override IList<timple.BaseNode> LinearStream
        {
            get
            {
                return linear_stream;
            }
        }

        public override int Count
        {
            get
            {
                return linear_stream.Count;
            }
        }

        static void DFAdd(CilGraph ret, int il_offset, CilNode parent, Dictionary<int, CilNode> offset_map, util.Set<int> visited)
        {
            if (visited.Contains(il_offset))
                return;
            visited.Add(il_offset);

            CilNode n = offset_map[il_offset];
            if (parent == null)
                ret.Starts.Add(n);

            foreach (int next_il in n.il.il_offsets_after)
            {
                CilNode next = offset_map[next_il];
                n.Next.Add(next);
                next.Prev.Add(n);
                DFAdd(ret, next_il, n, offset_map, visited);
            }
        }

        public override void Add(timple.BaseNode n)
        {
            base.Add(n);
            if (!(n is CilNode))
                throw new Exception("Can only add CilNodes to a CilGraph");
            linear_stream.Add(n);
        }

        static void ParseCode(IList<byte> code, ref int base_offset, Dictionary<int, CilNode> offset_map, Metadata m, Assembler.AssemblerOptions opts)
        {
            int offset = 0;
            while (offset < code.Count)
            {
                InstructionLine line = new InstructionLine();
                line.from_cil = true;
                line.il_offset = offset + base_offset;

                /* Parse prefixes */
                bool cont = true;
                while (cont)
                {
                    if (code[offset] == 0xfe)
                    {
                        switch (code[offset + 1])
                        {
                            case 0x16:
                                line.Prefixes.constrained = true;
                                offset += 2;
                                line.Prefixes.constrained_tok = new Token(LSB_Assembler.FromByteArrayU4S(code, offset), m);
                                offset += 4;
                                break;
                            case 0x19:
                                if ((code[offset + 2] & 0x01) == 0x01)
                                    line.Prefixes.no_typecheck = true;
                                if ((code[offset + 2] & 0x02) == 0x02)
                                    line.Prefixes.no_rangecheck = true;
                                if ((code[offset + 2] & 0x04) == 0x04)
                                    line.Prefixes.no_nullcheck = true;
                                offset += 3;
                                break;
                            case 0x1e:
                                line.Prefixes.read_only = true;
                                offset += 2;
                                break;
                            case 0x14:
                                line.Prefixes.tail = true;
                                offset += 2;
                                break;
                            case 0x12:
                                line.Prefixes.unaligned = true;
                                line.Prefixes.unaligned_alignment = (int)code[offset + 2];
                                offset += 3;
                                break;
                            case 0x13:
                                line.Prefixes.volatile_ = true;
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
                if (code[offset] == (int)Opcode.SingleOpcodes.double_)
                {
                    offset++;
                    line.opcode = OpcodeList.Opcodes[0xfe00 + code[offset]];
                }
                else if (code[offset] == (int)Opcode.SingleOpcodes.tysila)
                {
                    if (opts.AllowTysilaOpcodes)
                    {
                        offset++;
                        line.opcode = OpcodeList.Opcodes[0xfd00 + code[offset]];
                    }
                    else
                        throw new UnauthorizedAccessException("Opcodes in the range 0xfd00 - 0xfdff are not allowed in user code");
                }
                else
                    line.opcode = OpcodeList.Opcodes[code[offset]];
                offset++;

                /* Parse immediate operands */
                switch (line.opcode.inline)
                {
                    case Opcode.InlineVar.InlineBrTarget:
                    case Opcode.InlineVar.InlineI:
                        line.inline_int = LSB_Assembler.FromByteArrayI4S(code, offset);
                        line.inline_uint = LSB_Assembler.FromByteArrayU4S(code, offset);
                        offset += 4;
                        break;
                    case Opcode.InlineVar.InlineField:
                    case Opcode.InlineVar.InlineMethod:
                    case Opcode.InlineVar.InlineSig:
                    case Opcode.InlineVar.InlineString:
                    case Opcode.InlineVar.InlineTok:
                    case Opcode.InlineVar.InlineType:
                        line.inline_tok = new Token(LSB_Assembler.FromByteArrayU4S(code, offset), m);
                        offset += 4;
                        break;
                    case Opcode.InlineVar.InlineI8:
                        line.inline_int64 = LSB_Assembler.FromByteArrayI8S(code, offset);
                        offset += 8;
                        break;
                    case Opcode.InlineVar.InlineR:
                        line.inline_dbl = LSB_Assembler.FromByteArrayR8S(code, offset);
                        offset += 8;
                        break;
                    case Opcode.InlineVar.InlineVar:
                        line.inline_int = LSB_Assembler.FromByteArrayI2S(code, offset);
                        offset += 2;
                        break;
                    case Opcode.InlineVar.ShortInlineBrTarget:
                    case Opcode.InlineVar.ShortInlineI:
                    case Opcode.InlineVar.ShortInlineVar:
                        line.inline_int = LSB_Assembler.FromByteArrayI1S(code, offset);
                        line.inline_uint = LSB_Assembler.FromByteArrayU1S(code, offset);
                        offset += 1;
                        break;
                    case Opcode.InlineVar.ShortInlineR:
                        line.inline_sgl = LSB_Assembler.FromByteArrayR4S(code, offset);
                        offset += 4;
                        break;
                    case Opcode.InlineVar.InlineSwitch:
                        uint switch_len = LSB_Assembler.FromByteArrayU4S(code, offset);
                        line.inline_int = (int)switch_len;
                        offset += 4;
                        for (uint switch_it = 0; switch_it < switch_len; switch_it++)
                        {
                            line.inline_array.Add(LSB_Assembler.FromByteArrayI4S(code, offset));
                            offset += 4;
                        }
                        break;
                }

                /* Determine the next instruction in the stream */
                switch (line.opcode.ctrl)
                {
                    case Opcode.ControlFlow.BRANCH:
                        line.il_offsets_after.Add(offset + base_offset + line.inline_int);
                        break;

                    case Opcode.ControlFlow.COND_BRANCH:
                        if (line.opcode.opcode1 == Opcode.SingleOpcodes.switch_)
                        {
                            foreach (int jmp_target in line.inline_array)
                                line.il_offsets_after.Add(offset + base_offset + jmp_target);
                            line.il_offsets_after.Add(offset);
                        }
                        else
                        {
                            line.il_offsets_after.Add(offset + base_offset);
                            line.il_offsets_after.Add(offset + base_offset + line.inline_int);
                        }
                        break;

                    case Opcode.ControlFlow.NEXT:
                    case Opcode.ControlFlow.CALL:
                    case Opcode.ControlFlow.BREAK:
                        line.il_offsets_after.Add(offset + base_offset);
                        break;
                }

                line.il_offset_after = offset + base_offset;
                offset_map[line.il_offset] = new CilNode { il = line };
            }

            base_offset += offset;
        }
    }
}
