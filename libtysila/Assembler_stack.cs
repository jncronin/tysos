/* Copyright (C) 2008 - 2011 by John Cronin
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
    partial class Assembler
    {
        public class PseudoStack
        {
            public var contains_variable;
            public Signature.Param type;
            public bool needs_loading = true;
            public string name = null;
        }

        void TraceStack(List<cfg_node> nodes, int start_node, Metadata.MethodDefRow meth, Metadata m, Assembler.MethodToCompile call_site, Assembler.MethodToCompile mtc, AssemblerState state)
        {
            List<PseudoStack> pstack = new List<PseudoStack>();

            TraceStack(nodes[start_node], pstack,
                GetLocalVars(meth, state, call_site.tsig, call_site.msig),
                GetLocalArgs(call_site.msig, meth, state, call_site.tsig, call_site.msig),
                meth, m, call_site, mtc, state, nodes);
        }

        private List<PseudoStack> GetLocalVars(Metadata.MethodDefRow meth, AssemblerState state, Signature.BaseOrComplexType containing_type, Signature.BaseMethod containing_meth)
        {
            List<PseudoStack> lvs = new List<PseudoStack>();

            Signature.LocalVars sig = meth.GetLocalVars(this);
            int var_no = 0;
            foreach (Signature.Param p in sig.Vars)
                lvs.Add(new PseudoStack { type = Signature.ResolveGenericParam(p, containing_type, containing_meth, this), contains_variable = 0 , name = "V_" + (var_no++).ToString() });

            foreach (PseudoStack lv in lvs)
                state.lv_types.Add(new TypeToCompile(Metadata.GetTypeDef(lv.type.Type, this, false), lv.type, this));

            if (!state.local_vars_allocated)
            {
                // Allocate hardware space for the local vars
                int next_lv_loc = 0;

                // First allocate space for the methinfo pointer
                if (Options.EnableRTTI)
                {
                    hardware_location mip = GetMethinfoPointerLocation();
                    if (mip != null)
                    {
                        if ((mip is hardware_stackloc) && (((hardware_stackloc)mip).size == 0))
                        {
                            // Special case for 'use next stack location'
                            state.methinfo_pointer = new hardware_stackloc { loc = next_lv_loc, size = GetSizeOfPointer() };
                            next_lv_loc += GetSizeOfPointer();
                        }
                        else
                            state.methinfo_pointer = mip;
                    }
                }

                ArchSpecificStackSetup(state, ref next_lv_loc);

                foreach (PseudoStack lv in lvs)
                {
                    int size = GetSizeOf(lv.type);
                    state.local_vars.Add(new hardware_stackloc { loc = next_lv_loc, size = size });
                    state.lv_names.Add(lv.name);
                    next_lv_loc += size;
                    next_lv_loc = util.align(next_lv_loc, GetSizeOfPointer());
                }
                state.stack_space_used = next_lv_loc;
                /*for (int i = 0; i < lvs.Count; i++)
                    state.reg_alloc.GetLocalVarAllocator().GetHardwareLocation(var.LocalVar(i), GetSizeOf(lvs[i].type));*/

                state.local_vars_allocated = true;

                // Assign each local var to a logical var
                for (int i = 0; i < lvs.Count; i++)
                    state.lv_locs[i] = state.next_variable++;
            }

            return lvs;
        }

        private List<PseudoStack> GetLocalArgs(Signature.BaseMethod sig, Metadata.MethodDefRow mdr, AssemblerState state, Signature.BaseOrComplexType containing_type, Signature.BaseMethod containing_meth)
        {
            Signature.Method meth = null;
            if (sig is Signature.Method)
                meth = sig as Signature.Method;
            else if(sig is Signature.GenericMethod)
                meth = ((Signature.GenericMethod)sig).GenMethod;

            Signature.BaseOrComplexType this_pointer = containing_type;
            /*Assembler.TypeToCompile containing_ttc = new TypeToCompile { _ass = this, type = Metadata.GetTypeDef(containing_type, this), tsig = new Signature.Param(containing_type, this) };
            Layout l = Layout.GetLayout(containing_ttc, this);
            Metadata.TypeDefRow tdr = Metadata.GetOwningType(mdr.m, mdr);
            Assembler.TypeToCompile this_ttc = l.GetInterface(tdr).Interface.InterfaceType;*/

            Assembler.TypeToCompile this_ttc = new TypeToCompile { _ass = this, type = Metadata.GetTypeDef(containing_type, this), tsig = new Signature.Param(containing_type, this) };

            List<PseudoStack> las = new List<PseudoStack>();

            if (meth.HasThis && (!meth.ExplicitThis))
            {

                if (this_ttc.type.IsValueType(this))
                {
                    /* Value types expect the this pointer to be a managed reference to an instance of the value type (CIL I:13.3) */
                    Signature.BaseOrComplexType this_bct = this_ttc.tsig.Type;
                    if (this_bct is Signature.BoxedType)
                        this_bct = ((Signature.BoxedType)this_bct).Type;
                    if (this_bct is Signature.ManagedPointer)
                        this_bct = ((Signature.ManagedPointer)this_bct).ElemType;
                    Signature.Param mptr_type = new Signature.Param(new Signature.ManagedPointer { ElemType = this_bct }, this);
                    las.Add(new PseudoStack { type = mptr_type, name = "this" });

                    state.la_types.Add(new TypeToCompile(this_ttc.type, mptr_type, this));
                }
                else
                {
                    las.Add(new PseudoStack { type = this_ttc.tsig, name = "this" });
                    state.la_types.Add(new TypeToCompile(this_ttc.type, this_ttc.tsig, this));
                }
            }

            for(int i = 0; i < meth.Params.Count; i++)
            {
                Signature.Param p = meth.Params[i];
                Signature.Param p2 = Signature.ResolveGenericParam(p, containing_type, containing_meth, this);
                string pname = null;

                foreach (Metadata.ParamRow pr in mdr.GetParamNames())
                {
                    if ((pr.Sequence > 0) && ((((int)pr.Sequence) - 1) == i) && (pr.Name != null))
                        pname = pr.Name;
                }

                las.Add(new PseudoStack { type = p2, contains_variable = 0, name = pname });
                state.la_types.Add(new TypeToCompile(Metadata.GetTypeDef(p2.Type, this, false), p2, this));
            }

            if (!state.local_args_allocated)
            {
                // Determine the location of the local args
                CallConv cc = call_convs[state.call_conv](new MethodToCompile { _ass = this, meth = mdr, msig = sig, tsigp = this_ttc.tsig, type = this_ttc.type }, CallConv.StackPOV.Callee, this, new ThreeAddressCode(ThreeAddressCode.Op.call_void));
                for (int i = 0; i < las.Count; i++)
                {
                    state.local_args.Add(cc.Arguments[i]);
                    state.la_names.Add(las[i].name);
                }
                state.cc = cc;

                state.local_args_allocated = true;

                // Assign each local arg to a logical var
                for (int i = 0; i < las.Count; i++)
                    state.la_locs[i] = state.next_variable++;
            }

            return las;
        }

        private void TraceStack(cfg_node cfg_node, List<PseudoStack> pstack, List<PseudoStack> local_vars,
            List<PseudoStack> local_args,
            Metadata.MethodDefRow meth,
            Metadata m, Assembler.MethodToCompile call_site,
            Assembler.MethodToCompile mtc,
            AssemblerState state, List<cfg_node> nodes)
        {
            if (cfg_node.stack_traced)
                return;
            cfg_node.stack_traced = true;

            cfg_node.pstack_before = new List<PseudoStack>(pstack);
            cfg_node.lv_before = new List<PseudoStack>(local_vars);
            cfg_node.la_before = new List<PseudoStack>(local_args);
            List<PseudoStack> pstack2 = new List<PseudoStack>();
            List<PseudoStack> lv2 = new List<PseudoStack>();
            List<PseudoStack> la2 = new List<PseudoStack>();

            // insert block label
            cfg_node._tacs_prephi.Add(new LabelEx(cfg_node.block_id));

            // track the stack through the current basic block
            int j = 0;
            while(j < cfg_node.instrs.Count)
            {
                InstructionLine i = cfg_node.instrs[j];

                i.stack_before = new List<PseudoStack>(pstack);
                i.lv_before = new List<PseudoStack>(local_vars);
                i.lv_after = new List<PseudoStack>(local_vars);
                i.la_before = new List<PseudoStack>(local_args);
                i.la_after = new List<PseudoStack>(local_args);

                // Decompose a complex operation
                if (DecomposeComplexOpts(cfg_node.instrs, ref j, mtc.GetTTC(this), mtc, state))
                    continue;

                // at this point we can encode the types of variables into the opcodes
                i.pop_count = -1;
                i.cfg_node = cfg_node;
                EncodeOpcode(i, meth, mtc, m, call_site, cfg_node, state, nodes);

                switch (i.opcode.pop)
                {
                    case (int)PopBehaviour.Pop0:
                        break;
                    case (int)PopBehaviour.Pop1:
                    case (int)PopBehaviour.PopI:
                    case (int)PopBehaviour.PopRef:
                        pop(pstack);
                        break;
                    case (int)PopBehaviour.Pop1 + (int)PopBehaviour.Pop1:
                    case (int)PopBehaviour.Pop1 + (int)PopBehaviour.PopI:
                    case (int)PopBehaviour.PopI8 + (int)PopBehaviour.PopI:
                    case (int)PopBehaviour.PopI + (int)PopBehaviour.PopR4:
                    case (int)PopBehaviour.PopI + (int)PopBehaviour.PopR8:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.PopI:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.Pop1:
                    case (int)PopBehaviour.PopI * 2:
                        pop(pstack); pop(pstack);
                        break;
                    case (int)PopBehaviour.PopI * 3:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.PopI * 2:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.PopI + (int)PopBehaviour.PopI8:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.PopI + (int)PopBehaviour.PopR4:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.PopI + (int)PopBehaviour.PopR8:
                    case (int)PopBehaviour.PopRef + (int)PopBehaviour.PopI + (int)PopBehaviour.PopRef:
                        pop(pstack); pop(pstack); pop(pstack);
                        break;
                    case (int)PopBehaviour.VarPop:
                        if (i.pop_count < 0)
                            throw new NotSupportedException();
                        for (int k = 0; k < i.pop_count; k++)
                            pop(pstack);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (i.pushes == null)
                {
                    switch (i.opcode.push)
                    {
                        case (int)PushBehaviour.Push0:
                            break;
                        case (int)PushBehaviour.PushI:
                            push(pstack, new PseudoStack { type = new Signature.Param(BaseType_Type.I) , contains_variable = i.pushes_variable });
                            break;
                        case (int)PushBehaviour.PushI8:
                            push(pstack, new PseudoStack { type = new Signature.Param(BaseType_Type.I8) , contains_variable = i.pushes_variable });
                            break;
                        case (int)PushBehaviour.PushR4:
                            push(pstack, new PseudoStack { type = new Signature.Param(BaseType_Type.R4) , contains_variable = i.pushes_variable });
                            break;
                        case (int)PushBehaviour.PushR8:
                            push(pstack, new PseudoStack { type = new Signature.Param(BaseType_Type.R8) , contains_variable = i.pushes_variable });
                            break;
                        case (int)PushBehaviour.PushRef:
                            push(pstack, new PseudoStack { type = new Signature.Param(BaseType_Type.Object) , contains_variable = i.pushes_variable });
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    if (i.pushes.CliType(this) != CliType.void_)
                    {
                        PseudoStack push_1 = new PseudoStack { type = i.pushes, contains_variable = i.pushes_variable };
                        push_1.contains_variable.v_size = GetSizeOf(push_1.type);
                        push(pstack, push_1);

                        if (i.opcode.push == ((int)PushBehaviour.Push1 * 2))
                        {
                            PseudoStack push_2 = new PseudoStack { type = i.pushes, contains_variable = i.pushes_variable };
                            push_2.contains_variable.v_size = GetSizeOf(push_2.type);
                            push(pstack, push_2);
                        }
                    }
                }

                if (i.opcode.directly_modifies_stack)
                    pstack = new List<PseudoStack>(i.stack_after);
                else
                    i.stack_after = new List<PseudoStack>(pstack);
                local_vars = i.lv_after;
                local_args = i.la_after;

                j++;
            }

            // Fix up the pseudo stack so it does not contain local args or vars at the end of a node - this ensures that IdentifyGlobalVars works correctly
            for (int ps_idx = 0; ps_idx < pstack.Count; ps_idx++)
            {
                if ((pstack[ps_idx].contains_variable.type == var.var_type.LocalArg) || (pstack[ps_idx].contains_variable.type == var.var_type.LocalVar))
                {
                    ThreeAddressCode coerce = new ThreeAddressCode(GetAssignTac(pstack[ps_idx].type.CliType(this)), state.next_variable++, pstack[ps_idx].contains_variable, var.Null, pstack[ps_idx].contains_variable.v_size);

                    if (cfg_node.instrs.Count > 0)
                    {
                        switch (cfg_node.instrs[cfg_node.instrs.Count - 1].opcode.ctrl)
                        {
                            case ControlFlow.BRANCH:
                            case ControlFlow.BREAK:
                            case ControlFlow.COND_BRANCH:
                            case ControlFlow.RETURN:
                                cfg_node.instrs[cfg_node.instrs.Count - 1].tacs.Insert(0, coerce);
                                break;

                            default:
                                cfg_node._tacs_end.Add(coerce);
                                break;
                        }
                    }
                    else
                        cfg_node._tacs_end.Add(coerce);

                    pstack[ps_idx].contains_variable = state.next_variable - 1;
                }
            }

            cfg_node.pstack_after = pstack;
            cfg_node.lv_after = local_vars;
            cfg_node.la_after = local_args;

            // Fall through to the next block if this one does not finish with an unconditional branch
            if (cfg_node.has_fall_through)
                cfg_node._tacs_end.Add(new BrEx(ThreeAddressCode.Op.br, cfg_node.isuc[0].block_id));

            // track the stack of successors
            foreach (cfg_node n in cfg_node.isuc)
                TraceStack(n, new List<PseudoStack>(pstack), new List<PseudoStack>(local_vars),
                    new List<PseudoStack>(local_args), meth, m, call_site, mtc, state, nodes);
        }

        private PseudoStack pop(List<PseudoStack> pstack)
        {
            PseudoStack p = pstack[pstack.Count - 1];
            pstack.RemoveAt(pstack.Count - 1);
            return p;
        }

        private void push(List<PseudoStack> pstack, PseudoStack p)
        {
            pstack.Add(p);
        }
    }
}
