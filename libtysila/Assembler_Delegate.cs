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

/* Generate implementations for methods on delegate classes
 * 
 * We have to provide the following (see CIL II:14.6)
 * 
 * void .ctor(this, Object, IntPtr)
 * void Invoke(this, meth_args, ...)
 * System.IAsyncResult BeginInvoke(this, meth_args, ..., System.AsyncCallback, Object)
 * void EndInvoke(this, IAsyncResult)
 */

namespace libtysila
{
    partial class Assembler
    {
        bool GenerateDelegateFunction(MethodToCompile mtc, AssemblerState state)
        {
            if (!mtc.type.IsDelegate(this))
                return false;

            if (mtc.meth.Name == ".ctor")
            {
                GenerateDelegateCtor(mtc, state);
                return true;
            }
            else if (mtc.meth.Name == "Invoke")
            {
                GenerateDelegateInvoke(mtc, state);
                return true;
            }

            return false;
        }

        void RewriteSystemDelegate(Metadata.TypeDefRow delegate_tdr)
        {
            /* Rewrite System.Delegate to use a virtftnptr for method_ptr */

            foreach (Metadata.FieldRow fr in delegate_tdr.Fields)
            {
                if (fr.Name == "method_ptr")
                {
                    fr.fsig = new Signature.Field(new Signature.Param(BaseType_Type.VirtFtnPtr));
                    return;
                }
            }

            throw new Exception("method_ptr field not found");
        }

        void RewriteDelegateCtor(Signature.Method msig)
        {
            if (Signature.ParamCompare(msig.Params[1], new Signature.Param(BaseType_Type.I), this))
                msig.Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);
        }

        void GenerateDelegateInvoke(Assembler.MethodToCompile mtc, AssemblerState state)
        {
            /* Argument 0 is the delegate object
             * 
             * We load argument 0, extract the m_target from it
             * Then load the rest of the arguments in order
             * Then re-load argument 0 and extract the method_ptr from it
             * Then run calli
             */


            /* Create method signatures for both static and instance methods for the
             * called method */
            Signature.Method del_meth_inst = new Signature.Method();
            del_meth_inst.CallingConvention = Signature.Method.CallConv.Default;
            del_meth_inst.m = mtc.msig.m;
            del_meth_inst.RetType = mtc.msig.Method.RetType;
            foreach (Signature.Param p in mtc.msig.Method.Params)
                del_meth_inst.Params.Add(p);
            del_meth_inst.HasThis = true;
            del_meth_inst.ExplicitThis = false;

            Signature.Method del_meth_s = new Signature.Method();
            del_meth_s.CallingConvention = Signature.Method.CallConv.Default;
            del_meth_s.m = mtc.msig.m;
            del_meth_s.RetType = mtc.msig.Method.RetType;
            foreach (Signature.Param p in mtc.msig.Method.Params)
                del_meth_s.Params.Add(p);
            del_meth_s.HasThis = false;
            del_meth_s.ExplicitThis = false;

            /* Get the offsets of the fields of the delegats object */
            Layout l = Layout.GetLayout(mtc.GetTTC(this), this);
            Layout.Field m_target = l.GetFirstInstanceField("m_target");
            if (m_target == null)
                throw new Exception("m_target not found in " + mtc.GetTTC(this).ToString());
            Layout.Field method_ptr = l.GetFirstInstanceField("method_ptr");
            if (method_ptr == null)
                throw new Exception("method_ptr not found in " + mtc.GetTTC(this).ToString());

            /* First, decide if this is a static or instance call */
            cfg_node first_node = new cfg_node(state.next_block++, mtc);
            first_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            first_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = m_target.field } });
            first_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.brfalse] });

            /* Encode calling an instance method */
            cfg_node inst_node = new cfg_node(state.next_block++, mtc);
            inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = m_target.field } });
            for (int i = 0; i < mtc.msig.Method.Params.Count; i++)
                inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = i + 1 });
            inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = method_ptr.field } });
            inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.calli], inline_tok = new MTCToken { mtc = new MethodToCompile { msig = del_meth_inst } } });
            inst_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.br] });

            /* Encode calling a static method */
            cfg_node static_node = new cfg_node(state.next_block++, mtc);
            for (int i = 0; i < mtc.msig.Method.Params.Count; i++)
                static_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = i + 1 });
            static_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            static_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = method_ptr.field } });
            static_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.calli], inline_tok = new MTCToken { mtc = new MethodToCompile { msig = del_meth_s } } });

            /* Encode the end node */
            cfg_node end_node = new cfg_node(state.next_block++, mtc);
            end_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            /* Patch up the nodes */
            first_node.ipred_ids = new List<int>();
            first_node.isuc_ids = new List<int>();
            inst_node.ipred_ids = new List<int>();
            inst_node.isuc_ids = new List<int>();
            static_node.ipred_ids = new List<int>();
            static_node.isuc_ids = new List<int>();
            end_node.ipred_ids = new List<int>();
            end_node.isuc_ids = new List<int>();

            first_node.isuc_ids.Add(inst_node.block_id);
            first_node.isuc_ids.Add(static_node.block_id);
            inst_node.ipred_ids.Add(first_node.block_id);
            inst_node.isuc_ids.Add(end_node.block_id);
            static_node.ipred_ids.Add(first_node.block_id);
            static_node.isuc_ids.Add(end_node.block_id);
            end_node.ipred_ids.Add(inst_node.block_id);
            end_node.ipred_ids.Add(static_node.block_id);

            mtc.meth.nodes = new List<cfg_node>();
            mtc.meth.nodes.Add(first_node);
            mtc.meth.nodes.Add(inst_node);
            mtc.meth.nodes.Add(static_node);
            mtc.meth.nodes.Add(end_node);
        }

        void GenerateDelegateCtor(Assembler.MethodToCompile mtc, AssemblerState state)
        {
            /* CIL expectes this to be void .ctor(this delegate_object, Object object_of_defining_class, IntPtr meth_pointer)
             * 
             * Instead we re-write it to be:
             *   void .ctor(this delegate_object, Object object_of_defining_class, virtftnptr meth_pointer)
             *   
             * and set the fields in the object appropriately.
             * 
             * Specifically, we set virtftnptr method_ptr to be meth_pointer, and object m_target to object_of_defining_class
             * 
             * Code is:
             * 
             * ldarg.0
             * dup
             * ldarg.1
             * stfld <m_target>
             * ldarg.2
             * stfld <method_ptr>
             * ret
             */

            ((Signature.Method)mtc.msig).Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);

            Layout l = Layout.GetLayout(mtc.GetTTC(this), this);
            Layout.Field m_target = l.GetFirstInstanceField("m_target");
            if (m_target == null)
                throw new Exception("m_target not found in " + mtc.GetTTC(this).ToString());
            Layout.Field method_ptr = l.GetFirstInstanceField("method_ptr");
            if (method_ptr == null)
                throw new Exception("method_ptr not found in " + mtc.GetTTC(this).ToString());

            cfg_node node = new cfg_node(0, mtc);
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_1] });
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = m_target.field } });
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_2] });
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = method_ptr.field } });
            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            mtc.meth.nodes = new List<cfg_node>();
            mtc.meth.nodes.Add(node);
        }
    }
}
