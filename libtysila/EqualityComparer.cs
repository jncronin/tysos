using System;
using System.Collections.Generic;
using System.Text;

namespace libtysila
{
    /* The following class is taken from Mono.  mono/corlib/System.Collections.Generic/EqualityComparer.cs
     * Authors: Ben Maurer (bmaurer@ximian.com), Copyright (C) 2004 Novell, Inc under the same license as this file
     * 
     * We need to use our own version of this as EqualityComparer<T> has a static constructor which instantiates
     * a generic type, and if the jit is not functioning this cannot yet be done */
    public class GenericEqualityComparer<T> : EqualityComparer<T> where T : System.IEquatable<T>
    {
        public override int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        public override bool Equals(T x, T y)
        {
            if (x == null)
                return y == null;

            return x.Equals(y);
        }
    }

    public class BaseType_TypeEqualityComparer : IEqualityComparer<BaseType_Type>
    {
        public bool Equals(BaseType_Type x, BaseType_Type y)
        {
            return x == y;
        }

        public int GetHashCode(BaseType_Type obj)
        {
            return obj.GetHashCode();
        }
    }

    public class ThreeAddressCode_OpEqualityComparer : IEqualityComparer<ThreeAddressCode.Op>
    {
        public bool Equals(ThreeAddressCode.Op x, ThreeAddressCode.Op y)
        {
            return x == y;
        }

        public int GetHashCode(ThreeAddressCode.Op obj)
        {
            return obj.GetHashCode();
        }
    }
}
