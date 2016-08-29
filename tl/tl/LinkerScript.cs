/* Copyright (C) 2013-2016 by John Cronin
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
        public LinkerScript(string ScriptName) { name = ScriptName; }

        public List<ScriptEntry> Script = new List<ScriptEntry>();
        string name = "";

        public string Name { get { return name; } }

        struct SizeAlign { public int Size; public int Align; public ISymbol Sym; }

        public void RunScript(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs)
        {
            // First, identify the maximum sizes of common symbols
            Dictionary<string, SizeAlign> comm_syms = new Dictionary<string, SizeAlign>();
            foreach(var iput in inputs)
            {
                var comm = iput.GetCommonSection();
                if(comm != null)
                {
                    for(int i = 0; i < comm.GetSymbolCount(); i++)
                    {
                        var comm_sym = comm.GetSymbol(i);
                        if(comm_sym != null)
                        {
                            string name = comm_sym.Name;
                            if (comm_sym.Type == SymbolType.Local)
                                name = iput.Filename + "." + name;
                            if (!comm_syms.ContainsKey(name) ||
                                comm_syms[name].Size < comm_sym.Size)
                                comm_syms[name] = new SizeAlign { Size = (int)comm_sym.Size, Align = (int)comm_sym.Offset, Sym = comm_sym };
                        }
                    }
                }
            }
            int comm_len = 0;
            foreach (var k in comm_syms)
            {
                if ((comm_len % k.Value.Align) != 0)
                    comm_len = (comm_len / k.Value.Align) * k.Value.Align + k.Value.Align;
                comm_len += k.Value.Size;
            }

            // Run the script
            LinkerScriptState state = new LinkerScriptState();
            state.comm_length = comm_len;
            state.cur_section = output.GetGlobalSection();
            foreach (ScriptEntry se in Script)
                se.DoCommand(output, inputs, state);

            if (Program.is_reloc)
                output.IsExecutable = false;
            else
                output.IsExecutable = true;

            Dictionary<string, int> globl_syms = new Dictionary<string, int>();
            int sym_idx = 0;
            if (output.IsExecutable)
            {
                // Resolve weak symbols
                while (sym_idx < output.GetSymbolCount())
                {
                    ISymbol sym = output.GetSymbol(sym_idx);
                    if (sym.Type == SymbolType.Weak)
                    {
                        // Whether to promote to a global symbol
                        bool for_removal = false;

                        // Have we found the appropriate global symbol before?
                        if (globl_syms.ContainsKey(sym.Name))
                            for_removal = true;  // for removal
                        else
                        {
                            // Search from here onwards looking for a global
                            //  symbol with the same name
                            for (int sym_idx_2 = sym_idx; sym_idx_2 < output.GetSymbolCount(); sym_idx_2++)
                            {
                                ISymbol sym_2 = output.GetSymbol(sym_idx_2);
                                if (sym_2.Type == SymbolType.Global &&
                                    sym_2.Name == sym.Name)
                                {
                                    globl_syms[sym.Name] = sym_idx_2;
                                    for_removal = true;
                                    break;
                                }
                            }
                        }

                        if (for_removal)
                        {
                            output.RemoveSymbol(sym_idx);
                            continue;
                        }
                        else
                        {
                            // Promote to a global symbol
                            sym.Type = SymbolType.Global;
                            globl_syms[sym.Name] = sym_idx;
                        }
                    }
                    else if (sym.Type == SymbolType.Global)
                        globl_syms[sym.Name] = sym_idx;

                    sym_idx++;
                }

                // Resolve common symbols.  If a global symbol is defined for
                //  this, we use that, else we allocate a space in bss for it
                ulong cur_comm_sym_offset = state.comm_offset;
                foreach (var k in comm_syms.Keys)
                {
                    if (!globl_syms.ContainsKey(k))
                    {
                        // define a symbol in the common section for this
                        if (state.comm_sect == null)
                            throw new Exception("Common sections used but no common section defined in linker script");

                        uint align = (uint)comm_syms[k].Align;
                        if ((cur_comm_sym_offset % align) != 0)
                            cur_comm_sym_offset = (cur_comm_sym_offset / align) * align + align;

                        ISymbol comm_sym = comm_syms[k].Sym;
                        //comm_sym.DefinedIn = state.comm_sect;
                        comm_sym.Offset = cur_comm_sym_offset;
                        comm_sym.Name = k;
                        cur_comm_sym_offset += (uint)comm_syms[k].Size;
                    }
                }
            }

            // Now iterate through the data, saving to the appropriate sections
            foreach (KeyValuePair<binary_library.ISection, List<byte>> kvp in state.data)
            {
                if (kvp.Key != null)
                {
                    if (kvp.Key.HasData)
                    {
                        kvp.Key.Length = kvp.Value.Count;
                        for (int i = 0; i < kvp.Value.Count; i++)
                            kvp.Key.Data[i] = kvp.Value[i];
                    }
                }
            }

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
                        if(output.IsExecutable == false)
                        {
                            // Create a new relocation for the output file
                            IRelocation new_copy_reloc = output.CreateRelocation();
                            new_copy_reloc.DefinedIn = state.input_section_locations[reloc.DefinedIn].OutputSection;
                            new_copy_reloc.Offset = reloc.Offset + state.input_section_locations[reloc.DefinedIn].OutputSectionOffset;
                            new_copy_reloc.Addend = reloc.Addend;
                            new_copy_reloc.References = reloc.References;
                            new_copy_reloc.Type = reloc.Type;
                            output.AddRelocation(new_copy_reloc);
                            continue;
                        }
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
                                throw new Exception("Label '" + reloc_target.Name + "' not found");

                            target_section = found_target.DefinedIn;
                            target_section_offset = found_target.Offset;
                        }
                        else if (reloc_target.DefinedIn == reloc_target.DefinedIn.File.GetCommonSection())
                        {
                            // Get full name of symbol
                            string name = reloc_target.Name;
                            if (reloc_target.Type == SymbolType.Local)
                                name = reloc_target.DefinedIn.File.Filename + "." + name;
                            target_section = state.comm_sect;

                            if (globl_syms.ContainsKey(name))
                            {
                                ISymbol found_target = output.FindSymbol(reloc_target.Name);
                                if (found_target == null)
                                    throw new Exception("Label '" + reloc_target.Name + "' not found");

                                target_section = found_target.DefinedIn;
                                target_section_offset = found_target.Offset;
                            }
                            else
                            {
                                target_section = state.comm_sect;
                                target_section_offset = reloc_target.Offset;
                            }
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
                        new_reloc.References.Name = reloc_target.Name;
                        new_reloc.Type = reloc.Type;
                        
                        // Evaluate the relocation
                        long val = new_reloc.Type.Evaluate(new_reloc);

                        // Does it fit in the provided space?
                        bool fits = true;
                        ulong uval = BitConverter.ToUInt64(BitConverter.GetBytes(val), 0);
                        var t = new_reloc.Type;
                        ulong ext_mask = 0xffffffffffffffffUL << (t.BitLength + t.BitOffset);
                        if ((t.BitLength + t.BitOffset) >= 64)
                            ext_mask = 0;
                        if (new_reloc.Type.IsSigned)
                        {
                            var sign_bit = (uval >> (t.BitLength + t.BitOffset - 1)) & 0x1;
                            if (sign_bit == 1 && (uval & ext_mask) != ext_mask)
                                fits = false;
                            else if (sign_bit == 0 && (uval & ext_mask) != 0)
                                fits = false;
                        }
                        else if ((uval & ext_mask) != 0)
                            fits = false;
                        if (fits == false)
                            throw new Exception("Relocation truncated to fit: " +
                                reloc_target.Name + " against " +
                                t.Name);

                        // Build the new value to reinsert in the data stream
                        byte[] orig = new byte[8];
                        for (int idx = 0; idx < new_reloc.Type.Length; idx++)
                            orig[idx] = new_reloc.DefinedIn.Data[(int)new_reloc.Offset + idx];
                        ulong orig_val = BitConverter.ToUInt64(orig, 0);

                        // Mask out those bits we don't want
                        orig_val &= new_reloc.Type.KeepMask;

                        // Only set those bits we want
                        uval &= new_reloc.Type.SetMask;
                        uval |= orig_val;

                        byte[] uvalb = BitConverter.GetBytes(uval);
                        for (int idx = 0; idx < new_reloc.Type.Length; idx++)
                            new_reloc.DefinedIn.Data[(int)new_reloc.Offset + idx] = uvalb[idx];                        
                    }
                }
            }

            // Remove local and undefined symbols from the output file
            sym_idx = 0;
            while(sym_idx < output.GetSymbolCount())
            {
                ISymbol sym = output.GetSymbol(sym_idx);
                if (sym.Type == SymbolType.Local || sym.Type == SymbolType.Undefined)
                    output.RemoveSymbol(sym_idx);
                else
                    sym_idx++;
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

            public int comm_length = 0;
            public ulong comm_offset = 0;
            public ISection comm_sect = null;

            // ELF dynamic sections
            public ISection dyn_sect = null;
            public ISection hash_sect = null;
        }

        public abstract class ScriptEntry
        {
            public abstract void DoCommand(binary_library.IBinaryFile output, IList<binary_library.IBinaryFile> inputs, LinkerScriptState state);
        }

        public class AddInputFile : ScriptEntry
        {
            string f;

            public AddInputFile(string ifile) { f = ifile; }
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                binary_library.elf.ElfFile ef = new binary_library.elf.ElfFile();
                ef.Filename = f;
                ef.Read();
                inputs.Add(ef);
            }
        }

        public class GenerateELFHash : ScriptEntry
        {
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                Program.gen_hash = true;
            }
        }

        public class AddData : ScriptEntry
        {
            IEnumerable<byte> d;

            public AddData(IEnumerable<byte> data) { d = data; }

            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                foreach (byte b in d)
                    state.data[state.cur_section].Add(b);
            }
        }

        public class AddComment : ScriptEntry
        {
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                var d = Encoding.ASCII.GetBytes(Program.comment);
                foreach (byte b in d)
                    state.data[state.cur_section].Add(b);
            }
        }

        public class EntryPoint : ScriptEntry
        {
            string epoint;

            public EntryPoint(string entry_point) { epoint = entry_point; }
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                output.EntryPoint = epoint;
            }
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
                        if (new_offset != 0)
                        {
                            if ((state.cur_offset % new_offset) != 0)
                                state.cur_offset = (state.cur_offset / new_offset) * new_offset + new_offset;
                        }
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
                sym.Type = binary_library.SymbolType.Global;
                sym.DefinedIn = state.cur_section;
                if (sym.DefinedIn == null)
                {
                    output.AddSymbol(sym);
                    sym.Offset = state.cur_offset;
                }
                else
                    sym.Offset = state.cur_sect_offset;
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
            int size = 0;

            public enum StandardSection { Text, Data, Rodata, Bss, ElfDynamic, ElfHash };

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
                    case StandardSection.ElfDynamic:
                        name = ".dynamic";
                        addr_align = 0;
                        isalloc = true;
                        iswrite = false;
                        isexec = false;
                        hasdata = true;
                        size = -1; // max size for Elf64 dynamic section
                        break;
                    case StandardSection.ElfHash:
                        name = ".hash";
                        addr_align = 0;
                        isalloc = true;
                        iswrite = false;
                        isexec = false;
                        hasdata = true;
                        size = -2;  // calculated later                        
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

                if(hasdata && size < 0)
                {
                    switch(size)
                    {
                        case -1:
                            // elf dynamic
                            if (output.Bitness == Bitness.Bits64)
                                state.cur_section.Length = 464;
                            else
                                state.cur_section.Length = 192;
                            break;
                        case -2:
                            // elf hash
                            int ssize = 8;      // nbuckets + nchain
                            ssize += binary_library.elf.ElfFile.CalculateBucketCount(output.GetSymbolCount()) * 4;
                            ssize += output.GetSymbolCount() * 4;
                            state.cur_section.Length = ssize;
                            break;
                    }
                }
            }
        }

        public class CommonSection : ScriptEntry
        {
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                state.comm_offset = state.cur_sect_offset;
                state.cur_sect_offset += (ulong)state.comm_length;
                state.comm_sect = state.cur_section;

                var osect = state.cur_section;
                if (osect.HasData)
                {
                    for (int i = 0; i < state.comm_length; i++)
                        state.data[osect].Add(0);
                }
                else
                    osect.Length += state.comm_length;
            }
        }

        public class ElfDynamicSection : ScriptEntry
        {
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                state.dyn_sect = state.cur_section;
            }
        }

        public class ElfHashSection : ScriptEntry
        {
            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                state.hash_sect = state.cur_section;
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
                    else
                        osect.Length += sect.Length;

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
                        new_sym.ObjectType = sym.ObjectType;
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
                if (state.cur_section.HasData)
                    state.cur_offset += (ulong)state.data[state.cur_section].Count;
                else
                    state.cur_offset += (ulong)state.cur_section.Length;
                state.cur_section = state.prev_section;
            }
        }

        public class SetExecutable : ScriptEntry
        {
            bool is_exec = false;

            public SetExecutable(bool IsExec) { is_exec = IsExec; }

            public override void DoCommand(IBinaryFile output, IList<IBinaryFile> inputs, LinkerScriptState state)
            {
                if (Program.is_reloc == false && is_exec == false)      // Command line overrides this
                    Program.is_reloc = true;
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
