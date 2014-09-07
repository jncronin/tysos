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
using System.IO;
using libtysila;

/* An interactive mode for tysila */

namespace tysila
{
    class Interactive : Assembler.IrDumpFeedback, CompilerRunner.IDoCompileFeedback
    {
        internal Assembler.TypeToCompile ?cur_type = null;
        internal Assembler.MethodToCompile ?cur_method = null;
        internal Metadata module = null;
        internal Assembler ass;
        internal Layout.Interface iface = null;
        internal IOutputFile writer = new DummyWriter();

        public Interactive() { }
        public Interactive(Assembler assembler) { ass = assembler; }

        public enum InteractiveReturn { Quit, Run };

        public InteractiveReturn Run()
        {
            if (ass == null)
                ass = libtysila.Assembler.CreateAssembler(Program.arch, new FileSystemFileLoader(), null, Program.options);
            select_module(Program.input_file);

            Console.WriteLine(Program.bplate);

            while (true)
            {
                Console.WriteLine();
                DisplayInfo();
                DisplayPrompt();

                string input = Console.ReadLine();

#if !DEBUG
                try
                {
#endif
                    string[] tok = ToTok(input);
                    if (tok.Length < 1)
                        continue;
                    else if ((tok[0] == "q") || (tok[0] == "quit") || (tok[0] == "exit"))
                        return InteractiveReturn.Quit;
                    else if ((tok[0] == "r") || (tok[0] == "run"))
                        return InteractiveReturn.Run;
                    else if (tok[0] == "select")
                    {
                        if (tok.Length < 3)
                            uc();
                        else if ((tok[1] == "arch") || (tok[1] == "architecture"))
                        {
                            Assembler.Architecture arch = Assembler.ParseArchitectureString(tok[2]);
                            if (arch == null)
                                throw new Exception("Invalid architecture");
                            ass = Assembler.CreateAssembler(arch, new FileSystemFileLoader(), null, Program.options);
                            select_module(Program.input_file);
                        }
                        else if ((tok[1] == "module") || (tok[1] == "mod"))
                        {
                            try
                            {
                                module = new ModuleList(this).GetRow(tok[2]).obj as Metadata;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                select_module(tok[2]);
                            }
                        }
                        else if (tok[1] == "type")
                            select_type(tok[2]);
                        else if (tok[1] == "method")
                        {
                            if (tok.Length == 4)
                                select_type(tok[3]);
                            select_method(tok[2]);
                        }
                        else
                            uc();
                    }
                    else if (tok[0] == "find")
                    {
                        if (tok[0].Length < 3)
                            uc();
                        else if (tok[1] == "method")
                        {
                            if (tok.Length == 4)
                                select_type(tok[3]);
                            MethodList ml = new MethodList(this);

                            FilteredList fl = FilteredList.DoFilter(ml, new FilteredList.Filter
                            {
                                Field = "MethodName",
                                SearchTerm = tok[2],
                                Method = FilteredList.Filter.SearchMethod.Contains
                            });

                            DisplayList(fl);
                        }
                    }
                    else if (tok[0] == "list")
                    {
                        if (tok.Length < 2)
                            uc();
                        else if ((tok[1] == "arch") || (tok[1] == "architecture") || (tok[1] == "archs") || (tok[1] == "architectures"))
                            Console.WriteLine(Program.GetSupportedArchitecturesList());
                        else if ((tok[1] == "modules") || (tok[1] == "module") || (tok[1] == "mod") || (tok[1] == "mods"))
                            DisplayList(new ModuleList(this));
                        else if ((tok[1] == "types") || (tok[1] == "type"))
                        {
                            if (tok.Length == 3)
                                select_module(tok[2]);
                            DisplayList(new TypeList(this));
                        }
                        else if (tok[1] == "methods")
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);
                            DisplayList(new MethodList(this));
                        }
                        else if ((tok[1] == "interfaces") || (tok[1] == "interface"))
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);
                            DisplayList(new IFaceList(this));
                        }
                        else if ((tok[1] == "interfacemembers") || (tok[1] == "interfacemethods"))
                        {
                            if (tok.Length < 3)
                                uc();
                            else
                            {
                                if (tok.Length == 4)
                                    select_type(tok[3]);
                                iface = new IFaceList(this).GetRow(tok[2]).obj as Layout.Interface;
                                DisplayList(new IFaceMethods(this));
                            }
                        }
                        else if (tok[1] == "fields")
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);
                            DisplayList(new Fields(this, false));
                        }
                        else if (tok[1] == "staticfields")
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);
                            DisplayList(new Fields(this, true));
                        }
                        else if (tok[1] == "virtualmethods")
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);
                            DisplayList(new VirtualMethods(this));
                        }
                        else if ((tok[1] == "bases") || (tok[1] == "baseclasses"))
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);

                            if (!cur_type.HasValue)
                                throw new Exception("No type selected");

                            Assembler.TypeToCompile? ttc = cur_type.Value;
                            while (ttc.HasValue)
                            {
                                DisplayLine(Signature.GetString(ttc.Value.tsig, ass));
                                Layout l = Layout.GetLayout(ttc.Value, ass);
                                ttc = l.Extends;
                            }
                        }
                        else if (tok[1] == "typeinfo")
                        {
                            if (tok.Length == 3)
                                select_type(tok[2]);

                            if (!cur_type.HasValue)
                                throw new Exception("No type selected");

                            Layout l = Layout.GetTypeInfoLayout(cur_type.Value, ass, false);

                        }
                        else
                            uc();
                    }
                    else if (tok[0] == "load")
                    {
                        if (tok.Length < 3)
                            uc();
                        else if ((tok[1] == "module") || (tok[1] == "mod"))
                            select_module(tok[2], false);
                        else
                            uc();
                    }
                    else if (tok[0] == "assemble")
                    {
                        if (tok.Length >= 3)
                            select_type(tok[2]);
                        if (tok.Length >= 2)
                            select_method(tok[1]);
                        if (cur_method.HasValue == false)
                            throw new Exception("No method selected");
                        ass.Requestor.PurgeAll();
                        ass.Requestor.RequestMethod(cur_method.Value);
                        ass.OptimizedIrFeedback = this;
                        CompilerRunner.DoCompile(ass, writer, this);
                    }
                    else if (tok[0] == "mangletypeinfo")
                    {
                        if (tok.Length >= 2)
                            select_type(tok[1]);
                        Console.WriteLine(Mangler2.MangleTypeInfo(cur_type.Value, ass));
                    }
                    else if (tok[0] == "mangletypestatic")
                    {
                        if (tok.Length >= 2)
                            select_type(tok[1]);
                        Console.WriteLine(Mangler2.MangleTypeStatic(cur_type.Value, ass));
                    }
                    else if (tok[0] == "manglemethod")
                    {
                        if (tok.Length >= 3)
                            select_type(tok[2]);
                        if (tok.Length >= 2)
                            select_method(tok[1]);
                        if (cur_method.HasValue == false)
                            throw new Exception("No method selected");
                        Console.WriteLine(Mangler2.MangleMethod(cur_method.Value, ass));
                    }
                    else if (tok[0] == "manglemethodinfo")
                    {
                        if (tok.Length >= 3)
                            select_type(tok[2]);
                        if (tok.Length >= 2)
                            select_method(tok[1]);
                        if (cur_method.HasValue == false)
                            throw new Exception("No method selected");
                        Console.WriteLine(Mangler2.MangleMethodInfoSymbol(cur_method.Value, ass));
                    }
                    else if (tok[0] == "showgentype")
                    {
                        if (tok.Length >= 3)
                            select_type(tok[2]);
                        if (tok.Length >= 2)
                            select_method(tok[1]);
                        if (cur_method.HasValue == false)
                            throw new Exception("No method selected");
                        Console.WriteLine(cur_method.Value.GenMethodType.ToString());
                    }
                    else if (tok[0] == "createlayout")
                    {
                        if (tok.Length >= 2)
                            select_type(tok[1]);
                        Layout l = Layout.GetLayout(cur_type.Value, ass);
                    }
                    else if (tok[0] == "assembleone")
                    {
                        if (tok.Length >= 3)
                            select_type(tok[2]);
                        if (tok.Length >= 2)
                            select_method(tok[1]);
                        if (cur_method.HasValue == false)
                            throw new Exception("No method selected");
                        ass.OptimizedIrFeedback = this;

                        DummyWriter dw = new DummyWriter();
                        libtysila.tydb.TyDbFile old_debug = ass.debug;
                        bool old_produces_debug = ass._debug_produces_output;
                        ass.debug = new tydbfile.TyDbFile();
                        ass._debug_produces_output = false;
                        Assembler.AssembleBlockOutput abo = ass.AssembleMethod(cur_method.Value, dw, null);

                        tydisasm.tydisasm d = tydisasm.tydisasm.GetDisassembler(ass.Arch.InstructionSet);
                        if (d != null)
                        {
                            while (dw.MoreToRead)
                            {
                                ulong offset = (ulong)dw.Offset;

                                bool has_il = false;
                                int il_offset = 0;
                                if (ass.debug.Functions.Count == 1)
                                {
                                    foreach (libtysila.tydb.Line line in ass.debug.Functions[0].Lines)
                                    {
                                        if (line.CompiledOffset == (int)offset)
                                        {
                                            has_il = true;
                                            il_offset = line.ILOffset;
                                        }
                                    }
                                }

                                dw.LineStart();
                                tydisasm.line disasm = d.GetNextLine(dw);

                                if (has_il)
                                {
                                    Console.WriteLine();
                                    Console.Write("IL_" + il_offset.ToString("X4") + ": ", il_offset);
                                    /*if (abo.instrs.ContainsKey(il_offset))
                                        Console.Write(abo.instrs[il_offset].instr.ToString());*/
                                    Console.WriteLine();
                                }

                                string offset_str = "x16";
                                if (ass.GetBitness() == Assembler.Bitness.Bits32)
                                    offset_str = "x8";
                                Console.WriteLine("{0} {1,-30} {2}", offset.ToString(offset_str), disasm.OpcodeString, disasm.ToDisassembledString(d));

                                /* See if there are any relocations here */
                                foreach (libasm.RelocationBlock reloc in abo.relocs)
                                {
                                    if ((ulong)reloc.Offset >= disasm.offset_start && (ulong)reloc.Offset < disasm.offset_end)
                                    {
                                        Console.WriteLine("        {0} {1} t:{2} s:{3} v:{4}", reloc.Offset.ToString("x16"), reloc.Target.ToString(),
                                            reloc.RelType.ToString(), reloc.Size.ToString(), reloc.Value.ToString());
                                    }

                                }
                                if (disasm.opcodes == null)
                                    break;
                            }
                        }

                        ass.debug = old_debug;
                        ass._debug_produces_output = old_produces_debug;
                    }
                    else if (tok[0] == "assembletype")
                    {
                        if (tok.Length >= 2)
                            select_type(tok[1]);
                        if (cur_type.HasValue == false)
                            throw new Exception("No type selected");
                        ass.Requestor.PurgeAll();
                        ass.Requestor.RequestTypeInfo(cur_type.Value);
                        ass.OptimizedIrFeedback = this;
                        CompilerRunner.DoCompile(ass, writer, this);
                    }
                    else if (tok[0] == "box")
                    {
                        if (cur_type.HasValue)
                            cur_type = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(new Signature.BoxedType(cur_type.Value.tsig.Type), ass), type = cur_type.Value.type };
                    }
                    else if (tok[0] == "unbox")
                    {
                        if (cur_type.HasValue && (cur_type.Value.tsig.Type is Signature.BoxedType))
                            cur_type = new Assembler.TypeToCompile { tsig = new Signature.Param(((Signature.BoxedType)cur_type.Value.tsig.Type).Type, ass), type = cur_type.Value.type, _ass = ass };
                    }
                    else if (tok[0] == "createreference")
                    {
                        if (cur_type.HasValue)
                            cur_type = new Assembler.TypeToCompile { tsig = new Signature.Param(new Signature.ManagedPointer { _ass = ass, ElemType = cur_type.Value.tsig.Type }, ass), type = cur_type.Value.type };
                    }
                    else if (tok[0] == "createpointer")
                    {
                        if (cur_type.HasValue)
                            cur_type = new Assembler.TypeToCompile { tsig = new Signature.Param(new Signature.UnmanagedPointer { _ass = ass, BaseType = cur_type.Value.tsig.Type }, ass), type = cur_type.Value.type };
                    }
                    else if (tok[0] == "dereference")
                    {
                        if (cur_type.HasValue && (cur_type.Value.tsig.Type is Signature.ManagedPointer))
                            cur_type = new Assembler.TypeToCompile { tsig = new Signature.Param(((Signature.ManagedPointer)cur_type.Value.tsig.Type).ElemType, ass), type = cur_type.Value.type, _ass = ass };
                        else if (cur_type.HasValue && (cur_type.Value.tsig.Type is Signature.UnmanagedPointer))
                            cur_type = new Assembler.TypeToCompile { tsig = new Signature.Param(((Signature.UnmanagedPointer)cur_type.Value.tsig.Type).BaseType, ass), type = cur_type.Value.type, _ass = ass };
                    }
                    else if (tok[0] == "classsize")
                    {
                        if (cur_type.HasValue)
                        {
                            Layout l = Layout.GetLayout(cur_type.Value, ass);
                            Console.WriteLine("ClassSize: {0}   StaticClassSize: {1}", l.ClassSize, l.StaticClassSize);
                        }
                    }
                    else if (tok[0] == "clitype")
                    {
                        if (cur_type.HasValue)
                            Console.WriteLine("CliType: {0}", cur_type.Value.tsig.CliType(ass));
                    }
                    else if (tok[0] == "createarray")
                    {
                        throw new NotImplementedException();
                        /*
                        int rank = -1;
                        if (tok.Length >= 2)
                            rank = Int32.Parse(tok[1]);
                        else
                        {
                            DisplayPrompt("Rank");
                            string s_rank = GetLine();
                            rank = Int32.Parse(s_rank);
                        }

                        Signature.ComplexArray ca = new Signature.ComplexArray { ElemType = cur_type.Value.tsig.Type, LoBounds = new int[rank], Rank = rank, Sizes = new int[rank] };
                        Assembler.TypeToCompile new_ttc = ass.CreateArray(new Signature.Param(ca, ass), rank, cur_type.Value);
                        cur_type = new_ttc;*/
                    }
                    else if (tok[0] == "createzba")
                    {
                        throw new NotImplementedException();
                        /*
                        Signature.ZeroBasedArray zba = new Signature.ZeroBasedArray { ElemType = cur_type.Value.tsig.Type };
                        Assembler.TypeToCompile new_ttc = ass.CreateArray(new Signature.Param(zba, ass), 1, cur_type.Value);
                        cur_type = new_ttc;*/
                    }
                    else
                        uc();
#if !DEBUG
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    Console.WriteLine(e.TargetSite.Name);
                }
#endif
            }
        }

        private void select_method(string p)
        {
            Assembler.MethodToCompile mtc = (Assembler.MethodToCompile)new MethodList(this).GetRow(p).obj;

            if (mtc.msig is Signature.Method)
            {
                Signature.Method m = mtc.msig as Signature.Method;
                if (m.GenParamCount > 0)
                {
                    Signature.GenericMethod gm = new Signature.GenericMethod();

                    gm.GenMethod = m;
                    gm.m = m.m;

                    for (int gpi = 0; gpi < m.GenParamCount; gpi++)
                    {
                        bool cont = true;
                        while (cont)
                        {
                            DisplayPrompt("GenMethodParm " + gpi.ToString());
                            string new_type = GetLine();
                            if ((new_type == "q") || (new_type == "x"))
                                throw new Exception();
                            else
                            {
                                try
                                {
                                    Row new_row = new TypeList(this).GetRow(new_type);
                                    if (new_row == null)
                                        throw new Exception("Type not found");
                                    else
                                    {
                                        gm.GenParams.Add(((TypeList.AssemblyTTC)new_row.obj).ttc.tsig.Type);
                                        cont = false;
                                    }
                                }
                                catch (Exception e)
                                {
                                    DisplayLine(e.Message);
                                    cont = true;
                                }
                            }
                        }
                    }
                    gm.GenMethod = Signature.ResolveGenericMember(m, mtc.tsig, gm, ass) as Signature.Method;
                    mtc.msig = gm;
                }
            }

            cur_method = mtc;
        }

        private void select_type(string p)
        {
            TypeList.AssemblyTTC attc = new TypeList(this).GetRow(p).obj as TypeList.AssemblyTTC;
            module = attc.module;
            cur_type = attc.ttc;
        }

        private Metadata select_module(string p) { return select_module(p, true); }
        private Metadata select_module(string p, bool do_select)
        {
            Metadata old_mod = module;
            try
            {
                Metadata new_mod = ass.FindAssembly(p);
                if (do_select)
                    module = new_mod;
                return new_mod;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                module = old_mod;
                return null;
            }
        }

        private void uc()
        {
            Console.WriteLine("Unknown command");
        }

        private string[] ToTok(string input)
        {
            string[] ret = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return ret;
        }

        public void DisplayLine(string line) { Console.WriteLine(line); }
        public void DisplayPrompt() { DisplayPrompt(""); }
        public void DisplayPrompt(string prompt)
        {
            Console.Write(prompt + "> ");
        }
        public string GetLine()
        { return Console.ReadLine(); }

        private void DisplayInfo()
        {
            Console.WriteLine("Architecture: " + ass.Arch);
            Console.WriteLine("Module: " + ((module != null) ? (module.Information.name) : "[null]"));
            Console.WriteLine("Current Type: " + ((cur_type.HasValue) ? (Signature.GetString(cur_type.Value.tsig, ass)) : "[null]"));
            Console.WriteLine("Current Method: " + ((cur_method.HasValue) ? (Signature.GetString(cur_method.Value, ass)) : "[null]"));
        }

        private void DisplayList(IInteractiveList l)
        {
            Header[] hdrs = l.GetHeaders();
            int rc = l.GetRowCount();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hdrs.Length; i++)
            {
                if (hdrs[i].visible)
                {
                    if (i > 0)
                        sb.Append("   ");
                    sb.Append("{" + i.ToString() + "," + hdrs[i].width.ToString() + "}");
                }
            }

            Console.WriteLine(sb.ToString(), hdrs);
            Console.WriteLine();
            for (int i = 0; i < rc; i++)
                Console.WriteLine(sb.ToString(), l.GetRow(i).fields);
        }

        class Header
        {
            public int width; public string name; public bool visible = true; public bool searchable = false;
            public override string ToString()
            {
                return name;
            }
        }

        class Row { public object obj; public string[] fields; }

        interface IInteractiveList
        {
            Header[] GetHeaders();
            Row GetRow(string src_field);
            Row GetRow(int rowno);
            int GetRowCount();
        }

        abstract class InteractiveList
        {
            protected Interactive i;
            public InteractiveList(Interactive interactive) { i = interactive; }

            public virtual Row GetRow(string src_field)
            {
                int count = GetRowCount();
                Header[] hdrs = GetHeaders();
                for (int i = 0; i < count; i++)
                {
                    Row r = GetRow(i);
                    for (int j = 0; j < r.fields.Length; j++)
                    {
                        if ((hdrs[j].searchable == true) && (r.fields[j] == src_field))
                            return r;
                    }
                }
                return null;
            }

            public abstract int GetRowCount();
            public abstract Row GetRow(int rowno);
            public abstract Header[] GetHeaders();
        }

        class Fields : InteractiveList, IInteractiveList
        {
            bool _static = false;
            public Fields(Interactive interactive, bool Static) : base(interactive) { _static = Static; }

            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", searchable = true },
                    new Header { width = 3, name = "Off" },
                    new Header { width = -65, name = "Name" }
                };
            }

            public override Row GetRow(int rowno)
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");

                Layout.Field f = null;

                Layout l3 = Layout.GetLayout(i.cur_type.Value, i.ass);
                if (_static)
                    f = l3.StaticFields[rowno];
                else
                    f = l3.InstanceFields[rowno];

                return new Row { obj = f, fields = new string[] { rowno.ToString(), f.offset.ToString(), Signature.GetString(f.field, i.ass) } };
            }

            public override int GetRowCount()
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");

                Layout l3 = Layout.GetLayout(i.cur_type.Value, i.ass);
                if (_static)
                    return l3.StaticFields.Count;
                else
                    return l3.InstanceFields.Count;
            }
        }

        class VirtualMethods : InteractiveList, IInteractiveList
        {
            public VirtualMethods(Interactive interactive) : base(interactive) { }

            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", searchable = true },
                    new Header { width = 3, name = "Off" },
                    new Header { width = -71, name = "Name" },
                    new Header { width = -76, name = "Implementation" }
                };
            }

            public override Row GetRow(int rowno)
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");

                Layout l3 = Layout.GetLayout(i.cur_type.Value, i.ass);
                Layout.Method m = l3.VirtualMethods[rowno];
                return new Row { obj = m, fields = new string[] { rowno.ToString(), m.offset.ToString(), Signature.GetString(m.meth, i.ass), m.implementation } };
            }

            public override int GetRowCount()
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");

                Layout l3 = Layout.GetLayout(i.cur_type.Value, i.ass);
                return l3.VirtualMethods.Count;
            }
        }

        class IFaceMethods : InteractiveList, IInteractiveList
        {
            public IFaceMethods(Interactive interactive) : base(interactive) { }
            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", searchable = true },
                    new Header { width = 3, name = "Off" },
                    new Header { width = -71, name = "Name" },
                    new Header { width = -76, name = "Implementation" }
                };
            }
            public override Row GetRow(int rowno)
            {
                if (i.iface == null)
                    throw new Exception("No interface selected");
                Layout.Method iim = i.iface.methods[rowno];
                return new Row { obj = iim, fields = new string[] { rowno.ToString(), iim.offset.ToString(), Signature.GetString(iim.meth, i.ass), iim.implementation } };
            }
            public override int GetRowCount()
            {
                if (i.iface == null)
                    throw new Exception("No interface selected");
                return i.iface.methods.Count;
            }
        }

        class FilteredList : IInteractiveList
        {
            IInteractiveList baselist;
            List<int> included_indices = new List<int>();
            int idx_header = -1;

            public class Filter
            {
                public string Field;
                public int FieldIdx;
                public string SearchTerm;
                public SearchMethod Method;

                public enum SearchMethod
                {
                    Contains, NotContains, StartsWith, NotStartsWith, EndsWith, NotEndsWith,
                    Equals, NotEquals
                };
            }

            public static FilteredList DoFilter(IInteractiveList BaseList, params Filter[] Filters)
            { return DoFilter(BaseList, false, Filters); }

            public static FilteredList DoFilter(IInteractiveList BaseList, bool IncludeAll, params Filter[] Filters)
            {
                FilteredList fl = new FilteredList(BaseList, IncludeAll);

                // Find field indices
                Header[] hdrs = fl.baselist.GetHeaders();
                foreach (Filter f in Filters)
                {
                    int field_idx = -1;

                    for (int i = 0; i < hdrs.Length; i++)
                    {
                        if (hdrs[i].name == f.Field)
                        {
                            field_idx = i;
                            break;
                        }
                    }

                    if (field_idx == -1)
                        throw new NotSupportedException("Base list does not contain field: " + f.Field);
                    f.FieldIdx = field_idx;
                }

                // Run the filter
                int rc = fl.baselist.GetRowCount();
                for (int i = 0; i < rc; i++)
                {
                    int include = 0;  // 0 = do nothing, 1 = add, 2 = remove

                    foreach (Filter f in Filters)
                    {
                        string field_val = fl.baselist.GetRow(i).fields[f.FieldIdx];

                        switch (f.Method)
                        {
                            case Filter.SearchMethod.Contains:
                                if (field_val.Contains(f.SearchTerm))
                                    include = 1;
                                break;
                            case Filter.SearchMethod.EndsWith:
                                if (field_val.EndsWith(f.SearchTerm))
                                    include = 1;
                                break;
                            case Filter.SearchMethod.Equals:
                                if (field_val.Equals(f.SearchTerm))
                                    include = 1;
                                break;
                            case Filter.SearchMethod.NotContains:
                                if (field_val.Contains(f.SearchTerm))
                                    include = 2;
                                else
                                    include = 1;
                                break;
                            case Filter.SearchMethod.NotEndsWith:
                                if (field_val.EndsWith(f.SearchTerm))
                                    include = 2;
                                else
                                    include = 1;
                                break;
                            case Filter.SearchMethod.NotEquals:
                                if (field_val.Equals(f.SearchTerm))
                                    include = 2;
                                else
                                    include = 1;
                                break;
                            case Filter.SearchMethod.NotStartsWith:
                                if (field_val.StartsWith(f.SearchTerm))
                                    include = 2;
                                else
                                    include = 1;
                                break;
                            case Filter.SearchMethod.StartsWith:
                                if (field_val.StartsWith(f.SearchTerm))
                                    include = 1;
                                break;
                        }
                    }

                    if (include == 1)
                        fl.Add(i);
                    else if (include == 2)
                        fl.Delete(i);
                }

                return fl;
            }

            public FilteredList(IInteractiveList BaseList, bool IncludeAll)
            {
                baselist = BaseList;
                if (IncludeAll)
                    AddAll();

                // Find the Idx header
                Header[] hdrs = baselist.GetHeaders();
                for(int i = 0; i < hdrs.Length; i++)
                {
                    if (hdrs[i].name == "Idx")
                    {
                        idx_header = i;
                        break;
                    }
                }

                if (idx_header == -1)
                    throw new NotSupportedException("BaseList must contain a Idx header");
            }

            private void AddAll()
            {
                int rc = baselist.GetRowCount();
                for (int i = 0; i < rc; i++)
                    Add(Int32.Parse(baselist.GetRow(i).fields[idx_header]));
            }

            private void Add(int idx)
            {
                if (!included_indices.Contains(idx))
                {
                    if (baselist.GetRow(idx) != null)
                        included_indices.Add(idx);
                }
            }

            private void Delete(int idx)
            {
                if (included_indices.Contains(idx))
                    included_indices.Remove(idx);
            }

            private void Clear()
            {
                included_indices.Clear();
            }

            public Header[] GetHeaders()
            {
                return baselist.GetHeaders();
            }

            public Row GetRow(string src_field)
            {
                throw new NotSupportedException("GetRow(string) is not supported on FilteredList");
            }

            public Row GetRow(int rowno)
            {
                if ((rowno < 0) || (rowno >= included_indices.Count))
                    return null;
                return baselist.GetRow(included_indices[rowno]);
            }

            public int GetRowCount()
            {
                return included_indices.Count;
            }
        }

        class IFaceList : InteractiveList, IInteractiveList
        {
            public IFaceList(Interactive interactive) : base(interactive) { }
            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", searchable = true },
                    new Header { width = -52, name = "Name" }
                };
            }
            public override Row GetRow(int rowno)
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");

                Layout l = Layout.GetLayout(i.cur_type.Value, i.ass);
                Layout.Interface ii = l.Interfaces[rowno];
                return new Row { obj = ii, fields = new string[] { rowno.ToString(), Signature.GetString(ii.iface.tsig, i.ass) } };
            }
            public override int GetRowCount()
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");
                Layout l = Layout.GetLayout(i.cur_type.Value, i.ass);
                return l.Interfaces.Count;
            }
        }

        class MethodList : InteractiveList, IInteractiveList
        {
            public MethodList(Interactive interactive) : base(interactive) { }
            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", visible = true, searchable = true },
                    new Header { width = -60, name = "Name", visible = true, searchable = true },
                    new Header { width = 0, name = "MethodName", visible = false, searchable = false }
                };
            }
            public override Row GetRow(int rowno)
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");

                /*Metadata.TableIndex meth = i.cur_type.Value.type.MethodList + rowno;
                Metadata.MethodDefRow mdr = Metadata.GetMethodDef(meth.ToToken(), i.ass);*/
                Metadata.MethodDefRow mdr = i.cur_type.Value.type.Methods[rowno];
                Signature.BaseMethod msig = Signature.ResolveGenericMember(Signature.ParseMethodSig(mdr), i.cur_type.Value.tsig.Type, null, i.ass);
                Assembler.MethodToCompile mtc = new Assembler.MethodToCompile { _ass = i.ass, meth = mdr, msig = msig, tsigp = i.cur_type.Value.tsig, type = i.cur_type.Value.type };

                return new Row { obj = mtc, fields = new string[] { rowno.ToString(), Signature.GetString(msig, mdr.Name, mdr.GetParamNames(), i.ass), mdr.Name } };
            }
            public override int GetRowCount()
            {
                if (!(i.cur_type.HasValue))
                    throw new Exception("No type selected");
                return i.cur_type.Value.type.Methods.Count;
                //return Metadata.GetLastMethod(i.cur_type.Value.type) - i.cur_type.Value.type.MethodList;
            }
        }

        class ModuleList : InteractiveList, IInteractiveList
        {
            public ModuleList(Interactive interactive) : base(interactive) { }
            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", visible = true, searchable = true },
                    new Header { width = -60, name = "Name", visible = true, searchable = true }
                };
            }
            public override Row GetRow(int rowno)
            {
                List<Assembler.AssemblyInformation> ais = i.ass.GetLoadedAssemblies();
                return new Row { fields = new string[] { rowno.ToString(), ais[rowno].name }, obj = ais[rowno].m };
            }
            public override int GetRowCount()
            {
                List<Assembler.AssemblyInformation> ais = i.ass.GetLoadedAssemblies();
                return ais.Count;
            }
        }

        class TypeList : InteractiveList, IInteractiveList
        {
            public TypeList(Interactive interactive) : base(interactive) { }
            public override Header[] GetHeaders()
            {
                return new Header[] { new Header { width = 3, name = "Idx", visible = true, searchable = true },
                    new Header { width = -60, name = "Name", visible = true, searchable = true }
                };
            }

            public override Row GetRow(string src_field)
            {
                bool boxed = false;
                Metadata mod = i.module;
                List<string> generic_params = new List<string>();

                Signature.BaseType bt = Signature.Param.MakeBaseType(src_field);
                if (bt != null)
                {
                    Signature.Param bt_p = new Signature.Param(bt, i.ass);
                    AssemblyTTC bt_ret = new AssemblyTTC { ttc = new Assembler.TypeToCompile { _ass = i.ass, tsig = bt_p, type = Metadata.GetTypeDef(bt, i.ass) }, module = i.select_module("mscorlib", false) };
                    return new Row { obj = bt_ret, fields = get_fields(bt_ret.ttc, 0) };
                }

                if (src_field.StartsWith("["))
                {
                    string assembly;

                    if (!src_field.Contains("]"))
                        throw new Exception("Mismatched []");
                    assembly = src_field.Substring(1, src_field.IndexOf(']') - 1);
                    src_field = src_field.Substring(src_field.IndexOf(']') + 1);
                    if (assembly == "")
                        throw new Exception("No module selected");
                    mod = i.select_module(assembly, false);
                }

                if (src_field.EndsWith(">"))
                {
                    string gp_str;

                    if (!src_field.Contains("<"))
                        throw new Exception("Mismatched []");
                    gp_str = src_field.Substring(src_field.IndexOf('<') + 1);
                    gp_str = gp_str.Substring(0, gp_str.Length - 1);
                    src_field = src_field.Substring(0, src_field.IndexOf('<'));

                    string[] temp_gps = gp_str.Split(new char[] { ',' });
                    foreach (string temp_gp in temp_gps)
                        generic_params.Add(temp_gp.Trim());
                }

                if (src_field.StartsWith("{boxed}"))
                {
                    boxed = true;
                    src_field = src_field.Substring("{boxed}".Length);
                }

                string n = src_field;
                string ns = "";
                if (src_field.Contains("."))
                {
                    ns = src_field.Substring(0, src_field.LastIndexOf('.'));
                    n = src_field.Substring(src_field.LastIndexOf('.') + 1);
                }

                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mod, ns, n, i.ass);
                Signature.Param tsig = null;
                if (tdr == null)
                    throw new Exception("Type not found in module: " + mod.ModuleName);

                if (boxed && tdr.IsValueType(i.ass))
                    tsig = new Signature.Param(new Signature.BoxedType(new Signature.Param(tdr, i.ass).Type), i.ass);
                else if (tdr.IsGeneric)
                {
                    List<Metadata.GenericParamRow> gprs = Metadata.GetGenericParams(tdr.m, tdr);
                    Signature.GenericType gt = new Signature.GenericType { GenType = new Signature.Param(tdr, i.ass).Type, GenParams = new List<Signature.BaseOrComplexType>() };
                    int cur_gp = 0;
                    foreach (Metadata.GenericParamRow gpr in gprs)
                    {
                        try
                        {
                            gt.GenParams.Add(((AssemblyTTC)GetRow(generic_params[cur_gp]).obj).ttc.tsig.Type);
                        }
                        catch
                        {
                            bool cont = true;
                            while (cont)
                            {
                                i.DisplayPrompt("GenParm " + gpr.Number.ToString() + " (" + gpr.Name + ")");
                                string new_type = i.GetLine();
                                if ((new_type == "q") || (new_type == "x"))
                                    throw new Exception();
                                else if (new_type == "g")
                                {
                                    gt.GenParams.Add(new libtysila.Signature.BaseType(BaseType_Type.UninstantiatedGenericParam));
                                    cont = false;
                                }
                                else
                                {
                                    try
                                    {
                                        Row new_row = GetRow(new_type);
                                        if (new_row == null)
                                            throw new Exception("Type not found");
                                        else
                                        {
                                            gt.GenParams.Add(((AssemblyTTC)new_row.obj).ttc.tsig.Type);
                                            cont = false;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        i.DisplayLine(e.Message);
                                        cont = true;
                                    }
                                }
                            }
                        }
                        cur_gp++;
                    }
                    tsig = new Signature.Param(gt, i.ass);
                }
                else
                    tsig = new Signature.Param(tdr, i.ass);

                Assembler.TypeToCompile ttc = new Assembler.TypeToCompile { _ass = i.ass, type = tdr, tsig = tsig };

                return new Row { obj = new AssemblyTTC { module = mod, ttc = ttc }, fields = get_fields(ttc, 0) };
            }

            private string[] get_fields(Assembler.TypeToCompile ttc, int rowno)
            {
                if (ttc.type == null)
                    return new string[] { rowno.ToString(), ttc.tsig.ToString() };
                return new string[] { rowno.ToString(), ttc.type.TypeFullName };
            }

            public override Row GetRow(int rowno)
            {
                Metadata.TypeDefRow tdr = i.module.Tables[(int)Metadata.TableId.TypeDef][rowno] as Metadata.TypeDefRow;
                Assembler.TypeToCompile ttc = new Assembler.TypeToCompile { _ass = i.ass, type = tdr, tsig = new Signature.Param(tdr, i.ass) };
                return new Row { obj = new AssemblyTTC { module = i.module, ttc = ttc }, fields = get_fields(ttc, rowno) };
            }

            public override int GetRowCount()
            {
                if (i.module == null)
                    throw new Exception("No module selected");
                return i.module.Tables[(int)Metadata.TableId.TypeDef].Length;                
            }

            public class AssemblyTTC { public Metadata module; public Assembler.TypeToCompile ttc; }
        }

        /* class InteractiveList2
        {
            public class Header
            {
                public string Name;
                public bool Searchable = false;
                public int Width = 10;
            }

            public List<Header> Headers = new List<Header>();

            public void DisplayList(System.IO.TextWriter o)
            {
                int indent = 0;
                int cur_start = indent;
                int def_width = 80;
                bool start_line = true;
                int spacer = 3;

                foreach (Header h in Headers)
                {
                    while ((cur_start + h.Width) > def_width)
                    {
                        if (start_line)
                            h.Width = def_width - cur_start;
                        else
                        {
                            indent += 2;
                            cur_start = indent;
                            start_line = true;
                        }
                    }

                    if (start_line)
                    {
                        o.WriteLine();
                        o.Write(repeat_char(' ', indent));
                    }

                    string trimname = trim(h.Name, h.Width);
                    
                    o.Write(trimname);
                    o.Write(repeat_char(' ', spacer));

                    cur_start += h.Width + spacer;
                }
            }

            private string trim(string p, int length)
            {
                if (p.Length >= length)
                    return p + repeat_char(' ', length - p.Length);
                if (length < 4)
                    return p.Substring(0, length);
                return p.Substring(0, length - 3) + "...";
            }

            private string repeat_char(char p, int length)
            {
                return new String(p, length);
            }
        } */

        public void IrDump(string line)
        {
            Console.WriteLine(line);
        }

        public void AssembleMethodFeedback(Assembler.MethodToCompile mtc)
        {
            Console.WriteLine("Assembling Method: " + Signature.GetString(mtc, ass));
            Console.WriteLine("  in Type: " + Signature.GetString(mtc.tsig, ass));
        }

        public void AssembleTypeFeedback(Assembler.TypeToCompile ttc)
        {
            Console.WriteLine("Assembling Type: " + Signature.GetString(ttc.tsig, ass));
        }

        public void AssembleTypeInfoFeedback(Assembler.TypeToCompile ttc)
        {
            Console.WriteLine("Assembling TypeInfo: " + Signature.GetString(ttc.tsig, ass));
        }

        public void AssembleFieldInfoFeedback(Assembler.FieldToCompile ftc)
        {
            Console.WriteLine("Assembling FieldInfo: " + Signature.GetString(ftc, ass));
        }

        public void AssembleMethodInfoFeedback(Assembler.MethodToCompile mtc)
        {
            Console.WriteLine("Assembling MethodInfo: " + Signature.GetString(mtc, ass));
        }
    }
}
