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

namespace libtysila.frontend.cil.OpcodeEncodings
{
    class conv
    {
        public static void Conv(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            Signature.Param srcp = il.stack_after.Pop();
            vara srcv = il.stack_vars_after.Pop();

            Assembler.CliType src_ct = srcp.CliType(ass);
            BaseType_Type dest_bt;
            bool ovf = false;
            bool un = false;

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.conv_i:
                case Opcode.SingleOpcodes.conv_ovf_i:
                case Opcode.SingleOpcodes.conv_ovf_i_un:
                    dest_bt = BaseType_Type.I;
                    break;

                case Opcode.SingleOpcodes.conv_i1:
                case Opcode.SingleOpcodes.conv_ovf_i1:
                case Opcode.SingleOpcodes.conv_ovf_i1_un:
                    dest_bt = BaseType_Type.I1;
                    break;

                case Opcode.SingleOpcodes.conv_i2:
                case Opcode.SingleOpcodes.conv_ovf_i2:
                case Opcode.SingleOpcodes.conv_ovf_i2_un:
                    dest_bt = BaseType_Type.I2;
                    break;

                case Opcode.SingleOpcodes.conv_i4:
                case Opcode.SingleOpcodes.conv_ovf_i4:
                case Opcode.SingleOpcodes.conv_ovf_i4_un:
                    dest_bt = BaseType_Type.I4;
                    break;

                case Opcode.SingleOpcodes.conv_i8:
                case Opcode.SingleOpcodes.conv_ovf_i8:
                case Opcode.SingleOpcodes.conv_ovf_i8_un:
                    dest_bt = BaseType_Type.I8;
                    break;

                case Opcode.SingleOpcodes.conv_u:
                case Opcode.SingleOpcodes.conv_ovf_u:
                case Opcode.SingleOpcodes.conv_ovf_u_un:
                    dest_bt = BaseType_Type.U;
                    break;

                case Opcode.SingleOpcodes.conv_u1:
                case Opcode.SingleOpcodes.conv_ovf_u1:
                case Opcode.SingleOpcodes.conv_ovf_u1_un:
                    dest_bt = BaseType_Type.U1;
                    break;

                case Opcode.SingleOpcodes.conv_u2:
                case Opcode.SingleOpcodes.conv_ovf_u2:
                case Opcode.SingleOpcodes.conv_ovf_u2_un:
                    dest_bt = BaseType_Type.U2;
                    break;

                case Opcode.SingleOpcodes.conv_u4:
                case Opcode.SingleOpcodes.conv_ovf_u4:
                case Opcode.SingleOpcodes.conv_ovf_u4_un:
                    dest_bt = BaseType_Type.U4;
                    break;

                case Opcode.SingleOpcodes.conv_u8:
                case Opcode.SingleOpcodes.conv_ovf_u8:
                case Opcode.SingleOpcodes.conv_ovf_u8_un:
                    dest_bt = BaseType_Type.U8;
                    break;

                case Opcode.SingleOpcodes.conv_r_un:
                    dest_bt = BaseType_Type.R8;
                    break;

                case Opcode.SingleOpcodes.conv_r4:
                    dest_bt = BaseType_Type.R4;
                    break;

                case Opcode.SingleOpcodes.conv_r8:
                    dest_bt = BaseType_Type.R8;
                    break;

                default:
                    throw new NotSupportedException("Unsupported conv opcode: " + il.opcode.ToString());
            }

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.conv_ovf_i:
                case Opcode.SingleOpcodes.conv_ovf_i1:
                case Opcode.SingleOpcodes.conv_ovf_i2:
                case Opcode.SingleOpcodes.conv_ovf_i4:
                case Opcode.SingleOpcodes.conv_ovf_i8:
                case Opcode.SingleOpcodes.conv_ovf_u:
                case Opcode.SingleOpcodes.conv_ovf_u1:
                case Opcode.SingleOpcodes.conv_ovf_u2:
                case Opcode.SingleOpcodes.conv_ovf_u4:
                case Opcode.SingleOpcodes.conv_ovf_u8:
                    ovf = true;
                    break;

                case Opcode.SingleOpcodes.conv_ovf_i_un:
                case Opcode.SingleOpcodes.conv_ovf_i1_un:
                case Opcode.SingleOpcodes.conv_ovf_i2_un:
                case Opcode.SingleOpcodes.conv_ovf_i4_un:
                case Opcode.SingleOpcodes.conv_ovf_i8_un:
                case Opcode.SingleOpcodes.conv_ovf_u_un:
                case Opcode.SingleOpcodes.conv_ovf_u1_un:
                case Opcode.SingleOpcodes.conv_ovf_u2_un:
                case Opcode.SingleOpcodes.conv_ovf_u4_un:
                case Opcode.SingleOpcodes.conv_ovf_u8_un:
                    ovf = true;
                    un = true;
                    break;

                case Opcode.SingleOpcodes.conv_r_un:
                    un = true;
                    break;
            }

            vara dest_v = enc_conv(il, dest_bt, srcp, srcv, false, ref next_variable, ass, attrs);

            if (dest_v.VarType != vara.vara_type.Void)
            {
                // Perform overflow testing if requested
                if (ovf)
                {
                    // convert back to original type and then compare with src
                    Signature.Param dest_p = new Signature.Param(dest_bt);
                    vara second_dest_v;

                    switch (srcp.CliType(ass))
                    {
                        case Assembler.CliType.int32:
                            if (un)
                                second_dest_v = enc_conv(il, BaseType_Type.U4, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            else
                                second_dest_v = enc_conv(il, BaseType_Type.I4, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            il.tacs.Add(new timple.TimpleThrowBrNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.throwne), srcv, second_dest_v, vara.Const(Assembler.throw_OverflowException)));
                            break;
                        case Assembler.CliType.int64:
                            if (un)
                                second_dest_v = enc_conv(il, BaseType_Type.U8, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            else
                                second_dest_v = enc_conv(il, BaseType_Type.I8, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            il.tacs.Add(new timple.TimpleThrowBrNode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.throwne), srcv, second_dest_v, vara.Const(Assembler.throw_OverflowException)));
                            break;
                        case Assembler.CliType.native_int:
                            if (un)
                                second_dest_v = enc_conv(il, BaseType_Type.U, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            else
                                second_dest_v = enc_conv(il, BaseType_Type.I, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            il.tacs.Add(new timple.TimpleThrowBrNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.throwne), srcv, second_dest_v, vara.Const(Assembler.throw_OverflowException)));
                            break;
                        case Assembler.CliType.F64:
                            second_dest_v = enc_conv(il, BaseType_Type.R8, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            il.tacs.Add(new timple.TimpleThrowBrNode(ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.throwne), srcv, second_dest_v, vara.Const(Assembler.throw_OverflowException)));
                            break;
                        case Assembler.CliType.F32:
                            second_dest_v = enc_conv(il, BaseType_Type.R4, dest_p, dest_v, true, ref next_variable, ass, attrs);
                            il.tacs.Add(new timple.TimpleThrowBrNode(ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.throwne), srcv, second_dest_v, vara.Const(Assembler.throw_OverflowException)));
                            break;
                    }
                }
            }

            il.stack_after.Push(new Signature.Param(dest_bt));
            il.stack_vars_after.Push(dest_v);
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
