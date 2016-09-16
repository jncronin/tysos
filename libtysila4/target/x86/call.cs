/* Copyright (C) 2016 by John Cronin
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
using libtysila4.ir;

namespace libtysila4.target.x86
{
    partial class x86_Assembler
    {
        void LowerCall(Opcode irnode)
        {
            irnode.is_mc = true;
            irnode.mcinsts = new List<MCInst>();

            // Insert node where we can save callee save registers
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = ir.Opcode.vl_str, v = target.Generic.g_precall }
                }
            });

            /* Parameters are:
                0: call
                1: call site
                2: ret_val/void
                then actual parameters */

            var csite = irnode.uses[0];
            Param rettype = null;
            if (irnode.defs.Length > 0)
                rettype = irnode.defs[0];
            else
                rettype = new Param { t = Opcode.vl_void };

            int pcount = irnode.uses.Length - 1;

            /* Decide where parameters go using the requested calling convention */
            string cc = "sysv";
            var cc2 = cc_map[cc];

            int stack_loc = 0;
            var reglocs = GetRegLocs(csite, ref stack_loc,
                cc2);

            /* Set-up stack */
            if (stack_loc != 0)
            {
                irnode.mcinsts.Add(new MCInst
                {
                    p = new Param[]
                    {
                    new Param { t = Opcode.vl_str, str = "sub_rm32_imm32", v = x86_sub_rm32_imm32 },
                    new Param { t = Opcode.vl_mreg, mreg = r_esp },
                    new Param { t = Opcode.vl_c32, v = stack_loc }
                    }
                });
            }

            /* Do the moves */
            for (int i = 0; i < pcount; i++)
            {
                Param[] ps = new Param[3];
                int opcode = x86_mov_rm32_r32;

                switch(irnode.uses[i + 1].t)
                {
                    case Opcode.vl_c:
                    case Opcode.vl_c32:
                        opcode = x86_mov_rm32_imm32;
                        break;

                    case Opcode.vl_mreg:
                    case Opcode.vl_stack:
                    case Opcode.vl_stack32:
                        break;

                    default:
                        throw new NotImplementedException();
                }

                ps[0] = new Param { t = Opcode.vl_str, str = "mov", v = opcode };
                var regloc = reglocs[i];
                if (regloc.type == rt_stack)
                    regloc = new ContentsReg { basereg = r_esp, disp = regloc.stack_loc };
                ps[1] = new Param { t = Opcode.vl_mreg, mreg = regloc };
                ps[2] = irnode.uses[i + 1];
                irnode.mcinsts.Add(new MCInst { p = ps });
            }

            // Determine return location
            Reg retreg = null;
            if (rettype.t != Opcode.vl_void)
            {
                var retcc = retcc_map["ret_" + cc];
                var retloc = retcc[rettype.ct][0];
                retreg = this.regs[retloc];
            }

            Param[] pscall;

            if (retreg != null)
            {
                pscall = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "call", v = x86_call_rel32 },
                    csite,
                    new Param { t = Opcode.vl_mreg, mreg = retreg, ud = Param.UseDefType.Def }
                };
            }
            else
            {
                pscall = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "call", v = x86_call_rel32 },
                    csite,
                };
            }
            irnode.mcinsts.Add(new MCInst { p = pscall });

            if(retreg != null)
            { 
                Param[] ps = new Param[3];
                ps[0] = new Param { t = Opcode.vl_str, str = "mov", v = x86_mov_rm32_r32 };
                ps[1] = irnode.defs[0];
                ps[2] = new Param { t = Opcode.vl_mreg, mreg = retreg, ud = Param.UseDefType.Use };
                irnode.mcinsts.Add(new MCInst { p = ps });
            }

            /* Restore stack */
            if (stack_loc != 0)
            {
                irnode.mcinsts.Add(new MCInst
                {
                    p = new Param[]
                    {
                    new Param { t = Opcode.vl_str, str = "add_rm32_imm32", v = x86_add_rm32_imm32 },
                    new Param { t = Opcode.vl_mreg, mreg = r_esp },
                    new Param { t = Opcode.vl_c32, v = stack_loc }
                    }
                });
            }

            // Insert node where we can restore callee save registers
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = ir.Opcode.vl_str, v = target.Generic.g_postcall }
                }
            });
        }

        void LowerReturn(Opcode irnode)
        {
            irnode.is_mc = true;
            irnode.mcinsts = new List<MCInst>();
            var cc = "sysv";

            var csite = irnode.uses[0];
            Param rettype = null;
            Reg retreg = null;
            if (irnode.uses.Length > 1)
            {
                rettype = irnode.uses[1];

                var retcc = retcc_map["ret_" + cc];
                var retloc = retcc[rettype.ct][0];
                retreg = this.regs[retloc];
                Param[] ps = new Param[3];
                ps[0] = new Param { t = Opcode.vl_str, str = "mov", v = x86_mov_r32_rm32 };
                ps[1] = new Param { t = Opcode.vl_mreg, mreg = retreg, ud = Param.UseDefType.Def };
                ps[2] = irnode.uses[1];
                irnode.mcinsts.Add(new MCInst { p = ps });
            }

            /* Epilogue */
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, v = Generic.g_restorecalleepreserves }
                }
            });
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "mov", v = x86_mov_r32_rm32 },
                    new Param { t = Opcode.vl_mreg, mreg = r_esp },
                    new Param { t = Opcode.vl_mreg, mreg = r_ebp }
                }
            });
            irnode.mcinsts.Add(new MCInst
            {
                p = new Param[]
                {
                    new Param { t = Opcode.vl_str, str = "pop", v = x86_pop_r32 },
                    new Param { t = Opcode.vl_mreg, mreg = r_ebp }
                }
            });

            if (retreg == null)
            {
                irnode.mcinsts.Add(new MCInst
                {
                    p = new Param[]
                    {
                        new Param { t = Opcode.vl_str, str = "ret", v = x86_ret },
                    }
                });
            }
            else
            {
                irnode.mcinsts.Add(new MCInst
                {
                    p = new Param[]
                    {
                        new Param { t = Opcode.vl_str, str = "ret", v = x86_ret },
                        new Param { t = Opcode.vl_mreg, mreg = retreg, ud = Param.UseDefType.Use }
                    }
                });

            }
        }
    }
}
