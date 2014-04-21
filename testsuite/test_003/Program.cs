namespace test_003
{
    class Program
    {
        static void Main(string[] args)
        {
            OtherClass oc = new OtherClass();

            oc.do_sum(5);
        }
    }

    class OtherClass
    {
        public int do_sum(int a)
        {
            return a * a;
        }
    }
}
