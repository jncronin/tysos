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
using tysos;
using tysos.lib;
using tysos.Resources;

namespace disk
{
    partial class disk : tysos.lib.VirtualDirectoryServer
    {
        internal disk(tysos.lib.File.Property[] Properties)
        {
            root = new List<tysos.lib.File.Property>(Properties);
        }

        vfs.BlockDevice bdev;

        public override bool InitServer()
        {
            System.Diagnostics.Debugger.Log(0, "disk", "Disk driver started");

            /* Get our ports and interrupt */
            foreach (var r in root)
            {
                if (r.Name == "blockdev" && (r.Value is vfs.BlockDevice))
                {
                    bdev = r.Value as vfs.BlockDevice;
                }
            }

            if (bdev == null)
            {
                System.Diagnostics.Debugger.Log(0, "disk", "No blockdev property found");
                return false;
            }

            /* Get the first sector of the device */
            byte[] sect_1 = new byte[bdev.SectorSize];
            tysos.lib.MonoIOError err;
            long read = bdev.Read(0, 1, sect_1, 0, out err);
            if (err != MonoIOError.ERROR_SUCCESS)
            {
                System.Diagnostics.Debugger.Log(0, "disk", "Attempt to read first sector failed: bytes_read: " + read.ToString() + ", error: " + err.ToString());
                return false;
            }

            StringBuilder sb = null;
            for(int i = 0; i < bdev.SectorSize; i++)
            {
                if(i % 8 == 0)
                {
                    if (sb != null)
                        System.Diagnostics.Debugger.Log(0, "disk", sb.ToString());
                    sb = new StringBuilder();
                    sb.Append(i.ToString() + ": ");
                }

                sb.Append(sect_1[i].ToString("X2"));
                sb.Append(" ");
            }

            if (sb != null)
                System.Diagnostics.Debugger.Log(0, "disk", sb.ToString());

            return true;
        }
    }
}
