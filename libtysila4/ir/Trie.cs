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

namespace libtysila4.ir
{
    class Trie<T, U> where T : class
    {
        TrieNode start = new TrieNode();

        class TrieNode
        {
            internal T n;
            internal bool is_end;
            internal U ret;
            internal List<TrieNode> next = new List<TrieNode>();
        }

        public bool Find(IList<T> nodes, ref U val)
        {
            TrieNode cn = start;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if(i == nodes.Count - 1)
                {
                    if (cn.is_end)
                    {
                        val = cn.ret;
                        return true;
                    }
                    else
                        return false;
                }

                bool found = false;
                foreach (var next in cn.next)
                {
                    if (next.n == node)
                    {
                        found = true;
                        cn = next;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return false;
        }

        public void Add(List<T> nodes, U val)
        {
            TrieNode cn = start;
            for(int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                bool found = false;
                foreach(var next in cn.next)
                {
                    if(next.n == node)
                    {
                        found = true;
                        cn = next;
                        break;
                    }
                }

                if(!found)
                {
                    var nn = new TrieNode();
                    nn.n = node;
                    cn.next.Add(nn);
                    cn = nn;
                }

                if (i == nodes.Count - 1)
                {
                    cn.is_end = true;
                    cn.ret = val;
                }
            }
        }
    }
}
