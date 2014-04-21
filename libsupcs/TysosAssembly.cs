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

/* This defines the TysosAssembly which is a subtype of System.Reflection.Assembly
 * 
 * All AssemblyInfo structures produced by tysila2 follow this layout
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace libsupcs
{
    /* System.Reflection.Assembly defines an internal constructor, so we cannot subclass
     * it directly outside of corlib therefore we need to use the following attribute */
    [ExtendsOverride("_ZW19System#2EReflection8AssemblyTI")]
    [VTableAlias("__tysos_assembly_vt")]
    public class TysosAssembly
    {
        [NullTerminatedListOf(typeof(TysosType))]
        IntPtr _Types;

        [MethodAlias("_ZW19System#2EReflection8AssemblyM_0_8GetTypes_Ru1ZU6System4Type_P2u1tb")]
        System.Type[] GetTypes(bool exportedOnly)
        {
            unsafe
            {
                IntPtr* cur_type = (IntPtr*)_Types;
                int count = 0;

                if (cur_type != null)
                {
                    while (*cur_type != (IntPtr)0)
                    {
                        TysosType t = TysosType.ReinterpretAsType(*cur_type);

                        if (!exportedOnly || t.IsPublic)
                            count++;

                        cur_type++;
                    }

                    Type[] ret = new Type[count];
                    cur_type = (IntPtr *)_Types;
                    int i = 0;
                    while(*cur_type != (IntPtr)0)
                    {
                        TysosType t = TysosType.ReinterpretAsType(*cur_type);

                        if (!exportedOnly || t.IsPublic)
                            ret[i++] = t;

                        cur_type++;
                    }

                    return ret;
                }
            }

            return new Type[] { };
        }
    }

    [ExtendsOverride("_ZW19System#2EReflection6ModuleTI")]
    [VTableAlias("__tysos_module_vt")]
    public class TysosModule
    {
        long compile_time;
        public DateTime CompileTime { get { return new DateTime(compile_time); } }

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern TysosModule ReinterpretAsTysosModule(System.Reflection.Module module);
    }
}
