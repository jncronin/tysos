using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace interpret_opcodes
{
    class Program
    {
        static void Main(string[] args)
        {
            string fname;

            if (args.Length < 1)
                fname = "opcodes2.txt";
            else
                fname = args[0];

            // Convert a file containing lines of the type 0xA0 stelem.r4 to an
            // enum SingleOpcodes { stelem_r4 = 0xA0 }

            List<string> s = new List<string>();
            List<string> d = new List<string>();

            FileStream file = new FileStream(fname, FileMode.Open, FileAccess.Read);
            StreamReader fr = new StreamReader(file);

            string line;
            while ((line = fr.ReadLine()) != null)
            {
                bool dbl = false;

                if (line.ToUpper().StartsWith("0XFE "))
                {
                    line = line.Substring(5);
                    dbl = true;
                }

                int first_space = line.IndexOf(' ');
                string val = line.Substring(0, first_space);
                string name = line.Substring(first_space + 1);
                name = name.Replace('.', '_');

                string output = name + " = " + val;
                if (dbl)
                    d.Add(output);
                else
                    s.Add(output);
            }
            fr.Close();

            // output
            FileStream fout = new FileStream("opcodes.cs", FileMode.Create, FileAccess.Write);
            StreamWriter fw = new StreamWriter(fout);

            fw.WriteLine("enum SingleOpcodes {");
            for(int i = 0; i < s.Count; i++) {
                fw.Write("\t" + s[i]);
                if (i < (s.Count - 1))
                    fw.Write(",");
                fw.WriteLine();
            }
            fw.WriteLine("}");
            fw.WriteLine();

            fw.WriteLine("enum DoubleOpcodes {");
            for(int i = 0; i < d.Count; i++) {
                fw.Write("\t" + d[i]);
                if (i < (d.Count - 1))
                    fw.Write(",");
                fw.WriteLine();
            }
            fw.WriteLine("}");
            fw.WriteLine();

            fw.Close();
        }
    }
}
