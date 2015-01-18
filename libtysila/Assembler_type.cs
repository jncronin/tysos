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
    partial class Assembler
    {
        TypeToCompile? tysos_type_ttc = null;
        Layout tysos_type_l = null;
        string tysos_type_ti = null;
        internal Dictionary<string, int> tysos_type_offsets = null;

        public Layout GetTysosTypeLayout()
        {
            if(tysos_type_l == null)
            {
                tysos_type_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosType", this);
                tysos_type_l = Layout.GetLayout(tysos_type_ttc.Value, this, false);
                tysos_type_ti = Mangler2.MangleTypeInfo(tysos_type_ttc.Value, this);

                tysos_type_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_type_l.InstanceFields)
                    {
                        if (!tysos_type_offsets.ContainsKey(f.name))
                            tysos_type_offsets.Add(f.name, f.offset);
                    }
                }
            }

            return tysos_type_l;
        }

        TypeToCompile? tysos_gt_ttc = null;
        Layout tysos_gt_l = null;
        string tysos_gt_ti = null;
        Dictionary<string, int> tysos_gt_offsets = null;

        public Layout GetTysosGenericTypeLayout()
        {
            if (tysos_gt_l == null)
            {
                tysos_gt_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosGenericType", this);
                tysos_gt_l = Layout.GetLayout(tysos_gt_ttc.Value, this, false);
                tysos_gt_ti = Mangler2.MangleTypeInfo(tysos_gt_ttc.Value, this);

                tysos_gt_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_gt_l.InstanceFields)
                    {
                        if (!tysos_gt_offsets.ContainsKey(f.name))
                            tysos_gt_offsets.Add(f.name, f.offset);
                    }
                }
            }

            return tysos_gt_l;
        }

        TypeToCompile? tysos_gtd_ttc = null;
        Layout tysos_gtd_l = null;
        string tysos_gtd_ti = null;
        Dictionary<string, int> tysos_gtd_offsets = null;

        public Layout GetTysosGenericTypeDefinitionLayout()
        {
            if (tysos_gtd_l == null)
            {
                tysos_gtd_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosGenericTypeDefinition", this);
                tysos_gtd_l = Layout.GetLayout(tysos_gtd_ttc.Value, this, false);
                tysos_gtd_ti = Mangler2.MangleTypeInfo(tysos_gtd_ttc.Value, this);

                tysos_gtd_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_gtd_l.InstanceFields)
                    {
                        if (!tysos_gtd_offsets.ContainsKey(f.name))
                            tysos_gtd_offsets.Add(f.name, f.offset);
                    }
                }
            }

            return tysos_gtd_l;
        }

        TypeToCompile? tysos_at_ttc = null;
        Layout tysos_at_l = null;
        string tysos_at_ti = null;
        Dictionary<string, int> tysos_at_offsets = null;

        public Layout GetTysosArrayTypeLayout()
        {
            if (tysos_at_l == null)
            {
                tysos_at_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosArrayType", this);
                tysos_at_l = Layout.GetLayout(tysos_at_ttc.Value, this, false);
                tysos_at_ti = Mangler2.MangleTypeInfo(tysos_at_ttc.Value, this);

                tysos_at_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_at_l.InstanceFields)
                    {
                        if (!tysos_at_offsets.ContainsKey(f.name))
                            tysos_at_offsets.Add(f.name, f.offset);
                    }
                }
            }

            return tysos_at_l;
        }

#if MT
        public void AssembleType(object ttcr) { AssembleType(ttcr as TTCRequest); }
        public void AssembleType(TTCRequest ttcr) { AssembleType(ttcr.ttc, ttcr.of); }
#endif
        public void AssembleType(Assembler.TypeToCompile ttc, IOutputFile of) { AssembleType(ttc, of, false); }
        public void AssembleType(Assembler.TypeToCompile ttc, IOutputFile of, bool static_fields_only)
        {
            if (of == null)
            {
#if MT
                rtc--;
#endif
                return;
            }


            lock (of)
            {
                /* Write the static fields */
                Layout l = Layout.GetLayout(ttc, this, false);
                if (l.StaticFields.Count > 0)
                {
                    of.AlignData(GetSizeOfPointer());
                    of.AddDataSymbol(of.GetData().Count, l.static_object_name, ttc.tsig.Type.IsWeakLinkage);
                    for (int i = 0; i < l.StaticClassSize; i++)
                        of.GetData().Add(0);
                }

                if (!static_fields_only)
                {
                    l = Layout.GetTypeInfoLayout(ttc, this, true);
                    // Write typeinfo
                    WriteInfoStructure(l.TypeInfoLayout, of);
                }
            }

#if MT
            rtc--;
#endif
        }

        public void AssembleMethodInfo(Assembler.MethodToCompile mtc, IOutputFile of, bool do_meth_address)
        {
            if (of == null)
                return;

            IList<Layout.StructureLayout> l = Layout.GetMethodInfoLayout(mtc, this, do_meth_address);
            WriteInfoStructure(l, of);
        }

        internal void WriteInfoStructure(IList<Layout.StructureLayout> l, IOutputFile of)
        {
            // Get the maximum size of the structure
            int size = 0;
            foreach (Layout.StructureLayout sl in l)
            {
                if ((sl.Offset + sl.PredefinedLength) > size)
                    size = sl.Offset + sl.PredefinedLength;
            }

            // Create a buffer in memory for the structure
            byte[] buf = new byte[size];

            lock (of)
            {
                of.AlignData(GetSizeOfPointer());

                // Store the current offset in the data section
                int start_offset = of.GetData().Count;

                // Get the name of the info structure
                string info_name = "";
                if (l[0].Entries[0].Type != Layout.LayoutEntry.LayoutEntryType.Symbol)
                    throw new Exception("First entry of an info structure has to be a symbol!");
                else
                    info_name = l[0].Entries[0].SymbolName;

                // Write each entry to the buffer
                foreach (Layout.StructureLayout sl in l)
                {
                    foreach (Layout.LayoutEntry le in sl.Entries)
                    {
                        int le_offset = le.OffsetWithinStructure + sl.Offset;

                        switch (le.Type)
                        {
                            case Layout.LayoutEntry.LayoutEntryType.Symbol:
                                if (le.SymbolIsExternal)
                                    of.AddDataSymbol(start_offset + le_offset, le.SymbolName, le.SymbolIsWeak);
                                break;

                            case Layout.LayoutEntry.LayoutEntryType.Const:
                                SetByteArray(buf, le_offset, le.Const);
                                break;

                            case Layout.LayoutEntry.LayoutEntryType.InternalReference:
                                int offset = 0;

                                if (le.InternalReference.Id == Layout.ID_Signatures)
                                    offset = l[Layout.ID_Signatures + le.InternalReference.EntryNum].Offset;
                                else
                                    offset = l[le.InternalReference.Id].Offset + le.InternalReference.Offset;

                                of.AddDataRelocation(start_offset + le_offset, info_name, DataToDataRelocType(), offset);
                                break;

                            case Layout.LayoutEntry.LayoutEntryType.Relocation:
                                of.AddDataRelocation(start_offset + le_offset, le.Relocation.name, le.Relocation.rel_type, le.Relocation.value);
                                break;
                        }
                    }
                }

                // Now write the buffer to the output
                IList<byte> data = of.GetData();
                foreach (byte b in buf)
                    data.Add(b);
            }
        }

        private void WriteZeroIntPtr(IOutputFile of)
        {
            int count = this.GetSizeOf(new Signature.Param(BaseType_Type.I));
            for (int i = 0; i < count; i++)
                of.GetData().Add(0);
        }

        private void WriteZeroInt32(IOutputFile of)
        {
            int count = this.GetSizeOf(new Signature.Param(BaseType_Type.I4));
            for (int i = 0; i < count; i++)
                of.GetData().Add(0);
        }
    }
}
