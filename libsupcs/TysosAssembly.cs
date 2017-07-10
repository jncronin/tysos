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
    [ExtendsOverride("_ZW19System#2EReflection8Assembly")]
    [VTableAlias("__tysos_assembly_vt")]
    public unsafe class TysosAssembly
    {
        void* metadata;

        [MethodAlias("_ZW19System#2EReflection8Assembly_8GetTypes_Ru1ZU6System4Type_P2u1tb")]
        System.Type[] GetTypes(bool exportedOnly)
        {
            throw new NotImplementedException();
        }

        [AlwaysCompile]
        [MethodAlias("_ZW19System#2EReflection8Assembly_27GetManifestResourceInternal_Ru1I_P4u1tu1SRiRV6Module")]
        static void* GetManifestResource(void *ass, string name, out int size, out System.Reflection.Module mod)
        {
            // this always fails for now
            size = 0;
            mod = null;
            return null;
        }
    }

    [ExtendsOverride("_ZW19System#2EReflection6Module")]
    [VTableAlias("__tysos_module_vt")]
    public class TysosModule
    {
        long compile_time;
        public DateTime CompileTime { get { return new DateTime(compile_time); } }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [ReinterpretAsMethod]
        public static extern TysosModule ReinterpretAsTysosModule(System.Reflection.Module module);
    }
}
