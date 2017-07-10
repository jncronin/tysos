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

        public override string Namespace
        {
            get {
                var corlib = Metadata.MSCorlib;
                System.Diagnostics.Debugger.Break();
                throw new NotImplementedException();
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
            get { throw new NotImplementedException(); }
        }

        public virtual TysosType UnboxedType { get { throw new NotImplementedException(); } }

        public override bool IsGenericType { get { throw new NotImplementedException(); } }
        public override bool IsGenericTypeDefinition { get { throw new NotImplementedException(); } }

        [MethodAlias("_ZW6System4Type_18type_is_subtype_of_Rb_P3V4TypeV4Typeb")]
        [AlwaysCompile]
        static bool IsSubtypeOf(TysosType subclass, TysosType superclass, bool check_interfaces)
        {
            throw new NotImplementedException();
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

        public bool IsBoxed { get { throw new NotImplementedException(); } }
        public bool IsUnmanagedPointer { get { throw new NotImplementedException(); } }
        public bool IsZeroBasedArray { get { throw new NotImplementedException(); } }
        public bool IsManagedPointer { get { throw new NotImplementedException(); } }
        public bool IsDynamic { get { throw new NotImplementedException(); } }
        public uint TypeFlags { get { throw new NotImplementedException(); } }
        public bool IsSimpleType { get { throw new NotImplementedException(); } }
        public uint SimpleTypeElementType { get { throw new NotImplementedException(); } }
        public TysosType GetUnboxedType() { return ReinterpretAsType(UnboxedType); }

        public bool IsUninstantiatedGenericTypeParameter { get { throw new NotImplementedException(); } }
        public bool IsUninstantiatedGenericMethodParameter { get { throw new NotImplementedException(); } }
        public int UgtpIdx { get { throw new NotImplementedException(); } }
        public int UgmpIdx { get { throw new NotImplementedException(); } }

        protected override bool IsValueTypeImpl()
        {
            throw new NotImplementedException();
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

            var ret = new TysosType();
            byte* retp = (byte*)CastOperations.ReinterpretAsPointer(ret);
            *(void**)(retp + ClassOperations.GetSystemTypeImplOffset()) = vtbl;

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
    }
}
