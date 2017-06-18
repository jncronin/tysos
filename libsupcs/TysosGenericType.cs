/* Copyright (C) 2012 by John Cronin
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

namespace libsupcs
{
    [VTableAlias("__tysos_gtd_vt")]
    [SpecialType]
    class TysosGenericTypeDefinition : TysosType
    {
        internal UIntPtr GenericParams;
    }

    [VTableAlias("__tysos_gt_vt")]
    [SpecialType]
    class TysosGenericType : TysosType
    {
        // The definition on which this type is based
        internal TysosGenericTypeDefinition GenericTypeDefinition;

        public override Type GetGenericTypeDefinition()
        {
            return GenericTypeDefinition;
        }

        // The generic type parameters
        internal TysosType[] GenericParams
        {
            get
            {
                if(_GenericParams == null)
                {
                    // convert _GParams into an array
                    unsafe
                    {
                        void** curgp = (void**)_GParams;
                        int count = 0;
                        if(curgp != null)
                        {
                            while (*curgp != null)
                            {
                                curgp++;
                                count++;
                            }
                        }

                        TysosType[] ret = new TysosType[count];
                        curgp = (void**)_GParams;
                        count = 0;
                        if(curgp != null)
                        {
                            while(*curgp != null)
                            {
                                ret[count++] = ReinterpretAsType(*curgp);
                                curgp++;
                            }
                        }
                        _GenericParams = ret;
                    }
                }
                return _GenericParams;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return GenericParams;
        }

        // _GenericParams is filled in by MakeGenericType()
        internal TysosType[] _GenericParams;

        // _GParams is filled in by tysila
        [NullTerminatedListOf(typeof(TysosType))]
        internal UIntPtr _GParams;

        internal TysosGenericType() { }

        public override string FullName
        {
            get
            {
                string ret = base.FullName;
                ret += "[";
                for (int i = 0; i < GenericParams.Length; i++)
                {
                    if (i != 0)
                        ret += ",";
                    ret += "[";
                    ret += GenericParams[i].AssemblyQualifiedName;
                    ret += "]";
                }
                ret += "]";
                return ret;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return true;
            }
        }

        [AlwaysCompile]
        [MethodAlias("_ZW6System4Type_15MakeGenericType_RV4Type_P2V4Typeu1ZV4Type")]
        internal static TysosGenericType MakeGenericType(TysosType type_def, TysosType[] p)
        {
            if (!type_def.IsGenericTypeDefinition)
                throw new Exception("type_def is not a valid generic type definition");

            TysosGenericType ret = new TysosGenericType();

            ret.GenericTypeDefinition = type_def as TysosGenericTypeDefinition;
            ret._GenericParams = new TysosType[p.Length];
            for (int i = 0; i < p.Length; i++)
                ret._GenericParams[i] = p[i];
            ret._Assembly = type_def._Assembly;
            ret._Module = type_def._Module;
            ret.Flags = type_def.Flags;
            ret.ImplFlags = IF_DYNAMIC;
            ret.TypeName = type_def.TypeName;
            ret.TypeNamespace = type_def.TypeNamespace;

            // layout new type and calculate class size
            DoGenericLayout(ret, type_def, p);

            // Instantiate base classes
            ret.Extends = InstantiateType(type_def.Extends, p);

            // Instantiate unboxed type
            if (type_def.UnboxedType != (IntPtr)0)
                ret.UnboxedType = libsupcs.CastOperations.ReinterpretAsIntPtr(InstantiateType(ReinterpretAsType(type_def.UnboxedType), p));
            
            // Instantiate VTable and Interfaces
            InstantiateVTable(ret, type_def, p);

            // Note the following three need to be implemented in libtysila/Layout_InfoStructures.cs first
            // TODO instantiate nested types
            ret.NestedTypes = (IntPtr)0;
            // TODO instantiate properties
            ret.Properties = (IntPtr)0;
            // TODO instantiate events
            ret.Events = (IntPtr)0;

            return ret;
        }

        private unsafe static void InstantiateVTable(TysosGenericType ret, TysosType type_def, TysosType[] p)
        {
            /* See libtysila/Layout_InfoStructures.cs for the layout of the VTable
             * Note that for generic type definitions, the actual implementations (e.g. method addresses)
             * are replaced by pointers to the appropriate info structures
             */
            // Allocate the new vtable
            IntPtr* new_vt = (IntPtr*)CastOperations.ReinterpretAsIntPtr(MemoryOperations.GcMalloc(type_def.VTableLength));
            ret.VTable = (IntPtr)new_vt;

            // Determine the old vtable
            IntPtr* old_vt = (IntPtr*)*(IntPtr*)CastOperations.ReinterpretAsIntPtr(type_def);

            // Determine the end of the old vtable
            IntPtr* old_vt_end = (IntPtr*)OtherOperations.Add((IntPtr)old_vt, type_def.VTableLength);

            // Write the typeinfo pointer
            *new_vt++ = CastOperations.ReinterpretAsIntPtr(ret);

            // Store the place where we have to write the interface map pointer
            IntPtr* new_ifacemapptr_location = new_vt++;

            // Write the extends_vtbl
            if (ret.Extends != null)
                *new_vt++ = ret.Extends.VTable;
            else
                *new_vt++ = (IntPtr)0;

            // Load the old ifacemapptr
            old_vt++;
            IntPtr* old_ifacemapptr = (IntPtr *)*old_vt++;

            // Iterate through the virtual methods
            while (old_vt < old_ifacemapptr)
            {
                // Handle the case of pure virtual functions
                if (*old_vt == (IntPtr)0)
                {
                    *new_vt++ = JitOperations.GetAddressOfObject(TysosMethod.PureVirtualName);
                    old_vt++;
                }
                else
                    *new_vt++ = InstantiateMethod(ReinterpretAsMethodInfo(*old_vt++), ret, p).MethodAddress;
            }

            // We now have to write out the interface list
            // First, point the interface map pointer and Interfaces members to here
            *new_ifacemapptr_location = (IntPtr)new_vt;
            ret.Interfaces = (IntPtr)new_vt;

            // Now iterate through the list until '0' is reached
            while (*old_vt != (IntPtr)0)
            {
                // Interface type info pointer
                *new_vt++ = CastOperations.ReinterpretAsIntPtr(InstantiateType(ReinterpretAsType(*old_vt++), p));

                /* Now work out where the interface methods are stored
                 * They will be stored at ret.Interfaces + x, where 
                 *  x = old_vt_interface_method_pointer - old_ifacemapptr
                 */
                IntPtr x = libsupcs.OtherOperations.Sub(*old_vt++, (IntPtr)old_ifacemapptr);
                *new_vt++ = libsupcs.OtherOperations.Add(ret.Interfaces, x);
            }

            // Null terminate
            *new_vt++ = (IntPtr)0;
            old_vt++;

            // Now iterate through all the interface methods implementing them as we go
            while (old_vt < old_vt_end)
            {
                if (*old_vt == (IntPtr)0)
                {
                    *new_vt++ = JitOperations.GetAddressOfObject(TysosMethod.PureVirtualName);
                    old_vt++;
                }
                else
                    *new_vt++ = InstantiateMethod(ReinterpretAsMethodInfo(*old_vt++), ret, p).MethodAddress;
            }
        }

        private static void DoGenericLayout(TysosGenericType ret, TysosType type_def, TysosType[] p)
        {
            // Layout the new type

            // Layout fields
            int classsize = 0;
            int staticclasssize = 0;
            
            // First, iterate through the Fields array to get the total number
            int field_count = 0;
            ret.Fields = (IntPtr)0;
            unsafe
            {
                IntPtr* field_ptr = (IntPtr*)type_def.Fields;

                if (field_ptr != null)
                {
                    while (*field_ptr != (IntPtr)0)
                    {
                        field_count++;
                        field_ptr++;
                    }
                }
            }

            if (field_count != 0)
            {
                // Now allocate a new fields array for the new type
                IntPtr new_fields = CastOperations.ReinterpretAsIntPtr(MemoryOperations.GcMalloc((IntPtr)((field_count + 1) * OtherOperations.GetPointerSize())));

                // Fill in the new fields array
                unsafe
                {
                    IntPtr* new_fields_ptr = (IntPtr*)new_fields;
                    IntPtr* field_ptr = (IntPtr*)type_def.Fields;

                    if (field_ptr != null)
                    {
                        while (*field_ptr != (IntPtr)0)
                        {
                            TysosField new_field = InstantiateField(ReinterpretAsFieldInfo(*field_ptr), p);
                            new_field.OwningType = ret;

                            if (new_field.IsStatic)
                            {
                                new_field.offset = staticclasssize;
                                staticclasssize += GetFieldSize(new_field);
                            }
                            else
                            {
                                new_field.offset = classsize;
                                classsize += GetFieldSize(new_field);
                            }
                            *new_fields_ptr = CastOperations.ReinterpretAsIntPtr(new_field);
                            new_fields_ptr++;
                            field_ptr++;
                        }
                    }

                    // Null terminate the array
                    *new_fields_ptr = (IntPtr)0;
                    ret.Fields = new_fields;
                }
            }
            ret.ClassSize = classsize;
            ret.StaticClassSize = staticclasssize;

            // Layout methods
            ret.Methods = (IntPtr)0;

            // First, iterate through the Methods array to get the total number
            int meth_count = 0;
            unsafe
            {
                IntPtr* meth_ptr = (IntPtr*)type_def.Methods;

                if (meth_ptr != null)
                {
                    while (*meth_ptr != (IntPtr)0)
                    {
                        meth_count++;
                        meth_ptr++;
                    }
                }
            }

            if (meth_count != 0)
            {
                // Now allocate a new methods array for the new type
                IntPtr new_meths = CastOperations.ReinterpretAsIntPtr(MemoryOperations.GcMalloc((IntPtr)((meth_count + 1) * OtherOperations.GetPointerSize())));

                // Fill in the new methods array
                unsafe
                {
                    IntPtr* new_meths_ptr = (IntPtr*)new_meths;
                    IntPtr* meth_ptr = (IntPtr*)type_def.Methods;

                    if (meth_ptr != null)
                    {
                        while (*meth_ptr != (IntPtr)0)
                        {
                            TysosMethod new_meth = InstantiateMethod(ReinterpretAsMethodInfo(*meth_ptr), ret, p);

                            *new_meths_ptr = CastOperations.ReinterpretAsIntPtr(new_meth);
                            new_meths_ptr++;
                            meth_ptr++;
                        }
                    }

                    // Null terminate the array
                    *new_meths_ptr = (IntPtr)0;
                    ret.Methods = new_meths;
                }
            }
        }

        private static TysosMethod InstantiateMethod(TysosMethod methodInfo, TysosType owning_type, TysosType[] p)
        {
            TysosMethod ret = new TysosMethod();
            ret._Name = methodInfo._Name;
            ret.Flags = methodInfo.Flags;
            ret.ImplFlags = methodInfo.ImplFlags;
            ret.MethodAddress = (IntPtr)0;
            ret.TysosFlags = methodInfo.TysosFlags;
            ret.Instructions = methodInfo.Instructions;
            ret.OwningType = owning_type;

            // Interpret the Return Value
            ret._ReturnType = InstantiateType(methodInfo._ReturnType, p);

            // Interpret the parameters
            // First, count the params
            int param_count = 0;
            unsafe
            {
                IntPtr* param_ptr = (IntPtr*)methodInfo._Params;

                if (param_ptr != null)
                {
                    while (*param_ptr != (IntPtr)0)
                    {
                        param_count++;
                        param_ptr++;
                    }
                }
            }

            // Allocate an array for it
            IntPtr new_params = CastOperations.ReinterpretAsIntPtr(MemoryOperations.GcMalloc((IntPtr)((param_count + 1) * OtherOperations.GetPointerSize())));

            // Fill in the new methods array
            unsafe
            {
                IntPtr* new_params_ptr = (IntPtr*)new_params;
                IntPtr* param_ptr = (IntPtr*)methodInfo._Params;

                if (param_ptr != null)
                {
                    while (*param_ptr != (IntPtr)0)
                    {
                        TysosType new_param = InstantiateType(ReinterpretAsType(*param_ptr), p);
                        
                        *new_params_ptr = CastOperations.ReinterpretAsIntPtr(new_param);
                        new_params_ptr++;
                        param_ptr++;
                    }
                }

                // Null terminate the array
                *new_params_ptr = (IntPtr)0;
                ret._Params = new_params;
            }

            // Compile the method
            ret.MethodAddress = JitOperations.JitCompile(ret);

            return ret;
        }

        private static int GetFieldSize(TysosField new_field)
        {
            return ((TysosType)new_field.FieldType).GetSizeAsField();
        }

        private static TysosField InstantiateField(TysosField fieldInfo, TysosType[] p)
        {
            // Create a new field info, and then fill in the type info appropriately
            TysosField new_field;
            unsafe
            {
                new_field = ReinterpretAsFieldInfo(MemoryOperations.GcMalloc((IntPtr)((TysosType)typeof(TysosField)).GetClassSize()));
                MemoryOperations.MemCpy((void*)CastOperations.ReinterpretAsIntPtr(new_field), (void*)CastOperations.ReinterpretAsIntPtr(fieldInfo), ((TysosType)typeof(TysosField)).GetClassSize());
            }

            // Fill in the field type
            new_field._FieldType = InstantiateType(fieldInfo._FieldType, p);

            return new_field;
        }

        private static TysosType InstantiateType(TysosType type, TysosType[] p)
        {
            // If it is not a generic type, and has no UnboxedType to instantiate, just use the same type
            if ((!type.IsGenericType) && (type.UnboxedType == (IntPtr)0))
                return type;

            // Decide if its a generic type parameter
            if (type.IsUninstantiatedGenericTypeParameter)
            {
                int ugtp_idx = type.UgtpIdx;
                type = p[ugtp_idx];
            }
            
            // Create a new type info
            TysosType new_type;
            int cl_size = ((TysosType)type.GetType()).GetClassSize();
            unsafe
            {
                new_type = ReinterpretAsType(MemoryOperations.GcMalloc((IntPtr)cl_size));
                MemoryOperations.MemCpy((void*)CastOperations.ReinterpretAsIntPtr(new_type), (void*)CastOperations.ReinterpretAsIntPtr(type), cl_size);
            }
            new_type.ImplFlags |= TysosType.IF_DYNAMIC;
            new_type.Extends = InstantiateType(new_type.Extends, p);

            // Instantiate it if required
            
            /* First, decide if its a special type:
             *  1) Zero-based array
             *  2) Boxed type
             *  3) Managed pointer
             *  4) Unmanaged pointer
             */

            uint implflags = type.ImplFlags & TysosType.IF_TYPE_MASK;
            switch (implflags)
            {
                case TysosType.IF_ZBA:
                case TysosType.IF_BOXED:
                case TysosType.IF_MPTR:
                case TysosType.IF_PTR:
                    new_type.UnboxedType = CastOperations.ReinterpretAsIntPtr(InstantiateType(ReinterpretAsType(type.UnboxedType), p));
                    break;
            }

            // If not, then we just rely on the copy           

            return new_type;
        }
    }
}
