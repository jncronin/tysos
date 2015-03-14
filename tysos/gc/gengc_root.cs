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

namespace tysos.gc
{
    unsafe partial class gengc
    {
        void AddRoots(byte *start, byte *end)
        {
            if (hdr->roots == null || hdr->roots->size == hdr->roots->capacity)
                allocate_root_block();

            root_header* r = hdr->roots;
            byte** rptr = (byte**)((byte*)r + sizeof(root_header) +
                r->size * sizeof(byte*) * 2);
            *rptr++ = start;
            *rptr++ = end;

            r->size++;
        }

        private void allocate_root_block()
        {
            /* Default each root block to store 1024 root definitions
             * Each definition is a start and end pointer, thus size of block
             * is sizeof(root_header) + 2 * 1024 * sizeof(byte *)
             */

            chunk_header* chk = allocate_chunk(sizeof(root_header) + 2048 * sizeof(byte*));
            if(chk == null)
            {
                Formatter.WriteLine("gengc: add_root_block: allocate_chunk() returned null",
                    Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }
            chk->flags &= ~(1 << 4);        // not large block
            chk->flags |= 1 << 5;           // is root block

            root_header* r = (root_header*)((byte*)chk + sizeof(chunk_header));
            r->next = hdr->roots;
            r->capacity = 1024;
            r->size = 0;

            hdr->roots = r;
        }
    }
}
