using System;
using System.Collections.Generic;
using System.Text;

namespace CMExpLib
{
    public class SymbolTable
    {
        public libtysila.Assembler ass;
        public LayoutManager lm;

        public class Symbol
        {
            public string Name;
            public ulong vaddr;
            public ulong offset;
            public ulong size;

            public Elf64Reader.ElfHeader r;

            public override string ToString()
            {
                return Name;
            }
        }

        public Dictionary<string, Symbol> AssemblySymbols = new Dictionary<string, Symbol>();
        public Dictionary<string, Symbol> Symbols = new Dictionary<string, Symbol>();
        public Dictionary<ulong, Symbol> SymbolsFromVaddr = new Dictionary<ulong, Symbol>();

        public void Add(string name, ulong vaddr, ulong offset, ulong size, Elf64Reader.ElfHeader r)
        {
            Symbol s = new Symbol { Name = name, vaddr = vaddr, offset = offset, r = r, size = size };
            if (name.StartsWith("_A"))
                AssemblySymbols.Add(name, s);
            Symbols.Add(name, s);
            if(!SymbolsFromVaddr.ContainsKey(s.vaddr))
                SymbolsFromVaddr.Add(s.vaddr, s);
        }

        public string GetSymbolName(ulong vaddr)
        {
            if (SymbolsFromVaddr.ContainsKey(vaddr))
                return SymbolsFromVaddr[vaddr].Name;
            return "unknown";
        }
    }
}
