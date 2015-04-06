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
using libtysila;

namespace Elf32
{
    internal class ElfWriter : libtysila.IOutputFile
    {
        Elf32_Ehdr ehdr;
        string e_point;
        string s_filename;
        libtysila.Assembler ass;

        internal enum ArchType { i586, ARM };
        ArchType at;

        class static_fields_pointer { public string static_object; public string typeinfo_name; }
        List<static_fields_pointer> static_fields = new List<static_fields_pointer>();

        public void SetEntryPoint(string entry_point)
        { e_point = entry_point; }

        public ElfWriter(string source_filename, string comment, libtysila.Assembler assembler)
        {
            ass = assembler;
            s_filename = source_filename;

            if (ass.Arch.InstructionSet == "i586")
                at = ArchType.i586;
            else if (ass.Arch.InstructionSet == "arm")
                at = ArchType.ARM;
            else
                throw new Exception("Unknown architecture: " + ass.Arch.InstructionSet);
            
            ehdr = new Elf32_Ehdr(source_filename, comment, at);
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
                Elf32_Symbol_Shdr.Elf32_Sym entry_sym = null;
                Elf32_Symbol_Shdr.Elf32_Sym epoint_sym = null;
                foreach (Elf32_Symbol_Shdr.Elf32_Sym sym in ehdr.e_syms.defined_syms)
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
            uint cur_offset;

            if (ehdr.phdrs.Count > 0)
            {
                ehdr.e_phoff = (uint)Align(Elf32_Ehdr.GetLength(), 4);
                ehdr.e_phentsize = (ushort)Elf32_Phdr.GetLength();
                cur_offset = ehdr.e_phoff + (uint)ehdr.e_phnum * (uint)ehdr.e_phentsize;}
            else
            {
                ehdr.e_phoff = 0;
                ehdr.e_phentsize = 0;
                cur_offset = (uint)Align(Elf32_Ehdr.GetLength(), 4);
            }            

            ehdr.GenerateRelocationSymbols();

            foreach (Elf32_Shdr shdr in ehdr.shdrs)
            {
                if (shdr.sh_type != SectionType.SHT_NULL)
                {
                    shdr.sh_offset = Align(cur_offset, 4);
                    cur_offset = shdr.sh_offset + shdr.sh_size;
                }
            }

            ehdr.e_shoff = Align(cur_offset, 4);

            // Now write the actual elf header table
            output.Seek(0, SeekOrigin.Begin);
            ehdr.Write(output);

            // Write program header table
            output.Seek((long)ehdr.e_phoff, SeekOrigin.Begin);
            foreach (Elf32_Phdr phdr in ehdr.phdrs)
                phdr.Write(output);

            // Write actual section data
            foreach (Elf32_Shdr shdr in ehdr.shdrs)
            {
                output.Seek((long)shdr.sh_offset, SeekOrigin.Begin);
                Write(output, shdr.data);
            }

            // Write section header data
            output.Seek((long)ehdr.e_shoff, SeekOrigin.Begin);
            foreach (Elf32_Shdr shdr in ehdr.shdrs)
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

        private uint Align(uint v, uint align)
        {
            uint extra = v % align;
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

        public static void Write(object s, Int32 v)
        {
            if (v >= 0)
                Write(s, (UInt32)v);
            else
                Write(s, (~((UInt32)(-v))) + 1);
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

        public ISymbol AddTextSymbol(int offset, string name, bool local_only, bool is_func, bool is_weak)
        {
            uint st_size = 4;
            byte st_info = 0;
            if (is_func)
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.SymbolTypes.STT_FUNC;
            else
            {
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.SymbolTypes.STT_NOTYPE;
                st_size = 0;
            }

            if (local_only)
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_LOCAL;
            else if (is_weak)
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_WEAK;
            else
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_GLOBAL;

            Elf32_Symbol_Shdr.Elf32_Sym ret = new Elf32_Symbol_Shdr.Elf32_Sym
            {
                name = name,
                st_name = ehdr.e_symstr.GetOffset(name),
                st_info = st_info,
                st_shndx = ehdr.text.index,
                st_value = Convert.ToUInt32(offset),
                st_size = st_size,
                st_other = 0
            };
            ehdr.e_syms.defined_syms.Add(ret);
            if(!local_only)
                ehdr.e_syms.name_to_sym.Add(name, ehdr.e_syms.defined_syms.Count - 1);
            return ret;
        }

        public ISymbol AddDataSymbol(int offset, string name, bool is_weak)
        {
            byte st_info = 0;
            if (is_weak)
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_WEAK;
            else
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_GLOBAL;

            Elf32_Symbol_Shdr.Elf32_Sym ret = new Elf32_Symbol_Shdr.Elf32_Sym
            {
                name = name,
                st_name = ehdr.e_symstr.GetOffset(name),
                st_info = (byte)(st_info | Elf32_Symbol_Shdr.Elf32_Sym.SymbolTypes.STT_OBJECT),
                st_shndx = ehdr.data.index,
                st_value = Convert.ToUInt32(offset),
                st_size = 4,
                st_other = 0
            };
            ehdr.e_syms.defined_syms.Add(ret);
            ehdr.e_syms.name_to_sym.Add(name, ehdr.e_syms.defined_syms.Count - 1);
            return ret;
        }

        public ISymbol AddRodataSymbol(int offset, string name, bool is_weak)
        {
            byte st_info = 0;
            if (is_weak)
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_WEAK;
            else
                st_info |= Elf32_Symbol_Shdr.Elf32_Sym.BindingFlags.STB_GLOBAL;

            Elf32_Symbol_Shdr.Elf32_Sym ret = new Elf32_Symbol_Shdr.Elf32_Sym
            {
                name = name,
                st_name = ehdr.e_symstr.GetOffset(name),
                st_info = (byte)(st_info | Elf32_Symbol_Shdr.Elf32_Sym.SymbolTypes.STT_OBJECT),
                st_shndx = ehdr.rodata.index,
                st_value = Convert.ToUInt32(offset),
                st_size = 4,
                st_other = 0
            };
            ehdr.e_syms.defined_syms.Add(ret);
            ehdr.e_syms.name_to_sym.Add(name, ehdr.e_syms.defined_syms.Count - 1);
            return ret;
        }

        public void AddTextRelocation(int offset, string name, uint rel_type, long value)
        {
            ehdr.relatext.relocs.Add(new Elf32_Rela_Shdr.Elf32_Rela((uint)offset, rel_type, (int)value, name));
        }

        public void AddDataRelocation(int offset, string name, uint rel_type, long value)
        {
            ehdr.reladata.relocs.Add(new Elf32_Rela_Shdr.Elf32_Rela((uint)offset, rel_type, (int)value, name));
        }

        public void AddRodataRelocation(int offset, string name, uint rel_type, long value)
        {
            ehdr.relarodata.relocs.Add(new Elf32_Rela_Shdr.Elf32_Rela((uint)offset, rel_type, (int)value, name));
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

            AddRodataSymbol(GetRodata().Count, "_static_fields_" + libtysila.Mangler2.EncodeString(s_file), false);
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


        public void AddBssBytes(int count)
        {
            throw new NotImplementedException();
        }

        public void AlignBss(int a)
        {
            throw new NotImplementedException();
        }

        public ISymbol AddBssSymbol(int offset, string name, bool is_weak)
        {
            throw new NotImplementedException();
        }

        public void AddBssRelocation(int offset, string name, uint rel_type, long value)
        {
            throw new NotImplementedException();
        }


        public int GetBssOffset()
        {
            throw new NotImplementedException();
        }
    }
}
