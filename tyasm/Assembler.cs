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

namespace tyasm
{
	partial class Assembler
	{
        protected class AssemblerState
        {
            public AssemblerState(binary_library.IBinaryFile binary_file) { file = binary_file; SelectSection(".text"); }

            public Dictionary<string, binary_library.ISection> output_sections = new Dictionary<string, binary_library.ISection>();
            public Dictionary<string, int> output_pointer = new Dictionary<string, int>();
            public Dictionary<string, List<OutputBlock>> output_blocks = new Dictionary<string, List<OutputBlock>>();

            public Dictionary<string, binary_library.SymbolType> sym_types = new Dictionary<string, binary_library.SymbolType>();
            public Dictionary<string, binary_library.SymbolObjectType> sym_obj_types = new Dictionary<string, binary_library.SymbolObjectType>();
            public List<string> extern_labels = new List<string>();

            binary_library.IBinaryFile file;

            string cur_section;

            public string CurrentSectionName { get { return cur_section; } set { SelectSection(value); } }
            public binary_library.ISection CurrentSection { get { return output_sections[cur_section]; } }
            public int CurrentPointer { get { return output_pointer[cur_section]; } set { output_pointer[cur_section] = value; } }
            public List<OutputBlock> OutputBlocks { get { return output_blocks[cur_section]; } }

            public binary_library.ISection SelectSection(string name)
            {
                if (output_sections.ContainsKey(name))
                {
                    cur_section = name;
                    return output_sections[name];
                }
                else
                {
                    output_sections[name] = file.CreateSection();
                    output_pointer[name] = 0;
                    output_blocks[name] = new List<OutputBlock>();
                    cur_section = name;
                    return output_sections[name];
                }
            }

            public binary_library.Bitness cur_bitness = binary_library.Bitness.BitsUnknown;
        }

        protected class Location
        {
            public enum LocationModifierType { ValueOf, ContentsOf };
            public LocationModifierType ModifierType = LocationModifierType.ValueOf;
            public enum LocationType { Register, Label, Number, Location, None };
            public LocationType AType = LocationType.None;
            public object A;
            public LocationType BType = LocationType.None;
            public object B;
            public enum NumOpType { Plus, Minus, Multiply, Divide, None };
            public NumOpType NumOp = NumOpType.None;
        }

        protected abstract class OutputBlock
        {

        }

        protected class CodeBlock : OutputBlock
        {
            public IList<byte> Code;
        }

        protected class Label : OutputBlock
        {
            public binary_library.SymbolType Type;
            public binary_library.SymbolObjectType ObjectType;

            public string Name;
        }

        protected class Relocation : OutputBlock
        {
            public binary_library.IRelocationType rel_type;
            public string Target;
            public int Addend;
        }

        protected virtual AssemblerState CreateAssemblerState(binary_library.IBinaryFile file) { return new AssemblerState(file); }
        public Assembler()
        {
            InitOpcodes();
        }

        public void Assemble(AsmParser.ParseOutput input, binary_library.IBinaryFile output)
        {
            AssemblerState state = CreateAssemblerState(output);

            if (input.Type != AsmParser.ParseOutput.OutputType.Block)
                throw new Exception("Expected Block");

            foreach (AsmParser.ParseOutput line in input.Children)
            {
                if (line.Type != AsmParser.ParseOutput.OutputType.Line)
                    throw new Exception("Expected Line");

                foreach (AsmParser.ParseOutput line_entry in line.Children)
                {
                    switch (line_entry.Type)
                    {
                        case AsmParser.ParseOutput.OutputType.DirectiveCommand:
                            AssembleDirective(line_entry, state);
                            break;
                        case AsmParser.ParseOutput.OutputType.OpCommand:
                            AssembleOperation(line_entry, state);
                            break;
                        case AsmParser.ParseOutput.OutputType.Label:
                            AssembleLabel(line_entry, state);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

        }

        private void AssembleLabel(AsmParser.ParseOutput line_entry, AssemblerState state)
        {
            Label l = new Label { Name = line_entry.Name, ObjectType = binary_library.SymbolObjectType.Unknown, Type = binary_library.SymbolType.Local };
            if (state.sym_types.ContainsKey(line_entry.Name))
                l.Type = state.sym_types[line_entry.Name];
            if (state.sym_obj_types.ContainsKey(line_entry.Name))
                l.ObjectType = state.sym_obj_types[line_entry.Name];
            state.OutputBlocks.Add(l);
        }

        private void AssembleOperation(AsmParser.ParseOutput line_entry, AssemblerState state)
        {
            // Interpret the operation
            List<AsmParser.ParseOutput> prefixes = new List<AsmParser.ParseOutput>();
            AsmParser.ParseOutput op = null;
            List<AsmParser.ParseOutput> parameters = new List<AsmParser.ParseOutput>();

            foreach (AsmParser.ParseOutput p in line_entry.Children)
            {
                switch (p.Type)
                {
                    case AsmParser.ParseOutput.OutputType.Operation:
                        if (op != null)
                            throw new Exception("More than one operation per line");
                        op = p;
                        break;
                    case AsmParser.ParseOutput.OutputType.Prefix:
                        prefixes.Add(p);
                        break;
                    case AsmParser.ParseOutput.OutputType.Parameter:
                        if (p.Children.Count > 1)
                            throw new Exception("Only one parameter child allowed");
                        parameters.Add(p.Children[0]);
                        break;
                    default:
                        throw new Exception("Unsupported operation argument type: " + p.ToString());
                }
            
            }

            // Now get the appropriate operations
            OpcodeImplementation.OpcodeDelegate del = FindOperation(op, prefixes, parameters, state);
            if ((del == null) || (!del(state.OutputBlocks, op, prefixes, parameters, state)))
                throw new Exception("Unable to assemble: " + line_entry.ToString());
        }

        private OpcodeImplementation.OpcodeDelegate FindOperation(AsmParser.ParseOutput op, List<AsmParser.ParseOutput> prefixes, List<AsmParser.ParseOutput> parameters, AssemblerState state)
        {
            if (!opcodes.ContainsKey(op.Name))
                return null;
            List<OpcodeImplementation> ops = opcodes[op.Name];
            if (ops == null)
                return null;
            foreach (OpcodeImplementation oi in ops)
            {
                // Match prefixes
                bool allowed = true;
                foreach (AsmParser.ParseOutput prefix in prefixes)
                {
                    if (!oi.AllowedPrefixes.Contains(prefix.Name.ToLower()))
                    {
                        allowed = false;
                        break;
                    }
                }
                if(!allowed)
                    continue;

                // Match parameters
                if (parameters.Count != oi.ParamConstraints.Count)
                    continue;

                for (int i = 0; i < parameters.Count; i++)
                {
                    Location l = ParseLocation(parameters[i]);
                    if (!MatchConstraint(oi.ParamConstraints[i], l))
                    {
                        allowed = false;
                        break;
                    }
                }
                if (!allowed)
                    continue;

                return oi.emitter;
                    
            }
            return null;
        }

        protected virtual Location ParseLocation(AsmParser.ParseOutput param)
        {
            switch (param.Type)
            {
                case AsmParser.ParseOutput.OutputType.Label:
                    return new Location { AType = Location.LocationType.Label, A = param.Name };
                case AsmParser.ParseOutput.OutputType.Number:
                    return new Location { AType = Location.LocationType.Number, A = param.Name };
                case AsmParser.ParseOutput.OutputType.Register:
                    return new Location { AType = Location.LocationType.Register, A = param.Name };
                case AsmParser.ParseOutput.OutputType.ContentsOf:
                    AsmParser.ParseOutput p = param.Children[0];
                    if (p.Type != AsmParser.ParseOutput.OutputType.Parameter)
                        throw new NotSupportedException();
                    AsmParser.ParseOutput e = p.Children[0];
                    if (e.Type != AsmParser.ParseOutput.OutputType.Expression)
                        throw new NotSupportedException();
                    Location l = ParseLocation(e.Children[0]);
                    l.ModifierType = Location.LocationModifierType.ContentsOf;
                    if (e.Children.Count > 1)
                    {
                        if (e.Children[1].Type != AsmParser.ParseOutput.OutputType.NumOp)
                            throw new NotSupportedException();
                        AsmParser.ParseOutput nop = e.Children[1];
                        if (nop.Name == "+")
                            l.NumOp = Location.NumOpType.Plus;
                        else if (nop.Name == "-")
                            l.NumOp = Location.NumOpType.Minus;
                        else
                            throw new NotSupportedException();
                        Location b = ParseLocation(e.Children[2]);
                        l.BType = b.AType;
                        l.B = b.A;

                    }
                    return l;
                default:
                    throw new NotSupportedException();
            }

        }

        protected virtual bool MatchConstraint(ParameterConstraint constraint, Location l)
        {
            if ((constraint.Type == ParameterConstraint.ConstraintType.Const) && (l.ModifierType == Location.LocationModifierType.ValueOf) &&
                (l.AType == Location.LocationType.Number) && (l.NumOp == Location.NumOpType.None))
            {
                // TODO match bitness etc
                return true;
            }
            if ((constraint.Type == ParameterConstraint.ConstraintType.Const) && (l.ModifierType == Location.LocationModifierType.ValueOf) &&
                (l.AType == Location.LocationType.Label) && (l.NumOp == Location.NumOpType.None))
            {
                // TODO match bitness etc
                return true;
            }
            return false;
        }

        private void AssembleDirective(AsmParser.ParseOutput line_entry, AssemblerState state)
        {
            AsmParser.ParseOutput dir = line_entry.Children[0];

            // Interpret directives
            if ((dir.Name == "weak") || (dir.Name == "global"))
            {
                // these are of the form <weak | global> <label> [:function | :data]
                string t_name = line_entry.Children[1].Children[0].Name;
                binary_library.SymbolType st = binary_library.SymbolType.Undefined;
                binary_library.SymbolObjectType sot = binary_library.SymbolObjectType.Unknown;

                if (dir.Name == "weak")
                    st = binary_library.SymbolType.Weak;
                else if (dir.Name == "global")
                    st = binary_library.SymbolType.Global;

                if (line_entry.Children.Count >= 3)
                {
                    string ot_name = line_entry.Children[2].Children[0].Name;
                    if (ot_name == "function")
                        sot = binary_library.SymbolObjectType.Function;
                    else if (ot_name == "data")
                        sot = binary_library.SymbolObjectType.Object;
                }

                if (st != binary_library.SymbolType.Undefined)
                    state.sym_types[t_name] = st;
                if (sot != binary_library.SymbolObjectType.Unknown)
                    state.sym_obj_types[t_name] = sot;
            }
            else if (dir.Name == "extern")
            {
                string t_name = line_entry.Children[1].Name;
                if (!state.extern_labels.Contains(t_name))
                    state.extern_labels.Add(t_name);
            }
            else if (dir.Name == "bits16")
                state.cur_bitness = binary_library.Bitness.Bits16;
            else if (dir.Name == "bits32")
                state.cur_bitness = binary_library.Bitness.Bits32;
            else if (dir.Name == "bits64")
                state.cur_bitness = binary_library.Bitness.Bits64;
            else
                throw new NotImplementedException();
        }
	}
}
