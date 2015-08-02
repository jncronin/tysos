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
    class ind
    {
        public static void ldind(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            Assembler.CliType dt = Assembler.CliType.void_;
            Signature.Param pushes = null;
            ThreeAddressCode.OpName op = ThreeAddressCode.OpName.invalid;

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldind_i:
                    dt = Assembler.CliType.native_int;
                    op = ThreeAddressCode.OpName.peek_u;
                    pushes = new Signature.Param(BaseType_Type.I);
                    break;

                case Opcode.SingleOpcodes.ldind_i1:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.peek_i1;
                    pushes = new Signature.Param(BaseType_Type.I1);
                    break;

                case Opcode.SingleOpcodes.ldind_i2:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.peek_i2;
                    pushes = new Signature.Param(BaseType_Type.I2);
                    break;

                case Opcode.SingleOpcodes.ldind_i4:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.peek_u4;
                    pushes = new Signature.Param(BaseType_Type.I4);
                    break;

                case Opcode.SingleOpcodes.ldind_i8:
                    dt = Assembler.CliType.int64;
                    op = ThreeAddressCode.OpName.peek_u8;
                    pushes = new Signature.Param(BaseType_Type.I8);
                    break;

                case Opcode.SingleOpcodes.ldind_r4:
                    dt = Assembler.CliType.F32;
                    op = ThreeAddressCode.OpName.peek_r4;
                    pushes = new Signature.Param(BaseType_Type.R4);
                    break;

                case Opcode.SingleOpcodes.ldind_r8:
                    dt = Assembler.CliType.F64;
                    op = ThreeAddressCode.OpName.peek_r8;
                    pushes = new Signature.Param(BaseType_Type.R8);
                    break;

                case Opcode.SingleOpcodes.ldind_ref:
                    dt = Assembler.CliType.O;
                    op = ThreeAddressCode.OpName.peek_u;
                    pushes = new Signature.Param(BaseType_Type.Object);
                    break;

                case Opcode.SingleOpcodes.ldind_u1:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.peek_u1;
                    pushes = new Signature.Param(BaseType_Type.U1);
                    break;

                case Opcode.SingleOpcodes.ldind_u2:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.peek_u2;
                    pushes = new Signature.Param(BaseType_Type.U2);
                    break;

                case Opcode.SingleOpcodes.ldind_u4:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.peek_u4;
                    pushes = new Signature.Param(BaseType_Type.U4);
                    break;

                default:
                    throw new Exception("Unsupported ldind opcode");
            }

            vara r = vara.Logical(next_variable++, dt);
            il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(op, dt), r, il.stack_vars_after.Pop(), vara.Void()));
            il.stack_after.Pop();

            il.stack_vars_after.Push(r);
            il.stack_after.Push(pushes);
        }

        public static void stind(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            ThreeAddressCode.OpName op = ThreeAddressCode.OpName.invalid;
            Assembler.CliType dt = Assembler.CliType.void_;

            switch (il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.stind_i:
                    dt = Assembler.CliType.native_int;
                    op = ThreeAddressCode.OpName.poke_u;
                    break;

                case Opcode.SingleOpcodes.stind_i1:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.poke_u1;
                    break;

                case Opcode.SingleOpcodes.stind_i2:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.poke_u2;
                    break;

                case Opcode.SingleOpcodes.stind_i4:
                    dt = Assembler.CliType.int32;
                    op = ThreeAddressCode.OpName.poke_u4;
                    break;

                case Opcode.SingleOpcodes.stind_i8:
                    dt = Assembler.CliType.int64;
                    op = ThreeAddressCode.OpName.poke_u8;
                    break;

                case Opcode.SingleOpcodes.stind_r4:
                    dt = Assembler.CliType.F32;
                    op = ThreeAddressCode.OpName.poke_r4;
                    break;

                case Opcode.SingleOpcodes.stind_r8:
                    dt = Assembler.CliType.F64;
                    op = ThreeAddressCode.OpName.poke_r8;
                    break;

                case Opcode.SingleOpcodes.stind_ref:
                    dt = Assembler.CliType.O;
                    op = ThreeAddressCode.OpName.poke_u;
                    break;

                default:
                    throw new Exception("Unsupported ldind opcode");
            }

            vara val = il.stack_vars_after.Pop();
            vara addr = il.stack_vars_after.Pop();

            il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(op, dt), vara.Void(), addr, val));
            il.stack_after.Pop();
            il.stack_after.Pop();
        }

        public static void tybel_stind(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            int len = 0;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.stind_i:
                    len = ass.GetSizeOfIntPtr();
                    break;

                case Opcode.SingleOpcodes.stind_i1:
                    len = 1;
                    break;

                case Opcode.SingleOpcodes.stind_i2:
                    len = 2;
                    break;

                case Opcode.SingleOpcodes.stind_i4:
                    len = 4;
                    break;

                case Opcode.SingleOpcodes.stind_i8:
                    len = 8;
                    break;

                case Opcode.SingleOpcodes.stind_r4:
                    len = 4;
                    break;

                case Opcode.SingleOpcodes.stind_r8:
                    len = 8;
                    break;

                case Opcode.SingleOpcodes.stind_ref:
                    len = ass.GetSizeOfPointer();
                    break;

                default:
                    throw new Exception("Unsupported ldind opcode");
            }

            libasm.hardware_location loc_val = il.stack_vars_after.Pop(ass);
            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_val = il.stack_after.Pop();
            Signature.Param p_addr = il.stack_after.Pop();

            if (!((p_addr.Type is Signature.ManagedPointer) || ((p_addr.Type is Signature.BaseType) &&
                (((Signature.BaseType)p_addr.Type).Type == BaseType_Type.I || ((Signature.BaseType)p_addr.Type).Type == BaseType_Type.U)) ||
                (p_addr.Type is Signature.UnmanagedPointer)))
                throw new Exception("stind without valid addr type: " + p_addr.Type.ToString());

            if (ass.Options.VerifiableCIL)
            {
                if (!(p_addr.Type is Signature.ManagedPointer))
                    throw new Exception("stind without addr being managed pointer");
                Signature.BaseOrComplexType t_addr = ((Signature.ManagedPointer)p_addr.Type).ElemType;
                if (!Signature.ParamCompare(p_val, new Signature.Param(t_addr, ass), ass))
                    throw new Exception("stind - mismatch between val type: " + p_val.ToString() + " and addr type: " + p_addr.ToString());
            }

            ass.Poke(state, il.stack_vars_before, loc_addr, loc_val, len, il.il.tybel);
        }

        public static void tybel_ldind(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param T = null;
            bool signed = false;

            switch (il.il.opcode.opcode1)
            {
                case Opcode.SingleOpcodes.ldind_i:
                    T = new Signature.Param(BaseType_Type.I);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldind_i1:
                    T = new Signature.Param(BaseType_Type.I1);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldind_i2:
                    T = new Signature.Param(BaseType_Type.I2);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldind_i4:
                    T = new Signature.Param(BaseType_Type.I4);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldind_i8:
                    T = new Signature.Param(BaseType_Type.I8);
                    signed = true;
                    break;
                case Opcode.SingleOpcodes.ldind_u1:
                    T = new Signature.Param(BaseType_Type.U1);
                    break;
                case Opcode.SingleOpcodes.ldind_u2:
                    T = new Signature.Param(BaseType_Type.U2);
                    break;
                case Opcode.SingleOpcodes.ldind_u4:
                    T = new Signature.Param(BaseType_Type.U4);
                    break;
                case Opcode.SingleOpcodes.ldind_r4:
                    T = new Signature.Param(BaseType_Type.R4);
                    break;
                case Opcode.SingleOpcodes.ldind_r8:
                    T = new Signature.Param(BaseType_Type.R8);
                    break;
                case Opcode.SingleOpcodes.ldind_ref:
                    T = new Signature.Param(BaseType_Type.I);
                    signed = true;
                    break;
                default:
                    throw new Exception("Unsupported ldind opcode");
            }

            libasm.hardware_location loc_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_addr = il.stack_after.Pop();

            if (!((p_addr.Type is Signature.ManagedPointer) || ((p_addr.Type is Signature.BaseType) && 
                (((Signature.BaseType)p_addr.Type).Type == BaseType_Type.I || ((Signature.BaseType)p_addr.Type).Type == BaseType_Type.U)) ||
                (p_addr.Type is Signature.UnmanagedPointer)))
                throw new Exception("ldind without valid addr type: " + p_addr.Type.ToString());

            if ((p_addr.Type is Signature.ManagedPointer) && il.il.opcode.opcode1 == Opcode.SingleOpcodes.ldind_ref)
                T = new Signature.Param(((Signature.ManagedPointer)p_addr.Type).ElemType, ass);

            if (ass.Options.VerifiableCIL)
            {
                if (!(p_addr.Type is Signature.ManagedPointer))
                    throw new Exception("ldind without addr being managed pointer");
                Signature.BaseOrComplexType t_addr = ((Signature.ManagedPointer)p_addr.Type).ElemType;
                if (!ass.IsAssignableTo(T, new Signature.Param(t_addr, ass)))
                    throw new Assembler.AssemblerException("ldind: stack type '" + t_addr.ToString() + "' is not " +
                        "assignable to T '" + T.ToString() + "'", il.il, mtc);
            }

            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(T, ass);
            ass.Peek(state, il.stack_vars_before, loc_dest, loc_addr, ass.GetPackedSizeOf(T), il.il.tybel);
            ass.Conv(state, il.stack_vars_before, loc_dest, loc_dest, new Signature.BaseType(T.CliType(ass)), new Signature.BaseType(T), signed, il.il.tybel);

            il.stack_after.Push(T);
        }
    }
}
