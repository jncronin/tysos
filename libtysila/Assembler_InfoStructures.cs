/* Copyright (C) 2011 by John Cronin
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
        TypeToCompile? tysos_field_ttc = null;
        Layout tysos_field_l = null;
        string tysos_field_ti = null;
        Dictionary<string, int> tysos_field_offsets = null;

        TypeToCompile? tysos_method_ttc = null;
        Layout tysos_method_l = null;
        string tysos_method_ti = null;
        Dictionary<string, int> tysos_method_offsets = null;

        TypeToCompile? tysos_ehclause_ttc = null;
        Layout tysos_ehclause_l = null;
        string tysos_ehclause_ti = null;
        Dictionary<string, int> tysos_ehclause_offsets = null;

        TypeToCompile? tysos_assembly_ttc = null;
        Layout tysos_assembly_l = null;
        string tysos_assembly_ti = null;
        Dictionary<string, int> tysos_assembly_offsets = null;

        TypeToCompile? tysos_module_ttc = null;
        Layout tysos_module_l = null;
        string tysos_module_ti = null;
        Dictionary<string, int> tysos_module_offsets = null;

        public Layout GetTysosAssemblyLayout()
        {
            if (tysos_assembly_l == null)
            {
                tysos_assembly_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosAssembly", this);
                tysos_assembly_l = Layout.GetLayout(tysos_assembly_ttc.Value, this);
                tysos_assembly_ti = Mangler2.MangleTypeInfo(tysos_assembly_ttc.Value, this);

                tysos_assembly_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_assembly_l.InstanceFields)
                    {
                        if (!tysos_assembly_offsets.ContainsKey(f.name))
                            tysos_assembly_offsets.Add(f.name, f.offset);
                    }
                }

                tysos_module_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosModule", this);
                tysos_module_l = Layout.GetLayout(tysos_module_ttc.Value, this);
                tysos_module_ti = Mangler2.MangleTypeInfo(tysos_module_ttc.Value, this);

                tysos_module_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_module_l.InstanceFields)
                    {
                        if (!tysos_module_offsets.ContainsKey(f.name))
                            tysos_module_offsets.Add(f.name, f.offset);
                    }
                }
            }

            return tysos_assembly_l;
        }

        public Layout GetTysosModuleLayout()
        {
            GetTysosAssemblyLayout();
            return tysos_module_l;
        }

        public Layout GetTysosFieldLayout()
        {
            if (tysos_field_l == null)
            {
                tysos_field_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosField", this);
                tysos_field_l = Layout.GetLayout(tysos_field_ttc.Value, this);
                tysos_field_ti = Mangler2.MangleTypeInfo(tysos_field_ttc.Value, this);

                tysos_field_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_field_l.InstanceFields)
                    {
                        if (!tysos_field_offsets.ContainsKey(f.name))
                            tysos_field_offsets.Add(f.name, f.offset);
                    }
                }
            }

            return tysos_field_l;
        }

        public Layout GetEHClausesLayout()
        {
            if (tysos_ehclause_l == null)
                GetTysosMethodLayout();
            return tysos_ehclause_l;
        }

        public Layout GetTysosMethodLayout()
        {
            if (tysos_method_l == null)
            {
                tysos_method_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosMethod", this);
                tysos_method_l = Layout.GetLayout(tysos_method_ttc.Value, this);
                tysos_method_ti = Mangler2.MangleTypeInfo(tysos_method_ttc.Value, this);

                tysos_method_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_method_l.InstanceFields)
                    {
                        if (!tysos_method_offsets.ContainsKey(f.name))
                            tysos_method_offsets.Add(f.name, f.offset);
                    }
                }


                tysos_ehclause_ttc = Metadata.GetTTC("libsupcs", "libsupcs", "TysosMethod+EHClause", this);
                tysos_ehclause_l = Layout.GetLayout(tysos_ehclause_ttc.Value, this);
                tysos_ehclause_ti = Mangler2.MangleTypeInfo(tysos_ehclause_ttc.Value, this);

                tysos_ehclause_offsets = new Dictionary<string, int>();
                {
                    foreach (Layout.Field f in tysos_ehclause_l.InstanceFields)
                    {
                        if (!tysos_ehclause_offsets.ContainsKey(f.name))
                            tysos_ehclause_offsets.Add(f.name, f.offset);
                    }
                }

            }

            return tysos_method_l;
        }

        public void AssembleAssemblyInfo(Metadata m, IOutputFile of)
        {
            GetTysosAssemblyLayout();
            
            if (of == null)
                return;

            lock (of)
            {
                of.AlignData(GetSizeOfPointer());
                IList<byte> data_sect = of.GetData();
                int ai_top = data_sect.Count;

                string mangled_assembly_name = Mangler2.MangleAssembly(m, this);
                of.AddDataSymbol(ai_top, mangled_assembly_name);

                // Create the field info struct in memory
                byte[] ai = new byte[tysos_assembly_l.ClassSize];

                // __vtbl pointer
                of.AddDataRelocation(ai_top + tysos_assembly_offsets["IntPtr __vtbl"], "__tysos_assembly_vt", DataToDataRelocType(), 0);

                // __obj_id
                SetByteArray(ai, tysos_assembly_offsets["Int32 __object_id"], ToByteArraySignExtend(next_object_id.Increment, 4));

                // String assemblyName
                vara v_n = m.StringTable.GetStringAddress(m.ModuleName, this);
                of.AddDataRelocation(ai_top + tysos_assembly_offsets["String assemblyName"], v_n.LabelVal, DataToDataRelocType(), v_n.Offset);

                // IntPtr _Types
                of.AddDataRelocation(ai_top + tysos_assembly_offsets["IntPtr _Types"], mangled_assembly_name, DataToDataRelocType(), ai.Length);

                Layout.append_byte_array(data_sect, ai);

                // Add the list of types
                // for now, we don't include generic types as they will by definition be uninitiated and this may cause errors compiling all the methods in them
                List<string> type_names = new List<string>();
                foreach (Metadata.TypeDefRow tdr in m.Tables[(int)Metadata.TableId.TypeDef])
                {
                    if (!tdr.IsGeneric && !tdr.TypeName.StartsWith("<PrivateImplementationDetails>"))
                    {
                        Assembler.TypeToCompile ttc = Metadata.GetTTC(new Metadata.TableIndex(tdr), new Assembler.TypeToCompile(), this);
                        type_names.Add(Mangler2.MangleTypeInfo(ttc, this));
                        Requestor.RequestTypeInfo(ttc);
                    }
                }

                foreach (string type_name in type_names)
                {
                    of.AddDataRelocation(of.GetData().Count, type_name, DataToDataRelocType(), 0);
                    WriteZeroIntPtr(of);
                }

                // null terminate type list
                WriteZeroIntPtr(of);
            }
        }

        public void AssembleModuleInfo(Metadata m, IOutputFile of)
        {
            GetTysosAssemblyLayout();

            if (of == null)
                return;

            lock (of)
            {
                of.AlignData(GetSizeOfPointer());
                IList<byte> data_sect = of.GetData();
                int mdi_top = data_sect.Count;

                of.AddDataSymbol(mdi_top, Mangler2.MangleModule(m, this));

                // Create the field info struct in memory
                byte[] mdi = new byte[tysos_module_l.ClassSize];

                // __vtbl pointer
                of.AddDataRelocation(mdi_top + tysos_module_offsets["IntPtr __vtbl"], "__tysos_module_vt", DataToDataRelocType(), 0);

                // __obj_id
                SetByteArray(mdi, tysos_module_offsets["Int32 __object_id"], ToByteArraySignExtend(next_object_id.Increment, 4));

                // String name
                vara v_n = m.StringTable.GetStringAddress(m.ModuleName, this);
                of.AddDataRelocation(mdi_top + tysos_module_offsets["String name"], v_n.LabelVal, DataToDataRelocType(), v_n.Offset);

                // System.Reflection.Assembly assembly
                of.AddDataRelocation(mdi_top + tysos_module_offsets["System.Reflection.Assembly assembly"], Mangler2.MangleAssembly(m, this), DataToDataRelocType(), 0);
                Requestor.RequestAssembly(m);

                // long compile_time
                SetByteArray(mdi, tysos_module_offsets["Int64 compile_time"], ToByteArraySignExtend(DateTime.Now.Ticks, 8));

                Layout.append_byte_array(data_sect, mdi);
            }
        }

        internal int get_compiled_offset(int il_offset, AssembleBlockOutput abo)
        {
            if (abo.instrs.ContainsKey(il_offset))
                return abo.instrs[il_offset].compiled_offset;
            else
                return abo.compiled_code_length;
        }
    }
}
