using binary_library;
using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.jit
{
    class JitBinary : BinaryFile
    {
        Bitness bness;
        public JitBinary(Bitness bitness) { bness = bitness; }

        public override Bitness Bitness { get { return bness; } set { } }

        public override IProgramHeader ProgramHeader => throw new NotImplementedException();
    }
}
