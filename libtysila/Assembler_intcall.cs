/* Copyright (C) 2008 - 2012 by John Cronin
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
        protected virtual bool Arch_enc_intcall(string mangled_name, InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state, bool provides, ref bool i_pushes_set)
        {
            return false;
        }

        private bool provides_intcall(MethodToCompile mtc)
        {
            InstructionLine instr = new InstructionLine();
            instr.stack_before = new List<PseudoStack>();
            for (int i = 0; i < get_arg_count(mtc.msig); i++)
                instr.stack_before.Add(new PseudoStack { contains_variable = var.Null });
            return enc_intcall(instr, mtc.meth, mtc.msig, mtc.type, mtc.tsigp, new AssemblerState(), true);
        }

        private bool enc_intcall(InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state)
        { return enc_intcall(i, mdr, msig, tdr, tsig, state, false); }
        private bool enc_intcall(InstructionLine i, Metadata.MethodDefRow mdr, Signature.BaseMethod msig, Metadata.TypeDefRow tdr, Signature.Param tsig, AssemblerState state, bool provides)
        {
            string methname;
            bool handled = true;
            bool i_pushes_set = false;

            Assembler.TypeToCompile ttc = new TypeToCompile { _ass = this, tsig = tsig, type = tdr };

            methname = Mangler2.MangleMethod(new Assembler.MethodToCompile { type = tdr, tsigp = tsig, meth = mdr, msig = msig, _ass = this }, this);

            if (methname == "_Zu1OM_0_19InternalGetHashCode_Ri_P1u1O")
            {
                var obj_var = i.stack_before[i.stack_before.Count - 1].contains_variable;
                if (obj_var.type != var.var_type.LogicalVar)
                {
                    i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), state.next_variable++, obj_var, var.Null));
                    obj_var = state.next_variable - 1;
                }
                var obj_id_var = state.next_variable++;

                int fld_offset = Layout.GetLayout(ttc, this, false).GetField("Int32 __object_id", false).offset;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), obj_id_var, var.ContentsOf(obj_var, fld_offset), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), state.next_variable++, obj_id_var,
                    var.Const((UInt32)0x1E3779B1)));
            }
            else if (methname == "_Zu1OM_0_15MemberwiseClone_Ru1O_P1u1t")
            {
                /* Returns a direct copy of the current object
                 * 
                 * First allocate a new memory block of the correct size
                 * If the object is not a string type, then get the class size from the TypeInfo structure
                 * If it is a string type: load its length, multiply by sizeof(char), add String.data_offset
                 * 
                 * Then set the object_id to something new
                 */

                var v_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_str_ti = state.next_variable++;
                var v_obj_vt = state.next_variable++;
                var v_obj_ti = state.next_variable++;
                var v_length = state.next_variable++;
                var v_newobj = state.next_variable++;
                var v_objid = state.next_variable++;

                int blk_notstring = state.next_block++;
                int blk_lengthfound = state.next_block++;

                TypeToCompile str_ttc = Metadata.GetTTC(new Signature.Param(BaseType_Type.String), new TypeToCompile { _ass = this, tsig = tsig, type = tdr }, null, this);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_obj_vt, var.ContentsOf(v_obj, 0), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_obj_ti, var.ContentsOf(v_obj_vt, 0), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_str_ti, var.AddrOfObject(Mangler2.MangleTypeInfo(str_ttc, this)), var.Null));
                Requestor.RequestTypeInfo(str_ttc);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.cmp), var.Null, v_obj_vt, v_str_ti));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.bne), blk_notstring));

                /* It is a string */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_length, var.ContentsOf(v_obj, GetStringFieldOffset(StringFields.length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_length, v_length, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add), v_length, v_length, var.Const(GetStringFieldOffset(StringFields.data_offset))));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpNull(ThreeAddressCode.OpName.br), blk_lengthfound));

                /* Its not a string */
                i.tacs.Add(LabelEx.LocalLabel(blk_notstring));
                GetTysosTypeLayout();
                var v_globallength = v_length;
                v_globallength.is_global = true;        // this prevents the assembler from marking the variable dead and stops it removing the above code which
                // also assigns to v_length
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_globallength, var.ContentsOf(v_obj_ti, tysos_type_offsets["Int32 ClassSize"]), var.Null));

                /* Get the memory and do the memcpy */
                i.tacs.Add(LabelEx.LocalLabel(blk_lengthfound));
                i.tacs.Add(new CallEx(v_newobj, new var[] { v_length }, "gcmalloc", callconv_gcmalloc));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_newobj, v_obj, v_length }, "__memcpy", callconv_memcpy));

                /* Store the new object id */
                i.tacs.Add(new CallEx(v_objid, new var[] { }, "__get_new_obj_id", callconv_getobjid));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), var.ContentsOf(v_newobj, GetStringFieldOffset(StringFields.objid)), v_objid, var.Null));

                i.pushes_variable = v_newobj;
                i_pushes_set = true;
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU1_Rh_P1u1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u1), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU2_Rt_P1u1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u2), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU4_Rj_P1u1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u4), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU8_Ry_P1u1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.peek_u8), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU1_Rh_P1y")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u1), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU2_Rt_P1y")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u2), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU4_Rj_P1y")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.peek_u4), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_6PeekU8_Ry_P1y")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.peek_u8), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2yh")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u1), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2yt")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u2), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2yj")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u4), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2yy")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.poke_u8), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Uh")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u1), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Ut")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u2), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Uj")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.poke_u4), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2u1Uy")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.poke_u8), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_4Poke_Rv_P2yu1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.poke_u), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX15OtherOperationsM_0_3Add_Ru1I_P2u1Iu1I")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));                    
            }
            else if (methname == "_ZX15OtherOperationsM_0_3Add_Ru1U_P2u1Uu1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX15OtherOperationsM_0_3Mul_Ru1I_P2u1Iu1I")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.mul), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX15OtherOperationsM_0_3Mul_Ru1U_P2u1Uu1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.mul_un), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX15OtherOperationsM_0_3Sub_Ru1I_P2u1Iu1I")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX15OtherOperationsM_0_3Sub_Ru1U_P2u1Uu1U")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.sub), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX12IoOperationsM_0_7PortOut_Rv_P2th")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.portout_u2_u1), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX12IoOperationsM_0_7PortOut_Rv_P2tt")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.portout_u2_u2), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX12IoOperationsM_0_7PortOut_Rv_P2tj")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.portout_u2_u4), 0,
                    i.stack_before[i.stack_before.Count - 2].contains_variable,
                    i.stack_before[i.stack_before.Count - 1].contains_variable));
            }
            else if (methname == "_ZX12IoOperationsM_0_7PortInb_Rh_P1t")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.portin_u2_u1), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            }
            else if (methname == "_ZX12IoOperationsM_0_7PortInw_Rt_P1t")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.portin_u2_u2), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            }
            else if (methname == "_ZX12IoOperationsM_0_7PortInd_Rj_P1t")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.portin_u2_u4), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            }
            else if (methname == "_ZX16MemoryOperationsM_0_16GetInternalArray_RPv_P1W6System5Array")
            {
                /* static void *GetInternalArray(System.Array array) */

                var v_ret = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_ret, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_ret, var.ContentsOf(v_ret, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));
            }
            else if (mdr.IsInternalCall && mdr.Name.StartsWith("ReinterpretAs"))
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), state.next_variable++,
                    i.stack_before[i.stack_before.Count - 1].contains_variable, 0));
            }
            else if (methname.StartsWith("_ZX14CastOperationsM_0_9GetArg"))
            {
                char arg_c = methname["_ZX14CastOperationsM_0_9GetArg".Length];
                var la = var.LocalArg(Int32.Parse(new String(new char[] { arg_c })));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), state.next_variable++, la, var.Null));
            }
            else if (methname == "_ZX15OtherOperationsM_0_5CallI_Rv_P1u1U")
            {
                i.tacs.Add(new CallEx(var.Null, new var[] { }, i.stack_before[i.stack_before.Count - 1].contains_variable, MakeStaticCall(Options.CallingConvention, new Signature.Param(BaseType_Type.Void), new List<Signature.Param>(), ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call))));
            }
            else if (methname == "_ZX15OtherOperationsM_0_5CallI_Rv_P1y")
            {
                i.tacs.Add(new CallEx(var.Null, new var[] { }, i.stack_before[i.stack_before.Count - 1].contains_variable, MakeStaticCall(Options.CallingConvention, new Signature.Param(BaseType_Type.Void), new List<Signature.Param>(), ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.call))));
            }
            else if (methname == "_ZW6System5ArrayM_0_9GetLength_Ri_P2u1ti")
            {
                /* For this and the next two functions on System.Array, we need to create a super class e.g. int[]
                 * that defines the fields we access (lobounds/sizes/rank/elem_size/inner_array) and then get the field offsets from it
                 * This is safe because all ComplexArrays define these fields in the same location, and System.Array
                 * is marked abstract and so it must be extended by a ComplexArray.
                 * 
                 * This is accomplished in Assembler_Array.cs
                 * 
                 * TODO: can we prove that the only thing it is extended by is a ComplexArray???
                 */

                var v_sizes = state.next_variable++;
                var v_rank = state.next_variable++;
                var v_obj = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_dimension = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_offset = state.next_variable++;
                var v_offset_i = state.next_variable++;

                enc_checknullref(i, v_obj);

                // Range check
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_rank, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.rank)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.cmp), var.Null, v_dimension, v_rank));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.throwge_un), var.Null, var.Const(throw_IndexOutOfRangeException), var.Null));

                // Load the correct element within the sizes array (== array_start + dimension * packedsizeof(I4))
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_sizes, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.sizes)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_offset, v_dimension, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_offset, v_offset, var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.I4)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_isx), v_offset_i, v_offset, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), v_sizes, v_sizes, v_offset));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), state.next_variable++, var.ContentsOf(v_sizes), var.Null));
            }
            else if (methname == "_ZW6System5ArrayM_0_7GetRank_Ri_P1u1t")
            {
                var v_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;

                enc_checknullref(i, v_obj);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), state.next_variable++, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.rank)), var.Null));
            }
            else if (methname == "_ZW6System5ArrayM_0_13GetLowerBound_Ri_P2u1ti")
            {
                var v_lobounds = state.next_variable++;
                var v_rank = state.next_variable++;
                var v_obj = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_dimension = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_offset = state.next_variable++;
                var v_offset_i = state.next_variable++;

                enc_checknullref(i, v_obj);

                // Range check
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_rank, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.rank)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.cmp), var.Null, v_dimension, v_rank));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.throwge_un), var.Null, var.Const(throw_IndexOutOfRangeException), var.Null));

                // Load the correct element within the sizes array (== dimension * packedsizeof(I4))
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_lobounds, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.lobounds)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_offset, v_dimension, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_offset, v_offset, var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.I4)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_isx), v_offset_i, v_offset, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), v_lobounds, v_lobounds, v_offset));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), state.next_variable++, var.ContentsOf(v_lobounds), var.Null));
            }
            else if (methname == "_ZW6System5ArrayM_0_12GetValueImpl_Ru1O_P2u1ti")
            {
                /* instance object System.Array.GetValueImpl(this, int pos)
                 * 
                 * return as a boxed object the value at position pos
                 * we rely on the virtual method GetValueImpl defined on the concrete array type for this
                 */

                var v_obj = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_pos = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_vtbl = state.next_variable++;
                var v_ret = state.next_variable++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_vtbl, var.ContentsOf(v_obj), var.Null));
                int vtbl_offset = GetArrayFieldOffset(ArrayFields.getvalueimpl_vtbl_offset);
                i.tacs.Add(new CallEx(v_ret, new var[] { v_obj, v_pos }, var.ContentsOf(v_vtbl, vtbl_offset), callconv_getvalueimpl));

                i.pushes_variable = v_ret;
                i_pushes_set = true;
            }
            else if (methname == "_ZW6System5ArrayM_0_12SetValueImpl_Rv_P3u1tu1Oi")
                return false;
            else if (methname == "_ZW6System5ArrayM_0_8GetValue_Ru1O_P2u1tu1Zi")
                return false;
            else if (methname == "_ZW6System5ArrayM_0_8SetValue_Rv_P3u1tu1Ou1Zi")
                return false;
            else if (methname == "_ZW6System5ArrayM_0_8FastCopy_Rb_P5V5ArrayiV5Arrayii")
            {
                /* static bool System.Array.FastCopy(Array sourceArray, int sourceIndex, Array destinationArray,
                 *                                      int destinationIndex, int length)
                 *                                      
                 * perform a copy between source and dest
                 * return false if this cannot be done quickly (e.g. the underlying types do not match)
                 * if sourceArray == destinationArray perform a memmove (safely move overlapping arrays) if memcpy
                 * would destroy data (sourceIndex < destIndex)
                 * 
                 * code is:
                 * 
                 * nullrefchecks
                 * 
                 * compare elemtypes
                 * retval = false
                 * jne .end
                 * 
                 * retval = true
                 * cmp length, 0
                 * je .end
                 * 
                 * check sourceIndex + length <= sourceArray.__inner_array_bytelength
                 * check destinationIndex + length <= destinationArray.__inner_array_bytelength
                 * 
                 * calculate start offsets in both internal arrays
                 * calculate byte length
                 * 
                 * cmp sourceArray, destArray
                 * jne .memcpy
                 * cmp sourceIndex, destIndex
                 * ja .memcpy
                 * je .end      // if ((sourceArray == destArray) && (sourceIndex == destIndex)) do nothing
                 * call(__memmove)
                 * jmp .end
                 * .memcpy
                 * call(__memcpy)
                 * .end
                 */

                var v_length = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_destIndex = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_destArray = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_srcIndex = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_srcArray = i.stack_before[i.stack_before.Count - 5].contains_variable;
                var v_retval = state.next_variable++;
                int blk_end = state.next_block++;
                int blk_memcpy = state.next_block++;

                enc_checknullref(i, v_srcArray);
                enc_checknullref(i, v_destArray);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_retval, var.Const(0), var.Null));

                var v_srcET = state.next_variable++;
                var v_destET = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_srcET, var.ContentsOf(v_srcArray, GetArrayFieldOffset(ArrayFields.elemtype)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.assign), v_destET, var.ContentsOf(v_destArray, GetArrayFieldOffset(ArrayFields.elemtype)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.cmp), var.Null, v_srcET, v_destET));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.bne), blk_end));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_retval, var.Const(-1), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.cmp), var.Null, v_length, var.Const(0)));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.beq), blk_end));

                var v_srcEnd = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add), v_srcEnd, v_srcIndex, v_length));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.cmp), var.Null, v_srcEnd, var.ContentsOf(v_srcArray, GetArrayFieldOffset(ArrayFields.inner_array_length), 4)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.throwg_un), var.Null, var.Const(throw_IndexOutOfRangeException), var.Null));
                var v_destEnd = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.add), v_destEnd, v_destIndex, v_length));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.cmp), var.Null, v_destEnd, var.ContentsOf(v_destArray, GetArrayFieldOffset(ArrayFields.inner_array_length), 4)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.throwg_un), var.Null, var.Const(throw_IndexOutOfRangeException), var.Null));

                var v_srcIntIndex = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_srcIntIndex, v_srcIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_srcIntIndex, v_srcIntIndex, var.ContentsOf(v_srcArray, GetArrayFieldOffset(ArrayFields.elem_size), 4)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_isx), v_srcIntIndex, v_srcIntIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), v_srcIntIndex, v_srcIntIndex, var.ContentsOf(v_srcArray, GetArrayFieldOffset(ArrayFields.inner_array))));
                var v_destIntIndex = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_destIntIndex, v_destIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_destIntIndex, v_destIntIndex, var.ContentsOf(v_destArray, GetArrayFieldOffset(ArrayFields.elem_size), 4)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.conv_i4_isx), v_destIntIndex, v_destIntIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.add), v_destIntIndex, v_destIntIndex, var.ContentsOf(v_destArray, GetArrayFieldOffset(ArrayFields.inner_array))));
                var v_bytelength = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign), v_bytelength, v_length, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.mul), v_bytelength, v_bytelength, var.ContentsOf(v_srcArray, GetArrayFieldOffset(ArrayFields.elem_size), 4)));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI(ThreeAddressCode.OpName.cmp), var.Null, v_srcArray, v_destArray));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.bne), blk_memcpy));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.cmp), var.Null, v_srcIndex, v_destIndex));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.ba), blk_memcpy));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.beq), blk_end));

                i.tacs.Add(new CallEx(var.Null, new var[] { v_destIntIndex, v_srcIntIndex, v_bytelength }, "__memmove", callconv_memmove));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.br), blk_end));

                i.tacs.Add(LabelEx.LocalLabel(blk_memcpy));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_destIntIndex, v_srcIntIndex, v_bytelength }, "__memcpy", callconv_memcpy));

                i.tacs.Add(LabelEx.LocalLabel(blk_end));
                i.pushes_variable = v_retval;
                i_pushes_set = true;
            }
            else if (methname == "_ZW6System5ArrayM_0_18CreateInstanceImpl_RV5Array_P3V4Typeu1Ziu1Zi")
                return false;
            else if (methname == "_ZW6System5ArrayM_0_13ClearInternal_Rv_P3V5Arrayii")
            {
                /* static void System.Array.ClearInternal(Array arr, int index, int length)
                 * 
                 * Clear 'length' elements in the flattened array arr starting at index
                 * Clear to zero, false or null depending on type
                 * 
                 * Bounds checking is performed by System.Array.Clear
                 * 
                 * v_start_addr = inner_array + index * elemsize
                 * v_bytelength = length * elemsize
                 */

                var v_arr = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_index = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_length = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_start_addr = state.next_variable++;
                var v_bytelength = state.next_variable++;

                enc_checknullref(i, v_arr);

                // v_start_addr = inner_array + index * elemsize
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_start_addr, v_index, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_start_addr, v_start_addr, var.ContentsOf(v_arr, GetArrayFieldOffset(ArrayFields.elem_size))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_start_addr, v_start_addr, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_start_addr, v_start_addr, var.ContentsOf(v_arr, GetArrayFieldOffset(ArrayFields.inner_array))));

                // v_bytelength = length * elemsize
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_bytelength, v_length, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_bytelength, v_bytelength, var.ContentsOf(v_arr, GetArrayFieldOffset(ArrayFields.elem_size))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_bytelength, v_bytelength, var.Null));

                // do the clear
                i.tacs.Add(new CallEx(var.Null, new var[] { v_start_addr, var.Const(0), v_bytelength }, "__memset", callconv_memset));
            }
            else if (methname == "_ZW6System5ArrayM_0_5Clone_Ru1O_P1u1t")
            {
                /* instance object System.Array.Clone()
                 * 
                 * Create a shallow copy of the array
                 * 
                 * We copy the array object and the inner array
                 * Set the object id and inner array members of the array object respectively
                 *
                 * As nothing updates the sizes and lobounds members, we can just re-use them
                 */

                var orig_arr_var_id = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var new_obj_id = state.next_variable++;
                var new_arr = state.next_variable++;
                var new_inner_array = state.next_variable++;
                var byte_length = state.next_variable++;
                var orig_inner_array = state.next_variable++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, byte_length, var.ContentsOf(orig_arr_var_id, GetArrayFieldOffset(ArrayFields.inner_array_length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, orig_inner_array, var.ContentsOf(orig_arr_var_id, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));

                i.tacs.Add(new CallEx(new_inner_array, new var[] { byte_length }, "gcmalloc", callconv_gcmalloc));
                i.tacs.Add(new CallEx(new_arr, new var[] { var.Const(GetArrayFieldOffset(ArrayFields.array_type_size)) }, "gcmalloc", callconv_gcmalloc));

                i.tacs.Add(new CallEx(var.Null, new var[] { new_inner_array, orig_inner_array, byte_length }, "__memcpy", callconv_memcpy));
                i.tacs.Add(new CallEx(var.Null, new var[] { new_arr, orig_arr_var_id, var.Const(GetArrayFieldOffset(ArrayFields.array_type_size)) }, "__memcpy", callconv_memcpy));

                i.tacs.Add(new CallEx(new_obj_id, new var[] { }, "__get_new_obj_id", callconv_getobjid));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(new_arr, GetStringFieldOffset(StringFields.objid)), new_obj_id, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i8, var.ContentsOf(new_arr, GetStringFieldOffset(StringFields.mutex_lock)), var.Const(0L), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, var.ContentsOf(new_arr, GetArrayFieldOffset(ArrayFields.inner_array)), new_inner_array, var.Null));

                i.pushes_variable = new_arr;
                i_pushes_set = true;
            }
            else if (methname == "_Zu1SM_0_10get_Length_Ri_P1u1t")
            {
                var obj_var_id = i.stack_before[i.stack_before.Count - 1].contains_variable;
                enc_checknullref(i, obj_var_id);

                // length = *(obj_var_id + length_offset)
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.ContentsOf(obj_var_id, GetStringFieldOffset(StringFields.length)), 0));
            }
            else if (methname == "_Zu1SM_0_9get_Chars_Rc_P2u1ti")
            {
                // char get_Chars(int32 idx)
                // if(idx >= length) throw IndexOutOfRangeException
                // if(idx < 0) throw IndexOutOfRangeException
                // addr = obj + obj.ClassSize + idx * char.Size
                // retval = peek_u2(addr)

                var v_str = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_idx = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_length = state.next_variable++;
                var v_addr = state.next_variable++;
                var v_offset = state.next_variable++;

                enc_checknullref(i, v_str);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_length, var.ContentsOf(v_str, GetStringFieldOffset(StringFields.length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_idx, v_length));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.throwge_un, var.Null, var.Const(throw_IndexOutOfRangeException), var.Null));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_offset, v_idx, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_offset, v_offset, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i4, v_offset, v_offset, var.Const(GetStringFieldOffset(StringFields.data_offset))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_addr, v_offset, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_addr, v_addr, v_str));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.peek_u2, state.next_variable++, v_addr, var.Null));
            }
            else if (methname == "_Zu1SM_0_19InternalAllocateStr_Ru1S_P1i")
            {
                /* static string InternalAllocateString(int32 length)
                 * 
                 * v_ret = gcmalloc(sizeof(string) + length * sizeof(char))
                 * *(v_ret + __vtbl_offset) = System.String_VT
                 * *(v_ret + __obj_id) = next_object_id++
                 * *(v_ret + length) = length
                 */

                Assembler.TypeToCompile ttc_string = Metadata.GetTTC("mscorlib", "System", "String", this);
                Layout l = Layout.GetTypeInfoLayout(ttc_string, this, false);

                var v_ret = state.next_variable++;
                var v_size = state.next_variable++;
                var v_length = state.next_variable++;
                var v_mem_size = state.next_variable++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_size, var.Const(GetStringFieldOffset(StringFields.data_offset)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_length, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_length, v_length, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i4, v_size, v_size, v_length));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_mem_size, v_size, var.Null));
                i.tacs.Add(new CallEx(v_ret, new var[] { v_mem_size }, "gcmalloc", callconv_gcmalloc));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, var.ContentsOf(v_ret, GetStringFieldOffset(StringFields.vtbl)), var.AddrOfObject(l.typeinfo_object_name, l.FixedLayout[Layout.ID_VTableStructure].Offset), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(v_ret, GetStringFieldOffset(StringFields.objid)), var.Const(next_object_id.Increment), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(v_ret, GetStringFieldOffset(StringFields.length)), i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));

                i.pushes_variable = v_ret;
                i_pushes_set = true;
            }
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P2u1tPa")
            {
                /* instance void .ctor(sbyte * val)
                 * 
                 * The string should be already set up to the correct length by InternalAllocateStr
                 * 
                 * We define a conversion function __mbstowcs(wchar_t *wcstr, const char *mbstr, size_t count)
                 * where count is the maximum number of characters to convert.
                 * For safety's sake, we set this to be the length of the string object we are converting into.
                 */

                var v_str_len = state.next_variable++;
                var v_str_data = state.next_variable++;
                var v_src_data = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_obj = i.stack_before[i.stack_before.Count - 2].contains_variable;

                enc_checknullref(i, v_src_data);
                enc_checknullref(i, v_obj);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_str_len, var.ContentsOf(v_obj, GetStringFieldOffset(StringFields.length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_str_data, v_obj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_str_data, v_str_data, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_str_data, v_src_data, v_str_len }, "__mbstowcs", callconv_mbstowcs));
            }
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P2u1tPc")
                return false;
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P4u1tPcii")
            {
                /* instance void .ctor(char * value, int startIndex, int length)
                 * 
                 * The string should already be set up to the correct length by InternalAllocateStr
                 */

                var v_str_len = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_startIndex = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_value = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_obj = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_str_data = state.next_variable++;
                var v_src_data = state.next_variable++;
                var v_byte_len = state.next_variable++;

                enc_checknullref(i, v_value);
                enc_checknullref(i, v_obj);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_src_data, v_startIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i, v_src_data, v_src_data, var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.Char)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_src_data, v_src_data, v_value));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_str_data, v_obj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_str_data, v_str_data, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_byte_len, v_str_len, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_byte_len, v_byte_len, var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.Char)))));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_str_data, v_src_data, v_byte_len }, "__memcpy", callconv_memcpy));
            }
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P4u1tPaii")
                return false;
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P5u1tPaiiW13System#2EText8Encoding")
                return false;
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P4u1tu1Zcii")
            {
                /* instance void .ctor(char[] value, int startIndex, int length)
                 * 
                 * The string should already be set up to the correct length by InternalAllocateStr
                 */

                var v_str_len = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_startIndex = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_value = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_obj = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_str_data = state.next_variable++;
                var v_src_data = state.next_variable++;
                var v_byte_len = state.next_variable++;

                enc_checknullref(i, v_value);
                enc_checknullref(i, v_obj);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_src_data, v_startIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i, v_src_data, v_src_data, var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.Char)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_src_data, v_src_data, var.ContentsOf(v_value, GetArrayFieldOffset(ArrayFields.inner_array))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_str_data, v_obj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_str_data, v_str_data, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_byte_len, v_str_len, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_byte_len, v_byte_len, var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.Char)))));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_str_data, v_src_data, v_byte_len }, "__memcpy", callconv_memcpy));
            }
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P2u1tu1Zc")
            {
                /* instance void .ctor(char[] c)
                 * 
                 * The string should already be set up to the correct length by InternalAllocateStr
                 */

                var v_str_len = state.next_variable++;
                var v_c = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_obj = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_str_data = state.next_variable++;
                var v_c_data = state.next_variable++;

                enc_checknullref(i, v_obj);
                enc_checknullref(i, v_c);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_c_data, var.ContentsOf(v_c, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_str_data, v_obj, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_str_len, var.ContentsOf(v_obj, GetStringFieldOffset(StringFields.length)), var.Const(GetPackedSizeOf(new Signature.Param(BaseType_Type.Char)))));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_str_data, v_c_data, v_str_len }, "__memcpy", callconv_memcpy));
            }
            else if (methname == "_Zu1SM_0_7#2Ector_Rv_P3u1tci")
            {
                /* instance void .ctor(char c, int count)
                 * 
                 * The string should already be set up to the correct length by InternalAllocateStr
                 */

                var v_str_len = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_c = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_obj = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_str_data = state.next_variable++;

                enc_checknullref(i, v_obj);

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_str_data, v_obj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_str_data, v_str_data, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_str_data, v_c, v_str_len }, "__memsetw", callconv_memsetw));
            }
            else if (methname == "_ZW35System#2ERuntime#2ECompilerServices14RuntimeHelpersM_0_22get_OffsetToStringData_Ri_P0")
            {
                /* static int32 get_OffsetToStringData()
                 * 
                 * ret sizeof(string)
                 */

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetStringFieldOffset(StringFields.data_offset)), var.Null));
            }
            else if (methname == "_ZW35System#2ERuntime#2ECompilerServices14RuntimeHelpersM_0_15InitializeArray_Rv_P2U6System5Arrayu1I")
            {
                /* static void InitializeArray(System.Array array, native int FieldInfo)
                 * 
                 * memcpy(array->inner_array, FieldInfo->Literal_data, array->bytesize)
                 */

                var v_array = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_fieldinfo = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_elemsize = state.next_variable++;
                var v_arraylength = state.next_variable++;
                var v_intarray = state.next_variable++;
                var v_litdata = state.next_variable++;

                enc_checknullref(i, v_array);
                enc_checknullref(i, v_fieldinfo);

                /* calculate the byte length of the inner array */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_elemsize, var.ContentsOf(v_array, GetArrayFieldOffset(ArrayFields.elem_size)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_arraylength, var.ContentsOf(v_array, GetArrayFieldOffset(ArrayFields.inner_array_length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_arraylength, v_arraylength, v_elemsize));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_arraylength, v_arraylength, var.Null));

                /* get the inner array address */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_intarray, var.ContentsOf(v_array, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));

                /* get the literal data */
                GetTysosFieldLayout();
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_litdata, var.ContentsOf(v_fieldinfo, tysos_field_offsets["IntPtr Literal_data"]), var.Null));
                enc_checknullref(i, v_litdata);

                /* call memcpy to set up the array */
                i.tacs.Add(new CallEx(var.Null, new var[] { v_intarray, v_litdata, v_arraylength }, "__memcpy", callconv_memcpy));
            }
            else if (methname == "_ZW20System#2EDiagnostics10StackTraceM_0_9get_trace_Ru1ZV10StackFrame_P3U6System9Exceptionib")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++,
                    var.Const(0L), var.Null));
            }
            else if (methname == "_Zu1SM_0_14InternalStrcpy_Rv_P3u1Siu1Zc")
            {
                /* static void InternalStrcpy(string dest, int32 destPos, char[] chars) */

                var v_dest = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_destPos = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_chars = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_actdest = state.next_variable++;
                var v_actsrc = state.next_variable++;
                var v_length = state.next_variable++;
                var v_destposadj = state.next_variable++;

                enc_checknullref(i, v_dest);
                enc_checknullref(i, v_chars);

                /* set up actdest */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actdest, v_dest, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_destposadj, v_destPos, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_destposadj, v_destposadj, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_destposadj, v_destposadj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, v_destposadj));

                /* set up length */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_length, var.ContentsOf(v_chars, GetArrayFieldOffset(ArrayFields.sizes)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_length, var.ContentsOf(v_length), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i4sx, v_length, v_length, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_length, v_length, var.Const(2)));

                /* set up actsrc */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actsrc, var.ContentsOf(v_chars, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));

                /* call memmove */
                i.tacs.Add(new CallEx(var.Null, new var[] { v_actdest, v_actsrc, v_length }, "__memmove", callconv_memmove));
            }
            else if (methname == "_Zu1SM_0_14InternalStrcpy_Rv_P5u1Siu1Sii")
            {
                /* static void InternalStrcpy(string dest, int32 destPos, string src, int32 sPos, int32 count) */

                var v_dest = i.stack_before[i.stack_before.Count - 5].contains_variable;
                var v_destPos = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_src = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_sPos = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_count = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_actdest = state.next_variable++;
                var v_actsrc = state.next_variable++;
                var v_length = state.next_variable++;
                var v_destposadj = state.next_variable++;
                var v_srcposadj = state.next_variable++;

                enc_checknullref(i, v_dest);
                enc_checknullref(i, v_src);

                /* set up actdest */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actdest, v_dest, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_destposadj, v_destPos, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_destposadj, v_destposadj, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_destposadj, v_destposadj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, v_destposadj));

                /* set up length */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_length, v_count, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_length, v_length, var.Const(2)));

                /* set up actsrc */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actsrc, v_src, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actsrc, v_actsrc, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_srcposadj, v_sPos, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_srcposadj, v_srcposadj, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_srcposadj, v_srcposadj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actsrc, v_actsrc, v_srcposadj));

                /* call memmove */
                i.tacs.Add(new CallEx(var.Null, new var[] { v_actdest, v_actsrc, v_length }, "__memmove", callconv_memmove));
            }
            else if (methname == "_Zu1SM_0_14InternalStrcpy_Rv_P3u1Siu1S")
            {
                /* static void InternalStrcpy(string dest, int32 destPos, string src) */

                var v_dest = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_destPos = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_src = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_actdest = state.next_variable++;
                var v_actsrc = state.next_variable++;
                var v_length = state.next_variable++;
                var v_destposadj = state.next_variable++;

                enc_checknullref(i, v_dest);
                enc_checknullref(i, v_src);

                /* set up actdest */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actdest, v_dest, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_destposadj, v_destPos, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_destposadj, v_destposadj, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_destposadj, v_destposadj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, v_destposadj));

                /* set up length */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_length, var.ContentsOf(v_src, GetStringFieldOffset(StringFields.length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_length, v_length, var.Const(2)));

                /* set up actsrc */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actsrc, v_src, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actsrc, v_actsrc, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));

                /* call memmove */
                i.tacs.Add(new CallEx(var.Null, new var[] { v_actdest, v_actsrc, v_length }, "__memmove", callconv_memmove));
            }
            else if (methname == "_Zu1SM_0_14InternalStrcpy_Rv_P5u1Siu1Zcii")
            {
                /* static void InternalStrcpy(string dest, int32 destPos, char[] chars, int32 sPos, int32 count) */

                var v_dest = i.stack_before[i.stack_before.Count - 5].contains_variable;
                var v_destPos = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_chars = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_sPos = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_count = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_actdest = state.next_variable++;
                var v_actsrc = state.next_variable++;
                var v_length = state.next_variable++;
                var v_destposadj = state.next_variable++;
                var v_srcposadj = state.next_variable++;

                enc_checknullref(i, v_dest);
                enc_checknullref(i, v_chars);

                /* set up actdest */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actdest, v_dest, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, var.Const(new IntPtr(GetStringFieldOffset(StringFields.data_offset)))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_destposadj, v_destPos, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_destposadj, v_destposadj, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_destposadj, v_destposadj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actdest, v_actdest, v_destposadj));

                /* set up length */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_length, v_count, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_length, v_length, var.Const(2)));

                /* set up actsrc */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_actsrc, var.ContentsOf(v_chars, GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_srcposadj, v_sPos, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_srcposadj, v_srcposadj, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_srcposadj, v_srcposadj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_actsrc, v_actsrc, v_srcposadj));

                /* call memmove */
                i.tacs.Add(new CallEx(var.Null, new var[] { v_actdest, v_actsrc, v_length }, "__memmove", callconv_memmove));
            }
            else if (methname == "_ZW6System4TypeM_0_14EqualsInternal_Rb_P2u1tV4Type")
            {
                /* instance bool System.Type.EqualsInternal(Type type)
                 * 
                 * Check for reference equality
                 */

                var v_a = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_b = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_ret = state.next_variable++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, var.Null, v_a, v_b));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.seteq, v_ret, var.Null, var.Null));
            }
            else if (methname == "_ZW6System4TypeM_0_20internal_from_handle_RV4Type_P1u1I")
            {
                /* static System.Type internal_from_handle(IntPtr handle.Value)
                 * 
                 * In this case the argument should already be an instance of libsupcs.TysosType - we therefore
                 * merely have to check it for null and return it.
                 * 
                 */

                var v_handle = i.stack_before[i.stack_before.Count - 1].contains_variable;
                enc_checknullref(i, v_handle);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, v_handle, var.Null));
            }
            else if (methname == "_Zu1OM_0_7GetType_RW6System4Type_P1u1t")
            {
                // this is instance System.Type GetType()
                /* To get an instance of libsupcs.TysosType, we dereference the object to get its VTable, then dereference it again */

                var v_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_vtbl = state.next_variable++;
                var v_unboxed_type = state.next_variable++;
                var v_typeinfo = state.next_variable++;
                i.node_global_vars.Add(v_unboxed_type);
                i.node_global_vars.Add(v_typeinfo);

                int blk_skip = state.next_block++;

                enc_checknullref(i, v_obj);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_vtbl, var.ContentsOf(v_obj, 0, GetSizeOfPointer()), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_typeinfo, var.ContentsOf(v_vtbl, 0, GetSizeOfPointer()), var.Null));

                // Return the unboxed type info if this is a boxed value type
                GetTysosTypeLayout();
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_unboxed_type, var.ContentsOf(v_typeinfo, tysos_type_offsets["IntPtr UnboxedType"]), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, var.Null, v_unboxed_type, var.Const(0)));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.beq, blk_skip));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_typeinfo, v_unboxed_type, var.Null));
                i.tacs.Add(LabelEx.LocalLabel(blk_skip));

                i.pushes_variable = v_typeinfo;
                i_pushes_set = true;
            }
            else if (methname == "_ZW34System#2ERuntime#2EInteropServices7MarshalM_0_37GetFunctionPointerForDelegateInternal_Ru1I_P1U6System8Delegate")
            {
                /* static IntPtr GetFunctionPointerForDelegateInternal(System.Delegate)
                 * 
                 * 
                 * assign_from_virtftn_ptr(delegate + field_offset)
                 */

                if (provides)
                    return true;

                Layout l = Layout.GetLayout(new TypeToCompile(i.stack_before[i.stack_before.Count - 1].type, this), this);
                int method_ptr_offset = l.GetField("VirtFtnPtr method_ptr", false).offset;
                var obj_id = i.stack_before[i.stack_before.Count - 1].contains_variable;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_from_virtftnptr_ptr, state.next_variable++, var.ContentsOf(obj_id, method_ptr_offset), var.Null));
            }
            else if (methname == "_ZX15OtherOperationsM_0_4Halt_Rv_P0")
            {
                i.tacs.Add(new CallEx(var.Null, new var[] { }, "__halt", MakeStaticCall(Options.CallingConvention, new Signature.Param(BaseType_Type.Void), new List<Signature.Param>(), ThreeAddressCode.Op.call_void)));
            }
            else if (methname == "_ZW6System4MathM_0_4Sqrt_Rd_P1d")
            {
                // static double Sqrt(double x)
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.sqrt_r8, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            }
            else if (methname == "_ZW18System#2EThreading6ThreadM_0_22CurrentThread_internal_RV6Thread_P0")
            {
                /* static System.Threading.Thread CurrentThread_internal()
                 * 
                 * Create a new System.Threading.Thread object, and assign the current thread id to its 'thread_id' field */

                Assembler.TypeToCompile thread_ttc = Metadata.GetTTC("mscorlib", "System.Threading", "Thread", this);
                Layout l = Layout.GetTypeInfoLayout(thread_ttc, this, false);

                var v_ret = state.next_variable++;
                var v_objid = state.next_variable++;
                var v_threadid = state.next_variable++;

                i.tacs.Add(new CallEx(v_ret, new var[] { var.Const(l.ClassSize) }, "gcmalloc", callconv_gcmalloc));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, var.ContentsOf(v_ret, l.GetFirstInstanceField("__vtbl").offset), var.AddrOfObject(l.typeinfo_object_name, l.FixedLayout[Layout.ID_VTableStructure].Offset), var.Null));
                i.tacs.Add(new CallEx(v_objid, new var[] { }, "__get_new_obj_id", callconv_getobjid));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, var.ContentsOf(v_ret, l.GetFirstInstanceField("__object_id").offset), v_objid, var.Null));
                i.tacs.Add(new CallEx(v_threadid, new var[] { }, "__get_cur_thread_id", callconv_getcurthreadid));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_uzx, v_threadid, v_threadid, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i8, var.ContentsOf(v_ret, l.GetFirstInstanceField("thread_id").offset), v_threadid, var.Null));

                i.pushes_variable = v_ret;
                i_pushes_set = true;
            }
            else if (methname == "_ZW18System#2EThreading7MonitorM_0_17Monitor_try_enter_Rb_P2u1Oi")
            {
                /* static bool Monitor_try_enter(object obj, int ms)
                 * 
                 * Try and acquire the mutex lock on object obj, waiting a maximum of ms milliseconds
                 * if ms == System.Threading.Timeout.Infinite then wait forever
                 */

                /* Currently we do not honour the ms argument
                 * If it is 0 then just try one and return success/failure
                 * Otherwise try infinitely
                 * 
                 * return true if we were successful, else false
                 * 
                 * code is:
                 * 
                 * mutex_lock_addr = obj + offset(mutex_lock)
                 * ret = false
                 * thread_id = call(__get_cur_thread_id)
                 * L1:
                 * ret = try_acquire(mutex_lock_addr, thread_id)
                 * cmp(ret, 0)
                 * bne L2
                 * cmp(ms, 0)
                 * bne L1
                 * L2:
                 * 
                 */

                var v_obj = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_ms = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_mutex_lock_addr = state.next_variable++;
                var v_ret = state.next_variable++;
                var v_thread_id = state.next_variable++;
                v_thread_id.is_global = true;
                i.node_global_vars.Add(v_thread_id);
                i.node_global_vars.Add(v_ms);
                i.node_global_vars.Add(v_ret);
                i.node_global_vars.Add(v_mutex_lock_addr);

                int blk_loop = state.next_block++;
                int blk_success = state.next_block++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_mutex_lock_addr, v_obj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_mutex_lock_addr, v_mutex_lock_addr, var.Const(GetStringFieldOffset(StringFields.mutex_lock))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.Const(0), var.Null));
                i.tacs.Add(new CallEx(v_thread_id, new var[] { }, "__get_cur_thread_id", callconv_getcurthreadid));

                i.tacs.Add(LabelEx.LocalLabel(blk_loop));
                var v_globalret = v_ret;
                v_globalret.is_global = true;
                i.tacs.Add(new CallEx(v_ret, new var[] { v_mutex_lock_addr, v_thread_id }, "__try_acquire", callconv_try_acquire));
                //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.try_acquire_i8, v_ret, v_mutex_lock_addr, v_thread_id));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_ret, var.Const(0), var.Null));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.bne, blk_success));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_ms, var.Const(0), var.Null));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.bne, blk_loop));

                i.tacs.Add(LabelEx.LocalLabel(blk_success));

                i.pushes_variable = v_ret;
                i_pushes_set = true;
            }
            else if (methname == "_ZW18System#2EThreading7MonitorM_0_12Monitor_exit_Rv_P1u1O")
            {
                /* static void Monitor_exit(object obj)
                 * 
                 * Release the mutex lock on obj, if we own it
                 */

                /* code is:
                 * 
                 * mutex_lock_addr = obj + offset(mutex_lock)
                 * thread_id = call(__get_cur_thread_id)
                 * release(mutex_lock_addr, thread_id)
                 * 
                 */

                var v_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_mutex_lock_addr = state.next_variable++;
                var v_thread_id = state.next_variable++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_mutex_lock_addr, v_obj, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_mutex_lock_addr, v_mutex_lock_addr, var.Const(GetStringFieldOffset(StringFields.mutex_lock))));
                i.tacs.Add(new CallEx(v_thread_id, new var[] { }, "__get_cur_thread_id", callconv_getcurthreadid));
                i.tacs.Add(new CallEx(var.Null, new var[] { v_mutex_lock_addr, v_thread_id }, "__release", callconv_release));
                //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.release_i8, var.Null, v_mutex_lock_addr, v_thread_id));
            }
            else if (methname == "_ZX15OtherOperationsM_0_16GetUsedStackSize_Ri_P0")
            {
                /* static int GetUsedStackSize()
                 * 
                 * Return the size in bytes of data stored on the stack in the current method
                 * 
                 * This is calculated at a later stage than here, so for now just insert a placeholder
                 */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(new var.SpecialConstValue(var.SpecialConstValue.SpecialConstValueType.UsedStackSize)), var.Null));
            }
            else if (methname == "_Zu1SM_0_14InternalCopyTo_Rv_P5u1tiu1Zcii")
            {
                /* void string.InternalCopyTo(int sIndex, char []dest, int destIndex, int count)
                 */

                var v_src = i.stack_before[i.stack_before.Count - 5].contains_variable;
                var v_sIndex = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_dest = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_destIndex = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_count = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_srca = state.next_variable++;
                var v_desta = state.next_variable++;
                var v_c = state.next_variable++;

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_srca, v_sIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i, v_srca, v_srca, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_srca, v_srca, v_src));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_srca, v_srca, var.Const(GetStringFieldOffset(StringFields.data_offset))));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_desta, v_destIndex, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i, v_desta, v_desta, var.Const(2)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_desta, v_desta, var.ContentsOf(v_dest, GetArrayFieldOffset(ArrayFields.inner_array))));

                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_c, v_count, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_c, v_c, var.Const(2)));

                i.tacs.Add(new CallEx(var.Null, new var[] { v_desta, v_srca, v_c }, "__memcpy", callconv_memcpy));
            }
            else if (methname == "_ZW6System9ValueTypeM_0_14InternalEquals_Rb_P3u1Ou1ORu1Zu1O")
            {
                /* static bool System.ValueType.InternalEquals(object o1, object o2, out object[] fields)
                 */

                var v_o1 = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_o2 = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_fields = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_ret = state.next_variable++;
                v_ret.is_global = true;
                i.node_global_vars.Add(v_ret);

                var v_o1_vt = state.next_variable++;
                var v_o1_ti = state.next_variable++;
                var v_o2_vt = state.next_variable++;
                var v_o2_ti = state.next_variable++;

                var v_len = state.next_variable++;

                var v_o1_cmp = state.next_variable++;
                var v_o2_cmp = state.next_variable++;

                var v_memcpy_result = state.next_variable++;

                int blk_getlen = state.next_block++;
                int blk_end = state.next_block++;
                int blk_success = state.next_block++;

                enc_checknullref(i, v_o1);
                enc_checknullref(i, v_o2);

                // get type typeinfo of o1 and o2
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_o1_vt, var.ContentsOf(v_o1), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_o1_ti, var.ContentsOf(v_o1_vt), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_o2_vt, var.ContentsOf(v_o2), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, v_o2_ti, var.ContentsOf(v_o2_vt), var.Null));

                // check they are the same type
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i, var.Null, v_o1_ti, v_o2_ti));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.beq, blk_getlen));

                // if not then fail
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.Const(0), var.Null));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.br, blk_end));

                // now perform the comparison proper
                i.tacs.Add(LabelEx.LocalLabel(blk_getlen));

                // we compare from the end of the fixed fields (vtbl, object_id and mutex_lock) to the end of the object
                // the end of the fixed fields can be found by getting the class size of System.Object
                int sys_obj_size = Layout.GetLayout(new TypeToCompile(new Signature.Param(BaseType_Type.Object), this), this).ClassSize;

                // the length of the object is found in the typeinfo
                int classsize_offset = GetTysosTypeLayout().InstanceFieldOffsets["Int32 ClassSize"];
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_len, var.ContentsOf(v_o1_ti, classsize_offset), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.sub_i4, v_len, v_len, var.Const(sys_obj_size)));

                // get the start addresses to compare
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_o1_cmp, v_o1, var.Const((IntPtr)sys_obj_size)));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_o2_cmp, v_o2, var.Const((IntPtr)sys_obj_size)));

                // call memcmp
                i.tacs.Add(new CallEx(v_memcpy_result, new var[] { v_o1_cmp, v_o2_cmp, v_len }, "__memcmp", callconv_memcmp));

                // memcmp returns 0 for success
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_memcpy_result, var.Const(0)));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.beq, blk_success));

                // fail
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.Const(0), var.Null));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.br, blk_end));

                // success
                i.tacs.Add(LabelEx.LocalLabel(blk_success));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.Const(-1), var.Null));

                // end
                i.tacs.Add(LabelEx.LocalLabel(blk_end));

                i.pushes_variable = v_ret;
                i_pushes_set = true;
            }
            else if (methname == "_ZW6System6BufferM_0_17BlockCopyInternal_Rb_P5V5ArrayiV5Arrayii")
            {
                /* static bool System.Buffer.BlockCopyInternal(Array src, int srcOffset, Array dest, int destOffset, int count)
                 */

                var v_src = i.stack_before[i.stack_before.Count - 5].contains_variable;
                var v_srcOffset = i.stack_before[i.stack_before.Count - 4].contains_variable;
                var v_dest = i.stack_before[i.stack_before.Count - 3].contains_variable;
                var v_destOffset = i.stack_before[i.stack_before.Count - 2].contains_variable;
                var v_count = i.stack_before[i.stack_before.Count - 1].contains_variable;

                var v_ret = state.next_variable++;
                var v_srca = state.next_variable++;
                var v_desta = state.next_variable++;

                var v_srcmax = state.next_variable++;
                var v_destmax = state.next_variable++;

                var v_srclen = state.next_variable++;
                var v_destlen = state.next_variable++;

                v_ret.is_global = true;
                i.node_global_vars.Add(v_ret);

                int blk_fail = state.next_block++;
                int blk_success = state.next_block++;
                int blk_end = state.next_block++;

                /* Check for overflow within src */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i4, v_srcmax, v_srcOffset, v_count));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_srclen, var.ContentsOf(v_src, GetArrayFieldOffset(ArrayFields.inner_array_length)), var.ContentsOf(v_src, GetArrayFieldOffset(ArrayFields.elem_size))));
                //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_srcmax, var.ContentsOf(v_src, GetArrayFieldOffset(ArrayFields.inner_array_length))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_srcmax, v_srclen));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.bg, blk_fail));

                /* Check for overflow within dest */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i4, v_destmax, v_destOffset, v_count));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_destlen, var.ContentsOf(v_dest, GetArrayFieldOffset(ArrayFields.inner_array_length)), var.ContentsOf(v_dest, GetArrayFieldOffset(ArrayFields.elem_size))));
                //i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_destmax, var.ContentsOf(v_dest, GetArrayFieldOffset(ArrayFields.inner_array_length))));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.cmp_i4, var.Null, v_destmax, v_destlen));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.ble, blk_success));

                /* Return false on failure */
                i.tacs.Add(LabelEx.LocalLabel(blk_fail));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.Const(0), var.Null));
                i.tacs.Add(new BrEx(ThreeAddressCode.Op.br, blk_end));

                /* The bounds check succeeded - continue */
                i.tacs.Add(LabelEx.LocalLabel(blk_success));

                /* Get src address */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_srca, v_srcOffset, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_srca, v_srca, var.ContentsOf(v_src, GetArrayFieldOffset(ArrayFields.inner_array))));

                /* Get dest address */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, v_desta, v_destOffset, var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.add_i, v_desta, v_desta, var.ContentsOf(v_dest, GetArrayFieldOffset(ArrayFields.inner_array))));

                /* Execute memmove */
                i.tacs.Add(new CallEx(var.Null, new var[] { v_desta, v_srca, v_count }, "__memmove", callconv_memmove));

                /* Success */
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.Const(-1), var.Null));
                i.tacs.Add(LabelEx.LocalLabel(blk_end));

                i.pushes_variable = v_ret;
                i_pushes_set = true;
            }
            else if (methname == "_ZW6System6BufferM_0_18ByteLengthInternal_Ri_P1V5Array")
            {
                /* static int System.Buffer.ByteLengthInternal(Array obj)
                 */

                var v_obj = i.stack_before[i.stack_before.Count - 1].contains_variable;
                var v_ret = state.next_variable++;
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, v_ret, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.inner_array_length)), var.Null));
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.mul_i4, v_ret, v_ret, var.ContentsOf(v_obj, GetArrayFieldOffset(ArrayFields.elem_size))));
            }
            else if (methname == "_ZW6System4EnumM_0_9get_value_Ru1O_Pu1t")
            {
                /* instance object System.Enum.get_value() */
                // TODO
                return false;
            }
            else if (methname == "_ZW20System#2EDiagnostics8DebuggerM_0_5Break_Rv_P0")
            {
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.break_));
            }
            else if (methname == "_ZX15ClassOperationsM_0_22GetObjectIdFieldOffset_Ru1U_P0")
            {
                /* static UIntPtr GetObjectIdFieldOffset */
                Layout l = Layout.GetTypeInfoLayout(new TypeToCompile(new Signature.Param(BaseType_Type.Object), this), this, false);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.Const(l.GetFirstInstanceField("__object_id").offset), var.Null));
            }
            else if (methname == "_ZX15ClassOperationsM_0_18GetVtblFieldOffset_Ru1U_P0")
            {
                /* static UIntPtr GetVtblFieldOffset */
                Layout l = Layout.GetTypeInfoLayout(new TypeToCompile(new Signature.Param(BaseType_Type.Object), this), this, false);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.Const(l.GetFirstInstanceField("__vtbl").offset), var.Null));
            }
            else if (methname == "_ZX15ClassOperationsM_0_24GetVtblTypeInfoPtrOffset_Ru1U_P0")
            {
                /* static UIntPtr GetVtblTypeInfoPtrOffset */
                Layout l = Layout.GetTypeInfoLayout(new TypeToCompile(new Signature.Param(BaseType_Type.Object), this), this, false);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.Const(0), var.Null));
            }
            else if (methname == "_ZX15ClassOperationsM_0_26GetVtblInterfacesPtrOffset_Ru1U_P0")
            {
                /* static UIntPtr GetVtblInterfacesPtrOffset */
                Layout l = Layout.GetTypeInfoLayout(new TypeToCompile(new Signature.Param(BaseType_Type.Object), this), this, false);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.Const(GetSizeOfPointer()), var.Null));
            }
            else if (methname == "_ZX15ClassOperationsM_0_27GetVtblExtendsVtblPtrOffset_Ru1U_P0")
            {
                /* static UIntPtr GetVtblExtendsVtblPtrOffset */
                Layout l = Layout.GetTypeInfoLayout(new TypeToCompile(new Signature.Param(BaseType_Type.Object), this), this, false);
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.Const(2 * GetSizeOfPointer()), var.Null));
            }
            else if (methname == "_ZX15ArrayOperationsM_0_17GetElemSizeOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.elem_size)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_17GetLoboundsOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.lobounds)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_13GetRankOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.rank)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_17GetElemTypeOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.elemtype)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_14GetSizesOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.sizes)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_25GetInnerArrayLengthOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.inner_array_length)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_19GetInnerArrayOffset_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.inner_array)), var.Null));
            else if (methname == "_ZX15ArrayOperationsM_0_17GetArrayClassSize_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(GetArrayFieldOffset(ArrayFields.array_type_size)), var.Null));
            else if (methname == "_ZX15OtherOperationsM_0_22GetStaticObjectAddress_Ru1I_P1u1S")
            {
                string str = i.stack_before[i.stack_before.Count - 1].contains_variable.known_value as string;
                if (str == null)
                {
                    if (provides)
                        return false;
                    throw new Exception("GetStaticObjectAddress: unable to identify requested string");
                }
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.AddrOfObject(str), var.Null));
            }
            else if (methname == "_ZX15OtherOperationsM_0_18GetFunctionAddress_Ru1I_P1u1S")
            {
                string str = i.stack_before[i.stack_before.Count - 1].contains_variable.known_value as string;
                if (str == null)
                {
                    if (provides)
                        return false;
                    throw new Exception("GetFunctionAddress: unable to identify requested string");
                }
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, var.AddrOfFunction(str), var.Null));
            }
            /* Handle casting to/from IntPtr/UIntPtr/void * */
            else if (methname == "_Zu1UM_0_11op_Explicit_Ry_P1u1U")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i8sx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1UM_0_11op_Explicit_Rj_P1u1U")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i4sx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1UM_0_11op_Explicit_Ru1U_P1y")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_isx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1UM_0_11op_Explicit_Ru1U_P1j")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1UM_0_11op_Explicit_Ru1U_P1Pv")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1IM_0_11op_Explicit_Rx_P1u1I")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i8sx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1IM_0_11op_Explicit_Ri_P1u1I")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i_i4sx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1IM_0_11op_Explicit_Ru1I_P1x")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i8_isx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1IM_0_11op_Explicit_Ru1I_P1i")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.conv_i4_isx, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));
            else if (methname == "_Zu1IM_0_11op_Explicit_Ru1I_P1Pv")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i, state.next_variable++, i.stack_before[i.stack_before.Count - 1].contains_variable, var.Null));

            else if (methname == "_ZX15OtherOperationsM_0_14GetPointerSize_Ri_P0")
                i.tacs.Add(new ThreeAddressCode(ThreeAddressCode.Op.assign_i4, state.next_variable++, var.Const(this.GetSizeOfPointer()), var.Null));

            else
            {
                handled = Arch_enc_intcall(methname, i, mdr, msig, tdr, tsig, state, provides, ref i_pushes_set);

                if (!handled)
                {
                    /*
#if !DEBUG
                    if(mdr.IsInternalCall)
                        throw new NotImplementedException(methname);
#endif
                     */
                    handled = false;
                    return handled;
                }
            }

            if (!(msig is Signature.Method))
            {
                throw new NotSupportedException();
            }

            Signature.Method msigm = msig as Signature.Method;
            i.pushes = msigm.RetType;
            if (i.pop_count < 0)
            {
                i.pop_count = msigm.Params.Count;
                if (msigm.HasThis && !msigm.ExplicitThis)
                    i.pop_count++;
            }
            if (!i_pushes_set && (msigm.RetType.CliType(this) != CliType.void_))
                i.pushes_variable = state.next_variable - 1;

            return handled;
        }
    }
}
