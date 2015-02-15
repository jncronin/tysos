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

/* The Layout class is also responsible for determining the actual layout of the
 * TypeInfo, FieldInfo and MethodInfo structures in the resultant binary
 * 
 * The layout is as follows:
 * 
 * TypeInfoSymbol:      TypeInfoStructure
 *                      VTable Structure
 *                      MethodInfo List
 *                      FieldInfo List
 *                      PropInfo List
 *                      EventInfo List
 *                      NestedTypes List
 *                      MethodInfos
 *                      FieldInfos
 *                      PropertyInfos
 *                      EventInfos
 *                      Sig_reference 0
 *                      Sig_reference 1
 *                      ...
 *                      
 * The layout of the VTable is:
 * 
 * VTableSymbol:        TypeInfo pointer
 *                      ITableMapPointer
 *                      Extends pointer to vtable
 *                      Virtual method 0
 *                      Virtual method 1
 *                      ...
 *                      Virtual method n
 * ITableMapPointer:    Interface 0 typeinfo pointer
 *  (the Interfaces     Interface 0 Methods pointer
 *  member also points  Interface 1 typeinfo pointer
 *  here)               Interface 1 Methods pointer
 *                      ...
 *                      Interface n typeinfo pointer
 *                      Interface n Methods pointer
 *                      IntPtr(0)   (null-terminate)
 * IFace0MethPtr:       Interface 0 method 0
 *                      Interface 0 method 1
 *                      ...
 *                      Interface 0 method n
 * IFace1MethPtr:       Interface 1 method 0
 *                      Interface 1 method 1
 *                      ...
 *                      Interface 1 method n
 *                      ...
 * IFaceNMethPtr:       Interface n method 0
 *                      Interface n method 1
 *                      ...
 *                      Interface n method n
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    partial class Layout
    {
        public const int ID_TypeInfoStructure = 0;
        public const int ID_VTableStructure = 1;
        public const int ID_MethodInfoList = 2;
        public const int ID_FieldInfoList = 3;
        public const int ID_PropInfoList = 4;
        public const int ID_EventInfoList = 5;
        public const int ID_NestedTypesList = 6;
        public const int ID_MethodInfos = 7;
        public const int ID_FieldInfos = 8;
        public const int ID_PropertyInfos = 9;
        public const int ID_EventInfos = 10;
        public const int ID_EHList = 11;
        public const int ID_EHClauses = 12;
        public const int ID_Sig_references = 13;
        public const int ID_GenericTypeParams = 14;
        public const int ID_Signatures = 15;

        public class LayoutEntry
        {
            internal enum LayoutEntryType { Symbol, Const, Relocation, InternalReference };
            internal LayoutEntryType Type;
            internal string SymbolName;
            internal byte[] Const;
            internal int OffsetWithinStructure;
            internal int OverallOffset;
            internal bool SymbolIsExternal = true;
            internal bool SymbolIsWeak = false;

            internal Layout _layout;

            internal class RelocationType
            {
                internal string name;
                internal uint rel_type;
                internal long value;
            }

            internal RelocationType Relocation;

            internal class InternalReferenceType
            {
                internal int Id;
                internal int Offset;
                internal int EntryNum;
            }

            internal InternalReferenceType InternalReference;

            internal int Length
            {
                get
                {
                    switch (Type)
                    {
                        case LayoutEntryType.Const:
                            return Const.Length;
                        case LayoutEntryType.InternalReference:
                        case LayoutEntryType.Relocation:
                            return _layout._ass.GetSizeOfPointer();
                        case LayoutEntryType.Symbol:
                            return 0;
                        default:
                            return 0;
                    }
                }
            }
        }

        public class StructureLayout
        {
            public int Id;
            public int Offset;
            public int PredefinedLength;
            public List<LayoutEntry> Entries = new List<LayoutEntry>();
        }

        public StructureLayout[] FixedLayout = new StructureLayout[ID_Signatures];
        public List<StructureLayout> Signatures = new List<StructureLayout>();
        public Dictionary<string, int> Symbols;

        public IList<StructureLayout> TypeInfoLayout
        {
            get
            {
                List<StructureLayout> ret = new List<StructureLayout>(FixedLayout);
                ret.AddRange(Signatures);
                return ret;
            }
        }

        static Dictionary<Assembler.TypeToCompile, Layout> ti_layout_cache = new Dictionary<Assembler.TypeToCompile, Layout>();
        static Dictionary<Assembler.TypeToCompile, Layout> ti_layout_cache_no_eh = new Dictionary<Assembler.TypeToCompile, Layout>();

        /*public static Layout GetTypeInfoLayout(Assembler.TypeToCompile ttc, Assembler ass)
        { return GetTypeInfoLayout(ttc, ass, true); } */
        public static Layout GetTypeInfoLayout(Assembler.TypeToCompile ttc, Assembler ass, bool do_eh_clauses)
        { return GetTypeInfoLayout(ttc, ass, do_eh_clauses, ass.Options.EnableRTTI); }
        public static Layout GetTypeInfoLayout(Assembler.TypeToCompile ttc, Assembler ass, bool do_eh_clauses, bool request_types)
        {
            if (ti_layout_cache.ContainsKey(ttc))
                return ti_layout_cache[ttc];
            //if (layout_cache.ContainsKey(ttc))
                //return layout_cache[ttc];
            if (!do_eh_clauses && ti_layout_cache_no_eh.ContainsKey(ttc))
                return ti_layout_cache_no_eh[ttc];

            Layout l = Layout.GetLayout(ttc, ass, request_types);
            l.CreateTypeInfoLayout(ttc, ass, do_eh_clauses);

            if (do_eh_clauses)
                ti_layout_cache.Add(ttc, l);
            else
                ti_layout_cache_no_eh.Add(ttc, l);

            return l;
        }

        enum TypeInfoType { NonGeneric, GenericType, GenericTypeDefinition, Array };

        StructureLayout CreateVTableLayout(Assembler.TypeToCompile ttc, Assembler ass, ref int itablemap_ptr, bool is_instantiable,
            bool vtable_only)
        {
            // Now layout the VTable
            StructureLayout vt_layout = new StructureLayout();
            vt_layout.Id = ID_VTableStructure;
            FixedLayout[ID_VTableStructure] = vt_layout;
            int vt_offset = 0;

            // Add in any VTableAliases
            foreach (Metadata.CustomAttributeRow car in ttc.type.CustomAttrs)
            {
                Assembler.MethodToCompile camtc = Metadata.GetMTC(car.Type, new Assembler.TypeToCompile(), null, ass);

                string caname = Mangler2.MangleMethod(camtc, ass);

                if ((caname == "_ZX20VTableAliasAttributeM_0_7#2Ector_Rv_P2u1tu1S") & ttc.tsig.Type.IsConcreteType)
                {
                    if (car.Value[0] != 0x01)
                        throw new NotSupportedException();
                    if (car.Value[1] != 0x00)
                        throw new NotSupportedException();
                    int ca_offset = 2;
                    int len = Metadata.ReadCompressedInteger(car.Value, ref ca_offset);
                    string s = new UTF8Encoding().GetString(car.Value, ca_offset, len);
                    vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Symbol, 
                        OffsetWithinStructure = vt_offset, SymbolName = s, SymbolIsWeak = ttc.tsig.Type.IsWeakLinkage });
                }
            }

            // Add the typeinfo pointer
            if (ass.Options.EnableRTTI)
                vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = vt_offset, InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_TypeInfoStructure } });
            else
                vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = vt_offset, Const = ass.IntPtrByteArray(0) });
            vt_offset += ass.GetSizeOfPointer();

            // Store the address of the itablemap pointer for later
            int itablemap_pointer_offset = vt_offset;
            vt_offset += ass.GetSizeOfPointer();

            // Extends vtbl pointer
            if (ttc.type.Extends.Value != null)
            {
                Assembler.TypeToCompile extends_ttc = Metadata.GetTTC(ttc.type.Extends, ttc, ass);
                ass.Requestor.RequestTypeInfo(extends_ttc);
                Layout extends_l = Layout.GetLayout(extends_ttc, ass);
                extends_l.CreateTypeInfoLayout(extends_ttc, ass, false);
                vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(extends_ttc, ass), rel_type = ass.DataToDataRelocType(), value = extends_l.FixedLayout[Layout.ID_VTableStructure].Offset } });
            }
            else
                vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, Const = ass.IntPtrByteArray(0) });
            vt_offset += ass.GetSizeOfPointer();

            if (is_instantiable && ((implflags & libsupcs.TysosType.IF_GTD) == 0))
            {
                // Write out the virtual methods
                foreach (Layout.Method vm in VirtualMethods)
                {
                    vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = new LayoutEntry.RelocationType { name = vm.implementation, rel_type = ass.DataToDataRelocType(), value = 0 } });
                    vt_offset += ass.GetSizeOfPointer();

                    if (vm.implementation_mtc.HasValue)
                        ass.Requestor.RequestMethod(vm.implementation_mtc.Value);
                }
            }
            else if ((implflags & libsupcs.TysosType.IF_GTD) == libsupcs.TysosType.IF_GTD)
            {
                // For generic type definitions, we just insert a link to the method info for the
                //  uninstantiated method
                foreach (Layout.Method vm in VirtualMethods)
                {
                    // If this is a generic type definition for an interface, then the implementation will always be pure virtual
                    if (!vm.implementation_mtc.HasValue)
                        vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = vt_offset, Const = ass.IntPtrByteArray(0) });
                    else
                    {
                        LayoutEntry.RelocationType gmi_rel = GetGMIRel(vm.implementation_mtc.Value, ass);
                        vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = gmi_rel });

                        // TODO: check if the following is necessary
                        ass.Requestor.RequestGenericMethodInfo(vm.implementation_mtc.Value);
                    }
                    vt_offset += ass.GetSizeOfPointer();

                }
            }

            // Now point the itablemap pointer to here
            vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = itablemap_pointer_offset, InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_VTableStructure, Offset = vt_offset } });

            // Point the type info interface pointer to here
            itablemap_ptr = vt_offset;
            //ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Interfaces"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_VTableStructure, Offset = vt_offset } });

            List<int> interface_methodlistptr_offsets = new List<int>();
            if (is_instantiable && ((implflags & libsupcs.TysosType.IF_GTD) == 0))
            {
                // Write out the interfaces list
                foreach (Layout.Interface iface in Interfaces)
                {
                    vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = new LayoutEntry.RelocationType { name = iface.typeinfo_name, rel_type = ass.DataToDataRelocType(), value = 0 } });
                    vt_offset += ass.GetSizeOfPointer();
                    interface_methodlistptr_offsets.Add(vt_offset);
                    vt_offset += ass.GetSizeOfPointer();

                    ass.Requestor.RequestTypeInfo(iface.iface);
                }
            }
            else if ((implflags & libsupcs.TysosType.IF_GTD) == libsupcs.TysosType.IF_GTD)
            {
                // For GTDs, write out a list of the uninstantiated interface typeinfos
                foreach (Layout.Interface iface in Interfaces)
                {
                    vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = new LayoutEntry.RelocationType { name = iface.typeinfo_name, rel_type = ass.DataToDataRelocType(), value = 0 } });
                    vt_offset += ass.GetSizeOfPointer();
                    interface_methodlistptr_offsets.Add(vt_offset);
                    vt_offset += ass.GetSizeOfPointer();

                    ass.Requestor.RequestTypeInfo(iface.iface);
                }
            }

            // Null terminate the list
            vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = vt_offset, Const = ass.IntPtrByteArray(0) });
            vt_offset += ass.GetSizeOfPointer();

            if (is_instantiable && ((implflags & libsupcs.TysosType.IF_GTD) == 0))
            {
                // Now write out the actual interface method implementations
                for (int idx = 0; idx < Interfaces.Count; idx++)
                {
                    // First point the interface methodlist pointer to here
                    vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = interface_methodlistptr_offsets[idx], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_VTableStructure, Offset = vt_offset } });

                    // Now write out the methods
                    foreach (Layout.Method im in Interfaces[idx].methods)
                    {
                        vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = new LayoutEntry.RelocationType { name = im.implementation, rel_type = ass.DataToDataRelocType(), value = 0 } });
                        vt_offset += ass.GetSizeOfPointer();

                        if (im.implementation_mtc.HasValue)
                            ass.Requestor.RequestMethod(im.implementation_mtc.Value);
                    }
                }
            }
            else if ((implflags & libsupcs.TysosType.IF_GTD) == libsupcs.TysosType.IF_GTD)
            {
                // write out the interface methods as pointers to method info structures
                //  see the vtable layout above for the idea
                for (int idx = 0; idx < Interfaces.Count; idx++)
                {
                    // First point the interface methodlist pointer to here
                    vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = interface_methodlistptr_offsets[idx], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_VTableStructure, Offset = vt_offset } });

                    // Now write out a list of method info pointers
                    foreach (Layout.Method im in Interfaces[idx].methods)
                    {
                        if (im.implementation_mtc.HasValue)
                        {
                            // TODO: see if the following is required
                            ass.Requestor.RequestGenericMethodInfo(im.implementation_mtc.Value);
                            LayoutEntry.RelocationType gmi_rel = GetGMIRel(im.implementation_mtc.Value, ass);
                            vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = vt_offset, Relocation = gmi_rel });
                        }
                        else
                            vt_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = vt_offset, Const = ass.IntPtrByteArray(0) });

                        vt_offset += ass.GetSizeOfPointer();
                    }
                }
            }

            // Now write out its size
            vt_layout.PredefinedLength = vt_offset;

            return vt_layout;
        }

        /* This function creates the actual type info layout */
        void CreateTypeInfoLayout(Assembler.TypeToCompile ttc, Assembler ass, bool do_eh_clauses)
        {
            /* First create the typeinfo layout */
            StructureLayout ti_layout = new StructureLayout();
            FixedLayout[ID_TypeInfoStructure] = ti_layout;
            Layout ti_l = ass.GetTysosTypeLayout();
            ti_layout.Id = ID_TypeInfoStructure;
            ti_layout.PredefinedLength = ti_l.ClassSize;
            if (ass.Options.EnableRTTI == false)
                ti_layout.PredefinedLength = 2 * ass.GetSizeOfPointer(); // vtbl + obj_id (aligned to multiple of ptr size)
            TypeInfoType ti_type = TypeInfoType.NonGeneric;
            bool is_instantiable = true;
            if ((this.implflags & libsupcs.TysosType.IF_GT) != 0)
            {
                ti_type = TypeInfoType.GenericType;
                ti_l = ass.GetTysosGenericTypeLayout();
                ti_layout.PredefinedLength = ti_l.ClassSize;
            }
            else if ((this.implflags & libsupcs.TysosType.IF_GTD) != 0)
            {
                ti_type = TypeInfoType.GenericTypeDefinition;
                ti_l = ass.GetTysosGenericTypeDefinitionLayout();
                ti_layout.PredefinedLength = ti_l.ClassSize;
            }
            else if (ttc.tsig.Type is Signature.BaseArray)
            {
                ti_type = TypeInfoType.Array;
                ti_l = ass.GetTysosArrayTypeLayout();
                ti_layout.PredefinedLength = ti_l.ClassSize;
            }

            //bool is_instantiable = IsInstantiable(ttc.tsig.Type, ass, true, false);
            if ((implflags & libsupcs.TysosType.IF_TYPE_MASK) == libsupcs.TysosType.IF_MPTR)
                is_instantiable = false;
            if ((implflags & libsupcs.TysosType.IF_TYPE_MASK) == libsupcs.TysosType.IF_PTR)
                is_instantiable = false;
            if (ttc.tsig.Type.IsInstantiable == false)
                is_instantiable = false;
            //bool is_instantiable = (((implflags & libsupcs.TysosType.IF_TYPE_MASK) == 0) || ((implflags & libsupcs.TysosType.IF_TYPE_MASK) == libsupcs.TysosType.IF_ENUM));

            // TypeInfo symbol
            ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Symbol, OffsetWithinStructure = 0, 
                SymbolName = Mangler2.MangleTypeInfo(ttc, ass), SymbolIsWeak = ttc.tsig.Type.IsWeakLinkage });
            
            // __vtbl pointer
            string vtbl_name = "__tysos_type_vt";
            if (ti_type == TypeInfoType.GenericType)
                vtbl_name = "__tysos_gt_vt";
            else if (ti_type == TypeInfoType.GenericTypeDefinition)
                vtbl_name = "__tysos_gtd_vt";
            else if (ti_type == TypeInfoType.Array)
                vtbl_name = "__tysos_arraytype_vt";

            if (ass.Options.EnableRTTI)
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr __vtbl"], Relocation = new LayoutEntry.RelocationType { name = vtbl_name, rel_type = ass.DataToDataRelocType(), value = 0 } });
            else
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr __vtbl"], Const = ass.IntPtrByteArray(0) });

            // _obj_id
            ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = ti_l.InstanceFieldOffsets["Int32 __object_id"], Const = ass.ToByteArraySignExtend(ass.next_object_id.Increment, 4) });

            // typedef table for use in signatures
            Dictionary<Metadata.TypeDefRow, byte> typedef_table = new Dictionary<Metadata.TypeDefRow, byte>();
            byte next_typedef = 0;

            if (ass.Options.EnableRTTI)
            {
                // System.RuntimeTypeHandle _impl
                /* All types defined at compile time are 'system types'
                 * For these we can just set _impl to the typeinfo itself
                 * This makes System.Type.IsSystemType return true
                 * For types created at runtime by reflection we may need to do something different
                 */
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["System.RuntimeTypeHandle _impl"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_TypeInfoStructure } });

                // libsupcs.TysosType Extends
                if (ttc.type.Extends.Value != null)
                {
                    Assembler.TypeToCompile extends_ttc = Metadata.GetTTC(ttc.type.Extends, ttc, ass);
                    ass.Requestor.RequestTypeInfo(extends_ttc);
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["libsupcs.TysosType Extends"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(extends_ttc, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                }

                // string TypeName
                vara v_n = ttc.type.m.StringTable.GetStringAddress(ttc.type.TypeName, ass);
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["String TypeName"], Relocation = new LayoutEntry.RelocationType { name = v_n.LabelVal, rel_type = ass.DataToDataRelocType(), value = v_n.Offset } });

                // string TypeNamespace
                vara v_ns = ttc.type.m.StringTable.GetStringAddress(ttc.type.TypeNamespace, ass);
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["String TypeNamespace"], Relocation = new LayoutEntry.RelocationType { name = v_ns.LabelVal, rel_type = ass.DataToDataRelocType(), value = v_ns.Offset } });

                // System.Reflection.Assembly _Assembly
                if (ttc.type.m != null)
                {
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["System.Reflection.Assembly _Assembly"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleAssembly(ttc.type.m, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                    ass.Requestor.RequestAssembly(ttc.type.m);
                }

                // System.Reflection.Module _Module
                if (ttc.type.m != null)
                {
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["System.Reflection.Module _Module"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleModule(ttc.type.m, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                    ass.Requestor.RequestModule(ttc.type.m);
                }

                // IntPtr UnboxedType
                if (unboxed_typeinfo.HasValue)
                {
                    if (unboxed_typeinfo.Value.Equals(ttc))
                        ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr UnboxedType"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_TypeInfoStructure } });
                    else
                    {
                        int ubt_ti_offset = 0;
                        ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr UnboxedType"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(unboxed_typeinfo.Value, ass), rel_type = ass.DataToDataRelocType(), value = ubt_ti_offset } });
                        ass.Requestor.RequestTypeInfo(unboxed_typeinfo.Value);
                    }
                }

                // IntPtr VTable
                if (boxed_typeinfo.HasValue)
                {
                    if (boxed_typeinfo.Value.Equals(ttc))
                        ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr VTable"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_VTableStructure } });
                    else
                    {
                        Layout bt_l = Layout.GetTypeInfoLayout(boxed_typeinfo.Value, ass, do_eh_clauses);
                        int bt_vt_offset = bt_l.FixedLayout[ID_VTableStructure].Offset;
                        ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr VTable"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(boxed_typeinfo.Value, ass), rel_type = ass.DataToDataRelocType(), value = bt_vt_offset } });
                        ass.Requestor.RequestTypeInfo(boxed_typeinfo.Value);
                    }
                }


                // Int32 Flags
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = ti_l.InstanceFieldOffsets["Int32 Flags"], Const = ass.ToByteArrayZeroExtend(ttc.type.Flags, 4) });

                // Int32 ImplFlags
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = ti_l.InstanceFieldOffsets["UInt32 ImplFlags"], Const = ass.ToByteArrayZeroExtend(implflags, 4) });

                // Int32 ClassSize
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = ti_l.InstanceFieldOffsets["Int32 ClassSize"], Const = ass.ToByteArrayZeroExtend(ClassSize, 4) });

                // IntPtr Signature
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Signature"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_Signatures, EntryNum = Signatures.Count } });
                Signatures.Add(write_ti_signature(ttc, ttc.tsig.Type, typedef_table, ref next_typedef));

                // IntPtr Sig_references
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Sig_references"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_Sig_references } });


                if (is_instantiable)
                {
                    // IntPtr Fields
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Fields"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_FieldInfoList } });

                    // IntPtr Methods
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Methods"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_MethodInfoList } });

                    // IntPtr Events
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Events"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_EventInfoList } });

                    // IntPtr Properties
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Properties"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_PropInfoList } });

                    // IntPtr NestedTypes
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr NestedTypes"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_NestedTypesList } });
                }

                if (ti_type == TypeInfoType.Array)
                {
                    Signature.BaseArray ba = ttc.tsig.Type as Signature.BaseArray;
                    string et_name = Mangler2.MangleTypeInfo(Metadata.GetTTC(new Signature.Param(ba.ElemType, ass), ttc, null, ass), ass);
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["libsupcs.TysosType elemtype"], Relocation = new LayoutEntry.RelocationType { name = et_name, rel_type = ass.DataToDataRelocType(), value = 0 } });
                }
            }
            
            // Now layout the VTable
            int itablemap_ptr = 0;
            StructureLayout vt_layout = CreateVTableLayout(ttc, ass, ref itablemap_ptr, is_instantiable, false);

            if (ass.Options.EnableRTTI)
            {
                // Point the type info interface pointer to its location in the vtable
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr Interfaces"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_VTableStructure, Offset = itablemap_ptr } });

                // Now write out its size
                ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = ti_l.InstanceFieldOffsets["IntPtr VTableLength"], Const = ass.IntPtrByteArray(vt_layout.PredefinedLength) });

                // Generic type
                if (ti_type == TypeInfoType.GenericType)
                {
                    // UIntPtr _GParams
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = ti_l.InstanceFieldOffsets["UIntPtr _GParams"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_GenericTypeParams } });
                    StructureLayout gparams_layout = new StructureLayout();
                    gparams_layout.Id = ID_GenericTypeParams;
                    FixedLayout[ID_GenericTypeParams] = gparams_layout;

                    Signature.GenericType gt = this._ttc.tsig.Type as Signature.GenericType;
                    if (gt == null)
                        throw new Exception("Not a valid generic type");

                    int gp_offset = 0;
                    foreach (Signature.BaseOrComplexType gp in gt.GenParams)
                    {
                        Assembler.TypeToCompile param_ttc = new Assembler.TypeToCompile(gp, ass);
                        string param_type = Mangler2.MangleTypeInfo(param_ttc, ass);
                        gparams_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = gp_offset, Relocation = new LayoutEntry.RelocationType { name = param_type, rel_type = ass.DataToDataRelocType(), value = 0 } });
                        gp_offset += ass.GetSizeOfPointer();
                        ass.Requestor.RequestTypeInfo(param_ttc);
                    }
                    // Null terminate
                    gparams_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = gp_offset, Const = ass.IntPtrByteArray(0) });
                    gp_offset += ass.GetSizeOfPointer();

                    gparams_layout.PredefinedLength = gp_offset;

                    // TysosGenericTypeDefinition GenericTypeDefintion
                    ti_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = ti_l.InstanceFieldOffsets["libsupcs.TysosGenericTypeDefinition GenericTypeDefinition"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(new Assembler.TypeToCompile(gt.GenType, ass), ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                    ass.Requestor.RequestTypeInfo(new Assembler.TypeToCompile(gt.GenType, ass));
                }

                if (is_instantiable)
                {
                    // Now layout the Field list
                    StructureLayout fi_list_layout = new StructureLayout();
                    fi_list_layout.Id = ID_FieldInfoList;
                    FixedLayout[ID_FieldInfoList] = fi_list_layout;

                    StructureLayout fi_layout = new StructureLayout();
                    fi_layout.Id = ID_FieldInfos;
                    FixedLayout[ID_FieldInfos] = fi_layout;

                    Layout fi_l = ass.GetTysosFieldLayout();

                    int fi_list_offset = 0;
                    int fi_offset = 0;

                    List<Field> fields = new List<Field>();
                    foreach (Field f in InstanceFields)
                        fields.Add(f);
                    foreach (Field f in StaticFields)
                        fields.Add(f);
                    foreach (Layout.Field f in fields)
                    {
                        if (f.field.field.RuntimeInternal || (Metadata.GetOwningType(f.field.field.m, f.field.field) == ttc.type))
                        {
                            // First write an entry in the field info list
                            fi_list_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = fi_list_offset, InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_FieldInfos, Offset = fi_offset } });
                            fi_list_offset += ass.GetSizeOfPointer();

                            // Now write out the field info itself
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Symbol,
                                OffsetWithinStructure = fi_offset, SymbolName = Mangler2.MangleFieldInfoSymbol(f.field, ass),
                                SymbolIsExternal = false, SymbolIsWeak = ttc.tsig.Type.IsWeakLinkage });

                            // __vtbl pointer
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["IntPtr __vtbl"], Relocation = new LayoutEntry.RelocationType { name = "__tysos_field_vt", rel_type = ass.DataToDataRelocType(), value = 0 } });

                            // __obj_id
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["Int32 __object_id"], Const = ass.ToByteArraySignExtend(ass.next_object_id.Increment, 4) });

                            // libsupcs.TysosType OwningType
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["libsupcs.TysosType OwningType"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_TypeInfoStructure } });

                            // libsupcs.TysosType _FieldType
                            Assembler.TypeToCompile field_type = Metadata.GetTTC(f.field.fsig, f.field.DefinedIn, null, ass);
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["libsupcs.TysosType _FieldType"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(field_type, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                            ass.Requestor.RequestTypeInfo(field_type);

                            // String _Name
                            vara v_nf = f.field.DefinedIn.type.m.StringTable.GetStringAddress(f.field.field.Name, ass);
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["String _Name"], Relocation = new LayoutEntry.RelocationType { name = v_nf.LabelVal, rel_type = ass.DataToDataRelocType(), value = v_nf.Offset } });

                            // UInt32 Flags
                            uint flags = f.field.field.Flags;
                            if (f.field.field.RuntimeInternal)
                                flags |= libsupcs.TysosField.IF_RUNTIME_INTERNAL;
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["UInt32 Flags"], Const = ass.ToByteArrayZeroExtend(flags, 4) });

                            // Int32 offset
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["Int32 offset"], Const = ass.ToByteArrayZeroExtend(f.offset, 4) });

                            // IntPtr Signature
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["IntPtr Signature"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_Signatures, EntryNum = Signatures.Count } });
                            Signatures.Add(write_ti_signature(field_type, field_type.tsig.Type, typedef_table, ref next_typedef));

                            // IntPtr Sig_references
                            fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["IntPtr Sig_references"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_Sig_references } });

                            // Literal and constant data
                            int cur_fi_offset = fi_offset;
                            cur_fi_offset += fi_l.ClassSize;

                            if (f.field.field.LiteralData != null)
                            {
                                fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["IntPtr Literal_data"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_FieldInfos, Offset = cur_fi_offset } });
                                fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = cur_fi_offset, Const = f.field.field.LiteralData });
                                cur_fi_offset += f.field.field.LiteralData.Length;
                            }
                            if (f.field.field.Constant != null)
                            {
                                fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = fi_offset + fi_l.InstanceFieldOffsets["IntPtr Constant_data"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_FieldInfos, Offset = cur_fi_offset } });
                                fi_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = cur_fi_offset, Const = f.field.field.Constant.Value });
                                cur_fi_offset += f.field.field.Constant.Value.Length;
                            }

                            fi_offset = cur_fi_offset;
                        }
                    }

                    // Null terminate the field info list
                    fi_list_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = fi_list_offset, Const = ass.IntPtrByteArray(0) });
                    fi_list_offset += ass.GetSizeOfPointer();

                    // Set the lengths of the structures
                    fi_layout.PredefinedLength = fi_offset;
                    fi_list_layout.PredefinedLength = fi_list_offset;


                    // Now layout the method list
                    StructureLayout mi_list_layout = new StructureLayout();
                    mi_list_layout.Id = ID_MethodInfoList;
                    FixedLayout[ID_MethodInfoList] = mi_list_layout;

                    StructureLayout mi_layout = new StructureLayout();
                    mi_layout.Id = ID_MethodInfos;
                    FixedLayout[ID_MethodInfos] = mi_layout;

                    StructureLayout eh_list_layout = new StructureLayout();
                    eh_list_layout.Id = ID_EHList;
                    FixedLayout[ID_EHList] = eh_list_layout;

                    StructureLayout eh_layout = new StructureLayout();
                    eh_layout.Id = ID_EHClauses;
                    FixedLayout[ID_EHClauses] = eh_layout;

                    Layout mi_l = ass.GetTysosMethodLayout();
                    Layout eh_l = ass.GetEHClausesLayout();

                    int mi_list_offset = 0;
                    int mi_offset = 0;
                    int eh_list_offset = 0;
                    int eh_offset = 0;

                    Metadata.TableIndex first_method = ttc.type.MethodList;
                    Metadata.TableIndex last_method = Metadata.GetLastMethod(ttc.type);

                    IEnumerable<Assembler.MethodToCompile> meths = GetMethods(ttc, ass);
                    foreach (Assembler.MethodToCompile mtc in meths)
                    {
                        /* Do not write out special methods */
                        if (mtc.meth.CustomAttributes != null)
                        {
                            if (mtc.meth.CustomAttributes.ContainsKey("libsupcs.ReinterpretAsMethod"))
                                continue;
                        }
                        // First write an entry in the method info list
                        mi_list_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = mi_list_offset, InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_MethodInfos, Offset = mi_offset } });
                        mi_list_offset += ass.GetSizeOfPointer();

                        //Assembler.MethodToCompile mtc = new Assembler.MethodToCompile { _ass = ass, meth = mdr, msig = mdr.ActualSignature, tsigp = ttc.tsig, type = ttc.type };
                        //mtc.msig = Signature.ResolveGenericMember(mtc.msig, ttc.tsig.Type, null, ass);
                        CreateMethodInfoLayout(mtc, mi_layout, ref mi_offset, eh_layout, ref eh_offset, eh_list_layout, ref eh_list_offset, ass, this, do_eh_clauses, true);
                    }

                    // Null terminate the mi_list
                    mi_list_layout.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = mi_list_offset, Const = ass.IntPtrByteArray(0) });
                    mi_list_offset += ass.GetSizeOfPointer();

                    // Set the lengths of the structures
                    mi_list_layout.PredefinedLength = mi_list_offset;
                    mi_layout.PredefinedLength = mi_offset;
                    eh_list_layout.PredefinedLength = eh_list_offset;
                    eh_layout.PredefinedLength = eh_offset;


                    // TODO Properties list


                    // TODO Events list


                    // TODO Nested Types list
                }
            }

            LayoutStructs();
        }

        void LayoutStructs()
        {
            // Now work out the total lengths of the info structures and their individual offsets
            int offset = 0;
            List<StructureLayout> all_structs = new List<StructureLayout>();
            for (int i = 0; i < ID_Signatures; i++)
            {
                if (FixedLayout[i] == null)
                    FixedLayout[i] = new StructureLayout { Id = i, PredefinedLength = 0 };

                FixedLayout[i].Offset = offset;
                offset += FixedLayout[i].PredefinedLength;
                all_structs.Add(FixedLayout[i]);
            }
            foreach (StructureLayout sl in Signatures)
            {
                sl.Offset = offset;
                offset += sl.PredefinedLength;
                all_structs.Add(sl);
            }


            // Compile a list of all the objects in this typeinfo
            Symbols = new Dictionary<string, int>();
            foreach (StructureLayout sl in all_structs)
            {
                int sl_offset = sl.Offset;

                foreach (LayoutEntry le in sl.Entries)
                {
                    int le_offset = sl_offset + le.OffsetWithinStructure;
                    le.OverallOffset = le_offset;

                    if (le.Type == LayoutEntry.LayoutEntryType.Symbol)
                        Symbols.Add(le.SymbolName, le_offset);
                }
            }
        }

        internal static LayoutEntry.RelocationType GetGMIRel(Assembler.MethodToCompile mtc, Assembler ass)
        {
            /* Return a relocation to the method info for a particular method.
             * 
             * If the method is defined on a generic type definition, then return a MI pointer,
             * else return the TI it is defined on + relevant offset
             */

            if (mtc.tsig.IsInstantiable)
            {
                Layout l = Layout.GetLayout(mtc.GetTTC(ass), ass);
                int meth_offset = l.GetVirtualMethod(mtc).offset;
                //int meth_offset = Layout.GetSymbolOffset(Mangler2.MangleMethodInfoSymbol(mtc, ass), mtc.GetTTC(ass), ass);
                return new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(mtc.GetTTC(ass), ass), rel_type = ass.DataToDataRelocType(), value = meth_offset };
            }
            else
                return new LayoutEntry.RelocationType { name = Mangler2.MangleMethodInfoSymbol(mtc, ass), rel_type = ass.DataToDataRelocType(), value = 0 };
        }

        private IEnumerable<Assembler.MethodToCompile> GetMethods(Assembler.TypeToCompile ttc, Assembler ass)
        {
            return GetMethods(ttc.tsig.Type, ttc.tsig.Type, ass);
        }

        private IEnumerable<Assembler.MethodToCompile> GetMethods(Signature.BaseOrComplexType tsig, Signature.BaseOrComplexType parent, Assembler ass)
        {
            List<Assembler.MethodToCompile> ret = new List<Assembler.MethodToCompile>();

            if (tsig is Signature.ManagedPointer)
            {
                Signature.ManagedPointer mp = tsig as Signature.ManagedPointer;
                if (mp.ElemType.IsValueType(ass))
                    return GetMethods(Metadata.GetTypeDef(mp.ElemType, ass), tsig, ass);
                else
                    return ret;
            }
            else if (tsig is Signature.BoxedType)
            {
                Signature.BoxedType bt = tsig as Signature.BoxedType;
                if (bt.Type.IsValueType(ass))
                    return GetMethods(Metadata.GetTypeDef(bt.Type, ass), tsig, ass);
                else
                    return ret;
            }
            /*else if (tsig.IsValueType(ass))
                return ret;*/
            else
                return GetMethods(Metadata.GetTypeDef(tsig, ass), tsig, ass);
        }

        private IEnumerable<Assembler.MethodToCompile> GetMethods(Metadata.TypeDefRow typeDefRow, Signature.BaseOrComplexType tsig, Assembler ass)
        {
            List<Assembler.MethodToCompile> ret = new List<Assembler.MethodToCompile>();

            foreach (Metadata.MethodDefRow mdr in typeDefRow.Methods)
            {               
                Assembler.MethodToCompile mtc = new Assembler.MethodToCompile { _ass = ass, type = typeDefRow, tsigp = new Signature.Param(tsig, ass), meth = mdr, msig = mdr.ActualSignature };
                mtc.msig = Signature.ResolveGenericMember(mtc.msig, tsig, null, ass);
                mtc.msig.Method.meth = mdr;
                ret.Add(mtc);
            }

            return ret;
        }

        internal static IList<StructureLayout> GetMethodInfoLayout(Assembler.MethodToCompile mtc, Assembler ass, bool do_meth_address)
        {
            StructureLayout[] sls = new StructureLayout[ID_Signatures];
            for (int i = 0; i < ID_Signatures; i++)
                sls[i] = new StructureLayout { Id = i, PredefinedLength = 0 };

            int mi_offset = 0;
            int eh_offset = 0;
            int eh_list_offset = 0;

            Layout l = new Layout();
            l._ass = ass;

            CreateMethodInfoLayout(mtc, sls[ID_MethodInfos], ref mi_offset, sls[ID_EHClauses], ref eh_offset, sls[ID_EHList], ref eh_list_offset, ass, l, true, do_meth_address);

            sls[ID_MethodInfos].PredefinedLength = mi_offset;
            sls[ID_EHClauses].PredefinedLength = eh_offset;
            sls[ID_EHList].PredefinedLength = eh_list_offset;

            int offset = 0;
            for (int i = 0; i < ID_Signatures; i++)
            {
                sls[i].Offset = offset;
                offset += sls[i].PredefinedLength;
            }

            bool is_weak = false;
            if (mtc.msig is Signature.GenericMethod)
                is_weak = true;
            if (mtc.tsig.IsWeakLinkage)
                is_weak = true;

            is_weak = true;
            sls[0].Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Symbol,
                SymbolName = Mangler2.MangleMethodInfoSymbol(mtc, ass), SymbolIsWeak = is_weak });

            return sls;
        }

        private static void CreateMethodInfoLayout(Assembler.MethodToCompile mtc, StructureLayout mi_layout, ref int mi_offset, StructureLayout eh_layout, ref int eh_offset,
            StructureLayout eh_list_layout, ref int eh_list_offset, Assembler ass, Layout l, bool do_eh_clauses, bool do_meth_address)
        {
            bool is_instantiable = false;
            if(do_eh_clauses || do_meth_address)
                is_instantiable = IsInstantiable(mtc, ass);
            Layout mi_l = ass.GetTysosMethodLayout();
            Layout eh_l = ass.GetEHClausesLayout();

            // Now write out the method info itself
            bool is_weak = false;
            if (mtc.msig is Signature.GenericMethod)
                is_weak = true;
            if (mtc.tsig.IsWeakLinkage)
                is_weak = true;
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Symbol, OffsetWithinStructure = mi_offset,
                SymbolName = Mangler2.MangleMethodInfoSymbol(mtc, ass), SymbolIsExternal = false, SymbolIsWeak = is_weak });

            // __vtbl pointer
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["IntPtr __vtbl"], Relocation = new LayoutEntry.RelocationType { name = "__tysos_method_vt", rel_type = ass.DataToDataRelocType(), value = 0 } });

            // __obj_id
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["Int32 __object_id"], Const = ass.ToByteArraySignExtend(ass.next_object_id.Increment, 4) });

            // libsupcs.TysosType OwningType
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["libsupcs.TysosType OwningType"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_TypeInfoStructure } });

            // libsupcs.TysosType _ReturnType
            Assembler.TypeToCompile return_type = Metadata.GetTTC(mtc.msig.Method.RetType, mtc.GetTTC(ass), null, ass);
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["libsupcs.TysosType _ReturnType"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(return_type, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
            ass.Requestor.RequestTypeInfo(return_type);

            // IntPtr _Params
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["IntPtr _Params"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_EHList, Offset = eh_list_offset } });
            foreach (Signature.Param p in mtc.msig.Method.Params)
            {
                Assembler.TypeToCompile p_ttc = Metadata.GetTTC(p, mtc.GetTTC(ass), mtc.msig, ass);
                string p_mangled_name = Mangler2.MangleTypeInfo(p_ttc, ass);
                eh_list_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = eh_list_offset, Relocation = new LayoutEntry.RelocationType { name = p_mangled_name, rel_type = ass.DataToDataRelocType() } });
                eh_list_offset += ass.GetSizeOfPointer();
                ass.Requestor.RequestTypeInfo(p_ttc);
            }
            eh_list_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_list_offset, Const = ass.IntPtrByteArray(0) });
            eh_list_offset += ass.GetSizeOfPointer();

            // String _Name
            vara v_nm = mtc.type.m.StringTable.GetStringAddress(mtc.meth.Name, ass);
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["String _Name"], Relocation = new LayoutEntry.RelocationType { name = v_nm.LabelVal, rel_type = ass.DataToDataRelocType(), value = v_nm.Offset } });

            // String _MangledName
            vara v_nmn = mtc.type.m.StringTable.GetStringAddress(Mangler2.MangleMethod(mtc, ass), ass);
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["String _MangledName"], Relocation = new LayoutEntry.RelocationType { name = v_nmn.LabelVal, rel_type = ass.DataToDataRelocType(), value = v_nmn.Offset } });

            // Int32 Flags
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["Int32 Flags"], Const = ass.ToByteArrayZeroExtend(mtc.meth.Flags, 4) });

            // Int32 ImplFlags
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["Int32 ImplFlags"], Const = ass.ToByteArrayZeroExtend(mtc.meth.ImplFlags, 4) });

            // UInt32 TysosFlags
            UInt32 tf = 0;
            tf |= GetTysosFlagsForMethod(mtc);
            tf |= ass.GetTysosFlagsForMethod(mtc);
            mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["UInt32 TysosFlags"], Const = ass.ToByteArrayZeroExtend(tf, 4) });

            if (mtc.meth.IgnoreAttribute == true)
                is_instantiable = false;
            if (mtc.IsInstantiable == false)
                is_instantiable = false;
            if (is_instantiable && ((l.implflags & libsupcs.TysosType.IF_GTD) == 0) && do_meth_address)
            {
                // IntPtr MethodAddress
                mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["IntPtr MethodAddress"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleMethod(mtc, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                ass.Requestor.RequestMethod(mtc);

                if (do_eh_clauses && (mtc.meth.Body.exceptions.Count > 0))
                {
                    // Add in exception handler clauses
                    Assembler.AssembleBlockOutput abo = ass.AssembleMethod(mtc);

                    // IntPtr EHClauses
                    mi_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = mi_offset + mi_l.InstanceFieldOffsets["IntPtr EHClauses"], InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_EHList, Offset = eh_list_offset } });

                    foreach (Metadata.MethodBody.EHClause ehc in mtc.meth.Body.exceptions)
                    {
                        // First write out an entry in the EH list
                        eh_list_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.InternalReference, OffsetWithinStructure = eh_list_offset, InternalReference = new LayoutEntry.InternalReferenceType { Id = ID_EHClauses, Offset = eh_offset } });
                        eh_list_offset += ass.GetSizeOfPointer();

                        // IntPtr __vtbl
                        eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["IntPtr __vtbl"], Relocation = new LayoutEntry.RelocationType { name = "__tysos_ehclause_vt", rel_type = ass.DataToDataRelocType(), value = 0 } });

                        // Int32 __object_id
                        eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["Int32 __object_id"], Const = ass.ToByteArraySignExtend(ass.next_object_id.Increment, 4) });

                        // IntPtr TryStart
                        eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["IntPtr TryStart"], Const = ass.IntPtrByteArray(ass.get_compiled_offset((int)ehc.TryOffset, abo)) });

                        // IntPtr TryEnd
                        eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["IntPtr TryEnd"], Const = ass.IntPtrByteArray(ass.get_compiled_offset((int)(ehc.TryOffset + ehc.TryLength), abo)) });

                        // IntPtr Handler
                        eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["IntPtr Handler"], Const = ass.IntPtrByteArray(ass.get_compiled_offset((int)ehc.HandlerOffset, abo)) });

                        if (ehc.IsCatch)
                        {
                            // libsupcs.TysosType CatchObject
                            Assembler.TypeToCompile catch_ttc = Metadata.GetTTC(ehc.ClassToken, mtc.GetTTC(ass), mtc.msig, ass);
                            eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Relocation, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["libsupcs.TysosType CatchObject"], Relocation = new LayoutEntry.RelocationType { name = Mangler2.MangleTypeInfo(catch_ttc, ass), rel_type = ass.DataToDataRelocType(), value = 0 } });
                            ass.Requestor.RequestTypeInfo(catch_ttc);
                        }

                        // Int32 Flags
                        eh_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_offset + eh_l.InstanceFieldOffsets["Int32 Flags"], Const = ass.ToByteArrayZeroExtend(ehc.Flags, 4) });

                        // Increment eh_offset
                        eh_offset += eh_l.ClassSize;
                    }

                    // Null terminate the eh_list
                    eh_list_layout.Entries.Add(new LayoutEntry { _layout = l, Type = LayoutEntry.LayoutEntryType.Const, OffsetWithinStructure = eh_list_offset, Const = ass.IntPtrByteArray(0) });
                    eh_list_offset += ass.GetSizeOfPointer();
                }
            }

            // Increment mi_offset
            mi_offset += mi_l.ClassSize;
        }

        private static uint GetTysosFlagsForMethod(Assembler.MethodToCompile mtc)
        {
            uint ret = 0;
            if (mtc.msig.Method.CallingConvention == Signature.Method.CallConv.VarArg)
                ret = libsupcs.TysosMethod.TF_CC_VARARGS;
            else
                ret = libsupcs.TysosMethod.TF_CC_STANDARD;
            if (mtc.msig.Method.HasThis)
                ret |= libsupcs.TysosMethod.TF_CC_HASTHIS;
            if (mtc.msig.Method.ExplicitThis)
                ret |= libsupcs.TysosMethod.TF_CC_EXPLICITTHIS;
            return ret;
        }

        private StructureLayout write_ti_signature(Assembler.TypeToCompile containing_type, Signature.BaseOrComplexType cur_bct, Dictionary<Metadata.TypeDefRow, byte> typedef_table, ref byte next_typedef_id)
        {
            StructureLayout sl = new StructureLayout();
            List<byte> bytes = new List<byte>();
            write_ti_signature(bytes, containing_type, cur_bct, typedef_table, ref next_typedef_id, _ass);
            sl.Entries.Add(new LayoutEntry { _layout = this, Type = LayoutEntry.LayoutEntryType.Const, Const = bytes.ToArray() });
            sl.PredefinedLength = bytes.Count;
            return sl;
        }

        private void write_ti_signature(IList<byte> sig, Assembler.TypeToCompile containing_type, Signature.BaseOrComplexType cur_bct, Dictionary<Metadata.TypeDefRow, byte> typedef_table, ref byte next_typedef_id, Assembler ass)
        {
            if (cur_bct is Signature.BaseType)
                sig.Add((byte)((Signature.BaseType)cur_bct).Type);
            else if (cur_bct is Signature.ComplexType)
            {
                Metadata.TypeDefRow tdr = Metadata.GetTypeDef(((Signature.ComplexType)cur_bct).Type, ass);
                if (tdr.IsValueType(ass))
                    sig.Add((byte)BaseType_Type.ValueType);
                else
                    sig.Add((byte)BaseType_Type.Class);
                if (!typedef_table.ContainsKey(tdr))
                    typedef_table.Add(tdr, next_typedef_id++);
                sig.Add(typedef_table[tdr]);
            }
            else if (cur_bct is Signature.BoxedType)
            {
                sig.Add((byte)BaseType_Type.Boxed);
                write_ti_signature(sig, containing_type, ((Signature.BoxedType)cur_bct).Type, typedef_table, ref next_typedef_id, ass);
            }
            else if (cur_bct is Signature.GenericType)
            {
                sig.Add((byte)BaseType_Type.GenericInst);
                write_ti_signature(sig, containing_type, ((Signature.GenericType)cur_bct).GenType, typedef_table, ref next_typedef_id, ass);
                byte[] comp_argcount = Metadata.CompressInteger(Convert.ToUInt32(((Signature.GenericType)cur_bct).GenParams.Count));
                foreach (byte b in comp_argcount)
                    sig.Add(b);
                foreach (Signature.BaseOrComplexType gp in ((Signature.GenericType)cur_bct).GenParams)
                    write_ti_signature(sig, containing_type, gp, typedef_table, ref next_typedef_id, ass);
            }
            else if (cur_bct is Signature.ZeroBasedArray)
            {
                sig.Add((byte)BaseType_Type.SzArray);
                write_ti_signature(sig, containing_type, ((Signature.ZeroBasedArray)cur_bct).ElemType, typedef_table, ref next_typedef_id, ass);
            }
            else if (cur_bct is Signature.ComplexArray)
            {
                Signature.ComplexArray ca = cur_bct as Signature.ComplexArray;
                sig.Add((byte)BaseType_Type.Array);
                write_ti_signature(sig, containing_type, ca.ElemType, typedef_table, ref next_typedef_id, ass);
                append_byte_array(sig, Metadata.CompressInteger((uint)ca.Rank));
                append_byte_array(sig, Metadata.CompressInteger((uint)ca.Sizes.Length));
                foreach (int size in ca.Sizes)
                    append_byte_array(sig, Metadata.CompressInteger((uint)size));
                append_byte_array(sig, Metadata.CompressInteger((uint)ca.LoBounds.Length));
                foreach (int lobound in ca.LoBounds)
                    append_byte_array(sig, Metadata.CompressInteger(ass.FromByteArrayU4(ass.ToByteArray(lobound))));
            }
            else if (cur_bct is Signature.ManagedPointer)
            {
                sig.Add((byte)BaseType_Type.Byref);
                write_ti_signature(sig, containing_type, ((Signature.ManagedPointer)cur_bct).ElemType, typedef_table, ref next_typedef_id, ass);
            }
            else if (cur_bct is Signature.UnmanagedPointer)
            {
                sig.Add((byte)BaseType_Type.Ptr);
                write_ti_signature(sig, containing_type, ((Signature.UnmanagedPointer)cur_bct).BaseType, typedef_table, ref next_typedef_id, ass);
            }
            else
                throw new NotImplementedException(Signature.GetString(cur_bct, ass));
        }

        internal static void append_byte_array(IList<byte> dest, byte[] src)
        {
            foreach (byte b in src)
                dest.Add(b);
        }

        internal static int GetSymbolOffset(string sym_name, Assembler.TypeToCompile ttc, Assembler ass)
        {
            Layout l = Layout.GetTypeInfoLayout(ttc, ass, false);
            return l.Symbols[sym_name];
        }
    }
}
