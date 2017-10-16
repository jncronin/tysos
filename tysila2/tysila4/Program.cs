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
            var arg_str = "t:L:f:e:o:d:qDC:H:m:";
            string target = "x86";
            string debug_file = null;
            string output_file = null;
            string epoint = null;
            string cfile = null;
            string hfile = null;
            bool quiet = false;
            bool require_metadata_version_match = true;
            Dictionary<string, object> opts = new Dictionary<string, object>();
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
                    case 'q':
                        quiet = true;
                        break;
                    case 'D':
                        require_metadata_version_match = false;
                        break;
                    case 'C':
                        cfile = go.Optarg;
                        break;
                    case 'H':
                        hfile = go.Optarg;
                        break;
                    case 'm':
                        {
                            var opt = go.Optarg;
                            object optval = true;
                            if(opt.Contains("="))
                            {
                                var optvals = opt.Substring(opt.IndexOf("=") + 1);
                                opt = opt.Substring(0, opt.IndexOf("="));

                                int intval;
                                if (optvals.ToLower() == "false" || optvals.ToLower() == "off" || optvals.ToLower() == "no")
                                    optval = false;
                                else if (optvals.ToLower() == "true" || optvals.ToLower() == "on" || optvals.ToLower() == "yes")
                                    optval = true;
                                else if (int.TryParse(optvals, out intval))
                                    optval = intval;
                                else
                                    optval = optvals;
                            }
                            else if(opt.StartsWith("no-"))
                            {
                                opt = opt.Substring(3);
                                optval = false;
                            }
                            opts[opt] = optval;
                        }
                        break;
                }
            }

            var fname = go.Optarg;

            if(fname == String.Empty)
            {
                Console.WriteLine("No input file specified");
                return;
            }

            if(cfile != null && hfile == null)
            {
                Console.WriteLine("-H must be used if -C is used");
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
            al.RequireVersionMatch = require_metadata_version_match;

            var m = al.GetAssembly(fname);
            if(m == null)
            {
                Console.WriteLine("Input file " + fname + " not found");
                throw new Exception(fname + " not found");
            }

            var t = libtysila5.target.Target.targets[target];
            // try and set target options
            foreach(var kvp in opts)
            {
                if(!t.Options.TrySet(kvp.Key, kvp.Value))
                {
                    Console.WriteLine("Unable to set target option " + kvp.Key + " to " + kvp.Value.ToString());
                    return;
                }
            }

            if (output_file != null)
            {
                var bf = new binary_library.elf.ElfFile(binary_library.Bitness.Bits32);
                t.bf = bf;
                bf.Init();
                bf.Architecture = target;
                var st = new libtysila5.StringTable(
                    m.GetStringEntry(metadata.MetadataStream.tid_Module,
                    1, 1), al, t);
                t.st = st;
                t.r = new libtysila5.CachingRequestor(m);
                t.InitIntcalls();

                /* for now, just assemble all public and protected
                non-generic methods in public types, plus the
                entry point */
                StringBuilder debug = new StringBuilder();
                for (int i = 1; i <= m.table_rows[metadata.MetadataStream.tid_MethodDef]; i++)
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
                    if (tid == metadata.MetadataStream.tid_MethodDef)
                    {
                        if (row == i)
                        {
                            if (epoint != null)
                                ms.aliases = new List<string> { epoint };

                            mflags = 6;
                            tflags = 1;
                        }
                    }

                    /* See if we have an always compile attribute */
                    if (ms.HasCustomAttribute("_ZN14libsupcs#2Edll8libsupcs22AlwaysCompileAttribute_7#2Ector_Rv_P1u1t") ||
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
                        libtysila5.libtysila.AssembleMethod(ms.ms,
                            bf, t, debug, m, ms.c);
                        if (!quiet)
                            Console.WriteLine(ms.ms.m.MangleMethod(ms.ms));
                    }
                    else if (!t.r.StaticFieldRequestor.Empty)
                    {
                        var sf = t.r.StaticFieldRequestor.GetNext();
                        libtysila5.layout.Layout.OutputStaticFields(sf,
                            t, bf, m);
                        if (!quiet)
                            Console.WriteLine(sf.MangleType() + "S");
                    }
                    else if (!t.r.EHRequestor.Empty)
                    {
                        var eh = t.r.EHRequestor.GetNext();
                        libtysila5.layout.Layout.OutputEHdr(eh,
                            t, bf, m);
                        if (!quiet)
                            Console.WriteLine(eh.ms.MangleMethod() + "EH");
                    }
                    else if (!t.r.VTableRequestor.Empty)
                    {
                        var vt = t.r.VTableRequestor.GetNext();
                        libtysila5.layout.Layout.OutputVTable(vt,
                            t, bf, m);
                        if (!quiet)
                            Console.WriteLine(vt.MangleType());
                    }
                    else if(!t.r.DelegateRequestor.Empty)
                    {
                        var d = t.r.DelegateRequestor.GetNext();
                        libtysila5.ir.ConvertToIR.CreateDelegate(d, t);
                        if (!quiet)
                            Console.WriteLine(d.MangleType() + "D");
                    }
                    else if(!t.r.BoxedMethodRequestor.Empty)
                    {
                        var bm = t.r.BoxedMethodRequestor.GetNext();
                        libtysila5.libtysila.AssembleBoxedMethod(bm.ms,
                            bf, t, debug);
                        if (!quiet)
                            Console.WriteLine(bm.ms.MangleMethod());
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

                for (int i = 0; i < len; i++)
                    rdata.Data.Add(m.file.ReadByte(i));

                var mdsymend = bf.CreateSymbol();
                mdsymend.DefinedIn = rdata;
                mdsymend.Name = m.AssemblyName + "_end";
                mdsymend.ObjectType = binary_library.SymbolObjectType.Object;
                mdsymend.Offset = (ulong)rdata.Data.Count;
                mdsymend.Type = binary_library.SymbolType.Global;
                mdsymend.Size = 0;
                rdata.AddSymbol(mdsymend);

                /* Write output file */
                bf.Filename = output_file;
                bf.Write();
            }

            if(hfile != null)
            {
                COutput.WriteHeader(m, t, hfile, cfile);
            }
        }
    }
}
