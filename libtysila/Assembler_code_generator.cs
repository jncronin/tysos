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
using libasm;

namespace libtysila
{
    partial class Assembler
    {
        internal delegate IEnumerable<OutputBlock> code_emitter(ThreeAddressCode.Op op, var result, var op1,
            var op2, ThreeAddressCode tac, AssemblerState state);

        internal class opcode_choice
        {
            public code_emitter code_emitter;
            public hloc_constraint result, op1, op2;
            public bool swap_ops = false;
            public hardware_location[] clobber_list;
        }

        internal class output_opcode
        {
            public ThreeAddressCode.Op op;
            public opcode_choice[] opcode_choice;

            public hardware_location recommended_O1, recommended_O2, recommended_R;
            public List<hardware_location> not_recommended_O1, not_recommended_O2, not_recommended_R;
        }

        internal class opcode_match
        {
            public enum match_quality { Full, Partial, None };
            public int QualityCount;
            public match_quality Quality;
            public bool ResultMatch;
            public bool Op1Match;
            public bool Op2Match;
            public opcode_choice Match;
        }

        internal Dictionary<ThreeAddressCode.Op, output_opcode> output_opcodes = null;
        internal Dictionary<string, output_opcode> misc_opcodes = null;

        protected void InitArchOpcodes()
        {
            output_opcodes = new Dictionary<ThreeAddressCode.Op, output_opcode>(new ThreeAddressCode_OpEqualityComparer());
            misc_opcodes = new Dictionary<string, output_opcode>();

            output_opcodes.Add(ThreeAddressCode.Op.label, new output_opcode
            {
                op = ThreeAddressCode.Op.label,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = null_emit,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.None },
                        op2 = new hloc_constraint { constraint = hloc_constraint.c_.None },
                        result = new hloc_constraint { constraint = hloc_constraint.c_.None },
                    }
                }
            });
            output_opcodes.Add(ThreeAddressCode.Op.loc_label, output_opcodes[ThreeAddressCode.Op.label]);

            output_opcodes.Add(ThreeAddressCode.Op.instruction_label, new output_opcode
            {
                op = ThreeAddressCode.Op.instruction_label,
                opcode_choice = new opcode_choice[] {
                    new opcode_choice { code_emitter = instruction_label_emit,
                        op1 = new hloc_constraint { constraint = hloc_constraint.c_.None },
                        op2 = new hloc_constraint { constraint = hloc_constraint.c_.None },
                        result = new hloc_constraint { constraint = hloc_constraint.c_.None },
                    }
                }
            });

            arch_init_opcodes();
        }

        internal IEnumerable<OutputBlock> null_emit(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return new List<OutputBlock>(); }

        internal IEnumerable<OutputBlock> instruction_label_emit(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        { return new List<OutputBlock>() { ((InstructionLabelEx)tac).instr }; }

        internal virtual IEnumerable<OutputBlock> arch_misc_emit(ThreeAddressCode.Op op, var result, var op1, var op2,
            MiscEx tac, AssemblerState state)
        {
            return null;
        }

        internal IEnumerable<OutputBlock> misc_emit(ThreeAddressCode.Op op, var result, var op1, var op2,
            ThreeAddressCode tac, AssemblerState state)
        {
            MiscEx me = tac as MiscEx;
            if(me == null)
                throw new NotSupportedException("misc opcode without MiscEx object");

            IEnumerable<OutputBlock> ret = arch_misc_emit(op, result, op1, op2, me, state);
            if (ret != null)
                return ret;

            throw new NotSupportedException("misc opcode: " + me.Name + " not supported");
        }

        internal Dictionary<int, hloc_constraint> GeneratePreferredLocations(List<ThreeAddressCode> ir,
            Dictionary<int, var_semantic> semantics)
        {
            Dictionary<int, hloc_constraint> ret = new Dictionary<int, hloc_constraint>();

            foreach (ThreeAddressCode inst in ir)
            {
                output_opcode op;
                try
                {
                    op = GetOutputOpcode(inst);
                }
                catch (KeyNotFoundException)
                {
                    throw new Exception(this.GetType().Name + " does not implement " + inst.ToString());
                }

                List<opcode_match> matches = OpcodeMatchesFromSemantics(op, inst.Result, inst.Operand1, inst.Operand2,
                    semantics);

                bool has_match = false;
                int best_quality = -1;
                opcode_match best_match = new opcode_match();

                foreach (opcode_match om in matches)
                {
                    if (om.Quality == opcode_match.match_quality.Full)
                    {
                        has_match = true;
                        best_match = om;
                        break;
                    }
                    if (om.QualityCount > best_quality)
                    {
                        // Don't try and contrain a variable to a constant
                        if ((om.Op1Match || (om.Match.op1.constraint != hloc_constraint.c_.Immediate)) &&
                            (om.Op2Match || (om.Match.op2.constraint != hloc_constraint.c_.Immediate)) &&
                            (om.ResultMatch || (om.Match.result.constraint != hloc_constraint.c_.Immediate)))
                        {
                            best_quality = om.QualityCount;
                            best_match = om;
                        }
                    }
                }
                if (!has_match)
                {
                    if (best_quality < 0)
                        continue;

                    // we have a partial match, therefore need to coerce various variables to a particular
                    //  hardware location
                    hloc_constraint rcons = best_match.Match.result;
                    if (best_match.Match.result.constraint == hloc_constraint.c_.Operand1)
                        rcons = best_match.Match.op1;
                    if (best_match.Match.result.constraint == hloc_constraint.c_.Operand2)
                        rcons = best_match.Match.op2;

                    if (!best_match.ResultMatch && !ret.ContainsKey(inst.Result.logical_var) && (inst.Result.type == var.var_type.LogicalVar))
                        ret.Add(inst.Result.logical_var, rcons);
                    if (!best_match.Op1Match && !ret.ContainsKey(inst.Operand1.logical_var) && (inst.Operand1.type == var.var_type.LogicalVar))
                        ret.Add(inst.Operand1.logical_var, best_match.Match.op1);
                    if (!best_match.Op2Match && !ret.ContainsKey(inst.Operand2.logical_var) && (inst.Operand2.type == var.var_type.LogicalVar))
                        ret.Add(inst.Operand2.logical_var, best_match.Match.op2);
                }
            }

            return ret;
        }

        internal var_semantic GetSemantic(var v, Dictionary<int, var_semantic> semantics, cfg_node node)
        {
            if (v.type == var.var_type.LogicalVar)
                return semantics[v.logical_var];
            else if (v.type == var.var_type.Const)
            {
                if ((v.constant_val.GetType() == typeof(Int64)) ||
                    (v.constant_val.GetType() == typeof(UInt64)))
                    return new var_semantic { needs_int64 = true };
                else if ((v.constant_val.GetType() == typeof(Int32)) ||
                    (v.constant_val.GetType() == typeof(UInt32)) ||
                    (v.constant_val.GetType() == typeof(Int16)) ||
                    (v.constant_val.GetType() == typeof(UInt16)) ||
                    (v.constant_val.GetType() == typeof(Char)) ||
                    (v.constant_val.GetType() == typeof(Byte)) ||
                    (v.constant_val.GetType() == typeof(SByte)))
                    return new var_semantic { needs_int32 = true };
                else if ((v.constant_val.GetType() == typeof(IntPtr)) ||
                    (v.constant_val.GetType() == typeof(UIntPtr)))
                    return new var_semantic { needs_intptr = true };
                else if (v.constant_val.GetType() == typeof(Double))
                    return new var_semantic { needs_float64 = true };
                else if (v.constant_val.GetType() == typeof(Single))
                    return new var_semantic { needs_float32 = true };
                else if (v.constant_val.GetType() == typeof(var.SpecialConstValue))
                {
                    var.SpecialConstValue scv = v.constant_val as var.SpecialConstValue;
                    switch (scv.Type)
                    {
                        case var.SpecialConstValue.SpecialConstValueType.UsedStackSize:
                            return new var_semantic { needs_int32 = true };
                        default:
                            throw new NotSupportedException("SpecialConstValueType: " + scv.Type.ToString() + " is not supported in Assembler.GetSemantic()");
                    }
                }
                else
                    return new var_semantic { needs_vtype = true };
            }
            else if (v.type == var.var_type.LocalArg)
            {
                CliType ct = node.la_after[v.local_arg].type.CliType(this);
                int vt_size = GetSizeOf(node.la_after[v.local_arg].type);
                return GetSemantic(ct, vt_size);
            }
            else if (v.type == var.var_type.LocalVar)
            {
                CliType ct = node.lv_after[v.local_var].type.CliType(this);
                int vt_size = GetSizeOf(node.lv_after[v.local_var].type);
                return GetSemantic(ct, vt_size);
            }
            else if ((v.type == var.var_type.ContentsOf) || (v.type == var.var_type.ContentsOfPlusConstant))
            {
                if (v.v_size == 8)
                    return new var_semantic { needs_int64 = true };
                else if (v.v_size == 4)
                    return new var_semantic { needs_int32 = true };
                else
                    return new var_semantic { needs_intptr = true };
            }
            else
                return new var_semantic { needs_intptr = true };
        }

        internal List<OutputBlock> GenerateCode(List<cfg_node> nodes, RegisterGraph var_locations, AssemblerState state, Dictionary<int, var_semantic> semantics, ref bool changes_made,
            List<ThreeAddressCode> faulting_instructions, MethodToCompile mtc)
        {
            // Produce the code for a method

            List<OutputBlock> ret = new List<OutputBlock>();

            foreach (cfg_node node in nodes)
            {
                int i = 0;

                if (node.optimized_ir != null)
                {
                    while (i < node.optimized_ir.Count)
                    {
                        ThreeAddressCode inst = node.optimized_ir[i];

                        // insert a block as a label if requested
                        if (inst is LabelEx)
                            ret.Add(new NodeHeader { block_id = ((LabelEx)inst).Block_id });

                        // Build hardware constriant equivalents of the variables
                        hloc_constraint O1, O2, R;
                        O1 = BuildHlocFromVar(inst.Operand1, var_locations, state);
                        O2 = BuildHlocFromVar(inst.Operand2, var_locations, state);
                        R = BuildHlocFromVar(inst.Result, var_locations, state);

                        if (inst is CallEx)
                        {
                            CallEx ce = inst as CallEx;
                            for(int j = 0; j < ce.Var_Args.Length; j++)
                            {
                                hloc_constraint ce_var_loc = BuildHlocFromVar(ce.Var_Args[j], var_locations, state);
                                if (!ce_var_loc.IsSpecificOrConst)
                                    throw new NotSupportedException();
                                ce.Var_Args[j].hardware_loc = ce_var_loc.specific;
                            }
                        }

                        // Choose an instruction to use
                        output_opcode op = GetOutputOpcode(inst);
                        List<opcode_match> oms = OpcodeMatchesFromHloc(op, R, O1, O2);
                        opcode_match om = GetFullMatch(oms);

                        if (om == null)
                        {
                            // Determine the best match
                            int best_match_quality = -1;
                            opcode_match best_match = null;

                            foreach (opcode_match om_try in oms)
                            {
                                if (om_try.QualityCount > best_match_quality)
                                {
                                    // Fail if we select an opcode where we have to coerce to an immediate
                                    if ((om_try.Op1Match == false) && (om_try.Match.op1.constraint == hloc_constraint.c_.Immediate))
                                        continue;
                                    if ((om_try.Op2Match == false) && (om_try.Match.op2.constraint == hloc_constraint.c_.Immediate))
                                        continue;
                                    best_match_quality = om_try.QualityCount;
                                    best_match = om_try;
                                }
                            }

                            if (best_match == null)
                                throw new Exception("No possible matches");

                            // insert coercion code
                            int coercion_lines = 1;
                            if (!best_match.Op1Match)
                            {
                                var intermediate = state.next_variable++;

                                node.optimized_ir.Insert(i, new ThreeAddressCode(GetAssignTac(GetSemantic(inst.Operand1, semantics, node)), intermediate, inst.Operand1, var.Null,
                                    GetSemantic(inst.Operand1, semantics, node).vtype_size));
                                inst.Operand1 = intermediate;
                                inst.CoercionCount++;
                                changes_made = true;
                                coercion_lines++;
                            }
                            if (!best_match.Op2Match)
                            {
                                var intermediate = state.next_variable++;

                                node.optimized_ir.Insert(i, new ThreeAddressCode(GetAssignTac(GetSemantic(inst.Operand2, semantics, node)), intermediate, inst.Operand2, var.Null,
                                    GetSemantic(inst.Operand2, semantics, node).vtype_size));
                                inst.Operand2 = intermediate;
                                inst.CoercionCount++;
                                changes_made = true;
                                coercion_lines++;
                            }
                            if (!best_match.ResultMatch)
                            {
                                var intermediate = state.next_variable++;

                                ThreeAddressCode.Op assign_tac = GetAssignTac(inst.GetResultType());
                                int vt_size = (inst.VTSize.HasValue) ? inst.VTSize.Value : 0;

                                node.optimized_ir.Insert(i + coercion_lines, new ThreeAddressCode(assign_tac, inst.Result, intermediate, var.Null, vt_size));
                                inst.Result = intermediate;
                                inst.CoercionCount++;
                                changes_made = true;
                                coercion_lines++;
                            }

                            if (inst.CoercionCount > maxCoercionCountPerInstruction)
                                throw new TooManyCoercionsException(mtc, inst);

                            if (changes_made && (faulting_instructions != null))
                                faulting_instructions.Add(inst);

                            // Skip on past the current instruction
                            i += coercion_lines;
                            continue;
                        }

                        inst.Result.hardware_loc = R.specific;
                        inst.Operand1.hardware_loc = O1.specific;
                        inst.Operand2.hardware_loc = O2.specific;

                        state.UsedLocations.Add(inst.Result.hardware_loc);

                        // Now output the actual code

                        // store clobbered registers
                        List<hardware_location> cur_hlocs = new List<hardware_location>();
                        IEnumerable<var> clobberable_vars = util.Intersect<var>(inst.live_vars, inst.live_vars_after);
                        foreach(var v in clobberable_vars)
                            cur_hlocs.Add(var_locations.graph[v].hloc);

                        if (inst.requires_used_locations_list)
                        {
                            IEnumerable<var> used_locs = util.Union<var>(inst.live_vars, inst.live_vars_after);
                            inst.used_locations = new List<hardware_location>();
                            inst.used_var_locations = new Dictionary<int, hardware_location>();
                            foreach (var v in used_locs)
                            {
                                inst.used_locations.Add(var_locations.graph[v].hloc);
                                inst.used_var_locations.Add(v, var_locations.graph[v].hloc);
                            }
                        }
                        IEnumerable<hardware_location> clobbered = util.Intersect<hardware_location>(cur_hlocs, om.Match.clobber_list);
                        List<hardware_location> clobbered_list = null;
                        if (clobbered is List<hardware_location>)
                            clobbered_list = clobbered as List<hardware_location>;
                        else
                            clobbered_list = new List<hardware_location>(clobbered);

                        foreach (hardware_location clobbered_hloc in clobbered_list)
                            ret.Add(new CodeBlock(SaveLocation(clobbered_hloc), new CodeBlock.CompiledInstruction("save " + clobbered_hloc.ToString())));

                        // Interpret special const codes
                        InterpretSpecialConstCode(ref inst.Result, state);
                        InterpretSpecialConstCode(ref inst.Operand1, state);
                        InterpretSpecialConstCode(ref inst.Operand2, state);
                        if (inst is CallEx)
                        {
                            CallEx ce = inst as CallEx;
                            for(int x = 0; x < ce.Var_Args.Length; x++)
                                InterpretSpecialConstCode(ref ce.Var_Args[x], state);
                        }

                        // Ensure its not an opcode of type A = A
                        bool skip = false;
                        if ((inst.Operator == ThreeAddressCode.Op.assign_i) || (inst.Operator == ThreeAddressCode.Op.assign_i4) || (inst.Operator == ThreeAddressCode.Op.assign_i8) ||
                            (inst.Operator == ThreeAddressCode.Op.assign_r4) || (inst.Operator == ThreeAddressCode.Op.assign_r8))
                        {
                            if (inst.Result.hardware_loc.Equals(inst.Operand1.hardware_loc))
                                skip = true;
                        }

                        // Output the code itself
                        if (!skip)
                        {
                            ret.AddRange(om.Match.code_emitter(inst.Operator, inst.Result, inst.Operand1, inst.Operand2,
                                inst, state));
                        }

                        // restore clobbered registers
                        for (int j = clobbered_list.Count - 1; j >= 0; j--)
                            ret.Add(new CodeBlock(RestoreLocation(clobbered_list[j]), new CodeBlock.CompiledInstruction("restore " + clobbered_list[j].ToString())));

                        i++;
                    }
                }
            }

            return ret;
        }

        private void InterpretSpecialConstCode(ref var var, AssemblerState state)
        {
            if (var.constant_val is var.SpecialConstValue)
            {
                var.SpecialConstValue scv = var.constant_val as var.SpecialConstValue;

                switch (scv.Type)
                {
                    case libtysila.var.SpecialConstValue.SpecialConstValueType.UsedStackSize:
                        var.constant_val = state.stack_space_used;
                        break;
                }
            }
        }

        private hardware_location is_nested(hloc_constraint h)
        {
            // return true for contents/addresses of things which aren't registers

            if (h.IsSpecific)
            {
                if (h.specific is hardware_addressof)
                {
                    hardware_addressof a = h.specific as hardware_addressof;
                    if ((a.base_loc is hardware_addressof) ||
                        (a.base_loc is hardware_contentsof) ||
                        (a.base_loc is hardware_stackloc))
                        return a.base_loc;
                }
                else if (h.specific is hardware_contentsof)
                {
                    hardware_contentsof c = h.specific as hardware_contentsof;
                    if ((c.base_loc is hardware_addressof) ||
                        (c.base_loc is hardware_contentsof) ||
                        (c.base_loc is hardware_stackloc))
                        return c.base_loc;
                }
            }
            return null;
        }

        /*private opcode_match GetBestMatch(List<opcode_match> oms, List<hardware_location> clobbered,
            List<OutputBlock> coercion, List<OutputBlock> post_coercion, List<hardware_location> temp_locs,
            ref hloc_constraint R, ref hloc_constraint O1, ref hloc_constraint O2, AssemblerState state)
        {
            // Get the best match possible, converting hardware locations as necessary

            int best_coercions = -1;
            int best_spills = -1;
            opcode_match best_match = null;
            List<hardware_location> best_clobbered = null;
            List<OutputBlock> best_coercion = null;
            List<OutputBlock> best_postcoercion = null;
            hardware_location bestop1 = null, bestop2 = null, bestres = null;
            List<hardware_location> best_templocs = null;

            foreach (opcode_match om in oms)
            {
                int cur_coercions = 0;
                int cur_spill = 0;
                List<hardware_location> cur_clobbered = new List<hardware_location>();
                List<OutputBlock> cur_coercion = new List<OutputBlock>();
                List<OutputBlock> cur_postcoercion = new List<OutputBlock>();
                hardware_location newop1, newop2, newres;
                newop1 = O1.specific;
                newop2 = O2.specific;
                newres = R.specific;
                List<hardware_location> cur_templocs = new List<hardware_location>();

                if (!OpcodeMatches(om.Match.op1, O1))
                {
                    if (CanCoerce(om.Match.op1, O1.specific, cur_clobbered, cur_coercion, cur_templocs, ref newop1, false, state) == false)
                        continue;
                    cur_coercions++;
                }
                if (!OpcodeMatches(om.Match.op2, O2))
                {
                    if (CanCoerce(om.Match.op2, O2.specific, cur_clobbered, cur_coercion, cur_templocs, ref newop2, false, state) == false)
                        continue;
                    cur_coercions++;
                }
                if (!OpcodeMatches(om.Match.result, R))
                {
                    hloc_constraint rcons = om.Match.result;
                    if (rcons.constraint == hloc_constraint.c_.Operand1)
                        rcons = O1;
                    if (rcons.constraint == hloc_constraint.c_.Operand2)
                        rcons = O2;

                    if (CanCoerce(rcons, R.specific, cur_clobbered, cur_postcoercion, cur_templocs, ref newres, true, state) == false)
                        continue;
                    cur_coercions++;
                }

                cur_spill = cur_clobbered.Count;

                if ((best_match == null) || ((cur_coercions + 2 * cur_spill) < (best_coercions + 2 * best_spills)))
                {
                    best_match = om;
                    best_spills = cur_spill;
                    best_coercion = cur_coercion;
                    best_postcoercion = cur_postcoercion;
                    best_clobbered = cur_clobbered;
                    best_coercions = cur_coercions;
                    bestop1 = newop1;
                    bestop2 = newop2;
                    bestres = newres;
                    if (best_templocs != null)
                    {
                        foreach (hardware_location hloc in best_templocs)
                            state.reg_alloc.FreeRegister(hloc);
                    }
                    best_templocs = cur_templocs;
                }
                else
                {
                    foreach (hardware_location hloc in cur_templocs)
                        state.reg_alloc.FreeRegister(hloc);
                }
            }

            if (best_match == null)
                return null;
            coercion.AddRange(best_coercion);
            post_coercion.AddRange(best_postcoercion);
            clobbered.AddRange(best_clobbered);
            temp_locs.AddRange(best_templocs);
            R = new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = bestres };
            O1 = new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = bestop1 };
            O2 = new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = bestop2 };
            return best_match;
        }*/

        /*private bool CanCoerce(hloc_constraint dest, hardware_location src, List<hardware_location> clobbered, List<OutputBlock> coercion,
            List<hardware_location> temp_locs, ref hardware_location newsrc, bool is_post, AssemblerState state)
        {
            if ((dest.constraint == hloc_constraint.c_.None) && (src != null))
                return false;
            if ((dest.constraint == hloc_constraint.c_.Immediate) && (src != null))
                return false;
            if (dest.constraint == hloc_constraint.c_.List)
            {
                foreach (hloc_constraint lcons in dest.specific_list)
                {
                    if (CanCoerce(lcons, src, clobbered, coercion, temp_locs, ref newsrc, is_post, state))
                        return true;
                }
                return false;
            }

            hardware_location newloc = state.reg_alloc.GetRegister(dest, var.Coerce());
            if (newloc == null)
            {
                if (dest.constraint == hloc_constraint.c_.Specific)
                {
                    if (is_post)
                    {
                        if (CanCoerce(src, dest.specific, coercion, state))
                        {
                            newsrc = dest.specific;
                            clobbered.Add(dest.specific);
                            return true;
                        }
                    }
                    else
                    {
                        if (CanCoerce(dest.specific, src, coercion, state))
                        {
                            newsrc = dest.specific;
                            clobbered.Add(dest.specific);
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {

                if (CanCoerce(newloc, src, coercion, state))
                {
                    newsrc = newloc;
                    temp_locs.Add(newloc);
                    return true;
                }
                return false;
            }
        }*/

        private opcode_match GetFullMatch(List<opcode_match> oms)
        {
            foreach (opcode_match om in oms)
            {
                if (om.Quality == opcode_match.match_quality.Full)
                    return om;
            }
            return null;
        }

        private hloc_constraint BuildHlocFromVar(var var, IVarToHLocProvider var_locations, AssemblerState state)
        {
            if (var.type == libtysila.var.var_type.LocalVar)
            {
                return new hloc_constraint
                {
                    constraint = hloc_constraint.c_.Specific,
                    specific = state.local_vars[var.local_var]
                };
            }
            else if (var.type == libtysila.var.var_type.LocalArg)
            {
                if (state.local_args[var.local_arg].ValueLocation == null)
                    throw new Exception("Argument location not defined");
                return new hloc_constraint
                {
                    constraint = hloc_constraint.c_.Specific,
                    specific = state.local_args[var.local_arg].ValueLocation
                };
            }
            else if (var.type == libtysila.var.var_type.ContentsOf || var.type == libtysila.var.var_type.ContentsOfPlusConstant)
            {
                return new hloc_constraint
                {
                    constraint = hloc_constraint.c_.Specific,
                    specific = new hardware_contentsof { base_loc = BuildHlocFromVar(var.base_var.v, var_locations, state).specific, const_offset = var.constant_offset, size = var.v_size }
                };
            }
            else if (var.type == libtysila.var.var_type.AddressOf || var.type == libtysila.var.var_type.AddressOfPlusConstant)
            {
                if (var.base_var.v.type == libtysila.var.var_type.Label)
                {
                    return new hloc_constraint
                    {
                        constraint = hloc_constraint.c_.Specific,
                        specific = new hardware_addressoflabel { label = var.base_var.v.label, const_offset = var.constant_offset }
                    };
                }
                else
                {
                    if (var.type == libtysila.var.var_type.AddressOfPlusConstant)
                        throw new NotSupportedException();
                    return new hloc_constraint
                    {
                        constraint = hloc_constraint.c_.Specific,
                        specific = new hardware_addressof { base_loc = BuildHlocFromVar(var.base_var.v, var_locations, state).specific }
                    };
                }
            }
            else if (var.type == libtysila.var.var_type.LogicalVar)
            {
                return new hloc_constraint
                {
                    constraint = hloc_constraint.c_.Specific,
                    specific = var_locations.GetLocationOf(var)
                };
            }
            else if (var.type == libtysila.var.var_type.Const)
            {
                return new hloc_constraint
                {
                    constraint = hloc_constraint.c_.Immediate,
                    specific = new const_location { c = var.constant_val }
                };
            }
            else if(var.type == libtysila.var.var_type.Void)
                return new hloc_constraint { constraint = hloc_constraint.c_.None };
            else
                throw new NotSupportedException();
        }

        private List<opcode_match> OpcodeMatchesFromSemantics(output_opcode op, var result, var op1, var op2,
            Dictionary<int, var_semantic> semantics)
        {
            List<opcode_match> ret = new List<opcode_match>();

            foreach (opcode_choice opc in op.opcode_choice)
            {
                ret.Add(OpcodeMatches(opc, GetConstraintFromSemantic(result, semantics),
                    GetConstraintFromSemantic(op1, semantics),
                    GetConstraintFromSemantic(op2, semantics),
                    result, op1, op2
                    ));
            }

            return ret;
        }

        private List<opcode_match> OpcodeMatchesFromHloc(output_opcode op, hloc_constraint r, hloc_constraint op1,
            hloc_constraint op2)
        { return OpcodeMatchesFromHloc(op, r, op1, op2, new var(), new var(), new var()); }
        private List<opcode_match> OpcodeMatchesFromHloc(output_opcode op, hloc_constraint r, hloc_constraint op1,
            hloc_constraint op2, var R, var O1, var O2)
        {
            List<opcode_match> ret = new List<opcode_match>();

            foreach (opcode_choice opc in op.opcode_choice)
            {
                opcode_match cur_match = OpcodeMatches(opc, r, op1, op2, R, O1, O2);

                // TODO: ensure we don't match anything which is clobbered

                ret.Add(cur_match);
            }

            return ret;
        }

        private opcode_match OpcodeMatches(opcode_choice opc, hloc_constraint r, hloc_constraint op1, hloc_constraint op2,
            var v_result, var v_op1, var v_op2)
        {
            opcode_match ret = new opcode_match();
            ret.Match = opc;
            if (opc.result.constraint == hloc_constraint.c_.Operand1)
            {
                ret.ResultMatch = OpcodeMatches(opc.op1, r);
                if (ret.ResultMatch == true)
                {
                    if ((v_result.hardware_loc != null) && (v_op1.hardware_loc != null) &&
                        !v_result.hardware_loc.Equals(v_op1.hardware_loc))
                    {
                        ret.ResultMatch = false;
                    }
                }
            }
            else if (opc.result.constraint == hloc_constraint.c_.Operand2)
            {
                ret.ResultMatch = OpcodeMatches(opc.op2, r);
                if (ret.ResultMatch == true)
                {
                    if ((v_result.hardware_loc != null) && (v_op2.hardware_loc != null) &&
                        !v_result.hardware_loc.Equals(v_op2.hardware_loc))
                    {
                        ret.ResultMatch = false;
                    }
                }
            }
            else
                ret.ResultMatch = OpcodeMatches(opc.result, r);
            ret.Op1Match = OpcodeMatches(opc.op1, op1);
            ret.Op2Match = OpcodeMatches(opc.op2, op2);
            ret.QualityCount = 0;
            if (ret.ResultMatch)
                ret.QualityCount++;
            if (ret.Op1Match)
                ret.QualityCount++;
            if (ret.Op2Match)
                ret.QualityCount++;
            if (ret.QualityCount == 0)
                ret.Quality = opcode_match.match_quality.None;
            else if (ret.QualityCount == 3)
                ret.Quality = opcode_match.match_quality.Full;
            else
                ret.Quality = opcode_match.match_quality.Partial;
            return ret;
        }

        protected bool OpcodeMatches(hloc_constraint dest, hloc_constraint src)
        {
            // Can src be used in place of dest?
            if (dest.constraint == hloc_constraint.c_.List)
            {
                foreach (hloc_constraint newdest in dest.specific_list)
                {
                    if (OpcodeMatches(newdest, src))
                        return true;
                }
                return false;
            }
            if (dest.constraint == src.constraint)
            {
                if (dest.constraint == hloc_constraint.c_.Specific)
                {
                    if (dest.specific.Equals(src.specific))
                        return true;
                    return false;
                }
                if (dest.constraint == hloc_constraint.c_.AnyOfType)
                {
                    if (dest.specific.GetType() != src.specific.GetType())
                        return false;
                    if (dest.specific is hardware_contentsof)
                        return OpcodeMatches(new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = ((hardware_contentsof)dest.specific).base_loc },
                            new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = ((hardware_contentsof)src.specific).base_loc });
                    else if (dest.specific is hardware_addressof)
                        return OpcodeMatches(new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = ((hardware_addressof)dest.specific).base_loc },
                            new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = ((hardware_addressof)src.specific).base_loc });
                    else
                        return true;
                }
                if (dest.constraint == hloc_constraint.c_.Immediate)
                {
                    switch (dest.const_bitsize)
                    {
                        case 0:
                            return true;

                        case 8:
                            if (FitsSByte(((const_location)src.specific).c))
                                return true;
                            else
                                return false;

                        case 12:
                            if(FitsInt12(((const_location)src.specific).c))
                                return true;
                            else
                                return false;

                        case 16:
                            if(FitsInt16(((const_location)src.specific).c))
                                return true;
                            else
                                return false;

                        case 32:
                            if (FitsInt32(((const_location)src.specific).c))
                                return true;
                            else
                                return false;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                    return true;
            }
            if ((src.constraint == hloc_constraint.c_.None) && (dest.constraint == hloc_constraint.c_.AnyOrNone))
                return true;
            if (src.constraint == hloc_constraint.c_.Specific)
            {
                if (dest.constraint == hloc_constraint.c_.Any)
                    return true;
                if (dest.constraint == hloc_constraint.c_.AnyOrNone)
                    return true;
                if (dest.constraint == hloc_constraint.c_.AnyOfType)
                {
                    if (dest.specific.GetType() != src.specific.GetType())
                        return false;
                    if (dest.specific is hardware_contentsof)
                        return OpcodeMatches(new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = ((hardware_contentsof)dest.specific).base_loc },
                            new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = ((hardware_contentsof)src.specific).base_loc });
                    else if (dest.specific is hardware_addressof)
                        return OpcodeMatches(new hloc_constraint { constraint = hloc_constraint.c_.AnyOfType, specific = ((hardware_addressof)dest.specific).base_loc },
                            new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = ((hardware_addressof)src.specific).base_loc });
                    return true;
                }
            }
            return false;
        }

        private hloc_constraint GetConstraintFromSemantic(var v, Dictionary<int, var_semantic> semantics)
        {
            if (v.logical_var > 0)
            {
                var_semantic vs = semantics[v];
                return GetConstraintFromSemantic(vs);
            }
            else if (v.constant_val != null)
                return new hloc_constraint { constraint = hloc_constraint.c_.Immediate };
            else
                return new hloc_constraint { constraint = hloc_constraint.c_.None };
        }

        internal List<OutputBlock> OBList(OutputBlock b)
        { return new List<OutputBlock> { b }; }
        internal List<OutputBlock> OBList(OutputBlock b1, OutputBlock b2)
        { return new List<OutputBlock> { b1, b2 }; }
        internal List<OutputBlock> OBList(IList<byte> blist)
        {
            return new List<OutputBlock> { new CodeBlock { Code = blist } };
        }
        internal List<OutputBlock> OBList(IList<byte> blist1, IList<byte> blist2)
        {
            return new List<OutputBlock> { new CodeBlock { Code = blist1 }, new CodeBlock { Code = blist2 } };
        }

        internal bool FitsSByte(object o)
        {
            if (o.GetType() == typeof(Single))
                return false;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            if ((Convert.ToInt64(o) < SByte.MinValue) || (Convert.ToInt64(o) > SByte.MaxValue))
                return false;
            return true;
        }
        internal static bool FitsInt32(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            if ((Convert.ToInt64(o) < Int32.MinValue) || (Convert.ToInt64(o) > Int32.MaxValue))
                return false;
            return true;
        }
        internal bool FitsInt16(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            if ((Convert.ToInt64(o) < Int16.MinValue) || (Convert.ToInt64(o) > Int16.MaxValue))
                return false;
            return true;
        }
        internal bool FitsInt12(object o)
        {
            if (o.GetType() == typeof(Single))
                return true;
            if (o.GetType() == typeof(Double))
                return false;
            if (o.GetType() == typeof(IntPtr))
                o = ((IntPtr)o).ToInt64();
            else if (o.GetType() == typeof(UIntPtr))
                o = ((UIntPtr)o).ToUInt64();
            if ((Convert.ToInt64(o) < -2048) || (Convert.ToInt64(o) > 2047))
                return false;
            return true;
        }
        protected bool IsSigned(object o)
        {
            Type t = o.GetType();

            if ((t == typeof(IntPtr)) || (t == typeof(SByte)) ||
                (t == typeof(Int16)) || (t == typeof(Int32)) ||
                (t == typeof(Int64)) || (t == typeof(Single)) ||
                (t == typeof(Double)))
                return true;
            else if ((t == typeof(UIntPtr)) || (t == typeof(Byte)) ||
                (t == typeof(Char)) || (t == typeof(UInt16)) ||
                (t == typeof(UInt32)) || (t == typeof(UInt32)) ||
                (t == typeof(UInt64)))
                return false;
            else
                throw new NotSupportedException();
        }
    }
}
