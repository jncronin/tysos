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

namespace libtysila
{
    partial class Assembler
    {
        public enum SingleOpcodes
        {
            nop = 0x00,
            break_ = 0x01,
            ldarg_0 = 0x02,
            ldarg_1 = 0x03,
            ldarg_2 = 0x04,
            ldarg_3 = 0x05,
            ldloc_0 = 0x06,
            ldloc_1 = 0x07,
            ldloc_2 = 0x08,
            ldloc_3 = 0x09,
            stloc_0 = 0x0A,
            stloc_1 = 0x0B,
            stloc_2 = 0x0C,
            stloc_3 = 0x0D,
            ldarg_s = 0x0E,
            ldarga_s = 0x0F,
            starg_s = 0x10,
            ldloc_s = 0x11,
            ldloca_s = 0x12,
            stloc_s = 0x13,
            ldnull = 0x14,
            ldc_i4_m1 = 0x15,
            ldc_i4_0 = 0x16,
            ldc_i4_1 = 0x17,
            ldc_i4_2 = 0x18,
            ldc_i4_3 = 0x19,
            ldc_i4_4 = 0x1A,
            ldc_i4_5 = 0x1B,
            ldc_i4_6 = 0x1C,
            ldc_i4_7 = 0x1D,
            ldc_i4_8 = 0x1E,
            ldc_i4_s = 0x1F,
            ldc_i4 = 0x20,
            ldc_i8 = 0x21,
            ldc_r4 = 0x22,
            ldc_r8 = 0x23,
            dup = 0x25,
            pop = 0x26,
            jmp = 0x27,
            call = 0x28,
            calli = 0x29,
            ret = 0x2A,
            br_s = 0x2B,
            brfalse_s = 0x2C,
            brtrue_s = 0x2D,
            beq_s = 0x2E,
            bge_s = 0x2F,
            bgt_s = 0x30,
            ble_s = 0x31,
            blt_s = 0x32,
            bne_un_s = 0x33,
            bge_un_s = 0x34,
            bgt_un_s = 0x35,
            ble_un_s = 0x36,
            blt_un_s = 0x37,
            br = 0x38,
            brfalse = 0x39,
            brtrue = 0x3A,
            beq = 0x3B,
            bge = 0x3C,
            bgt = 0x3D,
            ble = 0x3E,
            blt = 0x3F,
            bne_un = 0x40,
            bge_un = 0x41,
            bgt_un = 0x42,
            ble_un = 0x43,
            blt_un = 0x44,
            switch_ = 0x45,
            ldind_i1 = 0x46,
            ldind_u1 = 0x47,
            ldind_i2 = 0x48,
            ldind_u2 = 0x49,
            ldind_i4 = 0x4A,
            ldind_u4 = 0x4B,
            ldind_i8 = 0x4C,
            ldind_i = 0x4D,
            ldind_r4 = 0x4E,
            ldind_r8 = 0x4F,
            ldind_ref = 0x50,
            stind_ref = 0x51,
            stind_i1 = 0x52,
            stind_i2 = 0x53,
            stind_i4 = 0x54,
            stind_i8 = 0x55,
            stind_r4 = 0x56,
            stind_r8 = 0x57,
            add = 0x58,
            sub = 0x59,
            mul = 0x5A,
            div = 0x5B,
            div_un = 0x5C,
            rem = 0x5D,
            rem_un = 0x5E,
            and = 0x5F,
            or = 0x60,
            xor = 0x61,
            shl = 0x62,
            shr = 0x63,
            shr_un = 0x64,
            neg = 0x65,
            not = 0x66,
            conv_i1 = 0x67,
            conv_i2 = 0x68,
            conv_i4 = 0x69,
            conv_i8 = 0x6A,
            conv_r4 = 0x6B,
            conv_r8 = 0x6C,
            conv_u4 = 0x6D,
            conv_u8 = 0x6E,
            callvirt = 0x6F,
            cpobj = 0x70,
            ldobj = 0x71,
            ldstr = 0x72,
            newobj = 0x73,
            castclass = 0x74,
            isinst = 0x75,
            conv_r_un = 0x76,
            unbox = 0x79,
            throw_ = 0x7A,
            ldfld = 0x7B,
            ldflda = 0x7C,
            stfld = 0x7D,
            ldsfld = 0x7E,
            ldsflda = 0x7F,
            stsfld = 0x80,
            stobj = 0x81,
            conv_ovf_i1_un = 0x82,
            conv_ovf_i2_un = 0x83,
            conv_ovf_i4_un = 0x84,
            conv_ovf_i8_un = 0x85,
            conv_ovf_u1_un = 0x86,
            conv_ovf_u2_un = 0x87,
            conv_ovf_u4_un = 0x88,
            conv_ovf_u8_un = 0x89,
            conv_ovf_i_un = 0x8A,
            conv_ovf_u_un = 0x8B,
            box = 0x8C,
            newarr = 0x8D,
            ldlen = 0x8E,
            ldelema = 0x8F,
            ldelem_i1 = 0x90,
            ldelem_u1 = 0x91,
            ldelem_i2 = 0x92,
            ldelem_u2 = 0x93,
            ldelem_i4 = 0x94,
            ldelem_u4 = 0x95,
            ldelem_i8 = 0x96,
            ldelem_i = 0x97,
            ldelem_r4 = 0x98,
            ldelem_r8 = 0x99,
            ldelem_ref = 0x9A,
            stelem_i = 0x9B,
            stelem_i1 = 0x9C,
            stelem_i2 = 0x9D,
            stelem_i4 = 0x9E,
            stelem_i8 = 0x9F,
            stelem_r4 = 0xA0,
            stelem_r8 = 0xA1,
            stelem_ref = 0xA2,
            ldelem = 0xA3,
            stelem = 0xA4,
            unbox_any = 0xA5,
            conv_ovf_i1 = 0xB3,
            conv_ovf_u1 = 0xB4,
            conv_ovf_i2 = 0xB5,
            conv_ovf_u2 = 0xB6,
            conv_ovf_i4 = 0xB7,
            conv_ovf_u4 = 0xB8,
            conv_ovf_i8 = 0xB9,
            conv_ovf_u8 = 0xBA,
            refanyval = 0xC2,
            ckfinite = 0xC3,
            mkrefany = 0xC6,
            ldtoken = 0xD0,
            conv_u2 = 0xD1,
            conv_u1 = 0xD2,
            conv_i = 0xD3,
            conv_ovf_i = 0xD4,
            conv_ovf_u = 0xD5,
            add_ovf = 0xD6,
            add_ovf_un = 0xD7,
            mul_ovf = 0xD8,
            mul_ovf_un = 0xD9,
            sub_ovf = 0xDA,
            sub_ovf_un = 0xDB,
            endfinally = 0xDC,
            leave = 0xDD,
            leave_s = 0xDE,
            stind_i = 0xDF,
            conv_u = 0xE0,
            double_ = 0xFE,
            tysila = 0xFD
        };

        public enum DoubleOpcodes
        {
            arglist = 0x00,
            ceq = 0x01,
            cgt = 0x02,
            cgt_un = 0x03,
            clt = 0x04,
            clt_un = 0x05,
            ldftn = 0x06,
            ldvirtftn = 0x07,
            ldarg = 0x09,
            ldarga = 0x0A,
            starg = 0x0B,
            ldloc = 0x0C,
            ldloca = 0x0D,
            stloc = 0x0E,
            localloc = 0x0F,
            endfilter = 0x11,
            unaligned_ = 0x12,
            volatile_ = 0x13,
            tail_ = 0x14,
            initobj = 0x15,
            cpblk = 0x17,
            initblk = 0x18,
            rethrow = 0x1A,
            _sizeof = 0x1C,

            flip = 0x20,
            flip3 = 0x21,
            init_rth = 0x22,
            castclassex = 0x23,
            throwfalse = 0x24,
            ldelem_vt = 0x25,
            init_rmh = 0x26,
            init_rfh = 0x27,
            stelem_vt = 0x28,
            profile = 0x29,
            gcmalloc = 0x2a,
            ldobj_addr = 0x2b,
            mbstrlen = 0x2c,
            loadcatchobj = 0x2d,
            instruction_label = 0x2e,
            pushback = 0x2f,
            throwtrue = 0x30,
            bringforward = 0x31
        }

        public class Opcode
        {
            public SingleOpcodes opcode1;
            public DoubleOpcodes opcode2;
            public bool directly_modifies_stack = false;
            public int opcode
            {
                get
                {
                    if (opcode1 == SingleOpcodes.double_)
                        return (int)opcode2 + (((int)opcode1) << 8);
                    else
                        return (int)opcode1;
                }
            }
            public string name;
            public int pop;
            public int push;
            public InlineVar inline;
            public ControlFlow ctrl;
        }

        internal class InstructionLabel : InstructionLine
        {
            public InstructionHeader instr;
            public InstructionLabel(Assembler ass, InstructionLine inst) { opcode = ass.Opcodes[0xfd2e]; instr = new InstructionHeader { ass = ass, il_offset = inst.il_offset, instr = inst }; }
            public override string ToString()
            {
                return "label: " + instr.instr.ToString();
            }
        }

        public enum PopBehaviour { Pop0 = 1, Pop1 = 2, PopI = 8, PopI8 = 32, PopR4 = 64, PopR8 = 128, PopRef = 256, VarPop = 512 };
        public enum PushBehaviour { Push0 = 1, Push1 = 2, PushI = 8, PushI8 = 16, PushR4 = 32, PushR8 = 64, PushRef = 128, VarPush = 256 };
        public enum InlineVar
        {
            InlineBrTarget, InlineField, InlineI, InlineI8, InlineMethod, InlineNone, InlineR,
            InlineSig, InlineString, InlineSwitch, InlineTok, InlineType, InlineVar, ShortInlineBrTarget,
            ShortInlineI, ShortInlineR, ShortInlineVar
        };
        public enum ControlFlow { BRANCH, CALL, COND_BRANCH, META, NEXT, RETURN, THROW, BREAK };

        protected Dictionary<int, Opcode> Opcodes = new Dictionary<int,Opcode>(new libtysila.GenericEqualityComparer<int>());

        void InitOpcodes()
        {
            Opcodes.Add(0x00, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x00, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "nop", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x01, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x01, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "break", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.BREAK });
            Opcodes.Add(0x02, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x02, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldarg.0", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x03, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x03, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldarg.1", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x04, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x04, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldarg.2", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x05, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x05, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldarg.3", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x06, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x06, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldloc.0", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x07, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x07, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldloc.1", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x08, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x08, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldloc.2", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x09, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x09, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldloc.3", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x0A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x0A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stloc.0", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x0B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x0B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stloc.1", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x0C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x0C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stloc.2", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x0D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x0D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stloc.3", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x0E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x0E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldarg.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.ShortInlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x0F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x0F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldarga.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.ShortInlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x10, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x10, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "starg.s", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x11, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x11, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldloc.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.ShortInlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x12, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x12, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldloca.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.ShortInlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x13, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x13, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stloc.s", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x14, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x14, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldnull", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x15, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x15, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.m1", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x16, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x16, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.0", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x17, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x17, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.1", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x18, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x18, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.2", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x19, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x19, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.3", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x1A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x1A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.4", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x1B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x1B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.5", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x1C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x1C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.6", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x1D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x1D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.7", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x1E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x1E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.8", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x1F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x1F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.ShortInlineI, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x20, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x20, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i4", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineI, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x21, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x21, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.i8", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineI8, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x22, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x22, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.r4", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushR4, inline = Assembler.InlineVar.ShortInlineR, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x23, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x23, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldc.r8", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushR8, inline = Assembler.InlineVar.InlineR, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x24, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x24, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x25, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x25, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "dup", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1 + (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x26, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x26, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "pop", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x27, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x27, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "jmp", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineMethod, ctrl = Assembler.ControlFlow.CALL });
            Opcodes.Add(0x28, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x28, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "call", pop = (int)Assembler.PopBehaviour.VarPop, push = (int)Assembler.PushBehaviour.VarPush, inline = Assembler.InlineVar.InlineMethod, ctrl = Assembler.ControlFlow.CALL });
            Opcodes.Add(0x29, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x29, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "calli", pop = (int)Assembler.PopBehaviour.VarPop, push = (int)Assembler.PushBehaviour.VarPush, inline = Assembler.InlineVar.InlineSig, ctrl = Assembler.ControlFlow.CALL });
            Opcodes.Add(0x2A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x2A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ret", pop = (int)Assembler.PopBehaviour.VarPop, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.RETURN });
            Opcodes.Add(0x2B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x2B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "br.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.BRANCH });
            Opcodes.Add(0x2C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x2C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "brfalse.s", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x2D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x2D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "brtrue.s", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x2E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x2E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "beq.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x2F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x2F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bge.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x30, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x30, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bgt.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x31, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x31, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ble.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x32, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x32, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "blt.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x33, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x33, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bne.un.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x34, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x34, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bge.un.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x35, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x35, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bgt.un.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x36, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x36, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ble.un.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x37, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x37, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "blt.un.s", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x38, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x38, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "br", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.BRANCH });
            Opcodes.Add(0x39, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x39, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "brfalse", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x3A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x3A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "brtrue", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x3B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x3B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "beq", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x3C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x3C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bge", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x3D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x3D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bgt", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x3E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x3E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ble", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x3F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x3F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "blt", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x40, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x40, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bne.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x41, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x41, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bge.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x42, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x42, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "bgt.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x43, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x43, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ble.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x44, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x44, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "blt.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x45, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x45, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "switch", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineSwitch, ctrl = Assembler.ControlFlow.COND_BRANCH });
            Opcodes.Add(0x46, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x46, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.i1", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x47, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x47, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.u1", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x48, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x48, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.i2", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x49, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x49, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.u2", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x4A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x4A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.i4", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x4B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x4B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.u4", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x4C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x4C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.i8", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x4D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x4D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.i", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x4E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x4E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.r4", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushR4, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x4F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x4F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.r8", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushR8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x50, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x50, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldind.ref", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x51, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x51, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.ref", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x52, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x52, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.i1", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x53, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x53, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.i2", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x54, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x54, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.i4", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x55, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x55, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.i8", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI8, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x56, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x56, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.r4", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopR4, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x57, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x57, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.r8", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopR8, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x58, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x58, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "add", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x59, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x59, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "sub", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x5A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x5A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "mul", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x5B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x5B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "div", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x5C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x5C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "div.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x5D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x5D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "rem", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x5E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x5E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "rem.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x5F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x5F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "and", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x60, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x60, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "or", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x61, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x61, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "xor", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x62, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x62, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "shl", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x63, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x63, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "shr", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x64, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x64, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "shr.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x65, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x65, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "neg", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x66, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x66, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "not", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x67, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x67, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.i1", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x68, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x68, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.i2", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x69, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x69, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.i4", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x6A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x6A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.i8", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x6B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x6B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.r4", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushR4, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x6C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x6C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.r8", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushR8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x6D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x6D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.u4", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x6E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x6E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.u8", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x6F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x6F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "callvirt", pop = (int)Assembler.PopBehaviour.VarPop, push = (int)Assembler.PushBehaviour.VarPush, inline = Assembler.InlineVar.InlineMethod, ctrl = Assembler.ControlFlow.CALL });
            Opcodes.Add(0x70, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x70, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "cpobj", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x71, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x71, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldobj", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x72, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x72, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldstr", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineString, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x73, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x73, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "newobj", pop = (int)Assembler.PopBehaviour.VarPop, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineMethod, ctrl = Assembler.ControlFlow.CALL });
            Opcodes.Add(0x74, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x74, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "castclass", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x75, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x75, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "isinst", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x76, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x76, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.r.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushR8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x77, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x77, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x78, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x78, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x79, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x79, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unbox", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x7A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x7A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "throw", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.THROW });
            Opcodes.Add(0x7B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x7B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldfld", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineField, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x7C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x7C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldflda", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineField, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x7D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x7D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stfld", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineField, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x7E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x7E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldsfld", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineField, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x7F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x7F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldsflda", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineField, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x80, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x80, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stsfld", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineField, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x81, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x81, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stobj", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x82, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x82, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i1.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x83, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x83, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i2.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x84, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x84, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i4.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x85, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x85, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i8.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x86, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x86, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u1.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x87, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x87, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u2.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x88, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x88, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u4.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x89, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x89, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u8.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x8A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x8A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x8B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x8B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u.un", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x8C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x8C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "box", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x8D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x8D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "newarr", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x8E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x8E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldlen", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x8F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x8F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelema", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x90, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x90, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.i1", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x91, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x91, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.u1", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x92, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x92, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.i2", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x93, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x93, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.u2", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x94, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x94, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.i4", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x95, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x95, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.u4", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x96, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x96, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.i8", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x97, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x97, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.i", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x98, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x98, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.r4", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushR4, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x99, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x99, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.r8", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushR8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x9A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x9A, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem.ref", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushRef, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x9B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x9B, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.i", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x9C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x9C, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.i1", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x9D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x9D, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.i2", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x9E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x9E, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.i4", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0x9F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0x9F, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.i8", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI8, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA0, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA0, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.r4", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopR4, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA1, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA1, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.r8", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopR8, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA2, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA2, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem.ref", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA3, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA3, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldelem", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA4, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA4, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stelem", pop = (int)Assembler.PopBehaviour.PopRef + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA5, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA5, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unbox.any", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA6, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA6, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA7, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA7, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA8, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA8, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xA9, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xA9, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xAA, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xAA, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xAB, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xAB, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xAC, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xAC, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xAD, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xAD, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xAE, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xAE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xAF, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xAF, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB0, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB0, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB1, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB1, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB2, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB2, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB3, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB3, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i1", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB4, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB4, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u1", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB5, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB5, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i2", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB6, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB6, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u2", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB7, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB7, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i4", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB8, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB8, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u4", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xB9, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xB9, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i8", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xBA, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xBA, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u8", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xBB, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xBB, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xBC, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xBC, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xBD, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xBD, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xBE, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xBE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xBF, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xBF, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC0, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC0, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC1, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC1, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC2, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC2, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "refanyval", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC3, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC3, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ckfinite", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushR8, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC4, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC4, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC5, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC5, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC6, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC6, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "mkrefany", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC7, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC7, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC8, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC8, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xC9, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xC9, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xCA, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xCA, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xCB, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xCB, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xCC, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xCC, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xCD, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xCD, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xCE, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xCE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xCF, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xCF, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD0, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD0, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "ldtoken", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineTok, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD1, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD1, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.u2", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD2, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD2, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.u1", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD3, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD3, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.i", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD4, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD4, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.i", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD5, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD5, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.ovf.u", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD6, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD6, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "add.ovf", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD7, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD7, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "add.ovf.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD8, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD8, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "mul.ovf", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xD9, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xD9, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "mul.ovf.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xDA, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xDA, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "sub.ovf", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xDB, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xDB, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "sub.ovf.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xDC, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xDC, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "endfinally", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.RETURN });
            Opcodes.Add(0xDD, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xDD, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "leave", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineBrTarget, ctrl = Assembler.ControlFlow.BRANCH, directly_modifies_stack = true });
            Opcodes.Add(0xDE, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xDE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "leave.s", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineBrTarget, ctrl = Assembler.ControlFlow.BRANCH, directly_modifies_stack = true });
            Opcodes.Add(0xDF, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xDF, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "stind.i", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE0, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE0, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "conv.u", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE1, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE1, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE2, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE2, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE3, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE3, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE4, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE4, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE5, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE5, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE6, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE6, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE7, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE7, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE8, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE8, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xE9, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xE9, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xEA, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xEA, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xEB, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xEB, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xEC, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xEC, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xED, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xED, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xEE, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xEE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xEF, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xEF, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF0, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF0, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF1, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF1, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF2, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF2, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF3, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF3, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF4, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF4, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF5, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF5, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF6, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF6, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF7, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF7, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xF8, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF8, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix7", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xF9, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xF9, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix6", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFA, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFA, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix5", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFB, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFB, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix4", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFC, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFC, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix3", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFD, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFD, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix2", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFE, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefix1", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFF, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFF, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "prefixref", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFE00, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x00, name = "arglist", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE01, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x01, name = "ceq", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE02, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x02, name = "cgt", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE03, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x03, name = "cgt.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE04, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x04, name = "clt", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE05, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x05, name = "clt.un", pop = (int)Assembler.PopBehaviour.Pop1 + (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE06, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x06, name = "ldftn", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineMethod, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE07, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x07, name = "ldvirtftn", pop = (int)Assembler.PopBehaviour.PopRef, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineMethod, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE08, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x08, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE09, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x09, name = "ldarg", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE0A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x0A, name = "ldarga", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE0B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x0B, name = "starg", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE0C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x0C, name = "ldloc", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push1, inline = Assembler.InlineVar.InlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE0D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x0D, name = "ldloca", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE0E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x0E, name = "stloc", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineVar, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE0F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x0F, name = "localloc", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE10, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x10, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE11, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x11, name = "endfilter", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.RETURN });
            Opcodes.Add(0xFE12, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x12, name = "unaligned.", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.ShortInlineI, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFE13, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x13, name = "volatile.", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFE14, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x14, name = "tail.", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.META });
            Opcodes.Add(0xFE15, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x15, name = "initobj", pop = (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE16, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x16, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE17, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x17, name = "cpblk", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE18, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x18, name = "initblk", pop = (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI + (int)Assembler.PopBehaviour.PopI, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE19, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x19, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE1A, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x1A, name = "rethrow", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.THROW });
            Opcodes.Add(0xFE1B, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x1B, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE1C, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x1C, name = "sizeof", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineType, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE1D, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x1D, name = "refanytype", pop = (int)Assembler.PopBehaviour.Pop1, push = (int)Assembler.PushBehaviour.PushI, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE1E, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x1E, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE1F, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x1F, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE20, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x20, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE21, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x21, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });
            Opcodes.Add(0xFE22, new Assembler.Opcode { opcode1 = (Assembler.SingleOpcodes)0xFE, opcode2 = (Assembler.DoubleOpcodes)0x22, name = "unused", pop = (int)Assembler.PopBehaviour.Pop0, push = (int)Assembler.PushBehaviour.Push0, inline = Assembler.InlineVar.InlineNone, ctrl = Assembler.ControlFlow.NEXT });

            // The following are tysila dependent
            Opcodes.Add(0xFD20, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.flip,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "flip",
                directly_modifies_stack = true
            });
            Opcodes.Add(0xFD21, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.flip3,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "flip3",
                directly_modifies_stack = true
            });
            Opcodes.Add(0xfd22, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.init_rth,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "init_rth"
            });
            Opcodes.Add(0xfd23, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.castclassex,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "castclassex",
                directly_modifies_stack = true
            });
            Opcodes.Add(0xfd24, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.throwfalse,
                pop = (int)PopBehaviour.PopI,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "throwfalse"
            });
            Opcodes.Add(0xfd25, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.ldelem_vt,
                pop = (int)PopBehaviour.Pop1 + (int)PopBehaviour.Pop1,
                push = (int)PushBehaviour.Push1,
                ctrl = ControlFlow.NEXT,
                name = "ldelem_vt"
            });
            Opcodes.Add(0xfd26, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.init_rmh,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "init_mth"
            });
            Opcodes.Add(0xfd27, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.init_rfh,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "init_rfh"
            });
            Opcodes.Add(0xfd28, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.stelem_vt,
                pop = (int)PopBehaviour.Pop1 + (int)PopBehaviour.Pop1 + (int)PopBehaviour.Pop1,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "stelem_vt"
            });
            Opcodes.Add(0xfd29, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.profile,
                pop = (int)PopBehaviour.PopI,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "profile"
            });
            Opcodes.Add(0xfd2a, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.gcmalloc,
                pop = (int)PopBehaviour.Pop1,
                push = (int)PushBehaviour.PushI,
                ctrl = ControlFlow.NEXT,
                name = "gcmalloc"
            });
            Opcodes.Add(0xfd2b, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.ldobj_addr,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.PushI,
                ctrl = ControlFlow.NEXT,
                name = "ldobj_addr"
            });
            Opcodes.Add(0xfd2c, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.mbstrlen,
                pop = (int)PopBehaviour.Pop1,
                push = (int)PushBehaviour.PushI,
                ctrl = ControlFlow.NEXT,
                name = "mbstrlen"
            });
            Opcodes.Add(0xfd2d, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.loadcatchobj,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.PushI,
                ctrl = ControlFlow.NEXT,
                name = "loadcatchobj"
            });
            Opcodes.Add(0xfd2e, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.instruction_label,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "instruction_label"
            });
            Opcodes.Add(0xfd2f, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.pushback,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                directly_modifies_stack = true,
                ctrl = ControlFlow.NEXT,
                name = "pushback"
            });
            Opcodes.Add(0xfd30, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.throwtrue,
                pop = (int)PopBehaviour.PopI,
                push = (int)PushBehaviour.Push0,
                ctrl = ControlFlow.NEXT,
                name = "throwtrue"
            });
            Opcodes.Add(0xfd31, new Opcode
            {
                opcode1 = SingleOpcodes.tysila,
                opcode2 = DoubleOpcodes.bringforward,
                pop = (int)PopBehaviour.Pop0,
                push = (int)PushBehaviour.Push0,
                directly_modifies_stack = true,
                ctrl = ControlFlow.NEXT,
                name = "bringforward"
            });
        }
    }
}
