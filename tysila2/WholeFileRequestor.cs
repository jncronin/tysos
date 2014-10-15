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
using libtysila;

namespace tysila
{
    class WholeFileRequestor : libtysila.Assembler.MemberRequestor
    {
        util.Stack<Assembler.TypeToCompile> types = new util.Stack<Assembler.TypeToCompile>();
        util.Stack<Assembler.MethodToCompile> methods = new util.Stack<Assembler.MethodToCompile>();
        util.Stack<Metadata> modules = new util.Stack<Metadata>();
        util.Stack<Metadata> assemblies = new util.Stack<Metadata>();

        util.Stack<Assembler.MethodToCompile> generic_methods = new util.Stack<Assembler.MethodToCompile>();
        util.Set<Assembler.MethodToCompile> generic_methods_done = new util.Set<Assembler.MethodToCompile>();

        util.Set<Assembler.TypeToCompile> generic_types_done = new util.Set<Assembler.TypeToCompile>();
        util.Set<Assembler.MethodToCompile> generic_type_methods_done = new util.Set<Assembler.MethodToCompile>();

        public void RequestWholeModule(libtysila.Metadata module)
        {
            if (ass.Options.EnableRTTI)
            {
                modules.Push(module);
                assemblies.Push(module);
            }

            foreach (Metadata.TypeDefRow tdr in module.Tables[(int)Metadata.TableId.TypeDef])
            {
                if (tdr.ExcludedByArch)
                    continue;
                types.Push(new Assembler.TypeToCompile { _ass = ass, tsig = new Signature.Param(tdr, ass), type = tdr });
            }

            if (module.ModuleName == "libsupcs")
            {
                Signature.Param vf_p = new Signature.Param(BaseType_Type.VirtFtnPtr);
                Metadata.TypeDefRow vf_tdr = Metadata.GetTypeDef(vf_p.Type, ass);
                Assembler.TypeToCompile vf_ttc = new libtysila.Assembler.TypeToCompile { _ass = ass, type = vf_tdr, tsig = vf_p };
                types.Push(vf_ttc);

                Signature.Param gp_p = new Signature.Param(BaseType_Type.UninstantiatedGenericParam);
                Metadata.TypeDefRow gp_tdr = Metadata.GetTypeDef(gp_p.Type, ass);
                Assembler.TypeToCompile gp_ttc = new Assembler.TypeToCompile { _ass = ass, type = gp_tdr, tsig = gp_p };
                types.Push(gp_ttc);

                Signature.Param gmp_p = new Signature.Param(BaseType_Type.UninstantiatedGenericMethodParam);
                Metadata.TypeDefRow gmp_tdr = Metadata.GetTypeDef(gmp_p.Type, ass);
                Assembler.TypeToCompile gmp_ttc = new Assembler.TypeToCompile { _ass = ass, type = gmp_tdr, tsig = gmp_p };
                types.Push(gmp_ttc);
            }

            foreach (Metadata.MethodDefRow mdr in module.Tables[(int)Metadata.TableId.MethodDef])
            {
                if (mdr.IsGeneric)
                    continue;
                if (mdr.owning_type.IsGeneric)
                    continue;
                if (mdr.ExcludedByArch)
                    continue;
                if (mdr.owning_type.ExcludedByArch)
                    continue;
                methods.Push(new Assembler.MethodToCompile
                {
                    _ass = ass,
                    tsigp = new Signature.Param(mdr.owning_type, ass),
                    type = mdr.owning_type,
                    meth = mdr,
                    msig = mdr.GetSignature(),
                    m = mdr.m
                });
            }
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
            return methods.Pop();
        }

        public override libtysila.Assembler.MethodToCompile GetNextGenericMethodInfo()
        {
            return generic_methods.Pop();
        }

        public override libtysila.Assembler.TypeToCompile GetNextTypeInfo()
        {
            return types.Pop();
        }

        public override libtysila.Assembler.TypeToCompile GetNextStaticFields()
        {
            throw new NotImplementedException();
        }

        public override libtysila.Metadata GetNextAssembly()
        {
            return assemblies.Pop();
        }

        public override libtysila.Metadata GetNextModule()
        {
            return modules.Pop();
        }

        public override void PurgeAll()
        {
            throw new NotImplementedException();
        }

        public override bool MoreAssemblies
        {
            get { return assemblies.Count > 0; }
        }

        public override bool MoreGenericMethodInfos
        {
            get { return generic_methods.Count > 0; }
        }

        public override bool MoreMethods
        {
            get { return methods.Count > 0; }
        }

        public override bool MoreModules
        {
            get { return modules.Count > 0; }
        }

        public override bool MoreTypeInfos
        {
            get { return types.Count > 0; }
        }

        public override bool MoreStaticFields
        {
            get { return false; }
        }

        public override void RequestAssembly(Metadata module)
        {
            base.RequestAssembly(module);
        }

        public override void RequestModule(Metadata module)
        {
            base.RequestModule(module);
        }

        public override void RequestGenericMethodInfo(Assembler.MethodToCompile mtc)
        {
            base.RequestGenericMethodInfo(mtc);
            if (mtc.meth.ExcludedByArch)
                return;

            //if (mtc.IsInstantiable == false)
            //    throw new Exception();

            if (!generic_methods_done.Contains(mtc))
            {
                generic_methods_done.Add(mtc);
                generic_methods.Push(mtc);
            }
        }

        public override void RequestMethod(Assembler.MethodToCompile mtc)
        {
            base.RequestMethod(mtc);
            if (mtc.meth.ExcludedByArch)
                return;
            if (mtc.type.ExcludedByArch)
                return;

            if (mtc.IsInstantiable == false)
                throw new Exception();

            /* Is this a method on an instantiated generic type or generic method instantiation? */
            if (mtc.tsig.IsWeakLinkage || (mtc.msig is Signature.GenericMethod))
            {
                if (!generic_type_methods_done.Contains(mtc))
                {
                    generic_type_methods_done.Add(mtc);
                    methods.Push(mtc);
                }
            }

            /* otherwise don't do anything (methods in this module are already
             * requested) */
        }

        public override void RequestTypeInfo(Assembler.TypeToCompile ttc)
        {
            base.RequestTypeInfo(ttc);
            if (ttc.type.ExcludedByArch)
                return;

            if (ttc.IsInstantiable == false)
            {
                int asdfgfdas = 0;
            }

            /* Is this a generic type instantiation? */
            if (ttc.tsig.Type.IsWeakLinkage)
            {
                if (!generic_types_done.Contains(ttc))
                {
                    generic_types_done.Add(ttc);
                    types.Push(ttc);
                }
            }

            /* otherwise don't do anything (non-generic types in this module are
             * already requested) */
        }

        public override void RequestStaticFields(Assembler.TypeToCompile ttc)
        {
            base.RequestStaticFields(ttc);
            RequestTypeInfo(ttc);
        }
    }
}
