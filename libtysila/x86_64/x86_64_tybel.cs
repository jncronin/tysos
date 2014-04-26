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
using libtysila.timple;

namespace libtysila
{
    partial class x86_64_Assembler
    {
        public override IList<tybel.Node> SelectInstruction(timple.TreeNode inst, ref int next_var)
        {
            List<tybel.Node> ret;

            if (inst is TimpleLabelNode)
                return new tybel.Node[] { new tybel.LabelNode((((TimpleLabelNode)inst).Label == null) ? ("L" + ((TimpleLabelNode)inst).BlockId.ToString()) : ((TimpleLabelNode)inst).Label) };
            else if (inst is TimpleCallNode)
            {
                ret = new List<tybel.Node>();
                ChooseCallInstruction(ret, inst as TimpleCallNode, ref next_var);
                return ret;
            }
            else if (inst is TimpleBrNode)
            {
                TimpleBrNode tbn = inst as TimpleBrNode;
                ThreeAddressCode.Op op = ResolveNativeIntOp(tbn.Op);

                switch (op)
                {
                    case ThreeAddressCode.Op.ble_i4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.CMPL, ret, inst, tbn.O1, tbn.O2);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.JLE, ret, inst, vara.Label("L" + tbn.BlockTarget));
                        return ret;
                }
            }
            else if (inst is TimpleNode)
            {
                TimpleNode tn = inst as TimpleNode;
                ThreeAddressCode.Op op = ResolveNativeIntOp(tn.Op);

                switch (op)
                {
                    case ThreeAddressCode.Op.assign_i4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, tn.R, tn.O1);
                        return ret;

                    case ThreeAddressCode.Op.assign_i8:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, inst, tn.R, tn.O1);
                        return ret;

                    case ThreeAddressCode.Op.add_i4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, tn.R, tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.ADDL, ret, inst, tn.R, tn.O2);
                        return ret;

                    case ThreeAddressCode.Op.ret_i4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, vara.MachineReg(Rax), tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.LEAVE, ret, inst);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.RETN, ret, inst, vara.MachineReg(Rax));
                        return ret;

                    case ThreeAddressCode.Op.call_i4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, vara.MachineReg(Rax), tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, tn.R, vara.MachineReg(Rax));
                        return ret;

                    case ThreeAddressCode.Op.call_i8:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, vara.MachineReg(Rax), tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, inst, tn.R, vara.MachineReg(Rax));
                        return ret;

                    case ThreeAddressCode.Op.call_void:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, tn.O1);
                        return ret;

                    case ThreeAddressCode.Op.adjstack:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.ADDL, ret, inst, vara.MachineReg(Rsp), tn.O1);
                        return ret;

                    case ThreeAddressCode.Op.save:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.PUSH, ret, inst, tn.O1);
                        return ret;

                    case ThreeAddressCode.Op.restore:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.POP, ret, inst, tn.O1);
                        return ret;

                    default:
                        throw new NotImplementedException("No encoding provided for " + op.ToString());
                }
            }
            throw new NotImplementedException();
        }

        private void ChooseInstruction(x86_64.x86_64_asm.opcode op, List<tybel.Node> ret, timple.TreeNode tinst, params vara[] vars)
        {
            if (x86_64.x86_64_asm.Opcodes == null)
                x86_64.x86_64_asm.InitOpcodes(GetBitness());

            List<x86_64.x86_64_asm> opcodes = x86_64.x86_64_asm.Opcodes[op];

            foreach (x86_64.x86_64_asm opcode in opcodes)
            {
                /* Compare the variables against what we have */

                if (opcode.ops.Length != vars.Length)
                    continue;

                bool fits = true;
                for (int idx = 0; idx < opcode.ops.Length; idx++)
                {
                    if (!OpFits(vars[idx], opcode.ops[idx]))
                    {
                        fits = false;
                        break;
                    }
                }
                if (fits)
                {
                    ret.Add(new x86_64_TybelNode(tinst, opcode, vars));
                    return;
                }
            }
            throw new NotImplementedException("No valid encoding found for " + op.ToString() + " in " + tinst.ToString());
        }

        private bool OpFits(vara vara, x86_64.x86_64_asm.optype optype)
        {
            Assembler.CliType dt = vara.DataType;
            if(dt == CliType.native_int)
            {
                if(GetBitness() == Bitness.Bits64)
                    dt = CliType.int64;
                else
                    dt = CliType.int32;
            }

            switch (vara.VarType)
            {
                case libtysila.vara.vara_type.Logical:
                    switch (dt)
                    {
                        case CliType.int32:
                            if ((optype == x86_64.x86_64_asm.optype.R32) ||
                                (optype == x86_64.x86_64_asm.optype.RM32))
                                return true;
                            else
                                return false;

                        case CliType.int64:
                            if ((optype == x86_64.x86_64_asm.optype.R64) ||
                                (optype == x86_64.x86_64_asm.optype.RM64))
                                return true;
                            else
                                return false;

                    }
                    return false;

                case libtysila.vara.vara_type.ContentsOf:
                    if ((optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM64))
                        return true;
                    return false;

                case libtysila.vara.vara_type.Const:
                    switch(dt)
                    {
                        case CliType.int32:
                        case CliType.int64:
                            if((FitsSByte(vara.ConstVal) && (optype == x86_64.x86_64_asm.optype.Imm8)))
                                return true;
                            if((FitsInt16(vara.ConstVal) && (optype == x86_64.x86_64_asm.optype.Imm16)))
                                return true;
                            if((FitsInt32(vara.ConstVal) && (optype == x86_64.x86_64_asm.optype.Imm32)))
                                return true;
                            if((GetBitness() == Bitness.Bits64) && (dt == CliType.int64) && (optype == x86_64.x86_64_asm.optype.Imm64))
                                return true;
                            return false;
                    }
                    return false;

                case libtysila.vara.vara_type.Label:
                    if (optype == x86_64.x86_64_asm.optype.Rel32)
                        return true;
                    return false;

                case libtysila.vara.vara_type.MachineReg:
                    if((vara.MachineRegVal.Equals(Rax)) && ((optype == x86_64.x86_64_asm.optype.rax) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rbx)) && ((optype == x86_64.x86_64_asm.optype.rbx) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rcx)) && ((optype == x86_64.x86_64_asm.optype.rcx) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rdx)) && ((optype == x86_64.x86_64_asm.optype.rdx) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rsi)) && ((optype == x86_64.x86_64_asm.optype.rsi) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rdi)) && ((optype == x86_64.x86_64_asm.optype.rdi) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rbp)) && ((optype == x86_64.x86_64_asm.optype.rbp) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rsp)) && ((optype == x86_64.x86_64_asm.optype.rsp) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R8)) && ((optype == x86_64.x86_64_asm.optype.r8) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R9)) && ((optype == x86_64.x86_64_asm.optype.r9) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R10)) && ((optype == x86_64.x86_64_asm.optype.r10) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R11)) && ((optype == x86_64.x86_64_asm.optype.r11) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R12)) && ((optype == x86_64.x86_64_asm.optype.r12) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R13)) && ((optype == x86_64.x86_64_asm.optype.r13) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R14)) && ((optype == x86_64.x86_64_asm.optype.r14) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R15)) && ((optype == x86_64.x86_64_asm.optype.r15) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;

                    if ((vara.MachineRegVal is libasm.hardware_contentsof) && (((libasm.hardware_contentsof)vara.MachineRegVal).base_loc is libasm.x86_64_gpr) && ((optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.RM32)))
                        return true;
                                       
                    return false;
            }

            return false;
        }

        private bool OpFits(vara vara1, x86_64_TybelNode.inst_def.optype optype)
        {
            return VarToOpType(vara1).Contains(optype);
        }

        private ICollection<x86_64_TybelNode.inst_def.optype> VarToOpType(vara v)
        {
            util.Set<x86_64_TybelNode.inst_def.optype> ret = new util.Set<x86_64_TybelNode.inst_def.optype>();

            Assembler.CliType dt = v.DataType;
            if(dt == CliType.native_int)
            {
                if(GetBitness() == Bitness.Bits32)
                    dt = CliType.int32;
                else
                    dt = CliType.int64;
            }

            switch (v.VarType)
            {
                case vara.vara_type.Const:
                    switch (dt)
                    {
                        case CliType.int32:
                        case CliType.int64:
                            if (FitsSByte(v.ConstVal))
                                ret.Add(x86_64_TybelNode.inst_def.optype.Imm8);
                            if (FitsInt16(v.ConstVal))
                                ret.Add(x86_64_TybelNode.inst_def.optype.Imm16);
                            if (FitsInt32(v.ConstVal))
                                ret.Add(x86_64_TybelNode.inst_def.optype.Imm32);
                            if (dt == CliType.int64)
                                ret.Add(x86_64_TybelNode.inst_def.optype.Imm64);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case vara.vara_type.Logical:
                    switch (dt)
                    {
                        case CliType.int32:
                            ret.Add(x86_64_TybelNode.inst_def.optype.R32);
                            ret.Add(x86_64_TybelNode.inst_def.optype.RM32);
                            break;

                        case CliType.int64:
                            ret.Add(x86_64_TybelNode.inst_def.optype.R64);
                            ret.Add(x86_64_TybelNode.inst_def.optype.RM64);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case vara.vara_type.MachineReg:
                    ret.Add(x86_64_TybelNode.inst_def.optype.FixedReg);
                    break;

                case vara.vara_type.Label:
                    ret.Add(x86_64_TybelNode.inst_def.optype.Rel32);
                    switch (GetBitness())
                    {
                        case Bitness.Bits32:
                            ret.Add(x86_64_TybelNode.inst_def.optype.Imm32);
                            break;
                        case Bitness.Bits64:
                            ret.Add(x86_64_TybelNode.inst_def.optype.Imm64);
                            break;
                    }
                    break;

                case vara.vara_type.Void:
                    break;

                default:
                    throw new NotImplementedException();
            }

            return ret;
        }
    }
}
