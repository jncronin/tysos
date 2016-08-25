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

namespace libtysila4.target
{
    public class RemoveUnnecessaryMoves
    {
        public static graph.Graph RemoveUnnecessaryMovesPass(graph.Graph input, object target)
        {
            var t = Target.targets[target as string];

            foreach (var n in input.LinearStream)
            {
                var mcn = n.c as MCNode;

                for (int i = 0; i < mcn.insts.Count; i++)
                {
                    var I = mcn.insts[i];
                    if (t.IsMoveMreg(I) && t.GetMoveSrc(I).mreg.Equals(t.GetMoveDest(I).mreg))
                    {
                        mcn.insts.RemoveAt(i);
                        i--;
                    }
                    else if(t.IsBranch(I) && i < (mcn.insts.Count - 1))
                    {
                        // this is an unnecessary branch in the middle of a
                        //  basic block - can be removed
                        mcn.insts.RemoveAt(i);
                        i--;
                    }
                }
            }

            return input;
        }
    }
}