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
    partial class call
    {
        public static void tybel_call(frontend.cil.CilNode il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_block,
            Encoder.EncoderState state, Assembler.MethodAttributes attrs)
        {
            /* Encode the opcodes call, calli, callvirt, ldftn and ldvirtftn */

            // First, identify the method to call
            Assembler.MethodToCompile call_mtc;
            if (il.il.inline_tok is MTCToken)
                call_mtc = ((MTCToken)il.il.inline_tok).mtc;
            else
                call_mtc = Metadata.GetMTC(new Metadata.TableIndex(il.il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);

            // Determine the type of call
            bool is_callvirt = false;
            bool is_ldvirtftn = false;
            bool is_ldftn = false;
            bool is_calli = false;
            bool is_call = false;
            bool is_virt = false;
            if (il.il.opcode.opcode == 0x6f)
                is_callvirt = true;
            if (il.il.opcode.opcode == 0xfe07)
                is_ldvirtftn = true;
            if (il.il.opcode.opcode == 0x29)
                is_calli = true;
            if (il.il.opcode.opcode == 0x28)
                is_call = true;
            if (is_callvirt || is_ldvirtftn)
                is_virt = true;
            if (il.il.opcode.opcode == 0xfe06)
                is_ldftn = true;

            // If calling an internal call, then emit it as an inline instead if possible
            if (is_call)
            {
                if (enc_intcall(il, ass, mtc, ref next_block, state, attrs))
                    return;
            }

            Signature.Method msigm = null;
            if (call_mtc.msig is Signature.Method)
                msigm = call_mtc.msig as Signature.Method;
            else
                msigm = ((Signature.GenericMethod)call_mtc.msig).GenMethod;
            int arg_count = get_arg_count(call_mtc.msig);


            // Determine the address of the function to call along with whether to adjust the this pointer
            libasm.hardware_location fptr;
            //vara v_fptr = vara.Logical(next_variable++, Assembler.CliType.native_int);
            libasm.hardware_location this_pointer;
            vara v_thisadjust = vara.Const(new IntPtr(0), Assembler.CliType.native_int);
            vara v_this_pointer = vara.Void();

            if (is_virt && call_mtc.meth.IsVirtual)
            {
                // Identify the type on the stack
                Assembler.TypeToCompile cur_ttc = new Assembler.TypeToCompile(il.stack_before[il.stack_before.Count - arg_count], ass);
                Layout l = Layout.GetLayout(cur_ttc, ass);

                // Identify the type that defines the method to call
                Assembler.TypeToCompile call_mtc_ttc = call_mtc.GetTTC(ass);
                if (call_mtc_ttc.type.IsValueType(ass) && !(call_mtc_ttc.tsig.Type is Signature.BoxedType))
                    call_mtc_ttc.tsig.Type = new Signature.BoxedType(call_mtc_ttc.tsig.Type);
                Layout call_mtc_ttc_l = Layout.GetLayout(call_mtc_ttc, ass, false);

                // make the this pointer be a simple logical var
                this_pointer = il.stack_vars_before.GetAddressOf(il.stack_before.Count - arg_count, ass);
                ass.Assign(state, il.stack_vars_before, ass.GetTemporary(), this_pointer, Assembler.CliType.native_int, il.il.tybel);
                this_pointer = ass.GetTemporary();

                // get the vtable for the this_pointer object - v_vtable = [v_this_pointer]
                libasm.hardware_location vtbl = ass.GetTemporary2();
                ass.Assign(state, il.stack_vars_before, vtbl, new libasm.hardware_contentsof { base_loc = this_pointer, size = ass.GetSizeOfPointer() }, Assembler.CliType.native_int, il.il.tybel);

                if (call_mtc_ttc.type.IsInterface)
                {
                    // Perform an interface call
                    fptr = null;

                    /* At this point, temporary1 is being used as the this pointer, and t2 is the vtbl pointer */
                    /* First, make t2 be the itable pointer */
                    libasm.hardware_location t1 = ass.GetTemporary();
                    libasm.hardware_location t2 = ass.GetTemporary2();
                    ass.Assign(state, il.stack_vars_before, t2, new libasm.hardware_contentsof { base_loc = vtbl, const_offset = ass.GetSizeOfPointer(), size = ass.GetSizeOfPointer() }, Assembler.CliType.native_int, il.il.tybel);

                    /* Now load the target interface typeinfo to t1 */
                    ass.Assign(state, il.stack_vars_before, t1, new libasm.hardware_addressoflabel(call_mtc_ttc_l.typeinfo_object_name, true), Assembler.CliType.native_int, il.il.tybel);

                    /* The procedure from here is:
                     * 
                     * start_search:
                     *     cmp [itablepointer], 0
                     *     throw_equal interface_not_found
                     *     cmp [itablepointer], iface_ti
                     *     je iface_found
                     * do_loop:
                     *     add itable_pointer, GetSizeOfIntPtr() * 2
                     *     jmp start_search
                     * iface_found:
                     *     ifacemembers = [itablepointer + GetSizeOfIntPtr()]
                     *     vfptr = [ifacemembers + offset_within_interface]
                     */

                    Layout.Interface iface = Layout.GetInterfaceLayout(call_mtc_ttc, ass);
                    int offset_within_interface = iface.GetVirtualMethod(call_mtc, ass).offset;

                    int start_search = state.next_blk++;
                    int do_loop = state.next_blk++;
                    int iface_found = state.next_blk++;
                    string l_start_search = "L" + start_search.ToString();
                    string l_do_loop = "L" + do_loop.ToString();
                    string l_iface_found = "L" + iface_found.ToString();

                    il.il.tybel.Add(new tybel.LabelNode(l_start_search, true));
                    ass.ThrowIf(state, il.stack_vars_before, new libasm.hardware_contentsof { base_loc = t2, size = ass.GetSizeOfPointer() },
                        new libasm.const_location { c = 0 },
                        new libasm.hardware_addressoflabel("sthrow", false),
                        new libasm.const_location { c = Assembler.throw_MissingMethodException },
                        Assembler.CliType.native_int, ThreeAddressCode.OpName.throweq, il.il.tybel);
                    ass.BrIf(state, il.stack_vars_before, new libasm.hardware_addressoflabel(l_iface_found, false),
                        new libasm.hardware_contentsof { base_loc = t2, size = ass.GetSizeOfPointer() },
                        t1, ThreeAddressCode.OpName.beq, Assembler.CliType.native_int, il.il.tybel);
                    il.il.tybel.Add(new tybel.LabelNode(l_do_loop, true));
                    ass.Add(state, il.stack_vars_before, t2, t2, new libasm.const_location { c = ass.GetSizeOfPointer() * 2 }, Assembler.CliType.native_int, il.il.tybel);
                    ass.Br(state, il.stack_vars_before, new libasm.hardware_addressoflabel(l_start_search, false), il.il.tybel);
                    il.il.tybel.Add(new tybel.LabelNode(l_iface_found, true));
                    ass.Assign(state, il.stack_vars_before, t2, 
                        new libasm.hardware_contentsof { base_loc = t2, const_offset = ass.GetSizeOfPointer(), size = ass.GetSizeOfPointer() },
                        Assembler.CliType.native_int, il.il.tybel);
                    fptr = t1;
                    ass.Assign(state, il.stack_vars_before, fptr, 
                        new libasm.hardware_contentsof { base_loc = t2, const_offset = offset_within_interface, size = ass.GetSizeOfPointer() }, 
                        Assembler.CliType.native_int, il.il.tybel);
                }
                else
                {
                    // Perform a virtual call

                    // Get the position of the virtual method in the vtable
                    Layout.Method m = call_mtc_ttc_l.GetVirtualMethod(call_mtc);
                    int vmeth_offset = m.offset;

                    // At this point, fptr = [v_vtable + vmeth_offset]
                    fptr = ass.GetTemporary();
                    ass.Assign(state, il.stack_vars_before, fptr, new libasm.hardware_contentsof { base_loc = vtbl, size = ass.GetSizeOfPointer(), const_offset = vmeth_offset }, Assembler.CliType.native_int, il.il.tybel);
                }
            }
            else if (is_calli)
            {
                // If its a call_i, then the last stack position will be of type virtftnptr and we extract the pointer from that

                if (!((il.stack_before[il.stack_before.Count - 1].Type is Signature.BaseType) && (((Signature.BaseType)il.stack_before[il.stack_before.Count - 1].Type).Type == BaseType_Type.VirtFtnPtr)))
                    throw new Exception("calli used without valid virtftnptr on stack");

                libasm.hardware_location virtftnptr = il.stack_vars_before.GetAddressOf(il.stack_before.Count - 1, ass);
                fptr = ass.GetTemporary();
                ass.Assign(state, il.stack_vars_before, fptr, virtftnptr, Assembler.CliType.native_int, il.il.tybel);

                // The arguments are one place further back for a calli instruction
                this_pointer = il.stack_vars_before.GetAddressOf(il.stack_before.Count - arg_count - 1, ass);
            }
            else
            {
                // Its either a simple call or ldftn.  Load the function pointer
                string func_name;

                // Does the method to call have a methodalias?
                if (call_mtc.meth.ReferenceAlias != null)
                    func_name = call_mtc.meth.ReferenceAlias;
                else
                    func_name = Mangler2.MangleMethod(call_mtc, ass);

                fptr = ass.GetTemporary();
                ass.Assign(state, il.stack_vars_before, fptr, new libasm.hardware_addressoflabel(func_name, false), Assembler.CliType.native_int, il.il.tybel); 

                // Request the method to be compiled
                ass.Requestor.RequestMethod(call_mtc);

                // If its a static method we need to initialize it
                /*if (call_mtc.meth.IsStatic && !call_mtc.type.IsBeforeFieldInit)
                {
                    if (!i.cfg_node.types_whose_static_fields_are_referenced.Contains(call_mtc.GetTTC(this)))
                        i.cfg_node.types_whose_static_fields_are_referenced.Add(call_mtc.GetTTC(this));
                }*/
                // TODO
            }

            // If this is a ldftn/ldvirtftn, then create a virtftnptr and return it
            if (is_ldftn || is_ldvirtftn)
            {
                if (is_ldvirtftn)
                {
                    il.stack_after.Pop();
                    il.stack_vars_after.Pop(ass);
                }

                libasm.hardware_location virtftnptr = il.stack_vars_after.GetAddressFor(new Signature.Param(Assembler.CliType.virtftnptr), ass);
                ass.Assign(state, il.stack_vars_before, virtftnptr, fptr, Assembler.CliType.native_int, il.il.tybel);
                il.stack_after.Push(new Signature.Param(Assembler.CliType.virtftnptr));

                return;
            }

            // Determine the arguments to push
            bool is_vt_call = false;
            CallConv cc;
            libasm.hardware_location[] var_args = new libasm.hardware_location[arg_count];
            for (int j = 0; j < arg_count; j++)
                var_args[j] = il.stack_vars_before.GetAddressOf(il.stack_before.Count - (arg_count - j) + (is_calli ? -1 : 0), ass);

            // Get the calling convention
            ThreeAddressCode.Op call_tac;
            if (Assembler.GetLdObjTac(msigm.RetType.CliType(ass)).Type == Assembler.CliType.vt)
            {
                is_vt_call = true;
                call_tac = ThreeAddressCode.Op.OpVT(ThreeAddressCode.OpName.call);
            }
            else
            {
                is_vt_call = false;
                call_tac = Assembler.GetCallTac(msigm.RetType.CliType(ass));
            }
            int vt_size = 0;
            if (is_vt_call)
                vt_size = ass.GetSizeOf(msigm.RetType);
            string call_conv = ass.Options.CallingConvention;
            if ((call_mtc.meth != null) && (call_mtc.meth.CallConvOverride != null))
                call_conv = call_mtc.meth.CallConvOverride;
            cc = ass.call_convs[call_conv](call_mtc, CallConv.StackPOV.Caller, ass, new ThreeAddressCode(call_tac, vt_size));

            /* Sort out the stack */
            int pop_count = arg_count;
            if (is_calli)
                pop_count++;
            for (int i = 0; i < pop_count; i++)
            {
                il.stack_after.Pop();
                il.stack_vars_after.Pop(ass);
            }

            // Make the actual call
            libasm.hardware_location v_ret = (msigm.RetType.CliType(ass) == Assembler.CliType.void_) ? null : il.stack_vars_after.GetAddressFor(msigm.RetType, ass);

            ass.Call(state, il.stack_vars_before, fptr, v_ret, var_args, cc, il.il.tybel);

            if (msigm.RetType.CliType(ass) != Assembler.CliType.void_)
            {
                il.stack_after.Push(msigm.RetType);
            }
        }

        public static void enc_call(InstructionLine il, Assembler ass, Assembler.MethodToCompile mtc, ref int next_variable,
            ref int next_block, List<vara> la_vars, List<vara> lv_vars, List<Signature.Param> las, List<Signature.Param> lvs,
            Assembler.MethodAttributes attrs)
        {
            /* Encode the opcodes call, calli, callvirt, ldftn and ldvirtftn */

            // First, identify the method to call
            Assembler.MethodToCompile call_mtc;
            if (il.inline_tok is MTCToken)
                call_mtc = ((MTCToken)il.inline_tok).mtc;
            else
                call_mtc = Metadata.GetMTC(new Metadata.TableIndex(il.inline_tok), mtc.GetTTC(ass), mtc.msig, ass);

            // Determine the type of call
            bool is_callvirt = false;
            bool is_ldvirtftn = false;
            bool is_ldftn = false;
            bool is_calli = false;
            bool is_call = false;
            bool is_virt = false;
            if (il.opcode.opcode == 0x6f)
                is_callvirt = true;
            if (il.opcode.opcode == 0xfe07)
                is_ldvirtftn = true;
            if (il.opcode.opcode == 0x29)
                is_calli = true;
            if (il.opcode.opcode == 0x28)
                is_call = true;
            if (is_callvirt || is_ldvirtftn)
                is_virt = true;
            if (il.opcode.opcode == 0xfe06)
                is_ldftn = true;

            // If calling an internal call, then emit it as an inline instead if possible
            if (is_call)
            {
                //if (enc_intcall(il, ass, mtc, ref next_variable, ref next_block, la_vars, lv_vars, las, lvs, attrs))
                //    return;
            }

            Signature.Method msigm = null;
            if (call_mtc.msig is Signature.Method)
                msigm = call_mtc.msig as Signature.Method;
            else
                msigm = ((Signature.GenericMethod)call_mtc.msig).GenMethod;
            int arg_count = get_arg_count(call_mtc.msig);


            // Determine the address of the function to call along with whether to adjust the this pointer
            vara v_fptr = vara.Logical(next_variable++, Assembler.CliType.native_int);
            vara v_thisadjust = vara.Const(new IntPtr(0), Assembler.CliType.native_int);
            vara v_this_pointer = vara.Void();

            if (is_virt && call_mtc.meth.IsVirtual)
            {
                // Identify the type on the stack
                Assembler.TypeToCompile cur_ttc = new Assembler.TypeToCompile(il.stack_before[il.stack_before.Count - arg_count], ass);
                Layout l = Layout.GetLayout(cur_ttc, ass);

                // Identify the type that defines the method to call
                Assembler.TypeToCompile call_mtc_ttc = call_mtc.GetTTC(ass);
                if (call_mtc_ttc.type.IsValueType(ass) && !(call_mtc_ttc.tsig.Type is Signature.BoxedType))
                    call_mtc_ttc.tsig.Type = new Signature.BoxedType(call_mtc_ttc.tsig.Type);
                Layout call_mtc_ttc_l = Layout.GetLayout(call_mtc_ttc, ass, false);

                // make the this pointer be a simple logical var
                v_this_pointer = il.stack_vars_before[il.stack_vars_before.Count - arg_count];
                if (v_this_pointer.VarType != vara.vara_type.Logical)
                {
                    vara new_this_pointer = vara.Logical(next_variable++, Assembler.CliType.native_int);
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), new_this_pointer, v_this_pointer, vara.Void()));
                    v_this_pointer = new_this_pointer;
                }

                // get the vtable for the this_pointer object - v_vtable = [v_this_pointer]
                vara v_vtable = vara.Logical(next_variable++, Assembler.CliType.native_int);
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_vtable, vara.ContentsOf(v_this_pointer.LogicalVar, Assembler.CliType.native_int), vara.Void()));

                if (call_mtc_ttc.type.IsInterface)
                {
                    // Perform an interface call

                    /* Find the interface typeinfo */
                    vara iface_ti_obj = vara.Logical(next_variable++, Assembler.CliType.native_int);
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), iface_ti_obj, vara.Label(call_mtc_ttc_l.typeinfo_object_name, true), vara.Void()));
                    ass.Requestor.RequestTypeInfo(call_mtc_ttc);

                    /* Find the itablepointer (2nd entry in vtable) */
                    vara itableptr_obj = vara.Logical(next_variable++, Assembler.CliType.native_int);
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), itableptr_obj, vara.ContentsOf(v_vtable.LogicalVar, ass.GetSizeOfIntPtr(), Assembler.CliType.native_int), vara.Void()));

                    /* The procedure from here is:
                     * 
                     * start_search:
                     *     cmp [itablepointer], 0
                     *     throw_equal interface_not_found
                     *     cmp [itablepointer], iface_ti
                     *     je iface_found
                     * do_loop:
                     *     add itable_pointer, GetSizeOfIntPtr() * 2
                     *     jmp start_search
                     * iface_found:
                     *     ifacemembers = [itablepointer + GetSizeOfIntPtr()]
                     *     vfptr = [ifacemembers + offset_within_interface]
                     */

                    timple.TimpleLabelNode start_search = new timple.TimpleLabelNode(next_block++);
                    timple.TimpleLabelNode do_loop = new timple.TimpleLabelNode(next_block++);
                    timple.TimpleLabelNode iface_found = new timple.TimpleLabelNode(next_block++);

                    vara ifacemembers = vara.Logical(next_variable++, Assembler.CliType.native_int);
                    Layout.Interface iface = Layout.GetInterfaceLayout(call_mtc_ttc, ass);
                    int offset_within_interface = iface.GetVirtualMethod(call_mtc, ass).offset;

                    il.tacs.Add(start_search);
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.cmp), vara.Void(), vara.ContentsOf(itableptr_obj, Assembler.CliType.native_int), vara.Const(0, Assembler.CliType.native_int)));
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.throweq), vara.Void(), vara.Const(Assembler.throw_MissingMethodException, Assembler.CliType.int32), vara.Void()));
                    il.tacs.Add(new timple.TimpleBrNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.beq), iface_found, do_loop, vara.ContentsOf(itableptr_obj, Assembler.CliType.native_int), iface_ti_obj));
                    il.tacs.Add(do_loop);
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), itableptr_obj, itableptr_obj, vara.Const(ass.GetSizeOfIntPtr() * 2, Assembler.CliType.native_int)));
                    il.tacs.Add(new timple.TimpleBrNode(start_search));
                    il.tacs.Add(iface_found);
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), ifacemembers, vara.ContentsOf(itableptr_obj, ass.GetSizeOfIntPtr(), Assembler.CliType.native_int), vara.Void()));
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_fptr, vara.ContentsOf(ifacemembers, offset_within_interface, Assembler.CliType.native_int), vara.Void()));
                }
                else
                {
                    // Perform a virtual call

                    // Get the position of the virtual method in the vtable
                    Layout.Method m = call_mtc_ttc_l.GetVirtualMethod(call_mtc);
                    int vmeth_offset = m.offset;

                    // At this point, fptr = [v_vtable + vmeth_offset]
                    il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_fptr, vara.ContentsOf(v_vtable, vmeth_offset, Assembler.CliType.native_int), vara.Void()));
                }
            }
            else if (is_calli)
            {
                // If its a call_i, then the last stack position will be of type virtftnptr and we extract the pointer from that

                if (!((il.stack_before[il.stack_before.Count - 1].Type is Signature.BaseType) && (((Signature.BaseType)il.stack_before[il.stack_before.Count - 1].Type).Type == BaseType_Type.VirtFtnPtr)))
                    throw new Exception("calli used without valid virtftnptr on stack");

                vara v_virtftnptr = il.stack_vars_before[il.stack_before.Count - 1];
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign_from_virtftnptr_ptr), v_fptr, v_virtftnptr, vara.Void()));

                // The arguments are one place further back for a calli instruction
                v_this_pointer = il.stack_vars_before[il.stack_before.Count - arg_count - 1];
            }
            else
            {
                // Its either a simple call or ldftn.  Load the function pointer
                string func_name;

                // Does the method to call have a methodalias?
                if (call_mtc.meth.ReferenceAlias != null)
                    func_name = call_mtc.meth.ReferenceAlias;
                else
                    func_name = Mangler2.MangleMethod(call_mtc, ass);
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_fptr, vara.Label(func_name, false), vara.Void()));

                // Request the method to be compiled
                ass.Requestor.RequestMethod(call_mtc);

                // If its a static method we need to initialize it
                /*if (call_mtc.meth.IsStatic && !call_mtc.type.IsBeforeFieldInit)
                {
                    if (!i.cfg_node.types_whose_static_fields_are_referenced.Contains(call_mtc.GetTTC(this)))
                        i.cfg_node.types_whose_static_fields_are_referenced.Add(call_mtc.GetTTC(this));
                }*/
                // TODO
            }

            // If this is a ldftn/ldvirtftn, then create a virtftnptr and return it
            if (is_ldftn || is_ldvirtftn)
            {
                vara v_virtftnptr = vara.Logical(next_variable++, Assembler.CliType.native_int);
                il.tacs.Add(new timple.TimpleNode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign_to_virtftnptr), v_virtftnptr, v_fptr, v_thisadjust));

                //i.pushes = new Signature.Param(BaseType_Type.VirtFtnPtr);
                //i.pushes_variable = v_virtftnptr;
                // TODO
                return;
            }

            // Determine the arguments to push
            bool is_vt_call = false;
            CallConv cc;
            vara[] var_args = null;
            var_args = new vara[arg_count];
            for (int j = 0; j < arg_count; j++)
            {
                vara v = il.stack_vars_before[il.stack_vars_before.Count - (arg_count - j) + (is_calli ? -1 : 0)];
                /* if ((v.is_address_of_vt) && !((j == 0) && (call_mtc.msig.Method.HasThis || call_mtc.msig.Method.ExplicitThis)))
                    var_args[j] = var.ContentsOf(v, 0, v.v_size);
                else */ // TODO
                    var_args[j] = v;
            }

            // Get the calling convention
            ThreeAddressCode.Op call_tac;
            if (Assembler.GetLdObjTac(msigm.RetType.CliType(ass)).Type == Assembler.CliType.vt)
            {
                is_vt_call = true;
                call_tac = ThreeAddressCode.Op.OpVT(ThreeAddressCode.OpName.call);
            }
            else
            {
                is_vt_call = false;
                call_tac = Assembler.GetCallTac(msigm.RetType.CliType(ass));
            }
            int vt_size = 0;
            if (is_vt_call)
                vt_size = ass.GetSizeOf(msigm.RetType);
            string call_conv = ass.Options.CallingConvention;
            if ((call_mtc.meth != null) && (call_mtc.meth.CallConvOverride != null))
                call_conv = call_mtc.meth.CallConvOverride;
            cc = ass.call_convs[call_conv](call_mtc, CallConv.StackPOV.Caller, ass, new ThreeAddressCode(call_tac, vt_size));

            // Make the actual call
            vara v_ret = (msigm.RetType.CliType(ass) == Assembler.CliType.void_) ? vara.Void() : vara.Logical(next_variable++, msigm.RetType.CliType(ass));

            exceptions.eh_store_lvs_las(mtc, il, lv_vars, la_vars);
            il.tacs.Add(new timple.TimpleCallNode(call_tac, v_ret, v_fptr, var_args, call_mtc.msig.Method, attrs.call_conv));


            /* Sort out the stack */
            int pop_count = arg_count;
            if (is_calli)
                pop_count++;
            for (int i = 0; i < pop_count; i++)
            {
                il.stack_after.Pop();
                il.stack_vars_after.Pop();
            }

            if (msigm.RetType.CliType(ass) != Assembler.CliType.void_)
            {
                il.stack_after.Push(msigm.RetType);
                il.stack_vars_after.Push(v_ret);
            }
        }

        private static int get_arg_count(Signature.BaseMethod m)
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
