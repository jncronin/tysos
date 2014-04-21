/* Copyright (C) 2013 by John Cronin
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

namespace tysos.jit
{
    class Jit
    {
        [libsupcs.MethodAlias("jit_tm")]
        internal static IntPtr JitCompile(libsupcs.TysosMethod m)
        {
            // Determine if the method is already compiled
            string mangled_name = libstdcs.Mangler.MangleMethod(m);
            ulong address = Program.stab.GetAddress(mangled_name);
            if (address != 0)
                return (IntPtr)(long)address;
            
            // Otherwise we have to compile it
            Formatter.Write("Request to compile method: ", Program.arch.DebugOutput);
            Formatter.Write(m.Name, Program.arch.DebugOutput);
            Formatter.WriteLine(".  Not currently implemenented.", Program.arch.DebugOutput);

            Formatter.Write("Mangled name: ", Program.arch.DebugOutput);
            Formatter.WriteLine(mangled_name, Program.arch.DebugOutput);

            return (IntPtr)0;
        }

        [libsupcs.MethodAlias("jit_addrof")]
        internal static IntPtr GetAddressOfObject(string name)
        {
            ulong address = Program.stab.GetAddress(name);
            if (address != 0)
                return (IntPtr)(long)address;

            // Requested name does not exist
            Formatter.Write("Request for object: ", Program.arch.DebugOutput);
            Formatter.Write(name, Program.arch.DebugOutput);
            Formatter.WriteLine(".  Does not exist.", Program.arch.DebugOutput);

            throw new MissingMemberException(name);
        }
    }
}
