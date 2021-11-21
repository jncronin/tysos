using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.jit
{
    class JitProcess
    {
        const int JF_NOTDONE = 0;
        const int JF_INPROG = 1;
        const int JF_DONE = 2;

        public static void ProcessRequestedItems(libtysila5.TysilaState s, SymbolTable stab)
        {
            while(!s.r.Empty)
            {
                /* TODO:
                 * check each member and process accordingly.
                 * 
                 * First need to check it has not already been done or is in progress (need flag member of each type to do this, as well as check symbol table)
                 * - If not in progress/done, remove from requestor, set as in process and do it, then set done
                 * - If in progress, return it to the requestor
                 * - If done, do nothing (already removed)
                 * 
                 * Members of MethodRequestor just need a JIT stub creating (TODO)
                 * Members of FullMethodRequestor actually need creating
                 */

                while(!s.r.BoxedMethodRequestor.Empty)
                {
                    var ne = s.r.BoxedMethodRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if(f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        s.r.BoxedMethodRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  TODO: check not in stab (do we need this step?)
                    libtysila5.libtysila.AssembleBoxedMethod(ne.ms, s.bf, Jit.t, s);

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }

                while (!s.r.DelegateRequestor.Empty)
                {
                    var ne = s.r.DelegateRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if (f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        s.r.DelegateRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  TODO: check not in stab (do we need this step?)
                    libtysila5.ir.ConvertToIR.CreateDelegate(ne, Jit.t, s);

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }

                while (!s.r.EHRequestor.Empty)
                {
                    var ne = s.r.EHRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if (f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        s.r.EHRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  TODO: check not in stab (do we need this step?)
                    libtysila5.layout.Layout.OutputEHdr(ne, Jit.t, s.bf, s);

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }

                while (!s.r.MethodRequestor.Empty)
                {
                    var ne = s.r.MethodRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if (f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        s.r.MethodRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  Check not in stab
                    if (stab.GetAddress(ne.ms.MangleMethod()) == 0)
                        throw new NotImplementedException("JIT stub assembler for " + ne.ms.MangleMethod());

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }

                while (!s.r.StaticFieldRequestor.Empty)
                {
                    var ne = s.r.StaticFieldRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if (f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        s.r.StaticFieldRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  Check not in stab
                    if (stab.GetAddress(ne.MangleType() + "S") == 0)
                        libtysila5.layout.Layout.OutputStaticFields(ne, Jit.t, s.bf);

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }

                while (!s.r.VTableRequestor.Empty)
                {
                    var ne = s.r.VTableRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if (f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        s.r.VTableRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  Check not in stab
                    if (stab.GetAddress(ne.MangleType()) == 0)
                        libtysila5.layout.Layout.OutputVTable(ne, Jit.t, s.bf, s);

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }

                while(!((JitRequestor)s.r).FullMethodRequestor.Empty)
                {
                    var ne = ((JitRequestor)s.r).FullMethodRequestor.GetNext();

                    // Try and set flags to INPROG
                    var f = System.Threading.Interlocked.CompareExchange(ref ne.JitFlags, JF_INPROG, JF_NOTDONE);
                    if (f == JF_DONE)
                        continue;       // Already done
                    if (f == JF_INPROG)
                    {
                        // return, and skip any more processing of this group (as may just pick this one back up again)
                        ((JitRequestor)s.r).FullMethodRequestor.Request(ne);
                        break;
                    }

                    // f was NOTDONE, and has been set to INPROG.  Check not in stab
                    if(stab.GetAddress(ne.ms.MangleMethod()) == 0)
                        libtysila5.libtysila.AssembleMethod(ne.ms, s.bf, Jit.t, s);

                    // set to DONE
                    ne.JitFlags = JF_DONE;
                }
            }
        }
    }
}
