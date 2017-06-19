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

/* This defines the TysosType which is a subtype of System.Type
 * 
 * All TypeInfo structures produced by tysila2 follow this layout
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace libsupcs
{
    [VTableAlias("__tysos_type_vt")]
    [SpecialType]
    public class TysosType : System.Type
    {
        internal TysosType Extends;
        internal string TypeName;
        internal string TypeNamespace;
        [NullTerminatedListOf(typeof(TysosType))]
        internal IntPtr Interfaces;
        [NullTerminatedListOf(typeof(TysosField))]
        internal IntPtr Fields;
        [NullTerminatedListOf(typeof(TysosMethod))]
        internal IntPtr Methods;
        internal IntPtr Events;
        internal IntPtr Properties;
        [NullTerminatedListOf(typeof(TysosType))]
        internal IntPtr NestedTypes;
        internal System.Reflection.Assembly _Assembly;
        internal System.Reflection.Module _Module;
        internal IntPtr Signature;
        internal IntPtr Sig_references;
        internal IntPtr UnboxedType;
        public IntPtr VTable;
        internal Int32 Flags;
        internal UInt32 ImplFlags;
        protected Int32 ClassSize;
        protected Int32 StaticClassSize;
        internal IntPtr VTableLength;

        /* The following five are mutually exclusive for a single type info
         * 
         * To specify an array of enums, for example, there would be a type info for
         * the array which points to the type info for the enum */
        public const uint IF_PTR = 0x1;
        public const uint IF_MPTR = 0x2;
        public const uint IF_ZBA = 0x3;
        public const uint IF_BOXED = 0x4;
        public const uint IF_ENUM = 0x5;

        /* A mask to get the above entries out of the ImplFlags */
        public const uint IF_TYPE_MASK = 0x7;

        /* Is the type created dynamically at runtime? (has implications for CastClassEx) */
        public const uint IF_DYNAMIC = 0x8;

        /* Is this a generic type definition? (i.e. requires instantiating) */
        public const uint IF_GTD = 0x10;

        /* Is this a concrete instantiation of a generic type? */
        public const uint IF_GT = 0x20;

        /* Is this a value type? */
        public const uint IF_VTYPE = 0x40;

        /* Is this an uninstantiated generic type parameter? */
        public const uint IF_UGTP = 0x80;

        /* Is this an uninstantiated generic method parameter? */
        public const uint IF_UGMP = 0x100;

        /* Mask for the uninstantiated generic type parameter number */
        public const uint IF_UGTP_MASK = 0x000f0000;
        public const int IF_UGTP_SHIFT = 16;

        /* Mask for the uninstantiated generic method parameter number */
        public const uint IF_UGMP_MASK = 0x00f00000;
        public const int IF_UGMP_SHIFT = 20;

        /* Is this a simple built-in type?  If so, this mask contains the simple type element type */
        public const uint IF_SIMPLE_ET = 0xff000000;
        public const int IF_SIMPLE_ET_SHIFT = 24;

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosField ReinterpretAsFieldInfo(IntPtr addr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosField ReinterpretAsFieldInfo(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosMethod ReinterpretAsMethodInfo(IntPtr addr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosType ReinterpretAsType(IntPtr addr);

        [Bits32Only]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosType ReinterpretAsType(uint addr);

        [Bits64Only]
        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosType ReinterpretAsType(ulong addr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosType ReinterpretAsType(object obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static unsafe extern TysosType ReinterpretAsType(void* obj);

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static unsafe extern TysosMethod ReinterpretAsMethodInfo(void* obj);

        public virtual int GetClassSize() { return ClassSize; }
        
        public override System.Reflection.Assembly Assembly
        {
            get { return _Assembly; }
        }

        public override string AssemblyQualifiedName
        {
            get
            {
                return FullName + ", " + Assembly.FullName;
            }
        }

        public override Type BaseType
        {
            get { return Extends; }
        }

        public override string FullName
        {
            get { return TypeNamespace + "." + TypeName; }
        }

        public override Guid GUID
        {
            get { throw new NotImplementedException(); }
        }

        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl()
        {
            return (System.Reflection.TypeAttributes)Flags;
        }

        protected override System.Reflection.ConstructorInfo GetConstructorImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            TysosMethod meth = GetMethodImpl(".ctor", bindingAttr, binder, callConvention, types, modifiers) as TysosMethod;
            if (meth == null)
                return null;
            return new ConstructorInfo(meth, this);
        }

        public override System.Reflection.ConstructorInfo[] GetConstructors(System.Reflection.BindingFlags bindingAttr)
        {
            System.Reflection.MethodInfo[] mis = GetMethods(bindingAttr);
            int count = 0;
            foreach (System.Reflection.MethodInfo mi in mis)
            {
                if (mi.IsConstructor)
                {
                    if (((bindingAttr & System.Reflection.BindingFlags.Static) == System.Reflection.BindingFlags.Static) && mi.IsStatic)
                        count++;
                    if (((bindingAttr & System.Reflection.BindingFlags.Instance) == System.Reflection.BindingFlags.Instance) && !mi.IsStatic)
                        count++;
                }
            }
            System.Reflection.ConstructorInfo[] ret = new System.Reflection.ConstructorInfo[count];
            int i = 0;
            foreach (System.Reflection.MethodInfo mi in mis)
            {
                if (mi.IsConstructor)
                {
                    bool add = false;
                    if (((bindingAttr & System.Reflection.BindingFlags.Static) == System.Reflection.BindingFlags.Static) && mi.IsStatic)
                        add = true;
                    if (((bindingAttr & System.Reflection.BindingFlags.Instance) == System.Reflection.BindingFlags.Instance) && !mi.IsStatic)
                        add = true;

                    if(add)
                        ret[i++] = new ConstructorInfo(mi as TysosMethod, this);
                }
            }
            return ret;
        }

        public override Type GetElementType()
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.EventInfo GetEvent(string name, System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.EventInfo[] GetEvents(System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.FieldInfo GetField(string name, System.Reflection.BindingFlags bindingAttr)
        {
            System.Reflection.FieldInfo[] fields = GetFields(bindingAttr);
            foreach (System.Reflection.FieldInfo f in fields)
            {
                if (f.Name == name)
                    return f;
            }
            return null;
        }

        public override System.Reflection.FieldInfo[] GetFields(System.Reflection.BindingFlags bindingAttr)
        {
            if (IsBoxed || IsManagedPointer || IsUnmanagedPointer)
                return GetUnboxedType().GetFields(bindingAttr);

            unsafe
            {
                IntPtr* cur_field = (IntPtr *)Fields;

                if (cur_field != null)
                {
                    int count = 0;

                    while (*cur_field != new IntPtr(0))
                    {
                        System.Reflection.FieldInfo fi = ReinterpretAsFieldInfo(*cur_field);
                        bool add = false;
                        if (((bindingAttr & System.Reflection.BindingFlags.Instance) == System.Reflection.BindingFlags.Instance) && !fi.IsStatic)
                            add = true;
                        if (((bindingAttr & System.Reflection.BindingFlags.Static) == System.Reflection.BindingFlags.Static) && fi.IsStatic)
                            add = true;

                        if (add)
                            count++;
                        cur_field++;
                    }

                    System.Reflection.FieldInfo[] ret = new System.Reflection.FieldInfo[count];
                    int i = 0;
                    cur_field = (IntPtr*)Fields;
                    while (*cur_field != new IntPtr(0))
                    {
                        System.Reflection.FieldInfo fi = ReinterpretAsFieldInfo(*cur_field);
                        bool add = false;
                        if (((bindingAttr & System.Reflection.BindingFlags.Instance) == System.Reflection.BindingFlags.Instance) && !fi.IsStatic)
                            add = true;
                        if (((bindingAttr & System.Reflection.BindingFlags.Static) == System.Reflection.BindingFlags.Static) && fi.IsStatic)
                            add = true;

                        if (add)
                            ret[i++] = fi;
                        cur_field++;
                    }

                    return ret;
                }
            }

            return new System.Reflection.FieldInfo[] { };
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            unsafe
            {
                IntPtr* cur_iface = (IntPtr*)Interfaces;
                int count = 0;

                if (cur_iface != null)
                {
                    while (*cur_iface != new IntPtr(0))
                    {
                        count++;
                        cur_iface += 2;
                    }

                    Type[] ret = new Type[count];
                    cur_iface = (IntPtr*)Interfaces;
                    int i = 0;

                    while (*cur_iface != new IntPtr(0))
                    {
                        Type iface = ReinterpretAsType(*cur_iface);
                        ret[i++] = iface;
                        cur_iface += 2;
                    }

                    return ret;
                }
            }

            return new Type[] { };
        }

        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            if (Methods == (IntPtr)0)
                return null;

            unsafe
            {
                IntPtr *cur_meth = (IntPtr *)Methods;
                while(*cur_meth != (IntPtr)0)
                {
                    TysosMethod mi = ReinterpretAsMethodInfo(*cur_meth);
                    if(MatchBindingFlags(mi, bindingAttr))
                    {
                        if (mi.Name == name)
                        {
                            // Match _Params
                            if (mi._Params == (IntPtr)0)
                            {
                                if (types == null)
                                    return mi;
                                if (types.Length == 0)
                                    return mi;
                            }
                            else if (types != null)
                            {
                                bool param_match = true;
                                int cur_p = 0;

                                IntPtr* cur_param = (IntPtr*)mi._Params;
                                while (*cur_param != (IntPtr)0)
                                {
                                    TysosType p = ReinterpretAsType(*cur_param);
                                    if (cur_p >= types.Length)
                                        param_match = false;
                                    else if (p != types[cur_p])
                                        param_match = false;

                                    cur_p++;
                                    cur_param++;
                                }

                                if (cur_p != types.Length)
                                    param_match = false;

                                if (param_match)
                                    return mi;
                            }
                        }
                    }
                    cur_meth++;
                }
            }

            return null;
        }

        private bool MatchBindingFlags(System.Reflection.MethodInfo mi, System.Reflection.BindingFlags bindingAttr)
        {
            bool match = false;
            if (((bindingAttr & System.Reflection.BindingFlags.Static) == System.Reflection.BindingFlags.Static) && (mi.IsStatic))
                match = true;
            if (((bindingAttr & System.Reflection.BindingFlags.Instance) == System.Reflection.BindingFlags.Instance) && (!mi.IsStatic))
                match = true;

            return match;
        }

        public override System.Reflection.MethodInfo[] GetMethods(System.Reflection.BindingFlags bindingAttr)
        {
            System.Reflection.MethodInfo[] ret;

            unsafe
            {
                IntPtr* cur_meth = (IntPtr*)Methods;
                int count = 0;
                if (cur_meth != null)
                {
                    while (*cur_meth != new IntPtr(0))
                    {
                        System.Reflection.MethodInfo mi = ReinterpretAsMethodInfo(*cur_meth);

                        if (MatchBindingFlags(mi, bindingAttr))
                            count++;

                        cur_meth++;
                    }

                    cur_meth = (IntPtr*)Methods;
                    int i = 0;
                    ret = new System.Reflection.MethodInfo[count];
                    while (*cur_meth != new IntPtr(0))
                    {
                        System.Reflection.MethodInfo mi = ReinterpretAsMethodInfo(*cur_meth);

                        if (MatchBindingFlags(mi, bindingAttr))
                            ret[i++] = mi;

                        cur_meth++;
                    }
                    return ret;
                }
            }

            return new System.Reflection.MethodInfo[] { };
        }

        public override Type GetNestedType(string name, System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.PropertyInfo[] GetProperties(System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override System.Reflection.PropertyInfo GetPropertyImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, Type returnType, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl()
        {
            throw new NotImplementedException();
        }

        public override object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            if ((invokeAttr & System.Reflection.BindingFlags.CreateInstance) != 0)
            {
                // Invoke constructor

                // build an array of the types to search for
                Type[] param_types = null;
                if (args != null)
                {
                    param_types = new Type[args.Length];
                    for (int i = 0; i < args.Length; i++)
                        param_types[i] = args[i].GetType();
                }

                // Find the constructor
                System.Reflection.ConstructorInfo ci = GetConstructor(invokeAttr, binder, param_types, modifiers);
                if (ci == null)
                    throw new MissingMethodException("Could not find a matching constructor");

                // Execute it
                return ci.Invoke(args);
            }
            else
            {
                throw new NotImplementedException("InvokeMember currently only defined for constructors");
            }
        }

        protected override bool IsArrayImpl()
        {
            return IsZeroBasedArray;
        }

        protected override bool IsByRefImpl()
        {
            return IsManagedPointer;
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl()
        {
            return IsUnmanagedPointer;
        }

        protected override bool IsPrimitiveImpl()
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.Module Module
        {
            get { return _Module; }
        }

        public override string Namespace
        {
            get { return TypeNamespace; }
        }

        public override Type UnderlyingSystemType
        {
            get { return this; }
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override string Name
        {
            get { return TypeName; }
        }

        public override bool IsGenericType { get { return (ImplFlags & IF_GT) == IF_GT; } }
        public override bool IsGenericTypeDefinition { get { return (ImplFlags & IF_GTD) == IF_GTD; } }

        [MethodAlias("_ZW6System4Type_18type_is_subtype_of_Rb_P3V4TypeV4Typeb")]
        [AlwaysCompile]
        static bool IsSubtypeOf(TysosType subclass, TysosType superclass, bool check_interfaces)
        {
            if (subclass == superclass)
                return false;

            if (check_interfaces)
            {
                /* Is superclass an interface of subclass? */

                Type[] ifaces = subclass.GetInterfaces();
                foreach (Type iface in ifaces)
                    if (superclass == iface)
                        return true;
            }

            TysosType base_class = subclass.Extends;
            while (base_class != null)
            {
                if (base_class == superclass)
                    return true;
                base_class = base_class.Extends;
            }

            return false;
        }

        [MethodAlias("_ZW6System4Type_23type_is_assignable_from_Rb_P2V4TypeV4Type")]
        [AlwaysCompile]
        static bool IsAssignableFrom(TysosType cur_type, TysosType from_type)
        {
            if (cur_type == from_type)
                return true;

            if (cur_type.IsBoxed && from_type.IsBoxed)
                return IsAssignableFrom(ReinterpretAsType(cur_type.UnboxedType), ReinterpretAsType(from_type.UnboxedType));
            if (cur_type.IsManagedPointer && from_type.IsManagedPointer)
                return IsAssignableFrom(ReinterpretAsType(cur_type.UnboxedType), ReinterpretAsType(from_type.UnboxedType));
            if (cur_type.IsZeroBasedArray && from_type.IsZeroBasedArray)
                return IsAssignableFrom(ReinterpretAsType(cur_type.UnboxedType), ReinterpretAsType(from_type.UnboxedType));
            if (cur_type.IsManagedPointer && from_type.IsManagedPointer)
                return IsAssignableFrom(ReinterpretAsType(cur_type.UnboxedType), ReinterpretAsType(from_type.UnboxedType));

            return IsSubtypeOf(from_type, cur_type, true);
        }

        public override Type GetGenericTypeDefinition()
        {
            throw new InvalidOperationException();
        }

        public bool IsBoxed { get { return (ImplFlags & IF_TYPE_MASK) == IF_BOXED; } }
        public bool IsUnmanagedPointer { get { return (ImplFlags & IF_TYPE_MASK) == IF_PTR; } }
        public bool IsZeroBasedArray { get { return (ImplFlags & IF_TYPE_MASK) == IF_ZBA; } }
        public bool IsManagedPointer { get { return (ImplFlags & IF_TYPE_MASK) == IF_MPTR; } }
        public bool IsDynamic { get { return (ImplFlags & IF_DYNAMIC) == IF_DYNAMIC; } }
        public uint TypeFlags { get { return ImplFlags & IF_TYPE_MASK; } }
        public bool IsSimpleType { get { return (ImplFlags & IF_SIMPLE_ET) != 0; } }
        public uint SimpleTypeElementType { get { return (ImplFlags & IF_SIMPLE_ET) >> IF_SIMPLE_ET_SHIFT; } }
        public TysosType GetUnboxedType() { return ReinterpretAsType(UnboxedType); }

        public bool IsUninstantiatedGenericTypeParameter { get { return (ImplFlags & IF_UGTP) == IF_UGTP; } }
        public bool IsUninstantiatedGenericMethodParameter { get { return (ImplFlags & IF_UGMP) == IF_UGMP; } }
        public int UgtpIdx { get { return (int)((ImplFlags & IF_UGTP_MASK) >> IF_UGTP_SHIFT); } }
        public int UgmpIdx { get { return (int)((ImplFlags & IF_UGMP_MASK) >> IF_UGMP_SHIFT); } }

        protected override bool IsValueTypeImpl()
        {
            return (ImplFlags & IF_VTYPE) == IF_VTYPE;
        }

        [MethodAlias("_ZW6System4Type_15make_array_type_RV4Type_P2u1ti")]
        [AlwaysCompile]
        static TysosType make_array_type(TysosType cur_type, int rank)
        {
            if (rank != 1)
                throw new Exception("Rank can only be 1 in make_array_type");

            return TysosArrayType.make_zba(cur_type);
        }

        internal object Create()
        {
            object ret = MemoryOperations.GcMalloc(new IntPtr(this.ClassSize));

            unsafe
            {
                void* addr = CastOperations.ReinterpretAsPointer(ret);
                *(IntPtr*)((byte*)addr + libsupcs.ClassOperations.GetVtblFieldOffset()) = this.VTable;
            }

            return ret;
        }

        static internal int obj_id = 0;

        [MethodAlias("_Zu1O_7GetType_RW6System4Type_P1u1t")]
        [AlwaysCompile]
        static unsafe TysosType Object_GetType(void ***obj)
        {
            void** vtbl = *obj;
            void* ti = *vtbl;

            TysosType ret = ReinterpretAsType(ti);
            if (ret.IsBoxed)
                return ret.GetUnboxedType();
            else
                return ret;
        }

        [MethodAlias("_ZW6System4Type_14EqualsInternal_Rb_P2u1tV4Type")]
        [AlwaysCompile]
        static bool EqualsInternal(TysosType a, TysosType b)
        {
            return a.CompareTypes(b);
        }

        protected internal virtual bool CompareTypes(TysosType other)
        {
            if (this.IsDynamic || other.IsDynamic)
                throw new NotImplementedException("CompareTypes not implemented for dynamic types");
            return this == other;
        }

        [AlwaysCompile]
        [MethodAlias("castclassex")]
        internal static unsafe void *CastClassEx(void *from_obj, void *to_vtbl)
        {
            if (from_obj == null)
                return null;

            if (to_vtbl == null)
                throw new InvalidCastException("CastClassEx: to_vtbl is null");

            void* from_type;
            void* from_vtbl;
            void* to_type;

            from_vtbl = *(void**)from_obj;
            if (from_vtbl == null)
                throw new InvalidCastException("CastClassEx: from_vtbl is null");
            from_type = *(void**)from_vtbl;
            to_type = *(void**)to_vtbl;

            if (from_type == to_type)
                return from_obj;

            bool has_rtti = true;
            if (from_type == null)
                has_rtti = false;
            if (*(void**)from_type == null)
                has_rtti = false;
            if (to_type == null)
                has_rtti = false;
            if (*(void**)to_type == null)
                has_rtti = false;

            /* Check for equality amongst dynamic types */
            if (has_rtti)
            {
                TysosType from_type_obj = ReinterpretAsType(from_type);
                TysosType to_type_obj = ReinterpretAsType(to_type);
                if ((from_type_obj.IsDynamic || to_type_obj.IsDynamic) && (from_type_obj.TypeFlags != 0) && (from_type_obj.TypeFlags == to_type_obj.TypeFlags))
                {
                    if (IsAssignableFrom(to_type_obj.GetUnboxedType(), from_type_obj.GetUnboxedType()))
                        return from_obj;
                    return null;
                }
            }

            /* Check whether we extend the type */
            void* cur_extends_vtbl = *(void**)((byte*)from_vtbl + ClassOperations.GetVtblExtendsVtblPtrOffset());
            while (cur_extends_vtbl != null)
            {
                if (cur_extends_vtbl == to_vtbl)
                    return from_obj;
                cur_extends_vtbl = *(void**)((byte*)cur_extends_vtbl + ClassOperations.GetVtblExtendsVtblPtrOffset());
            }

            /* Check whether we implement the type as an interface */
            void** cur_iface_ptr = *(void***)((byte*)from_vtbl + ClassOperations.GetVtblInterfacesPtrOffset());

            if (cur_iface_ptr != null)
            {
                while (*cur_iface_ptr != null)
                {
                    if (*cur_iface_ptr == to_type)
                        return from_obj;

                    cur_iface_ptr += 2;
                }
            }

            return null;
        }

        /** <summary>Get the size of the type when it is a field in a type.  This will return the pointer size for
         * reference types and boxed value types and the size of the type for unboxed value types</summary> */
        internal virtual int GetSizeAsField()
        {
            if (IsValueType)
                return GetClassSize();
            else
                return OtherOperations.GetPointerSize();
        }

        /** <summary>Get the size of the type when it is an array element.  This will the return the pointer size
         * for reference types and boxed value types and the packed size for unboxed value types</summary> */
        internal virtual int GetSizeAsArrayElement()
        {
            if (IsValueType)
            {
                if (IsSimpleType)
                {
                    switch (SimpleTypeElementType)
                    {
                        case 0x02:
                        case 0x04:
                        case 0x05:
                            return 1;

                        case 0x03:
                        case 0x06:
                        case 0x07:
                            return 2;
                    }
                }

                return GetClassSize();
            }
            else
                return OtherOperations.GetPointerSize();
        }

        [AlwaysCompile]
        [WeakLinkage]
        [MethodAlias("_Zu1L_14InternalEquals_Rb_P3u1Ou1ORu1Zu1O")]
        private static unsafe bool ValueType_InternalEquals(void** o1, void** o2, out void* fields)
        {
            /* This doesn't yet perform the required behaviour.  Currently, we just
             * perform a byte-by-byte comparison, however if any of the fields are
             * reference types we should instead run .Equals() on them.
             * 
             * If we were to pass the references in the fields array i.e.
             * [ obj1_field1, obj2_field1, obj1_field2, obj2_field2, ... ]
             * then mono would do this for us.
             * 
             * We need to check both type equality and byte-by-bye equality */

            fields = null;

            void* o1vt = *o1;
            void* o2vt = *o2;

            // This needs fixing for dynamic types
            if (o1vt != o2vt)
                return false;

            // Get type sizes
            int o1tsize = *(int*)((byte*)o1vt + ClassOperations.GetVtblTypeSizeOffset());
            int o2tsize = *(int*)((byte*)o2vt + ClassOperations.GetVtblTypeSizeOffset());

            if (o1tsize != o2tsize)
                return false;

            int header_size = ClassOperations.GetBoxedTypeDataOffset();

            byte* o1_ptr = (byte*)o1 + header_size;
            byte* o2_ptr = (byte*)o2 + header_size;

            if (MemoryOperations.MemCmp(o1_ptr, o2_ptr, o1tsize) == 0)
                return true;
            else
                return false;
        }

        [AlwaysCompile]
        [WeakLinkage]
        [MethodAlias("_Zu1L_19InternalGetHashCode_Ri_P2u1ORu1Zu1O")]
        private static unsafe int ValueType_InternalGetHashCode(void** o, out void* fields)
        {
            /* This doesn't yet perform the required behaviour.  Currently, we just
             * perform a byte-by-byte hash, however if any of the fields are
             * reference types we should instead run .GetHashCode() on them.
             * 
             * If we were to pass the references in the fields array i.e.
             * [ field1, field2, ... ]
             * then mono would do this for us. */

            fields = null;

            void* ovt = *o;

            // Get type size
            int otsize = *(int*)((byte*)ovt + ClassOperations.GetVtblTypeSizeOffset());

            // Get pointer to data
            int header_size = ClassOperations.GetBoxedTypeDataOffset();

            byte* o_ptr = (byte*)o + header_size;

            // ELF hash
            uint h = 0, g;
            for(int i = 0; i < otsize; i++)
            {
                h = (h << 4) + *o_ptr++;
                g = h & 0xf0000000;
                if(g != 0)
                {
                    h ^= g >> 24;
                }
                h &= ~g;
            }

            unchecked
            {
                return (int)h;
            }
        }
    }
}
