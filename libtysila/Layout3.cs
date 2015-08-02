/* Copyright (C) 2011 by John Cronin
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


/* A reimplementation of the layout engine to support classes with single inheritance
 * and multiple interface implementations similar to Java
 * 
 * The layout is much simplified compared with Layout2.  We still define the object
 * instance layout, the vtable layout and the typeinfo layout, but all instances of an
 * object reference the same object pointer, with the base classes' members first
 * all the way up to the current class's members.
 * 
 * e.g.
 * 
 * Object layout             Vtable layout
 * 
 * vtbl_pointer  --------->  typeinfo_pointer -----------------> typeinfo
 * __object_id           +-- itablemap_pointer
 * extends vtbl_ptr      |   ToString()
 * base class fields     |   Equals()
 * derived class fields  |   GetHashCode()
 *                       |   Finalize()
 *                       |   BaseClass.method1()
 *                       |   BaseClass.method2()
 *                       |   DerivedClass.method1()
 *                       |   DerivedClass.method2()
 *                       +-> InterfaceA typeinfo_pointer ------> typeinfo
 *                       +-- InterfaceA method_list_pointer
 *                       |   InterfaceB typeinfo_pointer ------> typeinfo
 *                       |   InterfaceB method_list_pointer --+
 *                       |   null_ptr (end of list)           |
 *                       +-> InterfaceA.method1()             |
 *                           InterfaceA.method2()             |
 *                           InterfaceB.method1()  <----------+
 *                           InterfaceB.method2()
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    public partial class Layout
    {
        static Dictionary<Assembler.TypeToCompile, Layout> layout_cache = new Dictionary<Assembler.TypeToCompile, Layout>();
        static Dictionary<Assembler.TypeToCompile, int> class_size_cache = new Dictionary<Assembler.TypeToCompile, int>();
        static Dictionary<Assembler.TypeToCompile, Interface> interface_cache = new Dictionary<Assembler.TypeToCompile, Interface>();

        Assembler _ass;
        Assembler.TypeToCompile _ttc;

        public List<Field> InstanceFields;
        public List<Field> StaticFields;
        public List<Method> VirtualMethods;
        public List<Interface> Interfaces;
        public List<Assembler.TypeToCompile> ClassesImplemented;
        public Assembler.TypeToCompile? Extends;
        public Dictionary<string, int> InstanceFieldOffsets;

        public int vtbl_offset = 0;
        public bool has_vtbl = false;
        public int vtbl_offset_within_typeinfo = 0;
        public bool has_obj_id = false;
        public int obj_id_offset = 0;
        public string static_object_name = null;
        public string typeinfo_object_name = null;
        //public string vtbl_object_name = null;

        uint implflags = 0;

        internal Assembler.TypeToCompile? unboxed_typeinfo = null;
        internal Assembler.TypeToCompile? boxed_typeinfo = null;

        int _staticclasssize;
        int _classsize;

        public int StaticClassSize { get { return _staticclasssize; } }
        public int ClassSize { get { return _classsize; } }

        private Layout() { }

        /* This is the main function to call to create a layout */
        public static Layout GetLayout(Assembler.TypeToCompile ttc, Assembler ass) { return GetLayout(ttc, ass, true); }
        public static Layout GetLayout(Assembler.TypeToCompile ttc, Assembler ass, bool request_types)
        {
            if (layout_cache.ContainsKey(ttc))
                return layout_cache[ttc];

            if (Signature.ParamCompare(ttc.tsig, new Signature.Param(BaseType_Type.RefGenericParam), ass))
                return GetLayout(Metadata.GetTTC(new Signature.Param(BaseType_Type.Object), ttc, null, ass), ass);

            Layout l = new Layout();
            l._ass = ass;
            l.CreateLayout(ttc, ass, request_types);
            layout_cache.Add(ttc, l);
            return l;
        }

        public static int GetClassInstanceSize(Assembler.TypeToCompile ttc, Assembler ass)
        {
            if (class_size_cache.ContainsKey(ttc))
                return class_size_cache[ttc];
            if (layout_cache.ContainsKey(ttc))
                return layout_cache[ttc].ClassSize;

            Layout l = new Layout();
            l._ass = ass;
            l.LayoutInstanceFields(ttc, ass);
            class_size_cache.Add(ttc, l.ClassSize);
            return l.ClassSize;
        }

        internal static Interface GetInterfaceLayout(Assembler.TypeToCompile iface_ttc, Assembler ass)
        {
            if (interface_cache.ContainsKey(iface_ttc))
                return interface_cache[iface_ttc];
            Interface i = ImplementInterface(iface_ttc, null, new Assembler.TypeToCompile(), ass);
            return i;
        }

        private void CreateLayout(Assembler.TypeToCompile ttc, Assembler ass, bool request_types)
        {
            /* Create a class layout */
            _ttc = ttc;
            implflags = 0;

            static_object_name = Mangler2.MangleTypeStatic(ttc, ass);
            typeinfo_object_name = Mangler2.MangleTypeInfo(ttc, ass);
            //vtbl_object_name = Mangler2.MangleVTableName(ttc, ass);

            if (IsInstantiable(ttc.tsig.Type, ass, false))
            {
                boxed_typeinfo = ttc;
                if (request_types)
                    ass.Requestor.RequestTypeInfo(ttc);
            }

            if (ttc.tsig.Type.IsValueType(ass))
                implflags |= libsupcs.TysosType.IF_VTYPE;

            if (ttc.tsig.Type is Signature.BaseType)
            {
                Signature.BaseType bt = ttc.tsig.Type as Signature.BaseType;
                implflags |= ((uint)bt.Type << libsupcs.TysosType.IF_SIMPLE_ET_SHIFT);

                if (bt.Type == BaseType_Type.UninstantiatedGenericParam)
                {
                    if (bt.Ugp_idx >= 8)
                        throw new Exception("Generic type parameters are currently limited to 8");
                    implflags |= libsupcs.TysosType.IF_UGTP;
                    implflags |= ((uint)bt.Ugp_idx << libsupcs.TysosType.IF_UGTP_SHIFT);
                }
            }

            if ((ttc.tsig.Type is Signature.ComplexType) && ttc.type.IsGeneric)
                implflags |= libsupcs.TysosType.IF_GTD;

            if (ttc.tsig.Type is Signature.GenericType)
            {
                Signature.GenericType gt = ttc.tsig.Type as Signature.GenericType;
                foreach (Signature.BaseOrComplexType gp in gt.GenParams)
                {
                    if ((gp is Signature.BaseType) && (((Signature.BaseType)gp).Type == BaseType_Type.UninstantiatedGenericParam))
                        implflags |= libsupcs.TysosType.IF_GTD;
                }
                if((implflags & libsupcs.TysosType.IF_GTD) == 0)
                    implflags |= libsupcs.TysosType.IF_GT;
            }

            if (ttc.tsig.Type is Signature.BoxedType)
            {
                Signature.BaseOrComplexType unboxed_type = ((Signature.BoxedType)ttc.tsig.Type).Type;
                Assembler.TypeToCompile unboxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(unboxed_type, ass), type = ttc.type };
                unboxed_typeinfo = unboxed_ttc;
                if(request_types)
                    ass.Requestor.RequestTypeInfo(unboxed_ttc);
                implflags |= libsupcs.TysosType.IF_BOXED;
            }
            else if (ttc.tsig.Type is Signature.UnmanagedPointer)
            {
                Signature.BaseOrComplexType unboxed_type = ((Signature.UnmanagedPointer)ttc.tsig.Type).BaseType;
                Assembler.TypeToCompile unboxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(unboxed_type, ass), type = ttc.type };
                unboxed_typeinfo = unboxed_ttc;
                if (request_types)
                    ass.Requestor.RequestTypeInfo(unboxed_ttc);
                implflags |= libsupcs.TysosType.IF_PTR;
            }
            else if (ttc.tsig.Type is Signature.ManagedPointer)
            {
                Signature.BaseOrComplexType unboxed_type = ((Signature.ManagedPointer)ttc.tsig.Type).ElemType;
                Assembler.TypeToCompile unboxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(unboxed_type, ass), type = ttc.type };
                unboxed_typeinfo = unboxed_ttc;
                if (request_types)
                    ass.Requestor.RequestTypeInfo(unboxed_ttc);
                implflags = libsupcs.TysosType.IF_MPTR;
            }
            else if (ttc.tsig.Type is Signature.ZeroBasedArray)
            {
                Signature.BaseOrComplexType unboxed_type = ((Signature.ZeroBasedArray)ttc.tsig.Type).ElemType;
                Assembler.TypeToCompile unboxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(unboxed_type, ass), type = Metadata.GetTypeDef(((Signature.ZeroBasedArray)ttc.tsig.Type).ElemType, ass) };
                unboxed_typeinfo = unboxed_ttc;
                if (request_types)
                    ass.Requestor.RequestTypeInfo(unboxed_ttc);
                implflags |= libsupcs.TysosType.IF_ZBA;
            }
            else if (ttc.type.IsEnum(ass))
            {
                List<Metadata.FieldRow> ifs = ttc.type.GetAllInstanceFields(ass);
                if (ifs.Count != 1)
                    throw new Exception("Invalid number of instance fields for enum: " + ifs.Count.ToString());
                Signature.Param fsig = Signature.ResolveGenericParam(ifs[0].GetSignature().AsParam(ass), ttc.tsig.Type, null, ass);
                Assembler.TypeToCompile unboxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = fsig, type = Metadata.GetTypeDef(fsig.Type, ass) };
                unboxed_typeinfo = unboxed_ttc;
                if (request_types)
                    ass.Requestor.RequestTypeInfo(unboxed_ttc);
                
                implflags |= libsupcs.TysosType.IF_ENUM;
            }

            IdentifyImplementedClasses(ttc, ass);
            LayoutInstanceFields(ttc, ass);
            LayoutStaticFields(ttc, ass);
            LayoutVirtualMethods(ttc, ass, request_types);
            ImplementVirtualMethods(ttc, ass);
            LayoutInterfaces(ttc, ass);
        }

        internal List<Field> GetFlattenedInstanceFieldLayout(Assembler.TypeToCompile parent, Signature.BaseMethod containing_meth, Assembler ass)
        {
            List<Field> ret = new List<Field>();

            foreach (Field f in InstanceFields)
            {
                if (f.field.fsig.CliType(ass) == Assembler.CliType.vt)
                {
                    Signature.Param vt_fsig = Signature.ResolveGenericParam(f.field.fsig, parent.tsig.Type, containing_meth, ass);
                    Layout vt_l = Layout.GetLayout(Metadata.GetTTC(vt_fsig, parent, containing_meth, ass), ass, false);
                    List<Field> vt_fields = vt_l.GetFlattenedInstanceFieldLayout(parent, containing_meth, ass);

                    foreach (Field vt_f in vt_fields)
                    {
                        Field new_vt_f = new Field
                        {
                            field = vt_f.field,
                            is_static = vt_f.is_static,
                            mangled_name = vt_f.mangled_name,
                            name = vt_f.name,
                            offset = f.offset + vt_f.offset,
                            size = vt_f.size
                        };

                        ret.Add(new_vt_f);
                    }
                }
                else
                    ret.Add(f);
            }

            return ret;
        }

        internal static bool IsInstantiable(Token t, Assembler.TypeToCompile parent, Signature.BaseMethod containing_meth, Assembler ass, bool allow_void)
        {
            if (t is FTCToken)
            {
                FTCToken ftc = t as FTCToken;

                Signature.Param def = Signature.ResolveGenericParam(new Signature.Param(ftc.ftc.DefinedIn.tsig.Type, ass), parent.tsig.Type, containing_meth, ass);
                if (!IsInstantiable(def.Type, ass, true))
                    return false;

                Signature.Param fsig = Signature.ResolveGenericParam(new Signature.Param(ftc.ftc.fsig.Type, ass), parent.tsig.Type, containing_meth, ass);
                if (!IsInstantiable(fsig.Type, ass, true))
                    return false;

                return true;
            }
            else if (t is MTCToken)
            {
                MTCToken mtc = t as MTCToken;

                if (mtc.mtc.tsigp != null)
                {
                    Signature.Param tsig = Signature.ResolveGenericParam(mtc.mtc.tsigp, parent.tsig.Type, containing_meth, ass);
                    if (!IsInstantiable(tsig.Type, ass, true))
                        return false;
                }

                return true;
            }
            else if (t is TTCToken)
            {
                TTCToken ttc = t as TTCToken;

                Signature.Param tsig = Signature.ResolveGenericParam(ttc.ttc.tsig, parent.tsig.Type, containing_meth, ass);
                if (!IsInstantiable(tsig.Type, ass, true, allow_void))
                    return false;

                return true;
            }
            else if ((t.Value is Metadata.TypeDefRow) || (t.Value is Metadata.TypeRefRow) || (t.Value is Metadata.TypeSpecRow))
            {
                Assembler.TypeToCompile ttc = Metadata.GetTTC(t, parent, containing_meth, ass);
                return IsInstantiable(ttc.tsig.Type, ass, true, allow_void);
            }
            else if (t.Value is Metadata.MethodDefRow)
            {
                Assembler.MethodToCompile mtc = Metadata.GetMTC(new Metadata.TableIndex(t), parent, containing_meth, ass);
                return IsInstantiable(new MTCToken { mtc = mtc }, parent, containing_meth, ass, allow_void);
            }
            else if (t.Value is Metadata.FieldRow)
            {
                Assembler.FieldToCompile ftc = Metadata.GetFTC(new Metadata.TableIndex(t), parent, containing_meth, ass);
                return IsInstantiable(new FTCToken { ftc = ftc }, parent, containing_meth, ass, allow_void);
            }
            else if (t.Value is Metadata.MemberRefRow)
            {
                Metadata.MemberRefRow mrr = t.Value as Metadata.MemberRefRow;
                if (mrr.Signature[0] == 0x6)
                {
                    Assembler.FieldToCompile ftc = Metadata.GetFTC(new Metadata.TableIndex(t), parent, containing_meth, ass);
                    return IsInstantiable(new FTCToken { ftc = ftc }, parent, containing_meth, ass, allow_void);
                }
                else
                {
                    Assembler.MethodToCompile mtc = Metadata.GetMTC(new Metadata.TableIndex(t), parent, containing_meth, ass);
                    return IsInstantiable(new MTCToken { mtc = mtc }, parent, containing_meth, ass, allow_void);
                }
            }
            else
                return true;
        }

        internal static bool IsInstantiable(Signature.BaseOrComplexType type, Assembler ass, bool allow_vtypes)
        { return IsInstantiable(type, ass, allow_vtypes, false); }
        internal static bool IsInstantiable(Signature.BaseOrComplexType type, Assembler ass, bool allow_vtypes, bool allow_void)
        {
            // Return true if a concrete instantiation of the class can be produced, else false

            // Note that managed pointers and unmanaged pointers are value types, rather than
            //  instantiations of classes, therefore they return false if allow_vtypes is false

            if (type is Signature.BaseType)
            {
                BaseType_Type bt = ((Signature.BaseType)type).Type;
                switch (bt)
                {
                    case BaseType_Type.Array:
                    case BaseType_Type.Boxed:
                    case BaseType_Type.Byref:
                    case BaseType_Type.Class:
                    case BaseType_Type.End:
                    case BaseType_Type.FnPtr:
                    case BaseType_Type.GenericInst:
                    case BaseType_Type.Ptr:
                    case BaseType_Type.SzArray:
                    case BaseType_Type.UninstantiatedGenericParam:
                    case BaseType_Type.ValueType:
                    case BaseType_Type.Var:
                        return false;

                    case BaseType_Type.Void:
                        return allow_void;

                    case BaseType_Type.Boolean:
                    case BaseType_Type.Byte:
                    case BaseType_Type.Char:
                    case BaseType_Type.I:
                    case BaseType_Type.I1:
                    case BaseType_Type.I2:
                    case BaseType_Type.I4:
                    case BaseType_Type.I8:
                    case BaseType_Type.R4:
                    case BaseType_Type.R8:
                    case BaseType_Type.U:
                    case BaseType_Type.U1:
                    case BaseType_Type.U2:
                    case BaseType_Type.U4:
                    case BaseType_Type.U8:
                        return allow_vtypes;
                }
                return true;
            }
            else if (type is Signature.ComplexType)
            {
                Signature.ComplexType ct = type as Signature.ComplexType;
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(ct, ass);
                if (tdr.IsGeneric)
                    return false;

                return true;
            }
            else if (type is Signature.BaseArray)
                return IsInstantiable(((Signature.BaseArray)type).ElemType, ass, true);
            else if (type is Signature.BoxedType)
                return IsInstantiable(((Signature.BoxedType)type).Type, ass, true);
            else if (type is Signature.GenericType)
            {
                Signature.GenericType gt = type as Signature.GenericType;

                if (!(gt.GenType is Signature.ComplexType))
                    return false;

                foreach (Signature.BaseOrComplexType gp in gt.GenParams)
                    if (!IsInstantiable(gp, ass, true))
                        return false;

                return true;
            }
            else if (type is Signature.UnmanagedPointer)
            {
                if (!allow_vtypes)
                    return false;
                // allow void *
                return IsInstantiable(((Signature.UnmanagedPointer)type).BaseType, ass, true, true);
            }
            else if (type is Signature.ManagedPointer)
            {
                if (!allow_vtypes)
                    return false;
                if (!((Signature.ManagedPointer)type).ElemType.IsValueType(ass))
                    return false;
                return IsInstantiable(((Signature.ManagedPointer)type).ElemType, ass, true);
            }

            return false;
        }

        private void IdentifyImplementedClasses(Assembler.TypeToCompile ttc, Assembler ass)
        {
            ClassesImplemented = new List<Assembler.TypeToCompile>();

            /* Unboxed value types do not implement other classes */
            if(ttc.type.IsValueType(ass) && !(ttc.tsig.Type is Signature.BoxedType))
                ClassesImplemented.Add(ttc);
            else
                _identify_implemented_classes(ttc, ttc, ass);
        }

        private void _identify_implemented_classes(Assembler.TypeToCompile ttc, Assembler.TypeToCompile parent, Assembler ass)
        {
            ClassesImplemented.Add(ttc);

            if (ttc.type.IsInterface)
                return;

            if (ttc.type.Extends.Value != null)
            {
                Assembler.TypeToCompile next_ttc = Metadata.GetTTC(ttc.type.Extends, parent, ass);

                /* if this is a value type, and unboxed, then box it */
                if (next_ttc.type.IsValueType(ass) && !(next_ttc.tsig.Type is Signature.BoxedType))
                    next_ttc.tsig = new Signature.Param(new Signature.BoxedType(next_ttc.tsig.Type), ass);

                if (!Extends.HasValue)
                    Extends = next_ttc;

                _identify_implemented_classes(next_ttc, parent, ass);
            }
        }

        private static Interface ImplementInterface(Assembler.TypeToCompile iface_ttc, Layout find_implementations, Assembler.TypeToCompile parent, Assembler ass)
        {
            Interface iface = new Interface { iface = iface_ttc };
            iface.methods = new List<Method>();
            iface.typeinfo_name = Mangler2.MangleTypeInfo(iface_ttc, ass);
            int iface_offset = 0;

            foreach (Metadata.MethodDefRow mdr in iface_ttc.type.Methods)
            {
                Signature.BaseMethod msig = Signature.ResolveGenericMember(mdr.GetSignature(), iface_ttc.tsig.Type, null, ass);
                msig.Method.meth = mdr;

                Method m = new Method
                {
                    meth = new Assembler.MethodToCompile
                    {
                        meth = mdr,
                        msig = msig,
                        type = parent.type,
                        tsigp = parent.tsig,
                        _ass = ass
                    },
                    offset = iface_offset
                };

                if (find_implementations != null)
                {
                    m.implementation_mtc = GetImplementation(m.meth, parent, parent, ass, find_implementations);
                    if (m.implementation_mtc.HasValue)
                        m.implementation = Mangler2.MangleMethod(m.implementation_mtc.Value, ass);
                }

                iface_offset += ass.GetSizeOfIntPtr();

                iface.methods.Add(m);
            }

            return iface;
        }

        private List<Assembler.TypeToCompile> GetTypeInterfaces(Assembler.TypeToCompile parent, Assembler ass)
        {
            /* A type implements those intefaces explicitly defined on it, as well as
             * those defined in its base classes */

            List<Assembler.TypeToCompile> ret = new List<Assembler.TypeToCompile>();

            Assembler.TypeToCompile cur_ttc = parent;
            bool cont = true;

            do
            {
                /* Get all the interfaces on the current type */

                foreach (Metadata.InterfaceImplRow iir in cur_ttc.type._InterfaceImpls)
                {
                    Assembler.TypeToCompile iface_ttc = Metadata.GetTTC(iir.Interface, cur_ttc, ass);

                    if (!ret.Contains(iface_ttc))
                        ret.Add(iface_ttc);
                }

                if (cur_ttc.type.Extends.Value != null)
                    cur_ttc = Metadata.GetTTC(cur_ttc.type.Extends, cur_ttc, ass);
                else
                    cont = false;
            } while (cont);

            return ret;
        }

        private void LayoutInterfaces(Assembler.TypeToCompile ttc, Assembler ass)
        {

            Interfaces = new List<Interface>();

            /* Unboxed value types do not define interfaces */
            if (ttc.type.IsValueType(ass) && !(ttc.tsig.Type is Signature.BoxedType))
                return;

            List<Assembler.TypeToCompile> iface_list = GetTypeInterfaces(ttc, ass);

            foreach (Assembler.TypeToCompile iface_ttc in iface_list)
            {
                Interface iface = ImplementInterface(iface_ttc, this, ttc, ass);
                Interfaces.Add(iface);
            }
        }

        private void ImplementVirtualMethods(Assembler.TypeToCompile ttc, Assembler ass)
        {
            foreach (Method m in VirtualMethods)
            {
                m.implementation_mtc = GetImplementation(m.meth, ttc, ttc, ass, this);
                if (m.implementation_mtc.HasValue)
                    m.implementation = Mangler2.MangleMethod(m.implementation_mtc.Value, ass);
            }
        }

        private static Assembler.MethodToCompile? GetImplementation(Assembler.MethodToCompile method, Assembler.TypeToCompile type_to_search, Assembler.TypeToCompile parent, Assembler ass, Layout l)
        {
            /* Get the implementation for a virtual method
             * 
             * First see if there is an entry in the MethodImpl table for the method in the current type, if not search its Method list, else
             * repeat the process for base classes back to System.Object
             */

            foreach (Metadata.MethodImplRow mir in type_to_search.type.MethodImpls)
            {
                Assembler.MethodToCompile mir_mtc = Metadata.GetMTC(mir.MethodDeclaration, parent, null, ass);

                if (mir_mtc.meth.Name == method.meth.Name)
                {
                    if (Signature.BaseMethodSigCompare(mir_mtc.msig, method.msig, ass))
                    {
                        Assembler.MethodToCompile mir_body = Metadata.GetMTC(mir.MethodBody, parent, null, ass);

                        /* Ensure that the implementing method is defined on the type or one of its base types */
                        bool found = false;
                        foreach (Assembler.TypeToCompile base_class in l.ClassesImplemented)
                        {
                            if (base_class.type == mir_body.type)
                            {
                                found = true;
                                mir_body.tsigp = base_class.tsig;
                                break;
                            }
                        }

                        if(!found)
                            throw new Exception("MethodBody is not defined in this class (breaks CIL II:22.27 point 6)");

                        /* Return null if the implementation is marked abstract */
                        if (mir_body.meth.IsAbstract && !mir_body.meth.IsInternalCall)
                            return null;

                        /* If we specifically don't implement this, then ignore and continue */
                        if (mir_body.meth.IgnoreAttribute)
                            continue;

                        return mir_body;
                    }
                }
            }

            foreach (Metadata.MethodDefRow mdr in type_to_search.type.Methods)
            {
                if (mdr.Name == method.meth.Name)
                {
                    Signature.BaseMethod test_msig = Signature.ResolveGenericMember(mdr.GetSignature(), parent.tsig.Type, null, ass);
                    test_msig.Method.meth = mdr;
                    if (Signature.BaseMethodSigCompare(test_msig, method.msig, ass))
                    {
                        /* Return null if the implementation is marked abstract */
                        if (mdr.IsAbstract && !mdr.IsInternalCall)
                        {
                            if (type_to_search.type.IsDelegate(ass))
                            {
                                if(method.meth.Name == "Invoke")        // unless it is Delegate.Invoke, which we provide
                                    return new Assembler.MethodToCompile(ass, mdr, test_msig, type_to_search.type, type_to_search.tsig, null);
                            }
                            return null;
                        }

                        if (mdr.IgnoreAttribute)
                            continue;

                        return new Assembler.MethodToCompile(ass, mdr, test_msig, type_to_search.type, type_to_search.tsig, null);
                    }
                }
            }

            if (type_to_search.type.Extends.Value == null)
                return null;

            return GetImplementation(method, Metadata.GetTTC(type_to_search.type.Extends, parent, ass), parent, ass, l);
        }

        private void LayoutVirtualMethods(Assembler.TypeToCompile ttc, Assembler ass, bool request_types)
        {
            VirtualMethods = new List<Method>();

            int vmeth_offset = 3 * ass.GetSizeOfIntPtr();   // first three members are typeinfo_pointer, itablemap_pointer and extends_vtbl

            /* Unboxed virtual types do not have virtual methods */
            if (ttc.type.IsValueType(ass) && !(ttc.tsig.Type is Signature.BoxedType))
            {
                if ((ttc.tsig.Type is Signature.BaseType) || (ttc.tsig.Type is Signature.ComplexType))
                {
                    Assembler.TypeToCompile boxed_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(new Signature.BoxedType(ttc.tsig.Type), ass), type = ttc.type };

                    if (IsInstantiable(boxed_ttc.tsig.Type, ass, false))
                    {
                        boxed_typeinfo = boxed_ttc;
                        if(request_types)
                            ass.Requestor.RequestTypeInfo(boxed_ttc);
                    }
                }
                else
                    boxed_typeinfo = null;
                return;
            }

            List<Metadata.MethodDefRow> mdrs = ttc.type.GetAllVirtualMethods(ass);
            foreach (Metadata.MethodDefRow mdr in mdrs)
            {
                Signature.BaseMethod msig = Signature.ResolveGenericMember(mdr.GetSignature(), ttc.tsig.Type, null, ass);
                msig.Method.meth = mdr;

                Method m = new Method
                {
                    meth = new Assembler.MethodToCompile
                    {
                        _ass = ass,
                        meth = mdr,
                        msig = msig,
                        tsigp = ttc.tsig,
                        type = ttc.type
                    },
                    offset = vmeth_offset
                };
                vmeth_offset += ass.GetSizeOfIntPtr();

                VirtualMethods.Add(m);
            }
        }

        private void LayoutStaticFields(Assembler.TypeToCompile ttc, Assembler ass)
        {
            StaticFields = new List<Field>();
            _staticclasssize = 0;

            if (ttc.tsig.Type is Signature.BoxedType)
                return;     // boxed value types do not have static fields - these are implemented on the unboxed representation

            int static_field_count = 0;
            foreach (Metadata.FieldRow fr in ttc.type.Fields)
            {
                if (fr.IsStatic)
                    static_field_count++;
            }

            // add a flags field
            StaticFields.Add(new Field
            {
                field = new Assembler.FieldToCompile
                    {
                        _ass = ass,
                        definedin_tsig = ttc.tsig,
                        definedin_type = ttc.type,
                        field = new Metadata.FieldRow
                        {
                            ass = ass,
                            fsig = new Signature.Field(new Signature.Param(BaseType_Type.I4)),
                            Name = "__flags",
                            RuntimeInternal = true
                        },
                        fsig = new Signature.Param(BaseType_Type.I4),
                        memberof_tsig = ttc.tsig,
                        memberof_type = ttc.type
                    },
                is_static = true,
                name = "__flags",
                offset = 0,
                size = 4
            });
            _staticclasssize += 4;

            foreach (Metadata.FieldRow fr in ttc.type.Fields)
            {
                if (fr.IsStatic)
                {
                    Signature.Param fsig = Signature.ResolveGenericParam(fr.GetSignature().AsParam(ass), ttc.tsig.Type, null, ass);


                    Assembler.FieldToCompile ftc = new Assembler.FieldToCompile
                    {
                        _ass = ass,
                        definedin_tsig = ttc.tsig,
                        definedin_type = ttc.type,
                        field = fr,
                        fsig = fsig,
                        memberof_tsig = ttc.tsig,
                        memberof_type = ttc.type
                    };

                    Field f = new Field
                    {
                        field = ftc,
                        is_static = false,
                        size = ass.GetSizeOf(fsig)
                    };

                    /* Align up to a multiple of the field size */
                    _staticclasssize = util.align(_staticclasssize, f.size);
                    f.offset = _staticclasssize;

                    f.name = Signature.GetString(f.field, ass);
                    f.mangled_name = Mangler2.MangleFieldInfoSymbol(f.field, ass);

                    _staticclasssize += f.size;
                    StaticFields.Add(f);
                }
            }
        }

        private void LayoutInstanceFields(Assembler.TypeToCompile ttc, Assembler ass)
        {
            InstanceFields = new List<Field>();
            _classsize = 0;

            /* If this is a reference type we need to add a vtbl_pointer as the first field */
            if (!ttc.type.IsValueType(ass) || (ttc.tsig.Type is Signature.BoxedType))
            {
                Field f = new Field
                {
                    field = new Assembler.FieldToCompile {
                        definedin_tsig = ttc.tsig,
                        definedin_type = ttc.type,
                        field = new Metadata.FieldRow { Flags = 0x600, fsig = new Signature.Field(new Signature.Param(BaseType_Type.I)), m = ttc.type.m, Name = "__vtbl", owning_type = ttc.type, RuntimeInternal = true },
                        fsig = new Signature.Param(BaseType_Type.I),
                        memberof_tsig = ttc.tsig,
                        memberof_type = ttc.type
                    },
                    is_static = false,
                    offset = _classsize,
                    size = ass.GetSizeOfIntPtr()
                };
                f.name = Signature.GetString(f.field, ass);
                f.mangled_name = Mangler2.MangleFieldInfoSymbol(f.field, ass);

                vtbl_offset = f.offset;
                has_vtbl = true;

                _classsize += f.size;

                InstanceFields.Add(f);
            }

            /* Boxed types only have one member, namely the actual value of the type which is boxed, in addition to whatever is inherited from Object */
            if (ttc.tsig.Type is Signature.BoxedType)
            {
                Metadata.TypeDefRow sys_obj = Metadata.TypeDefRow.GetSystemObject(ass);

                foreach (Metadata.FieldRow fr in sys_obj.GetAllInstanceFields(ass))
                {
                    {
                        Signature.Param fsig = Signature.ResolveGenericParam(fr.GetSignature().AsParam(ass), ttc.tsig.Type, null, ass);

                        Assembler.FieldToCompile ftc = new Assembler.FieldToCompile
                        {
                            _ass = ass,
                            definedin_tsig = ttc.tsig,
                            definedin_type = ttc.type,
                            field = fr,
                            fsig = fsig,
                            memberof_tsig = ttc.tsig,
                            memberof_type = ttc.type
                        };

                        Field f = new Field();
                        f.field = ftc;
                        f.is_static = false;
                        f.offset = _classsize;
                        f.size = ass.GetSizeOf(fsig);
                        f.name = Signature.GetString(f.field, ass);
                        f.mangled_name = Mangler2.MangleFieldInfoSymbol(f.field, ass);

                        InstanceFields.Add(f);

                        if ((fr.Name == "__object_id") && (fr.Flags == 0x601))
                        {
                            has_obj_id = true;
                            obj_id_offset = f.offset;
                        }

                        _classsize += f.size;
                    }
                }

                Signature.Param p = new Signature.Param(((Signature.BoxedType)ttc.tsig.Type).Type, ass);

                Field val_f = new Field
                {
                    field = new Assembler.FieldToCompile
                    {
                        definedin_type = ttc.type,
                        definedin_tsig = ttc.tsig,
                        field = new Metadata.FieldRow { Flags = 0x606, fsig = new Signature.Field(p), m = ttc.type.m, Name = "m_value", owning_type = ttc.type },
                        fsig = p,
                        memberof_type = ttc.type,
                        memberof_tsig = ttc.tsig
                    },
                    is_static = false,
                    offset = _classsize,
                    size = ass.GetSizeOf(p)
                };
                val_f.name = Signature.GetString(val_f.field, ass);
                val_f.mangled_name = Mangler2.MangleFieldInfoSymbol(val_f.field, ass);

                _classsize += val_f.size;

                /* Align up to a multiple of the largest field size */
                _classsize = util.align(_classsize, util.max(ass.GetSizeOfPointer(), val_f.size));

                InstanceFields.Add(val_f);
                return;
            }

            /* Now add all the other instance fields */
            List<Metadata.FieldRow> frs = ttc.type.GetAllInstanceFields(ass);
            foreach (Metadata.FieldRow fr in frs)
            {
                Signature.Param fsig = Signature.ResolveGenericParam(fr.GetSignature().AsParam(ass), ttc.tsig.Type, null, ass);

                Assembler.FieldToCompile ftc = new Assembler.FieldToCompile
                {
                    _ass = ass,
                    definedin_tsig = ttc.tsig,
                    definedin_type = ttc.type,
                    field = fr,
                    fsig = fsig,
                    memberof_tsig = ttc.tsig,
                    memberof_type = ttc.type
                };

                Field f = new Field();
                f.field = ftc;
                f.is_static = false;
                f.size = ass.GetSizeOf(fsig);

                /* Align up to a multiple of the field size */
                _classsize = util.align(_classsize, f.size);
                f.offset = _classsize;
                f.name = Signature.GetString(f.field, ass);
                f.mangled_name = Mangler2.MangleFieldInfoSymbol(f.field, ass);

                InstanceFields.Add(f);

                if ((fr.Name == "__object_id") && (fr.Flags == 0x601))
                {
                    has_obj_id = true;
                    obj_id_offset = f.offset;
                }

                _classsize += f.size;
            }

            /* Add the fields to the InstanceFieldOffsets dictionary */
            InstanceFieldOffsets = new Dictionary<string,int>();
            foreach (Field f in InstanceFields)
            {
                if (!InstanceFieldOffsets.ContainsKey(f.name))
                    InstanceFieldOffsets.Add(f.name, f.offset);
            }
        }

        public Field GetFirstInstanceField(string name)
        {
            foreach (Field f in InstanceFields)
            {
                if (f.field.field.Name == name)
                    return f;
            }
            return null;
        }

        public Field GetField(string name, bool _static)
        {
            List<Field> test = InstanceFields;
            if (_static)
                test = StaticFields;

            foreach (Field f in test)
            {
                if (name == f.mangled_name)
                    return f;
                if (name == f.name)
                    return f;
                if (name == f.field.field.Name)
                    return f;
            }

            return null;
        }

        internal Method GetVirtualMethod(Assembler.MethodToCompile meth)
        {
            foreach (Method test_m in VirtualMethods)
            {
                if (Signature.MethodCompare(meth, test_m.meth, _ass, false))
                    return test_m;
            }

            return null;
        }

        public Method GetVirtualMethod(string name)
        {
            foreach (Method test_m in VirtualMethods)
            {
                if (test_m.meth.meth.Name == name)
                    return test_m;
            }

            return null;
        }

        public class Field
        {
            public Assembler.FieldToCompile field;
            public bool is_static;
            public int offset;
            public int size;

            public string mangled_name;
            public string name;
        }

        public class Method
        {
            public Assembler.MethodToCompile meth;
            public int offset;
            public string implementation = libsupcs.TysosMethod.PureVirtualName;
            public Assembler.MethodToCompile? implementation_mtc;
        }

        public class Interface
        {
            public Assembler.TypeToCompile iface;
            public List<Method> methods;
            public string typeinfo_name;

            internal Method GetVirtualMethod(Assembler.MethodToCompile meth, Assembler ass)
            {
                foreach (Method test_m in methods)
                {
                    if (Signature.MethodCompare(meth, test_m.meth, ass, false))
                        return test_m;
                }

                return null;
            }
        }

        internal static bool IsInstantiable(Assembler.MethodToCompile mtc, Assembler assembler)
        {
            /* Determine if a method is instantiable (i.e. does not contain any unresolved generic
             * parameters
             */

            if (mtc.meth.IsInternalCall)
                return true;

            if (mtc.meth.Body == null || mtc.meth.Body.Body == null || mtc.meth.Body.Body.Length == 0)
                return false;
            if (mtc.tsig is Signature.BoxedType && mtc.meth.IsStatic)
                return false;

            if (mtc.tsig.IsInstantiable == false)
                return false;
            return mtc.msig.IsInstantiable;

            /*try
            {
                if(assembler.AssembleMethod(mtc, true) != null)
                    return true;
                return false;
            }
            catch (Exception)
            {
                return false;
            }*/
        }
    }

    partial class Assembler
    {
        internal enum VTTIFields { vtbl_extendsvtblptr, vtbl_ifaceptr, vtbl_typeinfoptr, ti_vtblptr, ti_objid };
        bool vtti_fields_calculated = false;
        int vtti_vtbl_extendsvtblptr_offset, vtti_vtbl_ifaceptr_offset, vtti_vtbl_typeinfoptr_offset;
        int vtti_ti_vtblptr, vtti_ti_objid;

        internal int GetVTTIFieldOffset(VTTIFields field)
        {
            if (!vtti_fields_calculated)
            {
                // Generate the layout of System.Object to interrogate about its fields

                Assembler.TypeToCompile obj_ttc = Metadata.GetTTC("mscorlib", "System", "Object", this);
                Layout l = Layout.GetTypeInfoLayout(obj_ttc, this, false, false);

                vtti_ti_vtblptr = l.vtbl_offset;
                vtti_ti_objid = l.obj_id_offset;
                vtti_vtbl_typeinfoptr_offset = 0;
                vtti_vtbl_ifaceptr_offset = GetSizeOfPointer();
                vtti_vtbl_extendsvtblptr_offset = GetSizeOfPointer() * 2;

                vtti_fields_calculated = true;
            }

            switch (field)
            {
                case VTTIFields.ti_objid:
                    return vtti_ti_objid;
                case VTTIFields.ti_vtblptr:
                    return vtti_ti_vtblptr;
                case VTTIFields.vtbl_extendsvtblptr:
                    return vtti_vtbl_extendsvtblptr_offset;
                case VTTIFields.vtbl_ifaceptr:
                    return vtti_vtbl_ifaceptr_offset;
                case VTTIFields.vtbl_typeinfoptr:
                    return vtti_vtbl_typeinfoptr_offset;
            }

            throw new NotSupportedException();
        }
    }
}
