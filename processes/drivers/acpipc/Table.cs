/* Copyright (C) 2015 by John Cronin
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
using tysos.Messages;

namespace acpipc
{
    class Table
    {
        string signature;
        tysos.VirtualMemoryResource64 vmem;

        internal static Table InterpretTable(tysos.VirtualMemoryResource64 table)
        {
            Table ret;
            char[] sig = new char[4];

            sig[0] = (char)table.Read(table.Addr64, 1);
            sig[1] = (char)table.Read(table.Addr64 + 1, 1);
            sig[2] = (char)table.Read(table.Addr64 + 2, 1);
            sig[3] = (char)table.Read(table.Addr64 + 3, 1);

            string sig_string = new string(sig);
            System.Diagnostics.Debugger.Log(0, "acpipc", "Found table with signature " +
                sig_string);

            // Check checksum
            uint length = (uint)table.Read(table.Addr64 + 4, 4);
            byte csum = 0;
            unchecked
            {
                for(uint i = 0; i < length; i++)
                {
                    csum += (byte)table.Read(table.Addr64 + i, 1);
                }
            }

            System.Diagnostics.Debugger.Break();
            if (csum == 0)
                System.Diagnostics.Debugger.Log(0, "acpipc", "table checksum passed");
            else
                System.Diagnostics.Debugger.Log(0, "acpipc", "table checksum failed: " +
                    csum.ToString());

            if(sig_string == "TODO")
            {
                throw new NotImplementedException();
            }
            else
            {
                ret = new Table();
            }

            ret.signature = sig_string;
            ret.vmem = table;

            return ret;
        }
    }
}
