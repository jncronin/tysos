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
        static List<hardware_location> cdecl_i386_preserves_with_rax = new List<hardware_location> {
            x86_64_Assembler.Rax, x86_64_Assembler.Rcx, x86_64_Assembler.Rdx, x86_64_Assembler.Xmm0,
            x86_64_Assembler.Xmm1, x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3, x86_64_Assembler.Xmm4,
            x86_64_Assembler.Xmm5, x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7
        };
        static List<hardware_location> cdecl_i386_preserves = new List<hardware_location> {
            x86_64_Assembler.Rcx, x86_64_Assembler.Rdx, x86_64_Assembler.Xmm0,
            x86_64_Assembler.Xmm1, x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3, x86_64_Assembler.Xmm4,
            x86_64_Assembler.Xmm5, x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7
        };
        static List<hardware_location> isr_i386_preserves = new List<hardware_location> {
            x86_64_Assembler.Rax, x86_64_Assembler.Rbx, x86_64_Assembler.Rcx, x86_64_Assembler.Rdx,
            x86_64_Assembler.Rsi, x86_64_Assembler.Rdi,
            x86_64_Assembler.Xmm0, x86_64_Assembler.Xmm1, x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3,
            x86_64_Assembler.Xmm4, x86_64_Assembler.Xmm5, x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7
        };
        static List<hardware_location> cdecl_x86_64_preserves_with_rax = new List<hardware_location> {
            x86_64_Assembler.Rax, x86_64_Assembler.Rcx, x86_64_Assembler.Rdx, x86_64_Assembler.Xmm0,
            x86_64_Assembler.Xmm1, x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3, x86_64_Assembler.Xmm4,
            x86_64_Assembler.Xmm5, x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7,
            x86_64_Assembler.Xmm8, x86_64_Assembler.Xmm9, x86_64_Assembler.Xmm10, x86_64_Assembler.Xmm11,
            x86_64_Assembler.Xmm12, x86_64_Assembler.Xmm13, x86_64_Assembler.Xmm14, x86_64_Assembler.Xmm15,
            x86_64_Assembler.R8, x86_64_Assembler.R9, x86_64_Assembler.R10, x86_64_Assembler.R11,
            x86_64_Assembler.R12, x86_64_Assembler.R13, x86_64_Assembler.R14, x86_64_Assembler.R15
        };
        static List<hardware_location> cdecl_x86_64_preserves = new List<hardware_location> {
            x86_64_Assembler.Rcx, x86_64_Assembler.Rdx, x86_64_Assembler.Xmm0,
            x86_64_Assembler.Xmm1, x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3, x86_64_Assembler.Xmm4,
            x86_64_Assembler.Xmm5, x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7,
            x86_64_Assembler.Xmm8, x86_64_Assembler.Xmm9, x86_64_Assembler.Xmm10, x86_64_Assembler.Xmm11,
            x86_64_Assembler.Xmm12, x86_64_Assembler.Xmm13, x86_64_Assembler.Xmm14, x86_64_Assembler.Xmm15,
            x86_64_Assembler.R8, x86_64_Assembler.R9, x86_64_Assembler.R10, x86_64_Assembler.R11,
            x86_64_Assembler.R12, x86_64_Assembler.R13, x86_64_Assembler.R14, x86_64_Assembler.R15
        };
        static List<hardware_location> isr_x86_64_preserves = new List<hardware_location> {
            x86_64_Assembler.Rax, x86_64_Assembler.Rbx, x86_64_Assembler.Rcx, x86_64_Assembler.Rdx,
            x86_64_Assembler.Rsi, x86_64_Assembler.Rdi,
            x86_64_Assembler.R8, x86_64_Assembler.R9, x86_64_Assembler.R10, x86_64_Assembler.R11,
            x86_64_Assembler.R12, x86_64_Assembler.R13, x86_64_Assembler.R14, x86_64_Assembler.R15,
            x86_64_Assembler.Xmm0, x86_64_Assembler.Xmm1, x86_64_Assembler.Xmm2, x86_64_Assembler.Xmm3,
            x86_64_Assembler.Xmm4, x86_64_Assembler.Xmm5, x86_64_Assembler.Xmm6, x86_64_Assembler.Xmm7,
            x86_64_Assembler.Xmm8, x86_64_Assembler.Xmm9, x86_64_Assembler.Xmm10, x86_64_Assembler.Xmm11,
            x86_64_Assembler.Xmm12, x86_64_Assembler.Xmm13, x86_64_Assembler.Xmm14, x86_64_Assembler.Xmm15
        };
        static List<hardware_location> cdecl_callee_preserves = new List<hardware_location> {
            x86_64_Assembler.Rbx, x86_64_Assembler.Rdi, x86_64_Assembler.Rsi };
    
        public static CallConv isr2(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass)
        {
            CallConv ret = new CallConv();

            ret.CallerCleansStack = true;

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
            {
                int sz = ass.GetSizeOfPointer();
                ret.Arguments = new List<ArgumentLocation> { new ArgumentLocation { ValueLocation = new hardware_contentsof { base_loc = x86_64_Assembler.Rbp, const_offset = sz, size = sz }, ValueSize = sz, Type = new Signature.Param(BaseType_Type.I) } };
            }
            else
                throw new Exception("No more than one argument is allowed on an ISR");

            ret.StackSpaceUsed = 0;
            ret.ReturnValue = null;
            ret.MethodSig = mtc.msig;

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
            if (vs.needs_virtftnptr)
            {
                if (ass.GetSizeOf(new Signature.Param(Assembler.CliType.virtftnptr)) == 8)
                    vs.needs_int64 = true;
                else if (ass.GetSizeOf(new Signature.Param(Assembler.CliType.virtftnptr)) == 4)
                    vs.needs_int32 = true;
                else
                    vs.needs_vtype = true;
            }            

            if (vs.needs_integer)
            {
                if (reg_pos < amd64_gprs.Length)
                    return new ArgumentLocation { ValueLocation = amd64_gprs[reg_pos++], ValueSize = v_size, Type = p };
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
                        ValueSize = v_size,
                        Type = p
                    };
                    stack_pos += v_size;
                    stack_pos = util.align(stack_pos, pointer_size);
                    return ret;
                }
            }
            else if (vs.needs_float)
            {
                if (xmm_pos < amd64_xmms.Length)
                    return new ArgumentLocation { ValueLocation = amd64_xmms[xmm_pos++], ValueSize = v_size, Type = p };
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
                        ValueSize = v_size,
                        Type = p
                    };
                    stack_pos += v_size;
                    stack_pos = util.align(stack_pos, pointer_size);
                    return ret;
                }
            }
            else if(vs.needs_vtype)
            {
                ArgumentLocation ret = new ArgumentLocation { ValueSize = v_size, 
                    ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = v_size }, Type = p };
                stack_pos += v_size;
                stack_pos = util.align(stack_pos, pointer_size);
                return ret;
            }
            else
                throw new Exception("Argument type " + Signature.GetString(p, ass) + " currently not supported by amd64 calling convention");
        }

        public static CallConv isr(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass)
        {
            CallConv ret = sysv_i386(mtc, pov, ass, true);
            ret.Name = "isr";

            bool i586 = false;
            if (((x86_64_Assembler)ass).ia == x86_64_Assembler.IA.i586)
                i586 = true;

            ret.CalleePreservesLocations = i586 ? isr_i386_preserves : isr_x86_64_preserves;
            ret.CalleeAlwaysSavesLocations = i586 ? isr_i386_preserves : isr_x86_64_preserves;

            //if (mtc.msig.Method.Params.Count > 1)
            //    throw new Exception("ISRs can only have zero or one arguments");
            if (mtc.msig.Method.RetType != null && !((mtc.msig.Method.RetType.Type is Signature.BaseType) && 
                (((Signature.BaseType)mtc.msig.Method.RetType.Type).Type == BaseType_Type.Void)))
                throw new Exception("ISRs cannot return a value");

            return ret;
        }

        public static CallConv amd64(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass)
        {
            /* SysV AMD64 ABI (simplified)
             * 
             * integer/pointers in rdi, rsi, rdx, rcx, r8, r9
             * floats in xmm0 - xmm7
             * then stack space used
             * 
             * callee preserves rbx, rbp, r12-r15, xmm0-15
             * 
             * We currently don't support other types (i.e. structs)
             */

            CallConv ret = new CallConv();

            ret.Name = "amd64";

            ret.CallerCleansStack = true;

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

            ret.CallerPreservesLocations.Add(x86_64_Assembler.Rax);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Rcx);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Rdx);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Rsi);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Rdi);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.R8);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.R9);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.R10);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.R11);

            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm0);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm1);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm2);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm3);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm4);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm5);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm6);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm7);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm8);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm9);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm10);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm11);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm12);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm13);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm14);
            ret.CallerPreservesLocations.Add(x86_64_Assembler.Xmm15);

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

            if (m.RetType == null)
                ret.ReturnValue = null;
            else if (m.RetType.CliType(ass) == Assembler.CliType.none)
                ret.ReturnValue = null;
            else if (m.RetType.CliType(ass) == Assembler.CliType.void_)
                ret.ReturnValue = null;
            else
            {
                var_semantic ret_vs = ass.GetSemantic(m.RetType.CliType(ass), ass.GetSizeOf(m.RetType));

                if (ret_vs.needs_integer)
                    ret.ReturnValue = x86_64_Assembler.Rax;
                else if (ret_vs.needs_float)
                    ret.ReturnValue = x86_64_Assembler.Xmm0;
                else if (ret_vs.needs_vtype)
                    ret.HiddenRetValArgument = amd64_gprs[reg_pos++];
                else
                    throw new Exception("Return type " + Signature.GetString(m.RetType, ass) + " currently not supported by amd64 calling convention");
            }

            if (m.HasThis && !m.ExplicitThis)
            {
                Signature.Param this_ptr = mtc.tsigp;
                if (mtc.type.IsValueType(ass))
                {
                    if (!(mtc.tsig is Signature.BoxedType) && !(mtc.tsig is Signature.ManagedPointer))
                        this_ptr = new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = mtc.tsig }, ass);
                }
                ArgumentLocation al = new ArgumentLocation { ValueLocation = amd64_gprs[reg_pos++], ValueSize = pointer_size, Type = this_ptr };

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
                ret.Arguments.Add(amd64_argloc(vs, v_size, base_reg, ref stack_pos, ref reg_pos, ref xmm_pos, pointer_size, p, ass));
            }

            ret.StackSpaceUsed = stack_pos;
            ret.MethodSig = mtc.msig;

            return ret;
        }

        public static CallConv sysv_i386(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass)
        { return sysv_i386(mtc, pov, ass, false); }

        public static CallConv sysv_i386(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass, bool is_isr)
        {
            CallConv ret = new CallConv();

            ret.Name = "sysv_i386";

            ret.CallerCleansStack = true;

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

                if (is_isr)
                    stack_pos -= pointer_size;
                
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
                Signature.Param this_ptr = mtc.tsigp;
                if (mtc.type.IsValueType(ass))
                {
                    if (!(mtc.tsig is Signature.BoxedType) && !(mtc.tsig is Signature.ManagedPointer))
                        this_ptr = new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = mtc.tsig }, ass);
                }

                ArgumentLocation al = new ArgumentLocation { ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = pointer_size }, ValueSize = pointer_size, Type = this_ptr };

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
                ret.Arguments.Add(new ArgumentLocation { ValueSize = v_size, ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = v_size }, Type = p });

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

            if (m.RetType == null)
                ret.ReturnValue = null;
            else if (m.RetType.CliType(ass) == Assembler.CliType.none)
                ret.ReturnValue = null;
            else if (m.RetType.CliType(ass) == Assembler.CliType.void_)
                ret.ReturnValue = null;
            else
            {
                var_semantic ret_vs = ass.GetSemantic(m.RetType.CliType(ass), ass.GetSizeOf(m.RetType));

                if (ret_vs.needs_integer)
                {
                    if (ret_vs.needs_int64 && ass.GetBitness() == Assembler.Bitness.Bits32)
                        ret.ReturnValue = new multiple_hardware_location(x86_64_Assembler.Rax, x86_64_Assembler.Rdx);
                    else
                        ret.ReturnValue = x86_64_Assembler.Rax;
                }
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

            ret.StackSpaceUsed = stack_pos;

            if (i586)
            {
                if ((ret.ReturnValue != null) && (ret.ReturnValue.Equals(x86_64_Assembler.Rax)))
                    ret.CallerPreservesLocations = cdecl_i386_preserves;
                else
                    ret.CallerPreservesLocations = cdecl_i386_preserves_with_rax;
            }
            else
            {
                if ((ret.ReturnValue != null) && (ret.ReturnValue.Equals(x86_64_Assembler.Rax)))
                    ret.CallerPreservesLocations = cdecl_x86_64_preserves;
                else
                    ret.CallerPreservesLocations = cdecl_x86_64_preserves_with_rax;
            }
            ret.CalleePreservesLocations = cdecl_callee_preserves;

            ret.MethodSig = mtc.msig;

            return ret;
        }
    }

    partial class x86_64_Assembler
    {
        protected override void arch_init_callconvs()
        {
            call_convs.Clear();
            call_convs.Add("cdecl", x86_64_CallConv.sysv_i386);
            call_convs.Add("tcdecl", x86_64_CallConv.sysv_i386);
            call_convs.Add("isr", x86_64_CallConv.isr);
            call_convs.Add("amd64", x86_64_CallConv.amd64);
            call_convs.Add("sysv", x86_64_CallConv.sysv_i386);
            call_convs.Add("default", x86_64_CallConv.sysv_i386);

            if (ia == IA.x86_64)
            {
                call_convs["default"] = x86_64_CallConv.amd64;
                call_convs["sysv"] = x86_64_CallConv.amd64;
                call_convs.Add("gnu", x86_64_CallConv.amd64);
                call_convs.Add("gcc", x86_64_CallConv.amd64);
            }
        }

        internal CallConv callconv_conv_u4_r8
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R8), new List<Signature.Param> { new Signature.Param(BaseType_Type.U4) }, ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.call));
            }
        }
        internal CallConv callconv_conv_u8_r8
        {
            get
            {
                return MakeStaticCall("default", new Signature.Param(BaseType_Type.R8), new List<Signature.Param> { new Signature.Param(BaseType_Type.U8) }, ThreeAddressCode.Op.OpR8(ThreeAddressCode.OpName.call));
            }
        }
    }
}
