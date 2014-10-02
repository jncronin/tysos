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

            public MethodAttributes(Assembler _ass) { ass = _ass; }
        }

        public AssembleBlockOutput AssembleMethod(MethodToCompile mtc, IOutputFile output, List<string> unimplemented_internal_calls, bool cache_output, bool dry_run)
        {
            Metadata.MethodDefRow meth = mtc.meth;
            Signature.BaseMethod call_site = mtc.msig;
            Metadata.TypeDefRow tdr = mtc.type;
            Signature.BaseOrComplexType tsig = mtc.tsig;
            Metadata m = meth.m;

            MethodAttributes attrs = new MethodAttributes(this);
            ParseAttributes(m, mtc, attrs);
            attrs.mangled_name = Mangler2.MangleMethod(mtc, this);

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

            if(call_site == null)
                call_site = Signature.ParseMethodDefSig(meth.m, meth.Signature, this);

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
                        if (!GenerateDelegateFunction(mtc, attrs))
                            return null;
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

            // Compile to tybel
            List<tybel.Node> tybel_instrs = libtysila.frontend.cil.Encoder.Encode(instrs, mtc, this, attrs);

            // Generate machine code
            List<byte> code = new List<byte>();
            List<libasm.ExportedSymbol> syms = new List<libasm.ExportedSymbol>();
            List<libasm.RelocationBlock> relocs = new List<libasm.RelocationBlock>();
            List<tybel.Tybel.DebugNode> debug = new List<libtysila.tybel.Tybel.DebugNode>();
            tybel.Tybel.Assemble(tybel_instrs, code, syms, relocs, this, attrs, debug);

            // Write code to output stream
            int text_base = output.GetText().Count;
            attrs.method_aliases.Add(attrs.mangled_name);
            foreach (string alias in attrs.method_aliases)
                output.AddTextSymbol(text_base, alias, false, true, attrs.is_weak_implementation);

            foreach (byte b in code)
                output.GetText().Add(b);

            foreach (libasm.ExportedSymbol sym in syms)
            {
                if (sym.LocalOnly == false)
                    output.AddTextSymbol(text_base + sym.Offset, sym.Name, false, false, false);
            }

            foreach (libasm.RelocationBlock reloc in relocs)
                output.AddTextRelocation(text_base + reloc.Offset, reloc.Target, reloc.RelType, reloc.Value);

            return new AssembleBlockOutput { code = code, compiled_code_length = code.Count, relocs = relocs, debug = debug };
        }

        private frontend.cil.CilGraph RewriteInternalCall(MethodToCompile mtc)
        {
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();

            /* ldarg x n, call, ret */
            for (int i = 0; i < get_arg_count(mtc.msig); i++)
                instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = i });
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = mtc } });
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.ret] });

            return instrs;
        }

        private frontend.cil.CilGraph RewriteBoxedTypeMethod(MethodToCompile mtc)
        {
            frontend.cil.CilGraph instrs = new frontend.cil.CilGraph();
            /* write out a trampoline function */
            TypeToCompile unboxed_type = new TypeToCompile { _ass = this, type = mtc.type, tsig = new Signature.Param(((Signature.BoxedType)mtc.tsigp.Type).Type, this) };

            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.ldarg_0] });
            instrs.Add(new frontend.cil.InstructionLine
            {
                opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.unbox],
                inline_tok = new TTCToken
                {
                    ttc = unboxed_type
                }
            });
            for (int i = 1; i < get_arg_count(mtc.msig); i++)
                instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[0xfe09], inline_int = i });

            MethodToCompile unboxed_meth = new MethodToCompile { _ass = this, tsigp = unboxed_type.tsig, type = unboxed_type.type, meth = mtc.meth, msig = mtc.msig };
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = unboxed_meth } });
            instrs.Add(new frontend.cil.InstructionLine { opcode = frontend.cil.OpcodeList.Opcodes[(int)SingleOpcodes.ret] });

            return instrs;
        }

        private bool GenerateDelegateFunction(MethodToCompile mtc, MethodAttributes attrs)
        {
            return false;
            throw new NotImplementedException();
        }

        private frontend.cil.CilGraph RewriteVararg(MethodToCompile mtc)
        {
            throw new NotImplementedException();
        }

        private frontend.cil.CilGraph RewriteNonCLSCompliant(MethodToCompile mtc)
        {
            throw new NotImplementedException();
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

                        Signature.Method new_sig = new Signature.Method
                        {
                            CallingConvention = meth_sig.CallingConvention,
                            ExplicitThis = meth_sig.ExplicitThis,
                            GenParamCount = meth_sig.GenParamCount,
                            HasThis = meth_sig.HasThis,
                            m = meth_sig.m,
                            ParamCount = arg_no + 1,
                            RetType = meth_sig.RetType,
                            Params = new List<Signature.Param>()
                        };
                        foreach (Signature.Param p in meth_sig.Params)
                            new_sig.Params.Add(p);
                        while (new_sig.Params.Count <= arg_no)
                            new_sig.Params.Add(null);
                        new_sig.Params[arg_no] = new Signature.Param((BaseType_Type)arg_type);

                        mtc.msig = new_sig;
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

                    {
                        string name = camtc.type.TypeFullName;
                        if (name.EndsWith("Attribute"))
                            name = name.Substring(0, name.Length - "Attribute".Length);

                        int offset = 0;
                        if (car.Value[offset++] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[offset++] != 0x00)
                            throw new NotSupportedException();

                        attrs.attrs[name] = new Dictionary<string, object>();

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
                                    default:
                                        throw new NotImplementedException();
                                }

                            }

                            attrs.attrs[name][pname] = o;
                        }
                    }
                }
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
