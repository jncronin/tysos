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
        public override IList<tybel.Node> SelectInstruction(timple.TreeNode inst, ref int next_var, ref int next_block, 
            IList<libasm.hardware_location> las, IList<libasm.hardware_location> lvs)
        {
            List<tybel.Node> ret;
            bool success = true;

            if (inst is TimpleLabelNode)
                return new tybel.Node[] { new tybel.LabelNode((((TimpleLabelNode)inst).Label == null) ? ("L" + ((TimpleLabelNode)inst).BlockId.ToString()) : ((TimpleLabelNode)inst).Label, ((TimpleLabelNode)inst).Label == null) };
            else if (inst is TimpleCallNode)
            {
                ret = new List<tybel.Node>();
                ChooseCallInstruction(ret, inst as TimpleCallNode, ref next_var, ref next_block, las, lvs);
                if(success) return ret;
            }
            else if (inst is TimpleBrNode)
            {
                TimpleBrNode tbn = inst as TimpleBrNode;
                ThreeAddressCode.Op op = ResolveNativeIntOp(tbn.Op);
                tbn.Op = op;

                switch (op.Operator)
                {
                    case ThreeAddressCode.OpName.beq:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.CMPL, x86_64.x86_64_asm.opcode.CMPQ), ret, inst, ref next_var, ref success, tbn.O1, tbn.O2);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.JZ, ret, inst, ref next_var, ref success, vara.Label("L" + tbn.BlockTargetTrue, false));
                        if(success) return ret; break;

                    case ThreeAddressCode.OpName.ble:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.CMPL, x86_64.x86_64_asm.opcode.CMPQ), ret, inst, ref next_var, ref success, tbn.O1, tbn.O2);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.JLE, ret, inst, ref next_var, ref success, vara.Label("L" + tbn.BlockTargetTrue, false));
                        if(success) return ret; break;

                    case ThreeAddressCode.OpName.bne:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.CMPL, x86_64.x86_64_asm.opcode.CMPQ), ret, inst, ref next_var, ref success, tbn.O1, tbn.O2);
                        ChooseInstruction(x86_64.x86_64_asm.opcode.JNZ, ret, inst, ref next_var, ref success, vara.Label("L" + tbn.BlockTargetTrue, false));
                        if(success) return ret; break;

                    case ThreeAddressCode.OpName.br:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.JMP, ret, inst, ref next_var, ref success, vara.Label("L" + tbn.BlockTargetTrue, false));
                        if(success) return ret; break;

                    default:
                        throw new NotImplementedException("No encoding provided for " + op.ToString());
                }
            }
            else if (inst is TimpleThrowBrNode)
            {
                TimpleThrowBrNode tn = inst as TimpleThrowBrNode;
                ThreeAddressCode.Op op = ResolveNativeIntOp(tn.Op);
                tn.Op = op;

                x86_64.x86_64_asm.opcode jmp_op;
                int blk_id = next_block++;

                switch (op.Operator)
                {
                    case ThreeAddressCode.OpName.throwge_un:
                        jmp_op = x86_64.x86_64_asm.opcode.JB;
                        break;
                    case ThreeAddressCode.OpName.throwg_un:
                        jmp_op = x86_64.x86_64_asm.opcode.JBE;
                        break;
                    case ThreeAddressCode.OpName.throweq:
                        jmp_op = x86_64.x86_64_asm.opcode.JNZ;
                        break;
                    case ThreeAddressCode.OpName.throwne:
                        jmp_op = x86_64.x86_64_asm.opcode.JZ;
                        break;
                    default:
                        throw new NotImplementedException("No encoding provided for " + tn.Op.ToString());
                }

                ret = new List<tybel.Node>();
                ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.CMPL, x86_64.x86_64_asm.opcode.CMPQ), ret, inst, ref next_var, ref success, tn.O1, tn.O2);
                ChooseInstruction(jmp_op, ret, inst, ref next_var, ref success, vara.Label("L" + blk_id.ToString(), false));

                TimpleCallNode tcn = new TimpleCallNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call), vara.Void(), tn.ThrowTarget, new vara[] { tn.ThrowObj }, msig_throw);
                ChooseCallInstruction(ret, tcn, ref next_var, ref next_block, las, lvs);

                ret.Add(new tybel.LabelNode("L" + blk_id.ToString(), true));

                if (success) return ret;
            }
            else if (inst is TimpleNode)
            {
                TimpleNode tn = inst as TimpleNode;
                ThreeAddressCode.Op op = ResolveNativeIntOp(tn.Op);
                tn.Op = op;

                switch (op.Operator)
                {
                    case ThreeAddressCode.OpName.assign:
                        ret = new List<tybel.Node>();
                        if (tn.O1.VarType == vara.vara_type.AddrOf)
                            ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.LEAL, x86_64.x86_64_asm.opcode.LEAQ), ret, inst, ref next_var, ref success, tn.R, tn.O1.GetLogicalVar());
                        else
                            ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVL, x86_64.x86_64_asm.opcode.MOVQ), ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.add:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVLx, x86_64.x86_64_asm.opcode.MOVQx), ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.ADDL, x86_64.x86_64_asm.opcode.ADDQ), ret, inst, ref next_var, ref success, tn.R, tn.O2);
                        if (success) return new List<tybel.Node> { new tybel.MultipleNode(ret, new vara[] { tn.O1, tn.O2 }, new vara[] { tn.R }) }; break;

                    case ThreeAddressCode.OpName.alloca:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetBitnessOpcode(GetBitness(), x86_64.x86_64_asm.opcode.SUBL, x86_64.x86_64_asm.opcode.SUBQ), ret, inst, ref next_var, ref success, vara.MachineReg(Rsp), vara.Const(util.align((int)tn.O1.ConstVal, GetSizeOfPointer()), CliType.native_int));
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVL, x86_64.x86_64_asm.opcode.MOVQ), ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(Rsp));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.and:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVLx, x86_64.x86_64_asm.opcode.MOVQx), ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.ANDL, x86_64.x86_64_asm.opcode.ANDQ), ret, inst, ref next_var, ref success, tn.R, tn.O2);
                        if (success) return new List<tybel.Node> { new tybel.MultipleNode(ret, new vara[] { tn.O1, tn.O2 }, new vara[] { tn.R }) }; break;

                    case ThreeAddressCode.OpName.or:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVLx, x86_64.x86_64_asm.opcode.MOVQx), ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.ORL, x86_64.x86_64_asm.opcode.ORQ), ret, inst, ref next_var, ref success, tn.R, tn.O2);
                        if (success) return new List<tybel.Node> { new tybel.MultipleNode(ret, new vara[] { tn.O1, tn.O2 }, new vara[] { tn.R }) }; break;

                    case ThreeAddressCode.OpName.xor:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVLx, x86_64.x86_64_asm.opcode.MOVQx), ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.XORL, x86_64.x86_64_asm.opcode.XORQ), ret, inst, ref next_var, ref success, tn.R, tn.O2);
                        if (success) return new List<tybel.Node> { new tybel.MultipleNode(ret, new vara[] { tn.O1, tn.O2 }, new vara[] { tn.R }) }; break;

                    case ThreeAddressCode.OpName.ret:
                        switch (op.Type)
                        {
                            case CliType.int32:
                                ret = new List<tybel.Node>();
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, ref next_var, ref success, vara.MachineReg(Rax), tn.O1);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.LEAVE, ret, inst, ref next_var, ref success);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.RETN, ret, inst, ref next_var, ref success, vara.MachineReg(Rax));
                                if (success) return ret; break;

                            case CliType.int64:
                                ret = new List<tybel.Node>();
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, inst, ref next_var, ref success, vara.MachineReg(Rax), tn.O1);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.LEAVE, ret, inst, ref next_var, ref success);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.RETN, ret, inst, ref next_var, ref success, vara.MachineReg(Rax));
                                if (success) return ret; break;

                            case CliType.void_:
                                ret = new List<tybel.Node>();
                                ChooseInstruction(x86_64.x86_64_asm.opcode.LEAVE, ret, inst, ref next_var, ref success);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.RETN, ret, inst, ref next_var, ref success);
                                if (success) return ret; break;
                        }
                        break;

                    case ThreeAddressCode.OpName.call:
                        switch (op.Type)
                        {
                            case CliType.int32:
                                ret = new List<tybel.Node>();
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, ref next_var, ref success, vara.MachineReg(Rax), tn.O1);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(Rax));
                                if (success) return ret; break;

                            case CliType.int64:
                                ret = new List<tybel.Node>();
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, ref next_var, ref success, vara.MachineReg(Rax), tn.O1);
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(Rax));
                                if (success) return ret; break;

                            case CliType.void_:
                                ret = new List<tybel.Node>();
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, ref next_var, ref success, tn.O1);
                                if (success) return ret; break;
                        }
                        break;

                    case ThreeAddressCode.OpName.enter:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.PUSH, ret, inst, ref next_var, ref success, vara.MachineReg(Rbp));
                        ChooseInstruction(x86_64.x86_64_asm.GetBitnessOpcode(GetBitness(), x86_64.x86_64_asm.opcode.MOVL, x86_64.x86_64_asm.opcode.MOVQ), ret, inst, ref next_var, ref success, vara.MachineReg(Rbp), vara.MachineReg(Rsp));
                        ChooseInstruction(x86_64.x86_64_asm.opcode.METHPREFIX, ret, inst, ref next_var, ref success);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.adjstack:
                        ret = new List<tybel.Node>();
                        if ((int)tn.O1.ConstVal < 0)
                            ChooseInstruction(x86_64.x86_64_asm.GetBitnessOpcode(GetBitness(), x86_64.x86_64_asm.opcode.SUBL, x86_64.x86_64_asm.opcode.SUBQ), ret, inst, ref next_var, ref success, vara.MachineReg(Rsp), vara.Const(-((int)tn.O1.ConstVal), tn.O1.DataType));
                        else
                            ChooseInstruction(x86_64.x86_64_asm.GetBitnessOpcode(GetBitness(), x86_64.x86_64_asm.opcode.ADDL, x86_64.x86_64_asm.opcode.ADDQ), ret, inst, ref next_var, ref success, vara.MachineReg(Rsp), tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.save:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.PUSH, ret, inst, ref next_var, ref success, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.restore:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.POP, ret, inst, ref next_var, ref success, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.la_load:
                        ret = new List<tybel.Node>();
                        switch (tn.R.DataType)
                        {
                            case CliType.int32:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVLx, ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(las[(int)tn.O1.ConstVal]));
                                break;
                            case CliType.int64:
                            case CliType.native_int:
                            case CliType.O:
                            case CliType.reference:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQx, ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(las[(int)tn.O1.ConstVal]));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.la_store:
                        ret = new List<tybel.Node>();
                        switch (tn.O2.DataType)
                        {
                            case CliType.int32:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVLx, ret, inst, ref next_var, ref success, vara.MachineReg(las[(int)tn.O1.ConstVal]), tn.O2);
                                break;
                            case CliType.int64:
                            case CliType.native_int:
                            case CliType.O:
                            case CliType.reference:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQx, ret, inst, ref next_var, ref success, vara.MachineReg(las[(int)tn.O1.ConstVal]), tn.O2);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.lv_load:
                        ret = new List<tybel.Node>();
                        switch (tn.R.DataType)
                        {
                            case CliType.int32:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVLx, ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(lvs[(int)tn.O1.ConstVal]));
                                break;
                            case CliType.int64:
                            case CliType.native_int:
                            case CliType.O:
                            case CliType.reference:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQx, ret, inst, ref next_var, ref success, tn.R, vara.MachineReg(lvs[(int)tn.O1.ConstVal]));
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.lv_store:
                        ret = new List<tybel.Node>();
                        switch (tn.O2.DataType)
                        {
                            case CliType.int32:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVLx, ret, inst, ref next_var, ref success, vara.MachineReg(lvs[(int)tn.O1.ConstVal]), tn.O2);
                                break;
                            case CliType.int64:
                            case CliType.native_int:
                            case CliType.O:
                            case CliType.reference:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQx, ret, inst, ref next_var, ref success, vara.MachineReg(lvs[(int)tn.O1.ConstVal]), tn.O2);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.peek_i1:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXB, ret, inst, ref next_var, ref success, tn.R, vara.ContentsOf(tn.O1, CliType.int32));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.peek_i2:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXW, ret, inst, ref next_var, ref success, tn.R, vara.ContentsOf(tn.O1, CliType.int32));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.peek_u1:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXB, ret, inst, ref next_var, ref success, tn.R, vara.ContentsOf(tn.O1, CliType.int32));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.peek_u2:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXW, ret, inst, ref next_var, ref success, tn.R, vara.ContentsOf(tn.O1, CliType.int32));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.peek_u4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVLx, ret, inst, ref next_var, ref success, tn.R, vara.ContentsOf(tn.O1, CliType.int32));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.peek_u8:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQx, ret, inst, ref next_var, ref success, tn.R, vara.ContentsOf(tn.O1, CliType.int32));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.poke_u1:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVBx, ret, inst, ref next_var, ref success, vara.ContentsOf(tn.O1, CliType.int32), tn.O2);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.poke_u2:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVWx, ret, inst, ref next_var, ref success, vara.ContentsOf(tn.O1, CliType.int32), tn.O2);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.poke_u4:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVLx, ret, inst, ref next_var, ref success, vara.ContentsOf(tn.O1, CliType.int32), tn.O2);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.poke_u8:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQx, ret, inst, ref next_var, ref success, vara.ContentsOf(tn.O1, CliType.int64), tn.O2);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.conv_i4_i8sx:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXD, ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.conv_i4_i1sx:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXB, ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.conv_i4_u1zx:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXB, ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.conv_i8_i4sx:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.conv_i4_u8zx:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXL, ret, inst, ref next_var, ref success, tn.R, tn.O1);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.br_ehclause:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.CALL, ret, inst, ref next_var, ref success, vara.Label("L" + tn.O1.ConstVal.ToString(), false));
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.endfinally:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.RETN, ret, inst, ref next_var, ref success);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.mul:
                        ret = new List<tybel.Node>();
                        if (tn.O2.VarType == vara.vara_type.Const)
                            ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.IMULL, x86_64.x86_64_asm.opcode.IMULQ), ret, inst, ref next_var, ref success, tn.R, tn.O1, tn.O2);
                        else
                        {
                            ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.MOVLx, x86_64.x86_64_asm.opcode.MOVQx), ret, inst, ref next_var, ref success, tn.R, tn.O1);
                            ChooseInstruction(x86_64.x86_64_asm.GetCTOpcode(op.Type, GetBitness(), x86_64.x86_64_asm.opcode.IMULL, x86_64.x86_64_asm.opcode.IMULQ), ret, inst, ref next_var, ref success, tn.R, tn.O2);
                        }
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.setl:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.XORLz, ret, inst, ref next_var, ref success, tn.R, tn.R);
                        switch (op.Type)
                        {
                            case CliType.int32:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CMPL, ret, inst, ref next_var, ref success, tn.O1, tn.O2);
                                break;
                            case CliType.int64:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CMPQ, ret, inst, ref next_var, ref success, tn.O1, tn.O2);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        ChooseInstruction(x86_64.x86_64_asm.opcode.SETL, ret, inst, ref next_var, ref success, tn.R);
                        if (success) return ret; break;

                    case ThreeAddressCode.OpName.seteq:
                        ret = new List<tybel.Node>();
                        ChooseInstruction(x86_64.x86_64_asm.opcode.XORL, ret, inst, ref next_var, ref success, tn.R, tn.R);
                        switch (op.Type)
                        {
                            case CliType.int32:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CMPL, ret, inst, ref next_var, ref success, tn.O1, tn.O2);
                                break;
                            case CliType.int64:
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CMPQ, ret, inst, ref next_var, ref success, tn.O1, tn.O2);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        ChooseInstruction(x86_64.x86_64_asm.opcode.SETZ, ret, inst, ref next_var, ref success, tn.R);
                        if (success) return ret; break;

                    default:
                        throw new NotImplementedException("No encoding provided for " + op.ToString());
                }
            }

            return null;
        }

        private void ChooseInstruction(x86_64.x86_64_asm.opcode op, List<tybel.Node> ret, timple.TreeNode tinst, ref int next_var, ref bool success,
            params vara[] vars)
        {
            if (x86_64.x86_64_asm.Opcodes == null)
                x86_64.x86_64_asm.InitOpcodes(GetBitness());

            if (success == false)
                return;

            List<x86_64.x86_64_asm> opcodes = x86_64.x86_64_asm.Opcodes[op];

            if(opcodes.Count == 0)
                throw new NotImplementedException("No encodings yet defined for " + op.ToString() + " in " + tinst.ToString());

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
                    success = true;
                    return;
                }
            }

            success = false;
        }

        private bool OpFits(vara vara, x86_64.x86_64_asm.optype optype)
        {
            Assembler.CliType dt = vara.DataType;
            if((dt == CliType.native_int) || (dt == CliType.O) || (dt == CliType.reference))
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
                            if (vara.needs_memloc)
                            {
                                if ((optype == x86_64.x86_64_asm.optype.RM32) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264as8))
                                    return true;
                                else
                                    return false;
                            }
                            else
                            {
                                if ((optype == x86_64.x86_64_asm.optype.R32) ||
                                    (optype == x86_64.x86_64_asm.optype.RM32) ||
                                    (optype == x86_64.x86_64_asm.optype.R8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264as8))
                                    return true;
                                else
                                    return false;
                            }

                        case CliType.int64:
                            if (vara.needs_memloc)
                            {
                                if ((optype == x86_64.x86_64_asm.optype.RM64) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264as8))
                                    return true;
                                else
                                    return false;
                            }
                            else
                            {
                                if ((optype == x86_64.x86_64_asm.optype.R64) ||
                                    (optype == x86_64.x86_64_asm.optype.RM64) ||
                                    (optype == x86_64.x86_64_asm.optype.R8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264as8))
                                    return true;
                                else
                                    return false;
                            }
                    }
                    return false;

                case libtysila.vara.vara_type.ContentsOf:
                    if ((optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM64)
                        || (optype == x86_64.x86_64_asm.optype.RM8) || (optype == x86_64.x86_64_asm.optype.RM16) ||
                                (optype == x86_64.x86_64_asm.optype.RM8163264) ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264as8))
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
                    if (vara.IsObject)
                    {
                        if (Options.PIC)
                        {
                            if (optype == x86_64.x86_64_asm.optype.RM32 ||
                                optype == x86_64.x86_64_asm.optype.RM8163264 ||
                                    (optype == x86_64.x86_64_asm.optype.RM8163264as8))
                                return true;
                        }
                        else
                        {
                            if (Arch.InstructionSet == "x86_64")
                            {
                                if (optype == x86_64.x86_64_asm.optype.Imm64)
                                    return true;
                            }
                            else
                            {
                                if (optype == x86_64.x86_64_asm.optype.Imm32)
                                    return true;
                            }
                        }
                    }
                    else
                    {
                        if (optype == x86_64.x86_64_asm.optype.Rel32)
                            return true;
                        if (Arch.InstructionSet == "x86_64")
                        {
                            if (optype == x86_64.x86_64_asm.optype.Imm64)
                                return true;
                        }
                        else
                        {
                            if (optype == x86_64.x86_64_asm.optype.Imm32)
                                return true;
                        }
                    }                            

                    return false;

                case libtysila.vara.vara_type.MachineReg:
                    if((vara.MachineRegVal.Equals(Rax)) && ((optype == x86_64.x86_64_asm.optype.rax) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rbx)) && ((optype == x86_64.x86_64_asm.optype.rbx) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rcx)) && ((optype == x86_64.x86_64_asm.optype.rcx) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rdx)) && ((optype == x86_64.x86_64_asm.optype.rdx) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rsi)) && ((optype == x86_64.x86_64_asm.optype.rsi) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rdi)) && ((optype == x86_64.x86_64_asm.optype.rdi) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rbp)) && ((optype == x86_64.x86_64_asm.optype.rbp) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(Rsp)) && ((optype == x86_64.x86_64_asm.optype.rsp) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R8)) && ((optype == x86_64.x86_64_asm.optype.r8) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R9)) && ((optype == x86_64.x86_64_asm.optype.r9) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R10)) && ((optype == x86_64.x86_64_asm.optype.r10) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R11)) && ((optype == x86_64.x86_64_asm.optype.r11) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R12)) && ((optype == x86_64.x86_64_asm.optype.r12) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R13)) && ((optype == x86_64.x86_64_asm.optype.r13) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R14)) && ((optype == x86_64.x86_64_asm.optype.r14) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                    if ((vara.MachineRegVal.Equals(R15)) && ((optype == x86_64.x86_64_asm.optype.r15) || (optype == x86_64.x86_64_asm.optype.R64) || (optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.R32) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;

                    if ((((vara.MachineRegVal is libasm.hardware_contentsof) && (((libasm.hardware_contentsof)vara.MachineRegVal).base_loc is libasm.x86_64_gpr)) || (vara.MachineRegVal is libasm.hardware_stackloc)) && ((optype == x86_64.x86_64_asm.optype.RM64) || (optype == x86_64.x86_64_asm.optype.RM32) || (optype == x86_64.x86_64_asm.optype.RM8163264) || (optype == x86_64.x86_64_asm.optype.RM8163264as8)))
                        return true;
                                       
                    return false;
            }

            return false;
        }
    }
}
