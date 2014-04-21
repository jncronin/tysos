using System;
using System.Collections.Generic;
using System.Text;

namespace CMExpLib
{
    public class MetadataObject
    {
        public ulong Address;
        public string LayoutName;

        Layout Layout;
        public Dictionary<string, object> Fields = new Dictionary<string,object>();

        internal Elf64Reader.ElfHeader ehdr;
        internal ulong file_offset;

        static Dictionary<ulong, MetadataObject> cache = new Dictionary<ulong, MetadataObject>();

        public class Reference
        {
            public Elf64Reader.ElfHeader ehdr;
            public ulong file_offset;
            public ulong Address;
            public string Name;
            public string Type;

            public override string ToString()
            {
                return Name;
            }
        }

        class ObjectToName
        {
            public string VTName;
            public string NameExp;
        }

        static ObjectToName[] obj_to_name = new ObjectToName[] {
            new ObjectToName { VTName = "__tysos_type_vt", NameExp = "${TypeNamespace}.${TypeName}" },
            new ObjectToName { VTName = "__tysos_gt_vt", NameExp = "${TypeNamespace}.${TypeName}" },
            new ObjectToName { VTName = "__tysos_gtd_vt", NameExp = "${TypeNamespace}.${TypeName}" },
            new ObjectToName { VTName = "__tysos_assembly_vt", NameExp = "${assemblyName}"},
            new ObjectToName { VTName = "__tysos_field_vt", NameExp = "${_Name}"},
            new ObjectToName { VTName = "__tysos_method_vt", NameExp = "${_Name}"}
        };

        public void Read(bool read_references)
        {
            // Determine the typeinfo object
            ulong vt_vaddr = ehdr.ReadPointer(file_offset);
            ulong vt_offset = ehdr.VaddrToOffset(vt_vaddr);
            ulong ti_vaddr = ehdr.ReadPointer(vt_offset);

            Layout = ehdr.stab.lm.Layouts[ti_vaddr];
            LayoutName = Layout.Name;

            foreach(Layout.Field f in Layout.Fields)
            {
                object o = ReadFieldData(f, file_offset, ehdr, read_references);
                Fields.Add(f.Name, o);
            }
        }

        private object ReadFieldData(CMExpLib.Layout.Field f, ulong file_offset, Elf64Reader.ElfHeader ehdr)
        { return ReadFieldData(f, file_offset, ehdr, true); }
        private object ReadFieldData(CMExpLib.Layout.Field f, ulong file_offset, Elf64Reader.ElfHeader ehdr, bool read_references)
        {
            long old_pos = ehdr.r.BaseStream.Position;
            object ret = null;
            bool is_obj = false;
            ehdr.r.BaseStream.Seek((long)file_offset + (long)f.Offset, System.IO.SeekOrigin.Begin);
            if (f.ftype == CMExpLib.Layout.Field.FType.Value)
            {
                if (f.FieldType == null)
                {
                    // It's an unknown field type, load it as a signed integer
                    switch (f.Length)
                    {
                        case 1:
                            ret = ehdr.r.ReadSByte();
                            break;
                        case 2:
                            ret = ehdr.r.ReadInt16();
                            break;
                        case 4:
                            ret = ehdr.r.ReadInt32();
                            is_obj = true;
                            break;
                        case 8:
                            ret = ehdr.r.ReadInt64();
                            is_obj = true;
                            break;
                        default:
                            ret = ehdr.r.ReadBytes(f.Length);
                            break;
                    }
                }
                else
                {
                    if (f.FieldType == "I")
                    {
                        ret = ehdr.ReadPointer((ulong)ehdr.r.BaseStream.Position);
                        is_obj = true;
                    }
                    else if (f.FieldType == "U")
                    {
                        ret = ehdr.ReadPointer((ulong)ehdr.r.BaseStream.Position);
                        is_obj = true;
                    }
                    else if (f.FieldType == "I1")
                        ret = ehdr.r.ReadSByte();
                    else if (f.FieldType == "I2")
                        ret = ehdr.r.ReadInt16();
                    else if (f.FieldType == "I4")
                        ret = ehdr.r.ReadInt32();
                    else if (f.FieldType == "I8")
                        ret = ehdr.r.ReadInt64();
                    else if (f.FieldType == "U1")
                        ret = ehdr.r.ReadByte();
                    else if (f.FieldType == "U2")
                        ret = ehdr.r.ReadUInt16();
                    else if (f.FieldType == "U4")
                        ret = ehdr.r.ReadUInt32();
                    else if (f.FieldType == "U8")
                        ret = ehdr.r.ReadUInt64();
                    else if (f.FieldType == "Boolean")
                    {
                        if (ehdr.r.ReadByte() == 0)
                            ret = false;
                        ret = true;
                    }
                    else if (f.FieldType == "Char")
                        ret = (char)ehdr.r.ReadUInt16();
                    else if (f.FieldType == "R4")
                        ret = ehdr.r.ReadSingle();
                    else if (f.FieldType == "R8")
                        ret = ehdr.r.ReadDouble();
                    else if (f.FieldType == "String")
                    {
                        ulong str_ptr = ehdr.ReadPointer((ulong)ehdr.r.BaseStream.Position);
                        ulong str_offset = ehdr.VaddrToOffset(str_ptr);

                        ehdr.r.BaseStream.Seek((long)str_offset + (long)ehdr.stab.ass.GetStringFieldOffset(libtysila.Assembler.StringFields.length), System.IO.SeekOrigin.Begin);
                        int str_len = ehdr.r.ReadInt32();
                        ehdr.r.BaseStream.Seek((long)str_offset + (long)ehdr.stab.ass.GetStringFieldOffset(libtysila.Assembler.StringFields.data_offset), System.IO.SeekOrigin.Begin);
                        byte[] str_data = ehdr.r.ReadBytes(str_len * 2);
                        ret = Encoding.Unicode.GetString(str_data);
                    }
                    else
                    {
                        // It's an unknown field type, load it as a signed integer
                        switch (f.Length)
                        {
                            case 1:
                                ret = ehdr.r.ReadSByte();
                                break;
                            case 2:
                                ret = ehdr.r.ReadInt16();
                                break;
                            case 4:
                                ret = ehdr.r.ReadInt32();
                                is_obj = true;
                                break;
                            case 8:
                                ret = ehdr.r.ReadInt64();
                                is_obj = true;
                                break;
                            default:
                                ret = ehdr.r.ReadBytes(f.Length);
                                break;
                        }
                    }
                }
            }
            else if (f.ftype == CMExpLib.Layout.Field.FType.NTArray)
            {
                ulong list_vaddr = ehdr.ReadPointer();
                if(list_vaddr == 0)
                    return new object[] {};

                ulong list_offset = ehdr.VaddrToOffset(list_vaddr);
                ehdr.r.BaseStream.Seek((long)list_offset, System.IO.SeekOrigin.Begin);

                ulong cur_item;
                List<object> items = new List<object>();

                do
                {
                    cur_item = ehdr.ReadPointer();
                    if (cur_item != 0)
                    {
                        if (read_references)
                            items.Add(ReadReference(cur_item, ehdr));
                        else
                            items.Add(cur_item);
                    // The Interfaces list in TysosType is special in that each item is interspersed
                    //  with the pointer to the vtable for that interface
                    if ((f.l.Name == "_ZX9TysosTypeTI") && (f.Name == "Interfaces"))
                        ehdr.ReadPointer();                    
                    }
                } while (cur_item != 0);
                ret = items.ToArray();
            }

            if (is_obj && read_references)
                ret = ReadReference(Convert.ToUInt64(ret), ehdr);

            ehdr.r.BaseStream.Seek(old_pos, System.IO.SeekOrigin.Begin);

            return ret;
        }

        public static object ReadReference(ulong vaddr, Elf64Reader.ElfHeader ehdr)
        {
            // Try and read the vaddr as a reference
            long old_pos = ehdr.r.BaseStream.Position;

            try
            {
                ulong file_offset = ehdr.VaddrToOffset(vaddr);
                ulong vt = ehdr.ReadPointer(file_offset);
                string vt_name = ehdr.stab.GetSymbolName(vt);

                foreach (ObjectToName otn in obj_to_name)
                {
                    if (otn.VTName == vt_name)
                    {
                        Reference ret = new Reference();
                        ret.Address = vaddr;
                        ret.file_offset = file_offset;
                        ret.Type = vt_name;

                        //ulong ti_vaddr = ehdr.ReadPointer(ehdr.VaddrToOffset(vt));
                        MetadataObject r = ReadVaddr(vaddr, ehdr, false);
                        ret.Name = ParseName(r, otn.NameExp);

                        return ret;
                    }
                }

                return vaddr;
            }
            catch (ParseException)
            {
                throw;
            }
            catch (Exception)
            {
                return vaddr;
            }
            finally
            {
                ehdr.r.BaseStream.Seek(old_pos, System.IO.SeekOrigin.Begin);
            }
        }

        public class ParseException : Exception
        { public ParseException(string msg) : base(msg) { } }

        private static string ParseName(MetadataObject obj, string NameExp)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder cur_item = new StringBuilder();
            bool in_item = false;

            for (int i = 0; i < NameExp.Length; i++)
            {
                char c = NameExp[i];

                if (c == '$')
                {
                    if (in_item)
                        throw new ParseException("Nested items not allowed");
                    cur_item = new StringBuilder();
                    in_item = true;
                }
                else if (c == '{')
                {
                    if (!in_item)
                        sb.Append(c);
                }
                else if (c == '}')
                {
                    if (in_item)
                    {
                        string fname = cur_item.ToString();
                        if (!obj.Fields.ContainsKey(fname))
                        {
                            StringBuilder vf_sb = new StringBuilder();
                            int f_id = 0;
                            foreach (string field_name in obj.Fields.Keys)
                            {
                                if (f_id != 0)
                                    vf_sb.Append(", ");
                                vf_sb.Append(field_name);
                                f_id++;
                            }
                            string valid_fields = vf_sb.ToString();

                            throw new ParseException("Field name: " + fname + " not found within type " + obj.LayoutName + "." + Environment.NewLine + "Valid fields: " + valid_fields + ".");
                        }
                        object o = obj.Fields[fname];
                        if (o is string)
                            sb.Append((string)o);
                        else
                            throw new ParseException("Field name: " + fname + " is not of type string");
                        in_item = false;
                    }
                    else
                        sb.Append(c);
                }
                else if (in_item)
                {
                    cur_item.Append(c);
                }
                else
                {
                    sb.Append(c);
                }                
            }

            return sb.ToString();
        }

        public static MetadataObject ReadVaddr(ulong vaddr, Elf64Reader.ElfHeader ehdr)
        { return ReadVaddr(vaddr, ehdr, true); }

        public static MetadataObject ReadVaddr(ulong vaddr, Elf64Reader.ElfHeader ehdr, bool read_references)
        {
            if (cache.ContainsKey(vaddr))
                return cache[vaddr];

            MetadataObject ret = new MetadataObject();
            ret.Address = vaddr;
            ret.ehdr = ehdr;
            ret.file_offset = ehdr.VaddrToOffset(vaddr);
            ret.Read(read_references);

            if(read_references)
                cache[vaddr] = ret;
            return ret;
        }

        public static MetadataObject Read(SymbolTable.Symbol sym)
        {
            return ReadVaddr(sym.vaddr, sym.r);
        }
    }
}
