using System;
using System.Collections.Generic;
using System.Text;
using tysila;
using System.IO;

namespace tysila2_doc
{
    class TIRdoc
    {
        class TIRdocEntry
        {
            public ThreeAddressCode tac;
            public string op1 = "Unused";
            public string op2 = "Unused";
            public string result = "Unused";
            public string desc = "";
            public string optype { get { return tac.GetOpType().ToString(); } }
        }

        public List<TIRdocEntry> tir_docs = new List<TIRdocEntry>
        {
            new TIRdocEntry { tac = new ThreeAddressCode(ThreeAddressCode.Op.poke_u), op1 = "destination address", op2 = "value",
                desc = "Move the value of op2 to the address pointed to by op1 (native int size)"
            }
        };

        public void MakeTIRDocs()
        {
            DirectoryInfo di = new DirectoryInfo("doc/tir");
            if (!di.Exists)
                di.Create();

            FileStream fs = new FileStream("doc/tir/index.html", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("<HTML>");
            sw.WriteLine("<HEAD><TITLE>TysilaIntermediateRepresentation</TITLE></HEAD>");
            sw.WriteLine("<BODY><H1>TysilaIntermediateRepresentation</H1>");

            foreach (TIRdocEntry tde in tir_docs)
            {
                sw.WriteLine("<P><A HREF=\"tir_" + tde.tac.Operator.ToString() + ".html\">" + tde.tac.Operator.ToString() + "</A></P>");

                FileStream fs2 = new FileStream("doc/tir/tir_" + tde.tac.Operator.ToString() + ".html", FileMode.Create);
                StreamWriter sw2 = new StreamWriter(fs2);

                sw2.WriteLine("<HTML>");
                sw2.WriteLine("<HEAD><TITLE>" + tde.tac.Operator.ToString() + "</TITLE></HEAD>");
                sw2.WriteLine("<BODY><H1>" + tde.tac.Operator.ToString() + "</H1>");
                sw2.WriteLine("<P>" + tde.desc + "</P>");
                sw2.WriteLine("<TABLE>");
                sw2.WriteLine("<TR><TD>op1</TD><TD>" + tde.op1 + "</TD></TR>");
                sw2.WriteLine("<TR><TD>op2</TD><TD>" + tde.op2 + "</TD></TR>");
                sw2.WriteLine("<TR><TD>result</TD><TD>" + tde.result + "</TD></TR>");
                sw2.WriteLine("</TABLE></BODY></HTML>");

                sw2.Close();
            }

            sw.WriteLine("</BODY></HTML>");
            sw.Close();
        }
    }
}
