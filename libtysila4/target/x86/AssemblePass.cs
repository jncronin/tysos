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
using libtysila4.util;

namespace libtysila4.target.x86
{
    public class Assemble
    {
        public static graph.Graph AssemblePass(graph.Graph g, Target t)
        {
            return (t as x86_Assembler).AssemblePass(g);
        }
    }

    partial class x86_Assembler
    {

        internal graph.Graph AssemblePass(graph.Graph g)
        { 
            graph.AssembledCodeGraph ret = new graph.AssembledCodeGraph();
            var Code = text_section.Data as List<byte>;

            int[] bb_starts = new int[g.LinearStream.Count];
            List<int> rel_srcs = new List<int>();
            List<int> rel_dests = new List<int>();

            ret.g = g;

            int cur_bb = -1;

            foreach(var n in g.LinearStream)
            {
                var mcn = n.c as MCNode;

                cur_bb++;
                bb_starts[cur_bb] = Code.Count;
                
                foreach(var I in mcn.all_insts)
                {
                    if (I.p.Length == 0)
                        continue;

                    switch(I.p[0].v)
                    {
                        case x86_push_rm32:
                            Code.Add(0xff);
                            Code.AddRange(ModRM(6, I.p[1].mreg));
                            break;
                        case x86_push_r32:
                            Code.Add(PlusRD(0x50, I.p[1].mreg));
                            break;
                        case x86_pop_rm32:
                            Code.Add(0x8f);
                            Code.AddRange(ModRM(0, I.p[1].mreg));
                            break;
                        case x86_pop_r32:
                            Code.Add(PlusRD(0x58, I.p[1].mreg));
                            break;
                        case x86_mov_r32_rm32:
                            Code.Add(0x8b);
                            Code.AddRange(ModRM(I.p[1].mreg, I.p[2].mreg));
                            break;
                        case x86_mov_rm32_r32:
                            Code.Add(0x89);
                            Code.AddRange(ModRM(I.p[2].mreg, I.p[1].mreg));
                            break;
                        case x86_sub_rm32_imm32:
                            Code.Add(0x81);
                            Code.AddRange(ModRM(5, I.p[1].mreg));
                            AddImm32(Code, I.p[2].v);
                            break;
                        case x86_call_rel32:
                            {
                                Code.Add(0xe8);
                                var reloc = bf.CreateRelocation();
                                reloc.DefinedIn = text_section;
                                reloc.Type = new binary_library.elf.ElfFile.Rel_386_PC32();
                                reloc.Addend = -4;
                                reloc.References = bf.CreateSymbol();
                                reloc.References.DefinedIn = null;
                                reloc.References.Name = I.p[1].str;
                                reloc.References.ObjectType = binary_library.SymbolObjectType.Function;
                                reloc.Offset = (ulong)Code.Count;
                                bf.AddRelocation(reloc);
                                AddImm32(Code, 0);
                            }
                            break;
                        case x86_cmp_rm32_r32:
                            Code.Add(0x39);
                            Code.AddRange(ModRM(I.p[2].mreg, I.p[1].mreg));
                            break;
                        case x86_cmp_r32_rm32:
                            Code.Add(0x3b);
                            Code.AddRange(ModRM(I.p[1].mreg, I.p[2].mreg));
                            break;
                        case x86_cmp_rm32_imm32:
                            if (I.p[1].mreg == r_eax)
                                Code.Add(0x3d);
                            else
                            {
                                Code.Add(0x81);
                                Code.AddRange(ModRM(7, I.p[1].mreg));
                            }
                            AddImm32(Code, I.p[2].v);
                            break;
                        case x86_set_rm32:
                            if(I.p[1].v != ir.Opcode.cc_never)
                            { 
                                Code.Add(0x0f);
                                switch (I.p[1].v)
                                {
                                    case ir.Opcode.cc_a:
                                        Code.Add(0x97);
                                        break;
                                    case ir.Opcode.cc_ae:
                                        Code.Add(0x93);
                                        break;
                                    case ir.Opcode.cc_b:
                                        Code.Add(0x92);
                                        break;
                                    case ir.Opcode.cc_be:
                                        Code.Add(0x96);
                                        break;
                                    case ir.Opcode.cc_eq:
                                        Code.Add(0x94);
                                        break;
                                    case ir.Opcode.cc_ge:
                                        Code.Add(0x9d);
                                        break;
                                    case ir.Opcode.cc_gt:
                                        Code.Add(0x9f);
                                        break;
                                    case ir.Opcode.cc_le:
                                        Code.Add(0x9e);
                                        break;
                                    case ir.Opcode.cc_lt:
                                        Code.Add(0x9c);
                                        break;
                                    case ir.Opcode.cc_ne:
                                        Code.Add(0x95);
                                        break;
                                    case ir.Opcode.cc_always:
                                        throw new NotImplementedException();
                                }
                                Code.AddRange(ModRM(2, I.p[2].mreg));
                            }
                            break;
                        case x86_movsxbd:
                            Code.Add(0x0f);
                            Code.Add(0xbe);
                            Code.AddRange(ModRM(I.p[2].mreg, I.p[1].mreg));
                            break;
                        case x86_jcc_rel32:
                            if (I.p[1].v != ir.Opcode.cc_never)
                            {
                                Code.Add(0x0f);
                                switch (I.p[1].v)
                                {
                                    case ir.Opcode.cc_a:
                                        Code.Add(0x87);
                                        break;
                                    case ir.Opcode.cc_ae:
                                        Code.Add(0x83);
                                        break;
                                    case ir.Opcode.cc_b:
                                        Code.Add(0x82);
                                        break;
                                    case ir.Opcode.cc_be:
                                        Code.Add(0x86);
                                        break;
                                    case ir.Opcode.cc_eq:
                                        Code.Add(0x84);
                                        break;
                                    case ir.Opcode.cc_ge:
                                        Code.Add(0x8d);
                                        break;
                                    case ir.Opcode.cc_gt:
                                        Code.Add(0x8f);
                                        break;
                                    case ir.Opcode.cc_le:
                                        Code.Add(0x8e);
                                        break;
                                    case ir.Opcode.cc_lt:
                                        Code.Add(0x8c);
                                        break;
                                    case ir.Opcode.cc_ne:
                                        Code.Add(0x85);
                                        break;
                                    case ir.Opcode.cc_always:
                                        throw new NotImplementedException();
                                }

                                rel_srcs.Add(Code.Count);
                                rel_dests.Add((int)I.p[2].v);
                                AddImm32(Code, 0);
                            }
                            break;
                        case x86_jmp_rel32:
                            Code.Add(0xe9);
                            rel_srcs.Add(Code.Count);
                            rel_dests.Add((int)I.p[1].v);
                            AddImm32(Code, 0);
                            break;
                        case x86_ret:
                            Code.Add(0xc3);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            // Patch up references

            for(int i = 0; i < rel_srcs.Count; i++)
            {
                var src = rel_srcs[i];
                var dest = rel_dests[i];
                var dest_offset = bb_starts[dest];
                var offset = dest_offset - src - 4;
                InsertImm32(Code, offset, src);
            }

            ret.bf = bf;
            ret.text_section = text_section;

            return ret;
        }

        private void InsertImm32(List<byte> c, int v, int offset)
        {
            c[offset] = (byte)(v & 0xff);
            c[offset + 1] = (byte)((v >> 8) & 0xff);
            c[offset + 2] = (byte)((v >> 16) & 0xff);
            c[offset + 3] = (byte)((v >> 24) & 0xff);
        }

        private void AddImm32(List<byte> c, long v)
        {
            c.Add((byte)(v & 0xff));
            c.Add((byte)((v >> 8) & 0xff));
            c.Add((byte)((v >> 16) & 0xff));
            c.Add((byte)((v >> 24) & 0xff));
        }

        private IEnumerable<byte> ModRM(Reg r, Reg rm)
        {
            int r_val = GetR(r);
            int rm_val, mod_val, disp_len, disp_val;
            GetModRM(rm, out rm_val, out mod_val, out disp_len, out disp_val);
            return ModRM(r_val, rm_val, mod_val, disp_len, disp_val);
        }

        private int GetR(Reg r)
        {
            if (r == r_eax)
                return 0;
            else if (r == r_ecx)
                return 1;
            else if (r == r_edx)
                return 2;
            else if (r == r_ebx)
                return 3;
            else if (r == r_esp)
                return 4;
            else if (r == r_ebp)
                return 5;
            else if (r == r_esi)
                return 6;
            else if (r == r_edi)
                return 7;

            throw new NotSupportedException();
        }

        private byte PlusRD(int v, Reg mreg)
        {
            if (mreg == r_eax)
                return (byte)(v + 0);
            else if (mreg == r_ecx)
                return (byte)(v + 1);
            else if (mreg == r_edx)
                return (byte)(v + 2);
            else if (mreg == r_ebx)
                return (byte)(v + 3);
            else if (mreg == r_esp)
                return (byte)(v + 4);
            else if (mreg == r_ebp)
                return (byte)(v + 5);
            else if (mreg == r_esi)
                return (byte)(v + 6);
            else if (mreg == r_edi)
                return (byte)(v + 7);

            throw new NotSupportedException();
        }

        private IEnumerable<byte> ModRM(int r, Target.Reg rm)
        {
            int rm_val, mod_val, disp_len, disp_val;
            GetModRM(rm, out rm_val, out mod_val, out disp_len, out disp_val);
            return ModRM(r, rm_val, mod_val, disp_len, disp_val);
        }

        private void GetModRM(Reg rm, out int rm_val, out int mod_val, out int disp_len, out int disp_val)
        {
            if (rm is Target.ContentsReg)
            {
                var cr = rm as Target.ContentsReg;
                if (cr.disp == 0)
                {
                    mod_val = 0;
                    disp_len = 0;
                }
                else if (cr.disp >= -128 && cr.disp < 127)
                {
                    mod_val = 1;
                    disp_len = 1;
                }
                else
                {
                    mod_val = 2;
                    disp_len = 4;
                }
                disp_val = (int)cr.disp;
                rm_val = GetRM(cr.basereg);
            }
            else
            {
                mod_val = 3;
                disp_len = 0;
                disp_val = 0;
                rm_val = GetRM(rm);
            }
        }

        private int GetMod(Target.Reg rm)
        {
            if(rm is Target.ContentsReg)
            {
                var cr = rm as Target.ContentsReg;
                if (cr.disp == 0)
                    return 0;
                else if (cr.disp >= -128 && cr.disp < 127)
                    return 1;
                else
                    return 2;
            }
            return 3;
        }

        private int GetRM(Target.Reg rm)
        {
            if(rm is Target.ContentsReg)
            {
                var cr = rm as Target.ContentsReg;
                rm = cr.basereg;
            }

            if (rm == r_eax)
                return 0;
            else if (rm == r_ecx)
                return 1;
            else if (rm == r_edx)
                return 2;
            else if (rm == r_ebx)
                return 3;
            else if (rm == r_esp)
                return 4;
            else if (rm == r_ebp)
                return 5;
            else if (rm == r_esi)
                return 6;
            else if (rm == r_edi)
                return 7;

            throw new NotSupportedException();
        }

        private IEnumerable<byte> ModRM(int r, int rm, int mod, int disp_len = 0, int disp = 0)
        {
            yield return (byte)(mod << 6 | r << 3 | rm);
            for (int i = 0; i < disp_len; i++)
                yield return (byte)(disp >> (8 * i));
        }
    }
}
