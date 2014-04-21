using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace tysila2_doc
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryInfo di = new DirectoryInfo("doc");
            if (!di.Exists)
                di.Create();
            TIRdoc td = new TIRdoc();
            td.MakeTIRDocs();
        }
    }
}
