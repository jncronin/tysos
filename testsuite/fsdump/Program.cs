/* Copyright (C) 2015 by John Cronin
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

namespace fsdump
{
    class Program
    {
        static void Main(string[] args)
        {
            string root = "/";

            while (true)
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(root);
                dump_dir(di);
            }
        }

        private static void dump_dir(System.IO.DirectoryInfo cdi)
        {
            Print(cdi.FullName);
            if (cdi.Exists == false)
                return;
            System.IO.FileInfo[] fis = cdi.GetFiles();
            foreach(System.IO.FileInfo fi in fis)
                Print("- " + fi.FullName);
            System.IO.DirectoryInfo[] dis = cdi.GetDirectories();
            foreach (System.IO.DirectoryInfo di in dis)
                dump_dir(di);
        }

        static void Print(string msg)
        {
            System.Diagnostics.Debugger.Log(0, "fsdump", msg + "\n");
        }
    }
}
