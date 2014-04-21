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

/* tybel = tysila back-end language */

using System;
using System.Collections.Generic;

namespace libtysila.tybel
{
    public class Tybel : timple.BaseGraph
    {
        public Dictionary<timple.TreeNode, IList<Node>> TimpleMap;
        public int NextVar;

        public static Tybel BuildGraph(timple.Optimizer.OptimizeReturn code, Assembler ass)
        {
            Tybel ret = new Tybel();
            ret.TimpleMap = new Dictionary<timple.TreeNode, IList<Node>>();
            ret.NextVar = 0;

            /* Determine the greatest logical var in use */
            foreach (vara v in code.Liveness.defs.Keys)
            {
                if (v.LogicalVar >= ret.NextVar)
                    ret.NextVar = v.LogicalVar + 1;
            }

            foreach (timple.TreeNode n in code.Code)
            {
                IList<Node> tybel = ass.SelectInstruction(n, ref ret.NextVar);
                ret.TimpleMap[n] = tybel;
            }

            util.Set<timple.TreeNode> visited = new util.Set<timple.TreeNode>();
            foreach (timple.TreeNode n in code.CodeTree.Starts)
            {
                ret.Starts.Add(ret.TimpleMap[n][0]);
                ret.DFAdd(n, visited);
            }

            return ret;
        }

        private void DFAdd(timple.TreeNode n, util.Set<timple.TreeNode> visited)
        {
            if (visited.Contains(n))
                return;

            visited.Add(n);

            for (int i = 1; i < TimpleMap[n].Count; i++)
                AddEdge(TimpleMap[n][i - 1], TimpleMap[n][i]);

            Node last = TimpleMap[n][TimpleMap[n].Count - 1];

            if (n.next.Count == 0)
                Ends.Add(last);
            else
            {
                foreach (timple.TreeNode next in n.next)
                {
                    AddEdge(last, TimpleMap[next][0]);
                    DFAdd(next, visited);
                }
            }
        }

        private void AddEdge(Node parent, Node c)
        {
            if (c.next == null)
                c.next = new List<timple.BaseNode>();
            if (c.prev == null)
                c.prev = new List<timple.BaseNode>();

            if (parent.next == null)
                parent.next = new List<timple.BaseNode>();
            if (parent.prev == null)
                parent.prev = new List<timple.BaseNode>();

            parent.Next.Add(c);
            c.Prev.Add(parent);
        }

        public int Count
        {
            get
            {
                util.Set<timple.BaseNode> visited = new util.Set<timple.BaseNode>();
                int n = 0;
                foreach (timple.BaseNode node in Starts)
                    DFCount(node, visited, ref n);
                return n;
            }
        }

        private void DFCount(timple.BaseNode s, util.Set<timple.BaseNode> visited, ref int n)
        {
            if (!visited.Contains(s))
            {
                visited.Add(s);

                foreach (timple.BaseNode suc in s.Next)
                    DFCount(suc, visited, ref n);

                n++;
            }
        }

        public IList<timple.BaseNode> LinearStream
        {
            get
            {
                Node[] ret = new Node[Count];
                util.Set<timple.BaseNode> visited = new util.Set<timple.BaseNode>();
                int n = Count - 1;
                foreach (timple.BaseNode s in Starts)
                    DFS(s, ret, ref n, visited);
                return ret;
            }
        }

        private void DFS(timple.BaseNode s, timple.BaseNode[] ret, ref int n, util.Set<timple.BaseNode> visited)
        {
            /* algorithm 17.5 from Appel */
            if (visited.Contains(s) == false)
            {
                visited.Add(s);

                /* we do the last successor first, as that is the non-default path, and should come last */
                for (int i = s.Next.Count - 1; i >= 0; i--)
                    DFS(s.Next[i], ret, ref n, visited);

                ret[n] = s;
                n--;
            }
        }

    }
}
