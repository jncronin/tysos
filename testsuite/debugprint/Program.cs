using System;

namespace debugprint
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Log(0, "debugprint", "Waiting for logger to be available...");
            tysos.ServerObject logger = null;

            while (logger == null)
                logger = tysos.Syscalls.ProcessFunctions.GetSpecialProcess(tysos.Syscalls.ProcessFunctions.SpecialProcessType.Logger);

            System.Diagnostics.Debugger.Log(0, "debugprint", " Logger available\n");

            while (true)
            {
                logger.Invoke("LogMessage", new object[] { "debugprint", 0, "Remote log message\n" });
            }
        }
    }
}
