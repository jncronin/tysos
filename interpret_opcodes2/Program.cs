using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace interpret_opcodes2
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

            // Convert a file containing lines of the type 
            // OPDEF(id, name, pop_stack, push_stack, inline_var, opcode_type, num_opcodes, opcode1, opcode2, control flow)
            // (split over two lines) to a set of class opcode objects

            FileStream file = new FileStream(fname, FileMode.Open, FileAccess.Read);
            StreamReader fr = new StreamReader(file);

            FileStream fo = new FileStream("output2.cs", FileMode.Create, FileAccess.Write);
            StreamWriter fw = new StreamWriter(fo);

            string line, line2;
            while (((line = fr.ReadLine()) != null) && ((line2 = fr.ReadLine()) != null))
            {
                List<string> entries = new List<string>();
                string[] split_line1 = line.Split(',');
                foreach (string s in split_line1)
                    if(s.Trim().Length > 0) entries.Add(s.Trim());
                string[] split_line2 = line2.Split(',');
                foreach (string s in split_line2)
                    if (s.Trim().Length > 0) entries.Add(s.Trim());

                string opcode1 = (entries[6] == "1") ? entries[8] : entries[7];
                string opcode2 = (entries[6] == "1") ? "0x00" : entries[8];
                string key = (entries[6] == "1") ? opcode1 : opcode1 + opcode2.Substring(2);
                string name = entries[1];
                string pop = entries[2];
                string push = entries[3];

                string[] poplist = pop.Split('+');
                pop = "";
                for (int i = 0; i < poplist.Length; i++)
                {
                    if (poplist[i].Length == 0)
                        continue;
                    pop += "(int)Assembler.PopBehaviour.";
                    pop += poplist[i];
                    if (i < (poplist.Length - 1))
                        pop += " + ";
                }

                string[] pushlist = push.Split('+');
                push = "";
                for (int i = 0; i < pushlist.Length; i++)
                {
                    if (pushlist[i].Length == 0)
                        continue;
                    push += "(int)Assembler.PushBehaviour.";
                    push += pushlist[i];
                    if (i < (pushlist.Length - 1))
                        push += " + ";
                }

                string inline = entries[4];
                string ctrl = entries[9].Trim(')');

                string o = "Opcodes.Add(" + key + ", new Assembler.Opcode " +
                "{ opcode1 = (Assembler.SingleOpcodes)" + opcode1 + 
                    ", opcode2 = (Assembler.DoubleOpcodes)" + opcode2 + ", name = " + name +
                    ", pop = " + pop +
                    ", push = " + push +
                    ", inline = Assembler.InlineVar." + inline +
                    ", ctrl = Assembler.ControlFlow." + ctrl + " });";
                fw.WriteLine(o);
            }

            fw.Close();
            fr.Close();
        }
    }
}
