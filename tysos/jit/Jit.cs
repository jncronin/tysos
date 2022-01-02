using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.jit
{
    class Jit
    {
        public static libtysila5.target.Target t;  // should be set by arch
        public static binary_library.Bitness bness = binary_library.Bitness.Bits32; // should be set by arch
        public static JitStubAssembler jsa; // should be set by arch

        static int next_st_id = 0;

        const int TextSection = 0;
        const int RDataSection = 1;
        const int DataSection = 2;

        static libtysila5.TysilaState InitTysilaState()
        {
            var s = new libtysila5.TysilaState();
            s.bf = new JitBinary(bness);
            s.bf.Init();
            s.bf.Architecture = t.name;
            s.text_section = s.bf.GetTextSection();

            var r = new JitRequestor();
            s.r = r;

            /* thread-safe generate new symbol name */
            while (true)
            {
                int cur_st_id = next_st_id;
                if (System.Threading.Interlocked.CompareExchange(ref next_st_id, cur_st_id + 2, cur_st_id) == cur_st_id)
                {
                    s.st = new libtysila5.StringTable("jit" + cur_st_id.ToString(), libsupcs.Metadata.BAL, t);
                    s.sigt = new libtysila5.SignatureTable("jit" + (cur_st_id + 1).ToString());
                    break;
                }
            }

            return s;
        }

        [libsupcs.AlwaysCompile]
        [libsupcs.MethodAlias("jit_tm")]
        internal static unsafe void* JitCompile(metadata.MethodSpec meth)
        {
            var s = InitTysilaState();

            // Add the new method to the requestor
            ((JitRequestor)s.r).FullMethodRequestor.Request(meth);

            // Compile all needed bits
            JitProcess.ProcessRequestedItems(s, Program.stab);

            // Add everything from the current state to output sections
            return CopyToOutput(s, TextSection);    // First Method requested will be at start of Text section
        }

        [libsupcs.MethodAlias("jit_vtable")]
        [libsupcs.AlwaysCompile]
        internal static unsafe void* JitCompile(metadata.TypeSpec ts)
        {
            var s = InitTysilaState();

            // Add the new vtable to the requestor
            ((JitRequestor)s.r).VTableRequestor.Request(ts);

            // Compile all needed bits
            JitProcess.ProcessRequestedItems(s, Program.stab);

            // Add everything from the current state to output sections
            return CopyToOutput(s, RDataSection);   // First VTable requested will be at start of RData section
        }

        private unsafe static void* CopyToOutput(libtysila5.TysilaState s, int sect_id)
        {
            // copy from the JitBinary file to final location, process symbols and relocations
            var bf = s.bf;
            var tsect = bf.GetTextSection();
            var rsect = bf.GetRDataSection();
            var dsect = bf.GetDataSection();

            // TODO: ensure these are not garbage collected
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
                    else if (cur_sym.DefinedIn == tsect)
                    {
                        addr = cur_sym.Offset + tout;
                        // JIT Stubs are marked as weak
                        Program.stab.Add(cur_sym.Name, (ulong)addr, (ulong)cur_sym.Size, cur_sym.Type == binary_library.SymbolType.Weak);
                    }

                    if (addr != null)
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
                                *((byte**)addr) = (byte*)(taddr) + cur_reloc.Addend;
                            }
                            else
                            {
                                System.Diagnostics.Debugger.Log(0, "test_vtable", "Unsupported reloc type " + cur_reloc.Type.Name);
                            }
                        }
                    }
                }
            }

            switch(sect_id)
            {
                case TextSection:
                    return tout;
                case RDataSection:
                    return rout;
                case DataSection:
                    return dout;
                default:
                    return null;
            }
        }

        public abstract class JitStubAssembler
        {
            public abstract bool AssembleJitStub(metadata.MethodSpec ms, libtysila5.target.Target t,
                binary_library.IBinaryFile bf, libtysila5.TysilaState s);
        }
    }
}
