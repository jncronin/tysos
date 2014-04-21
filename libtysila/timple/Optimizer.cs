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

namespace libtysila.timple
{
    public class Optimizer
    {
        public class OptimizeReturn
        {
            public IList<BaseNode> Code;
            public TimpleGraph CodeTree;
            public Liveness Liveness;
        }

        public static OptimizeReturn Optimize(IList<TreeNode> tacs)
        { return Optimize(tacs, true); }

        public static OptimizeReturn Optimize(IList<TreeNode> tacs, bool optimize)
        {
            TimpleGraph g = libtysila.timple.TimpleGraph.BuildGraph(tacs);
            DomTree d = libtysila.timple.DomTree.BuildDomTree(g);
            Liveness l = libtysila.timple.Liveness.LivenessAnalysis(g);

            if (!optimize)
                return new OptimizeReturn { Code = g.LinearStream, CodeTree = g, Liveness = l };

            SSATree ssa = libtysila.timple.SSATree.BuildSSATree(g, d, l);

            Liveness ssa_l = libtysila.timple.Liveness.LivenessAnalysis(ssa);
            DeadCodeElimination.DoElimination(ssa, ssa_l);
            ConstantPropagation.DoPropagation(ssa, ssa_l, d);
            TimpleGraph opt = ssa.ConvertFromSSA();
            ssa_l.TrimEmpty();

            Liveness opt_l = Liveness.LivenessAnalysis(opt);

            return new OptimizeReturn { Code = opt.LinearStream, CodeTree = opt, Liveness = opt_l };
        }
    }
}
