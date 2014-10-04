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
        protected virtual void ChooseCallInstruction(List<tybel.Node> ret, timple.TimpleCallNode inst, ref int next_var, ref int next_block,
            IList<libasm.hardware_location> las, IList<libasm.hardware_location> lvs)
        {
            CallConv cc = call_convs[inst.CallConv](new Assembler.MethodToCompile { msig = inst.MethSig }, CallConv.StackPOV.Caller, this);

            List<vara> isect_list = new List<vara>();
            foreach(libasm.hardware_location isect in cc.CallerPreservesLocations)
                isect_list.Add(vara.MachineReg(isect));

            tybel.SpecialNode save_node = new tybel.SpecialNode { Type = tybel.SpecialNode.SpecialNodeType.SaveLiveIntersect, VarList = isect_list };

            if (inst.MethSig.Returns)
                ret.Add(save_node);

            if (cc.StackSpaceUsed != 0)
                ret.AddRange(SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.adjstack), vara.Void(), vara.Const(-cc.StackSpaceUsed, CliType.native_int), vara.Void()), ref next_var, ref next_block, las, lvs));

            for(int i = 0; i < cc.Arguments.Count; i++)
            {
                CallConv.ArgumentLocation arg = cc.Arguments[i];
                ThreeAddressCode.Op assign_op = ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.invalid);
                switch (arg.ValueSize)
                {
                    case 4:
                        assign_op = ThreeAddressCode.Op.OpI4(ThreeAddressCode.OpName.assign);
                        break;

                    case 8:
                        assign_op = ThreeAddressCode.Op.OpI8(ThreeAddressCode.OpName.assign);
                        break;

                    default:
                        assign_op = ThreeAddressCode.Op.OpVT(ThreeAddressCode.OpName.assign, inst.MethSig.Params[i]);
                        break;
                }

                timple.TimpleNode assign_inst = new timple.TimpleNode(assign_op, vara.MachineReg(arg.ValueLocation, inst.VarArgs[i].DataType), inst.VarArgs[i], vara.Void());
                IList<tybel.Node> assign_ops = SelectInstruction(assign_inst, ref next_var, ref next_block, las, lvs);
                if (assign_ops == null)
                {
                    List<timple.TreeNode> rewrite_insts = tybel.Tybel.RewriteInst(assign_inst, ref next_var);
                    foreach (timple.TreeNode rewrite_inst in rewrite_insts)
                    {
                        IList<tybel.Node> rewrite_ops = SelectInstruction(rewrite_inst, ref next_var, ref next_block, las, lvs);
                        if (rewrite_ops == null)
                            throw new Exception("Cannot encode call");
                        ret.AddRange(rewrite_ops);
                    }
                }
                else
                    ret.AddRange(assign_ops);
            }
            //ret.AddRange(SelectInstruction(new timple.TimpleNode(cc.CallTac, inst.R, inst.O1, vara.Void()), ref next_var, ref next_block, las, lvs));

            if (inst.MethSig.Returns)
            {
                if (cc.StackSpaceUsed != 0)
                    ret.AddRange(SelectInstruction(new timple.TimpleNode(ThreeAddressCode.Op.OpVoid(ThreeAddressCode.OpName.adjstack), vara.Void(), vara.Const(cc.StackSpaceUsed, CliType.native_int), vara.Void()), ref next_var, ref next_block, las, lvs));

                ret.Add(new tybel.SpecialNode { Type = tybel.SpecialNode.SpecialNodeType.Restore, VarList = isect_list, SaveNode = save_node });
                save_node.SaveNode = ret[ret.Count - 1];
            }
        }
    }
}
