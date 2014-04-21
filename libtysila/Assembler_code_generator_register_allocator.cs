/* Copyright (C) 2011 by John Cronin
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

/* This is part of the code generator responsible for allocating hardware locations to virtual variables
 * 
 * Essentially we use a graph colouring algorithm
 * 
 * First we create the graph from the liveness analysis
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using libasm;

namespace libtysila
{
    partial class Assembler
    {
        internal RegisterGraph GenerateRegisterGraph(List<cfg_node> nodes, AssemblerState state)
        {
            RegisterGraph ret = new RegisterGraph(this, state);

            foreach (cfg_node node in nodes)
            {
                if (node.optimized_ir != null)
                {
                    foreach (ThreeAddressCode inst in node.optimized_ir)
                    {
                        /* From http://www.cs.princeton.edu/courses/archive/spr05/cos320/notes/Register%20Allocation.ppt
                         * 
                         * Two variables interfere if:
                         *   - they are both initially live
                         *   - they are both live after the function
                         *   - one is defined and the other is live after the function
                         *       e.g. (for point 3) if a = b + c and a is dead after the function but b is live then a and b interfere
                         */

                        int i, j;

                        for (i = 0; i < inst.live_vars.Count; i++)
                        {
                            for (j = 0; j < inst.live_vars.Count; j++)
                            {
                                if (i != j)
                                    ret.AddEdge(inst.live_vars[i].logical_var, inst.live_vars[j].logical_var);
                            }
                        }

                        for (i = 0; i < inst.live_vars_after.Count; i++)
                        {
                            for (j = 0; j < inst.live_vars_after.Count; j++)
                            {
                                if (i != j)
                                    ret.AddEdge(inst.live_vars_after[i].logical_var, inst.live_vars_after[j].logical_var);
                            }

                            if ((inst.Result.type == var.var_type.LogicalVar) && (inst.Result.logical_var == inst.live_vars_after[i].logical_var))
                                ret.AddEdge(inst.live_vars_after[i].logical_var, inst.Result.logical_var);
                        }

                        // Also add in the instructions which are use this variable
                        if (inst.Operand1.type == var.var_type.LogicalVar)
                            ret.AddVarUsage(inst.Operand1.logical_var, inst);
                        if (inst.Operand2.type == var.var_type.LogicalVar)
                            ret.AddVarUsage(inst.Operand2.logical_var, inst);
                        if (inst.Result.type == var.var_type.LogicalVar)
                            ret.AddVarUsage(inst.Result.logical_var, inst);
                        if (inst is CallEx)
                        {
                            foreach (var v in ((CallEx)inst).Var_Args)
                            {
                                if (v.type == var.var_type.LogicalVar)
                                    ret.AddVarUsage(v.logical_var, inst);
                            }
                        }
                    }
                }
            }

            return ret;
        }

        List<hardware_location> GetHlocList(hloc_constraint constraint)
        {
            List<hardware_location> ret = new List<hardware_location>();

            if (constraint.constraint == hloc_constraint.c_.Specific)
            {
                if (this.IsLocationAllowed(constraint.specific))
                    ret.Add(constraint.specific);
            }
            else if (constraint.constraint == hloc_constraint.c_.List)
            {
                foreach (hloc_constraint hc in constraint.specific_list)
                    ret.AddRange(GetHlocList(hc));
            }
            else if (constraint.constraint == hloc_constraint.c_.AnyOfType)
            {
                if (constraint.specific.GetType() == typeof(hardware_stackloc))
                    ret.Add(constraint.specific);
                else
                    ret.AddRange(this.GetAllHardwareLocationsOfType(constraint.specific.GetType(), constraint.specific));
            }

            return ret;
        }

        internal void ColourGraph(RegisterGraph graph, Dictionary<int, var_semantic> semantics)
        {
            int v = -1;

            while ((v = graph.GetNextVarToColour()) != -1)
            {
                List<hardware_location> possible_hlocs = GetHlocList(GetConstraintFromSemantic(semantics[v]));

                bool coloured = false;

                foreach (hardware_location hloc in possible_hlocs)
                {
                    if (graph.CanColour(v, hloc) == -1)
                    {
                        graph.Colour(v, hloc);
                        coloured = true;
                        break;
                    }
                }

                if (!coloured)
                    throw new Exception("Unable to colour!");
            }
        }

        internal void PrecolourGraph(RegisterGraph graph, AssemblerState state, Dictionary<int, var_semantic> semantics, ref bool changes_made, List<ThreeAddressCode> faulting_instructions,
            MethodToCompile mtc)
        {
            /* Precolouring involves assigning hardware locations to variables based on recommended locations,
             * e.g div on x86_64 uses rax and rdx
             */

            graph.allocated_vars.Clear();
            graph.unallocated_vars.Clear();

            foreach (KeyValuePair<int, hardware_location> kvp in state.required_locations)
            {
                if (graph.graph.ContainsKey(kvp.Key))
                    graph.allocated_vars.Add(kvp.Key);
            }

            int next_not_recommended_loc = -2;

            // Determine first if we shouldn't have a certain register used during this instruction
            int[] cur_graph_keys = new int[graph.graph.Keys.Count];
            graph.graph.Keys.CopyTo(cur_graph_keys, 0);
            foreach (int graph_key in cur_graph_keys)
            {
                foreach (ThreeAddressCode inst in graph.graph[graph_key].instrs)
                {
                    List<hardware_location> not_recommended_hlocs = GetNotRecommendedLocations(graph_key, inst);
                    if (not_recommended_hlocs == null)
                        continue;

                    foreach (hardware_location not_recommended_hloc in not_recommended_hlocs)
                    {
                        graph.AddEdge(graph_key, next_not_recommended_loc--);
                        if (graph.CanColour(next_not_recommended_loc + 1, not_recommended_hloc) == -1)
                            graph.graph[next_not_recommended_loc + 1].hloc = not_recommended_hloc;
                    }
                }
            }

            foreach (KeyValuePair<int, RegisterGraph.VarUsage> graph_entry in graph.graph)
            {
                graph.unallocated_vars.Add(graph_entry.Key);

                foreach (ThreeAddressCode inst in graph_entry.Value.instrs)
                {
                    // Determine if there is a possible requirement for this instruction to have the variable in a set hardware location
                    hardware_location hloc = GetRecommendedLocation(graph_entry.Key, inst);

                    if (hloc == null)
                        continue;

                    // The instruction recommends a location, now see if that will fit with the semantics
                    hloc_constraint semantic_constraint = GetConstraintFromSemantic(semantics[graph_entry.Key]);
                    if(!hloc_constraint.IsAssignableTo(semantic_constraint, new hloc_constraint { constraint = hloc_constraint.c_.Specific, specific = hloc }))
                        continue;

                    // Finally, check whether this location is permissible with its neighbours in the register graph
                    int conflicting_var = graph.CanColour(graph_entry.Key, hloc);
                    if (conflicting_var == -1)
                    {
                        graph_entry.Value.hloc = hloc;
                        graph.allocated_vars.Add(graph_entry.Key);
                        graph.unallocated_vars.Remove(graph_entry.Key);
                        break;
                    }
                    else
                    {
                        /* We cannot precolour the graph how we want to
                         * 
                         * We need to move out of the way all the other variables which want to use this slot
                         * 
                         * To do this, we need to identify the instructions with fixed registers and have them use
                         * a short lived temporary variable instead.
                         * 
                         * e.g. if the variable is a result v5 = call_i(foobar)
                         * then recode it as:
                         *   v6 = call_i(foobar)
                         *   v5 = v6
                         *   
                         * if the variable is an operand v2 = div(v3, v4)
                         * then recode it as
                         * v5 = v3
                         * v2 = div(v5, v4)
                         * 
                         * If there are no interfering instructions, try just assinging this value to another location to 
                         * break up the graph
                         *                          
                         */

                        List<ThreeAddressCodeAndVar> interfering_instrs = IdentifyInterferingInstructions(hloc, graph, graph_entry.Value.adj);

                        if (interfering_instrs.Count == 0)
                        {
                            var_semantic vs = semantics[graph_entry.Key];
                            int vt_size = vs.vtype_size;
                            ThreeAddressCode.Op assign_tac = GetAssignTac(vs);

                            // Find where to insert this
                            cfg_node node = inst.node;
                            int line_no = node.optimized_ir.IndexOf(inst);

                            // Insert an assign
                            var temp_var = state.next_variable++;
                            if(inst.Result.Equals(graph_entry.Key))
                            {
                                node.optimized_ir[line_no].Result = temp_var;
                                node.optimized_ir.Insert(line_no + 1, new ThreeAddressCode(assign_tac, graph_entry.Key, temp_var, var.Null, vt_size));
                            }
                            if(inst.Operand1.Equals(graph_entry.Key))
                            {
                                node.optimized_ir.Insert(line_no, new ThreeAddressCode(assign_tac, temp_var, graph_entry.Key, var.Null, vt_size));
                                node.optimized_ir[line_no + 1].Operand1 = temp_var;
                                line_no++;
                            }
                            if (inst.Operand2.Equals(graph_entry.Key))
                            {
                                node.optimized_ir.Insert(line_no, new ThreeAddressCode(assign_tac, temp_var, graph_entry.Key, var.Null, vt_size));
                                node.optimized_ir[line_no + 1].Operand2 = temp_var;
                                line_no++;
                            }
                        }
                        else
                        {
                            foreach (ThreeAddressCodeAndVar rewrite_inst in interfering_instrs)
                            {
                                // First identify where this code section is
                                cfg_node node = rewrite_inst.tac.node;
                                int line_no = node.optimized_ir.IndexOf(rewrite_inst.tac);

                                var conflict_var = rewrite_inst.var;
                                ThreeAddressCode.Op assign_tac = GetAssignTac(semantics[conflict_var]);
                                int vt_size = semantics[conflict_var].vtype_size;

                                // Then rewrite its instructions
                                if (rewrite_inst.tac.Result.Equals(conflict_var))
                                {
                                    var temp_var = state.next_variable++;
                                    node.optimized_ir[line_no].Result = temp_var;
                                    node.optimized_ir.Insert(line_no + 1, new ThreeAddressCode(assign_tac, conflict_var, temp_var, var.Null, vt_size));
                                    rewrite_inst.tac.CoercionCount++;
                                }
                                if (rewrite_inst.tac.Operand1.Equals(conflict_var))
                                {
                                    var temp_var = state.next_variable++;
                                    node.optimized_ir.Insert(line_no, new ThreeAddressCode(assign_tac, temp_var, conflict_var, var.Null, vt_size));
                                    node.optimized_ir[line_no + 1].Operand1 = temp_var;
                                    line_no++;
                                    rewrite_inst.tac.CoercionCount++;
                                }
                                if (rewrite_inst.tac.Operand2.Equals(conflict_var))
                                {
                                    var temp_var = state.next_variable++;
                                    node.optimized_ir.Insert(line_no, new ThreeAddressCode(assign_tac, temp_var, conflict_var, var.Null, vt_size));
                                    node.optimized_ir[line_no + 1].Operand2 = temp_var;
                                    line_no++;
                                    rewrite_inst.tac.CoercionCount++;
                                }

                                if (rewrite_inst.tac.CoercionCount > maxCoercionCountPerInstruction)
                                    throw new TooManyCoercionsException(mtc, rewrite_inst.tac);
                            }
                        }
                        
                        /* var spill_var = state.next_variable++;
                        ThreeAddressCode.Op asssign_tac = GetAssignTac(semantics[conflicting_var]);
                        int vt_size = semantics[conflicting_var].vtype_size;
                        var orig_var = conflicting_var;

                        node.optimized_ir.Insert(line_no, new ThreeAddressCode(asssign_tac, spill_var, orig_var, var.Null, vt_size));
                        node.optimized_ir.Insert(line_no + 2, new ThreeAddressCode(asssign_tac, orig_var, spill_var, var.Null, vt_size));

                        if(inst.UsedLocations().Contains(conflicting_var))
                            throw new Exception("Refer to problem above - not yet implemented");*/

                        if (faulting_instructions != null)
                            faulting_instructions.Add(inst);

                        changes_made = true;
                        return;
                    }
                }
            }
        }

        struct ThreeAddressCodeAndVar
        {
            public ThreeAddressCode tac;
            public int var;
        }

        private List<ThreeAddressCodeAndVar> IdentifyInterferingInstructions(hardware_location hloc, RegisterGraph graph, List<int> list)
        {
            // Return a list of instructions involving the variables in list that reference the fixed location hloc

            List<ThreeAddressCodeAndVar> tacs = new List<ThreeAddressCodeAndVar>();

            foreach (int v in list)
            {
                for (int i = 0; i < graph.graph[v].instrs.Count; i++)
                {
                    ThreeAddressCode inst = graph.graph[v].instrs[i];

                    if (hloc.Equals(GetRecommendedLocation(v, inst)))
                        tacs.Add(new ThreeAddressCodeAndVar { tac = inst, var = v });
                }
            }

            return tacs;
        }

        internal output_opcode GetOutputOpcode(ThreeAddressCode inst)
        {
            if (inst.Operator == ThreeAddressCode.Op.misc)
                return misc_opcodes[((MiscEx)inst).Name];
            else
                return output_opcodes[inst.Operator];
        }

        private hardware_location GetRecommendedLocation(int v, ThreeAddressCode inst)
        {
            output_opcode oo = GetOutputOpcode(inst);

            if ((inst.Operand1.type == var.var_type.LogicalVar) && (inst.Operand1.logical_var == v))
            {
                if (oo.recommended_O1 != null)
                    return oo.recommended_O1;
            }

            if ((inst.Operand2.type == var.var_type.LogicalVar) && (inst.Operand2.logical_var == v))
            {
                if (oo.recommended_O2 != null)
                    return oo.recommended_O2;
            }

            if ((inst.Result.type == var.var_type.LogicalVar) && (inst.Result.logical_var == v))
            {
                if (oo.recommended_R != null)
                    return oo.recommended_R;
            }

            return null;
        }

        private List<hardware_location> GetNotRecommendedLocations(int v, ThreeAddressCode inst)
        {
            output_opcode oo = GetOutputOpcode(inst);

            if ((inst.Operand1.type == var.var_type.LogicalVar) && (inst.Operand1.logical_var == v))
            {
                if (oo.not_recommended_O1 != null)
                    return oo.not_recommended_O1;
            }

            if ((inst.Operand2.type == var.var_type.LogicalVar) && (inst.Operand2.logical_var == v))
            {
                if (oo.not_recommended_O2 != null)
                    return oo.not_recommended_O2;
            }

            if ((inst.Result.type == var.var_type.LogicalVar) && (inst.Result.logical_var == v))
            {
                if (oo.not_recommended_R != null)
                    return oo.not_recommended_R;
            }

            return null;
        }


        internal class RegisterGraph : IVarToHLocProvider
        {
            /* The RegisterGraph is implemented as an adjacency list
             * 
             * Every time an edge is added (a, b), we add b to the list of a's adjacent values,
             *  and add a to the list of b's adjacent values
             */

            public RegisterGraph(Assembler _ass, AssemblerState _state) { ass = _ass; state = _state; }

            public class VarUsage
            {
                public List<int> adj = new List<int>();
                public List<ThreeAddressCode> instrs = new List<ThreeAddressCode>();
                public hardware_location hloc;
                public int graph_id = -1;

                public override string ToString()
                {
                    if (hloc != null)
                        return hloc.ToString();
                    else
                        return "Unassigned location";
                }
            }

            public Dictionary<int, VarUsage> graph = new Dictionary<int, VarUsage>();
            public int first_item = -1;

            public List<int> allocated_vars = new List<int>();
            public List<int> unallocated_vars = new List<int>();

            public AssemblerState state;
            public Assembler ass;

            public int GetNextVarToColour()
            {
                /* Return the order in which the variables should be allocated
                 * 
                 * We use the algorithm:
                 *  - If there are already allocated variables:
                 *      Identify which adjacency of an already allocated variable has the least number of unallocated adjacencies
                 *  - Else:
                 *      Identify the node which is not allocated but has the least number of unallocated adjacencies
                 */

                List<int> to_check = null;
                if (allocated_vars.Count > 0)
                {
                    to_check = new List<int>();
                    foreach (int v in allocated_vars)
                    {
                        foreach (int v2 in graph[v].adj)
                        {
                            if (graph[v2].hloc == null)
                                to_check.Add(v2);
                        }
                    }
                }
                else
                    to_check = unallocated_vars;

                int best_var = -1;
                int best_count = int.MaxValue;
                for (int i = 0; i < to_check.Count; i++)
                {
                    int uncoloured = CountUncolouredsAdjancecies(to_check[i]);
                    if (uncoloured < best_count)
                    {
                        best_var = to_check[i];
                        best_count = uncoloured;
                    }
                }

                return best_var;
            }

            public List<RegisterGraph> SplitGraph()
            {
                List<RegisterGraph> ret = new List<RegisterGraph>();
                int graph_id = 0;

                foreach (KeyValuePair<int, VarUsage> graph_entry in graph)
                {
                    if (graph_entry.Value.graph_id == -1)
                    {
                        RegisterGraph new_graph = new RegisterGraph(ass, state);
                        new_graph.first_item = graph_entry.Key;
                        add_to_graph(new_graph, graph_id, graph_entry.Key, this);
                        ret.Add(new_graph);
                        graph_id++;
                    }
                }

                return ret;
            }

            static void add_to_graph(RegisterGraph new_graph, int graph_id, int node_id, RegisterGraph big_rg)
            {
                VarUsage node = big_rg.graph[node_id];
                node.graph_id = graph_id;
                new_graph.graph.Add(node_id, node);

                foreach (int adj in node.adj)
                {
                    VarUsage next_node = big_rg.graph[adj];
                    if (next_node.graph_id == -1)
                        add_to_graph(new_graph, graph_id, adj, big_rg);
                }
            }

            /*public List<RegisterGraph> SplitGraph()
            {
                /* Split the graph into a list of graphs where all nodes are connected
                 * 
                 * e.g. the graph A-B-C  D-E
                 * would be split into graphs
                 */

            /*    List<RegisterGraph> ret = new List<RegisterGraph>();

                foreach (KeyValuePair<int, VarUsage> graph_entry in graph)
                {
                    bool added = false;

                    foreach (RegisterGraph test_graph in ret)
                    {
                        if (IsLinkedTo(graph_entry.Key, test_graph.first_item))
                        {
                            test_graph.graph.Add(graph_entry.Key, graph_entry.Value);
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        RegisterGraph rg = new RegisterGraph(ass, state);
                        rg.first_item = graph_entry.Key;
                        rg.graph.Add(graph_entry.Key, graph_entry.Value);
                        ret.Add(rg);
                    }
                }

                return ret;
            } */



            protected bool IsLinkedTo(int v1, int v2, List<int> checked_nodes)
            {
                if (v1 == v2)
                    return true;

                if (checked_nodes.Contains(v1))
                    return false;

                checked_nodes.Add(v1);

                foreach (int v1_adj in graph[v1].adj)
                {
                    if (IsLinkedTo(v1_adj, v2, checked_nodes))
                        return true;
                }

                return false;
            }

            public bool IsLinkedTo(int v1, int v2)
            {
                // Return true is there is a possible link between two items
                return IsLinkedTo(v1, v2, new List<int>());
            }

            public void Colour(int v, hardware_location hloc)
            {
                graph[v].hloc = hloc;
                allocated_vars.Add(v);
                unallocated_vars.Remove(v);
            }

            /*public int GetNextVarToColour(int adjacent_to)
            {
                // Return the next uncoloured var with the least adjacency count

                // If adjacent_to == -1 then for all vars
                // Else for the adjacencies of the numbered var

                int best_var = -1;
                int best_var_count = int.MaxValue;

                List<int> to_check = null;

                if (adjacent_to == -1)
                    to_check = unallocated_vars;
                else
                    to_check = graph[adjacent_to].adj;

                for(int i = 0; i < to_check.Count; i++)
                {
                    int uncoloured = CountUncolouredsAdjancecies(to_check[i]);
                    if (uncoloured < best_var_count)
                    {
                        best_var = to_check[i];
                        best_var_count = uncoloured;
                    }
                }

                return best_var;
            }*/

            public int CountUncolouredsAdjancecies(int v)
            {
                int ret = 0;
                List<int> adjs = graph[v].adj;
                foreach (int adj in adjs)
                {
                    if (graph[adj].hloc == null)
                        ret++;
                }
                return ret;
            }

            public int GetNextUncolouredAdjacency(int v)
            {
                List<int> adjs = graph[v].adj;
                foreach (int adj in adjs)
                {
                    if (graph[adj].hloc == null)
                        return adj;
                }
                return -1;
            }

            public bool HasUncolouredAdjacencies(int v)
            {
                if (GetNextUncolouredAdjacency(v) == -1)
                    return true;
                else
                    return false;
            }

            public static int stackloc_comparer(hardware_stackloc a, hardware_stackloc b)
            {
                return a.loc - b.loc;
            }

            public int CanColour(int v, hardware_location hloc)
            {
                if (hloc is hardware_stackloc)
                {
                    // Generate a list of used stack locations (include adjacent variables and local vars)
                    List<hardware_stackloc> used_locs = new List<hardware_stackloc>();
                    foreach (int adj in graph[v].adj)
                    {
                        if (graph[adj].hloc is hardware_stackloc)
                            used_locs.Add(graph[adj].hloc as hardware_stackloc);
                    }
                    foreach (hardware_location lv_loc in state.local_vars)
                    {
                        if (lv_loc is hardware_stackloc)
                            used_locs.Add(lv_loc as hardware_stackloc);
                    }
                    used_locs.Sort(stackloc_comparer);

                    // Find a free gap in the stack to use

                    int loc = -1;
                    int prev_end = 0;
                    int search_size = ((hardware_stackloc)hloc).size;
                    int stack_align = ass.GetSizeOfPointer();

                    for (int i = 0; i < used_locs.Count; i++)
                    {
                        // Try and fit us in at prev_end
                        if ((used_locs[i].loc - prev_end) >= search_size)
                        {
                            loc = prev_end;
                            break;
                        }

                        // Else move on to the next
                        prev_end = used_locs[i].loc + used_locs[i].size;
                        prev_end = util.align(prev_end, stack_align);
                    }

                    // If no space was found, add us on to the end of the stack
                    if (loc == -1)
                        loc = prev_end;

                    ((hardware_stackloc)hloc).loc = loc;

                    // Update the maximum stack space used count if necessary
                    if((loc + search_size) > state.stack_space_used)
                        state.stack_space_used = util.align(loc + search_size, ass.GetSizeOfPointer());

                    return -1;
                }
                else
                {
                    foreach (int adj in graph[v].adj)
                    {
                        if (hloc.Equals(graph[adj].hloc))
                            return adj;
                    }
                }
                return -1;
            }

            public void AddVarUsage(int v, ThreeAddressCode inst)
            {
                List<ThreeAddressCode> tac_list = null;

                if (graph.ContainsKey(v))
                    tac_list = graph[v].instrs;
                else
                {
                    VarUsage vu = new VarUsage();
                    graph.Add(v, vu);
                    tac_list = vu.instrs;
                }

                if (!tac_list.Contains(inst))
                    tac_list.Add(inst);
            }

            protected void AddAdjacency(int v1, int v2)
            {
                // Add v2 to the list of v1's adjacencies
                List<int> adj_list = null;
                if (graph.ContainsKey(v1))
                    adj_list = graph[v1].adj;
                else
                {
                    VarUsage v = new VarUsage();
                    adj_list = v.adj;
                    graph.Add(v1, v);
                }

                if (!adj_list.Contains(v2))
                    adj_list.Add(v2);
            }

            public void AddEdge(int v1, int v2)
            {
                if (v1 == v2)
                    return;

                // Add the adjacency both ways
                AddAdjacency(v1, v2);
                AddAdjacency(v2, v1);
            }

            public string _readable { get { return this.ToString(); } }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                foreach (KeyValuePair<int, VarUsage> kvp in graph)
                {
                    sb.Append(kvp.Key.ToString() + ": ");

                    int i;
                    for (i = 0; i < kvp.Value.adj.Count; i++)
                    {
                        if (i != 0)
                            sb.Append(", ");
                        sb.Append(kvp.Value.adj[i].ToString());
                    }
                    sb.Append(" ");
                    sb.Append(Environment.NewLine);
                }

                return sb.ToString();
            }

            public hardware_location GetLocationOf(var v)
            {
                return graph[v.logical_var].hloc;
            }
        }
    }
}
