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

using System;
using System.Collections.Generic;
using System.Text;
using libasm;

namespace libtysila
{
    public class CallConv
    {
        public class ArgumentLocation
        {
            public hardware_location ReferenceLocation;
            public hardware_location ValueLocation;
            public int ValueSize;
            public bool ExpectsVTRef = false;
        }

        public bool CallerCleansStack = true;

        //public hardware_location[] CalleePreservesLocations = new hardware_location[] { };
        public List<hardware_location> CalleePreservesLocations = new List<hardware_location>();

        public bool ArgsInRegisters = false;
        public Assembler.AssemblerOptions.RegisterAllocatorType? RequiredRegAlloc;

        public hardware_location ReturnValue;

        public List<ArgumentLocation> Arguments;

        public int StackSpaceUsed = 0;

        public enum StackPOV { Caller, Callee };

        public ThreeAddressCode.Op CallTac;

        public delegate CallConv GetCallConv(Assembler.MethodToCompile meth, StackPOV pov, Assembler ass, ThreeAddressCode call_tac);
    }

    partial class Assembler
    {
        public Dictionary<string, CallConv.GetCallConv> call_convs = new Dictionary<string, CallConv.GetCallConv>(new GenericEqualityComparer<string>());

        protected abstract void arch_init_callconvs();

        internal CallConv MakeStaticCall(string call_conv, Signature.Param rettype, List<Signature.Param> paramlist, ThreeAddressCode.Op call_tac)
        {
            return call_convs[call_conv](new MethodToCompile
            {
                _ass = this,
                meth = null,
                tsigp = null,
                type = null,
                msig = new Signature.Method
                {
                    CallingConvention = Signature.Method.CallConv.Default,
                    ExplicitThis = false,
                    HasThis = false,
                    GenParamCount = 0,
                    Params = paramlist,
                    ParamCount = paramlist.Count,
                    RetType = rettype
                }
            }, CallConv.StackPOV.Caller, this, new ThreeAddressCode(call_tac));
        }

        internal CallConv callconv_gcmalloc
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I), new List<Signature.Param> { new Signature.Param(BaseType_Type.I) }, ThreeAddressCode.Op.call_i);
            }
        }

        internal CallConv callconv_throw
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.Object), new Signature.Param(BaseType_Type.Object) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_sthrow
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.Object) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_castclassex
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I) }, ThreeAddressCode.Op.call_i);
            }
        }

        internal CallConv callconv_profile
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.String) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_cctor
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param>(), ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_memcpy
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_memmove
        {
            get
            {
                return callconv_memcpy;
            }
        }

        internal CallConv callconv_memcmp
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_i4);
            }
        }

        internal CallConv callconv_memset
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_memsetw
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_mbstrlen
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_i4);
            }
        }

        internal CallConv callconv_mbstowcs
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_getobjid
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { }, ThreeAddressCode.Op.call_i4);
            }
        }

        internal CallConv callconv_getcurthreadid
        {
            get
            {
                return callconv_getobjid;
            }
        }

        internal CallConv callconv_try_acquire
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_i4);
            }
        }

        internal CallConv callconv_release
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_void);
            }
        }

        internal CallConv callconv_getvalueimpl
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Object), new List<Signature.Param> { new Signature.Param(BaseType_Type.Object), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.call_i);
            }
        }
    }
}
