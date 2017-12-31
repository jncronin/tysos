/* Copyright (C) 2017-2018 by John Cronin
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
using System.Threading.Tasks;

namespace binary_library.archive
{
    class ArchiveFile : BinaryFile, IBinaryFile, IBinaryFileCollection, IBinaryFileTypeName
    {
        List<IBinaryFile> files = new List<IBinaryFile>();

        public override string Architecture { get => files[0].Architecture; set => throw new NotImplementedException(); }
        public override string OS { get => files[0].OS; set => throw new NotImplementedException(); }
        public override string BinaryType { get => files[0].BinaryType; set => throw new NotImplementedException(); }

        public override string EntryPoint {
            get
            {
                foreach(var file in files)
                    if (file.EntryPoint != null) return file.EntryPoint;
                return null;

            }
            set => throw new NotImplementedException();
        }
        public override Bitness Bitness { get => files[0].Bitness; set => throw new NotImplementedException(); }
        public override bool IsExecutable { get => false; set => throw new NotImplementedException(); }

        public override IProgramHeader ProgramHeader => throw new NotImplementedException();

        public override int AddRelocation(IRelocation reloc)
        {
            throw new NotImplementedException();
        }

        public override int AddSection(ISection section)
        {
            throw new NotImplementedException();
        }

        public override int AddSymbol(ISymbol symbol)
        {
            throw new NotImplementedException();
        }

        public override bool ContainsSymbol(ISymbol symbol)
        {
            foreach (var file in files)
                if (file.ContainsSymbol(symbol)) return true;
            return false;
        }

        public override ISection CopySectionType(ISection tmpl)
        {
            throw new NotImplementedException();
        }

        public override ISection CreateContentsSection()
        {
            throw new NotImplementedException();
        }

        public override IRelocation CreateRelocation()
        {
            throw new NotImplementedException();
        }

        public override ISection CreateSection()
        {
            throw new NotImplementedException();
        }

        public override ISymbol CreateSymbol()
        {
            throw new NotImplementedException();
        }

        public IBinaryFile FindBinaryFile(string name)
        {
            foreach (var file in files)
                if (file.Filename == name) return file;
            return null;
        }

        public override ISection FindSection(string name)
        {
            foreach(var file in files)
            {
                var sect = file.FindSection(name);
                if (sect != null) return sect;
            }
            return null;
        }

        public override ISymbol FindSymbol(string name)
        {
            foreach (var file in files)
            {
                var sym = file.FindSymbol(name);
                if (sym != null) return sym;
            }
            return null;
        }

        public IBinaryFile GetBinaryFile(int idx)
        {
            return files[idx];
        }

        public int GetBinaryFileCount()
        {
            return files.Count;
        }

        public override ISection GetBSSSection()
        {
            throw new NotImplementedException();
        }

        public override ISection GetCommonSection()
        {
            throw new NotImplementedException();
        }

        public override ISection GetDataSection()
        {
            throw new NotImplementedException();
        }

        public override ISection GetGlobalSection()
        {
            throw new NotImplementedException();
        }

        public override ISection GetRDataSection()
        {
            throw new NotImplementedException();
        }

        public override IRelocation GetRelocation(int idx)
        {
            foreach(var file in files)
            {
                if (idx < file.GetRelocationCount())
                    return file.GetRelocation(idx);
                idx -= file.GetRelocationCount();
            }
            return null;
        }

        public override int GetRelocationCount()
        {
            int ret = 0;
            foreach (var file in files)
                ret += file.GetRelocationCount();
            return ret;
        }

        public override ISection GetSection(int idx)
        {
            throw new NotImplementedException();
        }

        public override int GetSectionCount()
        {
            throw new NotImplementedException();
        }

        public string[] GetSupportedFileTypes()
        {
            return new string[] { ".a", "archive" };
        }

        public override ISymbol GetSymbol(int idx)
        {
            foreach (var file in files)
            {
                if (idx < file.GetSymbolCount())
                    return file.GetSymbol(idx);
                idx -= file.GetSymbolCount();
            }
            return null;
        }

        public override int GetSymbolCount()
        {
            int ret = 0;
            foreach (var file in files)
                ret += file.GetSymbolCount();
            return ret;
        }

        public override ISection GetTextSection()
        {
            throw new NotImplementedException();
        }

        public override void Init()
        {
        }

        public override void RemoveRelocation(int idx)
        {
            throw new NotImplementedException();
        }

        public override void RemoveSection(int idx)
        {
            throw new NotImplementedException();
        }

        public override void RemoveSymbol(int idx)
        {
            throw new NotImplementedException();
        }

        public override void Write()
        {
            throw new NotImplementedException();
        }

        protected override void Read(BinaryReader r)
        {
            // First ensure the file contains the appropriate magiv
            var magic = new string(r.ReadChars(8));
            if (magic != "!<arch>\n")
                throw new Exception("Archive magic not found");

            // Extended filename table
            Dictionary<int, string> ext_fname = new Dictionary<int, string>();

            // Read each file in turn
            try
            {
                while (true)
                {
                    /* Read 16 byte file name */
                    string fname = new string(r.ReadChars(16));
                    fname = fname.Trim();
                    if(fname != "//" && fname != "/")
                        fname = fname.TrimEnd('/');

                    /* Read entries we are uninterested in */
                    r.ReadBytes(12);        // File modification time stamp
                    r.ReadBytes(6);         // Owner ID
                    r.ReadBytes(6);         // Group ID
                    r.ReadBytes(8);         // File mode

                    /* File size is ascii characters */
                    string fsize = new string(r.ReadChars(10)).Trim();
                    if (fsize == "")
                        fsize = "0";        // handle empty entries
                    var fs = int.Parse(fsize);

                    /* End chars */
                    var end = r.ReadBytes(2);
                    if (end == null || end.Length < 2)
                        throw new EndOfStreamException();
                    if (end[0] != 0x60 || end[1] != 0x0a)
                        throw new Exception("Invalid ending characters");

                    /* Read the data */
                    var d = r.ReadBytes(fs);

                    /* Determine what to do based on file name */
                    if (fname == "/")
                    {
                        // Symbol lookup table - ignore
                    }
                    else if (fname == "//")
                    {
                        // extended file name table

                        // parse as ascii, splitting on line feed characters
                        int ptr = 0;
                        int sptr = 0;
                        StringBuilder sb = new StringBuilder();
                        while(ptr < d.Length)
                        {
                            var c = (char)d[ptr++];
                            if (c == '\n')
                            {
                                ext_fname[sptr] = sb.ToString().TrimEnd('/');
                                sptr = ptr;
                                sb = new StringBuilder();
                            }
                            else
                                sb.Append(c);
                        }
                    }
                    else
                    {
                        if (fname.StartsWith("/"))
                        {
                            // extended file name
                            int offset = int.Parse(fname.TrimStart('/'));
                            fname = ext_fname[offset];
                        }

                        /* Load up the archive member - elf only for now */
                        var fi = new FileInfo(fname);
                        var bi = CreateBinaryFile(fi.Extension);

                        bi.Filename = fname;
                        bi.Read(new MemoryStream(d));

                        files.Add(bi);
                    }

                    /* Advance to a multiple of 2 bytes */
                    if (r.BaseStream.Position % 2 == 1)
                        r.ReadByte();
                }
            }
            catch(System.IO.EndOfStreamException)
            {
                // do nothing - this is expected
            }
        }
    }
}
