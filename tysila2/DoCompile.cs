/* Copyright (C) 2010 - 2012 by John Cronin
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
    class CompilerRunner
    {
#if MT
        internal class TTCRequest { public TypeToCompile ttc; public IOutputFile of; }
        internal class MTCRequest { public MethodToCompile mtc; public IOutputFile of; }
        internal class FTCRequest { public FieldToCompile ftc; public IOutputFile of; }
#endif

        static Assembler.LockedInt rtc = new Assembler.LockedInt();        /* number of threads running */

        internal static void DoCompile(Assembler ass, IOutputFile w) { DoCompile(ass, w, null, null); }
        internal static void DoCompile(Assembler ass, IOutputFile w, IDoCompileFeedback dcf) { DoCompile(ass, w, null, dcf); }
        internal static void DoCompile(Assembler ass, IOutputFile w, List<string> unimplemented_internal_calls, IDoCompileFeedback dcf)
        {
            Assembler.MemberRequestor Requestor = ass.Requestor;

            while (Requestor.MoreToDo || (rtc.Count > 0))
            {
                while (Requestor.MoreMethods)
                {
                    Assembler.MethodToCompile? mtc = null;

                    lock (Requestor.meth_lock)
                    {
                        if (Requestor.MoreMethods)
                            mtc = Requestor.GetNextMethod();
                    }

                    if (mtc.HasValue)
                    {
                        if (dcf != null)
                            dcf.AssembleMethodFeedback(mtc.Value);

#if MT
                        rtc++;
                        new System.Threading.Thread(AssembleMethod).Start(new MTCRequest { mtc = mtc.Value, of = w });
#else
                        ass.AssembleMethod(mtc.Value, w, unimplemented_internal_calls);
                        //ass.AssembleMethodInfo(mtc.Value, w);
#endif
                    }
                }

                while (Requestor.MoreTypeInfos)
                {
                    Assembler.TypeToCompile? ttc = null;

                    lock (Requestor.ti_lock)
                    {
                        if (Requestor.MoreTypeInfos)
                            ttc = Requestor.GetNextTypeInfo();
                    }

                    if (ttc.HasValue)
                    {
                        if (dcf != null)
                            dcf.AssembleTypeInfoFeedback(ttc.Value);
#if MT
                        rtc++;
                        if(ass.Options.EnableRTTI)
                            new System.Threading.Thread(AssembleTypeInfo).Start(new TTCRequest { ttc = ttc.Value, of = w });
                        else
                            throw new NotImplementedException();
#else
                        ass.AssembleType(ttc.Value, w);
#endif
                    }
                }

                while (Requestor.MoreStaticFields)
                {
                    Assembler.TypeToCompile? ttc = null;

                    lock (Requestor.static_fields_lock)
                    {
                        if (Requestor.MoreStaticFields)
                            ttc = Requestor.GetNextStaticFields();
                    }

                    if (ttc.HasValue)
                    {
                        if (dcf != null)
                            dcf.AssembleTypeInfoFeedback(ttc.Value);
                        ass.AssembleType(ttc.Value, w, true);
                    }
                }

                while (Requestor.MoreAssemblies)
                {
                    Metadata m = null;

                    lock (Requestor.assembly_lock)
                    {
                        if (Requestor.MoreAssemblies)
                            m = Requestor.GetNextAssembly();
                    }

                    if (m != null)
                        ass.AssembleAssemblyInfo(m, w);
                }

                while (Requestor.MoreModules)
                {
                    Metadata m = null;

                    lock (Requestor.module_lock)
                    {
                        if (Requestor.MoreModules)
                            m = Requestor.GetNextModule();
                    }

                    if (m != null)
                        ass.AssembleModuleInfo(m, w);
                }

                while (Requestor.MoreGenericMethodInfos)
                {
                    Assembler.MethodToCompile? mtc = null;

                    lock (Requestor.meth_lock)
                    {
                        if (Requestor.MoreGenericMethodInfos)
                            mtc = Requestor.GetNextGenericMethodInfo();
                    }

                    if (mtc.HasValue)
                    {
                        if (dcf != null)
                            dcf.AssembleMethodInfoFeedback(mtc.Value);

                        ass.AssembleMethodInfo(mtc.Value, w, true);
                    }
                }
            }
        }

        public interface IDoCompileFeedback
        {
            void AssembleMethodFeedback(Assembler.MethodToCompile mtc);
            void AssembleTypeFeedback(Assembler.TypeToCompile ttc);
            void AssembleTypeInfoFeedback(Assembler.TypeToCompile ttc);
            void AssembleFieldInfoFeedback(Assembler.FieldToCompile ftc);
            void AssembleMethodInfoFeedback(Assembler.MethodToCompile mtc);
        }
    }
}
