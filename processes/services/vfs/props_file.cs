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

namespace vfs
{
    class props_file : fixed_text_file
    {
        VersionedList<tysos.StructuredStartupParameters.Param> props = 
            new VersionedList<tysos.StructuredStartupParameters.Param>();

        string cached_str = "";

        internal props_file(string Name, tysos.StructuredStartupParameters Props,
            DirectoryFileSystemObject Parent) : base(Name, Parent)
        {
            foreach (tysos.StructuredStartupParameters.Param p in Props.Parameters)
                props.Add(p);
        }

        protected internal override string contents
        {
            get
            {
                if(props.Changed)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (tysos.StructuredStartupParameters.Param p in props)
                    {
                        sb.Append(p.Name);
                        sb.Append(": ");
                        sb.Append(p.Value.ToString());
                        sb.Append("\n");
                    }
                    cached_str = sb.ToString();
                }
                return cached_str;
            }
        }
    }
}
