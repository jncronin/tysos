/* Copyright (C) 2013 by John Cronin
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

namespace libtysila.arm
{
    class arm_CallConv : CallConv
    {
        public static CallConv tyeabi(Assembler.MethodToCompile mtc, StackPOV pov, Assembler ass, ThreeAddressCode call_tac)
        {
            CallConv ret = new CallConv();

            ret.CallerCleansStack = true;
            if(call_tac != null)
                ret.CallTac = call_tac.Operator;
            ret.Arguments = new List<ArgumentLocation>();
            ret.ArgsInRegisters = true;

            int stack_pos = 0;
            int pointer_size = 4;
            int ncrn = 0;

            Signature.Method m = null;
            if (mtc.msig is Signature.Method)
                m = mtc.msig as Signature.Method;
            else if (mtc.msig is Signature.GenericMethod)
                m = ((Signature.GenericMethod)mtc.msig).GenMethod;
            else
                throw new NotSupportedException();

            hardware_location base_reg = arm_Assembler.SP;
            if (pov == CallConv.StackPOV.Callee)
            {
                stack_pos = 8;
                base_reg = arm_Assembler.R12;
            }

            if (m.HasThis && !m.ExplicitThis)
            {
                ArgumentLocation al = new ArgumentLocation { ValueLocation = arm_Assembler.Rx(ncrn++), ValueSize = pointer_size };

                if ((mtc.type != null) && mtc.type.IsValueType(ass) && !(mtc.tsig is Signature.BoxedType))
                    al.ExpectsVTRef = true;

                ret.Arguments.Add(al);
            }

            foreach (Signature.Param p in m.Params)
            {
                int v_size = ass.GetSizeOf(p);

                if (v_size < 4)
                    v_size = 4;

                if ((v_size == 4) && (ncrn < 4))
                    ret.Arguments.Add(new ArgumentLocation { ValueSize = v_size, ValueLocation = arm_Assembler.Rx(ncrn++) });
                else
                {
                    ret.Arguments.Add(new ArgumentLocation { ValueSize = v_size, ValueLocation = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = v_size } });
                    stack_pos += v_size;
                    stack_pos = util.align(stack_pos, pointer_size);
                }
            }

            if (call_tac != null)
            {
                if (call_tac.Operator == ThreeAddressCode.Op.call_void)
                    ret.ReturnValue = null;
                else
                {
                    var_semantic ret_vs = call_tac.GetResultSemantic(ass);

                    if (ret_vs.needs_integer)
                        ret.ReturnValue = arm_Assembler.R0;
                    else
                    {
                        int r_size = ass.GetSizeOf(m.RetType);
                        ret.ReturnValue = new hardware_contentsof { base_loc = base_reg, const_offset = stack_pos, size = r_size };
                        stack_pos += r_size;
                        stack_pos = util.align(stack_pos, pointer_size);
                    }
                }
            }

            return ret;
        }
    }

    partial class arm_Assembler
    {
        protected override void arch_init_callconvs()
        {
            call_convs.Clear();
            call_convs.Add("tyeabi", arm_CallConv.tyeabi);
            call_convs.Add("eabi", arm_CallConv.tyeabi);
            call_convs.Add("default", arm_CallConv.tyeabi);
            call_convs.Add("gnu", arm_CallConv.tyeabi);
        }
    }
}
