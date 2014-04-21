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

namespace tysos
{
    class Environment
    {
        public Dictionary<string, string> env_vars;

        public Environment()
        {
            env_vars = new Dictionary<string,string>(new Program.MyGenericEqualityComparer<string>());
        }

        [libsupcs.MethodAlias("_ZW6System11EnvironmentM_0_11get_NewLine_Ru1S_P0")]
        [libsupcs.AlwaysCompile]
        static string Environment_GetNewLine()
        { return "\n"; }

        [libsupcs.MethodAlias("_ZW6System11EnvironmentM_0_30internalGetEnvironmentVariable_Ru1S_P1u1S")]
        [libsupcs.AlwaysCompile]
        static string GetEnvVar(string var_name)
        {
            if (Program.env.env_vars.ContainsKey(var_name))
                return Program.env.env_vars[var_name];
            return "";
        }

        [libsupcs.MethodAlias("_ZW6System11EnvironmentM_0_12get_Platform_RV10PlatformID_P0")]
        [libsupcs.AlwaysCompile]
        static int get_Platform()
        {
            return 128;
        }

        [libsupcs.MethodAlias("_ZW6System11EnvironmentM_0_18GetOSVersionString_Ru1S_P0")]
        [libsupcs.AlwaysCompile]
        static string GetOSVersionString()
        {
            return "0.2.0.0";
        }

        [libsupcs.MethodAlias("_ZW6System11EnvironmentM_0_15internalGetHome_Ru1S_P0")]
        [libsupcs.AlwaysCompile]
        static string internalGetHome()
        {
            return "/";
        }
    }
}
