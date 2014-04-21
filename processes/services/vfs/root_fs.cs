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

namespace vfs
{
    class root_fs : DirectoryFileSystemObject
    {
        FileSystemObject dev_node;
        FileSystemObject sys_node;
        FileSystemObject proc_node;
        DirectoryFileSystemObject users_node;
        DirectoryFileSystemObject progs_node;
        DirectoryFileSystemObject storage_node;

        public root_fs() : base("", null)
        {
            dev_node = new dev_node(this);
            sys_node = new sys_node(this);
            proc_node = new proc_node(this);
            users_node = new DirectoryFileSystemObject("users", this);
            progs_node = new DirectoryFileSystemObject("programs", this);
            storage_node = new DirectoryFileSystemObject("storage", this);

            children.Add(dev_node);
            children.Add(sys_node);
            children.Add(proc_node);
            children.Add(users_node);
            children.Add(progs_node);
            children.Add(storage_node);
        }
    }
}
