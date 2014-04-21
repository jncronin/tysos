using System;
using System.Collections.Generic;
using System.Text;

namespace JitTestHost
{
    class JitMemoryManager
    {
        const int MEM_SIZE = 0x1000000;
        static byte[] memory = new byte[MEM_SIZE];
        int next_free;

        public byte[] Memory { get { return memory; } }

        public int Alloc(int size)
        {
            if ((next_free + size) > MEM_SIZE)
                throw new OutOfMemoryException();
            int ret = next_free;
            next_free += size;
            return ret;
        }
    }
}
