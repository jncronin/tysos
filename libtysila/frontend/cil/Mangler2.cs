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

/* Changed Aug 2011 to use an entirely different mangling system based loosely
 * on the Itanium C++ ABI
 * 
 * We use the following EBNF:
 * 
 * <name>: _Z<type name><object name>
 *         _M<count><module name>              # module info for <module name> (a string, with length in count)
 *         _A<count><assembly name>            # assembly info for <assembly name> (a string, with length in count)
 * 
 * <type name>: <prefix><nested-name><generic-inst>
 * 
 * <prefix>:    P               # unmanaged pointer to
 *              R               # reference to
 *              B               # boxed type
 * 
 * <nested-name>:   N<module count><module><nspace count><nspace><name count><type name>
 *                      # type specified by module, name space and name
 *                  U<nspace count><nspace><name count><type name>
 *                      # type specified by most recently used module, name space and name
 *                  V<name count><type name>
 *                      # type specified by most recently used module and name space, and name
 *                  W<nspace count><nspace><name count><type name>
 *                      # type within mscorlib, specified by name space and name
 *                  X<name count><type name>
 *                      # type within libsupcs, in namespace libsupcs, specified by name
 *                  <predefined-name>
 *                  
 * where <module count>, <nspace count> and <name count> are of type <integer>
 * <integer>: integer in decimal format
 *                  
 * <predefined-name>:   v               # void
 *                      c               # Char
 *                      b               # Boolean
 *                      a               # I1
 *                      h               # U1
 *                      s               # I2
 *                      t               # U2
 *                      i               # I4
 *                      j               # U4
 *                      x               # I8
 *                      y               # U8
 *                      f               # R4
 *                      d               # R8
 *                      u1I             # I
 *                      u1U             # U
 *                      u1S             # String
 *                      u1T             # TypedByRef
 *                      u1O             # Object
 *                      u1V             # VirtFtnPtr
 *                      u1P             # Uninstantiated generic param
 *                      u1A<array-def>  # ComplexArray
 *                      u1Z<elem-type>  # ZeroBasedArray, <elem-type> is of type <type name>
 *                      u1t             # this pointer
 *                      u1G<integer>    # GenericParam, followed by ParamNumber
 *                      u1g<integer>    # GenericMethodParam, followed by ParamNumber
 *                      u1R             # Ref - used to denote a generic reference type in a coalesced generic method
 *                      
 * <array-def>: <type name><rank>_<lobound 0>_ ... <lobound n-1>_<size 0>_ ... <size n-1>_
 * <type name> is the base type of the array of type <string>
 * <rank>, <lobound n>, <size n> are all of type <integer>
 * 
 * <object name>:   TV                                      # Virtual table
 *                  TI                                      # Typeinfo structure
 *                  MI<method-def>                          # Method info for a certain method
 *                  FI<name count><name><field-type>        # Field info for a certain field
 *                  M_<method-def>                          # Executable code for a certain method
 *                  S                                       # Static data for a type
 *                  
 * <method-def>:    <flags>_<name count><name><ret-type><params><generic-meth-inst>
 * <flags>:         0       CallConv.Default
 *                  1       CallConv.VarArg
 *                  2       CallConv.Generic
 * 
 * <ret-type>:      _R<type name>
 * <field-type>:    _R<type name>
 * 
 * where <name count> is of type <integer>
 * 
 * <params>: _P<param count><param 1><param 2><param 3>...<param n>
 * where <param> is of type <type name> and <param count> is of type <integer>
 * 
 * <generic-inst>:  <nothing>
 *                  _G<param count><gparam 1><gparam 2><gparam 3>...<gparam n>
 * <generic-meth-inst>: <nothing>
 *                      _g<param count><gparam 1><gparam 2><gparam 3>...<gparam n>
 *                     
 * where <gparam> is of type <type name>
 * 
 * <string>:    ASCII encoding of a string
 *              Characters which cannot be directly represented in ELF symbols are represented as #XX
 *              where XX is the hexadecimal encoding (using capitals) of the character
 */

namespace libtysila
{
    public class Mangler2
    {
        public class ObjectToMangle
        {
            public Signature.BaseOrComplexType Type;
            public string ObjectName;
            public ObjectTypeType ObjectType;
            public Signature.Param Field;
            public Signature.BaseMethod Method;
            public Metadata.MethodDefRow mdr;

            public enum ObjectTypeType { TypeInfo, VTable, MethodInfo, FieldInfo, Method, StaticData, Assembly, Module, Unknown, NotFound };
        }

        class ManglerState
        {
            public string cur_module = null;
            public string cur_nspace = null;
        }

        public static string MangleModule(Metadata m, Assembler ass)
        { return MangleName(new ObjectToMangle { ObjectName = m.ModuleName, ObjectType = ObjectToMangle.ObjectTypeType.Module }, ass); }
        public static string MangleAssembly(Metadata m, Assembler ass)
        { return MangleName(new ObjectToMangle { ObjectName = m.ModuleName, ObjectType = ObjectToMangle.ObjectTypeType.Assembly }, ass); }
        public static string MangleTypeInfo(Assembler.TypeToCompile ttc, Assembler ass)
        { return MangleName(new ObjectToMangle { Type = ttc.tsig.Type, ObjectType = ObjectToMangle.ObjectTypeType.TypeInfo }, ass); }
        public static string MangleVTableName(Assembler.TypeToCompile ttc, Assembler ass)
        { return MangleName(new ObjectToMangle { Type = ttc.tsig.Type, ObjectType = ObjectToMangle.ObjectTypeType.VTable }, ass); }
        public static string MangleTypeStatic(Assembler.TypeToCompile ttc, Assembler ass)
        { return MangleName(new ObjectToMangle { Type = ttc.tsig.Type, ObjectType = ObjectToMangle.ObjectTypeType.StaticData }, ass); }
        public static string MangleMethod(Assembler.MethodToCompile mtc, Assembler ass)
        { return MangleName(new ObjectToMangle { Type = mtc.tsig, ObjectName = mtc.meth.Name, Method = mtc.msig, ObjectType = ObjectToMangle.ObjectTypeType.Method, mdr = mtc.meth }, ass); }
        public static string MangleMethodInfoSymbol(Assembler.MethodToCompile mtc, Assembler ass)
        { return MangleName(new ObjectToMangle { Type = mtc.tsig, ObjectName = mtc.meth.Name, Method = mtc.msig, ObjectType = ObjectToMangle.ObjectTypeType.MethodInfo }, ass); }
        public static string MangleFieldInfoSymbol(Assembler.FieldToCompile ftc, Assembler ass)
        { return MangleName(new ObjectToMangle { Type = ftc.definedin_tsig.Type, ObjectName = ftc.field.Name, Field = ftc.fsig, ObjectType = ObjectToMangle.ObjectTypeType.FieldInfo }, ass); }
        public static string MangleMethodOnly(Assembler.MethodToCompile mtc, Assembler ass)
        {
            StringBuilder sb = new StringBuilder();
            MangleObjectName(sb, new ObjectToMangle { Type = mtc.tsig, ObjectName = mtc.meth.Name, Method = mtc.msig, ObjectType = ObjectToMangle.ObjectTypeType.Method }, ass, new ManglerState());
            return sb.ToString();
        }

        public static string MangleName(ObjectToMangle obj, Assembler ass)
        {
            StringBuilder sb = new StringBuilder();
            ManglerState state = new ManglerState();

            if (obj.ObjectType == ObjectToMangle.ObjectTypeType.Module)
            {
                sb.Append("_M");
                string obj_name = EncodeString(obj.ObjectName);
                sb.Append(obj_name.Length.ToString());
                sb.Append(obj_name);
            }
            else if (obj.ObjectType == ObjectToMangle.ObjectTypeType.Assembly)
            {
                sb.Append("_A");
                string obj_name = EncodeString(obj.ObjectName);
                sb.Append(obj_name.Length.ToString());
                sb.Append(obj_name);
            }
            else
            {
                if ((obj.ObjectType == ObjectToMangle.ObjectTypeType.Method) && (obj.mdr != null) && (obj.mdr.ReferenceAlias != null))
                    return obj.mdr.ReferenceAlias;
                sb.Append("_Z");
                MangleTypeName(sb, obj.Type, ass, state);
                MangleObjectName(sb, obj, ass, state);
            }

            return sb.ToString();
        }

        private static void MangleObjectName(StringBuilder sb, ObjectToMangle obj, Assembler ass, ManglerState state)
        {
            switch (obj.ObjectType)
            {
                case ObjectToMangle.ObjectTypeType.FieldInfo:
                    {
                        sb.Append("FI");
                        string obj_name = EncodeString(obj.ObjectName);
                        sb.Append(obj_name.Length.ToString());
                        sb.Append(obj_name);
                        sb.Append("_R");
                        MangleTypeName(sb, obj.Field.Type, ass, state);
                        break;
                    }
                case ObjectToMangle.ObjectTypeType.Method:
                    {
                        sb.Append("M_");
                        sb.Append(((int)obj.Method.Method.CallingConvention).ToString());
                        sb.Append("_");
                        string obj_name = EncodeString(obj.ObjectName);
                        sb.Append(obj_name.Length.ToString());
                        sb.Append(obj_name);
                        sb.Append("_R");
                        MangleTypeName(sb, obj.Method.Method.RetType.Type, ass, state);
                        MangleMethodParams(sb, obj.Method.Method, ass, state);
                        MangleGenericMethodParams(sb, obj.Method, ass, state);
                        break;
                    }
                case ObjectToMangle.ObjectTypeType.MethodInfo:
                    {
                        sb.Append("MI");
                        sb.Append(((int)obj.Method.Method.CallingConvention).ToString());
                        sb.Append("_");
                        string obj_name = EncodeString(obj.ObjectName);
                        sb.Append(obj_name.Length.ToString());
                        sb.Append(obj_name);
                        sb.Append("_R");
                        MangleTypeName(sb, obj.Method.Method.RetType.Type, ass, state);
                        MangleMethodParams(sb, obj.Method.Method, ass, state);
                        MangleGenericMethodParams(sb, obj.Method, ass, state);
                        break;
                    }
                case ObjectToMangle.ObjectTypeType.StaticData:
                    sb.Append("S");
                    break;
                case ObjectToMangle.ObjectTypeType.TypeInfo:
                    sb.Append("TI");
                    break;
                case ObjectToMangle.ObjectTypeType.VTable:
                    sb.Append("VT");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void MangleGenericMethodParams(StringBuilder sb, Signature.BaseMethod baseMethod, Assembler ass, ManglerState state)
        {
            if (baseMethod is Signature.GenericMethod)
            {
                sb.Append("_g");
                sb.Append(((Signature.GenericMethod)baseMethod).GenParams.Count.ToString());
                foreach (Signature.BaseOrComplexType p in ((Signature.GenericMethod)baseMethod).GenParams)
                    MangleTypeName(sb, p, ass, state);
            }
        }

        private static void MangleMethodParams(StringBuilder sb, Signature.Method method, Assembler ass, ManglerState state)
        {
            sb.Append("_P");
            sb.Append((method.Params.Count + ((method.HasThis && !method.ExplicitThis) ? 1 : 0)).ToString());

            if (method.HasThis && !method.ExplicitThis)
                sb.Append("u1t");       // this pointer
            foreach (Signature.Param p in method.Params)
                MangleTypeName(sb, p.Type, ass, state);
        }

        static void MangleTypeName(StringBuilder sb, Signature.BaseOrComplexType type, Assembler ass, ManglerState state)
        {
            while (!(type is Signature.BaseType) && !(type is Signature.ComplexType) && !(type is Signature.GenericType) &&
                !(type is Signature.GenericParam) && !(type is Signature.GenericMethodParam))
            {
                if (type is Signature.BoxedType)
                {
                    Signature.BoxedType bt = type as Signature.BoxedType;
                    sb.Append("B");
                    type = bt.Type;
                }
                else if (type is Signature.ManagedPointer)
                {
                    Signature.ManagedPointer mp = type as Signature.ManagedPointer;
                    sb.Append("R");
                    type = mp.ElemType;
                }
                else if (type is Signature.UnmanagedPointer)
                {
                    Signature.UnmanagedPointer ump = type as Signature.UnmanagedPointer;
                    sb.Append("P");
                    type = ump.BaseType;
                }
                else if (type is Signature.ZeroBasedArray)
                {
                    Signature.ZeroBasedArray zba = type as Signature.ZeroBasedArray;
                    sb.Append("u1Z");
                    MangleTypeName(sb, zba.ElemType, ass, state);
                    return;
                }
                else if (type is Signature.ComplexArray)
                {
                    Signature.ComplexArray ca = type as Signature.ComplexArray;
                    sb.Append("u1A");
                    MangleTypeName(sb, ca.ElemType, ass, state);

                    sb.Append(ca.Rank.ToString());
                    sb.Append("_");
                    for (int i = 0; i < ca.Rank; i++)
                    {
                        if (i < ca.LoBounds.Length)
                            sb.Append(ca.LoBounds[i].ToString());
                        sb.Append("_");
                    }
                    for (int i = 0; i < ca.Rank; i++)
                    {
                        if (i < ca.Sizes.Length)
                            sb.Append(ca.Sizes[i].ToString());
                        sb.Append("_");
                    }
                    return;
                }
                else throw new NotSupportedException();
            }

            if (type is Signature.BaseType)
            {
                Signature.BaseType bt = type as Signature.BaseType;
                switch (bt.Type)
                {
                    case BaseType_Type.Void:
                        sb.Append("v");
                        break;
                    case BaseType_Type.Char:
                        sb.Append("c");
                        break;
                    case BaseType_Type.Boolean:
                        sb.Append("b");
                        break;
                    case BaseType_Type.I1:
                        sb.Append("a");
                        break;
                    case BaseType_Type.U1:
                        sb.Append("h");
                        break;
                    case BaseType_Type.I2:
                        sb.Append("s");
                        break;
                    case BaseType_Type.U2:
                        sb.Append("t");
                        break;
                    case BaseType_Type.I4:
                        sb.Append("i");
                        break;
                    case BaseType_Type.U4:
                        sb.Append("j");
                        break;
                    case BaseType_Type.I8:
                        sb.Append("x");
                        break;
                    case BaseType_Type.U8:
                        sb.Append("y");
                        break;
                    case BaseType_Type.R4:
                        sb.Append("f");
                        break;
                    case BaseType_Type.R8:
                        sb.Append("d");
                        break;
                    case BaseType_Type.I:
                        sb.Append("u1I");
                        break;
                    case BaseType_Type.U:
                        sb.Append("u1U");
                        break;
                    case BaseType_Type.String:
                        sb.Append("u1S");
                        break;
                    case BaseType_Type.TypedByRef:
                        sb.Append("u1T");
                        break;
                    case BaseType_Type.Object:
                        sb.Append("u1O");
                        break;
                    case BaseType_Type.VirtFtnPtr:
                        sb.Append("u1V");
                        break;
                    case BaseType_Type.UninstantiatedGenericParam:
                        sb.Append("u1P");
                        break;
                    case BaseType_Type.RefGenericParam:
                        sb.Append("u1R");
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (type is Signature.ComplexType)
            {
                Signature.ComplexType ct = type as Signature.ComplexType;
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(ct.Type, ass);

                string mod = EncodeString(tdr.m.ModuleName);
                string nspace = EncodeString(tdr.TypeNamespace);
                string name = EncodeString(tdr.TypeName);

                if (mod == state.cur_module)
                {
                    if (nspace == state.cur_nspace)
                    {
                        sb.Append("V");
                        AppendStringWithLength(sb, name);
                    }
                    else
                    {
                        sb.Append("U");
                        AppendStringWithLength(sb, nspace);
                        AppendStringWithLength(sb, name);
                        state.cur_nspace = nspace;
                    }
                }
                else if (mod == "mscorlib")
                {
                    sb.Append("W");
                    AppendStringWithLength(sb, nspace);
                    AppendStringWithLength(sb, name);
                    state.cur_module = mod;
                    state.cur_nspace = nspace;
                }
                else if ((mod == "libsupcs") && (nspace == "libsupcs"))
                {
                    sb.Append("X");
                    AppendStringWithLength(sb, name);
                    state.cur_module = mod;
                    state.cur_nspace = nspace;
                }
                else
                {
                    sb.Append("N");
                    AppendStringWithLength(sb, mod);
                    AppendStringWithLength(sb, nspace);
                    AppendStringWithLength(sb, name);
                    state.cur_module = mod;
                    state.cur_nspace = nspace;
                }
            }
            else if (type is Signature.GenericType)
            {
                Signature.GenericType gt = type as Signature.GenericType;
                MangleTypeName(sb, gt.GenType, ass, state);
                sb.Append("_G");
                sb.Append(gt.GenParams.Count.ToString());
                foreach (Signature.BaseOrComplexType p in gt.GenParams)
                    MangleTypeName(sb, p, ass, state);
            }
            else if (type is Signature.GenericParam)
            {
                Signature.GenericParam gp = type as Signature.GenericParam;
                sb.Append("u1G");
                sb.Append(gp.ParamNumber.ToString());
            }
            else if (type is Signature.GenericMethodParam)
            {
                Signature.GenericMethodParam gmp = type as Signature.GenericMethodParam;
                sb.Append("u1g");
                sb.Append(gmp.ParamNumber.ToString());
            }
            else
                throw new NotSupportedException();
        }

        public static Assembler.TypeToCompile DemangleType(string name, Assembler ass)
        {
            ObjectToMangle obj = DemangleName(name, ass);
            if ((obj.ObjectType == ObjectToMangle.ObjectTypeType.TypeInfo) || (obj.ObjectType == ObjectToMangle.ObjectTypeType.StaticData))
            {
                return GetType(obj, ass);
            }
            throw new Exception("Invalid mangled name");
        }

        public static Assembler.MethodToCompile DemangleMethod(string name, Assembler ass)
        {
            ObjectToMangle obj = DemangleName(name, ass);
            if ((obj.ObjectType == ObjectToMangle.ObjectTypeType.Method) || (obj.ObjectType == ObjectToMangle.ObjectTypeType.MethodInfo))
            {
                return GetMethod(obj, ass);
            }
            throw new Exception("Invalid mangled name");
        }

        public static Assembler.FieldToCompile DemangleField(string name, Assembler ass)
        {
            ObjectToMangle obj = DemangleName(name, ass);
            if (obj.ObjectType == ObjectToMangle.ObjectTypeType.FieldInfo)
            {
                return GetField(obj, ass);
            }
            throw new Exception("Invalid mangled name");
        }

        public static Assembler.TypeToCompile GetType(ObjectToMangle obj, Assembler ass)
        {
            return new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(obj.Type, ass), type = Metadata.GetTypeDef(obj.Type, ass, false) };
        }

        public static Assembler.MethodToCompile GetMethod(ObjectToMangle obj, Assembler ass)
        {
            Assembler.MethodToCompile ret = new Assembler.MethodToCompile { _ass = ass };
            ret.tsigp = new Signature.Param(obj.Type, ass);
            ret.type = Metadata.GetTypeDef(obj.Type, ass, false);
            ret.msig = obj.Method;
            ret.meth = Metadata.GetMethodDef(obj.ObjectName, ret.type, ret.msig, ret.tsig, ret.msig, ass);
            return ret;
        }

        public static Assembler.FieldToCompile GetField(ObjectToMangle obj, Assembler ass)
        {
            Assembler.FieldToCompile ret = new Assembler.FieldToCompile { _ass = ass };
            ret.definedin_tsig = ret.memberof_tsig = new Signature.Param(obj.Type, ass);
            ret.definedin_type = ret.memberof_type = Metadata.GetTypeDef(obj.Type, ass, false);
            ret.fsig = obj.Field;

            if (obj.ObjectName == "m_value")
                ret.field = new Metadata.FieldRow { ass = ass, Name = "m_value", owning_type = ret.definedin_type };
            else
                ret.field = Metadata.GetFieldDef(obj.ObjectName, ret.definedin_type, ass);
            return ret;
        }

        public static ObjectToMangle DemangleName(string name, Assembler ass)
        {
            char[] c = name.ToCharArray();
            int x = 0;
            ObjectToMangle ret;
            ManglerState state = new ManglerState();

            if (ReadString("_M", c, ref x))
                ret = new ObjectToMangle { ObjectName = ReadStringWithLength(c, ref x), ObjectType = ObjectToMangle.ObjectTypeType.Module };
            else if (ReadString("_A", c, ref x))
                ret = new ObjectToMangle { ObjectName = ReadStringWithLength(c, ref x), ObjectType = ObjectToMangle.ObjectTypeType.Assembly };
            else if (!ReadString("_Z", c, ref x))
            {
                ret = new ObjectToMangle();
                ret.ObjectType = ObjectToMangle.ObjectTypeType.Unknown;
                ret.ObjectName = name;
                return ret;
            }
            else
            {
                Signature.BaseOrComplexType type = DemangleTypeName(c, ref x, ass, state);
                if (type == null)
                    ret = new ObjectToMangle { ObjectType = ObjectToMangle.ObjectTypeType.NotFound };
                else
                    ret = DemangleObject(c, ref x, ass, state);
                ret.Type = type;
            }
            return ret;
        }

        private static ObjectToMangle DemangleObject(char[] c, ref int x, Assembler ass, ManglerState state)
        {
            ObjectToMangle ret = new ObjectToMangle();

            if (ReadString("TI", c, ref x))
                ret.ObjectType = ObjectToMangle.ObjectTypeType.TypeInfo;
            else if (ReadString("S", c, ref x))
                ret.ObjectType = ObjectToMangle.ObjectTypeType.StaticData;
            else if (ReadString("MI", c, ref x))
            {
                ret.ObjectType = ObjectToMangle.ObjectTypeType.MethodInfo;
                Signature.Method.CallConv cc = (Signature.Method.CallConv)(ReadInteger(c, ref x).Value);
                ExpectString("_", c, ref x);
                int name_len = ReadInteger(c, ref x).Value;
                ret.ObjectName = ReadString(name_len, c, ref x);
                ret.Method = DemangleMethodParams(c, ref x, ass, state);
                ret.Method.Method.CallingConvention = cc;
                Signature.GenericMethod gm = DemangleGenericMethodParams(c, ref x, ass, state);
                if (gm != null)
                {
                    gm.GenMethod = ret.Method as Signature.Method;
                    ret.Method.Method.GenParamCount = gm.GenParams.Count;
                    ret.Method = gm;
                }
            }
            else if (ReadString("M_", c, ref x))
            {
                ret.ObjectType = ObjectToMangle.ObjectTypeType.Method;
                Signature.Method.CallConv cc = (Signature.Method.CallConv)(ReadInteger(c, ref x).Value);
                ExpectString("_", c, ref x);
                int name_len = ReadInteger(c, ref x).Value;
                ret.ObjectName = ReadString(name_len, c, ref x);
                ret.Method = DemangleMethodParams(c, ref x, ass, state);
                ret.Method.Method.CallingConvention = cc;
                Signature.GenericMethod gm = DemangleGenericMethodParams(c, ref x, ass, state);
                if (gm != null)
                {
                    gm.GenMethod = ret.Method as Signature.Method;
                    ret.Method.Method.GenParamCount = gm.GenParams.Count;
                    ret.Method = gm;
                }
            }
            else if (ReadString("FI", c, ref x))
            {
                ret.ObjectType = ObjectToMangle.ObjectTypeType.FieldInfo;
                int name_len = ReadInteger(c, ref x).Value;
                ret.ObjectName = ReadString(name_len, c, ref x);
                ExpectString("_R", c, ref x);
                ret.Field = new Signature.Param(DemangleTypeName(c, ref x, ass, state), ass);
            }
            else
                throw new NotSupportedException();

            return ret;
        }

        private static Signature.GenericMethod DemangleGenericMethodParams(char[] c, ref int x, Assembler ass, ManglerState state)
        {
            if (!ReadString("_g", c, ref x))
                return null;

            Signature.GenericMethod ret = new Signature.GenericMethod();

            int pc = ReadInteger(c, ref x).Value;
            for (int i = 0; i < pc; i++)
                ret.GenParams.Add(DemangleTypeName(c, ref x, ass, state));

            return ret;
        }

        private static Signature.BaseMethod DemangleMethodParams(char[] c, ref int x, Assembler ass, ManglerState state)
        {
            Signature.Method ret = new Signature.Method();
            ExpectString("_R", c, ref x);
            ret.RetType = new Signature.Param(DemangleTypeName(c, ref x, ass, state), ass);

            ExpectString("_P", c, ref x);

            int pc = ReadInteger(c, ref x).Value;
            for (int i = 0; i < pc; i++)
            {
                if (ReadString("u1t", c, ref x))
                {
                    if (i != 0)
                        throw new Exception("Out of order this pointer");
                    ret.HasThis = true;
                }
                else
                    ret.Params.Add(new Signature.Param(DemangleTypeName(c, ref x, ass, state), ass));
            }

            return ret;
        }

        private static Signature.BaseOrComplexType DemangleTypeName(char[] c, ref int x, Assembler ass, ManglerState state)
        {
            bool reading_prefixes = true;
            List<string> prefixes = new List<string>();

            while (reading_prefixes)
            {
                if (ReadString("P", c, ref x))
                    prefixes.Add("P");
                else if (ReadString("R", c, ref x))
                    prefixes.Add("R");
                else if (ReadString("B", c, ref x))
                    prefixes.Add("B");
                else
                    reading_prefixes = false;
            }

            Signature.BaseOrComplexType type = null;

            if (ReadString("v", c, ref x))
                type = new Signature.BaseType(BaseType_Type.Void);
            else if (ReadString("c", c, ref x))
                type = new Signature.BaseType(BaseType_Type.Char);
            else if (ReadString("b", c, ref x))
                type = new Signature.BaseType(BaseType_Type.Boolean);
            else if (ReadString("a", c, ref x))
                type = new Signature.BaseType(BaseType_Type.I1);
            else if (ReadString("h", c, ref x))
                type = new Signature.BaseType(BaseType_Type.U1);
            else if (ReadString("s", c, ref x))
                type = new Signature.BaseType(BaseType_Type.I2);
            else if (ReadString("t", c, ref x))
                type = new Signature.BaseType(BaseType_Type.U2);
            else if (ReadString("i", c, ref x))
                type = new Signature.BaseType(BaseType_Type.I4);
            else if (ReadString("j", c, ref x))
                type = new Signature.BaseType(BaseType_Type.U4);
            else if (ReadString("x", c, ref x))
                type = new Signature.BaseType(BaseType_Type.I8);
            else if (ReadString("y", c, ref x))
                type = new Signature.BaseType(BaseType_Type.U8);
            else if (ReadString("f", c, ref x))
                type = new Signature.BaseType(BaseType_Type.R4);
            else if (ReadString("d", c, ref x))
                type = new Signature.BaseType(BaseType_Type.R8);
            else if (ReadString("u1I", c, ref x))
                type = new Signature.BaseType(BaseType_Type.I);
            else if (ReadString("u1U", c, ref x))
                type = new Signature.BaseType(BaseType_Type.U);
            else if (ReadString("u1S", c, ref x))
                type = new Signature.BaseType(BaseType_Type.String);
            else if (ReadString("u1T", c, ref x))
                type = new Signature.BaseType(BaseType_Type.TypedByRef);
            else if (ReadString("u1O", c, ref x))
                type = new Signature.BaseType(BaseType_Type.Object);
            else if (ReadString("u1V", c, ref x))
                type = new Signature.BaseType(BaseType_Type.VirtFtnPtr);
            else if (ReadString("u1P", c, ref x))
                type = new Signature.BaseType(BaseType_Type.UninstantiatedGenericParam);
            else if (ReadString("u1G", c, ref x))
            {
                int? gpi = ReadInteger(c, ref x);
                if (gpi.HasValue)
                    type = new Signature.GenericParam { _ass = ass, ParamNumber = gpi.Value };
                else
                    throw new Exception("Expected integer at position " + x.ToString());
            }
            else if (ReadString("u1A", c, ref x))
            {
                // Read ComplexArray
                Signature.ComplexArray ca = new Signature.ComplexArray();
                type = ca;
                ca._ass = ass;
                ca.ElemType = DemangleTypeName(c, ref x, ass, state);
                if (ca.ElemType == null)
                    throw new Exception("Invalid type name in ComplexArray sequence");
                ca.Rank = ReadInteger(c, ref x).Value;
                ExpectString("_", c, ref x);
                List<int> lobounds = new List<int>();
                List<int> sizes = new List<int>();
                for (int i = 0; i < ca.Rank; i++)
                {
                    int? lobound = ReadInteger(c, ref x);
                    if (lobound.HasValue)
                        lobounds.Add(lobound.Value);
                    ExpectString("_", c, ref x);
                }
                for (int i = 0; i < ca.Rank; i++)
                {
                    int? size = ReadInteger(c, ref x);
                    if (size.HasValue)
                        sizes.Add(size.Value);
                    ExpectString("_", c, ref x);
                }
                ca.LoBounds = lobounds.ToArray();
                ca.Sizes = sizes.ToArray();
            }
            else if (ReadString("u1Z", c, ref x))
            {
                // Read SzArray
                Signature.ZeroBasedArray zba = new Signature.ZeroBasedArray();
                type = zba;
                zba._ass = ass;
                zba.ElemType = DemangleTypeName(c, ref x, ass, state);
                if (zba.ElemType == null)
                    throw new Exception("Invalid type name in ZeroBasedArray sequence");
            }
            else if (ReadString("N", c, ref x))
            {
                // Read ComplexType
                Signature.ComplexType ct = new Signature.ComplexType(ass);
                ct._ass = ass;

                string mod = ReadStringWithLength(c, ref x);
                string nspace = ReadStringWithLength(c, ref x);
                string name = ReadStringWithLength(c, ref x);

                state.cur_module = mod;
                state.cur_nspace = nspace;

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mod, nspace, name, ass);
                ct.Type = new Token(tdr);
                type = ct;
            }
            else if (ReadString("U", c, ref x))
            {
                // Read ComplexType
                Signature.ComplexType ct = new Signature.ComplexType(ass);
                ct._ass = ass;

                if (state.cur_module == null)
                    throw new Exception("Invalid 'U' token");
                string mod = state.cur_module;
                string nspace = ReadStringWithLength(c, ref x);
                string name = ReadStringWithLength(c, ref x);

                state.cur_nspace = nspace;

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mod, nspace, name, ass);
                if (tdr == null)
                    return null;
                ct.Type = new Token(tdr);
                type = ct;
            }
            else if (ReadString("V", c, ref x))
            {
                // Read ComplexType
                Signature.ComplexType ct = new Signature.ComplexType(ass);
                ct._ass = ass;

                if (state.cur_module == null)
                    throw new Exception("Invalid 'U' token");
                if (state.cur_nspace == null)
                    throw new Exception("Invalid 'U' token");
                string mod = state.cur_module;
                string nspace = state.cur_nspace;
                string name = ReadStringWithLength(c, ref x);

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mod, nspace, name, ass);
                if (tdr == null)
                    return null;
                ct.Type = new Token(tdr);
                type = ct;
            }
            else if (ReadString("W", c, ref x))
            {
                // Read ComplexType
                Signature.ComplexType ct = new Signature.ComplexType(ass);
                ct._ass = ass;

                string mod = "mscorlib";
                string nspace = ReadStringWithLength(c, ref x);
                string name = ReadStringWithLength(c, ref x);

                state.cur_module = mod;
                state.cur_nspace = nspace;

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mod, nspace, name, ass);
                if (tdr == null)
                    return null;
                ct.Type = new Token(tdr);
                type = ct;
            }
            else if (ReadString("X", c, ref x))
            {
                // Read ComplexType
                Signature.ComplexType ct = new Signature.ComplexType(ass);
                ct._ass = ass;

                string mod = "libsupcs";
                string nspace = "libsupcs";
                string name = ReadStringWithLength(c, ref x);

                state.cur_module = mod;
                state.cur_nspace = nspace;

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mod, nspace, name, ass);
                if (tdr == null)
                    return null;
                ct.Type = new Token(tdr);
                type = ct;
            }
            else
                return null;

            if (ReadString("_G", c, ref x))
            {
                Signature.GenericType gt = new Signature.GenericType { _ass = ass, GenType = type };

                // Read generic params
                int pc = ReadInteger(c, ref x).Value;
                for (int i = 0; i < pc; i++)
                    gt.GenParams.Add(DemangleTypeName(c, ref x, ass, state));
                type = gt;
            }
 
            // Interpret prefixes
            for (int i = prefixes.Count - 1; i >= 0; i--)
            {
                if (prefixes[i] == "P")
                {
                    Signature.UnmanagedPointer ump = new Signature.UnmanagedPointer { _ass = ass, BaseType = type };
                    type = ump;
                }
                else if (prefixes[i] == "R")
                {
                    Signature.ManagedPointer mp = new Signature.ManagedPointer { _ass = ass, ElemType = type };
                    type = mp;
                }
                else if (prefixes[i] == "B")
                {
                    Signature.BoxedType bt = new Signature.BoxedType { _ass = ass, Type = type };
                    type = bt;
                }
                else
                    throw new NotSupportedException();
            }

            return type;
        }

        private static int? ReadInteger(char[] c, ref int x)
        {
            List<char> c_l = new List<char>();

            char cur_c = c[x];
            if (cur_c == '0')
            {
                x++;
                return 0;
            }

            while ((cur_c >= '0') && (cur_c <= '9'))
            {
                c_l.Add(cur_c);
                x++;
                cur_c = c[x];
            }

            if (c_l.Count == 0)
                return null;

            string s = new string(c_l.ToArray());
            return Int32.Parse(s);
        }

        private static string ReadString(int count, char[] c, ref int x)
        {
            if ((x + count) > c.Length)
                return null;
            StringBuilder ret = new StringBuilder();
            int i = x;
            while(i < (x + count))
            {
                if (c[i] == '#')
                {
                    if ((i + 3) >= c.Length)
                        throw new Exception("Invalid character encoding");
                    i++;
                    string s = new string(c, i, 2);
                    ret.Append((char)Convert.ToByte(s, 16));
                    i += 2;
                }
                else
                {
                    ret.Append(c[i]);
                    i++;
                }
            }

            x += count;
            return ret.ToString();
        }

        private static bool ReadString(string p, char[] c, ref int x)
        {
            /* Compare the characters at position x in c with p
             * If they are equal, advance x by the length of p and return true
             * Else return false and do not advance x
             */

            if ((x + p.Length) > c.Length)
                return false;
            if (p == new string(c, x, p.Length))
            {
                x += p.Length;
                return true;
            }
            return false;
        }

        private static void ExpectString(string p, char[] c, ref int x)
        {
            if(ReadString(p, c, ref x) == false)
                throw new Exception("Invalid mangled name: " + p + " expected");
        }

        public static string EncodeString(string p)
        {
            /* Many tools cannot deal with +, - and . characters in labels.
             * 
             * Therefore we encode them to # followed by the hex value (in capitals) of the ascii code of the character
             * We encode # itself too
             */

            if (p.Contains("+") || p.Contains("-") || p.Contains(".") || p.Contains("#") || p.Contains(":") || p.Contains("/") || p.Contains("\\"))
            {
                StringBuilder sb = new StringBuilder();
                foreach (char c in p)
                {
                    if ((c == '+') || (c == '-') || (c == '.') || (c == '#') || (c == ':') || (c == '/') || (c == '\\'))
                    {
                        byte[] enc = ASCIIEncoding.ASCII.GetBytes(new char[] { c });
                        sb.Append("#");
                        sb.Append(enc[0].ToString("X2"));
                    }
                    else
                        sb.Append(c);
                }
                return sb.ToString();
            }
            return p;
        }

        private static void AppendStringWithLength(StringBuilder sb, string s)
        {
            sb.Append(s.Length.ToString());
            sb.Append(s);
        }

        private static string ReadStringWithLength(char[] c, ref int x)
        {
            int str_len = ReadInteger(c, ref x).Value;
            return ReadString(str_len, c, ref x);
        }
    }
}
