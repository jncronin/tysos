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

namespace tl
{
    class DefaultScripts
    {
        static public Dictionary<string, LinkerScript> Scripts = new Dictionary<string, LinkerScript>();

        internal static void InitScripts()
        {
            InitElf();
            InitElfTyObj();
            InitElfTyKObj();
            InitElfJCA();
            InitElfJCAProg();
        }

        internal static LinkerScript GetScript(string[] ntriple)
        {
            /* Try multiple combinations of ntriple

                arch-format-os
                arch-format
                format-os
                format
            */

            string afo = ntriple[0] + "-" + ntriple[1] + "-" + ntriple[2];
            string af = ntriple[0] + "-" + ntriple[1];
            string fo = ntriple[1] + "-" + ntriple[2];
            string f = ntriple[1];

            if (Scripts.ContainsKey(afo)) return Scripts[afo];
            if (Scripts.ContainsKey(af)) return Scripts[af];
            if (Scripts.ContainsKey(fo)) return Scripts[fo];
            if (Scripts.ContainsKey(f)) return Scripts[f];

            // Default to ELF32 script
            return Scripts["elf"];
        }

        private static void InitElf()
        {
            LinkerScript elf32_def = new LinkerScript("elf");
            elf32_def.Script.Add(new LinkerScript.EntryPoint("main"));
            elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["text-start-addr"].ULong));
            elf32_def.Script.Add(new LinkerScript.DefineSymbol("__begin"));

            elf32_def.Script.Add(new LinkerScript.DefineSymbol("__text"));
            elf32_def.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Text));
            elf32_def.Script.Add(new LinkerScript.InputFileSection(".text*"));
            elf32_def.Script.Add(new LinkerScript.EndSection());

            if (Program.options["data-start-addr"].Set)
                elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["data-start-addr"].ULong));
            else
                elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, Program.options["page-size"].ULong));
            elf32_def.Script.Add(new LinkerScript.DefineSymbol("__data"));
            elf32_def.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Data));
            elf32_def.Script.Add(new LinkerScript.InputFileSection(".data*"));
            elf32_def.Script.Add(new LinkerScript.EndSection());

            if (Program.options["rodata-start-addr"].Set)
                elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["rodata-start-addr"].ULong));
            else
                elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, Program.options["page-size"].ULong));
            elf32_def.Script.Add(new LinkerScript.DefineSymbol("__rodata"));
            elf32_def.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Rodata));
            elf32_def.Script.Add(new LinkerScript.InputFileSection(".rodata*"));
            elf32_def.Script.Add(new LinkerScript.EndSection());

            if (Program.options["bss-start-addr"].Set)
                elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["bss-start-addr"].ULong));
            else
                elf32_def.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, Program.options["page-size"].ULong));
            elf32_def.Script.Add(new LinkerScript.DefineSymbol("__bss"));
            elf32_def.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Bss));
            elf32_def.Script.Add(new LinkerScript.InputFileSection(".bss*"));
            elf32_def.Script.Add(new LinkerScript.EndSection());

            elf32_def.Script.Add(new LinkerScript.DefineSymbol("__end"));

            Scripts["elf"] = elf32_def;
            Scripts["elf64"] = elf32_def;
        }

        private static void InitElfTyObj()
        {
            // TyObj is a relocatable format that will eventually contain a ELF hash of the symbol table
            LinkerScript elf32_tyobj = new LinkerScript("elf-tyobj");
            elf32_tyobj.Script.Add(new LinkerScript.SetExecutable(false));
            elf32_tyobj.Script.Add(new LinkerScript.GenerateELFHash());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Text));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSymbol("__text"));
            elf32_tyobj.Script.Add(new LinkerScript.InputFileSection(".text*"));
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Data));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSymbol("__data"));
            elf32_tyobj.Script.Add(new LinkerScript.InputFileSection(".data*"));
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Rodata));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSymbol("__rodata"));
            elf32_tyobj.Script.Add(new LinkerScript.InputFileSection(".rodata*"));
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Bss));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSymbol("__bss"));
            elf32_tyobj.Script.Add(new LinkerScript.InputFileSection(".bss*"));
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(".comment", 0, false, false, false, true));
            elf32_tyobj.Script.Add(new LinkerScript.InputFileSection(".comment*"));
            elf32_tyobj.Script.Add(new LinkerScript.AddComment());
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.ElfDynamic));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSymbol("_DYNAMIC"));
            elf32_tyobj.Script.Add(new LinkerScript.ElfDynamicSection());
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            elf32_tyobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tyobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.ElfHash));
            elf32_tyobj.Script.Add(new LinkerScript.ElfHashSection());
            elf32_tyobj.Script.Add(new LinkerScript.EndSection());

            Scripts["elf-tyobj"] = elf32_tyobj;
        }

        private static void InitElfTyKObj()
        {
            // TyKObj is a relocatable format for the tysos kernel containing a elf hash and a text address of 0x40000000
            LinkerScript elf32_tykobj = new LinkerScript("elf-tykobj");
            elf32_tykobj.Script.Add(new LinkerScript.SetExecutable(false));
            elf32_tykobj.Script.Add(new LinkerScript.GenerateELFHash());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0x40000000));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Text));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSymbol("__text"));
            elf32_tykobj.Script.Add(new LinkerScript.InputFileSection(".text*"));
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, 16));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Data));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSymbol("__data"));
            elf32_tykobj.Script.Add(new LinkerScript.InputFileSection(".data*"));
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, 16));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Rodata));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSymbol("__rodata"));
            elf32_tykobj.Script.Add(new LinkerScript.InputFileSection(".rodata*"));
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, 16));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Bss));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSymbol("__bss"));
            elf32_tykobj.Script.Add(new LinkerScript.InputFileSection(".bss*"));
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(".comment", 0, false, false, false, true));
            elf32_tykobj.Script.Add(new LinkerScript.InputFileSection(".comment*"));
            elf32_tykobj.Script.Add(new LinkerScript.AddComment());
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.ElfDynamic));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSymbol("_DYNAMIC"));
            elf32_tykobj.Script.Add(new LinkerScript.ElfDynamicSection());
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0));
            elf32_tykobj.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.ElfHash));
            elf32_tykobj.Script.Add(new LinkerScript.ElfHashSection());
            elf32_tykobj.Script.Add(new LinkerScript.EndSection());

            elf32_tykobj.Script.Add(new LinkerScript.GenerateELFProgramHeaders());

            Scripts["elf-tykobj"] = elf32_tykobj;
        }


        private static void InitElfJCA()
        {
            // JCA-elf-none is optimized for loading into boot roms.
            //  Specifically:
            //  .text starts at 0x0
            //  .rodata is included in .text for code size
            //  .bss starts at 0x400000
            //  .data is included in .bss (and therefore initialized data
            //      is not allowed)

            LinkerScript elf32_jca = new LinkerScript("elf-jca");
            if(Program.options["text-start-addr"].Set)
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["text-start-addr"].ULong));

            elf32_jca.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Text));

            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__begin"));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__text"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".text*"));

            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__rodata"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".rodata*"));

            elf32_jca.Script.Add(new LinkerScript.EndSection());

            if (Program.options["bss-start-addr"].Set)
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["bss-start-addr"].ULong));
            else
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0x800000));
            elf32_jca.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Bss));

            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__bss"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".bss*"));

            elf32_jca.Script.Add(new LinkerScript.CommonSection());

            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__data"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".data*"));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__end"));

            elf32_jca.Script.Add(new LinkerScript.EndSection());


            Scripts["jca-elf-none"] = elf32_jca;
        }

        private static void InitElfJCAProg()
        {
            // JCA-elf-prog is a JCA program
            //  Specifically:
            //  .text starts at 0x400000
            //  .rodata follows aligned on a 4 byte boundary
            //  .data follows aligned on a 4 byte boundary
            //  .bss follows aligned on a 4 byte boundary

            LinkerScript elf32_jca = new LinkerScript("elf-jca-prog");
            if (Program.options["epoint"].Set)
                elf32_jca.Script.Add(new LinkerScript.EntryPoint(Program.options["epoint"].String));
            else
                elf32_jca.Script.Add(new LinkerScript.EntryPoint("_start"));

            if (!Program.options["nostartfiles"].Set)
                elf32_jca.Script.Add(new LinkerScript.AddInputFile("crt0.o"));

            if (Program.options["text-start-addr"].Set)
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["text-start-addr"].ULong));
            else
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, 0x400000));

            elf32_jca.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Text));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__begin"));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__text"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".text*"));
            elf32_jca.Script.Add(new LinkerScript.EndSection());

            if (Program.options["rodata-start-addr"].Set)
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["rodata-start-addr"].ULong));
            else
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, 4));
            elf32_jca.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Rodata));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__rodata"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".rodata*"));
            elf32_jca.Script.Add(new LinkerScript.EndSection());

            if (Program.options["data-start-addr"].Set)
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["data-start-addr"].ULong));
            else
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, 4));
            elf32_jca.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Data));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__data"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".data*"));
            elf32_jca.Script.Add(new LinkerScript.EndSection());

            if (Program.options["bss-start-addr"].Set)
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Set, Program.options["bss-start-addr"].ULong));
            else
                elf32_jca.Script.Add(new LinkerScript.UpdateOffset(LinkerScript.UpdateOffset.UpdateType.Align, 4));
            elf32_jca.Script.Add(new LinkerScript.DefineSection(LinkerScript.DefineSection.StandardSection.Bss));
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__bss"));
            elf32_jca.Script.Add(new LinkerScript.InputFileSection(".bss*"));
            elf32_jca.Script.Add(new LinkerScript.CommonSection());
            elf32_jca.Script.Add(new LinkerScript.DefineSymbol("__end"));
            elf32_jca.Script.Add(new LinkerScript.EndSection());

            Scripts["jca-elf-prog"] = elf32_jca;
        }

    }
}
