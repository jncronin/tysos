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

namespace tydb
{
    abstract class dbgarch
    {
        internal class register
        {
            internal int length;
            internal string name;
            internal int id;
        }

        internal register[] registers;
        internal int PC_id;
        internal bool is_lsb = true;
        internal tydisasm.tydisasm disasm = null;
        internal int address_size = 4;
        internal int data_size = 4;
        internal libtysila.Assembler ass;

        internal register get_reg(string name)
        {
            if (registers == null)
                return null;

            foreach (register r in registers)
            {
                if (r.name == name)
                    return r;
            }

            return null;
        }

        internal abstract bool Init(string[] features);

        static internal bool Create()
        {
            if (Program.arch_name == null)
                throw new Exception("architecture not selected");

            try
            {
                Type archt = Type.GetType("tydb." + Program.arch_name + "_dbgarch", false, true);
                System.Reflection.ConstructorInfo ctorm = archt.GetConstructor(new Type[] { });
                if (ctorm == null)
                    throw new TypeLoadException();
                Program.arch = ctorm.Invoke(new object[] { }) as dbgarch;
            }
            catch (Exception e)
            {
                if (e is TypeLoadException)
                {
                    Console.WriteLine(Program.arch_name + " is not a valid debug architecture");
                    Console.WriteLine();
                    return false;
                }
                throw;
            }

            if(Program.arch.Init(Program.feature_list.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)))
            {
                //Program.arch.ass.search_dirs = Program.lib_dirs;
                return true;
            }
            return false;
        }
    }
}
