/* Copyright (C) 2016 by John Cronin
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
using System.IO;
using System.Text;

namespace tysila4
{
    class Program
    {
        /* Boiler plate */
        const string year = "2009 - 2017";
        const string authors = "John Cronin <jncronin@tysos.org>";
        const string website = "http://www.tysos.org";
        const string nl = "\n";
        public static string bplate = "tysila " + libtysila5.libtysila.VersionString + " (" + website + ")" + nl +
            "Copyright (C) " + year + " " + authors + nl +
            "This is free software.  Please see the source for copying conditions.  There is no warranty, " +
            "not even for merchantability or fitness for a particular purpose";

        static string comment = nl + "tysila" + nl + "ver: " + libtysila5.libtysila.VersionString + nl;

        public static List<string> search_dirs = new List<string> {
            "",
            ".",
            Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Program)).Location),
            DirectoryDelimiter
        };

        public static string DirectoryDelimiter
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    return "/";
                else
                    return "\\";
            }
        }

        static void Main(string[] args)
        {
            var argc = args.Length;
            char c;
            var go = new XGetoptCS.XGetopt();
            var arg_str = "t:L:f:e:o:d:";
            string target = "x86_64";
            string debug_file = null;
            string output_file = null;
            string epoint = null;
            while((c = go.Getopt(argc, args, arg_str)) != '\0')
            {
                switch(c)
                {
                    case 't':
                        target = go.Optarg;
                        break;
                    case 'L':
                        search_dirs.Add(go.Optarg);
                        break;
                    case 'd':
                        debug_file = go.Optarg;
                        break;
                    case 'o':
                        output_file = go.Optarg;
                        break;
                    case 'e':
                        epoint = go.Optarg;
                        break;
                }
            }

            var fname = go.Optarg;

            if(fname == String.Empty)
            {
                Console.WriteLine("No input file specified");
                return;
            }

            //var fname = "D:\\tysos\\branches\\tysila3\\libsupcs\\bin\\Release\\libsupcs.dll";
            //var fname = @"D:\tysos\branches\tysila3\testsuite\test_002\bin\Release\test_002.exe";
            //var fname = @"D:\tysos\branches\tysila3\testsuite\ifelse\ifelse.exe";
            //var fname = @"barebones\kernel.exe";
            //var fname = @"test_005.exe";
            //var fname = @"vtype\vtype.exe";
            //var fname = @"D:\tysos\branches\tysila3\mono\corlib\mscorlib.dll";
            //var fname = @"vcall\vcall.exe";

            libtysila5.libtysila.AssemblyLoader al = new libtysila5.libtysila.AssemblyLoader(
                new FileSystemFileLoader());

            //search_dirs.Add(@"..\mono\corlib");

            var m = al.GetAssembly(fname);

            var t = libtysila5.target.Target.targets[target];
            var bf = new binary_library.elf.ElfFile(binary_library.Bitness.Bits32);
            t.bf = bf;
            bf.Init();
            bf.Architecture = target;
            var st = new libtysila5.StringTable(
                m.GetStringEntry(metadata.MetadataStream.tid_Module,
                1, 1), al, t);
            t.st = st;
            t.r = new libtysila5.CachingRequestor(m);

            /* for now, just assemble all public and protected
            non-generic methods in public types, plus the
            entry point */
            StringBuilder debug = new StringBuilder();
            for(int i = 1; i <= m.table_rows[metadata.MetadataStream.tid_MethodDef]; i++)
            {
                metadata.MethodSpec ms = new metadata.MethodSpec
                {
                    m = m,
                    mdrow = i,
                    msig = 0
                };

                ms.type = new metadata.TypeSpec
                {
                    m = m,
                    tdrow = m.methoddef_owners[ms.mdrow]
                };

                var mflags = m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                    i, 2);
                var tflags = m.GetIntEntry(metadata.MetadataStream.tid_TypeDef,
                    ms.type.tdrow, 0);

                mflags &= 0x7;
                tflags &= 0x7;

                /* See if this is the entry point */
                int tid, row;
                m.InterpretToken(m.entry_point_token, out tid, out row);
                if(tid == metadata.MetadataStream.tid_MethodDef)
                {
                    if(row == i)
                    {
                        if(epoint != null)
                            ms.aliases = new List<string> { epoint };

                        mflags = 6;
                        tflags = 1;
                    }
                }

                /* See if we have an always compile attribute */
                if(ms.HasCustomAttribute("_ZN14libsupcs#2Edll8libsupcs22AlwaysCompileAttribute_7#2Ector_Rv_P1u1t") ||
                    ms.type.HasCustomAttribute("_ZN14libsupcs#2Edll8libsupcs22AlwaysCompileAttribute_7#2Ector_Rv_P1u1t"))
                {
                    mflags = 6;
                    tflags = 1;
                }

                ms.msig = (int)m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                    i, 4);

                if (ms.type.IsGenericTemplate == false &&
                    ms.IsGenericTemplate == false &&
                    (mflags == 0x4 || mflags == 0x5 || mflags == 0x6) &&
                    tflags != 0)
                {
                    t.r.MethodRequestor.Request(ms);
                }
            }

            /* Also assemble all public non-generic type infos */
            for (int i = 1; i <= m.table_rows[metadata.MetadataStream.tid_TypeDef]; i++)
            {
                var flags = (int)m.GetIntEntry(metadata.MetadataStream.tid_TypeDef,
                    i, 0);
                if (((flags & 0x7) != 0x1) &&
                    ((flags & 0x7) != 0x2))
                    continue;
                var ts = new metadata.TypeSpec { m = m, tdrow = i };
                if (ts.IsGeneric)
                    continue;
                t.r.StaticFieldRequestor.Request(ts);
                t.r.VTableRequestor.Request(ts.Box);
            }

            while (!t.r.Empty)
            {
                if (!t.r.MethodRequestor.Empty)
                {
                    var ms = t.r.MethodRequestor.GetNext();
                    libtysila5.libtysila.AssembleMethod(ms,
                        bf, t, debug, m);
                    Console.WriteLine(ms.m.MangleMethod(ms));
                }
                else if(!t.r.StaticFieldRequestor.Empty)
                {
                    var sf = t.r.StaticFieldRequestor.GetNext();
                    libtysila5.layout.Layout.OutputStaticFields(sf,
                        t, bf, m);
                    Console.WriteLine(sf.MangleType() + "S");
                }
                else if(!t.r.EHRequestor.Empty)
                {
                    var eh = t.r.EHRequestor.GetNext();
                    libtysila5.layout.Layout.OutputEHdr(eh,
                        t, bf, m);
                    Console.WriteLine(eh.ms.MangleMethod() + "EH");
                }
                else if(!t.r.VTableRequestor.Empty)
                {
                    var vt = t.r.VTableRequestor.GetNext();
                    libtysila5.layout.Layout.OutputVTable(vt,
                        t, bf, m);
                    Console.WriteLine(vt.MangleType());
                }
            }

            if (debug_file != null)
            {
                string d = debug.ToString();

                StreamWriter sw = new StreamWriter(debug_file);
                sw.Write(d);
                sw.Close();
            }

            /* String table */
            st.WriteToOutput(bf, m, t);

            /* Include original metadata */
            var rdata = bf.GetRDataSection();
            rdata.Align(t.GetPointerSize());
            var mdsym = bf.CreateSymbol();
            mdsym.DefinedIn = rdata;
            mdsym.Name = m.AssemblyName;
            mdsym.ObjectType = binary_library.SymbolObjectType.Object;
            mdsym.Offset = (ulong)rdata.Data.Count;
            mdsym.Type = binary_library.SymbolType.Global;
            var len = m.file.GetLength();
            mdsym.Size = len;
            rdata.AddSymbol(mdsym);

            for(int i = 0; i < len; i++)
                rdata.Data.Add(m.file.ReadByte(i));           

            /* Write output file */
            bf.Filename = output_file;
            bf.Write();
        }
    }
}
