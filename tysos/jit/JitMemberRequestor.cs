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

namespace tysos.jit
{
    class JitMemberRequestor : libtysila.Assembler.MemberRequestor
    {
        List<string> compiled_objects = new List<string>();
        List<JitMethod> req_meths = new List<JitMethod>();
        List<libtysila.Metadata> req_ass = new List<libtysila.Metadata>();
        List<libtysila.Assembler.MethodToCompile> req_gms = new List<libtysila.Assembler.MethodToCompile>();
        List<libtysila.Metadata> req_mods = new List<libtysila.Metadata>();
        List<libtysila.Assembler.TypeToCompile> req_tis = new List<libtysila.Assembler.TypeToCompile>();

        internal class JitMethod
        {
            public libtysila.Assembler.MethodToCompile mtc;
            public bool is_jit_stub;
        }

        public override void RequestMethod(libtysila.Assembler.MethodToCompile mtc)
        {
            RequestMethod(mtc, true);
        }

        public void RequestMethod(libtysila.Assembler.MethodToCompile mtc, bool jit_stub)
        {
            if (mtc.type.IsDelegate(ass) && (mtc.meth.Name == ".ctor"))
            {
                // Rewrite delegate constructors
                mtc.msig.Method.Params[1] = new libtysila.Signature.Param(libtysila.BaseType_Type.VirtFtnPtr);
            }

            string mangled_name = mtc.ToString();
            if (compiled_objects.Contains(mangled_name))
                return;
            if (Program.IsCompiled(mangled_name))
                return;
            foreach (JitMethod jm in req_meths)
            {
                if (jm.mtc.Equals(mtc))
                    return;
            }

            base.RequestMethod(mtc);
            req_meths.Add(new JitMethod { mtc = mtc, is_jit_stub = jit_stub });
        }

        public override void RequestAssembly(libtysila.Metadata module)
        {
            string mangled_name = libtysila.Mangler2.MangleAssembly(module, ass);
            if (compiled_objects.Contains(mangled_name))
                return;
            if (Program.IsCompiled(mangled_name))
                return;
            if (req_mods.Contains(module))
                return;

            base.RequestAssembly(module);
            req_mods.Add(module);
        }

        public override void RequestGenericMethodInfo(libtysila.Assembler.MethodToCompile mtc)
        {
            string mangled_name = libtysila.Mangler2.MangleMethodInfoSymbol(mtc, ass);
            if (compiled_objects.Contains(mangled_name))
                return;
            if (Program.IsCompiled(mangled_name))
                return;
            if (req_gms.Contains(mtc))
                return;

            base.RequestGenericMethodInfo(mtc);
            req_gms.Add(mtc);
        }

        public override void RequestModule(libtysila.Metadata module)
        {
            string mangled_name = libtysila.Mangler2.MangleModule(module, ass);
            if (compiled_objects.Contains(mangled_name))
                return;
            if (Program.IsCompiled(mangled_name))
                return;
            if (req_ass.Contains(module))
                return;

            base.RequestModule(module);
            req_ass.Add(module);
        }

        public override void RequestStaticFields(libtysila.Assembler.TypeToCompile ttc)
        {
            throw new NotImplementedException();
            //base.RequestStaticFields(ttc);
        }

        public override void RequestTypeInfo(libtysila.Assembler.TypeToCompile ttc)
        {
            string mangled_name = ttc.ToString();
            if (compiled_objects.Contains(mangled_name))
                return;
            if (Program.IsCompiled(mangled_name))
                return;
            if (req_tis.Contains(ttc))
                return;

            base.RequestTypeInfo(ttc);
            req_tis.Add(ttc);
        }

        public override void ExcludeModule(libtysila.Metadata module)
        {
            throw new NotImplementedException();
        }

        public override void ExcludeAssembly(libtysila.Metadata module)
        {
            throw new NotImplementedException();
        }

        public override void ExcludeMethod(libtysila.Assembler.MethodToCompile mtc)
        {
            throw new NotImplementedException();
        }

        public override void ExcludeTypeInfo(libtysila.Assembler.TypeToCompile ttc)
        {
            throw new NotImplementedException();
        }

        public override void ExcludeGenericMethodInfo(libtysila.Assembler.MethodToCompile mtc)
        {
            throw new NotImplementedException();
        }

        public override void SkipChecks(bool skip)
        {
            throw new NotImplementedException();
        }

        public override libtysila.Assembler.MethodToCompile GetNextMethod()
        {
            throw new NotImplementedException();
        }

        public JitMethod GetNextJitMethod()
        {
            if (req_meths.Count > 0)
            {
                JitMethod ret = req_meths[0];
                req_meths.RemoveAt(0);
                compiled_objects.Add(ret.mtc.ToString());
                return ret;
            }
            return null;
        }

        public libtysila.Assembler.MethodToCompile? GetNextJitMethodInfo()
        {
            if (req_gms.Count > 0)
            {
                libtysila.Assembler.MethodToCompile ret = req_gms[0];
                req_gms.RemoveAt(0);
                compiled_objects.Add(ret.ToString());
                return ret;
            }
            return null;
        }

        public libtysila.Assembler.TypeToCompile? GetNextJITType()
        {
            if (req_tis.Count > 0)
            {
                libtysila.Assembler.TypeToCompile ret = req_tis[0];
                req_tis.RemoveAt(0);
                compiled_objects.Add(ret.ToString());
                return ret;
            }
            return null;
        }

        public override libtysila.Assembler.MethodToCompile GetNextGenericMethodInfo()
        {
            throw new NotImplementedException();
        }

        public override libtysila.Assembler.TypeToCompile GetNextTypeInfo()
        {
            throw new NotImplementedException();
        }

        public override libtysila.Assembler.TypeToCompile GetNextStaticFields()
        {
            throw new NotImplementedException();
        }

        public override libtysila.Metadata GetNextAssembly()
        {
            if (req_ass.Count > 0)
            {
                libtysila.Metadata ret = req_ass[0];
                req_ass.RemoveAt(0);
                compiled_objects.Add(ret.ModuleName);
                return ret;
            }
            return null;
        }

        public override libtysila.Metadata GetNextModule()
        {
            if (req_mods.Count > 0)
            {
                libtysila.Metadata ret = req_mods[0];
                req_mods.RemoveAt(0);
                compiled_objects.Add(ret.ModuleName);
                return ret;
            }
            return null;
        }

        public override void PurgeAll()
        {
            throw new NotImplementedException();
        }
    }
}
