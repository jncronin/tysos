using System;

namespace testca
{
    interface IPhysMemAllocator
    {
        void Initialise(vmem vmem);

        ulong BeginAlloc();
        ulong EndAlloc(ulong vaddr, ulong paddr);
        void Free(ulong vaddr, ulong paddr);

        ulong AllocRange(ulong length);

        ulong GetFreePageCount();
        ulong GetFreeMemory();
    }
}
