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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace typroject
{
    class Program
    {
        static string tools_ver_override = null;
        public static int platform;

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
            platform = (int)Environment.OSVersion.Platform;

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
            if (fname.EndsWith(".csproj"))
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
            if (tools_ver_override != null)
                tools_ver = tools_ver_override;

            if (platform == 0)
            {
                // assume mono for unix
                if ((tools_ver == "2.0") || (tools_ver == "3.0") || (tools_ver == "3.5") || (tools_ver == "4.0"))
                    return "gmcs";
                else
                    return "mcs";
            }
            else
            {
                // assume csc for windows

                string windir = Environment.GetEnvironmentVariable("windir");
                string framework_dir = windir + "\\Microsoft.NET\\Framework";
                DirectoryInfo fdi = new DirectoryInfo(framework_dir);
                DirectoryInfo[] matches = fdi.GetDirectories("v" + tools_ver + "*");

                foreach (DirectoryInfo match in matches)
                {
                    FileInfo[] file_matches = match.GetFiles("csc.exe");
                    if (file_matches.Length == 1)
                        return file_matches[0].FullName;
                }

                // fallback
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
                string fwork_ver;
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
    }

    public class Project
    {
        public string ProjectFile;
        public string ProjectName;
        public Guid Guid;
        public string OutputFile;

        public string configuration;
        public string tools_ver;
        public OutputType output_type;
        public string assembly_name;
        public string extra_add;
        public string output_path;
        public string defines;

        public List<string> Sources = new List<string>();
        public List<string> References = new List<string>();
        public List<Project> ProjectReferences = new List<Project>();

        public string ErrorMessage;

        public Project() { }
        public Project(string filename, string name, Guid guid)
        { ProjectFile = filename; ProjectName = name; Guid = guid; }

        public enum OutputType { Exe, Library };

        public static Project xml_read(Stream file, string config, string basedir, string curdir)
        {
            Project ret = new Project();
            return xml_read(ret, file, config, basedir, curdir);
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

            return ret;
        }

        internal static Project xml_read(Project ret, Stream file, string config, string basedir, string curdir)
        {
            /* Load a project from the project file specified */
            basedir = add_dir_split(basedir);
            curdir = add_dir_split(curdir);
            Uri uri_basedir = new Uri(basedir);
            Uri uri_curdir = new Uri(curdir);

            XPathDocument r = new XPathDocument(file);
            XPathNavigator n = r.CreateNavigator();
            n.MoveToRoot();
            n.MoveToFirstChild();
            XmlNamespaceManager nm = new XmlNamespaceManager(n.NameTable);
            nm.AddNamespace("d", n.NamespaceURI);

            /* First find the properties of this project */
            if (config != null)
                ret.configuration = config;
            else
                ret.configuration = n.SelectSingleNode("//d:PropertyGroup/d:Configuration", nm).Value;

            ret.tools_ver = n.SelectSingleNode("@ToolsVersion", nm).Value;

            ret.Guid = new Guid(n.SelectSingleNode("//d:PropertyGroup/d:ProjectGuid", nm).Value);
            string otype = n.SelectSingleNode("//d:PropertyGroup/d:OutputType", nm).Value;
            if(otype == "Library")
                ret.output_type = OutputType.Library;
            else if(otype == "Exe")
                ret.output_type = OutputType.Exe;
            else
                throw new Exception("Unknown output type: " + otype);
            ret.assembly_name = n.SelectSingleNode("//d:PropertyGroup/d:AssemblyName", nm).Value;

            if (ret.ProjectName == null)
                ret.ProjectName = ret.assembly_name;

            if (ret.configuration.ToLower() == "release")
                ret.extra_add = "/debug:pdbonly /optimize+";
            else if (ret.configuration.ToLower() == "debug")
                ret.extra_add = "/debug:full";

            XPathNavigator unsafe_node = n.SelectSingleNode("//d:PropertyGroup/d:AllowUnsafeBlocks", nm);
            if (unsafe_node != null)
            {
                if (unsafe_node.Value.ToLower() == "true")
                    ret.extra_add += " /unsafe";
            }

            /* Find all property groups with conditions */
            XPathNodeIterator nit = n.Select("//d:PropertyGroup[@Condition]", nm);
            while (nit.MoveNext())
            {
                if (nit.Current.SelectSingleNode("@Condition", nm).Value.Contains(ret.configuration))
                {
                    string output_path = nit.Current.SelectSingleNode(".//d:OutputPath", nm).Value;
                    ret.output_path = rel_path(output_path, uri_basedir, uri_curdir);

                    output_path = add_dir_split(output_path);
                    string ext = "";
                    if (ret.output_type == OutputType.Library)
                        ext = ".dll";
                    else if (ret.output_type == OutputType.Exe)
                        ext = ".exe";
                    string output_file = output_path + ret.assembly_name + ext;
                    ret.OutputFile = rel_path(output_file, uri_basedir, uri_curdir);
                        
                    ret.defines = nit.Current.SelectSingleNode(".//d:DefineConstants", nm).Value;
                }
            }

            /* Load all source file names */
            XPathNodeIterator sfit = n.Select("//d:ItemGroup/d:Compile/@Include", nm);
            while (sfit.MoveNext())
            {
                ret.Sources.Add(rel_path(sfit.Current.Value, uri_basedir, uri_curdir));
            }

            /* Load all references */
            XPathNodeIterator rit = n.Select("//d:ItemGroup/d:Reference/@Include", nm);
            while (rit.MoveNext())
                ret.References.Add(rit.Current.Value);

            /* Load project references */
            XPathNodeIterator prit = n.Select("//d:ItemGroup/d:ProjectReference", nm);
            while (prit.MoveNext())
            {
                Project pref = new Project();
                pref.Guid = new Guid(prit.Current.SelectSingleNode("./d:Project", nm).Value);
                pref.ProjectFile = rel_path(prit.Current.SelectSingleNode("./@Include", nm).Value, uri_basedir, uri_curdir);
                pref.ProjectName = prit.Current.SelectSingleNode("./d:Name", nm).Value;
                FileInfo pref_fi = new FileInfo(pref.ProjectFile);
                xml_read(pref, pref_fi.Open(FileMode.Open, FileAccess.Read), config, pref_fi.DirectoryName, curdir);
                ret.ProjectReferences.Add(pref);
            }

            return ret;
        }

        internal static string add_dir_split(string dir)
        {
            if (dir.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                return dir;
            else
                return dir + System.IO.Path.DirectorySeparatorChar;
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
            Program.platform = (int)Environment.OSVersion.Platform;
            string tv = tools_ver;
            if (tools_ver_override != null)
                tv = tools_ver_override;
            string csc_cmd = Program.csc(tv);
            StringBuilder sb = new StringBuilder();

            sb.Append("/out:\"");
            sb.Append(Program.replace_dir_split(OutputFile));
            sb.Append("\" ");

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
                sb.Append(Program.replace_dir_split(def));
                sb.Append(" ");
            }

            foreach (string lib_dir in extra_libdirs)
            {
                sb.Append("/lib:\"");
                sb.Append(Program.replace_dir_split(lib_dir));
                sb.Append("\" ");
            }

            sb.Append("/lib:\"");
            sb.Append(Program.ref_dir(tools_ver));
            sb.Append("\" ");

            foreach (string lib in extra_libs)
            {
                sb.Append("/reference:\"");
                sb.Append(Program.replace_dir_split(lib));
                sb.Append("\" ");
            }

            foreach (string lib in References)
            {
                sb.Append("/reference:\"");
                sb.Append(Program.replace_dir_split(lib));
                sb.Append(".dll\" ");
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

            foreach (string src in Sources)
            {
                sb.Append("\"");
                sb.Append(Program.replace_dir_split(src));
                sb.Append("\" ");
            }

            string cmd_args = sb.ToString();

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

            if (c_proc.ExitCode != 0)
            {
                //output.WriteLine("Building returned " + c_proc.ExitCode.ToString());
                return c_proc.ExitCode;
            }

            /* Copy project references to output directory */
            System.IO.FileInfo o_fi = new FileInfo(OutputFile);
            string dest_dir = add_dir_split(o_fi.DirectoryName);
            foreach (Project pref in ProjectReferences)
            {
                string src = pref.OutputFile;
                string dest = dest_dir + new System.IO.FileInfo(src).Name;
                File.Copy(src, dest, true);
            }

            return 0;
        }
    }
}
