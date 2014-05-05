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
            var ret = AddrOf(0);
            ret.address_of = false;
            ret.local_var = var_id;
            return ret;
        }

        public static var LocalArg(int arg_id)
        {
            throw new NotSupportedException();
            var ret = AddrOf(0);
            ret.address_of = false;
            ret.local_arg = arg_id;
            return ret;
        }

        public static var AddrLocalVar(int var_id)
        {
            throw new NotSupportedException();
            return AddrOf(LocalVar(var_id));
        }

        public static var AddrLocalArg(int var_id)
        {
            throw new NotSupportedException();
            return AddrOf(LocalArg(var_id));
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
        public enum Op
        {
            invalid,
            ldconst_i4, ldconst_i8, ldconst_r4, ldconst_r8, ldconst_i,
            add_i4, add_i8, add_r8, add_r4, add_i,
            add_ovf_i4, add_ovf_i8, add_ovf_i,
            add_ovf_un_i4, add_ovf_un_i8, add_ovf_un_i,
            and_i4, and_i8, and_i,
            arglist,
            cmp_i4, cmp_i8, cmp_i, cmp_r8, cmp_r4, cmp_r8_un, cmp_r4_un,
            br, beq, bne, bg, bge, bl, ble, ba, bae, bb, bbe, br_ehclause,
            throweq, throwne, throw_ovf, throw_ovf_un, throwge_un, throwg_un,
            break_, throw_,
            ldftn,
            call_i4, call_i8, call_i, call_r4, call_r8, call_void,
            call_vt,
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
            div_i4, div_i8, div_i, div_r8, div_r4, div_u4, div_u8, div_u,
            setstring_value,
            getstring_value,
            storestring,
            jmpmethod,
            ldobj_i4, ldobj_i8, ldobj_r4, ldobj_r8, ldobj_i, ldobj_vt, ldobja_ex_i,
            stobj_i4, stobj_i8, stobj_r4, stobj_r8, stobj_i, stobj_vt,
            ldarga, ldloca, ldstra, lddataa,
            mul_i4, mul_i8, mul_i, mul_r8, mul_r4,
            mul_ovf_i4, mul_ovf_i8, mul_ovf_i,
            mul_ovf_un_i4, mul_ovf_un_i8, mul_ovf_un_i,
            mul_un_i4, mul_un_i8, mul_un_i,
            neg_i4, neg_i8, neg_i, neg_r8, neg_r4,
            not_i4, not_i8, not_i,
            or_i4, or_i8, or_i,
            rem_i4, rem_i8, rem_i, rem_r8, rem_r4,
            rem_un_i4, rem_un_i8, rem_un_i,
            ret_void, ret_i4, ret_i8, ret_i, ret_r8, ret_vt,
            shl_i4, shl_i8, shl_i,
            shr_i4, shr_i8, shr_i,
            shr_un_i4, shr_un_i8, shr_un_i,
            sub_i4, sub_i8, sub_i, sub_r8, sub_r4,
            sub_ovf_i, sub_ovf_un_i,
            switch_,
            xor_i4, xor_i8, xor_i,
            sizeof_,
            malloc,
            assign_i4, assign_i8, assign_r4, assign_r8, assign_i, assign_vt,
            assign_v_i4, assign_v_i8, assign_v_i,
            assign_to_virtftnptr,
            assign_from_virtftnptr_ptr, assign_from_virtftnptr_thisadjust,
            assign_virtftnptr,
            ldobj_virtftnptr,
            label, loc_label, instruction_label, enter, nop,
            phi_i, phi_i4, phi_i8, phi_r4, phi_r8, phi_vt,

            peek_u1, peek_u2, peek_u4, peek_u8, peek_u, peek_i1, peek_i2, peek_r4, peek_r8,
            poke_u1, poke_u2, poke_u4, poke_u8, poke_u, poke_r4, poke_r8,

            portout_u2_u1, portout_u2_u2, portout_u2_u4, portout_u2_u8, portout_u2_u,
            portin_u2_u1, portin_u2_u2, portin_u2_u4, portin_u2_u8, portin_u2_u,

            try_acquire_i8, release_i8,

            sqrt_r8,

            alloca_i4, alloca_i,
            zeromem,

            ldcatchobj, ldmethinfo, endfinally,

            localarg,

            beq_i4, beq_i8, beq_i, beq_r8, beq_r4, beq_r8_un, beq_r4_un,
            bne_i4, bne_i8, bne_i, bne_r8, bne_r4, bne_r8_un, bne_r4_un,
            bg_i4, bg_i8, bg_i, bg_r8, bg_r4, bg_r8_un, bg_r4_un,
            bge_i4, bge_i8, bge_i, bge_r8, bge_r4, bge_r8_un, bge_r4_un,
            bl_i4, bl_i8, bl_i, bl_r8, bl_r4, bl_r8_un, bl_r4_un,
            ble_i4, ble_i8, ble_i, ble_r8, ble_r4, ble_r8_un, ble_r4_un,
            ba_i4, ba_i8, ba_i, ba_r8, ba_r4, ba_r8_un, ba_r4_un,
            bae_i4, bae_i8, bae_i, bae_r8, bae_r4, bae_r8_un, bae_r4_un,
            bb_i4, bb_i8, bb_i, bb_r8, bb_r4, bb_r8_un, bb_r4_un,
            bbe_i4, bbe_i8, bbe_i, bbe_r8, bbe_r4, bbe_r8_un, bbe_r4_un,

            adjstack, save, restore,

            misc
        }

        public enum OpType { BinNumOp, UnNumOp, ConstOp, ConvOp, CmpOp, CallOp, AssignOp, InternalOp,
            OtherOptimizeOp, OtherOp, PhiOp, BrOp, ReturnOp, CmpBrOp }

        internal List<var> live_vars, live_vars_after;

        internal int CoercionCount = 0;

        internal var Result, Operand1, Operand2;
        public Op Operator;

        public Assembler.cfg_node node;

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
                switch (Operator)
                {
                    case Op.call_i:
                    case Op.call_i4:
                    case Op.call_i8:
                    case Op.call_r4:
                    case Op.call_r8:
                    case Op.call_void:
                    case Op.call_vt:
                    case Op.throw_:
                    case Op.throw_ovf:
                    case Op.throw_ovf_un:
                    case Op.throweq:
                    case Op.throwg_un:
                    case Op.throwge_un:
                    case Op.throwne:
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
            switch (op)
            {
                case Op.br:
                case Op.br_ehclause:
                case Op.throw_:
                case Op.ret_i:
                case Op.ret_i4:
                case Op.ret_i8:
                case Op.ret_r8:
                case Op.ret_void:
                case Op.ret_vt:
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
            if ((GetOpType() == OpType.AssignOp) && (Operator != Op.assign_from_virtftnptr_ptr) && (Operator != Op.assign_from_virtftnptr_thisadjust) &&
                (Operator != Op.assign_to_virtftnptr))
                return Result.ToString() + " = " + Operand1.ToString() + ((IsVolatile) ? " [volatile]" : "");
            /*return ((Result > 0) ? (Result.ToString() + " = ") : "") + Operator.ToString() + "(" +
                ((Operand1.constant_val != null) ? ("$" + Operand1.constant_val.ToString()) : ((Operand1 > 0) ? Operand1.ToString() : "")) +
                (((Operand2.constant_val != null) || (Operand2 > 0)) ? (", " +
                ((Operand2.constant_val != null) ? ("$" + Operand2.constant_val.ToString()) : Operand2.ToString())) : "") + ")";*/

            string op = Operator.ToString();
            if ((Operator == Op.misc) && (this is MiscEx))
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
            if (Operator == Op.instruction_label)
                return this.ToString();
            else if (Operator == Op.label)
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
                switch (Operator)
                {
                    case Op.assign_v_i:
                    case Op.assign_v_i4:
                    case Op.assign_v_i8:
                        return true;
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
            switch(op)
            {
                case Op.add_i:
                case Op.add_i4:
                case Op.add_i8:
                case Op.add_ovf_i:
                case Op.add_ovf_i4:
                case Op.add_ovf_i8:
                case Op.add_ovf_un_i:
                case Op.add_ovf_un_i4:
                case Op.add_ovf_un_i8:
                case Op.add_r8:
                case Op.add_r4:
                case Op.and_i:
                case Op.and_i4:
                case Op.and_i8:
                case Op.div_i:
                case Op.div_i4:
                case Op.div_i8:
                case Op.div_r8:
                case Op.div_r4:
                case Op.div_u:
                case Op.div_u4:
                case Op.div_u8:
                case Op.mul_i:
                case Op.mul_i4:
                case Op.mul_i8:
                case Op.mul_ovf_i:
                case Op.mul_ovf_i4:
                case Op.mul_ovf_i8:
                case Op.mul_ovf_un_i:
                case Op.mul_ovf_un_i4:
                case Op.mul_ovf_un_i8:
                case Op.mul_r8:
                case Op.mul_r4:
                case Op.mul_un_i:
                case Op.mul_un_i4:
                case Op.mul_un_i8:
                case Op.or_i:
                case Op.or_i4:
                case Op.or_i8:
                case Op.rem_i:
                case Op.rem_i4:
                case Op.rem_i8:
                case Op.rem_r8:
                case Op.rem_r4:
                case Op.rem_un_i:
                case Op.rem_un_i4:
                case Op.rem_un_i8:
                case Op.shl_i:
                case Op.shl_i4:
                case Op.shl_i8:
                case Op.shr_i:
                case Op.shr_i4:
                case Op.shr_i8:
                case Op.shr_un_i:
                case Op.shr_un_i4:
                case Op.shr_un_i8:
                case Op.sub_i:
                case Op.sub_i4:
                case Op.sub_i8:
                case Op.sub_ovf_i:
                case Op.sub_ovf_un_i:
                case Op.sub_r8:
                case Op.sub_r4:
                case Op.xor_i:
                case Op.xor_i4:
                case Op.xor_i8:
                    return OpType.BinNumOp;

                case Op.neg_i:
                case Op.neg_i4:
                case Op.neg_i8:
                case Op.neg_r8:
                case Op.not_i:
                case Op.not_i4:
                case Op.not_i8:
                    return OpType.UnNumOp;

                case Op.conv_i_i1sx:
                case Op.conv_i_i2sx:
                case Op.conv_i_i4sx:
                case Op.conv_i_i8sx:
                case Op.conv_i_isx:
                case Op.conv_i_r4:
                case Op.conv_i_r8:
                case Op.conv_i_u1zx:
                case Op.conv_i_u2zx:
                case Op.conv_i_u4zx:
                case Op.conv_i_u8zx:
                case Op.conv_i_uzx:
                case Op.conv_i4_i1sx:
                case Op.conv_i4_i2sx:
                case Op.conv_i4_i8sx:
                case Op.conv_i4_isx:
                case Op.conv_i4_r4:
                case Op.conv_i4_r8:
                case Op.conv_i4_u1zx:
                case Op.conv_i4_u2zx:
                case Op.conv_i4_u8zx:
                case Op.conv_i4_uzx:
                case Op.conv_i8_i1sx:
                case Op.conv_i8_i2sx:
                case Op.conv_i8_i4sx:
                case Op.conv_i8_isx:
                case Op.conv_i8_r4:
                case Op.conv_i8_r8:
                case Op.conv_i8_u1zx:
                case Op.conv_i8_u2zx:
                case Op.conv_i8_u4zx:
                case Op.conv_i8_uzx:
                case Op.conv_r8_i4:
                case Op.conv_r8_i8:
                    return OpType.ConvOp;

                case Op.ldconst_i:
                case Op.ldconst_i4:
                case Op.ldconst_i8:
                case Op.ldconst_r4:
                case Op.ldconst_r8:
                    return OpType.ConstOp;

                case Op.cmp_i:
                case Op.cmp_i4:
                case Op.cmp_i8:
                case Op.cmp_r8:
                case Op.cmp_r4:
                case Op.cmp_r4_un:
                case Op.cmp_r8_un:
                    return OpType.CmpOp;

                case Op.call_i:
                case Op.call_i4:
                case Op.call_i8:
                case Op.call_r4:
                case Op.call_r8:
                case Op.call_void:
                case Op.call_vt:
                    return OpType.CallOp;

                case Op.assign_i:
                case Op.assign_i4:
                case Op.assign_i8:
                case Op.assign_r4:
                case Op.assign_r8:
                case Op.assign_vt:
                case Op.assign_virtftnptr:
                case Op.assign_v_i:
                case Op.assign_v_i4:
                case Op.assign_v_i8:
                    return OpType.AssignOp;

                case Op.poke_u:
                case Op.poke_u1:
                case Op.poke_u2:
                case Op.poke_u4:
                case Op.poke_u8:
                case Op.peek_u:
                case Op.peek_u1:
                case Op.peek_u2:
                case Op.peek_u4:
                case Op.peek_u8:
                    return OpType.InternalOp;

                case Op.phi_i:
                case Op.phi_i4:
                case Op.phi_i8:
                case Op.phi_r4:
                case Op.phi_r8:
                case Op.phi_vt:
                    return OpType.PhiOp;

                case Op.ret_i:
                case Op.ret_i4:
                case Op.ret_i8:
                case Op.ret_r8:
                    return OpType.ReturnOp;

                case Op.br:
                case Op.br_ehclause:
                case Op.brfinite:
                case Op.ba:
                case Op.bae:
                case Op.bb:
                case Op.bbe:
                case Op.beq:
                case Op.bg:
                case Op.bge:
                case Op.bl:
                case Op.ble:
                case Op.bne:
                    return OpType.BrOp;

                case Op.beq_i4:
                case Op.beq_i8:
                case Op.beq_i:
                case Op.beq_r8:
                case Op.beq_r4:
                case Op.beq_r8_un:
                case Op.beq_r4_un:
                case Op.bne_i4:
                case Op.bne_i8:
                case Op.bne_i:
                case Op.bne_r8:
                case Op.bne_r4:
                case Op.bne_r8_un:
                case Op.bne_r4_un:
                case Op.bg_i4:
                case Op.bg_i8:
                case Op.bg_i:
                case Op.bg_r8:
                case Op.bg_r4:
                case Op.bg_r8_un:
                case Op.bg_r4_un:
                case Op.bge_i4:
                case Op.bge_i8:
                case Op.bge_i:
                case Op.bge_r8:
                case Op.bge_r4:
                case Op.bge_r8_un:
                case Op.bge_r4_un:
                case Op.bl_i4:
                case Op.bl_i8:
                case Op.bl_i:
                case Op.bl_r8:
                case Op.bl_r4:
                case Op.bl_r8_un:
                case Op.bl_r4_un:
                case Op.ble_i4:
                case Op.ble_i8:
                case Op.ble_i:
                case Op.ble_r8:
                case Op.ble_r4:
                case Op.ble_r8_un:
                case Op.ble_r4_un:
                case Op.ba_i4:
                case Op.ba_i8:
                case Op.ba_i:
                case Op.ba_r8:
                case Op.ba_r4:
                case Op.ba_r8_un:
                case Op.ba_r4_un:
                case Op.bae_i4:
                case Op.bae_i8:
                case Op.bae_i:
                case Op.bae_r8:
                case Op.bae_r4:
                case Op.bae_r8_un:
                case Op.bae_r4_un:
                case Op.bb_i4:
                case Op.bb_i8:
                case Op.bb_i:
                case Op.bb_r8:
                case Op.bb_r4:
                case Op.bb_r8_un:
                case Op.bb_r4_un:
                case Op.bbe_i4:
                case Op.bbe_i8:
                case Op.bbe_i:
                case Op.bbe_r8:
                case Op.bbe_r4:
                case Op.bbe_r8_un:
                case Op.bbe_r4_un:
                    return OpType.CmpBrOp;

                default:
                    return OpType.OtherOp;
            }
        }

        internal virtual libtysila.Assembler.CliType GetOp1Type()
        {
            switch (Operator)
            {
                case Op.add_i:
                case Op.add_ovf_i:
                case Op.add_ovf_un_i:
                case Op.and_i:
                case Op.assign_i:
                case Op.cmp_i:
                case Op.conv_i_i1sx:
                case Op.conv_i_i2sx:
                case Op.conv_i_i4sx:
                case Op.conv_i_i8sx:
                case Op.conv_i_isx:
                case Op.conv_i_r4:
                case Op.conv_i_r8:
                case Op.conv_i_u1zx:
                case Op.conv_i_u2zx:
                case Op.conv_i_u4zx:
                case Op.conv_i_u8zx:
                case Op.conv_i_uzx:
                case Op.conv_u_r8:
                case Op.div_i:
                case Op.div_u:
                case Op.mul_i:
                case Op.mul_ovf_i:
                case Op.mul_ovf_un_i:
                case Op.mul_un_i:
                case Op.neg_i:
                case Op.not_i:
                case Op.or_i:
                case Op.rem_i:
                case Op.shl_i:
                case Op.shr_i:
                case Op.shr_un_i:
                case Op.sub_i:
                case Op.sub_ovf_i:
                case Op.sub_ovf_un_i:
                case Op.xor_i:
                case Op.zeromem:
                    return Assembler.CliType.native_int;

                case Op.add_i4:
                case Op.add_ovf_i4:
                case Op.add_ovf_un_i4:
                case Op.and_i4:
                case Op.assign_i4:
                case Op.cmp_i4:
                case Op.conv_i4_i1sx:
                case Op.conv_i4_i2sx:
                case Op.conv_i4_i8sx:
                case Op.conv_i4_isx:
                case Op.conv_i4_r4:
                case Op.conv_i4_r8:
                case Op.conv_i4_u1zx:
                case Op.conv_i4_u2zx:
                case Op.conv_i4_u8zx:
                case Op.conv_i4_uzx:
                case Op.conv_u4_r8:
                case Op.div_i4:
                case Op.div_u4:
                case Op.mul_i4:
                case Op.mul_ovf_i4:
                case Op.mul_ovf_un_i4:
                case Op.mul_un_i4:
                case Op.neg_i4:
                case Op.not_i4:
                case Op.or_i4:
                case Op.rem_i4:
                case Op.rem_un_i4:
                case Op.shl_i4:
                case Op.shr_i4:
                case Op.shr_un_i4:
                case Op.sub_i4:
                case Op.xor_i4:
                    return Assembler.CliType.int32;

                case Op.add_i8:
                case Op.add_ovf_i8:
                case Op.add_ovf_un_i8:
                case Op.assign_i8:
                case Op.cmp_i8:
                case Op.conv_i8_i1sx:
                case Op.conv_i8_i2sx:
                case Op.conv_i8_i4sx:
                case Op.conv_i8_isx:
                case Op.conv_i8_r4:
                case Op.conv_i8_r8:
                case Op.conv_i8_u1zx:
                case Op.conv_i8_u2zx:
                case Op.conv_i8_u4zx:
                case Op.conv_i8_uzx:
                case Op.div_i8:
                case Op.div_u8:
                case Op.mul_i8:
                case Op.mul_ovf_i8:
                case Op.mul_ovf_un_i8:
                case Op.mul_un_i8:
                case Op.neg_i8:
                case Op.not_i8:
                case Op.or_i8:
                case Op.rem_i8:
                case Op.rem_un_i8:
                case Op.ret_i8:
                case Op.shl_i8:
                case Op.shr_i8:
                case Op.shr_un_i8:
                case Op.sub_i8:
                case Op.xor_i8:
                    return Assembler.CliType.int64;

                default:
                    throw new NotSupportedException();
            }
        }

        internal virtual libtysila.Assembler.CliType GetOp2Type()
        {
            switch (Operator)
            {
                case Op.add_i:
                case Op.add_ovf_i:
                case Op.add_ovf_un_i:
                case Op.and_i:
                case Op.cmp_i:
                case Op.div_i:
                case Op.div_u:
                case Op.mul_i:
                case Op.mul_ovf_i:
                case Op.mul_ovf_un_i:
                case Op.mul_un_i:
                case Op.or_i:
                case Op.rem_i:
                case Op.sub_i:
                case Op.sub_ovf_i:
                case Op.sub_ovf_un_i:
                case Op.xor_i:
                    return Assembler.CliType.native_int;

                case Op.add_i4:
                case Op.add_ovf_i4:
                case Op.add_ovf_un_i4:
                case Op.and_i4:
                case Op.cmp_i4:
                case Op.div_i4:
                case Op.div_u4:
                case Op.mul_i4:
                case Op.mul_ovf_i4:
                case Op.mul_ovf_un_i4:
                case Op.mul_un_i4:
                case Op.or_i4:
                case Op.rem_i4:
                case Op.rem_un_i4:
                case Op.shl_i4:
                case Op.shr_i4:
                case Op.shr_un_i4:
                case Op.shl_i:
                case Op.shl_i8:
                case Op.shr_i:
                case Op.shr_i8:
                case Op.shr_un_i:
                case Op.shr_un_i8:
                case Op.sub_i4:
                case Op.xor_i4:
                case Op.zeromem:
                    return Assembler.CliType.int32;

                case Op.add_i8:
                case Op.add_ovf_i8:
                case Op.add_ovf_un_i8:
                case Op.cmp_i8:
                case Op.div_i8:
                case Op.div_u8:
                case Op.mul_i8:
                case Op.mul_ovf_i8:
                case Op.mul_ovf_un_i8:
                case Op.mul_un_i8:
                case Op.or_i8:
                case Op.rem_i8:
                case Op.rem_un_i8:
                case Op.ret_i8:
                case Op.sub_i8:
                case Op.xor_i8:
                    return Assembler.CliType.int64;

                case Op.assign_i:
                case Op.assign_i4:
                case Op.assign_i8:
                case Op.assign_r4:
                case Op.assign_r8:
                case Op.conv_i_i1sx:
                case Op.conv_i_i2sx:
                case Op.conv_i_i4sx:
                case Op.conv_i_i8sx:
                case Op.conv_i_isx:
                case Op.conv_i_r4:
                case Op.conv_i_r8:
                case Op.conv_i_u1zx:
                case Op.conv_i_u2zx:
                case Op.conv_i_u4zx:
                case Op.conv_i_u8zx:
                case Op.conv_i_uzx:
                case Op.conv_i4_i1sx:
                case Op.conv_i4_i2sx:
                case Op.conv_i4_i8sx:
                case Op.conv_i4_isx:
                case Op.conv_i4_r4:
                case Op.conv_i4_r8:
                case Op.conv_i4_u1zx:
                case Op.conv_i4_u2zx:
                case Op.conv_i4_u8zx:
                case Op.conv_i4_uzx:
                case Op.conv_i8_i1sx:
                case Op.conv_i8_i2sx:
                case Op.conv_i8_i4sx:
                case Op.conv_i8_isx:
                case Op.conv_i8_r4:
                case Op.conv_i8_r8:
                case Op.conv_i8_u1zx:
                case Op.conv_i8_u2zx:
                case Op.conv_i8_u4zx:
                case Op.conv_i8_uzx:
                case Op.conv_r4_i:
                case Op.conv_r4_i4:
                case Op.conv_r4_i8:
                case Op.conv_r4_r8:
                case Op.conv_r8_i:
                case Op.conv_r8_i4:
                case Op.conv_r8_i8:
                case Op.conv_r8_r4:
                case Op.conv_u_r8:
                case Op.conv_u4_r8:
                case Op.conv_u8_r8:
                    return Assembler.CliType.void_;

                default:
                    throw new NotSupportedException();
            }
        }

        internal virtual libtysila.Assembler.CliType GetResultType()
        {
            switch (Operator)
            {
                case Op.add_i:
                case Op.add_ovf_i:
                case Op.add_ovf_un_i:
                case Op.and_i:
                case Op.assign_i:
                case Op.assign_v_i:
                case Op.call_i:
                case Op.conv_i_isx:
                case Op.conv_i_uzx:
                case Op.conv_i4_isx:
                case Op.conv_i4_uzx:
                case Op.conv_i8_isx:
                case Op.conv_i8_uzx:
                case Op.div_i:
                case Op.div_u:
                case Op.ldarga:
                case Op.ldconst_i:
                case Op.lddataa:
                case Op.ldftn:
                case Op.ldloca:
                case Op.ldobj_i:
                case Op.ldstra:
                case Op.mul_i:
                case Op.mul_ovf_i:
                case Op.mul_ovf_un_i:
                case Op.mul_un_i:
                case Op.neg_i:
                case Op.not_i:
                case Op.or_i:
                case Op.rem_i:
                case Op.rem_un_i:
                case Op.shl_i:
                case Op.shr_i:
                case Op.shr_un_i:
                case Op.sub_i:
                case Op.sub_ovf_i:
                case Op.sub_ovf_un_i:
                case Op.xor_i:
                case Op.malloc:
                case Op.peek_u:
                case Op.assign_from_virtftnptr_ptr:
                case Op.assign_from_virtftnptr_thisadjust:
                case Op.portin_u2_u:
                case Op.ldcatchobj:
                case Op.ldmethinfo:
                    return Assembler.CliType.native_int;

                case Op.alloca_i:
                case Op.alloca_i4:
                    return Assembler.CliType.reference;

                case Op.add_i4:
                case Op.add_ovf_i4:
                case Op.add_ovf_un_i4:
                case Op.and_i4:
                case Op.assign_i4:
                case Op.assign_v_i4:
                case Op.call_i4:
                case Op.conv_i_i1sx:
                case Op.conv_i_i2sx:
                case Op.conv_i_i4sx:
                case Op.conv_i_u1zx:
                case Op.conv_i_u2zx:
                case Op.conv_i_u4zx:
                case Op.conv_i4_i1sx:
                case Op.conv_i4_i2sx:
                case Op.conv_i4_u1zx:
                case Op.conv_i4_u2zx:
                case Op.conv_i8_i1sx:
                case Op.conv_i8_i2sx:
                case Op.conv_i8_i4sx:
                case Op.conv_i8_u1zx:
                case Op.conv_i8_u2zx:
                case Op.conv_i8_u4zx:
                case Op.conv_r8_i4:
                case Op.conv_r4_i4:
                case Op.div_i4:
                case Op.div_u4:
                case Op.ldconst_i4:
                case Op.ldobj_i4:
                case Op.mul_i4:
                case Op.mul_ovf_i4:
                case Op.mul_ovf_un_i4:
                case Op.mul_un_i4:
                case Op.neg_i4:
                case Op.not_i4:
                case Op.or_i4:
                case Op.rem_i4:
                case Op.rem_un_i4:
                case Op.shl_i4:
                case Op.shr_i4:
                case Op.shr_un_i4:
                case Op.sub_i4:
                case Op.xor_i4:
                case Op.seta:
                case Op.setae:
                case Op.setb:
                case Op.setbe:
                case Op.seteq:
                case Op.setg:
                case Op.setge:
                case Op.setl:
                case Op.setle:
                case Op.setne:
                case Op.peek_u4:
                case Op.peek_u2:
                case Op.peek_u1:
                case Op.peek_i2:
                case Op.peek_i1:
                case Op.portin_u2_u4:
                case Op.portin_u2_u2:
                case Op.portin_u2_u1:
                case Op.try_acquire_i8:
                    return Assembler.CliType.int32;

                case Op.add_i8:
                case Op.add_ovf_i8:
                case Op.add_ovf_un_i8:
                case Op.and_i8:
                case Op.assign_i8:
                case Op.assign_v_i8:
                case Op.call_i8:
                case Op.conv_i_i8sx:
                case Op.conv_i_u8zx:
                case Op.conv_i4_i8sx:
                case Op.conv_i4_u8zx:
                case Op.conv_r8_i8:
                case Op.div_i8:
                case Op.div_u8:
                case Op.ldconst_i8:
                case Op.ldobj_i8:
                case Op.mul_i8:
                case Op.mul_ovf_i8:
                case Op.mul_ovf_un_i8:
                case Op.mul_un_i8:
                case Op.neg_i8:
                case Op.not_i8:
                case Op.or_i8:
                case Op.rem_i8:
                case Op.rem_un_i8:
                case Op.shl_i8:
                case Op.shr_i8:
                case Op.shr_un_i8:
                case Op.sub_i8:
                case Op.xor_i8:
                case Op.peek_u8:
                case Op.portin_u2_u8:
                    return Assembler.CliType.int64;

                case Op.add_r8:
                case Op.assign_r8:
                case Op.call_r8:
                case Op.conv_i_r8:
                case Op.conv_i4_r8:
                case Op.conv_i8_r8:
                case Op.conv_r4_r8:
                case Op.div_r8:
                case Op.ldconst_r8:
                case Op.ldobj_r8:
                case Op.mul_r8:
                case Op.neg_r8:
                case Op.rem_r8:
                case Op.sub_r8:
                case Op.sqrt_r8:
                case Op.peek_r8:
                    return Assembler.CliType.F64;

                case Op.add_r4:
                case Op.assign_r4:
                case Op.conv_r8_r4:
                case Op.div_r4:
                case Op.mul_r4:
                case Op.neg_r4:
                case Op.rem_r4:
                case Op.sub_r4:
                case Op.call_r4:
                case Op.conv_i_r4:
                case Op.conv_i4_r4:
                case Op.conv_i8_r4:
                case Op.ldconst_r4:
                case Op.ldobj_r4:
                case Op.peek_r4:
                    return Assembler.CliType.F32;

                case Op.ldobj_vt:
                case Op.assign_vt:
                case Op.call_vt:
                    return Assembler.CliType.vt;

                case Op.assign_to_virtftnptr:
                case Op.assign_virtftnptr:
                    return Assembler.CliType.virtftnptr;

                case Op.misc:
                case Op.zeromem:
                    return Assembler.CliType.void_;

                default:
                    throw new NotSupportedException();
            }
        }

        internal var_semantic GetResultSemantic(Assembler ass)
        {
            return ass.GetSemantic(GetResultType(), VTSize);
        }
    }

    class MiscEx : ThreeAddressCode
    {
        public string Name;
        protected Assembler.CliType _op1_type, _op2_type, _r_type;
        protected OpType _optype;

        internal MiscEx(string name, var result, var operand1, var operand2, Assembler.CliType result_type, Assembler.CliType op1_type,
            Assembler.CliType op2_type, OpType op_type) : base(Op.misc, result, operand1, operand2)
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
        public LabelEx(int block_id) { Operator = Op.label; Block_id = block_id; }
        private LabelEx() { }
        public static LabelEx LocalLabel(int block_id) { return new LabelEx { Operator = Op.loc_label, Block_id = block_id }; }
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

        public SwitchEx() { Operator = Op.switch_; }
    }

    class InstructionLabelEx : ThreeAddressCode
    {
        public InstructionHeader instr;

        public override string ToString()
        {
            return instr.ToString();
        }

        public InstructionLabelEx(InstructionHeader inst) : base(Op.instruction_label, var.Null, var.Null, var.Null) { instr = inst; }
    }

    public class CallEx : ThreeAddressCode
    {
        public var[] Var_Args;

        public CallConv call_conv = null;

        public CallEx(var var_result, var[] var_args, string target, CallConv callconv, int vt_size) : this(var_result, var_args, target, callconv) { VTSize = vt_size; var_result.v_size = vt_size; }
        public CallEx(var var_result, var[] var_args, string target, CallConv callconv) : this(var_result, var_args, callconv.CallTac, target, callconv) { }
        public CallEx(var var_result, var[] var_args, var target, CallConv callconv) : this(var_result, var_args, callconv.CallTac, target, callconv) { }

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
            Operator = Op.phi_i;
            Var_Args = new var[param_count];
            for (int i = 0; i < param_count; i++)
                Var_Args[i] = v;
            Result = v;
        }

        public PhiEx2(var v, IList<var> phi_params) { Operator = Op.phi_i; Var_Args = phi_params; Result = v; }

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

    class IrDump
    {
        IEnumerable<libtysila.Assembler.cfg_node> _nodes;
        public IrDump(IEnumerable<libtysila.Assembler.cfg_node> nodes) { _nodes = nodes; }

        public string Ir
        {
            get
            {
                StringBuilder dbg1sb = new StringBuilder();
                foreach (Assembler.cfg_node node in _nodes)
                {
                    if (node.optimized_ir != null)
                    {
                        foreach (ThreeAddressCode ir1 in node.optimized_ir)
                            dbg1sb.Append(ir1.IndentedString() + Environment.NewLine);
                    }
                    else
                    {
                        foreach (ThreeAddressCode ir1 in node.tacs)
                            dbg1sb.Append(ir1.IndentedString() + Environment.NewLine);
                    }
                }
                return dbg1sb.ToString();
            }
        }
    }
}
