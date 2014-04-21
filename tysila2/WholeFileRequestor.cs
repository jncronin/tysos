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

namespace tysila
{
    class WholeFileRequestor : libtysila.Assembler.MemberRequestor
    {
        bool started_iterating = false;

        int mod_idx = 0;
        int ass_idx = 0;
        int meth_mod_idx = 0;
        int meth_idx = 0;
        int type_mod_idx = 0;
        int type_idx = 0;
        int gm_idx;
        int sf_mod_idx = 0;
        int sf_idx = 0;

        List<libtysila.Metadata> modules = new List<libtysila.Metadata>();
        List<libtysila.Metadata> mods_to_do = new List<libtysila.Metadata>();
        List<libtysila.Metadata> ass_to_do = new List<libtysila.Metadata>();
        List<libtysila.Assembler.MethodToCompile> meths_to_do = new List<libtysila.Assembler.MethodToCompile>();
        List<libtysila.Assembler.TypeToCompile> types_to_do = new List<libtysila.Assembler.TypeToCompile>();
        List<libtysila.Assembler.MethodToCompile> gmis_to_do = new List<libtysila.Assembler.MethodToCompile>();
        List<libtysila.Assembler.TypeToCompile> sfs_to_do = new List<libtysila.Assembler.TypeToCompile>();

        public void RequestWholeModule(libtysila.Metadata module)
        {
            if (started_iterating)
                throw new Exception("Cannot add modules after iteration has started");
            if (!modules.Contains(module))
                modules.Add(module);
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
            started_iterating = true;

            if (meth_mod_idx != -1)
            {
                if (meth_idx >= modules[meth_mod_idx].Tables[(int)libtysila.Metadata.TableId.MethodDef].Length)
                {
                    meth_mod_idx++;
                    meth_idx = 0;
                }

                if (meth_mod_idx >= modules.Count)
                {
                    meth_mod_idx = -1;
                    meth_idx = 0;
                }
                else
                {
                    libtysila.Metadata.MethodDefRow mdr = modules[meth_mod_idx].Tables[(int)libtysila.Metadata.TableId.MethodDef][meth_idx++] as libtysila.Metadata.MethodDefRow;
                    libtysila.Assembler.TypeToCompile ttc = new libtysila.Assembler.TypeToCompile(libtysila.Metadata.GetOwningType(mdr.m, mdr), ass);
                    libtysila.Signature.BaseMethod msig = mdr.GetSignature();
                    return new libtysila.Assembler.MethodToCompile { _ass = ass, m = mdr.m, meth = mdr, msig = msig, type = ttc.type, tsigp = ttc.tsig };
                }
            }

            if (meth_mod_idx == -1)
                return meths_to_do[meth_idx++];
            throw new Exception();
        }

        public override libtysila.Assembler.MethodToCompile GetNextGenericMethodInfo()
        {
            started_iterating = true;
            return gmis_to_do[gm_idx++];
        }

        public override libtysila.Assembler.TypeToCompile GetNextTypeInfo()
        {
            started_iterating = true;

            if (type_mod_idx != -1)
            {
                if (type_idx >= modules[type_mod_idx].Tables[(int)libtysila.Metadata.TableId.TypeDef].Length)
                {
                    type_mod_idx++;
                    type_idx = 0;
                }

                if (type_mod_idx >= modules.Count)
                {
                    type_mod_idx = -1;
                    type_idx = 0;
                }
                else
                {
                    libtysila.Metadata.TypeDefRow tdr = modules[type_mod_idx].Tables[(int)libtysila.Metadata.TableId.TypeDef][type_idx++] as libtysila.Metadata.TypeDefRow;
                    libtysila.Assembler.TypeToCompile ttc = new libtysila.Assembler.TypeToCompile(tdr, ass);
                    if (tdr.IsGeneric)
                        return GetNextTypeInfo();
                    else
                        return ttc;
                }
            }

            if (type_mod_idx == -1)
                return types_to_do[type_idx++];
            throw new Exception();
        }

        public override libtysila.Assembler.TypeToCompile GetNextStaticFields()
        {
            started_iterating = true;

            if (sf_mod_idx != -1)
            {
                if (sf_idx >= modules[sf_mod_idx].Tables[(int)libtysila.Metadata.TableId.TypeDef].Length)
                {
                    sf_mod_idx++;
                    sf_idx = 0;
                }

                if (sf_mod_idx >= modules.Count)
                {
                    sf_mod_idx = -1;
                    sf_idx = 0;
                }
                else
                {
                    libtysila.Metadata.TypeDefRow tdr = modules[sf_mod_idx].Tables[(int)libtysila.Metadata.TableId.TypeDef][sf_idx++] as libtysila.Metadata.TypeDefRow;
                    libtysila.Assembler.TypeToCompile ttc = new libtysila.Assembler.TypeToCompile(tdr, ass);
                    return ttc;
                }
            }

            if (sf_mod_idx == -1)
                return sfs_to_do[sf_idx++];
            throw new Exception();
        }

        public override libtysila.Metadata GetNextAssembly()
        {
            if (ass_idx >= modules.Count)
                return ass_to_do[ass_idx++ - modules.Count];
            return modules[ass_idx++];
        }

        public override libtysila.Metadata GetNextModule()
        {
            if (mod_idx >= modules.Count)
                return mods_to_do[mod_idx++ - modules.Count];
            return modules[mod_idx++];
        }

        public override void PurgeAll()
        {
            throw new NotImplementedException();
        }
    }
}
