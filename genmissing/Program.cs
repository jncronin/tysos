/* Copyright (C) 2017-2018 by John Cronin
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
using System.Threading.Tasks;

namespace genmissing
{
    class Program
    {
        static string[] input_files = new string[]
        {
            @"D:\tysos\libsupcs\bin\Release/libsupcs.a",
            @"D:\tysos\tysos\bin\Release/tysos.obj",
            @"D:\tysos\coreclr/mscorlib.obj",
            @"./tysos/x86_64/cpu.o",
            @"./tysos/x86_64/halt.o",
            @"./tysos/x86_64/exceptions.o",
            @"./tysos/x86_64/switcher.o",
            @"D:\tysos\metadata\bin\Release/metadata.obj",
        };

        static void Main(string[] args)
        {
            /* Load up each input file in turn */
            var ifiles = new List<binary_library.IBinaryFile>();
            foreach(var ifname in input_files)
            {
                var ifinfo = new System.IO.FileInfo(ifname);
                if (!ifinfo.Exists)
                    throw new System.IO.FileNotFoundException("Cannot find: " + ifname);

                /* Determine file type from extension */
                binary_library.IBinaryFile ifobj = null;
                if (ifinfo.Extension == ".o" || ifinfo.Extension == ".obj")
                    ifobj = new binary_library.elf.ElfFile();

                if (ifobj == null)
                    ifobj = binary_library.BinaryFile.CreateBinaryFile(ifinfo.Extension);

                if (ifobj == null)
                    throw new Exception("Unsupported file type: " + ifinfo.FullName);

                /* Load up the particular file */
                ifobj.Filename = ifinfo.FullName;
                ifobj.Read();
                ifiles.Add(ifobj);
            }
        }
    }
}
