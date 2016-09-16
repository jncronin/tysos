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
using System.Text;
using libtysila4.util;

namespace libtysila4
{
    public partial class libtysila
    {
        public static bool AssembleMethod(metadata.MethodSpec ms,
            binary_library.IBinaryFile bf, target.Target t,
            StringBuilder debug_passes = null)
        {
            var ts = bf.GetTextSection();
            t.bf = bf;
            t.text_section = ts;

            var csite = ms.msig;
            var mdef = ms.mdrow;
            var m = ms.m;

            // Get signature if not specified
            if (csite == 0)
            {
                csite = (int)m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                    mdef, 4);
            }

            // Get method RVA
            var meth = m.GetRVA(m.GetIntEntry(metadata.MetadataStream.tid_MethodDef,
                mdef, 0));

            var flags = meth.ReadByte(0);
            int max_stack = 0;
            long code_size = 0;
            long lvar_sig_tok = 0;
            int boffset = 0;

            if ((flags & 0x3) == 0x2)
            {
                // Tiny header
                code_size = flags >> 2;
                max_stack = 8;
                boffset = 1;
            }
            else if ((flags & 0x3) == 0x3)
            {
                // Fat header
                uint fat_flags = meth.ReadUShort(0) & 0xfffU;
                int fat_hdr_len = (meth.ReadUShort(0) >> 12) * 4;
                max_stack = meth.ReadUShort(2);
                code_size = meth.ReadUInt(4);
                lvar_sig_tok = meth.ReadUInt(8);
                boffset = fat_hdr_len;
            }
            else
                throw new Exception("Invalid method header flags");

            // Get mangled name for defining a symbol
            var mangled_name = m.MangleMethod(ms);
            var meth_sym = bf.CreateSymbol();
            meth_sym.Name = mangled_name;
            meth_sym.ObjectType = binary_library.SymbolObjectType.Function;
            meth_sym.Offset = (ulong)ts.Data.Count;
            meth_sym.Type = binary_library.SymbolType.Global;
            ts.AddSymbol(meth_sym);
            if(debug_passes != null)
            {
                debug_passes.Append("Assembling method ");
                debug_passes.Append(mangled_name);
                debug_passes.Append(Environment.NewLine);
                debug_passes.Append(Environment.NewLine);
            }

            // Define passes
            var passes = new List<graph.Graph.PassDelegate>
            {
                ir.IrGraph.LowerCilGraph,
                ir.StackTracePass.TraceStackPass,
                target.Target.MCLowerPass,
                graph.DominanceGraph.GenerateDominanceGraph,
                target.SSA.ConvertToSSAPass,
                target.Liveness.DoGenKill,
                target.Liveness.LivenessAnalysis,
                target.RegAlloc.RegAllocPass,
                target.Liveness.MRegLivenessAnalysis,
                target.PreserveRegistersAroundCall.PreserveRegistersAroundCallPass,
                target.RemoveUnnecessaryMoves.RemoveUnnecessaryMovesPass,
                target.CalleePreserves.CalleePreservesPass,
                target.AllocateLocalVars.AllocateLocalVarsPass,
                target.MangleCallsites.MangleCallsitesPass,
            };
            passes.AddRange(t.GetOutputMCPasses());

            // Get first graph
            graph.Graph cg = cil.CilGraph.ReadCilStream(meth,
                ms, boffset, (int)code_size, lvar_sig_tok);

            // Run passes
            foreach (var pass in passes)
            {
                if(debug_passes != null)
                {
                    debug_passes.Append("Graph before " + pass.Method.Name + ":" + System.Environment.NewLine);
                    debug_passes.Append(cg.LinearStreamString);
                    debug_passes.Append(Environment.NewLine);
                    debug_passes.Append(Environment.NewLine);
                }

                cg = cg.RunPass(pass, t);
            }

            if (debug_passes != null)
            {
                debug_passes.Append("Final graph:" + Environment.NewLine);
                debug_passes.Append(cg.LinearStreamString);
            }

            meth_sym.Size = ts.Data.Count - (int)meth_sym.Offset;

            return true;
        }
    }
}
