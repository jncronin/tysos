/* Copyright (C) 2014-2016 by John Cronin
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

namespace binary_library.elf
{
    partial class ElfFile
    {
        static int[] elf_buckets = new int[] { 1, 3, 17, 37, 67, 97, 131, 197,
            263, 521, 1031, 2053, 4099, 8209, 16411, 32771, 0 };

        public static int CalculateBucketCount(int symbol_count)
        {
            for (int i = 0; elf_buckets[i] != 0; i++)
            {
                if (symbol_count < elf_buckets[i + 1])
                    return elf_buckets[i];
                if (elf_buckets[i + 1] == 0)
                    return elf_buckets[i];
            }
            return 0;   // never reached
        }
        
        // This is taken from tysos/ElfReader.cs
        internal static uint HashFunction(string s)
        {
            uint h = 0;
            uint g;

            foreach (char c in s)
            {
                h = (h << 4) + (byte)c;
                g = h & 0xf0000000;
                if (g != 0)
                    h ^= g >> 24;
                h &= 0x0fffffff;
            }

            return h;
        }

        // Build the hash table
        List<int>[] BuildHashTable(List<ElfSymbol> f)
        {
            int sc = f.Count;
            int bc = CalculateBucketCount(sc);
            List<int>[] ht = new List<int>[bc];

            for (int i = 1; i < sc; i++)
            {
                binary_library.ISymbol sym = f[i];
                uint hash = HashFunction(sym.Name);
                int bucket_no = (int)(hash % (uint)bc);

                if (ht[bucket_no] == null)
                    ht[bucket_no] = new List<int>();
                ht[bucket_no].Add(i);
            }

            return ht;
        }

        // Write out the hash table
        void WriteHash(BinaryWriter s, List<ElfSymbol> syms, ElfClass ec)
        {
            // Build hash table
            long hash_table_start = s.BaseStream.Position;
            List<int>[] ht = BuildHashTable(syms);
            WriteIntPtr(s, ht.Length, ec);
            WriteIntPtr(s, syms.Count, ec);

            // Build arrays to contain the bucket and chains
            long[] buckets = new long[ht.Length];
            long[] chains = new long[syms.Count];

            // Iterate through each chain
            for (int i = 0; i < ht.Length; i++)
            {
                List<int> bucket = ht[i];
                if (bucket == null)
                    continue;

                for (int j = 0; j < bucket.Count; j++)
                {
                    int chain = bucket[j];

                    // First entry is pointed to by bucket
                    if (j == 0)
                        buckets[i] = chain;

                    int next_chain = 0;
                    if (j < (bucket.Count - 1))
                        next_chain = bucket[j + 1];

                    chains[chain] = next_chain;
                }
            }

            // Write out
            foreach (long b in buckets)
                WriteIntPtr(s, b, ec);
            foreach (long c in chains)
                WriteIntPtr(s, c, ec);
        }

        void WriteIntPtr(BinaryWriter s, long val, ElfClass ec)
        {
            switch(ec)
            {
                case ElfClass.ELFCLASS32:
                case ElfClass.ELFCLASS64:
                    s.Write((int)val);
                    break;
                default:
                    throw new Exception("Unsupported elf class value: " + ec.ToString());
            }
        }
    }
}
