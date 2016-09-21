/* Copyright (C) 2016 by John Cronin
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

namespace acpipc.Aml
{
    internal abstract class BuiltinMethod
    {
        internal IMachineInterface _mi;
        internal Namespace _n;
        internal BuiltinMethod(IMachineInterface mi, Namespace n)
        {
            _mi = mi;
            _n = n;
        }

        internal abstract ACPIObject Execute(Namespace.State state);
    }

    internal class OSMethod : BuiltinMethod
    {
        public OSMethod(IMachineInterface mi, Namespace n) : base(mi, n) { }

        string osval = "Windows 2015";
        internal override ACPIObject Execute(Namespace.State state)
        {
            System.Diagnostics.Debugger.Log(0, "acpipc", "_OS called, returning " + osval);
            return new ACPIObject(ACPIObject.DataType.String, osval);
        }
    }

    internal class OSIMethod : BuiltinMethod
    {
        public OSIMethod(IMachineInterface mi, Namespace n) : base(mi, n) { }

        internal override ACPIObject Execute(Namespace.State state)
        {
            System.Diagnostics.Debugger.Log(0, "acpipc", "_OSI(" + state.Args[0].ToString() + ")");
            ACPIObject osval = state.Args[0].EvaluateTo(ACPIObject.DataType.String, _mi, state, _n);
            string osvalstr = osval.Data as string;

            if(osvalstr == null)
            {
                System.Diagnostics.Debugger.Log(0, "acpipc", "_OSI(null) called, returning 0");
                return Namespace.Zero;
            }
            ulong ret = Namespace.Ones;

            System.Diagnostics.Debugger.Log(0, "acpipc", "_OSI(" + osvalstr + ") called, returning " + ret.ToString());
            return ret;
        }
    }
}
