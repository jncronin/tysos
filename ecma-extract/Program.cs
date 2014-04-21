using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.XPath;

namespace ecma_extract
{
    class Program
    {
        static List<string> assembly_names = new List<string>();
        static List<string> type_names = new List<string>();

        static int Main(string[] args)
        {
            string fname = null;
            string output_dir = ".";
            if (args.Length == 0)
                disp_usage();

            foreach (string arg in args)
            {
                if (arg.StartsWith("--assembly="))
                    assembly_names.Add(arg.Substring("--assembly=".Length));
                else if (arg.StartsWith("--type="))
                    type_names.Add(arg.Substring("--type=".Length));
                else if(arg.StartsWith("--output_dir="))
                    output_dir = arg.Substring("--output_dir=".Length);
                else if (arg.StartsWith("-"))
                {
                    disp_usage();
                    return -1;
                }
                else
                    fname = arg;
            }

            if (fname == null)
            {
                disp_usage();
                return -1;
            }

            // Create output_dir if necessary
            DirectoryInfo odir = new DirectoryInfo(output_dir);
            if (!odir.Exists)
                odir.Create();

            string query_string = "";
            string a_qs = "";
            string t_qs = "";
            if (assembly_names.Count > 0)
            {
                for (int i = 0; i < assembly_names.Count; i++)
                {
                    a_qs += "AssemblyInfo/AssemblyName=\"" + assembly_names[i] + "\"";
                    if(i < (assembly_names.Count - 1))
                        a_qs += " or ";
                }
            }

            if (type_names.Count > 0)
            {
                for (int i = 0; i < type_names.Count; i++)
                {
                    t_qs += "@FullName=\"" + type_names[i] + "\"";
                    if (i < (type_names.Count - 1))
                        t_qs += " or ";
                }
            }

            if ((a_qs != "") || (t_qs != ""))
            {
                query_string = "[";
                if (a_qs != "")
                {
                    query_string += "(" + a_qs + ")";
                    if (t_qs != "")
                        query_string += " and ";
                }
                if (t_qs != "")
                    query_string += "(" + t_qs + ")";
                query_string += "]";
            }

            string xpath_query = "//Type" + query_string;

            XPathDocument x = new XPathDocument(new FileStream(fname, FileMode.Open, FileAccess.Read));
            XPathNavigator n = x.CreateNavigator();

            XPathNodeIterator types = n.Select(xpath_query);
            while (types.MoveNext())
            {
                string nspace;
                string fullname;
                string cssig;

                fullname = types.Current.SelectSingleNode("@FullName").Value;
                cssig = types.Current.SelectSingleNode("TypeSignature[@Language=\"C#\"]/@Value").Value;
                nspace = fullname.Substring(0, fullname.LastIndexOf('.'));

                FileStream o = new FileStream(odir.FullName + "\\" + fullname + ".cs", FileMode.Create,
                    FileAccess.Write);
                StreamWriter sw = new StreamWriter(o, Encoding.UTF8);
                sw.WriteLine("namespace " + nspace + " {");

                // Attributes
                XPathNodeIterator attrs = n.Select("Attributes/Attribute");
                while (attrs.MoveNext())
                {
                    string attr = attrs.Current.SelectSingleNode("AttributeName").Value;
                    sw.WriteLine("\t[" + attr + "]");
                }

                sw.WriteLine("\t" + cssig);

                if (!cssig.Contains(";"))
                {
                    sw.WriteLine("\t{");

                    XPathNodeIterator members = types.Current.Select("Members/Member");
                    while (members.MoveNext())
                    {
                        string m_cssig = members.Current.SelectSingleNode("MemberSignature[@Language=\"C#\"]/" +
                            "@Value").Value;
                        if (!m_cssig.Contains(";"))
                            m_cssig += ";";
                        sw.WriteLine("\t\t" + m_cssig);
                    }

                    sw.WriteLine("\t}");
                }

                sw.WriteLine("}");
                sw.WriteLine();
                sw.Close();
            }

            return 0;
        }

        private static void disp_usage()
        {
            Console.WriteLine("usage: " + Environment.GetCommandLineArgs()[0] + " [options] [xml_file]");
            Console.WriteLine();
            Console.WriteLine("options:");
            Console.WriteLine("\t--assembly=assembly_name");
            Console.WriteLine("\t--type=type_name");
            Console.WriteLine("\t--output_dir=output_directory");
            Console.WriteLine();
        }
    }
}
