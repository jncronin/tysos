using System;
using System.Collections.Generic;
using System.Text;

namespace tysos
{
    /**<summary>Implements a shared ring buffer with separate read and write pointers.</summary> */
    public unsafe class SharedMemory<T> where T : unmanaged
    {
        byte* b;
        int l;
        int max;
        int align;

        int rptr, wptr;

        byte[] buf_avoidgc; // persist buf to avoid being garbage collected

        public SharedMemory(void *baseaddr, int memlen)
        {
            init(baseaddr, memlen);
        }

        void init(void* baseaddr, int memlen)
        {
            b = (byte*)baseaddr;
            l = memlen;

            align = ((libsupcs.TysosType)typeof(T)).GetClassSize();
            int psize = sizeof(IntPtr);
            while ((align & (psize - 1)) != 0)
                align++;

            max = l / align;

            rptr = 0;
            wptr = 0;
        }

        public SharedMemory(byte[] buf)
        {
            buf_avoidgc = buf;

            byte* baseaddr = *(byte**)((byte*)libsupcs.CastOperations.ReinterpretAsPointer(buf) + libsupcs.ArrayOperations.GetInnerArrayOffset());

            init(baseaddr, buf.Length);
        }

        public SharedMemory(VirtualMemoryResource32 vm) : this((void*)vm.Addr32, (int)vm.Length32) { }
        public SharedMemory(VirtualMemoryResource64 vm) : this((void*)vm.Addr64, (int)vm.Length64) { }

        public T* GetNextRead()
        {
            int next_ptr = -1;
            do
            {
                int cur_ptr = rptr;
                next_ptr = rptr + 1;
                if (next_ptr >= max)
                    next_ptr = 0;
                if (cur_ptr == wptr)
                    return null;

                if (System.Threading.Interlocked.CompareExchange(ref rptr, next_ptr, cur_ptr) == cur_ptr)
                    return (T*)(b + align * cur_ptr);
            } while (true);
        }

        public T* GetNextWrite()
        {
            int next_ptr = -1;
            do
            {
                int cur_ptr = wptr;
                next_ptr = wptr + 1;
                if (next_ptr >= max)
                    next_ptr = 0;
                if (next_ptr == wptr)
                    return null;

                if (System.Threading.Interlocked.CompareExchange(ref wptr, next_ptr, cur_ptr) == cur_ptr)
                    return (T*)(b + align * cur_ptr);
            } while (true);
        }

        public bool Read(out T val)
        {
            T* oref = GetNextRead();
            if(oref == null)
            {
                val = default(T);
                return false;
            }
            else
            {
                val = *oref;
                return true;
            }
        }

        public bool Write(T val)
        {
            T* oref = GetNextWrite();
            if (oref == null)
                return false;
            *oref = val;
            return true;
        }

        public bool MessgeReady { get { return rptr != wptr; } }
    }
}
