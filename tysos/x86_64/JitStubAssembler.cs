using binary_library;
using libtysila5;
using libtysila5.target;
using metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.x86_64
{
    class JitStubAssembler : jit.Jit.JitStubAssembler
    {
        public override bool AssembleJitStub(MethodSpec ms, Target t, IBinaryFile bf, TysilaState s)
        {
            var tsect = bf.GetTextSection();
            var mname = ms.MangleMethod();

            // Add symbol
            var sym = bf.CreateSymbol();
            sym.Name = mname;
            sym.ObjectType = SymbolObjectType.Function;
            sym.Offset = (ulong)tsect.Data.Count;
            sym.Type = SymbolType.Global;
            tsect.AddSymbol(sym);

            // The code here comes from libtysila5/target/x86/x86_64-jitstub.asm
            var b = new byte[]
            {
                0x83, 0x3d, 0xf1, 0xff, 0xff, 0xff, 0x02,
                0x74, 0x58,
                0x57,
                0x48, 0xc7, 0xc7, 0x01, 0x00, 0x00, 0x00,
                0x31, 0xc0,
                0xf0, 0x0f, 0xb1, 0x3d, 0xdd, 0xff, 0xff,
                0xff,
                0x83, 0xf8, 0x01,
                0x7c, 0x06,
                0x7f, 0x3e,
                0xf3, 0x90,
                0xeb, 0xeb,
                0x56,
                0x52,
                0x51,
                0x41, 0x50,
                0x41, 0x51,
                0x41, 0x52,
                0x41, 0x53,
                0x48, 0x8b, 0x3d, 0xb8, 0xff, 0xff, 0xff,
                0x48, 0xb8
            };
            foreach (var bi in b)
                tsect.Data.Add(bi);

            // Next comes a R_X86_64_64 relocation to jit_tm
            var jit_tm_sym = bf.CreateSymbol();
            jit_tm_sym.Name = "jit_tm";
            jit_tm_sym.Type = SymbolType.Undefined;
            
            var reloc = bf.CreateRelocation();
            reloc.Addend = 0;
            reloc.Offset = (ulong)tsect.Data.Count;
            reloc.References = jit_tm_sym;
            reloc.Type = new binary_library.elf.ElfFile.Rel_x86_64_64();
            reloc.DefinedIn = tsect;
            bf.AddRelocation(reloc);
            for (int i = 0; i < 8; i++)
                tsect.Data.Add(0);

            // Continue with the rest of the function
            b = new byte[]
            {
                0xff, 0xd0,
                0x41, 0x5b,
                0x41, 0x5a,
                0x41, 0x59,
                0x41, 0x58,
                0x59,
                0x5a,
                0x5e,
                0x48, 0x89, 0x05, 0x9a, 0xff, 0xff, 0xff,
                0xc7, 0x05, 0x98, 0xff, 0xff, 0xff, 0x02, 0x00, 0x00, 0x00,
                0x5f,
                0xff, 0x25, 0x89, 0xff, 0xff, 0xff
            };
            foreach (var bi in b)
                tsect.Data.Add(bi);

            return true;
        }
    }
}
