/* Copyright (C) 2008 - 2011 by John Cronin
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

namespace libtysila
{
    partial class Assembler
    {
        private MemberRequestor _requestor; public MemberRequestor Requestor { get { return _requestor; } }

        public abstract class MemberRequestor
        {
            public List<Metadata> ModulesToAssemble = new List<Metadata>();

            public object meth_lock = new object();
            public object type_lock = new object();
            public object ti_lock = new object();
            public object gmi_lock = new object();
            public object module_lock = new object();
            public object assembly_lock = new object();
            public object static_fields_lock = new object();

            void check_rtti_enabled()
            {
                if (!ass._options.EnableRTTI)
                    throw new Exception("Attempt to request RTTI object when -fno-rtti was passed");
            }

            public virtual void RequestModule(Metadata module) { check_rtti_enabled(); }
            public virtual void RequestAssembly(Metadata module) { check_rtti_enabled(); }
            public virtual void RequestMethod(Assembler.MethodToCompile mtc) { }
            public virtual void RequestGenericMethodInfo(Assembler.MethodToCompile mtc) { check_rtti_enabled(); }
            public virtual void RequestTypeInfo(Assembler.TypeToCompile ttc) { /*check_rtti_enabled();*/ }
            public virtual void RequestStaticFields(Assembler.TypeToCompile ttc) { }
            public abstract void ExcludeModule(Metadata module);
            public abstract void ExcludeAssembly(Metadata module);
            public abstract void ExcludeMethod(Assembler.MethodToCompile mtc);
            public abstract void ExcludeTypeInfo(Assembler.TypeToCompile ttc);
            public abstract void ExcludeGenericMethodInfo(Assembler.MethodToCompile mtc);
            public abstract void SkipChecks(bool skip);

            public abstract Assembler.MethodToCompile GetNextMethod();
            public abstract Assembler.MethodToCompile GetNextGenericMethodInfo();
            public abstract Assembler.TypeToCompile GetNextTypeInfo();
            public abstract Assembler.TypeToCompile GetNextStaticFields();
            public abstract Metadata GetNextAssembly();
            public abstract Metadata GetNextModule();

            protected Assembler ass;
            public Assembler Assembler { get { return ass; } set { ass = value; } }

            public abstract void PurgeAll();

            public abstract bool MoreTypeInfos { get; }
            public abstract bool MoreMethods { get; }
            public abstract bool MoreModules { get; }
            public abstract bool MoreAssemblies { get; }
            public abstract bool MoreGenericMethodInfos { get; }
            public abstract bool MoreStaticFields { get; }
            public virtual bool MoreToDo { get { return MoreTypeInfos || MoreMethods || MoreModules || MoreAssemblies || MoreGenericMethodInfos || MoreStaticFields; } }
        }

        public class FileBasedMemberRequestor : MemberRequestor
        {
            internal bool skip_checks = false;

            bool ignore_libsupcs(Metadata m)
            {
                if (m == null)
                    return false;
                if (m.IsLibSupCs && !ass.Options.IncludeLibsupcs && !ass.Options.InExtraAdd)
                    return true;
                if (m.IsLibStdCs && !ass.Options.IncludeLibstdcs && !ass.Options.InExtraAdd)
                    return true;
                return false;
            }

            public override void SkipChecks(bool skip)
            {
                skip_checks = skip;
            }

            public override void RequestModule(Metadata module)
            {
                base.RequestModule(module);
                if (ignore_libsupcs(module))
                    return;
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(module)))
                {
                    lock (module_lock)
                    {
                        if (!_requested_modules.Contains(module) && !_compiled_modules.Contains(module))
                            _requested_modules.Add(module);
                    }
                }
            }

            public override void RequestAssembly(Metadata module)
            {
                base.RequestAssembly(module);
                if (ignore_libsupcs(module))
                    return;
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(module)))
                {
                    lock (assembly_lock)
                    {
                        if (!_requested_assemblies.Contains(module) && !_compiled_assemblies.Contains(module))
                            _requested_assemblies.Add(module);
                    }
                }
            }

            public override void RequestMethod(Assembler.MethodToCompile mtc)
            {
#if DEBUG
                if (mtc.tsig is Signature.GenericType)
                {
                    Signature.GenericType gt = mtc.tsig as Signature.GenericType;
                    foreach (Signature.BaseOrComplexType gp in gt.GenParams)
                    {
                        if ((gp is Signature.BaseType) && (((Signature.BaseType)gp).Type == BaseType_Type.UninstantiatedGenericParam))
                            throw new Exception("Trying to request method of uninstantiated generic type");
                    }
                }

                if (mtc.type.IsValueType(ass))
                {
                    //if (!(mtc.tsig is Signature.ManagedPointer) && !(mtc.tsig is Signature.BoxedType))
                    //    throw new Exception("Attempt to request method on value type without pointer or boxed type reference");
                }
#endif
                base.RequestMethod(mtc);
                if (ignore_libsupcs(mtc.type.m))
                    return;
                if (mtc.meth.ExcludedByArch)
                    return;
                if (mtc.meth.IgnoreAttribute)
                    return;
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(mtc.meth.m)))
                {
                    lock (meth_lock)
                    {
                        if (mtc.type.IsDelegate(ass) && (mtc.meth.Name == ".ctor"))
                            ass.RewriteDelegateCtor(mtc.msig.Method);
                        if ((skip_checks) || ((!ExistsIn(mtc, _requested_meths)) && (!ExistsIn(mtc, _compiled_meths))))
                            _requested_meths.Add(mtc, 0);
                    }
                }
            }

            public override void RequestGenericMethodInfo(Assembler.MethodToCompile mtc)
            {
                base.RequestGenericMethodInfo(mtc);
                if (ignore_libsupcs(mtc.type.m))
                    return;
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(mtc.meth.m)))
                {
                    lock (meth_lock)
                    {
                        if (mtc.type.IsDelegate(ass) && (mtc.meth.Name == ".ctor"))
                            ass.RewriteDelegateCtor(mtc.msig.Method);
//                        if ((skip_checks) || ((!ExistsIn(mtc, _requested_gmis))))
                        if ((skip_checks) || ((!ExistsIn(mtc, _requested_gmis)) && (!ExistsIn(mtc, _compiled_gmis) && ((mtc.msig.Method.CallingConvention == Signature.Method.CallConv.Generic) || ((!ExistsIn(mtc, _requested_meths)) && (!ExistsIn(mtc, _compiled_meths)))))))
                            _requested_gmis.Add(mtc, 0);
                    }
                }
            }

            public override void RequestTypeInfo(Assembler.TypeToCompile ttc)
            {
                if (ttc.type == null)
                    return;

                base.RequestTypeInfo(ttc);
                if (ignore_libsupcs(ttc.type.m))
                    return;
                if (ttc.type.ExcludedByArch)
                    return;
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(ttc.type.m)))
                {
                    lock (ti_lock)
                    {
                        if ((skip_checks) || ((!ExistsIn(ttc, _requested_tis)) && (!ExistsIn(ttc, _compiled_tis))))
                            _requested_tis.Add(ttc, 0);
                    }
                }
            }

            public override void RequestStaticFields(Assembler.TypeToCompile ttc)
            {
                base.RequestStaticFields(ttc);
                if (ignore_libsupcs(ttc.type.m))
                    return;
                if (ttc.type.ExcludedByArch)
                    return;
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(ttc.type.m)))
                {
                    lock (ti_lock)
                    {
                        if ((skip_checks) || ((!ExistsIn(ttc, _requested_sfs)) && (!ExistsIn(ttc, _compiled_sfs))))
                            _requested_sfs.Add(ttc, 0);
                    }
                }
            }


            public override void ExcludeModule(Metadata module)
            {
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(module)))
                {
                    lock (module_lock)
                    {
                        if (!_requested_modules.Contains(module) && !_compiled_modules.Contains(module))
                            _compiled_modules.Add(module);
                    }
                }
            }

            public override void ExcludeAssembly(Metadata module)
            {
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(module)))
                {
                    lock (assembly_lock)
                    {
                        if (!_requested_assemblies.Contains(module) && !_compiled_assemblies.Contains(module))
                            _compiled_assemblies.Add(module);
                    }
                }
            }


            public override void ExcludeMethod(Assembler.MethodToCompile mtc)
            {
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(mtc.meth.m)))
                {
                    lock (meth_lock)
                    {
                        if (mtc.type.IsDelegate(ass) && (mtc.meth.Name == ".ctor"))
                            ass.RewriteDelegateCtor(mtc.msig.Method);
                        if ((skip_checks) || ((!ExistsIn(mtc, _requested_meths)) && (!ExistsIn(mtc, _compiled_meths))))
                            _compiled_meths.Add(mtc, 0);
                    }
                }
            }

            public override void ExcludeTypeInfo(Assembler.TypeToCompile ttc)
            {
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(ttc.type.m)))
                {
                    lock (ti_lock)
                    {
                        if ((skip_checks) || ((!ExistsIn(ttc, _requested_tis)) && (!ExistsIn(ttc, _compiled_tis))))
                            _compiled_tis.Add(ttc, 0);
                    }
                }
            }

            public override void ExcludeGenericMethodInfo(Assembler.MethodToCompile mtc)
            {
                if ((ModulesToAssemble.Count == 0) || (ModulesToAssemble.Contains(mtc.meth.m)))
                {
                    lock (meth_lock)
                    {
                        if (mtc.type.IsDelegate(ass) && (mtc.meth.Name == ".ctor"))
                            ass.RewriteDelegateCtor(mtc.msig.Method);
                        if ((skip_checks) || ((!ExistsIn(mtc, _requested_gmis)) && (!ExistsIn(mtc, _compiled_gmis))))
                            _compiled_gmis.Add(mtc, 0);
                    }
                }
            }

            private bool ExistsIn(Assembler.MethodToCompile mtc, Dictionary<Assembler.MethodToCompile, int> mtcs)
            {
                return mtcs.ContainsKey(mtc);
            }

            private bool ExistsIn(Assembler.TypeToCompile ttc, Dictionary<Assembler.TypeToCompile, int> ttcs)
            {
                return ttcs.ContainsKey(ttc);
            }

            private bool ExistsIn(Assembler.FieldToCompile ftc, Dictionary<Assembler.FieldToCompile, int> ftcs)
            {
                return ftcs.ContainsKey(ftc);
            }

            internal Dictionary<Assembler.TypeToCompile, int> _requested_tis = new Dictionary<Assembler.TypeToCompile, int>();
            internal Dictionary<Assembler.TypeToCompile, int> _requested_sfs = new Dictionary<Assembler.TypeToCompile, int>();
            internal Dictionary<Assembler.MethodToCompile, int> _requested_meths = new Dictionary<Assembler.MethodToCompile, int>();
            internal Dictionary<Assembler.MethodToCompile, int> _requested_gmis = new Dictionary<MethodToCompile, int>();
            internal List<Metadata> _requested_modules = new List<Metadata>();
            internal List<Metadata> _requested_assemblies = new List<Metadata>();

            internal Dictionary<Assembler.TypeToCompile, int> _compiled_tis = new Dictionary<Assembler.TypeToCompile, int>();
            internal Dictionary<Assembler.TypeToCompile, int> _compiled_sfs = new Dictionary<Assembler.TypeToCompile, int>();
            internal Dictionary<Assembler.MethodToCompile, int> _compiled_meths = new Dictionary<Assembler.MethodToCompile, int>();
            internal Dictionary<Assembler.MethodToCompile, int> _compiled_gmis = new Dictionary<MethodToCompile, int>();
            internal List<Metadata> _compiled_modules = new List<Metadata>();
            internal List<Metadata> _compiled_assemblies = new List<Metadata>();

            public override bool MoreTypeInfos { get { if (_requested_tis.Count == 0) return false; return true; } }
            public override bool MoreMethods { get { if (_requested_meths.Count == 0) return false; return true; } }
            public override bool MoreModules { get { if (_requested_modules.Count == 0) return false; return true; } }
            public override bool MoreAssemblies { get { if (_requested_assemblies.Count == 0) return false; return true; } }
            public override bool MoreGenericMethodInfos { get { if (_requested_gmis.Count == 0) return false; return true; } }
            public override bool MoreStaticFields { get { if (_requested_sfs.Count == 0) return false; return true; } }
            public override bool MoreToDo { get { lock (ti_lock) { lock (meth_lock) { lock (type_lock) { lock (gmi_lock) { lock (assembly_lock) { lock (module_lock) { lock (static_fields_lock) { if (MoreTypeInfos || MoreMethods || MoreModules || MoreAssemblies || MoreGenericMethodInfos || MoreStaticFields) return true; else return false; } } } } } } } } }

            public void PurgeRequestedTypeInfos() { _requested_tis.Clear(); }
            public void PurgeRequestedMethods() { _requested_meths.Clear(); }
            public void PurgeRequestedModules() { _requested_modules.Clear(); }
            public void PurgeRequestedAssemblies() { _requested_assemblies.Clear(); }
            public void PurgeRequestedGenericMethodInfos() { _requested_gmis.Clear(); }
            public void PurgeRequestedStaticFields() { _requested_sfs.Clear(); }
            public void PurgeAssembledTypeInfos() { _compiled_tis.Clear(); }
            public void PurgeAssembledMethods() { _compiled_meths.Clear(); }
            public void PurgeAssembledModules() { _compiled_modules.Clear(); }
            public void PurgeAssembledAssemblies() { _compiled_assemblies.Clear(); }
            public void PurgeAssembledGenericMethodInfos() { _compiled_gmis.Clear(); }
            public void PurgeAssembledStaticFields() { _compiled_sfs.Clear(); }
            public override void PurgeAll()
            {
                PurgeAssembledMethods(); PurgeAssembledTypeInfos(); PurgeRequestedMethods(); PurgeRequestedTypeInfos(); 
                PurgeAssembledAssemblies(); PurgeAssembledModules(); PurgeRequestedAssemblies(); PurgeRequestedModules();
                PurgeAssembledGenericMethodInfos(); PurgeRequestedGenericMethodInfos();
                PurgeAssembledStaticFields(); PurgeRequestedStaticFields();
            }

            public override Assembler.MethodToCompile GetNextMethod()
            {
                if (!MoreMethods)
                    throw new Exception("No more methods");
                
                IEnumerator<MethodToCompile> e = _requested_meths.Keys.GetEnumerator();
                e.MoveNext();
                Assembler.MethodToCompile ret = e.Current;
                _compiled_meths.Add(ret, 0);
                _requested_meths.Remove(ret);
                return ret;
            }

            public override Assembler.MethodToCompile GetNextGenericMethodInfo()
            {
                if (!MoreGenericMethodInfos)
                    throw new Exception("No more methods");

                IEnumerator<MethodToCompile> e = _requested_gmis.Keys.GetEnumerator();
                e.MoveNext();
                Assembler.MethodToCompile ret = e.Current;
                _compiled_gmis.Add(ret, 0);
                _requested_gmis.Remove(ret);
                return ret;
            }

            public override Assembler.TypeToCompile GetNextTypeInfo()
            {
                if (!MoreTypeInfos)
                    throw new Exception("No more typeinfos");

                IEnumerator<TypeToCompile> e = _requested_tis.Keys.GetEnumerator();
                e.MoveNext();
                Assembler.TypeToCompile ret = e.Current;
                _compiled_tis.Add(ret, 0);
                _requested_tis.Remove(ret);
                return ret;
            }
            public override Assembler.TypeToCompile GetNextStaticFields()
            {
                if (!MoreStaticFields)
                    throw new Exception("No more static fields");

                IEnumerator<TypeToCompile> e = _requested_sfs.Keys.GetEnumerator();
                e.MoveNext();
                Assembler.TypeToCompile ret = e.Current;
                _compiled_sfs.Add(ret, 0);
                _requested_sfs.Remove(ret);
                return ret;
            }
            public override Metadata GetNextAssembly()
            {
                if (!MoreAssemblies)
                    throw new Exception("No more assemblies");
                Metadata ret = _requested_assemblies[0];
                _requested_assemblies.RemoveAt(0);
                _compiled_assemblies.Add(ret);
                return ret;
            }
            public override Metadata GetNextModule()
            {
                if (!MoreModules)
                    throw new Exception("No more modules");
                Metadata ret = _requested_modules[0];
                _requested_modules.RemoveAt(0);
                _compiled_modules.Add(ret);
                return ret;
            }
        }
    }
}
