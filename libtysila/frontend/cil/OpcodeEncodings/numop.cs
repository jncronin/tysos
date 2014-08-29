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
    class numop
    {
        class binnumop_key
        {
            public Assembler.CliType a_type;
            public Assembler.CliType b_type;
            public int opcode;

            public override int GetHashCode()
            {
                int hc = a_type.GetHashCode();
                hc <<= 12;
                hc ^= b_type.GetHashCode();
                hc <<= 12;
                hc ^= opcode.GetHashCode();
                return hc;
            }

            public override bool Equals(object obj)
            {
                binnumop_key other = obj as binnumop_key;
                if (obj == null)
                    return false;
                if (a_type != other.a_type)
                    return false;
                if (b_type != other.b_type)
                    return false;
                if (opcode != other.opcode)
                    return false;
                return true;
            }

            public override string ToString()
            {
                return a_type.ToString() + ", " + b_type.ToString() + ", " + opcode.ToString();
            }
        }

        class numop_val
        {
            public Assembler.CliType dt;
            public ThreeAddressCode.Op op;
            public ThreeAddressCode.Op throw_op = ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.invalid);
        }

        static Dictionary<binnumop_key, numop_val> binnumops = new Dictionary<binnumop_key, numop_val>();

        static numop()
        {
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.int32, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int64, b_type = Assembler.CliType.int64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.int64, op = ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.reference, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.reference, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.reference, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.reference, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.reference, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.reference, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.reference, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.reference, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.F32, b_type = Assembler.CliType.F32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.F32, op = ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.add) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.F64, b_type = Assembler.CliType.F64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.add) }] = new numop_val { dt = Assembler.CliType.F64, op = ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.add) };

            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.int32, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int64, b_type = Assembler.CliType.int64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.int64, op = ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.reference, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.reference, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.reference, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.reference, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.F32, b_type = Assembler.CliType.F32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.F32, op = ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.F64, b_type = Assembler.CliType.F64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.F64, op = ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.sub) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.reference, b_type = Assembler.CliType.reference, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.sub) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub) };

            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.and) }] = new numop_val { dt = Assembler.CliType.int32, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.and) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int64, b_type = Assembler.CliType.int64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.and) }] = new numop_val { dt = Assembler.CliType.int64, op = ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.and) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.and) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.and) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.and) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.and) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.and) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.and) };

            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.or) }] = new numop_val { dt = Assembler.CliType.int32, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.or) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int64, b_type = Assembler.CliType.int64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.or) }] = new numop_val { dt = Assembler.CliType.int64, op = ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.or) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.or) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.or) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.or) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.or) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.or) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.or) };

            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.xor) }] = new numop_val { dt = Assembler.CliType.int32, op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.xor) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int64, b_type = Assembler.CliType.int64, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.xor) }] = new numop_val { dt = Assembler.CliType.int64, op = ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.xor) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.int32, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.xor) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.xor) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.int32, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.xor) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.xor) };
            binnumops[new binnumop_key { a_type = Assembler.CliType.native_int, b_type = Assembler.CliType.native_int, opcode = Opcode.OpcodeVal(Opcode.SingleOpcodes.xor) }] = new numop_val { dt = Assembler.CliType.native_int, op = ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.xor) };
        }

        public static void binnumop(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            vara b = il.stack_vars_after.Pop();
            vara a = il.stack_vars_after.Pop();
            il.stack_after.Pop();
            il.stack_after.Pop();

            binnumop_key k = new binnumop_key { a_type = a.DataType, b_type = b.DataType, opcode = il.opcode.opcode };
            numop_val v;
            if (!binnumops.TryGetValue(k, out v))
                throw new Exception("Invalid binary num op combination: " + k.ToString());

            vara r = vara.Logical(next_variable++, v.dt);
            il.tacs.Add(new timple.TimpleNode(v.op, r, a, b));
            if (v.throw_op != ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.invalid))
                il.tacs.Add(new timple.TimpleNode(v.throw_op, vara.Void(), vara.Void(), vara.Void()));

            il.stack_vars_after.Push(r);
            il.stack_after.Push(new Signature.Param(v.dt));
        }
    }
}
