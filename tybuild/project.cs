/* Copyright (c) 2008, John Cronin
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the copyright holder nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace tybuild
{
    class ProjectId
    {
        public string ProjectFile;
        public string ProjectName;
        public Guid Guid;
        public string OutputFile;

	public string ErrorMessage;

        public ProjectId() {}
        public ProjectId(string filename, string name, Guid guid)
        { ProjectFile = filename; ProjectName = name; Guid = guid; }

        public Project Project;

        public string ProjectBaseDir
        {
            get
            {
                return tybuild.GetBaseDir(ProjectFile);
            }
        }
    }

    class Project
    {
        /* Defines a project - source files, compiler options, referenced projects */

        public List<string> Sources = new List<string>();
        public List<string> References = new List<string>();
        public List<ProjectId> ProjectReferences = new List<ProjectId>();

        public string CommandLineArgs
        {
            get
            {
                return CommandLineArgsWithoutSources + " " + src_line();
            }
        }

        public string CommandLineArgsWithoutSources
        {
            get
            {
                string defs = "";
                if (defines != "")
                    defs = " /define:" + defines + " ";
                return "/out:\"" + OutputFile + "\" /target:" + output_type.ToLower() + defs +
                    extra_add + " " + ref_line() + "/noconfig /nologo";
            }
        }

        public string SourceList
        {
            get
            {
                return src_line();
            }
        }

        private string ref_line()
        {
            StringBuilder ret = new StringBuilder();
            foreach (string r in References)
                ret.Append("/r:\"" + tybuild.AppendDirSplit(tybuild.ref_dir(tools_ver)) + AppendDll(r) + "\" ");
            foreach (ProjectId pr in ProjectReferences)
                ret.Append("/r:\"" + tybuild.ReplaceDirSplit(pr.OutputFile, unix_splits) +
					 "\" ");
            return ret.ToString();
        }

        private string src_line()
        {
            StringBuilder ret = new StringBuilder();
            foreach (string s in Sources)
                ret.Append("\"" + tybuild.AppendDirSplit(base_dir) + 
						tybuild.ReplaceDirSplit(s, unix_splits) + "\" ");
            return ret.ToString();
        }

        private static string AppendDll(string fname)
        {
            if (fname.EndsWith(".exe") || fname.EndsWith(".dll"))
                return fname;
            return tybuild.ReplaceDirSplit(fname) + ".dll";
        }

        string base_dir = ".";
        public string BaseDir
        { get { return base_dir; } }
        public string OutputDir
        { get { return tybuild.AppendDirSplit(base_dir) + 
				  tybuild.ReplaceDirSplit(output_path, unix_splits); } }
        public string OutputFile
        { get { return tybuild.AppendDirSplit(OutputDir) + AssemblyName; } }
        public string AssemblyName
        { get { return (((assembly_name.EndsWith(".exe") || (assembly_name.EndsWith(".dll")) ? assembly_name : assembly_name + ((output_type.ToLower() == "library") ? ".dll" : ".exe")))); } }
        public string ToolsVer
        { get { return tools_ver; } }

        public bool IsBuilt = false;
        public Guid Guid;

        string output_type;
        string configuration;
        string assembly_name;
        string output_path = ".";
        string defines = "";
        public string extra_add = "";
        string tools_ver = "2.0";
        bool unix_splits = false;

        public Project(string[] input, string output_name)
        {
            Sources.AddRange(input);
            assembly_name = output_name;
            output_type = "exe";
        }

        public Project(Stream file, string config, string basedir)
        {
            base_dir = basedir;

            StreamReader sr = new StreamReader(file);
            if (sr.ReadLine() == "#sources")
                sources_read(sr, config, basedir);
            else
            {
                file.Seek(0, SeekOrigin.Begin);
                xml_read(file, config, basedir);
            }
        }

        void sources_read(StreamReader file, string config, string basedir)
        {
            while (!file.EndOfStream)
            {
                string line = file.ReadLine();

                if (line.StartsWith("#define "))
                    defines += line.Substring("#define ".Length) + " ";
                else if (line.StartsWith("#outdir "))
                    output_path = line.Substring("#outdir ".Length);
                else if (line.StartsWith("#assemblyname "))
                    assembly_name = line.Substring("#assemblyname ".Length);
                else if (line.StartsWith("#target "))
                    output_type = line.Substring("#target ".Length);
                else if (line.StartsWith("#ref "))
                    References.Add(line.Substring("#ref ".Length));
                else if (line.StartsWith("#extra "))
                    extra_add += line.Substring("#extra ".Length) + " ";
                else if (line.StartsWith("#tools "))
                    tools_ver = line.Substring("#tools ".Length);
                else if (line.StartsWith("#unix"))
                    unix_splits = true;
                else if (line.StartsWith("#dos"))
                    unix_splits = false;
                else if (line.StartsWith("#")) continue; // ignore
                else
                {
                    if (line.Length > 0)
                        Sources.Add(line.Trim());
                }
            }
        }

        void xml_read(Stream file, string config, string basedir)          
        {
            /* Load a project from the project file specified */

            XPathDocument r = new XPathDocument(file);
            XPathNavigator n = r.CreateNavigator();
            n.MoveToRoot();
            n.MoveToFirstChild();
            XmlNamespaceManager nm = new XmlNamespaceManager(n.NameTable);
            nm.AddNamespace("d", n.NamespaceURI);

            /* First find the properties of this project */
            if (config != null)
                configuration = config;
            else
                configuration = n.SelectSingleNode("//d:PropertyGroup/d:Configuration", nm).Value;

            tools_ver = n.SelectSingleNode("@ToolsVersion", nm).Value;

            Guid = new Guid(n.SelectSingleNode("//d:PropertyGroup/d:ProjectGuid", nm).Value);
            output_type = n.SelectSingleNode("//d:PropertyGroup/d:OutputType", nm).Value;
            assembly_name = n.SelectSingleNode("//d:PropertyGroup/d:AssemblyName", nm).Value;

            if (configuration.ToLower() == "release")
                extra_add = "/debug:pdbonly /optimize+";
            else if (configuration.ToLower() == "debug")
                extra_add = "/debug:full";

            XPathNavigator unsafe_node = n.SelectSingleNode("//d:PropertyGroup/d:AllowUnsafeBlocks", nm);
            if (unsafe_node != null)
            {
                if (unsafe_node.Value.ToLower() == "true")
                    extra_add += " /unsafe";
            }

            /* Find all property groups with conditions */
            XPathNodeIterator nit = n.Select("//d:PropertyGroup[@Condition]", nm);
            while (nit.MoveNext())
            {
                if (nit.Current.SelectSingleNode("@Condition", nm).Value.Contains(configuration))
                {
                    output_path = nit.Current.SelectSingleNode(".//d:OutputPath", nm).Value;
                    defines = nit.Current.SelectSingleNode(".//d:DefineConstants", nm).Value;
                }
            }

            /* Load all source file names */
            XPathNodeIterator sfit = n.Select("//d:ItemGroup/d:Compile/@Include", nm);
            while (sfit.MoveNext())
                Sources.Add(tybuild.ReplaceDirSplit(sfit.Current.Value));

            /* Load all references */
            XPathNodeIterator rit = n.Select("//d:ItemGroup/d:Reference/@Include", nm);
            while (rit.MoveNext())
                References.Add(rit.Current.Value);

            /* Load project references */
            XPathNodeIterator prit = n.Select("//d:ItemGroup/d:ProjectReference", nm);
            while (prit.MoveNext())
            {
                ProjectId pref = new ProjectId();
                pref.Guid = new Guid(prit.Current.SelectSingleNode("./d:Project", nm).Value);
                pref.ProjectFile = tybuild.ReplaceDirSplit(prit.Current.SelectSingleNode("./@Include", nm).Value);
                pref.ProjectName = prit.Current.SelectSingleNode("./d:Name", nm).Value;
                ProjectReferences.Add(pref);
            }
        }
    }
}
