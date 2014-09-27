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
    public class Signature
    {
        public class CompareOpts
        {
            public bool CheckByRef = true;
        }

        public class HasCustomMods
        {
            public List<CustomMod> CustomMods = new List<CustomMod>();
        }

        public class CustomMod
        {
            public enum CustomModType
            {
                Optional, Required
            }

            public CustomModType TypeOfCustomMod = CustomModType.Optional;

            public Metadata.TableIndex Type;

            public override bool Equals(object obj)
            {
                throw new NotSupportedException("Use Signature.CustomModCompare");
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        public class LocalVars
        {
            public List<Param> Vars = new List<Param>();
        }

        public class Param : HasCustomMods
        {
            public BaseOrComplexType Type;
            public bool Pinned = false;
            public string name = null;

            Assembler _ass;

            public override bool Equals(object obj)
            {
                throw new NotSupportedException("Use Signature.ParamCompare");
            }
            public override int GetHashCode()
            {
                return Type.GetHashCode();
            }

            public Param(Assembler ass) { _ass = ass; }
            public Param(Assembler.CliType clitype) : this(null)
            { Type = new BaseType(clitype); }
            public Param(BaseType_Type basetype) : this(null)
            { Type = new BaseType(basetype); }
            public Param(BaseType_Type basetype, string n)
                : this(null)
            { Type = new BaseType(basetype); name = n; }
            public Param(BaseType_Type basetype, int ugp_idx, string n, Assembler ass)
                : this(ass)
            { Type = new BaseType(basetype, ugp_idx); name = n; }            
            public Param(Metadata.ITableRow trow, Assembler ass) : this(ass)
            {
                from_tok(new Token(trow), ass);
            }
            public Param(Token t, Assembler ass) : this(ass)
            {
                from_tok(t, ass);
            }
            public Param(Signature.BaseOrComplexType bct, Assembler ass) : this(ass)
            {
                if (bct is BaseType)
                    Type = new BaseType(((BaseType)bct).Type);
                else if (bct is ComplexType)
                    from_tok(((ComplexType)bct).Type, ass);
                else
                    Type = bct;
            }
            void from_tok(Token t, Assembler ass)
            {
                Type = null;
                if (t is TTCToken)
                {
                    Type = ((TTCToken)t).ttc.tsig.Type;
                    return;
                }
                if (t.Value is Metadata.TypeRefRow)
                {
                    Metadata.TypeRefRow trr = t.Value as Metadata.TypeRefRow;
                    if (trr.ResolutionScope.Value is Metadata.AssemblyRefRow)
                    {
                        Metadata.AssemblyRefRow arr = trr.ResolutionScope.Value as Metadata.AssemblyRefRow;
                        if (arr.Name == "mscorlib")
                            check_basetype(trr.TypeFullName);
                    }
                }
                else if (t.Value is Metadata.TypeSpecRow)
                {
                    int offset = 0;
                    Type = ParseTypeSig(t.Metadata, ((Metadata.TypeSpecRow)t.Value).Signature, ref offset, ass);
                }
                else if (t.Value is Metadata.TypeDefRow)
                    check_basetype(((Metadata.TypeDefRow)t.Value).TypeFullName);
                if (Type == null)
                {
                    Type = new ComplexType(t, ass);
                }
            }

            public void IdentifyBasetype(Assembler ass)
            {
                if (Type is ComplexType)
                {
                    ComplexType ct = Type as ComplexType;
                    if (ct.Type.Metadata.ModuleName == "mscorlib")
                    {
                        Metadata.TypeDefRow tdr = Metadata.GetTypeDef(ct.Type, ass);
                        check_basetype(tdr.TypeFullName);
                    }
                }
            }

            public static BaseType MakeBaseType(string s)
            {
                Param p = new Param(null);
                p.check_basetype(s);
                if (p.Type is BaseType)
                    return p.Type as BaseType;
                else
                {
                    p.check_basetype("System." + s);
                    if (p.Type is BaseType)
                        return p.Type as BaseType;
                }
                return null;                
            }

            private void check_basetype(string p)
            {
                if (p == "System.Boolean")
                    Type = new BaseType(BaseType_Type.Boolean);
                else if (p == "System.Char")
                    Type = new BaseType(BaseType_Type.Char);
                else if (p == "System.SByte")
                    Type = new BaseType(BaseType_Type.I1);
                else if (p == "System.Byte")
                    Type = new BaseType(BaseType_Type.U1);
                else if (p == "System.Double")
                    Type = new BaseType(BaseType_Type.R8);
                else if (p == "System.Single")
                    Type = new BaseType(BaseType_Type.R4);
                else if (p == "System.Int32")
                    Type = new BaseType(BaseType_Type.I4);
                else if (p == "System.UInt32")
                    Type = new BaseType(BaseType_Type.U4);
                else if (p == "System.Int16")
                    Type = new BaseType(BaseType_Type.I2);
                else if (p == "System.UInt16")
                    Type = new BaseType(BaseType_Type.U2);
                else if (p == "System.Int64")
                    Type = new BaseType(BaseType_Type.I8);
                else if (p == "System.UInt64")
                    Type = new BaseType(BaseType_Type.U8);
                else if (p == "System.IntPtr")
                    Type = new BaseType(BaseType_Type.I);
                else if (p == "System.UIntPtr")
                    Type = new BaseType(BaseType_Type.U);
                else if (p == "System.Object")
                    Type = new BaseType(BaseType_Type.Object);
                else if (p == "System.String")
                    Type = new BaseType(BaseType_Type.String);
                else if (p == "System.UninstantiatedGenericParam")
                    Type = new BaseType(BaseType_Type.UninstantiatedGenericParam);
                else if (p == "System.VirtFtnPtr")
                    Type = new BaseType(BaseType_Type.VirtFtnPtr);
                else if (p == "System.Void")
                    Type = new BaseType(BaseType_Type.Void);
                else if (p == "ref")
                    Type = new BaseType(BaseType_Type.RefGenericParam);
                else if (p == "param")
                    Type = new BaseType(BaseType_Type.UninstantiatedGenericParam);
            }

            public libtysila.Assembler.CliType CliType(Assembler ass)
            { return CliType(this.Type, ass); }
            public static libtysila.Assembler.CliType CliType(Signature.BaseOrComplexType Type, Assembler ass)
            {
                if (Type is BaseType)
                {
                    BaseType bt = Type as BaseType;
                    switch (bt.Type)
                    {
                        case BaseType_Type.Boolean:
                        case BaseType_Type.Byte:
                        case BaseType_Type.Char:
                        case BaseType_Type.I1:
                        case BaseType_Type.I2:
                        case BaseType_Type.I4:
                        case BaseType_Type.U1:
                        case BaseType_Type.U2:
                        case BaseType_Type.U4:
                            return Assembler.CliType.int32;

                        case BaseType_Type.Void:
                        case BaseType_Type.UninstantiatedGenericParam:
                            return Assembler.CliType.void_;

                        case BaseType_Type.I8:
                        case BaseType_Type.U8:
                            return Assembler.CliType.int64;

                        case BaseType_Type.I:
                        case BaseType_Type.U:
                            return Assembler.CliType.native_int;

                        case BaseType_Type.R4:
                            return Assembler.CliType.F32;

                        case BaseType_Type.R8:
                            return Assembler.CliType.F64;

                        case BaseType_Type.TypedByRef:
                            return Assembler.CliType.vt;

                        case BaseType_Type.VirtFtnPtr:
                            return Assembler.CliType.virtftnptr;
                    }
                }
                else if (Type is GenericType)
                    return new Signature.Param(((GenericType)Type).GenType, ass).CliType(ass);
                else if (Type is ComplexType)
                {
                    if (((ComplexType)Type).isValueType)
                    {
                        // if it inherits from system.enum then we need to get the underlying type of the
                        //  enum
                        Metadata.TypeDefRow tdr = Metadata.GetTypeDef(((ComplexType)Type).Type, ass);

                        if (Metadata.GetTypeFullname(tdr.Extends.ToToken()) == "System.Enum")
                        {
                            Metadata.TableIndex last_field = Metadata.GetLastField(tdr.m, tdr);
                            for (Metadata.TableIndex i = tdr.FieldList; i < last_field; i++)
                            {
                                if (((Metadata.FieldRow)i.Value).Name == "value__")
                                {
                                    Signature.Field fsig = Signature.ParseFieldSig(tdr.m, ((Metadata.FieldRow)i.Value).Signature, ass);
                                    return fsig.AsParam(ass).CliType(ass);
                                }
                            }
                        }

                        return Assembler.CliType.vt;
                    }
                }
                else if (Type is ManagedPointer)
                    return Assembler.CliType.reference;
                else if (Type is UnmanagedPointer)
                    return Assembler.CliType.native_int;

                return Assembler.CliType.O;
            }

            public override string ToString()
            {
                return Type.ToString();
            }
        }

        public abstract class BaseOrComplexType : IEquatable<BaseOrComplexType>
        {
            public Assembler _ass;

            public abstract IList<BaseOrComplexType> GetBaseTypes();

            public libtysila.Assembler.CliType CliType(Assembler ass)
            { return Param.CliType(this, ass); }

            public virtual bool IsConcreteType { get { return false; } }

            public bool IsInstantiable
            {
                get
                {
                    if (this is Signature.BaseType)
                    {
                        if (((Signature.BaseType)this).Type == BaseType_Type.UninstantiatedGenericParam)
                            return false;
                    }
                    foreach (BaseOrComplexType bct in this.GetBaseTypes())
                    {
                        if (bct.IsInstantiable == false)
                            return false;
                    }
                    return true;
                }
            }

            public bool IsValueType(Assembler ass)
            {
                libtysila.Assembler.CliType ct = CliType(ass);
                switch (ct)
                {
                    case Assembler.CliType.F32:
                    case Assembler.CliType.F64:
                    case Assembler.CliType.int32:
                    case Assembler.CliType.int64:
                    case Assembler.CliType.native_int:
                    case Assembler.CliType.virtftnptr:
                    case Assembler.CliType.vt:
                    case Assembler.CliType.void_:
                        return true;

                    case Assembler.CliType.O:
                    case Assembler.CliType.reference:
                        return false;

                    default:
                        throw new Exception("Unsupported CliType");
                }
            }

            public bool IsRefGenericParam
            {
                get
                {
                    if ((this is Signature.BaseType) && (((Signature.BaseType)this).Type == BaseType_Type.RefGenericParam))
                        return true;
                    return false;
                }
            }

            public bool IsObject
            {
                get
                {
                    if ((this is Signature.BaseType) && (((Signature.BaseType)this).Type == BaseType_Type.Object))
                        return true;
                    return false;
                }
            }

            public IEnumerable<byte> Signature;
            public string SignatureString
            {
                get
                {
                    if (Signature == null)
                        return "";
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in Signature)
                        sb.Append(b.ToString("X2"));
                    return sb.ToString();
                }
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public virtual bool Equals(BaseOrComplexType other)
            {
                throw new NotImplementedException();
            }
        }

        public class BaseType : BaseOrComplexType
        {
            public BaseType_Type Type;
            public int Ugp_idx;

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { };
            }

            public BaseType() { }
            public BaseType(BaseType_Type type) { Type = type; Signature = make_sig_string(); }
            public BaseType(BaseType_Type type, int ugp_idx) { Type = type; Ugp_idx = ugp_idx; Signature = make_sig_string(); }

            public override bool IsConcreteType
            {
                get
                {
                    return true;
                }
            }

            public string GetTypeName()
            {
                switch (Type)
                {
                    case BaseType_Type.Array:
                        return "Array";
                    case BaseType_Type.Boolean:
                        return "Boolean";
                    case BaseType_Type.Byte:
                        return "Byte";
                    case BaseType_Type.Char:
                        return "Char";
                    case BaseType_Type.I:
                        return "IntPtr";
                    case BaseType_Type.I1:
                        return "SByte";
                    case BaseType_Type.I2:
                        return "Int16";
                    case BaseType_Type.I4:
                        return "Int32";
                    case BaseType_Type.I8:
                        return "Int64";
                    case BaseType_Type.Object:
                        return "Object";
                    case BaseType_Type.R4:
                        return "Single";
                    case BaseType_Type.R8:
                        return "Double";
                    case BaseType_Type.String:
                        return "String";
                    case BaseType_Type.U:
                        return "UIntPtr";
                    case BaseType_Type.U1:
                        return "Byte";
                    case BaseType_Type.U2:
                        return "UInt16";
                    case BaseType_Type.U4:
                        return "UInt32";
                    case BaseType_Type.U8:
                        return "UInt64";
                    case BaseType_Type.Void:
                        return "void";
                    case BaseType_Type.TypedByRef:
                        return "TypedReference";
                    case BaseType_Type.UninstantiatedGenericParam:
                        return "T";
                    case BaseType_Type.VirtFtnPtr:
                        return "VirtFtnPtr";
                    case BaseType_Type.RefGenericParam:
                        return "ref";
                    default:
                        throw new NotSupportedException();
                }
            }

            private IEnumerable<byte> make_sig_string()
            {
                return new byte[] { (byte)Type };
            }
            public BaseType(Assembler.CliType type)
            {
                switch (type)
                {
                    case Assembler.CliType.int32:
                        Type = BaseType_Type.I4;
                        break;
                    case Assembler.CliType.int64:
                        Type = BaseType_Type.I8;
                        break;
                    case Assembler.CliType.native_int:
                        Type = BaseType_Type.I;
                        break;
                    case Assembler.CliType.F64:
                        Type = BaseType_Type.R8;
                        break;
                    case Assembler.CliType.F32:
                        Type = BaseType_Type.R4;
                        break;
                    case Assembler.CliType.reference:
                        Type = BaseType_Type.U;
                        break;
                    case Assembler.CliType.O:
                        Type = BaseType_Type.Object;
                        break;
                    case Assembler.CliType.virtftnptr:
                        Type = BaseType_Type.VirtFtnPtr;
                        break;
                    case Assembler.CliType.void_:
                        Type = BaseType_Type.Void;
                        break;
                    default:
                        throw new NotSupportedException();
                }
                Signature = make_sig_string();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is BaseType))
                    return false;
                BaseType b = obj as BaseType;
                if (Type != b.Type)
                    return false;
                return true;
            }
            public override int GetHashCode()
            {
                return (int)Type ^ 0x1a2b3c4d;
            }

            public override string ToString()
            {
                return GetTypeName();
            }
        }

        public class BoxedType : BaseOrComplexType
        {
            public BaseOrComplexType Type;
            public BoxedType() { }
            public BoxedType(Token t, Assembler ass)
            {
                Type = new Signature.Param(t, ass).Type;
            }
            public BoxedType(BaseOrComplexType bct)
            {
                Type = bct;
            }

            public override int GetHashCode()
            {
                return Type.GetHashCode() ^ 0x2b3c4d1a;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { Type };
            }
        }

        public class ComplexType : BaseOrComplexType
        {
            public Token Type;
            public bool isValueType = false;
            bool? isEnum;
            public Assembler a;

            public bool IsEnum
            {
                get
                {
                    if (isEnum.HasValue)
                        return isEnum.Value;
                    else
                    {
                        Metadata.TypeDefRow tdr = Metadata.GetTypeDef(this, a);
                        isEnum = tdr.IsEnum(a);
                        return isEnum.Value;
                    }
                }
            }

            public override bool IsConcreteType
            {
                get
                {
                    return true;
                }
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { };
            }

            public ComplexType(Assembler ass) { a = ass; }
            public ComplexType(Token t, Assembler ass)
            {
                a = ass;
                if (t.Value is Metadata.TypeRefRow)
                    t = new Token(Metadata.GetTypeDef(t, ass));

                Type = t;

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(t, ass);
                if (tdr.IsValueType(ass))
                    isValueType = true;
                if (tdr.IsEnum(ass))
                    isEnum = true;

                if (t.Value is Metadata.TypeSpecRow)
                    Signature = ((Metadata.TypeSpecRow)t.Value).Signature;
                else
                {
                    Signature = new List<byte> { isValueType ? (byte)0x11 : (byte)0x12 };
                    ((List<byte>)Signature).AddRange(new Token(tdr).CompressTypeDefOrRef());
                }
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException("Use Signature.CTCompare");
            }
            public override int GetHashCode()
            {
                return Type.Value.GetHashCode() ^ 0x3c4d1a2b;
            }
        }

        public abstract class BaseArray : BaseOrComplexType
        {
            public Metadata.TypeDefRow ArrayType;
            public BaseOrComplexType ElemType;

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { ElemType };
            }
        }

        public class ZeroBasedArray : BaseArray
        {
            public int numElems = -1;

            public override int GetHashCode()
            {
                return ElemType.GetHashCode() ^ 0x4d1a2b3c;
            }

            public override bool Equals(BaseOrComplexType other)
            {
                if (other is ZeroBasedArray)
                {
                    ZeroBasedArray zba = other as ZeroBasedArray;
                    return BCTCompare(ElemType, zba.ElemType, _ass);
                }
                else
                    return false;
            }
        }

        public class ComplexArray : BaseArray
        {
            public int Rank;
            public int[] Sizes;
            public int[] LoBounds;

            public override bool Equals(BaseOrComplexType other)
            {
                if (other is ComplexArray)
                    return Equals((ComplexArray)other);
                return false;
            }

            public bool Equals(ComplexArray other)
            {
                if (Rank != other.Rank)
                    return false;
                if(Sizes.Length != other.Sizes.Length)
                    return false;
                for (int i = 0; i < Sizes.Length; i++)
                {
                    if (Sizes[i] != other.Sizes[i])
                        return false;
                }
                if (LoBounds.Length != other.LoBounds.Length)
                    return false;
                for (int i = 0; i < LoBounds.Length; i++)
                {
                    if (LoBounds[i] != other.LoBounds[i])
                        return false;
                }
                if (!BCTCompare(ElemType, other.ElemType, _ass))
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                return ElemType.GetHashCode() ^ Rank ^ Sizes.Length ^ LoBounds.Length;
            }
        }

        public class ManagedPointer : BaseOrComplexType
        {
            public BaseOrComplexType ElemType;

            public override int GetHashCode()
            {
                return ElemType.GetHashCode() ^ 0x2a3b4c5d;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { ElemType };
            }
        }

        public class UnmanagedPointer : BaseOrComplexType
        {
            public BaseOrComplexType BaseType;

            public override int GetHashCode()
            {
                return BaseType.GetHashCode() ^ 0x3b4c5d2a;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { BaseType };
            }
        }

        public class TypedReference : BaseOrComplexType
        {
            public override int GetHashCode()
            {
                return 0x4c5d2a3b;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { };
            }
        }

        public class GenericParam : BaseOrComplexType
        {
            public int ParamNumber;

            public override int GetHashCode()
            {
                return 0x5d2a3b4c ^ ParamNumber;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { };
            }
        }

        public class GenericMethodParam : BaseOrComplexType
        {
            public int ParamNumber;

            public override int GetHashCode()
            {
                return 0x3a4b5c6d ^ ParamNumber;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                return new BaseOrComplexType[] { };
            }
        }

        public class GenericType : BaseOrComplexType
        {
            public BaseOrComplexType GenType;
            public List<BaseOrComplexType> GenParams = new List<BaseOrComplexType>();

            public override bool Equals(object obj)
            {
                throw new NotSupportedException("Use Signature.GTCompare");
            }
            public override int GetHashCode()
            {
                int hc = GenType.GetHashCode();
                for (int i = 0; i < GenParams.Count; i++)
                {
                    BaseOrComplexType gp = GenParams[i];
                    hc ^= (gp.GetHashCode() << (i % 32));
                }
                return hc;
            }

            public override IList<BaseOrComplexType> GetBaseTypes()
            {
                List<BaseOrComplexType> ret = new List<BaseOrComplexType>(GenParams);
                ret.Add(GenType);
                return ret;
            }
        }

        public class BaseMethod {
            public IEnumerable<byte> Signature;
            public Metadata m;

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public Method Method
            {
                get
                {
                    if (this is Method)
                        return this as Method;
                    else
                        return ((GenericMethod)this).GenMethod;
                }
            }
        }

        public class GenericMethod : BaseMethod
        {
            public Method GenMethod = null;
            public List<BaseOrComplexType> GenParams = new List<BaseOrComplexType>();

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }
            public override int GetHashCode()
            {
                int hc = 0x7a8b9cad;
                hc ^= GenMethod.GetHashCode();
                for (int i = 0; i < GenParams.Count; i++)
                {
                    BaseOrComplexType gp = GenParams[i];
                    hc ^= (gp.GetHashCode() << (i % 32));
                }
                return hc;
            }
        }

        public class Field : HasCustomMods
        {
            Assembler _ass;
            public Field(Assembler ass) { _ass = ass; }
            public BaseOrComplexType Type;
            public Param AsParam(Assembler ass)  { return new Signature.Param(ass) { Type = Type }; }
            public Metadata m;
            public Field() { }
            public Field(Signature.Param p)
            {
                CustomMods.AddRange(p.CustomMods);
                Type = p.Type;
            }

            public static implicit operator Param(Field f) { return f.AsParam(f._ass); }
        }

        public class Property
        {
            public bool HasThis = false;
            public int ParamCount;
            public Param Type;
            public List<Param> Params = new List<Param>();
        }

        public class Method : BaseMethod
        {
            public bool HasThis = false;
            public bool ExplicitThis = false;

            public bool Returns = true;

            public enum CallConv { Default, VarArg, Generic };
            public CallConv CallingConvention;

            public int GenParamCount;

            public int ParamCount;
            public Param RetType;
            public List<Param> Params = new List<Param>();

            public override bool Equals(object obj)
            {
                throw new NotSupportedException("Use Signature.MethodSigCompare");
            }
            public override int GetHashCode()
            {
                int hc = 0x5a6b7c8d;
                if (HasThis)
                    hc ^= 0x6b7c8d5a;
                if(ExplicitThis)
                    hc ^= 0x7c8d5a6b;
                hc ^= (int)CallingConvention;
                unchecked
                {
                    hc ^= (Params.Count << 24);
                }
                hc ^= RetType.GetHashCode();
                for (int i = 0; i < Params.Count; i++)
                {
                    Param p = Params[i];
                    hc ^= (p.GetHashCode() << (i % 32));
                }
                return hc;
            }
        }

        static public GenericMethod ParseMethodSpec(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParseMethodSpec(m, s, ref x, ass);
        }

        static public GenericMethod ParseMethodSpec(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            GenericMethod ret = new GenericMethod();

            ret.m = m;
            ret.Signature = s;

            if (s[offset] != 0x0a)
                throw new Exception("Invalid MethodSpec signature");

            offset++;

            int paramCount = Metadata.ReadCompressedInteger(s, ref offset);
            for (int i = 0; i < paramCount; i++)
                ret.GenParams.Add(ParseTypeSig(m, s, ref offset, ass));

            return ret;
        }

        static public Property ParsePropertySig(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParsePropertySig(m, s, ref x, ass);
        }

        static public Property ParsePropertySig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            Property ret = new Property();

            if (s[offset] == 0x8)
                ret.HasThis = false;
            else if (s[offset] == 0x28)
                ret.HasThis = true;
            else
                throw new Exception("Invalid Property Signature");
            offset++;

            ret.ParamCount = Metadata.ReadCompressedInteger(s, ref offset);
            ret.Type = ParseParamSig(m, s, ref offset, ass);
            for (int i = 0; i < ret.ParamCount; i++)
                ret.Params.Add(ParseParamSig(m, s, ref offset, ass));

            return ret;
        }

        static public LocalVars ParseLocalVarsSig(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParseLocalVarsSig(m, s, ref x, ass);
        }

        static public LocalVars ParseLocalVarsSig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            LocalVars ret = new LocalVars();
            if (s == null)
                return ret;
            if (s[offset] != 0x7)
                throw new Exception("Invalid LocalVars signature");
            offset++;

            int count = Metadata.ReadCompressedInteger(s, ref offset);

            for (int i = 0; i < count; i++)
                ret.Vars.Add(ParseParamSig(m, s, ref offset, ass));

            return ret;
        }

        static public BaseMethod ParseMethodSig(Metadata.TableRow itrow)
        {
            if (itrow is Metadata.MethodDefRow)
            {
                if (((Metadata.MethodDefRow)itrow).msig != null)
                    return ((Metadata.MethodDefRow)itrow).msig;
                return ParseMethodDefSig(((Metadata.MethodDefRow)itrow).m, ((Metadata.MethodDefRow)itrow).Signature, itrow.ass);
            }
            else if (itrow is Metadata.MemberRefRow)
                return ParseMethodDefSig(((Metadata.MemberRefRow)itrow).m, ((Metadata.MemberRefRow)itrow).Signature, itrow.ass);
            else if (itrow is Metadata.MethodSpecRow)
                return ParseMethodSpec(((Metadata.MethodSpecRow)itrow).m, ((Metadata.MethodSpecRow)itrow).Instantiation, itrow.ass);
            else
                throw new NotSupportedException();
        }

        static public BaseMethod ParseMethodDefSig(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParseMethodDefSig(m, s, ref x, ass);
        }

        static public BaseMethod ParseMethodDefSig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            Method ret = new Method();

            ret.Signature = s;
            ret.m = m;

            if ((s[offset] & 0x20) == 0x20)
                ret.HasThis = true;
            if ((s[offset] & 0x40) == 0x40)
                ret.ExplicitThis = true;
            ret.CallingConvention = Method.CallConv.Default;
            if ((s[offset] & 0x5) == 0x5)
                ret.CallingConvention = Method.CallConv.VarArg;
            if ((s[offset] & 0x10) == 0x10)
            {
                ret.CallingConvention = Method.CallConv.Generic;
            }
            offset++;
            if (ret.CallingConvention == Method.CallConv.Generic)
                ret.GenParamCount = Metadata.ReadCompressedInteger(s, ref offset);

            ret.ParamCount = Metadata.ReadCompressedInteger(s, ref offset);

            ret.RetType = ParseParamSig(m, s, ref offset, ass);

            for (int i = 0; i < ret.ParamCount; i++)
                ret.Params.Add(ParseParamSig(m, s, ref offset, ass));

            /*if (ret.CallingConvention == Method.CallConv.Generic)
            {
                GenericMethod gm = new GenericMethod();
                gm.GenMethod = ret;
                gm.Signature = s;
                gm.m = m;
                for (int i = 0; i < ret.GenParamCount; i++)
                    gm.GenParams.Add(new BaseType(BaseType_Type.UninstantiatedGenericParam));
                return gm;
            }*/

            return ret;
        }

        static public BaseMethod ParseMangledMethodSig(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParseMangledMethodSig(m, s, ref x, ass);
        }
        static public BaseMethod ParseMangledMethodSig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            BaseMethod msig = ParseMethodDefSig(m, s, ref offset, ass);

            if (msig.Method.CallingConvention != Method.CallConv.Generic)
                return msig;

            if (s[offset] != 0xa)
                throw new Exception("Invalid generic method");
            offset++;

            GenericMethod ret = msig as GenericMethod;

            int gen_params = Metadata.ReadCompressedInteger(s, ref offset);
            for (int i = 0; i < gen_params; i++)
                ret.GenParams.Add(ParseTypeSig(m, s, ref offset, ass));

            return ret;
        }

        static public void ParseCustomMods(Metadata m, byte[] s, ref int offset, HasCustomMods cm)
        {
            while ((s[offset] == 0x1f) || (s[offset] == 0x20))
            {
                CustomMod c = new CustomMod();
                c.TypeOfCustomMod = (s[offset] == 0x1f) ? CustomMod.CustomModType.Required :
                    CustomMod.CustomModType.Optional;
                offset++;
                c.Type = new Metadata.TableIndex(m, Metadata.ReadCompressedInteger(s, ref offset), 2,
                    new Metadata.TableId[] { Metadata.TableId.TypeDef, Metadata.TableId.TypeRef,
                        Metadata.TableId.TypeSpec });
                cm.CustomMods.Add(c);
            }
        }

        static public Field ParseFieldSig(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParseFieldSig(m, s, ref x, ass);
        }

        static public Field ParseFieldSig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            Field ret = new Field();

            ret.m = m;

            if (s[offset] != 0x06)
                throw new Exception("Invalid Field Signature");
            offset++;

            ParseCustomMods(m, s, ref offset, ret);
            ret.Type = ParseTypeSig(m, s, ref offset, ass);

            return ret;
        }

        static public Param ParseParamSig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            Param ret = new Param(null);

            ParseCustomMods(m, s, ref offset, ret);
            if (s[offset] == 0x45)
            {
                ret.Pinned = true;
                offset++;
            }

            if (s[offset] == 0x16)
            {
                ret.Type = new BaseType(BaseType_Type.TypedByRef);
                offset++;
            }
            else if (s[offset] == 0x41)
            {
                // Just ignore the SENTINEL byte user in vararg call site signatures
                offset++;
            }
            else
            {
                ret.Type = ParseTypeSig(m, s, ref offset, ass);
            }

            return ret;
        }

        static public BaseOrComplexType ParseTypeSig(Metadata m, byte[] s, Assembler ass)
        {
            int x = 0;
            return ParseTypeSig(m, s, ref x, ass);
        }

        static public BaseOrComplexType ParseTypeSig(Metadata m, byte[] s, ref int offset, Assembler ass)
        {
            BaseOrComplexType r;
            int start_offset = offset;
            if ((s[offset] <= 0x0e) || (s[offset] == 0x16) || (s[offset] == 0x18) || (s[offset] == 0x19) || (s[offset] == 0x1c) || (s[offset] == 0xfe) || (s[offset] == 0xfd))
            {
                BaseType ret = new BaseType();
                ret.Type = (BaseType_Type)s[offset];
                offset++;
                r = ret;
            }
            else if ((s[offset] == 0x11) || (s[offset] == 0x12))
            {
                ComplexType ret = new ComplexType(ass);
                if (s[offset] == 0x11)
                    ret.isValueType = true;
                offset++;
                ret.Type = new Metadata.TableIndex(m, Metadata.ReadCompressedInteger(s, ref offset), 2,
                    new Metadata.TableId[] { Metadata.TableId.TypeDef, Metadata.TableId.TypeRef,
                        Metadata.TableId.TypeSpec }).ToToken(m);
                if (ret.Type.Value is Metadata.TypeRefRow)
                    ret.Type = new Token(Metadata.GetTypeDef(ret.Type.Value, ass));
                r = ret;
            }
            else if (s[offset] == 0x1d)
            {
                /* SZARRAY followed by Type */
                ZeroBasedArray ret = new ZeroBasedArray();
                offset++;
                ret.ElemType = ParseTypeSig(m, s, ref offset, ass);
                r = ret;
            }
            else if (s[offset] == 0x13)
            {
                GenericParam ret = new GenericParam();
                offset++;
                ret.ParamNumber = Metadata.ReadCompressedInteger(s, ref offset);
                r = ret;
            }
            else if (s[offset] == 0x15)
            {
                GenericType ret = new GenericType();
                offset++;
                ret.GenType = ParseTypeSig(m, s, ref offset, ass);
                int param_count = Metadata.ReadCompressedInteger(s, ref offset);
                for (int i = 0; i < param_count; i++)
                    ret.GenParams.Add(ParseTypeSig(m, s, ref offset, ass));
                r = ret;
            }
            else if (s[offset] == 0x0f)
            {
                UnmanagedPointer ret = new UnmanagedPointer();
                offset++;
                ret.BaseType = ParseTypeSig(m, s, ref offset, ass);
                r = ret;
            }
            else if (s[offset] == 0x10)
            {
                ManagedPointer ret = new ManagedPointer();
                offset++;
                ret.ElemType = ParseTypeSig(m, s, ref offset, ass);
                r = ret;
            }
            else if (s[offset] == 0x1e)
            {
                GenericMethodParam ret = new GenericMethodParam();
                offset++;
                ret.ParamNumber = Metadata.ReadCompressedInteger(s, ref offset);
                r = ret;
            }
            else if (s[offset] == 0x14)
            {
                ComplexArray ret = new ComplexArray();
                offset++;
                ret.ElemType = ParseTypeSig(m, s, ref offset, ass);
                ret.Rank = Metadata.ReadCompressedInteger(s, ref offset);

                int numSizes = Metadata.ReadCompressedInteger(s, ref offset);
                ret.Sizes = new int[numSizes];
                for (int i = 0; i < numSizes; i++)
                    ret.Sizes[i] = Metadata.ReadCompressedInteger(s, ref offset);

                int numLoBounds = Metadata.ReadCompressedInteger(s, ref offset);
                ret.LoBounds = new int[numLoBounds];
                for (int i = 0; i < numLoBounds; i++)
                    ret.LoBounds[i] = Metadata.ReadCompressedInteger(s, ref offset);

                r = ret;
            }
            else if (s[offset] == 0x51)
            {
                BoxedType ret = new BoxedType();
                offset++;
                ret.Type = ParseTypeSig(m, s, ref offset, ass);
                r = ret;
            }
            else
            {
                throw new Exception("Element type not recognised");
            }
            List<byte> rsig = new List<byte>();
            for (int i = start_offset; i < offset; i++)
                rsig.Add(s[i]);
            r.Signature = rsig;
            return r;
        }

        public static Signature.BaseMethod ResolveGenericMember(Signature.BaseMethod orig_sig, Signature.BaseOrComplexType parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            if ((!((parent is GenericType) ||
                ((parent is BoxedType) && (((BoxedType)parent).Type is GenericType)) ||
                ((parent is ManagedPointer) && (((ManagedPointer)parent).ElemType is GenericType))))
                && (!(containing_meth is GenericMethod)))
                return orig_sig;

            if (orig_sig is Signature.Method)
            {
                Signature.Method bmm = orig_sig as Signature.Method;
                Signature.Method ret = new Method();
                if(orig_sig.Signature != null)
                    ret.Signature = new List<byte>(orig_sig.Signature);
                ret.CallingConvention = bmm.CallingConvention;
                ret.ExplicitThis = bmm.ExplicitThis;
                ret.GenParamCount = bmm.GenParamCount;
                ret.HasThis = bmm.HasThis;
                ret.m = bmm.m;
                ret.ParamCount = bmm.ParamCount;
                ret.Params = new List<Param>();
                foreach (Param p in bmm.Params)
                    ret.Params.Add(ResolveGenericParam(p, parent, containing_meth, ass));
                ret.RetType = ResolveGenericParam(bmm.RetType, parent, containing_meth, ass);
                
                return ret;
            }
            else if (orig_sig is Signature.GenericMethod)
            {
                Signature.GenericMethod gmm = orig_sig as Signature.GenericMethod;
                Signature.GenericMethod ret = new GenericMethod();
                ret.GenMethod = gmm.GenMethod;
                foreach (BaseOrComplexType p in gmm.GenParams)
                    ret.GenParams.Add(ResolveGenericParam(new Param(p, ass), parent, containing_meth, ass).Type);

                return ret;
            }
            else
                throw new NotImplementedException();
        }

        /*public static Signature.Method ResolveGenericMethodParams(Signature.Method orig_sig, Signature.GenericMethod parent, Assembler ass)
        {
            Signature.Method ret = new Method();
            ret.CallingConvention = orig_sig.CallingConvention;
            ret.ExplicitThis = orig_sig.ExplicitThis;
            ret.GenParamCount = orig_sig.GenParamCount;
            ret.HasThis = orig_sig.HasThis;
            ret.Params = new List<Param>();
            foreach (Param p in orig_sig.Params)
                ret.Params.Add(ResolveGenericMethodParam(p, parent, ass));
            ret.RetType = ResolveGenericMethodParam(orig_sig.RetType, parent, ass);
            return ret;
        } 

        public static Signature.Param ResolveGenericMethodParam(Signature.Param p, Signature.GenericMethod parent, Assembler ass)
        {
            if (!(p.Type is Signature.GenericMethodParam))
                return p;
            return new Param(parent.GenParams[((Signature.GenericMethodParam)p.Type).ParamNumber], ass);
        } */

        /*public static Signature.BaseOrComplexType ResolveGenericType(Signature.BaseOrComplexType bct, Signature.BaseOrComplexType parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            if ((!(parent is GenericType)) && (!(containing_meth is Signature.GenericMethod)))
                return bct;

            if (bct is Signature.GenericType)
            {
                Signature.GenericType gt = bct as Signature.GenericType;
                Signature.GenericType ret = new GenericType();
                ret.GenParams = new List<BaseOrComplexType>();
                ret.GenType = gt.GenType;
                ret.Signature = gt.Signature;
                foreach (Signature.BaseOrComplexType p in gt.GenParams)
                    ret.GenParams.Add(ResolveGenericParam(new Param(p, ass), parent, containing_meth, ass).Type);
                return ret;
            }
            else if (bct is Signature.GenericMethodParam)
            {
                throw new NotImplementedException();
            }
            else
                return bct;
        } */

        public static Signature.Param ResolveGenericParam(Signature.Param p, Signature.BaseOrComplexType containing_sig, Signature.BaseMethod containing_meth,
            Assembler ass)
        {
            if (p.Type is BaseType)
                return p;
            else if (p.Type is ComplexType)
                return p;
            else if (p.Type is GenericParam)
            {
                GenericParam gp = p.Type as GenericParam;
                GenericType gt;
                if (containing_sig is GenericType)
                    gt = containing_sig as GenericType;
                else if (containing_sig is BoxedType)
                {
                    BoxedType bt = containing_sig as BoxedType;
                    if (bt.Type is GenericType)
                        gt = bt.Type as GenericType;
                    else
                        return new Param(BaseType_Type.UninstantiatedGenericParam, gp.ParamNumber, p.name, ass);
                }
                else if (containing_sig is ManagedPointer)
                {
                    ManagedPointer mp = containing_sig as ManagedPointer;
                    if (mp.ElemType is GenericType)
                        gt = mp.ElemType as GenericType;
                    else
                        return new Param(BaseType_Type.UninstantiatedGenericParam, gp.ParamNumber, p.name, ass);
                }
                else if (containing_sig is UnmanagedPointer)
                {
                    UnmanagedPointer ump = containing_sig as UnmanagedPointer;
                    if (ump.BaseType is GenericType)
                        gt = ump.BaseType as GenericType;
                    else
                        return new Param(BaseType_Type.UninstantiatedGenericParam, gp.ParamNumber, p.name, ass);
                }
                else
                    return new Param(BaseType_Type.UninstantiatedGenericParam, gp.ParamNumber, p.name, ass);

                return new Param (ass)
                {
                    CustomMods = p.CustomMods,
                    Type = ResolveGenericParam(new Signature.Param(gt.GenParams[((GenericParam)p.Type).ParamNumber], ass), containing_sig, containing_meth, ass).Type,
                    name = p.name
                };
            }
            else if (p.Type is GenericMethodParam)
            {
                if (!(containing_meth is GenericMethod))
                    return new Param(BaseType_Type.UninstantiatedGenericParam, p.name);

                return new Param (ass)
                {
                    CustomMods = p.CustomMods,
                    Type = ResolveGenericParam(new Param(((GenericMethod)containing_meth).GenParams[((GenericMethodParam)p.Type).ParamNumber], ass), containing_sig, containing_meth, ass).Type,
                    name = p.name
                };
            }
            else if (p.Type is GenericType)
            {
                GenericType gt = new GenericType
                {
                    GenType = ((GenericType)p.Type).GenType,
                    Signature = p.Type.Signature
                };

                foreach (Signature.BaseOrComplexType gp in ((GenericType)p.Type).GenParams)
                    gt.GenParams.Add(ResolveGenericParam(new Signature.Param(gp, ass), containing_sig, containing_meth, ass).Type);
                return new Param (ass)
                {
                    CustomMods = p.CustomMods,
                    Type = gt,
                    Pinned = p.Pinned,
                    name = p.name
                };
            }
            else if (p.Type is ZeroBasedArray)
            {
                return new Param (ass)
                {
                    CustomMods = p.CustomMods,
                    Type = new ZeroBasedArray
                    {
                        Signature = ((ZeroBasedArray)p.Type).Signature,
                        numElems = ((ZeroBasedArray)p.Type).numElems,
                        ElemType = ResolveGenericParam(new Param(((ZeroBasedArray)p.Type).ElemType, ass), containing_sig, containing_meth, ass).Type
                    },
                    Pinned = p.Pinned,
                    name = p.name
                };
            }
            else if (p.Type is ManagedPointer)
            {
                return new Param (ass)
                {
                    CustomMods = p.CustomMods,
                    Pinned = p.Pinned,
                    Type = new ManagedPointer
                    {
                        Signature = ((ManagedPointer)p.Type).Signature,
                        ElemType = ResolveGenericParam(new Param(((ManagedPointer)p.Type).ElemType, ass), containing_sig, containing_meth, ass).Type
                    },
                    name = p.name
                };
            }
            else if (p.Type is UnmanagedPointer)
            {
                return new Param(ass)
                {
                    CustomMods = p.CustomMods,
                    Pinned = p.Pinned,
                    Type = new UnmanagedPointer
                    {
                        Signature = ((UnmanagedPointer)p.Type).Signature,
                        BaseType = ResolveGenericParam(new Param(((UnmanagedPointer)p.Type).BaseType, ass), containing_sig, containing_meth, ass).Type
                    },
                    name = p.name
                };
            }
            else if (p.Type is BoxedType)
            {
                return new Param(ass)
                {
                    CustomMods = p.CustomMods,
                    Pinned = p.Pinned,
                    Type = new BoxedType
                    {
                        Type = ResolveGenericParam(new Param(((BoxedType)p.Type).Type, ass), containing_sig, containing_meth, ass).Type
                    },
                    name = p.name
                };
            }
            else if (p.Type is ComplexArray)
            {
                return new Param(ass)
                {
                    CustomMods = p.CustomMods,
                    Pinned = p.Pinned,
                    Type = new ComplexArray
                    {
                        ElemType = ResolveGenericParam(new Param(((ComplexArray)p.Type).ElemType, ass), containing_sig, containing_meth, ass).Type,
                        LoBounds = ((ComplexArray)p.Type).LoBounds,
                        Rank = ((ComplexArray)p.Type).Rank,
                        Sizes = ((ComplexArray)p.Type).Sizes
                    },
                    name = p.name
                };
            }
            else
                throw new NotSupportedException();
        }

        internal static bool MethodCompare(Assembler.MethodToCompile a, Assembler.MethodToCompile b, Assembler ass)
        { return MethodCompare(a, b, ass, false); }
        internal static bool MethodCompare(Assembler.MethodToCompile a, Assembler.MethodToCompile b, Assembler ass, bool compare_type)
        {
            if (compare_type)
            {
                if (!TypeCompare(a.GetTTC(ass), b.GetTTC(ass), ass))
                    return false;
            }

            if (a.meth.Name != b.meth.Name)
                return false;

            return BaseMethodSigCompare(a.msig, b.msig, ass);
        }

        internal static bool BaseMethodSigCompare(Signature.BaseMethod a, Signature.BaseMethod b, Assembler ass)
        {
            if (a.GetType() != b.GetType())
                return false;

            if (a is Signature.Method)
                return MethodSigCompare(a as Signature.Method, b as Signature.Method, ass);
            else if (a is Signature.GenericMethod)
                return GenMethodSigCompare(a as Signature.GenericMethod, b as Signature.GenericMethod, ass);
            else
                throw new NotImplementedException();
        }

        internal static bool GenMethodSigCompare(GenericMethod a, GenericMethod b, Assembler ass)
        {
            if (MethodSigCompare(a.GenMethod, b.GenMethod, ass) == false)
                return false;
            if (a.GenParams.Count != b.GenParams.Count)
                return false;
            for (int i = 0; i < a.GenParams.Count; i++)
            {
                if (!BCTCompare(a.GenParams[i], b.GenParams[i], ass))
                    return false;
            }
            return true;
        }

        internal static bool MethodSigCompare(Method a, Method b, Assembler ass)
        {
            if (a.HasThis != b.HasThis)
                return false;
            if (a.ExplicitThis != b.ExplicitThis)
                return false;
            if (a.CallingConvention != b.CallingConvention)
                return false;
            if(!ParamCompare(a.RetType, b.RetType, ass))
                return false;
            if (a.Params.Count != b.Params.Count)
                return false;
            for (int i = 0; i < a.Params.Count; i++)
            {
                if(!ParamCompare(a.Params[i], b.Params[i], ass))
                    return false;
            }
            return true;
        }

        internal static bool TypeCompare(Assembler.TypeToCompile a, Assembler.TypeToCompile b, Assembler ass)
        { return TypeCompare(a, b, ass, null); }
        internal static bool TypeCompare(Assembler.TypeToCompile a, Assembler.TypeToCompile b, Assembler ass, CompareOpts opts)
        {
            return ParamCompare(a.tsig, b.tsig, ass, opts);
        }

        internal static bool ParamCompare(Param a, Param b, Assembler ass)
        { return ParamCompare(a, b, ass, null); }
        internal static bool ParamCompare(Param a, Param b, Assembler ass, CompareOpts opts)
        {
            if (!BCTCompare(a.Type, b.Type, ass))
                return false;
            if (a.CustomMods.Count != b.CustomMods.Count)
                return false;
            for (int i = 0; i < a.CustomMods.Count; i++)
            {
                if(!CustomModCompare(a.CustomMods[i], b.CustomMods[i], ass))
                    return false;
            }
            return true;
        }

        private static bool CustomModCompare(CustomMod a, CustomMod b, Assembler ass)
        {
            if (a.TypeOfCustomMod != b.TypeOfCustomMod)
                return false;
            Assembler.TypeToCompile at = Metadata.GetTTC(a.Type, new Assembler.TypeToCompile(), ass);
            Assembler.TypeToCompile bt = Metadata.GetTTC(b.Type, new Assembler.TypeToCompile(), ass);
            return TypeCompare(at, bt, ass);
        }

        internal static bool BCTCompare(BaseOrComplexType a, BaseOrComplexType b, Assembler ass)
        {
            if (a.GetType() != b.GetType())
                return false;
            if (a is Signature.BaseType)
            {
                if (((Signature.BaseType)a).Type == ((Signature.BaseType)b).Type)
                    return true;
                return false;
            }
            else if (a is Signature.ComplexType)
                return CTCompare(a as ComplexType, b as ComplexType, ass);
            else if (a is Signature.GenericType)
                return GTCompare(a as GenericType, b as GenericType, ass);
            else if (a is ZeroBasedArray)
                return BCTCompare(((ZeroBasedArray)a).ElemType, ((ZeroBasedArray)b).ElemType, ass);
            else if (a is BoxedType)
                return BCTCompare(((BoxedType)a).Type, ((BoxedType)b).Type, ass);
            else if (a is ManagedPointer)
                return BCTCompare(((ManagedPointer)a).ElemType, ((ManagedPointer)b).ElemType, ass);
            else if (a is UnmanagedPointer)
                return BCTCompare(((UnmanagedPointer)a).BaseType, ((UnmanagedPointer)b).BaseType, ass);
            else if (a is ComplexArray)
            {
                ComplexArray ca_a = a as ComplexArray;
                ComplexArray ca_b = b as ComplexArray;

                if (!BCTCompare(ca_a.ElemType, ca_b.ElemType, ass))
                    return false;
                if (ca_a.Rank != ca_b.Rank)
                    return false;
                if (ca_a.Sizes.Length != ca_b.Sizes.Length)
                    return false;
                for (int i = 0; i < ca_a.Sizes.Length; i++)
                {
                    if (ca_a.Sizes[i] != ca_b.Sizes[i])
                        return false;
                }
                if (ca_a.LoBounds.Length != ca_b.LoBounds.Length)
                    return false;
                for (int i = 0; i < ca_a.LoBounds.Length; i++)
                {
                    if (ca_a.LoBounds[i] != ca_b.LoBounds[i])
                        return false;
                }
                return true;
            }
            else if (a is GenericMethodParam)
            {
                if (((Signature.GenericMethodParam)a).ParamNumber == ((Signature.GenericMethodParam)b).ParamNumber)
                    return true;
                return false;
            }
            else if (a is GenericParam)
            {
                if (((Signature.GenericParam)a).ParamNumber == ((Signature.GenericParam)b).ParamNumber)
                    return true;
                return false;
            }
            else
                throw new NotImplementedException();
        }

        internal static bool GTCompare(GenericType a, GenericType b, Assembler ass)
        {
            if(!BCTCompare(a.GenType, b.GenType, ass))
                return false;
            if (a.GenParams.Count != b.GenParams.Count)
                return false;
            for (int i = 0; i < a.GenParams.Count; i++)
            {
                if(!BCTCompare(a.GenParams[i], b.GenParams[i], ass))
                    return false;
            }
            return true;
        }

        internal static bool CTCompare(ComplexType a, ComplexType b, Assembler ass)
        {
            Metadata.TypeDefRow tdra, tdrb;

            if (a.Type.Value is Metadata.TypeDefRow)
                tdra = a.Type.Value as Metadata.TypeDefRow;
            else if (a.Type.Value is Metadata.TypeRefRow)
                tdra = Metadata.GetTypeDef(a.Type, ass);
            else
                throw new NotSupportedException();

            if (b.Type.Value is Metadata.TypeDefRow)
                tdrb = b.Type.Value as Metadata.TypeDefRow;
            else if (b.Type.Value is Metadata.TypeRefRow)
                tdrb = Metadata.GetTypeDef(b.Type, ass);
            else
                throw new NotSupportedException();

            if(tdra == tdrb)
                return true;
            return false;
        }

        internal static bool FieldCompare(Assembler.FieldToCompile a, Assembler.FieldToCompile b, Assembler ass)
        { return FieldCompare(a, b, ass, false); }
        internal static bool FieldCompare(Assembler.FieldToCompile a, Assembler.FieldToCompile b, Assembler ass, bool compare_type)
        {
            if (compare_type)
            {
                if (!TypeCompare(new Assembler.TypeToCompile { _ass = ass, type = a.definedin_type, tsig = a.definedin_tsig }, new Assembler.TypeToCompile { _ass = ass, type = b.definedin_type, tsig = b.definedin_tsig }, ass))
                    return false;
            }

            if (a.field.Name != b.field.Name)
                return false;

            return ParamCompare(a.fsig, b.fsig, ass);
        }

        public static string GetString(Signature.Param p, Assembler ass)
        {
            return GetString(p.Type, ass);
        }

        public static string GetString(Assembler.MethodToCompile mtc, Assembler ass)
        { return GetString(mtc.msig, mtc.meth.Name, mtc.meth.GetParamNames(), ass); }

        public static string GetString(Signature.BaseMethod method, string meth_name, List<Metadata.ParamRow> prs, Assembler ass)
        {
            if (method is Signature.Method)
            {
                Signature.Method m = method as Signature.Method;
                StringBuilder sb = new StringBuilder();
                if (!((m.HasThis) || (m.ExplicitThis)))
                    sb.Append("static ");
                sb.Append(GetString(m.RetType, ass));
                sb.Append(" ");
                sb.Append(meth_name);
                sb.Append("(");
                if ((m.HasThis) && (!m.ExplicitThis))
                {
                    sb.Append("this");
                    if (m.Params.Count > 0)
                        sb.Append(", ");
                }
                for (int i = 0; i < m.Params.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(GetString(m.Params[i], ass));

                    foreach (Metadata.ParamRow pr in prs)
                    {
                        if ((pr.Sequence > 0) && ((((int)pr.Sequence) - 1) == i) && (pr.Name != null))
                            sb.Append(" " + pr.Name);
                    }
                }
                sb.Append(")");
                return sb.ToString();
            }
            else if (method is Signature.GenericMethod)
            {
                Signature.GenericMethod gm = method as Signature.GenericMethod;
                string mstr = GetString(gm.GenMethod, meth_name, prs, ass);

                StringBuilder sb = new StringBuilder();
                sb.Append(mstr.Substring(0, mstr.IndexOf('(')));
                sb.Append("<");
                for (int i = 0; i < gm.GenParams.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(GetString(gm.GenParams[i], ass));
                }
                sb.Append(">");
                sb.Append(mstr.Substring(mstr.IndexOf('(')));
                return sb.ToString();
            }
            else
                throw new NotSupportedException();
        }

        public static string GetString(BaseOrComplexType baseOrComplexType, Assembler ass)
        {
            if (baseOrComplexType is BaseType)
            {
                BaseType bt = baseOrComplexType as BaseType;
                return bt.GetTypeName();
            }
            else if (baseOrComplexType is ComplexType)
            {
                ComplexType ct = baseOrComplexType as ComplexType;
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(ct.Type, ass);
                return tdr.TypeFullName;
            }
            else if (baseOrComplexType is ZeroBasedArray)
            {
                ZeroBasedArray zba = baseOrComplexType as ZeroBasedArray;
                return GetString(zba.ElemType, ass) + "[]";
            }
            else if (baseOrComplexType is BoxedType)
            {
                BoxedType bt = baseOrComplexType as BoxedType;
                return "{boxed}" + GetString(bt.Type, ass);
            }
            else if (baseOrComplexType is GenericType)
            {
                GenericType gt = baseOrComplexType as GenericType;
                StringBuilder sb = new StringBuilder();
                sb.Append(GetString(gt.GenType, ass));
                sb.Append("<");
                for (int i = 0; i < gt.GenParams.Count; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(GetString(gt.GenParams[i], ass));
                }
                sb.Append(">");
                return sb.ToString();
            }
            else if (baseOrComplexType is ManagedPointer)
            {
                ManagedPointer mp = baseOrComplexType as ManagedPointer;
                return GetString(mp.ElemType, ass) + " &";
            }
            else if (baseOrComplexType is UnmanagedPointer)
            {
                UnmanagedPointer ump = baseOrComplexType as UnmanagedPointer;
                return GetString(ump.BaseType, ass) + " *";
            }
            else if (baseOrComplexType is GenericMethodParam)
            {
                return "<!!" + ((GenericMethodParam)baseOrComplexType).ParamNumber.ToString() + ">";
            }
            else if (baseOrComplexType is GenericParam)
            {
                return "<!" + ((GenericParam)baseOrComplexType).ParamNumber.ToString() + ">";
            }
            else if (baseOrComplexType is ComplexArray)
            {
                StringBuilder sb = new StringBuilder();
                ComplexArray ca = baseOrComplexType as ComplexArray;
                sb.Append(GetString(ca.ElemType, ass));
                sb.Append("[");
                for (int i = 0; i < ca.Rank; i++)
                {
                    if (i != 0)
                        sb.Append(",");
                    if (i < ca.LoBounds.Length)
                        sb.Append(ca.LoBounds[i].ToString());
                    sb.Append("...");
                    if ((i < ca.LoBounds.Length) && (i < ca.Sizes.Length))
                        sb.Append((ca.LoBounds[i] + ca.Sizes[i]).ToString());
                }
                sb.Append("]");
                return sb.ToString();
            }
            else
                throw new NotImplementedException();
        }

        public static string GetString(Assembler.FieldToCompile ftc, Assembler ass)
        {
            return GetString(ftc.fsig, ass) + " " + ftc.field.Name;
        }
    }
}
