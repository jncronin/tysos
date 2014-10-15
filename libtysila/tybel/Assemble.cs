/* Copyright (C) 2008 - 2014 by John Cronin
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

namespace libtysila.tybel
{
    partial class Tybel
    {
        public class DebugNode : libasm.OutputBlock
        {
            public libtysila.timple.BaseNode Code;
            public int Offset;
        }

        public static void Assemble(List<Node> instrs, List<byte> code, List<libasm.ExportedSymbol> syms, List<libasm.RelocationBlock> relocs,
            Assembler ass, Assembler.MethodAttributes attrs, List<DebugNode> debug)
        {
            List<libasm.OutputBlock> obs = new List<libasm.OutputBlock>();

            foreach (Node n in instrs)
            {
                if (ass.Options.Debug)
                    obs.Add(new DebugNode { Code = n });

                obs.AddRange(n.Assemble(ass, attrs));
            }

            /* Identify labels and their offsets */
            int cur_offset = code.Count;
            Dictionary<string, int> loc_labels = new Dictionary<string,int>();
            foreach (libasm.OutputBlock b in obs)
            {
                if (b is libasm.CodeBlock)
                    cur_offset += ((libasm.CodeBlock)b).Code.Count;
                else if (b is libasm.PrefixBlock)
                    cur_offset += ((libasm.PrefixBlock)b).Code.Count;
                else if (b is DebugNode)
                    ((DebugNode)b).Offset = cur_offset;
                else if (b is libasm.LocalSymbol)
                {
                    libasm.LocalSymbol ls = b as libasm.LocalSymbol;
                    ls.Offset = cur_offset;
                    loc_labels[ls.Name] = cur_offset;
                }
                else if (b is libasm.ExportedSymbol)
                {
                    libasm.ExportedSymbol es = b as libasm.ExportedSymbol;
                    es.Offset = cur_offset;
                    loc_labels[es.Name] = cur_offset;
                    syms.Add(es);
                }
                else if (b is libasm.RelativeReference)
                {
                    libasm.RelativeReference rr = b as libasm.RelativeReference;
                    cur_offset += rr.Size;
                }
                else if (b is libasm.RelocationBlock)
                {
                    libasm.RelocationBlock rb = b as libasm.RelocationBlock;
                    cur_offset += rb.Size;
                }
            }

            /* Loop through again, resolving local references */
            foreach (libasm.OutputBlock b in obs)
            {
                if (b is libasm.CodeBlock)
                    code.AddRange(((libasm.CodeBlock)b).Code);
                else if (b is libasm.PrefixBlock)
                    code.AddRange(((libasm.PrefixBlock)b).Code);
                else if (b is DebugNode)
                {
                    if (debug != null)
                        debug.Add(b as DebugNode);
                }
                else if (b is libasm.RelativeReference)
                {
                    libasm.RelativeReference rr = b as libasm.RelativeReference;
                    if (loc_labels.ContainsKey(rr.Target))
                    {
                        /* This is a local reference */
                        long offset = loc_labels[rr.Target] - code.Count + rr.Addend;

                        code.AddRange(ass.ToByteArraySignExtend(offset, rr.Size));
                    }
                    else
                    {
                        if (rr.Target.StartsWith("L"))
                        {
                            throw new Exception("local label: " + rr.Target + " not found");
                        }
                        relocs.Add(new libasm.RelocationBlock
                        {
                            Offset = code.Count,
                            Size = rr.Size,
                            Value = rr.Addend,
                            Target = rr.Target,
                            RelType = rr.RelType
                        });

                        code.AddRange(ass.ToByteArraySignExtend(0, rr.Size));
                    }
                }
                else if (b is libasm.RelocationBlock)
                {
                    libasm.RelocationBlock rb = b as libasm.RelocationBlock;

                    rb.Offset = code.Count;

                    byte[] rbs = ass.ToByteArraySignExtend(rb.Value, rb.Size);
                    code.AddRange(rbs);
                    relocs.Add(rb);
                }
            }
        }
    }
}
