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
using vfs;

namespace ata
{
    public class Drive : vfs.BlockDevice
    {
        ata.DeviceInfo d;
        ata a;

        internal Drive(ata.DeviceInfo devinfo, ata atadev)
        {
            d = devinfo;
            a = atadev;
        }

        public override long SectorCount
        { get { return (long)d.SectorCount; } }

        public override long SectorSize
        { get { return d.SectorSize; } }

        public override BlockEvent ReadAsync(long sector_idx, long sector_count, byte[] buf, int buf_offset)
        {
            System.Diagnostics.Debugger.Log(0, "ata", "Drive: ReadAsync(" + sector_idx.ToString() + ", " + sector_count.ToString() + ", , )");
            ata.Cmd c = new ata.Cmd();
            c.is_write = false;
            c.sector_idx = (ulong)sector_idx;
            c.sector_count = (ulong)sector_count;
            c.cur_sector = c.sector_idx;
            c.buf = buf;
            c.buf_offset = buf_offset;
            c.ev = new BlockEvent();
            c.d = d;

            lock(a.cmds)
            {
                a.cmds.Add(c);
            }

            System.Diagnostics.Debugger.Log(0, "ata", "Drive: ReadAsync returning");

            return c.ev;
        }

        public override BlockEvent WriteAsync(long sector_idx, long sector_count, byte[] buf, int buf_offset)
        {
            ata.Cmd c = new ata.Cmd();
            c.is_write = false;
            c.sector_idx = (ulong)sector_idx;
            c.sector_count = (ulong)sector_count;
            c.cur_sector = c.sector_idx;
            c.buf = buf;
            c.buf_offset = buf_offset;
            c.ev = new BlockEvent();
            c.d = d;

            lock (a.cmds)
            {
                a.cmds.Add(c);
            }

            return c.ev;
        }
    }
}
