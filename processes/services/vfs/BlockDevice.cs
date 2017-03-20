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

namespace vfs
{
    public abstract class BlockDevice
    {
        public abstract long SectorSize { get; }
        public abstract long SectorCount { get; }
        public abstract BlockEvent ReadAsync(long sector_idx, long sector_count, byte[] buf, int buf_offset);
        public abstract BlockEvent WriteAsync(long sector_idx, long sector_count, byte[] buf, int buf_offset);

        public virtual long Read(long sector_idx, long sector_count, byte[] buf, int buf_offset, out tysos.lib.MonoIOError err)
        {
            BlockEvent ev = ReadAsync(sector_idx, sector_count, buf, buf_offset);

            while (ev.IsSet == false)
                tysos.Syscalls.SchedulerFunctions.Block(ev);

            err = ev.Error;
            return ev.SectorsTransferred;
        }

        public virtual long Write(long sector_idx, long sector_count, byte[] buf, int buf_offset, out tysos.lib.MonoIOError err)
        {
            BlockEvent ev = WriteAsync(sector_idx, sector_count, buf, buf_offset);

            while (ev.IsSet == false)
                tysos.Syscalls.SchedulerFunctions.Block(ev);

            err = ev.Error;
            return ev.SectorsTransferred;
        }
    }

    public class BlockEvent : tysos.Event
    {
        public tysos.lib.MonoIOError Error;
        public long SectorsTransferred;
    }
}
