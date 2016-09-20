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
        const string year = "2009 - 2016";
        const string authors = "John Cronin <jncronin@tysos.org>";
        const string website = "http://www.tysos.org";
        const string nl = "\n";
        public static string bplate = "tysila " + libtysila4.libtysila.VersionString + " (" + website + ")" + nl +
            "Copyright (C) " + year + " " + authors + nl +
            "This is free software.  Please see the source for copying conditions.  There is no warranty, " +
            "not even for merchantability or fitness for a particular purpose";

        static string comment = nl + "tysila" + nl + "ver: " + libtysila4.libtysila.VersionString + nl;

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
            //var fname = "D:\\tysos\\branches\\tysila3\\libsupcs\\bin\\Release\\libsupcs.dll";
            //var fname = @"D:\tysos\branches\tysila3\testsuite\test_002\bin\Release\test_002.exe";
            //var fname = @"D:\tysos\branches\tysila3\testsuite\ifelse\ifelse.exe";
            //var fname = @"kernel.exe";
            //var fname = @"test_005.exe";
            //var fname = @"vtype.exe";
            //var fname = @"D:\tysos\branches\tysila3\mono\corlib\mscorlib.dll";
            var fname = "vcall.exe";

            libtysila4.libtysila.AssemblyLoader al = new libtysila4.libtysila.AssemblyLoader(
                new FileSystemFileLoader());

            search_dirs.Add(@"..\..\mono\corlib");

            var m = al.GetAssembly(fname);

            var t = libtysila4.target.Target.targets["x86"];
            var bf = new binary_library.elf.ElfFile(binary_library.Bitness.Bits32);
            t.bf = bf;
            bf.Init();
            bf.Architecture = "x86";
            var st = new libtysila4.StringTable(
                m.GetStringEntry(metadata.MetadataStream.tid_Module,
                1, 1), al, t);
            t.st = st;
            t.r = new libtysila4.CachingRequestor();

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
                        ms.aliases = new List<string> { "kmain" };

                        mflags = 6;
                        tflags = 1;
                    }
                }

                ms.msig = (int)m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                    i, 4);

                if (ms.type.IsGenericTemplate == false &&
                    (mflags == 0x4 || mflags == 0x5 || mflags == 0x6) &&
                    tflags != 0)
                {
                    t.r.MethodRequestor.Request(ms);
                }
            }

            while (!t.r.MethodRequestor.Empty)
            {
                var ms = t.r.MethodRequestor.GetNext();
                libtysila4.libtysila.AssembleMethod(ms,
                    bf, t, debug);
                Console.WriteLine(ms.m.MangleMethod(ms));
            }

            string d = debug.ToString();

            StreamWriter sw = new StreamWriter("debug.txt");
            sw.Write(d);
            sw.Close();

            /* and all static fields */
            for(int i = 1; i <= m.table_rows[metadata.MetadataStream.tid_TypeDef]; i++)
            {
                metadata.TypeSpec ts;
                m.GetTypeDefRow(metadata.MetadataStream.tid_TypeDef, i, out ts);
                libtysila4.layout.Layout.OutputStaticFields(ts,
                    t, bf);
            }

            /* String table */
            st.WriteToOutput(bf, m, t);

            bf.Filename = "output.o";
            bf.Write();

            /*for(int t = 0; t < m.GetTableCount(); t++)
            {
                Console.WriteLine("Table " + t.ToString("X2"));

                for(int r = 1; r <= m.GetRowCount(t); r++)
                {
                    Console.Write("  ");
                    Console.Write(r.ToString("0000"));
                    Console.Write(": ");

                    for(int c = 0; c < m.GetColCount(t); c++)
                    {
                        if (c != 0)
                            Console.Write(", ");

                        var fe = m.GetFieldEntry(t, r, c);
                        Console.Write(fe.ToString());
                    }

                    Console.WriteLine();
                }
                Console.WriteLine();
            }*/

            /*var meth = m.GetRVA(m.GetIntEntry(6, 1, 0));

            var flags = meth.ReadByte(0);
            if ((flags & 0x3) == 0x2)
            {
                // Tiny
                throw new NotImplementedException();
            }
            else if ((flags & 0x3) == 0x3)
            {
                // Fat
                uint fat_flags = meth.ReadUShort(0) & 0xfffU;
                int fat_hdr_len = (meth.ReadUShort(0) >> 12) * 4;
                int max_stack = meth.ReadUShort(2);
                long code_size = meth.ReadUInt(4);
                long lvar_sig_tok = meth.ReadUInt(8);

                var t = libtysila4.target.Target.targets["x86"];
                var bf = new binary_library.elf.ElfFile(binary_library.Bitness.Bits32);
                t.bf = bf;
                bf.Init();
                bf.Architecture = "x86";
                var text_sect = bf.CreateContentsSection();
                text_sect.IsAlloc = true;
                text_sect.IsExecutable = true;
                text_sect.IsWriteable = false;
                text_sect.Name = ".text";
                t.text_section = text_sect;
                t.bf.AddSection(text_sect);

                var passes = new List<libtysila4.graph.Graph.PassDelegate>
                {
                    libtysila4.ir.IrGraph.LowerCilGraph,
                    libtysila4.ir.StackTracePass.TraceStackPass,
                    libtysila4.target.Target.MCLowerPass,
                    libtysila4.graph.DominanceGraph.GenerateDominanceGraph,
                    libtysila4.target.SSA.ConvertToSSAPass,
                    libtysila4.target.Liveness.DoGenKill,
                    libtysila4.target.Liveness.LivenessAnalysis,
                    libtysila4.target.RegAlloc.RegAllocPass,
                    libtysila4.target.Liveness.MRegLivenessAnalysis,
                    libtysila4.target.PreserveRegistersAroundCall.PreserveRegistersAroundCallPass,
                    libtysila4.target.RemoveUnnecessaryMoves.RemoveUnnecessaryMovesPass,
                    libtysila4.target.CalleePreserves.CalleePreservesPass,
                    libtysila4.target.AllocateLocalVars.AllocateLocalVarsPass,
                    libtysila4.target.MangleCallsites.MangleCallsitesPass,
                };
                passes.AddRange(t.GetOutputMCPasses());

                libtysila4.graph.Graph cg = libtysila4.cil.CilGraph.ReadCilStream(meth,
                    m, 1, fat_hdr_len, (int)code_size, lvar_sig_tok);

                List<libtysila4.graph.Graph> graphs = new List<libtysila4.graph.Graph>();
                StringBuilder sb = new StringBuilder();
                graphs.Add(cg);
                foreach(var pass in passes)
                {
                    sb.Append("Graph before " + pass.Method.Name + ":" + System.Environment.NewLine);
                    sb.Append(cg.LinearStreamString);
                    sb.Append(Environment.NewLine);
                    sb.Append(Environment.NewLine);

                    cg = cg.RunPass(pass, t);
                    graphs.Add(cg);
                }

                // TODO: fix up phis

                sb.Append("Final graph:" + Environment.NewLine);
                sb.Append(cg.LinearStreamString);
                var sb_text = sb.ToString();

                bf.Filename = "output.o";
                bf.Write();
                //throw new NotImplementedException();
            }
            else
                throw new Exception("Invalid method header type");*/
        }
    }
}
