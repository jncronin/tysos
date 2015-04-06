/* Copyright (C) 2012 by John Cronin
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

namespace tysos.jit
{
    class JitOutput : libtysila.IOutputFile
    {
        public List<byte> text = new List<byte>();
        public List<byte> data = new List<byte>();
        public List<byte> rodata = new List<byte>();
        public List<byte> bss = new List<byte>();
        public Dictionary<string, int> text_sym = new Dictionary<string, int>(new libtysila.GenericEqualityComparer<string>());
        public Dictionary<string, int> data_sym = new Dictionary<string, int>(new libtysila.GenericEqualityComparer<string>());
        public Dictionary<string, int> rodata_sym = new Dictionary<string, int>(new libtysila.GenericEqualityComparer<string>());
        public Dictionary<string, int> bss_sym = new Dictionary<string, int>(new libtysila.GenericEqualityComparer<string>());
        public Dictionary<int, Relocation> text_rel = new Dictionary<int, Relocation>(new libtysila.GenericEqualityComparer<int>());
        public Dictionary<int, Relocation> data_rel = new Dictionary<int, Relocation>(new libtysila.GenericEqualityComparer<int>());
        public Dictionary<int, Relocation> rodata_rel = new Dictionary<int, Relocation>(new libtysila.GenericEqualityComparer<int>());
        public Dictionary<int, Relocation> bss_rel = new Dictionary<int, Relocation>(new libtysila.GenericEqualityComparer<int>());

        public class Relocation
        {
            public string Name;
            public uint RelType;
            public long Value;

            static Dictionary<uint, string> reloc_types;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder(Name);
                if (Value != 0)
                    sb.Append(" + 0x" + Value.ToString("x"));
                if (reloc_types.ContainsKey(RelType))
                    sb.Append("   (" + reloc_types[RelType] + ")");
                return sb.ToString();
            }

            static Relocation()
            {
                reloc_types = new Dictionary<uint, string>(new libtysila.GenericEqualityComparer<uint>());
                Type t = typeof(libtysila.x86_64.x86_64_elf64);
                System.Reflection.FieldInfo[] fis = t.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                foreach (System.Reflection.FieldInfo fi in fis)
                {
                    if (fi.IsLiteral)
                        reloc_types.Add((uint)fi.GetValue(null), fi.Name);
                }
            }
        }


        public IList<byte> GetText()
        {
            return text;
        }

        public IList<byte> GetData()
        {
            return data;
        }

        public IList<byte> GetRodata()
        {
            return rodata;
        }

        public void AlignText(int a)
        {
            
        }

        public void AlignData(int a)
        {
        }

        public void AlignRodata(int a)
        {
        }

        public void AlignBss(int a)
        { }

        public void AddBssBytes(int count)
        {
            while (count-- > 0)
                bss.Add(0);
        }

        public int GetBssOffset()
        {
            return bss.Count;
        }

        public libtysila.ISymbol AddTextSymbol(int offset, string name, bool local_only, bool is_func, bool is_weak)
        {
            text_sym.Add(name, offset);
            return new JitSym { Name = name, Offset = offset };
        }

        public libtysila.ISymbol AddDataSymbol(int offset, string name, bool is_weak)
        {
            data_sym.Add(name, offset);
            return new JitSym { Name = name, Offset = offset };
        }

        public libtysila.ISymbol AddRodataSymbol(int offset, string name, bool is_weak)
        {
            rodata_sym.Add(name, offset);
            return new JitSym { Name = name, Offset = offset };
        }

        public libtysila.ISymbol AddBssSymbol(int offset, string name, bool is_weak)
        {
            bss_sym.Add(name, offset);
            return new JitSym { Name = name, Offset = offset };
        }

        public void AddTextRelocation(int offset, string name, uint rel_type, long value)
        {
            text_rel.Add(offset, new Relocation { Name = name, RelType = rel_type, Value = value });
        }

        public void AddDataRelocation(int offset, string name, uint rel_type, long value)
        {
            data_rel.Add(offset, new Relocation { Name = name, RelType = rel_type, Value = value });
        }

        public void AddRodataRelocation(int offset, string name, uint rel_type, long value)
        {
            rodata_rel.Add(offset, new Relocation { Name = name, RelType = rel_type, Value = value });
        }

        public void AddBssRelocation(int offset, string name, uint rel_type, long value)
        {
            bss_rel.Add(offset, new Relocation { Name = name, RelType = rel_type, Value = value });
        }

        public void SetEntryPoint(string name)
        {
            //throw new NotImplementedException();
        }

        public void Write(System.IO.Stream output)
        {
            //throw new NotImplementedException();
        }

        public void DumpText(System.IO.TextWriter output)
        {
            //throw new NotImplementedException();
        }

        class JitSym : libtysila.ISymbol
        {
            string name;
            int offset;
            int length;

            public string Name
            {
                get
                {
                    return name;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public int Offset
            {
                get
                {
                    return offset;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public int Length
            {
                get
                {
                    return length;
                }
                set
                {
                    length = value;
                }
            }

            public bool Weak
            {
                get
                {
                    return false;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
