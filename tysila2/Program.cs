/* Copyright (C) 2008 - 2011 by John Cronin
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
using System.IO;
using libtysila;

namespace tysila
{
    class Program
    {
        //public const string DefaultAssembler = "x86_64";
        //public const string DefaultOFormat = "elf64";
        public const string DefaultArchitecture = "x86_64s-elf64-tysos";

        static string output_header = null;
        public static string input_file = null;
        static string output_file = null;
        static string unimplemented = null;

        //public static string assembler = DefaultAssembler;
        //public static string oformat = DefaultOFormat;

        public static libtysila.Assembler.Architecture arch = libtysila.Assembler.ParseArchitectureString(DefaultArchitecture);
        public static libtysila.Assembler.AssemblerOptions options = new Assembler.AssemblerOptions();
        static string output_cinit = null;
        static string irdump = null;
		static int debug_level = 0;
        static bool interactive = false;
        static bool quiet = false;
        static bool requested_module_only = false;
        static bool whole_module = false;
        static bool packed_structs = false;
        static bool profile = false;
        static string debug_file = null;
        static bool debug = false;
        static List<string> extra_types = new List<string>();
        static List<string> extra_methods = new List<string>();
        static List<string> extra_typeinfos = new List<string>();
        static List<string> exclude_files = new List<string>();
        static List<string> include_files = new List<string>();

        /* Boiler plate */
        const string year = "2009 - 2014";
        const string authors = "John Cronin <jncronin@tysos.org>";
        const string website = "http://www.tysos.org";
        const string nl = "\n";
        public static string bplate = "tysila " + libtysila.Assembler.VersionString + " (" + website + ")" + nl +
            "Copyright (C) " + year + " " + authors + nl +
            "This is free software.  Please see the source for copying conditions.  There is no warranty, " +
            "not even for merchantability or fitness for a particular purpose";

        static string comment = nl + "tysila" + nl + "ver: " + libtysila.Assembler.VersionString + nl;

        public static List<string> search_dirs = new List<string> { "", ".", Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Program)).Location), Program.DirectoryDelimiter };

		public static string DirectoryDelimiter
		{
			get
			{
				if(Environment.OSVersion.Platform == PlatformID.Unix)
					return "/";
				else
					return "\\";
			}
		}

        static int Main(string[] args)
        {
            if (ParseArgs(args) == false)
            {
                ShowHelp();
                return -1;
            }

            if (arch == null)
            {
                WriteLine("Unsupported architecture!");
                return -1;
            }

            WholeFileRequestor req = new WholeFileRequestor();
            libtysila.Assembler ass = libtysila.Assembler.CreateAssembler(arch, new FileSystemFileLoader(), req, Program.options);
            comment += "arch: " + arch.ToString() + nl;
            comment += "comp-date: " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + nl;
            comment += "endtysila" + nl;
            ass.profile = profile;
            if (output_file == null)
                ass.output_name = "";
            else
            {
                FileInfo fi = new FileInfo(output_file);
                string fname = fi.Name;
                fname = fname.Substring(0, fname.Length - fi.Extension.Length);
                ass.output_name = fname;

                if (fname == "libsupcs")
                    ass.Options.IncludeLibsupcs = true;
                if (fname == "libstdcs")
                    ass.Options.IncludeLibstdcs = true;

                if (debug)
                {
                    debug_file = fi.Name + ".tydb";
                    tydbfile.TyDbFile tydb = new tydbfile.TyDbFile();
                    tydb.CompiledFileName = fi.FullName;
                    ass.debug = tydb;
                }
            }

            if (new FileInfo(input_file).Name.ToLower() == "libsupcs.dll")
                ass.Options.IncludeLibsupcs = true;
            if (new FileInfo(input_file).Name.ToLower() == "libstdcs.dll")
                ass.Options.IncludeLibstdcs = true;

            if (ass == null)
            {
                ShowHelp();
                return -1;
            }

            Metadata m = Metadata.LoadAssembly(input_file, ass, ass.output_name);
            libtysila.File f = m.File;
            
            if (irdump != null)
                ass.ProduceIrDump = true;

            ass.AddAssembly(m);

            if (interactive)
            {
                Interactive i = new Interactive(ass);
                if (i.Run() == Interactive.InteractiveReturn.Quit)
                    return 0;
            }

#if !DEBUG
            try
            {
#endif
            if (output_file != null)
            {
                /* Find the requested output module */
                IOutputFile ew = null;
                Type[] ass_types = typeof(Program).Assembly.GetTypes();
                foreach (Type t in ass_types)
                {
                    if (t.Name.ToLower() == (arch.OutputFormat + "Writer").ToLower())
                    {
                        System.Reflection.ConstructorInfo ctorm2 = t.GetConstructor(new Type[] { typeof(string), typeof(string), typeof(Assembler) });
                        ew = ctorm2.Invoke(new object[] { input_file, comment, ass }) as IOutputFile;
                        break;
                    }
                }
                if (ew == null)
                {
                    ShowHelp();
                    return -1;
                }

                // Add the modules non-generic types and methods to the list of those to compile
                req.RequestWholeModule(m);

                // Find the entry point
                if (f.GetStartToken() != 0x0)
                {
                    libtysila.Token ept = new Token(f.GetStartToken(), m);
                    Metadata.MethodDefRow mdr = ept.Value as Metadata.MethodDefRow;
                    Metadata.TypeDefRow tdr = Metadata.GetOwningType(mdr.m, mdr);
                    Assembler.MethodToCompile start_point = new Assembler.MethodToCompile
                    {
                        _ass = ass,
                        meth = mdr,
                        msig = Signature.ParseMethodDefSig(m, mdr.Signature, ass),
                        type = tdr,
                        tsigp = new Signature.Param(new Token(tdr), ass),
                        m = tdr.m,
                        MetadataToken = ept.ToUInt32()
                    };
                    ew.SetEntryPoint(Mangler2.MangleMethod(start_point, ass));
                }

                // Do the actual compilation
                CompilerRunner.IDoCompileFeedback feedback = null;
                if (!quiet)
                    feedback = new Interactive(ass);
                List<string> unimplemented_list = null;
                if (unimplemented != null)
                    unimplemented_list = new List<string>();
                CompilerRunner.DoCompile(ass, ew, unimplemented_list, feedback);

                // Add the string tables for the requested modules
                foreach (Assembler.AssemblyInformation ai in ass.GetLoadedAssemblies())
                    ai.m.StringTable.WriteToOutput(ew, ass);

                // Write the output file
                if (!quiet)
                    new Interactive(ass).DisplayLine("Writing output file: " + output_file);
                FileStream oput = new FileStream(output_file, FileMode.Create, FileAccess.Write);
                ew.Write(oput);
                oput.Close();

                // Write the unimplemented list
                if (unimplemented != null)
                {
                    if (!quiet)
                        new Interactive(ass).DisplayLine("Writing list of unimplemented internal calls: " + unimplemented);
                    FileStream u_oput = new FileStream(unimplemented, FileMode.Create, FileAccess.Write);
                    StreamWriter u_sw = new StreamWriter(u_oput);
                    foreach (string str in unimplemented_list)
                        u_sw.WriteLine(str);
                    u_sw.Close();
                }
            }
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
				if(debug_level >= 1)
				{
					Console.WriteLine("Exception object: " + e.GetType().ToString());
					Console.WriteLine("Stack trace: \n{0}", e.StackTrace);
				}
                return -1;
            }
#endif

            if ((output_cinit != null) && (output_header == null))
            {
                if (output_cinit.Contains(".c"))
                    output_header = output_cinit.Substring(0, output_cinit.LastIndexOf(".c")) + ".h";
                else
                    output_header = output_cinit + ".h";
            }
            if (output_header != null)
                WriteHeader(m, ass);

            if (irdump != null)
            {
                StreamWriter idsw = new StreamWriter(new FileStream(irdump, FileMode.Create, FileAccess.Write));
                idsw.Write(ass.IrDump);
                idsw.Close();
            }

            if (debug_file != null)
            {
                FileStream fs = new FileStream(debug_file, FileMode.Create, FileAccess.Write);
                ass.debug.Write(fs);
                fs.Close();
            }

            return 0;
        }

        private static void WriteHeader(Metadata m, Assembler ass)
        {
            FileStream fs = new FileStream(output_header, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);

            StreamWriter hmsw = new StreamWriter(new MemoryStream());
            StreamWriter cmsw = new StreamWriter(new MemoryStream());

            StreamWriter oci = null;
            System.IO.FileInfo header_fi = new FileInfo(output_header);
            if (output_cinit != null)
            {
                oci = new StreamWriter(new FileStream(output_cinit, FileMode.Create, FileAccess.Write));
                oci.WriteLine("#include \"" + header_fi.Name + "\"");
                oci.WriteLine("#include <string.h>");
                oci.WriteLine("#include <stdlib.h>");
                oci.WriteLine("#include <stdint.h>");
                oci.WriteLine();
                oci.WriteLine("uint64_t Get_Symbol_Addr(const char *name);");
                oci.WriteLine();
                oci.WriteLine("static int32_t next_obj_id = -1;");
                oci.WriteLine();
            }

            List<string> advance_defines = new List<string>();
            List<string> external_defines = new List<string>();
            List<string> func_headers = new List<string>();

            EmitType(Metadata.GetTypeDef("mscorlib", "System", "Object", ass), hmsw, cmsw, advance_defines,
                external_defines, func_headers, ass);
            EmitType(Metadata.GetTypeDef("mscorlib", "System", "String", ass), hmsw, cmsw, advance_defines,
                external_defines, func_headers, ass);
            EmitArrayInit(hmsw, cmsw, func_headers, ass);

            foreach (Metadata.CustomAttributeRow car in m.Tables[(int)Metadata.TableId.CustomAttribute])
            {
                Assembler.MethodToCompile camtc = Metadata.GetMTC(car.Type, new Assembler.TypeToCompile(), null, ass);

                //Mangler.IncludeModule = false;
                string caname = Mangler2.MangleMethod(camtc, ass);
                //Mangler.IncludeModule = true;
                if (caname == "_ZX22OutputCHeaderAttributeM_0_7#2Ector_Rv_P1u1t")
                {
                    Metadata.TypeDefRow tdr = Metadata.GetTypeDef(car.Parent.ToToken(), ass);
                    EmitType(tdr, hmsw, cmsw, advance_defines, external_defines, func_headers, ass);
                }
            }

            sw.WriteLine("#include <stdint.h>");
            sw.WriteLine();
            sw.WriteLine("#ifdef INTPTR");
            sw.WriteLine("#undef INTPTR");
            sw.WriteLine("#endif");
            sw.WriteLine("#ifdef UINTPTR");
            sw.WriteLine("#undef UINTPTR");
            sw.WriteLine("#endif");
            sw.WriteLine();
            sw.WriteLine("#define INTPTR " + ass.GetCType(BaseType_Type.I));
            sw.WriteLine("#define UINTPTR " + ass.GetCType(BaseType_Type.U));
            sw.WriteLine();
            EmitArrayType(sw, ass);
            foreach (string s in advance_defines)
                sw.WriteLine(s);
            sw.WriteLine();
            if (oci != null)
            {
                foreach (string s2 in func_headers)
                    sw.WriteLine(s2);
                sw.WriteLine();
            }
            hmsw.Flush();
            StreamReader hmsr = new StreamReader(hmsw.BaseStream);
            hmsr.BaseStream.Seek(0, SeekOrigin.Begin);
            string hs = hmsr.ReadLine();
            while (hs != null)
            {
                sw.WriteLine(hs);
                hs = hmsr.ReadLine();
            }

            sw.Close();

            if (oci != null)
            {
                foreach (string s in external_defines)
                    oci.WriteLine(s);
                oci.WriteLine();

                cmsw.Flush();
                StreamReader cmsr = new StreamReader(cmsw.BaseStream);
                cmsr.BaseStream.Seek(0, SeekOrigin.Begin);
                string cs = cmsr.ReadLine();
                while (cs != null)
                {
                    oci.WriteLine(cs);
                    cs = cmsr.ReadLine();
                }
                oci.Close();
            }
        }

        private static void EmitType(Metadata.TypeDefRow tdr, StreamWriter hmsw, StreamWriter cmsw,
            List<string> advance_defines, List<string> external_defines, List<string> header_funcs, Assembler ass)
        {
            Layout l = Layout.GetTypeInfoLayout(new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(tdr, ass), type = tdr }, ass, false);
            //Layout l = tdr.GetLayout(new Signature.Param(new Token(tdr), ass).Type, ass, null);

            if (!tdr.IsEnum(ass))
            {
                hmsw.WriteLine("struct " + tdr.TypeNamespace + "_" + tdr.TypeName + " {");
                advance_defines.Add("struct " + tdr.TypeNamespace + "_" + tdr.TypeName + ";");
                foreach (Layout.Field iif in l.InstanceFields)
                    hmsw.WriteLine("    " + ass.GetCType(iif.field.fsig) + " " + iif.field.field.Name + ";");
                if (packed_structs)
                    hmsw.WriteLine("} __attribute__((__packed__));");
                else
                    hmsw.WriteLine("};");
            }
            else
            {
                // Identify underlying type
                string u_type = ass.GetUnderlyingCType(tdr);
                bool needs_comma = false;
                hmsw.WriteLine("enum " + tdr.TypeNamespace + "_" + tdr.TypeName + " {");
                for (Metadata.TableIndex ti = tdr.FieldList; ti < Metadata.GetLastField(tdr.m, tdr); ti++)
                {
                    Metadata.FieldRow vfr = ti.Value as Metadata.FieldRow;
                    if ((vfr.Flags & 0x8050) == 0x8050)
                    {
                        foreach (Metadata.ConstantRow cr in tdr.m.Tables[(int)Metadata.TableId.Constant])
                        {
                            if (cr.Parent.Value == vfr)
                            {
                                int val = ass.FromByteArrayI4(cr.Value);
                                if (needs_comma)
                                    hmsw.WriteLine(",");
                                hmsw.Write("    " + vfr.Name + " = " + val.ToString());
                                needs_comma = true;
                            }
                        }
                    }
                }
                hmsw.WriteLine();
                hmsw.WriteLine("};");
            }

            hmsw.WriteLine();

            if (output_cinit != null)
            {
                if (!tdr.IsValueType(ass))
                {
                    string init_func = "void Init_" + tdr.TypeNamespace + "_" + tdr.TypeName + "(struct " +
                        tdr.TypeNamespace + "_" + tdr.TypeName + " *obj)";
                    cmsw.WriteLine(init_func);
                    header_funcs.Add(init_func + ";");
                    cmsw.WriteLine("{");

                    if (l.has_vtbl)
                        cmsw.WriteLine("    obj->__vtbl = Get_Symbol_Addr(\"" + l.typeinfo_object_name + "\") + " + l.FixedLayout[Layout.ID_VTableStructure].Offset.ToString() + ";");
                    if (l.has_obj_id)
                        cmsw.WriteLine("    obj->__object_id = next_obj_id--;");

                    cmsw.WriteLine("}");
                    cmsw.WriteLine();

                    if (tdr.TypeFullName == "System.String")
                        EmitStringInit(tdr, hmsw, cmsw, advance_defines, external_defines, header_funcs, ass);
                }
            }
        }

        private static void EmitArrayInit(StreamWriter hmsw, StreamWriter cmsw, List<string> header_funcs, 
            Assembler ass)
        {
            EmitArrayInit(BaseType_Type.Object, "Ref", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.Byte, "Byte", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.Char, "Char", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.I, "I", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.I1, "I1", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.I2, "I2", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.I4, "I4", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.I8, "I8", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.U, "U", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.U1, "U1", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.U2, "U2", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.U4, "U4", hmsw, cmsw, header_funcs, ass);
            EmitArrayInit(BaseType_Type.U8, "U8", hmsw, cmsw, header_funcs, ass);
        }

        private static void EmitArrayType(StreamWriter hmsw, Assembler ass)
        {
            hmsw.WriteLine("struct __array");
            hmsw.WriteLine("{");
            hmsw.WriteLine("    " + ass.GetCType(BaseType_Type.I) + "          __vtbl;");
            hmsw.WriteLine("    int32_t          __object_id;");
            hmsw.WriteLine("    int64_t          __mutex_lock;");
            hmsw.WriteLine("    int32_t          rank;");
            hmsw.WriteLine("    int32_t          elem_size;");
            hmsw.WriteLine("    int32_t          inner_array_length;");
            hmsw.WriteLine("    " + ass.GetCType(BaseType_Type.I) + "          elemtype;");
            hmsw.WriteLine("    " + ass.GetCType(BaseType_Type.I) + "          lobounds;");
            hmsw.WriteLine("    " + ass.GetCType(BaseType_Type.I) + "          sizes;");
            hmsw.WriteLine("    " + ass.GetCType(BaseType_Type.I) + "          inner_array;");
            if (packed_structs)
                hmsw.WriteLine("} __attribute__((__packed__));");
            else
                hmsw.WriteLine("};");
            hmsw.WriteLine();
        }

        private static void EmitArrayInit(BaseType_Type baseType_Type, string tname, StreamWriter hmsw, StreamWriter cmsw,
            List<string> header_funcs, Assembler ass)
        {
            string typestr = ass.GetCType(baseType_Type);
            string init_func_name = "void Create_" + tname + "_Array(struct __array **arr_obj)";

            /* Arrays have four pieces of memory allocated:
             * - The array superblock
             * - The lobounds array
             * - The sizes array
             * - The actual array data
             * 
             * We do not allocate the last 3, as they may need to be placed at a different virtual address
             * when relocated - let the implementation decide this
             * 
             * Code is:
             * 
             * struct __array
             * {
             *     intptr           __vtbl;
             *     int32_t          __object_id;
             *     int64_t          __mutex_lock;
             *     int32_t          rank;
             *     int32_t          elem_size;
             *     intptr           lobounds;
             *     intptr           sizes;
             *     intptr           inner_array;
             * } __attribute__((__packed__));
             * 
             * void Create_X_Array(__array **arr_obj, int32_t num_elems)
             * {
             *     *arr_obj = (__array *)malloc(sizeof(arr_obj));
             *     (*arr_obj)->rank = 1;
             * }
             */

            int elem_size = ass.GetPackedSizeOf(new Signature.Param(baseType_Type));

            header_funcs.Add(init_func_name + ";");
            cmsw.WriteLine(init_func_name);
            cmsw.WriteLine("{");
            cmsw.WriteLine("    *arr_obj = (struct __array *)malloc(sizeof(struct __array));");
            cmsw.WriteLine("    (*arr_obj)->__object_id = next_obj_id--;");
            cmsw.WriteLine("    (*arr_obj)->rank = 1;");
            cmsw.WriteLine("    (*arr_obj)->elem_size = sizeof(" + typestr + ");");
            cmsw.WriteLine("}");
            cmsw.WriteLine();
        }

        private static void EmitStringInit(Metadata.TypeDefRow tdr, StreamWriter hmsw, StreamWriter cmsw,
            List<string> advance_defines, List<string> external_defines, List<string> header_funcs, Assembler ass)
        {
            // Emit a string creation instruction of the form:
            // void CreateString(System_String **obj, const char *s)

            string init_func = "void CreateString(struct System_String **obj, const char *s)";
            header_funcs.Add(init_func + ";");

            cmsw.WriteLine(init_func);
            cmsw.WriteLine("{");
            cmsw.WriteLine("    int l = strlen(s);");
            cmsw.WriteLine("    int i;");
            cmsw.WriteLine("    " + ass.GetCType(BaseType_Type.Char) + " *p;");
            cmsw.WriteLine("    *obj = (struct System_String *)malloc(sizeof(struct System_String) + l * sizeof(" +
                ass.GetCType(BaseType_Type.Char) + "));");
            cmsw.WriteLine("    Init_System_String(*obj);");
            cmsw.WriteLine("    (*obj)->length = l;");
            cmsw.WriteLine("    p = &((*obj)->start_char);");
            //cmsw.WriteLine("    p = (" + ass.GetCType(BaseType_Type.Char) +
            //    " *)(*obj + sizeof(struct System_String));");
            cmsw.WriteLine("    for(i = 0; i < l; i++)");
            cmsw.WriteLine("        p[i] = (" + ass.GetCType(BaseType_Type.Char) + ")s[i];");
            cmsw.WriteLine("}");
            cmsw.WriteLine();
        }

        private static bool ParseArgs(string[] args)
        {
            int i = 0;
            while (i < args.Length)
            {
                if (args[i] == "--arch")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;

                    arch = libtysila.Assembler.ParseArchitectureString(args[i]);
                    if (arch == null)
                        return false;
                }
                else if (args[i] == "-o")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    output_file = args[i];
                }
				else if (args[i] == "-d")
				{
					debug_level++;
				}
                else if (args[i] == "--output-header")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    output_header = args[i];
                }
                else if (args[i] == "--output-cinit")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    output_cinit = args[i];
                }
                else if (args[i] == "--irdump")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    irdump = args[i];
                }
                else if (args[i] == "--exclude")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    exclude_files.Add(args[i]);
                }
                else if (args[i] == "--include")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    include_files.Add(args[i]);
                }
                else if (args[i] == "--unimplemented")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    unimplemented = args[i];
                }
                else if (args[i] == "--requested-module-only")
                {
                    requested_module_only = true;
                }
                else if (args[i].StartsWith("-L"))
                {
                    if (args[i].Length > 2)
                        Program.search_dirs.Insert(0, args[i].Substring(2));
                    else
                    {
                        i++;
                        Program.search_dirs.Insert(0, args[i]);
                    }
                }
                else if (args[i] == "--extra-type")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    extra_types.Add(args[i]);
                }
                else if (args[i] == "--extra-typeinfo")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    extra_typeinfos.Add(args[i]);
                }
                else if (args[i] == "--extra-method")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    extra_methods.Add(args[i]);
                }
                else if (args[i] == "--rename-epoint")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    options.EntryPointName = args[i];
                }
                else if (args[i] == "--callconv")
                {
                    i++;
                    if ((i >= args.Length) || (args[i].StartsWith("-")))
                        return false;
                    options.CallingConvention = args[i];
                }
                else if (args[i].StartsWith("-f"))
                {
                    if (!BoolOptions.SetOption(args[i].Substring(2), options))
                        return false;
                }
                else if (args[i] == "-i")
                {
                    interactive = true;
                }
                else if (args[i] == "-q")
                {
                    quiet = true;
                }
                else if (args[i] == "-g")
                {
                    debug = true;
                }
                else if (args[i] == "--whole-module")
                {
                    whole_module = true;
                }
                else if (args[i] == "--profile")
                {
                    profile = true;
                }
                else if (args[i] == "-c")
                {
                    whole_module = true;
                    requested_module_only = true;
                }
                else if (args[i].StartsWith("-"))
                    return false;
                else
                {
                    input_file = args[i];
                    FileInfo fi = new FileInfo(input_file);
                    search_dirs.Add(fi.DirectoryName);
                }
                i++;
            }
            if (input_file == null)
                return false;
            return true;
        }

        public static string GetSupportedArchitecturesList()
        {
            StringBuilder sb = new StringBuilder();
            libtysila.Assembler.Architecture[] a = libtysila.Assembler.ListArchitectures();
            for (int i = 0; i < a.Length; i++)
            {
                sb.Append(a[i].ToString());
                if (i < (a.Length - 1))
                    sb.Append(", ");
            }
            return sb.ToString();
        }

        static void ShowHelp()
        {
            WriteLine(bplate);
            Console.WriteLine();
            string cmd_line = Environment.GetCommandLineArgs()[0];
            FileInfo fi = new FileInfo(cmd_line);
            Console.WriteLine("Usage: " + fi.Name + " [options] <input_file>");
            Console.WriteLine();
            WriteOption("-o <output_file>", "generate an object file");
            WriteOption("--arch <architecture>", "specify the target architecture");
            WriteOption("--output-header <name>", "output a C-like header for certain types as marked by the ABI.OutputCHeader attribute");
            WriteOption("--output-cinit <name>", "output a C file to initialize certain types implies --output-header");
            WriteOption("--irdump <irdump_file>", "dump the intermediate representation to a file");
            WriteOption("--unimplemented <file>", "list all referenced internal calls not provided internally in file.  Requires -o");
            WriteOption("-L <dir>", "add dir to the directory search list for references");
            WriteOption("-d", "increase debug level");
            WriteOption("-i", "start in interactive mode");
            WriteOption("--requested-module-only", "only compile items in the current module");
            WriteOption("--whole-module", "compile every non-generic type and method in the module, and every generic type and method which is referenced at least once");
            WriteOption("--exclude <file>", "do not compile symbols listed in <file>, symbols are mangled name with each symbol specified on a new line");
            WriteOption("--include <file>", "explicitly compile symbols listed in <file>, symbols are mangled name with each symbol specified on a new line");
            WriteOption("--rename-epoint <name>", "give the entry point function a different label in the output object file");
            WriteOption("--callconv <callconv>", "the calling convention to use.  Available calling conventions vary by platform.  " +
                "'default' is always available and 'gnu' is available on several platforms.  Can be overridden by the CallingConvention attribute.");
            WriteOption("-c", "compile the whole module, and nothing else.  Equivalent to --requested-module-only and --whole-module");
            WriteOption("--profile", "emit profiling stubs");
            WriteOption("-q", "quiet mode");
            WriteOption("-g", "generate debugging symbols in <output_file>.tydb");
            foreach (string[] f_opt in BoolOptions.Descriptions)
                WriteOption(f_opt[0], f_opt[1]);
            Console.WriteLine();
            Console.WriteLine("The following options require <label> to be a mangled name:");
            WriteOption("--extra-type <label>", "request that a particular type is included");
            WriteOption("--extra-typeinfo <label>", "request that a particular typeinfo is included");
            WriteOption("--extra-method <label>", "request that a particular method is included");
            Console.WriteLine();
            WriteLine("Supported architectures: " + GetSupportedArchitecturesList());
            Console.WriteLine();
            Console.WriteLine();
        }

        static int WindowWidth
        {
            get
            {
                int scr_width = 80;
                try { scr_width = Console.WindowWidth; }
                catch (Exception) { }
                return scr_width;
            }
        }

        private static void WriteLine(string line)
        {
            foreach (string s in SplitStrings(line, WindowWidth - 1))
                Console.WriteLine(s);
        }

        private static void WriteOption(string option, string description)
        {
            int scr_width = WindowWidth;

            int opt_width = 24;

            if (option.Length > opt_width)
                opt_width = option.Length;

            int desc_width = scr_width - opt_width - 4;

            string[] split_strings = SplitStrings(description, desc_width);
            string fmt_string = " {0,-" + opt_width.ToString() + "} {1}{2}";

            Console.WriteLine(fmt_string, option, "", split_strings[0]);
            for (int i = 1; i < split_strings.Length; i++)
                Console.WriteLine(fmt_string, "", " ", split_strings[i]);            
        }

        private static string[] SplitStrings(string s, int length)
        {
            // Split a string on word boundaries
            List<string> ret = new List<string>();
            string[] split_nls = s.Split(new string[] { "\n" }, StringSplitOptions.None);


            foreach (string split_nl in split_nls)
            {
                string[] split = split_nl.Split(' ');
                StringBuilder sb = new StringBuilder();

                foreach (string sp in split)
                {
                    if (sb.Length == 0)
                        sb.Append(sp);
                    else
                    {
                        if ((sb.Length + 1 + sp.Length) > length)
                        {
                            ret.Add(sb.ToString());
                            sb = new StringBuilder(sp);
                        }
                        else if ((sb.Length + 1 + sp.Length) == length)
                        {
                            sb.Append(" ");
                            sb.Append(sp);
                            ret.Add(sb.ToString());
                            sb = new StringBuilder();
                        }
                        else
                        {
                            sb.Append(" ");
                            sb.Append(sp);
                        }
                    }
                }

                if ((ret.Count == 0) || (sb.Length > 0))
                    ret.Add(sb.ToString());
            }

            return ret.ToArray();
        }

        class BoolOptions
        {
            class Option
            {
                public string cmd_line_name;
                public string ao_name;
                public string description;
                public string[] enable_depends;
                public string[] disable_depends;
                public bool? _default;
                public bool display = true;
                public bool allow = true;
            }

            static Dictionary<string, Option> options = new Dictionary<string, Option>();

            static BoolOptions()
            {
                options.Add("exceptions", new Option
                {
                    cmd_line_name = "exceptions",
                    ao_name = "EnableExceptions",
                    description = "exceptions",
                    enable_depends = new string[] { },
                    disable_depends = new string[] { }
                });
                options.Add("rtti", new Option
                {
                    cmd_line_name = "rtti",
                    ao_name = "EnableRTTI",
                    description = "run-time type information",
                    enable_depends = new string[] { },
                    disable_depends = new string[] { "no-exceptions" }
                });
                options.Add("profile", new Option
                {
                    cmd_line_name = "profile",
                    ao_name = "Profile",
                    description = "calls to external profile interface",
                    enable_depends = new string[] { },
                    disable_depends = new string[] { }
                });
                options.Add("coalesce", new Option
                {
                    cmd_line_name = "coalesce",
                    ao_name = "CoalesceGenericRefTypes",
                    description = "coalescing generic reference type method implementations",
                    enable_depends = new string[] { },
                    disable_depends = new string[] { }
                });
                options.Add("includelibsupcs", new Option
                {
                    cmd_line_name = "includelibsupcs",
                    ao_name = "IncludeLibsupcs",
                    allow = false
                });
                options.Add("includelibstdcs", new Option
                {
                    cmd_line_name = "includelibstdcs",
                    ao_name = "IncludeLibstdcs",
                    allow = false
                });
                options.Add("miniassembler", new Option
                {
                    cmd_line_name = "miniassembler",
                    ao_name = "MiniAssembler",
                    allow = false
                });
                options.Add("inextraadd", new Option
                {
                    cmd_line_name = "inextraadd",
                    ao_name = "InExtraAdd",
                    allow = false
                });
                options.Add("pic", new Option
                {
                    cmd_line_name = "pic",
                    ao_name = "PIC",
                    description = "position-independent code",
                    enable_depends = new string[] { },
                    disable_depends = new string[] { }
                });

                // Now add in any extra members found in AssemblerOptions
                libtysila.Assembler.AssemblerOptions ao = new Assembler.AssemblerOptions();
                System.Type t = typeof(libtysila.Assembler.AssemblerOptions);
                foreach (System.Reflection.FieldInfo fi in t.GetFields())
                {
                    if (fi.FieldType != typeof(bool))
                        continue;

                    Option o = null;
                    foreach (Option test_o in options.Values)
                    {
                        if (test_o.ao_name == fi.Name)
                        {
                            o = test_o;
                            break;
                        }
                    }

                    if (o == null)
                    {
                        o = new Option { cmd_line_name = fi.Name.ToLower(), ao_name = fi.Name, description = fi.Name, enable_depends = new string[] { }, disable_depends = new string[] { } };
                        options.Add(o.cmd_line_name, o);
                    }

                    if (!o._default.HasValue)
                        o._default = (bool)fi.GetValue(ao);
                }
            }

            public static bool SetOption(string option, libtysila.Assembler.AssemblerOptions opts)
            {
                bool set = true;
                if (option.StartsWith("no-"))
                {
                    set = false;
                    option = option.Substring(3);
                }

                if (!options.ContainsKey(option))
                    return false;

                Option o = options[option];

                if (o.allow == false)
                    return false;

                System.Reflection.FieldInfo fi = opts.GetType().GetField(o.ao_name);
                if (fi != null)
                    fi.SetValue(opts, set);

                string[] depends = null;
                if (set)
                    depends = o.enable_depends;
                else
                    depends = o.disable_depends;

                if (depends != null)
                {
                    foreach (string s in depends)
                        SetOption(s, opts);
                }

                return true;
            }

            public static string[][] Descriptions
            {
                get
                {
                    List<string[]> ret = new List<string[]>();

                    foreach (Option o in options.Values)
                    {
                        if (!o.display || !o.allow)
                            continue;

                        StringBuilder sb = new StringBuilder();

                        string opt = null;
                        string[] depends = null;

                        if (o._default == false)
                        {
                            opt = "-f" + o.cmd_line_name;
                            sb.Append("enable ");
                            depends = o.enable_depends;
                        }
                        else
                        {
                            opt = "-fno-" + o.cmd_line_name;
                            sb.Append("disable ");
                            depends = o.disable_depends;
                        }

                        sb.Append(o.description);

                        if ((depends != null) && (depends.Length > 0))
                        {
                            sb.Append(" (implies ");
                            for (int i = 0; i < depends.Length; i++)
                            {
                                if (i > 0)
                                    sb.Append(", ");
                                sb.Append("-f");
                                sb.Append(depends[i]);
                            }
                            sb.Append(")");
                        }

                        ret.Add(new string[] { opt, sb.ToString() });
                    }
                    return ret.ToArray();
                }
            }
        }
    }
}
