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

namespace tl
{
    class DefaultScripts
    {
        static public Dictionary<string, LinkerScript> Scripts = new Dictionary<string, LinkerScript>();

        internal static void InitScripts()
        {
            InitElf();
        }

        private static void InitElf()
        {
            LinkerScript elf32_def = new LinkerScript();
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
    }
}
