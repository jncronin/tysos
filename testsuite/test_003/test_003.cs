using libsupcs;

namespace test_003
{
	[NoBaseClass]
    class Program
    {
        static void Main(string[] args)
        {
            OtherClass oc = new OtherClass();

            oc.do_sum(5);
        }
    }

	[NoBaseClass]
    class OtherClass
    {
        public int do_sum(int a)
        {
            return a + a;
        }
    }
}
