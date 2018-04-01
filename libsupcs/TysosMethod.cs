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

/* This defines the TysosMethod which is a subtype of System.Reflection.MethodInfo
 * 
 * All MethodInfo structures produced by tysila2 follow this layout
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace libsupcs
{
    [ExtendsOverride("_ZW19System#2EReflection17RuntimeMethodInfo")]
    [VTableAlias("__tysos_method_vt")]
    public unsafe class TysosMethod : System.Reflection.MethodInfo
    {
        public TysosType OwningType;
        public TysosType _ReturnType;
        [NullTerminatedListOf(typeof(TysosType))]
        public IntPtr _Params;
        public string _Name;
        public string _MangledName;
        public IntPtr Signature;
        public IntPtr Sig_references;
        public Int32 Flags;
        public Int32 ImplFlags;
        public UInt32 TysosFlags;
        public void* MethodAddress;
        [NullTerminatedListOf(typeof(EHClause))]
        public IntPtr EHClauses;
        public IntPtr Instructions;

        public const string PureVirtualName = "__cxa_pure_virtual";

        metadata.MethodSpec mspec;

        public TysosMethod(metadata.MethodSpec ms, TysosType owning_type)
        {
            mspec = ms;
            OwningType = owning_type;
        }

        public const UInt32 TF_X86_ISR = 0x10000001;
        public const UInt32 TF_X86_ISREC = 0x10000002;

        public const UInt32 TF_CC_STANDARD = 1;
        public const UInt32 TF_CC_VARARGS = 2;
        public const UInt32 TF_CC_HASTHIS = 32;
        public const UInt32 TF_CC_EXPLICITTHIS = 64;
        public const UInt32 TF_CC_MASK = 0x7f;

        [VTableAlias("__tysos_ehclause_vt")]
        public class EHClause
        {
            public IntPtr TryStart;
            public IntPtr TryEnd;
            public IntPtr Handler;
            public TysosType CatchObject;
            public Int32 Flags;

            public bool IsFinally { get { if ((Flags & 0x2) == 0x2) return true; return false; } }
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        //[MethodReferenceAlias("__invoke")]
        //static extern object InternalInvoke(IntPtr meth, Object[] parameters, TysosType rettype);

        public override System.Reflection.CallingConventions CallingConvention
        {
            get
            {
                return (System.Reflection.CallingConventions)(int)(TysosFlags & TF_CC_MASK);
            }
        }

        public override System.Reflection.MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }

        public override System.Reflection.ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { throw new NotImplementedException(); }
        }

        public override System.Reflection.MethodAttributes Attributes
        {
            get
            {
                return (System.Reflection.MethodAttributes)mspec.m.GetIntEntry(metadata.MetadataStream.tid_MethodDef, mspec.mdrow, 2);
            }
        }

        public override System.Reflection.MethodImplAttributes GetMethodImplementationFlags()
        {
            return (System.Reflection.MethodImplAttributes)mspec.m.GetIntEntry(metadata.MetadataStream.tid_MethodDef, mspec.mdrow, 1);
        }

        public unsafe override System.Reflection.ParameterInfo[] GetParameters()
        {
            // count the number of params
            if (_Params == (IntPtr)0)
                return new System.Reflection.ParameterInfo[] { };
            int count = 0;
            IntPtr* cur_param = (IntPtr*)_Params;
            while (*cur_param != (IntPtr)0)
            {
                count++;
                cur_param++;
            }

            // build the returned array
            System.Reflection.ParameterInfo[] ret = new System.Reflection.ParameterInfo[count];
            cur_param = (IntPtr*)_Params;
            count = 0;
            while (*cur_param != (IntPtr)0)
            {
                TysosParameterInfo pi = new TysosParameterInfo(TysosType.ReinterpretAsType(*cur_param), count, this);
                ret[count] = pi;
                count++;
                cur_param++;
            }
            return ret;
        }

        public override Type ReturnType
        {
            get
            {
                return _ReturnType;
            }
        }

        public override object Invoke(object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, object[] parameters, System.Globalization.CultureInfo culture)
        {

            if (MethodAddress == null)
            {
                var mangled_name = mspec.MangleMethod();
                System.Diagnostics.Debugger.Log(0, "libsupcs", "TysosMethod.Invoke: requesting run-time address for " + mangled_name);
                MethodAddress = JitOperations.GetAddressOfObject(mspec.MangleMethod());
            }
            if (MethodAddress == null)
                throw new System.Reflection.TargetException("Method does not have a defined implementation (" + OwningType.FullName + "." + Name + "())");
            if (!IsStatic && (obj == null))
                throw new System.Reflection.TargetException("Instance method and obj is null (" + OwningType.FullName + "." + Name + "())");

            // TODO: check number and type of parameters

            // Build a new params array to include obj
            int p_length = 0;
            if (parameters != null)
                p_length = parameters.Length;

            object[] p;
            if (!IsStatic)
            {
                p = new object[p_length + 1];
                p[0] = obj;
                for (int i = 0; i < p_length; i++)
                    p[i + 1] = parameters[i];
                p_length++;
            }
            else
                p = parameters;

            unsafe
            {
                //return InternalInvoke(MethodAddress, p_length, (p == null) ? null : MemoryOperations.GetInternalArray(p));
            }

            throw new NotImplementedException();
            //return InternalInvoke(MethodAddress, p, this._ReturnType);
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        public override Type DeclaringType
        {
            get { return OwningType; }
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
            throw new NotImplementedException();
        }

        public override string Name
        {
            get
            {
                if(_Name == null)
                {
                    string name = mspec.m.GetStringEntry(metadata.MetadataStream.tid_MethodDef, mspec.mdrow, 3);
                    System.Threading.Interlocked.CompareExchange(ref _Name, name, null);
                }
                return _Name;
            }
        }

        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class TysosParameterInfo : System.Reflection.ParameterInfo
    {
        internal TysosParameterInfo(Type param_type, int param_no, TysosMethod decl_method)
        {
            /* This is a basic implementation: tysila does not currently provide either the
             * parameter name or its attributes in the _Params list */

            this.ClassImpl = param_type;
            this.PositionImpl = param_no;
            this.NameImpl = param_no.ToString();
            this.MemberImpl = decl_method;
            this.DefaultValueImpl = null;
            this.AttrsImpl = System.Reflection.ParameterAttributes.None;
        }
    }
}
