using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using binary_library;
using libtysila5;
using libtysila5.layout;
using metadata;

namespace tysos
{
    class jittestmethods
    {
        static int next_st_id = 0;

        internal static unsafe void test(libsupcs.TysosMethod meth)
        {
            var ms = meth.mspec;
            var t = libtysila5.target.Target.targets["x86_64"];
            var bf = new jit_binary();
            bf.Init();
            bf.Architecture = "x86_64";
            var st = new libtysila5.StringTable("jit" + (next_st_id++).ToString(), libsupcs.Metadata.BAL, t);
            t.st = st;
            t.r = new jit_requestor();
            t.InitIntcalls();

            StringBuilder dbg = new StringBuilder();
            
            libtysila5.libtysila.AssembleMethod(ms, bf, t, dbg);

            // Add stringtable to rdata
            // Add string table
            st.WriteToOutput(bf, ms.m, t);

            // try and run the new method
            var tsect = bf.GetTextSection();
            var rsect = bf.GetRDataSection();
            var dsect = bf.GetDataSection();

            var rout = (byte*)(void*)gc.gc.Alloc((ulong)rsect.Length);
            var dout = (byte*)(void*)gc.gc.Alloc((ulong)dsect.Length);
            var tout = (byte*)(void*)gc.gc.Alloc((ulong)tsect.Length);

            System.Diagnostics.Debugger.Log(0, "jittest", "rout @ " + ((ulong)rout).ToString("X") +
                ", length " + rsect.Length.ToString());
            System.Diagnostics.Debugger.Log(0, "jittest", "dout @ " + ((ulong)dout).ToString("X") +
                ", length " + dsect.Length.ToString());
            System.Diagnostics.Debugger.Log(0, "jittest", "tout @ " + ((ulong)tout).ToString("X") +
                ", length " + tsect.Length.ToString());

            for (var i = 0; i < rsect.Length; i++)
                rout[i] = rsect.Data[i];
            for (var i = 0; i < dsect.Length; i++)
                dout[i] = dsect.Data[i];
            for (var i = 0; i < tsect.Length; i++)
                tout[i] = tsect.Data[i];

            // Add symbols
            for (var i = 0; i < bf.GetSymbolCount(); i++)
            {
                var cur_sym = bf.GetSymbol(i);
                if (cur_sym.DefinedIn != null)
                {
                    byte* addr = null;
                    if (cur_sym.DefinedIn == dsect)
                    {
                        addr = cur_sym.Offset + dout;
                        Program.stab.Add(cur_sym.Name, (ulong)addr, (ulong)cur_sym.Size);
                    }
                    else if (cur_sym.DefinedIn == rsect)
                    {
                        addr = cur_sym.Offset + rout;
                        Program.stab.Add(cur_sym.Name, (ulong)addr, (ulong)cur_sym.Size);
                    }
                    else if(cur_sym.DefinedIn == tsect)
                    {
                        addr = cur_sym.Offset + tout;
                        Program.stab.Add(cur_sym.Name, (ulong)addr, (ulong)cur_sym.Size);
                    }

                    if(addr != null)
                    {
                        System.Diagnostics.Debugger.Log(0, "jittest", "Created symbol " +
                            cur_sym.Name + " at " + ((ulong)addr).ToString("X") + "" +
                            " (offset " + cur_sym.Offset.ToString("X") + ")");
                    }
                }
            }

            var reloc_count = bf.GetRelocationCount();
            System.Diagnostics.Debugger.Log(0, "jittest", "Found " + reloc_count.ToString() + " relocs");

            for (var i = 0; i < bf.GetRelocationCount(); i++)
            {
                var cur_reloc = bf.GetRelocation(i);
                if (cur_reloc.DefinedIn != null)
                {
                    byte* addr = null;
                    if (cur_reloc.DefinedIn == dsect)
                    {
                        addr = cur_reloc.Offset + dout;
                    }
                    else if (cur_reloc.DefinedIn == rsect)
                    {
                        addr = cur_reloc.Offset + rout;
                    }
                    else if (cur_reloc.DefinedIn == tsect)
                    {
                        addr = cur_reloc.Offset + tout;
                    }

                    if (addr != null)
                    {
                        var target = cur_reloc.References.Name;
                        var taddr = Program.GetAddressOfObject(target);

                        if (taddr == IntPtr.Zero)
                        {
                            System.Diagnostics.Debugger.Log(0, "test_vtable", "Unable to find target reloc " + target);
                        }
                        else
                        {
                            if (cur_reloc.Type.Type == binary_library.elf.ElfFile.R_X86_64_64)
                            {
                                *((byte**)taddr) = addr;
                            }
                            else
                            {
                                System.Diagnostics.Debugger.Log(0, "test_vtable", "Unsupported reloc type " + cur_reloc.Type.Name);
                            }
                        }
                    }
                }
            }

            System.Diagnostics.Debugger.Log(0, "jittest", "Debug: " + dbg.ToString());

            libsupcs.OtherOperations.AsmBreakpoint();

        }

        [libsupcs.MethodAlias("jit_vtable")]
        [libsupcs.AlwaysCompile]
        internal static unsafe void* test_vtable(metadata.TypeSpec ts)
        {
            var t = libtysila5.target.Target.targets["x86_64"];

            var bf = new jit_binary();
            bf.Init();
            bf.Architecture = "x86_64";
            var st = new libtysila5.StringTable("jit", libsupcs.Metadata.BAL, t);
            t.st = st;
            t.r = new jit_requestor();


            // we assume t.InitIntcalls has been called here - to check

            System.Diagnostics.Debugger.Log(0, "test_vtable", "Calling OutputVTable");
            libtysila5.layout.Layout.OutputVTable(ts, t, bf);
            System.Diagnostics.Debugger.Log(0, "test_vtable", "OutputVTable returned");

            var os = bf.GetRDataSection();
            System.Diagnostics.Debugger.Log(0, "test_vtable", os.Length.ToString() + " rdata bytes");
            System.Diagnostics.Debugger.Log(0, "test_vtable", bf.GetSymbolCount().ToString() + " symbols");
            for(int i = 0; i < bf.GetSymbolCount(); i++)
            {
                var cur_sym = bf.GetSymbol(i);
                System.Diagnostics.Debugger.Log(0, "test_vtable", cur_sym.Name + " @ " + cur_sym.Offset.ToString("X"));
            }

            System.Diagnostics.Debugger.Log(0, "test_vtable", bf.GetRelocationCount().ToString() + " relocs");
            for(int i = 0; i < bf.GetRelocationCount(); i++)
            {
                var cur_reloc = bf.GetRelocation(i);
                var addr = Program.GetAddressOfObject(cur_reloc.References.Name);
                if(addr == IntPtr.Zero)
                {
                    System.Diagnostics.Debugger.Log(0, "test_vtable", cur_reloc.Offset.ToString("X") + " -> " +
                        cur_reloc.References.Name + " (undefined)");
                }
                else
                {
                    System.Diagnostics.Debugger.Log(0, "test_vtable", cur_reloc.Offset.ToString("X") + " -> " +
                        cur_reloc.References.Name + " (" + addr.ToString("X") + ")");
                }
            }

            // Create the output vtable
            var dsect = bf.GetDataSection();
            var rsect = bf.GetRDataSection();

            var rout = (byte*)(void*)gc.gc.Alloc((ulong)rsect.Length);
            var dout = (byte*)(void*)gc.gc.Alloc((ulong)dsect.Length);

            for (var i = 0; i < rsect.Length; i++)
                rout[i] = rsect.Data[i];
            for (var i = 0; i < dsect.Length; i++)
                dout[i] = dsect.Data[i];

            // Add symbols
            for(var i = 0; i < bf.GetSymbolCount(); i++)
            {
                var cur_sym = bf.GetSymbol(i);
                if(cur_sym.DefinedIn != null)
                {
                    if(cur_sym.DefinedIn == dsect)
                    {
                        var addr = cur_sym.Offset + dout;
                        Program.stab.Add(cur_sym.Name, (ulong)addr, (ulong)cur_sym.Size);
                    }
                    else if(cur_sym.DefinedIn == rsect)
                    {
                        var addr = cur_sym.Offset + rout;
                        Program.stab.Add(cur_sym.Name, (ulong)addr, (ulong)cur_sym.Size);
                    }
                }
            }

            // Handle relocs
            for(var i = 0; i < bf.GetRelocationCount(); i++)
            {
                var cur_reloc = bf.GetRelocation(i);
                if(cur_reloc.DefinedIn != null)
                {
                    byte* addr = null;
                    if(cur_reloc.DefinedIn == dsect)
                    {
                        addr = cur_reloc.Offset + dout;
                    }
                    else if(cur_reloc.DefinedIn == rsect)
                    {
                        addr = cur_reloc.Offset + rout;
                    }
                    if(addr != null)
                    {
                        var target = cur_reloc.References.Name;
                        var taddr = Program.GetAddressOfObject(target);

                        if(taddr == IntPtr.Zero)
                        {
                            System.Diagnostics.Debugger.Log(0, "test_vtable", "Unable to find target reloc " + target);
                        }
                        else
                        {
                            if(cur_reloc.Type.Type == binary_library.elf.ElfFile.R_X86_64_64)
                            {
                                *((byte**)taddr) = addr;
                            }
                            else
                            {
                                System.Diagnostics.Debugger.Log(0, "test_vtable", "Unsupported reloc type " + cur_reloc.Type.Name);
                            }
                        }
                    }
                }
            }

            // the vtable starts at the beginning of the rdata section
            return rout;
        }

        static string get_string()
        {
            return "I am tysos";
        }

        class jit_binary : binary_library.BinaryFile
        {
            public override Bitness Bitness { get { return Bitness.Bits64; } set { } }

            public override IProgramHeader ProgramHeader => throw new NotImplementedException();
        }

        class jit_requestor : libtysila5.Requestor
        {
            vt_requestor vtr = new vt_requestor();
            m_requestor mr = new m_requestor();
            eh_requestor ehr = new eh_requestor();
            bm_requestor bmr = new bm_requestor();
            sf_requestor sfr = new sf_requestor();
            d_requestor dr = new d_requestor();

            public override IndividualRequestor<TypeSpec> VTableRequestor => vtr;
            public override IndividualRequestor<Layout.MethodSpecWithEhdr> MethodRequestor => mr;
            public override IndividualRequestor<Layout.MethodSpecWithEhdr> EHRequestor => ehr;
            public override IndividualRequestor<Layout.MethodSpecWithEhdr> BoxedMethodRequestor => bmr;
            public override IndividualRequestor<TypeSpec> StaticFieldRequestor => sfr;
            public override IndividualRequestor<TypeSpec> DelegateRequestor => dr;
        }

        class vt_requestor : IndividualRequestor<TypeSpec>
        {
            public override bool Empty => throw new NotImplementedException();

            public override TypeSpec GetNext()
            {
                throw new NotImplementedException();
            }

            public override void Remove(TypeSpec v)
            {
                throw new NotImplementedException();
            }

            public override void Request(TypeSpec v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "vt_requestor: request for " + v.MangleType());
            }
        }

        class eh_requestor : m_requestor
        {
            public override void Request(Layout.MethodSpecWithEhdr v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "eh_requestor: request for " + v.ms.MangleMethod());
            }
        }

        class m_requestor : IndividualRequestor<Layout.MethodSpecWithEhdr>
        {
            public override bool Empty => throw new NotImplementedException();

            public override Layout.MethodSpecWithEhdr GetNext()
            {
                throw new NotImplementedException();
            }

            public override void Remove(Layout.MethodSpecWithEhdr v)
            {
                throw new NotImplementedException();
            }

            public override void Request(Layout.MethodSpecWithEhdr v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "m_requestor: request for " + v.ms.MangleMethod());
            }
        }

        class bm_requestor : m_requestor
        {
            public override void Request(Layout.MethodSpecWithEhdr v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "bm_requestor: request for " + v.ms.MangleMethod());
            }
        }

        class sf_requestor : vt_requestor
        {
            public override void Request(TypeSpec v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "sf_requestor: request for " + v.MangleType());
            }
        }

        class d_requestor : vt_requestor
        {
            public override void Request(TypeSpec v)
            {
                System.Diagnostics.Debugger.Log(0, "jitmethods", "d_requestor: request for " + v.MangleType());
            }
        }
    }
}
