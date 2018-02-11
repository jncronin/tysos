/* Copyright (C) 2013-2016 by John Cronin
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

namespace binary_library
{
    abstract public class BinaryFile : IBinaryFile
    {
        protected string filename = "";
        protected string architecture = "";
        protected string os = "";
        protected string binary_type = "";
        protected string epoint = "";
        protected bool is_exec = false;
        protected List<ISection> sections = new List<ISection>();
        protected List<IRelocation> relocs = new List<IRelocation>();

        protected Dictionary<string, ISection> sect_map = new Dictionary<string, ISection>();

        protected ISection text, data, rdata, bss;

        public abstract Bitness Bitness { get; set; }

        public virtual string Filename
        {
            get
            {
                return filename;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                filename = value;
            }
        }

        public virtual string Architecture
        {
            get
            {
                return architecture;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                architecture = value;
            }
        }

        public virtual string OS
        {
            get
            {
                return os;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                os = value;
            }
        }

        public virtual string BinaryType
        {
            get
            {
                return binary_type;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                binary_type = value;
            }
        }

        public virtual string EntryPoint
        {
            get
            {
                return epoint;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                epoint = value;
            }
        }

        public virtual bool IsExecutable
        {
            get
            {
                return is_exec;
            }
            set
            {
                is_exec = value;
            }
        }

        public string NameTriple
        {
            get
            {
                return Architecture + "-" + BinaryType + "-" + OS;
            }
        }

        public abstract IProgramHeader ProgramHeader { get; }

        public virtual void Write()
        {
            System.IO.BinaryWriter w = new System.IO.BinaryWriter(new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write));
            Write(w);
            w.Close();
        }

        public virtual void Read()
        {
            sections.Clear();
            relocs.Clear();
            sect_map.Clear();
            System.IO.BinaryReader r = new System.IO.BinaryReader(new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read));
            Read(r);
            r.Close();
        }

        public virtual void Read(System.IO.Stream stream)
        {
            var r = new System.IO.BinaryReader(stream);
            Read(r);
        }

        protected virtual void Write(System.IO.BinaryWriter w) { throw new NotImplementedException(); }
        protected virtual void Read(System.IO.BinaryReader r) { throw new NotImplementedException(); }

        public virtual int GetSectionCount() { return sections.Count; }
        public virtual ISection GetSection(int idx) { return sections[idx]; }
        public virtual int AddSection(ISection section) { sections.Add(section); sect_map[section.Name] = section; return sections.Count - 1; }
        public virtual void RemoveSection(int idx) { sect_map.Remove(sections[idx].Name); sections.RemoveAt(idx); }
        public virtual int GetSymbolCount() {
            int ret = 0;
            foreach (var sect in sections)
            {
                if (sect == null)
                    continue;
                ret += sect.GetSymbolCount();
            }
            return ret;
        }
        public virtual ISymbol GetSymbol(int idx)  {
            foreach(var sect in sections)
            {
                if (sect == null)
                    continue;
                if (idx < sect.GetSymbolCount())
                    return sect.GetSymbol(idx);
                idx -= sect.GetSymbolCount();
            }
            return null;
        }
        public virtual IEnumerable<ISymbol> GetSymbols()
        {
            foreach(var sect in sections)
            {
                if (sect == null)
                    continue;
                foreach (var sym in sect.GetSymbols())
                    yield return sym;
            }
        }
        public virtual int GetRelocationCount() { return relocs.Count; }
        public virtual IRelocation GetRelocation(int idx) { return relocs[idx]; }
        public virtual int AddRelocation(IRelocation reloc) { relocs.Add(reloc); return relocs.Count - 1; }
        public virtual void RemoveRelocation(int idx) { relocs.RemoveAt(idx); }


        public virtual ISymbol FindSymbol(string name)
        {
            foreach(var sect in sections)
            {
                if (sect == null)
                    continue;
                var sym = sect.FindSymbol(name);
                if (sym != null)
                    return sym;
            }
            return null;
        }

        public virtual ISection FindSection(string name)
        {
            if (sect_map.TryGetValue(name, out ISection val))
                return val;
            else
                return null;
        }

#if HAVE_SYSTEM
        public virtual ISection FindSection(System.Text.RegularExpressions.Regex r)
        {
            foreach (ISection sect in sections)
            {
                if ((sect != null) && (r.IsMatch(sect.Name)))
                    return sect;
            }
            return null;
        }
#endif

        public virtual bool ContainsSymbol(ISymbol symbol)
        {
            foreach(var sect in sections)
            {
                if (sect.ContainsSymbol(symbol))
                    return true;
            }
            return false;
        }

        public virtual void Init()
        { }

        public virtual ISymbol CreateSymbol() { return new Symbol(); }
        public virtual ISection CreateSection() { return new GeneralSection(this); }
        public virtual ISection CreateContentsSection() { return new ContentsSection(this); }
        public virtual IRelocation CreateRelocation() { return new Relocation(); }

        public virtual ISection GetGlobalSection() { return null; }
        public virtual ISection GetCommonSection() { return null; }

        public static IBinaryFile CreateBinaryFile(string file_type)
        {
            foreach (System.Type type in typeof(BinaryFile).Assembly.GetTypes())
            {
                if (type.IsAbstract)
                    continue;
                if (type.GetInterface("IBinaryFile") == null)
                    continue;

                bool found = false;

                if (type.Name.EndsWith("File"))
                {
                    if (type.Name.Substring(0, type.Name.Length - 4).ToLower() == file_type.ToLower())
                        found = true;
                }

                object o;
                System.Reflection.ConstructorInfo ci = type.GetConstructor(System.Type.EmptyTypes);
                if (ci == null)
                    continue;
                o = ci.Invoke(null);

                IBinaryFileTypeName bftn = o as IBinaryFileTypeName;
                if ((bftn != null) && !found)
                {
                    string[] ftns = bftn.GetSupportedFileTypes();
                    foreach (string ftn in ftns)
                    {
                        if (ftn.ToLower() == file_type.ToLower())
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    return o as IBinaryFile;
            }

            return null;
        }

        public virtual ISection GetTextSection()
        {
            if (text == null)
            {
                text = new ContentsSection(this);
                text.Name = ".text";
                text.IsAlloc = true;
                text.IsWriteable = false;
                text.IsExecutable = true;
                text.AddrAlign = (int)Bitness / 4;
                AddSection(text);
            }
            return text;
        }

        

        public virtual ISection GetDataSection()
        {
            if (data == null)
            {
                data = new ContentsSection(this);
                data.Name = ".data";
                data.IsAlloc = true;
                data.IsWriteable = true;
                data.IsExecutable = false;
                data.AddrAlign = (int)Bitness / 4;
                AddSection(data);
            }
            return data;
        }

        public virtual ISection GetRDataSection()
        {
            if (rdata == null)
            {
                rdata = new ContentsSection(this);
                rdata.Name = ".rdata";
                rdata.IsAlloc = true;
                rdata.IsWriteable = false;
                rdata.IsExecutable = false;
                rdata.AddrAlign = (int)Bitness / 4;
                AddSection(rdata);
            }
            return rdata;
        }

        public virtual ISection GetBSSSection()
        {
            return null;
        }

        public virtual ISection CopySectionType(ISection tmpl)
        {
            ISection ret;
            if (tmpl is ContentsSection)
                ret = new ContentsSection(this);
            else if (tmpl is BssSection)
                ret = new BssSection(this);
            else
                throw new NotImplementedException();

            ret.AddrAlign = tmpl.AddrAlign;
            ret.IsAlloc = tmpl.IsAlloc;
            ret.IsExecutable = tmpl.IsExecutable;
            ret.IsWriteable = tmpl.IsWriteable;
            ret.LoadAddress = tmpl.LoadAddress;
            ret.Name = tmpl.Name;

            AddSection(ret);

            return ret;
        }
    }
}
