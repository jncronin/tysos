using libsupcs;

namespace test_005
{
	[NoBaseClass]
    class Program
    {
        static void Main(string[] args)
        {
            OtherClass<int> oc = new OtherClass<int>();
            int x = oc.DoStuff(3);
        }

		[NoBaseClass]
        class OtherClass<T>
        {
            public T DoStuff(T val)
            {
                return val;
            }
        }
    }
}
