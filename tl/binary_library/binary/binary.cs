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

namespace binary_library.binary
{
    public class FlatBinaryFile : BinaryFile, IBinaryFile
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
            foreach(var sect in sections)
            {
                if (sect.HasData)
                    progbits.Add(sect);
            }
            progbits.Sort((a, b) => a.LoadAddress.CompareTo(b.LoadAddress));

            if (progbits.Count == 0)
                return;

            // Now rebase zero to be the load address of the first section
            ulong base_addr = progbits[0].LoadAddress;
            foreach(var sect in progbits)
            {
                if (sect.Length == 0)
                    continue;

                if (base_addr > sect.LoadAddress)
                    throw new Exception("Sections overlap!");

                // write zeros up to the current load address
                while (base_addr < sect.LoadAddress)
                {
                    w.Write((byte)0);
                    base_addr++;
                }

                // write the section data
                foreach(byte b in sect.Data)
                {
                    w.Write(b);
                    base_addr++;
                }
            }
        }
    }
}
