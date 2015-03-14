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

/* This is an experimental generational garbage collector for tysos
 * 
 * The basic unit is the 'chunk'.  This is an arbritrary length block of memory.
 * 
 * It can have two uses - firstly, it may be a 'large object' - this is an object
 * greater than the large object limit in size, and is an entire object following
 * the chunk header.
 * 
 * Secondly, it can be a container for an array of small objects.  Following the
 * chunk header is a small object array header which identifies the size and
 * number of small objects contained in the chunk, followed by a bitmap
 * containing 4 bits per small object denoting:
 *  bits 0-1:   00  - free
 *              01  - white (potential for deletion by GC)
 *              10  - black (reachable and all internal pointers point to grey objects)
 *              11  - grey (reachable but its internal pointers not scanned yet)
 *  bit 2:      0   - young generation
 *              1   - old generation
 *  bit 3:      reserved
 *  
 *  Then following the bitmap come the objects themselves aligned on a native int boundary
 *  
 * The chunks themselves are arranged in a b-tree sorted on address if in use, thus finding
 * the containing chunk based on address is a O(log N) operation, and sorted on size if not
 * 
 * The chunk headers contain:
 *  void *  parent
 *  void *  left child
 *  void *  right child
 *  void *  length
 *  int     flags
 *  int     lock_object
 *  
 * The first 4 bits of flags are as per the small object bitmap.  Bit 4 is a flag denoting
 * whether the chunk contains a small object array or a large object (0 for small object
 * array, 1 for large object).  Bit 5 denotes a root header chunk.
 * 
 * 
 */


using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.gc
{
    unsafe partial class gengc
    {
        /* tree_idx determines which tree is being used:
         * 
         * 0 - free tree, sorted by length
         * 1 - used tree, sorted by address
         */
            
        struct chunk_header
        {
            public chunk_header* parent;
            public chunk_header* left;
            public chunk_header* right;
            public int red;
            public byte* length;
            public int flags;
            public int lock_object;
        }

        struct sma_header
        {
            public int obj_length;
            public int total_count;
            public int free_count;
            public sma_header* next;
            public int* global_free_count;
        }

        struct root_header
        {
            public int size;
            public int capacity;
            public root_header* next;
        }

        struct heap_header
        {
            public chunk_header* root_used_chunk;
            public chunk_header* root_free_chunk;
            public chunk_header* nil;
            public root_header* roots;
            public int lo_size;

            /* Following this is a C-style array of sma_header * pointers, followed
             * by a C-style array of ints containing total free count of each size of
             * small object.  The total length of each array is sm_sizes.Length */
        }

        heap_header* hdr = null;
        void* heap_start = null;
        void* heap_end = null;

        internal static gengc heap = null;

        static int[] sm_sizes = new int[] { 8, 16, 24, 32, 48, 64, 80, 96, 128, 192, 256, 384, 512,
            1024, 2048 };

        /* sm_total_counts must be a multiple of 8 */
        static int[] sm_total_counts = new int[] { 512, 256, 160, 128, 80, 64, 48, 40, 32, 16, 8, 8, 8,
            8, 8 };
        
        public void Init(void *start, void *end)
        {
            /* Check we have enough space for our structures */
            if (((byte*)end - (byte*)start) < 0x100000)
                throw new Exception("Not enough heap space provided");

            /* Sanity check */
            if (sm_sizes.Length != sm_total_counts.Length)
                throw new Exception("sm_sizes and sm_total_counts are not of the same length");

            /* Allocate the header structure */
            hdr = (heap_header*)start;

            /* Start chunks 1 page in */
            byte* cur_ptr = (byte*)start + 0x1000;

            /* Allocate the nil chunk */
            chunk_header* nil = (chunk_header*)cur_ptr;
            cur_ptr += sizeof(chunk_header);
            nil->parent = nil;
            nil->left = nil;
            nil->right = nil;
            nil->red = 0;
            nil->flags = 0;
            nil->length = (byte*)0;
            hdr->nil = nil;

            /* Allocate the used tree root chunk */
            chunk_header* root_u = (chunk_header*)(cur_ptr);
            cur_ptr += sizeof(chunk_header);
            root_u->parent = nil;
            root_u->left = nil;
            root_u->right = nil;
            root_u->red = 0;
            root_u->length = (byte*)0;
            root_u->flags = 0;
            hdr->root_used_chunk = root_u;

            /* Allocate the free tree root chunk */
            chunk_header* root_f = (chunk_header*)(cur_ptr);
            cur_ptr += sizeof(chunk_header);
            root_f->parent = nil;
            root_f->left = nil;
            root_f->right = nil;
            root_f->red = 0;
            root_f->length = (byte*)0;
            root_f->flags = 0;
            hdr->root_free_chunk = root_f;

            /* Allocate the first chunk */
            chunk_header* c = (chunk_header*)(cur_ptr);
            c->length = (byte *)((byte*)end - (byte*)c - sizeof(chunk_header));
            c->flags = 1 << 4;      // large object
            c->lock_object = 0;

            /* Add the first chunk to the tree */
            RBTreeInsert(hdr, 0, c);


            hdr->lo_size = sm_sizes[sm_sizes.Length - 1];

            /* Allocate an array for the array of next free sm arrays */
            for(int i = 0; i < sm_sizes.Length; i++)
            {
                *get_sma_ptr(i) = null;
                *get_sm_free_ptr(i) = 0;
            }
        }

        public void *Alloc(int length)
        {
            /* Decide if we want a large or small object */
            if(length > hdr->lo_size)
            {
                chunk_header* c = allocate_chunk(length);
                if (c == null)
                    return null;
                return (void*)((byte*)c + sizeof(chunk_header));
            }
            else
            {
                /* Small object */
                int sm_index = get_size_index(length);
                if (sm_index < 0)
                    return null;

                /* Get first sma_header */
                sma_header** sma_ptr = get_sma_ptr(sm_index);
                int* sm_free_ptr = get_sm_free_ptr(sm_index);

                /* Ensure there is free space of the appropriate size */
                if(*sm_free_ptr == 0)
                    allocate_sma_header(sma_ptr, sm_index);

                /* Allocate space */
                return allocate_small_object(*sma_ptr, sm_index);
            }            
        }

        sma_header** get_sma_ptr(int sm_index)
        {
            return (sma_header**)((byte*)hdr + sizeof(heap_header) +
                sm_index * sizeof(sma_header**));
        }

        int *get_sm_free_ptr(int sm_index)
        {
            return (int*)((byte*)hdr + sizeof(heap_header) +
                sm_sizes.Length * sizeof(sma_header*) +
                sm_index * sizeof(int));
        }

        private unsafe void* allocate_small_object(sma_header* sma_header, int sm_index)
        {
            sma_header* cur_hdr = sma_header;

            while(cur_hdr != null)
            {
                if(cur_hdr->free_count == 0)
                {
                    cur_hdr = cur_hdr->next;
                    continue;
                }

                /* Iterate through the bitmap looking for a free space */
                for(int i = 0; i < cur_hdr->total_count; i += 8)
                {
                    /* Each 32-bit test uint covers 8 entries of 4 bits */
                    uint* test = (uint*)((byte*)cur_hdr + sizeof(sma_header) +
                        i * 4);

                    int free = -1;
                    /* Test each bitfield in turn */
                    if ((*test & 0x3) == 0)
                    {
                        free = 0;
                        *test |= 0x1;
                    }
                    else if ((*test & 0x30) == 0)
                    {
                        free = 1;
                        *test |= 0x10;
                    }
                    else if ((*test & 0x300) == 0)
                    {
                        free = 2;
                        *test |= 0x100;
                    }
                    else if ((*test & 0x3000) == 0)
                    {
                        free = 3;
                        *test |= 0x1000;
                    }
                    else if ((*test & 0x30000) == 0)
                    {
                        free = 4;
                        *test |= 0x10000;
                    }
                    else if ((*test & 0x300000) == 0)
                    {
                        free = 5;
                        *test |= 0x100000;
                    }
                    else if ((*test & 0x3000000) == 0)
                    {
                        free = 6;
                        *test |= 0x1000000;
                    }
                    else if ((*test & 0x30000000) == 0)
                    {
                        free = 7;
                        *test |= 0x10000000;
                    }

                    if(free != -1)
                    {
#if DEBUG_SM
                        Formatter.Write("free: ", Program.arch.DebugOutput);
                        Formatter.Write((ulong)free, Program.arch.DebugOutput);
                        Formatter.Write(", i: ", Program.arch.DebugOutput);
                        Formatter.Write((ulong)i, Program.arch.DebugOutput);
                        Formatter.Write(", obj_length: ", Program.arch.DebugOutput);
                        Formatter.Write((ulong)cur_hdr->obj_length, Program.arch.DebugOutput);
                        Formatter.WriteLine(Program.arch.DebugOutput);
#endif

                        /* Return index i + free */
                        sma_header->free_count -= 1;
                        *get_sm_free_ptr(sm_index) -= 1;
                        return (void*)((byte*)cur_hdr + sizeof(sma_header) +
                            cur_hdr->total_count * 4 +
                            (i + free) * cur_hdr->obj_length);
                    }
                }
            }
            return null;
        }

        private unsafe void* allocate_sma_header(sma_header** sma_ptr, int sm_index)
        {
            sma_header* old_entry = *sma_ptr;
            int obj_length = sm_sizes[sm_index];
            int obj_count = sm_total_counts[sm_index];

            chunk_header* c = allocate_chunk(sizeof(sma_header) + obj_count * 4 +
                obj_count * obj_length);
            if (c == null)
                return null;

            /* Set as small object */
            c->flags &= ~(1 << 4);

            /* Fill in the header */
            sma_header* h = (sma_header*)((byte*)c + sizeof(chunk_header));
            h->obj_length = obj_length;
            h->free_count = obj_count;
            h->total_count = obj_count;
            h->next = old_entry;
            h->global_free_count = get_sm_free_ptr(sm_index);

            /* Add our free blocks to the total free count */
            *get_sm_free_ptr(sm_index) += obj_count;

            /* Set all memory as free */
            for (int i = 0; i < obj_count; i += 8)
            {
                /* Each 32-bit test uint covers 8 entries of 4 bits */
                uint* test = (uint*)((byte*)h + sizeof(sma_header) +
                    i * 4);
                *test = 0;
            }

            // TODO: ?zero the returned memory block

            *sma_ptr = h;
            return h;
        }

        private unsafe chunk_header* allocate_chunk(int length)
        {
            /* Find the smallest chunk larger than or equal to length
             * 
             * Perform an insertion operation on a new node of length l,
             * - if during the insert we come across a chunk of the exact
             *      length, ues it
             * - else, then look for the in-order predecessor of the new node
             * (don't actually add the new node to the tree though)
             * Remove the identified chunk
             * 
             * Then, split the chunk if required, add the used bit to the used
             * tree and the remaining bit to the free tree */

            int act_length = util.align(length, 32);

            chunk_header* chk = search(hdr, 0, 0, (byte*)act_length);
            if(chk == null)
            {
                Formatter.WriteLine("gengc: allocate_chunk(): search() returned null",
                    Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }
            if(chk->length < (byte *)length)
            {
                Formatter.Write("gengc: allocate_chunk(): search() returned too small a chunk (",
                    Program.arch.DebugOutput);
                Formatter.Write((ulong)chk->length, Program.arch.DebugOutput);
                Formatter.Write(") when ", Program.arch.DebugOutput);
                Formatter.Write((ulong)length, Program.arch.DebugOutput);
                Formatter.WriteLine(" was requested", Program.arch.DebugOutput);
                libsupcs.OtherOperations.Halt();
            }

            /* Remove the chunk from the free tree */
            RBDelete(hdr, 0, chk);
            
            if(chk->length - act_length > (byte *)hdr->lo_size)
            {
                /* There is enough space to split our chunk - build a new free chunk */
                chunk_header* free_chk = (chunk_header*)((byte *)chk + sizeof(chunk_header)
                    + act_length);

                free_chk->parent = hdr->nil;
                free_chk->left = hdr->nil;
                free_chk->right = hdr->nil;
                free_chk->flags = 1 << 4;
                free_chk->lock_object = 0;
                free_chk->red = 0;
                free_chk->length = chk->length - act_length - sizeof(chunk_header);

                RBTreeInsert(hdr, 0, free_chk);
            }

            /* Build the chunk denoting a used block */
            chk->length = (byte*)act_length;
            chk->parent = hdr->nil;
            chk->left = hdr->nil;
            chk->right = hdr->nil;
            chk->lock_object = 0;
            chk->flags = 1 << 4 | 1;    // large block, white
            
            chk->red = 0;

            RBTreeInsert(hdr, 1, chk);

            return chk;
        }

        int get_size_index(int size)
        {
            for(int i = 0; i < sm_sizes.Length; i++)
            {
                if (size <= sm_sizes[i])
                    return i;
            }
            return -1;
        }

        int Compare(chunk_header *a, chunk_header *b, int tree_idx)
        {
            /* Compare(a, b) should return 1 if a > b, -1 if a < b, 0 if equal */

            if(tree_idx == 0)
            {
                /* free tree, sorted by length */
                if (a->length > b->length)
                    return 1;
                if (a->length < b->length)
                    return -1;
                return 0;
            }
            else
            {
                /* used tree, sorted by address */
                if (a > b)
                    return 1;
                if (a < b)
                    return -1;
                else
                    return 0;
            }
        }

        chunk_header *search(heap_header* tree, int tree_idx, int pattern, byte *x)
        {
            /* Return the chunk that matches the given pattern value:
             * 
             * 0) smallest value larger than or equal to (in-order successor of x)
             * 1) largest value smaller than or equal to (in-order predecessor of x)
             */

            /* First we do an 'pseudo-insert' on the new value and determine its would-be
             * parent and whether it would be the left or right child of that parent */

            chunk_header* parent = null;
            int child_idx = 0;  /* 0 = left child, 1 = right child */

            chunk_header* root = (tree_idx == 0) ? tree->root_free_chunk : tree->root_used_chunk;
            chunk_header* nil = tree->nil;
            root = root->left;
            chunk_header* src = root;

            while(src != nil)
            {
                byte* node_val;
                if(tree_idx == 0)
                {
                    /* free tree, sorted by length */
                    node_val = src->length;
                }
                else
                {
                    /* used tree, sorted by address */
                    node_val = (byte*)src;
                }

                if(node_val == x)
                {
                    /* We've found an exact match - return it */
                    return src;
                }
                if (x > node_val)
                {
                    /* search to the right */
                    parent = src;
                    child_idx = 1;
                    src = src->right;
                }
                else
                {
                    /* search to the left */
                    parent = src;
                    child_idx = 0;
                    src = src->left;
                }
            }
            
            /* Now we have a leaf node, and know which child of its parent it is
             * 
             * Look for either the in-order predecessor or successor depending on the
             * value of 'pattern'
             * 
             * To do this, for successor we travel up until we find a node that is a
             * left child, then take its parent.
             * 
             * For predecessor, we travel up until we find a node that is the right child
             * of its parent, then take its parent
             */

            while (pattern != child_idx)
            {
                /* go up the tree */
                chunk_header* cur_node = parent;
                parent = parent->parent;

                if (parent == nil)
                {
                    /* Couldn't find a block of the appropriate size */
                    return null;
                }

                /* determine whether cur_node is the left or right child of parent */
                if (cur_node == parent->left)
                    child_idx = 0;
                else
                    child_idx = 1;
            }

            return parent;
        }
    }
}
