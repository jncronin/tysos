using System;

namespace debugprint
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Log(0, "debugprint", "Waiting for logger to be available...");
            tysos.Interfaces.ILogger logger = null;

            while (logger == null)
                logger = tysos.Syscalls.ProcessFunctions.GetLogger();

            System.Diagnostics.Debugger.Log(0, "debugprint", " Logger available\n");

            while (true)
            {
                logger.LogMessage("debugprint", 0, "Remote log message\n");
            }
        }
    }
}
