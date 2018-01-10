﻿/* Copyright (C) 2017 by John Cronin
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
using System.Text;
using libtysila5.cil;
using metadata;
using libtysila5.util;

namespace libtysila5.ir
{
    public partial class ConvertToIR
    {
        internal delegate Stack<StackItem> intcall_delegate(CilNode n, Code c, Stack<StackItem> stack_before);
        internal static System.Collections.Generic.Dictionary<string, intcall_delegate> intcalls =
            new System.Collections.Generic.Dictionary<string, intcall_delegate>(
                new GenericEqualityComparer<string>());

        static void init_intcalls()
        {
            intcalls["_Zu1S_9get_Chars_Rc_P2u1ti"] = string_getChars;
            intcalls["_Zu1S_10get_Length_Ri_P1u1t"] = string_getLength;
            intcalls["_Zu1S_19InternalAllocateStr_Ru1S_P1i"] = string_InternalAllocate;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Add_Ru1I_P2u1Iu1I"] = intptr_Add;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Mul_Ru1I_P2u1Iu1I"] = intptr_Mul;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Sub_Ru1I_P2u1Iu1I"] = intptr_Sub;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Add_Ru1U_P2u1Uu1U"] = uintptr_Add;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Mul_Ru1U_P2u1Uu1U"] = uintptr_Mul;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Sub_Ru1U_P2u1Uu1U"] = uintptr_Sub;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_5CallI_Rv_P1Pv"] = calli;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_5CallI_Rv_P2PvPv"] = calli_pvpv;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_18GetFunctionAddress_Ru1I_P1u1S"] = get_func_address;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_22GetStaticObjectAddress_RPv_P1u1S"] = get_static_obj_address;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_14GetPointerSize_Ri_P0"] = get_pointer_size;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetArrayClassSize_Ri_P0"] = array_getArrayClassSize;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetElemTypeOffset_Ri_P0"] = array_getElemTypeOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_19GetInnerArrayOffset_Ri_P0"] = array_getInnerArrayOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetElemSizeOffset_Ri_P0"] = array_getElemSizeOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_13GetRankOffset_Ri_P0"] = array_getRankOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_14GetSizesOffset_Ri_P0"] = array_getSizesOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetLoboundsOffset_Ri_P0"] = array_getLoboundsOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_16GetInternalArray_RPv_P1W6System5Array"] = array_getInternalArray;

            intcalls["_ZN14libsupcs#2Edll8libsupcs16StringOperations_13GetDataOffset_Ri_P0"] = string_getDataOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16StringOperations_15GetLengthOffset_Ri_P0"] = string_getLengthOffset;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_26GetVtblInterfacesPtrOffset_Ri_P0"] = class_getVtblInterfacesPtrOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_27GetVtblExtendsVtblPtrOffset_Ri_P0"] = class_getVtblExtendsPtrOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_22GetBoxedTypeDataOffset_Ri_P0"] = class_getBoxedTypeDataOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_18GetVtblFieldOffset_Ri_P0"] = class_getVtblFieldOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_21GetVtblTypeSizeOffset_Ri_P0"] = class_getVtblTypeSizeOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_18GetMutexLockOffset_Ri_P0"] = class_getMutexLockOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_23GetSystemTypeImplOffset_Ri_P0"] = class_getSystemTypeImplOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_14GetFieldOffset_Ri_P2u1Su1S"] = class_getFieldOffset;

            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU1_Rh_P1u1U"] = peek_Byte;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU2_Rt_P1u1U"] = peek_Ushort;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU4_Rj_P1u1U"] = peek_Uint;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU8_Ry_P1u1U"] = peek_Ulong;

            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Uh"] = poke_Byte;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Ut"] = poke_Ushort;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Uj"] = poke_Uint;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Uy"] = poke_Ulong;

            intcalls["_ZW20System#2EDiagnostics8Debugger_5Break_Rv_P0"] = debugger_Break;

            intcalls["_ZW34System#2ERuntime#2EInteropServices7Marshal_37GetFunctionPointerForDelegateInternal_Ru1I_P1U6System8Delegate"] = getFunctionPointerForDelegate;

            intcalls["_ZW35System#2ERuntime#2ECompilerServices14RuntimeHelpers_15InitializeArray_Rv_P2U6System5Arrayu1I"] = runtimeHelpers_initializeArray;
            intcalls["_ZW35System#2ERuntime#2ECompilerServices14RuntimeHelpers_22get_OffsetToStringData_Ri_P0"] = runtimeHelpers_getOffsetToStringData;

            intcalls["_ZW20System#2EDiagnostics8Debugger_3Log_Rv_P3iu1Su1S"] = debugger_Log;

            intcalls["_ZW19System#2EReflection8Assembly_20GetExecutingAssembly_RV8Assembly_P0"] = assembly_GetExecutingAssembly;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_21SyncValCompareAndSwap_Rh_P3Phhh"] = sync_cswap;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_12SpinlockHint_Rv_P0"] = spinlock_hint;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_28GetTypedReferenceValueOffset_Ri_P0"] = typedref_ValueOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_27GetTypedReferenceTypeOffset_Ri_P0"] = typedref_TypeOffset;

            intcalls["_ZW18System#2EThreading11Interlocked_16_CompareExchange_Rv_P3u1Tu1Tu1O"] = threading_CompareExchange_TypedRef;
        }

        private static Stack<StackItem> threading_CompareExchange_TypedRef(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            /* _CompareExchange(TypedReference location1, TypedReference Value, object comparand) */

            // Convert in-place location1 to its ptr
            stack_before = typedref_ValueOffset(n, c, stack_before);
            stack_before = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.add, ir.Opcode.ct_intptr, 3, -1, 2);
            stack_before = ldind(n, c, stack_before, c.ms.m.SystemIntPtr, 2, false, 2);

            // Convert in-place Value to its ptr then dereference
            stack_before = typedref_ValueOffset(n, c, stack_before);
            stack_before = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.add, ir.Opcode.ct_intptr, 2, -1, 1);
            stack_before = ldind(n, c, stack_before, c.ms.m.SystemIntPtr, 1, false, 1);
            stack_before = ldind(n, c, stack_before, c.ms.m.SystemObject, 1, false, 1);

            // Do synchronized instruction (arga = value, argb = comparand, argc = location, res = new)
            var stack_after = new Stack<StackItem>(stack_before);
            stack_after.Pop();
            stack_after.Pop();
            stack_after.Push(new StackItem { ts = c.ms.m.SystemObject });
            n.irnodes.Add(new CilNode.IRNode { parent = n, opcode = Opcode.oc_syncvalcompareandswap,
                imm_l = c.t.GetPointerSize(), imm_ul = 0, stack_before = stack_before, stack_after = stack_after, 
                arg_a = 1, arg_b = 0, arg_c = 2, res_a = 0 });

            // store res back to *location1
            stack_after = stind(n, c, stack_after, c.t.GetPointerSize());

            return stack_after;
        }

        private static Stack<StackItem> typedref_ValueOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var typedref = c.ms.m.al.GetAssembly("mscorlib").GetSimpleTypeSpec((int)CorElementType.TypedByRef);
            return ldc(n, c, stack_before, layout.Layout.GetFieldOffset(typedref, "Value", c.t), (int)CorElementType.I4);
        }

        private static Stack<StackItem> typedref_TypeOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var typedref = c.ms.m.al.GetAssembly("mscorlib").GetSimpleTypeSpec((int)CorElementType.TypedByRef);
            return ldc(n, c, stack_before, layout.Layout.GetFieldOffset(typedref, "Type", c.t), (int)CorElementType.I4);
        }

        private static Stack<StackItem> spinlock_hint(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            n.irnodes.Add(new CilNode.IRNode { parent = n, opcode = Opcode.oc_spinlockhint, stack_before = stack_before, stack_after = stack_before });
            return stack_before;
        }

        private static Stack<StackItem> sync_cswap(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = new Stack<StackItem>(stack_before);

            stack_after.Pop();
            stack_after.Pop();
            var ret_type_ptr = stack_after.Pop().ts;
            var ret_type = ret_type_ptr.other;

            var size = c.t.GetSize(ret_type);
            stack_after.Push(new StackItem { ts = ret_type });

            n.irnodes.Add(new CilNode.IRNode { parent = n, opcode = Opcode.oc_syncvalcompareandswap, imm_l = size, imm_ul = ret_type.IsSigned ? 1UL : 0UL, stack_before = stack_before, stack_after = stack_after });

            return stack_after;
        }

        private static Stack<StackItem> runtimeHelpers_getOffsetToStringData(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = ldc(n, c, stack_before, layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Start_Char, c));
            return stack_after;
        }

        private static Stack<StackItem> string_InternalAllocate(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            // save string length
            var stack_after = copy_to_front(n, c, stack_before);

            // Multiply number of characters by two, then add size of the string object
            stack_after = ldc(n, c, stack_after, 2);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.mul, Opcode.ct_intptr);
            stack_after = ldc(n, c, stack_after, layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Start_Char, c), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);

            // Create object
            stack_after = call(n, c, stack_after, false, "gcmalloc", c.special_meths,
                c.special_meths.gcmalloc);

            // vtbl
            stack_after = copy_to_front(n, c, stack_after);
            stack_after = ldlab(n, c, stack_after, c.ms.m.SystemString.Type.MangleType());
            stack_after = stind(n, c, stack_after, c.t.psize);

            // mutex lock
            stack_after = copy_to_front(n, c, stack_after);
            stack_after = ldc(n, c, stack_after, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.MutexLock, c.t), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldc(n, c, stack_after, 0, 0x18);
            stack_after = stind(n, c, stack_after, c.t.psize);

            /* now the stack is ..., length, object.
             * 
             * First save the length then move object up the stack */
            stack_after = copy_to_front(n, c, stack_after);
            stack_after = ldc(n, c, stack_after, layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Length, c), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = copy_to_front(n, c, stack_after, 2);
            stack_after = stind(n, c, stack_after, c.t.GetSize(c.ms.m.SystemIntPtr));

            var stack_after2 = new Stack<StackItem>(stack_after);
            stack_after2.Pop();
            stack_after2.Pop();
            stack_after2.Push(new StackItem { ts = c.ms.m.SystemString });

            n.irnodes.Add(new CilNode.IRNode { parent = n, opcode = Opcode.oc_stackcopy, arg_a = 0, res_a = 0, stack_before = stack_after, stack_after = stack_after2 });

            return stack_after2;
        }

        private static Stack<StackItem> assembly_GetExecutingAssembly(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            // Create a TysosAssembly and populate it
            var libsupcs = c.ms.m.al.GetAssembly("libsupcs");
            var mscorlib = c.ms.m.al.GetAssembly("mscorlib");
            var tysosAssembly = libsupcs.GetTypeSpec("libsupcs", "TysosAssembly");
            var systemAssembly = mscorlib.GetTypeSpec("System.Reflection", "Assembly");

            var systemAssemblyctor = mscorlib.GetMethodSpec(systemAssembly, ".ctor", c.special_meths.inst_Rv_P0,
                c.special_meths);

            var stack_after = newobj(n, c, stack_before, systemAssemblyctor, tysosAssembly);

            // metadata entry
            stack_after = copy_to_front(n, c, stack_after);
            stack_after = ldc(n, c, stack_after, layout.Layout.GetFieldOffset(tysosAssembly, "metadata", c.t), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldlab(n, c, stack_after, c.ms.m.AssemblyName);
            stack_after = stind(n, c, stack_after, c.t.psize);

            // assemblyName entry
            stack_after = copy_to_front(n, c, stack_after);
            stack_after = ldc(n, c, stack_after, layout.Layout.GetFieldOffset(tysosAssembly, "assemblyName", c.t), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldstr(n, c, stack_after, c.ms.m.AssemblyName);
            stack_after = stind(n, c, stack_after, c.t.psize);

            return stack_after;
        }

        private static Stack<StackItem> debugger_Log(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = call(n, c, stack_before, false, "__log", c.special_meths,
                c.special_meths.debugger_Log);
            return stack_after;
        }

        private static Stack<StackItem> string_getDataOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = ldc(n, c, stack_before, layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Start_Char, c));
            return stack_after;
        }

        private static Stack<StackItem> string_getLengthOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = ldc(n, c, stack_before, layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Length, c));
            return stack_after;
        }

        private static Stack<StackItem> runtimeHelpers_initializeArray(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            // load dest pointer onto stack
            var stack_after = copy_to_front(n, c, stack_before, 1);
            stack_after = ldc(n, c, stack_after, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.DataArrayPointer, c.t), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemIntPtr);

            // load src pointer onto stack
            stack_after = copy_to_front(n, c, stack_after, 1);
            stack_after = ldc(n, c, stack_after, 2 * c.t.psize, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemIntPtr);

            // load byte count onto stack
            stack_after = copy_to_front(n, c, stack_after, 2);
            stack_after = ldc(n, c, stack_after, c.t.psize, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemInt32);

            // call memcpy
            stack_after = memcpy(n, c, stack_after);

            // remove memcpy arguments
            stack_after = new Stack<StackItem>(stack_after);
            stack_after.Pop();
            stack_after.Pop();
            stack_after.Pop();

            // remove function arguments
            stack_after.Pop();
            stack_after.Pop();

            return stack_after;
        }

        private static Stack<StackItem> getFunctionPointerForDelegate(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var dgate = c.ms.m.SystemDelegate.Type;
            var method_ptr = dgate.m.GetFieldDefRow("method_ptr", dgate);
            if (method_ptr == null)
                method_ptr = dgate.m.GetFieldDefRow("_methodPtr", dgate);
            TypeSpec fld_ts;
            var stack_after = ldflda(n, c, stack_before, false, out fld_ts, 0, method_ptr);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemIntPtr);
            return stack_after;
        }

        private static Stack<StackItem> class_getSystemTypeImplOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var systype = c.ms.m.al.GetAssembly("libsupcs").GetTypeSpec("libsupcs", "TysosType");
            var v = layout.Layout.GetFieldOffset(systype, "_impl", c.t);

            return ldc(n, c, stack_before, v);
        }

        private static Stack<StackItem> debugger_Break(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            n.irnodes.Add(new CilNode.IRNode { parent = n, opcode = Opcode.oc_break, stack_before = stack_before, stack_after = stack_before });
            return stack_before;
        }

        private static Stack<StackItem> calli(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return call(n, c, stack_before, true, "calli_target", c.special_meths, c.special_meths.static_Rv_P0);
        }

        private static Stack<StackItem> calli_pvpv(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return call(n, c, stack_before, true, "calli_target", c.special_meths, c.special_meths.static_Rv_P1Pv);
        }

        private static Stack<StackItem> poke_Ulong(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return stind(n, c, stack_before, 8);
        }

        private static Stack<StackItem> poke_Uint(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return stind(n, c, stack_before, 4);
        }

        private static Stack<StackItem> poke_Ushort(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return stind(n, c, stack_before, 2);
        }

        private static Stack<StackItem> poke_Byte(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return stind(n, c, stack_before, 1);
        }

        private static Stack<StackItem> peek_Ulong(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldind(n, c, stack_before, c.ms.m.SystemUInt64);
        }

        private static Stack<StackItem> peek_Uint(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldind(n, c, stack_before, c.ms.m.SystemUInt32);
        }

        private static Stack<StackItem> peek_Ushort(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldind(n, c, stack_before, c.ms.m.SystemUInt16);
        }

        private static Stack<StackItem> peek_Byte(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldind(n, c, stack_before, c.ms.m.SystemByte);
        }

        private static Stack<StackItem> array_getInternalArray(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.DataArrayPointer, c.t), 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemVoid.Type.Pointer);
            return stack_after;
        }

        private static Stack<StackItem> class_getVtblFieldOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, 0);
        }

        private static Stack<StackItem> class_getBoxedTypeDataOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetTypeSize(c.ms.m.SystemObject, c.t));
        }

        private static Stack<StackItem> class_getVtblExtendsPtrOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, 2 * c.t.GetPointerSize());
        }

        private static Stack<StackItem> class_getVtblInterfacesPtrOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, 1 * c.t.GetPointerSize());
        }

        private static Stack<StackItem> class_getVtblTypeSizeOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, 3 * c.t.GetPointerSize());
        }

        private static Stack<StackItem> class_getMutexLockOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.MutexLock, c.t));
        }

        private static Stack<StackItem> array_getLoboundsOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.LoboundsPointer, c.t));
        }

        private static Stack<StackItem> array_getSizesOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.SizesPointer, c.t));
        }

        private static Stack<StackItem> array_getRankOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.Rank, c.t));
        }

        private static Stack<StackItem> array_getElemSizeOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.ElemTypeSize, c.t));
        }

        private static Stack<StackItem> array_getInnerArrayOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.DataArrayPointer, c.t));
        }

        private static Stack<StackItem> array_getElemTypeOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayFieldOffset(layout.Layout.ArrayField.ElemTypeVtblPointer, c.t));
        }

        private static Stack<StackItem> array_getArrayClassSize(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, layout.Layout.GetArrayObjectSize(c.t));
        }

        private static Stack<StackItem> get_pointer_size(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            return ldc(n, c, stack_before, c.t.GetPointerSize());
        }

        private static Stack<StackItem> class_getFieldOffset(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = new Stack<StackItem>(stack_before);
            var field = stack_after.Pop().str_val;
            var type = stack_after.Pop().str_val;

            if(field == null || type == null)
            {
                throw new Exception("getFieldOffset with null arguments");
            }

            var ts = c.ms.m.DemangleType(type);
            var offset = layout.Layout.GetFieldOffset(ts, field, c.t);

            stack_after = ldc(n, c, stack_after, offset);

            return stack_after;
        }

        private static Stack<StackItem> get_static_obj_address(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = new Stack<StackItem>(stack_before);
            var src = stack_after.Pop();

            if (src.str_val == null)
                return null;

            stack_after = ldlab(n, c, stack_after, src.str_val);
            return stack_after;
        }

        private static Stack<StackItem> get_func_address(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = new Stack<StackItem>(stack_before);
            var src = stack_after.Pop();

            if (src.str_val == null)
                return null;

            stack_after = ldlab(n, c, stack_after, src.str_val);
            return stack_after;
        }

        private static Stack<StackItem> intptr_Mul(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.mul,
                Opcode.ct_intptr);

            return stack_after;
        }

        private static Stack<StackItem> intptr_Add(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.add,
                Opcode.ct_intptr);

            return stack_after;
        }

        private static Stack<StackItem> intptr_Sub(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.sub,
                Opcode.ct_intptr);

            return stack_after;
        }

        private static Stack<StackItem> uintptr_Mul(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.mul,
                Opcode.ct_intptr);

            return stack_after;
        }

        private static Stack<StackItem> uintptr_Add(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.add,
                Opcode.ct_intptr);

            return stack_after;
        }

        private static Stack<StackItem> uintptr_Sub(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var stack_after = binnumop(n, c, stack_before, cil.Opcode.SingleOpcodes.sub,
                Opcode.ct_intptr);

            return stack_after;
        }

        static Stack<StackItem> string_getChars(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var char_offset = layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Start_Char, c);

            var stack_after = ldc(n, c, stack_before, 2);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.mul, Opcode.ct_int32);
            stack_after = conv(n, c, stack_after, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldc(n, c, stack_after, char_offset, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemChar);

            return stack_after;
        }

        static Stack<StackItem> string_getLength(CilNode n, Code c, Stack<StackItem> stack_before)
        {
            var length_offset = layout.Layout.GetStringFieldOffset(layout.Layout.StringField.Length, c);

            var stack_after = ldc(n, c, stack_before, length_offset, 0x18);
            stack_after = binnumop(n, c, stack_after, cil.Opcode.SingleOpcodes.add, Opcode.ct_intptr);
            stack_after = ldind(n, c, stack_after, c.ms.m.SystemInt32);

            return stack_after;
        }
    }
}
