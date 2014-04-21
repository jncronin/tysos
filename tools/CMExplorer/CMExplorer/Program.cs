using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CMExplorer
{
    class Program
    {
        internal static List<string> search_dirs = new List<string>();
        internal static string DirectoryDelimiter = "/";

        static void Main(string[] args)
        {
            CMExpLib.Elf64Reader r = new CMExpLib.Elf64Reader();
            search_dirs.Add("../../../../../tysila2/bin/Release");
            search_dirs.Add("../../../../../mono/corlib");
            CMExpLib.Elf64Reader.ElfHeader ehdr = r.Read("../../../../../tysos/tysos.bin", new FileSystemFileLoader());

            

            foreach (CMExpLib.SymbolTable.Symbol s in ehdr.stab.AssemblySymbols.Values)
            {
                CMExpLib.MetadataObject mo = CMExpLib.MetadataObject.Read(s);

            }
        }
    }
}
