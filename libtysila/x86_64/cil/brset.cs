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
using libtysila.frontend.cil;
using libasm;

namespace libtysila.x86_64.cil
{
    class brset
    {
        public static void tybel_br(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int il_target = il.il.il_offset_after + il.il.inline_int;
            CilNode target = state.offset_map[il_target];
            ((x86_64_Assembler)ass).ChooseInstruction(x86_64_asm.opcode.JMP, il.il.tybel, vara.Label("L" + target.il_label.ToString(), false));
        }

        public static void tybel_brset(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location a_loc, b_loc;
            Signature.Param a_p, b_p;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.brtrue:
                case Opcode.SingleOpcodes.brtrue_s:
                    a_loc = il.stack_vars_after.Pop(ass);
                    b_loc = new libasm.const_location { c = 1 };
                    a_p = il.stack_after.Pop();
                    b_p = a_p;
                    break;
                case Opcode.SingleOpcodes.brfalse:
                case Opcode.SingleOpcodes.brfalse_s:
                    a_loc = il.stack_vars_after.Pop(ass);
                    b_loc = new const_location { c = 0 };
                    a_p = il.stack_after.Pop();
                    b_p = a_p;
                    break;
                default:
                    b_loc = il.stack_vars_after.Pop(ass);
                    a_loc = il.stack_vars_after.Pop(ass);
                    b_p = il.stack_after.Pop();
                    a_p = il.stack_after.Pop();
                    break;
            }

            Assembler.CliType a_ct = a_p.CliType(ass);
            Assembler.CliType b_ct = b_p.CliType(ass);

            x86_64_Assembler a = ass as x86_64_Assembler;

            a_loc = x86_64_Assembler.ResolveStackLoc(a, state, a_loc);
            b_loc = x86_64_Assembler.ResolveStackLoc(a, state, b_loc);

            if(a_p.CliType(ass) != b_p.CliType(ass))
                throw new Exception("Mimatched datatypes on " + il.ToString() + ": " + a_p.ToString() + " and " + b_p.ToString());

            switch(a_p.CliType(ass))
            {
                case Assembler.CliType.int32:
                case Assembler.CliType.int64:
                case Assembler.CliType.native_int:
                    if (!(a_loc is x86_64_gpr) && !(b_loc is x86_64_gpr))
                    {
                        if (!(((a_loc is hardware_contentsof) || (a_loc is hardware_stackloc)) && (b_loc is const_location)))
                        {
                            x86_64_Assembler.EncMov(ass as x86_64_Assembler, state, x86_64_Assembler.Rax, a_loc, il.il.tybel);
                            a_loc = x86_64_Assembler.Rax;
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            x86_64_asm.opcode cmp_op;
            switch(a_p.CliType(ass))
            {
                case Assembler.CliType.int32:
                    cmp_op = x86_64_asm.opcode.CMPL;
                    break;
                case Assembler.CliType.int64:
                    if(a.ia == x86_64_Assembler.IA.i586)
                        throw new NotImplementedException();
                    else
                        cmp_op = x86_64_asm.opcode.CMPQ;
                    break;
                case Assembler.CliType.native_int:
                    if(a.ia == x86_64_Assembler.IA.i586)
                        cmp_op = x86_64_asm.opcode.CMPL;
                    else
                        cmp_op = x86_64_asm.opcode.CMPQ;
                    break;
                default:
                    throw new NotImplementedException();
            }

            a.ChooseInstruction(cmp_op, il.il.tybel, vara.MachineReg(a_loc), vara.MachineReg(b_loc));

            x86_64_asm.opcode setbr_op;
            bool is_set = false;
            switch (il.il.opcode)
            {
                case 0xfe01:
                    // ceq
                    setbr_op = x86_64_asm.opcode.SETZ;
                    is_set = true;
                    break;
                case 0xfe02:
                    // cgt
                    setbr_op = x86_64_asm.opcode.SETNLE;
                    is_set = true;
                    break;
                case 0xfe03:
                    // cgt.un
                    setbr_op = x86_64_asm.opcode.SETNBE;
                    is_set = true;
                    break;
                case 0xfe04:
                    // clt
                    setbr_op = x86_64_asm.opcode.SETL;
                    is_set = true;
                    break;
                case 0xfe05:
                    // clt.un
                    setbr_op = x86_64_asm.opcode.SETB;
                    is_set = true;
                    break;
                default:
                    switch (il.il.opcode.opcode1)
                    {
                        case Opcode.SingleOpcodes.brtrue:
                        case Opcode.SingleOpcodes.brtrue_s:
                        case Opcode.SingleOpcodes.brfalse:
                        case Opcode.SingleOpcodes.brfalse_s:
                        case Opcode.SingleOpcodes.beq:
                        case Opcode.SingleOpcodes.beq_s:
                            setbr_op = x86_64_asm.opcode.JZ;
                            break;
                        case Opcode.SingleOpcodes.bge:
                        case Opcode.SingleOpcodes.bge_s:
                            setbr_op = x86_64_asm.opcode.JNL;
                            break;
                        case Opcode.SingleOpcodes.bge_un:
                        case Opcode.SingleOpcodes.bge_un_s:
                            setbr_op = x86_64_asm.opcode.JNB;
                            break;
                        case Opcode.SingleOpcodes.bgt:
                        case Opcode.SingleOpcodes.bgt_s:
                            setbr_op = x86_64_asm.opcode.JNLE;
                            break;
                        case Opcode.SingleOpcodes.bgt_un:
                        case Opcode.SingleOpcodes.bgt_un_s:
                            setbr_op = x86_64_asm.opcode.JNBE;
                            break;
                        case Opcode.SingleOpcodes.ble:
                        case Opcode.SingleOpcodes.ble_s:
                            setbr_op = x86_64_asm.opcode.JLE;
                            break;
                        case Opcode.SingleOpcodes.ble_un:
                        case Opcode.SingleOpcodes.ble_un_s:
                            setbr_op = x86_64_asm.opcode.JBE;
                            break;
                        case Opcode.SingleOpcodes.blt:
                        case Opcode.SingleOpcodes.blt_s:
                            setbr_op = x86_64_asm.opcode.JL;
                            break;
                        case Opcode.SingleOpcodes.blt_un:
                        case Opcode.SingleOpcodes.blt_un_s:
                            setbr_op = x86_64_asm.opcode.JB;
                            break;
                        case Opcode.SingleOpcodes.bne_un:
                        case Opcode.SingleOpcodes.bne_un_s:
                            setbr_op = x86_64_asm.opcode.JNZ;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
            }

            if (is_set)
            {
                a.ChooseInstruction(setbr_op, il.il.tybel, vara.MachineReg(x86_64_Assembler.Rax));
                a.ChooseInstruction(x86_64_asm.opcode.MOVZXB, il.il.tybel, vara.MachineReg(x86_64_Assembler.Rax), vara.MachineReg(x86_64_Assembler.Rax));

                libasm.hardware_location ret_loc = il.stack_vars_after.GetAddressFor(new Signature.Param(BaseType_Type.I4), ass);
                a.Assign(state, il.stack_vars_before, ret_loc, x86_64_Assembler.Rax, Assembler.CliType.int32, il.il.tybel);

                il.stack_after.Push(new Signature.Param(BaseType_Type.I4));
            }
            else
            {
                int il_target = il.il.il_offset_after + il.il.inline_int;
                CilNode target = state.offset_map[il_target];
                a.ChooseInstruction(setbr_op, il.il.tybel, vara.Label("L" + target.il_label.ToString(), false));
            }
        }
    }
}
