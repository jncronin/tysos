using System;
using System.Collections.Generic;

namespace JitTestHost
{
    class Program
    {
        static string arch = "x86_64-jit-tysos";
        static libtysila.Assembler ass;
        static JitFileLoader file_loader;
        static JitMemberRequestor member_requestor;
        static JitOutput output;
        internal static JitMemoryManager mmgr;
        static bool debug = true;

        static void Main(string[] args)
        {
            file_loader = new JitFileLoader();
            member_requestor = new JitMemberRequestor();
            output = new JitOutput();
            mmgr = new JitMemoryManager();

            file_loader.LoadModuleToMemory("test_002", System.IO.Path.Combine(Environment.CurrentDirectory, "../../../testsuite/test_002/bin/Debug/test_002.exe"));
            file_loader.LoadModuleToMemory("mscorlib", System.IO.Path.Combine(Environment.CurrentDirectory, "../../../mono/corlib/mscorlib.dll"));
            file_loader.LoadModuleToMemory("libsupcs", System.IO.Path.Combine(Environment.CurrentDirectory, "../../../libsupcs/bin/Release/libsupcs.dll"));

            ass = libtysila.Assembler.CreateAssembler(libtysila.Assembler.ParseArchitectureString(arch), file_loader, member_requestor, null);
            member_requestor.Assembler = ass;
            libtysila.Metadata module = ass.FindAssembly("test_002");
            libtysila.Assembler.MethodToCompile? mtc = module.GetEntryPoint(ass);
            member_requestor.RequestMethod(mtc.Value, false);

            // the jit stub to call
            symbols.Add("__jit", 0);
            
            // Now do the compilation
            int objects_assembled;
            do
            {
                objects_assembled = 0;

                JitMemberRequestor.JitMethod next_meth = member_requestor.GetNextJitMethod();
                if (next_meth != null)
                {
                    if (debug)
                        Console.WriteLine("Method: " + next_meth.mtc.ToString() + ((next_meth.is_jit_stub) ? " (JIT stub)" : ""));
                    if (next_meth.is_jit_stub == false)
                    {
                        ass.AssembleMethod(next_meth.mtc, output, null);
                    }
                    else
                    {
                        // TODO: add jit stub
                        output.AddTextSymbol(output.GetText().Count, libtysila.Mangler2.MangleMethod(next_meth.mtc, ass), false, true, false);

                        /* for now
                         * 
                         * mov rax, __jit
                         * jmp rax
                         */
                        byte[] jit_stub = new byte[] { 0x48, 0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xe0 };
                        output.AddTextRelocation(output.GetText().Count + 2, "__jit", libtysila.x86_64.x86_64_elf64.R_X86_64_64, 0);
                        output.text.AddRange(jit_stub);
                    }
                    objects_assembled++;
                }

                libtysila.Assembler.MethodToCompile? next_mi = member_requestor.GetNextJitMethodInfo();
                if (next_mi.HasValue)
                {
                    if (debug)
                        Console.WriteLine("MethodInfo: " + next_mi.Value.ToString());
                    //ass.AssembleMethodInfo(next_mi.Value, output);

                    if (next_mi.Value.MetadataToken.HasValue)
                    {
                        // Write a short method info - magic (0x01010101), uint token, uintptr metadata_ptr

                        // TODO: modify for 32 bits
                        byte[] smi = new byte[16];
                        uint magic = 0x01010101;
                        BitConverter.GetBytes(magic).CopyTo(smi, 0);
                        BitConverter.GetBytes(next_mi.Value.MetadataToken.Value).CopyTo(smi, 4);
                        BitConverter.GetBytes((ulong)symbols["metadata_" + next_mi.Value.meth.m.ModuleName]).CopyTo(smi, 8);

                        output.AddRodataSymbol(output.GetRodata().Count, libtysila.Mangler2.MangleMethodInfoSymbol(next_mi.Value, ass));
                        output.rodata.AddRange(smi);
                    }
                    else
                        throw new Exception("No metadata token specified in requested method");

                    objects_assembled++;
                }

                libtysila.Assembler.TypeToCompile? next_ti = member_requestor.GetNextJITType();
                if (next_ti.HasValue)
                {
                    if (debug)
                        Console.WriteLine("TypeInfo: " + next_ti.Value.ToString());
                    //ass.AssembleType(next_ti.Value, output);
                    objects_assembled++;
                }

                libtysila.Metadata next_mod = member_requestor.GetNextModule();
                if (next_mod != null)
                {
                    if (debug)
                        Console.WriteLine("Module: " + next_mod.ModuleName);
                    ass.AssembleModuleInfo(next_mod, output);
                    objects_assembled++;
                }

                libtysila.Metadata next_ass = member_requestor.GetNextAssembly();
                if (next_mod != null)
                {
                    if (debug)
                        Console.WriteLine("Assembly: " + next_ass.ModuleName);
                    ass.AssembleAssemblyInfo(next_ass, output);
                    objects_assembled++;
                }

            } while (objects_assembled > 0);

            // Write to the output
            int len = output.text.Count + output.data.Count + output.rodata.Count;
            int base_addr = mmgr.Alloc(len);

            // Store symbols
            foreach (KeyValuePair<string, int> kvp in output.text_sym)
                RegisterSymbol(kvp.Key, kvp.Value + base_addr);
            foreach (KeyValuePair<string, int> kvp in output.data_sym)
                RegisterSymbol(kvp.Key, kvp.Value + base_addr);
            foreach (KeyValuePair<string, int> kvp in output.rodata_sym)
                RegisterSymbol(kvp.Key, kvp.Value + base_addr);

            // Write the data
            int offset = base_addr;
            output.text.CopyTo(mmgr.Memory, offset);
            offset += output.text.Count;
            output.data.CopyTo(mmgr.Memory, offset);
            offset += output.data.Count;
            output.rodata.CopyTo(mmgr.Memory, offset);

            // Perform relocations
            foreach (KeyValuePair<int, JitOutput.Relocation> kvp in output.text_rel)
                DoRelocation(kvp.Key + base_addr, kvp.Value);
            foreach (KeyValuePair<int, JitOutput.Relocation> kvp in output.data_rel)
                DoRelocation(kvp.Key + base_addr, kvp.Value);
            foreach (KeyValuePair<int, JitOutput.Relocation> kvp in output.rodata_rel)
                DoRelocation(kvp.Key + base_addr, kvp.Value);
        }

        private static void DoRelocation(int offset, JitOutput.Relocation relocation)
        {
            byte[] reloc = null;

            switch (relocation.RelType)
            {
                case libtysila.x86_64.x86_64_elf64.R_X86_64_64:
                    long target_addr = symbols[relocation.Name] + relocation.Value;
                    reloc = BitConverter.GetBytes((ulong)target_addr);
                    break;
                default:
                    throw new Exception("Unsupported relocation type");
            }

            reloc.CopyTo(mmgr.Memory, offset);
        }

        static Dictionary<string, int> symbols = new Dictionary<string, int>();

        public static void RegisterSymbol(string name, int addr)
        {
            symbols.Add(name, addr);
        }
        
        public static bool IsCompiled(string obj)
        {
            return symbols.ContainsKey(obj);
        }
    }
}
