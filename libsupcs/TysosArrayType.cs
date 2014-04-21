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

namespace libsupcs
{
    class TysosArrayType : TysosType
    {
        internal TysosType elemtype;

        protected override bool HasElementTypeImpl()
        {
            return true;
        }

        public override Type GetElementType()
        {
            return elemtype;
        }

        public override object InvokeMember(string name, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object target, object[] args, System.Reflection.ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
        {
            if ((invokeAttr & System.Reflection.BindingFlags.CreateInstance) != 0)
            {
                // this is a constructor

                // first, create the object
                object o = this.Create();

                // run the appropriate constructor
                if (args.Length == 1)
                    ctor1(o as Array, (int)args[0]);
                else if (args.Length == 2)
                    ctor2(o as Array, (int)args[0], (int)args[1]);
                else
                    throw new NotSupportedException("TysosArrayType.InvokeMember: constructor with " + args.Length + " arguments");

                return o;
            }
            else
            {
                // This is not a constructor

                // TODO: check binding attributes

                if (name == "Get")
                    return Get((Array)target, (int)args[0]);
                else if (name == "Set")
                {
                    throw new NotImplementedException("TysosArrayType.InvokeMember(Set)");
                    //Set((Array)target, (int)args[0], (IntPtr)args[1]);
                    //return null;
                }
                else if (name == "Address")
                {
                    return Address((Array)target, (int)args[0]);
                }
                else
                    return base.InvokeMember(name, invokeAttr, binder, target, args);
            }
        }

        static internal TysosArrayType make_zba(TysosType elem_type)
        {
            TysosArrayType ret = new TysosArrayType();

            if (elem_type.IsValueType && (elem_type.GetClassSize() > 8))
                throw new NotImplementedException("make_zba not implemented for value types");

            ret.elemtype = elem_type;
            ret.ClassSize = ArrayOperations.GetArrayClassSize();
            ret._Assembly = elem_type._Assembly;
            ret._Module = elem_type._Module;
            ret.Events = new IntPtr(0);
            ret.Extends = TysosType.ReinterpretAsType(OtherOperations.GetStaticObjectAddress("_ZW6System5ArrayTI"));
            ret.Fields = new IntPtr(0);
            ret.Flags = 0x1;    // public
            ret.ImplFlags = TysosType.IF_ZBA | TysosType.IF_DYNAMIC;
            ret.NestedTypes = new IntPtr(0);
            ret.Properties = new IntPtr(0);
            ret.Sig_references = new IntPtr(0);
            ret.Signature = new IntPtr(0);
            ret.TypeName = elem_type.TypeName + "[]";
            ret.TypeNamespace = elem_type.TypeNamespace;
            ret.UnboxedType = CastOperations.ReinterpretAsIntPtr(elem_type);

            // write out a vtable for this type

            // copy the System.Array vtable
            unsafe
            {
                // Work out the length of the vtable
                ulong sa_vtbl = (ulong)((TysosType)typeof(System.Array)).VTable;

                IntPtr sa_imap_ptr = *(IntPtr*)(sa_vtbl + (ulong)sizeof(IntPtr));

                while(*(IntPtr*)sa_imap_ptr != new IntPtr(0))
                    sa_imap_ptr = new IntPtr((long)((ulong)sa_imap_ptr + (ulong)sizeof(IntPtr)));

                ulong sa_vtbl_end = (ulong)sa_imap_ptr + (ulong)sizeof(IntPtr);

                ulong sa_vtbl_length = sa_vtbl_end - sa_vtbl;

                IntPtr new_vtable = CastOperations.ReinterpretAsIntPtr(MemoryOperations.GcMalloc(new IntPtr((long)sa_vtbl_length)));
                ret.VTable = new_vtable;

                // Now copy the System.Array vtbl here
                MemoryOperations.MemCpy((void*)new_vtable, (void*)sa_vtbl, (int)sa_vtbl_length);

                // Now set the TypeInfoPtr to point back to our new type
                *(UIntPtr*)new_vtable = CastOperations.ReinterpretAsUIntPtr(ret);

                // Point the IntPtr Interfaces pointer to the interface map
                ret.Interfaces = *(IntPtr*)((ulong)new_vtable + (uint)sizeof(IntPtr));
            }

            /* The method list should contain 5 members and a null terminator.  The
             * methods are:
             * 
             * ElemType Get(int idx);                   "__array_get"
             * void Set(int idx, ElemType val);         "__array_set"
             * ElemType & Address(int idx);             "__array_address"
             * .ctor(int size);                         "__array_ctor_1"
             * .ctor(int lobound, int size);            "__array_ctor_2"
             */

            unsafe
            {
                IntPtr method_list = CastOperations.ReinterpretAsIntPtr(MemoryOperations.GcMalloc(new IntPtr(6 * sizeof(IntPtr))));
                IntPtr ptr_size = (IntPtr)sizeof(IntPtr);

                *(IntPtr*)method_list = CastOperations.ReinterpretAsIntPtr(new TysosArrayTypeMethod(ret, TysosArrayTypeMethod.MethodType.Address));
                *(IntPtr*)(OtherOperations.Add(method_list, ptr_size)) = CastOperations.ReinterpretAsIntPtr(new TysosArrayTypeMethod(ret, TysosArrayTypeMethod.MethodType.Get));
                *(IntPtr*)(OtherOperations.Add(method_list, OtherOperations.Mul(ptr_size, (IntPtr)2))) = CastOperations.ReinterpretAsIntPtr(new TysosArrayTypeMethod(ret, TysosArrayTypeMethod.MethodType.Set));
                *(IntPtr*)(OtherOperations.Add(method_list, OtherOperations.Mul(ptr_size, (IntPtr)3))) = CastOperations.ReinterpretAsIntPtr(new TysosArrayTypeMethod(ret, TysosArrayTypeMethod.MethodType.Ctor1));
                *(IntPtr*)(OtherOperations.Add(method_list, OtherOperations.Mul(ptr_size, (IntPtr)4))) = CastOperations.ReinterpretAsIntPtr(new TysosArrayTypeMethod(ret, TysosArrayTypeMethod.MethodType.Ctor2));
                *(IntPtr*)(OtherOperations.Add(method_list, OtherOperations.Mul(ptr_size, (IntPtr)5))) = new IntPtr(0);

                ret.Methods = method_list;
            }

            return ret;
        }

        unsafe int get_elem_size(Array array)
        {
            return *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetElemSizeOffset()));
        }

        unsafe int vector_index(Array array, int idx)
        {
            int* lobounds = (int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetLoboundsOffset()));
            return idx - *lobounds;            
        }

        [MethodAlias("__array_address")]
        internal unsafe UIntPtr Address(Array array, int idx)
        {
            int vindex = vector_index(array, idx);
            return OtherOperations.Add((UIntPtr)MemoryOperations.GetInternalArray(array), (UIntPtr)(vindex * get_elem_size(array)));
        }

        [MethodAlias("__array_get")]
        internal unsafe IntPtr Get(Array array, int idx)
        {
            UIntPtr address = Address(array, idx);
            switch (get_elem_size(array))
            {
                case 1:
                    return new IntPtr(MemoryOperations.PeekU1(address));
                case 2:
                    return new IntPtr(MemoryOperations.PeekU2(address));
                case 4:
                    return new IntPtr(MemoryOperations.PeekU4(address));
                case 8:
                    return new IntPtr((long)MemoryOperations.PeekU8(address));
                default:
                    throw new NotImplementedException("Array.Get: not implemented for element size " + idx.ToString());
            }
        }

        [MethodAlias("__array_set")]
        internal unsafe void Set(Array array, int idx, IntPtr val)
        {
            UIntPtr address = Address(array, idx);
            switch (get_elem_size(array))
            {
                case 1:
                    MemoryOperations.Poke(address, (byte)val);
                    break;
                case 2:
                    MemoryOperations.Poke(address, (ushort)val);
                    break;
                case 4:
                    MemoryOperations.Poke(address, (uint)val);
                    break;
                case 8:
                    MemoryOperations.Poke(address, (ulong)val);
                    break;
            }
        }

        [MethodAlias("__array_ctor_1")]
        internal unsafe void ctor1(Array array, int size)
        {
            *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetRankOffset())) = 1;
            *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetElemSizeOffset())) = elemtype.GetClassSize();
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetElemTypeOffset())) = CastOperations.ReinterpretAsUIntPtr(elemtype);

            UIntPtr lobounds = CastOperations.ReinterpretAsUIntPtr(MemoryOperations.GcMalloc(new IntPtr(4)));
            *(int*)lobounds = 0;
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetLoboundsOffset())) = lobounds;

            UIntPtr sizes = CastOperations.ReinterpretAsUIntPtr(MemoryOperations.GcMalloc(new IntPtr(4)));
            *(int*)sizes = size;
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetSizesOffset())) = sizes;

            *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetInnerArrayLengthOffset())) = size;

            UIntPtr inner_array = CastOperations.ReinterpretAsUIntPtr(MemoryOperations.GcMalloc(new IntPtr(size * elemtype.GetClassSize())));
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetInnerArrayOffset())) = inner_array;
        }

        [MethodAlias("__array_ctor_2")]
        internal unsafe void ctor2(Array array, int lobound, int size)
        {
            *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetRankOffset())) = 1;
            *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetElemSizeOffset())) = elemtype.GetClassSize();
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetElemTypeOffset())) = CastOperations.ReinterpretAsUIntPtr(elemtype);

            UIntPtr lobounds = CastOperations.ReinterpretAsUIntPtr(MemoryOperations.GcMalloc(new IntPtr(4)));
            *(int*)lobounds = lobound;
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetLoboundsOffset())) = lobounds;

            UIntPtr sizes = CastOperations.ReinterpretAsUIntPtr(MemoryOperations.GcMalloc(new IntPtr(4)));
            *(int*)sizes = size;
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetSizesOffset())) = sizes;

            *(int*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetInnerArrayLengthOffset())) = size;

            UIntPtr inner_array = CastOperations.ReinterpretAsUIntPtr(MemoryOperations.GcMalloc(new IntPtr(size * elemtype.GetClassSize())));
            *(UIntPtr*)(OtherOperations.Add(CastOperations.ReinterpretAsIntPtr(array), (IntPtr)ArrayOperations.GetInnerArrayOffset())) = inner_array;
        }

        class TysosArrayTypeMethod : TysosMethod
        {
            internal enum MethodType { Get, Set, Address, Ctor1, Ctor2 };

            internal MethodType _type;
            internal TysosArrayType _containing_type;

            public override object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
            {
                switch (_type)
                {
                    case MethodType.Address:
                        return _containing_type.Address((System.Array)obj, (int)parameters[0]);
                    case MethodType.Ctor1:
                        _containing_type.ctor1((System.Array)obj, (int)parameters[0]);
                        return null;
                    case MethodType.Ctor2:
                        _containing_type.ctor2((System.Array)obj, (int)parameters[0], (int)parameters[1]);
                        return null;
                    case MethodType.Get:
                        return _containing_type.Get((System.Array)obj, (int)parameters[0]);
                    case MethodType.Set:
                        throw new NotImplementedException("TysosArrayType.Invoke(Set)");
                        //_containing_type.Set((System.Array)obj, (int)parameters[0], );
                        //return null;
                    default:
                        throw new NotSupportedException("Invalid TysosArrayType._type");
                }
            }

            internal TysosArrayTypeMethod(TysosArrayType containing_type, MethodType type)
            {
                _type = type;
                _containing_type = containing_type;

                this.Flags = 0x400;
                this.ImplFlags = 0;
                this.OwningType = _containing_type;

                switch (_type)
                {
                    case MethodType.Address:
                        this._Name = "Address";
                        this.MethodAddress = OtherOperations.GetFunctionAddress("__array_address");
                        break;
                    case MethodType.Ctor1:
                        this._Name = ".ctor";
                        this.MethodAddress = OtherOperations.GetFunctionAddress("__array_ctor_1");
                        break;
                    case MethodType.Ctor2:
                        this._Name = ".ctor";
                        this.MethodAddress = OtherOperations.GetFunctionAddress("__array_ctor_2");
                        break;
                    case MethodType.Get:
                        this._Name = "Get";
                        this.MethodAddress = OtherOperations.GetFunctionAddress("__array_get");
                        break;
                    case MethodType.Set:
                        this._Name = "Set";
                        this.MethodAddress = OtherOperations.GetFunctionAddress("__array_set");
                        break;
                }
            }
        }
    }
}
