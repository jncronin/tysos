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

// Interface with the metadata module to interpret metadata embedded in modules

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace libsupcs
{
    unsafe class Metadata
    {
        internal static Dictionary<string, metadata.MetadataStream> name_cache =
            new Dictionary<string, metadata.MetadataStream>(
                new GenericEqualityComparer<string>());
        internal static Dictionary<ulong, metadata.MetadataStream> ptr_cache =
            new Dictionary<ulong, metadata.MetadataStream>(
                new GenericEqualityComparer<ulong>());
        static metadata.MetadataStream mscorlib = null;
        static BinaryAssemblyLoader bal = null;

        public static metadata.MetadataStream MSCorlib
        {
            get
            {
                if (mscorlib == null)
                    load_mscorlib();
                return mscorlib;
            }
        }

        public static metadata.AssemblyLoader AssemblyLoader
        {
            get
            {
                if (bal == null)
                    bal = new BinaryAssemblyLoader();
                return bal;
            }
        }

        private static void load_mscorlib()
        {
            var str = AssemblyLoader.LoadAssembly("mscorlib");
            metadata.PEFile pef = new metadata.PEFile();
            var m = pef.Parse(new metadata.StreamInterface(str), AssemblyLoader);

            name_cache["mscorlib"] = m;
            ptr_cache[(ulong)OtherOperations.GetStaticObjectAddress("mscorlib")] = m;
        }

        class BinaryAssemblyLoader : metadata.AssemblyLoader
        {
            public override Stream LoadAssembly(string name)
            {
                void* ptr;
                void* end;
                if(name == "mscorlib" || name == "mscorlib.dll")
                {
                    ptr = OtherOperations.GetStaticObjectAddress("mscorlib");
                    end = OtherOperations.GetStaticObjectAddress("mscorlib_end");
                }
                else
                    throw new NotImplementedException();

                long len = (byte*)end - (byte*)ptr;

                return new BinaryStream((byte*)ptr, len);
            }
        }

        class BinaryStream : System.IO.Stream
        {
            byte* d;
            bool canwrite;
            long pos;
            long len;

            public BinaryStream(byte *data, long length)
            {
                d = data;
                len = Length;
                canwrite = true;
                pos = 0;
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return true;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return canwrite;
                }
            }

            public override long Length
            {
                get
                {
                    return len;
                }
            }

            public override long Position
            {
                get
                {
                    return pos;
                }

                set
                {
                    pos = value;
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                for(int i = 0; i < count; i++)
                {
                    buffer[offset + i] = *(d + pos++);
                }
                return count;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                switch(origin)
                {
                    case SeekOrigin.Begin:
                        pos = offset;
                        break;
                    case SeekOrigin.Current:
                        pos += offset;
                        break;
                    case SeekOrigin.End:
                        pos = len - offset;
                        break;
                }
                return pos;
            }

            public override void SetLength(long value)
            {
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                for(int i = 0; i < count; i++)
                {
                    *(d + pos++) = buffer[offset + i];
                }
            }
        }
    }
}
