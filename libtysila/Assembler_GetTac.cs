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
using libasm;

namespace libtysila
{
    partial class Assembler
    {
        public static ThreeAddressCode.Op GetPhiTac(CliType ct)
        { return GetPhiTac(GetLdObjTac(ct)); }
        internal ThreeAddressCode.Op GetAssignTac(Token token, Assembler ass)
        { return GetAssignTac(GetLdObjTac(token, ass)); }
        public static ThreeAddressCode.Op GetAssignTac(CliType ct)
        { return GetAssignTac(GetLdObjTac(ct)); }
        internal ThreeAddressCode.Op GetAssignTac(var_semantic vs)
        {
            if (vs.needs_float64)
                return ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.assign);
            else if (vs.needs_float32)
                return ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.assign);
            else if (vs.needs_vtype)
                return ThreeAddressCode.Op.OpVT(ThreeAddressCode.OpName.assign);
            else if (vs.needs_intptr)
                return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign);
            else if (vs.needs_int64)
                return ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.assign);
            else if (vs.needs_int32)
                return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign);
            else
                throw new NotSupportedException();
        }
        private static ThreeAddressCode.Op GetAssignTac(ThreeAddressCode.Op ldconsttac)
        {
            return new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, ldconsttac.Type, ldconsttac.VT_Type);
        }

        public static ThreeAddressCode.Op GetPhiTac(ThreeAddressCode.Op ldconsttac)
        {
            return new ThreeAddressCode.Op(ThreeAddressCode.OpName.phi, ldconsttac.Type, ldconsttac.VT_Type);
        }

        public static ThreeAddressCode.Op GetLdObjTac(CliType clitype)
        {
            return new ThreeAddressCode.Op(ThreeAddressCode.OpName.ldobj, clitype);
        }

        private ThreeAddressCode.Op GetLdObjTac(Token token, Assembler ass)
        {
            CliType ct = CliType.void_;

            if ((token.Value is Metadata.FieldRow) || (token.Value is Metadata.MemberRefRow))
            {
                Metadata.FieldRow frow = token.Value as Metadata.FieldRow;
                if (frow == null)
                {
                    // its a memberrefrow
                    Metadata.MemberRefRow mref = token.Value as Metadata.MemberRefRow;

                    if (mref.Signature[0] == 0x06)
                    {
                        // field reference
                        Signature.Field fsig = Signature.ParseFieldSig(token.Metadata, mref.Signature, ass);
                        ct = fsig.AsParam(ass).CliType(this);
                    }
                }
                else
                {
                    Signature.Field sig = Signature.ParseFieldSig(token.Metadata, frow.Signature, ass);
                    ct = sig.AsParam(ass).CliType(this);
                }
            }
            else if ((token.Value is Metadata.TypeDefRow) || (token.Value is Metadata.TypeRefRow))
            {
                Signature.Param p = new Signature.Param(token, this);
                ct = p.CliType(this);
            }

            return GetLdObjTac(ct);
        }

        private ThreeAddressCode.Op GetStObjTac(Token token, Assembler ass)
        { return GetStObjTac(GetLdObjTac(token, ass)); }
        private ThreeAddressCode.Op GetStObjTac(CliType ct)
        { return GetStObjTac(GetLdObjTac(ct)); }
        private ThreeAddressCode.Op GetCallTac(Token token, Assembler ass)
        { return GetCallTac(GetLdObjTac(token, ass)); }
        public static ThreeAddressCode.Op GetCallTac(CliType ct)
        { if (ct == CliType.void_) return ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call); else return GetCallTac(GetLdObjTac(ct)); }

        public static ThreeAddressCode.Op GetStObjTac(ThreeAddressCode.Op ldobjtac)
        {
            return new ThreeAddressCode.Op(ThreeAddressCode.OpName.stobj, ldobjtac.Type, ldobjtac.VT_Type);
        }

        public static ThreeAddressCode.Op GetCallTac(ThreeAddressCode.Op ldobjtac)
        {
            return new ThreeAddressCode.Op(ThreeAddressCode.OpName.call, ldobjtac.Type, ldobjtac.VT_Type);
        }

        public static ThreeAddressCode.Op GetPeekTac(Signature.Param val, Assembler assembler)
        {
            ThreeAddressCode.Op poketac = GetPokeTac(val, assembler);
            switch (poketac.Operator)
            {
                case ThreeAddressCode.OpName.poke_u:
                    return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.peek_u);
                case ThreeAddressCode.OpName.poke_u1:
                    return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u1);
                case ThreeAddressCode.OpName.poke_u2:
                    return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u2);
                case ThreeAddressCode.OpName.poke_u4:
                    return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u4);
                case ThreeAddressCode.OpName.poke_u8:
                    return ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.peek_u8);
                case ThreeAddressCode.OpName.poke_r4:
                    return ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.peek_r4);
                case ThreeAddressCode.OpName.poke_r8:
                    return ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.peek_r8);
                default:
                    throw new NotSupportedException();
            }
        }

        public static ThreeAddressCode.Op GetPokeTac(Signature.Param val, Assembler assembler)
        {
            if (val.Type is Signature.BaseType)
            {
                Signature.BaseType bt = val.Type as Signature.BaseType;
                switch (bt.Type)
                {
                    case BaseType_Type.Byte:
                    case BaseType_Type.I1:
                    case BaseType_Type.U1:
                    case BaseType_Type.Boolean:
                        return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u1);
                    case BaseType_Type.Char:
                    case BaseType_Type.U2:
                    case BaseType_Type.I2:
                        return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u2);
                    case BaseType_Type.I4:
                    case BaseType_Type.U4:
                        return ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u4);
                    case BaseType_Type.I:
                    case BaseType_Type.U8:
                    case BaseType_Type.I8:
                    case BaseType_Type.Object:
                    case BaseType_Type.String:
                        return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.poke_u);
                    case BaseType_Type.R4:
                        return ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.poke_r4);
                    case BaseType_Type.R8:
                        return ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.poke_r8);
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (val.Type is Signature.ComplexType)
            {
                Signature.ComplexType ct = val.Type as Signature.ComplexType;
                if (ct.isValueType)
                {
                    if (ct.IsEnum)
                    {
                        Assembler.CliType et = ct.CliType(assembler);
                        return GetPokeTac(new Signature.Param(et), assembler);
                    }
                    return ThreeAddressCode.Op.OpVT(ThreeAddressCode.OpName.peek_vt, new Signature.Param(ct, assembler));
                }
                return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.poke_u);
            }
            else if (val.Type is Signature.BoxedType)
                return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.poke_u);
            else if (val.Type is Signature.ZeroBasedArray)
                return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.poke_u);
            else if ((val.Type is Signature.ManagedPointer) || (val.Type is Signature.UnmanagedPointer))
                return ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.poke_u);
            else
            {
                throw new Exception();
            }
        }


        internal bool FitsSByte(object o)
        {
            if (o.GetType() == typeof(Single))
                return false;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            try
            {
                if ((Convert.ToInt64(o) < SByte.MinValue) || (Convert.ToInt64(o) > SByte.MaxValue))
                    return false;
            }
            catch (OverflowException)
            {
                if (Convert.ToUInt64(o) > (ulong)SByte.MaxValue)
                    return true;
            }
            return true;
        }

        internal bool FitsByte(object o)
        {
            if (o.GetType() == typeof(Single))
                return false;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            try
            {
                if ((Convert.ToInt64(o) < Byte.MinValue) || (Convert.ToInt64(o) > Byte.MaxValue))
                    return false;
            }
            catch (OverflowException)
            {
                if (Convert.ToUInt64(o) > (ulong)Byte.MaxValue)
                    return false;
            }

            return true;
        }

        internal static bool FitsInt32(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            try
            {
                if ((Convert.ToInt64(o) < Int32.MinValue) || (Convert.ToInt64(o) > Int32.MaxValue))
                    return false;
            }
            catch (OverflowException)
            {
                if (Convert.ToUInt64(o) > (ulong)Int32.MaxValue)
                    return false;
            }
            return true;
        }

        internal static bool FitsUInt32(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            try
            {
                if ((Convert.ToInt64(o) < UInt32.MinValue) || (Convert.ToInt64(o) > UInt32.MaxValue))
                    return false;
            }
            catch (OverflowException)
            {
                if (Convert.ToUInt64(o) > (ulong)UInt32.MaxValue)
                    return false;
            }
            return true;
        }

        internal bool FitsInt16(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            try
            {
                if ((Convert.ToInt64(o) < Int16.MinValue) || (Convert.ToInt64(o) > Int16.MaxValue))
                    return false;
            }
            catch (OverflowException)
            {
                if (Convert.ToUInt64(o) > (ulong)Int16.MaxValue)
                    return false;
            }
            return true;
        }
        internal bool FitsInt12(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            try
            {
                if ((Convert.ToInt64(o) < -2048) || (Convert.ToInt64(o) > 2047))
                    return false;
            }
            catch (OverflowException)
            {
                if (Convert.ToUInt64(o) > 2047)
                    return false;
            }
            return true;
        }
        protected bool IsSigned(object o)
        {
            Type t = o.GetType();

            if ((t == typeof(IntPtr)) || (t == typeof(SByte)) ||
                (t == typeof(Int16)) || (t == typeof(Int32)) ||
                (t == typeof(Int64)) || (t == typeof(Single)) ||
                (t == typeof(Double)))
                return true;
            else if ((t == typeof(UIntPtr)) || (t == typeof(Byte)) ||
                (t == typeof(Char)) || (t == typeof(UInt16)) ||
                (t == typeof(UInt32)) || (t == typeof(UInt32)) ||
                (t == typeof(UInt64)))
                return false;
            else
                throw new NotSupportedException();
        }

    }
}
