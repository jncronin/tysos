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

namespace libtysila
{
    partial class Assembler
    {
        protected virtual void ChooseCallInstruction(List<tybel.Node> ret, timple.TimpleCallNode inst, ref int next_var, IList<libasm.hardware_location> las)
        {
            CallConv cc = call_convs[inst.CallConv](new Assembler.MethodToCompile { msig = inst.MethSig }, CallConv.StackPOV.Caller, this, new ThreeAddressCode(inst.Op));

            List<vara> isect_list = new List<vara>();
            foreach(libasm.hardware_location isect in cc.CallerPreservesLocations)
                isect_list.Add(vara.MachineReg(isect));

            tybel.SpecialNode save_node = new tybel.SpecialNode { Type = tybel.SpecialNode.SpecialNodeType.SaveLiveIntersect, VarList = isect_list };
            ret.Add(save_node);
            if (cc.StackSpaceUsed != 0)
                ret.AddRange(SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.adjstack, vara.Void(), vara.Const(-cc.StackSpaceUsed, CliType.native_int), vara.Void()), ref next_var, las));

            for(int i = 0; i < cc.Arguments.Count; i++)
            {
                CallConv.ArgumentLocation arg = cc.Arguments[i];
                ThreeAddressCode.Op assign_op = ThreeAddressCode.Op.invalid;
                switch (arg.ValueSize)
                {
                    case 4:
                        assign_op = ThreeAddressCode.Op.assign_i4;
                        break;

                    case 8:
                        assign_op = ThreeAddressCode.Op.assign_i8;
                        break;

                    default:
                        assign_op = ThreeAddressCode.Op.assign_vt;
                        break;
                }

                ret.AddRange(SelectInstruction(new timple.TimpleNode(assign_op, vara.MachineReg(arg.ValueLocation), inst.VarArgs[i], vara.Void()), ref next_var, las));
            }
            ret.AddRange(SelectInstruction(new timple.TimpleNode(cc.CallTac, inst.R, inst.O1, vara.Void()), ref next_var, las));

            if (cc.StackSpaceUsed != 0)
                ret.AddRange(SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.adjstack, vara.Void(), vara.Const(cc.StackSpaceUsed, CliType.native_int), vara.Void()), ref next_var, las));

            ret.Add(new tybel.SpecialNode { Type = tybel.SpecialNode.SpecialNodeType.Restore, VarList = isect_list, SaveNode = save_node });
            save_node.SaveNode = ret[ret.Count - 1];
        }
    }
}
