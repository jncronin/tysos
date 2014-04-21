using System.Runtime.CompilerServices;
namespace ABI
{
    [global::System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class MethodAliasAttribute : System.Attribute
    {
        public MethodAliasAttribute(string alias) { }
    }

    [global::System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    sealed class AlwaysCompileAttribute : System.Attribute
    { }

    [global::System.AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class OutputCHeaderAttribute : System.Attribute
    { }

    class MemoryOperations
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        internal static extern byte Peek(ulong addr);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.InternalCall)]
        internal static extern void Poke(ulong addr, byte b);

    }

    class CastOperations
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern test_004.NextClass ReinterpretAsNextClass(ulong addr);
    }
}

namespace test_004
{
    class Program
    {
        static void Main(string[] args)
        {
            /*OtherClass oc = new OtherClass();
            oc.do_sum(25, 5);

            NextClass nc = new NextClass();
            nc.do_sum(30, 5);

            ThirdClass tc = new ThirdClass();
            tc.do_sum(40, 5);

            OtherClass lc = new ThirdClass();
            lc.do_sum(50, 5);

            byte b = ABI.MemoryOperations.Peek(0xb8000);
            ABI.MemoryOperations.Poke(0xb8000, b);*/

            NextClass another_class = ABI.CastOperations.ReinterpretAsNextClass(0x1000);
            another_class.do_sum(29, 30);
        }

        static long next_mem = 0x100000;

        [ABI.MethodAlias("gcmalloc")]
        [ABI.AlwaysCompile]
        static long malloc(long size)
        {
            long ret = next_mem;
            next_mem += size;
            return ret;
        }
    }

    class OtherClass
    {
        public virtual int do_sum(int a, int b)
        {
            return a / b;
        }
    }

    class NextClass : OtherClass
    { }

    [ABI.OutputCHeader]
    class ThirdClass : OtherClass
    {
        public override int do_sum(int a, int b)
        {
            return a * b;
        }
    }
}
