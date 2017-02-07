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

namespace metadata
{
    /**<summary>Represents a generic way of specifying all methods
    (whether generic or not)</summary>*/
    public class MethodSpec : Spec, IEquatable<MethodSpec>
    {
        public metadata.MetadataStream m;
        public int mdrow;
        public int msig;

        public TypeSpec[] gmparams;
        public TypeSpec type;

        public List<string> aliases;

        public string mangle_override = null;

        public override MetadataStream Metadata
        { get { return m; } }

        public TypeSpec[] gtparams
        {
            get
            {
                if (type == null)
                    return null;
                return type.gtparams;
            }
        }

        public bool Equals(MethodSpec other)
        {
            if (!m.Equals(other.m))
                return false;
            if (!type.Equals(other.type))
                return false;
            if (mdrow != other.mdrow)
                return false;
            if (msig != other.msig)
                return false;

            if (gmparams == null && other.gmparams == null)
                return true;
            if (gmparams == null)
                return false;
            if (other.gmparams == null)
                return false;
            if (gmparams.Length != other.gmparams.Length)
                return false;
            for (int i = 0; i < gmparams.Length; i++)
            {
                if (!gmparams[i].Equals(other.gmparams[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hc = m.GetHashCode();
            hc <<= 1;
            hc ^= mdrow.GetHashCode();
            hc <<= 1;
            hc ^= msig.GetHashCode();

            if (type != null)
            {
                hc <<= 1;
                hc ^= type.GetHashCode();
            }
            if (gmparams != null)
            {
                hc ^= gmparams.Length.GetHashCode();
                foreach (var gmparam in gmparams)
                {
                    hc <<= 1;
                    hc ^= gmparam.GetHashCode();
                }
            }

            return hc;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MethodSpec);
        }

        public bool IsGeneric
        {
            get
            {
                return m.gmparams[mdrow] != 0;
            }
        }

        public bool IsGenericTemplate
        {
            get
            {
                if (!IsGeneric)
                    return false;
                return gmparams == null;
            }
        }

        public override string ToString()
        {
            if (m == null)
                return "MethodSpec";
            return m.MangleMethod(this);
        }
    }
}
