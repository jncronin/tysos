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

namespace libtysila
{
    public class StringTable
    {
        public vara GetStringAddress(string s, Assembler ass)
        {
            return vara.Label(Label, _GetStringAddress(s, ass), true);
        }

        private int _GetStringAddress(string s, Assembler ass)
        {
            if (str_addrs.ContainsKey(s))
                return str_addrs[s];
            else
            {
                int ret = str_tab.Count;
                str_addrs.Add(s, ret);

                // build the System.String object
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef("mscorlib", "System", "String", ass);
                Layout l = Layout.GetLayout(new Assembler.TypeToCompile { _ass = ass, type = tdr, tsig = new Signature.Param(BaseType_Type.String) }, ass, false);
                byte[] sso = new byte[ass.GetStringFieldOffset(Assembler.StringFields.data_offset)];
                int l_offset = l.GetField("Int32 length", false).offset;
                ass.SetByteArray(sso, l_offset, s.Length);
                int objid_offset = l.GetField("Int32 __object_id", false).offset;
                ass.SetByteArray(sso, objid_offset, ass.next_object_id.Increment);

                foreach (byte b in sso)
                    str_tab.Add(b);

                foreach (char c in s)
                {
                    str_tab.Add((byte)(c & 0xff));
                    str_tab.Add((byte)((c >> 8) & 0xff));
                }
                return ret;
            }
        }

        Dictionary<string, int> str_addrs = new Dictionary<string, int>(new libtysila.GenericEqualityComparer<string>());

        List<byte> str_tab = new List<byte>();

        private string Label;

        public StringTable(string mod_name)
        { Label = mod_name; }

        public void WriteToOutput(IOutputFile of, Assembler ass)
        {
            lock (of)
            {
                int str_tab_start = of.GetRodata().Count;

                of.AlignRodata(ass.GetSizeOfPointer());
                of.AddRodataSymbol(of.GetRodata().Count, Label);
                foreach (byte b in str_tab)
                    of.GetRodata().Add(b);

                if (ass._options.EnableRTTI)
                {
                    /* Patch in the relocations to the System.String VTables */

                    Assembler.TypeToCompile str_ttc = new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(BaseType_Type.String), type = Metadata.GetTypeDef(new Signature.BaseType(BaseType_Type.String), ass) };
                    Layout l = Layout.GetTypeInfoLayout(str_ttc, ass, false, false);
                    string str_ti = l.typeinfo_object_name;
                    int vtbl_offset = l.FixedLayout[Layout.ID_VTableStructure].Offset;

                    foreach (KeyValuePair<string, int> kvp in str_addrs)
                        of.AddRodataRelocation(str_tab_start + kvp.Value + ass.GetStringFieldOffset(Assembler.StringFields.vtbl), ass.GetStringTypeInfoObjectName(), ass.DataToDataRelocType(), vtbl_offset);
                }
            }
        }
    }
}
