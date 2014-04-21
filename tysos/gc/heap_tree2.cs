/* Copyright (C) 2011, 2012 by John Cronin
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

/* The heap is split into arenas, each of which contains large and small heaps.
 * 
 * The large heap contains a tree of used and free blocks, which begin with a
 * block header.
 * 
 * The small heap is divided into 4 kiB blocks, each of which has a header and
 * is then divided into free blocks of a certain size (defined in the header)
 * and their free status is identified in a bitmap in the header.
 * 
 * The small heap is used for allocations up to small_cutoff (currently defined
 * as 512 bytes) 
 * 
 * 
 * The free block tree is sorted by size of the block.
 * The used block tree is sorted by the address of the block.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.gc
{
    partial class heap_arena
    {
        internal static bool debug = false;

        internal const ulong arena_blk_free_root = 0;
        internal const ulong arena_flags = 8;
        internal const ulong arena_lock = 16;
        internal const ulong arena_blk_used_root = 24;
        //internal const ulong arena_length = 32;       // unused
        internal const ulong arena_small_start = 40;
        internal const ulong arena_small_end = 48;
        internal const ulong arena_large_start = 56;
        internal const ulong arena_large_end = 64;
        internal const ulong arena_small_cutoff = 72;
        internal const ulong arena_next_free_32 = 80;
        internal const ulong arena_next_free_64 = 88;
        internal const ulong arena_next_free_128 = 96;
        internal const ulong arena_next_free_256 = 104;
        internal const ulong arena_next_free_512 = 112;
        internal const ulong arena_first_32 = 120;
        internal const ulong arena_first_64 = 128;
        internal const ulong arena_first_128 = 136;
        internal const ulong arena_first_256 = 144;
        internal const ulong arena_first_512 = 152;
        internal const ulong arena_next_free_small = 160;
        internal const ulong arena_in_use_list_start = 168;
        internal const ulong arena_header_length = 176;

        internal const ulong arena_run_size_count = 5;

        const ulong arena_flags_left_right_toggle = 1;
        const ulong arena_flags_suc_pred_toggle = 2;

        internal const ulong blk_parent = 0;
        internal const ulong blk_left = 8;
        internal const ulong blk_right = 16;
        internal const ulong blk_length = 24;
        internal const ulong blk_next = 32;
        internal const ulong blk_prev = 40;
        internal const ulong blk_gc_flags = 48;
        internal const ulong blk_header_length = 56;

        internal const ulong BLK_GC_FLAGS_INTPTR_ARRAY = 1;
        internal const ulong BLK_GC_FLAGS_GC_REFERENCED = 2;

        internal const ulong BLK_SORT_INDEX_ADDR_OF = 0xf000000000000000;

        internal ulong cur_arena;

        const ulong dbg_address = 0xffff8000006ea000;

        internal unsafe void Init(ulong small_cutoff, ulong small_start, ulong small_end, ulong long_start, ulong long_end)
        {
            cur_arena = small_start;
            ulong first_node = long_start;

            // clear the arena header
            for (ulong i = 0; i < arena_header_length; i += 8)
                *(ulong*)(cur_arena + i) = 0;

            *(ulong*)(cur_arena + arena_blk_free_root) = first_node;
            *(ulong*)(cur_arena + arena_flags) = 0;
            *(ulong*)(cur_arena + arena_lock) = 0;
            *(ulong*)(cur_arena + arena_blk_used_root) = 0;
            *(ulong*)(cur_arena + arena_small_cutoff) = small_cutoff;
            *(ulong*)(cur_arena + arena_small_start) = util.align(small_start + arena_header_length, 4096);
            *(ulong*)(cur_arena + arena_small_end) = small_end;
            *(ulong*)(cur_arena + arena_large_start) = long_start;
            *(ulong*)(cur_arena + arena_large_end) = long_end;
            *(ulong*)(cur_arena + arena_next_free_small) = *(ulong*)(cur_arena + arena_small_start);
            *(ulong*)(cur_arena + arena_in_use_list_start) = 0;

            *(ulong*)(first_node + blk_parent) = 0;
            *(ulong*)(first_node + blk_left) = 0;
            *(ulong*)(first_node + blk_right) = 0;
            *(ulong*)(first_node + blk_prev) = 0;
            *(ulong*)(first_node + blk_next) = 0;
            *(ulong*)(first_node + blk_length) = long_end - long_start - blk_header_length;
        }

        const ulong small_cutoff = 512;

        [libsupcs.Uninterruptible]
        internal unsafe void Free(ulong addr)
        {
            try
            {
                lib.Monitor.spinlockb(cur_arena + arena_lock);

                ulong node_addr = addr - blk_header_length;

                /* Remove it from the used list */
                blk_list_delete(cur_arena, node_addr);

                /* Remove it from the used tree */
                blk_delete(cur_arena, node_addr, arena_blk_used_root);

                /* Add it to the free tree */
                blk_add(cur_arena, node_addr, *(ulong*)(node_addr + blk_length), arena_blk_free_root, blk_length);
            }
            finally
            {
                lib.Monitor.spinunlockb(cur_arena + arena_lock);
            }
        }

        [libsupcs.Uninterruptible]
        internal unsafe ulong Alloc(ulong size, ulong flags)
        {
            try
            {
                lib.Monitor.spinlockb(cur_arena + arena_lock);

                if (size <= *(ulong*)(cur_arena + arena_small_cutoff))
                    return do_small_alloc(cur_arena, size);

                // test to find the optimum cutoff for large areas
                /*if (size <= small_cutoff)
                    small_count++;
                else
                    large_count++;

                if (debug)
                {
                    Formatter.Write("Alloc: size: ", Program.arch.DebugOutput);
                    Formatter.Write(size, Program.arch.DebugOutput);
                    Formatter.Write("  small_count: ", Program.arch.DebugOutput);
                    Formatter.Write(small_count, Program.arch.DebugOutput);
                    Formatter.Write("  large_count: ", Program.arch.DebugOutput);
                    Formatter.Write(large_count, Program.arch.DebugOutput);
                    Formatter.WriteLine(Program.arch.DebugOutput);
                }*/

                /* First get a block equal to or larger than the requested area */
                ulong ret = blk_get_greater_equal(cur_arena, *(ulong*)(cur_arena + arena_blk_free_root), size, blk_length);
                if (ret == 0)
                    return 0;

                /* Remove it from the free tree */
                blk_delete(cur_arena, ret, arena_blk_free_root);

                /* See if we can split the region and only return part of it */
                ulong block_length = *(ulong*)(ret + blk_length);
                if ((block_length - blk_header_length) > size)
                {
                    ulong new_block = ret + blk_header_length + size;
                    ulong new_block_length = block_length - blk_header_length - size;
                    blk_add(cur_arena, new_block, new_block_length, arena_blk_free_root, blk_length);

                    // set the size of the current block to allow it to be added again later
                    *(ulong*)(ret + blk_length) = size;
                }

                /* Store the current arena in the control area for the returned block
                 * This will allow us to restore it to the correct arena later */
                *(ulong*)(ret + blk_parent) = cur_arena;

                /* Store flags */
                *(ulong*)(ret + blk_gc_flags) = flags;

                /* Add it to the used tree */
                blk_add(cur_arena, ret, *(ulong*)(ret + blk_length), arena_blk_used_root, BLK_SORT_INDEX_ADDR_OF & blk_header_length);

                /* Add it to the used list */
                blk_list_add(cur_arena, ret);

                return ret + blk_header_length;
            }
            finally
            {
                lib.Monitor.spinunlockb(cur_arena + arena_lock);
            }
        }

        #region List functions
        unsafe void blk_list_add(ulong arena, ulong blk)
        {
            // Add to the start of the list
            ulong cur_start = *(ulong*)(arena + arena_in_use_list_start);
            if (cur_start != 0)
                *(ulong*)(cur_start + blk_prev) = blk;
            *(ulong*)(blk + blk_next) = *(ulong*)(arena + arena_in_use_list_start);
            *(ulong*)(blk + blk_prev) = 0;
            *(ulong*)(arena + arena_in_use_list_start) = blk;            
        }

        unsafe void blk_list_delete(ulong arena, ulong blk)
        {
            // Delete from the list
            ulong prev = *(ulong*)(blk + blk_prev);
            ulong next = *(ulong*)(blk + blk_next);

            // First ensure the start pointer is valid
            if (*(ulong*)(arena + arena_in_use_list_start) == blk)
                *(ulong*)(arena + arena_in_use_list_start) = next;

            // Patch up the blk prev node if exists
            if (prev != 0)
                *(ulong*)(prev + blk_next) = next;

            // Patch up the blk next node if exists
            if (next != 0)
                *(ulong*)(next + blk_prev) = prev;
        }

        #endregion

        #region Tree functions
        unsafe void blk_add(ulong arena, ulong blk, ulong length, ulong root_offset, ulong sort_index_offset)
        {
            *(ulong*)(blk + blk_length) = length;
            *(ulong*)(blk + blk_left) = 0;
            *(ulong*)(blk + blk_right) = 0;

#if GC_DEBUG
            if ((root_offset == arena_blk_used_root) && debug)
            {
                Formatter.Write("Adding to used list: address: 0x", Program.arch.DebugOutput);
                Formatter.Write(blk, "X", Program.arch.DebugOutput);
                Formatter.Write("  length: 0x", Program.arch.DebugOutput);
                Formatter.Write(length, "X", Program.arch.DebugOutput);
                Formatter.WriteLine(Program.arch.DebugOutput);
            }

            if (blk >= dbg_address)
                Formatter.WriteLine("Beginning add of test address", Program.arch.DebugOutput);
#endif

            if (*(ulong*)(arena + root_offset) == 0)
            {
                //Formatter.WriteLine("Added root tree entry", Program.arch.DebugOutput);
                /* The tree is empty, therefore add us as the root node */
                *(ulong*)(blk + blk_parent) = 0;
                *(ulong*)(arena + root_offset) = blk;
            }
            else
                blk_insert(arena, *(ulong*)(arena + root_offset), blk, sort_index_offset);

#if GC_DEBUG
            if (blk >= dbg_address)
                Formatter.WriteLine("Ending add of test address", Program.arch.DebugOutput);
#endif
        }

        unsafe void blk_insert(ulong arena, ulong below, ulong blk, ulong sort_index_offset)
        {
            /* This is an iterative implementation of insertion into a binary tree
             * 
             * This needs to be optimised to balance the tree somehow
             */

            ulong cur_below = below;

            while (true)
            {
                ulong left = 0;
                ulong comp = 0;

                if ((sort_index_offset & BLK_SORT_INDEX_ADDR_OF) == BLK_SORT_INDEX_ADDR_OF)
                    comp = blk - cur_below;
                else
                    comp = *(ulong*)(blk + sort_index_offset) - *(ulong*)(cur_below + sort_index_offset);

                if (comp == 0)
                {
                    /* Sort values are equal, this region can be inserted either to the right
                     * or left (which we alternate between) */

                    ulong flags = *(ulong*)(arena + arena_flags);
                    left = flags & arena_flags_left_right_toggle;
                    flags ^= arena_flags_left_right_toggle;
                    *(ulong*)(arena + arena_flags) = flags;
                }
                else if (comp < 0)
                    left = arena_flags_left_right_toggle;

                if (left == arena_flags_left_right_toggle)
                {
                    if (*(ulong*)(cur_below + blk_left) == 0)
                    {
                        *(ulong*)(cur_below + blk_left) = blk;
                        *(ulong*)(blk + blk_parent) = cur_below;
                        return;
                    }
                    else
                        cur_below = *(ulong*)(cur_below + blk_left);
                }
                else
                {
                    if (*(ulong*)(cur_below + blk_right) == 0)
                    {
                        *(ulong*)(cur_below + blk_right) = blk;
                        *(ulong*)(blk + blk_parent) = cur_below;
                        return;
                    }
                    else
                        cur_below = *(ulong*)(cur_below + blk_right);
                }
            }
        }

        unsafe void blk_insert2(ulong arena, ulong below, ulong blk, ulong sort_index_offset)
        {
            /* Recursive implementation of node insert function */

            ulong left = 0;
            ulong comp = 0;

#if GC_DEBUG
            if (blk >= dbg_address)
                Formatter.WriteLine("Beginning insert of test address", Program.arch.DebugOutput);
#endif

            if ((sort_index_offset & BLK_SORT_INDEX_ADDR_OF) == BLK_SORT_INDEX_ADDR_OF)
                comp = blk - below;
            else
                comp = *(ulong*)(blk + sort_index_offset) - *(ulong*)(below + sort_index_offset);

            if (comp == 0)
            {
                /* Lengths are equal, this region can be inserted either to the right
                 * or left (which we alternate between) */

                ulong flags = *(ulong*)(arena + arena_flags);
                left = flags & arena_flags_left_right_toggle;
                flags ^= arena_flags_left_right_toggle;
                *(ulong*)(arena + arena_flags) = flags;
            }
            else if (comp < 0)
                left = arena_flags_left_right_toggle;

            if (left == arena_flags_left_right_toggle)
            {
                if (*(ulong*)(below + blk_left) == 0)
                {
                    *(ulong*)(below + blk_left) = blk;
                    *(ulong*)(blk + blk_parent) = below;
                }
                else
                    blk_insert(arena, *(ulong*)(below + blk_left), blk, sort_index_offset);
            }
            else
            {
                if (*(ulong*)(below + blk_right) == 0)
                {
                    *(ulong*)(below + blk_right) = blk;
                    *(ulong*)(blk + blk_parent) = below;
                }
                else
                    blk_insert(arena, *(ulong*)(below + blk_right), blk, sort_index_offset);
            }
        }

        unsafe void blk_delete(ulong arena, ulong blk, ulong root_offset)
        {
            /* relation_to_parent:  0 = node has no parent
             *                      1 = node is left child of parent
             *                      2 = node is right child of parent
             */
            ulong relation_to_parent = 0;

            ulong parent = *(ulong*)(blk + blk_parent);
            ulong left_child = *(ulong*)(blk + blk_left);
            ulong right_child = *(ulong*)(blk + blk_right);

            if (parent == 0)
                relation_to_parent = 0;
            else
            {
                if (*(ulong*)(parent + blk_left) == blk)
                    relation_to_parent = 1;
                else
                    relation_to_parent = 2;
            }

            // determine number of children
            if (left_child != 0)
            {
                if (right_child != 0)
                {
                    // two children

                    /* non-trivial
                     * 
                     * first find either the node's in-order successor or predecessor
                     * then swap that node with the current one
                     */

                    // alternate between in-order successor and in-order predecessor
                    ulong flags = *(ulong*)(arena + arena_flags);
                    ulong successor = flags & arena_flags_suc_pred_toggle;
                    flags ^= arena_flags_suc_pred_toggle;
                    *(ulong*)(arena + arena_flags) = flags;

                    // find the node to swap with
                    ulong swap_node = 0;
                    if (successor == arena_flags_suc_pred_toggle)
                        swap_node = blk_in_order_successor(arena, blk);
                    else
                        swap_node = blk_in_order_predecessor(arena, blk);

                    // decide upon the swap node's relation to its parent
                    ulong swap_parent = *(ulong*)(swap_node + blk_parent);
                    ulong swap_relation_to_parent = 0;
                    if (swap_parent == 0)
                        swap_relation_to_parent = 0;
                    else if (*(ulong*)(swap_parent + blk_left) == swap_node)
                        swap_relation_to_parent = 1;
                    else
                        swap_relation_to_parent = 2;

                    // delete the swap node from it's original position
                    blk_set_child_entry(arena, swap_parent, swap_relation_to_parent, 0, root_offset);

                    // set the swap node's relations to be that of the original node
                    *(ulong*)(swap_node + blk_parent) = parent;
                    *(ulong*)(swap_node + blk_left) = left_child;
                    *(ulong*)(swap_node + blk_right) = right_child;

                    // set the node's parent's child to be the swap node
                    blk_set_child_entry(arena, parent, relation_to_parent, swap_node, root_offset);

                    // set the node's children to habe the swap node as their parent
                    *(ulong*)(left_child + blk_parent) = swap_node;
                    *(ulong*)(right_child + blk_parent) = swap_node;
                }
                else
                {
                    /* single left child
                     * replace this node with the left child
                     */
                    *(ulong*)(left_child + blk_parent) = parent;
                    blk_set_child_entry(arena, parent, relation_to_parent, left_child, root_offset);
                }
            }
            else
            {
                if (right_child != 0)
                {
                    /* single right child
                     * replace this node with the right child
                     */
                    *(ulong*)(right_child + blk_parent) = parent;
                    blk_set_child_entry(arena, parent, relation_to_parent, right_child, root_offset);
                }
                else
                {
                    // no children - just delete the entry in the parent
                    blk_set_child_entry(arena, parent, relation_to_parent, 0, root_offset);
                }
            }
        }

        private unsafe void blk_set_child_entry(ulong arena, ulong parent, ulong child_number, ulong blk, ulong root_offset)
        {
            if (child_number == 0)
                *(ulong*)(arena + root_offset) = blk;
            else if (child_number == 1)
                *(ulong*)(parent + blk_left) = blk;
            else if (child_number == 2)
                *(ulong*)(parent + blk_right) = blk;
        }

        private unsafe ulong blk_in_order_predecessor(ulong arena, ulong blk)
        {
            // return right-most of left subtree
            return blk_find_max_child(arena, *(ulong*)(blk + blk_left));
        }

        private static unsafe ulong blk_find_max_child(ulong arena, ulong blk)
        {
            // return right-most child - iterative implementation
            ulong right_child = *(ulong*)(blk + blk_right);
            ulong cur_blk = blk;
            while (right_child != 0)
            {
                cur_blk = right_child;
                right_child = *(ulong*)(cur_blk + blk_right);
            }
            return cur_blk;
        }

        private unsafe ulong blk_in_order_successor(ulong arena, ulong blk)
        {
            // return left-most of right subtree
            return blk_find_min_child(arena, *(ulong*)(blk + blk_right));
        }

        internal delegate void traversal_function(ulong blk);
        internal static unsafe void blk_in_order_traversal_recursive(ulong arena, ulong blk, traversal_function func)
        {
            if (blk == 0)
                return;
            blk_in_order_traversal_recursive(arena, *(ulong*)(blk + blk_left), func);
            func(blk);
            blk_in_order_traversal_recursive(arena, *(ulong*)(blk + blk_right), func);
        }

        internal static unsafe void blk_list_traversal(ulong arena, ulong blk, traversal_function func)
        {
            /* This iterates through the linked list rather than the tree, therefore it supports
             * the ability for nodes to be removed in the process of traversal */
            ulong cur_blk = blk;

            while (cur_blk != 0)
            {
                ulong next = *(ulong*)(cur_blk + blk_next);
                func(cur_blk);
                cur_blk = next;
            }
        }

        internal static unsafe void blk_in_order_traversal(ulong arena, ulong blk, traversal_function func)
        {
            // Adapted from http://www.perlmonks.org/?node_id=600456
            
            if (blk == 0)
                return;

            ulong cur_blk = blk;
            ulong prev_blk = *(ulong*)(cur_blk + blk_parent);

            while (cur_blk != 0)
            {
                ulong next_blk = 0;

                if (prev_blk == *(ulong*)(cur_blk + blk_parent))
                {
                    next_blk = *(ulong*)(cur_blk + blk_left);
                    if (next_blk == 0)
                    {
                        next_blk = *(ulong*)(cur_blk + blk_right);
                        if (next_blk == 0)
                            next_blk = *(ulong*)(cur_blk + blk_parent);
                        func(cur_blk);
                    }
                }
                else if (prev_blk == *(ulong*)(cur_blk + blk_left))
                {
                    next_blk = *(ulong*)(cur_blk + blk_right);
                    if (next_blk == 0)
                        next_blk = *(ulong*)(cur_blk + blk_parent);
                    func(cur_blk);
               }
                else if (prev_blk == *(ulong*)(cur_blk + blk_right))
                {
                    next_blk = *(ulong*)(cur_blk + blk_parent);
                }

                prev_blk = cur_blk;
                cur_blk = next_blk;
            }            
        }

        private static unsafe ulong blk_find_min_child(ulong arena, ulong blk)
        {
            // return left-most child - iterative implementation
            ulong left_child = *(ulong*)(blk + blk_left);
            ulong cur_blk = blk;
            while (left_child != 0)
            {
                cur_blk = left_child;
                left_child = *(ulong*)(cur_blk + blk_left);
            }
            return cur_blk;
        }

        private unsafe ulong blk_get_greater_equal(ulong arena, ulong blk, ulong val, ulong sort_index_offset)
        {
            /* Find a block below blk whose length is equal to or greater than length
             * Iterative implementation
             */

            ulong cur_blk = blk;
            ulong found_node = 0;
            ulong found_length = 0;

            if (cur_blk == 0)
                return 0;

            while (true)
            {
                ulong block_val = 0;
                if ((sort_index_offset & BLK_SORT_INDEX_ADDR_OF) == BLK_SORT_INDEX_ADDR_OF)
                    block_val = cur_blk + (sort_index_offset & ~BLK_SORT_INDEX_ADDR_OF);
                else
                    block_val = *(ulong*)(cur_blk + sort_index_offset);

                if (val == block_val)
                    return cur_blk;
                else if (block_val > val)
                {
                    /* This node is larger than what was requested, however try to optimise
                     * or choice by using a smaller node than this if possible
                     */

                    // first, store our current choice
                    if ((found_node == 0) || (block_val < found_length))
                    {
                        found_node = cur_blk;
                        found_length = block_val;
                    }

                    // is there a left child (i.e. something smaller than the current node) ?
                    ulong left_child = *(ulong*)(cur_blk + blk_left);
                    if (left_child == 0)
                    {
                        // there is no left child, therefore the best we can do is 'found_node'
                        return found_node;
                    }
                    else
                    {
                        // re-try with the left child
                        cur_blk = left_child;
                    }
                }
                else
                {
                    /* This node is smaller than what was requested, however it may have
                     * a right child which fits our requirements, so try that if possible
                     */
                    ulong right_child = *(ulong*)(cur_blk + blk_right);
                    if (right_child == 0)
                    {
                        /* there is no right child, therefore stop here and return whatever
                         * we have saved to 'found_node' */
                        return found_node;
                    }
                    else
                    {
                        // re-try with the right child
                        cur_blk = right_child;
                    }
                }
            }
        }

        internal unsafe ulong blk_get_less_equal(ulong arena, ulong blk, ulong val, ulong sort_index_offset)
        {
            /* Find a block below blk whose length is equal to or less than length
             * Iterative implementation
             */

            ulong cur_blk = blk;
            ulong found_node = 0;
            ulong found_length = 0;

            if (cur_blk == 0)
                return 0;

            while (true)
            {
                ulong block_val = 0;
                if ((sort_index_offset & BLK_SORT_INDEX_ADDR_OF) == BLK_SORT_INDEX_ADDR_OF)
                    block_val = cur_blk + (sort_index_offset & ~BLK_SORT_INDEX_ADDR_OF);
                else
                    block_val = *(ulong*)(cur_blk + sort_index_offset);

                if (val == block_val)
                    return cur_blk;
                else if (block_val < val)
                {
                    /* This node is smaller than what was requested, however try to optimise
                     * or choice by using a larger node than this if possible
                     */

                    // first, store our current choice
                    if ((found_node == 0) || (block_val < found_length))
                    {
                        found_node = cur_blk;
                        found_length = block_val;
                    }

                    // is there a right child (i.e. something larger than the current node) ?
                    ulong right_child = *(ulong*)(cur_blk + blk_right);
                    if (right_child == 0)
                    {
                        // there is no right child, therefore the best we can do is 'found_node'
                        return found_node;
                    }
                    else
                    {
                        // re-try with the right child
                        cur_blk = right_child;
                    }
                }
                else
                {
                    /* This node is greater than what was requested, however it may have
                     * a left child which fits our requirements, so try that if possible
                     */
                    ulong left_child = *(ulong*)(cur_blk + blk_left);
                    if (left_child == 0)
                    {
                        /* there is no left child, therefore stop here and return whatever
                         * we have saved to 'found_node' */
                        return found_node;
                    }
                    else
                    {
                        // re-try with the left child
                        cur_blk = left_child;
                    }
                }
            }
        }
        #endregion
    }
}
