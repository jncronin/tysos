/* Copyright (C) 2013 by John Cronin
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

namespace libtysila.arm
{
    partial class arm_Assembler
    {
        IEnumerable<OutputBlock> arm_ret_i4_gpr(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            List<OutputBlock> ret = new List<OutputBlock>();

            /* Restore stack frame:
             * 
             * mov sp, fp
             * pop fp
             */
            ret.Add(EncDPROpcode(cond.Always, 0x1a, R0, SP, 0, 0, FP));
            ret.Add(EncSingleRegListOpcode(cond.Always, 0x9, SP, FP));


            // At the start of a function we do push LR, therefore to return we simply do pop PC
            ret.Add(EncSingleRegListOpcode(cond.Always, 0x9, SP, PC));

            return ret;
        }

        IEnumerable<OutputBlock> arm_endfinally(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // Before a br_ehclause we do push LR, therefore to return we simply do pop PC
            return OBList(EncSingleRegListOpcode(cond.Always, 0x9, SP, PC));
        }

        IEnumerable<OutputBlock> arm_enter(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            // pushing LR is done in ArchSpecificProlog
            return new List<OutputBlock>();
        }

        IEnumerable<OutputBlock> arm_call(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            CallEx ce = tac as CallEx;
            CallConv cc = ce.call_conv;

            List<OutputBlock> ret = new List<OutputBlock>();

            // Determine how to most efficiently move the arguments into the registers

            // First, create a mapping of from/to locations
            Dictionary<hardware_location, hardware_location> from_to_map = new Dictionary<hardware_location, hardware_location>();
            for (int i = 0; i < ce.Var_Args.Length; i++)
            {
                hardware_location from;
                switch (ce.Var_Args[i].type)
                {
                    case var.var_type.LogicalVar:
                        from = state.var_hlocs.GetLocationOf(ce.Var_Args[i]);
                        break;
                    case var.var_type.ContentsOf:
                    case var.var_type.ContentsOfPlusConstant:
                        from = new hardware_contentsof
                        {
                            base_loc = state.var_hlocs.GetLocationOf(ce.Var_Args[i].base_var.v),
                            const_offset = ce.Var_Args[i].constant_offset,
                            size = ce.Var_Args[i].v_size
                        };
                        break;
                    case var.var_type.Const:
                        from = new const_location { c = ce.Var_Args[i].constant_val };
                        break;
                    case var.var_type.LocalVar:
                        from = state.local_vars[ce.Var_Args[i].local_var];
                        break;
                    case var.var_type.AddressOf:
                    case var.var_type.AddressOfPlusConstant:
                        {
                            var bloc = ce.Var_Args[i].base_var.v;

                            switch (bloc.type)
                            {
                                case var.var_type.Label:
                                    from = new hardware_addressoflabel { const_offset = ce.Var_Args[i].constant_offset, label = bloc.label };
                                    break;
                                case var.var_type.LocalVar:
                                    from = new hardware_addressof { base_loc = state.local_vars[bloc.local_var] };
                                    break;
                                case var.var_type.LogicalVar:
                                    from = new hardware_addressof { base_loc = state.var_hlocs.GetLocationOf(bloc) };
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }

                //hardware_location from = state.var_hlocs.GetLocationOf(ce.Var_Args[i]);
                hardware_location to = cc.Arguments[i].ValueLocation;

                from_to_map.Add(from, to);
            }

            // Do a simple move of registers as required as long as we don't overwrite an in use 'from' register
            int changes_made = 0;
            int iterations = 0;

            while (iterations < 2)
            {
                do
                {
                    changes_made = 0;
                    List<hardware_location> from_list = new List<hardware_location>(from_to_map.Keys);
                    List<hardware_location> from_list_removed = new List<hardware_location>();

                    int i = 0;
                    while (i < from_list.Count)
                    {
                        hardware_location from = from_list[i];
                        hardware_location to = from_to_map[from];

                        // if the argument is already in the appropriate location, then remove it from the to do list
                        if (from.Equals(to))
                        {
                            from_list.RemoveAt(i);
                            from_to_map.Remove(from);
                            changes_made++;
                            continue;
                        }

                        // try and move 'from' to 'to' if from_list does not contain 'to'
                        if (!from_to_map.ContainsKey(to))
                        {
                            arm_assign(to, from, ret, state);
                            from_list.RemoveAt(i);
                            from_to_map.Remove(from);
                            changes_made++;
                            continue;
                        }

                        i++;
                    }
                } while (changes_made != 0);

                // Success?
                if (from_to_map.Count == 0)
                    break;

                // Do a more complicated version - try moving a random entry to scratch
                iterations++;
                List<hardware_location> old_froms = new List<hardware_location>(from_to_map.Keys);
                hardware_location old_from = old_froms[0];
                hardware_location old_to = from_to_map[old_from];
                arm_assign(SCRATCH, old_from, ret, state);
                from_to_map.Remove(old_from);
                from_to_map[SCRATCH] = old_to;
            }

            if (from_to_map.Count > 0)
                throw new Exception("Unable to coerce call arguments");

            // Do the call
            if ((op1.type == var.var_type.AddressOf) && (op1.base_var.v.type == var.var_type.Label))
            {
                string target = op1.base_var.v.label;
                ret.Add(new RelocationBlock { RelType = 28, Target = target, Size = 3, Value = -2 });       // R_ARM_CALL (addend is shifted by 2)
                ret.Add(new CodeBlock { Code = new byte[] { 0xeb } });
            }
            else if (op1.type == var.var_type.LogicalVar)
            {
                ret.Add(EncDPROpcode(cond.Always, 0x12, PC, PC, 0x1e, 0x3, op1.hardware_loc));
            }
            else
                throw new NotSupportedException();


            return ret;
        }
    }
}