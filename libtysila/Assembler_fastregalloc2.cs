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

/* The fast register allocator (v2) */

using System;
using System.Collections.Generic;
using System.Text;
using libasm;

namespace libtysila
{
    partial class Assembler
    {
        internal interface IVarToHLocProvider
        {
            hardware_location GetLocationOf(var v);
        }

        public class fa_node_ra_state : cfg_node.node_regalloc_state
        {
            internal Dictionary<var, hardware_location> var_locs = new Dictionary<var, hardware_location>();
        }

        class fa_loc_provider : Assembler.IVarToHLocProvider
        {
            internal Dictionary<var, hardware_location> locs;
            public hardware_location GetLocationOf(var v)
            {
                return locs[v];
            }
        }

        List<OutputBlock> FastAlloc2(List<cfg_node> nodes, cfg_node end_node, AssemblerState state, MethodToCompile mtc)
        {
            List<OutputBlock> ret = new List<OutputBlock>();
            
            // Emit library calls for unimplemented opcodes
            EmitLibraryCalls(nodes, state);

            // Perform liveness analysis on the instruction stream
            // Add in node global vars
            AddNodeGlobalVars(nodes);

            int remove_count = 0;
            do
            {
                // Perform liveness analysis for variables
                LivenessAnalysis(end_node, nodes);

                // Remove code that assigns to dead variables
                remove_count = RemoveDeadCode(nodes);
            } while (remove_count > 0);


            // Determine semantics and recommended locations
            Dictionary<int, var_semantic> semantics = DetermineSemantics(nodes);
            Dictionary<var, List<hardware_location>> recommended_locs = GetRecommendedLocations(nodes);

            // Generate a list of start nodes
            List<cfg_node> start_nodes = new List<cfg_node>();
            foreach (cfg_node node in nodes)
            {
                /* Start nodes are either those with no immediate predecessors or those whose only
                 * immediate predecessor is themself */
                if (node.ipred.Count == 0)
                    start_nodes.Add(node);
                if ((node.ipred.Count == 1) && (node.ipred[0] == node))
                    start_nodes.Add(node);
            }

            // Set the entry nodes variables
            fa_node_ra_state node_0_entry_state = new fa_node_ra_state();
            foreach (KeyValuePair<int, hardware_location> kvp in state.required_locations)
                node_0_entry_state.var_locs[kvp.Key] = kvp.Value;
            foreach(cfg_node node in start_nodes)
                node.ra_state_at_start = node_0_entry_state;

            // Determine whether we take the address of any local args
            bool changes = false;
            if (state.la_v_map != null)
            {
                List<int> las = new List<int>(state.la_v_map.Keys);
                foreach (int la in las)
                {
                    var old_v = state.la_v_map[la];
                    if (semantics.ContainsKey(old_v) && semantics[old_v].needs_memloc)
                    {
                        // Is the assigned location of the variable a GPR?
                        hardware_location hloc = node_0_entry_state.var_locs[old_v];
                        if (!hloc.CanTakeAddressOf)
                        {
                            // Insert an instruction assigning from the local arg's location to here
                            var new_v = state.next_variable++;
                            ThreeAddressCode new_tac = new ThreeAddressCode(GetAssignTac(hloc.GetSemantic()), old_v, new_v, var.Null);
                            foreach (cfg_node start_node in start_nodes)
                                start_node.optimized_ir.Insert(0, new_tac);
                            state.la_v_map[la] = new_v;
                            node_0_entry_state.var_locs[new_v] = node_0_entry_state.var_locs[old_v];
                            node_0_entry_state.var_locs.Remove(old_v);
                            
                            changes = true;
                        }
                    }
                }
            }

            if (changes)
            {
                // Perform liveness analysis for variables
                LivenessAnalysis(end_node, nodes);
                recommended_locs = GetRecommendedLocations(nodes);
            }

            // First pass through the code is purely to allocate registers
            Dictionary<var, hardware_location> alloated_locations = new Dictionary<var, hardware_location>(node_0_entry_state.var_locs);
            fa_loc_provider lp = new fa_loc_provider { locs = alloated_locations };
            state.var_hlocs = lp;
            foreach(cfg_node node in start_nodes)
                fa_allocate_regs(node, state, semantics, recommended_locs, alloated_locations);

            // Second pass is to emit the code
            foreach (cfg_node node in nodes)
            {
                foreach (ThreeAddressCode inst in node.optimized_ir)
                {
                    // insert a block as a label if requested
                    if (inst is LabelEx)
                        ret.Add(new NodeHeader { block_id = ((LabelEx)inst).Block_id });

                    hloc_constraint O1 = BuildHlocFromVar(inst.Operand1, lp, state);
                    hloc_constraint O2 = BuildHlocFromVar(inst.Operand2, lp, state);
                    hloc_constraint R = BuildHlocFromVar(inst.Result, lp, state);

                    fa_emit_code(inst, O1, O2, R, lp, state, ret);
                }
            }

            return ret;
        }

        private void fa_emit_code(ThreeAddressCode inst, hloc_constraint O1, hloc_constraint O2, hloc_constraint R, fa_loc_provider lp, AssemblerState state, List<OutputBlock> ret)
        {
            // Choose the appropriate opcode
            inst.Operand1.hardware_loc = O1.specific;
            inst.Operand2.hardware_loc = O2.specific;
            inst.Result.hardware_loc = R.specific;
            List<opcode_match> matches;
           
            try
            {
                matches = OpcodeMatchesFromHloc(GetOutputOpcode(inst), R, O1, O2, inst.Result, inst.Operand1, inst.Operand2);
            }
            catch (KeyNotFoundException e)
            {
                throw new MissingOpcodeException(inst, inst.node.containing_meth);
            }

            opcode_match match = GetFullMatch(matches);

            List<OutputBlock> pre_coerce = new List<OutputBlock>();
            List<OutputBlock> post_coerce = new List<OutputBlock>();
            List<OutputBlock> pre_spill = new List<OutputBlock>();
            List<OutputBlock> post_spill = new List<OutputBlock>();
            Dictionary<var, hardware_location> post_spill_returns = new Dictionary<var, hardware_location>();
            List<hardware_location> surviving_locs = new List<hardware_location>(util.Intersect<hardware_location>(ExpandLocations(inst.locs_at_start.Values), ExpandLocations(inst.locs_at_end.Values)));

            if (match == null)
            {
                // We have to emit coercion code

                // First store where the locations are currently stored
                hardware_location orig_O1 = O1.specific;
                hardware_location orig_O2 = O2.specific;
                hardware_location orig_R = R.specific;

                // New locations
                hardware_location new_O1 = O1.specific;
                hardware_location new_O2 = O2.specific;
                hardware_location new_R = R.specific;

                // Store used locations (for use by the code which preserves callee saved registers)
                state.UsedLocations.Add(orig_O1);
                state.UsedLocations.Add(orig_O2);
                state.UsedLocations.Add(orig_R);

                // Determine the best match
                int best_match_quality = -1;
                opcode_match best_match = null;

                foreach (opcode_match om_try in matches)
                {
                    if (om_try.QualityCount > best_match_quality)
                    {
                        best_match_quality = om_try.QualityCount;
                        best_match = om_try;
                    }
                }

                if (best_match == null)
                    throw new Exception("No possible encodings for " + inst.Operator.ToString() + " in architecture " + _arch.ToString());
                match = best_match;

                if (!match.Op1Match)
                {
                    // Coerce op1

                    List<hardware_location> possible_hlocs = InterpretConstraint(match.Match.op1, orig_O1, orig_O2, state);
                    List<hardware_location> available_hlocs = GetAvailableHlocs(possible_hlocs, inst.locs_at_start.Values);
                    List<hardware_location> recommended_hlocs = new List<hardware_location>(util.Intersect<hardware_location>(available_hlocs,
                        new List<hardware_location> { output_opcodes[inst.Operator].recommended_O1 }));
                    if (recommended_hlocs.Count > 0)
                        new_O1 = recommended_hlocs[0];
                    else if (available_hlocs.Count > 0)
                        new_O1 = available_hlocs[0];
                    else
                    {
                        // We have to spill a register

                        // First determine the appropriate register to spill
                        hardware_location spill_from = possible_hlocs[0];

                        // Now determine where to spill to
                        List<hardware_location> possible_spill_tos = GetAvailableHlocs(GetAvailableHlocs(GetAllHardwareLocationsOfType(spill_from.GetType(), spill_from), inst.locs_at_end.Values),
                            inst.locs_at_start.Values);
                        hardware_location spill_to = null;
                        if (possible_spill_tos.Count > 0)
                            spill_to = possible_spill_tos[0];

                        if (spill_to == null)
                            throw new Exception();

                        // Output the spill
                        List<OutputBlock> temp_post_spill = new List<OutputBlock>();
                        Assign(spill_to, spill_from, pre_spill, state);
                        Assign(spill_from, spill_to, temp_post_spill, state);
                        for (int i = 0; i < temp_post_spill.Count; i++)
                            post_spill.Insert(0, temp_post_spill[i]);

                        new_O1 = spill_from;

                        // Find the var which we are spilling
                        var spill_var = var.Null;
                        foreach (KeyValuePair<var, hardware_location> kvp in lp.locs)
                        {
                            if (kvp.Value.Equals(spill_from))
                                spill_var = kvp.Key;
                        }

                        // Reassign it temporarily in the register allocator
                        lp.locs[spill_var] = spill_to;
                        post_spill_returns[spill_var] = spill_from;
                        if (surviving_locs.Contains(spill_from))
                        {
                            surviving_locs.Remove(spill_from);
                            surviving_locs.Add(spill_to);
                        }
                    }

                    inst.Operand1.hardware_loc = new_O1;
                    Assign(new_O1, orig_O1, pre_coerce, state);
                    inst.locs_at_start[state.next_variable++] = new_O1;
                }

                if (!match.Op2Match)
                {
                    // Coerce op2

                    List<hardware_location> possible_hlocs = InterpretConstraint(match.Match.op2, orig_O1, orig_O2, state);
                    List<hardware_location> available_hlocs = GetAvailableHlocs(possible_hlocs, inst.locs_at_start.Values);
                    List<hardware_location> recommended_hlocs = new List<hardware_location>(util.Intersect<hardware_location>(available_hlocs,
                        new List<hardware_location> { output_opcodes[inst.Operator].recommended_O2 }));
                    if (recommended_hlocs.Count > 0)
                        new_O2 = recommended_hlocs[0];
                    else if (available_hlocs.Count > 0)
                        new_O2 = available_hlocs[0];
                    else
                        throw new Exception();

                    inst.Operand2.hardware_loc = new_O2;
                    Assign(new_O2, orig_O2, pre_coerce, state);
                }

                if (!match.ResultMatch)
                {
                    // Coerce result

                    List<hardware_location> possible_hlocs = InterpretConstraint(match.Match.result, orig_O1, orig_O2, state);
                    List<hardware_location> available_hlocs = GetAvailableHlocs(possible_hlocs, inst.locs_at_start.Values);
                    List<hardware_location> recommended_hlocs = new List<hardware_location>(util.Intersect<hardware_location>(available_hlocs,
                        new List<hardware_location> { output_opcodes[inst.Operator].recommended_R }));
                    if (recommended_hlocs.Count > 0)
                        new_R = recommended_hlocs[0];
                    else if (available_hlocs.Count > 0)
                        new_R = available_hlocs[0];
                    else
                    {
                        // We have to spill a register

                        // First determine the appropriate register to spill
                        hardware_location spill_from = possible_hlocs[0];

                        // Now determine where to spill to
                        List<hardware_location> possible_spill_tos = GetAvailableHlocs(GetAvailableHlocs(GetAllHardwareLocationsOfType(spill_from.GetType(), spill_from), inst.locs_at_end.Values),
                            inst.locs_at_start.Values);
                        hardware_location spill_to = null;
                        if (possible_spill_tos.Count > 0)
                            spill_to = possible_spill_tos[0];

                        if (spill_to == null)
                            throw new Exception();

                        // Output the spill
                        List<OutputBlock> temp_post_spill = new List<OutputBlock>();
                        Assign(spill_to, spill_from, pre_spill, state);
                        Assign(spill_from, spill_to, temp_post_spill, state);
                        for (int i = 0; i < temp_post_spill.Count; i++)
                            post_spill.Insert(0, temp_post_spill[i]);

                        new_R = spill_from;

                        // Find the var which we are spilling
                        var spill_var = var.Null;
                        foreach (KeyValuePair<var, hardware_location> kvp in lp.locs)
                        {
                            if (kvp.Value.Equals(spill_from))
                                spill_var = kvp.Key;
                        }

                        // Reassign it temporarily in the register allocator
                        lp.locs[spill_var] = spill_to;
                        post_spill_returns[spill_var] = spill_from;
                        if (surviving_locs.Contains(spill_from))
                        {
                            surviving_locs.Remove(spill_from);
                            surviving_locs.Add(spill_to);
                        }
                    }

                    inst.Result.hardware_loc = new_R;
                    Assign(orig_R, new_R, post_coerce, state);
                }
            }

            // CallEx requires the hardware locations of all variables to be identified
            if (inst is CallEx)
            {
                CallEx ce = inst as CallEx;

                for(int i = 0; i < ce.Var_Args.Length; i++)
                {
                    if (ce.Var_Args[i].hardware_loc == null)
                    {
                        switch (ce.Var_Args[i].type)
                        {
                            case var.var_type.LogicalVar:
                                ce.Var_Args[i].hardware_loc = state.var_hlocs.GetLocationOf(ce.Var_Args[i]);
                                break;
                            case var.var_type.AddressOf:
                            case var.var_type.AddressOfPlusConstant:
                                switch (ce.Var_Args[i].base_var.v.type)
                                {
                                    case var.var_type.Label:
                                        ce.Var_Args[i].hardware_loc = new hardware_addressoflabel
                                        {
                                            label = ce.Var_Args[i].base_var.v.label,
                                            const_offset = ce.Var_Args[i].constant_offset
                                        };
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }
                                break;
                            case var.var_type.LocalVar:
                                ce.Var_Args[i].hardware_loc = state.local_vars[ce.Var_Args[i].local_var];
                                break;
                            case var.var_type.Const:
                                ce.Var_Args[i].hardware_loc = new const_location { c = ce.Var_Args[i].constant_val };
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            }

            // Determine clobbered registers
            List<hardware_location> clobbered_regs = new List<hardware_location>(util.Intersect<hardware_location>(surviving_locs, match.Match.clobber_list));
            List<OutputBlock> pre_clobber = new List<OutputBlock>();
            List<OutputBlock> post_clobber = new List<OutputBlock>();
            for (int i = 0; i < clobbered_regs.Count; i++)
                pre_clobber.Add(new CodeBlock(SaveLocation(clobbered_regs[i])));
            for (int i = clobbered_regs.Count - 1; i >= 0; i--)
                post_clobber.Add(new CodeBlock(RestoreLocation(clobbered_regs[i])));

            // Determine used locations if required
            if (inst.requires_used_locations_list)
            {
                IEnumerable<var> used_locs = util.Union<var>(inst.live_vars, inst.live_vars_after);
                inst.used_locations = new List<hardware_location>();
                inst.used_var_locations = new Dictionary<int, hardware_location>();
                foreach (var v in used_locs)
                {
                    inst.used_locations.Add(lp.locs[v]);
                    inst.used_var_locations.Add(v, lp.locs[v]);
                }
            }

            // Store those locations we use
            state.UsedLocations.Add(inst.Result.hardware_loc);
            state.UsedLocations.Add(inst.Operand1.hardware_loc);
            state.UsedLocations.Add(inst.Operand2.hardware_loc);

            // Output the code
            ret.AddRange(pre_spill);
            ret.AddRange(pre_coerce);
            ret.AddRange(pre_clobber);
            ret.AddRange(match.Match.code_emitter(inst.Operator, inst.Result, inst.Operand1, inst.Operand2, inst, state));
            ret.AddRange(post_clobber);
            ret.AddRange(post_coerce);
            ret.AddRange(post_spill);

            // Restore the stored locations of spilled registers
            foreach (KeyValuePair<var, hardware_location> spilled_reg in post_spill_returns)
                lp.locs[spilled_reg.Key] = spilled_reg.Value;
        }

        private IEnumerable<hardware_location> ExpandLocations(IEnumerable<hardware_location> locs)
        {
            foreach(hardware_location l in locs)
            {
                if(l is multiple_hardware_location)
                {
                    multiple_hardware_location mhl = l as multiple_hardware_location;
                    foreach(hardware_location l2 in ExpandLocations(mhl.hlocs))
                        yield return l2;
                }
                else
                    yield return l;
            }
            yield break;
        }

        private void fa_allocate_regs(cfg_node cfg_node, AssemblerState state, Dictionary<int, var_semantic> semantics, Dictionary<var, List<hardware_location>> recommended_locs,
            Dictionary<var, hardware_location> allocated_locations)
        {
            // Allocate registers for the current node
            fa_node_ra_state entry_state = cfg_node.ra_state_at_start as fa_node_ra_state;
            Dictionary<var, hardware_location> cur_locs = new Dictionary<var, hardware_location>(entry_state.var_locs);
            List<hardware_location> cur_hlocs = new List<hardware_location>(entry_state.var_locs.Values);

            // Pass through the instruction stream allocating locations as we go
            foreach (ThreeAddressCode inst in cfg_node.optimized_ir)
            {
                inst.locs_at_start = new Dictionary<var, hardware_location>(cur_locs);

                // Determine which variable can be freed following this instruction
                foreach (var v in util.Except<var>(cur_locs.Keys, inst.live_vars_after))
                {
                    //cur_hlocs.Remove(cur_locs[v]);
                    RemoveExpandedLocation(cur_hlocs, cur_locs[v]);
                    cur_locs.Remove(v);
                }

                // Determine which variables are new in this instruction
                foreach (var v in util.Except<var>(inst.live_vars_after, cur_locs.Keys))
                {
                    // Determine if we need to allocate a hardware location for it
                    if (allocated_locations.ContainsKey(v))
                    {
                        hardware_location preallocated_loc = allocated_locations[v];
                        if (cur_hlocs.Contains(preallocated_loc))
                            throw new Exception();
                        cur_hlocs.Add(preallocated_loc);
                        cur_locs.Add(v, preallocated_loc);
                    }
                    else
                    {
                        // We need to allocate a new location for it
                        hloc_constraint hc = GetConstraintFromSemantic(semantics[v]);

                        // Determine the locations of op1 and op2
                        hardware_location op1 = null;
                        hardware_location op2 = null;
                        if(cur_locs.ContainsKey(inst.Operand1))
                            op1 = cur_locs[inst.Operand1];
                        if(cur_locs.ContainsKey(inst.Operand2))
                            op2 = cur_locs[inst.Operand2];

                        List<hardware_location> possible_hlocs;
                        possible_hlocs = InterpretConstraint(hc, op1, op2, state);

                        List<hardware_location> available_hlocs = GetAvailableHlocs(possible_hlocs, cur_hlocs);
                        List<hardware_location> recommended_hlocs = new List<hardware_location>(util.Intersect<hardware_location>(available_hlocs, recommended_locs[v]));

                        hardware_location new_hloc = null;
                        if (recommended_hlocs.Count > 0)
                            new_hloc = recommended_hlocs[0];
                        else if (available_hlocs.Count > 0)
                            new_hloc = available_hlocs[0];
                        else
                            throw new Exception();

                        allocated_locations[v] = new_hloc;
                        AddExpandedLocation(cur_hlocs, new_hloc);
                        //cur_hlocs.Add(new_hloc);
                        cur_locs.Add(v, new_hloc);

                        if (new_hloc is hardware_stackloc)
                            state.stack_space_used += state.stack_space_used_cur_inst;
                        state.stack_space_used_cur_inst = 0;
                    }
                }

                inst.locs_at_end = new Dictionary<var, hardware_location>(cur_locs);
            }

            // Now store the current state
            fa_node_ra_state end_state = new fa_node_ra_state { var_locs = new Dictionary<var, hardware_location>(cur_locs) };
            cfg_node.ra_state_at_end = end_state;

            // Allocate regs for all child nodes
            foreach (cfg_node child in cfg_node.isuc)
            {
                if (child.ra_state_at_start == null)
                {
                    child.ra_state_at_start = end_state;
                    fa_allocate_regs(child, state, semantics, recommended_locs, allocated_locations);
                }
            }
        }

        private void AddExpandedLocation(List<hardware_location> cur_hlocs, hardware_location new_hloc)
        {
            if (new_hloc is multiple_hardware_location)
            {
                multiple_hardware_location mhl = new_hloc as multiple_hardware_location;
                foreach (hardware_location single_hloc in mhl.hlocs)
                    AddExpandedLocation(cur_hlocs, single_hloc);
            }
            else
                cur_hlocs.Add(new_hloc);
        }

        private void RemoveExpandedLocation(List<hardware_location> cur_hlocs, hardware_location new_hloc)
        {
            if (new_hloc is multiple_hardware_location)
            {
                multiple_hardware_location mhl = new_hloc as multiple_hardware_location;
                foreach (hardware_location single_hloc in mhl.hlocs)
                    RemoveExpandedLocation(cur_hlocs, single_hloc);
            }
            else
                cur_hlocs.Remove(new_hloc);
        }

        bool OverlapsLocation(hardware_location test_loc, ICollection<hardware_location> cur_hlocs)
        {
            if (test_loc is multiple_hardware_location)
            {
                multiple_hardware_location mhl = test_loc as multiple_hardware_location;
                foreach (hardware_location single_hloc in mhl.hlocs)
                {
                    if (OverlapsLocation(single_hloc, cur_hlocs))
                        return true;
                }
                return false;
            }
            return cur_hlocs.Contains(test_loc);
        }

        private List<hardware_location> GetAvailableHlocs(IEnumerable<hardware_location> possible_hlocs, ICollection<hardware_location> cur_hlocs)
        {
            List<hardware_location> ret = new List<hardware_location>();

            foreach (hardware_location hloc in possible_hlocs)
            {
                if (!OverlapsLocation(hloc, cur_hlocs))
                    ret.Add(hloc);
            }

            return ret;
        }

        private List<hardware_location> InterpretConstraint(hloc_constraint hc, hardware_location op1, hardware_location op2, AssemblerState state)
        {
            List<hardware_location> ret = new List<hardware_location>();

            switch (hc.constraint)
            {
                case hloc_constraint.c_.AnyOfType:
                    if (hc.specific is multiple_hardware_location)
                        ret.AddRange(GetMultipleHardwareLocations(hc.specific as multiple_hardware_location));
                    else if (hc.specific is hardware_stackloc)
                    {
                        // Set possible_hlocs to be the next stack entry
                        int stack_size = ((hardware_stackloc)hc.specific).size;
                        stack_size = util.align(stack_size, GetSizeOfPointer());

                        hardware_stackloc sloc = new hardware_stackloc { loc = state.stack_space_used + state.stack_space_used_cur_inst, size = stack_size };
                        state.stack_space_used_cur_inst += stack_size;

                        return new List<hardware_location> { sloc };
                    }
                    else
                        ret.AddRange(GetAllHardwareLocationsOfType(hc.specific.GetType(), hc.specific));
                    break;
                case hloc_constraint.c_.Specific:
                    if(this.IsLocationAllowed(hc.specific))
                        ret.Add(hc.specific);
                    break;
                case hloc_constraint.c_.None:
                    break;
                case hloc_constraint.c_.Immediate:
                    break;
                case hloc_constraint.c_.List:
                    foreach (hloc_constraint hc2 in hc.specific_list)
                        ret.AddRange(InterpretConstraint(hc2, op1, op2, state));
                    break;
                case hloc_constraint.c_.Operand1:
                    if (op1 != null)
                        ret.Add(op1);
                    break;
                case hloc_constraint.c_.Operand2:
                    if (op2 != null)
                        ret.Add(op2);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return ret;
        }

        protected IEnumerable<hardware_location> GetMultipleHardwareLocations(multiple_hardware_location hloc)
        {
            int multiple_loc_count = hloc.hlocs.Length;
            List<hardware_location> src_locs = new List<hardware_location>(GetAllHardwareLocationsOfType(hloc.hlocs.GetType().GetElementType(), null));

            List<hardware_location> ret = new List<hardware_location>();

            // Iterate through choosing all the appropriate combinations
            List<List<hardware_location>> combs = Combinations<hardware_location>(src_locs, multiple_loc_count);
            foreach (List<hardware_location> comb in combs)
            {
                multiple_hardware_location mhl = new multiple_hardware_location { hlocs = comb.ToArray() };
                ret.Add(mhl);
            }

            return ret;
        }

        List<List<T>> Combinations<T>(List<T> input, int length)
        {
            List<List<T>> ret = new List<List<T>>();

            for (int i = 0; i < input.Count; i++)
            {
                if (length == 1)
                    ret.Add(new List<T> { input[i] });
                else
                {
                    List<T> new_input = input.GetRange(i + 1, input.Count - (i + 1));
                    List<List<T>> new_ret = Combinations<T>(new_input, length - 1);

                    foreach (List<T> t in new_ret)
                    {
                        t.Insert(0, input[i]);
                        ret.Add(t);
                    }
                }
            }

            return ret;
        }

        private Dictionary<var, List<hardware_location>> GetRecommendedLocations(List<cfg_node> nodes)
        {
            Dictionary<var, List<hardware_location>> ret = new Dictionary<var, List<hardware_location>>();

            foreach (cfg_node node in nodes)
            {
                foreach (ThreeAddressCode tac in node.optimized_ir)
                {
                    try
                    {
                        AddRecommendedLocation(ret, tac.Operand1, output_opcodes[tac.Operator].recommended_O1);
                        AddRecommendedLocation(ret, tac.Operand2, output_opcodes[tac.Operator].recommended_O2);
                        AddRecommendedLocation(ret, tac.Result, output_opcodes[tac.Operator].recommended_R);
                    }
                    catch (KeyNotFoundException)
                    {
                        // This ensures if the above fails, due to a lack of output_opcode, the returned dictionary
                        //  will still contain an (empty) entry for the particular variable
                        AddRecommendedLocation(ret, tac.Operand1, null);
                        AddRecommendedLocation(ret, tac.Operand2, null);
                        AddRecommendedLocation(ret, tac.Result, null);
                    }
                }
            }

            return ret;
        }

        private void AddRecommendedLocation(Dictionary<var, List<hardware_location>> ret, var var, hardware_location hardware_location)
        {
            if (var.type != libtysila.var.var_type.LogicalVar)
                return;

            // Initialise the list if required
            if (!ret.ContainsKey(var))
                ret[var] = new List<hardware_location>();

            if (hardware_location != null)
            {
                if (!ret[var].Contains(hardware_location))
                    ret[var].Add(hardware_location);
            }
        }
    }
}
