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
using System.Text;
using binary_library;
using libtysila5.target;
using metadata;

namespace libtysila5.layout
{
    public partial class Layout
    {
        /* Vtable:

            TIPtr (just to a glorified TypeSpec)
            IFacePtr (to list of implemented interfaces)
            Extends (to base classes for quick castclassex)
            Method 0
            ...

            IFaceList TODO


        */

        public static void OutputVTable(TypeSpec ts,
            target.Target t, binary_library.IBinaryFile of,
            MetadataStream base_m = null)
        {
            // Don't compile if not for this architecture
            if (!t.IsTypeValid(ts))
                return;

            var os = of.GetRDataSection();
            var d = os.Data;
            var ptr_size = t.GetCTSize(ir.Opcode.ct_object);
            os.Align(ptr_size);

            ulong offset = (ulong)os.Data.Count;

            /* Symbol */
            var sym = of.CreateSymbol();
            sym.DefinedIn = os;
            sym.Name = ts.MangleType();
            sym.ObjectType = binary_library.SymbolObjectType.Object;
            sym.Offset = offset;
            sym.Type = binary_library.SymbolType.Global;
            os.AddSymbol(sym);

            if (base_m != null && ts.m != base_m)
                sym.Type = SymbolType.Weak;

            /* TIPtr */
            var tiptr_offset = t.st.GetSignatureAddress(ts.Signature, t);

            var ti_reloc = of.CreateRelocation();
            ti_reloc.Addend = tiptr_offset;
            ti_reloc.DefinedIn = os;
            ti_reloc.Offset = offset;
            ti_reloc.Type = t.GetDataToDataReloc();
            ti_reloc.References = t.st.GetStringTableSymbol(of);
            of.AddRelocation(ti_reloc);

            for (int i = 0; i < ptr_size; i++, offset++)
                d.Add(0);

            /* IFacePtr */
            IRelocation if_reloc = null;
            if (!ts.IsGenericTemplate)
            {
                if_reloc = of.CreateRelocation();
                if_reloc.DefinedIn = os;
                if_reloc.Offset = offset;
                if_reloc.Type = t.GetDataToDataReloc();
                if_reloc.References = sym;
                of.AddRelocation(if_reloc);
            }

            for (int i = 0; i < ptr_size; i++, offset++)
                d.Add(0);

            /* Extends */
            var ts_extends = ts.GetExtends();
            if(ts_extends != null)
            {
                var ext_reloc = of.CreateRelocation();
                ext_reloc.Addend = 0;
                ext_reloc.DefinedIn = os;
                ext_reloc.Offset = offset;
                ext_reloc.Type = t.GetDataToDataReloc();

                var ext_sym = of.CreateSymbol();
                ext_sym.DefinedIn = null;
                ext_sym.Name = ts_extends.MangleType();

                ext_reloc.References = ext_sym;
                of.AddRelocation(ext_reloc);

                t.r.VTableRequestor.Request(ts_extends);
            }
            for (int i = 0; i < ptr_size; i++, offset++)
                d.Add(0);

            if (!ts.IsGenericTemplate)
            {
                /* Virtual methods */

                // first build a list of base classes which we reverse to add the methods
                List<TypeSpec> all_classes = new List<TypeSpec>();
                var cur_ts = ts;
                while (cur_ts != null)
                {
                    all_classes.Add(cur_ts);
                    cur_ts = cur_ts.GetExtends();
                }

                // then implement all virtual methods
                for (int i = all_classes.Count - 1; i >= 0; i--)
                {
                    OutputVirtualMethods(ts, all_classes[i],
                        of, os, d, ref offset, t);
                }

                /* Interface implementations */

                // build list of implemented interfaces
                var ii = ts.ImplementedInterfaces;

                // first, add all interface implementations
                List<ulong> ii_offsets = new List<ulong>();

                for (int i = 0; i < ii.Count; i++)
                {
                    ii_offsets.Add(offset);
                    OutputInterface(ts, ii[i],
                        of, os, d, ref offset, t);
                    t.r.VTableRequestor.Request(ii[i]);
                }

                // point iface ptr here
                if_reloc.Addend = (long)offset;
                for (int i = 0; i < ii.Count; i++)
                {
                    // list is pointer to interface declaration, then implementation
                    var id_ptr_sym = of.CreateSymbol();
                    id_ptr_sym.DefinedIn = null;
                    id_ptr_sym.Name = ii[i].MangleType();

                    var id_ptr_reloc = of.CreateRelocation();
                    id_ptr_reloc.Addend = 0;
                    id_ptr_reloc.DefinedIn = os;
                    id_ptr_reloc.Offset = offset;
                    id_ptr_reloc.References = id_ptr_sym;
                    id_ptr_reloc.Type = t.GetDataToDataReloc();
                    of.AddRelocation(id_ptr_reloc);

                    for (int j = 0; j < ptr_size; j++, offset++)
                        d.Add(0);

                    // implementation
                    var ii_ptr_reloc = of.CreateRelocation();
                    ii_ptr_reloc.Addend = (long)ii_offsets[i];
                    ii_ptr_reloc.DefinedIn = os;
                    ii_ptr_reloc.Offset = offset;
                    ii_ptr_reloc.References = sym;
                    ii_ptr_reloc.Type = t.GetDataToDataReloc();
                    of.AddRelocation(ii_ptr_reloc);

                    for (int j = 0; j < ptr_size; j++, offset++)
                        d.Add(0);
                }

                // null terminate the list
                for (int j = 0; j < ptr_size; j++, offset++)
                    d.Add(0);
            }

            sym.Size = (long)(offset - sym.Offset);
        }

        private static void OutputInterface(TypeSpec impl_ts,
            TypeSpec iface_ts, IBinaryFile of, ISection os,
            IList<byte> d, ref ulong offset, Target t)
        {
            /* Iterate through methods */
            var first_mdef = iface_ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                iface_ts.tdrow, 5);
            var last_mdef = iface_ts.m.GetLastMethodDef(iface_ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                MethodSpec iface_ms;
                iface_ts.m.GetMethodDefRow(MetadataStream.tid_MethodDef,
                        (int)mdef_row, out iface_ms, iface_ts.gtparams, null);
                iface_ms.type = iface_ts;

                // First determine if there is a relevant MethodImpl entry
                MethodSpec impl_ms = null;
                for(int i = 1; i <= impl_ts.m.table_rows[MetadataStream.tid_MethodImpl]; i++)
                {
                    var Class = impl_ts.m.GetIntEntry(MetadataStream.tid_MethodImpl, i, 0);

                    if (Class == impl_ts.tdrow)
                    {
                        int mdecl_id, mdecl_row, mbody_id, mbody_row;
                        impl_ts.m.GetCodedIndexEntry(MetadataStream.tid_MethodImpl, i, 2,
                            impl_ts.m.MethodDefOrRef, out mdecl_id, out mdecl_row);
                        MethodSpec mdecl_ms;
                        impl_ts.m.GetMethodDefRow(mdecl_id, mdecl_row, out mdecl_ms, impl_ts.gtparams);

                        if(MetadataStream.CompareString(mdecl_ms.m,
                            mdecl_ms.m.GetIntEntry(MetadataStream.tid_MethodDef, mdecl_ms.mdrow, 3),
                            iface_ms.m,
                            iface_ms.m.GetIntEntry(MetadataStream.tid_MethodDef, iface_ms.mdrow, 3)) &&
                            MetadataStream.CompareSignature(mdecl_ms, iface_ms))
                        {
                            impl_ts.m.GetCodedIndexEntry(MetadataStream.tid_MethodImpl, i, 1,
                                impl_ts.m.MethodDefOrRef, out mbody_id, out mbody_row);
                            impl_ts.m.GetMethodDefRow(mbody_id, mbody_row, out impl_ms, impl_ts.gtparams);
                            impl_ms.type = impl_ts;
                            t.r.MethodRequestor.Request(impl_ms);
                            break;
                        }
                    }
                }

                // Then iterate through all base classes looking for an implementation
                if (impl_ms == null)
                    impl_ms = GetVirtualMethod(impl_ts, iface_ms, t, true);

                // Output reference
                string impl_target = (impl_ms == null) ? "__cxa_pure_virtual" : impl_ms.MangleMethod();

                var impl_sym = of.CreateSymbol();
                impl_sym.DefinedIn = null;
                impl_sym.Name = impl_target;
                impl_sym.ObjectType = SymbolObjectType.Function;

                var impl_reloc = of.CreateRelocation();
                impl_reloc.Addend = 0;
                impl_reloc.DefinedIn = os;
                impl_reloc.Offset = offset;
                impl_reloc.References = impl_sym;
                impl_reloc.Type = t.GetDataToCodeReloc();
                of.AddRelocation(impl_reloc);

                for (int i = 0; i < t.GetPointerSize(); i++, offset++)
                    d.Add(0);
            }
        }

        private static void OutputVirtualMethods(TypeSpec impl_ts,
            TypeSpec decl_ts, IBinaryFile of, ISection os,
            IList<byte> d, ref ulong offset, target.Target t)
        {
            /* Iterate through methods looking for virtual ones */
            var first_mdef = decl_ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                decl_ts.tdrow, 5);
            var last_mdef = decl_ts.m.GetLastMethodDef(decl_ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var flags = decl_ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 2);

                if ((flags & 0x40) == 0x40)
                {
                    MethodSpec decl_ms;
                    decl_ts.m.GetMethodDefRow(MetadataStream.tid_MethodDef,
                        (int)mdef_row, out decl_ms, decl_ts.gtparams, null);

                    var impl_ms = GetVirtualMethod(impl_ts, decl_ms, t);

                    string impl_target = (impl_ms == null) ? "__cxa_pure_virtual" : impl_ms.MangleMethod();

                    var impl_sym = of.CreateSymbol();
                    impl_sym.DefinedIn = null;
                    impl_sym.Name = impl_target;
                    impl_sym.ObjectType = SymbolObjectType.Function;

                    var impl_reloc = of.CreateRelocation();
                    impl_reloc.Addend = 0;
                    impl_reloc.DefinedIn = os;
                    impl_reloc.Offset = offset;
                    impl_reloc.References = impl_sym;
                    impl_reloc.Type = t.GetDataToCodeReloc();
                    of.AddRelocation(impl_reloc);

                    for (int i = 0; i < t.GetPointerSize(); i++, offset++)
                        d.Add(0);
                }
            }
        }

        private static MethodSpec GetVirtualMethod(TypeSpec impl_ts, MethodSpec decl_ms,
            target.Target t, bool allow_non_virtual = false)
        {
            /* Iterate through methods looking for virtual ones */
            var first_mdef = impl_ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                impl_ts.tdrow, 5);
            var last_mdef = impl_ts.m.GetLastMethodDef(impl_ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var flags = impl_ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 2);

                if (allow_non_virtual || (flags & 0x40) == 0x40)
                {
                    MethodSpec impl_ms;
                    impl_ts.m.GetMethodDefRow(MetadataStream.tid_MethodDef,
                        (int)mdef_row, out impl_ms, impl_ts.gtparams, null);
                    impl_ms.type = impl_ts;

                    if (MetadataStream.CompareString(impl_ms.m,
                        impl_ms.m.GetIntEntry(MetadataStream.tid_MethodDef, (int)mdef_row, 3),
                        decl_ms.m,
                        decl_ms.m.GetIntEntry(MetadataStream.tid_MethodDef, (int)decl_ms.mdrow, 3)))
                    {
                        if (MetadataStream.CompareSignature(impl_ms.m, impl_ms.msig,
                            impl_ts.gtparams, null,
                            decl_ms.m, decl_ms.msig, impl_ts.gtparams, null))
                        {
                            // this is the correct one
                            t.r.MethodRequestor.Request(impl_ms);
                            return impl_ms;
                        }
                    }
                }
            }

            // if not found, look to base classes
            var bc = impl_ts.GetExtends();
            if (bc != null)
                return GetVirtualMethod(bc, decl_ms, t);
            else
                return null;
        }

        public static int GetVTableOffset(metadata.MethodSpec ms)
        { return GetVTableOffset(ms.type, ms); }

        public static int GetVTableOffset(metadata.TypeSpec ts,
            metadata.MethodSpec ms)
        {
            var extends = ts.GetExtends();
            var vtbl_length = GetVTableMethLength(extends);
            var search_meth_name = ms.m.GetIntEntry(MetadataStream.tid_MethodDef,
                ms.mdrow, 3);

            /* Iterate through methods looking for requested
                one */
            var first_mdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 5);
            var last_mdef = ts.m.GetLastMethodDef(ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var flags = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 2);

                // Check on name
                var mname = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 3);
                if (MetadataStream.CompareString(ts.m, mname,
                    ms.m, search_meth_name))
                {
                    // TODO: check signature
                    var msig = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                        (int)mdef_row, 4);
                    if (MetadataStream.CompareSignature(ms.m, ms.msig, 
                        ms.gtparams, ms.gmparams,
                        ts.m, (int)msig, 
                        ts.gtparams, ms.gmparams))
                    {

                        if ((flags & 0x40) == 0x40)
                        {
                            // Virtual
                            return vtbl_length;
                        }
                        else
                        {
                            // Instance
                            return -1;
                        }
                    }
                }
                else
                {
                    // This is not the method we are looking for
                    // Virtual & NewSlot
                    if ((flags & 0x140) == 0x140)
                        vtbl_length++;
                }
            }

            return 0;   // fail
        }

        public static int GetVTableTIOffset(TypeSpec ts)
        {
            return GetVTableMethLength(ts);
        }

        private static int GetVTableMethLength(TypeSpec ts)
        {
            if (ts == null)
                return 3; // tiptr, ifaceptr, extends

            var extends = ts.GetExtends();
            var vtbl_length = GetVTableMethLength(extends);

            /* Iterate through methods adding 1 for each virtual
                one */
            var first_mdef = ts.m.GetIntEntry(MetadataStream.tid_TypeDef,
                ts.tdrow, 5);
            var last_mdef = ts.m.GetLastMethodDef(ts.tdrow);

            for (uint mdef_row = first_mdef; mdef_row < last_mdef; mdef_row++)
            {
                var flags = ts.m.GetIntEntry(MetadataStream.tid_MethodDef,
                    (int)mdef_row, 2);

                // Virtual & NewSlot
                if ((flags & 0x140) == 0x140)
                    vtbl_length++;
            }

            return vtbl_length;
        }
    }
}
