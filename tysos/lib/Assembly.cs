/* Copyright (C) 2012 by John Cronin
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

namespace tysos.lib
{
    class Assembly
    {
        [libsupcs.MethodAlias("_ZW19System#2EReflection8AssemblyM_0_20GetExecutingAssembly_RV8Assembly_P0")]
        static System.Reflection.Assembly GetExecutingAssembly()
        {
            // Return the System.Reflection.Assembly object of the code which called this function

            libsupcs.Unwinder u = Program.arch.GetUnwinder();
            u.UnwindOne();
            libsupcs.TysosMethod prev_method = u.GetMethodInfo();

            if (prev_method == null)
                throw new Exception("Unable to determine assembly due to methodinfo being null");
            if (prev_method.DeclaringType == null)
                throw new Exception("Unable to determine assembly due to methodinfo.DeclaringType being null");
            return prev_method.DeclaringType.Assembly;
        }

        [libsupcs.MethodAlias("_ZW19System#2EReflection8AssemblyM_0_27GetManifestResourceInternal_Ru1I_P4u1tu1SRiRV6Module")]
        IntPtr GetManifestResourceInternal(string name, out int size, out System.Reflection.Module module)
        {
            /* Return the address (and fill out size and module fields) of a resource embedded in the assembly.
             * 
             * For now, we don't support this and will return null
             * 
             * Log what was requested though
             */

            Formatter.WriteLine("System.Reflection.Assembly.GetManifestResourceInternal: Request for resource: " + name + " - currently unsupported.  Returning null.", Program.arch.DebugOutput);
            size = 0;
            module = null;
            return IntPtr.Zero;
        }
    }
}
