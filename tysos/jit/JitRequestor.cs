using libtysila5;
using libtysila5.layout;
using metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace tysos.jit
{
    class JitRequestor : libtysila5.Requestor
    {
        class JitItemRequestor<T> : libtysila5.IndividualRequestor<T> where T : class, IEquatable<T>
        {
            collections.ManagedRingBuffer<T> rb = new collections.ManagedRingBuffer<T>();

            public override bool Empty => rb.IsEmpty;
            public override T GetNext()
            {
                return rb.Dequeue(out var ret) ? ret : null;
            }
            public override void Request(T v)
            {
                if (!rb.Enqueue(v))
                    throw new Exception("Requestor ring buffer full");
            }
            public override void Remove(T v)
            {
                throw new NotImplementedException();
            }
        }

        JitItemRequestor<Layout.MethodSpecWithEhdr> bm = new JitItemRequestor<Layout.MethodSpecWithEhdr>();
        JitItemRequestor<Layout.MethodSpecWithEhdr> eh = new JitItemRequestor<Layout.MethodSpecWithEhdr>();
        JitItemRequestor<Layout.MethodSpecWithEhdr> m = new JitItemRequestor<Layout.MethodSpecWithEhdr>();
        JitItemRequestor<Layout.MethodSpecWithEhdr> fm = new JitItemRequestor<Layout.MethodSpecWithEhdr>();
        JitItemRequestor<TypeSpec> sf = new JitItemRequestor<TypeSpec>();
        JitItemRequestor<TypeSpec> vt = new JitItemRequestor<TypeSpec>();
        JitItemRequestor<TypeSpec> d = new JitItemRequestor<TypeSpec>();

        public override IndividualRequestor<Layout.MethodSpecWithEhdr> BoxedMethodRequestor => bm;
        public override IndividualRequestor<Layout.MethodSpecWithEhdr> EHRequestor => eh;
        public override IndividualRequestor<Layout.MethodSpecWithEhdr> MethodRequestor => m;
        public IndividualRequestor<Layout.MethodSpecWithEhdr> FullMethodRequestor => fm;
        public override IndividualRequestor<TypeSpec> StaticFieldRequestor => sf;
        public override IndividualRequestor<TypeSpec> VTableRequestor => vt;
        public override IndividualRequestor<TypeSpec> DelegateRequestor => d;
    }
}
