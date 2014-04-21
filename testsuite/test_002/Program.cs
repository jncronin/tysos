namespace test_002
{
    class Program
    {
        static int Main(string[] args)
        {
            int a = 25;
            int b = get_num(a);
            return b;
        }

        static int get_num(int a)
        {
            return a * a;
        }
    }
}
