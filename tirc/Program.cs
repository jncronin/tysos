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

using System;
using libtysila;
using System.Collections.Generic;

namespace tirc
{
    class Program
    {
        static void Main(string[] args)
        {
            string assname = "x86_64s-elf64-tysos";

            libtysila.Assembler.Architecture arch = libtysila.Assembler.ParseArchitectureString(assname);

            frontend_lib.FileSystemFileLoader floader = new frontend_lib.FileSystemFileLoader();
            floader.DirectoryDelimiter = "\\";
            floader.SearchDirs.Add(".");
            floader.SearchDirs.Add("..\\mono\\corlib");


            libtysila.Assembler ass = libtysila.Assembler.CreateAssembler(arch, floader, null, null);
            
            System.IO.FileStream f = new System.IO.FileStream("test.tir", System.IO.FileMode.Open);
            TIRParse.Parser p = new TIRParse.Parser(new TIRParse.Scanner(f), ass);
            bool res = p.Parse();

            /* Generate tybel code */
            libtysila.timple.Optimizer.OptimizeReturn opt = libtysila.timple.Optimizer.Optimize(p.tacs["test2"]);
            libtysila.tybel.Tybel tybel = libtysila.tybel.Tybel.GenerateTybel(opt, ass, new List<libasm.hardware_location>());

            /* Generate code */
            List<byte> code = new List<byte>();
            List<libasm.ExportedSymbol> syms = new List<libasm.ExportedSymbol>();
            List<libasm.RelocationBlock> relocs = new List<libasm.RelocationBlock>();
            tybel.Assemble(code, syms, relocs, ass);

            /* Dump dissassembly */
            tydisasm.tydisasm d = tydisasm.tydisasm.GetDisassembler(ass.Arch.InstructionSet);
            CodeStream cs = new CodeStream { code = code };
            if (d != null)
            {
                while (cs.MoreToRead)
                {
                    ulong offset = (ulong)cs.Offset;

                    tydisasm.line disasm = d.GetNextLine(cs);

                    Console.WriteLine("{0} {1,-30} {2}", offset.ToString("x16"), disasm.OpcodeString, disasm.ToString());
                    if (disasm.opcodes == null)
                        break;
                }
            }

            Console.ReadKey();
        }
    }

    class CodeStream : tydisasm.ByteProvider
    {
        public List<byte> code;
        public int offset = 0;

        public bool MoreToRead { get { return offset < code.Count; } }
        public int Offset { get { return offset; } }

        public override byte GetNextByte()
        {
            return code[offset++];
        }
    }
}

namespace TIRParse
{
    partial class Parser
    {
        internal Parser(Scanner s, libtysila.Assembler assembler) : base(s) { ass = assembler; }

        libtysila.Assembler ass;

        internal class partial_inst
        {
            public libtysila.ThreeAddressCode.Op op;
            public int block_target;
            public libtysila.vara call_target;
            public List<libtysila.vara> args;
        }

        string GetCallConv(partial_inst p)
        {
            if (!callconvs.ContainsKey(p.call_target.LabelVal))
                throw new ParseException("Please provide a method definition for " + p.call_target.LabelVal, ((Scanner)Scanner).sline,
                    ((Scanner)Scanner).scol);
            return callconvs[p.call_target.LabelVal];
        }

        libtysila.Signature.Method GetMethSig(partial_inst p)
        {
            if (!msigs.ContainsKey(p.call_target.LabelVal))
                throw new ParseException("Please provide a method definition for " + p.call_target.LabelVal, ((Scanner)Scanner).sline,
                    ((Scanner)Scanner).scol);
            return msigs[p.call_target.LabelVal];
        }

        libtysila.Assembler.CliType GetDataTypeOf(int v)
        {
            if (!dts.ContainsKey(v))
                throw new ParseException("Please provide a variable definition for " + v.ToString(), ((Scanner)Scanner).sline,
                    ((Scanner)Scanner).scol); 
            return dts[v];
        }

        libtysila.Assembler.TypeToCompile InterpretSimpleType(string module, string nspace, string name, List<libtysila.Assembler.TypeToCompile> gen_args)
        {
            if ((name == "int32") || (name == "Int32"))
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.int32), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);
            else if ((name == "int64") || (name == "Int64"))
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.int64), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);
            else if ((name == "native_int") || (name == "IntPtr"))
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.native_int), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);
            else if ((name == "F32") || (name == "Single"))
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.F32), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);
            else if ((name == "F64") || (name == "Double"))
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.F64), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);
            else if ((name == "O") || (name == "Object"))
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.O), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);
            else if (name == "vt")
                return libtysila.Metadata.GetTTC(new Signature.Param(Assembler.CliType.vt), new Assembler.TypeToCompile(new Signature.Param(BaseType_Type.Object), ass), null, ass);

            return InterpretType(module, nspace, name, gen_args);
        }

        libtysila.Assembler.TypeToCompile InterpretType(string module, string nspace, string name, List<libtysila.Assembler.TypeToCompile> gen_args)
        {
            libtysila.Assembler.TypeToCompile gtd_ttc = libtysila.Metadata.GetTTC(module, nspace, name, ass);

            return gtd_ttc;
        }

        void AddVarDef(IEnumerable<int> vs, Assembler.TypeToCompile ttc)
        {
            foreach(int v in vs)
                dts[v] = ttc.tsig.CliType(ass);
        }

        void AddExternDef(string name, libtysila.Assembler.TypeToCompile ret, List<libtysila.Assembler.TypeToCompile> p, string cc)
        {
            libtysila.Signature.Method msig = new libtysila.Signature.Method
            {
                CallingConvention = libtysila.Signature.Method.CallConv.Default,
                ExplicitThis = false,
                HasThis = false,
                RetType = ret.tsig,
                GenParamCount = 0,
                ParamCount = p.Count,
                Params = new List<libtysila.Signature.Param>()
            };
            foreach(libtysila.Assembler.TypeToCompile i in p)
                msig.Params.Add(i.tsig);

            libtysila.Assembler.MethodToCompile mtc = new libtysila.Assembler.MethodToCompile { _ass = ass, meth = null,
                msig = msig };

            callconvs[name] = cc;
            msigs[name] = msig;
        }
    }

    partial class Scanner
    {
        public override void yyerror(string format, params object[] args)
        {
            throw new ParseException(String.Format(format, args) + " at line " + yyline + ", col " + yycol, yyline, yycol);
        }

        internal int sline { get { return yyline; } }
        internal int scol { get { return yycol; } }
    }

    public class ParseException : Exception
    {
        int l, c;
        public ParseException(string msg, int line, int col) : base(msg) { l = line; c = col; }
    }
}
