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

/* Support calling of library functions for unimplemented TIR opcodes */

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    partial class Assembler
    {
        void EmitLibraryCalls(List<cfg_node> nodes, AssemblerState state)
        {
            foreach (cfg_node node in nodes)
                EmitLibraryCalls(node, state);
        }

        void EmitLibraryCalls(cfg_node node, AssemblerState state)
        {
            for (int i = 0; i < node.optimized_ir.Count; i++)
            {
                if (IsUnimplemented(node.optimized_ir[i]))
                    node.optimized_ir[i] = EmitLibraryCall(node.optimized_ir[i], state);
            }
        }

        bool IsUnimplemented(ThreeAddressCode tac) { return IsUnimplemented(tac.Operator); }
        bool IsUnimplemented(ThreeAddressCode.Op opcode)
        {
            return !output_opcodes.ContainsKey(opcode);
        }

        string LibraryCallMangledArg(CliType arg_type)
        {
            switch (arg_type)
            {
                case CliType.int32:
                    return "i4";
                case CliType.int64:
                    return "i8";
                case CliType.native_int:
                    return "i";
                case CliType.F32:
                    return "s";
                case CliType.F64:
                    return "d";
                case CliType.void_:
                    return "v";
                default:
                    throw new NotImplementedException();
            }
        }

        ThreeAddressCode EmitLibraryCall(ThreeAddressCode tac, AssemblerState state)
        {
            // Determine the result and operand types of the opcode
            CliType result;
            CliType op1;
            CliType op2;
            try
            {
                result = tac.GetResultType();
                op1 = tac.GetOp1Type();
                op2 = tac.GetOp2Type();
            }
            catch (Exception)
            {
                return tac;
            }

            // Determine the appropriate call opcode
            ThreeAddressCode.Op call_tac = GetCallTac(result);

            // Build a string name describing the call
            StringBuilder sb = new StringBuilder();
            sb.Append("__");
            sb.Append(tac.Operator.ToString());
            sb.Append("_");
            sb.Append(LibraryCallMangledArg(result));
            sb.Append(LibraryCallMangledArg(op1));
            sb.Append(LibraryCallMangledArg(op2));

            // Build the call tac
            List<var> args = new List<var>();
            List<Signature.Param> p = new List<Signature.Param>();
            if (op1 != CliType.void_)
            {
                args.Add(tac.Operand1);
                p.Add(new Signature.Param(op1));
            }
            if (op2 != CliType.void_)
            {
                args.Add(tac.Operand2);
                p.Add(new Signature.Param(op2));
            }

            CallConv cc = MakeStaticCall(Options.CallingConvention, new Signature.Param(result), p, call_tac);

            CallEx ce = new CallEx(tac.Result, args.ToArray(), call_tac, sb.ToString(), cc, (tac.VTSize.HasValue) ? tac.VTSize.Value : 0);
            return ce;
        }
    }
}
