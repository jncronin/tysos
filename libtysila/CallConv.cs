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
            public Signature.Param Type;
        }

        public bool CallerCleansStack = true;

        public string Name;

        public override string ToString()
        {
            if (Name != null)
                return Name;
            return base.ToString();
        }

        //public hardware_location[] CalleePreservesLocations = new hardware_location[] { };
        public List<hardware_location> CalleePreservesLocations = new List<hardware_location>();
        public List<hardware_location> CalleeAlwaysSavesLocations = new List<hardware_location>();
        public List<hardware_location> CallerPreservesLocations = new List<hardware_location>();

        public bool ArgsInRegisters = false;
        public Assembler.AssemblerOptions.RegisterAllocatorType? RequiredRegAlloc;

        public hardware_location ReturnValue;

        public List<ArgumentLocation> Arguments;
        public hardware_location HiddenRetValArgument;

        public int StackSpaceUsed = 0;

        public enum StackPOV { Caller, Callee };

        public Signature.BaseMethod MethodSig;

        public delegate CallConv GetCallConv(Assembler.MethodToCompile meth, StackPOV pov, Assembler ass);
    }

    partial class Assembler
    {
        Dictionary<string, CallConv.GetCallConv> _call_convs = null;
        public Dictionary<string, CallConv.GetCallConv> call_convs
        {
            get
            {
                if (_call_convs == null)
                {
                    _call_convs = new Dictionary<string, CallConv.GetCallConv>();
                    arch_init_callconvs();
                }
                return _call_convs;
            }
        }

        protected abstract void arch_init_callconvs();

        internal CallConv MakeStaticCall(string call_conv, Signature.Param rettype, List<Signature.Param> paramlist, ThreeAddressCode.Op call_tac)
        {
            bool returns = true;
            if (rettype == null)
            {
                returns = false;
                rettype = new Signature.Param(BaseType_Type.Void);
            }
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
                    RetType = rettype,
                    Returns = returns
                }
            }, CallConv.StackPOV.Caller, this);
        }

        internal CallConv callconv_gcmalloc
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I), new List<Signature.Param> { new Signature.Param(BaseType_Type.I) }, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_q_qq
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I8), new List<Signature.Param> { new Signature.Param(BaseType_Type.I8), new Signature.Param(BaseType_Type.I8) }, ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_q_q
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I8), new List<Signature.Param> { new Signature.Param(BaseType_Type.I8) }, ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_s_q
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I8) }, ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_s_s
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R4), new List<Signature.Param> { new Signature.Param(BaseType_Type.R4) }, ThreeAddressCode.Op.OpR4(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_d_d
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R8), new List<Signature.Param> { new Signature.Param(BaseType_Type.R8) }, ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_d_q
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R8), new List<Signature.Param> { new Signature.Param(BaseType_Type.I8) }, ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_numop_l_ll
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call));
            }
        }

        internal Signature.Method msig_gcmalloc
        {
            get
            {
                return new Signature.Method { ParamCount = 1, Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.I) }, RetType = new Signature.Param(BaseType_Type.I) };
            }
        }

        internal CallConv callconv_throw
        {
            get
            {
                return MakeStaticCall("default", null, new List<Signature.Param> { new Signature.Param(BaseType_Type.Object) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal Signature.Method msig_throw
        {
            get
            {
                return new Signature.Method { ParamCount = 1, Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.Object) }, RetType = new Signature.Param(BaseType_Type.Void), Returns = false };
            }
        }

        internal CallConv callconv_sthrow
        {
            get
            {
                return MakeStaticCall("default", null, new List<Signature.Param> { new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_castclassex
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I) }, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.call));
            }
        }

        internal Signature.Method msig_castclassex
        {
            get
            {
                return new Signature.Method { ParamCount = 2, Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I) }, RetType = new Signature.Param(BaseType_Type.I) };
            }
        }

        internal CallConv callconv_profile
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.String), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_cctor
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param>(), ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_memcpy
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
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
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_memset
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_wmemset
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_mbstrlen
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I) }, ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_strlen
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I) }, ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_mbstowcs
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_null
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_getobjid
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { }, ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call));
            }
        }

        internal Signature.Method msig_getobjid
        {
            get
            {
                return new Signature.Method { ParamCount = 0, Params = new List<Signature.Param> { }, RetType = new Signature.Param(BaseType_Type.I4) };
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
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.I4), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_release
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Void), new List<Signature.Param> { new Signature.Param(BaseType_Type.I), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call));
            }
        }

        internal CallConv callconv_getvalueimpl
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.Object), new List<Signature.Param> { new Signature.Param(BaseType_Type.Object), new Signature.Param(BaseType_Type.I4) }, ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.call));
            }
        }
    }
}
