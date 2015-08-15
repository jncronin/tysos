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
        bool collection_in_progress = false;
        bool alloc_in_progress = false;
        object collection_mutex = new object();

        [libsupcs.Uninterruptible]
        public void DoCollection()
        {
            /* We only want one collection to run at once.  If we just used a lock{} section,
             * then all threads that request a collection whilst one is already running would
             * have to wait for the current collection to finish, then run one themselves
             * immediately afterwards.  This is wasteful as they can just rely on the already
             * running collection to be good enough.
             * 
             * Thus, we protect the entry to the function so only one thread can check if a
             * collection is running and immediately start one if it isn't.  This is a short
             * code block so other threads won't have to wait long, they will then see the
             * collection_in_progress flag is true and exit.
             * 
             * The flag is reset at function exit atomically so no need for to reacquire the
             * mutex.
             */

            bool can_continue = false;

            while (can_continue == false)
            {
                lock (collection_mutex)
                {
                    if (collection_in_progress)
                    {
                        Formatter.WriteLine("gengc: attempt to run collection whilst already in progress",
                            Program.arch.DebugOutput);
                        return;
                    }

                    if (alloc_in_progress == false)
                    {
                        collection_in_progress = true;
                        can_continue = true;
                    }
                }
            }

            /* Run a collection.  Process is:
             * 
             * 1)       Whiten all objects (all chunks/small objects that are not root blocks)
             *              (this is done implicitly on assignment and on freeing)
             * 2)       Grey those listed in a root block
             * 3a)      Set blackened_count to 0
             * 3b)      Iterate though all blocks.  If a grey is encountered:
             * 3b.1)        Iterate through the object on native int boundaries
             * 3b.2)        Grey all white objects it points to
             * 3b.3)        Blacken the object
             * 3b.4)        blackened_count++
             * 3c)      If blackened_count > 0 loop to 3a
             * 4)       Iterate through again, reclaiming white blocks to be free, and
             *              whitening black blocks (there should be no grey blocks now)
             */

#if GENGC_BASICDEBUG
            Formatter.WriteLine("gengc: Starting collection", Program.arch.DebugOutput);
#endif

/* grey root blocks */
#if GENGC_BASICDEBUG
            Formatter.WriteLine("gengc: greying roots... ", Program.arch.DebugOutput);
#endif
            root_header* cur_root_hdr = hdr->roots;
            while(cur_root_hdr != null)
            {
                for(int i = 0; i < cur_root_hdr->size; i++)
                {
                    byte* root_start = *(byte**)((byte*)cur_root_hdr + sizeof(root_header) +
                        i * 2 * sizeof(byte*));
                    byte* root_end = *(byte**)((byte*)cur_root_hdr + sizeof(root_header) +
                        (i * 2 + 1) * sizeof(byte*));

                    grey_object(root_start, root_end);

#if GENGC_DEBUG
                    Formatter.Write((ulong)root_start, "X", Program.arch.DebugOutput);
                    Formatter.Write(" - ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)root_end, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                }
                cur_root_hdr = cur_root_hdr->next;
            }
#if GENGC_BASICDEBUG
            Formatter.WriteLine("done", Program.arch.DebugOutput);
#endif

/* blacken reachable blocks */
#if GENGC_BASICDEBUG
            Formatter.Write("gengc: blackening reachable blocks... ", Program.arch.DebugOutput);
#endif
            int blackened_count;
            int total_blackened = 0;
            int small_blackened = 0;
            int large_blackened = 0;
            int loops = 0;
            int block_count = 0;        // debugging count to ensure we traverse the whole tree
            chunk_header* chk;

            do
            {
                blackened_count = 0;
                block_count = 0;
                /* Iterate all blocks */

                // Get the first block
                chk = hdr->root_used_chunk;
                while (chk->left != hdr->nil)
                    chk = chk->left;
                
                while(chk != hdr->nil)
                {
                    block_count++;

                    if((chk->flags & 0x30) == 0x10)
                    {
                        /* large object - is it grey? */
                        if((chk->flags & 0x3) == 0x3)
                        {
                            byte* obj_start = (byte*)chk + sizeof(chunk_header);
                            byte* obj_end = obj_start + (int)chk->length;

                            /* grey all it points to */
                            grey_object(obj_start, obj_end);

                            /* blacken the object */
                            chk->flags &= ~0x1;
                            blackened_count++;
                            large_blackened++;

#if GENGC_DEBUG
                            Formatter.Write("gengc: INFO: greying large object: ", Program.arch.DebugOutput);
                            Formatter.Write((ulong)obj_start, "X", Program.arch.DebugOutput);
                            Formatter.Write(" - ", Program.arch.DebugOutput);
                            Formatter.Write((ulong)obj_end, "X", Program.arch.DebugOutput);
                            Formatter.WriteLine(Program.arch.DebugOutput);
#endif
                        }
                    }
                    else if ((chk->flags & 0x30) == 0x0)
                    {
                        /* small object array, iterate through each */
                        sma_header* smhdr = (sma_header*)((byte*)chk + sizeof(chunk_header));

                        for(int i = 0; i < smhdr->total_count; i += 8)
                        {
                            uint* uint_ptr = (uint*)((byte*)smhdr + sizeof(sma_header) +
                                i / 2);

                            for (int bit_idx = 0; bit_idx < 8; bit_idx++)
                            {
                                uint flag_pattern = 0x3U << (bit_idx * 4);
                                uint grey_pattern = 0x3U << (bit_idx * 4);
                                uint black_pattern = 0x2U << (bit_idx * 4);

                                if ((*uint_ptr & flag_pattern) == grey_pattern)
                                {
                                    byte* data_start = (byte*)smhdr + sizeof(sma_header) +
                                        smhdr->total_count * 4;
                                    byte* obj_start = data_start + (i + bit_idx) *
                                        smhdr->obj_length;
                                    byte* obj_end = obj_start + smhdr->obj_length;

                                    /* grey all it points to */
                                    grey_object(obj_start, obj_end);

                                    /* blacken the object */
                                    *uint_ptr &= ~flag_pattern;
                                    *uint_ptr |= black_pattern;
                                    blackened_count++;
                                    small_blackened++;
                                }
                            }
                        }
                    }
                    
                    // Loop to the next chunk
                    chk = TreeSuccessor(hdr, 1, chk);
                }

                total_blackened += blackened_count;
                loops++;

#if GENGC_DEBUG
                Formatter.Write("gengc: loop visited ", Program.arch.DebugOutput);
                Formatter.Write((ulong)block_count, Program.arch.DebugOutput);
                Formatter.WriteLine(" blocks", Program.arch.DebugOutput);
#endif
            } while (blackened_count > 0);
#if GENGC_BASICDEBUG
            Formatter.WriteLine("done", Program.arch.DebugOutput);
#endif

/* Iterate through again, setting white to free and black to white */
#if GENGC_BASICDEBUG
            Formatter.Write("gengc: freeing white blocks... ", Program.arch.DebugOutput);
#endif
            // Get the first block
            chk = hdr->root_used_chunk;
            while (chk->left != hdr->nil)
                chk = chk->left;

            int white_large_objects = 0;
            int white_small_objects = 0;
            int black_large_objects = 0;
            int black_small_objects = 0;
            block_count = 0;

            while (chk != hdr->nil)
            {
                block_count++;
                if ((chk->flags & 0x30) == 0x10)
                {
                    /* large object - is it white? */
                    if ((chk->flags & 0x3) == 0x3)
                    {
                        // TODO: delete large object
                        // Can we do a tree node delete whilst traversal is in progress?
                        white_large_objects++;
                    }
                    else if((chk->flags & 0x3) == 0x2)
                    {
                        // its black - set back to white
                        chk->flags &= ~0x3;
                        chk->flags |= 0x1;
                        black_large_objects++;
                    }
                }

                else if ((chk->flags & 0x30) == 0x0)
                {
                    /* small object array, iterate through each */
                    sma_header* smhdr = (sma_header*)((byte*)chk + sizeof(chunk_header));

                    for (int i = 0; i < smhdr->total_count; i += 8)
                    {
                        uint* uint_ptr = (uint*)((byte*)smhdr + sizeof(sma_header) +
                            i / 2);

                        for (int bit_idx = 0; bit_idx < 8; bit_idx++)
                        {
                            uint flag_pattern = 0x3U << (bit_idx * 4);
                            uint white_pattern = 0x1U << (bit_idx * 4);
                            uint black_pattern = 0x2U << (bit_idx * 4);

                            /* debugging - check we are not freeing a process/thread etc */
                            byte* data_start = (byte*)smhdr + sizeof(sma_header) +
                                smhdr->total_count * 4;
                            byte* obj_start = data_start + (i + bit_idx) *
                                smhdr->obj_length;

                            if ((*uint_ptr & flag_pattern) == white_pattern)
                            {
                                /* free the object */
                                *uint_ptr &= ~flag_pattern;
                                smhdr->free_count++;
                                (*smhdr->global_free_count)++;
                                white_small_objects++;
                            }
                            else if((*uint_ptr & flag_pattern) == black_pattern)
                            {
                                /* whiten the object */
                                *uint_ptr &= ~flag_pattern;
                                *uint_ptr |= white_pattern;
                                black_small_objects++;
                            }
                            else if((*uint_ptr & flag_pattern) == flag_pattern)
                            {
                                Formatter.Write("gengc: WARNING: object at ", Program.arch.DebugOutput);
                                Formatter.Write((ulong)obj_start, "X", Program.arch.DebugOutput);
                                Formatter.Write(" is grey on final loop: ", Program.arch.DebugOutput);
                                Formatter.Write(*uint_ptr & flag_pattern, "X", Program.arch.DebugOutput);
                                Formatter.WriteLine(Program.arch.DebugOutput);
                            }
                        }
                    }
                }

                // Loop to the next chunk
                chk = TreeSuccessor(hdr, 1, chk);
            }
#if GENGC_BASICDEBUG
            Formatter.WriteLine("done", Program.arch.DebugOutput);
#endif

#if GENGC_DEBUG
            Formatter.Write("gengc: final loop visited ", Program.arch.DebugOutput);
            Formatter.Write((ulong)block_count, Program.arch.DebugOutput);
            Formatter.WriteLine(" blocks", Program.arch.DebugOutput);

            /* Sanity check to ensure we visited all nodes */
            Formatter.Write("gengc: total number of blocks: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)hdr->used_chunks, Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
#endif

#if GENGC_BASICDEBUG
            Formatter.Write("gengc: Collection completed: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)white_large_objects, Program.arch.DebugOutput);
            Formatter.Write(" large objects and ", Program.arch.DebugOutput);
            Formatter.Write((ulong)white_small_objects, Program.arch.DebugOutput);
            Formatter.WriteLine(" small objects freed", Program.arch.DebugOutput);
#endif

#if GENGC_DEBUG
            Formatter.Write("gengc: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)black_large_objects, Program.arch.DebugOutput);
            Formatter.Write(" large objects and ", Program.arch.DebugOutput);
            Formatter.Write((ulong)black_small_objects, Program.arch.DebugOutput);
            Formatter.WriteLine(" small objects not freed", Program.arch.DebugOutput);

            Formatter.Write("gengc: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)large_blackened, Program.arch.DebugOutput);
            Formatter.Write(" large objects and ", Program.arch.DebugOutput);
            Formatter.Write((ulong)small_blackened, Program.arch.DebugOutput);
            Formatter.WriteLine(" small objects were blackened", Program.arch.DebugOutput);
#endif

#if GENGC_BASICDEBUG
            Formatter.Write("gengc: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)total_blackened, Program.arch.DebugOutput);
            Formatter.WriteLine(" objects remain in use", Program.arch.DebugOutput);
            Formatter.Write("gengc: ", Program.arch.DebugOutput);
            Formatter.Write((ulong)loops, Program.arch.DebugOutput);
            Formatter.WriteLine(" loops required to blacken all in-use objects", Program.arch.DebugOutput);
#endif

            allocs = 0;
            collection_in_progress = false;
        }

        private unsafe void grey_object(byte* obj_start, byte* obj_end)
        {
#if GENGC_DEBUG
            Formatter.Write("gengc: greying object from ", Program.arch.DebugOutput);
            Formatter.Write((ulong)obj_start, "X", Program.arch.DebugOutput);
            Formatter.Write(" to ", Program.arch.DebugOutput);
            Formatter.Write((ulong)obj_end, "X", Program.arch.DebugOutput);
            Formatter.WriteLine(Program.arch.DebugOutput);
#endif

            byte** cur_ptr = (byte**)obj_start;
            while((byte*)cur_ptr + sizeof(void*) <= obj_end)
            {
                byte* obj = *cur_ptr;

#if GENGC_DEBUG
                Formatter.Write("gengc: grey_object: member at: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)cur_ptr, "X", Program.arch.DebugOutput);
                Formatter.Write(" is: ", Program.arch.DebugOutput);
                Formatter.Write((ulong)obj, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
#endif

                /* First, check its at all valid (i.e. points within the heap) */
                if(obj >= heap_start && obj < heap_end)
                {
#if GENGC_DEBUG
                    Formatter.Write("gengc: grey_object: object has member ", Program.arch.DebugOutput);
                    Formatter.Write((ulong)obj, "X", Program.arch.DebugOutput);
                    Formatter.WriteLine(" which is potentially within the heap", Program.arch.DebugOutput);
#endif

                    /* Now get the chunk that contains it in the used tree */
                    chunk_header* chk = search(hdr, 1, 1, obj);

                    if(chk != null)
                    {
                        byte* chk_start = (byte*)chk + sizeof(chunk_header);
                        byte* chk_end = chk_start + (int)chk->length; // cannot add byte* to byte*

                        if(obj >= chk_start && obj < chk_end)
                        {
                            /* obj points into this chunk - check if its a large object,
                             * small object or something else (e.g. root pointer) */
                            if((chk->flags & 0x30) == 0x10)
                            {
                                /* large object - we grey it if it is white */
                                if((chk->flags & 0x3) == 0x1)
                                {
                                    chk->flags |= 0x2;
                                }
                            }
                            else if((chk->flags & 0x30) == 0x0)
                            {
                                /* small object - we need to identify its index in the array */
                                sma_header* smhdr = (sma_header*)((byte*)chk + sizeof(chunk_header));
                                byte* data_start = (byte*)smhdr + sizeof(sma_header) +
                                    smhdr->total_count * 4;

                                if (data_start > obj)
                                {
#if GENGC_DEBUG
                                    Formatter.Write("gengc: grey_object: object reference points to within sma_header, ignoring (obj: ", Program.arch.DebugOutput);
                                    Formatter.Write((ulong)obj, "X", Program.arch.DebugOutput);
                                    Formatter.Write(", data_start: ", Program.arch.DebugOutput);
                                    Formatter.Write((ulong)data_start, "X", Program.arch.DebugOutput);
                                    Formatter.Write(", total_count: ", Program.arch.DebugOutput);
                                    Formatter.Write((ulong)smhdr->total_count, Program.arch.DebugOutput);
                                    Formatter.Write(", obj_length: ", Program.arch.DebugOutput);
                                    Formatter.Write((ulong)smhdr->obj_length, Program.arch.DebugOutput);
                                    Formatter.WriteLine(")", Program.arch.DebugOutput);
#endif
                                }
                                else
                                {
                                    int idx = (int)((obj - data_start) / smhdr->obj_length);
                                    int uint_idx = idx / 8;
                                    int bit_idx = idx % 8;

                                    uint* uint_ptr = (uint*)((byte*)smhdr + sizeof(sma_header) +
                                        uint_idx * 4);

                                    uint flag_pattern = 0x3U << (bit_idx * 4);
                                    uint white_pattern = 0x1U << (bit_idx * 4);
                                    uint grey_pattern = 0x2U << (bit_idx * 4);

                                    if ((*uint_ptr & flag_pattern) == white_pattern)
                                        *uint_ptr |= grey_pattern;
                                }
                            }
                        }
                        
                    }

                }

                //cur_ptr++;
                cur_ptr = (byte**)(((byte*)cur_ptr) + 4);
            }
        }
    }
}
