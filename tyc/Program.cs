using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace tyc
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser2 p = new Parser2("c_syntax.bnf");


            string fname = "../tload/vmem.c";
            FileInfo fi = new FileInfo(fname);
            FileStream input = fi.Open(FileMode.Open, FileAccess.Read);
            string cur_dir = fi.DirectoryName;

            //FileStream input = new FileStream("c://users/jncronin/Documents/Visual Studio 2008/Projects/Tysos/tload/vmem.c", FileMode.Open, FileAccess.Read);

            StreamReader sr = new StreamReader(input);
            string text = sr.ReadToEnd();
            sr.Close();

            IncludeFileLocator ifl = new IncludeFileLocator { cur_dir = cur_dir,
                search_dirs = new string[] { "lib" } };

            List<Preprocessor.token> tokens = Preprocessor.Process(text, new Dictionary<string, List<Preprocessor.token>>(), ifl);
            StringBuilder sb = new StringBuilder();
            foreach (Preprocessor.token t in tokens)
            {
                if (t.type != Preprocessor.token.token_type.comment)
                {
                    sb.Append(t.value);
                    sb.Append(" ");
                }
            }
            string s = sb.ToString();

            List<Preprocessor.token> stripped_tokens = new List<Preprocessor.token>();
            foreach (Preprocessor.token t in tokens)
            {
                if (t.type != Preprocessor.token.token_type.comment)
                    stripped_tokens.Add(t);
            }

            p.ParseTokens(stripped_tokens, "translation_unit");

            //Parser.ParseFile(stripped_tokens);
        }

        internal class IncludeFileLocator
        {
            internal string ReadFile(string include_name, bool search_cur_dir)
            {
                if (search_cur_dir)
                {
                    string value = ReadFile(cur_dir, include_name);
                    if (value != null)
                        return value;
                }

                if (search_dirs != null)
                {
                    foreach (string dir in search_dirs)
                    {
                        string value = ReadFile(dir, include_name);
                        if (value != null)
                            return value;
                    }
                }

                throw new FileNotFoundException(include_name);
            }

            string ReadFile(string dir_name, string file_name)
            {
                DirectoryInfo di = new DirectoryInfo(dir_name);
                if (di.Exists)
                {
                    string file = di.FullName;
                    if (!file.EndsWith("\\"))
                        file += "\\";
                    file += file_name;

                    FileInfo fi = new FileInfo(file);
                    if (fi.Exists)
                    {
                        FileStream fs = fi.Open(FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs);
                        string value = sr.ReadToEnd();
                        sr.Close();
                        return value;
                    }
                }
                return null;
            }

            internal string cur_dir;
            internal string[] search_dirs;
        }
    }
}
