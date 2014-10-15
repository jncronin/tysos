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

namespace libtysila.frontend.cil.OpcodeEncodings
{
    class newobj
    {
        public static void tybel_newobj(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            // ..., arg1, ... argN -> obj

            // Determine the type to create and optionally the constructor to call afterwards
            Assembler.MethodToCompile? constructor;
            Assembler.TypeToCompile type;

            if (il.il.inline_tok is TTCToken)
            {
                type = ((TTCToken)il.il.inline_tok).ttc;
                type.tsig = Signature.ResolveGenericParam(type.tsig, mtc.tsig, mtc.msig, ass);
                constructor = null;
            }
            else if (il.il.inline_tok is MTCToken)
            {
                type = ((MTCToken)il.il.inline_tok).mtc.GetTTC(ass);
                type.tsig = Signature.ResolveGenericParam(type.tsig, mtc.tsig, mtc.msig, ass);
                constructor = ((MTCToken)il.il.inline_tok).mtc;
            }
            else if (il.il.inline_tok.Value is Metadata.TypeDefRow)
                throw new NotSupportedException();
            else
            {
                constructor = Metadata.GetMTC(new Metadata.TableIndex(il.il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);
                type = constructor.Value.GetTTC(ass);
            }

            Signature.Param type_pushes = type.tsig;
            Layout l = Layout.GetLayout(type, ass, false);
            int str_ctor_idx = -1;

            // Determine the parameter count
            int arg_count = 0;
            int stack_arg_count = 0;
            if (constructor.HasValue)
            {
                stack_arg_count = arg_count = constructor.Value.msig.Method.Params.Count;
                if ((constructor.Value.msig.Method.HasThis = true) && (constructor.Value.msig.Method.ExplicitThis == false))
                    arg_count++;
            }

            libasm.hardware_location t1 = ass.GetTemporary(state);
            libasm.hardware_location[] loc_params = new libasm.hardware_location[arg_count];
            Signature.Param[] p_params = new Signature.Param[arg_count];

            for(int i = arg_count - 1; i >= (arg_count - stack_arg_count); i--)
            {
                loc_params[i] = il.stack_vars_after.Pop(ass);
                p_params[i] = il.stack_after.Pop();
            }
            if (arg_count > stack_arg_count)
            {
                loc_params[0] = t1;
                p_params[0] = type_pushes;
            }

            // Allocate space for the new object
            libasm.hardware_location loc_obj = il.stack_vars_after.GetAddressFor(type_pushes, ass);
            libasm.hardware_location t2 = ass.GetTemporary2(state);
            libasm.hardware_location str_length = null;

            if (type.type.IsValueType(ass) && !(type.tsig.Type is Signature.BoxedType))
            {
                // Value types created with newobj are created on the stack
                ass.LoadAddress(state, il.stack_vars_before, t1, loc_obj, il.il.tybel);
            }
            else
            {
                /* String types need special code to calculate the size of the string */
                libasm.hardware_location obj_size = new libasm.const_location { c = l.ClassSize };
                if (Signature.ParamCompare(type_pushes, new Signature.Param(BaseType_Type.String), ass))
                {
                    obj_size = t2;
                    str_length = ass.GetTemporary3(state, Assembler.CliType.int32);
                    str_ctor_idx = get_string_size(il, ass, mtc, ref next_block, state, attrs, constructor.Value, loc_params, t2,
                        ref str_length);
                }

                ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("gcmalloc", false), t1,
                    new libasm.hardware_location[] { obj_size }, ass.callconv_gcmalloc, il.il.tybel);
            }
            ass.Assign(state, il.stack_vars_before, loc_obj, t1, Assembler.CliType.O, il.il.tybel);

            // Fill in the various runtime initialized fields
            if (l.has_vtbl)
            {
                l = Layout.GetTypeInfoLayout(type, ass, false);
                ass.Assign(state, il.stack_vars_before,
                    new libasm.hardware_contentsof { base_loc = t1, const_offset = l.vtbl_offset, size = ass.GetSizeOfPointer() },
                    new libasm.hardware_addressoflabel(l.typeinfo_object_name, l.FixedLayout[Layout.ID_VTableStructure].Offset, true),
                    Assembler.CliType.native_int, il.il.tybel);
            }
            if (l.has_obj_id)
            {
                Stack temp_stack = il.stack_vars_before.Clone();
                temp_stack.MarkUsed(t1);
                ass.Call(state, temp_stack, new libasm.hardware_addressoflabel("getobjid", false), t2, new libasm.hardware_location[] { },
                    ass.callconv_getobjid, il.il.tybel);
                ass.Assign(state, temp_stack,
                    new libasm.hardware_contentsof { base_loc = t1, const_offset = l.obj_id_offset, size = 4 },
                    t2, Assembler.CliType.int32, il.il.tybel);
            }

            // run constructors
            if (constructor == null)
            {
                ass.Requestor.RequestTypeInfo(type);
            }
            else
            {
                bool _is_ctor = false;

                if (((constructor.Value.meth.Flags & 0x800) == 0x800) && (constructor.Value.meth.Name == ".ctor"))
                {
                    _is_ctor = true;
                    ass.Requestor.RequestTypeInfo(type);
                }

                if (_is_ctor && type.type.IsDelegate(ass))
                {
                    /* requests to .ctor on delegate types need to be rewritten to have their last argument be virtftnptr rather
                     * than IntPtr */

                    /* Because the method may already be listed in Requestor's lists, we need to update the reference in them */
                    if (ass.Requestor is libtysila.Assembler.FileBasedMemberRequestor)
                    {
                        libtysila.Assembler.FileBasedMemberRequestor r = ass.Requestor as libtysila.Assembler.FileBasedMemberRequestor;

                        lock (ass.Requestor.gmi_lock)
                        {
                            lock (ass.Requestor.meth_lock)
                            {
                                bool _in_cm = false;
                                bool _in_rm = false;

                                if (r._compiled_meths.ContainsKey(constructor.Value))
                                {
                                    _in_cm = true;
                                    r._compiled_meths.Remove(constructor.Value);
                                }

                                Signature.Method msig = constructor.Value.msig.Method;
                                msig.Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);

                                if (_in_cm)
                                    r._compiled_meths.Add(constructor.Value, 0);
                                if (_in_rm)
                                    r._requested_meths.Add(constructor.Value, 0);
                            }
                        }
                    }
                    else
                    {
                        // JIT member requestor handles this for us automatically
                        //throw new Exception("JIT not yet supported");
                    }
                }

                if (str_ctor_idx != -1)
                    enc_str_ctor(il, ass, mtc, ref next_block, state, attrs, str_ctor_idx, loc_params, str_length);

                else if (constructor.Value.msig is Signature.Method)
                {
                    Signature.Method msigm = constructor.Value.msig as Signature.Method;

                    string callconv = attrs.call_conv;
                    if ((constructor.Value.meth != null) && (constructor.Value.meth.CallConvOverride != null))
                        callconv = constructor.Value.meth.CallConvOverride;

                    CallConv cc = ass.call_convs[callconv](constructor.Value, CallConv.StackPOV.Caller, ass);
                    Stack temp_stack = il.stack_vars_before.Clone();
                    temp_stack.MarkUsed(t1);
                    ass.Call(state, temp_stack, new libasm.hardware_addressoflabel(Mangler2.MangleMethod(constructor.Value, ass), false),
                        null, loc_params, cc, il.il.tybel);
                }
                else
                {
                    throw new NotImplementedException();
                }

                ass.Requestor.RequestMethod(constructor.Value);
            }

            il.stack_after.Push(type_pushes);
        }

        private static void enc_str_ctor(CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block, Encoder.EncoderState state, 
            Assembler.MethodAttributes attrs, int str_ctor_idx, libasm.hardware_location[] loc_params, libasm.hardware_location loc_strlength)
        {
            libasm.hardware_location t2 = ass.GetTemporary2(state);
            Stack used_locs = il.stack_vars_before.Clone();
            used_locs.MarkUsed(ass.GetTemporary(state)); used_locs.MarkUsed(t2);

            int str_length_offset = ass.GetStringFieldOffset(Assembler.StringFields.length);
            int str_data_offset = ass.GetStringFieldOffset(Assembler.StringFields.data_offset);

            libasm.hardware_location length_ptr = new libasm.hardware_contentsof { base_loc = loc_params[0], const_offset = str_length_offset,
                size = 4 };
            libasm.hardware_location data_ptr = new libasm.hardware_contentsof { base_loc = loc_params[0], const_offset = str_data_offset };
            libasm.hardware_location const_2 = new libasm.const_location { c = 2 };

            switch (str_ctor_idx)
            {
                case 1:
                    /* new string(char c, int count) */
                    ass.Assign(state, used_locs, length_ptr, loc_params[2], Assembler.CliType.int32, il.il.tybel);
                    ass.LoadAddress(state, used_locs, t2, data_ptr, il.il.tybel);
                    ass.WMemSet(state, used_locs, t2, loc_params[1], loc_params[2], il.il.tybel);
                    return;

                case 2:
                    /* new string(char[] value) */
                    ass.Assign(state, used_locs, t2, loc_params[1], Assembler.CliType.native_int, il.il.tybel);
                    ass.Assign(state, used_locs, t2, new libasm.hardware_contentsof { base_loc = t2, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.sizes), size = ass.GetSizeOfPointer() }, Assembler.CliType.native_int, il.il.tybel);
                    ass.Assign(state, used_locs, length_ptr, new libasm.hardware_contentsof { base_loc = t2, const_offset = 0, size = 4 }, Assembler.CliType.int32, il.il.tybel);
                    ass.Assign(state, used_locs, t2, loc_params[1], Assembler.CliType.native_int, il.il.tybel);
                    ass.Assign(state, used_locs, loc_params[1], new libasm.hardware_contentsof { base_loc = t2, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array), size = ass.GetSizeOfPointer() }, Assembler.CliType.native_int, il.il.tybel);
                    ass.Assign(state, used_locs, t2, new libasm.hardware_contentsof { base_loc = t2, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array_length), size = 4 }, Assembler.CliType.int32, il.il.tybel);
                    ass.LoadAddress(state, used_locs, loc_params[0], data_ptr, il.il.tybel);
                    ass.MemCpy(state, used_locs, loc_params[0], loc_params[1], t2, il.il.tybel);
                    return;

                case 3:
                    /* new string(char *value, int startIndex, int length) */
                    ass.Assign(state, used_locs, length_ptr, loc_params[3], Assembler.CliType.int32, il.il.tybel);
                    ass.Mul(state, used_locs, loc_params[2], loc_params[2], const_2, Assembler.CliType.int32, il.il.tybel);
                    ass.Conv(state, used_locs, loc_params[2], loc_params[2], new Signature.BaseType(BaseType_Type.I), new Signature.BaseType(BaseType_Type.I4), true, il.il.tybel);
                    ass.Add(state, used_locs, loc_params[1], loc_params[1], loc_params[2], Assembler.CliType.native_int, il.il.tybel);
                    ass.Mul(state, used_locs, loc_params[3], loc_params[3], const_2, Assembler.CliType.int32, il.il.tybel);
                    ass.LoadAddress(state, used_locs, t2, data_ptr, il.il.tybel);
                    ass.MemCpy(state, used_locs, t2, loc_params[1], loc_params[3], il.il.tybel);
                    return;

                case 4:
                    /* new string(sbyte *value) */
                    ass.Assign(state, used_locs, length_ptr, loc_strlength, Assembler.CliType.int32, il.il.tybel);
                    ass.LoadAddress(state, used_locs, t2, data_ptr, il.il.tybel);
                    ass.Call(state, used_locs, new libasm.hardware_addressoflabel("mbstowcs", false), null,
                        new libasm.hardware_location[] { t2, loc_params[1], loc_strlength }, ass.callconv_mbstowcs, il.il.tybel);
                    return;

                case 5:
                    /* new string(char *value, int startIndex, int length) */
                    ass.Assign(state, used_locs, length_ptr, loc_params[3], Assembler.CliType.int32, il.il.tybel);
                    ass.Mul(state, used_locs, loc_params[2], loc_params[2], const_2, Assembler.CliType.int32, il.il.tybel);
                    ass.Conv(state, used_locs, loc_params[2], loc_params[2], new Signature.BaseType(BaseType_Type.I), new Signature.BaseType(BaseType_Type.I4), true, il.il.tybel);
                    ass.Assign(state, used_locs, loc_params[1], new libasm.hardware_contentsof { base_loc = loc_params[1], const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array), size = ass.GetSizeOfPointer() }, Assembler.CliType.native_int, il.il.tybel);
                    ass.Add(state, used_locs, loc_params[1], loc_params[1], loc_params[2], Assembler.CliType.native_int, il.il.tybel);
                    ass.Mul(state, used_locs, loc_params[3], loc_params[3], const_2, Assembler.CliType.int32, il.il.tybel);
                    ass.LoadAddress(state, used_locs, t2, data_ptr, il.il.tybel);
                    ass.MemCpy(state, used_locs, t2, loc_params[1], loc_params[3], il.il.tybel);
                    return;

                case 6:
                    throw new NotImplementedException();

                case 7:
                    throw new NotImplementedException();

                case 8:
                    throw new NotImplementedException();

                default:
                    throw new NotSupportedException();
            }
        }

        private static int get_string_size(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs, Assembler.MethodToCompile str_mtc, libasm.hardware_location[] loc_params,
            libasm.hardware_location loc_dest, ref libasm.hardware_location loc_strlength)
        {
            Signature.Method c_1 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.Char), new Signature.Param(BaseType_Type.I4) } };
            Signature.Method c_2 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.Char) }, ass) } };
            Signature.Method c_3 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.Char) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };
            Signature.Method c_4 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, ass) } };
            Signature.Method c_5 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.ZeroBasedArray { ElemType = new Signature.BaseType(BaseType_Type.Char) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };
            Signature.Method c_6 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.Char) }, ass) } };
            Signature.Method c_7 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4), new Signature.Param(Metadata.GetTypeDef("mscorlib", "System.Text", "Encoding", ass), ass) } };
            Signature.Method c_8 = new Signature.Method { HasThis = true, RetType = new Signature.Param(BaseType_Type.Void), Params = new List<Signature.Param> { new Signature.Param(new Signature.UnmanagedPointer { BaseType = new Signature.BaseType(BaseType_Type.I1) }, ass), new Signature.Param(BaseType_Type.I4), new Signature.Param(BaseType_Type.I4) } };

            Stack used_locs = il.stack_vars_before.Clone();
            libasm.hardware_location t2 = ass.GetTemporary2(state);
            used_locs.MarkUsed(ass.GetTemporary(state));
            used_locs.MarkUsed(ass.GetTemporary2(state));
                
            if (Signature.BaseMethodSigCompare(c_1, str_mtc.msig, ass))
            {
                /* new string(char c, int count) */
                ass.Assign(state, used_locs, loc_dest, loc_params[2], Assembler.CliType.int32, il.il.tybel);
                ass.Mul(state, used_locs, loc_dest, loc_dest, new libasm.const_location { c = 2 }, Assembler.CliType.int32, il.il.tybel);
                loc_strlength = loc_params[2];
                return 1;
            }
            else if (Signature.BaseMethodSigCompare(c_2, str_mtc.msig, ass))
            {
                /* new string(char[] value) */
                libasm.hardware_location loc_value = loc_params[1];
                if (!(loc_value is libasm.x86_64_gpr))
                {
                    ass.Assign(state, used_locs, t2, loc_value, Assembler.CliType.native_int, il.il.tybel);
                    loc_value = t2;
                }
                ass.Assign(state, used_locs, loc_dest,
                    new libasm.hardware_contentsof { base_loc = loc_value, const_offset = ass.GetArrayFieldOffset(Assembler.ArrayFields.inner_array_length),
                        size = 4 }, Assembler.CliType.int32, il.il.tybel);
                loc_strlength = null;
                return 2;
            }
            else if (Signature.BaseMethodSigCompare(c_3, str_mtc.msig, ass))
            {
                /* new string(char * value, int startIndex, int length) */
                ass.Assign(state, used_locs, loc_dest, loc_params[3], Assembler.CliType.int32, il.il.tybel);
                ass.Mul(state, used_locs, loc_dest, loc_dest, new libasm.const_location { c = 2 }, Assembler.CliType.int32, il.il.tybel);
                loc_strlength = loc_params[3];
                return 3;
            }
            else if (Signature.BaseMethodSigCompare(c_4, str_mtc.msig, ass))
            {
                /* new string(sbyte * value) */
                used_locs.MarkUsed(ass.GetTemporary3(state));

                ass.Call(state, used_locs, new libasm.hardware_addressoflabel("strlen", false), loc_strlength,
                    new libasm.hardware_location[] { loc_params[1] }, ass.callconv_strlen, il.il.tybel);
                ass.Assign(state, used_locs, loc_dest, loc_strlength, Assembler.CliType.int32, il.il.tybel);
                ass.Mul(state, used_locs, loc_dest, loc_dest, new libasm.const_location { c = 2 }, Assembler.CliType.int32, il.il.tybel);
                return 4;
            }
            else if (Signature.BaseMethodSigCompare(c_5, str_mtc.msig, ass))
            {
                /* new string(char[] value, int startIndex, int length) */
                ass.Assign(state, used_locs, loc_dest, loc_params[3], Assembler.CliType.int32, il.il.tybel);
                ass.Mul(state, used_locs, loc_dest, loc_dest, new libasm.const_location { c = 2 }, Assembler.CliType.int32, il.il.tybel);
                loc_strlength = loc_params[3];
                return 3;              
            }
            else if (Signature.BaseMethodSigCompare(c_6, str_mtc.msig, ass))
            {
                throw new NotImplementedException();
            }
            else if (Signature.BaseMethodSigCompare(c_7, str_mtc.msig, ass))
            {
                throw new NotImplementedException();
            }
            else if (Signature.BaseMethodSigCompare(c_8, str_mtc.msig, ass))
            {
                throw new NotImplementedException();
            }
            else
                throw new Assembler.AssemblerException("newobj: unknown System.String constructor: " + str_mtc.ToString(),
                    il.il, mtc);
        }

        public static void enc_newobj(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            // ..., arg1, ... argN -> obj

            // Determine the type to create and optionally the constructor to call afterwards
            Assembler.MethodToCompile? constructor;
            Assembler.TypeToCompile type;

            if (il.inline_tok is TTCToken)
            {
                type = ((TTCToken)il.inline_tok).ttc;
                type.tsig = Signature.ResolveGenericParam(type.tsig, mtc.tsig, mtc.msig, ass);
                constructor = null;
            }
            else if (il.inline_tok is MTCToken)
            {
                type = ((MTCToken)il.inline_tok).mtc.GetTTC(ass);
                type.tsig = Signature.ResolveGenericParam(type.tsig, mtc.tsig, mtc.msig, ass);
                constructor = ((MTCToken)il.inline_tok).mtc;
            }
            else if (il.inline_tok.Value is Metadata.TypeDefRow)
                throw new NotSupportedException();
            else
            {
                constructor = Metadata.GetMTC(new Metadata.TableIndex(il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);
                type = constructor.Value.GetTTC(ass);
            }

            Signature.Param type_pushes = type.tsig;
            Layout l = Layout.GetLayout(type, ass, false);

            // Allocate space for the new object
            vara var_obj = vara.Logical(next_variable++, Assembler.CliType.O);
            if (type.type.IsValueType(ass) && !(type.tsig.Type is Signature.BoxedType))
            {
                // Value types created with newobj are created on the stack
                var_obj.vt_type = type_pushes;
                var_obj.vt_addr_of = true;
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.alloca), var_obj, vara.Const(l.ClassSize), vara.Void()));
            }
            else
            {
                il.tacs.Add(new timple.TimpleCallNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.call), var_obj, vara.Label("gcmalloc", false),
                    new vara[] { vara.Const(l.ClassSize) }, ass.msig_gcmalloc));
            }

            // Fill in the various runtime initialized fields
            if (l.has_vtbl)
            {
                l = Layout.GetTypeInfoLayout(type, ass, false);
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), vara.ContentsOf(var_obj, l.vtbl_offset, Assembler.CliType.native_int), vara.Label(l.typeinfo_object_name, l.FixedLayout[Layout.ID_VTableStructure].Offset, true), vara.Void()));
            }
            if (l.has_obj_id)
            {
                vara var_obj_id = vara.Logical(next_variable++, Assembler.CliType.int32);
                il.tacs.Add(new timple.TimpleCallNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.call), var_obj_id, vara.Label("getobjid", false),
                    new vara[] { }, ass.msig_getobjid));
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), vara.ContentsOf(var_obj, l.obj_id_offset, Assembler.CliType.int32), var_obj_id, vara.Void()));
            }

            // run constructors
            int pop_count = 0;
            
            if (constructor == null)
            {
                ass.Requestor.RequestTypeInfo(type);
            }
            else
            {
                bool _is_ctor = false;

                if (((constructor.Value.meth.Flags & 0x800) == 0x800) && (constructor.Value.meth.Name == ".ctor"))
                {
                    _is_ctor = true;
                    ass.Requestor.RequestTypeInfo(type);
                }

                if (_is_ctor && type.type.IsDelegate(ass))
                {
                    /* requests to .ctor on delegate types need to be rewritten to have their last argument be virtftnptr rather
                     * than IntPtr */

                    /* Because the method may already be listed in Requestor's lists, we need to update the reference in them */
                    if (ass.Requestor is libtysila.Assembler.FileBasedMemberRequestor)
                    {
                        libtysila.Assembler.FileBasedMemberRequestor r = ass.Requestor as libtysila.Assembler.FileBasedMemberRequestor;

                        lock (ass.Requestor.gmi_lock)
                        {
                            lock (ass.Requestor.meth_lock)
                            {
                                bool _in_cm = false;
                                bool _in_rm = false;

                                if (r._compiled_meths.ContainsKey(constructor.Value))
                                {
                                    _in_cm = true;
                                    r._compiled_meths.Remove(constructor.Value);
                                }

                                Signature.Method msig = constructor.Value.msig.Method;
                                msig.Params[1] = new Signature.Param(BaseType_Type.VirtFtnPtr);

                                if (_in_cm)
                                    r._compiled_meths.Add(constructor.Value, 0);
                                if (_in_rm)
                                    r._requested_meths.Add(constructor.Value, 0);
                            }
                        }
                    }
                    else
                    {
                        // JIT member requestor handles this for us automatically
                        //throw new Exception("JIT not yet supported");
                    }
                }

                if (constructor.Value.msig is Signature.Method)
                {
                    Signature.Method msigm = constructor.Value.msig as Signature.Method;
                    int arg_count = msigm.Params.Count;
                    if ((msigm.HasThis == true) && (msigm.ExplicitThis == false))
                        arg_count++;
                    pop_count = arg_count - 1;

                    vara[] var_args = new vara[arg_count];
                    var_args[0] = var_obj;
                    for (int j = 1; j < arg_count; j++)
                        var_args[j] = il.stack_vars_before[il.stack_vars_before.Count - (arg_count - j)];

                    string callconv = attrs.call_conv;
                    if ((constructor.Value.meth != null) && (constructor.Value.meth.CallConvOverride != null))
                        callconv = constructor.Value.meth.CallConvOverride;

                    il.tacs.Add(new timple.TimpleCallNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call), vara.Void(),
                        vara.Label(Mangler2.MangleMethod(constructor.Value, ass), false), var_args, constructor.Value.msig.Method));
                }
                else
                {
                    throw new NotImplementedException();
                }

                ass.Requestor.RequestMethod(constructor.Value);
            }

            for (int i = 0; i < pop_count; i++)
            {
                il.stack_after.Pop();
                il.stack_vars_after.Pop();
            }

            il.stack_after.Push(type_pushes);
            il.stack_vars_after.Push(var_obj);
        }

        public static void tybel_initobj(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            libasm.hardware_location obj_addr = il.stack_vars_after.Pop(ass);

            Signature.Param p_vt = il.stack_after.Pop();
            Signature.BaseOrComplexType bt = null;
            if (p_vt.Type is Signature.ManagedPointer)
                bt = ((Signature.ManagedPointer)p_vt.Type).ElemType;
            else if (p_vt.Type is Signature.UnmanagedPointer)
                bt = ((Signature.UnmanagedPointer)p_vt.Type).BaseType;
            else
                throw new Exception("stack type is not a managed or unmanaged pointer");

            Assembler.TypeToCompile typeTok = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.Param p_T = typeTok.tsig;

            Signature.Param et = new Signature.Param(bt, ass);
            if (Signature.ParamCompare(et, p_T, ass) == false)
                throw new Exception("initobj: token does not match stack type");

            int len = ass.GetSizeOf(et);
            if(!et.Type.IsValueType(ass))
                len = ass.GetSizeOfPointer();
            ass.MemSet(state, il.stack_vars_before, obj_addr, new libasm.const_location { c = 0 }, new libasm.const_location { c = len }, il.il.tybel);
        }

        public static void initobj(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            vara v_addr = il.stack_vars_after.Pop();

            Signature.Param p_vt = il.stack_after.Pop();
            Signature.BaseOrComplexType bt = null;
            if (p_vt.Type is Signature.ManagedPointer)
                bt = ((Signature.ManagedPointer)p_vt.Type).ElemType;
            else if (p_vt.Type is Signature.UnmanagedPointer)
                bt = ((Signature.UnmanagedPointer)p_vt.Type).BaseType;
            else
                throw new Exception("stack type is not a managed or unmanaged pointer");

            if (Signature.ParamCompare(new Signature.Param(bt, ass), new Signature.Param(il.inline_tok, ass), ass) == false)
                throw new Exception("initobj: token does not match stack type");

            if (bt.CliType(ass) == Assembler.CliType.vt)
            {
                Layout l = Layout.GetLayout(Metadata.GetTTC(new Signature.Param(bt, ass), mtc.GetTTC(ass), mtc.msig, ass), ass);
                List<Layout.Field> flat_fields = l.GetFlattenedInstanceFieldLayout(mtc.GetTTC(ass), mtc.msig, ass);

                for (int i = 0; i < flat_fields.Count; i++)
                {
                    Assembler.CliType ct = flat_fields[i].field.fsig.CliType(ass);
                    il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, ct), vara.ContentsOf(v_addr, flat_fields[i].offset, ct), vara.Const(0, ct), vara.Void()));
                }
            }
            else
                il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, bt.CliType(ass)), vara.ContentsOf(v_addr, bt.CliType(ass)), vara.Const(0, bt.CliType(ass)), vara.Void()));
            //foreach(vara v in v_vt.vara_list)
            //    il.tacs.Add(new timple.TimpleNode(new ThreeAddressCode.Op(ThreeAddressCode.OpName.assign, v.DataType), v, vara.Const(0, v.DataType), vara.Void()));
        }

        public static void tybel_localloc(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* Unless overridden in an arch-specific assembler, the default is just to call gcmalloc */
            if (ass.Options.VerifiableCIL)
                throw new Assembler.AssemblerException("localloc: localloc is never verifiable", il.il, mtc);

            Signature.Param p_size = il.stack_after.Pop();
            libasm.hardware_location loc_size = il.stack_vars_after.Pop(ass);

            if (p_size.CliType(ass) != Assembler.CliType.native_int)
            {
                if (p_size.CliType(ass) != Assembler.CliType.int32)
                    throw new Assembler.AssemblerException("localloc: type of size if not int32 or native int", il.il, mtc);
                ass.Conv(state, il.stack_vars_before, loc_size, loc_size, new Signature.BaseType(BaseType_Type.U),
                    new Signature.BaseType(BaseType_Type.U4), true, il.il.tybel);
            }

            Signature.Param p_ret = new Signature.Param(BaseType_Type.I);
            libasm.hardware_location loc_ret = il.stack_vars_after.GetAddressFor(p_ret, ass);

            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("gcmalloc", false), loc_ret,
                new libasm.hardware_location[] { loc_size }, ass.callconv_gcmalloc, il.il.tybel);

            il.stack_after.Push(p_ret);
        }

        public static void tybel_gcmalloc(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            Signature.Param p_size = il.stack_after.Pop();
            libasm.hardware_location loc_size = il.stack_vars_after.Pop(ass);

            if(p_size.CliType(ass) != Assembler.CliType.native_int)
            {
                if(p_size.CliType(ass) != Assembler.CliType.int32)
                    throw new Assembler.AssemblerException("gcmalloc: size must be int32 or native int", il.il, mtc);

                libasm.hardware_location t1 = ass.GetTemporary(state, Assembler.CliType.native_int);
                ass.Conv(state, il.stack_vars_before, t1, loc_size, new Signature.BaseType(BaseType_Type.U),
                    new Signature.BaseType(BaseType_Type.I4), false, il.il.tybel);
                loc_size = t1;
            }

            Assembler.TypeToCompile typeTok = Metadata.GetTTC(il.il.inline_tok, mtc.GetTTC(ass), mtc.msig, ass);
            Signature.Param p_T = typeTok.tsig;

            if (p_T.Type.IsValueType(ass))
            {
                throw new Assembler.AssemblerException("gcmalloc: typeTok must be a reference type (is instead " +
                    p_T.ToString() + ")", il.il, mtc);
            }

            libasm.hardware_location loc_dest = il.stack_vars_after.GetAddressFor(p_T, ass);

            ass.Call(state, il.stack_vars_before, new libasm.hardware_addressoflabel("gcmalloc", false), loc_dest,
                new libasm.hardware_location[] { loc_size }, ass.callconv_gcmalloc, il.il.tybel);

            il.stack_after.Push(p_T);
        }
    }
}
