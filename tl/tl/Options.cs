/* Copyright (C) 2013 by John Cronin
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
using System.Linq;
using System.Text;
using System.Globalization;

namespace tl
{
    class Options
    {
        public class OptionValue
        {
            public string String;
            public uint UInt;
            public int Int;
            public bool Bool;
            public ulong ULong;
            public long Long;
            public bool Set;

            public static OptionValue InterpretString(string s)
            {
                OptionValue r = new OptionValue();

                r.String = s;
                string s2 = s;
                System.Globalization.NumberStyles ns = System.Globalization.NumberStyles.Any;
                if(s.StartsWith("0x"))
                {
                    s2 = s.Substring(2);
                    ns = System.Globalization.NumberStyles.HexNumber;
                }
                uint.TryParse(s2, ns, CultureInfo.InvariantCulture, out r.UInt);
                int.TryParse(s2, ns, CultureInfo.InvariantCulture, out r.Int);
                bool.TryParse(s2, out r.Bool);
                ulong.TryParse(s2, ns, CultureInfo.InvariantCulture, out r.ULong);
                long.TryParse(s2, ns, CultureInfo.InvariantCulture, out r.Long);
                r.Set = true;

                return r;
            }

            public override string ToString()
            {
                if (Set)
                    return String;
                else
                    return "Not Set";
            }
        }

        Dictionary<string, OptionValue> Option = new Dictionary<string, OptionValue>();
        public OptionValue this[string s]
        {
            get
            {
                OptionValue r;
                if (Option.TryGetValue(s, out r))
                    return r;
                else r = new OptionValue { Set = false };
                Option[s] = r;
                return r;
            }
            set
            {
                Option[s] = value;
            }
        }
    }
}
