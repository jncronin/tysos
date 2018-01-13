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
    public unsafe class TysosType : System.Type
    {
        metadata.TypeSpec ts = null;

        /** <summary>holds a pointer to the vtbl represented by this type</summary> */
        void* _impl;

        internal metadata.TypeSpec tspec
        {
            get
            {
                if(ts == null)
                {
                    ts = Metadata.GetTypeSpec(this);
                }
                return ts;
            }
        }

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
        public static unsafe extern RuntimeTypeHandle ReinterpretAsTypeHandle(void* obj);


        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static unsafe extern TysosMethod ReinterpretAsMethodInfo(void* obj);

        public virtual int GetClassSize() { throw new NotImplementedException(); }

        public override System.Reflection.Assembly Assembly
        {
            get { throw new NotImplementedException(); }
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
            get { throw new NotImplementedException(); }
        }

        public override string FullName
        {
            get { return Namespace + "." + Name; }
        }

        public override Guid GUID
        {
            get { throw new NotImplementedException(); }
        }

        protected override System.Reflection.TypeAttributes GetAttributeFlagsImpl()
        {
            throw new NotImplementedException();
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

                    if (add)
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
            throw new NotImplementedException();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.MemberInfo[] GetMembers(System.Reflection.BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override System.Reflection.MethodInfo GetMethodImpl(string name, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Reflection.CallingConventions callConvention, Type[] types, System.Reflection.ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            get { throw new NotImplementedException(); }
        }

        string nspace = null, name = null;
        public override string Namespace
        {
            get {
                if (nspace != null)
                    return nspace;

                var ts = tspec;
                if (ts == null)
                    nspace = "System";
                else
                {
                    nspace = ts.m.GetStringEntry(metadata.MetadataStream.tid_TypeDef,
                        ts.tdrow, 2);
                }

                return nspace;
            }
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
            get
            {
                if (name != null)
                    return name;

                var ts = tspec;
                if (ts == null)
                    name = "Void";
                else
                {
                    name = ts.m.GetStringEntry(metadata.MetadataStream.tid_TypeDef,
                        ts.tdrow, 1);
                }

                return name;
            }
        }

        public virtual TysosType UnboxedType { get { throw new NotImplementedException(); } }

        public override bool IsGenericType { get { throw new NotImplementedException(); } }
        public override bool IsGenericTypeDefinition { get { throw new NotImplementedException(); } }

        [MethodAlias("_ZW6System4Type_18type_is_subtype_of_Rb_P3V4TypeV4Typeb")]
        [AlwaysCompile]
        static unsafe bool IsSubtypeOf(void* subclass, void* superclass, bool check_interfaces)
        {
            var sub_vt = *(void**)((byte*)subclass + ClassOperations.GetSystemTypeImplOffset());
            var super_vt = *(void**)((byte*)superclass + ClassOperations.GetSystemTypeImplOffset());

            var cur_sub_vt = *(void**)((byte*)sub_vt + ClassOperations.GetVtblExtendsVtblPtrOffset());

            while(cur_sub_vt != null)
            {
                if (cur_sub_vt == super_vt)
                    return true;

                cur_sub_vt = *(void**)((byte*)cur_sub_vt + ClassOperations.GetVtblExtendsVtblPtrOffset());
            }

            if(check_interfaces)
                throw new NotImplementedException();

            return false;
        }

        [MethodAlias("_ZW6System4Type_23type_is_assignable_from_Rb_P2V4TypeV4Type")]
        [AlwaysCompile]
        static unsafe bool IsAssignableFrom(TysosType cur_type, TysosType from_type)
        {
            if (cur_type == from_type)
                return true;

            var cur_vtbl = *((void**)((byte*)CastOperations.ReinterpretAsPointer(cur_type) + ClassOperations.GetSystemTypeImplOffset()));
            var from_vtbl = *((void**)((byte*)CastOperations.ReinterpretAsPointer(from_type) + ClassOperations.GetSystemTypeImplOffset()));

            // check extends chain
            var cur_from_vtbl = from_vtbl;
            while(cur_from_vtbl != null)
            {
                if (cur_from_vtbl == cur_vtbl)
                    return true;
                cur_from_vtbl = *((void**)((byte*)cur_from_vtbl + ClassOperations.GetVtblExtendsVtblPtrOffset()));
            }

            // check interfaces
            var cur_from_iface_ptr = *(void***)((byte*)cur_vtbl + ClassOperations.GetVtblInterfacesPtrOffset());
            while(*cur_from_iface_ptr != null)
            {
                if (*cur_from_iface_ptr == cur_vtbl)
                    return true;
                cur_from_iface_ptr += 2;
            }

            // check whether they are arrays of the same type
            var cur_ts = cur_type.tspec;
            var from_ts = cur_type.tspec;

            if(cur_ts.stype == metadata.TypeSpec.SpecialType.SzArray &&
                from_ts.stype == metadata.TypeSpec.SpecialType.SzArray)
            {
                if (cur_ts.other.Equals(from_ts.other))
                    return true;
            }

            // TODO: complex array

            return false;
        }

        public override Type GetGenericTypeDefinition()
        {
            throw new InvalidOperationException();
        }

        public unsafe override RuntimeTypeHandle TypeHandle
        {
            get
            {
                var vtbl = *GetImplOffset();
                return ReinterpretAsTypeHandle(vtbl);
            }
        }

        public bool IsUnmanagedPointer { get { return tspec.stype == metadata.TypeSpec.SpecialType.Ptr; } }
        public bool IsZeroBasedArray { get { return tspec.stype == metadata.TypeSpec.SpecialType.SzArray; } }
        public bool IsManagedPointer { get { return tspec.stype == metadata.TypeSpec.SpecialType.MPtr; } }
        public bool IsDynamic { get { throw new NotImplementedException(); } }
        public uint TypeFlags { get { throw new NotImplementedException(); } }

        public bool IsUninstantiatedGenericTypeParameter { get { throw new NotImplementedException(); } }
        public bool IsUninstantiatedGenericMethodParameter { get { throw new NotImplementedException(); } }
        public int UgtpIdx { get { throw new NotImplementedException(); } }
        public int UgmpIdx { get { throw new NotImplementedException(); } }

        internal unsafe static int GetValueTypeSize(void *boxed_vtbl)
        {
            /* Boxed value types (that aren't enums) store their size in the
             * typeinfo.  Enums store the underlying type there, so we have
             * to call ourselves again if this is an enum. */

            void* ti = *(void**)boxed_vtbl;
            void* extends = *(void**)((byte*)boxed_vtbl + ClassOperations.GetVtblExtendsVtblPtrOffset());
            if (extends == OtherOperations.GetStaticObjectAddress("_ZW6System4Enum"))
            {
                void *underlying_type = *((void**)ti + 1);
                return GetValueTypeSize(underlying_type);
            }

            /* All calling functions should guarantee this is a value type, so the following is valid */
            return *(int*)((void**)ti + 1);
        }

        protected override bool IsValueTypeImpl()
        {
            unsafe
            {
                void* extends = *(void**)((byte*)CastOperations.ReinterpretAsPointer(this) + ClassOperations.GetVtblExtendsVtblPtrOffset());

                if (extends == OtherOperations.GetStaticObjectAddress("_Zu1L") ||
                    extends == OtherOperations.GetStaticObjectAddress("_ZW6System4Enum"))
                    return true;
                return false;
            }
        }

        [MethodAlias("_ZW6System4Type_15make_array_type_RV4Type_P2u1ti")]
        [AlwaysCompile]
        static TysosType make_array_type(TysosType cur_type, int rank)
        {
            throw new NotImplementedException();
        }

        internal unsafe object Create()
        {
            byte* ret = (byte *)MemoryOperations.GcMalloc(GetClassSize());
            void* vtbl = *(void**)((byte*)CastOperations.ReinterpretAsPointer(this) + ClassOperations.GetSystemTypeImplOffset());

            *(void**)(ret + ClassOperations.GetVtblFieldOffset()) = vtbl;
            *(ulong*)(ret + ClassOperations.GetMutexLockOffset()) = 0;

            return CastOperations.ReinterpretAsObject(ret);
        }

        static internal int obj_id = 0;

        [MethodAlias("_Zu1O_7GetType_RW6System4Type_P1u1t")]
        [AlwaysCompile]
        static unsafe TysosType Object_GetType(void **obj)
        {
            void* vtbl = *obj;

            return internal_from_handle(vtbl);
        }

        [MethodAlias("_ZW35System#2ERuntime#2ECompilerServices14RuntimeHelpers_20_RunClassConstructor_Rv_P1U6System11RuntimeType")]
        [AlwaysCompile]
        static unsafe void RuntimeHelpers__RunClassConstructor(void *vtbl)
        {
            // Ensure ptr is valid
            if (vtbl == null)
            {
                System.Diagnostics.Debugger.Log(0, "libsupcs", "RuntimeHelpers._RunClassConstructor: called with null pointer");
                throw new Exception("Invalid type handle");
            }

            // dereference vtbl pointer to get ti ptr
            var ptr = *((void**)vtbl);
            if (ptr == null)
            {
                System.Diagnostics.Debugger.Log(0, "libsupcs", "RuntimeHelpers._RunClassConstructor: called with null pointer");
                throw new Exception("Invalid type handle");
            }

            if ((*((int*)ptr) & 0xf) != 0)
            {
                System.Diagnostics.Debugger.Log(0, "libsupcs", "RuntimeHelpers._RunClassConstructor: called with invalid runtimehandle: " +
                    (*((int*)ptr)).ToString() + " at " + ((ulong)ptr).ToString("X16"));
                System.Diagnostics.Debugger.Break();
                throw new Exception("Invalid type handle");
            }

            var ti_ptr = (void**)ptr;

            var cctor_addr = ti_ptr[3];
            if(cctor_addr != null)
            {
                System.Diagnostics.Debugger.Log(0, "libsupcs", "RuntimeHelpers._RunClassConstructor: running static constructor at " +
                    ((ulong)cctor_addr).ToString("X16"));
                OtherOperations.CallI(cctor_addr);
                System.Diagnostics.Debugger.Log(0, "libsupcs", "RuntimeHelpers._RunClassConstructor: finished running static constructor at " +
                    ((ulong)cctor_addr).ToString("X16"));
            }
        }

        internal unsafe void** GetImplOffset()
        {
            byte* tp = (byte*)CastOperations.ReinterpretAsPointer(this);
            return (void**)(tp + ClassOperations.GetSystemTypeImplOffset());
        }

        [WeakLinkage]
        [AlwaysCompile]
        [MethodAlias("_ZW6System4Type_13op_Inequality_Rb_P2V4TypeV4Type")]
        static internal unsafe bool NotEqualsInternal(TysosType a, TysosType b)
        {
            return !EqualsInternal(a, b);
        }

        [MethodAlias("_ZW6System4Type_11op_Equality_Rb_P2V4TypeV4Type")]
        [MethodAlias("_ZW6System4Type_14EqualsInternal_Rb_P2u1tV4Type")]
        [AlwaysCompile]
        static internal unsafe bool EqualsInternal(TysosType a, TysosType b)
        {
            void* a_vtbl = *(void**)((byte*)CastOperations.ReinterpretAsPointer(a) + ClassOperations.GetSystemTypeImplOffset());
            void* b_vtbl = *(void**)((byte*)CastOperations.ReinterpretAsPointer(b) + ClassOperations.GetSystemTypeImplOffset());

            if ((a_vtbl != null) && (a_vtbl == b_vtbl))
                return true;

            return a.tspec.Equals(b.tspec);
        }

        [AlwaysCompile]
        [MethodAlias("castclassex")]
        internal static unsafe void *CastClassEx(void *from_obj, void *to_vtbl)
        {
            if (from_obj == null)
            {
                return null;
            }

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

            /* If both are arrays with non-null elem types, do an array-element-compatible-with
             *  (CIL I:8.7.1) comparison */
            void* from_extends = *(void**)((byte*)from_vtbl + ClassOperations.GetVtblExtendsVtblPtrOffset());
            void* to_extends = *(void**)((byte*)to_vtbl + ClassOperations.GetVtblExtendsVtblPtrOffset());
            if(from_extends == OtherOperations.GetStaticObjectAddress("_ZW6System5Array") &&
                from_extends == to_extends)
            {
                void* from_et = *(((void**)from_type) + 1);
                void* to_et = *(((void**)to_type) + 1);

                if(from_et != null && to_et != null)
                {
                    from_et = get_array_element_compatible_with_vt(from_et);
                    to_et = get_array_element_compatible_with_vt(to_et);

                    if (from_et == to_et)
                        return from_obj;
                    else
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

        private static unsafe void* get_array_element_compatible_with_vt(void* et)
        {
            /* If this is an enum, get underlying type */
            et = get_enum_underlying_type(et);

            /* If this is a signed integer type, return the unsigned counterpart */
            if (et == OtherOperations.GetStaticObjectAddress("_Za"))
                return OtherOperations.GetStaticObjectAddress("_Zh");
            else if (et == OtherOperations.GetStaticObjectAddress("_Zs"))
                return OtherOperations.GetStaticObjectAddress("_Zt");
            else if (et == OtherOperations.GetStaticObjectAddress("_Zc"))
                return OtherOperations.GetStaticObjectAddress("_Zt");
            else if (et == OtherOperations.GetStaticObjectAddress("_Zi"))
                return OtherOperations.GetStaticObjectAddress("_Zj");
            else if (et == OtherOperations.GetStaticObjectAddress("_Zx"))
                return OtherOperations.GetStaticObjectAddress("_Zy");
            else if (et == OtherOperations.GetStaticObjectAddress("_Zu1I"))
                return OtherOperations.GetStaticObjectAddress("_Zu1U");

            /* Else return the vtable unchanged */
            return et;
        }

        /* If this is an enum vtable, return its underlying type, else
         * return the vtable unchanged */
        private static unsafe void* get_enum_underlying_type(void* vt)
        {
            void* extends = *(void**)((byte*)vt + ClassOperations.GetVtblExtendsVtblPtrOffset());
            if (extends == OtherOperations.GetStaticObjectAddress("_ZW6System4Enum"))
            {
                void** ti = *(void***)vt;
                return *(ti + 1);
            }
            return vt;
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

            /* Rationalise vtables to enums to their underlying type counterparts */
            void* o1ext = *(void**)((byte*)o1vt + ClassOperations.GetVtblExtendsVtblPtrOffset());
            void* o2ext = *(void**)((byte*)o2vt + ClassOperations.GetVtblExtendsVtblPtrOffset());
            if(o1ext == OtherOperations.GetStaticObjectAddress("_ZW6System4Enum"))
            {
                void** enum_ti = *(void***)o1vt;
                o1vt = *(enum_ti + 1);
            }
            if (o2ext == OtherOperations.GetStaticObjectAddress("_ZW6System4Enum"))
            {
                void** enum_ti = *(void***)o2vt;
                o2vt = *(enum_ti + 1);
            }

            // This needs fixing for dynamic types
            if (o1vt != o2vt)
            {
                while (true) ;
                return false;
            }

            // Get type sizes
            int o1tsize = *(int*)((byte*)o1vt + ClassOperations.GetVtblTypeSizeOffset());
            int o2tsize = *(int*)((byte*)o2vt + ClassOperations.GetVtblTypeSizeOffset());

            if (o1tsize != o2tsize)
                return false;

            int header_size = ClassOperations.GetBoxedTypeDataOffset();

            byte* o1_ptr = (byte*)o1 + header_size;
            byte* o2_ptr = (byte*)o2 + header_size;

            if (MemoryOperations.MemCmp(o1_ptr, o2_ptr, o1tsize - header_size) == 0)
                return true;
            else
                return false;
        }

        [AlwaysCompile]
        [WeakLinkage]
        [MethodAlias("_Zu1L_11GetHashCode_Ri_P1u1t")]
        private static unsafe int ValueType_GetHashCode(void **o)
        {
            void* f;
            return ValueType_InternalGetHashCode(o, out f);
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

        [AlwaysCompile]
        [WeakLinkage]
        [MethodAlias("_Zu1O_15MemberwiseClone_Ru1O_P1u1t")]
        static unsafe void* MemberwiseClone(void *obj)
        {
            void* vtbl = *((void**)obj);
            int class_size = *((byte*)vtbl + ClassOperations.GetVtblTypeSizeOffset());

            void* ret = MemoryOperations.GcMalloc(class_size);
            MemoryOperations.MemCpy(ret, obj, class_size);

            // Set the mutex lock on the new object to 0
            *(void**)((byte*)ret + ClassOperations.GetMutexLockOffset()) = null;

            return ret;
        }

        [AlwaysCompile]
        [WeakLinkage]
        [MethodAlias("_ZW6System4Type_17GetTypeFromHandle_RV4Type_P1V17RuntimeTypeHandle")]
        static internal unsafe TysosType internal_from_handle(void *vtbl)
        {
            /* The default value for a type info is:
             * 
             * intptr type (= 0)
             * intptr enum_underlying_type_vtbl_ptr
             * intptr tysostype pointer
             * intptr metadata reference count
             * metadata references
             * signature
             * 
             * To satisfy the constraint that there can only exist one
             * System.Type representation for a given type, we need to
             * do some trickery to ensure that this occurs.
             * 
             * On call of this function, we check the second byte of 'type'
             * If it is '2' (i.e type = 0x0200), we can take the third
             * member as a valid tysostype pointer
             * 
             * If it is '1', we wait until it is '2'
             * 
             * If it is zero, we atomically set it to '1' and create the
             * tysos type, then set it to '2' when done
             */
            void* ti = *(void**)vtbl;
            byte* lck = (byte*)ti + 1;

            while(true)
            {
                if(*lck == 2)
                {
                    void* ttype = *((void**)ti + 2);
                    return ReinterpretAsType(ttype);
                }
                else if(*lck == 0)
                {
                    if (OtherOperations.SyncValCompareAndSwap(lck, 0, 1) == 0)
                        break;
                }
                OtherOperations.SpinlockHint();
            }

            var ret = new TysosType();

            var ret_obj = CastOperations.ReinterpretAsPointer(ret);
            *(void**)((byte*)ret_obj + ClassOperations.GetSystemTypeImplOffset()) = vtbl;

            *((void**)ti + 2) = ret_obj;

            *lck = 2;

            return ret;
        }
    }
}
