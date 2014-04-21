namespace test_005
{
    class Program
    {
        static void Main(string[] args)
        {
            OtherClass<int> oc = new OtherClass<int>();
            int x = oc.DoStuff(3);
        }

        class OtherClass<T>
        {
            public T DoStuff(T val)
            {
                return val;
            }
        }
    }
}
