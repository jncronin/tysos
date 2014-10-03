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


/* From http://docs.go-mono.com/System.Array and CIL I:8.9.1
 * 
 * Every ComplexArray class is a new class created by the execution environment
 * It is a subclass of System.Array and defines the following five methods:
 * 
 * ElemType Get(int, ...)
 * void Set(int, ..., ElemType val)
 * ElemType & Address(int, ...)
 * .ctor(int size, ...)
 * .ctor(int lobound, int size, ...)
 * 
 * In addition, to assist with the System.Array.Get/SetValue[Impl] functions, we define
 * the following virtual methods:
 * object GetValueImpl(int pos)
 * 
 * Where the variable argument list is of length 'rank'
 * 
 * E.g. for a array with rank 3:
 * ElemType Get(int, int, int)
 * void Set(int, int, int, ElemType val)
 * ElemType & Address(int, int, int)
 * .ctor(int size, int size, int size)
 * .ctor(int lobound, int size, int lobound, int size, int lobound, int size)
 * 
 * 
 * The array object defines the following fields (in addition to those inherited from
 * System.Array):
 * 
 * int rank
 * int elem_size
 * int inner_array_length
 * intptr elem_type_ti
 * int[] lobounds
 * int[] sizes
 * ElemType[] inner_array
 * 
 * and defines the methods above.
 * 
 * In addition, we define the method int vector_index(int index1, int index2, int index3, etc)
 * to identify the index within inner_array that contains the element we want
 * 
 * vector_index is defined as:
 * 
 * rank3_index - rank3_lobound + rank3_size * ( rank2_index - rank2_lobound * rank2_size * ( rank1_index - rank1_lobound ) )
 */


using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    partial class Assembler
    {
        Dictionary<Signature.BaseOrComplexType, Assembler.TypeToCompile> complex_array_cache = new Dictionary<Signature.BaseOrComplexType, TypeToCompile>(new GenericEqualityComparer<Signature.BaseOrComplexType>());

        public Assembler.TypeToCompile CreateArray(Signature.Param array_sig, int rank, Assembler.TypeToCompile elem_type)
        { return CreateArray(array_sig, rank, elem_type, true); }
        public Assembler.TypeToCompile CreateArray(Signature.Param array_sig, int rank, Assembler.TypeToCompile elem_type, bool request_new_types)
        {
            Signature.BaseOrComplexType ca = array_sig.Type;
            if (ca == null)
                throw new NotSupportedException();

            ca._ass = this;
            if (complex_array_cache.ContainsKey(ca))
                return (complex_array_cache[ca]);

            Assembler.TypeToCompile ret = _CreateArray(array_sig, rank, elem_type, request_new_types);
            complex_array_cache.Add(ca, ret);
            return ret;
        }
        internal Assembler.TypeToCompile _CreateArray(Signature.Param array_sig, int rank, Assembler.TypeToCompile elem_type, bool request_new_types)
        {
            int elem_size = GetPackedSizeOf(elem_type.tsig);
            string mangled_et = Mangler2.MangleTypeInfo(elem_type, this);
            if(request_new_types && Options.EnableRTTI)
                Requestor.RequestTypeInfo(elem_type);

            // Create the array type
            Metadata.TypeDefRow array_type = new Metadata.TypeDefRow();
            if (array_sig.Type is Signature.ZeroBasedArray)
                array_type._ActualTypeName = "__SzArray_" + mangled_et;
            else
                array_type._ActualTypeName = "__Array_" + mangled_et;
            array_type._ActualTypeNamespace = "System";
            array_type.m = this.FindAssembly("mscorlib");
            array_type.Extends = new Metadata.TableIndex(Metadata.GetTypeDef("mscorlib", "System", "Array", this));
            array_type.Flags = 0x501;
            Assembler.TypeToCompile ret = new TypeToCompile { _ass = this, type = array_type, tsig = array_sig };
            ((Signature.BaseArray)array_sig.Type).ArrayType = array_type;

            // Create its fields
            Metadata.FieldRow rank_fr = new Metadata.FieldRow();
            rank_fr.Flags = 0x400;
            rank_fr.m = array_type.m;
            rank_fr.Name = "__rank";
            rank_fr.owning_type = array_type;
            rank_fr.fsig = new Signature.Field { Type = new Signature.BaseType(BaseType_Type.I4) };
            rank_fr.RuntimeInternal = true;
            array_type.Fields.Add(rank_fr);

            Metadata.FieldRow elemsize_fr = new Metadata.FieldRow();
            elemsize_fr.Flags = 0x400;
            elemsize_fr.m = array_type.m;
            elemsize_fr.Name = "__elemsize";
            elemsize_fr.owning_type = array_type;
            elemsize_fr.fsig = new Signature.Field { Type = new Signature.BaseType(BaseType_Type.I4) };
            elemsize_fr.RuntimeInternal = true;
            array_type.Fields.Add(elemsize_fr);

            Metadata.FieldRow ia_length_fr = new Metadata.FieldRow();
            ia_length_fr.Flags = 0x400;
            ia_length_fr.m = array_type.m;
            ia_length_fr.Name = "__inner_array_length";
            ia_length_fr.owning_type = array_type;
            ia_length_fr.fsig = new Signature.Field { Type = new Signature.BaseType(BaseType_Type.I4) };
            ia_length_fr.RuntimeInternal = true;
            array_type.Fields.Add(ia_length_fr);

            Metadata.FieldRow elemtype_ti_fr = new Metadata.FieldRow();
            elemtype_ti_fr.Flags = 0x400;
            elemtype_ti_fr.m = array_type.m;
            elemtype_ti_fr.Name = "__elemtype";
            elemtype_ti_fr.owning_type = array_type;
            elemtype_ti_fr.fsig = new Signature.Field { Type = new Signature.BaseType(BaseType_Type.I) };
            elemtype_ti_fr.RuntimeInternal = true;
            array_type.Fields.Add(elemtype_ti_fr);

            Metadata.FieldRow lobounds_fr = new Metadata.FieldRow();
            lobounds_fr.Flags = 0x400;
            lobounds_fr.m = array_type.m;
            lobounds_fr.Name = "__lobounds";
            lobounds_fr.owning_type = array_type;
            lobounds_fr.fsig = new Signature.Field { Type = new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.I4), numElems = rank } };
            lobounds_fr.RuntimeInternal = true;
            array_type.Fields.Add(lobounds_fr);

            Metadata.FieldRow sizes_fr = new Metadata.FieldRow();
            sizes_fr.Flags = 0x400;
            sizes_fr.m = array_type.m;
            sizes_fr.Name = "__sizes";
            sizes_fr.owning_type = array_type;
            sizes_fr.fsig = new Signature.Field { Type = new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.I4), numElems = rank } };
            sizes_fr.RuntimeInternal = true;
            array_type.Fields.Add(sizes_fr);

            Metadata.FieldRow inner_array_fr = new Metadata.FieldRow();
            inner_array_fr.Flags = 0x400;
            inner_array_fr.m = array_type.m;
            inner_array_fr.Name = "__inner_array";
            inner_array_fr.owning_type = array_type;
            inner_array_fr.fsig = new Signature.Field { Type = new Signature.ZeroBasedArray { ElemType = elem_type.tsig.Type } };
            inner_array_fr.RuntimeInternal = true;
            array_type.Fields.Add(inner_array_fr);

            FieldToCompile lobounds_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = lobounds_fr,
                fsig = lobounds_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };
            FieldToCompile sizes_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = sizes_fr,
                fsig = sizes_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };
            FieldToCompile inner_array_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = inner_array_fr,
                fsig = inner_array_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };
            FieldToCompile rank_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = rank_fr,
                fsig = rank_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };
            FieldToCompile elemsize_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = elemsize_fr,
                fsig = elemsize_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };
            FieldToCompile ia_length_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = ia_length_fr,
                fsig = ia_length_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };
            FieldToCompile elemtype_ftc = new FieldToCompile
            {
                _ass = this,
                definedin_tsig = ret.tsig,
                definedin_type = ret.type,
                field = elemtype_ti_fr,
                fsig = elemtype_ti_fr.fsig,
                memberof_tsig = ret.tsig,
                memberof_type = ret.type
            };

            // Create its methods

            // static int vector_index(int[] lobounds, int[] sizes, int idx1, int idx2, int idx3, ...)
#if false
            Metadata.MethodDefRow vector_index_mdr = new Metadata.MethodDefRow();
            vector_index_mdr.Body = new Metadata.MethodBody();

            Signature.Method vector_index_msig = new Signature.Method { CallingConvention = libtysila.Signature.Method.CallConv.Default, ExplicitThis = false, GenParamCount = 0, HasThis = false,
                m = array_type.m, ParamCount = rank + 2, RetType = new Signature.Param(BaseType_Type.I4) };
            Assembler.cfg_node vector_index_meth_node = new cfg_node(0, new MethodToCompile(this, vector_index_mdr, vector_index_msig, array_type, array_sig, null));

            for (int x = 0; x < rank; x++)
            {
                if (x == 0)
                {
                    /* ldarg x + 2
                     * ldarg lobounds (=0)
                     * ldc $x
                     * ldelem
                     * sub
                     */

                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = x + 2 });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = 0 });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4_s], inline_int = x });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelem_i4], int_array = true });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.sub] });
                }
                else        // x >= 1
                {
                    /* ldarg sizes (=1)
                     * ldc $x
                     * ldelem
                     * mul
                     * ldarg x + 2
                     * add
                     * ldarg lobounds (=0)
                     * ldc $x
                     * ldelem
                     * sub
                     */

                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_1] });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4_s], inline_int = x });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelem_i4], int_array = true });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.mul] });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = x + 2 });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.add] });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4_s], inline_int = x });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelem_i4], int_array = true });
                    vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.sub] });
                }
            }
            vector_index_meth_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            vector_index_mdr.nodes = new List<cfg_node> { vector_index_meth_node };
            vector_index_mdr.Body.InitLocals = false;
            vector_index_mdr.Body.LocalVars = null;
            vector_index_mdr.Body.MaxStack = 3;
            vector_index_mdr.Flags = 0x18b0;
            vector_index_mdr.ImplFlags = 0;
            vector_index_mdr.IsEntryPoint = false;
            vector_index_mdr.Name = "__vector_index";
            vector_index_mdr.owning_type = array_type;
            vector_index_mdr.m = array_type.m;
            vector_index_mdr.ParamList = new Metadata.TableIndex(array_type.m);
   
            vector_index_msig.Params.Add(new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.I4)}, this));
            vector_index_msig.Params.Add(new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.I4)}, this));
            for(int x = 0; x < rank; x++)
                vector_index_msig.Params.Add(new Signature.Param(BaseType_Type.I4));
            vector_index_mdr.msig = vector_index_msig;

            array_type.Methods.Add(vector_index_mdr);


            // instance ElemType Get(int i1, int i2, int i3, ...)
            Metadata.MethodDefRow get_mdr = new Metadata.MethodDefRow();
            get_mdr.Body = new Metadata.MethodBody();
            Assembler.cfg_node get_node = new cfg_node(0, new MethodToCompile(this, get_mdr, null, array_type, array_sig, null));
            _get_array_index_stack(get_node, rank, elem_type, inner_array_fr, lobounds_fr, sizes_fr, vector_index_mdr, ret);
            get_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelem], inline_tok = new TTCToken { ttc = elem_type }, int_array = true });
            get_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            get_mdr.nodes = new List<cfg_node> { get_node };
            get_mdr.Body.InitLocals = false;
            get_mdr.Body.LocalVars = null;
            get_mdr.Body.MaxStack = (uint)(rank + 3);
            get_mdr.Flags = 0x18a0;
            get_mdr.ImplFlags = 0;
            get_mdr.IsEntryPoint = false;
            get_mdr.Name = "Get";
            get_mdr.owning_type = array_type;
            get_mdr.ParamList = new Metadata.TableIndex(array_type.m);
            get_mdr.m = array_type.m;

            Signature.Method get_sig = new Signature.Method
            {
                CallingConvention = Signature.Method.CallConv.Default,
                ExplicitThis = false,
                GenParamCount = 0,
                HasThis = true,
                m = array_type.m,
                ParamCount = rank,
                RetType = new Signature.Param(elem_type.tsig.Type, this)
            };
            for (int x = 0; x < rank; x++)
                get_sig.Params.Add(new Signature.Param(BaseType_Type.I4));
            get_mdr.msig = get_sig;

            array_type.Methods.Add(get_mdr);


            // instance object GetValueImpl(int pos)
            Metadata.MethodDefRow getvalueimpl_mdr = new Metadata.MethodDefRow();
            getvalueimpl_mdr.Body = new Metadata.MethodBody();
            Assembler.cfg_node getvalueimpl_node = new cfg_node(0, new MethodToCompile(this, getvalueimpl_mdr, null, array_type, array_sig, null));
            getvalueimpl_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            getvalueimpl_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = inner_array_ftc } });
            getvalueimpl_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_1] });
            getvalueimpl_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelem], inline_tok = new TTCToken { ttc = elem_type }, int_array = true });
            getvalueimpl_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.box], inline_tok = new TTCToken { ttc = elem_type } });
            getvalueimpl_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            getvalueimpl_mdr.nodes = new List<cfg_node> { getvalueimpl_node };
            getvalueimpl_mdr.Body.InitLocals = false;
            getvalueimpl_mdr.Body.LocalVars = null;
            getvalueimpl_mdr.Body.MaxStack = 1;
            getvalueimpl_mdr.Flags = 0x18e0;
            getvalueimpl_mdr.ImplFlags = 0;
            getvalueimpl_mdr.IsEntryPoint = false;
            getvalueimpl_mdr.Name = "GetValueImpl";
            getvalueimpl_mdr.owning_type = array_type;
            getvalueimpl_mdr.ParamList = new Metadata.TableIndex(array_type.m);
            getvalueimpl_mdr.m = array_type.m;

            Signature.Method getvalueimpl_sig = new Signature.Method
            {
                CallingConvention = libtysila.Signature.Method.CallConv.Default,
                ExplicitThis = false,
                GenParamCount = 0,
                HasThis = true,
                m = array_type.m,
                ParamCount = 1,
                RetType = new Signature.Param(BaseType_Type.Object)
            };
            getvalueimpl_sig.Params.Add(new Signature.Param(BaseType_Type.I4));
            getvalueimpl_mdr.msig = getvalueimpl_sig;

            array_type.Methods.Add(getvalueimpl_mdr);
            


            // instance void Set(int i1, int i2, int i3, ..., ElemType val)
            Metadata.MethodDefRow set_mdr = new Metadata.MethodDefRow();
            set_mdr.Body = new Metadata.MethodBody();
            Assembler.cfg_node set_node = new cfg_node(0, new MethodToCompile(this, set_mdr, null, array_type, array_sig, null));
            _get_array_index_stack(set_node, rank, elem_type, inner_array_fr, lobounds_fr, sizes_fr, vector_index_mdr, ret);
            set_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = rank + 1 });
            set_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stelem], inline_tok = new TTCToken { ttc = elem_type }, int_array = true });
            set_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            set_mdr.nodes = new List<cfg_node> { set_node };
            set_mdr.Body.InitLocals = false;
            set_mdr.Body.LocalVars = null;
            set_mdr.Body.MaxStack = (uint)(rank + 4);
            set_mdr.Flags = 0x18a0;
            set_mdr.ImplFlags = 0;
            set_mdr.IsEntryPoint = false;
            set_mdr.Name = "Set";
            set_mdr.owning_type = array_type;
            set_mdr.ParamList = new Metadata.TableIndex(array_type.m);
            set_mdr.m = array_type.m;

            Signature.Method set_sig = new Signature.Method
            {
                CallingConvention = Signature.Method.CallConv.Default,
                ExplicitThis = false,
                GenParamCount = 0,
                HasThis = true,
                m = array_type.m,
                ParamCount = rank + 1,
                RetType = new Signature.Param(BaseType_Type.Void)
            };
            for(int x = 0; x < rank; x++)
                set_sig.Params.Add(new Signature.Param(BaseType_Type.I4));
            set_sig.Params.Add(new Signature.Param(elem_type.tsig.Type, this));
            set_mdr.msig = set_sig;

            array_type.Methods.Add(set_mdr);


            // instance ElemType & Address(int i1, int i2, int i3, ...)
            Metadata.MethodDefRow addr_mdr = new Metadata.MethodDefRow();
            addr_mdr.Body = new Metadata.MethodBody();
            Assembler.cfg_node addr_node = new cfg_node(0, new MethodToCompile(this, addr_mdr, null, array_type, array_sig, null));
            _get_array_index_stack(addr_node, rank, elem_type, inner_array_fr, lobounds_fr, sizes_fr, vector_index_mdr, ret);
            addr_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldelema], inline_tok = new TTCToken { ttc = elem_type }, int_array = true });
            addr_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            addr_mdr.nodes = new List<cfg_node> { addr_node };
            addr_mdr.Body.InitLocals = false;
            addr_mdr.Body.LocalVars = null;
            addr_mdr.Body.MaxStack = (uint)(rank + 3);
            addr_mdr.Flags = 0x18a0;
            addr_mdr.ImplFlags = 0;
            addr_mdr.IsEntryPoint = false;
            addr_mdr.Name = "Address";
            addr_mdr.owning_type = array_type;
            addr_mdr.ParamList = new Metadata.TableIndex(array_type.m);
            addr_mdr.m = array_type.m;

            Signature.Method addr_sig = new Signature.Method
            {
                CallingConvention = Signature.Method.CallConv.Default,
                ExplicitThis = false,
                GenParamCount = 0,
                HasThis = true,
                m = array_type.m,
                ParamCount = rank,
                RetType = new Signature.Param(new Signature.ManagedPointer { ElemType = elem_type.tsig.Type }, this)
            };
            for (int x = 0; x < rank; x++)
                addr_sig.Params.Add(new Signature.Param(BaseType_Type.I4));
            addr_mdr.msig = addr_sig;

            array_type.Methods.Add(addr_mdr);


            // instance void .ctor(int size_1, int size_2, int size_3, ...)
            Metadata.MethodDefRow ctor_1_mdr = new Metadata.MethodDefRow();
            ctor_1_mdr.Body = new Metadata.MethodBody();
            ctor_1_mdr.Body.LVars = new Signature.LocalVars { Vars = new List<Signature.Param> { new Signature.Param(BaseType_Type.I4) } };
            Assembler.cfg_node ctor_1_node = new cfg_node(0, new MethodToCompile(this, ctor_1_mdr, null, array_type, array_sig, null));

            TypeToCompile Int32_ttc = Metadata.GetTTC("mscorlib", "System", "Int32", this);
            TypeToCompile Int32_arr_ttc = new TypeToCompile { _ass = this, tsig = new Signature.Param(new Signature.ZeroBasedArray { ElemType = Int32_ttc.tsig.Type, numElems = rank }, this), type = Int32_ttc.type };
            TypeToCompile ET_arr_ttc = new TypeToCompile { _ass = this, tsig = new Signature.Param(new Signature.ZeroBasedArray { ElemType = elem_type.tsig.Type }, this), type = elem_type.type };

            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2b], inline_tok = new StringToken { str = mangled_et } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = elemtype_ftc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = rank });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = rank_ftc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = elem_size });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = elemsize_ftc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = rank * GetPackedSizeOf(new Signature.Param(BaseType_Type.I4)) });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2a], inline_tok = new TTCToken { ttc = Int32_arr_ttc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = lobounds_ftc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = rank * GetPackedSizeOf(new Signature.Param(BaseType_Type.I4)) });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2a], inline_tok = new TTCToken { ttc = Int32_arr_ttc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = sizes_ftc } });
            for (int x = 0; x < rank; x++)
            {
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = lobounds_ftc } });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = x });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4_0] });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stelem_i4], int_array = true });
            }
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4_1] });
            for (int x = 0; x < rank; x++)
            {
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = sizes_ftc } });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = x });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = x + 1 });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stelem_i4], int_array = true });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = x + 1 });
                ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.mul] });
            }
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stloc_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = elem_size });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.mul] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2a], inline_tok = new TTCToken { ttc = ET_arr_ttc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = inner_array_ftc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldloc_0] });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = ia_length_ftc } });
            ctor_1_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            ctor_1_mdr.nodes = new List<cfg_node> { ctor_1_node };
            ctor_1_mdr.Body.InitLocals = false;
            ctor_1_mdr.Body.LocalVars = null;
            ctor_1_mdr.Body.MaxStack = 5;
            ctor_1_mdr.Flags = 0x18a0;
            ctor_1_mdr.ImplFlags = 0;
            ctor_1_mdr.IsEntryPoint = false;
            ctor_1_mdr.Name = ".ctor";
            ctor_1_mdr.owning_type = array_type;
            ctor_1_mdr.ParamList = new Metadata.TableIndex(array_type.m);
            ctor_1_mdr.m = array_type.m;

            Signature.Method ctor_1_sig = new Signature.Method
            {
                CallingConvention = Signature.Method.CallConv.Default,
                ExplicitThis = false,
                GenParamCount = 0,
                HasThis = true,
                m = array_type.m,
                ParamCount = rank,
                RetType = new Signature.Param(BaseType_Type.Void)
            };
            for (int x = 0; x < rank; x++)
                ctor_1_sig.Params.Add(new Signature.Param(BaseType_Type.I4));
            ctor_1_mdr.msig = ctor_1_sig;

            array_type.Methods.Add(ctor_1_mdr);


            // instance void .ctor(int lobound_1, int size_1, int lobound_2, int size_2, ...)
            Metadata.MethodDefRow ctor_2_mdr = new Metadata.MethodDefRow();
            ctor_2_mdr.Body = new Metadata.MethodBody();
            ctor_2_mdr.Body.LVars = new Signature.LocalVars { Vars = new List<Signature.Param> { new Signature.Param(BaseType_Type.I4) } };
            Assembler.cfg_node ctor_2_node = new cfg_node(0, new MethodToCompile(this, ctor_2_mdr, null, array_type, array_sig, null));

            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2b], inline_tok = new StringToken { str = mangled_et } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = elemtype_ftc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = rank });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = rank_ftc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = elem_size });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = elemsize_ftc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = rank * GetPackedSizeOf(new Signature.Param(BaseType_Type.I4)) });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2a], inline_tok = new TTCToken { ttc = Int32_arr_ttc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = lobounds_ftc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = rank * GetPackedSizeOf(new Signature.Param(BaseType_Type.I4)) });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2a], inline_tok = new TTCToken { ttc = Int32_arr_ttc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = sizes_ftc } });
            for (int x = 0; x < rank; x++)
            {
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = lobounds_ftc } });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = x });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = 2 * x + 1 });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stelem_i4], int_array = true });
            }
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4_1] });
            for (int x = 0; x < rank; x++)
            {
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldfld], inline_tok = new FTCToken { ftc = sizes_ftc } });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = x });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = 2 * x + 2 });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stelem_i4], int_array = true });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = 2 * x + 2 });
                ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.mul] });
            }
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.dup] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stloc_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldc_i4], inline_int = elem_size });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.mul] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[0xfd2a], inline_tok = new TTCToken { ttc = ET_arr_ttc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = inner_array_ftc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldloc_0] });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.stfld], inline_tok = new FTCToken { ftc = ia_length_ftc } });
            ctor_2_node.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ret] });

            ctor_2_mdr.nodes = new List<cfg_node> { ctor_2_node };
            ctor_2_mdr.Body.InitLocals = false;
            ctor_2_mdr.Body.LocalVars = null;
            ctor_2_mdr.Body.MaxStack = 5;
            ctor_2_mdr.Flags = 0x18a0;
            ctor_2_mdr.ImplFlags = 0;
            ctor_2_mdr.IsEntryPoint = false;
            ctor_2_mdr.Name = ".ctor";
            ctor_2_mdr.owning_type = array_type;
            ctor_2_mdr.ParamList = new Metadata.TableIndex(array_type.m);
            ctor_2_mdr.m = array_type.m;

            Signature.Method ctor_2_sig = new Signature.Method
            {
                CallingConvention = Signature.Method.CallConv.Default,
                ExplicitThis = false,
                GenParamCount = 0,
                HasThis = true,
                m = array_type.m,
                ParamCount = rank,
                RetType = new Signature.Param(BaseType_Type.Void)
            };
            for (int x = 0; x < (2 * rank); x++)
                ctor_2_sig.Params.Add(new Signature.Param(BaseType_Type.I4));
            ctor_2_mdr.msig = ctor_2_sig;

            array_type.Methods.Add(ctor_2_mdr);
#endif

            return ret;
        }

#if false
        void _get_array_index_stack(Assembler.cfg_node ret, int rank, Assembler.TypeToCompile elem_type, Metadata.FieldRow inner_array_fr,
            Metadata.FieldRow lobounds_fr, Metadata.FieldRow sizes_fr, Metadata.MethodDefRow vector_index_mdr, Assembler.TypeToCompile array_type)
        {
            /* Return a method containing the following instructions:
             * 
             * ldarg 0 (= this)
             * ldfld ElemType[] inner_array
             * ldarg 0
             * ldfld int[] lobounds
             * ldarg 0
             * ldfld int[] sizes
             * for(x = 0; x < rank; x++) ldarg (x + 1)
             * call __vector_index
             * 
             * This will create a stack with element 0 being the inner array and element 1 being
             * the index within it
             * This is then perfect for passing to ldelem, ldelema and stelem
             */

            ret.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ret.instrs.Add(new InstructionLine
            {
                opcode = Opcodes[(int)SingleOpcodes.ldfld],
                inline_tok = new FTCToken
                {
                    ftc = new FieldToCompile
                    {
                        _ass = this,
                        definedin_tsig = array_type.tsig,
                        definedin_type = array_type.type,
                        memberof_tsig = array_type.tsig,
                        memberof_type = array_type.type,
                        field = inner_array_fr,
                        fsig = inner_array_fr.fsig
                    }
                }
            });
            ret.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ret.instrs.Add(new InstructionLine
            {
                opcode = Opcodes[(int)SingleOpcodes.ldfld],
                inline_tok = new FTCToken
                {
                    ftc = new FieldToCompile
                    {
                        _ass = this,
                        definedin_tsig = array_type.tsig,
                        definedin_type = array_type.type,
                        memberof_tsig = array_type.tsig,
                        memberof_type = array_type.type,
                        field = lobounds_fr,
                        fsig = lobounds_fr.fsig
                    }
                }
            });
            ret.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_0] });
            ret.instrs.Add(new InstructionLine
            {
                opcode = Opcodes[(int)SingleOpcodes.ldfld],
                inline_tok = new FTCToken
                {
                    ftc = new FieldToCompile
                    {
                        _ass = this,
                        definedin_tsig = array_type.tsig,
                        definedin_type = array_type.type,
                        memberof_tsig = array_type.tsig,
                        memberof_type = array_type.type,
                        field = sizes_fr,
                        fsig = sizes_fr.fsig
                    }
                }
            });
            for (int x = 0; x < rank; x++)
                ret.instrs.Add(new InstructionLine { opcode = Opcodes[(int)SingleOpcodes.ldarg_s], inline_int = x + 1 });
            ret.instrs.Add(new InstructionLine
            {
                opcode = Opcodes[(int)SingleOpcodes.call],
                inline_tok = new MTCToken
                {
                    mtc = new MethodToCompile
                    {
                        _ass = this,
                        meth = vector_index_mdr,
                        msig = vector_index_mdr.msig,
                        tsigp = array_type.tsig,
                        type = array_type.type
                    }
                }
            });
        }
#endif

        internal enum ArrayFields { rank, lobounds, sizes, elem_size, inner_array, elemtype, inner_array_length, array_type_size, getvalueimpl_vtbl_offset };
        bool array_fields_calculated = false;
        int array_rank_offset, array_lobounds_offset, array_sizes_offset, array_elem_size_offset, array_inner_array_offset, array_type_size, array_getvalueimpl_vtbl_offset;
        int array_elemtype_offset, array_inner_array_length_offset;
        
        internal int GetArrayFieldOffset(ArrayFields field)
        {
            if (!array_fields_calculated)
            {
                // Generate a simple complex array to interrogate about its fields

                TypeToCompile simple_array = CreateArray(new Signature.Param(new Signature.ComplexArray { _ass = this, ElemType = new Signature.BaseType(BaseType_Type.I4), Rank = 1, LoBounds = new int[] { }, Sizes = new int[] { } },
                    this), 1, Metadata.GetTTC("mscorlib", "System", "Int32", this));
                Layout l = Layout.GetLayout(simple_array, this, false);

                array_rank_offset = l.GetFirstInstanceField("__rank").offset;
                array_lobounds_offset = l.GetFirstInstanceField("__lobounds").offset;
                array_sizes_offset = l.GetFirstInstanceField("__sizes").offset;
                array_elem_size_offset = l.GetFirstInstanceField("__elemsize").offset;
                array_inner_array_offset = l.GetFirstInstanceField("__inner_array").offset;
                array_elemtype_offset = l.GetFirstInstanceField("__elemtype").offset;
                array_inner_array_length_offset = l.GetFirstInstanceField("__inner_array_length").offset;
                array_type_size = l.ClassSize;

                //array_getvalueimpl_vtbl_offset = l.GetVirtualMethod("GetValueImpl").offset;

                array_fields_calculated = true;
            }

            switch (field)
            {
                case ArrayFields.elem_size:
                    return array_elem_size_offset;
                case ArrayFields.inner_array:
                    return array_inner_array_offset;
                case ArrayFields.lobounds:
                    return array_lobounds_offset;
                case ArrayFields.rank:
                    return array_rank_offset;
                case ArrayFields.sizes:
                    return array_sizes_offset;
                case ArrayFields.elemtype:
                    return array_elemtype_offset;
                case ArrayFields.inner_array_length:
                    return array_inner_array_length_offset;
                case ArrayFields.array_type_size:
                    return array_type_size;
                case ArrayFields.getvalueimpl_vtbl_offset:
                    return array_getvalueimpl_vtbl_offset;
            }

            throw new NotSupportedException();
        }
    }
}
