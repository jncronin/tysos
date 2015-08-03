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
    public interface IFile
    {
        long Length { get; }
        IInputStream GetInputStream();
        IOutputStream GetOutputStream();
        ICollection<tysos.lib.File.Property> Properties { get; }
        tysos.lib.File.Property GetPropertyByName(string name);
        int IntProperties { get; }
        tysos.lib.MonoIOError Error { get; }
    }

    public interface IInputStream
    {
        int Read(byte[] dest, int dest_offset, int count);
        int DataAvailable(int timeout);
        long Length { get; }
        long Position { get; }
        void Seek(long position, SeekPosition whence);
    }

    public interface IOutputStream
    {
        void Write(byte[] src, int src_offset, int count);
        long Length { get; }
        long Position { get; }
        void Seek(long position, SeekPosition whence);
    }

    public enum SeekPosition { Set, Cur, End }

    public interface IDirectory
    {

    }
}
