/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace libtysila
{
    partial class Assembler
    {
        bool is_assignment_compatible(Signature.Param dest, Signature.Param src)
        { return is_assignment_compatible(dest.Type, src.Type); }
        bool is_assignment_compatible(Signature.BaseOrComplexType dest, Signature.BaseOrComplexType src)
        {
            /* See CIL I:8.7 for the definition of assignment-compatible */

            if (dest.GetType() != src.GetType())
                return false;
            if (dest is Signature.BaseType)
            {
                Signature.BaseType btd = dest as Signature.BaseType;
                Signature.BaseType bts = src as Signature.BaseType;

                if (btd.Type == bts.Type)
                    return true;

                if (((btd.Type == BaseType_Type.U1) && (bts.Type == BaseType_Type.I1)) ||
                    ((btd.Type == BaseType_Type.I1) && (bts.Type == BaseType_Type.U1)) ||
                    ((btd.Type == BaseType_Type.Boolean) && (bts.Type == BaseType_Type.I1)) ||
                    ((btd.Type == BaseType_Type.I1) && (bts.Type == BaseType_Type.Boolean)) ||
                    ((btd.Type == BaseType_Type.Boolean) && (bts.Type == BaseType_Type.U1)) ||
                    ((btd.Type == BaseType_Type.U1) && (bts.Type == BaseType_Type.Boolean)))
                    return true;
                if (((btd.Type == BaseType_Type.U2) && (bts.Type == BaseType_Type.I2)) ||
                    ((btd.Type == BaseType_Type.I2) && (bts.Type == BaseType_Type.U2)))
                    return true;
                if (((btd.Type == BaseType_Type.U4) && (bts.Type == BaseType_Type.I4)) ||
                    ((btd.Type == BaseType_Type.I4) && (bts.Type == BaseType_Type.U4)))
                    return true;
                if (((btd.Type == BaseType_Type.U8) && (bts.Type == BaseType_Type.I8)) ||
                    ((btd.Type == BaseType_Type.I8) && (bts.Type == BaseType_Type.U8)))
                    return true;
                if (((btd.Type == BaseType_Type.U) && (bts.Type == BaseType_Type.I)) ||
                    ((btd.Type == BaseType_Type.I) && (bts.Type == BaseType_Type.U)))
                    return true;

                return false;
            }
            if (dest is Signature.ZeroBasedArray)
            {
                Signature.ZeroBasedArray zbd = dest as Signature.ZeroBasedArray;
                Signature.ZeroBasedArray zbs = src as Signature.ZeroBasedArray;
                return is_assignment_compatible(zbd.ElemType, zbs.ElemType);
            }
            if (dest is Signature.ComplexType)
            {
                Signature.ComplexType ctd = dest as Signature.ComplexType;
                Signature.ComplexType cts = src as Signature.ComplexType;

                if (ctd.Type.Value == cts.Type.Value)
                    return true;
                if (can_cast(ctd.Type, cts.Type))
                    return true;
                else
                    return false;
            }
            if (dest is Signature.GenericType)
            {
                Signature.GenericType gtd = dest as Signature.GenericType;
                Signature.GenericType gts = dest as Signature.GenericType;

                if (!is_assignment_compatible(gtd.GenType, gts.GenType))
                    return false;
                if (gtd.GenParams.Count != gts.GenParams.Count)
                    return false;
                for (int i = 0; i < gtd.GenParams.Count; i++)
                {
                    if (!is_assignment_compatible(gtd.GenParams[i], gts.GenParams[i]))
                        return false;
                }
                return true;
            }
            throw new NotSupportedException();
        }

        bool can_cast(Signature.BaseOrComplexType dest, Signature.BaseOrComplexType src)
        {
            if (src.IsRefGenericParam && dest.IsObject)
                return true;
            if (dest.IsRefGenericParam && !src.IsValueType(this))
                return true;
            if (src.IsRefGenericParam)
                return false;
            
            if ((dest is Signature.BaseType) && (src is Signature.BaseType))
                return can_cast(dest as Signature.BaseType, src as Signature.BaseType);
            if ((dest is Signature.ZeroBasedArray) && (src is Signature.ZeroBasedArray))
                return can_cast(((Signature.ZeroBasedArray)dest).ElemType, ((Signature.ZeroBasedArray)src).ElemType);

            return can_cast(Metadata.GetTypeDef(dest, this), Metadata.GetTypeDef(src, this));    
        }

        bool can_cast(Signature.BaseType dest, Signature.BaseType src)
        {
            if (dest.Type == src.Type)
                return true;
            return false;
        }

        bool can_cast(Token dest, Token src)
        { return can_cast(Metadata.GetTypeDef(dest, this), Metadata.GetTypeDef(src, this)); }

        bool can_cast(Metadata.TypeDefRow tdd, Metadata.TypeDefRow tds)
        {
            if (tdd == tds)
                return true;
            if (tds.Extends.Value != null)
                return can_cast(tdd, Metadata.GetTypeDef(tds.Extends.Value, this));
            return false;
        }
    }
}
