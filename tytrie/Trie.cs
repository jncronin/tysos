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
using System.Text;
using System.IO;

namespace tytrie
{
    class Trie
    {
        Palette pal;
        TrieTier base_tier;
        Dictionary<string, ulong> unencodable;

        class TrieTier
        {
            public bool is_terminal;
            public ulong terminal;
            public TrieTier[] Tiers;
            public int file_offset;

            public TrieTier(Palette palette)
            {
                Tiers = new TrieTier[palette.Size];
            }
        }

        public Trie(Palette palette) { pal = palette; Init(); }
        public Trie() { pal = new Palette(); Init(); }

        delegate void TraversalDelegate(TrieTier tier, object[] p); 

        void Init()
        {
            base_tier = new TrieTier(pal);
            unencodable = new Dictionary<string, ulong>();
        }

        public bool AddSymbol(string name, ulong val)
        { return AddSymbol(name, val, false); }
        public bool AddSymbol(string name, ulong val, bool overwrite)
        {
            if (!IsEncodable(name))
            {
                if (overwrite || !unencodable.ContainsKey(name))
                {
                    unencodable[name] = val;
                    return true;
                }
                return false;
            }

            TrieTier current_tier = base_tier;

            foreach (char c in name)
            {
                int idx = pal.GetIdx(c);
                current_tier.is_terminal = false;
                if (current_tier.Tiers[idx] == null)
                    current_tier.Tiers[idx] = new TrieTier(pal);
                current_tier = current_tier.Tiers[idx];
            }
            int null_idx = pal.GetIdx('\0');
            if ((current_tier.Tiers[null_idx] == null) || overwrite)
            {
                current_tier.Tiers[null_idx] = new TrieTier(pal);
                current_tier.Tiers[null_idx].is_terminal = true;
                current_tier.Tiers[null_idx].terminal = val;
                return true;
            }
            else
                return false;
        }

        bool IsEncodable(string name)
        {
            try
            {
                foreach (char c in name)
                    pal.GetIdx(c);
                pal.GetIdx('\0');
            }
            catch (KeyNotFoundException)
            {
                return false;
            }

            return true;
        }

        void PostOrderTraverse(TraversalDelegate func, object[] p)
        {
            PostOrderTraverse(base_tier, func, p);
        }

        void PostOrderTraverse(TrieTier cur_node, TraversalDelegate func, object[] p)
        {
            // Visit all subtrees, then perform the function on the current node
            for (int i = 0; i < cur_node.Tiers.Length; i++)
            {
                if (cur_node.Tiers[i] != null)
                    PostOrderTraverse(cur_node.Tiers[i], func, p);
            }
            func(cur_node, p);
        }

        public void Write(BinaryWriter s, int ver, int endian, int bitness)
        {
            // Write header
            long header_offset = s.BaseStream.Position;
            s.Write((byte)'T');
            s.Write((byte)'Y');
            s.Write((byte)'T');
            s.Write((byte)'R');
            s.Write((byte)'I');
            s.Write((byte)'E');
            s.Write((byte)' ');
            s.Write((byte)' ');
            s.Write((ushort)ver);
            s.Write((byte)endian);
            s.Write((byte)bitness);

            long base_trie_offset = s.BaseStream.Position;
            s.Write((int)0);

            long palette_offset = s.BaseStream.Position;
            s.Write((int)0);

            // palette size
            s.Write((int)pal.Size);

            long unencodable_offset = s.BaseStream.Position;
            s.Write((int)0);

            // unencodable size
            s.Write((int)unencodable.Count);

            // Write out the tries, deepest first
            PostOrderTraverse(WriteTrie, new object[] { s, endian, bitness });

            // Write out starting trie
            long cur_offset = s.BaseStream.Position;
            s.BaseStream.Seek(base_trie_offset, SeekOrigin.Begin);
            s.Write((int)base_tier.file_offset);
            
            // Write out palette offset
            s.BaseStream.Seek(palette_offset, SeekOrigin.Begin);
            s.Write((int)cur_offset);

            // Write out palette
            s.BaseStream.Seek(cur_offset, SeekOrigin.Begin);
            for (int i = 0; i < pal.Size; i++)
                s.Write((byte)pal.GetChar(i));

            // Write out unencodable strings
            Dictionary<string, long> unencodable_offsets = new Dictionary<string, long>();
            foreach (string unenc_s in unencodable.Keys)
            {
                unencodable_offsets[unenc_s] = s.BaseStream.Position;
                foreach (char c in unenc_s)
                    s.Write((byte)c);
                s.Write((byte)0);
            }
            cur_offset = s.BaseStream.Position;
            foreach (string unenc_s in unencodable.Keys)
            {
                long pos = unencodable_offsets[unenc_s];
                ulong val = unencodable[unenc_s];
                s.Write((int)pos);
                if (bitness == 0)
                    s.Write((uint)val);
                else if (bitness == 1)
                    s.Write((ulong)val);
                else
                    throw new Exception();
            }
            long end_offset = s.BaseStream.Position;
            s.BaseStream.Seek(unencodable_offset, SeekOrigin.Begin);
            s.Write((int)cur_offset);
            s.BaseStream.Seek(end_offset, SeekOrigin.Begin);
        }

        void WriteTrie(TrieTier cur_node, object[] p)
        {
            BinaryWriter s = p[0] as BinaryWriter;
            int endian = (int)p[1];
            int bitness = (int)p[2];

            if (cur_node.is_terminal)
                return;

            cur_node.file_offset = (int)s.BaseStream.Position;
            foreach (TrieTier child in cur_node.Tiers)
            {
                ulong val = 0;
                if (child != null)
                {
                    if (child.is_terminal)
                        val = child.terminal;
                    else
                    {
                        if (child.file_offset == 0)
                            throw new Exception("Child file offset is 0");
                        val = (ulong)child.file_offset;
                    }
                }

                if (bitness == 0)
                    s.Write((uint)val);
                else if (bitness == 1)
                    s.Write((ulong)val);
                else
                    throw new Exception("Unknown bitness value: " + bitness.ToString());
            }
        }
    }
}
