/* Copyright (C) 2011 by John Cronin
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
using System.Text;
using System.IO;

namespace shell
{
    class shell
    {
        static void Main(string[] args)
        {
            bool cont = true;
            while (cont)
            {
                Console.Write("$ ");
                string line = Console.ReadLine();

                string[] line_pieces = line.Split(' ');

                if (line_pieces.Length > 0)
                {
                    if (line_pieces[0] == "pwd")
                        Console.WriteLine(Environment.CurrentDirectory);
                    else if (line_pieces[0] == "cd")
                    {
                        if (line_pieces.Length > 1)
                            Environment.CurrentDirectory = get_fqn(line_pieces[1]);
                        else
                            Environment.CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                    else if (line_pieces[0] == "ls")
                    {
                        DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);
                        FileInfo[] file_list = di.GetFiles();
                        DirectoryInfo[] dir_list = di.GetDirectories();

                        foreach (DirectoryInfo di2 in dir_list)
                            Console.WriteLine("<DIR>   " + di2.Name);
                        foreach (FileInfo fi in file_list)
                            Console.WriteLine("        " + fi.Name);
                    }
                    else if (line_pieces[0] == "cat")
                    {
                        for (int i = 1; i < line_pieces.Length; i++)
                        {
                            string s = get_fqn(line_pieces[i]);
                            StreamReader sr = new StreamReader(s);
                            while (!sr.EndOfStream)
                                Console.WriteLine(sr.ReadLine());
                            sr.Close();
                        }
                    }
                    else
                    {
                        /*FileInfo fi = new FileInfo(get_fqn(line_pieces[0]));
                        if (fi.Exists)
                        {
                            string p = String.Join(" ", line_pieces, 1, line_pieces.Length - 1);
                            System.Diagnostics.Process.Start(fi.FullName, p);
                        }
                        else */
                            Console.WriteLine(line_pieces[0] + " is not a recognised executable or built-in command");
                    }
                }
            }
        }

        private static string get_fqn(string p)
        {
            if (p.StartsWith("/"))
                return p;
            else if (Environment.CurrentDirectory == "/")
                return "/" + p;
            else
                return Environment.CurrentDirectory + "/" + p;
        }
    }
}
