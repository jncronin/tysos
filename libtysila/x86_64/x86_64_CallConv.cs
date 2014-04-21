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
    class x86_64_CallConv : CallConv
    {
        public static CallConv isr(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass, ThreeAddressCode call_tac)
        {
            CallConv ret = new CallConv();

            ret.CallerCleansStack = true;
            ret.CallTac = ThreeAddressCode.Op.call_void;

            /* For an isr on the x86_64, the stack is either:
             * 
             * if no error code:
             * [rbp + 0]:       previous rbp
             * [rbp + 8]:       return address
             * [rbp + 16]:      return cs
             * [rbp + 24]:      saved rflags
             * 
             * if an error code:
             * [rbp + 0]:       previous rbp
             * [rbp + 8]:       error code
             * [rbp + 16]:      return address
             * [rbp + 24]:      return cs
             * [rbp + 32]:      saved rflags
             */

            Signature.Method m = mtc.msig.Method;

            if (m.Params.Count == 0)
                ret.Arguments = new List<ArgumentLocation>();
            else if (m.Params.Count == 1)
                ret.Arguments = new List<ArgumentLocation> { new ArgumentLocation { ValueLocation = new hardware_contentsof { base_loc = x86_64_Assembler.Rbp, const_offset = 8, size = 8 }, ValueSize = 8 } };
            else
                throw new Exception("No more than one argument is allowed on an ISR");

            ret.StackSpaceUsed = 0;
            ret.ReturnValue = null;

            return ret;
        }

        static hardware_location[] amd64_gprs = new hardware_location[] { x86_64_Assembler.Rdi, x86_64_Assembler.Rsi,
            x86_64_Assembler.Rdx, x86_64_Assembler.Rcx, x86_64_Assembler.R8, x86_64_Assembler.R9 };
        static hardware_location[] amd64_xmms = new hardware_location[] { x86_64_Assembler.Xmm0, x86_64_Assembler.Xmm1,
            x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3, x86_64_Assembler.Xmm4, x86_64_Assembler.Xmm5,
            x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7 };

        static ArgumentLocation amd64_argloc(var_semantic vs, int v_size, hardware_location base_reg, ref int stack_pos,
            ref int reg_pos, ref int xmm_pos, int pointer_size, Signature.Param p, Assembler ass)
        {
            if (vs.needs_integer)
            {
                if (reg_pos < amd64_gprs.Length)
                    return new ArgumentLocation { ValueLocation = amd64_gprs[reg_pos++], ValueSize = v_size };
                else
                {
                    ArgumentLocation ret = new ArgumentLocation
                    {
                        ValueLocation = new hardware_contentsof
                        {
                            base_loc = base_reg,
                            const_offset = stack_pos,
                            size = v_size
                        },
                        ValueSize = v_size
                    };
                    stack_pos += v_size;
                    stack_pos = util.align(stack_pos, pointer_size);
                    return ret;
                }
            }
            else if (vs.needs_float)
            {
                if (xmm_pos < amd64_xmms.Length)
                    return new ArgumentLocation { ValueLocation = amd64_xmms[xmm_pos++], ValueSize = v_size };
                else
                {
                    ArgumentLocation ret = new ArgumentLocation
                    {
                        ValueLocation = new hardware_contentsof
                        {
                            base_loc = base_reg,
                            const_offset = stack_pos,
                            size = v_size
                        },
                        ValueSize = v_size
                    };
                    stack_pos += v_size;
                    stack_pos = util.align(stack_pos, pointer_size);
                    return ret;
                }
            }
            else
                throw new Exception("Argument type " + Signature.GetString(p, ass) + " currently not supported by amd64 calling convention");
        }

        public static CallConv amd64(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass, ThreeAddressCode call_tac)
        {
            /* SysV AMD64 ABI (simplified)
             * 
             * integer/pointers in rdi, rsi, rdx, rcx, r8, r9
             * floats in xmm0 - xmm7
             * then stack space used
             * 
             * callee preserves rbx, rbp, r12-r15
             * 
             * We currently don't support other types (i.e. structs)
             */

            CallConv ret = new CallConv();

            ret.CallerCleansStack = true;
            if(call_tac != null)
                ret.CallTac = call_tac.Operator;

            ret.Arguments = new List<ArgumentLocation>();
            ret.ArgsInRegisters = true;
            ret.RequiredRegAlloc = Assembler.AssemblerOptions.RegisterAllocatorType.fastreg;

            //ret.CalleePreservesLocations = new hardware_location[] { x86_64_Assembler.Rbx, x86_64_Assembler.R12, x86_64_Assembler.R13,
            //    x86_64_Assembler.R14, x86_64_Assembler.R15 };
            ret.CalleePreservesLocations.Add(x86_64_Assembler.Rbx);
            ret.CalleePreservesLocations.Add(x86_64_Assembler.R12);
            ret.CalleePreservesLocations.Add(x86_64_Assembler.R13);
            ret.CalleePreservesLocations.Add(x86_64_Assembler.R14);
            ret.CalleePreservesLocations.Add(x86_64_Assembler.R15);

            int stack_pos = 0;
            int pointer_size = 8;
            int reg_pos = 0;
            int xmm_pos = 0;

            if (((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586)
                throw new Exception("Cannot use amd64 calling convention in i586 mode");

            hardware_location base_reg = x86_64_Assembler.Rsp;
            if (pov == StackPOV.Callee)
            {
                stack_pos = 16;
                base_reg = x86_64_Assembler.Rbp;
            }

            Signature.Method m = mtc.msig.Method;

            if (m.HasThis && !m.ExplicitThis)
            {
                ArgumentLocation al = new ArgumentLocation { ValueLocation = amd64_gprs[reg_pos++], ValueSize = pointer_size };

                if ((mtc.type != null) && mtc.type.IsValueType(ass) && !(mtc.tsig is Signature.BoxedType))
                    al.ExpectsVTRef = true;

                ret.Arguments.Add(al);
            }

            foreach (Signature.Param p in m.Params)
            {
                int v_size = ass.GetSizeOf(p);
                if (v_size < 8)
                    v_size = 8;

                var_semantic vs = ass.GetSemantic(p.CliType(ass), v_size);
                if (vs.needs_integer)
                    ret.Arguments.Add(amd64_argloc(vs, v_size, base_reg, ref stack_pos, ref reg_pos, ref xmm_pos, pointer_size, p, ass));
            }

            if (call_tac != null)
            {
                if (call_tac.Operator == ThreeAddressCode.Op.call_void)
                    ret.ReturnValue = null;
                else
                {
                    var_semantic ret_vs = call_tac.GetResultSemantic(ass);

                    if (ret_vs.needs_integer)
                        ret.ReturnValue = x86_64_Assembler.Rax;
                    else if (ret_vs.needs_float)
                        ret.ReturnValue = x86_64_Assembler.Xmm0;
                    else
                        throw new Exception("Return type " + Signature.GetString(m.RetType, ass) + " currently not supported by amd64 calling convention");
                }
            }

            ret.StackSpaceUsed = stack_pos;

            return ret;
        }

        public static CallConv tcdecl(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass, ThreeAddressCode call_tac)
        {
            CallConv ret = new CallConv();

            ret.CallerCleansStack = true;

            if(call_tac != null)
                ret.CallTac = call_tac.Operator;

            /* On the x86_64 stack, arguments are 8 byte aligned
             * 
             * From the function's point of view (pov == Callee), layout is:
             * 
             * [rbp + 0]:       previous ebp
             * [rbp + 8]:       return address
             * [rbp + 16]:      argument 0
             * etc
             * 
             * Value types are passed by value on the stack
             */

            ret.Arguments = new List<ArgumentLocation>();

            int stack_pos = 0;
            int pointer_size = 8;

            bool i586 = false;
            if (((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586)
                i586 = true;

            if (i586)
                pointer_size = 4;
            
            hardware_location base_reg = x86_64_Assembler.Rsp;
            if (pov == CallConv.StackPOV.Callee)
            {
                stack_pos = 16;
                if (i586)
                    stack_pos = 8;
                base_reg = x86_64_Assembler.Rbp;
            }

            Signature.Method m = null;
            if (mtc.msig is Signature.Method)
                m = mtc.msig as Signature.Method;
            else if (mtc.msig is Signature.GenericMethod)
                m = ((Signature.GenericMethod)mtc.msig).GenMethod;
            else
                throw new NotSupportedException();

            if (m.HasThis && !m.ExplicitThis)
            {
                ArgumentLocation al = new ArgumentLocation { ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = pointer_size }, ValueSize = pointer_size };

                if ((mtc.type != null) && mtc.type.IsValueType(ass) && !(mtc.tsig is Signature.BoxedType))
                    al.ExpectsVTRef = true;

                ret.Arguments.Add(al);
                stack_pos += pointer_size;
            }

            foreach (Signature.Param p in m.Params)
            {
                int v_size = ass.GetSizeOf(p);
                
                if (!i586 && (v_size < 8))
                    v_size = 8;
                if (i586 && (v_size < 4))
                    v_size = 4;
                ret.Arguments.Add(new ArgumentLocation { ValueSize = v_size, ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = v_size } });

                /*
                if (v_size > pointer_size)
                {
                    ArgumentLocation vtype_loc = new ArgumentLocation { ReferenceLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = pointer_size }, ValueSize = v_size };
                    ret.Arguments.Add(vtype_loc);
                    vtype_locs.Add(vtype_loc);
                    v_size = pointer_size;
                }
                else
                    ret.Arguments.Add(new ArgumentLocation { ValueSize = v_size, ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = pointer_size } }); */

                stack_pos += v_size;
                stack_pos = util.align(stack_pos, pointer_size);
            }

            /*foreach (ArgumentLocation vtype_loc in vtype_locs)
            {
                vtype_loc.ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = vtype_loc.ValueSize };
                stack_pos += vtype_loc.ValueSize;
                stack_pos = util.align(stack_pos, pointer_size);
            }*/

            if (call_tac != null)
            {
                if (call_tac.Operator == ThreeAddressCode.Op.call_void)
                    ret.ReturnValue = null;
                else
                {
                    var_semantic ret_vs = call_tac.GetResultSemantic(ass);

                    if (ret_vs.needs_integer)
                        ret.ReturnValue = x86_64_Assembler.Rax;
                    else if (ret_vs.needs_float)
                        ret.ReturnValue = x86_64_Assembler.Xmm0;
                    else
                    {
                        int r_size = ass.GetSizeOf(m.RetType);
                        ret.ReturnValue = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = r_size };
                        stack_pos += r_size;
                        stack_pos = util.align(stack_pos, pointer_size);
                    }
                }
            }
            else
            {
                if (Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.Void), ass))
                    ret.ReturnValue = null;
                else if (Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.I), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.I1), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.I2), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.I4), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.I8), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.U), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.U1), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.U2), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.U4), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.U8), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.Boolean), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.Byte), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.Char), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.Object), ass) ||
                    Signature.ParamCompare(m.RetType, new Signature.Param(BaseType_Type.String), ass))
                    ret.ReturnValue = x86_64_Assembler.Rax;
            }

            ret.StackSpaceUsed = stack_pos;

            return ret;
        }
    }

    partial class x86_64_Assembler
    {
        protected override void arch_init_callconvs()
        {
            call_convs.Clear();
            call_convs.Add("cdecl", x86_64_CallConv.tcdecl);
            call_convs.Add("tcdecl", x86_64_CallConv.tcdecl);
            call_convs.Add("default", x86_64_CallConv.tcdecl);
            call_convs.Add("isr", x86_64_CallConv.isr);
            call_convs.Add("amd64", x86_64_CallConv.amd64);

            if (ia == IA.x86_64)
            {
                call_convs.Add("gnu", x86_64_CallConv.amd64);
                call_convs.Add("gcc", x86_64_CallConv.amd64);
            }
        }

        internal CallConv callconv_conv_u4_r8
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R8), new List<Signature.Param> { new Signature.Param(BaseType_Type.U4) }, ThreeAddressCode.Op.call_r8);
            }
        }
        internal CallConv callconv_conv_u8_r8
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R8), new List<Signature.Param> { new Signature.Param(BaseType_Type.U8) }, ThreeAddressCode.Op.call_r8);
            }
        }
    }
}
