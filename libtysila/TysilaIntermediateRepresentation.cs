/* Copyright (C) 2008 - 2014 by John Cronin
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
using libasm;

namespace libtysila
{
    public struct var : IEquatable<var>
    {
        public class var_ref
        {
            public var v;
        }

        public int logical_var;
        public hardware_location hardware_loc;
        public object constant_val;
        public int ssa_form;

        public object known_value;

        public int local_var;
        public int constant_offset;
        public bool address_of;
        public bool contents_of;
        public string label;
        public int local_arg;
        public bool is_function;

        public bool defined;
        public var_ref base_var;

        public bool is_address_of_vt;
        public bool is_global;

        public int v_size;

        public var CloneVar()
        { return (var)this.MemberwiseClone(); }

        public bool DependsOn(var v)
        {
            if (this.Equals(v))
                return true;
            if (contents_of | address_of)
                return base_var.v.DependsOn(v);
            return false;
        }

        public static var Label(string s)
        {
            var ret = new var();
            ret.label = s;
            ret.defined = true;
            ret.local_arg = -1;
            ret.local_var = -1;
            return ret;
        }

        public static var FunctionLabel(string s)
        {
            var ret = new var();
            ret.label = s;
            ret.defined = true;
            ret.local_arg = -1;
            ret.local_var = -1;
            ret.is_function = true;
            return ret;
        }

        public static var AddrOf(var v)
        {
            var ret = new var();
            ret.address_of = true;
            ret.defined = true;
            ret.local_arg = -1;
            ret.local_var = -1;
            ret.base_var = new var_ref { v = v.CloneVar() };
            return ret;
        }

        public static var AddrOf(var v, int const_offset)
        {
            var ret = AddrOf(v);
            ret.constant_offset = const_offset;
            return ret;
        }

        public static var ContentsOf(var v)
        {
            var ret = new var();
            ret.contents_of = true;
            ret.defined = true;
            ret.local_arg = -1;
            ret.local_var = -1;
            ret.base_var = new var_ref { v = v.CloneVar() };
            ret.v_size = 0;
            return ret;
        }

        public static var ContentsOf(var v, int const_offset)
        {
            var ret = ContentsOf(v);
            ret.constant_offset = const_offset;
            return ret;
        }

        public static var ContentsOf(var v, int const_offset, int size)
        {
            var ret = ContentsOf(v);
            ret.constant_offset = const_offset;
            ret.v_size = size;
            return ret;
        }

        public static var AddrOfObject(string label)
        {
            return AddrOf(Label(label));
        }
            
        public static var AddrOfObject(string label, int offset)
        {
            var ret = AddrOfObject(label);
            ret.constant_offset = offset;
            return ret;
        }

        public static var AddrOfFunction(string label)
        {
            return AddrOf(FunctionLabel(label));
        }

        public static var ContentsOf(int log_id)
        {
            var ret = AddrOf(log_id);
            ret.address_of = false;
            ret.contents_of = true;
            ret.v_size = 0;
            return ret;
        }

        public static var ContentsOf(int log_id, int offset)
        {
            var ret = ContentsOf(log_id);
            ret.constant_offset = offset;
            return ret;
        }

        public class Coercion
        {
            public override string ToString()
            {
                return "Coerce";
            }
        }

        public static var Coerce()
        { return Const(new Coercion()); }
        public bool IsCoerce() { if (constant_val is Coercion) return true; return false; }

        public static var Const(object const_val)
        {
            var ret = AddrOf(0);
            ret.address_of = false;
            ret.constant_val = const_val;
            return ret;
        }

        public class SpecialConstValue
        {
            public enum SpecialConstValueType { UsedStackSize };
            public SpecialConstValueType Type;
            public SpecialConstValue(SpecialConstValueType type) { Type = type; }
        }

        public static var Undefined
        {
            get
            {
                var ret = new var();
                ret.defined = false;
                return ret;
            }
        }

        public static var Null
        {
            get
            {
                var ret = AddrOf(0);
                ret.address_of = false;
                return ret;
            }
        }

        public static var LocalVar(int var_id)
        {
            throw new NotSupportedException();
        }

        public static var LocalArg(int arg_id)
        {
            throw new NotSupportedException();
        }

        public static var AddrLocalVar(int var_id)
        {
            throw new NotSupportedException();
        }

        public static var AddrLocalArg(int var_id)
        {
            throw new NotSupportedException();
        }

        public static implicit operator int(var v)
        { return v.logical_var; }
        public static implicit operator var(int i)
        { return new var { logical_var = i, hardware_loc = null, local_var = -1, constant_offset = 0,
            address_of = false, constant_val = null, label = null, contents_of = false, defined = true,
            local_arg = -1 }; }

        public enum var_type
        {
            Const, LogicalVar, Label, LocalVar, LocalArg, ContentsOf, ContentsOfPlusConstant, AddressOf, AddressOfPlusConstant, Void
        }

        /*public enum var_type
        {
            Const, LogicalVar, AddressOfLocalVar, AddressOfLabel, AddressOfLabelPlusConstant,
            ContentsOfAddress, ContentsOfAddressPlusConstant, LocalVar, Void, LocalArg, AddressOfLocalArg,
            ContentsOfLocalArg, ContentsOfLocalVar
        };*/

        public var_type type
        {
            get
            {
                if (!defined)
                    return var_type.Void;
                if (constant_val != null)
                    return var_type.Const;
                if (label != null)
                    return var_type.Label;
                if (contents_of == true)
                {
                    if (constant_offset > 0)
                        return var_type.ContentsOfPlusConstant;
                    else
                        return var_type.ContentsOf;
                }
                if (address_of == true)
                {
                    if (constant_offset > 0)
                        return var_type.AddressOfPlusConstant;
                    else
                        return var_type.AddressOf;
                }
                if (local_var >= 0)
                    return var_type.LocalVar;
                if (local_arg >= 0)
                    return var_type.LocalArg;
                if (logical_var == 0)
                    return var_type.Void;
                return var_type.LogicalVar;
            }
        }

        public override string ToString()
        {
            switch (type)
            {
                case var_type.Const:
                    return "$" + constant_val.ToString();
                case var_type.AddressOf:
                    return "&" + base_var.v.ToString();
                case var_type.AddressOfPlusConstant:
                    return "&" + base_var.v.ToString() + " + $" + constant_offset.ToString();
                case var_type.ContentsOf:
                    return "*" + base_var.v.ToString();
                case var_type.ContentsOfPlusConstant:
                    return "*(" + base_var.v.ToString() + " + $" + constant_offset.ToString() + ")";
                case var_type.LocalVar:
                    return "lv" + local_var.ToString();
                case var_type.LocalArg:
                    return "la" + local_arg.ToString();
                case var_type.LogicalVar:
                    if (ssa_form != 0)
                        return logical_var.ToString() + "." + ssa_form.ToString();
                    else
                        return logical_var.ToString();
                case var_type.Void:
                    return "";
                case var_type.Label:
                    return label;
            }
            if (hardware_loc != null)
                return hardware_loc.ToString();
            return "[ERROR]";
        }

        public override int GetHashCode()
        {
            int hash_code = logical_var.GetHashCode();
            hash_code <<= 1;
            hash_code ^= ssa_form.GetHashCode();
            hash_code <<= 1;
            hash_code ^= local_var.GetHashCode();
            hash_code <<= 1;
            hash_code ^= local_arg.GetHashCode();
            hash_code <<= 1;
            hash_code ^= address_of.GetHashCode();
            hash_code <<= 1;
            hash_code ^= constant_offset.GetHashCode();
            hash_code <<= 1;
            hash_code ^= contents_of.GetHashCode();
            return hash_code;
        }

        public bool Equals(var other)
        {
            if (this.logical_var != other.logical_var)
                return false;
            if (this.ssa_form != other.ssa_form)
                return false;
            if (this.local_arg != other.local_arg)
                return false;
            if (this.local_var != other.local_var)
                return false;            
            if (this.address_of != other.address_of)
                return false;
            if (this.constant_offset != other.constant_offset)
                return false;
            if (this.constant_val != other.constant_val)
                return false;
            if (this.contents_of != other.contents_of)
                return false;
            if ((this.label != null) && !this.label.Equals(other.label))
                return false;

            if ((this.hardware_loc != null) && (other.hardware_loc != null) && !this.hardware_loc.Equals(other.hardware_loc))
                return false;
            if ((this.base_var != null) || (other.base_var != null))
            {
                if (this.base_var == null)
                    return false;
                if (other.base_var == null)
                    return false;
                if (!this.base_var.v.Equals(other.base_var.v))
                    return false;
            }
            return true;
        }

        public var ReferencedLogicalVar
        {
            get
            {
                if (base_var != null)
                    return base_var.v.ReferencedLogicalVar;
                if (logical_var > 0)
                    return this;
                return var.Null;
            }
        }
    }

    internal class TacLoc { 
        public ThreeAddressCode tac;
        public int offset;
        public override string ToString()
        {
            return offset.ToString() + ": " + tac.ToString();
        }
    };

    public class ThreeAddressCode
    {
        public Dictionary<var, hardware_location> locs_at_start;
        public Dictionary<var, hardware_location> locs_at_end;
        public int block_id;
        public IList<var> uses, defs;
        public ThreeAddressCode[] remove_if_optimized;
        internal bool optimized_out_by_removal_of_another_instruction = false;

        public struct Op
        {
            public OpName Operator;
            public Assembler.CliType Type;
            public Signature.Param VT_Type;

            public Op(OpName op, Assembler.CliType type) { Operator = op; Type = type; VT_Type = null; chk_vt(); }
            public Op(OpName op, Assembler.CliType type, Signature.Param vt_type) { Operator = op; Type = type; VT_Type = vt_type; chk_vt(); }
            public static Op OpI4(OpName op) { return new Op(op, Assembler.CliType.int32); }
            public static Op OpI8(OpName op) { return new Op(op, Assembler.CliType.int64); }
            public static Op OpI(OpName op) { return new Op(op, Assembler.CliType.native_int); }
            public static Op OpR4(OpName op) { return new Op(op, Assembler.CliType.F32); }
            public static Op OpR8(OpName op) { return new Op(op, Assembler.CliType.F64); }
            public static Op OpVoid(OpName op) { return new Op(op, Assembler.CliType.void_); }
            public static Op OpNull(OpName op) { return new Op(op, Assembler.CliType.none); }
            public static Op OpVT(OpName op, Signature.Param vt_type) { return new Op(op, Assembler.CliType.vt, vt_type); }
            public static Op OpVT(OpName op) { return new Op(op, Assembler.CliType.vt, null); }

            private void chk_vt()
            {
                if (Type == Assembler.CliType.vt && VT_Type == null)
                    throw new Exception("VT Type required");
            }

            public static bool operator== (Op a, Op b)
            {
                return a.Operator == b.Operator && a.Type == b.Type;
            }

            public static bool operator !=(Op a, Op b)
            {
                return a.Operator != b.Operator || a.Type != b.Type;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Op))
                    return false;
                Op b = (Op)obj;
                return this == b;
            }

            public override int GetHashCode()
            {
                return Operator.GetHashCode() ^ Type.GetHashCode();
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Operator.ToString());
                if (Type != Assembler.CliType.none)
                {
                    sb.Append(" ");
                    switch (Type)
                    {
                        case Assembler.CliType.F32:
                            sb.Append("R4");
                            break;
                        case Assembler.CliType.F64:
                            sb.Append("R8");
                            break;
                        case Assembler.CliType.int32:
                            sb.Append("I4");
                            break;
                        case Assembler.CliType.int64:
                            sb.Append("I8");
                            break;
                        case Assembler.CliType.native_int:
                        case Assembler.CliType.O:
                        case Assembler.CliType.reference:
                            sb.Append("I");
                            break;

                        case Assembler.CliType.void_:
                            sb.Append("void");
                            break;

                        case Assembler.CliType.vt:
                            sb.Append("VT{");
                            sb.Append(VT_Type.ToString());
                            sb.Append("}");
                            break;
                    }
                }

                return sb.ToString();
            }

            public OpType OpType
            { get { return ThreeAddressCode.GetOpType(this); } }
        }
            
        public enum OpName
        {
            invalid,
            ldconst,
            add,
            add_ovf,
            add_ovf_un,
            and,
            arglist,
            cmp, cmp_un,
            br, br_ehclause,
            throweq, throwne, throw_ovf, throw_ovf_un, throwge_un, throwg_un,
            
            break_, throw_,
            ldftn,
            call,
            seteq, setne, setg, setge, setl, setle, seta, setae, setb, setbe,
            examinef, brfinite,
            conv_i4_u1zx, conv_i4_i1sx, conv_i4_u2zx, conv_i4_i2sx,
            conv_i4_i8sx, conv_i4_u8zx, conv_i4_isx, conv_i4_uzx,
            conv_i8_u1zx, conv_i8_i1sx, conv_i8_u2zx, conv_i8_i2sx,
            conv_i8_i4sx, conv_i8_u4zx, conv_i8_isx, conv_i8_uzx,
            conv_i_u1zx, conv_i_i1sx, conv_i_u2zx, conv_i_i2sx,
            conv_i_i8sx, conv_i_u8zx, conv_i_isx, conv_i_uzx,
            conv_i_i4sx, conv_i_u4zx,
            conv_r8_i8, conv_r8_i4, conv_r8_i,
            conv_i4_r8, conv_i8_r8, conv_i_r8,
            conv_r4_i8, conv_r4_i4, conv_r4_i,
            conv_i4_r4, conv_i8_r4, conv_i_r4,
            conv_r8_r4, conv_r4_r8,
            conv_u4_r8, conv_u8_r8, conv_u_r8,
            movstring,
            div, div_un,
            setstring_value,
            getstring_value,
            storestring,
            jmpmethod,
            ldobj, ldobja_ex,
            stobj,
            ldarga, ldloca, ldstra, lddataa,
            mul,
            mul_ovf,
            mul_ovf_un,
            mul_un,
            neg,
            not,
            or,
            rem,
            rem_un,
            ret,
            shl,
            shr,
            shr_un,
            sub,
            sub_ovf, sub_ovf_un,
            switch_,
            xor,
            sizeof_,
            malloc,
            assign,
            assign_to_virtftnptr,
            assign_from_virtftnptr_ptr, assign_from_virtftnptr_thisadjust,
            assign_virtftnptr,
            ldobj_virtftnptr,
            label, loc_label, instruction_label, enter, nop,
            phi,
            touch,

            peek_u1, peek_u2, peek_u4, peek_u8, peek_u, peek_i1, peek_i2, peek_r4, peek_r8, peek_vt,
            poke_u1, poke_u2, poke_u4, poke_u8, poke_u, poke_r4, poke_r8, poke_vt,

            portout_u2_u1, portout_u2_u2, portout_u2_u4, portout_u2_u8, portout_u2_u,
            portin_u2_u1, portin_u2_u2, portin_u2_u4, portin_u2_u8, portin_u2_u,

            try_acquire_i8, release_i8,

            sqrt_r8,

            alloca,
            zeromem,

            ldcatchobj, ldmethinfo, endfinally,

            la_load, la_store,
            lv_load, lv_store,

            beq, beq_un,
            bne, bneun,
            bg, bg_un,
            bge, bge_un,
            bl, bl_un,
            ble, ble_un,
            ba, ba_un,
            bae, bae_un,
            bb, bb_un,
            bbe, bbe_un,

            adjstack, save, restore,

            misc
        }

        public enum OpType { BinNumOp, UnNumOp, ConstOp, ConvOp, CmpOp, CallOp, AssignOp, InternalOp,
            OtherOptimizeOp, OtherOp, PhiOp, BrOp, ReturnOp, CmpBrOp }

        internal List<var> live_vars, live_vars_after;

        internal int CoercionCount = 0;

        internal var Result, Operand1, Operand2;
        public Op Operator;

        public bool is_float = false;

        public ThreeAddressCode(Op operator_, int vt_size) { Operator = operator_; if (vt_size != 0) VTSize = vt_size; }
        public ThreeAddressCode(Op operator_) { Operator = operator_; }
        public ThreeAddressCode() { }
        public ThreeAddressCode(Op operator_, var result, var operand1, var operand2)
        { Operator = operator_; Result = result; Operand1 = operand1; Operand2 = operand2;
#if DEBUG
            if (operator_.ToString().EndsWith("_vt")) throw new Exception(operator_.ToString() + " with no vt_size specified");
#endif
        }
        internal ThreeAddressCode(Op operator_, var result, var operand1, var operand2, int vt_size) : this(operator_, result, operand1, operand2, vt_size, false) { }
        internal ThreeAddressCode(Op operator_, var result, var operand1, var operand2, int vt_size, bool _is_float)
        { Operator = operator_; Result = result; Operand1 = operand1; Operand2 = operand2; VTSize = vt_size; is_float = _is_float; }

        internal int? VTSize;

        public ICollection<hardware_location> used_locations;
        public Dictionary<int, hardware_location> used_var_locations;

        // Some instructions require a list of all the used hardware locations to be provided
        public bool requires_used_locations_list
        {
            get
            {
                switch (Operator.Operator)
                {
                    case OpName.call:
                    case OpName.throw_:
                    case OpName.throw_ovf:
                    case OpName.throw_ovf_un:
                    case OpName.throweq:
                    case OpName.throwg_un:
                    case OpName.throwge_un:
                    case OpName.throwne:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public bool FallsThrough
        {
            get
            {
                return OpFallsThrough(Operator);
            }
        }

        public static bool OpFallsThrough(Op op)
        {
            switch (op.Operator)
            {
                case OpName.br:
                case OpName.throw_:
                case OpName.ret:
                    return false;
                default:
                    return true;
            }
        }

        public virtual ThreeAddressCode Clone()
        {
            ThreeAddressCode n = new ThreeAddressCode();
            n.Operator = Operator;
            n.Operand1 = Operand1;
            n.Operand2 = Operand2;
            n.Result = Result;
            return n;
        }

        public override string ToString()
        {
            if ((GetOpType() == OpType.AssignOp) && (Operator.Operator != OpName.assign_from_virtftnptr_ptr) && (Operator.Operator != OpName.assign_from_virtftnptr_thisadjust) &&
                (Operator.Operator != OpName.assign_to_virtftnptr))
                return Result.ToString() + " = " + Operand1.ToString() + ((IsVolatile) ? " [volatile]" : "");
            /*return ((Result > 0) ? (Result.ToString() + " = ") : "") + Operator.ToString() + "(" +
                ((Operand1.constant_val != null) ? ("$" + Operand1.constant_val.ToString()) : ((Operand1 > 0) ? Operand1.ToString() : "")) +
                (((Operand2.constant_val != null) || (Operand2 > 0)) ? (", " +
                ((Operand2.constant_val != null) ? ("$" + Operand2.constant_val.ToString()) : Operand2.ToString())) : "") + ")";*/

            string op = Operator.ToString();
            if (Operator.Type != Assembler.CliType.none)
                op += " " + Operator.ToString();
                
            if ((Operator.Operator == OpName.misc) && (this is MiscEx))
                op = ((MiscEx)this).Name;

            return ((Result.type == var.var_type.Void) ? "" : (Result.ToString() + " = ")) +
                op + "(" +
                ((Operand1.type == var.var_type.Void) ? ")" :
                (Operand1.ToString() +
                ((Operand2.type == var.var_type.Void) ? ")" :
                (", " + Operand2.ToString() + ")"))));
        }

        public string IndentedString()
        {
            if (Operator.Operator == OpName.instruction_label)
                return this.ToString();
            else if (Operator.Operator == OpName.label)
                return "  " + this.ToString();
            else
                return "    " + this.ToString();
        }

        public virtual List<var> UsedLocations()
        {
            List<var> ret = new List<var>();
            if (Result.type != var.var_type.Void)
                ret.Add(Result);
            if (Operand1.type != var.var_type.Void)
                ret.Add(Operand1);
            if (Operand2.type != var.var_type.Void)
                ret.Add(Operand2);

            return ret;
        }

        public bool IsVolatile
        {
            get
            {
                switch (Operator.Operator)
                {
                    default:
                        return false;
                }
            }
        }


        public virtual OpType GetOpType()
        {
            return GetOpType(Operator);
        }

        public static OpType GetOpType(Op op)
        {
            switch(op.Operator)
            {
                case OpName.add:
                case OpName.add_ovf:
                case OpName.add_ovf_un:
                case OpName.and:
                case OpName.div:
                case OpName.div_un:
                case OpName.mul:
                case OpName.mul_ovf:
                case OpName.mul_ovf_un:
                case OpName.or:
                case OpName.rem:
                case OpName.rem_un:
                case OpName.shl:
                case OpName.shr:
                case OpName.shr_un:
                case OpName.sub:
                case OpName.sub_ovf:
                case OpName.sub_ovf_un:
                case OpName.xor:
                    return OpType.BinNumOp;

                case OpName.neg:
                case OpName.not:
                    return OpType.UnNumOp;

                case OpName.conv_i_i1sx:
                case OpName.conv_i_i2sx:
                case OpName.conv_i_i4sx:
                case OpName.conv_i_i8sx:
                case OpName.conv_i_isx:
                case OpName.conv_i_r4:
                case OpName.conv_i_r8:
                case OpName.conv_i_u1zx:
                case OpName.conv_i_u2zx:
                case OpName.conv_i_u4zx:
                case OpName.conv_i_u8zx:
                case OpName.conv_i_uzx:
                case OpName.conv_i4_i1sx:
                case OpName.conv_i4_i2sx:
                case OpName.conv_i4_i8sx:
                case OpName.conv_i4_isx:
                case OpName.conv_i4_r4:
                case OpName.conv_i4_r8:
                case OpName.conv_i4_u1zx:
                case OpName.conv_i4_u2zx:
                case OpName.conv_i4_u8zx:
                case OpName.conv_i4_uzx:
                case OpName.conv_i8_i1sx:
                case OpName.conv_i8_i2sx:
                case OpName.conv_i8_i4sx:
                case OpName.conv_i8_isx:
                case OpName.conv_i8_r4:
                case OpName.conv_i8_r8:
                case OpName.conv_i8_u1zx:
                case OpName.conv_i8_u2zx:
                case OpName.conv_i8_u4zx:
                case OpName.conv_i8_uzx:
                case OpName.conv_r8_i4:
                case OpName.conv_r8_i8:
                    return OpType.ConvOp;

                case OpName.ldconst:
                    return OpType.ConstOp;

                case OpName.cmp:
                case OpName.cmp_un:
                    return OpType.CmpOp;

                case OpName.call:
                    return OpType.CallOp;

                case OpName.assign:
                case OpName.assign_virtftnptr:
                    return OpType.AssignOp;

                case OpName.poke_u:
                case OpName.poke_u1:
                case OpName.poke_u2:
                case OpName.poke_u4:
                case OpName.poke_u8:
                case OpName.peek_u:
                case OpName.peek_u1:
                case OpName.peek_u2:
                case OpName.peek_u4:
                case OpName.peek_u8:
                    return OpType.InternalOp;

                case OpName.phi:
                    return OpType.PhiOp;

                case OpName.ret:
                    return OpType.ReturnOp;

                case OpName.br:
                case OpName.br_ehclause:
                case OpName.brfinite:
                case OpName.ba:
                case OpName.bae:
                case OpName.bb:
                case OpName.bbe:
                case OpName.beq:
                case OpName.bg:
                case OpName.bge:
                case OpName.bl:
                case OpName.ble:
                case OpName.bne:
                    return OpType.CmpBrOp;

                default:
                    return OpType.OtherOp;
            }
        }

        internal virtual libtysila.Assembler.CliType GetOp1Type()
        {
            switch (GetOpType())
            {
                case OpType.AssignOp:
                case OpType.BinNumOp:
                case OpType.UnNumOp:
                    return Operator.Type;

                default:
                    throw new NotImplementedException();
            }
        }

        internal virtual libtysila.Assembler.CliType GetOp2Type()
        {
            switch (GetOpType())
            {
                case OpType.AssignOp:
                case OpType.BinNumOp:
                    return Operator.Type;

                default:
                    throw new NotImplementedException();
            }
        }

        internal virtual libtysila.Assembler.CliType GetResultType()
        {
            switch (GetOpType())
            {
                case OpType.AssignOp:
                case OpType.BinNumOp:
                case OpType.UnNumOp:
                case OpType.CallOp:
                    return Operator.Type;

                default:
                    throw new NotImplementedException();
            }
        }

        internal var_semantic GetResultSemantic(Assembler ass)
        {
            return ass.GetSemantic(GetResultType(), VTSize);
        }

        internal static Op Get32BitOp(Op op)
        {
            if (op.Type == Assembler.CliType.native_int)
                return new Op(op.Operator, Assembler.CliType.int32);
            else
                return op;
        }

        internal static Op Get64BitOp(Op op)
        {
            if (op.Type == Assembler.CliType.native_int)
                return new Op(op.Operator, Assembler.CliType.int64);
            else
                return op;
        }
    }

    class MiscEx : ThreeAddressCode
    {
        public string Name;
        protected Assembler.CliType _op1_type, _op2_type, _r_type;
        protected OpType _optype;

        internal MiscEx(string name, var result, var operand1, var operand2, Assembler.CliType result_type, Assembler.CliType op1_type,
            Assembler.CliType op2_type, OpType op_type) : base(Op.OpNull(OpName.misc), result, operand1, operand2)
        { 
            Name = name;
            _op1_type = op1_type;
            _op2_type = op2_type;
            _r_type = result_type;
            _optype = op_type;
        }

        internal override Assembler.CliType GetOp1Type()
        {
            return _op1_type;
        }

        internal override Assembler.CliType GetOp2Type()
        {
            return _op2_type;
        }

        public override OpType GetOpType()
        {
            return _optype;
        }

        internal override Assembler.CliType GetResultType()
        {
            return _r_type;
        }
    }

    public class LabelEx : ThreeAddressCode
    {
        public int Block_id;
        public LabelEx(int block_id) { Operator = Op.OpNull(OpName.label); Block_id = block_id; }
        private LabelEx() { }
        public static LabelEx LocalLabel(int block_id) { return new LabelEx { Operator = Op.OpNull(OpName.loc_label), Block_id = block_id }; }
        public override string ToString()
        {
            return "L" + Block_id.ToString() + ":";
        }
        public override ThreeAddressCode Clone()
        {
            return new LabelEx(Block_id);
        }
    }

    public class BrEx : ThreeAddressCode
    {
        public int Block_Target;

        public BrEx(Op operator_, int block_target) : this(operator_, block_target, false) { }

        public BrEx(Op operator_, int block_target, bool _is_float)
        {
            Operator = operator_;
            Block_Target = block_target;
            is_float = _is_float;
        }

        public override string ToString()
        {
            return Operator.ToString() + "(L" + Block_Target.ToString() + ")";
        }

        public override ThreeAddressCode Clone()
        {
            BrEx n = new BrEx(Operator, Block_Target);
            return n;
        }
    }

    class SwitchEx : ThreeAddressCode
    {
        public List<int> Block_Targets = new List<int>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Operator.ToString());
            sb.Append("(");
            for (int i = 0; i < Block_Targets.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append("L");
                sb.Append(Block_Targets[i].ToString());
            }
            sb.Append(")");
            return sb.ToString();
        }

        public SwitchEx() { Operator = Op.OpNull(OpName.switch_); }
    }

    public class CallEx : ThreeAddressCode
    {
        public var[] Var_Args;

        public CallConv call_conv = null;

        //public CallEx(var var_result, var[] var_args, string target, CallConv callconv, int vt_size) : this(var_result, var_args, target, callconv) { VTSize = vt_size; var_result.v_size = vt_size; }
        //public CallEx(var var_result, var[] var_args, string target, CallConv callconv) : this(var_result, var_args, callconv.CallTac, target, callconv) { }
        //public CallEx(var var_result, var[] var_args, var target, CallConv callconv) : this(var_result, var_args, callconv.CallTac, target, callconv) { }

        public CallEx(var var_result, var[] var_args, Op call_op, string target, CallConv callconv, int vt_size) : this(var_result, var_args, call_op, target, callconv) { VTSize = vt_size; var_result.v_size = vt_size; }
        public CallEx(var var_result, var[] var_args, Op call_op, string target, CallConv callconv)
        { Operator = call_op; Result = var_result; Var_Args = var_args; Operand1 = var.AddrOfObject(target); call_conv = callconv; }

        public CallEx(var var_result, var[] var_args, Op call_op, var target, CallConv callconv, int vt_size) : this(var_result, var_args, call_op, target, callconv) { VTSize = vt_size; var_result.v_size = vt_size; }
        public CallEx(var var_result, var[] var_args, Op call_op, var target, CallConv callconv)
        { Operator = call_op; Result = var_result; Var_Args = var_args; Operand1 = target; call_conv = callconv; }

        public override string ToString()
        {
            string p = Operand1.ToString() + ": ";

            for (int i = 0; i < Var_Args.Length; i++)
            {
                p += Var_Args[i].ToString();
                if (i < (Var_Args.Length - 1))
                    p += ", ";
            }

            return ((Result > 0) ? (Result.ToString() + " = ") : "") + Operator.ToString() + "(" + p + ")";
        }

        public override List<var> UsedLocations()
        {
            List<var> ret = base.UsedLocations();
            foreach (var p in Var_Args)
            {
                if (p.type != var.var_type.Void)
                    ret.Add(p);
            }
            return ret;
        }

        public override ThreeAddressCode Clone()
        {
            var[] va = new var[Var_Args.Length];
            Array.Copy(Var_Args, va, Var_Args.Length);
            CallEx n = new CallEx(Result, va, Operator, Operand1, call_conv);
            return n;
        }
    }

    class PhiEx2 : ThreeAddressCode
    {
        public IList<var> Var_Args;

        public PhiEx2(var v, int param_count)
        {
            Operator = Op.OpI(OpName.phi);
            Var_Args = new var[param_count];
            for (int i = 0; i < param_count; i++)
                Var_Args[i] = v;
            Result = v;
        }

        public PhiEx2(var v, IList<var> phi_params) { Operator = Op.OpI(OpName.phi); Var_Args = phi_params; Result = v; }

        public override ThreeAddressCode Clone()
        {
            return new PhiEx2(Result, Var_Args);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(Result.ToString());
            sb.Append(" = phi(");
            for (int i = 0; i < Var_Args.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(Var_Args[i].ToString());
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

    class PhiEx : ThreeAddressCode
    {
        public List<int> Var_Args;

        public enum VariableLoc { PStack, LVar, LArg };
        public VariableLoc VarLoc;
        public int VarNum;

        public PhiEx(ThreeAddressCode.Op phiop, int var_result, IEnumerable<int> var_args, VariableLoc varloc,
            int varnum)
        { Operator = phiop; Result = var_result; Var_Args = new List<int>(var_args); VarLoc = varloc; VarNum = varnum; }

        public override string ToString()
        {
            string p = "";

            for (int i = 0; i < Var_Args.Count; i++)
            {
                p += Convert.ToString(Var_Args[i]);
                if (i < (Var_Args.Count - 1))
                    p += ", ";
            }

            return ((Result > 0) ? (Result.ToString() + " = ") : "") + Operator.ToString() + "(" + p + ")";
        }

    }
}
