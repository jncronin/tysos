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

namespace tysos.lib
{
    public class ErrorFile : tysos.lib.File
    {
        public ErrorFile(tysos.lib.MonoIOError error)
        {
            err = error;

            CanGrow = CanRead = CanWrite = CanSeek = false;
            d = null;
            pos = 0;
            isatty = false;
        }

        public override int Read(byte[] dest, int dest_offset, int count)
        {
            return -1;
        }

        public override bool DataAvailable(int timeout)
        {
            return false;
        }

        public override long Length
        {
            get
            {
                return 0;
            }
        }

        public override int Write(byte[] dest, int dest_offset, int count)
        {
            return -1;
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                throw new System.IO.EndOfStreamException();
            }
        }

        public override tysos.StructuredStartupParameters.Param GetPropertyByName(string name)
        {
            return null;
        }

        public override string Name
        {
            get
            {
                return "ERROR";
            }
        }
    }
}
