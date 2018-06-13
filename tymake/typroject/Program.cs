/* Copyright (C) 2014 by John Cronin
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace typroject
{
    public class Program
    {
        static string tools_ver_override = null;
        public static int platform { get { return (int)Environment.OSVersion.Platform; } }

        static int Main(string[] args)
        {
            string fname = null;
            List<string> sources = new List<string>();
            string cur_dir = Environment.CurrentDirectory;
            List<string> extra_libs = new List<string>();
            List<string> extra_libdirs = new List<string>();
            List<string> extra_defines = new List<string>();
            string config = "Release";
            bool do_unsafe = false;
            bool werror = false;

            bool emit_depends = false;

            /* parse args */
            int i = 0;
            bool doing_opts = true;
            while (i < args.Length)
            {
                string cur_arg = args[i];

                if (cur_arg.StartsWith("-"))
                {
                    if(doing_opts)
                    {
                        // its an option
                        if (cur_arg == "--help")
                        {
                            dump_opts();
                            return -1;
                        }
                        if (cur_arg == "-l" || cur_arg == "-L" || cur_arg == "-D" || cur_arg == "-C" || cur_arg == "-T")
                        {
                            i++;
                            if (i == args.Length || args[i].StartsWith("-"))
                            {
                                dump_opts();
                                return -1;
                            }
                            cur_arg = cur_arg + args[i];
                        }

                        if (cur_arg.StartsWith("-l"))
                            extra_libs.Add(cur_arg.Substring("-l".Length));
                        else if (cur_arg.StartsWith("-L"))
                            extra_libs.Add(cur_arg.Substring("-L".Length));
                        else if (cur_arg.StartsWith("-D"))
                            extra_defines.Add(cur_arg.Substring("-D".Length));
                        else if (cur_arg.StartsWith("-C"))
                            config = cur_arg.Substring("-C".Length);
                        else if (cur_arg.StartsWith("-T"))
                            tools_ver_override = cur_arg.Substring("-T".Length);
                        else if (cur_arg == "-M")
                            emit_depends = true;
                        else if (cur_arg == "--unsafe")
                            do_unsafe = true;
                        else if(cur_arg == "-Werror")
                            werror = true;

                    }
                    else
                    {
                        dump_opts();
                        return -1;
                    }
                }
                else
                {
                    doing_opts = false;
                    if(fname == null)
                        fname = cur_arg;
                    sources.Add(cur_arg);
                }
                i++;
            }

            string test_dir = cur_dir;

            if (fname != null)
            {
                FileInfo fi_fname = new FileInfo(fname);
                if (fi_fname.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                {
                    test_dir = fname;
                    fname = null;
                    sources.Clear();
                }
            }

            if (fname == null)
            {
                /* try and guess file name */
                DirectoryInfo cdir = new DirectoryInfo(test_dir);

				FileInfo[] projs = cdir.GetFiles("*.csproj");
                if (projs.Length > 1)
                {
                    Console.WriteLine("No file specified and more than one project in current directory");
                    dump_opts();
                    return -1;
                }
                else if (projs.Length == 0)
                {
                    FileInfo[] srcs = cdir.GetFiles("*.cs");
                    if (srcs.Length == 0)
                    {
                        Console.WriteLine("No file specified and no valid project or source files found in current directory.");
                        dump_opts();
                        return -1;
                    }

                    foreach (FileInfo src in srcs)
                    {
                        sources.Add(src.FullName);
                    }
                    fname = sources[0];
                }
                else
                    fname = projs[0].FullName;
            }

            FileInfo proj_file = new FileInfo(fname);
            if (proj_file.Exists == false)
            {
                Console.WriteLine("Project file not found: " + fname);
                dump_opts();
                return -1;
            }

            Project p;
            if (fname.EndsWith(".csproj") || fname.EndsWith(".proj"))
                p = Project.xml_read(proj_file.OpenRead(), config, proj_file.DirectoryName, cur_dir);
            else if (fname.EndsWith(".cs"))
                p = Project.src_read(sources, cur_dir, Project.OutputType.Exe);
            else
            {
                Console.WriteLine("Unable to interpret project file: " + fname);
                dump_opts();
                return -1;
            }

            if (emit_depends)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(p.OutputFile);
                sb.Append(": ");

                foreach (string src in p.Sources)
                {
                    sb.Append(src);
                    sb.Append(" ");
                }

                foreach (Project pref in p.ProjectReferences)
                {
                    sb.Append(pref.OutputFile);
                    sb.Append(" ");
                }

                sb.Append(Environment.NewLine);

                string depends = sb.ToString();

                string depend_file = fname.Substring(0, fname.LastIndexOf('.')) + ".d";
                FileInfo depend_fi = new FileInfo(depend_file);
                FileStream fs = new FileStream(depend_file, FileMode.Create, FileAccess.Write);
                byte[] data = new UTF8Encoding(false).GetBytes(depends);
                fs.Write(data, 0, data.Length);
                fs.Close();
            }
            else
            {
                int ret = p.build(extra_defines, extra_libdirs, extra_libs, do_unsafe);
                if (ret != 0)
                    return ret;
            }

            return 0;
        }

        private static void dump_opts()
        {
            throw new NotImplementedException();
        }

        public static string csc(string tools_ver)
        {
            bool is_v4plus;
            return csc(tools_ver, out is_v4plus);
        }

        static string get_dotnetsdk()
        {
            // Find dotnet.exe in path
            string dotnet = null;

            var path = Environment.GetEnvironmentVariable("PATH").Split(';');
            foreach(var pi in path)
            {
                try
                {
                    var test = new FileInfo(Project.add_dir_split(pi) + "dotnet.exe");
                    if (test.Exists)
                        dotnet = test.FullName;
                }
                catch (Exception)
                { }
            }

            if (dotnet == null)
                return null;

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = dotnet;
            p.StartInfo.Arguments = "--info";
            p.Start();

            string ret = null;
            while (!p.StandardOutput.EndOfStream)
            {
                /* Find the line starting Base Path */
                string output = p.StandardOutput.ReadLine().Trim();
                if(output.StartsWith("Base Path:"))
                {
                    output = output.Substring("Base Path:".Length).Trim();
                    ret = output;
                }
            }

            p.WaitForExit();

            return ret;
        }

        public static string csc(string tools_ver, out bool is_v4plus)
        {
            if (tools_ver_override != null)
                tools_ver = tools_ver_override;

            var dn = get_dotnetsdk();
            if (dn != null)
            {
                FileInfo roslyn = new FileInfo(Project.add_dir_split(Project.add_dir_split(dn) + "Roslyn") + "RunCsc.cmd");
                if (roslyn.Exists)
                {
                    is_v4plus = true;
                    return roslyn.FullName;
                }
                roslyn = new FileInfo(Project.add_dir_split(Project.add_dir_split(Project.add_dir_split(dn) + "Roslyn") + "bincore") + "RunCsc.cmd");
                if (roslyn.Exists)
                {
                    is_v4plus = true;
                    return roslyn.FullName;
                }
            }

            if (tools_ver.StartsWith("4.5") || tools_ver.StartsWith("4.6"))
                tools_ver = "4.0";

            if (platform == 0)
            {
                // assume mono for unix
                is_v4plus = false;
                if ((tools_ver == "2.0") || (tools_ver == "3.0") || (tools_ver == "3.5") || (tools_ver == "4.0"))
                    return "gmcs";
                else
                    return "mcs";
            }
            else
            {
                // assume csc for windows

                if(double.Parse(tools_ver) >= 12.0)
                {
                    // this is a visual studio version and tools are in program files/msbuild/x/bin
                    is_v4plus = true;
                    var ret = Project.add_dir_split(Project.add_dir_split(Project.add_dir_split(Project.add_dir_split(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) + "MSBuild") + tools_ver) + "bin") + "csc.exe";
                    return ret;
                }

                string windir = Environment.GetEnvironmentVariable("windir");
                string framework_dir = windir + "\\Microsoft.NET\\Framework";
                DirectoryInfo fdi = new DirectoryInfo(framework_dir);

                // if there is a version 4+ csc, we can use it and then use the
                //  multi-framework targetting feature to specify the actual
                //  framework
                DirectoryInfo[] all_matches = fdi.GetDirectories("v*");
                List<string> all_matches_dirs = new List<string>();
                foreach(var match in all_matches)
                {
                    FileInfo[] file_matches = match.GetFiles("csc.exe");
                    if (file_matches.Length == 1)
                    {
                        all_matches_dirs.Add(file_matches[0].FullName);
                    }
                }
                all_matches_dirs.Sort();
                var last_all_match = all_matches_dirs[all_matches_dirs.Count - 1];
                if (last_all_match[1] >= '4')
                {
                    is_v4plus = true;
                    return last_all_match;
                }

                // there is no v4+ framework installed, therefore fall
                //  back to searching for a specific compiler
                DirectoryInfo[] matches = fdi.GetDirectories("v" + tools_ver + "*");

                foreach (DirectoryInfo match in matches)
                {
                    FileInfo[] file_matches = match.GetFiles("csc.exe");
                    if (file_matches.Length == 1)
                    {
                        is_v4plus = file_matches[0].FullName[1] >= '4';
                        return file_matches[0].FullName;
                    }
                }

                // fallback
                is_v4plus = false;
                return "csc.exe";
            }
        }

        public static string replace_dir_split(string input)
        {
            if (platform != 0)
                return input.Replace('/', '\\');
            else
                return input;
        }

        public static string ref_dir(string tools_ver)
        {
            if (tools_ver_override != null)
                tools_ver = tools_ver_override;

            if (platform == 0)
                return "";
            else
            {
                double tvd;
                if (tools_ver.StartsWith("v"))
                    tools_ver = tools_ver.Substring(1);
                if (double.TryParse(tools_ver, out tvd) && tvd >= 12.0)
                {
                    // this is a visual studio version and libs are in program files/msbuild/x
                    var ret = Project.add_dir_split(Project.add_dir_split(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) + "MSBuild") + tools_ver;
                    return ret;
                }

                /* Get from the Reference Assemblies folder */
                var ref_ass_path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v" + tools_ver + "\\mscorlib.dll";
                var rap_fi = new FileInfo(ref_ass_path);
                if (rap_fi.Exists)
                    return rap_fi.DirectoryName;

                string fwork_ver;
                if (tools_ver == "4.5")
                    tools_ver = "4.0";

                if ((tools_ver == "2.0") || (tools_ver == "3.0") || (tools_ver == "3.5") || (tools_ver == "4.0"))
                    fwork_ver = "2.0";
                else
                    fwork_ver = tools_ver;

                string windir = Environment.GetEnvironmentVariable("windir");
                string framework_dir = windir + "\\Microsoft.NET\\Framework";
                DirectoryInfo fdi = new DirectoryInfo(framework_dir);
                DirectoryInfo[] matches = fdi.GetDirectories("v" + fwork_ver + "*");

                foreach (DirectoryInfo match in matches)
                {
                    FileInfo[] file_matches = match.GetFiles("System.dll");
                    if (file_matches.Length == 1)
                        return file_matches[0].DirectoryName;
                }

                // fallback
                return framework_dir + "\\v2.0.50727\\";
            }
        }

        internal static object resgen(string tools_ver)
        {
            throw new NotImplementedException();
        }
    }

    public class Project
    {
        public string ProjectFile;
        public string ProjectName;
        public Guid Guid;
        public string OutputFile;

        public string configuration;
        internal string curdir;
        internal Uri uri_basedir, uri_curdir;
        internal Dictionary<string, string> properties;
        public string tools_ver;
        public string lib_dir;
        public OutputType output_type;
        public string assembly_name;
        public string extra_add;
        public string output_path;
        public string defines;

        public List<string> Sources = new List<string>();
        public List<string> References = new List<string>();
        public List<string> Resources = new List<string>();
        public List<string> ResourceLogicalNames = new List<string>();
        public List<Project> ProjectReferences = new List<Project>();

        internal XmlNamespaceManager nm;
        internal Dictionary<string, XPathNavigator> targets = new Dictionary<string, XPathNavigator>();
        internal Dictionary<string, List<string>> items = new Dictionary<string, List<string>>();
        internal Dictionary<string, Type> tasks = new Dictionary<string, Type>();

        internal Dictionary<string, string> reference_overrides = null;

        public string ErrorMessage;

        public Project() { }
        public Project(string filename, string name, Guid guid)
        { ProjectFile = filename; ProjectName = name; Guid = guid; }

        public enum OutputType { Exe, Library };

        public static Project xml_read(Stream file, string config, string basedir, string curdir, List<string> imports = null, List<string> ref_overrides = null)
        {
            Project ret = new Project();
            return xml_read(ret, file, config, basedir, curdir, null, imports, ref_overrides);
        }

        public static Project sources_read(Stream file, string config, string basedir, string curdir)
        {
            Project ret = new Project();
            StreamReader sr = new StreamReader(file);
            ret.sources_read(sr, config, basedir, curdir);
            return ret;
        }

        internal void sources_read(StreamReader file, string config, string basedir, string curdir)
        {
            basedir = add_dir_split(basedir);
            curdir = add_dir_split(curdir);
            Uri uri_basedir = new Uri(basedir);
            Uri uri_curdir = new Uri(curdir);

            while (!file.EndOfStream)
            {
                string line = file.ReadLine();

                if (line.StartsWith("#define "))
                    defines += line.Substring("#define ".Length) + " ";
                else if (line.StartsWith("#outdir "))
                    output_path = rel_path(line.Substring("#outdir ".Length), uri_basedir, uri_curdir);
                else if (line.StartsWith("#assemblyname "))
                    assembly_name = line.Substring("#assemblyname ".Length);
                else if (line.StartsWith("#target "))
                {
                    string ot = line.Substring("#target ".Length).ToLower();
                    if (ot == "library")
                        output_type = OutputType.Library;
                    else if (ot == "exe")
                        output_type = OutputType.Exe;
                }
                else if (line.StartsWith("#ref "))
                    References.Add(line.Substring("#ref ".Length));
                else if (line.StartsWith("#extra "))
                    extra_add += line.Substring("#extra ".Length) + " ";
                else if (line.StartsWith("#tools "))
                    tools_ver = line.Substring("#tools ".Length);
                else if (line.StartsWith("#")) continue; // ignore
                else
                {
                    if (line.Length > 0)
                        Sources.Add(rel_path(line.Trim(), uri_basedir, uri_curdir));
                }
            }

            if (output_path == null)
                output_path = ".";
            if (OutputFile == null)
            {
                if (assembly_name == null)
                {
                    string bname = null;
                    if (Sources.Count > 0)
                    {
                        FileInfo s0_fi = new FileInfo(Sources[0]);
                        int ext_len = 0;
                        if (s0_fi.Extension.Length > 0)
                            ext_len = s0_fi.Extension.Length + 1;
                        bname = s0_fi.Name.Substring(0, s0_fi.Name.Length - ext_len);
                    }
                    else
                        bname = "out";

                    switch (output_type)
                    {
                        case OutputType.Exe:
                            assembly_name += ".exe";
                            break;
                        case OutputType.Library:
                            assembly_name += ".dll";
                            break;
                    }
                }

                OutputFile = rel_path(assembly_name, uri_basedir, uri_curdir);
            }
        }

        internal static Project src_read(List<string> sources, string curdir, OutputType otype)
        {
            Project ret = new Project();
            curdir = add_dir_split(curdir);
            Uri uri_curdir = new Uri(curdir);

            /* Decide on an output filename */
            string output_file = sources[0];
            output_file = output_file.Substring(0, output_file.LastIndexOf('.'));
            if (otype == OutputType.Library)
                output_file = output_file + ".dll";
            else
                output_file = output_file + ".exe";
            ret.output_type = otype;

            ret.OutputFile = rel_path(output_file, null, uri_curdir);

            FileInfo fi = new FileInfo(ret.OutputFile);
            ret.ProjectName = fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length);

            foreach(string src in sources)
                ret.Sources.Add(rel_path(src, null, uri_curdir));

            ret.properties = new Dictionary<string, string>();

            return ret;
        }

        internal static Project xml_read(Project ret, Stream file, string config, string basedir, string curdir, Dictionary<string, string> props = null,
            List<string> imports = null, List<string> ref_overrides = null)
        {
            /* Load a project from the project file specified */
            basedir = add_dir_split(basedir);
            curdir = add_dir_split(curdir);
            Uri uri_basedir = new Uri(basedir);
            Uri uri_curdir = new Uri(curdir);

            /* Reference overrides are of the form assembly_name = path */
            if (ref_overrides != null)
            {
                if (ret.reference_overrides == null)
                    ret.reference_overrides = new Dictionary<string, string>();
                foreach(var ro in ref_overrides)
                {
                    var ros = ro.Split('=');
                    ret.reference_overrides[ros[0].Trim()] = ros[1].Trim();
                }
            }

            XPathDocument r = new XPathDocument(file);
            XPathNavigator n = r.CreateNavigator();
            foreach(XPathNavigator cn in n.SelectChildren(XPathNodeType.Element))
            {
                n = cn;
                break;
            }
            XmlNamespaceManager nm = new XmlNamespaceManager(n.NameTable);
            nm.AddNamespace("d", n.NamespaceURI);

            ret.configuration = config;
            ret.curdir = curdir;
            ret.uri_basedir = uri_basedir;
            ret.uri_curdir = uri_curdir;
            ret.nm = nm;

            /* Default properties */
            bool is_proj = false;
            if (props == null)
            {
                props = new Dictionary<string, string>();
                props["BuildType"] = config;
                props["BuildArch"] = "x64";

                props["MSBuildNodeCount"] = "1";

                props["MSBuildBinPath"] = new FileInfo(typeof(Program).Module.FullyQualifiedName).DirectoryName;
                props["MSBuildExtensionsPath"] = props["MSBuildBinPath"];
                props["MSBuildToolsVersion"] = "typ1.0";

                props["__BuildOS"] = "Linux";
                props["__BuildArch"] = "x64";
                props["OSGroup"] = "Linux";
                props["TargetsUnix"] = "true";

                props["Configuration"] = config;

                is_proj = true;

                ret.tasks["GenerateResourcesCode"] = typeof(Microsoft.DotNet.Build.Tasks.GenerateResourcesCode);

            }
            ret.properties = props;

            /* Get the actual file name if we have it */
            var fs = file as FileStream;
            if(fs != null)
            {
                var fsi = new FileInfo(fs.Name);
                props["MSBuildThisFileFullPath"] = fsi.FullName;
                props["MSBuildThisFileName"] = fsi.Name.Substring(0, fsi.Name.Length - fsi.Extension.Length);
                props["MSBuildThisFileExtension"] = fsi.Extension;
                props["MSBuildThisFileDirectory"] = add_dir_split(fsi.DirectoryName);
                props["MSBuildThisFileDirectoryNoRoot"] = remove_drive(add_dir_split(fsi.DirectoryName));

                if(is_proj)
                {
                    props["MSBuildProjectFullPath"] = props["MSBuildThisFileFullPath"];
                    props["MSBuildProjectName"] = props["MSBuildThisFileName"];
                    props["MSBuildProjectExtension"] = props["MSBuildThisFileExtension"];
                    props["MSBuildProjectDirectory"] = remove_dir_split(props["MSBuildThisFileDirectory"]);
                    props["MSBuildProjectDirectoryNoRoot"] = remove_dir_split(props["MSBuildThisFileDirectoryNoRoot"]);

                    if (fsi.Name == "System.Private.CoreLib.csproj")
                    {
                        props["MSBuildProjectName"] = "mscorlib";
                        props["OutputFile"] = "mscorlib.dll";
                        props["AssemblyName"] = "mscorlib";
                    }

                    var deftargs = n.SelectSingleNode("@DefaultTargets", nm);
                    if(deftargs != null)
                    {
                        props["MSBuildProjectDefaultTargets"] = deftargs.Value;
                    }
                }
            }

            /* process any imports */
            if(imports != null)
            {
                foreach(var import in imports)
                {
                    //process_import(new FileInfo(import), ret);
                }
            }

            /* Iterate through all child nodes */
            foreach (XPathNavigator cn in n.Select("child::*", nm))
            {
                process_node(cn, ret, props, nm);
            }

            if(!is_proj)
                return ret;

            /* First find the properties of this project */
            ret.configuration = props["Configuration"];

            if (props.ContainsKey("TargetFrameworkVersion"))
                ret.tools_ver = props["TargetFrameworkVersion"];
            else
                ret.tools_ver = n.SelectSingleNode("@ToolsVersion", nm).Value;

            if(props.ContainsKey("ProjectGuid"))
                ret.Guid = new Guid(props["ProjectGuid"]);
            string otype = props["OutputType"];
            if(otype == "Library")
                ret.output_type = OutputType.Library;
            else if(otype == "Exe")
                ret.output_type = OutputType.Exe;
            else
                throw new Exception("Unknown output type: " + otype);

            try
            {
                if (props["MSBuildProjectName"] == "mscorlib")
                    ret.assembly_name = "mscorlib";
                else
                    ret.assembly_name = props["AssemblyName"];
            }
            catch(KeyNotFoundException)
            {
                ret.assembly_name = props["MSBuildProjectName"];
            }

            if (ret.ProjectName == null)
                ret.ProjectName = ret.assembly_name;

            ret.extra_add = "";
            ret.defines = "";
            if (get_prop("Optimize", props).ToLower() == "true")
                ret.extra_add = ret.extra_add + " /optimize";
            if (get_prop("DefineDebug", props).ToLower() == "true")
                ret.defines = ret.defines + ";DEBUG";
            if (get_prop("DefineTrace", props).ToLower() == "true")
                ret.defines = ret.defines + ";TRACE";
            if (get_prop("DebugType", props) != "")
                ret.extra_add = ret.extra_add + " /debug:" + get_prop("DebugType", props);
            if(get_prop("AllowUnsafeBlocks", props).ToLower() == "true")
                ret.extra_add += " /unsafe";
            ret.defines = ret.defines + ";" + get_prop("DefineConstants", props);
            if (get_prop("NoStdLib", props).ToLower() == "true")
                ret.extra_add += " /nostdlib";
            if (get_prop("ProduceReferenceAssembly", props).ToLower() == "true")
                ret.extra_add += " /refout";
            if (get_prop("Deterministic", props).ToLower() == "true")
                ret.extra_add += " /deterministic";
            if (get_prop("NoWin32Manifest", props).ToLower() == "true")
                ret.extra_add += " /nowin32manifest";
            if (get_prop("ModuleAssemblyName", props) != "")
                ret.extra_add += " /moduleassemblyname:" + get_prop("ModuleAssemblyName", props);
            if (get_prop("WarningLevel", props) != "")
                ret.extra_add += " /warn:" + get_prop("WarningLevel", props);
            if (get_prop("TreatWarningsAsErrors", props).ToLower() == "true")
                ret.extra_add += " /warnaserror+";
            if (get_prop("NoWarn", props) != "")
                ret.extra_add += " /nowarn:" + get_prop("NoWarn", props);

            string output_path = new DirectoryInfo("./").FullName;
            if (props.ContainsKey("OutputPath"))
                output_path = props["OutputPath"];
            ret.output_path = rel_path(output_path, uri_basedir, uri_curdir);
            string ext = "";
            if (ret.output_type == OutputType.Library)
                ext = ".dll";
            else if (ret.output_type == OutputType.Exe)
                ext = ".exe";
            string output_file = output_path + ret.assembly_name + ext;
            ret.OutputFile = rel_path(output_file, uri_basedir, uri_curdir);


            /* Sanitize defines */
            var defs = ret.defines.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            ret.defines = string.Join(";", defs);

            return ret;
        }

        private static string get_prop(string name, Dictionary<string, string> props)
        {
            if (props.ContainsKey(name))
                return props[name];
            else
                return "";
        }

        private static void process_node(XPathNavigator n, Project ret, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            if(process_condition(n, props, nm))
            {
                if (n.Name == "Import")
                    process_import(n, ret, props, nm);
                else if (n.Name == "PropertyGroup")
                    process_propertygroup(n, ret, props, nm);
                else if (n.Name == "ItemGroup")
                    process_itemgroup(n, ret, props, nm);
                else if(n.Name == "ItemDefinitionGroup")
                {
                    // ignore
                }
                else if(n.Name == "Target")
                {
                    if (process_condition(n, props, nm))
                        ret.targets[n.SelectSingleNode("@Name", nm).Value] = n;
                }
                else if(n.Name == "UsingTask")
                {
                    // ignore
                }
                else if(n.Name == "Choose")
                {
                    if(process_condition(n, props, nm))
                    {
                        bool when_done = false;
                        foreach(XPathNavigator cn in n.Select("child::*", nm))
                        {
                            if(cn.Name == "When")
                            {
                                if(process_condition(cn, props, nm))
                                {
                                    foreach (XPathNavigator wcn in cn.Select("child::*", nm))
                                    {
                                        process_node(wcn, ret, props, nm);
                                    }
                                    when_done = true;
                                    break;
                                }
                            }
                            else if(cn.Name == "Otherwise" && !when_done)
                            {
                                foreach (XPathNavigator wcn in cn.Select("child::*", nm))
                                {
                                    process_node(wcn, ret, props, nm);
                                }
                                when_done = true;
                                break;
                            }
                        }
                    }
                }
                else
                    throw new NotImplementedException();
            }
        }

        private static void process_itemgroup(XPathNavigator n, Project ret, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            if (n.InnerXml.Contains("SafeFileHandle.Windows.cs"))
                System.Diagnostics.Debugger.Break();
            if(process_condition(n, props, nm))
            {
                foreach (XPathNavigator cn in n.Select("child::*"))
                {
                    process_item(cn, ret, props, nm);
                }
            }
        }

        private static void process_item(XPathNavigator n, Project ret, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            if(process_condition(n, props, nm))
            {
                if (n.Name == "Compile")
                {
                    var dependsn = n.SelectSingleNode("@DependentUpon", nm);
                    if (dependsn != null)
                        throw new NotImplementedException();
                    var autogenn = n.SelectSingleNode("@AutoGen", nm);
                    if (autogenn != null)
                        throw new NotImplementedException();
                    var fname = process_string(n.SelectSingleNode("@Include", nm).Value, props);

                    var rp = rel_path(fname, ret.uri_basedir, ret.uri_curdir);
                    if (!ret.Sources.Contains(rp))
                        ret.Sources.Add(rp);
                }
                else if(n.Name == "EmbeddedResource")
                {
                    var dependsn = n.SelectSingleNode("@DependentUpon", nm);
                    if (dependsn != null)
                        throw new NotImplementedException();
                    var autogenn = n.SelectSingleNode("@AutoGen", nm);
                    if (autogenn != null)
                        throw new NotImplementedException();
                    var fname = process_string(n.SelectSingleNode("@Include", nm).Value, props);
                    var lname = process_string(n.SelectSingleNode("./d:LogicalName", nm).Value, props);

                    var rp = rel_path(fname, ret.uri_basedir, ret.uri_curdir);
                    if (!ret.Resources.Contains(rp))
                    {
                        ret.Resources.Add(rp);
                        ret.ResourceLogicalNames.Add(lname);
                    }
                }
                else if(n.Name == "ProjectReference")
                {
                    var prfname = rel_path(process_string(n.SelectSingleNode("@Include", nm).Value, props, ret.items), ret.uri_basedir, ret.uri_curdir);

                    Project pref = new Project();

                    var prguidn = n.SelectSingleNode("./d:Project", nm);
                    if(prguidn != null)
                        pref.Guid = new Guid(process_string(prguidn.Value, props, ret.items));
                    var prnamen = n.SelectSingleNode("./d:Name", nm);
                    if(prnamen != null)
                        pref.ProjectName = process_string(prnamen.Value, props, ret.items);

                    pref.ProjectFile = prfname;
                    FileInfo pref_fi = new FileInfo(pref.ProjectFile);
                    xml_read(pref, pref_fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ret.configuration, pref_fi.DirectoryName, ret.curdir);
                    ret.ProjectReferences.Add(pref);
                }
                else if(n.Name == "Reference")
                {
                    var rname = process_string(n.SelectSingleNode("@Include", nm).Value, props, ret.items);
                    ret.References.Add(rname);
                }
                else if(n.Name == "ReferencePath")
                {
                    var inc_path = n.SelectSingleNode("@Include", nm);
                    if(inc_path != null)
                    {
                        foreach (var fpath in ParseReferencePath(process_string(inc_path.Value, props, ret.items)))
                        {
                            if (!ret.References.Contains(fpath))
                                ret.References.Add(fpath);
                        }
                    }
                    var exc_path = n.SelectSingleNode("@Exclude", nm);
                    if (exc_path != null)
                    {
                        foreach (var fpath in ParseReferencePath(process_string(exc_path.Value, props, ret.items)))
                        {
                            if (ret.References.Contains(fpath))
                                ret.References.Remove(fpath);
                        }
                    }
                }
                else
                {
                    // Silently ignore
                }

                List<string> items;
                if(!ret.items.TryGetValue(n.Name, out items))
                {
                    items = new List<string>();
                    ret.items[n.Name] = items;
                }
                var str = process_string(n.SelectSingleNode("@Include", nm).Value, props);
                if (!items.Contains(str))
                    items.Add(str);
            }
        }

        private static IEnumerable<string> ParseReferencePath(string v)
        {
            List<string> ret = new List<string>();
            var v2 = v.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var v3 in v2)
            {
                var dir_end = v3.LastIndexOfAny(new char[] { '/', '\\' });
                string dir;
                if (dir_end == -1)
                    dir = ".";
                else
                    dir = v3.Substring(0, dir_end);
                string file = v3.Substring(dir_end + 1);
                var di = new DirectoryInfo(dir);
                if (di.Exists)
                {
                    di.GetFiles(file);
                }
                System.IO.FileInfo fi = new FileInfo(v3);
            }
            throw new NotImplementedException();
        }

        private static void process_propertygroup(XPathNavigator n, Project ret, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            if(process_condition(n, props, nm))
            {
                foreach(XPathNavigator cn in n.Select("child::*"))
                {
                    process_property(cn, ret, props, nm);
                }
            }
        }

        private static void process_property(XPathNavigator n, Project ret, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            if(process_condition(n, props, nm))
            {
                props[n.Name] = process_string(n.Value, props, ret.items);
            }
        }

        private static void process_import(XPathNavigator n, Project ret, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            var proj = n.SelectSingleNode("@Project", nm);
            if(proj != null)
            {
                var pname = proj.Value;
                pname = process_string(pname, props);

                var pfi = new FileInfo(rel_path(pname, ret.uri_basedir, ret.uri_curdir));
                if(pfi.Exists)
                {
                    process_import(pfi, ret);
                }
                else
                {
                }
            }
        }

        private static void process_import(FileInfo pfi, Project ret)
        {
            /* Save all the 'msbuildthisfile' properties */
            var fp = ret.properties["MSBuildThisFileFullPath"];
            var fn = ret.properties["MSBuildThisFileName"];
            var fe = ret.properties["MSBuildThisFileExtension"];
            var fd = ret.properties["MSBuildThisFileDirectory"];
            var fdnr = ret.properties["MSBuildThisFileDirectoryNoRoot"];

            /* save curdir/basedir */
            var cd = ret.curdir;
            var ucd = ret.uri_curdir;
            var ubd = ret.uri_basedir;

            xml_read(ret, pfi.OpenRead(), ret.configuration, pfi.DirectoryName, ret.curdir, ret.properties);

            /* Restore thiffile properties */
            ret.properties["MSBuildThisFileFullPath"] = fp;
            ret.properties["MSBuildThisFileName"] = fn;
            ret.properties["MSBuildThisFileExtension"] = fe;
            ret.properties["MSBuildThisFileDirectory"] = fd;
            ret.properties["MSBuildThisFileDirectoryNoRoot"] = fdnr;

            /* Restore curdir/basedir */
            ret.curdir = cd;
            ret.uri_curdir = ucd;
            ret.uri_basedir = ubd;
        }

        internal static string process_string(string value, Dictionary<string, string> props, Dictionary<string, List<string>> items = null)
        {
            StringBuilder ret = new StringBuilder();

            int substituted = 0;

            for(int i = 0; i < value.Length;)
            {
                bool is_item = false;
                if(value[i] == '$' || (value[i] == '@' && items != null))
                {
                    if (value[i] == '@')
                        is_item = true;

                    StringBuilder var_name = new StringBuilder();
                    i++;

                    if (value[i] != '(')
                    {
                        ret.Append(value[i - 1]);
                        continue;
                    }
                    i++;

                    int bcount = 1;     // brackets count to get nested variables
                    int maxbcount = 1;

                    while (true)
                    {
                        if(value[i] == ')')
                        {
                            bcount--;
                            if (bcount == 0)
                                break;
                            else
                                var_name.Append(')');
                        }
                        else if(value[i] == '(')
                        {
                            bcount++;
                            if (bcount > maxbcount)
                                maxbcount = bcount;
                            var_name.Append('(');
                        }
                        else
                            var_name.Append(value[i]);

                        i++;
                    }
                    i++;

                    var vn = var_name.ToString();
                    if (maxbcount > 1)
                        vn = process_string(vn, props);

                    if (vn.StartsWith("[MSBuild]::"))
                    {
                        if (vn.StartsWith("[MSBuild]::GetDirectoryNameOfFileAbove("))
                        {
                            var na_args = vn.Substring("[MSBuild]::GetDirectoryNameOfFileAbove(".Length).Trim(')').Split(new char[] { ',' });

                            var dichild = new DirectoryInfo(na_args[0].Trim());
                            var diparent = dichild.Parent;

                            while (diparent != null)
                            {
                                var find = new FileInfo(add_dir_split(diparent.FullName) + na_args[1].Trim());

                                if (find.Exists)
                                {
                                    ret.Append(diparent.FullName);
                                    break;
                                }

                                diparent = diparent.Parent;
                            }
                        }
                        else
                            throw new NotImplementedException();

                        substituted++;
                    }
                    else if (!is_item && props.ContainsKey(vn))
                    {
                        ret.Append(props[vn]);
                        substituted++;
                    }
                    else if (is_item && items.ContainsKey(vn))
                    {
                        ret.Append(string.Join(";", items[vn]));
                        substituted++;
                    }
                }
                else
                {
                    ret.Append(value[i]);
                    i++;
                }
            }

            // handle nested defines
            var r = ret.ToString();
            if (substituted != 0 && (r.Contains("$") || (r.Contains("@") && items != null)))
                return process_string(r, props, items);
            else
                return r;
        }

        private static bool process_condition(XPathNavigator n, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            bool proc = true;

            var cond = n.SelectSingleNode("@Condition", nm);
            if (cond != null)
                proc = process_condition(cond.Value, props);

            return proc;
        }

        private static bool process_condition(string value, Dictionary<string, string> props)
        {
            //value = process_string(value, props);

            var p = new Parser(new Scanner(value));
            p.Parse();

            var ret = p.val.Evaluate(new MakeState { props = props });

            if (ret.AsInt == 0)
                return false;
            return true;
        }

        internal static string add_dir_split(string dir)
        {
            if (dir.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                return dir;
            else
                return dir + System.IO.Path.DirectorySeparatorChar;
        }

        internal static string remove_dir_split(string dir)
        {
            return dir.TrimEnd(Path.DirectorySeparatorChar);
        }

        internal static string remove_drive(string dir)
        {
            if (dir.Length > 2 && dir[1] == ':' && char.IsLetter(dir[0]))
                return dir.Substring(2);
            else
                return dir;
        }

        internal static string rel_path(string file, Uri basedir, Uri cur_dir)
        {
            Uri abs_path;
            if (basedir != null)
            {
                abs_path = new Uri(basedir, file);
            }
            else
                abs_path = new Uri(file);
            Uri ret = cur_dir.MakeRelativeUri(abs_path);
            string unescaped = Uri.UnescapeDataString(ret.ToString());
            return unescaped;
        }

        public int build()
        {
            return build(new List<string>(), new List<string>(), new List<string>(), false);
        }

        public int build(List<string> extra_defines, List<string> extra_libdirs, List<string> extra_libs, bool do_unsafe)
        {
            return build(extra_defines, extra_libdirs, extra_libs, do_unsafe, System.Console.Out, null);
        }

        public int build(List<string> extra_defines, List<string> extra_libdirs, List<string> extra_libs, bool do_unsafe,
            System.IO.TextWriter output, string tools_ver_override)
        {
            /* Set AssemblyName to what it has been defined as */
            properties["AssemblyName"] = assembly_name;
            properties["AssemblyTitle"] = assembly_name;

            /* Ensure output paths exist */
            if (properties.ContainsKey("OutputPath"))
                create_directory(new DirectoryInfo(properties["OutputPath"]));
            if (properties.ContainsKey("IntermediateOutputPath"))
                create_directory(new DirectoryInfo(properties["IntermediateOutputPath"]));
            if (properties.ContainsKey("RuntimePath"))
                create_directory(new DirectoryInfo(properties["RuntimePath"]));

            /* Run any PrepareForBuild target */
            if (targets.ContainsKey("PrepareForBuild"))
            {
                process_node(targets["PrepareForBuild"], this, properties, nm);
            }

            /*foreach(var k in properties.Keys)
            {
                if(k.Contains("DependsOn"))
                {
                    var v = properties[k];
                    Console.WriteLine(k + ": " + process_string(v, properties, items));
                }
            }*/
            /* First handle any custom build actions */
            if (properties.ContainsKey("PrepareResourcesDependsOn"))
                process_dependson(properties["PrepareResourcesDependsOn"]);

            if (properties.ContainsKey("CompileDependsOn"))
                process_dependson(properties["CompileDependsOn"]);
            if (properties.ContainsKey("CoreCompileDependsOn"))
                process_dependson(properties["CoreCompileDependsOn"]);

            if (properties.ContainsKey("BuildDependsOn"))
                process_dependson(properties["BuildDependsOn"]);
            if (properties.ContainsKey("CoreBuildDependsOn"))
                process_dependson(properties["CoreBuildDependsOn"]);

            /* Ensure output directory exists */
            var of = new FileInfo(OutputFile);
            create_directory(of.Directory);

            string tv = tools_ver;
            if (tools_ver_override != null)
                tv = tools_ver_override;
            bool is_v4plus;
            string csc_cmd = Program.csc(tv, out is_v4plus);
            StringBuilder sb = new StringBuilder();

            if(csc_cmd.Contains("Roslyn"))
            {
                // Roslyn allows language version to be specified
                sb.Append("/langversion:Latest ");
            }

            sb.Append("/out:\"");
            sb.Append(Program.replace_dir_split(OutputFile));
            sb.Append("\" ");

            if (defines != null && defines.Length > 0)
            {
                sb.Append("/define:");
                sb.Append(defines);
                sb.Append(" ");
            }

            sb.Append("/target:");
            switch (output_type)
            {
                case Project.OutputType.Exe:
                    sb.Append("exe");
                    break;
                case Project.OutputType.Library:
                    sb.Append("library");
                    break;
            }
            sb.Append(" ");

            sb.Append(extra_add);
            sb.Append(" ");

            foreach (string def in extra_defines)
            {
                sb.Append("/define:");
                sb.Append(def);
                sb.Append(" ");
            }

            foreach (string lib_dir in extra_libdirs)
            {
                sb.Append("/lib:\"");
                string ld = Program.replace_dir_split(lib_dir);
                if (ld.EndsWith("\\"))
                    ld = ld.Substring(0, ld.Length - 1);
                sb.Append(ld);
                sb.Append("\" ");
            }

            string tvd = Program.ref_dir(tools_ver);
            /* Reference the tools directory if mscorlib is not overridden */
            if (lib_dir != null || reference_overrides == null || !reference_overrides.ContainsKey("mscorlib"))
            {
                if (lib_dir != null)
                    tvd = lib_dir;

                sb.Append("/lib:\"");
                if (tvd.EndsWith("\\"))
                    tvd = tvd.Substring(0, tvd.Length - 1);
                sb.Append(tvd);
                sb.Append("\" ");
            }

            /* Directly reference mscorlib in the appropriate tools dir.
             * This helps v4+ compilers target the correct framework */
            Dictionary<string, bool> ro_added = new Dictionary<string, bool>();
            if (is_v4plus && get_prop("NoStdLib", properties).ToLower() != "true")
            {
                sb.Append("/nostdlib ");

                if (reference_overrides != null && reference_overrides.ContainsKey("mscorlib"))
                {
                    foreach (var ro in get_reference_overrides(reference_overrides["mscorlib"], "mscorlib"))
                        sb.Append("/r:" + ro + " ");
                    ro_added["mscorlib"] = true;
                }
                else
                    sb.Append("/r:\"" + Program.replace_dir_split(add_dir_split(tvd)) + "mscorlib.dll\" ");
            }

            foreach (string lib in extra_libs)
            {
                sb.Append("/reference:\"");
                string ld = Program.replace_dir_split(lib);
                if (ld.EndsWith("\\"))
                    ld = ld.Substring(0, ld.Length - 1);
                sb.Append(ld); sb.Append("\" ");
            }

            /* First, replace those project declared references we are using with overrides if necessary */
            List<string> new_refs = new List<string>();
            foreach (string lib in References)
            {
                var lib2 = Program.replace_dir_split(lib);
                if (reference_overrides != null && reference_overrides.ContainsKey(lib2))
                {
                    if (ro_added.ContainsKey(lib2))
                        continue;

                    foreach (var ro in get_reference_overrides(reference_overrides[lib2], lib2))
                        new_refs.Add(ro);

                    ro_added[lib2] = true;
                }
                else
                    new_refs.Add("\"" + lib2 + "\"");
            }

            /* Next add overrides that are unaccounted for */
            if(reference_overrides != null)
            {
                foreach(var kvp in reference_overrides)
                {
                    if (!ro_added.ContainsKey(kvp.Key))
                    {
                        foreach(var ro in get_reference_overrides(kvp.Value, kvp.Key))
                            new_refs.Add(ro);
                        ro_added[kvp.Key] = true;
                    }
                }
            }

            /* Finally add to the build command */

            foreach(var lib in new_refs)
            { 
                sb.Append("/r:");
                var lref = Program.replace_dir_split(lib);
                if(!lref.Contains(".dll") && !lref.Contains(".exe"))
                {
                    lref = lib.TrimEnd('\"') + ".dll\"";
                }
                sb.Append(lref);
                sb.Append(" ");
            }

            foreach (Project pref in ProjectReferences)
            {
                sb.Append("/reference:\"");
                sb.Append(Program.replace_dir_split(pref.OutputFile));
                sb.Append("\" ");
            }

            if (do_unsafe)
                sb.Append("/unsafe ");

            sb.Append("/nologo /noconfig ");

            for(int i = 0; i < Resources.Count; i++)
            {
                var fname = Resources[i];
                FileInfo srcfi = new FileInfo(fname);

                if (srcfi.Extension == ".resx")
                {
                    var new_fname = fname.Substring(0, fname.Length - ".resx".Length) + ".resources";
                    var dstfi = new FileInfo(new_fname);
                    if (dstfi.Exists == false || dstfi.LastWriteTimeUtc < srcfi.LastWriteTimeUtc)
                    {
#if DEBUG
                        Console.WriteLine("Performing csc build step for " + new_fname);
#endif

                        var rsargs = "/compile \"" + fname + "\",\"" + new_fname + "\"";
                        throw new NotImplementedException();
                        var rscmd = Program.resgen(tools_ver);
                    }
                    else
                        fname = new_fname;
                }

                sb.Append("/res:\"");
                sb.Append(Program.replace_dir_split(fname));
                sb.Append("\"");
                if(i < ResourceLogicalNames.Count)
                {
                    sb.Append(",");
                    sb.Append(ResourceLogicalNames[i]);
                }
                sb.Append(" ");
            }

            foreach (string src in Sources)
            {
                sb.Append("\"");
                sb.Append(Program.replace_dir_split(src));
                sb.Append("\" ");
            }

            string cmd_args = sb.ToString();
            string cmd_file = null;
            System.IO.FileInfo cmd_fi = null;

            bool use_response = false;
            if(properties.ContainsKey("CompilerResponseFile"))
            {
                use_response = true;

                var frespfna = properties["CompilerResponseFile"].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var frespfn in frespfna)
                {
                    // append any extra response lines
                    var fresp = File.OpenRead(frespfn.Trim());
                    var sr = new StreamReader(fresp);

                    while (!sr.EndOfStream)
                        cmd_args = cmd_args + " " + sr.ReadLine();

                    fresp.Close();
                }
            }

            string old_cmd_args = cmd_args;
            if ((csc_cmd.Length + cmd_args.Length > 2080) || use_response)
            {
                System.DateTime now = System.DateTime.Now;
                cmd_file = "tymake-" + now.Ticks.ToString() + ".tmp";
                cmd_fi = new FileInfo(cmd_file);
                System.IO.StreamWriter sw = new StreamWriter(cmd_fi.Create());
                sw.WriteLine(cmd_args);
                sw.Close();
                cmd_args = "/nologo /noconfig @" + cmd_file;
            }

#if DEBUG
            Console.WriteLine("Performing csc build step for " + OutputFile);
#endif

            System.Diagnostics.Process c_proc = new System.Diagnostics.Process();
            c_proc.EnableRaisingEvents = false;
            c_proc.StartInfo.FileName = csc_cmd;

            c_proc.StartInfo.Arguments = cmd_args;

            c_proc.StartInfo.CreateNoWindow = true;
            c_proc.StartInfo.UseShellExecute = false;
            c_proc.StartInfo.RedirectStandardOutput = true;
            if (!c_proc.Start())
                throw new Exception();
            output.WriteLine(c_proc.StandardOutput.ReadToEnd());
            c_proc.WaitForExit();

            if (cmd_fi != null)
                cmd_fi.Delete();

            if (c_proc.ExitCode != 0)
            {
                //output.WriteLine("Building returned " + c_proc.ExitCode.ToString());
                return c_proc.ExitCode;
            }

            /* Copy project references to output directory */
            System.IO.FileInfo o_fi = new FileInfo(OutputFile);
            string dest_dir = add_dir_split(o_fi.DirectoryName);
            List<string> srcs = new List<string>();
            add_project_references(srcs, ProjectReferences);
            foreach(var src in srcs)
            {
                string dest = dest_dir + new System.IO.FileInfo(src).Name;
                File.Copy(src, dest, true);
            }

            return 0;
        }

        private void add_project_references(List<string> srcs, List<Project> projectReferences)
        {
            foreach(Project pref in projectReferences)
            {
                string src = pref.OutputFile;
                if(!srcs.Contains(src))
                {
                    srcs.Add(src);
                    add_project_references(srcs, pref.ProjectReferences);
                }
            }
        }

        private IEnumerable<string> get_reference_overrides(string joined, string override_name)
        {
            var ros = joined.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ro in ros)
                yield return "\"" + ro.Trim() + "\"";
        }

        private void process_dependson(string v)
        {
            var ts = v.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var t in ts)
            {
                var t2 = t.Trim();
                if (t2 == "")
                    continue;

                var tnode = targets[t2];

                foreach(XPathNavigator cn in tnode.Select("child::*", nm))
                {
                    process_task(cn, this, properties, nm);
                }
            }
        }

        private static void process_task(XPathNavigator n, Project project, Dictionary<string, string> props, XmlNamespaceManager nm)
        {
            if(process_condition(n, props, nm))
            {
                if (n.Name == "Message")
                {
                    var mtext = n.SelectSingleNode("@Text", nm);
                    if (mtext != null)
                    {
                        Console.WriteLine(process_string(mtext.Value, props));
                    }
                }
                else if (n.Name == "WriteLinesToFile")
                {
                    var file = process_string(n.SelectSingleNode("@File", nm).Value, props);
                    var overwrite = false;

                    var own = n.SelectSingleNode("@Overwrite");
                    if (own != null && process_string(own.Value, props).ToLower() == "true")
                        overwrite = true;

                    var fi = new FileInfo(file);
                    // create directory if required
                    create_directory(fi.Directory);

                    FileMode mode = overwrite ? FileMode.Create : FileMode.Open;
                    var f = File.Open(file, mode);
                    var sr = new StreamWriter(f);

                    var ln = n.SelectSingleNode("@Lines", nm);
                    if (ln != null)
                    {
                        var lines = process_array(ln.Value, project);

                        foreach (var line in lines)
                            sr.WriteLine(line);
                    }

                    sr.Close();
                }
                else if (n.Name == "ItemGroup")
                    process_itemgroup(n, project, props, nm);
                else if (n.Name == "PropertyGroup")
                    process_propertygroup(n, project, props, nm);
                else if (project.tasks.ContainsKey(n.Name))
                {
                    // Fill in appropriate properties
                    var t = project.tasks[n.Name];
                    var obj = t.GetConstructor(Type.EmptyTypes).Invoke(null);
                    foreach(XPathNavigator anode in n.Select("@*", nm))
                    {
                        var aname = anode.Name;
                        var atext = process_string(anode.Value, props, project.items);

                        var pi = t.GetProperty(aname);
                        pi.SetValue(obj, atext);
                    }

                    var ret = t.GetMethod("Execute").Invoke(obj, null);
                    if ((bool)ret == false)
                        throw new Exception();
                }
                else if(n.Name == "Error")
                {
                    throw new Exception(process_string(n.SelectSingleNode("@Text", nm).Value, props, project.items));
                }
                else
                    throw new NotImplementedException();
            }
        }

        private void gen_res_code(XPathNavigator n, XmlNamespaceManager nm)
        {
            var resx_file = process_string(n.SelectSingleNode("@ResxFilePath", nm).Value, properties, items);
            var os_file = process_string(n.SelectSingleNode("@OutputSourceFilePath", nm).Value, properties, items);
            var aname = process_string(n.SelectSingleNode("@AssemblyName", nm).Value, properties, items);

            var csc = new FileInfo(Program.csc(tools_ver));
            var resgen = add_dir_split(csc.DirectoryName) + "resgen.exe";
            if (!(new FileInfo(resgen)).Exists)
                throw new NotSupportedException();

            throw new NotImplementedException();
        }

        internal static void create_directory(FileInfo f)
        {
            create_directory(f.Directory);
        }

        internal static void create_directory(string fname)
        {
            FileInfo fi = new FileInfo(fname);
            if (fi.Exists)
                return;
            create_directory(fi);
        }

        internal static void create_directory(DirectoryInfo directory)
        {
            if (directory.Exists)
                return;
            create_directory(directory.Parent);
            directory.Create();
        }

        private static string[] process_array(string value, Project project)
        {
            value = process_string(value, project.properties, project.items);

            var ret = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for(int i = 0; i < ret.Length; i++)
            {
                ret[i] = ret[i].Replace("%3B", ";");
            }
            return ret;
        }
    }
}
