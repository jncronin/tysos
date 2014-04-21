using testca;
namespace Cli
{
    public class BuiltinFunction : System.Attribute
    {
        public BuiltinFunction(string s) { }
    }

    public class TypeAlias : System.Attribute
    {
        public TypeAlias(string s) { }
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
    public class DefineGlobal : System.Attribute
    {
        public DefineGlobal(string name) { }
    }

    public class Exceptions
    {
        public static void Throw(object e) { }
        public static void Throw(int e)
        {
            if (e == 1)
                Console.WriteLine("Kernel panic: System.OverflowException thrown");
            else if (e == 2)
                Console.WriteLine("Kernel panic: System.InvalidCastException thrown");
            else
                Console.WriteLine("Kernel panic: unknown exception thrown");
        }
    }

    public class RuntimeCheck
    {
        public static void PureVirtual()
        {
            Console.WriteLine("Kernel panic: attempt to call pure virtual function");
            while (true) { }
        }
    }
}
