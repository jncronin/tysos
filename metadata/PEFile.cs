/* Copyright (C) 2016 by John Cronin
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

namespace metadata
{
    public class PEFile
    {
        PE_File_Header pefh;
        Cli_Header clih;

        private class DataDir
        {
            public long RVA;
            public long Size;
        }

        private class SectionHeader
        {
            public String Name;
            public UInt32 VSize;
            public UInt32 VAddress;
            public UInt32 PSize;
            public UInt32 PAddress;
            public UInt32 Chars;
        }

        private class PE_File_Header
        {
            public UInt16 NumberOfSections;
            public DateTime TimeDateStamp;
            public UInt16 OptHeaderSize;
            public UInt16 Chars;

            public DataDir CliHeader;

            public SectionHeader[] Sections;
        }

        private class Cli_Header
        {
            public UInt32 EntryPointToken;
            public DataDir Metadata = new DataDir();
        }

        public class StreamHeader
        {
            public UInt32 Offset;
            public long FileOffset;
            public UInt32 Size;
            public String Name;
            public DataInterface di;
        }

        public MetadataStream Parse(System.IO.Stream file, long offset, long length, AssemblyLoader al)
        {
            // Parse into an ArrayInterface

            byte[] b = new byte[length];
            file.Seek(offset, SeekOrigin.Begin);
            file.Read(b, 0, (int)length);

            ArrayInterface ai = new ArrayInterface(b);
            return Parse(ai, al);
        }

        public MetadataStream Parse(System.IO.Stream file, AssemblyLoader al)
        {
            // Interpret from here to end
            long curpos = file.Position;
            file.Seek(0, SeekOrigin.End);
            long endpos = file.Position;
            file.Seek(curpos, SeekOrigin.Begin);

            return Parse(file, curpos, endpos - curpos, al);
        }

        public MetadataStream Parse(DataInterface file, AssemblyLoader al)
        {
            var m = new MetadataStream();
            m.al = al;
            pefh = new PE_File_Header();

            uint pefh_start = file.ReadUInt(0x3c) + 4;
            pefh.NumberOfSections = file.ReadUShort((int)pefh_start + 2);
            pefh.Sections = new SectionHeader[pefh.NumberOfSections];
            TimeSpan t = new TimeSpan(0, 0, (int)file.ReadUInt((int)pefh_start + 4));
            pefh.TimeDateStamp = new DateTime(1970, 1, 1) + t;
            pefh.OptHeaderSize = file.ReadUShort((int)pefh_start + 16);
            if (pefh.OptHeaderSize < 224)
                throw new Exception("PE optional header too small");
            pefh.Chars = file.ReadUShort((int)pefh_start + 18);
            if ((pefh.Chars & 0x3) != 0x2)
            {
                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: Invalid PE file header characteristics: " + pefh.Chars.ToString());
                System.Diagnostics.Debugger.Break();
                throw new Exception("Invalid PE file header characteristics");
            }
            pefh.CliHeader = new DataDir();
            pefh.CliHeader.RVA = file.ReadUInt((int)pefh_start + 228);
            pefh.CliHeader.Size = file.ReadUInt((int)pefh_start + 232);

            // Read the section headers
            uint sections_start = pefh_start + 20 + pefh.OptHeaderSize;
            for (uint i = 0; i < pefh.NumberOfSections; i++)
            {
                uint s_start = sections_start + i * 40;
                pefh.Sections[i] = new SectionHeader();

                char[] w_str = new char[9];
                for (int j = 0; j < 8; j++)
                    w_str[j] = (char)file.ReadByte((int)s_start + j);
                w_str[8] = '\0';

                pefh.Sections[i].Name = new String(w_str);
                pefh.Sections[i].Name = pefh.Sections[i].Name.Remove(pefh.Sections[i].Name.IndexOf("\0"));
                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: section name: " + pefh.Sections[i].Name + "\n");

                pefh.Sections[i].VSize = file.ReadUInt((int)s_start + 8);
                pefh.Sections[i].VAddress = file.ReadUInt((int)s_start + 12);
                pefh.Sections[i].PSize = file.ReadUInt((int)s_start + 16);
                pefh.Sections[i].PAddress = file.ReadUInt((int)s_start + 20);

                pefh.Sections[i].Chars = file.ReadUInt((int)s_start + 36);
            }

            // Read the Cli header
            long clih_offset = ResolveRVA(pefh.CliHeader.RVA);
            clih = new Cli_Header();
            clih.Metadata.RVA = file.ReadUInt((int)clih_offset + 8);
            clih.Metadata.Size = file.ReadUInt((int)clih_offset + 12);
            clih.EntryPointToken = file.ReadUInt((int)clih_offset + 20);

            m.entry_point_token = clih.EntryPointToken;

            System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: CLI header parsed");

            // First, read the metadata root
            long mroot_offset = ResolveRVA(clih.Metadata.RVA);
            uint sig = file.ReadUInt((int)mroot_offset);
            if (sig != 0x424A5342)
                throw new Exception("Invalid metadata root");
            uint vstr_len = file.ReadUInt((int)mroot_offset + 12);
            m.VersionString = ReadSZ(file, (int)mroot_offset + 16);
            ushort nstr = file.ReadUShort((int)mroot_offset + 16 +
                (int)vstr_len + 2);

            int cur_offset = (int)mroot_offset + 16 + (int)vstr_len + 4;
            // Now, read the stream headers
            for (ushort i = 0; i < nstr; i++)
            {
                StreamHeader sh = new StreamHeader();
                sh.Offset = file.ReadUInt(cur_offset);
                sh.FileOffset = ResolveRVA(clih.Metadata.RVA + sh.Offset);
                sh.Size = file.ReadUInt(cur_offset + 4);

                cur_offset += 8;
                StringBuilder sb = new StringBuilder();
                while(true)
                {
                    byte strb = file.ReadByte(cur_offset++);
                    if (strb == 0)
                        break;
                    else
                        sb.Append((char)strb);
                }
                while ((cur_offset & 0x3) != 0)
                    cur_offset++;

                sh.Name = sb.ToString();

                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: stream name: " + sh.Name);

                sh.di = file.Clone((int)sh.FileOffset);

                if (sh.Name == "#Strings")
                    m.sh_string = sh;
                else if (sh.Name == "#US")
                    m.sh_us = sh;
                else if (sh.Name == "#GUID")
                    m.sh_guid = sh;
                else if (sh.Name == "#Blob")
                    m.sh_blob = sh;
                else if (sh.Name == "#~")
                    m.sh_tables = sh;
                else
                {
                    System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: unknown table \"" + sh.Name + "\"");
                    throw new Exception("Unknown metadata table");
                }
            }

            // Parse tables
            if(m.sh_tables != null)
            {
                var di = m.sh_tables.di;
                var maj = di.ReadByte(4);
                var min = di.ReadByte(5);
                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: parsing tables");
                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: metadata table schema v" + maj.ToString() + "." + min.ToString());

                // Determine size of indices into the heaps
                var heapsizes = di.ReadByte(6);
                if ((heapsizes & 0x1) == 0x1)
                    m.wide_string = true;
                if ((heapsizes & 0x2) == 0x2)
                    m.wide_guid = true;
                if ((heapsizes & 0x4) == 0x4)
                    m.wide_blob = true;

                // Get list of valid tables
                var valid = di.ReadULong(8);
                int valid_count = 0;
                List<int> valid_tables = new List<int>();
                for(int i = 0; i < 64; i++)
                {
                    if (((valid >> i) & 0x1) == 0x1)
                    {
                        m.valid_tables[i] = true;
                        valid_count++;
                        valid_tables.Add(i);
                    }
                }

                // Get number of rows in each table
                int table_id = 0;
                foreach (var valid_table in valid_tables)
                    m.table_rows[valid_table] = (int)di.ReadUInt(24 + 4 * table_id++);

                // Interpret the schema of each table
                foreach (var valid_table in valid_tables)
                    InterpretSchema(valid_table, m);

                // Determine start offsets of each table
                int offset = 24 + 4 * valid_count;
                foreach(var valid_table in valid_tables)
                {
                    m.table_offsets[valid_table] = offset;
                    offset += m.table_rows[valid_table] * m.table_entry_size[valid_table];
                }
            }

            m.pef = this;
            m.file = file;

            /* Get this assembly name */
            if (m.table_rows[MetadataStream.tid_Assembly] == 1)
            {
                m.assemblyName = m.GetStringEntry(MetadataStream.tid_Assembly, 1, 7);

                // Handle dotnet coreclr mscorlib having a different name
                if (m.assemblyName == "System.Private.CoreLib")
                    m.assemblyName = "mscorlib";

                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: current assembly is " + m.assemblyName);
            }

            /* Load up all referenced assemblies */
            m.referenced_assemblies = new MetadataStream[m.table_rows[MetadataStream.tid_AssemblyRef]];
            for(int i = 1; i <= m.table_rows[MetadataStream.tid_AssemblyRef]; i++)
            {
                var ass_name = m.GetStringEntry(MetadataStream.tid_AssemblyRef, i, 6);
                var maj = (int)m.GetIntEntry(MetadataStream.tid_AssemblyRef, i, 0);
                var min = (int)m.GetIntEntry(MetadataStream.tid_AssemblyRef, i, 1);
                var build = (int)m.GetIntEntry(MetadataStream.tid_AssemblyRef, i, 2);
                var rev = (int)m.GetIntEntry(MetadataStream.tid_AssemblyRef, i, 3);

                System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: loading referenced assembly " + ass_name);

                if ((m.referenced_assemblies[i - 1] = al.GetAssembly(ass_name, maj, min, build, rev)) == null)
                    throw new Exception("Cannot load referenced assembly: " +
                        ass_name);
            }

            m.PatchMethodDefOwners();
            m.PatchFieldDefOwners();
            m.PatchFieldRVAs();
            m.PatchClassLayouts();
            m.PatchFieldConstants();
            m.PatchGTypes();
            if (m.assemblyName == "mscorlib")
            {
                m.is_corlib = true;
                m.PatchSimpleTypes();
            }
            m.PatchNestedTypes();
            m.PatchCustomAttrs();

            m.LoadBuiltinTypes();

            System.Diagnostics.Debugger.Log(0, "metadata", "PEFile.Parse: parsing complete");
            
            return m;
        }

        private void InterpretSchema(int table_id, MetadataStream m)
        {
            m.table_column_sizes[table_id] = new List<int>();
            m.table_column_offsets[table_id] = new List<int>();
            m.table_templates[table_id] = new List<MetadataStream.FieldTemplate>();
            switch(table_id)
            {
                case 0x20:
                    // Assembly
                    InterpretUInt(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    break;
                case 0x22:
                    // AssemblyOS
                    InterpretUInt(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretUInt(table_id, m);
                    break;
                case 0x21:
                    // AssemblyProcessor
                    InterpretUInt(table_id, m);
                    break;
                case 0x23:
                    // AssemblyRef
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x25:
                    // AssemblyRefOS
                    InterpretUInt(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.AssemblyRef]);
                    break;
                case 0x24:
                    // AssemblyRefProcessor
                    InterpretUInt(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.AssemblyRef]);
                    break;
                case 0x0f:
                    // ClassLayout
                    InterpretUShort(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    break;
                case 0x0b:
                    // Constant
                    InterpretUShort(table_id, m);
                    InterpretCodedIndex(table_id, m, m.HasConstant);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x0c:
                    // CustomAttribute
                    InterpretCodedIndex(table_id, m, m.HasCustomAttribute);
                    InterpretCodedIndex(table_id, m, m.CustomAttributeType);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x0e:
                    // DeclSecurity
                    InterpretUShort(table_id, m);
                    InterpretCodedIndex(table_id, m, m.HasDeclSecurity);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x12:
                    // EventMap
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.Event]);
                    break;
                case 0x14:
                    // Event
                    InterpretUShort(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretCodedIndex(table_id, m, m.TypeDefOrRef);
                    break;
                case 0x27:
                    // ExportedType
                    InterpretUInt(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretCodedIndex(table_id, m, m.Implementation);
                    break;
                case 0x04:
                    // Field
                    InterpretUShort(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x10:
                    // FieldLayout
                    InterpretUInt(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.Field]);
                    break;
                case 0x0d:
                    // FieldMarshal
                    InterpretCodedIndex(table_id, m, m.HasFieldMarshal);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x1d:
                    // FieldRVA
                    InterpretUInt(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.Field]);
                    break;
                case 0x26:
                    // File
                    InterpretUInt(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x2a:
                    // GenericParam
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretCodedIndex(table_id, m, m.TypeOrMethodDef);
                    InterpretStringIndex(table_id, m);
                    break;
                case 0x2c:
                    // GenericParamConstraint
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.GenericParam]);
                    InterpretCodedIndex(table_id, m, m.TypeDefOrRef);
                    break;
                case 0x1c:
                    // ImplMap
                    InterpretUShort(table_id, m);
                    InterpretCodedIndex(table_id, m, m.MemberForwarded);
                    InterpretStringIndex(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.ModuleRef]);
                    break;
                case 0x09:
                    // InterfaceImpl
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    InterpretCodedIndex(table_id, m, m.TypeDefOrRef);
                    break;
                case 0x28:
                    // ManifestResource
                    InterpretUInt(table_id, m);
                    InterpretUInt(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretCodedIndex(table_id, m, m.Implementation);
                    break;
                case 0x0a:
                    // MemberRef
                    InterpretCodedIndex(table_id, m, m.MemberRefParent);
                    InterpretStringIndex(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x06:
                    // MethodDef
                    InterpretUInt(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.Param]);
                    break;
                case 0x19:
                    // MethodImpl
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    InterpretCodedIndex(table_id, m, m.MethodDefOrRef);
                    InterpretCodedIndex(table_id, m, m.MethodDefOrRef);
                    break;
                case 0x18:
                    // MethodSemantics
                    InterpretUShort(table_id, m);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.MethodDef]);
                    InterpretCodedIndex(table_id, m, m.HasSemantics);
                    break;
                case 0x2b:
                    // MethodSpec
                    InterpretCodedIndex(table_id, m, m.MethodDefOrRef);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x00:
                    // Module
                    InterpretUShort(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretGuidIndex(table_id, m);
                    InterpretGuidIndex(table_id, m);
                    InterpretGuidIndex(table_id, m);
                    break;
                case 0x1a:
                    // ModuleRef
                    InterpretStringIndex(table_id, m);
                    break;
                case 0x29:
                    // NestedClass
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    break;
                case 0x08:
                    // Param
                    InterpretUShort(table_id, m);
                    InterpretUShort(table_id, m);
                    InterpretStringIndex(table_id, m);
                    break;
                case 0x17:
                    // Property
                    InterpretUShort(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x15:
                    // PropertyMap
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.TypeDef]);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.Property]);
                    break;
                case 0x11:
                    // StandAloneSig
                    InterpretBlobIndex(table_id, m);
                    break;
                case 0x02:
                    // TypeDef
                    InterpretUInt(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    InterpretCodedIndex(table_id, m, m.TypeDefOrRef);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.Field]);
                    InterpretSimpleIndex(table_id, m, m.TableIDs[MetadataStream.TableId.MethodDef]);
                    break;
                case 0x01:
                    // TypeRef
                    InterpretCodedIndex(table_id, m, m.ResolutionScope);
                    InterpretStringIndex(table_id, m);
                    InterpretStringIndex(table_id, m);
                    break;
                case 0x1b:
                    // TypeSpec
                    InterpretBlobIndex(table_id, m);
                    break;
                default:
                    throw new Exception("Unsupported metadata table type: " + table_id.ToString());
            }

            int row_size = 0;
            foreach(int sz in m.table_column_sizes[table_id])
            {
                m.table_column_offsets[table_id].Add(row_size);
                row_size += sz;
            }
            m.table_entry_size[table_id] = row_size;
        }

        private void InterpretCodedIndex(int table_id, MetadataStream m,
            MetadataStream.CodedIndexTemplate ci)
        {
            int maxindex = GetMaxIndex(m, ci.Members);

            if (maxindex >= (1 << (16 - ci.TagBits)))
                m.table_column_sizes[table_id].Add(4);
            else
                m.table_column_sizes[table_id].Add(2);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.CodedIndex,
                CodedTemplate = ci
            });
        }

        private int GetMaxIndex(MetadataStream m, int[] tables)
        {
            int max = 0;
            foreach(int i in tables)
            {
                if(i >= 0)
                {
                    if (m.table_rows[i] > max)
                        max = m.table_rows[i];
                }
            }
            return max;
        }

        private void InterpretSimpleIndex(int table_id, MetadataStream m, int v)
        {
            if(m.table_rows[v] < 65536)
                m.table_column_sizes[table_id].Add(2);
            else
                m.table_column_sizes[table_id].Add(4);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.SimpleIndex,
                SimpleIndex = v
            });
        }

        private void InterpretStringIndex(int table_id, MetadataStream m)
        {
            if (m.wide_string)
                m.table_column_sizes[table_id].Add(4);
            else
                m.table_column_sizes[table_id].Add(2);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.String
            });
        }

        private void InterpretBlobIndex(int table_id, MetadataStream m)
        {
            if(m.wide_blob)
                m.table_column_sizes[table_id].Add(4);
            else
                m.table_column_sizes[table_id].Add(2);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.Blob
            });
        }

        private void InterpretGuidIndex(int table_id, MetadataStream m)
        {
            if (m.wide_guid)
                m.table_column_sizes[table_id].Add(4);
            else
                m.table_column_sizes[table_id].Add(2);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.Guid
            });
        }

        private void InterpretUShort(int table_id, MetadataStream m)
        {
            m.table_column_sizes[table_id].Add(2);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.Int16
            });
        }

        private void InterpretUInt(int table_id, MetadataStream m)
        {
            m.table_column_sizes[table_id].Add(4);

            m.table_templates[table_id].Add(new MetadataStream.FieldTemplate
            {
                EntryType = MetadataStream.FieldTemplate.FieldType.Int32
            });
        }

        static string ReadSZ(Stream file)
        {
            byte b;
            StringBuilder s = new StringBuilder();

            while ((b = (byte)file.ReadByte()) != (byte)'\0')
            {
                s.Append((char)b);
            }

            return s.ToString();
        }

        static string ReadSZ(DataInterface file, int offset)
        {
            byte b;
            StringBuilder s = new StringBuilder();

            while ((b = file.ReadByte(offset++)) != 0)
                s.Append((char)b);

            return s.ToString();
        }
        static void Align32(Stream file)
        {
            long cur_pos = file.Position;
            long new_pos = (cur_pos + 3) & (~3);
            file.Seek(new_pos, SeekOrigin.Begin);
        }

        public long ResolveRVA(long RVA)
        {
            if (pefh.Sections == null)
                throw new Exception("Section table not initialized");
            for (uint i = 0; i < pefh.NumberOfSections; i++)
            {
                if (pefh.Sections[i] == null)
                    throw new Exception("Section not initialized");
                if (((pefh.Sections[i].Chars & 0x60) != 0) && ((UInt32)RVA >= pefh.Sections[i].VAddress) &&
                    ((uint)RVA < (pefh.Sections[i].VAddress + pefh.Sections[i].VSize)))
                    return (long)(pefh.Sections[i].PAddress + (UInt32)RVA - pefh.Sections[i].VAddress);
            }

            throw new Exception("RVA not defined in PE file");
        }
    }
}
