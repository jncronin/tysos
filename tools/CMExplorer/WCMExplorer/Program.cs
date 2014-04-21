using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WCMExplorer
{
    static class Program
    {
        internal static List<string> search_dirs = new List<string>();
        internal static string DirectoryDelimiter = "/";
        internal static CMExpLib.Elf64Reader.ElfHeader ehdr;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            search_dirs.Add("../../../../../tysila2/bin/Release");
            search_dirs.Add("../../../../../mono/corlib");
            search_dirs.Add("../../../../../tysos/bin/Release");

            LoadObjectFile("../../../../../tysos/tysos.bin");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        internal static void LoadObjectFile(string name)
        {
            CMExpLib.Elf64Reader r = new CMExpLib.Elf64Reader();
            try
            {
                ehdr = r.Read(name, new FileSystemFileLoader());
            }
            catch (System.IO.FileNotFoundException)
            {
                ehdr = null;
            }        
        }
    }
}
