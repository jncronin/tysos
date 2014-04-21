/* Copyright (C) 2011 by John Cronin
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

namespace tydb
{
    class regs
    {
        internal static void dump_regs()
        {
            // ensure we have all the requested registers and display them
            foreach (dbgarch.register r in Program.arch.registers)
            {
                if (!await.s.regs.ContainsKey(r.id) || !await.s.regs[r.id].HasValue)
                    await.s.regs[r.id] = get_reg(r.id);

                string s = "Unavailable";
                if(await.s.regs[r.id].HasValue)
                {
                    if(r.length == 8)
                        s = await.s.regs[r.id].Value.ToString("X16");
                    else if(r.length == 4)
                        s = await.s.regs[r.id].Value.ToString("X8");
                    else
                        throw new Exception("Unsupported length value");
                }
                
                Console.WriteLine("{0,-8} {1}", r.name, s);
            }
        }

        private static ulong? get_reg(int p)
        {
            throw new NotImplementedException();
        }
    }
}
