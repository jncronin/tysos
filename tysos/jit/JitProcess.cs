using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.jit
{
    class JitProcess
    {
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


                throw new NotImplementedException();
            }
        }
    }
}
