using System;
using System.Collections.Generic;
using System.Text;

namespace JitTestHost
{
    class JitFileLoader : libtysila.Assembler.FileLoader
    {
        Dictionary<string, byte[]> loaded_modules = new Dictionary<string, byte[]>();

        public void LoadModuleToMemory(string name, string fname)
        {
            System.IO.FileStream f = System.IO.File.OpenRead(fname);
            byte[] ret = new byte[f.Length];
            f.Read(ret, 0, (int)f.Length);
            f.Close();
            loaded_modules.Add(name, ret);

            int base_addr = Program.mmgr.Alloc(ret.Length);
            ret.CopyTo(Program.mmgr.Memory, base_addr);
            Program.RegisterSymbol("metadata_" + name, base_addr);
        }

        public override libtysila.Assembler.FileLoader.FileLoadResults LoadFile(string filename)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(loaded_modules[filename]);
            return new FileLoadResults { FullFilename = filename, ModuleName = filename, Stream = ms };
        }
    }
}
