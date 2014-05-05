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

        public static string VersionString { get { return "0.2.0"; } }

        public class AssemblerException : Exception
        {
            InstructionLine _inst;
            internal InstructionLine Instruction { get { return _inst; } }
            MethodToCompile _mtc;
            internal MethodToCompile Method { get { return _mtc; } }

            public AssemblerException(string msg, InstructionLine inst, MethodToCompile mtc) : base(msg) { _inst = inst; _mtc = mtc; }

            public override string Message
            {
                get
                {
                    return base.Message + ((_inst == null) ? "" : (" at IL_" + _inst.il_offset.ToString("x4") + ": " + _inst.ToString())) + " in " + _mtc.ToString();
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

            internal Assembler.IVarToHLocProvider var_hlocs = null;

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
            InitArchOpcodes();
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

        public class InstructionLine
        {
            public class PrefixesClass
            {
                public bool constrained = false;
                public Token constrained_tok = null;
                public bool no_typecheck = false;
                public bool no_rangecheck = false;
                public bool no_nullcheck = false;
                public bool read_only = false;
                public bool tail = false;
                public bool unaligned = false;
                public int unaligned_alignment = 0;
                public bool volatile_ = false;
            }

            public PrefixesClass Prefixes = new PrefixesClass();

            public int il_offset, il_offset_after;
            public Opcode opcode;
            public int inline_int;
            public uint inline_uint;
            public long inline_int64;
            public double inline_dbl;
            public float inline_sgl;
            public Token inline_tok;
            public List<int> inline_array = new List<int>();

            public bool from_cil = false;

            public int stack_before_adjust = 0;

            public bool start_block = false;
            public bool end_block = false;

            public bool int_array = false;

            public bool allow_obj_numop = false;

            public List<int> il_offsets_after = new List<int>();

            public Signature.Param pushes;
            public var pushes_variable = var.Null;
            public int pop_count;

            public List<var> node_global_vars = new List<var>();

            public List<PseudoStack> stack_before, stack_after, lv_before, lv_after, la_before, la_after;
            public List<ThreeAddressCode> tacs = new List<ThreeAddressCode>();

            internal cfg_node cfg_node;

            public override string ToString()
            {
                if (opcode != null)
                    return opcode.name;
                return base.ToString();
            }
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
        public AssembleBlockOutput AssembleMethod2(MethodToCompile mtc, IOutputFile output, List<string> unimplemented_internal_calls, bool cache_output, bool dry_run)
        {
            Metadata.MethodDefRow meth = mtc.meth;
            Signature.BaseMethod call_site = mtc.msig;
            Metadata.TypeDefRow tdr = mtc.type;
            Signature.BaseOrComplexType tsig = mtc.tsig;
            Metadata m = meth.m;
            AssemblerState state = GetNewAssemblerState();
            string mangled_name = Mangler2.MangleMethod(mtc, this);
            List<string> method_aliases = new List<string>();

            bool cls_compliant = true;
            bool uses_vararg = false;

            if (_options.CoalesceGenericRefTypes)
            {
                if (mtc.GenMethodType == MethodToCompile.GenericMethodType.CallsRefImplementation)
                    state.calls_ref_implementation = true;
                else if (mtc.GenMethodType == MethodToCompile.GenericMethodType.RefImplementation)
                    state.is_ref_implementation = true;
            }

            //state.reg_alloc = GetRegisterAllocator();

            if (mtc.meth.m.Information.name == "mscorlib")
                state.security = AssemblerSecurity.CorlibSecurity;
            if (mtc.meth.m.Information.name == "libsupcs")
                state.security = AssemblerSecurity.CorlibSecurity;

            if (mtc.meth.CallConvOverride != null)
                state.call_conv = mtc.meth.CallConvOverride;

            state.profile = profile;

            state.orig_sig = mtc.msig;

            /* Parse relevant attributes */
            foreach (Metadata.CustomAttributeRow car in m.Tables[(int)Metadata.TableId.CustomAttribute])
            {
                Metadata.MethodDefRow mdr = Metadata.GetMethodDef(car.Parent, this);
                if (mdr == meth)
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
                        method_aliases.Add(s);
                    }

                    if (caname == "_ZX24UninterruptibleAttributeM_0_7#2Ector_Rv_P1u1t")
                        state.uninterruptible_method = true;

                    if (caname == "_ZX22ExtraArgumentAttributeM_0_7#2Ector_Rv_P3u1tii")
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

                    if (caname == "_ZW6System21CLSCompliantAttributeM_0_7#2Ector_Rv_P2u1tb")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        if (car.Value[2] == 0x00)
                            cls_compliant = false;
                    }

                    if (caname == "_ZX16ProfileAttributeM_0_7#2Ector_Rv_P2u1tb")
                    {
                        if (car.Value[0] != 0x01)
                            throw new NotSupportedException();
                        if (car.Value[1] != 0x00)
                            throw new NotSupportedException();
                        if (car.Value[2] == 0x00)
                            state.profile = false;
                        else
                            state.profile = true;
                    }

                    if (caname == "_ZX16SyscallAttributeM_0_7#2Ector_Rv_P1u1t")
                        state.syscall = true;

                    InterpretMethodCustomAttribute(mtc, car, state);
                }
            }

            if(call_site == null)
                call_site = Signature.ParseMethodDefSig(meth.m, meth.Signature, this);

            if (call_site.Method.CallingConvention == Signature.Method.CallConv.VarArg)
                uses_vararg = true;

            List<cfg_node> nodes = null;
          
            // Get the implementation
            byte[] impl = meth.Body.Body;
            if ((impl == null) && (meth.nodes == null))
            {
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
                string short_mangled_name = mangled_name;
                if (short_mangled_name.StartsWith("_ZX14CastOperationsM_0_") && short_mangled_name.Contains("GetArg"))
                {
#if MT
                    rtc--;
#endif
                    return null;
                }

                bool provides = false;

                if (meth.IsInternalCall || meth.IsPinvokeImpl)
                {
                    if (provides_intcall(mtc))
                    {
                        provides = true;

                        /* ldarg x n, call, ret */
                        cfg_node node = new cfg_node(0, mtc);

                        for (int i = 0; i < get_arg_count(mtc.msig); i++)
                            node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = i });
                        node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = mtc } });
                        node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

                        meth.nodes = new List<cfg_node>();
                        meth.nodes.Add(node);
                    }
                    else
                    {
                        /* The internal call needs to be provided by the system */
                        if (unimplemented_internal_calls != null)
                        {
                            lock (unimplemented_internal_calls)
                            {
                                if (!unimplemented_internal_calls.Contains(mangled_name))
                                    unimplemented_internal_calls.Add(mangled_name);
                            }
                        }
                    }
                }
                
                if(!((meth.IsInternalCall || meth.IsPinvokeImpl) && provides))
                {
                    if (!GenerateDelegateFunction(mtc, state))
                    {
#if MT
                        rtc--;
#endif
                        return null;
                    }
                }
            }

            /* Methods marked CLSCompliantAttribute(false) are not supported - rewrite them to throw an exception */
            if ((cls_compliant == false) && (state.security.AllowCLSCompliantFalse == false))
            {
                cfg_node node = new cfg_node(0, mtc);

                node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldstr], inline_tok = new StringToken { str = "Method " + mtc.meth.Name + " is marked CLSCompliantAttribute(false), which is not supported" } });

                TypeToCompile exception_type = Metadata.GetTTC("mscorlib", "System", "Exception", this);
                Signature.BaseMethod ctor_sig = new Signature.Method
                    {
                        HasThis = true,
                        CallingConvention = libtysila.Signature.Method.CallConv.Default,
                        ExplicitThis = false,
                        GenParamCount = 0,
                        m = exception_type.type.m,
                        ParamCount = 1,
                        Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.String) },
                        RetType = new Signature.Param(BaseType_Type.Void)
                    };
                MethodToCompile ctor = new MethodToCompile
                {
                    _ass = this,
                    meth = Metadata.GetMethodDef(exception_type.type.m, ".ctor", exception_type.type, ctor_sig, this),
                    msig = ctor_sig,
                    tsigp = exception_type.tsig,
                    type = exception_type.type
                };
                node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.newobj], inline_tok = new MTCToken { mtc = ctor } });

                node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.throw_] });

                nodes = new List<cfg_node>();
                nodes.Add(node);
            }
            else if (uses_vararg)
            {
                cfg_node node = new cfg_node(0, mtc);

                node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldstr], inline_tok = new StringToken { str = "Method " + mtc.meth.Name + " uses the vararg calling convention, which is not supported" } });

                TypeToCompile exception_type = Metadata.GetTTC("mscorlib", "System", "Exception", this);
                Signature.BaseMethod ctor_sig = new Signature.Method
                {
                    HasThis = true,
                    CallingConvention = libtysila.Signature.Method.CallConv.Default,
                    ExplicitThis = false,
                    GenParamCount = 0,
                    m = exception_type.type.m,
                    ParamCount = 1,
                    Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.String) },
                    RetType = new Signature.Param(BaseType_Type.Void)
                };
                MethodToCompile ctor = new MethodToCompile
                {
                    _ass = this,
                    meth = Metadata.GetMethodDef(exception_type.type.m, ".ctor", exception_type.type, ctor_sig, this),
                    msig = ctor_sig,
                    tsigp = exception_type.tsig,
                    type = exception_type.type
                };
                node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.newobj], inline_tok = new MTCToken { mtc = ctor } });

                node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.throw_] });

                nodes = new List<cfg_node>();
                nodes.Add(node);
            }
            else
            {
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

                Signature.Method msig = null;
                if (mtc.msig is Signature.Method)
                    msig = mtc.msig as Signature.Method;
                else if (mtc.msig is Signature.GenericMethod)
                    msig = ((Signature.GenericMethod)mtc.msig).GenMethod;
                else
                    throw new Exception();

                if (msig.HasThis && (mtc.tsig is Signature.BoxedType))
                {
                    /* write out a trampoline function */
                    cfg_node node = new cfg_node(0, mtc);

                    TypeToCompile unboxed_type = new TypeToCompile { _ass = this, type = mtc.type, tsig = new Signature.Param(((Signature.BoxedType)mtc.tsigp.Type).Type, this) };

                    node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
                    node.instrs.Add(new InstructionLine
                    {
                        opcode = Opcodes[(int)SingleOpcodes.unbox],
                        inline_tok = new TTCToken
                        {
                            ttc = unboxed_type
                        }
                    });
                    for (int i = 1; i < get_arg_count(msig); i++)
                        node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfe09], inline_int = i });

                    MethodToCompile unboxed_meth = new MethodToCompile { _ass = this, tsigp = unboxed_type.tsig, type = unboxed_type.type, meth = mtc.meth, msig = mtc.msig };
                    node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.call], inline_tok = new MTCToken { mtc = unboxed_meth } });
                    node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

                    nodes = new List<cfg_node>();
                    nodes.Add(node);
                }
                else
                {
                    if (meth.nodes == null)
                    {
                        // Build a control graph
                        nodes = BuildControlGraph(impl, m, state, mtc);
                    }
                    else
                    {
                        /* The instructions are already encoded in the method - we do not have to parse it
                         * 
                         * However, we need to create new copies of the node objects as they will contain the
                         * intermediate stages of IR creation after this method is run.  If we try to compile
                         * the same method again, then they will be present in the new compilation attempt
                         * and likely cause problems (inaccessible variables etc).
                         */

                        Dictionary<int, cfg_node> node_dict = new Dictionary<int, cfg_node>(); ;
                        nodes = new List<cfg_node>();
                        foreach (cfg_node orig_node in meth.nodes)
                        {
                            cfg_node new_node = new cfg_node(orig_node.block_id, mtc);

                            foreach (InstructionLine orig_instr in orig_node.instrs)
                            {
                                new_node.instrs.Add(new InstructionLine
                                {
                                    opcode = orig_instr.opcode,
                                    inline_array = orig_instr.inline_array,
                                    inline_dbl = orig_instr.inline_dbl,
                                    inline_int = orig_instr.inline_int,
                                    inline_int64 = orig_instr.inline_int64,
                                    inline_tok = orig_instr.inline_tok,
                                    il_offset = orig_instr.il_offset,
                                    int_array = orig_instr.int_array
                                });
                            }

                            if (orig_node.ipred_ids == null)
                                new_node.ipred_ids = new List<int>();
                            else
                                new_node.ipred_ids = new List<int>(orig_node.ipred_ids);

                            if (orig_node.isuc_ids == null)
                                new_node.isuc_ids = new List<int>();
                            else
                                new_node.isuc_ids = new List<int>(orig_node.isuc_ids);

                            node_dict.Add(new_node.block_id, new_node);
                            nodes.Add(new_node);
                        }

                        /* Patch up ipred and isuc */
                        foreach (cfg_node node in nodes)
                        {
                            node.isuc = new List<cfg_node>();
                            node.ipred = new List<cfg_node>();

                            foreach (int pred in node.ipred_ids)
                                node.ipred.Add(node_dict[pred]);
                            foreach (int suc in node.isuc_ids)
                                node.isuc.Add(node_dict[suc]);
                        }
                    }
                }
            }

            /* Ensure there is no 'ldtoken' instruction in the instruction stream in coalesced methods */
            if (state.calls_ref_implementation)
            {
                foreach (cfg_node node in nodes)
                {
                    foreach (InstructionLine instr in node.instrs)
                    {
                        if (instr.opcode.opcode == (int)SingleOpcodes.ldtoken)
                        {
                            state.calls_ref_implementation = false;
                            break;
                        }
                    }
                    if (!state.calls_ref_implementation)
                        break;
                }
            }

            // Add in profiling code if required
            if (state.profile)
            {
                nodes[0].instrs.Insert(0, new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldstr], inline_tok = new StringToken { str = mangled_name } });
                nodes[0].instrs.Insert(1, new InstructionLine { opcode = Opcodes[0xfd29] });
                state.ret_decomposed = false;
            }
            state.mangled_name = mangled_name;

            // Build a list of start nodes
            List<StartNodeDefinition> start_nodes = new List<StartNodeDefinition>();
            start_nodes.Add(new StartNodeDefinition { exception_clause = null, node_id = 0 });

            Dictionary<int, int> instrs_to_node = new Dictionary<int, int>();
            bool multiple_nodes_with_same_iloffset = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (instrs_to_node.ContainsKey(nodes[i].il_offset))
                    multiple_nodes_with_same_iloffset = true;
                else
                    instrs_to_node.Add(nodes[i].il_offset, i);
            }

            if (multiple_nodes_with_same_iloffset & (meth.Body.exceptions.Count != 0))
                throw new Exception("Apparently CIL written method with exceptions - not allowed!");

            foreach (Metadata.MethodBody.EHClause eh in meth.Body.exceptions)
            {
                int node = instrs_to_node[(int)eh.HandlerOffset];
                start_nodes.Add(new StartNodeDefinition { node_id = node, exception_clause = eh });

                /* If this is a catch or filter exception handler, insert an instruction to load the passed exception object */
                if (eh.IsCatch)
                    nodes[node].instrs.Insert(0, new InstructionLine { opcode = Opcodes[0xfd2d], inline_tok = eh.ClassToken });
                else if (eh.IsFault)
                {
                    //throw new Exception("Fault handlers are not supported");
                }
                else if (eh.IsFilter)
                {
                    nodes[node].instrs.Insert(0, new InstructionLine { opcode = Opcodes[0xfd2d], inline_tok = eh.ClassToken });
                    throw new Exception("Filter handlers are not supported");
                }
            }

            if (dry_run)
            {
                // Purely ensure that all referenced types are instantiable

                if (CompiledMethodCache.ContainsKey(mtc))
                    return CompiledMethodCache[mtc];
                if (dry_run_cache.ContainsKey(mtc))
                {
                    if (dry_run_cache[mtc])
                        return new AssembleBlockOutput { };
                    else
                        return null;
                }

                foreach (Assembler.TypeToCompile lv in state.lv_types)
                {
                    if (!Layout.IsInstantiable(lv.tsig.Type, this, true))
                    {
                        dry_run_cache.Add(mtc, false);
                        return null;
                    }
                }
                foreach (Assembler.TypeToCompile la in state.la_types)
                {
                    if (!Layout.IsInstantiable(la.tsig.Type, this, true))
                    {
                        dry_run_cache.Add(mtc, false);
                        return null;
                    }
                }


                /*try
                {
                    AssembleBlockOutput abo = AssembleMethodBlock(nodes, start_nodes, mtc, state);
                    dry_run_cache.Add(mtc, true);
                    return new AssembleBlockOutput { };
                }
                catch (Exception)
                {
                    dry_run_cache.Add(mtc, false);
                    return null;
                }*/

                
                foreach (cfg_node node in nodes)
                {
                    foreach (InstructionLine i in node.instrs)
                    {
                        if (i.opcode.name != "ldtoken")
                        {
                            if (i.inline_tok != null)
                            {
                                if (!Layout.IsInstantiable(i.inline_tok, mtc.GetTTC(this), mtc.msig, this, true))
                                {
                                    dry_run_cache.Add(mtc, false);
                                    return null;
                                }
                            }
                        }
                    }
                }
                dry_run_cache.Add(mtc, true);
                return new AssembleBlockOutput { };
            }
            else
            {
                // Do the actual compilation
                AssembleBlockOutput main_block = AssembleMethodBlock(nodes, start_nodes, mtc, state);

                if (output != null)
                {
                    lock (output)
                    {
                        output.AlignText(GetSizeOfPointer());
                        int meth_offset = output.GetText().Count;
                        if (state.debug != null)
                            state.debug.TextOffset = (uint)meth_offset;
                        output.AddTextSymbol(meth_offset, mangled_name, false, true, mtc.meth.WeakLinkage);
                        foreach (string s in method_aliases)
                            output.AddTextSymbol(meth_offset, s, false, true, mtc.meth.WeakLinkage);

                        foreach (byte bb in main_block.code)
                            output.GetText().Add(bb);
                        foreach (RelocationBlock rb in main_block.relocs)
                            output.AddTextRelocation(rb.Offset + meth_offset, rb.Target, rb.RelType, rb.Value);
                        foreach (OutputBlock ob in main_block.symbols)
                        {
                            if (ob is ExportedSymbol)
                            {
                                ExportedSymbol es = ob as ExportedSymbol;
                                output.AddTextSymbol(es.Offset, es.Name, es.LocalOnly, es.IsFunc, false);
                            }
                        }

                        main_block.compiled_code_length = output.GetText().Count - meth_offset;
                    }
                }

                if (cache_output && !CompiledMethodCache.ContainsKey(mtc))
                    CompiledMethodCache.Add(mtc, main_block);

                return main_block;
            }

#if MT
            rtc--;
#endif
        }

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

        private AssembleBlockOutput AssembleMethodBlock(List<cfg_node> nodes, List<StartNodeDefinition> start_nodes,
            Assembler.MethodToCompile mtc, AssemblerState state)
        {
            // Perform stack tracing and instruction encoding
            foreach(StartNodeDefinition start_node in start_nodes)
                TraceStack(nodes, start_node.node_id, mtc.meth, mtc.meth.m, mtc, mtc, state);

            Assembler.MethodToCompile mi_sig = new MethodToCompile { _ass = this, meth = mtc.meth, msig = state.orig_sig, tsigp = mtc.tsigp, type = mtc.type, MetadataToken = mtc.MetadataToken };

            if (_options.EnableRTTI && !state.is_ref_implementation)
            {
                // Add in enter code - stores the current methd information somewhere
                if ((mi_sig.msig is Signature.GenericMethod) || is_jit)
                {
                    //Layout.LayoutEntry.RelocationType gmi_rel = Layout.GetGMIRel(mi_sig, this);

                    nodes[0]._tacs_prephi.Insert(1, new ThreeAddressCode(ThreeAddressCode.Op.enter, var.Null, var.AddrOfObject(Mangler2.MangleMethodInfoSymbol(mi_sig, this)), var.Null));
                    Requestor.RequestGenericMethodInfo(mi_sig);
                }
                else
                {
                    nodes[0]._tacs_prephi.Insert(1, new ThreeAddressCode(ThreeAddressCode.Op.enter, var.Null, var.AddrOfObject(Mangler2.MangleTypeInfo(mi_sig.GetTTC(this), this), Layout.GetSymbolOffset(Mangler2.MangleMethodInfoSymbol(mi_sig, this), mi_sig.GetTTC(this), this)), var.Null));
                    Requestor.RequestTypeInfo(mi_sig.GetTTC(this));
                }
            }

            // Add static constructors
            AddStaticConstructorCode(nodes, mtc, state);

            // Rebuild a linear TIR stream
            List<ThreeAddressCode> tacs = new List<ThreeAddressCode>();
            foreach (cfg_node node in nodes)
                tacs.AddRange(node.tacs);

            // Add pseudo-instructions to define local args
            foreach (KeyValuePair<int, var> la in state.la_locs)
                tacs.Insert(0, new ThreeAddressCode(ThreeAddressCode.Op.localarg, la.Value, var.Const(la.Key), var.Null));

            // Compile the TIR
            return AssembleTIR2(tacs, state);
            //return AssembleTIR(nodes, mtc, state);
        }

        AssembleBlockOutput AssembleTIR(List<cfg_node> nodes, MethodToCompile mtc, AssemblerState state)
        {
            // Create an IrDump for debugging purposes
            IrDump irdump = new IrDump(nodes);

            // Set up tydb output if required
            if ((debug != null) && (!_debug_produces_output || mtc.MetadataToken.HasValue))
            {
                state.debug = new tydb.Function();

                if (_debug_produces_output)
                {
                    state.debug.MetadataFileName = mtc.m.File.GetFileName();
                    state.debug.MetadataToken = mtc.MetadataToken.Value;
                }
                state.debug.MangledName = Mangler2.MangleMethod(mtc, this);
                debug.Functions.Add(state.debug);
            }

            // Some architectures pass arguments in registers - if this is the case, rewrite the appropriate local argument
            // references to use the appropriate logical var
            //if (Arch._extra_ops.Contains("args_in_registers") || ((state.cc != null) && state.cc.ArgsInRegisters))
            //    RewriteRegisterLocalArgs(nodes, state, mtc);

            // Insert a pseudo-end node if there is more than one end node
            cfg_node end = InsertPseudoEnd(nodes, state, mtc);

            // Generate dominators
            GenerateDominators(nodes);

            // Liveness analysis
            LivenessAnalysis2(nodes, end, state);

            // Convert to SSA form
            ConvertToSSA(nodes, state);

            // Identify global vars
            //  global vars dict links key = local var, value = global var
            Dictionary<int, int> global_vars_dict = new Dictionary<int, int>();
            IdentifyGlobalVars(nodes, ref global_vars_dict, state);
            // Rewrite the global variable references
            RewriteGlobalVars(nodes, global_vars_dict);

            // Build a linear representation of the instruction stream to allow unnecessary jumps to be
            //  eliminated
            List<ThreeAddressCode> ir = new List<ThreeAddressCode>();
            foreach (cfg_node node in nodes)
                ir.AddRange(node.tacs);

            // Remove unnecessary jumps and phi functions
            List<int> global_vars = new List<int>();
            CleanseOutput(ir, global_vars);

            // Dump if requested
            if (IrFeedback != null)
            {
                lock (IrFeedback)
                {
                    IrFeedback.IrDump(irdump.Ir);
                }
            }

            // Exclude certain variables from the optimization passes (mostly global vars)
            ICollection<var> exclude_vars = GenerateExcludeVars(ir);

            // Perform variable folding
            VariableFolding(ir, exclude_vars);

            // Perform constant folding and propagation
            ConstantFolding(ir, exclude_vars);

            // Remove instructions which have been optimized out
            RemoveOptimizedOutInstructions(ir);

            // Resolve phi instructions + generate list of global variables
            //ResolvePhis(ir, global_vars);
            GenerateGlobalVars(global_vars, global_vars_dict);

            // Perform TIR peephole optimizations
            TIRPeepholeOptimize(ir, nodes);

            // Reinsert the instruction stream into the basic blocks to enable liveness analysis
            ReinsertIR(ir, nodes);

            // Loop through the code, adding and removing insturctions as necessary to try and get it to compile
            int iterations = 0;
            bool changes_made = false;
            List<OutputBlock> blocks = null;

#if DEBUG
            List<ThreeAddressCode> faulting_instructions = new List<ThreeAddressCode>();
#else
                List<ThreeAddressCode> faulting_instructions = null;
#endif

            // The register allocation and code generation depends on which register allocator is being used
            AssemblerOptions.RegisterAllocatorType reg_alloc = Options.RegAlloc;
            if ((state.cc != null) && (state.cc.RequiredRegAlloc.HasValue))
                reg_alloc = state.cc.RequiredRegAlloc.Value;

            if (reg_alloc == AssemblerOptions.RegisterAllocatorType.graphcolour)
            {
                do
                {
                    changes_made = false;
                    int remove_count = 0;

                    // Add in node global vars
                    AddNodeGlobalVars(nodes);

                    do
                    {
                        // Perform liveness analysis for variables
                        LivenessAnalysis(end, nodes);

                        // Remove code that assigns to dead variables
                        remove_count = RemoveDeadCode(nodes);
                    } while (remove_count > 0);

                    // Some instructions below still work on a linear ir stream, therefore recreate it
                    ir = new List<ThreeAddressCode>();
                    foreach (cfg_node node in nodes)
                    {
                        if (node.optimized_ir != null)
                            ir.AddRange(node.optimized_ir);
                    }

                    // Do arch specific changes
                    ArchSpecific(ir, nodes, state, mtc);

                    // Determine the semantics of the variables
                    Dictionary<int, var_semantic> semantics = DetermineSemantics(nodes);

                    // Add in extra global vars which have been missed by previous passes
                    //AddExtraGlobalVars(global_vars, nodes);

                    // Remove global vars which have been optimized away
                    RemoveUnnecessaryGlobalVars(global_vars, semantics);

                    // Determine the preferred locations of certain variables
                    //Dictionary<int, hloc_constraint> preferred_locs = GeneratePreferredLocations(ir, semantics);

                    // Generate a coloured register graph
                    RegisterGraph big_rg = GenerateRegisterGraph(nodes, state);

                    // colour the graph with any required setup
                    foreach (KeyValuePair<int, hardware_location> kvp in state.required_locations)
                        big_rg.Colour(kvp.Key, kvp.Value);


                    List<RegisterGraph> rgs = big_rg.SplitGraph();

                    foreach (RegisterGraph rg in rgs)
                    {
                        // Precolour the graph
                        PrecolourGraph(rg, state, semantics, ref changes_made, faulting_instructions, mtc);
                        if (changes_made)
                            break;

                        // Do register allocation
                        ColourGraph(rg, semantics);
                    }

                    // Do actual code generation
                    if (!changes_made)
                        blocks = GenerateCode(nodes, big_rg, state, semantics, ref changes_made, faulting_instructions, mtc);

                    iterations++;

                    if (iterations == 50)
                    {
                        blocks = FastAlloc2(nodes, end, state, mtc);
                        changes_made = false;
                        //throw new TooManyIterationsException(mtc);
                    }
                } while (changes_made);
            }
            else if (reg_alloc == AssemblerOptions.RegisterAllocatorType.fastreg)
            {
                blocks = FastAlloc2(nodes, end, state, mtc);
            }
            else
                throw new Exception("Invalid register allocator specified: " + reg_alloc.ToString());

            // Add in arch specific code if neccessary
            ArchSpecificCode(state, blocks);
            List<OutputBlock> prolog = new List<OutputBlock>();
            if (state.is_ref_implementation == false)
                prolog = ArchSpecificProlog(state);

            // Is this a generic method/type method which is just a stub which calls the reference implementation?
            if (state.calls_ref_implementation)
            {
                blocks = new List<OutputBlock>(prolog);
                if (_options.EnableRTTI)
                {
                    // add enter code
                    {
                        ThreeAddressCode tac = new ThreeAddressCode(ThreeAddressCode.Op.enter, var.Null, var.AddrOfObject(Mangler2.MangleMethodInfoSymbol(mtc, this)), var.Null);
                        // Build hardware constriant equivalents of the variables
                        hloc_constraint O1, O2, R;
                        O1 = BuildHlocFromVar(tac.Operand1, null, state);
                        O2 = BuildHlocFromVar(tac.Operand2, null, state);
                        R = BuildHlocFromVar(tac.Result, null, state);
                        // Choose an instruction to use
                        output_opcode op = GetOutputOpcode(tac);
                        List<opcode_match> oms = OpcodeMatchesFromHloc(op, R, O1, O2);
                        opcode_match om = GetFullMatch(oms);

                        tac.Operand1.hardware_loc = new hardware_addressoflabel { label = tac.Operand1.base_var.v.label };
                        blocks.AddRange(om.Match.code_emitter(tac.Operator, tac.Result, tac.Operand1, tac.Operand2, tac, state));
                    }

                    // add jmp code
                    {
                        MethodToCompile ref_mtc = mtc.GetRefImplementation();
                        ThreeAddressCode tac = new ThreeAddressCode(ThreeAddressCode.Op.br, var.Null, var.AddrOfObject(Mangler2.MangleMethodInfoSymbol(ref_mtc, this)), var.Null);
                        // Build hardware constriant equivalents of the variables
                        hloc_constraint O1, O2, R;
                        O1 = BuildHlocFromVar(tac.Operand1, null, state);
                        O2 = BuildHlocFromVar(tac.Operand2, null, state);
                        R = BuildHlocFromVar(tac.Result, null, state);
                        // Choose an instruction to use
                        output_opcode op = GetOutputOpcode(tac);
                        List<opcode_match> oms = OpcodeMatchesFromHloc(op, R, O1, O2);
                        opcode_match om = GetFullMatch(oms);

                        blocks.AddRange(om.Match.code_emitter(tac.Operator, tac.Result, tac.Operand1, tac.Operand2, tac, state));

                        Requestor.RequestMethod(ref_mtc);
                    }
                }
            }
            else
                blocks.InsertRange(0, prolog);

            // Add in code to preserve callee-saved registers
            // First, work out those locations to save/restore
            List<hardware_location> locs_to_save = new List<hardware_location>(util.Intersect<hardware_location>(state.UsedLocations,
                state.cc.CalleePreservesLocations));
            
            // Generate the code to save/restore these registers
            List<byte> save_code = new List<byte>();
            foreach (hardware_location loc in locs_to_save)
                save_code.AddRange(SaveLocation(loc));
            locs_to_save.Reverse();
            List<byte> restore_code = new List<byte>();
            foreach (hardware_location loc in locs_to_save)
                restore_code.AddRange(RestoreLocation(loc));

            // Add it to the output
            foreach (CodeBlock save_block in state.CalleePreserveRegistersSaves)
                save_block.Code = save_code;
            foreach (CodeBlock restore_block in state.CalleePreserveRegistersRestores)
                restore_block.Code = restore_code;

            // Resolve local jumps
            List<RelocationBlock> rbs = new List<RelocationBlock>();
            List<OutputBlock> syms = new List<OutputBlock>();
            Dictionary<int, InstructionHeader> ihs = new Dictionary<int, InstructionHeader>();
            IList<byte> b = ResolveLocals(blocks, rbs, syms, ihs);

            if (this.ProduceIrDump == true)
            {
                _irdump += Mangler2.MangleMethod(mtc, this);
                _irdump += ":";
                _irdump += Environment.NewLine;
                _irdump += "global vars: ";
                foreach (int gv in global_vars)
                    _irdump += gv.ToString() + " ";
                _irdump += Environment.NewLine;
                _irdump += irdump.Ir;
                _irdump += Environment.NewLine;
            }

            if (OptimizedIrFeedback != null)
            {
                lock (OptimizedIrFeedback)
                {
                    OptimizedIrFeedback.IrDump(irdump.Ir);
                }
            }

            if(state.debug != null)
            {
                foreach (KeyValuePair<int, InstructionHeader> ih in ihs)
                    state.debug.Lines.Add(new tydb.Line { CompiledOffset = ih.Value.compiled_offset, ILOffset = ih.Value.il_offset });

                for(int i = 0; i < state.local_args.Count; i++)
                    state.debug.Args.Add(new tydb.VarArg { Location = GetDebugLocation(state.local_args[i].ValueLocation), Name = state.la_names[i] });

                for(int i = 0; i < state.local_vars.Count; i++)
                    state.debug.Vars.Add(new tydb.VarArg { Location = GetDebugLocation(state.local_vars[i]), Name = state.lv_names[i] });
            }

            return new AssembleBlockOutput { code = b, relocs = rbs, instrs = ihs, symbols = syms, prolog = prolog };
        }

        private void GenerateDominators(List<cfg_node> nodes)
        {
            /* The set of dominators of any node is the intersection of the dominators of all the predecessors of the
             * node, combined with the node itself */

            List<int>[] doms = new List<int>[nodes.Count];

            /* First, set the dominators of all start nodes to be the node itself, alone.  Every other node has all
             * the nodes as its potential dominators */
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].pred.Count == 0)
                    doms[i] = new List<int> { i };
                else
                {
                    doms[i] = new List<int>();
                    for (int j = 0; j < nodes.Count; j++)
                        doms[i].Add(j);
                }
            }

            /* Now repeatedly iterate over the node collection, making its dominators the dominators of its
             * predecessors (and the node itself).  Keep going as long as changes are being made */
            bool changes_made;
            do
            {
                changes_made = false;

                for (int i = 0; i < nodes.Count; i++)
                {
                    IEnumerable<int> isect = new List<int>();

                    for (int j = 0; j < nodes[i].ipred.Count; j++)
                    {
                        int pred_idx = nodes[i].ipred[j].block_id - 1;   // block IDs are numbered from 1

                        if (j == 0)
                            isect = doms[pred_idx];
                        else
                            isect = util.Intersect<int>(isect, doms[pred_idx]);
                    }

                    List<int> new_dom_list = new List<int>(isect);
                    new_dom_list.Add(i);

                    /* Now compare the new and old dominator list */
                    new_dom_list.Sort();
                    bool different = false;
                    if (new_dom_list.Count != doms[i].Count)
                        different = true;
                    else
                    {
                        for (int j = 0; j < new_dom_list.Count; j++)
                        {
                            if (new_dom_list[j] != doms[i][j])
                            {
                                different = true;
                                break;
                            }
                        }
                    }

                    /* If they are different, update the dominator list and note we have made changes */
                    if (different)
                    {
                        doms[i] = new_dom_list;
                        changes_made = true;
                    }
                }
            } while (changes_made);

            /* Now calculate immediate dominators.
             * 
             * The immediate dominator of n (idom[n]) is defined such that:
             *  1) idom[n] != n
             *  2) idom[n] dominates n  (assumed in the following algorithm)
             *  3) idom[n] does not dominate any other dominator of n
             */
            int[] idom = new int[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
                idom[i] = -1;

            for (int i_n = 0; i_n < nodes.Count; i_n++)
            {
                bool found_idom = false;

                for (int j_idx = 0; j_idx < doms[i_n].Count; j_idx++)
                {
                    int test_dom_n = doms[i_n][j_idx];
                    bool has_other_dominator = false;

                    /* Check rule 1 */
                    if (test_dom_n == i_n)
                        continue;

                    /* Does test_dom dominate any other dominator of i? */
                    for (int k_idx = 0; k_idx < doms[i_n].Count; k_idx++)
                    {
                        if (k_idx == j_idx)
                            continue;

                        int other_dom_n = doms[i_n][k_idx];
                        if (other_dom_n == i_n)
                            continue;
                        /* other_dom_n is another dominator of i_n that is distinct from test_dom_n
                         * 
                         * Iterate its dominators
                         */
                        for (int l_idx = 0; l_idx < doms[other_dom_n].Count; l_idx++)
                        {
                            int dom_of_other_node_n = doms[other_dom_n][l_idx];

                            if (dom_of_other_node_n == test_dom_n)
                            {
                                has_other_dominator = true;
                                break;
                            }
                        }

                        if (has_other_dominator)
                            break;
                    }

                    if (!has_other_dominator)
                    {
                        if (found_idom)
                            throw new NotSupportedException("Two immediate dominators found for node " + nodes[i_n].block_id.ToString() + ": "
                                + nodes[test_dom_n].block_id.ToString() + " and " + nodes[idom[i_n]].block_id.ToString());

                        found_idom = true;
                        idom[i_n] = test_dom_n;
                    }
                }
            }

            /* Now fill in the cfg_node lists in the nodes */
            for (int i = 0; i < nodes.Count; i++)
            {
                if (idom[i] != -1)
                    nodes[i].idom = nodes[idom[i]];
                else
                    nodes[i].idom = null;

                nodes[i].doms = new List<cfg_node>();
                for (int j = 0; j < doms[i].Count; j++)
                    nodes[i].doms.Add(nodes[doms[i][j]]);
            }
        }

        private void ConvertToSSA(List<cfg_node> nodes, AssemblerState state)
        {
            throw new NotImplementedException();
        }

        private void RemoveOptimizedOutInstructions(List<ThreeAddressCode> ir)
        {
            int i = 0;
            while (i < ir.Count)
            {
                if (ir[i].optimized_out_by_removal_of_another_instruction)
                    ir.RemoveAt(i);
                else
                    i++;
            }
        }

        var RewriteLocalArg(var la, Dictionary<int, int> la_v_map, Assembler.AssemblerState state, CallConv cc)
        {
            if ((la.type == var.var_type.AddressOf) || (la.type == var.var_type.AddressOfPlusConstant) ||
                (la.type == var.var_type.ContentsOf) || (la.type == var.var_type.ContentsOfPlusConstant))
            {
                la.base_var.v = RewriteLocalArg(la.base_var.v, la_v_map, state, cc);
                return la;
            }
            else if (la.type == var.var_type.LocalArg)
            {
                if (la_v_map.ContainsKey(la.local_arg))
                    return la_v_map[la.local_arg];
                if (cc.Arguments[la.local_arg].ValueLocation is register)
                {
                    int ret_v = state.next_variable++;
                    la_v_map[la.local_arg] = ret_v;
                    return ret_v;
                }
            }

            return la;
        }

        private void RewriteRegisterLocalArgs(List<cfg_node> nodes, AssemblerState state, MethodToCompile mtc)
        {
            // ARM uses registers as certain local arguments
            // Iterate through and set references to 'la(x)' to be a logical var if they are stored in arguments

            CallConv cc = call_convs[state.call_conv](mtc, CallConv.StackPOV.Callee, this, null);

            // Build a local arg -> logical var mapping
            state.la_v_map = new Dictionary<int, int>();
            foreach (cfg_node node in nodes)
            {
                foreach (ThreeAddressCode tac in node.tacs)
                {
                    tac.Operand1 = RewriteLocalArg(tac.Operand1, state.la_v_map, state, cc);
                    tac.Operand2 = RewriteLocalArg(tac.Operand2, state.la_v_map, state, cc);
                    tac.Result = RewriteLocalArg(tac.Result, state.la_v_map, state, cc);

                    if (tac is CallEx)
                    {
                        CallEx ce = tac as CallEx;

                        for (int i = 0; i < ce.Var_Args.Length; i++)
                            ce.Var_Args[i] = RewriteLocalArg(ce.Var_Args[i], state.la_v_map, state, cc);
                    }
                    if (tac is PhiEx)
                    {
                        PhiEx pe = tac as PhiEx;

                        for (int i = 0; i < pe.Var_Args.Count; i++)
                            pe.Var_Args[i] = RewriteLocalArg(pe.Var_Args[i], state.la_v_map, state, cc);
                    }
                }
            }

            // Build a logical var -> hardware location mapping
            foreach (KeyValuePair<int, int> kvp in state.la_v_map)
            {
                hardware_location hloc = cc.Arguments[kvp.Key].ValueLocation;
                state.required_locations.Add(kvp.Value, hloc);
            }
        }

        private void AddStaticConstructorCode(List<cfg_node> nodes, MethodToCompile mtc, AssemblerState state)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                // Add in code to run static constructors if required

                /* Code is:
                    * 
                    * type_info = &TI
                    * flags = [type_info + flag_offset]
                    * temp_flags = flags & 0x1
                    * cmp temp_flags, 0x1
                    * je .skip
                    * or flags, 0x1
                    * [type_info + flag_offset] = flags
                    * zeromem (&_s, static size)
                    * optionally call_void (cctor)
                    * .skip:
                    */
                foreach (TypeToCompile cctor_to_run in nodes[i].types_whose_static_fields_are_referenced)
                {
                    if (Layout.GetLayout(cctor_to_run, this).StaticFields.Count > 0)
                    {
                        /* Determine whether the constructor has been called previously */
                        bool called_previously = false;
                        cfg_node cn = nodes[i];
                        while (cn != null)
                        {
                            if (cn.ipred.Count > 1)
                                cn = null;
                            else if (cn.ipred.Count == 0)
                                cn = null;
                            else if (cn.ipred[0] == nodes[i])
                                cn = null;
                            else
                            {
                                cn = cn.ipred[0];
                                if (cn.types_whose_static_fields_are_referenced.Contains(cctor_to_run))
                                {
                                    called_previously = true;
                                    break;
                                }
                            }
                        }

                        if (!called_previously)
                        {
                            var v_type_info = state.next_variable++;
                            var v_flags = state.next_variable++;
                            var v_temp_flags = state.next_variable++;
                            int skip_block = state.next_block++;
                            //int flag_offset = GetTysosTypeLayout().GetField("Int32 ImplFlags", false).offset;
                            int static_size = Layout.GetLayout(cctor_to_run, this).StaticClassSize;
                            int flag_offset = 0;
                            bool has_cctor = false;
                            MethodToCompile cctor_mtc = new MethodToCompile();
                            foreach (Metadata.MethodDefRow cctor_mdr in cctor_to_run.type.Methods)
                            {
                                if (cctor_mdr.IsSpecialName && (cctor_mdr.Name == ".cctor"))
                                {
                                    has_cctor = true;

                                    cctor_mtc = Metadata.GetMTC(new Metadata.TableIndex(cctor_mdr), cctor_to_run, null, this);
                                    cctor_mtc.tsigp = cctor_to_run.tsig;
                                    Requestor.RequestMethod(cctor_mtc);

                                    break;
                                }
                            }

                            string cctor_type_mangled_name_ti = Mangler2.MangleTypeInfo(cctor_to_run, this);
                            string cctor_type_mangled_name_s = Mangler2.MangleTypeStatic(cctor_to_run, this);
                            //Requestor.RequestTypeInfo(cctor_to_run);

                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_type_info, var.AddrOfObject(cctor_type_mangled_name_s), var.Null));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_flags, var.ContentsOf(v_type_info, flag_offset), var.Null));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_temp_flags, v_flags, var.Null));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.and_i4, v_temp_flags, v_temp_flags, var.Const(0x1)));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_temp_flags, var.Const(0x1)));
                            nodes[i]._tacs_prephi.Add(new BrEx(ThreeAddressCode.Op.beq, skip_block));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.or_i4, v_flags, v_flags, var.Const(0x1)));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(v_type_info, flag_offset), v_flags, var.Null));
                            nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_type_info, v_type_info, var.Const(4)));
                            if (static_size > 0)
                                nodes[i]._tacs_prephi.Add(new ThreeAddressCode(ThreeAddressCode.Op.zeromem, var.Null, v_type_info, var.Const(static_size - 4)));
                            if (has_cctor && !(cctor_mtc.Equals(mtc)))       // don't call the constructor if this actually is the constructor we are compiling!
                                nodes[i]._tacs_prephi.Add(new CallEx(var.Null, new var[] { }, Mangler2.MangleMethod(cctor_mtc, this), callconv_cctor));
                            nodes[i]._tacs_prephi.Add(LabelEx.LocalLabel(skip_block));
                        }
                    }
                }
            }
        }

        private void AddNodeGlobalVars(List<cfg_node> nodes)
        {
            foreach (cfg_node node in nodes)
            {
                node.node_global_vars.Clear();

                foreach (InstructionLine inst in node.instrs)
                {
                    foreach (var v in inst.node_global_vars)
                    {
                        bool is_referenced = false;

                        /* Ensure the variable has not been optimized away */
                        foreach (ThreeAddressCode tac in node.optimized_ir)
                        {
                            if (v.Equals(GetReferencedLogicalVar(tac.Result)))
                            {
                                is_referenced = true;
                                break;
                            }
                            if (v.Equals(GetReferencedLogicalVar(tac.Operand1)))
                            {
                                is_referenced = true;
                                break;
                            }
                            if (v.Equals(GetReferencedLogicalVar(tac.Operand2)))
                            {
                                is_referenced = true;
                                break;
                            }
                            if (tac is CallEx)
                            {
                                CallEx ce = tac as CallEx;

                                foreach (var ce_var in ce.Var_Args)
                                {
                                    if (v.Equals(GetReferencedLogicalVar(ce_var)))
                                    {
                                        is_referenced = true;
                                        break;
                                    }

                                    if (is_referenced)
                                        break;
                                }
                            }
                        }

                        if (is_referenced)
                        {
                            if (!node.node_global_vars.Contains(v))
                                node.node_global_vars.Add(v);
                        }
                    }
                }
            }
        }

        private void TIRPeepholeOptimize(List<ThreeAddressCode> ir, List<cfg_node> nodes)
        {
            // Perform various peephole optimizations on the tir code
            for (int i = 0; i < ir.Count; i++)
            {
                ThreeAddressCode inst = ir[i];

                if (inst.GetOpType() == ThreeAddressCode.OpType.AssignOp)
                {
                    /* Some combinations of instructions take the form:
                     * 
                     * 1 = &la1/lv1 (using assign_vt)
                     * 2 = [1 + $const]
                     * 
                     * These can be optimised to:
                     * 
                     * 1 = &la1/lv1
                     * 2 = [la1/lv1 + $const]
                     * 
                     * and then the first command may be optimised away be the liveness analysis pass if no longer needed later on
                     */

                    /*
                    if ((inst.Operand1.type == var.var_type.ContentsOfAddress) ||
                        (inst.Operand1.type == var.var_type.ContentsOfAddressPlusConstant))
                    {
                        if (i > 0)
                        {
                            ThreeAddressCode prev_inst = ir[i - 1];

                            if ((prev_inst.GetOpType() == ThreeAddressCode.OpType.AssignOp) &&
                                ((prev_inst.Operand1.type == var.var_type.AddressOfLocalArg) ||
                                (prev_inst.Operand1.type == var.var_type.AddressOfLocalVar)))
                            {
                                inst.Operand1.logical_var = 0;

                                if (prev_inst.Operand1.type == var.var_type.AddressOfLocalArg)
                                    inst.Operand1.local_arg = prev_inst.Operand1.local_arg;
                                else
                                    inst.Operand1.local_var = prev_inst.Operand1.local_var;
                            }
                        }
                    } */
                }
                else if (inst.GetOpType() == ThreeAddressCode.OpType.ReturnOp)
                {
                    /* A lot of methods end:
                     * 
                     * lva = b
                     * L2:
                     * ret(lva)
                     * 
                     * These can be optimised to:
                     * 
                     * L2:
                     * ret(b)
                     * 
                     * as long as there is only one entry point into node '2'
                     */

                    if (i >= 2)
                    {
                        ThreeAddressCode l1 = ir[i - 2];
                        ThreeAddressCode l2 = ir[i - 1];
                        ThreeAddressCode l3 = ir[i];

                        if ((l1.GetOpType() == ThreeAddressCode.OpType.AssignOp) &&
                            (l1.Result.type == var.var_type.LocalVar))
                        {
                            var b = l1.Operand1;
                            int lv_no = l1.Result.local_var;

                            if((l2.Operator == ThreeAddressCode.Op.label) &&
                                (nodes[((LabelEx)l2).Block_id - 1].ipred.Count == 1))
                            {
                                if ((l3.GetOpType() == ThreeAddressCode.OpType.ReturnOp) &&
                                    (l3.Operand1.type == var.var_type.LocalVar) &&
                                    (l3.Operand1.local_var == lv_no))
                                {
                                    l3.Operand1 = b;

                                    ir.RemoveAt(i - 2);
                                    i--;
                                }
                            }
                        }
                    }
                }
            }
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

        private void RewriteGlobalVars(List<cfg_node> nodes, Dictionary<int, int> global_vars_dict)
        {
            foreach (cfg_node node in nodes)
            {
                foreach (ThreeAddressCode tac in node.tacs)
                {
                    RewriteGlobalVars(ref tac.Operand1, global_vars_dict);
                    RewriteGlobalVars(ref tac.Operand2, global_vars_dict);
                    RewriteGlobalVars(ref tac.Result, global_vars_dict);
                    if (tac is CallEx)
                    {
                        CallEx ce = tac as CallEx;
                        for(int i = 0; i < ce.Var_Args.Length; i++)
                            RewriteGlobalVars(ref ce.Var_Args[i], global_vars_dict);
                    }
                }
            }
        }

        private void RewriteGlobalVars(ref var var, Dictionary<int, int> global_vars_dict)
        {
            if ((var.type == libtysila.var.var_type.AddressOf) || (var.type == libtysila.var.var_type.ContentsOf) || (var.type == libtysila.var.var_type.ContentsOfPlusConstant) || (var.type == libtysila.var.var_type.AddressOfPlusConstant))
                RewriteGlobalVars(ref var.base_var.v, global_vars_dict);
            else
            {
                if ((var.logical_var != 0) && (global_vars_dict.ContainsKey(var.logical_var)))
                {
                    var.logical_var = global_vars_dict[var.logical_var];
                }
            }
        }

        private void IdentifyGlobalVars(List<cfg_node> nodes, ref Dictionary<int, int> global_vars_dict, AssemblerState state)
        {
            foreach (cfg_node node in nodes)
            {
                // match up the vars in the stack at the end of this block with the vars in the stack at the
                //  start of the next ones

                if (!node.stack_traced)
                    continue;

                for (int i = 0; i < node.pstack_after.Count; i++)
                {
                    int cur_gv = -1;

                    if (global_vars_dict.ContainsKey(node.pstack_after[i].contains_variable))
                        cur_gv = global_vars_dict[node.pstack_after[i].contains_variable];
                    else if (node.pstack_after[i].contains_variable == 0)
                        continue;
                    else
                    {
                        cur_gv = state.next_variable++;
                        global_vars_dict[node.pstack_after[i].contains_variable] = cur_gv;
                    }

                    foreach (cfg_node suc in node.isuc)
                    {
                        if (global_vars_dict.ContainsKey(suc.pstack_before[i].contains_variable))
                        {
                            if (global_vars_dict[suc.pstack_before[i].contains_variable] != cur_gv)
                            {
                                // rewrite all usages of the previous global var to use the current one
                                int refactor_var = global_vars_dict[suc.pstack_before[i].contains_variable];
                                Dictionary<int, int> new_gvd = new Dictionary<int, int>();
                                foreach (KeyValuePair<int, int> kvp in global_vars_dict)
                                {
                                    if (kvp.Value == refactor_var)
                                        new_gvd.Add(kvp.Key, cur_gv);
                                    else
                                        new_gvd.Add(kvp.Key, kvp.Value);
                                }
                                global_vars_dict = new_gvd;
                            }
                        }
                        else
                        {
                            /* propagate the global var further on */
                            global_vars_dict[suc.pstack_before[i].contains_variable] = cur_gv;
                        }
                    }
                }
            }
        }

        private void AddExtraGlobalVars(List<int> global_vars, List<cfg_node> nodes)
        {
            List<int> used_already = new List<int>();
            foreach (cfg_node node in nodes)
            {
                foreach (int v in node.all_used_vars)
                {
                    if (used_already.Contains(v))
                    {
                        if (!global_vars.Contains(v))
                            global_vars.Add(v);
                    }
                    else
                        used_already.Add(v);
                }
            }
        }

        private void RemoveUnnecessaryGlobalVars(List<int> global_vars, Dictionary<int, var_semantic> semantics)
        {
            int i = 0;
            while(i < global_vars.Count)
            {
                if (!semantics.ContainsKey(global_vars[i]))
                    global_vars.RemoveAt(i);
                else
                    i++;
            }
        }

        private ICollection<var> GenerateExcludeVars(List<ThreeAddressCode> ir)
        {
            /* Creates a list of variables that are assigned to more than once - we exclude these from optimization passes */
            List<var> ev = new List<var>();
            List<var> assigned_vars = new List<var>();
            foreach (ThreeAddressCode i in ir)
            {
                if (i.Result.type == var.var_type.LogicalVar)
                {
                    if ((assigned_vars.Contains(i.Result)) && (!ev.Contains(i.Result)))
                        ev.Add(i.Result);
                    else
                        assigned_vars.Add(i.Result);
                }
            }
            return ev;
        }

        private int RemoveDeadCode(List<cfg_node> nodes)
        {
            int remove_count = 0;
            foreach (cfg_node node in nodes)
            {
                int i = 0;
                if (node.optimized_ir != null)
                {
                    while (i < node.optimized_ir.Count)
                    {
                        if ((node.optimized_ir[i].Result.logical_var != 0) && 
                            (node.optimized_ir[i].Result.contents_of == false))
                        {
                            bool can_remove = true;
                            foreach (var v in node.optimized_ir[i].live_vars_after)
                            {
                                if (v.logical_var == node.optimized_ir[i].Result.logical_var)
                                {
                                    can_remove = false;
                                    break;
                                }
                            }

                            if (can_remove)
                            {
                                if (node.optimized_ir[i].GetOpType() == ThreeAddressCode.OpType.CallOp)
                                {
                                    node.optimized_ir[i].Operator = ThreeAddressCode.Op.call_void;
                                    node.optimized_ir[i].Result = var.Undefined;
                                }
                                else
                                {
                                    node.optimized_ir.RemoveAt(i);
                                    remove_count++;
                                    continue;
                                }
                            }
                        }

                        // Also remove code like 1 = assign(1)
                        if (node.optimized_ir[i].GetOpType() == ThreeAddressCode.OpType.AssignOp)
                        {
                            if ((node.optimized_ir[i].Result.type == var.var_type.LogicalVar) &&
                                (node.optimized_ir[i].Operand1.type == var.var_type.LogicalVar) &&
                                (node.optimized_ir[i].Result == node.optimized_ir[i].Operand1))
                            {
                                node.optimized_ir.RemoveAt(i);
                                remove_count++;
                                continue;
                            }
                        }
                        i++;
                    }
                }
            }
            return remove_count;
        }

        private void VariableFolding(List<ThreeAddressCode> ir, ICollection<var> exclude_vars)
        {
            Dictionary<int, var> vars = new Dictionary<int, var>();
            Dictionary<string, int> const_vars = new Dictionary<string, int>();

            int i = 0;

            i = 0;
            while(i < ir.Count)
            {
                ThreeAddressCode inst = ir[i];
                if (const_vars.ContainsKey(inst.Operand1.ToString()))
                    inst.Operand1 = const_vars[inst.Operand1.ToString()];
                if (inst.Operand1.type == var.var_type.LogicalVar)
                {
                    if (vars.ContainsKey(inst.Operand1.logical_var))
                        inst.Operand1 = vars[inst.Operand1.logical_var].CloneVar();
                }
                if (inst.Operand2.type == var.var_type.LogicalVar)
                {
                    if (vars.ContainsKey(inst.Operand2.logical_var))
                        inst.Operand2 = vars[inst.Operand2.logical_var].CloneVar();
                }
                if (inst.GetOpType() == ThreeAddressCode.OpType.CallOp)
                {
                    CallEx ce = inst as CallEx;
                    for(int j = 0; j < ce.Var_Args.Length; j++)
                    {
                        if(vars.ContainsKey(ce.Var_Args[j].logical_var))
                            ce.Var_Args[j] = vars[ce.Var_Args[j].logical_var].CloneVar();
                    }
                }
                if (inst.GetOpType() == ThreeAddressCode.OpType.AssignOp)
                {
                    if (inst.Result.type == var.var_type.LogicalVar)
                    {
                        switch (inst.Operand1.type)
                        {
                            case var.var_type.Const:
                            case var.var_type.LogicalVar:
                                if (!exclude_vars.Contains(inst.Result))
                                {
                                    vars.Add(inst.Result.logical_var, inst.Operand1);
                                    if (CanRemove(inst.Result.logical_var, ir))
                                    {
                                        ir.RemoveAt(i);
                                        continue;
                                    }
                                }
                                break;

                            case var.var_type.AddressOf:
                                if (!exclude_vars.Contains(inst.Result))
                                {
                                    vars.Add(inst.Result.logical_var, inst.Operand1);
                                    if(!const_vars.ContainsKey(inst.Operand1.ToString()))
                                        const_vars.Add(inst.Operand1.ToString(), inst.Result.logical_var);
                                }
                                break;
                        }
                    }
                }
                i++;
            }
        }

        struct jmp_source { public int target_block; public int src_oblock; public int src_suboblock; 
            public int src_suboblock2; public int src_offset;
            public int offset; public int length;
            public int shift_after_offset;
        }

        private IList<byte> ResolveLocals(IList<OutputBlock> blocks, IList<RelocationBlock> rbs, IList<OutputBlock> syms, IDictionary<int, InstructionHeader> instrs)
        {
            // Assume that the distance between a jump and its destination is the longest possible jump (based
            //  upon BlockChoices), then choose the appropriate block choices, and then fill in the actual
            //  distances

            // First, identify a list of jump sources and destinations
            Dictionary<int, int> header_list_blockid_obi = new Dictionary<int, int>();
            List<jmp_source> jmp_list = new List<jmp_source>();

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is NodeHeader)
                    header_list_blockid_obi.Add(((NodeHeader)blocks[i]).block_id, i);
                if (blocks[i] is NodeReference)
                    jmp_list.Add(new jmp_source { src_oblock = i, target_block = ((NodeReference)blocks[i]).block_id });
                if (blocks[i] is BlockChoice)
                {
                    for(int j = 0; j < ((BlockChoice)blocks[i]).Choices.Count; j++)
                    {
                        IList<OutputBlock> obs2 = ((BlockChoice)blocks[i]).Choices[j];
                        for (int k = 0; k < obs2.Count; k++)
                        {
                            OutputBlock ob2 = obs2[k];
                            if (ob2 is NodeHeader)
                                header_list_blockid_obi.Add(((NodeHeader)ob2).block_id, i);
                            if (ob2 is NodeReference)
                                jmp_list.Add(new jmp_source { src_oblock = i, target_block = ((NodeReference)ob2).block_id,
                                    src_suboblock = j, src_suboblock2 = k });
                        }
                    }
                }
            }

            // Now, for each jump work out the largest possible distance that needs to be crossed
            foreach (jmp_source js in jmp_list)
            {
                int source_block = js.src_oblock;
                int dest_block = header_list_blockid_obi[js.target_block];
                int offset = GetNodeReference(blocks, js).offset;
                int dir = (dest_block > source_block) ? 1 : -1;

                int dist = offset;

                for (int i = source_block; i != dest_block; i += dir)
                    dist += GetLongestBlockLength(blocks[i]) * dir;

                GetNodeReference(blocks, js).longest_distance = dist;
            }

            // Make a decision as to the shortest of the various block choices to use
            int cur_block = 0;
            while (cur_block < blocks.Count)
            {
                if (blocks[cur_block] is BlockChoice)
                {
                    IList<OutputBlock> best_subblock = null;
                    int best_subblock_length = Int32.MaxValue;
                    for (int cur_subblock = 0; cur_subblock < ((BlockChoice)blocks[cur_block]).Choices.Count;
                        cur_subblock++)
                    {
                        if(IsBlockChoicePossible(((BlockChoice)blocks[cur_block]).Choices[cur_subblock]))
                        {
                            int length_of = GetLongestBlockLength(((BlockChoice)blocks[cur_block]).Choices[cur_subblock]);
                            if (length_of < best_subblock_length)
                            {
                                best_subblock_length = length_of;
                                best_subblock = ((BlockChoice)blocks[cur_block]).Choices[cur_subblock];
                            }
                        }
                    }

                    if (best_subblock == null)
                        throw new Exception("no subblock possible");

                    // replace the block choice with the best subblock
                    blocks.RemoveAt(cur_block);
                    for (int cur_subblock2 = 0; cur_subblock2 < best_subblock.Count; cur_subblock2++)
                        blocks.Insert(cur_block + cur_subblock2, best_subblock[cur_subblock2]);
                }

                cur_block++;
            }

            // Now turn the output block stream into a linear byte array with the addresses of headers and
            //  references saved
            List<byte> b = new List<byte>();
            Dictionary<int, int> d_headerid_offset = new Dictionary<int, int>();
            List<jmp_source> l_js = new List<jmp_source>();

            bool addend_in_code = false;
            if(Arch._extra_ops.Contains("addend_in_code"))
                addend_in_code = true;

            foreach (OutputBlock ob in blocks)
            {
                if (ob is CodeBlock)
                    b.AddRange(((CodeBlock)ob).Code);
                else if (ob is NodeReference)
                {
                    l_js.Add(new jmp_source { target_block = ((NodeReference)ob).block_id, src_offset = b.Count,
                        offset = ((NodeReference)ob).offset, shift_after_offset = ((NodeReference)ob).shift_after_offset,
                        length = ((NodeReference)ob).length });
                    for (int c = 0; c < ((NodeReference)ob).length; c++)
                        b.Add(0);
                }
                else if (ob is NodeHeader)
                    d_headerid_offset.Add(((NodeHeader)ob).block_id, b.Count);
                else if (ob is RelocationBlock)
                {
                    RelocationBlock rb = ob as RelocationBlock;
                    rb.Offset = b.Count;
                    rbs.Add(rb);

                    if(addend_in_code)
                        rb.OutputBytes = ToByteArray(rb.Value);

                    for (int c = 0; c < ((RelocationBlock)ob).Size; c++)
                    {
                        if ((rb.OutputBytes != null) && (c < rb.OutputBytes.Length))
                            b.Add(rb.OutputBytes[c]);
                        else                               
                            b.Add(0);
                    }
                }
                else if (ob is InstructionHeader)
                {
                    InstructionHeader ih = ob as InstructionHeader;
                    ih.compiled_offset = b.Count;
                    instrs.Add(ih.il_offset, ih);
                }
                else if (ob is ExportedSymbol)
                {
                    ((ExportedSymbol)ob).Offset = b.Count;
                    syms.Add(ob);
                }
                else if (ob is LocalSymbol)
                {
                    ((LocalSymbol)ob).Offset = b.Count;
                    syms.Add(ob);
                }
            }

            foreach (jmp_source js in l_js)
            {
                int rel_offset = (d_headerid_offset[js.target_block] - js.src_offset + js.offset) >> js.shift_after_offset;

                NodeReference nr = new NodeReference { length = js.length, longest_distance = rel_offset };
                if (nr.IsPossible == false)
                    throw new Exception("relocation needs to be truncated to fit");

                this.SetByteArray(b, js.src_offset, rel_offset, js.length);
            }

            return b;
        }

        private bool IsBlockChoicePossible(IList<OutputBlock> iList)
        {
            foreach (OutputBlock ob in iList)
            {
                if (ob is NodeReference)
                    if (((NodeReference)ob).IsPossible == false)
                        return false;
            }
            return true;
        }

        private int GetLongestBlockLength(OutputBlock outputBlock)
        {
            if (outputBlock is CodeBlock)
                return ((CodeBlock)outputBlock).Code.Count;
            if (outputBlock is NodeReference)
                return ((NodeReference)outputBlock).length;
            if (outputBlock is NodeHeader)
                return 0;
            if (outputBlock is BlockChoice)
            {
                int longest = 0;
                foreach (List<OutputBlock> list_obs in ((BlockChoice)outputBlock).Choices)
                {
                    int cur_length = GetLongestBlockLength(list_obs);
                    if (cur_length > longest)
                        longest = cur_length;
                }
                return longest;
            }
            if (outputBlock is RelocationBlock)
                return ((RelocationBlock)outputBlock).Size;
            if (outputBlock is InstructionHeader)
                return 0;
            if (outputBlock is ExportedSymbol)
                return 0;
            throw new NotSupportedException();
        }

        internal int GetLongestBlockLength(IList<OutputBlock> list_obs)
        {
            int cur_length = 0;
            foreach (OutputBlock ob in list_obs)
                cur_length += GetLongestBlockLength(ob);
            return cur_length;
        }

        private NodeReference GetNodeReference(IList<OutputBlock> blocks, jmp_source js)
        {
            if (blocks[js.src_oblock] is BlockChoice)
                return ((BlockChoice)blocks[js.src_oblock]).Choices[js.src_suboblock][js.src_suboblock2] as NodeReference;
            return blocks[js.src_oblock] as NodeReference;
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

        private Dictionary<int, var_semantic> DetermineSemantics(List<cfg_node> nodes)
        {
            Dictionary<int, var_semantic> semantics = new Dictionary<int, var_semantic>();

            foreach (cfg_node node in nodes)
            {
                foreach (ThreeAddressCode inst in node.optimized_ir)
                {
                    int var = inst.Result;

                    if (var > 0)
                    {
                        var_semantic semantic = inst.GetResultSemantic(this);

                        if (semantics.ContainsKey(var))
                        {
                            // Merge the semantics
                            semantics[var].Merge(semantic);
                        }
                        else
                            semantics.Add(var, semantic);
                    }

                    // Determine whether we require the address of a variable
                    AddMemlocSemantic(inst.Operand1, semantics);
                    AddMemlocSemantic(inst.Operand2, semantics);
                    if (inst is CallEx)
                    {
                        CallEx ce = inst as CallEx;
                        foreach (var va in ce.Var_Args)
                            AddMemlocSemantic(va, semantics);
                    }
                }
            }

            return semantics;
        }

        internal cfg_node InsertPseudoEnd(List<cfg_node> nodes, AssemblerState state, MethodToCompile mtc)
        {
            List<cfg_node> end_nodes = new List<cfg_node>();

            for(int i = 0; i < nodes.Count; i++)
            {
                /* End nodes are those with no immediate successors, or those whose only immediate successor
                 * is the current node (to handle while(true) instruction; constructs)
                 */

                if (nodes[i].isuc.Count == 0)
                    end_nodes.Add(nodes[i]);
                
                if ((nodes[i].isuc.Count == 1) && (nodes[i].isuc[0] == nodes[i]))
                    end_nodes.Add(nodes[i]);
            }

            if (end_nodes.Count == 1)
                return end_nodes[0];
            if (end_nodes.Count == 0)
            {
                /* We are now in the situation where we have a non-trivial cyclic control flow graph
                 * 
                 * e.g
                 * 
                 * A -> B ----------> C --> D
                 *      |             ^     |
                 *      |             |     *
                 *      |             F <-- E
                 *      *
                 *      G --> H
                 *      ^     |
                 *      |     *
                 *      J <-- I
                 *      
                 * Here, we want to identify F and J as end nodes, and then insert a pseudo-end which
                 * has F and J as immediate predecessors
                 * 
                 * We use the following algorithm:
                 * 
                 * First, determine a list of 'non-unique ends'
                 * 
                 * A non-unique end is defined as all those nodes which either have no immediate successors
                 * or where all of the nodes immediate successors have already been processed
                 * 
                 * From this list, we determine a list of 'unique ends'
                 * 
                 * Unique ends are non-unique ends whose end-node is not contained within the path to any other
                 * non-unique end.
                 * 
                 * 
                 * We define a path to a non-unique end as a list of nodes
                 * The list of non-unique ends is therefore a list of list of nodes
                 */

                List<List<cfg_node>> non_unique_ends = new List<List<cfg_node>>();
                List<cfg_node> processed_nodes = new List<cfg_node>();

                DetermineNonUniqueEnds(non_unique_ends, processed_nodes, new List<cfg_node>(), nodes[0]);

                /* Now determine the unique end points */

                // make a copy of the list so we can iterate through it twice
                List<List<cfg_node>> non_unique_ends_copy = new List<List<cfg_node>>();
                foreach (List<cfg_node> cur_non_unique_end in non_unique_ends)
                {
                    cfg_node cur_node = cur_non_unique_end[cur_non_unique_end.Count - 1];
                    bool is_unique = true;

                    foreach (List<cfg_node> test_non_unique_end in non_unique_ends_copy)
                    {
                        if (test_non_unique_end != cur_non_unique_end)
                        {
                            if(test_non_unique_end.Contains(cur_node))
                            {
                                is_unique = false;
                                break;
                            }
                        }
                    }

                    if(is_unique)
                        end_nodes.Add(cur_node);
                }
            }

            cfg_node end_node = new cfg_node(state.next_block++, mtc);
            end_node.optimized_ir = new List<ThreeAddressCode>();
            foreach(cfg_node n in end_nodes)
            {
                n.isuc.Add(end_node);
                end_node.ipred.Add(n);
            }
            nodes.Add(end_node);
            return end_node;
        }

        private void DetermineNonUniqueEnds(List<List<cfg_node>> non_unique_ends, List<cfg_node> processed_nodes, List<cfg_node> cur_path, cfg_node cur_node)
        {
            // The current node has been processed
            processed_nodes.Add(cur_node);

            // Add ourselves to the current path
            cur_path.Add(cur_node);

            // If we have no immediate successors, we are a non-unique end
            if (cur_node.isuc.Count == 0)
            {
                non_unique_ends.Add(cur_path);
                return;
            }

            bool has_unprocessed_children = false;
            // Now we check to find all the non-processed successors and process them
            foreach (cfg_node child_node in cur_node.isuc)
            {
                if (!processed_nodes.Contains(child_node))
                {
                    has_unprocessed_children = true;

                    // Make a copy of the current path
                    List<cfg_node> new_path = new List<cfg_node>(cur_path);

                    DetermineNonUniqueEnds(non_unique_ends, processed_nodes, new_path, child_node);
                }
            }

            // If we have no unprocessed children, we are a non-unique end
            if (!has_unprocessed_children)
                non_unique_ends.Add(cur_path);
        }

        internal void LivenessAnalysis(cfg_node end_node, List<cfg_node> nodes)
        {
            end_node.live_vars_at_end.Clear();

            foreach (cfg_node n in nodes)
            {
                n.live_vars_at_end.Clear();
                n.live_vars_done = false;
            }

            List<var> live_vars = new List<var>();
            LivenessAnalysis(live_vars, end_node);
        }

        private void LivenessAnalysis(List<var> live_vars, cfg_node cur_node)
        {
            if (cur_node.optimized_ir != null)
            {
                for (int i = cur_node.optimized_ir.Count - 1; i >= 0; i--)
                {
                    ThreeAddressCode inst = cur_node.optimized_ir[i];

                    cur_node.optimized_ir[i].live_vars = new List<var>(live_vars);
                    cur_node.optimized_ir[i].live_vars_after = new List<var>(live_vars);

                    // Remove vars written to from live, then add vars read from
                    // Note that assigning to references like *1 and &1 are actually reading from 1
                    int v_R = GetReferencedLogicalVar(inst.Result);
                    if ((inst.Result.type == var.var_type.LogicalVar) && (inst.Result.is_global == false))
                    {
                        if (inst.live_vars.Contains(v_R))
                            inst.live_vars.Remove(v_R);
                    }
                    else if (v_R != 0 && !inst.live_vars.Contains(v_R))
                        inst.live_vars.Add(v_R);

                    int v_O1 = GetReferencedLogicalVar(inst.Operand1);
                    if (v_O1 != 0 && !inst.live_vars.Contains(v_O1))
                        inst.live_vars.Add(v_O1);
                    if (v_O1 != 0 && !cur_node.all_used_vars.Contains(v_O1))
                        cur_node.all_used_vars.Add(v_O1);
                    
                    int v_O2 = GetReferencedLogicalVar(inst.Operand2);
                    if (v_O2 != 0 && !inst.live_vars.Contains(v_O2))
                        inst.live_vars.Add(v_O2);
                    if (v_O2 != 0 && !cur_node.all_used_vars.Contains(v_O2))
                        cur_node.all_used_vars.Add(v_O2);

                    if (v_R != 0 && !cur_node.all_used_vars.Contains(v_R))
                        cur_node.all_used_vars.Add(v_R);

                    if (cur_node.optimized_ir[i] is CallEx)
                    {
                        CallEx ce = cur_node.optimized_ir[i] as CallEx;
                        foreach (var p in ce.Var_Args)
                        {
                            int v_v = GetReferencedLogicalVar(p);
                            if ((v_v != 0) && !cur_node.optimized_ir[i].live_vars.Contains(v_v))
                                cur_node.optimized_ir[i].live_vars.Add(v_v);
                            if ((v_v != 0) && !cur_node.all_used_vars.Contains(v_v))
                                cur_node.all_used_vars.Add(v_v);
                        }
                    }

                    // Add in all nodes defined as node-global
                    foreach (var ng in cur_node.node_global_vars)
                    {
                        if (!inst.live_vars.Contains(ng))
                            inst.live_vars.Add(ng);
                    }

                    live_vars = cur_node.optimized_ir[i].live_vars;
                }
            }

            cur_node.live_vars_done = true;
            cur_node.live_vars_at_start = live_vars;

            // Process predecessor nodes if there are extra live vars to do
            foreach (cfg_node pred in cur_node.ipred)
            {
                bool new_added = false;

                foreach (int i in live_vars)
                {
                    if (!pred.live_vars_at_end.Contains(i))
                    {
                        pred.live_vars_at_end.Add(i);
                        new_added = true;
                    }
                }

                if ((!pred.live_vars_done) || new_added)
                    LivenessAnalysis(pred.live_vars_at_end, pred);
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

        private void ReinsertIR(List<ThreeAddressCode> ir, List<cfg_node> nodes)
        {
            cfg_node cur_node = null;

            for (int i = 0; i < ir.Count; i++)
            {
                if ((ir[i].Operator == ThreeAddressCode.Op.label) && (ir[i] is LabelEx))
                {
                    int cur_label = ((LabelEx)ir[i]).Block_id;
                    cur_node = GetNode(cur_label, nodes);
                    if (cur_node == null)
                        throw new Exception("node not found");
                    cur_node.optimized_ir = new List<ThreeAddressCode>();
                }

                if (cur_node != null)
                {
                    cur_node.optimized_ir.Add(ir[i]);
                    ir[i].node = cur_node;
                }
            }
        }

        private cfg_node GetNode(int cur_label, List<cfg_node> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].block_id == cur_label)
                    return nodes[i];
            }
            return null;
        }

        private void ConstantFolding(List<ThreeAddressCode> ir, ICollection<var> exclude_vars)
        {
            Dictionary<int, object> constant_vals = new Dictionary<int, object>();

            int i = 0;
            while (i < ir.Count)
            {
                ThreeAddressCode.OpType optype = ir[i].GetOpType();
                if (optype == ThreeAddressCode.OpType.ConstOp)
                {
                    if (CanRemove(ir[i].Result, ir))
                    {
                        constant_vals.Add(ir[i].Result, ir[i].Result.constant_val);
                        ir.RemoveAt(i);
                        continue;
                    }
                    else
                        ir[i].Operator = GetAssignTac(ir[i].Operator);
                }
                if (optype == ThreeAddressCode.OpType.BinNumOp)
                {
                    if (ir[i].Operand1.type == var.var_type.LogicalVar)
                    {
                        if (constant_vals.ContainsKey(ir[i].Operand1))
                        {
                            ir[i].Operand1.constant_val = constant_vals[ir[i].Operand1];
                            ir[i].Operand1.logical_var = 0;
                        }
                    }
                    if (ir[i].Operand2.type == var.var_type.LogicalVar)
                    {
                        if (constant_vals.ContainsKey(ir[i].Operand2))
                        {
                            ir[i].Operand2.constant_val = constant_vals[ir[i].Operand2];
                            ir[i].Operand2.logical_var = 0;
                        }
                    }

                    if ((ir[i].Operand1.constant_val != null) && (ir[i].Operand2.constant_val != null))
                    {
                        if (EvaluateConstant(ir[i]))
                        {
                            if (ir[i].Result.type == var.var_type.LogicalVar)
                            {
                                //ir[i].Result = ir[i].Operand1;
                                if (!exclude_vars.Contains(ir[i].Result))
                                {
                                    constant_vals.Add(ir[i].Result, ir[i].Operand1.constant_val);
                                    if (CanRemove(ir[i].Result, ir))
                                    {
                                        ir.RemoveAt(i);
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    else if (ir[i].Operand2.constant_val != null)
                    {
                        // Determine if op2 if something special, i.e. 0 or 1 that we can possibly optimise away
                        ulong op2_ul = FromByteArrayU8(ToByteArrayZeroExtend(ir[i].Operand2.constant_val, 8));
                        if(op2_ul == 0UL)
                        {
                            switch(ir[i].Operator)
                            {
                                case ThreeAddressCode.Op.add_i:
                                case ThreeAddressCode.Op.sub_i:
                                    ir[i].Operator = ThreeAddressCode.Op.assign_i;
                                    ir[i].Operand2 = var.Null;
                                    break;

                                case ThreeAddressCode.Op.add_i4:
                                case ThreeAddressCode.Op.sub_i4:
                                    ir[i].Operator = ThreeAddressCode.Op.assign_i4;
                                    ir[i].Operand2 = var.Null;
                                    break;

                                case ThreeAddressCode.Op.add_i8:
                                case ThreeAddressCode.Op.sub_i8:
                                    ir[i].Operator = ThreeAddressCode.Op.assign_i8;
                                    ir[i].Operand2 = var.Null;
                                    break;
                            }
                        }
                        else if(op2_ul == 0UL)
                        {
                            switch(ir[i].Operator)
                            {
                                case ThreeAddressCode.Op.mul_i:
                                case ThreeAddressCode.Op.div_i:
                                    ir[i].Operator = ThreeAddressCode.Op.assign_i;
                                    ir[i].Operand2 = var.Null;
                                    break;

                                case ThreeAddressCode.Op.mul_i4:
                                case ThreeAddressCode.Op.div_i4:
                                    ir[i].Operator = ThreeAddressCode.Op.assign_i4;
                                    ir[i].Operand2 = var.Null;
                                    break;

                                case ThreeAddressCode.Op.mul_i8:
                                case ThreeAddressCode.Op.div_i8:
                                    ir[i].Operator = ThreeAddressCode.Op.assign_i8;
                                    ir[i].Operand2 = var.Null;
                                    break;
                            }                        
                        }
                    }
                }
                if (optype == ThreeAddressCode.OpType.CmpOp)
                {
                    if (constant_vals.ContainsKey(ir[i].Operand1))
                    {
                        ir[i].Operand1.constant_val = constant_vals[ir[i].Operand1];
                        ir[i].Operand1.logical_var = 0;
                    }
                    if (constant_vals.ContainsKey(ir[i].Operand2))
                    {
                        ir[i].Operand2.constant_val = constant_vals[ir[i].Operand2];
                        ir[i].Operand2.logical_var = 0;
                    }
                }
                if ((optype == ThreeAddressCode.OpType.InternalOp) || (optype == ThreeAddressCode.OpType.OtherOptimizeOp) || (optype == ThreeAddressCode.OpType.ReturnOp))
                {
                    if (constant_vals.ContainsKey(ir[i].Operand1))
                    {
                        ir[i].Operand1.constant_val = constant_vals[ir[i].Operand1];
                        ir[i].Operand1.logical_var = 0;
                    }
                    if (constant_vals.ContainsKey(ir[i].Operand2))
                    {
                        ir[i].Operand2.constant_val = constant_vals[ir[i].Operand2];
                        ir[i].Operand2.logical_var = 0;
                    }
                }
                if (optype == ThreeAddressCode.OpType.AssignOp)
                {
                    if ((ir[i].Operand1.type == var.var_type.Const) &&
                        (ir[i].Result.type == var.var_type.LogicalVar))
                    {
                        if (!exclude_vars.Contains(ir[i].Result))
                        {
                            constant_vals.Add(ir[i].Result, ir[i].Operand1.constant_val);
                            if (CanRemove(ir[i].Result, ir))
                            {
                                ir.RemoveAt(i);
                                continue;
                            }
                        }
                    }
                    else if ((ir[i].Operand1.type == var.var_type.LogicalVar) &&
                        (ir[i].Result.type == var.var_type.LogicalVar))
                    {
                        if (constant_vals.ContainsKey(ir[i].Operand1))
                        {
                            ir[i].Operand1.constant_val = constant_vals[ir[i].Operand1];
                            ir[i].Operand1.logical_var = 0;
                            if (!exclude_vars.Contains(ir[i].Result))
                            {
                                constant_vals.Add(ir[i].Result, ir[i].Operand1.constant_val);
                                if (CanRemove(ir[i].Result, ir))
                                {
                                    ir.RemoveAt(i);
                                    continue;
                                }
                            }
                        }
                    }
                    else if (ir[i].Operand1.type == var.var_type.LogicalVar)
                    {
                        if (constant_vals.ContainsKey(ir[i].Operand1))
                        {
                            ir[i].Operand1.constant_val = constant_vals[ir[i].Operand1];
                            ir[i].Operand1.logical_var = 0;
                        }
                    }
                }
                if ((optype == ThreeAddressCode.OpType.UnNumOp) || (optype == ThreeAddressCode.OpType.ConvOp))
                {
                    if (constant_vals.ContainsKey(ir[i].Operand1))
                    {
                        ir[i].Operand1.constant_val = constant_vals[ir[i].Operand1];
                        ir[i].Operand1.logical_var = 0;
                    }
                    if(ir[i].Operand1.constant_val != null)
                    {
                        if (EvaluateConstant(ir[i]))
                        {
                            if ((ir[i].Result.type == var.var_type.LogicalVar) && !exclude_vars.Contains(ir[i].Result))
                            {
                                constant_vals.Add(ir[i].Result, ir[i].Operand1.constant_val);
                                if (CanRemove(ir[i].Result, ir))
                                {
                                    ir.RemoveAt(i);
                                    continue;
                                }
                            }
                        }
                    }
                }
                if (optype == ThreeAddressCode.OpType.CallOp)
                {
                    CallEx ce = ir[i] as CallEx;
                    for(int j = 0; j < ce.Var_Args.Length; j++)
                    {
                        if (constant_vals.ContainsKey(ce.Var_Args[j]))
                        {
                            ce.Var_Args[j].constant_val = constant_vals[ce.Var_Args[j]];
                            ce.Var_Args[j].logical_var = 0;
                        }
                    }
                }
                i++;
            }
        }

        private bool CanRemove(int var_no, List<ThreeAddressCode> ir)
        {
            return false;
            foreach (ThreeAddressCode i in ir)
            {
                if (i is PhiEx)
                {
                    PhiEx phi = i as PhiEx;
                    if (phi.Var_Args.Contains(var_no))
                        return false;
                }
                //if (i.Operand1.logical_var == var_no)
                if(i.Operand1.DependsOn(var_no))
                {
                    if(i.Operand1.type == var.var_type.ContentsOf || i.Operand1.type == var.var_type.ContentsOfPlusConstant || i.GetOpType() == ThreeAddressCode.OpType.OtherOp)
                        return false;
                }
                //if (i.Operand2.logical_var == var_no)
                if(i.Operand2.DependsOn(var_no))
                {
                    if (i.Operand2.type == var.var_type.ContentsOf || i.Operand2.type == var.var_type.ContentsOfPlusConstant || i.GetOpType() == ThreeAddressCode.OpType.OtherOp)
                        return false;
                }
                //if (i.Result.logical_var == var_no)
                if(i.Result.DependsOn(var_no))
                {
                    if (i.Result.type == var.var_type.ContentsOf || i.Result.type == var.var_type.ContentsOfPlusConstant || i.GetOpType() == ThreeAddressCode.OpType.OtherOp)
                        return false;
                }
            }
            return true;
        }

        private bool EvaluateConstant(ThreeAddressCode threeAddressCode)
        {
            switch (threeAddressCode.Operator)
            {
                case ThreeAddressCode.Op.conv_i_i1sx:
                case ThreeAddressCode.Op.conv_i4_i1sx:
                case ThreeAddressCode.Op.conv_i8_i1sx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayI1(ToByteArraySignExtend(threeAddressCode.Operand1.constant_val, 1));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i4;
                    return true;
                case ThreeAddressCode.Op.conv_i_i2sx:
                case ThreeAddressCode.Op.conv_i4_i2sx:
                case ThreeAddressCode.Op.conv_i8_i2sx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayI2(ToByteArraySignExtend(threeAddressCode.Operand1.constant_val, 2));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i4;
                    return true;
                case ThreeAddressCode.Op.conv_i_i4sx:
                case ThreeAddressCode.Op.conv_i8_i4sx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayI4(ToByteArraySignExtend(threeAddressCode.Operand1.constant_val, 4));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i4;
                    return true;
                case ThreeAddressCode.Op.conv_i_i8sx:
                case ThreeAddressCode.Op.conv_i4_i8sx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayI8(ToByteArraySignExtend(threeAddressCode.Operand1.constant_val, 8));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i8;
                    return true;
                case ThreeAddressCode.Op.conv_i_isx:
                case ThreeAddressCode.Op.conv_i4_isx:
                case ThreeAddressCode.Op.conv_i8_isx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayI(ToByteArraySignExtend(threeAddressCode.Operand1.constant_val, GetSizeOfIntPtr()));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i;
                    return true;
                case ThreeAddressCode.Op.conv_i_u1zx:
                case ThreeAddressCode.Op.conv_i4_u1zx:
                case ThreeAddressCode.Op.conv_i8_u1zx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayU1(ToByteArrayZeroExtend(threeAddressCode.Operand1.constant_val, 1));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i4;
                    return true;
                case ThreeAddressCode.Op.conv_i_u2zx:
                case ThreeAddressCode.Op.conv_i4_u2zx:
                case ThreeAddressCode.Op.conv_i8_u2zx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayU2(ToByteArrayZeroExtend(threeAddressCode.Operand1.constant_val, 2));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i4;
                    return true;
                case ThreeAddressCode.Op.conv_i_u4zx:
                case ThreeAddressCode.Op.conv_i8_u4zx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayU4(ToByteArrayZeroExtend(threeAddressCode.Operand1.constant_val, 4));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i4;
                    return true;
                case ThreeAddressCode.Op.conv_i_u8zx:
                case ThreeAddressCode.Op.conv_i4_u8zx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayU8(ToByteArrayZeroExtend(threeAddressCode.Operand1.constant_val, 8));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i8;
                    return true;
                case ThreeAddressCode.Op.conv_i_uzx:
                case ThreeAddressCode.Op.conv_i4_uzx:
                case ThreeAddressCode.Op.conv_i8_uzx:
                    threeAddressCode.Operand1.constant_val = FromByteArrayU(ToByteArrayZeroExtend(threeAddressCode.Operand1.constant_val, GetSizeOfUIntPtr()));
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i;
                    return true;
                case ThreeAddressCode.Op.add_i:
                case ThreeAddressCode.Op.mul_i:
                case ThreeAddressCode.Op.sub_i:
                case ThreeAddressCode.Op.mul_un_i:
                case ThreeAddressCode.Op.mul_ovf_un_i:
                case ThreeAddressCode.Op.add_i4:
                case ThreeAddressCode.Op.mul_i4:
                case ThreeAddressCode.Op.sub_i4:
                case ThreeAddressCode.Op.mul_un_i4:
                case ThreeAddressCode.Op.mul_ovf_un_i4:
                case ThreeAddressCode.Op.add_i8:
                case ThreeAddressCode.Op.mul_i8:
                case ThreeAddressCode.Op.sub_i8:
                case ThreeAddressCode.Op.mul_un_i8:
                case ThreeAddressCode.Op.mul_ovf_un_i8:
                    threeAddressCode.Operand1.constant_val = EvaluateConstant(threeAddressCode.Operator, threeAddressCode.Operand1.constant_val, threeAddressCode.Operand2.constant_val);
                    threeAddressCode.Operand2 = var.Null;
                    threeAddressCode.Operator = ThreeAddressCode.Op.assign_i;

                    // There may be a 'throw' instruction which follows this - remove it
                    if (threeAddressCode.remove_if_optimized != null)
                    {
                        foreach (ThreeAddressCode rio in threeAddressCode.remove_if_optimized)
                            rio.optimized_out_by_removal_of_another_instruction = true;
                    }
                    return true;
            }
            return false;
        }

        private object EvaluateConstant(ThreeAddressCode.Op op, object a, object b)
        {
            switch (op)
            {
                case ThreeAddressCode.Op.add_i:
                    if (GetBitness() == Bitness.Bits64)
                        return EvaluateConstant(ThreeAddressCode.Op.add_i8, a, b);
                    else
                        return EvaluateConstant(ThreeAddressCode.Op.add_i4, a, b);
                case ThreeAddressCode.Op.sub_i:
                    if (GetBitness() == Bitness.Bits64)
                        return EvaluateConstant(ThreeAddressCode.Op.sub_i8, a, b);
                    else
                        return EvaluateConstant(ThreeAddressCode.Op.sub_i4, a, b);
                case ThreeAddressCode.Op.mul_i:
                    if (GetBitness() == Bitness.Bits64)
                        return EvaluateConstant(ThreeAddressCode.Op.mul_i8, a, b);
                    else
                        return EvaluateConstant(ThreeAddressCode.Op.mul_i4, a, b);
                case ThreeAddressCode.Op.mul_un_i:
                    if (GetBitness() == Bitness.Bits64)
                        return EvaluateConstant(ThreeAddressCode.Op.mul_un_i8, a, b);
                    else
                        return EvaluateConstant(ThreeAddressCode.Op.mul_un_i4, a, b);
                case ThreeAddressCode.Op.mul_ovf_un_i:
                    if (GetBitness() == Bitness.Bits64)
                        return EvaluateConstant(ThreeAddressCode.Op.mul_ovf_un_i8, a, b);
                    else
                        return EvaluateConstant(ThreeAddressCode.Op.mul_ovf_un_i4, a, b);

                case ThreeAddressCode.Op.add_i4:
                case ThreeAddressCode.Op.sub_i4:
                case ThreeAddressCode.Op.mul_un_i4:
                case ThreeAddressCode.Op.mul_ovf_un_i4:
                    {
                        uint a1 = FromByteArrayU4(ToByteArrayZeroExtend(a, 4));
                        uint b1 = FromByteArrayU4(ToByteArrayZeroExtend(b, 4));

                        switch (op)
                        {
                            case ThreeAddressCode.Op.add_i4:
                                return a1 + b1;
                            case ThreeAddressCode.Op.sub_i4:
                                return a1 - b1;
                            case ThreeAddressCode.Op.mul_un_i4:
                            case ThreeAddressCode.Op.mul_ovf_un_i4:
                                return a1 * b1;
                        }
                    }
                    break;

                case ThreeAddressCode.Op.add_i8:
                case ThreeAddressCode.Op.sub_i8:
                case ThreeAddressCode.Op.mul_un_i8:
                case ThreeAddressCode.Op.mul_ovf_un_i8:
                    {
                        ulong a1 = FromByteArrayU8(ToByteArrayZeroExtend(a, 8));
                        ulong b1 = FromByteArrayU8(ToByteArrayZeroExtend(b, 8));

                        switch (op)
                        {
                            case ThreeAddressCode.Op.add_i8:
                                return a1 + b1;
                            case ThreeAddressCode.Op.sub_i8:
                                return a1 - b1;
                            case ThreeAddressCode.Op.mul_un_i8:
                            case ThreeAddressCode.Op.mul_ovf_un_i8:
                                return a1 * b1;
                        }
                    }
                    break;

                case ThreeAddressCode.Op.mul_i4:
                    {
                        int a1 = FromByteArrayI4(ToByteArraySignExtend(a, 4));
                        int b1 = FromByteArrayI4(ToByteArraySignExtend(b, 4));

                        switch (op)
                        {
                            case ThreeAddressCode.Op.mul_i4:
                                return a1 * b1;
                        }
                    }
                    break;

                case ThreeAddressCode.Op.mul_i8:
                    {
                        long a1 = FromByteArrayI8(ToByteArraySignExtend(a, 8));
                        long b1 = FromByteArrayI8(ToByteArraySignExtend(b, 8));

                        switch (op)
                        {
                            case ThreeAddressCode.Op.mul_i8:
                                return a1 * b1;
                        }
                    }
                    break;
            }

            throw new NotSupportedException();
        }

        internal virtual bool ArchSpecificEvaluateConstant(ThreeAddressCode threeAddressCode)
        { return false; }

        private void CleanseOutput(List<ThreeAddressCode> ir, List<int> global_vars)
        {
            // Remove unnecessary jumps and phi functions
            int i = 0;
            while (i < ir.Count)
            {
                if (ir[i] is BrEx)
                {
                    if ((i + 1) < ir.Count)
                    {
                        if (ir[i + 1].Operator == ThreeAddressCode.Op.label)
                        {
                            if (((LabelEx)ir[i + 1]).Block_id == ((BrEx)ir[i]).Block_Target) {
                                ir.RemoveAt(i);
                                continue;
                            }
                        }
                    }
                }

                i++;
            }
        }

        private void PopulatePhi(List<cfg_node> nodes)
        {
            foreach (cfg_node cfg_node in nodes)
                PopulatePhi(cfg_node);
        }

        private void PopulatePhi(cfg_node cfg_node)
        {
            if ((cfg_node._tacs_phi.Count > 0) && (cfg_node._tacs_phi[0].GetOpType() == ThreeAddressCode.OpType.PhiOp))
            {
                foreach(ThreeAddressCode tac in cfg_node._tacs_phi)
                {
                    if (!(tac is PhiEx))
                        continue;
                    PhiEx phi = tac as PhiEx;

                    foreach (cfg_node pred in cfg_node.ipred)
                    {
                        int vnum;
                        switch (phi.VarLoc)
                        {
                            case PhiEx.VariableLoc.LArg:
                                vnum = pred.la_after[phi.VarNum].contains_variable;
                                break;
                            case PhiEx.VariableLoc.LVar:
                                vnum = pred.lv_after[phi.VarNum].contains_variable;
                                break;
                            case PhiEx.VariableLoc.PStack:
                                vnum = pred.pstack_after[phi.VarNum].contains_variable;
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        if ((vnum != 0) & (!phi.Var_Args.Contains(vnum)))
                            phi.Var_Args.Add(vnum);
                    }
                }
            }
        }

        /*Signature.Param GetArgument(Metadata m, Metadata.MethodDefRow meth, int p)
        {
            Signature.Param param = null;
            Signature.Method sig = Signature.ParseMethodDefSig(m, meth.Signature);
            if ((sig.HasThis == true) && (sig.ExplicitThis == false))
            {
                if (p == 0)
                {
                    param = new Signature.Param();
                    param.ByRef = false;
                    param.CustomMods = new List<Signature.CustomMod>();
                    param.TypedByRef = false;
                    param.Type = new Signature.ComplexType { Pinned = false, Type = new Metadata.TableIndex(m, Metadata.GetOwningType(m, meth)).ToToken(m) };
                }
                else
                    param = sig.Params[p - 1];
            }
            else
                param = sig.Params[p];
            return param;
        } */

        internal abstract void ArchSpecific(List<ThreeAddressCode> ir, List<cfg_node> nodes, AssemblerState state, MethodToCompile mtc);
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
        
        internal enum Bitness { Bits32, Bits64 };
        internal abstract Bitness GetBitness();

        internal abstract IEnumerable<hardware_location> GetAllHardwareLocationsOfType(System.Type type, hardware_location example);
        internal virtual bool IsLocationAllowed(hardware_location hloc) { return true; }

        //protected abstract hardware_location GetLocalArgLocation(var v);
        //protected abstract hardware_location GetLocalVarLocation(var v);

        internal abstract List<byte> SaveLocation(hardware_location loc);
        internal abstract List<byte> RestoreLocation(hardware_location loc);
        internal abstract List<byte> SwapLocation(hardware_location a, hardware_location b);

        internal abstract void Assign(hardware_location dest, hardware_location from, List<OutputBlock> ret, AssemblerState state);

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
            return GetArrayFieldOffset(ArrayFields.inner_array);
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
            int32, int64, native_int, F32, F64, O, reference, void_, vt, virtftnptr
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

        internal bool Implements(ThreeAddressCode.Op op)
        {
            return output_opcodes.ContainsKey(op);
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
