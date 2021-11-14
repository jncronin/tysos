using System;
using System.Collections.Generic;
using System.Text;

/* This is a lock-free multiple producer multiple consumer ring buffer based upon
 * the DPDK Ring Library described at https://doc.dpdk.org/guides/prog_guide/ring_lib.html */

namespace tysos.collections
{
    unsafe class RingBuffer<T> where T : unmanaged
    {
        T* d;
        int prod_head, prod_tail, cons_head, cons_tail;
        int mask;

        /** <summary>Create a ring buffer at the given memory location with the given length </summary> */
        public RingBuffer(void *mem, int length)
        {
            d = (T*)mem;

            // initialize indices
            prod_head = 0;
            prod_tail = 0;
            cons_head = 0;
            cons_tail = 0;

            // decide on length of the buffer
            int count = length / sizeof(T);

            // reduce to next lowest power of 2 so we don't need to worry about wraparound
            //  (instead we just do index & mask on data access)
            int highest_bit = 0;
            for(int i = 0; i < 32; i++)
            {
                if ((count & (1 << i)) != 0)
                    highest_bit = i;
            }
            count = 1 << highest_bit;
            mask = count - 1;
        }

        /** <summary>Add a value to the ring buffer</summary> */
        public bool Enqueue(T val)
        {
            int l_cons_tail;
            int l_prod_head;
            int l_prod_next;

            while (true)
            {
                l_cons_tail = cons_tail;
                l_prod_head = prod_head;
                l_prod_next = l_prod_head + 1;

                if ((l_prod_next & mask) == (l_cons_tail & mask))
                {
                    // Out of space
                    return false;
                }

                // Try and update prod_head
                if (System.Threading.Interlocked.CompareExchange(ref prod_head, l_prod_next, l_prod_head) == l_prod_head)
                    break;
            }

            d[l_prod_head & mask] = val;

            // update prod_tail
            while (System.Threading.Interlocked.CompareExchange(ref prod_tail, l_prod_next, l_prod_head) != l_prod_head) ;

            return true;
        }

        /** <summary>Get the next value from the ring buffer</summary> */
        public bool Dequeue(out T val)
        {
            int l_prod_tail;
            int l_cons_head;
            int l_cons_next;

            while(true)
            {
                l_prod_tail = prod_tail;
                l_cons_head = cons_head;
                l_cons_next = l_cons_head + 1;

                if((l_prod_tail & mask) == (l_cons_head & mask))
                {
                    val = default(T);
                    return false;
                }

                // Try and update cons_head
                if (System.Threading.Interlocked.CompareExchange(ref cons_head, l_cons_next, l_cons_head) == l_cons_head)
                    break;
            }

            val = d[l_cons_head & mask];

            // update cons_tail
            while (System.Threading.Interlocked.CompareExchange(ref cons_tail, l_cons_next, l_cons_head) != l_cons_head) ;

            return true;
        }

        /** <summary>Peek the next value from the ring buffer</summary> */
        public bool Peek(out T val)
        {
            while (true)
            {
                int l_prod_tail = prod_tail;
                int l_cons_head = cons_head;

                if ((l_prod_tail & mask) == (l_cons_head & mask))
                {
                    val = default(T);
                    return false;
                }

                T retval = d[l_cons_head & mask];

                // has there been an interval read which invalidates this?
                if(cons_head == l_cons_head)
                {
                    // no - return the value
                    val = retval;
                    return true;
                }
                // else loop and try again
            }
        }
    }
}
