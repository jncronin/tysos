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

namespace libtysila
{
    partial class x86_64_Assembler
    {
        internal bool IsFloat(CliType ct)
        {
            switch (ct)
            {
                case CliType.F32:
                case CliType.F64:
                    return true;
                default:
                    return false;
            }
        }

        internal override void Conv(Encoder.EncoderState state, Stack regs_in_use, hardware_location dest, hardware_location src, Signature.BaseType dest_type, Signature.BaseType src_type, bool signed, List<tybel.Node> ret)
        {
            dest = ResolveStackLoc(this, state, dest);
            src = ResolveStackLoc(this, state, src);

            libasm.hardware_location act_dest_loc = dest;

            if (IsFloat(dest_type.CliType(this)) == false && !(dest is x86_64_gpr))
                act_dest_loc = Rax;
            if (IsFloat(dest_type.CliType(this)) == true && !(dest is x86_64_xmm))
                act_dest_loc = Xmm0;
        
            BaseType_Type act_src = src_type.Type;
            BaseType_Type act_dest = dest_type.Type;

            switch (src_type.Type)
            {
                case BaseType_Type.Object:
                case BaseType_Type.String:
                    src_type.Type = BaseType_Type.I;
                    break;
            }
            switch (dest_type.Type)
            {
                case BaseType_Type.Object:
                case BaseType_Type.String:
                    dest_type.Type = BaseType_Type.I;
                    break;
            }

            if (src_type.Type == BaseType_Type.I)
                act_src = ia == IA.i586 ? BaseType_Type.I4 : BaseType_Type.I8;
            if (src_type.Type == BaseType_Type.U)
                act_src = ia == IA.i586 ? BaseType_Type.U4 : BaseType_Type.U8;
            if (src_type.Type == BaseType_Type.Char)
                act_src = BaseType_Type.U2;
            if (dest_type.Type == BaseType_Type.I)
                act_dest = ia == IA.i586 ? BaseType_Type.I4 : BaseType_Type.I8;
            if (dest_type.Type == BaseType_Type.U)
                act_dest = ia == IA.i586 ? BaseType_Type.U4 : BaseType_Type.U8;
            if (dest_type.Type == BaseType_Type.Char)
                act_dest = BaseType_Type.U2;

            if (act_dest == act_src)
            {
                if (dest.Equals(src))
                    return;
                else
                {
                    Assign(state, regs_in_use, dest, src, new Signature.Param(act_dest).CliType(this), ret);
                    return;
                }
            }

            CliType dt = new Signature.BaseType(act_dest).CliType(this);

            if (ia == IA.i586 && dt == CliType.int64)
            {
                libasm.multiple_hardware_location mhl_dest = mhl_split(this, state, dest, 2, 4);
                dest = mhl_dest.hlocs[0];
                dt = CliType.int32;
            }

            switch (dt)
            {
                case CliType.int32:
                    switch (act_src)
                    {
                        case BaseType_Type.I1:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXB, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.I2:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXW, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.I4:
                        case BaseType_Type.I8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U1:
                        case BaseType_Type.Boolean:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXB, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U2:
                        case BaseType_Type.Char:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXW, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U4:
                        case BaseType_Type.U8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.R4:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSS2SIL, ret, act_dest_loc, src);
                            break;
                        case BaseType_Type.R8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSD2SIL, ret, act_dest_loc, src);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case CliType.int64:
                    switch (act_src)
                    {
                        case BaseType_Type.I1:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXB, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.I2:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVSXW, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.I4:
                            ChooseInstruction(signed ? x86_64.x86_64_asm.opcode.MOVSXD : x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.I8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U1:
                        case BaseType_Type.Boolean:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXB, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U2:
                        case BaseType_Type.Char:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXW, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U4:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVL, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.U8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVQ, ret, vara.MachineReg(act_dest_loc), vara.MachineReg(src));
                            break;
                        case BaseType_Type.R4:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSS2SIQ, ret, act_dest_loc, src);
                            break;
                        case BaseType_Type.R8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSD2SIQ, ret, act_dest_loc, src);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case CliType.F32:
                    switch (act_src)
                    {
                        case BaseType_Type.U1:
                        case BaseType_Type.Boolean:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXB, ret, Rax, src);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SSL, ret, act_dest_loc, Rax);
                            break;

                        case BaseType_Type.U2:
                        case BaseType_Type.Char:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXW, ret, Rax, src);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SSL, ret, act_dest_loc, Rax);
                            break;

                        case BaseType_Type.I1:
                        case BaseType_Type.I2:
                        case BaseType_Type.I4:
                        case BaseType_Type.U4:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SSL, ret, act_dest_loc, src);
                            break;

                        case BaseType_Type.I8:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new hardware_addressoflabel("__floatdisf", false), act_dest_loc,
                                    new hardware_location[] { src }, callconv_numop_s_q, ret);
                            }
                            else
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SSQ, ret, act_dest_loc, src);
                            break;
                        case BaseType_Type.U8:
                            Call(state, regs_in_use, new hardware_addressoflabel("__floatundisf", false), act_dest_loc,
                                new hardware_location[] { src }, callconv_numop_s_q, ret);
                            break;

                        case BaseType_Type.R8:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSD2SS, ret, act_dest_loc, src);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                case CliType.F64:
                    switch (act_src)
                    {
                        case BaseType_Type.U1:
                        case BaseType_Type.Boolean:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXB, ret, Rax, src);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SDL, ret, act_dest_loc, Rax);
                            break;

                        case BaseType_Type.U2:
                        case BaseType_Type.Char:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.MOVZXW, ret, Rax, src);
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SDL, ret, act_dest_loc, Rax);
                            break;

                        case BaseType_Type.I1:
                        case BaseType_Type.I2:
                        case BaseType_Type.I4:
                        case BaseType_Type.U4:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SDL, ret, act_dest_loc, src);
                            break;

                        case BaseType_Type.I8:
                            if (ia == IA.i586)
                            {
                                Call(state, regs_in_use, new hardware_addressoflabel("__floatdidf", false), act_dest_loc,
                                    new hardware_location[] { src }, callconv_numop_d_q, ret);
                            }
                            else
                                ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSI2SDQ, ret, act_dest_loc, src);
                            break;

                        case BaseType_Type.U8:
                            Call(state, regs_in_use, new hardware_addressoflabel("__floatundidf", false), act_dest_loc,
                                new hardware_location[] { src }, callconv_numop_d_q, ret);
                            break;

                        case BaseType_Type.R4:
                            ChooseInstruction(x86_64.x86_64_asm.opcode.CVTSS2SD, ret, act_dest_loc, src);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (!dest.Equals(act_dest_loc))
                EncMov(this, state, dest, act_dest_loc, ret);

            if (ia == IA.i586 && dt == CliType.int64)
            {
                libasm.multiple_hardware_location mhl_dest = mhl_split(this, state, dest, 2, 4);
                
                /* if this is a signed conversion, we need to extract the top bit of mhl_dest[0] and
                 * replicate it throughout mhl_dest[1] */
                if (signed)
                {
                    /* we do:
                     * mov rdx, act_dest_loc
                     * shr rdx, 31
                     * mov mhl_dest[1], rdx */
                    EncMov(this, state, Rdx, act_dest_loc, ret);
                    ChooseInstruction(x86_64.x86_64_asm.opcode.SARL, ret, vara.MachineReg(Rdx), vara.Const(31));
                    EncMov(this, state, mhl_dest.hlocs[1], Rdx, ret);
                }
                else
                    Assign(state, regs_in_use, mhl_dest.hlocs[1], new libasm.const_location { c = 0 }, CliType.int32, ret);
            }
        }

        private static vara enc_conv(InstructionLine i, BaseType_Type dest_bt, Signature.Param srcp, vara srcv, bool force_conversion, ref int next_variable, Assembler ass,
            Assembler.MethodAttributes attrs)
        {
            Assembler.CliType src_ct = srcp.CliType(ass);
            // Convert from srcp.CliType to baseType_Type
            switch (src_ct)
            {
                case Assembler.CliType.int32:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_isx), vara.Logical(next_variable++, Assembler.CliType.native_int), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I1:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i4_i1sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I2:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i4_i2sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I4:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.I8:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i4_i8sx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_uzx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U1:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i4_u1zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U2:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i4_u2zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U4:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.U8:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i4_u8zx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.R4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.conv_i4_r4), vara.Logical(next_variable++, Assembler.CliType.F32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.R8:
                            if (i.opcode.opcode1 == Opcode.SingleOpcodes.conv_r_un)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_u4_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                            else
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_i4_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case Assembler.CliType.int64:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i8_isx), vara.Logical(next_variable++, Assembler.CliType.native_int), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I1:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i1sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I2:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i2sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i4sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I8:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.U:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i8_uzx), vara.Logical(next_variable++, Assembler.CliType.native_int), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U1:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u1zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U2:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u2zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u4zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U8:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.R4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.conv_i8_r4), vara.Logical(next_variable++, Assembler.CliType.F32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.R8:
                            if (i.opcode.opcode1 == Opcode.SingleOpcodes.conv_r_un)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_u8_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                            else
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_i8_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case Assembler.CliType.native_int:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.native_int), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.I1:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_i1sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I2:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_i2sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_i4sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I8:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i_i8sx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.native_int), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.U1:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_u1zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U2:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_u2zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_u4zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U8:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i_u8zx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.R4:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.conv_i_r4), vara.Logical(next_variable++, Assembler.CliType.F32), srcv, vara.Void()));
                            break;
                        case BaseType_Type.R8:
                            if (i.opcode.opcode1 == Opcode.SingleOpcodes.conv_r_un)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_u_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                            else
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_i_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case Assembler.CliType.O:
                case Assembler.CliType.reference:
                    switch (dest_bt)
                    {
                        case BaseType_Type.I8:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i_i8sx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.U8:
                            i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_i_u8zx), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                            break;
                        case BaseType_Type.I:
                        case BaseType_Type.U:
                            if (force_conversion)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.native_int), srcv, vara.Void()));
                            else
                                return vara.Void();
                            break;
                        case BaseType_Type.I4:
                            if (attrs.security.AllowConvORefToUI4)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_i4sx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            else
                                throw new NotSupportedException();
                            break;
                        case BaseType_Type.U4:
                            if (attrs.security.AllowConvORefToUI4)
                                i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i_u4zx), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                            else
                                throw new NotSupportedException();
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                case Assembler.CliType.F32:
                case Assembler.CliType.F64:
                    BaseType_Type from_type = ((Signature.BaseType)srcp.Type).Type;
                    switch (from_type)
                    {
                        case BaseType_Type.R4:
                            switch (dest_bt)
                            {
                                case BaseType_Type.R8:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.conv_r4_r8), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                                    break;
                                case BaseType_Type.R4:
                                    if (force_conversion)
                                        i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.F32), srcv, vara.Void()));
                                    else
                                        return vara.Void();
                                    break;
                                case BaseType_Type.I4:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.conv_r4_i4), vara.Logical(next_variable++, Assembler.CliType.int32), srcv, vara.Void()));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            break;
                        case BaseType_Type.R8:
                            switch (dest_bt)
                            {
                                case BaseType_Type.R4:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.conv_r8_r4), vara.Logical(next_variable++, Assembler.CliType.F32), srcv, vara.Void()));
                                    break;
                                case BaseType_Type.R8:
                                    if (force_conversion)
                                        i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.assign), vara.Logical(next_variable++, Assembler.CliType.F64), srcv, vara.Void()));
                                    else
                                        return vara.Void();
                                    break;
                                case BaseType_Type.U1:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u1zx), vara.Logical(next_variable, Assembler.CliType.int32), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.U2:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u2zx), vara.Logical(next_variable, Assembler.CliType.int32), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.U4:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_u4zx), vara.Logical(next_variable, Assembler.CliType.int32), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.I8:
                                case BaseType_Type.U8:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    break;
                                case BaseType_Type.I1:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i1sx), vara.Logical(next_variable, Assembler.CliType.int32), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.I2:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i2sx), vara.Logical(next_variable, Assembler.CliType.int32), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.I4:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.conv_i8_i4sx), vara.Logical(next_variable, Assembler.CliType.int32), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.I:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i8_isx), vara.Logical(next_variable, Assembler.CliType.native_int), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                case BaseType_Type.U:
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.conv_r8_i8), vara.Logical(next_variable++, Assembler.CliType.int64), srcv, vara.Void()));
                                    i.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i8_uzx), vara.Logical(next_variable, Assembler.CliType.native_int), vara.Logical(next_variable - 1, Assembler.CliType.int64), vara.Void()));
                                    next_variable++;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;
                default:
                    throw new NotSupportedException("Cannot use conv on type " + srcp.CliType(ass));
            }

            return ((timple.TimpleNode)i.tacs[i.tacs.Count - 1]).R;
        }
    }
}
