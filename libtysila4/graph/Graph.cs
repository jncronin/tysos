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
using System.Linq;
using System.Text;

namespace libtysila4.graph
{
    public class Graph
    {
        public List<BaseNode> Starts = new List<BaseNode>();
        public List<BaseNode> Ends = new List<BaseNode>();

        public List<BaseNode> bb_starts = new List<BaseNode>();
        public List<BaseNode> bb_ends = new List<BaseNode>();
        public List<List<BaseNode>> blocks = new List<List<BaseNode>>();
        public List<List<int>> bbs_before = new List<List<int>>();
        public List<List<int>> bbs_after = new List<List<int>>();

        public cil.CilGraph cg = null;

        public DominanceGraph DominanceGraph;

        public int BBCount { get { return bb_starts.Count; } }

        public int next_vreg_id;

        public List<BaseNode> LinearStream = new List<BaseNode>();
        public virtual string LinearStreamString
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(var n in LinearStream)
                {
                    sb.Append(n.ToString());
                    sb.Append(Environment.NewLine);
                }
                return sb.ToString();
            }
        }

        public delegate Graph PassDelegate(Graph InputGraph, target.Target t);

        public Graph RunPass(PassDelegate pass, target.Target t)
        { return pass(this, t); }

        public void RefreshBasicBlocks()
        {
            bb_starts.Clear();
            bb_ends.Clear();
            blocks.Clear();
            bbs_before.Clear();
            bbs_after.Clear();

            Dictionary<BaseNode, bool> done = new Dictionary<BaseNode, bool>(
                new GenericEqualityComparer<BaseNode>());

            foreach (var n in LinearStream)
                done[n] = false;

            foreach(var n in LinearStream)
            {
                if (done[n])
                    continue;

                /* Build the longest possible list of nodes from here:
                    1) First node can have more than one predecessor,
                        no others can.
                    2) Last node can have zero or more than one
                        successor, no others can.
                */

                List<BaseNode> cur_blocks = new List<BaseNode>();

                BaseNode cn = n;
                cn.bb = blocks.Count;
                done[cn] = true;
                cur_blocks.Add(cn);

                if(cn.NextCount == 1)
                {
                    cn = cn.Next1;
                    while(true)
                    {
                        // If multiple nodes converge on this, we
                        //  can't use it
                        if (cn.PrevCount != 1)
                            break;

                        cn.bb = blocks.Count;
                        done[cn] = true;
                        cur_blocks.Add(cn);

                        // If multiple nodes (or no nodes) leave, we
                        //  also can't use any beyond it
                        if (cn.NextCount != 1)
                            break;

                        cn = cn.Next1;
                    }
                }

                bb_starts.Add(cur_blocks[0]);
                bb_ends.Add(cur_blocks[cur_blocks.Count - 1]);
                blocks.Add(cur_blocks);
            }

            // Build bb graph
            foreach (var bb in blocks)
            {
                var first = bb[0];
                var last = bb[bb.Count - 1];

                List<int> cbbs_before = new List<int>();
                foreach (var prev in first.Prev)
                    cbbs_before.Add(prev.bb);
                bbs_before.Add(cbbs_before);

                List<int> cbbs_after = new List<int>();
                foreach (var next in last.Next)
                    cbbs_after.Add(next.bb);
                bbs_after.Add(cbbs_after);
            }
        }
    }

    public class AssembledCodeGraph : Graph
    {
        public Graph g;
        public binary_library.IBinaryFile bf;
        public binary_library.ISection text_section;

        public override string LinearStreamString
        {
            get
            {
                var Code = text_section.Data;
                int i = 0;
                StringBuilder sb = new StringBuilder();
                foreach(var b in Code)
                {
                    if(i == 8)
                    {
                        sb.Append(Environment.NewLine);
                        i = 0;
                    }
                    if (i > 0)
                        sb.Append(" ");
                    sb.Append("0x");
                    sb.Append(b.ToString("X2"));
                    i++;
                }
                if (i != 0)
                    sb.Append(Environment.NewLine);
                return sb.ToString();
            }
        }
    }
}
