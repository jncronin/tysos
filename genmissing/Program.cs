/* Copyright (C) 2017-2018 by John Cronin
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
using System.Threading.Tasks;
using binary_library.elf;

namespace genmissing
{
    class Program
    {
        static string[] input_files = new string[]
        {
            @"D:\tysos\libsupcs\bin\Release/libsupcs.a",
            @"D:\tysos\tysos\bin\Release/tysos.obj",
            @"D:\tysos\coreclr/mscorlib.obj",
            @"./tysos/x86_64/cpu.o",
            @"./tysos/x86_64/halt.o",
            @"./tysos/x86_64/exceptions.o",
            @"./tysos/x86_64/switcher.o",
            @"D:\tysos\metadata\bin\Release/metadata.obj",
        };

        static string[] lib_paths = new string[]
        {

        };

        static string arch = "x86_64";
        static string output_name = "output.o";        

        static void Main(string[] args)
        {
            /* Load up each input file in turn */
            var ifiles = new List<binary_library.IBinaryFile>();
            foreach(var ifname in input_files)
            {
                var ifinfo = new System.IO.FileInfo(ifname);
                if (!ifinfo.Exists)
                    throw new System.IO.FileNotFoundException("Cannot find: " + ifname);

                /* Determine file type from extension */
                binary_library.IBinaryFile ifobj = null;
                if (ifinfo.Extension == ".o" || ifinfo.Extension == ".obj")
                    ifobj = new binary_library.elf.ElfFile();

                if (ifobj == null)
                    ifobj = binary_library.BinaryFile.CreateBinaryFile(ifinfo.Extension);

                if (ifobj == null)
                    throw new Exception("Unsupported file type: " + ifinfo.FullName);

                /* Load up the particular file */
                ifobj.Filename = ifinfo.FullName;
                ifobj.Read();
                ifiles.Add(ifobj);
            }

            /* Get a list of all defined symbols */
            var def_syms = new Dictionary<string, binary_library.ISymbol>();
            foreach(var file in ifiles)
            {
                var sym_count = file.GetSymbolCount();
                for(int i = 0; i < sym_count; i++)
                {
                    var sym = file.GetSymbol(i);
                    if (sym.DefinedIn != null)
                        def_syms[sym.Name] = sym;
                }
            }

            /* Get a list of those relocations which are missing */
            var missing = new Dictionary<string, binary_library.ISymbol>();
            foreach(var file in ifiles)
            {
                var reloc_count = file.GetRelocationCount();
                for(int i = 0; i < reloc_count; i++)
                {
                    var reloc = file.GetRelocation(i);
                    if (reloc.References != null && !def_syms.ContainsKey(reloc.References.Name))
                        missing[reloc.References.Name] = reloc.References;
                }
            }

            /* Generate an output object */
            var o = new binary_library.elf.ElfFile(ifiles[0].Bitness);
            o.Init();

            o.Filename = output_name;

            tysila4.Program.search_dirs.Add(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1");
            tysila4.Program.search_dirs.Add(@"D:\tysos\tysos\bin\Release");

            /* Generate dummy module */
            var ms = new metadata.MetadataStream();
            ms.al = new libtysila5.libtysila.AssemblyLoader(new tysila4.FileSystemFileLoader());
            ms.LoadBuiltinTypes();

            /* Load up assembler target */
            var t = libtysila5.target.Target.targets[arch];
            var fsi = new System.IO.FileInfo(output_name);
            t.st = new libtysila5.StringTable(fsi.Name.Substring(0, fsi.Name.Length - fsi.Extension.Length), ms.al, t);

            /* Generate signature of missing_function(string) to call */
            var special_c = new libtysila5.Code { ms = new metadata.MethodSpec { m = ms } };
            var special = special_c.special_meths;
            var missing_msig = special.CreateMethodSignature(null, new metadata.TypeSpec[] { ms.SystemString });

            /* Generate stub functions for missing methods */
            var m = missing.Keys;
            var missing_types = new List<string>();
            var other_missing = new List<string>();
            foreach(var str in m)
            {
                try
                {
                    if (ms.IsMangledMethod(str))
                    {
                        GenerateMissingMethod(str, ms, t, missing_msig, o);
                    }
                    else
                        missing_types.Add(str);
                }
                catch(ArgumentException)
                {
                    other_missing.Add(str);
                }
            }
        }

        private static void GenerateMissingMethod(string str, metadata.MetadataStream m, libtysila5.target.Target t, int missing_msig, ElfFile o)
        {
            /* Generate new method spec */
            var ms = new metadata.MethodSpec { m = m, mangle_override = str };

            /* Generate stub code */
            libtysila5.Code c = new libtysila5.Code();
            c.t = t;
            c.ms = ms;

            var ir = new List<libtysila5.cil.CilNode.IRNode>();
            var stack = new libtysila5.util.Stack<libtysila5.ir.StackItem>();
            var cilnode = new libtysila5.cil.CilNode(ms, 0);
            cilnode.irnodes = ir;
            c.ir = ir;
            c.starts = new List<libtysila5.cil.CilNode> { cilnode };

            stack = libtysila5.ir.ConvertToIR.ldstr(cilnode, c, stack, str);
            stack = libtysila5.ir.ConvertToIR.call(cilnode, c, stack, false, "missing_function",
                c.special_meths, missing_msig);

            /* Assemble */
            libtysila5.libtysila.AssembleMethod(ms, o, t, null, null, c);
        }
    }
}
