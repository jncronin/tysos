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

namespace elfhash
{
    class HashReader
    {
        BinaryReader r;
        int ver;
        int endian;
        int bitness;
        string fname;
        int ht_offset;
        long nbucket;
        long nchain;
        long bucket_start;
        long chain_start;
        binary_library.IBinaryFile bf;

        public binary_library.IBinaryFile BinaryFile { get { return bf; } set { bf = value; } }
        public string ObjectFileName { get { return fname; } }
        public int Version { get { return ver; } }
        public int Bitness { get { return bitness; } }
        public int Endianness { get { return endian; } }
        
        public HashReader(BinaryReader reader)
        {
            r = reader;

            // Validate
            string sig = "TYHASH  ";
            foreach (char c in sig)
            {
                if (r.ReadByte() != (byte)c)
                    throw new Exception("Invalid file format");
            }

            // Read ver, endianness, bitness
            ver = (int)r.ReadInt16();
            endian = (int)r.ReadSByte();
            bitness = (int)r.ReadSByte();

            // Read ht offset
            ht_offset = r.ReadInt32();

            // Read file name
            int fname_offset = r.ReadInt32();
            r.BaseStream.Seek(fname_offset, SeekOrigin.Begin);
            StringBuilder sb = new StringBuilder();
            byte b;
            while ((b = r.ReadByte()) != 0)
                sb.Append((char)b);
            fname = sb.ToString();

            // Read bucket/chain count
            r.BaseStream.Seek(ht_offset, SeekOrigin.Begin);
            nbucket = ReadIntPtr();
            nchain = ReadIntPtr();
            bucket_start = r.BaseStream.Position;
            chain_start = bucket_start + nbucket * EntrySize();
        }

        int EntrySize()
        {
            switch (bitness)
            {
                case 0:
                    return 4;
                case 1:
                    return 8;
                default:
                    throw new Exception("Unsupported bitness: " + bitness.ToString());
            }
        }

        long ReadIntPtr()
        {
            switch (bitness)
            {
                case 0:
                    return r.ReadInt32();
                case 1:
                    return r.ReadInt64();
                default:
                    throw new Exception("Unsupported bitness: " + bitness.ToString());
            }
        }

        public binary_library.ISymbol FindSymbol(string s)
        {
            if (bf == null)
                throw new Exception("BinaryFile not set");

            uint hash = Hash.HashFunction(s);
            long bucket = (long)hash % nbucket;

            r.BaseStream.Seek(bucket_start + bucket * EntrySize(), SeekOrigin.Begin);
            long cur_sym_idx = ReadIntPtr();

            while (cur_sym_idx != 0)
            {
                binary_library.ISymbol cur_sym = bf.GetSymbol((int)cur_sym_idx);
                if (s.Equals(cur_sym.Name))
                    return cur_sym;

                r.BaseStream.Seek(chain_start + cur_sym_idx * EntrySize(), SeekOrigin.Begin);
                cur_sym_idx = ReadIntPtr();
            }

            return null;
        }
    }
}
