/* Copyright (C) 2015-2016 by John Cronin
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
using System.IO;
using System.Text;

namespace binary_library.binary
{
    public class HexFile : BinaryFile, IBinaryFile
    {
        public override Bitness Bitness
        {
            get
            {
                return Bitness.BitsUnknown;
            }

            set
            {
            }
        }

        public override IProgramHeader ProgramHeader
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void Write(BinaryWriter w)
        {
            // First, sort all progbits sections
            List<ISection> progbits = new List<ISection>();
            foreach (var sect in sections)
            {
                if (sect.HasData)
                    progbits.Add(sect);
            }
            progbits.Sort((a, b) => a.LoadAddress.CompareTo(b.LoadAddress));

            //StreamWriter sw = new StreamWriter(w.BaseStream, Encoding.ASCII);

            // Now rebase zero to be the load address of the first section
            uint addr = 0;
            ulong base_addr = progbits[0].LoadAddress;
            foreach (var sect in progbits)
            {
                if (sect.Length == 0)
                    continue;

                if (base_addr > sect.LoadAddress)
                    throw new Exception("Sections overlap!");

                // write zeros up to the current load address
                while (base_addr < sect.LoadAddress)
                {
                    Write(w.BaseStream, addr, 0);
                    base_addr++;
                    addr++;
                }

                // write the section data
                foreach (byte b in sect.Data)
                {
                    Write(w.BaseStream, addr, b);
                    base_addr++;
                    addr++;
                }
            }

            writeStr(w.BaseStream, ":00000001FF", true);
            w.BaseStream.Flush();
        }

        /* Implemented here incase mscorlib does not contain StreamWriter */
        public static void writeStr(Stream s, string v = "", bool newline = true)
        {
            foreach (var c in v)
                s.WriteByte((byte)c);
            if (newline)
                s.WriteByte((byte)'\n');
        }

        private void Write(Stream sw, uint addr, byte v)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(":01");
            sb.Append(addr.ToString("X4"));
            sb.Append("00");
            sb.Append(v.ToString("X2"));

            uint csum = 01 + addr + (addr >> 8) + v;
            csum &= 0xffU;
            csum = 0x100U - csum;
            csum &= 0xffU;
            sb.Append(csum.ToString("X2"));

            writeStr(sw, sb.ToString(), true);
        }
    }
}
