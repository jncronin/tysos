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
using System.Collections.Generic;
using libtysila;

namespace tysila3
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

            Metadata m = Metadata.LoadAssembly("mscorlib", ass, "mscorlib.obj");

            // Find static bool System.Object.Equals(object, object)
            Assembler.TypeToCompile object_ttc = Metadata.GetTTC("mscorlib", "System", "Object", ass);
            Signature.Method equals_sig = new Signature.Method
            {
                RetType = new Signature.Param(BaseType_Type.Boolean),
                Params = new List<Signature.Param> { new Signature.Param(BaseType_Type.Object), new Signature.Param(BaseType_Type.Object) }
            };
            Metadata.MethodDefRow equals_mdr = Metadata.GetMethodDef(m, "Equals", object_ttc.type, equals_sig, ass);
            Assembler.MethodToCompile equals_mtc = new Assembler.MethodToCompile(ass, equals_mdr, equals_sig, object_ttc.type, object_ttc.tsig);

            // Compile to timple
            libtysila.frontend.cil.CilGraph g = libtysila.frontend.cil.CilGraph.BuildGraph(equals_mdr.Body, m, new Assembler.AssemblerOptions());
            ass.Options.CallingConvention = "gnu";
            List<libtysila.timple.TreeNode> tacs = libtysila.frontend.cil.Encoder.Encode(g, equals_mtc, ass, new Assembler.MethodAttributes());
            
            // Compile to tybel
            libtysila.timple.Optimizer.OptimizeReturn opt = libtysila.timple.Optimizer.Optimize(tacs);
            CallConv cc = ass.call_convs["gnu"](equals_mtc, CallConv.StackPOV.Callee, ass, new ThreeAddressCode(ThreeAddressCode.Op.call_i4));
            List<libasm.hardware_location> las = new List<libasm.hardware_location>();
            foreach(CallConv.ArgumentLocation arg in cc.Arguments)
                las.Add(arg.ValueLocation);
            
            libtysila.tybel.Tybel tybel = libtysila.tybel.Tybel.GenerateTybel(opt, ass, las);

            // Generate machine code
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
