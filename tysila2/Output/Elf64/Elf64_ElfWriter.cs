/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace Elf64
{
    internal class Elf64Writer : libtysila.IOutputFile
    {
        Elf64_Ehdr ehdr;
        string e_point;
        string s_filename;
        libtysila.Assembler ass;

        class static_fields_pointer { public string static_object; public string typeinfo_name; }
        List<static_fields_pointer> static_fields = new List<static_fields_pointer>();

        public void AddStaticClassPointer(string static_object_name, string typeinfo_name)
        {
            static_fields.Add(new static_fields_pointer { static_object = static_object_name, typeinfo_name = typeinfo_name });
        }

        public void SetEntryPoint(string entry_point)
        { e_point = entry_point; }

        public Elf64Writer(string source_filename, string comment, libtysila.Assembler assembler)
        {
            ass = assembler;
            s_filename = source_filename;
            ehdr = new Elf64_Ehdr(source_filename, comment);
        }

        private bool WriteObjectFile(System.IO.Stream output)
        {
            // Write out the elf file.

            // Relocatable files are:
            //
            //   ELF header
            //   Program header table (optional)
            //   Section 1
            //   Section 2
            //   ...
            //   Section n
            //   Section header table

            // Find the entry point symbol
            string ept_sym = "_start";
            if (ass.Options.EntryPointName != null)
                ept_sym = ass.Options.EntryPointName;

            if (e_point != null)
            {
                Elf64_Symbol_Shdr.Elf64_Sym entry_sym = null;
                Elf64_Symbol_Shdr.Elf64_Sym epoint_sym = null;
                foreach (Elf64_Symbol_Shdr.Elf64_Sym sym in ehdr.e_syms.defined_syms)
                {
                    if (sym.name == ept_sym)
                        entry_sym = sym;
                    if (sym.name == e_point)
                        epoint_sym = sym;
                }

                if (entry_sym != null)
                    ehdr.e_syms.defined_syms.Remove(entry_sym);
                if (epoint_sym != null)
                    AddTextSymbol((int)epoint_sym.st_value, ept_sym, false, true, epoint_sym.IsWeak);
            }

            // First, determine the offsets of the various tables
            ulong cur_offset;

            if (ehdr.phdrs.Count > 0)
            {
                ehdr.e_phoff = (ulong)Align(Elf64_Ehdr.GetLength(), 8);
                ehdr.e_phentsize = (ushort)Elf64_Phdr.GetLength();
                cur_offset = ehdr.e_phoff + (ulong)ehdr.e_phnum * (ulong)ehdr.e_phentsize;}
            else
            {
                ehdr.e_phoff = 0;
                ehdr.e_phentsize = 0;
                cur_offset = (ulong)Align(Elf64_Ehdr.GetLength(), 8);
            }            

            ehdr.GenerateRelocationSymbols();

            foreach (Elf64_Shdr shdr in ehdr.shdrs)
            {
                if (shdr.sh_type != SectionType.SHT_NULL)
                {
                    shdr.sh_offset = Align(cur_offset, 8);
                    cur_offset = shdr.sh_offset + shdr.sh_size;
                }
            }

            ehdr.e_shoff = (ulong)Align(cur_offset, 8);

            // Now write the actual elf header table
            output.Seek(0, SeekOrigin.Begin);
            ehdr.Write(output);

            // Write program header table
            output.Seek((long)ehdr.e_phoff, SeekOrigin.Begin);
            foreach (Elf64_Phdr phdr in ehdr.phdrs)
                phdr.Write(output);

            // Write actual section data
            foreach (Elf64_Shdr shdr in ehdr.shdrs)
            {
                output.Seek((long)shdr.sh_offset, SeekOrigin.Begin);
                Write(output, shdr.data);
            }

            // Write section header data
            output.Seek((long)ehdr.e_shoff, SeekOrigin.Begin);
            foreach (Elf64_Shdr shdr in ehdr.shdrs)
                shdr.Write(output);

            return true;
        }

        public void AlignText(int a)
        { Align(ehdr.text.data, a); }
        public void AlignData(int a)
        { Align(ehdr.data.data, a); }
        public void AlignRodata(int a)
        { Align(ehdr.rodata.data, a); }

        private void Align(IList<byte> iList, int a)
        {
            while ((iList.Count % a) != 0)
                iList.Add(0);
        }

        private int Align(int v, int align)
        {
            int extra = v % align;
            if (extra == 0)
                return v;
            else
                return v - extra + align;
        }

        private ulong Align(ulong v, ulong align)
        {
            ulong extra = v % align;
            if (extra == 0)
                return v;
            else
                return v - extra + align;
        }

        public static void Write(object s, byte v)
        {
            if (s is Stream)
                ((Stream)s).WriteByte(v);
            else if (s is ICollection<byte>)
                ((ICollection<byte>)s).Add(v);
            else
                throw new Exception("Unknown container type");
        }

        public static void Write(object s, UInt16 v)
        {
            Write(s, (byte)(v & 0xff));
            Write(s, (byte)((v >> 8) & 0xff));
        }

        public static void Write(object s, UInt32 v)
        {
            Write(s, (UInt16)(v & 0xffff));
            Write(s, (UInt16)((v >> 16) & 0xffff));
        }

        public static void Write(object s, UInt64 v)
        {
            Write(s, (UInt32)(v & 0xffffffff));
            Write(s, (UInt32)((v >> 32) & 0xffffffff));
        }

        public static void Write(object s, Int64 v)
        {
            if (v >= 0)
                Write(s, (UInt64)v);
            else
                Write(s, (~((UInt64)(-v))) + 1);
        }

        public static void Write(object s, IEnumerable<byte> v)
        {
            if (v == null)
                return;
            foreach (byte b in v)
                Write(s, b);
        }

        #region IOutputFile Members

        public IList<byte> GetText()
        {
            return ehdr.text.data;
        }

        public IList<byte> GetData()
        {
            return ehdr.data.data;
        }

        public IList<byte> GetRodata()
        {
            return ehdr.rodata.data;
        }

        public void AddTextSymbol(int offset, string name, bool local_only, bool is_func, bool is_weak)
        {
            byte st_info = 0;
            ulong st_size = 8;
            if (is_func)
                st_info |= Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_FUNC;
            else
            {
                st_info |= Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_NOTYPE;
                st_size = 0;
            }

            if (local_only)
                st_info |= Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_LOCAL;
            else if (is_weak)
                st_info |= Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_WEAK;
            else
                st_info |= Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_GLOBAL;

            ehdr.e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = name,
                st_name = ehdr.e_symstr.GetOffset(name),
                st_info = st_info,
                st_shndx = ehdr.text.index,
                st_value = Convert.ToUInt64(offset),
                st_size = st_size,
                st_other = 0
            });
            ehdr.e_syms.name_to_sym.Add(name, ehdr.e_syms.defined_syms.Count - 1);
        }

        public void AddDataSymbol(int offset, string name)
        {
            ehdr.e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = name,
                st_name = ehdr.e_symstr.GetOffset(name),
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_GLOBAL |
                Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_OBJECT,
                st_shndx = ehdr.data.index,
                st_value = Convert.ToUInt64(offset),
                st_size = 8,
                st_other = 0
            });
            ehdr.e_syms.name_to_sym.Add(name, ehdr.e_syms.defined_syms.Count - 1);
        }

        public void AddRodataSymbol(int offset, string name)
        {
            ehdr.e_syms.defined_syms.Add(new Elf64_Symbol_Shdr.Elf64_Sym
            {
                name = name,
                st_name = ehdr.e_symstr.GetOffset(name),
                st_info = Elf64_Symbol_Shdr.Elf64_Sym.BindingFlags.STB_GLOBAL |
                Elf64_Symbol_Shdr.Elf64_Sym.SymbolTypes.STT_OBJECT,
                st_shndx = ehdr.rodata.index,
                st_value = Convert.ToUInt64(offset),
                st_size = 8,
                st_other = 0
            });
            ehdr.e_syms.name_to_sym.Add(name, ehdr.e_syms.defined_syms.Count - 1);
        }

        public void AddTextRelocation(int offset, string name, uint rel_type, int value)
        {
            ehdr.relatext.relocs.Add(new Elf64_Rela_Shdr.Elf64_Rela((ulong)offset, rel_type, value, name));
        }

        public void AddDataRelocation(int offset, string name, uint rel_type, int value)
        {
            ehdr.reladata.relocs.Add(new Elf64_Rela_Shdr.Elf64_Rela((ulong)offset, rel_type, value, name));
        }

        public void AddRodataRelocation(int offset, string name, uint rel_type, int value)
        {
            ehdr.relarodata.relocs.Add(new Elf64_Rela_Shdr.Elf64_Rela((ulong)offset, rel_type, value, name));
        }

        public void Write(Stream output)
        {
            WriteStaticFieldsPointers();
            WriteObjectFile(output);
        }

        private void WriteStaticFieldsPointers()
        {
            string[] s_files = s_filename.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            string s_file = string.Join("_", s_files);

            AddRodataSymbol(GetRodata().Count, "_static_fields_" + libtysila.Mangler2.EncodeString(s_file));
            foreach (static_fields_pointer sp in static_fields)
            {
                AddRodataRelocation(GetRodata().Count, sp.static_object, ass.DataToDataRelocType(), 0);
                for (int i = 0; i < ass.GetSizeOfPointer(); i++)
                    GetRodata().Add(0);
                AddDataRelocation(GetRodata().Count, sp.typeinfo_name, ass.DataToDataRelocType(), 0);
                for (int i = 0; i < ass.GetSizeOfPointer(); i++)
                    GetRodata().Add(0);
                
            }
            for (int i = 0; i < (2 * ass.GetSizeOfPointer()); i++)
                GetRodata().Add(0);
        }

        public void DumpText(TextWriter output) { }

        #endregion
    }
}
