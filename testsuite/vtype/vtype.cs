namespace VType
{
	public struct A
	{
		public int a;
		public byte b;
	}

	public struct B
	{
		public A a;
		public uint c;

		// combine vtype member calling with vtype return
		public B DoStuff()
		{
			B ret = new B();
			ret.a = a;
			ret.c = c + c;
			return ret;
		}
	}
	
	[libsupcs.NoBaseClass]
	class Program
	{
		static A GetA(int v)
		{
			A a = new A();
			a.a = v;
			return a;
		}
			

		static void Main()
		{
			A a = GetA(3);

			B b = new B();
			b.a.a = 2;
			b.c = 5;
			b.DoStuff();
		}
	}
}

