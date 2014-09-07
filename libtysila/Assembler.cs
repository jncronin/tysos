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
    abstract public partial class Assembler : IByteOperations
    {
        internal LockedInt next_object_id = 0;

        public bool ProduceIrDump = false;
        private string _irdump = "";
        public string IrDump { get { return _irdump; } }
        public string output_name;
        public tydb.TyDbFile debug;
        public bool _debug_produces_output = true;
        public FileLoader Loader;
        public DebugOutput debugOutput;

        public bool profile = false;
        protected bool is_jit = false;
        public bool IsJit { get { return is_jit; } }

        internal int maxCoercionCountPerInstruction = 10;

        internal Dictionary<MethodToCompile, AssembleBlockOutput> CompiledMethodCache; /* = new Dictionary<MethodToCompile, AssembleBlockOutput>(); */
        internal Dictionary<MethodToCompile, bool> dry_run_cache; /* = new Dictionary<MethodToCompile, bool>(); */

        Architecture _arch;
        internal AssemblerOptions _options;
        public AssemblerOptions Options { get { return _options; } }
        public Architecture Arch { get { return _arch; } }

        public static string VersionString { get { return "0.3.0"; } }

        public const int throw_OverflowException = 1;
        public const int throw_InvalidCastException = 2;
        public const int throw_NullReferenceException = 3;
        public const int throw_MissingMethodException = 4;
        public const int throw_IndexOutOfRangeException = 5;

        internal abstract List<libasm.hardware_location> GetLocalVarsLocations(List<Signature.Param> lvs_p, MethodAttributes attrs);

        public abstract RelocationBlock.RelocationType GetDataToDataRelocType();
        public abstract RelocationBlock.RelocationType GetDataToCodeRelocType();
        public abstract RelocationBlock.RelocationType GetCodeToDataRelocType();
        public abstract RelocationBlock.RelocationType GetCodeToCodeRelocType();

        internal abstract Dictionary<libasm.hardware_location, libasm.hardware_location> AllocateStackLocations(Assembler.MethodAttributes attrs);

        public class AssemblerException : Exception
        {
            libtysila.frontend.cil.InstructionLine _inst;
            internal libtysila.frontend.cil.InstructionLine Instruction { get { return _inst; } }
            MethodToCompile _mtc;
            internal MethodToCompile Method { get { return _mtc; } }

            public AssemblerException(string msg, libtysila.frontend.cil.InstructionLine inst, MethodToCompile mtc) : base(msg) { _inst = inst; _mtc = mtc; }

            public override string Message
            {
                get
                {
                    return base.Message + ((_inst == null) ? "" : (" at IL_" + 0.ToString("x4") + ": " + _inst.ToString())) + " in " + _mtc.ToString();
                }
            }
        }

        public abstract class DebugOutput
        {
            public const int DBG_ERROR = 0;
            public const int DBG_WARN = 1;
            public const int DBG_NOTICE = 2;
            public const int DBG_INFO = 3;
            public const int DBG_DEBUG = 4;

            public virtual void Write(string s) { Write(s, DBG_DEBUG); }
            public abstract void Write(string s, int level);
            public virtual void WriteLine(string s) { WriteLine(s, DBG_DEBUG); }
            public abstract void WriteLine(string s, int level);
        }

        public class Architecture : IEquatable<Architecture>
        {
            internal string _instruction_set;
            internal string _oformat;
            internal string _os;

            internal List<string> _extra_ops = new List<string>();

            public string InstructionSet { get { return _instruction_set; } }
            public string OutputFormat { get { return _oformat; } }
            public string OperatingSystem { get { return _os; } }
            public IEnumerable<string> ExtraOptions { get { return _extra_ops; } }

            public override string ToString()
            {
                return _instruction_set + "-" + _oformat + "-" + _os;
            }

            public bool Equals(Architecture other)
            {
                if (_instruction_set != other._instruction_set)
                    return false;
                if (_oformat != other._oformat)
                    return false;
                if (_os != other._os)
                    return false;
                return true;
            }
        }

        public class AssemblerSecurity
        {
            public bool AllowConvORefToUI4 = false;
            public bool AllowAssignIntPtrToVirtftnptr = false;
            public bool AllowTysilaOpcodes = false;
            public bool AllowCLSCompliantFalse = false;
            public bool RequireVerifiedCode = true;
            public bool AllowUnsafeMemoryAccess = false;

            public static AssemblerSecurity CorlibSecurity
            {
                get
                {
                    return new AssemblerSecurity
                    {
                        AllowConvORefToUI4 = true,
                        AllowAssignIntPtrToVirtftnptr = true,
                        AllowCLSCompliantFalse = true,
                        RequireVerifiedCode = false,
                        AllowUnsafeMemoryAccess = true
                    };
                }
            }

            public static AssemblerSecurity DefaultSecurity
            {
                get
                {
                    return new AssemblerSecurity();
                }
            }
        }

        public abstract class FileLoader
        {
            public class FileLoadResults
            {
                public string FullFilename;
                public string ModuleName;
                public System.IO.Stream Stream;
            }

            public abstract FileLoadResults LoadFile(string filename);
        }

        public class AssemblerOptions
        {
            public bool EnableRTTI = true;
            public bool EnableExceptions = true;
            public bool CoalesceGenericRefTypes = false;
            public string EntryPointName = null;
            public bool IncludeLibsupcs = false;
            public bool IncludeLibstdcs = false;
            public bool PIC = false;
            public bool MiniAssembler = false;
            public bool InExtraAdd = false;
            public enum RegisterAllocatorType { graphcolour, fastreg };
            public RegisterAllocatorType RegAlloc = RegisterAllocatorType.graphcolour;
            public string CallingConvention = "default";
            public bool AllowTysilaOpcodes = false;
        }

        public class AssemblerState
        {
            public int next_variable = 1;
            public int next_block = 1;
            public AssemblerSecurity security = new AssemblerSecurity();
            public List<hardware_location> local_vars = new List<hardware_location>();
            public hardware_location methinfo_pointer = null;
            public List<CallConv.ArgumentLocation> local_args = new List<CallConv.ArgumentLocation>();
            public List<AssemblerException> warnings = new List<AssemblerException>();
            public int stack_space_used = 0;
            public int stack_space_used_cur_inst = 0;

            public List<string> la_names = new List<string>();
            public List<string> lv_names = new List<string>();

            public Dictionary<int, var> la_locs = new Dictionary<int, var>();
            public Dictionary<int, var> lv_locs = new Dictionary<int, var>();

            public List<CodeBlock> CalleePreserveRegistersSaves = new List<CodeBlock>();
            public List<CodeBlock> CalleePreserveRegistersRestores = new List<CodeBlock>();
            public List<hardware_location> UsedLocations = new List<hardware_location>();

            public List<TypeToCompile> la_types = new List<TypeToCompile>();
            public List<TypeToCompile> lv_types = new List<TypeToCompile>();

            public bool calls_ref_implementation = false;
            public bool is_ref_implementation = false;

            public string mangled_name;
            public bool ret_decomposed = false;
            public bool profile = false;
            public bool syscall = false;
            public bool uninterruptible_method = false;
            public var mangled_name_var;

            public bool local_args_allocated = false;
            public bool local_vars_allocated = false;

            public string call_conv = "default";
            public CallConv cc;

            public Signature.BaseMethod orig_sig;

            public tydb.Function debug = null;

            public Dictionary<int, hardware_location> required_locations = new Dictionary<int, hardware_location>();
            public Dictionary<int, int> la_v_map;

            internal Dictionary<var, IList<ThreeAddressCode>> liveness_uses, liveness_defs, liveness_in, liveness_out;

            //public List<TypeToCompile> types_whose_static_fields_are_referenced = new List<TypeToCompile>();

            internal enum Pass { Preoptimized, SSA, Optimized };
            internal Pass CurPass = Pass.Preoptimized;

            internal Dictionary<int, IList<int>> blocks_pred;
            internal Dictionary<int, IList<int>> blocks_suc;
        }

        public interface IrDumpFeedback
        {
            void IrDump(string line);
        }
        public IrDumpFeedback IrFeedback = null;
        public IrDumpFeedback OptimizedIrFeedback = null;

        public struct MethodToCompile : IEquatable<MethodToCompile>
        {
            public Metadata.MethodDefRow meth; public Signature.BaseMethod msig;
            public Metadata.TypeDefRow type; public Signature.Param tsigp;
            public Assembler _ass;
            public uint? MetadataToken;
            public Metadata m;

            public enum GenericMethodType { Undetermined, No, ValueTypesOnly, RefImplementation, CallsRefImplementation };
            GenericMethodType _GenMethodType;
            public GenericMethodType GenMethodType
            {
                get
                {
                    if (_GenMethodType != GenericMethodType.Undetermined)
                        return _GenMethodType;

                    // Determine generic method type
                    if (msig == null)
                        return GenericMethodType.Undetermined;
                    if (meth == null)
                        return GenericMethodType.Undetermined;
                    if (tsigp == null)
                        return GenericMethodType.Undetermined;

                    bool has_refs = false;
                    bool has_generic_vts = false;

                    if (tsig is Signature.GenericType)
                    {
                        Signature.GenericType gt = tsig as Signature.GenericType;

                        foreach (Signature.BaseOrComplexType bct in gt.GenParams)
                        {
                            if ((bct is Signature.BaseType) && (((Signature.BaseType)bct).Type == BaseType_Type.RefGenericParam))
                                has_refs = true;
                            else if (bct.IsValueType(_ass))
                                has_generic_vts = true;
                            else
                            {
                                _GenMethodType = GenericMethodType.CallsRefImplementation;
                                return _GenMethodType;
                            }
                        }
                    }

                    if (msig is Signature.GenericMethod)
                    {
                        Signature.GenericMethod gm = msig as Signature.GenericMethod;

                        foreach(Signature.BaseOrComplexType bct in gm.GenParams)
                        {
                            if ((bct is Signature.BaseType) && (((Signature.BaseType)bct).Type == BaseType_Type.RefGenericParam))
                                has_refs = true;
                            else if (bct.IsValueType(_ass))
                                has_generic_vts = true;
                            else
                            {
                                _GenMethodType = GenericMethodType.CallsRefImplementation;
                                return _GenMethodType;
                            }
                        }
                    }

                    if (has_refs)
                        _GenMethodType = GenericMethodType.RefImplementation;
                    else if (has_generic_vts)
                        _GenMethodType = GenericMethodType.ValueTypesOnly;
                    else
                        _GenMethodType = GenericMethodType.No;

                    return _GenMethodType;
                }
            }
            public MethodToCompile GetRefImplementation()
            {
                if (GenMethodType != GenericMethodType.CallsRefImplementation)
                    return this;

                MethodToCompile ret = new MethodToCompile { m = this.m, MetadataToken = this.MetadataToken, meth = this.meth, type = this.type, _GenMethodType = GenericMethodType.RefImplementation };
                if (!(tsig is Signature.GenericType))
                    ret.tsigp = tsigp;
                else
                {
                    Signature.GenericType gt = tsig as Signature.GenericType;
                    Signature.GenericType new_gt = new Signature.GenericType { _ass = gt._ass, GenParams = new List<Signature.BaseOrComplexType>(), GenType = gt.GenType };
                    foreach (Signature.BaseOrComplexType bct in gt.GenParams)
                    {
                        if (bct.IsValueType(_ass))
                            new_gt.GenParams.Add(bct);
                        else
                            new_gt.GenParams.Add(new Signature.BaseType(BaseType_Type.RefGenericParam));
                    }
                    ret.tsigp = new Signature.Param(new_gt, _ass);
                }
                if (!(msig is Signature.GenericMethod))
                    ret.msig = msig;
                else
                {
                    Signature.GenericMethod gm = msig as Signature.GenericMethod;
                    Signature.GenericMethod new_gm = new Signature.GenericMethod { m = gm.m, GenMethod = gm.GenMethod };
                    foreach (Signature.BaseOrComplexType bct in gm.GenParams)
                    {
                        if (bct.IsValueType(_ass))
                            new_gm.GenParams.Add(bct);
                        else
                            new_gm.GenParams.Add(new Signature.BaseType(BaseType_Type.RefGenericParam));
                    }
                    ret.msig = new_gm;
                }
                return ret;
            }            

            public Signature.BaseOrComplexType tsig { get { return tsigp.Type; } }

            public MethodToCompile(Assembler ass, uint? metadata_token) { _ass = ass; meth = null; msig = null; type = null; tsigp = null; MetadataToken = metadata_token; m = null; _GenMethodType = GenericMethodType.Undetermined; }
            public MethodToCompile(Assembler ass, Metadata.MethodDefRow _meth, Signature.BaseMethod _msig, Metadata.TypeDefRow _type, Signature.Param _tsigp, uint? metadata_token)
            { _ass = ass; meth = _meth; msig = _msig; type = _type; tsigp = _tsigp; MetadataToken = metadata_token; m = null; _GenMethodType = GenericMethodType.Undetermined; }
            public MethodToCompile(Assembler ass, Metadata.MethodDefRow _meth, Signature.BaseMethod _msig, Metadata.TypeDefRow _type, Signature.Param _tsigp)
            { _ass = ass; meth = _meth; msig = _msig; type = _type; tsigp = _tsigp; MetadataToken = null; m = meth.m; _GenMethodType = GenericMethodType.Undetermined; }
            #region IEquatable<MethodToCompile> Members

            public bool Equals(MethodToCompile other)
            {
                return Signature.MethodCompare(this, other, _ass, true);
            }

            #endregion

            public override string ToString()
            {
                return Mangler2.MangleMethod(this, _ass);
            }

            public override int GetHashCode()
            {
                return meth.Name.GetHashCode() ^ msig.GetHashCode() ^ tsigp.GetHashCode();
            }

            public Assembler.TypeToCompile GetTTC(Assembler ass) { return new Assembler.TypeToCompile { _ass = ass, type = type, tsig = tsigp }; }
        }

        public struct FieldToCompile : IEquatable<FieldToCompile>
        {
            public Metadata.FieldRow field; public Signature.Param fsig;
            public Metadata.TypeDefRow definedin_type; public Signature.Param definedin_tsig;
            public Metadata.TypeDefRow memberof_type; public Signature.Param memberof_tsig;
            public Assembler _ass;

            public static FieldToCompile GetFTC(Assembler.TypeToCompile ttc, string mangled_name, Assembler ass)
            {
                for (Metadata.TableIndex ti = ttc.type.FieldList; ti < Metadata.GetLastField(ttc.type.m, ttc.type); ti++)
                {
                    Metadata.FieldRow fr = ti.Value as Metadata.FieldRow;
                    Signature.Param fsig = Signature.ResolveGenericParam(Signature.ParseFieldSig(fr.m, fr.Signature, ass).AsParam(ass), ttc.tsig.Type, null, ass);
                    FieldToCompile ftc = new FieldToCompile { _ass = ass, fsig = fsig, field = fr, definedin_tsig = ttc.tsig, definedin_type = ttc.type };

                    if (mangled_name == fr.Name)
                    {
                        return ftc;
                    }
                    else
                    {
                        if (Signature.GetString(ftc, ass) == mangled_name)
                            return ftc;
                    }
                }
                throw new KeyNotFoundException();
            }

            public TypeToCompile DefinedIn { get { return new TypeToCompile { _ass = _ass, tsig = definedin_tsig, type = definedin_type }; } }
            public TypeToCompile MemberOf { get { return new TypeToCompile { _ass = _ass, tsig = memberof_tsig, type = memberof_type }; } }

            public override int GetHashCode()
            {
                int hc = 0x1a3b5c7d;
                hc ^= field.Name.GetHashCode();
                hc ^= fsig.GetHashCode();
                if (definedin_tsig != null)
                    hc ^= definedin_tsig.GetHashCode();
                return hc;
            }

            public bool Equals(FieldToCompile other)
            {
                if (field.Name != other.field.Name)
                    return false;
                if (!Signature.ParamCompare(fsig, other.fsig, _ass))
                    return false;
                if (!DefinedIn.Equals(other.DefinedIn))
                    return false;
                return true;
            }
        }

        public struct TypeToCompile : IEquatable<Assembler.TypeToCompile>
        {
            public Metadata.TypeDefRow type; public Signature.Param tsig;
            public Assembler _ass;
            public TypeToCompile(Assembler ass) : this() { if (ass == null) throw new Exception("Must specify assembler"); _ass = ass; type = null; tsig = null; }

            #region IEquatable<MethodToCompile> Members

            public TypeToCompile(Signature.Param p, Assembler ass) : this(ass)
            { type = Metadata.GetTypeDef(p.Type, ass); tsig = p; }

            public TypeToCompile(Signature.BaseOrComplexType bct, Assembler ass)
                : this(ass)
            { type = Metadata.GetTypeDef(bct, ass); tsig = new Signature.Param(bct, ass); }

            public TypeToCompile(Metadata.TypeDefRow _type, Signature.Param _tsig, Assembler ass) : this(ass)
            { type = _type; tsig = _tsig; }

            public TypeToCompile(Metadata.TypeDefRow _type, Assembler ass) : this(ass)
            {
                type = _type;
                tsig = new Signature.Param(new Token(_type), ass);
            }

            public TypeToCompile(Token tok, Assembler ass) : this(ass)
            {
                type = Metadata.GetTypeDef(tok, ass);
                tsig = new Signature.Param(tok, ass);
            }

            public override string ToString()
            {
                return Mangler2.MangleTypeInfo(this, _ass);
            }

            #endregion

            public bool Equals(TypeToCompile other)
            {
                return Signature.ParamCompare(this.tsig, other.tsig, _ass);
            }

            public override int GetHashCode()
            {
                return tsig.GetHashCode();
            }
        }

        internal interface IStackAllocator
        {
            hardware_stackloc GetNextStackLoc(var var_id, int size);
            int GetStackSize();
        }

        internal abstract class HardwareStackAllocator : IStackAllocator
        {
            protected abstract int GetStackAlign();
            protected abstract bool AllocatesDownwards();

            protected int cur_stack_loc = 0;
            protected Dictionary<int, var> stack = new Dictionary<int,var>();

            public virtual int GetStackSize() { return cur_stack_loc; }

            public hardware_stackloc GetNextStackLoc(var var_id, int size)
            {
                // determine the actual size of the stack item (make a multiple of GetStackAlign())
                int s_align = GetStackAlign();
                int s_loc;

                if ((size % s_align) != 0)
                    size = ((size / s_align) + 1) * s_align;

                if (AllocatesDownwards())
                {
                    s_loc = cur_stack_loc - size;
                    cur_stack_loc = s_loc;
                }
                else
                {
                    s_loc = cur_stack_loc;
                    cur_stack_loc += size;
                }

                stack.Add(s_loc, var_id);

                return new hardware_stackloc { loc = s_loc };
            }

            public HardwareStackAllocator Clone()
            {
                HardwareStackAllocator clone = this.GetType().GetConstructor(new Type[] { }).Invoke(new object[] { }) as HardwareStackAllocator;
                clone.cur_stack_loc = cur_stack_loc;
                clone.stack = new Dictionary<int, var>(stack);
                return clone;
            }
        }

        public abstract class LocalVarArgHardwareAllocator
        {
            protected Dictionary<int, hardware_location> lvars = new Dictionary<int, hardware_location>();
            protected Dictionary<int, hardware_location> largs = new Dictionary<int, hardware_location>();
            protected Dictionary<int, hardware_location> logs = new Dictionary<int, hardware_location>();

            protected abstract hardware_location AllocateHardwareLocation(var v, int size);

            protected hardware_location GetHardwareLocation(int n, int size, var v, Dictionary<int, hardware_location> dict)
            {
                if (dict.ContainsKey(n))
                    return dict[n];

                if (size <= 0)
                    throw new Exception("Need to allocate hardware location but no size specified");

                hardware_location ret = AllocateHardwareLocation(v, size);
                if (ret == null)
                    throw new Exception("Unable to allocate hardware location");
                dict.Add(n, ret);
                return ret;
            }

            public hardware_location GetHardwareLocation(var v)
            { return GetHardwareLocation(v, 0); }

            public hardware_location GetHardwareLocation(var v, int size)
            {
                switch (v.type)
                {
                    case var.var_type.LocalArg:
                        return GetHardwareLocation(v.local_arg, size, v, largs);
                    case var.var_type.LocalVar:
                        return GetHardwareLocation(v.local_var, size, v, lvars);
                    case var.var_type.LogicalVar:
                        return GetHardwareLocation(v.logical_var, size, v, logs);
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public Assembler(Architecture arch, FileLoader fileLoader, MemberRequestor memberRequestor, AssemblerOptions options)
        {
            _arch = arch;
            CompiledMethodCache = new Dictionary<MethodToCompile,AssembleBlockOutput>(new libtysila.GenericEqualityComparer<MethodToCompile>());
            dry_run_cache = new Dictionary<MethodToCompile,bool>(new libtysila.GenericEqualityComparer<MethodToCompile>());

            if (options == null)
                _options = new AssemblerOptions();
            else
                _options = options;

            Loader = fileLoader;

            if (memberRequestor == null)
            {
                if (_options.MiniAssembler == false)
                {
                    if (arch.OutputFormat == "jit")
                        throw new Exception("JIT member requestor is not implemented yet");
                    _requestor = new FileBasedMemberRequestor();
                    _requestor.Assembler = this;
                }
                else
                    _requestor = null;
            }
            else
            {
                _requestor = memberRequestor;
                _requestor.Assembler = this;
            }

            InitOpcodes();
            arch_init_callconvs();
        }

        public class AssemblyInformation
        {
            public string name;
            public byte[] pubkey;
            public Version version;
            public Metadata m;
        }

        List<AssemblyInformation> loaded_assemblies = new List<AssemblyInformation>();

        internal void Debug(string s) { if (debugOutput != null) debugOutput.Write(s); }
        internal void Debug(string s, int level) { if (debugOutput != null) debugOutput.Write(s, level); }
        internal void DebugLine(string s) { if (debugOutput != null) debugOutput.WriteLine(s); }
        internal void DebugLine(string s, int level) { if (debugOutput != null) debugOutput.WriteLine(s, level); }

        public Metadata FindAssembly(Metadata.AssemblyRefRow arr)
        {
            AssemblyInformation ai = ContainsAssembly(arr.Name, arr.PublicKeyOrToken, arr.Version);
            if (ai != null)
                return ai.m;

            Metadata m = Metadata.LoadAssembly(arr.Name, this, output_name);
            AddAssembly(m);
            return m;
        }

        public Metadata FindAssembly(string name)
        {
            AssemblyInformation ai = ContainsAssembly(name, null, null);
            if (ai != null)
                return ai.m;

            Metadata m = Metadata.LoadAssembly(name, this, output_name);
            return AddAssembly(m);
        }

        public Metadata AddAssembly(Metadata m)
        {
            DebugLine("libtysila.Assembler.AddAssembly(" + m.ModuleName + ")");

            Metadata m2 = ContainsAssembly(m);
            if (m2 != null)
                return m2;
            Metadata.AssemblyRow ar = (Metadata.AssemblyRow)m.Tables[(int)Metadata.TableId.Assembly][0];
            AssemblyInformation ai = new AssemblyInformation
            {
                m = m,
                pubkey = ar.PublicKey,
                name = ar.Name,
                version = ar.Version
            };
            loaded_assemblies.Add(ai);
            m.Information = ai;

            /* If this is mscorlib, rewrite System.Delegate to use virtftnptr as the type of
             * method_ptr */
            if (m.ModuleName == "mscorlib")
            {
                Metadata.TypeDefRow delegate_tdr = Metadata.TypeDefRow.GetSystemDelegate(this);
                RewriteSystemDelegate(delegate_tdr);
            }

            /* Rewrite the .ctor of all delegate types and implement the ExtendsOverride attribute */
            foreach (Metadata.TypeDefRow tdr in m.Tables[(int)Metadata.TableId.TypeDef])
            {
                if (tdr.IsDelegate(this))
                {
                    foreach (Metadata.MethodDefRow mdr in tdr.Methods)
                    {
                        if (mdr.Name == ".ctor")
                        {
                            Signature.Method msig = mdr.GetSignature().Method;

                            if ((msig.Params.Count == 2) && (Signature.ParamCompare(msig.Params[0], new Signature.Param(BaseType_Type.Object), this)) &&
                                (Signature.ParamCompare(msig.Params[1], new Signature.Param(BaseType_Type.I), this)))
                            {
                                mdr.msig = mdr.GetSignature();
                                mdr.msig.Method.Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);
                            }
                        }
                    }
                }

                bool for_this_arch = false;
                bool for_any_arch = true;

                foreach (Metadata.CustomAttributeRow car in tdr.CustomAttrs)
                {
                    string caname = Mangler2.MangleMethod(Metadata.GetMTC(car.Type, new TypeToCompile(), null, this), this);

                    if (caname == "_ZX24ExtendsOverrideAttributeM_0_7#2Ector_Rv_P2u1tu1S")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        int offset = 2;
                        int len = Metadata.ReadCompressedInteger(car.Value, ref offset);
                        string s = new UTF8Encoding().GetString(car.Value, offset, len);

                        Assembler.TypeToCompile new_base = Mangler2.DemangleType(s, this);
                        tdr.Extends = new Metadata.TableIndex(new_base.type);
                    }
                    else if (caname == "_ZX20NoBaseClassAttributeM_0_7#2Ector_Rv_P1u1t")
                    {
                        tdr.Extends = new Metadata.TableIndex(tdr.m, 0, tdr.m.Tables[(int)Metadata.TableId.TypeDef]);
                    }

                    ForThisArchitecture(car, caname, ref for_this_arch, ref for_any_arch);
                }

                if (!for_this_arch && !for_any_arch)
                {
                    tdr.ExcludedByArch = true;
                    foreach (Metadata.MethodDefRow mdr in tdr.Methods)
                    {
                        mdr.ExcludedByArch = true;
                        mdr.IgnoreAttribute = true;
                    }
                }
            }

            /* Ignore those we want to */
            foreach (Metadata.MethodDefRow mdr in m.Tables[(int)Metadata.TableId.MethodDef])
            {
                bool for_this_arch = false;
                bool for_any_arch = true;

                foreach (Metadata.CustomAttributeRow car in mdr.CustomAttrs)
                {
                    string caname = Mangler2.MangleMethod(Metadata.GetMTC(car.Type, new TypeToCompile(), null, this), this);

                    if (caname == "_ZX29IgnoreImplementationAttributeM_0_7#2Ector_Rv_P1u1t")
                        mdr.IgnoreAttribute = true;
                    if (caname == "_ZX29MethodReferenceAliasAttributeM_0_7#2Ector_Rv_P2u1tu1S")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        int offset = 2;
                        int len = Metadata.ReadCompressedInteger(car.Value, ref offset);
                        mdr.ReferenceAlias = new UTF8Encoding().GetString(car.Value, offset, len);
                    }
                    if (caname == "_ZX26CallingConventionAttributeM_0_7#2Ector_Rv_P2u1tu1S")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        int offset = 2;
                        int len = Metadata.ReadCompressedInteger(car.Value, ref offset);
                        mdr.CallConvOverride = new UTF8Encoding().GetString(car.Value, offset, len);
                    }
                    if (caname == "_ZX20WeakLinkageAttributeM_0_7#2Ector_Rv_P1u1t")
                        mdr.WeakLinkage = true;
                    ForThisArchitecture(car, caname, ref for_this_arch, ref for_any_arch);
                }

                if (!for_this_arch && !for_any_arch)
                {
                    mdr.ExcludedByArch = true;
                    mdr.IgnoreAttribute = true;
                }
            }

            /* Load up the literal values stored in FieldRVAs */
            foreach (Metadata.FieldRVARow rvar in m.Tables[(int)Metadata.TableId.FieldRVA])
            {
                Metadata.FieldRow fr = Metadata.GetFieldDef(rvar.Field.ToToken(), this);

                int fsize = GetSizeOf(fr.GetSignature().AsParam(this));
                fr.LiteralData = new byte[fsize];
                m.File.GetDataAtRVA(fr.LiteralData, new UIntPtr(rvar.RVA), new UIntPtr((uint)fsize));
            }

            m.File.CloseFile();

            return m;
        }

        private void ForThisArchitecture(Metadata.CustomAttributeRow car, string caname, ref bool for_this_arch, ref bool for_any_arch)
        {
            /* We currently understand ArchDependentAttribute(string), OSDependentAttribute(string),
             * Bits64OnlyAttribute() and Bits32OnlyAttribute() */

            if (caname == "_ZX22ArchDependentAttributeM_0_7#2Ector_Rv_P2u1tu1S")
            {
                for_any_arch = false;
                string val = ReadAttributeString(car);
                if (IsArchCompatible(val))
                    for_this_arch = true;
            }
            else if (caname == "_ZX20OSDependentAttributeM_0_7#2Ector_Rv_P2u1tu1S")
            {
                for_any_arch = false;
                string val = ReadAttributeString(car);
                if (IsOSCompatible(val))
                    for_this_arch = true;
            }
            else if (caname == "_ZX19Bits64OnlyAttributeM_0_7#2Ector_Rv_P1u1t")
            {
                for_any_arch = false;
                if (GetBitness() == Bitness.Bits64)
                    for_this_arch = true;
            }
            else if (caname == "_ZX19Bits32OnlyAttributeM_0_7#2Ector_Rv_P1u1t")
            {
                for_any_arch = false;
                if (GetBitness() == Bitness.Bits32)
                    for_this_arch = true;
            }
        }

        private string ReadAttributeString(Metadata.CustomAttributeRow car)
        {
            if (car.Value[0] != 0x01)
                throw new NotSupportedException();
            if (car.Value[1] != 0x00)
                throw new NotSupportedException();
            int offset = 2;
            int len = Metadata.ReadCompressedInteger(car.Value, ref offset);
            return new UTF8Encoding().GetString(car.Value, offset, len);
        }

        Metadata ContainsAssembly(Metadata m)
        {
            Metadata.AssemblyRow ar = (Metadata.AssemblyRow)m.Tables[(int)Metadata.TableId.Assembly][0];
            AssemblyInformation ai = ContainsAssembly(ar.Name, ar.PublicKey, ar.Version);
            if(ai == null)
                return null;
            else
                return ai.m;
        }

        Metadata ContainsAssembly(Metadata.AssemblyRefRow arr)
        {
            AssemblyInformation ai = ContainsAssembly(arr.Name, arr.PublicKeyOrToken, arr.Version);
            if (ai == null)
                return null;
            else
                return ai.m;
        }

        AssemblyInformation ContainsAssembly(string name, byte[] pubkey, Version version)
        {
            foreach (AssemblyInformation ai in loaded_assemblies)
            {
                if ((ai.name == name) || ((ai.name == "corlib") && (name == "mscorlib")))
                    return ai;

                // TODO check pubkey and version if not null
            }
            return null;
        }

        public List<AssemblyInformation> GetLoadedAssemblies()
        {
            return loaded_assemblies;
        }

#if MT
        public void AssembleMethod(object mtcr) { AssembleMethod(mtcr as MTCRequest); }
        public void AssembleMethod(MTCRequest mtcr) { AssembleMethod(mtcr.mtc, mtcr.of); }
#endif
        public AssembleBlockOutput AssembleMethod(MethodToCompile mtc) { return AssembleMethod(mtc, false); }
        public AssembleBlockOutput AssembleMethod(MethodToCompile mtc, bool dry_run)
        {
            if (CompiledMethodCache.ContainsKey(mtc))
                return CompiledMethodCache[mtc];
            else
                return AssembleMethod(mtc, null, null, false, dry_run);
        }

        public AssembleBlockOutput AssembleMethod(MethodToCompile mtc, IOutputFile output, List<string> unimplemented_internal_calls)
        { return AssembleMethod(mtc, output, unimplemented_internal_calls, true, false); }

        private class StartNodeDefinition
        {
            public int node_id;
            public Metadata.MethodBody.EHClause exception_clause;
        }

        public class AssembleBlockOutput
        {
            public IList<byte> code;
            public IList<RelocationBlock> relocs;
            public IList<OutputBlock> symbols;
            public IDictionary<int, InstructionHeader> instrs;
            public IList<OutputBlock> prolog;

            public int compiled_code_length;
        }

        internal virtual void ArchSpecificCode(AssemblerState state, List<OutputBlock> blocks)
        { }

        internal virtual List<OutputBlock> ArchSpecificProlog(AssemblerState state) { return new List<OutputBlock>(); }

        private void GenerateGlobalVars(List<int> global_vars, Dictionary<int, int> global_vars_dict)
        {
            foreach (KeyValuePair<int, int> kvp in global_vars_dict)
            {
                if (!global_vars.Contains(kvp.Value))
                    global_vars.Add(kvp.Value);
            }
        }

        internal var_semantic GetSemantic(CliType ct, int? vt_size)
        {
            var_semantic vs = new var_semantic();

            switch (ct)
            {
                case Assembler.CliType.F64:
                    vs.needs_float64 = true;
                    vs.vtype_size = 8;
                    break;
                case CliType.F32:
                    vs.needs_float32 = true;
                    vs.vtype_size = 4;
                    break;
                case Assembler.CliType.int32:
                    vs.needs_int32 = true;
                    vs.vtype_size = 4;
                    break;
                case Assembler.CliType.int64:
                    vs.needs_int64 = true;
                    vs.vtype_size = 8;
                    break;
                case Assembler.CliType.native_int:
                case Assembler.CliType.O:
                case Assembler.CliType.reference:
                    vs.needs_intptr = true;
                    vs.vtype_size = GetSizeOfPointer();
                    break;
                case Assembler.CliType.vt:
                    vs.needs_vtype = true;
                    if ((!vt_size.HasValue) || (vt_size.Value == 0))
                        throw new Exception("VTSize not defined");
                    vs.vtype_size = vt_size.Value;
                    break;
                case CliType.virtftnptr:
                    vs.needs_virtftnptr = true;
                    vs.vtype_size = 2 * GetSizeOfPointer();
                    break;
            }
            return vs;
        }

        void MergeSemantic(var v, var_semantic semantic, Dictionary<int, var_semantic> semantics)
        {
            if (semantics.ContainsKey(v))
                semantics[v].Merge(semantic);
            else
                semantics[v] = semantic;
        }

        void AddMemlocSemantic(var v, Dictionary<int, var_semantic> semantics)
        {
            if (v.address_of && v.base_var.v.type == var.var_type.LogicalVar)
            {
                var_semantic memloc_semantic = new var_semantic { needs_memloc = true };
                MergeSemantic(v.base_var.v.logical_var, memloc_semantic, semantics);
            }
        }






        private int GetReferencedLogicalVar(var var)
        {
            if (var.base_var != null)
                return GetReferencedLogicalVar(var.base_var.v);
            if (var.type == libtysila.var.var_type.LogicalVar)
                return var.logical_var;
            return var.Null;
        }




        internal virtual bool ArchSpecificEvaluateConstant(ThreeAddressCode threeAddressCode)
        { return false; }





        internal virtual void ArchSpecificStackSetup(AssemblerState state, ref int next_lv_loc) { }
        internal abstract hloc_constraint GetConstraintFromSemantic(var_semantic vs);
        internal abstract void arch_init_opcodes();
        internal virtual bool IsArchCompatible(string arch)
        {
            foreach (ArchAssembler a in _ListArchitectures())
            {
                if (a.Assembler.Equals(this.GetType()) && a.Architecture._instruction_set.Equals(arch))
                    return true;
            }
            return false;
        }
        internal virtual bool IsOSCompatible(string os)
        {
            foreach (ArchAssembler a in _ListArchitectures())
            {
                if (a.Assembler.Equals(this.GetType()) && a.Architecture._os.Equals(os))
                    return true;
            }
            return false;
        }
        
        public enum Bitness { Bits32, Bits64 };
        public abstract Bitness GetBitness();

        internal abstract IEnumerable<hardware_location> GetAllHardwareLocationsOfType(System.Type type, hardware_location example);
        internal virtual bool IsLocationAllowed(hardware_location hloc) { return true; }

        //protected abstract hardware_location GetLocalArgLocation(var v);
        //protected abstract hardware_location GetLocalVarLocation(var v);

        internal abstract List<byte> SaveLocation(hardware_location loc);
        internal abstract List<byte> RestoreLocation(hardware_location loc);
        internal abstract List<byte> SwapLocation(hardware_location a, hardware_location b);

        public abstract uint DataToDataRelocType();
        internal abstract byte[] IntPtrByteArray(object v);
        internal abstract object ConvertToI(object v);
        internal abstract object ConvertToU(object v);

        internal abstract int GetSizeOfUncondBr();

        internal virtual UInt32 GetTysosFlagsForMethod(MethodToCompile mtc) { return 0; }

        internal abstract int GetSizeOf(Signature.Param p);

        internal virtual hardware_location GetMethinfoPointerLocation() { return null; }

        public abstract MiniAssembler GetMiniAssembler();

        internal abstract tydb.Location GetDebugLocation(hardware_location loc);
        public abstract int GetPackedSizeOf(Signature.Param p);
        public virtual int GetSizeOfIntPtr()
        {
            return GetSizeOf(new Signature.Param(BaseType_Type.I));
        }
        public virtual int GetSizeOfUIntPtr()
        {
            return GetSizeOf(new Signature.Param(BaseType_Type.U));
        }
        public virtual int GetSizeOfPointer()
        {
            return GetSizeOf(new Signature.Param(BaseType_Type.I));
        }
        internal virtual int GetSizeOfType(Signature.Param p)
        {
            Metadata.TypeDefRow tdr = Metadata.GetTypeDef(p.Type, this);
            if ((tdr.Layout != null) && (tdr.Layout.ClassSize != 0))
                return (int)tdr.Layout.ClassSize;
            return Layout.GetClassInstanceSize(new TypeToCompile { _ass = this, type = tdr, tsig = p }, this);
        }

        public virtual int GetArrayStart()
        {
            //return GetArrayFieldOffset(ArrayFields.inner_array);
            throw new NotImplementedException();
        }

        public string GetName()
        {
            if (this.GetType().Name.EndsWith("_Assembler"))
                return this.GetType().Name.Substring(0, this.GetType().Name.Length - 10);
            else
                return this.GetType().Name;
        }

        internal virtual void InterpretMethodCustomAttribute(Assembler.MethodToCompile mtc, Metadata.CustomAttributeRow car, AssemblerState state)
        { }

        internal virtual AssemblerState GetNewAssemblerState()
        { return new AssemblerState(); }

        public class InvalidOpcodeException : Exception
        {
            public InvalidOpcodeException(int opcode) {
                _opcode = opcode;
            }

            public int Opcode { get { return _opcode; } }

            private int _opcode;
        }

        public class StackUnderflowException : Exception { }

        /* CIL defines several internal data types: int32, int64, native_int, F, O and reference
         * 
         * We replace F with F32 and F64 to more easily model float32 and float64 values internally
         * 
         * In addition we add virtftnptr for security.  This is a type for storing a method pointer.
         * Specifically, the calli instruction only accepts a virtftnptr as input (rather than native
         * int as specified in CIL).  Likewise, a virtftnptr can only be created by the CIL instructions
         * ldftnptr and ldvirtftnptr.  This means it is impossible for user code to supply an arbritrary
         * pointer to the calli instruction (this prevents, for example, native machine code being written
         * to memory and then the address of that being passed to calli).
         * 
         * Given that the only functions we can take the address of using ldftn/ldvirtftn are managed
         * ones that have been compiled by tysila this ensures the safety of calli
         */

        public enum CliType
        {
            none, int32, int64, native_int, F32, F64, O, reference, void_, vt, virtftnptr
        }

        #region IByteOperations Members
        public abstract object FromByteArray(BaseType_Type type, IList<byte> v, int offset);
        public abstract object FromByteArray(BaseType_Type type, IList<byte> v);
        public abstract ulong FromByteArrayU8(IList<byte> v);
        public abstract ulong FromByteArrayU8(IList<byte> v, int offset);
        public abstract long FromByteArrayI8(IList<byte> v);
        public abstract long FromByteArrayI8(IList<byte> v, int offset);
        public abstract uint FromByteArrayU4(IList<byte> v);
        public abstract uint FromByteArrayU4(IList<byte> v, int offset);
        public abstract int FromByteArrayI4(IList<byte> v);
        public abstract int FromByteArrayI4(IList<byte> v, int offset);
        public abstract byte FromByteArrayU1(IList<byte> v);
        public abstract byte FromByteArrayU1(IList<byte> v, int offset);
        public abstract sbyte FromByteArrayI1(IList<byte> v);
        public abstract sbyte FromByteArrayI1(IList<byte> v, int offset);
        public abstract ushort FromByteArrayU2(IList<byte> v);
        public abstract ushort FromByteArrayU2(IList<byte> v, int offset);
        public abstract short FromByteArrayI2(IList<byte> v);
        public abstract short FromByteArrayI2(IList<byte> v, int offset);
        public abstract char FromByteArrayChar(IList<byte> v);
        public abstract char FromByteArrayChar(IList<byte> v, int offset);
        public abstract float FromByteArrayR4(IList<byte> v);
        public abstract float FromByteArrayR4(IList<byte> v, int offset);
        public abstract double FromByteArrayR8(IList<byte> v);
        public abstract double FromByteArrayR8(IList<byte> v, int offset);
        public abstract IntPtr FromByteArrayI(IList<byte> v);
        public abstract IntPtr FromByteArrayI(IList<byte> v, int offset);
        public abstract UIntPtr FromByteArrayU(IList<byte> v);
        public abstract UIntPtr FromByteArrayU(IList<byte> v, int offset);
        public abstract byte[] ToByteArray(byte v);
        public abstract byte[] ToByteArray(sbyte v);
        public abstract byte[] ToByteArray(short v);
        public abstract byte[] ToByteArray(ushort v);
        public abstract byte[] ToByteArray(int v);
        public abstract byte[] ToByteArray(uint v);
        public abstract byte[] ToByteArray(long v);
        public abstract byte[] ToByteArray(ulong v);
        public abstract byte[] ToByteArray(IntPtr v);
        public abstract byte[] ToByteArray(UIntPtr v);
        public abstract byte[] ToByteArray(ValueType v);
        public abstract byte[] ToByteArray(bool v);
        public abstract byte[] ToByteArray(float v);
        public abstract byte[] ToByteArray(double v);
        public abstract byte[] ToByteArrayZeroExtend(object v, int byte_count);
        public abstract byte[] ToByteArraySignExtend(object v, int byte_count);
        public abstract void SetByteArray(IList<byte> target, int t_offset, IList<byte> source, int s_offset, int s_size);
        public abstract void SetByteArray(IList<byte> target, int t_offset, byte v);
        public abstract void SetByteArray(IList<byte> target, int t_offset, short v);
        public abstract void SetByteArray(IList<byte> target, int t_offset, int v);
        public abstract void SetByteArray(IList<byte> target, int t_offset, int v, int v_size);
        public virtual void SetByteArray(IList<byte> target, int t_offset, IList<byte> source) { SetByteArray(target, t_offset, source, 0, source.Count); }
        #endregion

        public abstract string GetCType(BaseType_Type baseType_Type);

        public string GetUnderlyingCType(Metadata.TypeDefRow tdr)
        {
            if (!tdr.IsEnum(this))
                throw new InvalidOperationException();
            Layout l = Layout.GetLayout(new TypeToCompile { _ass = this, type = tdr, tsig = new Signature.Param(tdr, this) }, this);
            return GetCType(l.GetFirstInstanceField("value__").field.fsig);
        }

        public string GetCType(Signature.Param fsig)
        {
            if (fsig.Type is Signature.BaseType)
                return GetCType(((Signature.BaseType)fsig.Type).Type);
            else if (fsig.Type is Signature.ComplexType)
            {
                Signature.ComplexType ct = fsig.Type as Signature.ComplexType;
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(ct.Type, this);
                if (tdr.IsEnum(this))
                    return GetUnderlyingCType(tdr);
                else if (tdr.IsValueType(this))
                    return "struct " + tdr.TypeNamespace + "_" + tdr.TypeName;
                else
                    return GetCType(BaseType_Type.U);
            }
            else if (fsig.Type is Signature.ZeroBasedArray)
            {
                return GetCType(BaseType_Type.U);
            }
            else
                throw new NotSupportedException();
        }

        public string GetCType(Metadata.FieldRow fr)
        {
            Signature.Field fsig = Signature.ParseFieldSig(fr.m, fr.Signature, this);
            return GetCType(fsig.AsParam(this));
        }

       public virtual byte[] ToByteArray(object v)
        {
            if (v is bool)
                return ToByteArray((bool)v);
            else if (v is byte)
                return ToByteArray((byte)v);
            else if (v is sbyte)
                return ToByteArray((sbyte)v);
            else if (v is short)
                return ToByteArray((short)v);
            else if (v is ushort)
                return ToByteArray((ushort)v);
            else if (v is int)
                return ToByteArray((int)v);
            else if (v is uint)
                return ToByteArray((uint)v);
            else if (v is long)
                return ToByteArray((long)v);
            else if (v is ulong)
                return ToByteArray((ulong)v);
            else if (v is IntPtr)
                return ToByteArray((IntPtr)v);
            else if (v is UIntPtr)
                return ToByteArray((UIntPtr)v);
            else if (v is float)
                return ToByteArray((float)v);
            else if (v is double)
                return ToByteArray((double)v);
            else if (v is ValueType)
                return ToByteArray((ValueType)v);
            else
                throw new Exception("Unable to convert that type to byte string");
        }

        internal class TooManyIterationsException : AssemblerException
        {
            public TooManyIterationsException(MethodToCompile mtc) : base("Unable to assign registers in method", null, mtc) { }
        }

        internal class TooManyCoercionsException : AssemblerException
        {
            ThreeAddressCode tac;
            public ThreeAddressCode IRInstruction { get { return tac; } }
            public TooManyCoercionsException(MethodToCompile mtc, ThreeAddressCode _inst) : base("Too many coercions attempting to find machine encoding for instruction " + _inst.Operator.ToString(), null, mtc) { tac = _inst; }
        }

        internal Dictionary<BaseType_Type, Metadata.TypeDefRow> basetype_tdrs = new Dictionary<BaseType_Type, Metadata.TypeDefRow>(new BaseType_TypeEqualityComparer());
        internal Metadata.TypeDefRow GetBasetypeTypedef(Signature.BaseType bt)
        {
            if (basetype_tdrs.ContainsKey(bt.Type))
                return basetype_tdrs[bt.Type];

            Metadata corlib = FindAssembly(new Metadata.AssemblyRefRow { Name = "mscorlib" });

            if (bt.Type == BaseType_Type.VirtFtnPtr)
            {
                Metadata.TypeDefRow vfp_tdr = new Metadata.TypeDefRow { m = corlib, ass = this };
                vfp_tdr._ActualTypeName = "VirtFtnPtr";
                vfp_tdr._ActualTypeNamespace = "System";
                vfp_tdr.CustomAttrs = new List<Metadata.CustomAttributeRow>();
                vfp_tdr.Extends = new Metadata.TableIndex(Metadata.TypeDefRow.GetSystemValueType(this));
                vfp_tdr.Fields = new List<Metadata.FieldRow>();

                Metadata.FieldRow vfp_fr = new Metadata.FieldRow { m = corlib, ass = this };
                vfp_fr.CustomAttrs = new List<Metadata.CustomAttributeRow>();
                vfp_fr.Flags = 0x0;
                vfp_fr.fsig = new Signature.Field(new Signature.Param(BaseType_Type.VirtFtnPtr));
                vfp_fr.Name = "m_value";
                vfp_fr.owning_type = vfp_tdr;

                vfp_tdr.Fields.Add(vfp_fr);
                vfp_tdr.Flags = 0x100109;
                vfp_tdr.Methods = new List<Metadata.MethodDefRow>();

                basetype_tdrs.Add(bt.Type, vfp_tdr);
                return vfp_tdr;
            }
            else if (bt.Type == BaseType_Type.Void)
            {
                Metadata.TypeDefRow void_tdr = new Metadata.TypeDefRow { m = corlib, ass = this };
                void_tdr._ActualTypeName = "Void";
                void_tdr._ActualTypeNamespace = "System";
                void_tdr.CustomAttrs = new List<Metadata.CustomAttributeRow>();
                void_tdr.Extends = new Metadata.TableIndex(Metadata.TypeDefRow.GetSystemValueType(this));
                void_tdr.Fields = new List<Metadata.FieldRow>();
                void_tdr.Flags = 0x100109;
                void_tdr.Methods = new List<Metadata.MethodDefRow>();

                basetype_tdrs.Add(bt.Type, void_tdr);
                return void_tdr;
            }
            else if (bt.Type == BaseType_Type.UninstantiatedGenericParam)
            {
                Metadata.TypeDefRow ugp_tdr = new Metadata.TypeDefRow { m = corlib, ass = this };
                ugp_tdr._ActualTypeName = "UnstantiatedGenericParam";
                ugp_tdr._ActualTypeNamespace = "System";
                ugp_tdr.CustomAttrs = new List<Metadata.CustomAttributeRow>();
                ugp_tdr.Extends = new Metadata.TableIndex(Metadata.TypeDefRow.GetSystemValueType(this));
                ugp_tdr.Fields = new List<Metadata.FieldRow>();
                ugp_tdr.Flags = 0x100109;
                ugp_tdr.Methods = new List<Metadata.MethodDefRow>();

                basetype_tdrs.Add(bt.Type, ugp_tdr);
                return ugp_tdr;
            }

            foreach (Metadata.TypeDefRow tdr in corlib.Tables[(int)Metadata.TableId.TypeDef])
            {
                if ((tdr.TypeNamespace == "System") && (tdr.TypeName == bt.GetTypeName()))
                {
                    basetype_tdrs.Add(bt.Type, tdr);
                    return tdr;
                }
            }
            return null;
        }
    }

    class AssemblerInfo
    {
        public enum ProcessorType
        {
            x86, x86_64, PPC, IA64, ARM, Any, Other
        }

        public enum ReqSystem
        {
            Win32, WinCE, OSX, Linux, FreeBSD, OpenBSD, Unix, Any, None
        }

        public enum SystemExtension
        {
            Gui, Vista, WinXP, Win2000, WinNT4, WM6, WM5, PPC2003, CellularAccess, Linux_2_6, Linux_2_4, Linux_2_2,
            Linux_2_0, KDE, GNOME, X11, SSL
        }

        ProcessorType pt;
        ReqSystem st;
        bool pic;
        byte pad_byte;
        int default_alignment;

        public ProcessorType Processor { get { return pt; } }
        public ReqSystem System { get { return st; } }
        public bool PIC { get { return pic; } }
        public byte PadByte { get { return pad_byte; } }
        public int DefaultAlignment { get { return default_alignment; } }

        public AssemblerInfo(ProcessorType ptype, bool make_pic, byte padbyte, int defalign)
        { pt = ptype; pic = make_pic; st = ReqSystem.None; pad_byte = padbyte; default_alignment = defalign; }
    }
}
