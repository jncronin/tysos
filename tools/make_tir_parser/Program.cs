/* Copyright (C) 2014 by John Cronin
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

/* Parse the ThreeAddressCode.Op enumeration, and generate lex and parser rules for it */

using System;
using System.Collections.Generic;
using libtysila;
using System.Reflection;
using System.IO;

namespace make_tir_parser
{
    class Program
    {
        static void Main(string[] args)
        {
            string lexfname = "op.lex";
            string yaccfname = "op.y";

            System.Type t = typeof(ThreeAddressCode.Op);
            string[] enumnames = System.Enum.GetNames(t);
            ThreeAddressCode.Op[] enumvals = (ThreeAddressCode.Op[])System.Enum.GetValues(t);

            StreamWriter lsw = new StreamWriter(lexfname);
            foreach (string enumname in enumnames)
                lsw.WriteLine(enumname + "        " + "return (int)Tokens." + enumname.ToUpper() + ";");
            lsw.Close();

            List<string> ops = new List<string>();
            List<string> callops = new List<string>();
            List<string> brops = new List<string>();
            List<string> cmpbrops = new List<string>();

            for (int i = 0; i < enumnames.Length; i++)
            {
                switch (ThreeAddressCode.GetOpType(enumvals[i]))
                {
                    case ThreeAddressCode.OpType.CallOp:
                        callops.Add(enumnames[i]);
                        break;
                    case ThreeAddressCode.OpType.BrOp:
                        brops.Add(enumnames[i]);
                        break;
                    case ThreeAddressCode.OpType.CmpBrOp:
                        cmpbrops.Add(enumnames[i]);
                        break;
                    default:
                        ops.Add(enumnames[i]);
                        break;
                }
            }

            StreamWriter ysw = new StreamWriter(yaccfname);

            /* Write token definitions */
            foreach (string enumname in enumnames)
                ysw.WriteLine("%token <opval> " + enumname.ToUpper());
            ysw.WriteLine();
            ysw.WriteLine("%%");
            ysw.WriteLine();



            for (int i = 0; i < ops.Count; i++)
            {
                if (i == 0)
                    ysw.Write("op          :   ");
                else
                    ysw.Write("            |   ");

                ysw.WriteLine(ops[i].ToUpper() + " { $$ = libtysila.ThreeAddressCode.Op." + ops[i] + "; }");
            }
            ysw.WriteLine("            ;");
            ysw.WriteLine();

            for (int i = 0; i < callops.Count; i++)
            {
                if (i == 0)
                    ysw.Write("call_op     :   ");
                else
                    ysw.Write("            |   ");

                ysw.WriteLine(callops[i].ToUpper() + " { $$ = libtysila.ThreeAddressCode.Op." + callops[i] + "; }");
            }
            ysw.WriteLine("            ;");
            ysw.WriteLine();

            for (int i = 0; i < brops.Count; i++)
            {
                if (i == 0)
                    ysw.Write("br_op       :   ");
                else
                    ysw.Write("            |   ");

                ysw.WriteLine(brops[i].ToUpper() + " { $$ = libtysila.ThreeAddressCode.Op." + brops[i] + "; }");
            }
            ysw.WriteLine("            ;");
            ysw.WriteLine();

            for (int i = 0; i < cmpbrops.Count; i++)
            {
                if (i == 0)
                    ysw.Write("cmpbr_op       :   ");
                else
                    ysw.Write("            |   ");

                ysw.WriteLine(cmpbrops[i].ToUpper() + " { $$ = libtysila.ThreeAddressCode.Op." + cmpbrops[i] + "; }");
            }
            ysw.WriteLine("            ;");
            ysw.WriteLine();

            ysw.Close();
        }
    }
}
