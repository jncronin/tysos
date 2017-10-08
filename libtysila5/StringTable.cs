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
using libtysila5.target;
using metadata;

namespace libtysila5
{
    public class StringTable
    {
        public metadata.TypeSpec StringObject;
        public int string_obj_len = 0;
        public int length_offset = 0;

        public string GetStringTableName()
        {
            return Label;
        }

        Dictionary<object, binary_library.ISymbol> st_cache =
            new Dictionary<object, binary_library.ISymbol>(
                new GenericEqualityComparerRef<object>());

        public binary_library.ISymbol GetStringTableSymbol(binary_library.IBinaryFile of)
        {
            binary_library.ISymbol sym;
            if(st_cache.TryGetValue(of, out sym))
            {
                return sym;
            }
            else
            {
                sym = of.CreateSymbol();
                st_cache[of] = sym;
                return sym;
            }
        }

        public int GetSignatureAddress(IEnumerable<byte> sig, target.Target t)
        {
            var ptr_size = t.GetCTSize(ir.Opcode.ct_object);
            while (str_tab.Count % ptr_size != 0)
                str_tab.Add(0);

            int ret = str_tab.Count;

            foreach (byte b in sig)
                str_tab.Add(b);

            return ret;
        }

        public int GetSignatureAddress(metadata.TypeSpec.FullySpecSignature sig, target.Target t)
        {
            var ptr_size = t.GetCTSize(ir.Opcode.ct_object);
            while (str_tab.Count % ptr_size != 0)
                str_tab.Add(0);

            int ret = str_tab.Count;

            // first is type of signature
            str_tab.AddRange(t.IntPtrArray(BitConverter.GetBytes((int)sig.Type)));

            // then any extra data if necessary
            switch(sig.Type)
            {
                case metadata.Spec.FullySpecSignature.FSSType.Field:
                    // For fields with static data we insert it here
                    AddFieldSpecFields(sig.OriginalSpec as MethodSpec, str_tab, t);
                    break;

                case Spec.FullySpecSignature.FSSType.Type:
                    AddTypeSpecFields(sig.OriginalSpec as TypeSpec, str_tab, t);
                    break;
            }

            // then is length of module references
            str_tab.AddRange(t.IntPtrArray(BitConverter.GetBytes(sig.Modules.Count)));

            // then module references
            foreach(var mod in sig.Modules)
            {
                sig_metadata_addrs[str_tab.Count] = mod.AssemblyName;
                for (int i = 0; i < ptr_size; i++)
                    str_tab.Add(0);
            }

            // then signature
            str_tab.AddRange(sig.Signature);

            return ret;
        }

        private void AddTypeSpecFields(TypeSpec ts, List<byte> str_tab, Target t)
        {
            /* For types we add one special field:
             * 
             * If this is an enum, its a pointer to the vtable for the underlying type
             * If it is a zero-based array, its a pointer to the vtable for the element type
             * Else zero
             * 
             * Second special field is initialized to zero, and is used at runtime
             * to hold the pointer to the System.Type instance
             */

            if (ts.Unbox.IsEnum)
            {
                var ut = ts.Unbox.UnderlyingType;

                sig_metadata_addrs[str_tab.Count] = ut.MangleType();
            }
            else if(ts.stype == TypeSpec.SpecialType.SzArray)
            {
                sig_metadata_addrs[str_tab.Count] = ts.other.MangleType();
            }

            for (int i = 0; i < t.psize; i++)
                str_tab.Add(0);
            for (int i = 0; i < t.psize; i++)
                str_tab.Add(0);
        }

        private void AddFieldSpecFields(MethodSpec fs, List<byte> str_tab, target.Target t)
        {
            /* Field specs have two special fields:
             * 
             * IntPtr field_size
             * IntPtr field_data
             * 
             * where field_data may be null if no .data member is specified for the field
             */

            // read field signature to get the type of the field
            var sig_idx = fs.msig;
            sig_idx = fs.m.GetFieldSigTypeIndex(sig_idx);
            var ts = fs.m.GetTypeSpec(ref sig_idx, fs.gtparams, null);
            var fsize = t.GetSize(ts);

            str_tab.AddRange(t.IntPtrArray(BitConverter.GetBytes(fsize)));

            // now determine if the field has an rva associated with it
            int rva_id = 0;
            for(int i = 1; i <= fs.m.table_rows[MetadataStream.tid_FieldRVA]; i++)
            {
                var field_idx = fs.m.GetIntEntry(MetadataStream.tid_FieldRVA, i, 1);
                if(field_idx == fs.mdrow)
                {
                    rva_id = i;
                    break;
                }
            }

            // rva id
            if(rva_id != 0)
            {
                // TODO checks in CIL II 22.18
                var rva = fs.m.GetIntEntry(MetadataStream.tid_FieldRVA, rva_id, 0);
                var offset = fs.m.ResolveRVA(rva);
                sig_metadata_addrs[str_tab.Count] = fs.m.AssemblyName;
                sig_metadata_addends[str_tab.Count] = offset;
            }
            for (int i = 0; i < t.psize; i++)
                str_tab.Add(0);
        }

        public int GetStringAddress(string s, target.Target t)
        {
            if (str_addrs.ContainsKey(s))
                return str_addrs[s];
            else
            {
                var ptr_size = t.GetCTSize(ir.Opcode.ct_object);
                while (str_tab.Count % ptr_size != 0)
                    str_tab.Add(0);

                int ret = str_tab.Count;
                str_addrs.Add(s, ret);

                //int string_obj_len = layout.Layout.GetTypeSize(StringObject,
                //    t, false);
                //int string_type_offset = 

                int i = 0;
                for (; i < length_offset; i++)
                    str_tab.Add(0);
                int len = s.Length;
                str_tab.Add((byte)(len & 0xff));
                str_tab.Add((byte)((len >> 8) & 0xff));
                str_tab.Add((byte)((len >> 16) & 0xff));
                str_tab.Add((byte)((len >> 24) & 0xff));
                i += 4;
                for (; i < string_obj_len; i++)
                    str_tab.Add(0);

                foreach (char c in s)
                {
                    str_tab.Add((byte)(c & 0xff));
                    str_tab.Add((byte)((c >> 8) & 0xff));
                }
                return ret;
            }
        }

        Dictionary<int, string> sig_metadata_addrs =
            new Dictionary<int, string>(
                new GenericEqualityComparer<int>());
        Dictionary<int, long> sig_metadata_addends =
            new Dictionary<int, long>(
                new GenericEqualityComparer<int>());

        Dictionary<string, int> str_addrs =
            new Dictionary<string, int>(
                new GenericEqualityComparer<string>());

        List<byte> str_tab = new List<byte>();

        private string Label;

        public StringTable(string mod_name, libtysila.AssemblyLoader al,
            target.Target t)
        {
            Label = mod_name + "_StringTable";

            var corlib = al.GetAssembly("mscorlib");
            StringObject = corlib.GetTypeSpec("System", "String");
            var fs_len = corlib.GetFieldDefRow("length", StringObject);
            length_offset = layout.Layout.GetFieldOffset(StringObject, fs_len,
                t);
            var fs_start = corlib.GetFieldDefRow("start_char", StringObject);
            string_obj_len = layout.Layout.GetFieldOffset(StringObject, fs_start,
                t);
        }

        public void WriteToOutput(binary_library.IBinaryFile of,
            metadata.MetadataStream ms, target.Target t)
        {
            var rd = of.GetRDataSection();
            rd.Align(t.GetCTSize(ir.Opcode.ct_object));

            var stab_lab = GetStringTableSymbol(of);
            stab_lab.DefinedIn = rd;
            stab_lab.Name = Label;
            stab_lab.ObjectType = binary_library.SymbolObjectType.Object;
            stab_lab.Offset = (ulong)rd.Data.Count;
            stab_lab.Type = binary_library.SymbolType.Global;
            rd.AddSymbol(stab_lab);

            int stab_base = rd.Data.Count;

            foreach (byte b in str_tab)
                rd.Data.Add(b);

            var str_lab = of.CreateSymbol();
            str_lab.DefinedIn = null;
            str_lab.Name = StringObject.m.MangleType(StringObject);
            str_lab.ObjectType = binary_library.SymbolObjectType.Object;

            foreach(var str_addr in str_addrs.Values)
            {
                var reloc = of.CreateRelocation();
                reloc.DefinedIn = rd;
                reloc.Type = t.GetDataToDataReloc();
                reloc.Addend = 0;
                reloc.References = str_lab;
                reloc.Offset = (ulong)(str_addr + stab_base);
                of.AddRelocation(reloc);
            }

            foreach(var kvp in sig_metadata_addrs)
            {
                var reloc = of.CreateRelocation();
                reloc.DefinedIn = rd;
                reloc.Type = t.GetDataToDataReloc();
                reloc.Addend = 0;

                if (sig_metadata_addends.ContainsKey(kvp.Key))
                    reloc.Addend = sig_metadata_addends[kvp.Key];

                var md_lab = of.CreateSymbol();
                md_lab.DefinedIn = null;
                md_lab.Name = kvp.Value;
                md_lab.ObjectType = binary_library.SymbolObjectType.Object;

                reloc.References = md_lab;
                reloc.Offset = (ulong)(kvp.Key + stab_base);
                of.AddRelocation(reloc);
            }

            stab_lab.Size = rd.Data.Count - (int)stab_lab.Offset;
        }
    }
}
