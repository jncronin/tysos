/* Copyright (C) 2008 - 2011 by John Cronin
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
using libasm;

namespace libtysila
{
    abstract partial class Assembler
    {
        // Implement a control flow graph

        public class cfg_node
        {
            public class node_regalloc_state { };
            public node_regalloc_state ra_state_at_start = null;
            public node_regalloc_state ra_state_at_end = null;

            public List<cfg_node> ipred = new List<cfg_node>();      // immediate predecessors
            public List<cfg_node> isuc = new List<cfg_node>();       // immediate successors

            public List<int> ipred_ids;                             // block ids of predecessors and successors - used when we copy a node graph to reconstitute the links
            public List<int> isuc_ids;

            public List<var> live_vars_at_start = new List<var>();
            public Dictionary<var, hardware_location> live_vars_location = new Dictionary<var,hardware_location>();
            public List<var> live_vars_at_end = new List<var>();
            public bool live_vars_done = false;
            public List<int> all_used_vars = new List<int>();

            public List<var> node_global_vars = new List<var>();

            public bool IsEntry() { if(ipred.Count == 0) return true; else return false; }
            public bool IsExit() { if(isuc.Count == 0) return true; else return false; }

            public List<InstructionLine> instrs = new List<InstructionLine>();

            public int il_offset, il_offset_after;
            public List<int> il_offsets_after;

            public int block_id;
            public bool has_fall_through = true;

            public List<PseudoStack> pstack_before, pstack_after, lv_before, lv_after, la_before, la_after;

            public List<TypeToCompile> types_whose_static_fields_are_referenced = new List<TypeToCompile>();

            public bool stack_traced = false;

            public MethodToCompile containing_meth;

            public cfg_node(int Block_id, MethodToCompile _containing_meth) { block_id = Block_id; containing_meth = _containing_meth; }

            public List<ThreeAddressCode> _tacs_prephi = new List<ThreeAddressCode>();
            public List<ThreeAddressCode> _tacs_phi = new List<ThreeAddressCode>();
            public List<ThreeAddressCode> _tacs_end = new List<ThreeAddressCode>();
            public List<ThreeAddressCode> tacs
            {
                get
                {
                    List<ThreeAddressCode> ret = new List<ThreeAddressCode>();
                    ret.AddRange(_tacs_prephi);
                    ret.AddRange(_tacs_phi);
                    foreach (InstructionLine i in instrs)
                        ret.AddRange(i.tacs);
                    ret.AddRange(_tacs_end);
                    return ret;
                }
            }
            public List<ThreeAddressCode> optimized_ir;
            public List<ThreeAddressCode> ssa_ir;
            
            public List<cfg_node> pred
            {
                get
                {
                    // Return a list of all predecessors
                    List<cfg_node> ret = new List<cfg_node>();
                    add_ipred_isuc_recurse(true, ret, this);
                    return ret;
                }
            }

            public List<cfg_node> doms;
            public cfg_node idom;

            public List<cfg_node> suc
            {
                get
                {
                    // Return a list of all successors
                    List<cfg_node> ret = new List<cfg_node>();
                    add_ipred_isuc_recurse(false, ret, this);
                    return ret;
                }
            }

            private void add_ipred_isuc_recurse(bool p, List<cfg_node> ret, cfg_node cfg_node)
            {
                // utility for generating a unique list of all predecessors or successors
                // if p is true then use predecessors, else successors

                List<cfg_node> l;
                if (p)
                    l = cfg_node.ipred;
                else
                    l = cfg_node.isuc;

                foreach (cfg_node n in l)
                {
                    if (!ret.Contains(n))
                    {
                        ret.Add(n);
                        add_ipred_isuc_recurse(p, ret, n);
                    }
                }
            }

            public override string ToString()
            {
                return "cfg_node: " + block_id.ToString();
            }
        }

        public List<cfg_node> BuildControlGraph(byte[] impl, Metadata m, AssemblerState state, MethodToCompile mtc)
        {
            // Parse implementation to a list of instructions and jump targets
            List<int> jmp_targets = new List<int>();
            List<InstructionLine> lines = new List<InstructionLine>();

            int loc = 0;
            while (loc < impl.Length)
            {
                InstructionLine line = new InstructionLine();
                if (loc == 0)
                    line.start_block = true;
                line.il_offset = loc;
                line.from_cil = true;

                bool cont = true;
                while (cont)
                {
                    if (impl[loc] == 0xfe)
                    {
                        switch (impl[loc + 1])
                        {
                            case 0x16:
                                line.Prefixes.constrained = true;
                                loc += 2;
                                line.Prefixes.constrained_tok = new Token(FromByteArrayU4(impl, loc), m);
                                loc += 4;
                                break;
                            case 0x19:
                                if ((impl[loc + 2] & 0x01) == 0x01)
                                    line.Prefixes.no_typecheck = true;
                                if ((impl[loc + 2] & 0x02) == 0x02)
                                    line.Prefixes.no_rangecheck = true;
                                if ((impl[loc + 2] & 0x04) == 0x04)
                                    line.Prefixes.no_nullcheck = true;
                                loc += 3;
                                break;
                            case 0x1e:
                                line.Prefixes.read_only = true;
                                loc += 2;
                                break;
                            case 0x14:
                                line.Prefixes.tail = true;
                                loc += 2;
                                break;
                            case 0x12:
                                line.Prefixes.unaligned = true;
                                line.Prefixes.unaligned_alignment = (int)impl[loc + 2];
                                loc += 3;
                                break;
                            case 0x13:
                                line.Prefixes.volatile_ = true;
                                loc += 2;
                                break;
                            default:
                                cont = false;
                                break;
                        }
                    }
                    else
                        cont = false;
                }

                if (impl[loc] == (int)SingleOpcodes.double_)
                {
                    loc++;
                    line.opcode = Opcodes[0xfe00 + impl[loc]];
                }
                else if (impl[loc] == (int)SingleOpcodes.tysila)
                {
                    if (state.security.AllowTysilaOpcodes)
                    {
                        loc++;
                        line.opcode = Opcodes[0xfd00 + impl[loc]];
                    }
                    else
                        throw new UnauthorizedAccessException("Opcodes in the range 0xfd00 - 0xfdff are not allowed in user code");
                }
                else
                    line.opcode = Opcodes[impl[loc]];
                loc++;

                switch (line.opcode.ctrl)
                {
                    case ControlFlow.BRANCH:
                    case ControlFlow.BREAK:
                    case ControlFlow.COND_BRANCH:
                    case ControlFlow.RETURN:
                    case ControlFlow.THROW:
                        line.end_block = true;
                        break;
                }

                switch (line.opcode.inline)
                {
                    case InlineVar.InlineBrTarget:
                    case InlineVar.InlineI:
                        line.inline_int = FromByteArrayI4(impl, loc);
                        line.inline_uint = FromByteArrayU4(impl, loc);
                        loc += 4;
                        break;
                    case InlineVar.InlineField:
                    case InlineVar.InlineMethod:
                    case InlineVar.InlineSig:
                    case InlineVar.InlineString:
                    case InlineVar.InlineTok:
                    case InlineVar.InlineType:
                        line.inline_tok = new Token(FromByteArrayU4(impl, loc), m);
                        loc += 4;
                        break;
                    case InlineVar.InlineI8:
                        line.inline_int64 = FromByteArrayI8(impl, loc);
                        loc += 8;
                        break;
                    case InlineVar.InlineR:
                        line.inline_dbl = FromByteArrayR8(impl, loc);
                        loc += 8;
                        break;
                    case InlineVar.InlineVar:
                        line.inline_int = FromByteArrayI2(impl, loc);
                        loc += 2;
                        break;
                    case InlineVar.ShortInlineBrTarget:
                    case InlineVar.ShortInlineI:
                    case InlineVar.ShortInlineVar:
                        line.inline_int = FromByteArrayI1(impl, loc);
                        line.inline_uint = FromByteArrayU1(impl, loc);
                        loc += 1;
                        break;
                    case InlineVar.ShortInlineR:
                        line.inline_sgl = FromByteArrayR4(impl, loc);
                        loc += 4;
                        break;
                    case InlineVar.InlineSwitch:
                        uint switch_len = FromByteArrayU4(impl, loc);
                        line.inline_int = (int)switch_len;
                        loc += 4;
                        for (uint switch_it = 0; switch_it < switch_len; switch_it++)
                        {
                            line.inline_array.Add(FromByteArrayI4(impl, loc));
                            loc += 4;
                        }
                        break;
                }

                line.il_offset_after = loc;

                switch (line.opcode.ctrl)
                {
                    case ControlFlow.BRANCH:
                        line.il_offsets_after.Add(line.il_offset_after + line.inline_int);
                        jmp_targets.Add(line.il_offset_after + line.inline_int);
                        break;

                    case ControlFlow.COND_BRANCH:
                        if (line.opcode.opcode1 == SingleOpcodes.switch_)
                        {
                            foreach (int jmp_target in line.inline_array)
                            {
                                line.il_offsets_after.Add(line.il_offset_after + jmp_target);
                                jmp_targets.Add(line.il_offset_after + jmp_target);
                            }
                            line.il_offsets_after.Add(line.il_offset_after);
                        }
                        else
                        {
                            line.il_offsets_after.Add(line.il_offset_after);
                            line.il_offsets_after.Add(line.il_offset_after + line.inline_int);
                            jmp_targets.Add(line.il_offset_after + line.inline_int);
                        }
                        break;

                    case ControlFlow.NEXT:
                    case ControlFlow.CALL:
                    case ControlFlow.BREAK:
                        line.il_offsets_after.Add(line.il_offset_after);
                        break;
                }

                lines.Add(line);
            }

            // Determine all instructions that start or end blocks
            for (int i = 0; i < lines.Count; i++)
            {
                if (jmp_targets.Contains(lines[i].il_offset))
                    lines[i].start_block = true;
            }
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].start_block == false)
                    if (lines[i - 1].end_block == true)
                        lines[i].start_block = true;
                if (lines[i].end_block == false)
                    if (lines[i + 1].start_block == true)
                        lines[i].end_block = true;
            }

            List<cfg_node> nodes = new List<cfg_node>();
            cfg_node node = new cfg_node(state.next_block++, mtc);

            // Build a control graph
            for (int i = 0; i < lines.Count; i++)
            {
                if (node.instrs.Count == 0)
                {
                    node.il_offset = lines[i].il_offset;
                }
                node.instrs.Add(lines[i]);
                if (lines[i].end_block)
                {
                    node.il_offset_after = lines[i].il_offset_after;
                    node.il_offsets_after = lines[i].il_offsets_after;
                    nodes.Add(node);
                    node = new cfg_node(state.next_block++, mtc);
                }

            }
            // Patch up successors and predecessors
            for (int i = 0; i < nodes.Count; i++)
            {
                foreach (int il_after in nodes[i].il_offsets_after)
                {
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if (il_after == nodes[j].il_offset)
                        {
                            nodes[i].isuc.Add(nodes[j]);
                            nodes[j].ipred.Add(nodes[i]);
                        }
                    }
                }
            }

            // Add in instruction labels
            foreach (cfg_node cur_node in nodes)
            {
                for (int i = 0; i < cur_node.instrs.Count; i += 2)
                    cur_node.instrs.Insert(i, new InstructionLabel(this, cur_node.instrs[i]));
            }

            return nodes;
        }
    }
}
