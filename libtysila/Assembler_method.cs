/* Copyright (C) 2014 by John Cronin
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

using libtysila.frontend.cil;
using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    partial class Assembler
    {
        public class MethodAttributes
        {
            public bool calls_ref_implementation = false;
            public bool is_ref_implementation = false;

            public bool is_weak_implementation = false;

            public AssemblerSecurity security = new AssemblerSecurity();

            public bool profile = false;

            public List<string> method_aliases = new List<string>();
            public List<Signature.Param> extra_params = new List<Signature.Param>();
            public Dictionary<string, Dictionary<string, object>> attrs = new Dictionary<string, Dictionary<string, object>>();

            public bool uninterruptible_method = false;

            public bool cls_compliant = true;
            public bool uses_vararg = false;

            public string call_conv;
            public CallConv cc;

            public string mangled_name;

            public int lv_stack_space = 0;
            public int spill_stack_space = 0;

            public Assembler.MachineRegisterList LVStackLocs = new MachineRegisterList();
            public Assembler.MachineRegisterList SpillStackLocs = new MachineRegisterList();

            public Assembler ass;

            public util.Set<Assembler.TypeToCompile> types_whose_static_fields_are_referenced = new util.Set<TypeToCompile>();

            public MethodAttributes(Assembler _ass) { ass = _ass; }
        }

        public AssembleBlockOutput AssembleMethod(MethodToCompile mtc, IOutputFile output, List<string> unimplemented_internal_calls, bool cache_output, bool dry_run)
        {
            return AssembleMethod(mtc, output, unimplemented_internal_calls, cache_output, dry_run, true);
        }

        public AssembleBlockOutput AssembleMethod(MethodToCompile mtc, IOutputFile output, List<string> unimplemented_internal_calls, bool cache_output, bool dry_run, bool do_meth_info)
        {
            Metadata.MethodDefRow meth = mtc.meth;
            Signature.BaseMethod call_site = mtc.msig;
            Metadata.TypeDefRow tdr = mtc.type;
            Signature.BaseOrComplexType tsig = mtc.tsig;
            Metadata m = meth.m;

            MethodAttributes attrs = new MethodAttributes(this);
            ParseAttributes(m, mtc, attrs);
            attrs.mangled_name = Mangler2.MangleMethod(mtc, this);

            foreach (Signature.Param p in attrs.extra_params)
                mtc.msig.Method.Params.Add(p);
            mtc.msig.Method.ParamCount = mtc.msig.Method.Params.Count;

            if (_options.CoalesceGenericRefTypes)
            {
                if (mtc.GenMethodType == MethodToCompile.GenericMethodType.CallsRefImplementation)
                    attrs.calls_ref_implementation = true;
                else if (mtc.GenMethodType == MethodToCompile.GenericMethodType.RefImplementation)
                    attrs.is_ref_implementation = true;
            }

            //state.reg_alloc = GetRegisterAllocator();

            if (mtc.meth.m.Information.name == "mscorlib")
                attrs.security = AssemblerSecurity.CorlibSecurity;
            if (mtc.meth.m.Information.name == "libsupcs")
                attrs.security = AssemblerSecurity.CorlibSecurity;

            if (mtc.meth.CallConvOverride != null)
                attrs.call_conv = mtc.meth.CallConvOverride;
            else
                attrs.call_conv = _options.CallingConvention;

            if (call_site == null)
            {
                call_site = Signature.ParseMethodDefSig(meth.m, meth.Signature, this);
                call_site.Method.meth = meth;
            }

            if (call_site.Method.CallingConvention == Signature.Method.CallConv.VarArg)
                attrs.uses_vararg = true;

            /* Handle dry run attempts */
            if (dry_run)
                throw new NotImplementedException();

            /* Rewrite CLSCompliant(false) and vararg methods to throw an exception */
            frontend.cil.CilGraph instrs = null;
            if ((attrs.cls_compliant == false) && (attrs.security.AllowCLSCompliantFalse == false))
                instrs = RewriteNonCLSCompliant(mtc);
            else if (attrs.uses_vararg)
                instrs = RewriteVararg(mtc);

            /* Get the body of the method */
            if (instrs == null)
            {
                if (meth.instrs != null)
                {
                    /* Is there a method defined directly in the method header? */
                    instrs = meth.instrs;
                }
                else if (meth.Body.Body == null)
                {
                    /* The method has no body defined. */

                    /* If there is no implementation, we have to provide one.
                     * 
                     * If the method is marked InternalCall, and we provide the internal call, then simply rewrite it to call itself again
                     * Rationale here is that any call instruction referencing an internal call will get rewritten inline in the calling
                     * method.
                     * 
                     * The problem is that sometimes these methods need to be loaded into vtables (e.g. anything which inherits
                     * System.Object's implementation of GetHashCode), so we need to provide a concrete implementation.
                     * 
                     * If we do not provide it, assume it will be provided by an external library and do nothing.
                     * 
                     * Some special internal calls (e.g. GetArg) operate on the stack of the calling function, so cannot be implemented
                     * as stand-alones without knowledge of the stack frame of the underlying architecture - as these are not virtual
                     * functions, we do not need to rewrite them anyway, so just return.
                     */

                    /* Detect functions we are not going to rewrite */
                    string short_mangled_name = attrs.mangled_name;
                    if (short_mangled_name.StartsWith("_ZX14CastOperationsM_0_") && short_mangled_name.Contains("GetArg"))
                        return null;
                    if (attrs.attrs.ContainsKey("libsupcs.ReinterpretAsMethod"))
                        return null;

                    bool provides = false;

                    if (meth.IsInternalCall || meth.IsPinvokeImpl)
                    {
                        if (frontend.cil.OpcodeEncodings.call.ProvidesIntcall(mtc, this))
                        {
                            provides = true;
                            instrs = RewriteInternalCall(mtc);
                        }
                        else
                        {
                            /* The internal call needs to be provided by the system */
                            if (unimplemented_internal_calls != null)
                            {
                                lock (unimplemented_internal_calls)
                                {
                                    if (!unimplemented_internal_calls.Contains(attrs.mangled_name))
                                        unimplemented_internal_calls.Add(attrs.mangled_name);
                                }
                            }
                        }
                    }

                    if (!((meth.IsInternalCall || meth.IsPinvokeImpl) && provides))
                    {
                        CilGraph delegate_instrs = GenerateDelegateFunction(mtc, attrs);
                        if (delegate_instrs != null)
                            instrs = delegate_instrs;
                    }
                }
                else
                {
                    /* There is a method body defined.  However, first ensure this is not a
                     * call to an instance method on a boxed value type - these need converting
                     * to be an instance method in a reference value type instead */

                    /* Instance methods on value types expect the this pointer to be a managed pointer to the type
                     * 
                     * Calls to instance methods on value types pass either managed references or boxed value types as
                     * the this pointer (this supports calling virtual methods - e.g. to call the virtual method ToString()
                     * on a value type, an object reference must be passed as the this pointer, as the implementation may
                     * be provided by System.Object)
                     * 
                     * The problem here is that we then have to sometimes unbox the this pointer on entry to functions
                     * 
                     * The way to get around this is to introduce trampoline code in all instance methods on boxed value types
                     * which does:
                     * 
                     * ldarg.0
                     * unbox
                     * ldarg.1
                     * ldarg.2
                     * ldarg...
                     * call <method on unboxed value type - expects managed reference as the this pointer>
                     * ret
                     * 
                     * See CIL I:12.4.1.4 and CIL I:13.3 for references
                     */

                    Signature.Method msig = mtc.msig.Method;

                    if (msig.HasThis && (mtc.tsig is Signature.BoxedType))
                        instrs = RewriteBoxedTypeMethod(mtc);
                    else
                    {
                        /* There is a method defined, encode it */
                        instrs = frontend.cil.CilGraph.BuildGraph(mtc.meth.Body, m, _options);
                    }
                }
            }

            if (instrs == null)
                return null;

            // Compile to tybel
            List<tybel.Node> tybel_instrs = libtysila.frontend.cil.Encoder.Encode(instrs, mtc, this, attrs);

            // Generate machine code
            List<byte> code = new List<byte>();
            List<libasm.ExportedSymbol> syms = new List<libasm.ExportedSymbol>();
            List<libasm.RelocationBlock> relocs = new List<libasm.RelocationBlock>();
            List<tybel.Tybel.DebugNode> debug = new List<libtysila.tybel.Tybel.DebugNode>();
            tybel.Tybel.Assemble(tybel_instrs, code, syms, relocs, this, attrs, debug);

            // Generate IL->machine code map
            Dictionary<int, InstructionHeader> il_map = new Dictionary<int, InstructionHeader>();
            foreach (tybel.Tybel.DebugNode dn in debug)
            {
                if(dn is tybel.Tybel.CilBlock)
                {
                    CilNode cn = dn.Code as CilNode;
                    InstructionHeader ih = new InstructionHeader { ass = this,
                        il_offset = cn.il.il_offset, compiled_offset = dn.Offset };
                    il_map[cn.il.il_offset] = ih;
                }
            }

            if (output != null)
            {
                // Write a pointer to the method info if applicable
                if (Options.EnableRTTI && do_meth_info)
                {
                    int mi_ptr_base = output.GetText().Count;
                    for (int i = 0; i < GetSizeOfPointer(); i++)
                        output.GetText().Add(0);

                    Layout l = Layout.GetTypeInfoLayout(mtc.GetTTC(this), this, false);
                    string mi_name = Mangler2.MangleMethodInfoSymbol(mtc, this);
                    if (l.Symbols.ContainsKey(mi_name))
                    {
                        int mi_offset = l.Symbols[mi_name];
                        output.AddTextRelocation(mi_ptr_base, l.typeinfo_object_name,
                            GetDataToDataRelocType(), mi_offset);
                    }
                }

                // Write code to output stream
                int text_base = output.GetText().Count;
                attrs.method_aliases.Add(attrs.mangled_name);
                bool is_weak = attrs.is_weak_implementation;
                if (attrs.attrs.ContainsKey("libsupcs.WeakLinkage"))
                    is_weak = true;
                if (mtc.tsig.IsWeakLinkage)
                    is_weak = true;
                if (mtc.msig is Signature.GenericMethod)
                    is_weak = true;
                List<ISymbol> isyms = new List<ISymbol>();
                foreach (string alias in attrs.method_aliases)
                    isyms.Add(output.AddTextSymbol(text_base, alias, false, true, is_weak));

                foreach (byte b in code)
                    output.GetText().Add(b);

                foreach (libasm.RelocationBlock reloc in relocs)
                    output.AddTextRelocation(text_base + reloc.Offset, reloc.Target, reloc.RelType, reloc.Value);

                foreach (ISymbol isym in isyms)
                    isym.Length = output.GetText().Count - text_base;
            }

            return new AssembleBlockOutput { code = code, compiled_code_length = code.Count, relocs = relocs, debug = debug, instrs = il_map };
        }

        private frontend.cil.CilGraph RewriteInternalCall(MethodToCompile mtc)
        {
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();

            /* ldarg x n, call, ret */
            for (int i = 0; i < get_arg_count(mtc.msig); i++)
                instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_s)], inline_int = i });
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.call)], inline_tok = new MTCToken { mtc = mtc } });
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ret)] });

            return instrs;
        }

        private frontend.cil.CilGraph RewriteBoxedTypeMethod(MethodToCompile mtc)
        {
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();
            /* write out a trampoline function */
            TypeToCompile unboxed_type = new TypeToCompile { _ass = this, type = mtc.type, tsig = new Signature.Param(((Signature.BoxedType)mtc.tsigp.Type).Type, this) };

            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)] });
            instrs.Add(new frontend.cil.InstructionLine
            {
                opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.unbox)],
                inline_tok = new TTCToken
                {
                    ttc = unboxed_type
                }
            });
            for (int i = 1; i < get_arg_count(mtc.msig); i++)
                instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[0xfe09], inline_int = i });

            MethodToCompile unboxed_meth = new MethodToCompile { _ass = this, tsigp = unboxed_type.tsig, type = unboxed_type.type, meth = mtc.meth, msig = mtc.msig };
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.call)], inline_tok = new MTCToken { mtc = unboxed_meth } });
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ret)] });

            return instrs;
        }

        private CilGraph GenerateDelegateFunction(MethodToCompile mtc, MethodAttributes attrs)
        {
            if (mtc.type.IsDelegate(this))
            {
                if (mtc.meth.Name == ".ctor")
                    return RewriteDelegateCtor(mtc);
                if (mtc.meth.Name == "Invoke")
                    return RewriteDelegateInvoke(mtc);
                if (mtc.meth.Name == "BeginInvoke")
                    return RewriteDelegateBeginInvokeMethod(mtc);
                if (mtc.meth.Name == "EndInvoke")
                    return RewriteDelegateEndInvokeMethod(mtc);
            }
            return null;
        }

        private CilGraph RewriteDelegateEndInvokeMethod(MethodToCompile mtc)
        {
            /* Write out a function to throw a NotImplementedException */
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();

            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[frontend.cil.Opcode.OpcodeVal(frontend.cil.Opcode.DoubleOpcodes.sthrow)], inline_int = throw_NotImplementedException });

            EmitWarning(new AssemblerException("EndInvoke methods are not currently implemented - rewriting method to throw exception", null, mtc));

            return instrs;
        }

        private CilGraph RewriteDelegateBeginInvokeMethod(MethodToCompile mtc)
        {
            /* Write out a function to throw a NotImplementedException */
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();

            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[frontend.cil.Opcode.OpcodeVal(frontend.cil.Opcode.DoubleOpcodes.sthrow)], inline_int = throw_NotImplementedException });

            EmitWarning(new AssemblerException("BeginInvoke methods are not currently implemented - rewriting method to throw exception", null, mtc));

            return instrs;
        }

        private CilGraph RewriteDelegateInvoke(MethodToCompile mtc)
        {
            /* The code here depends on whether or not this is a
             * static invocation - this can be determined by examining
             * m_target
             * 
             * ldarg_0
             * ldfld m_target
             * brfalse -> static
             * ldarg_0
             * ldfld m_target
             * foreach param:
             *     ldarg_s
             * ldarg_0
             * ldfld method_ptr
             * calli delegate_instance_method
             * br -> end
             * static:
             * nop
             * foreach param:
             *     ldarg_s
             * ldarg_0
             * ldfld method_ptr
             * calli delegate_static_method
             * end:
             * ret
             */

            Layout l = Layout.GetLayout(mtc.GetTTC(this), this);
            Layout.Field m_target = l.GetFirstInstanceField("m_target");
            if (m_target == null)
                throw new Exception("m_target not found in " + mtc.GetTTC(this).ToString());
            Layout.Field method_ptr = l.GetFirstInstanceField("method_ptr");
            if (method_ptr == null)
                throw new Exception("method_ptr not found in " + mtc.GetTTC(this).ToString());

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

            int cur_il_offset = 0;

            /* Load up System.Object to use as the this pointer for the instance call (rationale is that
             * we don't know exactly which object the delegate will be called on, but that if an object
             * _is_ specified then it must be a reference type for the this pointer)
             * 
             * We need this to be specified in the MTC as various calling conventions check the type of it */
            Signature.Param p_obj = new Signature.Param(BaseType_Type.Object);
            Metadata.TypeDefRow tdr_obj = Metadata.GetTypeDef(p_obj.Type, this);

            CilGraph instrs = new CilGraph();
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)], il_offset = cur_il_offset++ });
            instrs.Add(new InstructionLine
            {
                opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldfld)],
                inline_tok = new FTCToken { ftc = m_target.field },
                il_offset = cur_il_offset++
            });
            CilNode brfalse = instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.brfalse)], il_offset = cur_il_offset++ });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)], il_offset = cur_il_offset++ });
            instrs.Add(new InstructionLine
            {
                opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldfld)],
                inline_tok = new FTCToken { ftc = m_target.field },
                il_offset = cur_il_offset++
            });
            for (int i = 0; i < mtc.msig.Method.Params.Count; i++)
            {
                instrs.Add(new InstructionLine
                {
                    opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_s)],
                    inline_int = i + 1,
                    il_offset = cur_il_offset++
                });
            }
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)], il_offset = cur_il_offset++ });
            instrs.Add(new InstructionLine
            {
                opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldfld)],
                inline_tok = new FTCToken { ftc = method_ptr.field },
                il_offset = cur_il_offset++
            });
            instrs.Add(new InstructionLine
            {
                opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.calli)],
                inline_tok = new MTCToken { mtc = new MethodToCompile { _ass = this, msig = del_meth_inst, meth = mtc.meth, tsigp = p_obj, type = tdr_obj } },
                il_offset = cur_il_offset++
            });
            CilNode ret = instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.br)], il_offset = cur_il_offset++ });
            CilNode _static = instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.nop)], il_offset = cur_il_offset++ });
            for (int i = 0; i < mtc.msig.Method.Params.Count; i++)
            {
                instrs.Add(new InstructionLine
                {
                    opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_s)],
                    inline_int = i + 1,
                    il_offset = cur_il_offset++
                });
            }
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)], il_offset = cur_il_offset++ });
            instrs.Add(new InstructionLine
            {
                opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldfld)],
                inline_tok = new FTCToken { ftc = method_ptr.field },
                il_offset = cur_il_offset++
            });
            instrs.Add(new InstructionLine
            {
                opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.calli)],
                inline_tok = new MTCToken { mtc = new MethodToCompile { _ass = this, msig = del_meth_s, meth = mtc.meth, tsigp = p_obj, type = tdr_obj } },
                il_offset = cur_il_offset++
            });
            CilNode end = instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ret)], il_offset = cur_il_offset++ });

            /* Add il_offset_after s (== il_offset + 1 here) */
            foreach (CilNode n in instrs.LinearStream)
                n.il.il_offset_after = n.il.il_offset + 1;
            
            /* link the blocks */
            brfalse.Next.Insert(0, _static);
            brfalse.il.inline_int = _static.il.il_offset - brfalse.il.il_offset_after;
            _static.Prev.Clear();
            _static.Prev.Insert(0, brfalse);

            ret.Next.Clear();
            ret.Next.Add(end);
            ret.il.inline_int = end.il.il_offset - ret.il.il_offset_after;
            end.Prev.Add(ret);

            return instrs;
        }

        private CilGraph RewriteDelegateCtor(MethodToCompile mtc)
        {
            /* .ctor(this delegate_object, Object object_of_defining_class, IntPtr meth_pointer)
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

            Layout l = Layout.GetLayout(mtc.GetTTC(this), this);
            Layout.Field m_target = l.GetFirstInstanceField("m_target");
            if (m_target == null)
                throw new Exception("m_target not found in " + mtc.GetTTC(this).ToString());
            Layout.Field method_ptr = l.GetFirstInstanceField("method_ptr");
            if (method_ptr == null)
                throw new Exception("method_ptr not found in " + mtc.GetTTC(this).ToString());

            CilGraph instrs = new CilGraph();
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_0)] });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.dup)] });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_1)] });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stfld)], 
                inline_tok = new FTCToken { ftc = m_target.field } });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ldarg_2)] });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.stfld)],
                inline_tok = new FTCToken { ftc = method_ptr.field } });
            instrs.Add(new InstructionLine { opcode = OpcodeList.Opcodes[Opcode.OpcodeVal(Opcode.SingleOpcodes.ret)] });

            return instrs;
        }

        private frontend.cil.CilGraph RewriteVararg(MethodToCompile mtc)
        {
            /* Write out a function to throw a NotImplementedException */
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();

            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[frontend.cil.Opcode.OpcodeVal(frontend.cil.Opcode.DoubleOpcodes.sthrow)], inline_int = throw_NotImplementedException });

            EmitWarning(new AssemblerException("vararg methods are not supported - rewriting method to throw exception", null, mtc));

            return instrs;
        }

        private frontend.cil.CilGraph RewriteNonCLSCompliant(MethodToCompile mtc)
        {
            /* Write out a function to throw a NotImplementedException */
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();

            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[frontend.cil.Opcode.OpcodeVal(frontend.cil.Opcode.DoubleOpcodes.sthrow)], inline_int = throw_NotImplementedException });

            EmitWarning(new AssemblerException("vararg methods are not supported - rewriting method to throw exception", null, mtc));

            return instrs;
        }

        public void ParseAttributes(Metadata m, Assembler.MethodToCompile mtc, MethodAttributes attrs)
        {
            /* Parse relevant attributes */
            foreach (Metadata.CustomAttributeRow car in m.Tables[(int)Metadata.TableId.CustomAttribute])
            {
                Metadata.MethodDefRow mdr = Metadata.GetMethodDef(car.Parent, this);
                if (mdr == mtc.meth)
                {
                    Assembler.MethodToCompile camtc = Metadata.GetMTC(car.Type, new TypeToCompile(), null, this);

                    string caname = Mangler2.MangleMethod(camtc, this);

                    if (caname == "_ZX20MethodAliasAttributeM_0_7#2Ector_Rv_P2u1tu1S")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        int offset = 2;
                        int len = Metadata.ReadCompressedInteger(car.Value, ref offset);
                        string s = new UTF8Encoding().GetString(car.Value, offset, len);
                        attrs.method_aliases.Add(s);
                    }
                    else if (caname == "_ZX24UninterruptibleAttributeM_0_7#2Ector_Rv_P1u1t")
                    {
                        attrs.uninterruptible_method = true;
                    }
                    else if (caname == "_ZX22ExtraArgumentAttributeM_0_7#2Ector_Rv_P3u1tii")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        int offset = 2;
                        int arg_no = (int)PEFile.Read32(car.Value, ref offset);
                        int arg_type = (int)PEFile.Read32(car.Value, ref offset);

                        if (!(mtc.msig is Signature.Method))
                            throw new NotSupportedException();
                        Signature.Method meth_sig = mtc.msig as Signature.Method;

                        /* Create a new signature for the method for use in the compilation.
                         * 
                         * If we change the signature that is already stored, it will change the signature
                         * in the MethodToCompile object in Assembler.Requestor's dictionaries, without
                         * changing the hashcode internally stored for it.  This will lead to problems
                         * with insertion/removal of MethodToCompiles
                         */

                        attrs.extra_params.Add(new Signature.Param((BaseType_Type)arg_type));
                    }
                    else if (caname == "_ZW6System21CLSCompliantAttributeM_0_7#2Ector_Rv_P2u1tb")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        if (car.Value[2] == 0x00)
                            attrs.cls_compliant = false;
                    }
                    else if (caname == "_ZX16ProfileAttributeM_0_7#2Ector_Rv_P2u1tb")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        if (car.Value[2] == 0x00)
                            attrs.profile = false;
                        else
                            attrs.profile = true;
                    }

                    ParseCustomAttribute(car, attrs.attrs);  
                }
            }
        }

        protected internal void ParseCustomAttribute(Metadata.CustomAttributeRow car, Dictionary<string, Dictionary<string, object>> ret)
        {
            Assembler.MethodToCompile camtc = Metadata.GetMTC(car.Type, new TypeToCompile(), null, this);

            string name = camtc.type.TypeFullName;
            if (name.EndsWith("Attribute"))
                name = name.Substring(0, name.Length - "Attribute".Length);

            int offset = 0;
            if (car.Value[offset++] != 0x01)
                throw new NotSupportedException();
            if (car.Value[offset++] != 0x00)
                throw new NotSupportedException();

            ret[name] = new Dictionary<string, object>();

            List<Metadata.ParamRow> ps = camtc.meth.GetParamNames();
            int unnamed_idx = 0;
            for (int i = 0; i < camtc.msig.Method.Params.Count; i++)
            {
                string pname;
                if (i < ps.Count)
                    pname = ps[i].Name;
                else
                    pname = "unnamed_" + (unnamed_idx++).ToString();

                Signature.Param p = camtc.msig.Method.Params[i];
                object o = null;
                if (p.Type is Signature.BaseType)
                {
                    Signature.BaseType bt = p.Type as Signature.BaseType;
                    switch (bt.Type)
                    {
                        case BaseType_Type.String:
                            int len = Metadata.ReadCompressedInteger(car.Value, ref offset);
                            string s = new UTF8Encoding().GetString(car.Value, offset, len);
                            offset += len;
                            o = s;
                            break;
                        case BaseType_Type.Boolean:
                            byte b = car.Value[offset++];
                            if (b == 0)
                                o = false;
                            else if (b == 1)
                                o = true;
                            else
                                throw new Exception("Invalid boolean value in custom attribute: " + b.ToString());
                            break;
                        case BaseType_Type.I4:
                            o = LSB_Assembler.FromByteArrayI4S(car.Value, offset);
                            offset += 4;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                }

                ret[name][pname] = o;
            }
        }

        private int get_arg_count(Signature.BaseMethod m)
        {
            Signature.Method meth;
            if (m is Signature.Method)
                meth = m as Signature.Method;
            else if (m is Signature.GenericMethod)
                meth = ((Signature.GenericMethod)m).GenMethod;
            else
                throw new NotSupportedException();

            int arg_count = meth.Params.Count;
            if ((meth.HasThis == true) && (meth.ExplicitThis == false))
                arg_count++;

            return arg_count;
        }
    }
}
