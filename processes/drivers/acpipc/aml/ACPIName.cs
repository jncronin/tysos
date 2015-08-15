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

namespace acpipc.Aml
{
    class ACPIName
    {
        List<string> name = new List<string>();
        bool isnull = true;

        public static ACPIName Clone(ACPIName other)
        {
            ACPIName n = new ACPIName();
            foreach (string part in other.name)
                n.name.Add(part);
            n.isnull = other.isnull;
            return n;
        }

        public ACPIName Clone()
        {
            return ACPIName.Clone(this);
        }

        public void CloneFrom(ACPIName other)
        {
            name.Clear();
            foreach (string nameseg in other.name)
                name.Add(nameseg);
            isnull = other.isnull;
        }

        public void Null()
        {
            isnull = true;
            name.Clear();
        }

        public bool IsNull { get { return isnull; } }

        public void Prefix()
        {
            name.RemoveAt(name.Count - 1);
            isnull = false;
        }

        public void Root()
        {
            name.Clear();
            isnull = false;
        }

        public static ACPIName RootName { get { ACPIName n = new ACPIName(); n.Root(); return n; } }

        public void NameSeg(string nameseg)
        {
            name.Add(nameseg);
            isnull = false;
        }

        public string NameElement(int idx)
        {
            return name[idx];
        }

        public override string ToString()
        {
            if (isnull)
                return "";

            if (name.Count == 0)
                return "\\";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Count; i++)
            {
                if (i == 0)
                    sb.Append("\\");
                else
                    sb.Append(".");
                sb.Append(name[i]);
            }

            return sb.ToString();
        }

        public static ACPIName FromString(string s)
        {
            ACPIName n;

            if ((s == null) || (s == ""))
            {
                n = new ACPIName();
                n.Null();
                return n;
            }

            if (s[0] != '\\')
                throw new Exception("Invalid ACPIName");

            n = new ACPIName();
            n.Root();
            s = s.Substring(1);
            string[] subs = s.Split('.');
            foreach (string sub in subs)
                n.NameSeg(sub);

            return n;
        }

        public static implicit operator ACPIName(string s)
        {
            return FromString(s);
        }

        public static implicit operator string(ACPIName n)
        {
            return n.ToString();
        }

        public ACPIName ScopeUp()
        {
            if (name.Count <= 1)
                return null;

            ACPIName n = this.Clone();
            n.name.RemoveAt(n.name.Count - 2);
            return n;
        }
    }
}
