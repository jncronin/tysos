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
        static Opcode[] newobj(cil.CilNode start, target.Target t)
        {
            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);

            metadata.MethodSpec ms;
            if (m.GetMethodDefRow(table_id, row, out ms,
                start.n.g.ms.gtparams,
                start.n.g.ms.gmparams) == false)
                throw new MissingMethodException();

            /* Get size of type */
            var type_size = layout.Layout.GetTypeSize(ms.type, t);

            t.r.VTableRequestor.Request(ms.type);

            /* Get opcodes for creating the object */
            var obj_p = new Param
            {
                m = start.n.g.ms.m,
                t = Opcode.vl_stack,
                ct = Opcode.ct_object,
                v = start.n.g.next_vreg_id++,
                stack_abs = true,
                ud = Param.UseDefType.Def
            };
            var tsize_p = new Param
            {
                t = Opcode.vl_c32,
                ct = Opcode.ct_int32,
                v = type_size
            };
            var malloc_target = new Param
            {
                t = Opcode.vl_call_target,
                str = "gcmalloc",
                m = special_meths,
                v2 = special_meths.gcmalloc
            };
            Opcode o_malloc = new Opcode
            {
                oc = Opcode.oc_call,
                defs = new Param[] { obj_p },
                uses = new Param[] { malloc_target, tsize_p },
                call_retval = ms.type,
                call_retval_stype = ms.type.SimpleType
            };

            /* Get opcodes for calling the constructor */
            var call_ops = call(start, ms, t);

            /* Separate param for obj_p because one will
            be a use and another a def */
            Param obj_p_use = new Param
            {
                t = obj_p.t,
                ct = obj_p.ct,
                v = obj_p.v,
                stack_abs = obj_p.stack_abs,
                ud = Param.UseDefType.Use
            };

            /* Replace the instance of the object with our
            new object */
            foreach (var call_op in call_ops)
            {
                if (call_op.oc == Opcode.oc_call)
                {
                    call_op.uses[1] = obj_p_use;
                }
            }

            /* Assign our new object to an actual stack location */
            Param res = new Param
            {
                t = Opcode.vl_stack,
                ct = Opcode.ct_object,
                v = 0
            };
            Opcode assign = new Opcode
            {
                oc = Opcode.oc_store,
                defs = new Param[] { res },
                uses = new Param[] { obj_p_use }
            };

            Opcode[] ret = new Opcode[2 + call_ops.Length];
            ret[0] = o_malloc;
            for (int i = 0; i < call_ops.Length; i++)
                ret[i + 1] = call_ops[i];
            ret[call_ops.Length + 1] = assign;

            return ret;
        }

        static Opcode[] initobj(cil.CilNode start, target.Target t)
        {
            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);

            var ms = start.n.g.ms;

            var ts = m.GetTypeSpec(table_id, row, ms.gtparams, ms.gmparams);

            var obj_size = t.GetSize(ts);

            // the value on the stack is a pointer, so just emit a zeromem instruction
            return new Opcode[]
            {
                new Opcode
                {
                    oc = Opcode.oc_zeromem,
                    uses = new Param[]
                    {
                        new Param { t = Opcode.vl_stack, ct = Opcode.ct_ref, v = 0 },
                        new Param { t = Opcode.vl_c32, ct = Opcode.ct_int32, v = obj_size },
                    },
                    defs = new Param[] { },
                    data_size = obj_size
                }
            };
        }

        static Opcode[] ldtoken(cil.CilNode start, target.Target t)
        {
            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);

            var ms = start.n.g.ms;

            string mangled_name;
            int offset = 0;
            metadata.MethodSpec tok_ms = null;
            metadata.TypeSpec tok_ts = null;

            switch(table_id)
            {
                case metadata.MetadataStream.tid_MethodDef:
                case metadata.MetadataStream.tid_MethodSpec:
                    if(!m.GetMethodDefRow(table_id, row, out tok_ms,
                        ms.gtparams, ms.gmparams))
                    {
                        throw new MissingMethodException();
                    }
                    break;

                case metadata.MetadataStream.tid_TypeDef:
                case metadata.MetadataStream.tid_TypeRef:
                case metadata.MetadataStream.tid_TypeSpec:
                    tok_ts = m.GetTypeSpec(table_id, row,
                        ms.gtparams, ms.gmparams);
                    if (tok_ts == null)
                        throw new TypeLoadException();
                    break;

                default:
                    throw new NotImplementedException();
            }

            if (tok_ts != null)
            {
                mangled_name = tok_ts.m.MangleType(tok_ts);
                t.r.VTableRequestor.Request(tok_ts);
                offset = layout.Layout.GetVTableTIOffset(tok_ts) *
                    t.GetPointerSize();
            }
            else if (tok_ms != null)
            {
                mangled_name = tok_ms.m.MangleMethodSpec(tok_ms);
                t.r.MethodSpecRequestor.Request(tok_ms);
                offset = 0;
            }
            else
                throw new NotImplementedException();

            return new Opcode[]
            {
                new Opcode
                {
                    oc = Opcode.oc_ldlabaddr,
                    uses = new Param[]
                    {
                        new Param { m = ms.m, t = Opcode.vl_str, str = mangled_name, v = offset },
                    },
                    defs = new Param[]
                    {
                        new Param { m = ms.m, t = Opcode.vl_stack, v = 0, ct = Opcode.ct_object },
                    }
                }
            };
        }

        static Opcode[] castclass(cil.CilNode start, target.Target t)
        {
            /* We don't actually encode castclass/isinst here, but pass
            it through to a later pass because the constant folding code
            may be able to optimise it by tracing the type passed to it */

            metadata.MetadataStream m;
            uint token;
            start.GetToken(out m, out token);
            int table_id, row;
            m.InterpretToken(token, out table_id, out row);

            var ms = start.n.g.ms;

            var ts = m.GetTypeSpec(table_id, row, ms.gtparams, ms.gmparams);

            int oc = Opcode.oc_castclass;
            if (start.opcode.opcode1 == cil.Opcode.SingleOpcodes.isinst)
                oc = Opcode.oc_isinst;

            return new Opcode[]
            {
                new Opcode
                {
                    oc = oc,
                    uses = new Param[]
                    {
                        new Param { m = ms.m, t = Opcode.vl_stack, v = 0, ct = Opcode.ct_object },
                        new Param { m = ms.m, t = Opcode.vl_ts_token, ts = ts },
                    },
                    defs = new Param[]
                    {
                        new Param { m = ms.m, t = Opcode.vl_stack, v = 0, ct = Opcode.ct_object },
                    }
                }
            };
        }
    }
}
