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

namespace tysila4
{
    class Program
    {
        static void Main(string[] args)
        {
            //var fname = "D:\\tysos\\branches\\tysila3\\libsupcs\\bin\\Release\\libsupcs.dll";
            //var fname = @"D:\tysos\branches\tysila3\testsuite\test_002\bin\Release\test_002.exe";
            var fname = @"D:\tysos\branches\tysila3\testsuite\ifelse\ifelse.exe";
            var f = new System.IO.FileStream(fname,
                System.IO.FileMode.Open, System.IO.FileAccess.Read);

            metadata.PEFile p = new metadata.PEFile();
            var m = p.Parse(f);

            for(int t = 0; t < m.GetTableCount(); t++)
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
            }

            var meth = m.GetRVA(m.GetIntEntry(6, 1, 0));

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

                var cg = libtysila4.cil.CilGraph.ReadCilStream(meth,
                    m, 1, fat_hdr_len, (int)code_size, lvar_sig_tok);
                var ig = cg.RunPass(libtysila4.ir.IrGraph.LowerCilGraph);
                ig = ig.RunPass(libtysila4.ir.StackTracePass.TraceStackPass);
                var mc = ig.RunPass(libtysila4.target.Target.MCLowerPass,
                    "x86");

                var mc_text = mc.LinearStreamString;

                mc.RunPass(libtysila4.graph.DominanceGraph.GenerateDominanceGraph);
                var ssa = mc.RunPass(libtysila4.target.SSA.ConvertToSSAPass);

                libtysila4.target.Liveness.DoGenKill(ssa);
                libtysila4.target.Liveness.LivenessAnalysis(ssa);

                mc = mc.RunPass(libtysila4.target.RegAlloc.RegAllocPass,
                    "x86");
                throw new NotImplementedException();
            }
            else
                throw new Exception("Invalid method header type");
        }
    }
}
