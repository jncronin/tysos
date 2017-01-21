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
using System.Text;
using libtysila4.cil;

namespace libtysila4.ir
{
    partial class IrGraph
    {
        static Opcode[] pop(CilNode start, target.Target t)
        {
            return new Opcode[]
            {
                new Opcode
                {
                    oc = Opcode.oc_pop,
                    uses = new Param[] { new Param { t = Opcode.vl_stack, v = 0 } },
                    defs = new Param[] { }
                },
            };
        }

        static Opcode[] dup(cil.CilNode start, target.Target t)
        {
            /* This is quite complicated with the way uses and defs are
            handled.  We have to assign the use to a temporary, then
            assign independently to the two defs.  Thankfully the
            register allocator will coalesce the moves for us again. */

            Param temp_def = new Param
            {
                t = Opcode.vl_stack,
                v = start.n.g.next_vreg_id,
                stack_abs = true,
                ud = Param.UseDefType.Def
            };
            Param temp_use = new Param
            {
                t = Opcode.vl_stack,
                v = start.n.g.next_vreg_id++,
                stack_abs = true,
                ud = Param.UseDefType.Use
            };

            return new Opcode[]
            {
                new Opcode
                {
                    oc = Opcode.oc_store,
                    uses = new Param[] { new Param { t = Opcode.vl_stack, v = 0 } },
                    defs = new Param[] { temp_def }
                },
                new Opcode
                {
                    oc = Opcode.oc_store,
                    uses = new Param[] { temp_use },
                    defs = new Param[] { new Param { t = Opcode.vl_stack, v = 0 } },
                },
                new Opcode
                {
                    oc = Opcode.oc_store,
                    uses = new Param[] { temp_use },
                    defs = new Param[] { new Param { t = Opcode.vl_stack, v = 0 } },
                },
            };
        }
    }
}
