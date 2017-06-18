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
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Add_Ru1I_P2u1Iu1I"] = intptr_Add;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Mul_Ru1I_P2u1Iu1I"] = intptr_Mul;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Sub_Ru1I_P2u1Iu1I"] = intptr_Sub;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Add_Ru1U_P2u1Uu1U"] = uintptr_Add;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Mul_Ru1U_P2u1Uu1U"] = uintptr_Mul;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_3Sub_Ru1U_P2u1Uu1U"] = uintptr_Sub;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_18GetFunctionAddress_Ru1I_P1u1S"] = get_func_address;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_22GetStaticObjectAddress_Ru1I_P1u1S"] = get_static_obj_address;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15OtherOperations_14GetPointerSize_Ri_P0"] = get_pointer_size;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetArrayClassSize_Ri_P0"] = array_getArrayClassSize;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetElemTypeOffset_Ri_P0"] = array_getElemTypeOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_19GetInnerArrayOffset_Ri_P0"] = array_getInnerArrayOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetElemSizeOffset_Ri_P0"] = array_getElemSizeOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_13GetRankOffset_Ri_P0"] = array_getRankOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_14GetSizesOffset_Ri_P0"] = array_getSizesOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ArrayOperations_17GetLoboundsOffset_Ri_P0"] = array_getLoboundsOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_16GetInternalArray_RPv_P1W6System5Array"] = array_getInternalArray;

            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_26GetVtblInterfacesPtrOffset_Ri_P0"] = class_getVtblInterfacesPtrOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_27GetVtblExtendsVtblPtrOffset_Ri_P0"] = class_getVtblExtendsPtrOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_22GetBoxedTypeDataOffset_Ri_P0"] = class_getBoxedTypeDataOffset;
            intcalls["_ZN14libsupcs#2Edll8libsupcs15ClassOperations_18GetVtblFieldOffset_Ri_P0"] = class_getVtblFieldOffset;

            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU1_Rh_P1u1U"] = peek_Byte;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU2_Rt_P1u1U"] = peek_Ushort;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU4_Rj_P1u1U"] = peek_Uint;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_6PeekU8_Ry_P1u1U"] = peek_Ulong;

            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Uh"] = poke_Byte;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Ut"] = poke_Ushort;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Uj"] = poke_Uint;
            intcalls["_ZN14libsupcs#2Edll8libsupcs16MemoryOperations_4Poke_Rv_P2u1Uy"] = poke_Ulong;
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
