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
using System.Linq;
using System.Text;
using binary_library;
using System.Text.RegularExpressions;

namespace tl
{
    class LinkerScript
    {
        public List<ScriptEntry> Script = new List<ScriptEntry>();

        public void RunScript(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs)
        {
            LinkerScriptState state = new LinkerScriptState();
            state.cur_section = output.GetGlobalSection();

            // Run the script
            foreach (ScriptEntry se in Script)
                se.DoCommand(output, inputs, state);

            // Resolve relocations
            foreach (IBinaryFile ifile in inputs)
            {
                int reloc_count = ifile.GetRelocationCount();
                for (int i = 0; i < reloc_count; i++)
                {
                    IRelocation reloc = ifile.GetRelocation(i);
                    
                    // Is the location of the relocation included in the output?
                    if (state.included_sections.Contains(reloc.DefinedIn))
                    {
                        // Where is the value to be relocated?
                        LinkerScriptState.InputSectionLocation isl = state.input_section_locations[reloc.DefinedIn];

                        // Get the target of the relocation
                        ISymbol reloc_target = reloc.References;
                        ISection target_section = null;
                        ulong target_section_offset = 0;
                        if (reloc_target.DefinedIn == null)
                        {
                            // Try and find the requested symbol
                            ISymbol found_target = output.FindSymbol(reloc_target.Name);
                            if (found_target == null)
                                throw new Exception();

                            target_section = found_target.DefinedIn;
                            target_section_offset = found_target.Offset;
                        }
                        else
                        {
                            // Use the one stored in the relocation
                            // First find out where the input section is located in the output
                            if (!state.included_sections.Contains(reloc_target.DefinedIn))
                                throw new Exception();
                            LinkerScriptState.InputSectionLocation target_isl = state.input_section_locations[reloc_target.DefinedIn];
                            target_section = target_isl.OutputSection;
                            target_section_offset = target_isl.OutputSectionOffset + reloc_target.Offset;
                        }

                        // Create a new relocation for the output file
                        IRelocation new_reloc = output.CreateRelocation();
                        new_reloc.DefinedIn = isl.OutputSection;
                        new_reloc.Offset = isl.OutputSectionOffset + reloc.Offset;
                        new_reloc.Addend = reloc.Addend;
                        new_reloc.References = output.CreateSymbol();
                        new_reloc.References.DefinedIn = target_section;
                        new_reloc.References.Offset = target_section_offset;
                        new_reloc.References.Size = reloc_target.Size;
                        new_reloc.Type = reloc.Type;
                        
                        // Evaluate the relocation
                        long val = new_reloc.Type.Evaluate(new_reloc);
                    }
                }
            }

            // Now iterate through the data, saving to the appropriate sections
            foreach (KeyValuePair<binary_library.ISection, List<byte>> kvp in state.data)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.Length = kvp.Value.Count;
                    if (kvp.Key.HasData)
                    {
                        for (int i = 0; i < kvp.Value.Count; i++)
                            kvp.Key.Data[i] = kvp.Value[i];
                    }
                }
            }
        }

        public class LinkerScriptState
        {
            public ulong cur_offset = 0;
            public ulong cur_sect_offset = 0;
            public binary_library.ISection cur_section = null;
            public binary_library.ISection prev_section = null;
            public Dictionary<binary_library.ISection, List<byte>> data = new Dictionary<binary_library.ISection, List<byte>>();
            public List<binary_library.ISection> included_sections = new List<binary_library.ISection>();

            public class InputSectionLocation { public ISection OutputSection; public ulong OutputSectionOffset; }
            public Dictionary<ISection, InputSectionLocation> input_section_locations = new Dictionary<ISection, InputSectionLocation>();
            public List<IRelocation> relocs = new List<IRelocation>();
        }

        public abstract class ScriptEntry
        {
            public abstract void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state);
        }

        public class UpdateOffset : ScriptEntry
        {
            ulong new_offset;
            public enum UpdateType { Add, Subtract, Set, Align };
            UpdateType _type;

            public UpdateOffset(UpdateType type, ulong offset) { _type = type; new_offset = offset; }
            public override void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state)
            {
                switch(_type)
                {
                    case UpdateType.Add:
                        state.cur_offset += new_offset;
                        break;
                    case UpdateType.Set:
                        state.cur_offset = new_offset;
                        break;
                    case UpdateType.Subtract:
                        state.cur_offset -= new_offset;
                        break;
                    case UpdateType.Align:
                        if ((state.cur_offset % new_offset) != 0)
                            state.cur_offset = (state.cur_offset / new_offset) * new_offset + new_offset;
                        break;
                }
            }
        }

        public class DefineSymbol : ScriptEntry
        {
            string name;
            public DefineSymbol(string Name) { name = Name; }

            public override void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state)
            {
                binary_library.ISymbol sym = output.CreateSymbol();
                sym.Name = name;
                sym.Offset = state.cur_offset;
                sym.Type = binary_library.SymbolType.Global;
                sym.DefinedIn = state.cur_section;
                output.AddSymbol(sym);
            }
        }

        public class DefineSection : ScriptEntry
        {
            string name;
            long addr_align;
            bool isalloc;
            bool iswrite;
            bool isexec;
            bool hasdata;

            public enum StandardSection { Text, Data, Rodata, Bss };

            public DefineSection(string Name, long AddrAlign, bool IsAlloc, bool IsWrite, bool IsExec, bool HasData)
            { name = Name; addr_align = AddrAlign; isalloc = IsAlloc; iswrite = IsWrite; isexec = IsExec; hasdata = HasData; }

            public DefineSection(StandardSection section)
            {
                switch (section)
                {
                    case StandardSection.Text:
                        name = ".text";
                        addr_align = 4;
                        isalloc = true;
                        iswrite = false;
                        isexec = true;
                        hasdata = true;
                        break;
                    case StandardSection.Data:
                        name = ".data";
                        addr_align = 0;
                        isalloc = true;
                        iswrite = true;
                        isexec = false;
                        hasdata = true;
                        break;
                    case StandardSection.Rodata:
                        name = ".rodata";
                        addr_align = 0;
                        isalloc = true;
                        iswrite = false;
                        isexec = false;
                        hasdata = true;
                        break;
                    case StandardSection.Bss:
                        name = ".bss";
                        addr_align = 0;
                        isalloc = true;
                        iswrite = true;
                        isexec = false;
                        hasdata = false;
                        break;
                }
            }

            public override void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state)
            {
                state.prev_section = state.cur_section;

                binary_library.ISection section = output.CreateSection();
                section.Name = name;
                section.AddrAlign = addr_align;
                section.IsAlloc = isalloc;
                section.IsWriteable = iswrite;
                section.IsExecutable = isexec;
                section.HasData = hasdata;
                section.LoadAddress = state.cur_offset;

                state.cur_section = section;
                state.data[section] = new List<byte>();
                state.cur_sect_offset = 0;
            }
        }

        public class InputFileSection : ScriptEntry
        {
            public enum InputFileSelection { Specific, All, AllNotSpecified };
            public string input_file;
            public string input_section;
            public InputFileSelection selection;

            public InputFileSection(string section) { input_file = null; input_section = section; selection = InputFileSelection.All; }

            public override void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state)
            {
                // Generate a list of sections to include
                IList<binary_library.ISection> sections = null;

                Regex r = new Regex(WildcardToRegex(input_section));

                switch (selection)
                {
                    case InputFileSelection.All:
                        sections = new List<binary_library.ISection>();
                        foreach (binary_library.IBinaryFile ifile in inputs)
                        {
                            binary_library.ISection sect = ifile.FindSection(r);
                            if (sect != null)
                                sections.Add(sect);
                        }
                        break;
                    case InputFileSelection.AllNotSpecified:
                        sections = new List<binary_library.ISection>();
                        foreach (binary_library.IBinaryFile ifile in inputs)
                        {
                            binary_library.ISection sect = ifile.FindSection(r);
                            if ((sect != null) && (!state.included_sections.Contains(sect)))
                                sections.Add(sect);
                        }
                        break;
                    case InputFileSelection.Specific:
                        sections = new List<binary_library.ISection>();
                        foreach (binary_library.IBinaryFile ifile in inputs)
                        {
                            System.IO.FileInfo fi = new System.IO.FileInfo(ifile.Filename);
                            if (fi.Exists && (fi.Name == input_file))
                            {
                                binary_library.ISection sect = ifile.FindSection(r);
                                if ((sect != null) && (!state.included_sections.Contains(sect)))
                                    sections.Add(sect);
                            }
                        }
                        break;                        
                }

                // Write the sections data to the section cache and its symbols to the output file
                binary_library.ISection osect = state.cur_section;
                foreach (binary_library.ISection sect in sections)
                {
                    // Write the data
                    if (osect.HasData)
                    {
                        if (sect.HasData)
                        {
                            foreach (byte b in sect.Data)
                                state.data[osect].Add(b);
                        }
                        else
                        {
                            for (int i = 0; i < sect.Length; i++)
                                state.data[osect].Add(0);
                        }
                    }

                    // Write the symbols
                    int sym_count = sect.GetSymbolCount();
                    for (int i = 0; i < sym_count; i++)
                    {
                        ISymbol sym = sect.GetSymbol(i);
                        ISymbol new_sym = output.CreateSymbol();
                        new_sym.Name = sym.Name;
                        new_sym.DefinedIn = osect;
                        new_sym.Offset = state.cur_sect_offset + sym.Offset;
                        new_sym.Size = sym.Size;
                        new_sym.Type = sym.Type;
                        osect.AddSymbol(new_sym);
                    }

                    // Save where this section is loaded to
                    state.input_section_locations[sect] = new LinkerScriptState.InputSectionLocation { OutputSection = osect, OutputSectionOffset = state.cur_sect_offset };
                    state.cur_sect_offset += (ulong)sect.Length;
                }

                // Add the sections to the list of included sections
                state.included_sections.AddRange(sections);
            }
        }

        public class EndSection : ScriptEntry
        {
            public override void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state)
            {
                output.AddSection(state.cur_section);
                state.cur_offset += (ulong)state.data[state.cur_section].Count;
                state.cur_section = state.prev_section;
            }
        }

        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }
    }
}
