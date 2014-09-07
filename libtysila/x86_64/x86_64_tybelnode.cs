/* Copyright (C) 2014 by John Cronin
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

namespace libtysila
{
    partial class x86_64_Assembler
    {
        internal partial class x86_64_TybelNode : tybel.Node
        {
            public x86_64.x86_64_asm inst;
            public List<vara> ops;
            public override IList<vara> VarList
            {
                get { return ops; }
            }

            static Dictionary<string, inst_def> instrs;
            public static Dictionary<string, List<inst_def>> instr_choices;

            public override bool IsMove
            {
                get {
                    if (inst.is_move)
                    {
                        foreach (vara v in ops)
                        {
                            //if (v.VarType != vara.vara_type.Logical)
                            //    return false;
                        }
                        return true;
                    }
                    return false;
                }
            }

            public override bool IsUnconditionalJmp
            {
                get { return inst.is_uncond_jmp; }
            }

            public override ICollection<vara> defs
            {
                get {
                    util.Set<vara> ret = new util.Set<vara>();

                    foreach (libasm.hardware_location oput in inst.outputs)
                    {
                        if (oput is x86_64.x86_64_asm.op_loc)
                        {
                            int idx = ((x86_64.x86_64_asm.op_loc)oput).op_idx;

                            switch (ops[idx].VarType)
                            {
                                case vara.vara_type.Logical:
                                case vara.vara_type.MachineReg:
                                    ret.Add(ops[idx]);
                                    break;
                            }
                        }
                        else throw new NotImplementedException();
                    }

                    foreach (libasm.hardware_location iput in inst.inputs)
                    {
                        if (iput is x86_64.x86_64_asm.op_loc)
                        {
                            int idx = ((x86_64.x86_64_asm.op_loc)iput).op_idx;

                            switch (ops[idx].VarType)
                            {
                                case vara.vara_type.AddrOf:
                                    ret.Add(vara.Logical(ops[idx].LogicalVar, ops[idx].SSA, CliType.native_int));
                                    break;
                            }
                        }
                        else
                            throw new NotImplementedException();
                    }

                    return ret;
                }
            }

            public override ICollection<vara> uses
            {
                get {
                    util.Set<vara> ret = new util.Set<vara>();

                    foreach (libasm.hardware_location oput in inst.outputs)
                    {
                        if (oput is x86_64.x86_64_asm.op_loc)
                        {
                            int idx = ((x86_64.x86_64_asm.op_loc)oput).op_idx;

                            switch (ops[idx].VarType)
                            {
                                case vara.vara_type.AddrOf:
                                case vara.vara_type.ContentsOf:
                                    ret.Add(vara.Logical(ops[idx].LogicalVar, ops[idx].SSA, CliType.native_int));
                                    break;
                            }
                        }
                        else
                            throw new NotImplementedException();
                    }

                    foreach (libasm.hardware_location iput in inst.inputs)
                    {
                        if (iput is x86_64.x86_64_asm.op_loc)
                        {
                            int idx = ((x86_64.x86_64_asm.op_loc)iput).op_idx;

                            switch (ops[idx].VarType)
                            {
                                case vara.vara_type.Logical:
                                case vara.vara_type.MachineReg:
                                    ret.Add(ops[idx]);
                                    break;

                                case vara.vara_type.ContentsOf:
                                case vara.vara_type.AddrOf:
                                    ret.Add(vara.Logical(ops[idx].LogicalVar, ops[idx].SSA, CliType.native_int));
                                    break;
                            }
                        }
                        else
                            throw new NotImplementedException();
                    }

                    return ret;
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                
                sb.Append(inst.int_name);
                if(ops.Count > 0)
                    sb.Append(" ");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(ops[i].ToString());
                }

                return sb.ToString();
            }

            public override IEnumerable<libasm.OutputBlock> Assemble(Assembler ass, Assembler.MethodAttributes attrs)
            {
                if (inst.OverrideFunc != null)
                    return inst.OverrideFunc(inst, attrs);

                List<libasm.OutputBlock> ret = new List<libasm.OutputBlock>();
                List<byte> a = new List<byte>();

                /* opcode encoding is:
                 * 
                 * 1a)      Lock/Rep prefix
                 * 1b)      Segment override prefix
                 * 1c)      Op size prefix
                 * 1d)      Address size prefix
                 * 2)       Rex prefix
                 * 3)       Opcode bytes
                 * 4)       Mod R/M
                 * 5)       SIB
                 * 6)       Disp
                 * 7)       Imm
                 */

                /* Determine which hardware locations encode:
                 *  1) Mod R/M: r
                 *  2) Mod R/M: rm
                 *  3) Imm
                 */

                libasm.hardware_location r = null;
                libasm.hardware_location rm = null;
                object imm = null;
                int imm_len = 0;
                string rel = null;
                int rel_len = 0;
                int rel_val = 0;
                libasm.RelocationBlock.RelocationType rel_type = null;
                libasm.RelocationBlock disp_reloc = null;
                bool need_rex_40 = false;
                for (int i = 0; i < inst.ops.Length; i++)
                {
                    x86_64.x86_64_asm.optype opt = inst.ops[i];

                    switch (opt)
                    {
                        case x86_64.x86_64_asm.optype.R8:
                        case x86_64.x86_64_asm.optype.R16:
                        case x86_64.x86_64_asm.optype.R32:
                        case x86_64.x86_64_asm.optype.R64:
                        case x86_64.x86_64_asm.optype.R8163264:
                            r = ops[i].MachineRegVal;
                            break;

                        case x86_64.x86_64_asm.optype.RM8:
                        case x86_64.x86_64_asm.optype.RM16:
                        case x86_64.x86_64_asm.optype.RM32:
                        case x86_64.x86_64_asm.optype.RM64:
                        case x86_64.x86_64_asm.optype.RM8163264:
                        case x86_64.x86_64_asm.optype.RM8163264as8:
                            if (opt == x86_64.x86_64_asm.optype.RM8163264as8)
                                need_rex_40 = true;

                            switch (ops[i].VarType)
                            {
                                case vara.vara_type.MachineReg:
                                    rm = ops[i].MachineRegVal;
                                    break;

                                case vara.vara_type.Label:
                                    {
                                        // TODO: decide on the exact encoding depending on PIC/non-PIC etc
                                        rm = new libasm.hardware_contentsof { base_loc = Rbp };  // RIP
                                        disp_reloc = new libasm.RelocationBlock { RelType = ass.GetCodeToDataRelocType(), Size = 4, Target = ops[i].LabelVal, Value = (int)ops[i].Offset };
                                    }
                                    break;

                                default:
                                    throw new NotImplementedException();
                            }
                            break;

                        case x86_64.x86_64_asm.optype.Imm8:
                            imm = ops[i].ConstVal;
                            imm_len = 1;
                            break;

                        case x86_64.x86_64_asm.optype.Imm16:
                            imm = ops[i].ConstVal;
                            imm_len = 2;
                            break;

                        case x86_64.x86_64_asm.optype.Imm32:
                            switch (ops[i].VarType)
                            {
                                case vara.vara_type.Label:
                                    rel = ops[i].LabelVal;
                                    rel_len = 4;
                                    break;
                                case vara.vara_type.Const:
                                    imm = ops[i].ConstVal;
                                    imm_len = 4;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            rel_type = ass.GetCodeToDataRelocType();
                            break;

                        case x86_64.x86_64_asm.optype.Imm64:
                            switch (ops[i].VarType)
                            {
                                case vara.vara_type.Label:
                                    rel = ops[i].LabelVal;
                                    rel_len = 8;
                                    break;
                                case vara.vara_type.Const:
                                    imm = ops[i].ConstVal;
                                    imm_len = 8;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            rel_type = ass.GetCodeToDataRelocType();
                            break;
                        case x86_64.x86_64_asm.optype.Rel8:
                            rel = ops[i].LabelVal;
                            rel_len = 1;
                            break;

                        case x86_64.x86_64_asm.optype.Rel16:
                            rel = ops[i].LabelVal;
                            rel_len = 2;
                            break;

                        case x86_64.x86_64_asm.optype.Rel32:
                            rel = ops[i].LabelVal;
                            rel_len = 4;
                            rel_type = ass.GetCodeToCodeRelocType();
                            rel_val = -4;
                            break;

                        case x86_64.x86_64_asm.optype.Rel64:
                            rel = ops[i].LabelVal;
                            rel_len = 8;
                            rel_val = -8;
                            break;
                    }
                }
                if (inst.has_rm && (r == null))
                    r = new libasm.x86_64_gpr { reg = (libasm.x86_64_gpr.RegId)inst.opcode_ext };

                /* Add the prefixes */
                if (inst.grp1_prefix)
                    a.Add(inst.grp1);
                if (inst.seg_override_prefix)
                    a.Add(inst.seg_override);
                if (inst.op_size_prefix)
                    a.Add(0x66);
                if (inst.addr_size_prefix)
                    a.Add(0x67);

                /* Build and the rex prefix */
                byte rex = 0;
                
                if (inst.rex_w)
                    rex |= 0x48;

                if (need_rex_40 && rm != null && (rm is libasm.x86_64_gpr) && ((libasm.x86_64_gpr)rm).base_val >= 4)
                    rex |= 0x40;

                if (!inst.opcode_adds && r != null && (r is libasm.x86_64_gpr) && ((libasm.x86_64_gpr)r).is_extended)
                    rex |= 0x44;        // rex.r if modr/m r field is extended
                if (inst.opcode_adds && r != null && (r is libasm.x86_64_gpr) && ((libasm.x86_64_gpr)r).is_extended)
                    rex |= 0x41;        // rex.b if opcode reg field is extended
                if (!inst.opcode_adds && rm != null && (rm is libasm.x86_64_gpr) && ((libasm.x86_64_gpr)rm).is_extended)
                    rex |= 0x41;        // rex.b if modr/m r/m field is extended (with mod == 3)
                if (!inst.opcode_adds && rm != null && (rm is libasm.hardware_contentsof) && (((libasm.hardware_contentsof)rm).base_loc is libasm.x86_64_gpr) && ((libasm.x86_64_gpr)(((libasm.hardware_contentsof)rm).base_loc)).is_extended)
                    rex |= 0x41;        // rex.b if modr/m r/m field is extended (with mod != 3)
                
                if (rex != 0)
                    a.Add(rex);

                /* Opcode */
                if (inst.prefix_0f)
                    a.Add(0x0f);
                if (inst.opcode_adds)
                    a.Add((byte)(inst.pri_opcode + ((libasm.x86_64_gpr)r).base_val));
                else
                    a.Add(inst.pri_opcode);

                /* Mod R/M */
                if (inst.has_rm)
                {
                    byte modrm = 0;
                    byte modrm_r = ((libasm.x86_64_gpr)r).base_val;
                    byte modrm_mod;
                    byte modrm_rm;
                    byte[] disp = null;
                    if (rm is libasm.x86_64_gpr)
                    {
                        modrm_mod = 3;
                        modrm_rm = ((libasm.x86_64_gpr)rm).base_val;
                    }
                    else if (rm is libasm.hardware_contentsof)
                    {
                        libasm.hardware_contentsof hco = rm as libasm.hardware_contentsof;

                        modrm_rm = ((libasm.x86_64_gpr)hco.base_loc).base_val;
                        if (hco.const_offset == 0)
                            modrm_mod = 0;
                        else if (ass.FitsSByte(hco.const_offset))
                        {
                            modrm_mod = 1;
                            disp = ass.ToByteArraySignExtend(hco.const_offset, 1);
                        }
                        else
                        {
                            modrm_mod = 2;
                            disp = ass.ToByteArraySignExtend(hco.const_offset, 4);
                        }
                    }
                    else
                        throw new NotSupportedException();

                    modrm = (byte)((modrm_rm & 0x7) | ((modrm_r & 0x7) << 3) | (modrm_mod << 6));
                    a.Add(modrm);

                    /* SIB? */
                    if (modrm_rm == 4 && modrm_mod != 3)
                    {
                        // RSP + none
                        a.Add(0x24);
                    }

                    /* Add disp */
                    if (disp != null)
                        a.AddRange(disp);
                    if (disp_reloc != null)
                    {
                        ret.Add(new libasm.CodeBlock(a));
                        ret.Add(disp_reloc);
                        a = new List<byte>();
                    }
                }

                /* Add imm */
                if (imm != null)
                    a.AddRange(ass.ToByteArraySignExtend(imm, imm_len));

                ret.Add(new libasm.CodeBlock(a));

                if (rel != null)
                    ret.Add(new libasm.RelativeReference { Target = rel, Size = rel_len, Addend = rel_val, RelType = rel_type });

                return ret;
            }

            public class inst_def
            {
                public string name;
                public string int_name;
                public bool rexw;
                public enum optype { R8, R16, R32, R64, RM8, RM16, RM32, RM64, Imm8, Imm16, Imm32, Imm64, FixedReg, Rel8, Rel32 };
                public byte[] opcodes;
                public optype[] ops;
                public int fixed_r;
                public int[] dest_indices, src_indices;


                public override string ToString()
                {
                    return int_name;
                }
            }

            static x86_64_TybelNode()
            {
                instrs = new Dictionary<string, inst_def>();
                instr_choices = new Dictionary<string, List<inst_def>>();

                /* instrs["mov_r32_rm32"] = new inst_def { name = "mov", opcodes = new byte[] { 0x8b }, ops = new inst_def.optype[] { inst_def.optype.R, inst_def.optype.RM } };
                instrs["mov_rm32_r32"] = new inst_def { name = "mov", opcodes = new byte[] { 0x89 }, ops = new inst_def.optype[] { inst_def.optype.RM, inst_def.optype.R } };
                instrs["cmp_rm32_imm32"] = new inst_def { name = "cmp", opcodes = new byte[] { 0x81 }, ops = new inst_def.optype[] { inst_def.optype.RM, inst_def.optype.Imm32 }, fixed_r = 7 };
                instrs["cmp_rm32_r32"] = new inst_def { name = "cmp", opcodes = new byte[] { 0x39 }, ops = new inst_def.optype[] { inst_def.optype.RM, inst_def.optype.R } };
                instrs["cmp_r32_rm32"] = new inst_def { name = "cmp", opcodes = new byte[] { 0x3b }, ops = new inst_def.optype[] { inst_def.optype.R, inst_def.optype.RM } };
                instrs["jle_rel32"] = new inst_def { name = "jle", opcodes = new byte[] { 0x0f, 0x8e }, ops = new inst_def.optype[] { inst_def.optype.Rel32 } };
                instrs["add_rm32_imm32"] = new inst_def { name = "add", opcodes = new byte[] { 0x81 }, ops = new inst_def.optype[] { inst_def.optype.RM, inst_def.optype.Imm32 }, fixed_r = 0 };
                instrs["add_rm32_r32"] = new inst_def { name = "add", opcodes = new byte[] { 0x01 }, ops = new inst_def.optype[] { inst_def.optype.RM, inst_def.optype.R } };
                instrs["add_r32_rm32"] = new inst_def { name = "add", opcodes = new byte[] { 0x03 }, ops = new inst_def.optype[] { inst_def.optype.R, inst_def.optype.RM } };
                instrs["leave"] = new inst_def { name = "leave", opcodes = new byte[] { 0xc9 }, ops = new inst_def.optype[] { } };
                instrs["ret"] = new inst_def { name = "ret", opcodes = new byte[] { 0xc3 }, ops = new inst_def.optype[] { } }; */
            }

            public x86_64_TybelNode(timple.TreeNode tinst, x86_64.x86_64_asm op, params vara[] operands)
            {
                TimpleInst = tinst;
                inst = op;
                ops = new List<vara>(operands);
            }
        }
    }
}
