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

namespace libtysila
{
	class PEFile : File
	{
        private bool parsed = false;
        private Stream f;

        private class PE_Props
        {
            public bool LargeString;
            public bool LargeGUID;
            public bool LargeBlob;
        }

        private PE_Props props;

        static Metadata.TableIndex ReadResolutionScope(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 2, m, new Metadata.TableId[] { Metadata.TableId.Module,
            Metadata.TableId.ModuleRef, Metadata.TableId.AssemblyRef, Metadata.TableId.TypeRef });
        }

        static Metadata.TableIndex ReadTypeDefOrRef(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 2, m, new Metadata.TableId[] { Metadata.TableId.TypeDef,
                Metadata.TableId.TypeRef, Metadata.TableId.TypeSpec });
        }

        static Metadata.TableIndex ReadMemberRefParent(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 3, m, new Metadata.TableId[] { Metadata.TableId.TypeDef,
                Metadata.TableId.TypeRef, Metadata.TableId.ModuleRef, Metadata.TableId.MethodDef,
                Metadata.TableId.TypeSpec} );
        }

        static Metadata.TableIndex ReadHasConstant(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 2, m, new Metadata.TableId[] { Metadata.TableId.Field,
                Metadata.TableId.Param, Metadata.TableId.Property });
        }

        static Metadata.TableIndex ReadHasCustomAttribute(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 5, m, new Metadata.TableId[] { 
                Metadata.TableId.MethodDef,
                Metadata.TableId.Field,
                Metadata.TableId.TypeRef,
                Metadata.TableId.TypeDef,
                Metadata.TableId.Param,
                Metadata.TableId.InterfaceImpl,
                Metadata.TableId.MemberRef,
                Metadata.TableId.Module,
                Metadata.TableId.NotUsed,
                Metadata.TableId.Property,
                Metadata.TableId.Event,
                Metadata.TableId.StandAloneSig,
                Metadata.TableId.ModuleRef,
                Metadata.TableId.TypeSpec,
                Metadata.TableId.Assembly,
                Metadata.TableId.AssemblyRef,
                Metadata.TableId.File,
                Metadata.TableId.ExportedType,
                Metadata.TableId.ManifestResource });
        }

        static Metadata.TableIndex ReadCustomAttributeType(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 3, m, new Metadata.TableId[] { Metadata.TableId.NotUsed,
                Metadata.TableId.NotUsed, Metadata.TableId.MethodDef, Metadata.TableId.MemberRef,
                Metadata.TableId.NotUsed });
        }

        static Metadata.TableIndex ReadHasDeclSecurity(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 2, m, new Metadata.TableId[] { Metadata.TableId.TypeDef,
                Metadata.TableId.MethodDef, Metadata.TableId.Assembly });
        }

        static Metadata.TableIndex ReadHasFieldMarshal(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 1, m, new Metadata.TableId[] { Metadata.TableId.Field,
                Metadata.TableId.Param });
        }

        static Metadata.TableIndex ReadHasSemantics(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 1, m, new Metadata.TableId[] { Metadata.TableId.Event,
                Metadata.TableId.Property });
        }

        static Metadata.TableIndex ReadImplementation(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 2, m, new Metadata.TableId[] { Metadata.TableId.File,
                Metadata.TableId.AssemblyRef, Metadata.TableId.ExportedType });
        }

        static Metadata.TableIndex ReadMethodDefOrRef(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 1, m, new Metadata.TableId[] { Metadata.TableId.MethodDef,
                Metadata.TableId.MemberRef });
        }

        static Metadata.TableIndex ReadMemberForwarded(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 1, m, new Metadata.TableId[] { Metadata.TableId.Field,
                Metadata.TableId.MethodDef });
        }

        static Metadata.TableIndex ReadTypeOrMethodDef(Stream file, Metadata m)
        {
            return ReadCodedIndex(file, 1, m, new Metadata.TableId[] { Metadata.TableId.TypeDef,
                Metadata.TableId.MethodDef });
        }

        static Metadata.TableIndex ReadSimpleIndex(Stream file, Metadata m, Metadata.TableId table)
        {
            Metadata.TableIndex ti = new Metadata.TableIndex(m);

            ti.Table = m.Tables[(int)table];
            if (m.Tables[(int)table].Length > 0xffff)
                ti.Index = (int)Read32(file);
            else
                ti.Index = (int)Read16(file);
            return ti;
        }

        static Metadata.TableIndex ReadCodedIndex(Stream file, int tagbits, Metadata m, Metadata.TableId[] tables)
        {
            Metadata.TableIndex ti = new Metadata.TableIndex(m);
            int maxindex = m.GetMaxIndex(tables);
            int table;

            if(maxindex >= (1 << (16 - tagbits))) {
                UInt32 val = Read32(file);
                ti.Index = (int)(val >> tagbits);
                table = (int)(val & (~(0xffffffff << tagbits)));
            } else {
                UInt16 val = Read16(file);
                ti.Index = (int)((UInt32)val >> tagbits);
                table = (int)((UInt32)(val & (~(0xffff << tagbits))));
            }

            ti.Table = m.Tables[(int)tables[table]];

            return ti;
        }

        static Guid ReadGUIDIndex(Stream file, PE_Props p, Metadata m)
        {
            UInt32 idx;
            if (p.LargeGUID) idx = Read32(file); else idx = (UInt32)Read16(file);
            if (idx == 0)
                return new Guid("00000000-0000-0000-0000-000000000000");
            byte[] buf = new byte[16];
            Array.Copy(m.GUIDHeap, (idx - 1) * 16, buf, 0, 16);
            return new Guid(buf);
        }

        static String ReadStringIndex(Stream file, PE_Props p, Metadata m)
        {
            UInt32 idx;
            if (p.LargeString) idx = Read32(file); else idx = (UInt32)Read16(file);
            StringBuilder str = new StringBuilder();
            byte b;
            while ((b = m.StringHeap[idx]) != (byte)'\0')
            {
                str.Append((char)b);
                idx++;
            }
            return str.ToString();
        }
            
        static byte[] ReadBlobIndex(Stream file, PE_Props p, Metadata m)
        {
            UInt32 idx;
            if (p.LargeBlob) idx = Read32(file); else idx = (UInt32)Read16(file);
            long bloblen;
            long blobstart;

            byte b1 = m.BlobHeap[idx];
            if ((b1 & 0xc0) == 0xc0)
            {
                b1 &= 0x3f;
                byte b2 = m.BlobHeap[idx + 1];
                byte b3 = m.BlobHeap[idx + 2];
                byte b4 = m.BlobHeap[idx + 3];
                blobstart = idx + 4;
                bloblen = (b1 << 24) + (b2 << 16) + (b3 << 8) + b4;
            }
            else if ((b1 & 0x80) != 0x0)
            {
                b1 &= 0x3f;
                byte b2 = m.BlobHeap[idx + 1];
                blobstart = idx + 2;
                bloblen = (b1 << 8) + b2;
            }
            else
            {
                blobstart = idx + 1;
                bloblen = b1 & 0x7f;
            }

            byte[] ret = new byte[bloblen];
            Array.Copy(m.BlobHeap, blobstart, ret, 0, bloblen);
            return ret;
        }

        static void Align32(Stream file)
        {
            long cur_pos = file.Position;
            long new_pos = (cur_pos + 3) & (~3);
            file.Seek(new_pos, SeekOrigin.Begin);
        }

        static String ReadSZ(Stream file)
        {
            byte b;
            StringBuilder s = new StringBuilder();

            while ((b = (byte)file.ReadByte()) != (byte)'\0')
            {
                s.Append((char)b);
            }

            return s.ToString();
        }

        static UInt32 Read32(System.IO.Stream file)
        {
            return (UInt32)((file.ReadByte() & 0xff) |
                ((file.ReadByte() & 0xff) << 8) |
                ((file.ReadByte() & 0xff) << 16) |
                ((file.ReadByte() & 0xff) << 24));
        }
        public static UInt32 Read32(byte[] byte_str, ref int offset_to_increment)
        {
            UInt32 ret = (UInt32)((((UInt32)byte_str[offset_to_increment]) & 0xff) |
                ((((UInt32)byte_str[offset_to_increment + 1]) & 0xff) << 8) |
                ((((UInt32)byte_str[offset_to_increment + 2]) & 0xff) << 16) |
                ((((UInt32)byte_str[offset_to_increment + 3]) & 0xff) << 24));
            offset_to_increment += 4;
            return ret;
        }

        static UInt32 Read24(System.IO.Stream file)
        {
            return ((UInt32)Read16(file) + ((UInt32)file.ReadByte() << 16));
        }

        static UInt16 Read16(System.IO.Stream file)
        {
            return (UInt16)((file.ReadByte() & 0xff) |
                ((file.ReadByte() & 0xff) << 8));
        }

        static UInt64 Read64(Stream file)
        {
            return ((UInt64)Read32(file) + ((UInt64)Read32(file) << 32));
        }

        static Version ReadVersion(Stream file)
        {
            Version v = new Version();
            v.MajorVersion = Read16(file);
            v.MinorVersion = Read16(file);
            v.BuildNumber = Read16(file);
            v.RevisionNumber = Read16(file);
            return v;
        }

        #region File Members

        static public bool IsFileType(System.IO.Stream file)
        {
            file.Seek(0, SeekOrigin.Begin);
            if (file.ReadByte() != 0x4d)
                return false;
            file.Seek(0x3c, SeekOrigin.Begin);
            UInt32 lfanew = Read32(file);
            file.Seek(lfanew, SeekOrigin.Begin);
            if (file.ReadByte() != 'P')
                return false;
            if (file.ReadByte() != 'E')
                return false;
            if (file.ReadByte() != '\0')
                return false;
            if (file.ReadByte() != '\0')
                return false;
            
            return true;
        }

        private class DataDir {
            public long RVA;
            public long Size;
        }

        private class SectionHeader {
            public String Name;
            public UInt32 VSize;
            public UInt32 VAddress;
            public UInt32 PSize;
            public UInt32 PAddress;
            public UInt32 Chars;
        }

        private class PE_File_Header {
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

        private class StreamHeader
        {
            public UInt32 Offset;
            public long FileOffset;
            public UInt32 Size;
            public String Name;
        }

        private StreamHeader sh_string, sh_guid, sh_blob, sh_us, sh_tables;

        private PE_File_Header pefh = new PE_File_Header();
        private Cli_Header clih = new Cli_Header();
        private string fname = null;

        public string GetFileName() { return fname; }

        public PEFile(string filename)
        {
            fname = filename;
        }

        public void Parse(System.IO.Stream file)
        {
            if (!IsFileType(file))
                throw new Exception("Incorrect file type");

            file.Seek(0x3c, SeekOrigin.Begin);
            UInt32 pefh_start = Read32(file) + 4;
            file.Seek(pefh_start + 2, SeekOrigin.Begin);
            pefh.NumberOfSections = Read16(file);
            pefh.Sections = new SectionHeader[pefh.NumberOfSections];
            file.Seek(pefh_start + 4, SeekOrigin.Begin);
            TimeSpan t = new TimeSpan(0, 0, (int)Read32(file));
            pefh.TimeDateStamp = new DateTime(1970, 1, 1) + t;
            file.Seek(pefh_start + 16, SeekOrigin.Begin);
            pefh.OptHeaderSize = Read16(file);
            if (pefh.OptHeaderSize < 224)
                throw new Exception("PE optional header too small");
            file.Seek(pefh_start + 18, SeekOrigin.Begin);
            pefh.Chars = Read16(file);
            if ((pefh.Chars != 0x210e) && (pefh.Chars != 0x10e) && (pefh.Chars != 0x102) && (pefh.Chars != 0x2102))
                throw new Exception("Invalid PE file header characteristics");
            file.Seek(pefh_start + 20 + 208, SeekOrigin.Begin);
            pefh.CliHeader = new DataDir();
            pefh.CliHeader.RVA = Read32(file);
            pefh.CliHeader.Size = Read32(file);

            // Read the section headers
            UInt32 sections_start = pefh_start + 20 + pefh.OptHeaderSize;
            UInt32 i;
            for (i = 0; i < pefh.NumberOfSections; i++)
            {
                UInt32 s_start = sections_start + i * 40;
                pefh.Sections[i] = new SectionHeader();

                file.Seek(s_start, SeekOrigin.Begin);
                byte[] str = new byte[9];
                file.Read(str, 0, 8);
                str[8] = (byte)'\0';
                char[] w_str = new char[9];
                for(int j = 0; j < 9; j++)
                    w_str[j] = (char)str[j];
                pefh.Sections[i].Name = new String(w_str);
                pefh.Sections[i].Name = pefh.Sections[i].Name.Remove(pefh.Sections[i].Name.IndexOf("\0"));

                file.Seek(s_start + 8, SeekOrigin.Begin);
                pefh.Sections[i].VSize = Read32(file);
                pefh.Sections[i].VAddress = Read32(file);
                pefh.Sections[i].PSize = Read32(file);
                pefh.Sections[i].PAddress = Read32(file);

                file.Seek(s_start + 36, SeekOrigin.Begin);
                pefh.Sections[i].Chars = Read32(file);
            }

            // Read the Cli header
            long clih_offset = ResolveRVA((UIntPtr)pefh.CliHeader.RVA);
            file.Seek(clih_offset + 8, SeekOrigin.Begin);
            clih.Metadata.RVA = Read32(file);
            clih.Metadata.Size = Read32(file);
            file.Seek(clih_offset + 20, SeekOrigin.Begin);
            clih.EntryPointToken = Read32(file);

            parsed = true;
            f = file;
        }

        public Metadata GetMetadata(Assembler ass)
        {
            if(!parsed)
                throw new Exception("PE file not parsed!");

            Metadata m = new Metadata();

            // First, read the metadata root
            long mroot_offset = ResolveRVA((UIntPtr)clih.Metadata.RVA);
            f.Seek(mroot_offset, SeekOrigin.Begin);
            UInt32 sig = Read32(f);
            if (sig != 0x424A5342)
                throw new Exception("Invalid metadata root");
            f.Seek(mroot_offset + 12, SeekOrigin.Begin);
            UInt32 vstr_len = Read32(f);
            m.VersionString = ReadSZ(f);
            f.Seek(mroot_offset + 16 + vstr_len + 2, SeekOrigin.Begin);
            UInt16 nstr = Read16(f);

            // Now, read the stream headers
            UInt16 i;
            for (i = 0; i < nstr; i++)
            {
                StreamHeader sh = new StreamHeader();
                sh.Offset = Read32(f);
                sh.FileOffset = ResolveRVA((UIntPtr)(clih.Metadata.RVA + sh.Offset));
                sh.Size = Read32(f);
                sh.Name = ReadSZ(f);
                Align32(f);

                if (sh.Name == "#Strings")
                    sh_string = sh;
                else if (sh.Name == "#US")
                    sh_us = sh;
                else if (sh.Name == "#GUID")
                    sh_guid = sh;
                else if (sh.Name == "#Blob")
                    sh_blob = sh;
                else if (sh.Name == "#~")
                    sh_tables = sh;
                else
                    throw new Exception("Unknown metadata table");
            }

            // And now read the heaps
            if (sh_blob != null)
            {
                m.BlobHeap = new byte[sh_blob.Size];
                f.Seek(sh_blob.FileOffset, SeekOrigin.Begin);
                f.Read(m.BlobHeap, 0, (int)sh_blob.Size);
            }
            else { m.BlobHeap = new byte[1] { 0 }; }
            if (sh_guid != null)
            {
                m.GUIDHeap = new byte[sh_guid.Size];
                f.Seek(sh_guid.FileOffset, SeekOrigin.Begin);
                f.Read(m.GUIDHeap, 0, (int)sh_guid.Size);
            }
            else { m.GUIDHeap = new byte[1] { 0 }; }
            if (sh_string != null)
            {
                m.StringHeap = new byte[sh_string.Size];
                f.Seek(sh_string.FileOffset, SeekOrigin.Begin);
                f.Read(m.StringHeap, 0, (int)sh_string.Size);
            }
            else { m.StringHeap = new byte[1] { 0 }; }
            if (sh_us != null)
            {
                m.USHeap = new byte[sh_us.Size];
                f.Seek(sh_us.FileOffset, SeekOrigin.Begin);
                f.Read(m.USHeap, 0, (int)sh_us.Size);
            }
            else { m.USHeap = new byte[1] { 0 }; }

            // Define the sizes of the heaps
            f.Seek(sh_tables.FileOffset + 6, SeekOrigin.Begin);
            byte hs = (byte)f.ReadByte();
            props = new PE_Props();
            if ((hs & 0x01) != 0x0)
                props.LargeString = true;
            if ((hs & 0x02) != 0x0)
                props.LargeGUID = true;
            if ((hs & 0x04) != 0x0)
                props.LargeBlob = true;

            // Work out the number of rows in each table
            f.Seek(sh_tables.FileOffset + 8, SeekOrigin.Begin);
            UInt64 valid = Read64(f);

            f.Seek(sh_tables.FileOffset + 24, SeekOrigin.Begin);
            for (i = 0; i < 64; i++)
            {
                UInt32 tsize;
                if (((valid >> i) & 0x1) != 0x0)
                    tsize = Read32(f);
                else
                    tsize = 0;
                m.Tables[i] = m.CreateTableRowArray(i, (int)tsize);
            }

            // Read the tables themselves
            for (i = 0; i < m.Tables[0x00].Length; i++)
            {
                Metadata.ModuleRow a = new Metadata.ModuleRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Generation = Read16(f);
                a.Name = ReadStringIndex(f, props, m);
                a.Mvid = ReadGUIDIndex(f, props, m);
                a.EncId = ReadGUIDIndex(f, props, m);
                a.EncBaseId = ReadGUIDIndex(f, props, m);
                m.Tables[0x00][i] = a;
            }

            for (i = 0; i < m.Tables[0x01].Length; i++)
            {
                Metadata.TypeRefRow a = new Metadata.TypeRefRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.ResolutionScope = ReadResolutionScope(f, m);
                a.TypeName = ReadStringIndex(f, props, m);
                a.TypeNamespace = ReadStringIndex(f, props, m);
                m.Tables[0x01][i] = a;
            }

            for (i = 0; i < m.Tables[0x02].Length; i++)
            {
                Metadata.TypeDefRow a = new Metadata.TypeDefRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Flags = Read32(f);
                a._ActualTypeName = ReadStringIndex(f, props, m);
                a._ActualTypeNamespace = ReadStringIndex(f, props, m);
                a.Extends = ReadTypeDefOrRef(f, m);
                a.FieldList = ReadSimpleIndex(f, m, Metadata.TableId.Field);
                a.MethodList = ReadSimpleIndex(f, m, Metadata.TableId.MethodDef);
                m.Tables[0x02][i] = a;
            }

            for (i = 0; i < m.Tables[0x04].Length; i++)
            {
                Metadata.FieldRow a = new Metadata.FieldRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Flags = Read16(f);
                a.Name = ReadStringIndex(f, props, m);
                a.Signature = ReadBlobIndex(f, props, m);
                m.Tables[0x04][i] = a;
            }

            for (i = 0; i < m.Tables[0x06].Length; i++)
            {
                Metadata.MethodDefRow a = new Metadata.MethodDefRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.RVA = Read32(f);
                a.ImplFlags = Read16(f);
                a.Flags = Read16(f);
                a.Name = ReadStringIndex(f, props, m);
                a.Signature = ReadBlobIndex(f, props, m);
                a.ParamList = ReadSimpleIndex(f, m, Metadata.TableId.Param);
                m.Tables[0x06][i] = a;
            }

            for (i = 0; i < m.Tables[0x08].Length; i++)
            {
                Metadata.ParamRow a = new Metadata.ParamRow();
                a.m = m;
                a.ass = ass;
                a.RowNumber = i + 1;
                a.Flags = Read16(f);
                a.Sequence = Read16(f);
                a.Name = ReadStringIndex(f, props, m);
                m.Tables[0x08][i] = a;
            }

            for (i = 0; i < m.Tables[0x09].Length; i++)
            {
                Metadata.InterfaceImplRow a = new Metadata.InterfaceImplRow();
                a.RowNumber = i + 1;
                a.ass = ass;
                a.m = m;
                a.Class = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                a.Interface = ReadTypeDefOrRef(f, m);
                m.Tables[0x09][i] = a;
            }

            for (i = 0; i < m.Tables[0x0a].Length; i++)
            {
                Metadata.MemberRefRow a = new Metadata.MemberRefRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Class = ReadMemberRefParent(f, m);
                a.Name = ReadStringIndex(f, props, m);
                a.Signature = ReadBlobIndex(f, props, m);
                m.Tables[0x0a][i] = a;
            }

            for (i = 0; i < m.Tables[0x0b].Length; i++)
            {
                Metadata.ConstantRow a = new Metadata.ConstantRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Type = (BaseType_Type)Read16(f);
                a.Parent = ReadHasConstant(f, m);
                a.Value = ReadBlobIndex(f, props, m);
                m.Tables[0x0b][i] = a;
            }

            for (i = 0; i < m.Tables[0x0c].Length; i++)
            {
                Metadata.CustomAttributeRow a = new Metadata.CustomAttributeRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Parent = ReadHasCustomAttribute(f, m);
                a.Type = ReadCustomAttributeType(f, m);
                a.Value = ReadBlobIndex(f, props, m);
                m.Tables[0x0c][i] = a;
            }

            for (i = 0; i < m.Tables[0x0d].Length; i++)
            {
                Metadata.FieldMarshalRow a = new Metadata.FieldMarshalRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Parent = ReadHasFieldMarshal(f, m);
                a.NativeType = ReadBlobIndex(f, props, m);
                m.Tables[0x0d][i] = a;
            }

            for (i = 0; i < m.Tables[0x0e].Length; i++)
            {
                Metadata.DeclSecurityRow a = new Metadata.DeclSecurityRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Action = Read16(f);
                a.Parent = ReadHasDeclSecurity(f, m);
                a.PermissionSet = ReadBlobIndex(f, props, m);
                m.Tables[0x0e][i] = a;
            }

            for (i = 0; i < m.Tables[0x0f].Length; i++)
            {
                Metadata.ClassLayoutRow a = new Metadata.ClassLayoutRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.PackingSize = Read16(f);
                a.ClassSize = Read32(f);
                a.Parent = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                m.Tables[0x0f][i] = a;
            }

            for (i = 0; i < m.Tables[0x10].Length; i++)
            {
                Metadata.FieldLayoutRow a = new Metadata.FieldLayoutRow();
                a.m = m;
                a.ass = ass;
                a.RowNumber = i + 1;
                a.Offset = Read32(f);
                a.Field = ReadSimpleIndex(f, m, Metadata.TableId.Field);
                m.Tables[0x10][i] = a;
            }

            for (i = 0; i < m.Tables[0x11].Length; i++)
            {
                Metadata.StandAloneSigRow a = new Metadata.StandAloneSigRow();
                a.m = m;
                a.ass = ass;
                a.RowNumber = i + 1;
                a.Signature = ReadBlobIndex(f, props, m);
                m.Tables[0x11][i] = a;
            }

            for (i = 0; i < m.Tables[0x12].Length; i++)
            {
                Metadata.EventMapRow a = new Metadata.EventMapRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Parent = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                a.EventList = ReadSimpleIndex(f, m, Metadata.TableId.Event);
                m.Tables[0x12][i] = a;
            }

            for (i = 0; i < m.Tables[0x14].Length; i++)
            {
                Metadata.EventRow a = new Metadata.EventRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.EventFlags = Read16(f);
                a.Name = ReadStringIndex(f, props, m);
                a.EventType = ReadTypeDefOrRef(f, m);
                m.Tables[0x14][i] = a;
            }

            for (i = 0; i < m.Tables[0x15].Length; i++)
            {
                Metadata.PropertyMapRow a = new Metadata.PropertyMapRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Parent = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                a.PropertyList = ReadSimpleIndex(f, m, Metadata.TableId.Property);
                m.Tables[0x15][i] = a;
            }

            for(i = 0; i < m.Tables[0x17].Length; i++)
            {
                Metadata.PropertyRow a = new Metadata.PropertyRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Flags = Read16(f);
                a.Name = ReadStringIndex(f, props, m);
                a.Type = ReadBlobIndex(f, props, m);
                m.Tables[0x17][i] = a;
            }

            for (i = 0; i < m.Tables[0x18].Length; i++)
            {
                Metadata.MethodSemanticsRow a = new Metadata.MethodSemanticsRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Semantics = Read16(f);
                a.Method = ReadSimpleIndex(f, m, Metadata.TableId.MethodDef);
                a.Association = ReadHasSemantics(f, m);
                m.Tables[0x18][i] = a;
            }

            for (i = 0; i < m.Tables[0x19].Length; i++)
            {
                Metadata.MethodImplRow a = new Metadata.MethodImplRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Class = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                a.MethodBody = ReadMethodDefOrRef(f, m);
                a.MethodDeclaration = ReadMethodDefOrRef(f, m);
                m.Tables[0x19][i] = a;
            }

            for (i = 0; i < m.Tables[0x1a].Length; i++)
            {
                Metadata.ModuleRefRow a = new Metadata.ModuleRefRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Name = ReadStringIndex(f, props, m);
                m.Tables[0x1a][i] = a;
            }

            for (i = 0; i < m.Tables[0x1b].Length; i++)
            {
                Metadata.TypeSpecRow a = new Metadata.TypeSpecRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Signature = ReadBlobIndex(f, props, m);
                m.Tables[0x1b][i] = a;
            }

            for (i = 0; i < m.Tables[0x1c].Length; i++)
            {
                Metadata.ImplMapRow a = new Metadata.ImplMapRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.MappingFlags = Read16(f);
                a.MemberForwarded = ReadMemberForwarded(f, m);
                a.ImportName = ReadStringIndex(f, props, m);
                a.ImportScope = ReadSimpleIndex(f, m, Metadata.TableId.ModuleRef);
                m.Tables[0x1c][i] = a;
            }

            for (i = 0; i < m.Tables[0x1d].Length; i++)
            {
                Metadata.FieldRVARow a = new Metadata.FieldRVARow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.RVA = Read32(f);
                a.Field = ReadSimpleIndex(f, m, Metadata.TableId.Field);
                m.Tables[0x1d][i] = a;
            }

            for (i = 0; i < m.Tables[0x20].Length; i++)
            {
                Metadata.AssemblyRow a = new Metadata.AssemblyRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.HashAlgId = (Metadata.AssemblyRow.AssemblyHashAlgorithm)Read32(f);
                a.Version = ReadVersion(f);
                a.Flags = Read32(f);
                a.PublicKey = ReadBlobIndex(f, props, m);
                a.Name = ReadStringIndex(f, props, m);
                a.Culture = ReadStringIndex(f, props, m);
                m.Tables[0x20][i] = a;
            }

            for (i = 0; i < m.Tables[0x23].Length; i++)
            {
                Metadata.AssemblyRefRow a = new Metadata.AssemblyRefRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Version = ReadVersion(f);
                a.Flags = Read32(f);
                a.PublicKeyOrToken = ReadBlobIndex(f, props, m);
                a.Name = ReadStringIndex(f, props, m);
                a.Culture = ReadStringIndex(f, props, m);
                a.HashValue = ReadBlobIndex(f, props, m);
                m.Tables[0x23][i] = a;
            }

            for (i = 0; i < m.Tables[0x26].Length; i++)
            {
                Metadata.FileRow a = new Metadata.FileRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Flags = Read32(f);
                a.Name = ReadStringIndex(f, props, m);
                a.HashValue = ReadBlobIndex(f, props, m);
                m.Tables[0x26][i] = a;
            }

            for (i = 0; i < m.Tables[0x27].Length; i++)
            {
                Metadata.ExportedTypeRow a = new Metadata.ExportedTypeRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Flags = Read32(f);
                a.TypeDefId = Read32(f);
                a.TypeName = ReadStringIndex(f, props, m);
                a.TypeNamespace = ReadStringIndex(f, props, m);
                a.Implementation = ReadImplementation(f, m);
                m.Tables[0x27][i] = a;
            }

            for (i = 0; i < m.Tables[0x28].Length; i++)
            {
                Metadata.ManifestResourceRow a = new Metadata.ManifestResourceRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Offset = Read32(f);
                a.Flags = Read32(f);
                a.Name = ReadStringIndex(f, props, m);
                a.Implementation = ReadImplementation(f, m);
                m.Tables[0x28][i] = a;
            }

            for (i = 0; i < m.Tables[0x29].Length; i++)
            {
                Metadata.NestedClassRow a = new Metadata.NestedClassRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.NestedClass = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                a.EnclosingClass = ReadSimpleIndex(f, m, Metadata.TableId.TypeDef);
                m.Tables[0x29][i] = a;
            }

            for (i = 0; i < m.Tables[0x2a].Length; i++)
            {
                Metadata.GenericParamRow a = new Metadata.GenericParamRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Number = Read16(f);
                a.Flags = Read16(f);
                a.Owner = ReadTypeOrMethodDef(f, m);
                a.Name = ReadStringIndex(f, props, m);
                m.Tables[0x2a][i] = a;
            }

            for (i = 0; i < m.Tables[0x2b].Length; i++)
            {
                Metadata.MethodSpecRow a = new Metadata.MethodSpecRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Method = ReadMethodDefOrRef(f, m);
                a.Instantiation = ReadBlobIndex(f, props, m);
                m.Tables[0x2b][i] = a;
            }

            for (i = 0; i < m.Tables[0x2c].Length; i++)
            {
                Metadata.GenericParamConstraintRow a = new Metadata.GenericParamConstraintRow();
                a.RowNumber = i + 1;
                a.m = m;
                a.ass = ass;
                a.Owner = ReadSimpleIndex(f, m, Metadata.TableId.GenericParam);
                a.Constraint = ReadTypeDefOrRef(f, m);
                m.Tables[0x2c][i] = a;
            }

            /* Interpret nested classes */
            foreach (Metadata.NestedClassRow ncr in m.Tables[(int)Metadata.TableId.NestedClass])
            {
                Metadata.TypeDefRow nested_class = Metadata.GetTypeDef(ncr.NestedClass.Value, ass);
                Metadata.TypeDefRow enclosing_class = Metadata.GetTypeDef(ncr.EnclosingClass.Value, ass);

                if (nested_class._NestedParent != null)
                    throw new MetadataParseException("Multiple nested parents defined for class");

                nested_class._NestedParent = enclosing_class;
                enclosing_class._NestedChildren.Add(nested_class);
            }

            /* Decide on the ownership of methods, fields, custom attributes, fieldRVAs, Constants, class layouts, interfaceimpls and methodimpls */
            int typedef_id = (int)Metadata.TableId.TypeDef;
            int typedef_count = m.Tables[typedef_id].Length;
            for (i = 0; i < typedef_count; i++)
            {
                Metadata.TypeDefRow tdr = m.Tables[typedef_id][i] as Metadata.TypeDefRow;
                Metadata.TableIndex first_method = tdr.MethodList;
                Metadata.TableIndex last_method = Metadata.GetLastMethod(tdr);

                for (Metadata.TableIndex meth_idx = first_method; meth_idx < last_method; meth_idx++)
                {
                    Metadata.MethodDefRow meth_mdr = meth_idx.Value as Metadata.MethodDefRow;
                    meth_mdr.owning_type = tdr;
                    tdr.Methods.Add(meth_mdr);
                }

                Metadata.TableIndex first_field = tdr.FieldList;
                Metadata.TableIndex last_field = Metadata.GetLastField(tdr.m, tdr);

                for (Metadata.TableIndex field_idx = first_field; field_idx < last_field; field_idx++)
                {
                    Metadata.FieldRow field_fr = field_idx.Value as Metadata.FieldRow;
                    field_fr.owning_type = tdr;
                    tdr.Fields.Add(field_fr);
                }
            }

            foreach (Metadata.ConstantRow cr in m.Tables[(int)Metadata.TableId.Constant])
            {
                if (cr.Parent.TableId == Metadata.TableId.Param)
                    ((Metadata.ParamRow)cr.Parent.Value).Constant = cr;
                else if (cr.Parent.TableId == Metadata.TableId.Field)
                    ((Metadata.FieldRow)cr.Parent.Value).Constant = cr;
                else if (cr.Parent.TableId == Metadata.TableId.Property)
                    ((Metadata.PropertyRow)cr.Parent.Value).Constant = cr;
            }

            foreach (Metadata.MethodImplRow mir in m.Tables[(int)Metadata.TableId.MethodImpl])
            {
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(mir.Class.ToToken(), ass);
                tdr.MethodImpls.Add(mir);
            }

            foreach (Metadata.FieldRVARow rva_fr in m.Tables[(int)Metadata.TableId.FieldRVA])
            {
                Metadata.FieldRow fr = Metadata.GetFieldDef(rva_fr.Field.ToToken(), ass);
                if (fr.RVA != null)
                    throw new Exception("Two FieldRVA rows reference the same Field row");
                fr.RVA = rva_fr;
            }

            foreach (Metadata.ClassLayoutRow clr in m.Tables[(int)Metadata.TableId.ClassLayout])
            {
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(clr.Parent.ToToken(), ass);

                if (tdr.IsAutoLayout)
                    throw new Exception("Cannot apply a ClassLayout to a type marked auto layout");
                if (tdr.Layout != null)
                    throw new Exception("Two ClassLayout rows reference the same TypeDef row");
                tdr.Layout = clr;
            }

            foreach (Metadata.CustomAttributeRow car in m.Tables[(int)Metadata.TableId.CustomAttribute])
            {
                Metadata.TableRow tr = car.Parent.Value as Metadata.TableRow;
                if(tr != null)
                    tr.CustomAttrs.Add(car);
            }

            foreach (Metadata.InterfaceImplRow iir in m.Tables[(int)Metadata.TableId.InterfaceImpl])
            {
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(iir.Class.ToToken(), ass);
                tdr._InterfaceImpls.Add(iir);
            }

            /* Now read the method body structures */
            foreach(Metadata.MethodDefRow mth in m.Tables[(int)Metadata.TableId.MethodDef]) {
                if (mth.RVA == 0)
                    mth.Flags |= 0x400;   // no implementation defined, set as abstract
                if ((mth.Flags & 0x400) == 0x400)
                    continue;   // method is abstract
                f.Seek(ResolveRVA((UIntPtr)mth.RVA), SeekOrigin.Begin);
                int firstbyte = f.ReadByte();
                if ((firstbyte & 3) == 2)
                {
                    /* Tiny header */
                    mth.Body.CodeRVA = mth.RVA + 1;
                    mth.Body.CodeLength = ((UInt32)firstbyte >> 2) & 0x3f;
                    mth.Body.LocalVars = null;
                    mth.Body.MaxStack = 8;
                }
                else if ((firstbyte & 3) == 3)
                {
                    /* Fat header */
                    int secondbyte = f.ReadByte();

                    bool has_extra_sects = false;

                    if ((firstbyte & 0x8) == 0x8)
                        has_extra_sects = true;

                    if ((firstbyte & 0x10) == 0x10)
                        mth.Body.InitLocals = true;

                    mth.Body.CodeRVA = mth.RVA + (((UInt32)secondbyte >> 4) & 0xf) * 4;
                    mth.Body.MaxStack = (UInt32)Read16(f);
                    mth.Body.CodeLength = Read32(f);

                    Token t = new Token(Read32(f), m);
                    //Metadata.TableIndex lv = ReadSimpleIndex(f, m, Metadata.TableId.StandAloneSig);
                    //if (lv.Index == 0)
                    //    mth.Body.LocalVars = null;
                    //else
                    //    mth.Body.LocalVars = ((Metadata.StandAloneSigRow)lv.Value).Signature;
                    if (t.Value == null)
                        mth.Body.LocalVars = null;
                    else
                        mth.Body.LocalVars = ((Metadata.StandAloneSigRow)t.Value).Signature;

                    if (has_extra_sects)
                    {
                        /* Read some extra sections which come after the method body */
                        f.Seek(mth.Body.CodeLength, SeekOrigin.Current);

                        // CIL II:25.4.5 extra sections are alingned on a 4 byte boundary
                        Align32(f);

                        bool cont = true;
                        while (cont)
                        {
                            cont = false;
                            int b1 = f.ReadByte();
                            if ((b1 & 0x80) == 0x80)
                                cont = true;

                            if ((b1 & 0x01) != 0x01)
                                throw new Exception("Only EHTable sections are supported");

                            bool is_fat = false;
                            if ((b1 & 0x40) == 0x40)
                                is_fat = true;

                            int clause_count = 0;
                            if (is_fat)
                                clause_count = ((int)Read24(f) - 4) / 24;
                            else
                            {
                                clause_count = (f.ReadByte() - 4) / 12;
                                Read16(f);
                            }

                            for (int clause = 0; clause < clause_count; clause++)
                            {
                                Metadata.MethodBody.EHClause eh = new Metadata.MethodBody.EHClause();

                                if (is_fat)
                                {
                                    eh.Flags = Read32(f);
                                    eh.TryOffset = Read32(f);
                                    eh.TryLength = Read32(f);
                                    eh.HandlerOffset = Read32(f);
                                    eh.HandlerLength = Read32(f);

                                }
                                else
                                {
                                    eh.Flags = (uint)Read16(f);
                                    eh.TryOffset = (uint)Read16(f);
                                    eh.TryLength = (uint)f.ReadByte();
                                    eh.HandlerOffset = (uint)Read16(f);
                                    eh.HandlerLength = (uint)f.ReadByte();
                                }

                                if (eh.Flags == 0x0)
                                    eh.ClassToken = new Token(Read32(f), m);
                                else if (eh.Flags == 0x1)
                                    eh.FilterOffset = Read32(f);
                                else
                                    Read32(f);

                                mth.Body.exceptions.Add(eh);
                            }
                        }
                    }
                }
                else
                    throw new Exception("Unknown method header type");

                f.Seek(ResolveRVA((UIntPtr)mth.Body.CodeRVA), SeekOrigin.Begin);
                mth.Body.Body = new byte[mth.Body.CodeLength];
                f.Read(mth.Body.Body, 0, (int)mth.Body.CodeLength);
            }

            return m;
        }

        public long ResolveRVA(UIntPtr RVA)
        {
            if (pefh.Sections == null)
                throw new Exception("Section table not initialized");
            for (UInt32 i = 0; i < pefh.NumberOfSections; i++)
            {
                if (pefh.Sections[i] == null)
                    throw new Exception("Section not initialized");
                if (((pefh.Sections[i].Chars & 0x60) != 0) && ((UInt32)RVA >= pefh.Sections[i].VAddress) &&
                    ((UInt32)RVA < (pefh.Sections[i].VAddress + pefh.Sections[i].VSize)))
                    return (long)(pefh.Sections[i].PAddress + (UInt32)RVA - pefh.Sections[i].VAddress);
            }

            throw new Exception("RVA not defined in PE file");
        }

        public UInt32 GetStartToken()
        {
            if(!parsed)
                throw new Exception("No PE file parsed");
            return clih.EntryPointToken;
        }

        #endregion

        public class MetadataParseException : Exception
        {
            public MetadataParseException(string msg) : base(msg) { }
        }

        public void GetDataAtRVA(byte[] buf, UIntPtr rva, UIntPtr length)
        {
            f.Seek(ResolveRVA(rva), SeekOrigin.Begin);
            f.Read(buf, 0, (int)length);
        }

        public void CloseFile()
        {
            f.Close();
        }
    }
}
