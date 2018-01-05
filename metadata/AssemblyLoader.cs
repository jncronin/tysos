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
using System.IO;

namespace metadata
{
    public abstract class AssemblyLoader
    {
        protected Dictionary<string, MetadataStream> cache =
            new Dictionary<string, MetadataStream>(
                new GenericEqualityComparer<string>());

        public Dictionary<string, string> TypeForwarders =
            new Dictionary<string, string>(
                new GenericEqualityComparer<string>());

        bool require_version_match = true;

        /**<summary>Is the version required to match when loading an assembly?</summary>
         */
        public virtual bool RequireVersionMatch
        {
            get { return require_version_match; }
            set { require_version_match = value; }
        }

        /**<summary>Add an already loaded assembly to the cache</summary> */
        public virtual void AddToCache(MetadataStream m, string name)
        {
            cache[name] = m;
        }

        /**<summary>Load an assembly (even if it is already loaded).  See
        GetAssembly to avoid unnecessary loads</summary> */
        public abstract DataInterface LoadAssembly(string name);

        /**<summary>Get an assembly by name</summary>*/
        public virtual MetadataStream GetAssembly(string name)
        {
            MetadataStream ms;

            var simple_name = StripPathAndExtension(name);

            if (cache.TryGetValue(simple_name, out ms))
                return ms;

            var s = LoadAssembly(name);
            if (s == null)
                return null;
            PEFile p = new metadata.PEFile();
            ms = p.Parse(s, this);

            cache[simple_name] = ms;
            return ms;
        }

        /**<summary>Get an assembly by name and version</summary>*/
        public virtual MetadataStream GetAssembly(string name,
            int major, int minor, int build, int revision)
        {
            MetadataStream ms;

            var simple_name = StripPathAndExtension(name);

            if (cache.TryGetValue(simple_name, out ms))
            {
                if(ms.MajorVersion == major &&
                    ms.MinorVersion == minor &&
                    ms.BuildVersion == build &&
                    ms.RevisionVersion == revision ||
                    RequireVersionMatch == false)
                return ms;
            }

            var s = LoadAssembly(name);
            if (s == null)
                return null;
            PEFile p = new metadata.PEFile();
            ms = p.Parse(s, this);

            if (ms.MajorVersion == major &&
                    ms.MinorVersion == minor &&
                    ms.BuildVersion == build &&
                    ms.RevisionVersion == revision ||
                    RequireVersionMatch == false)
            {
                cache[simple_name] = ms;
                return ms;
            }
            return null;
        }

        protected virtual string StripPathAndExtension(string name)
        {
            return StripExtension(StripPath(name));
        }

        protected virtual string StripPath(string name)
        {
            var li = name.LastIndexOf('\\');
            var li2 = name.LastIndexOf('/');
            if (li2 > li)
                li = li2;

            if (li == -1)
                return name;
            return
                name.Substring(li + 1);
        }

        protected virtual string StripExtension(string name)
        {
            var li = name.LastIndexOf(".exe");
            if (li == -1)
                li = name.LastIndexOf(".dll");
            if (li == -1)
                return name;
            else
                return name.Substring(0, li);
        }
    }
}
