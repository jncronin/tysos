using System;
using System.Collections.Generic;
using System.Text;

namespace CMExpLib
{
    public class LayoutManager
    {
        public Dictionary<ulong, Layout> Layouts = new Dictionary<ulong, Layout>();

        public LayoutManager(libtysila.Assembler ass, SymbolTable stab)
        {
            LoadLayout(ass.GetTysosTypeLayout(), stab);
            LoadLayout(ass.GetTysosMethodLayout(), stab);
            LoadLayout(ass.GetTysosGenericTypeLayout(), stab);
            LoadLayout(ass.GetTysosGenericTypeDefinitionLayout(), stab);
            LoadLayout(ass.GetTysosFieldLayout(), stab);
            LoadLayout(ass.GetTysosAssemblyLayout(), stab);
        }

        Layout LoadLayout(libtysila.Layout l, SymbolTable stab)
        {
            Layout ret = new Layout();

            ret.Name = l.typeinfo_object_name;

            SymbolTable.Symbol s = stab.Symbols[l.typeinfo_object_name];
            ret.offset = s.offset;
            ret.vaddr = s.vaddr;

            foreach (libtysila.Layout.Field f in l.InstanceFields)
            {
                Layout.Field newf = new Layout.Field();

                int space_idx = f.name.IndexOf(' ');
                newf.FieldType = f.name.Substring(0, space_idx);
                newf.Name = f.name.Substring(space_idx + 1);
                newf.Offset = f.offset;
                newf.Length = f.size;
                newf.l = ret;

                newf.ftype = Layout.Field.FType.Value;

                foreach (libtysila.Metadata.CustomAttributeRow car in f.field.field.CustomAttrs)
                {
                    string caname = libtysila.Mangler2.MangleMethod(libtysila.Metadata.GetMTC(car.Type, new libtysila.Assembler.TypeToCompile(), null, stab.ass), stab.ass);
                    if (caname == "_ZX29NullTerminatedListOfAttributeM_0_7#2Ector_Rv_P2u1tW6System4Type")
                        newf.ftype = Layout.Field.FType.NTArray;
                }

                if (f.field.fsig.Type is libtysila.Signature.BaseType)
                {
                    libtysila.Signature.BaseType bt = f.field.fsig.Type as libtysila.Signature.BaseType;

                    switch (bt.Type)
                    {
                        case libtysila.BaseType_Type.Boolean:
                        case libtysila.BaseType_Type.Byte:
                        case libtysila.BaseType_Type.Char:
                        case libtysila.BaseType_Type.I:
                        case libtysila.BaseType_Type.I1:
                        case libtysila.BaseType_Type.I2:
                        case libtysila.BaseType_Type.I4:
                        case libtysila.BaseType_Type.I8:
                        case libtysila.BaseType_Type.Object:
                        case libtysila.BaseType_Type.R4:
                        case libtysila.BaseType_Type.R8:
                        case libtysila.BaseType_Type.String:
                        case libtysila.BaseType_Type.U:
                        case libtysila.BaseType_Type.U1:
                        case libtysila.BaseType_Type.U2:
                        case libtysila.BaseType_Type.U4:
                        case libtysila.BaseType_Type.U8:
                            newf.FieldType = bt.Type.ToString();
                            break;
                    }
                }

                ret.Fields.Add(newf);
            }

            Layouts.Add(ret.vaddr, ret);
            return ret;
        }
    }

    public class Layout
    {
        public string Name;

        public ulong offset;
        public ulong vaddr;

        public List<Field> Fields = new List<Field>();

        public class Field
        {
            public string Name;
            public int Offset;
            public int Length;

            public enum FType { Value, NTArray };
            public FType ftype;
            public string FieldType;

            public Layout l;
        }
    }

    
}
