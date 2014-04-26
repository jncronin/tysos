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
        partial class x86_64_TybelNode : tybel.Node
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

            public override IList<byte> Assemble()
            {
                throw new NotImplementedException();
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
