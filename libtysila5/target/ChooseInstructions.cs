/* Copyright (C) 2017 by John Cronin
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

namespace libtysila5.target
{
    public class ChooseInstructions
    {
        public static void DoChoosing(Code c)
        {
            c.mc = new List<MCInst>();

            for(int i = 0; i < c.ir.Count;)
            {
                int end_node = i + 1;

                while (end_node < c.ir.Count && c.ir[end_node].parent.is_block_start == false)
                    end_node++;

                int count = end_node - i;

                while(count > 0)
                {
                    var instrs = c.t.ChooseInstruction(c.ir, i, count, c);
                    if (instrs != null)
                    {
                        c.ir[i].mc = instrs;
                        c.mc.AddRange(instrs);
                        break;
                    }

                    count--;
                }
                if (count == 0)
                    throw new Exception("Cannot encode " + c.ir[i].ToString());

                i += count;
            }
        }
    }

    public partial class Target
    {
        public abstract List<MCInst> ChooseInstruction(List<cil.CilNode.IRNode> node, int start, int count, Code c);
    }
}
