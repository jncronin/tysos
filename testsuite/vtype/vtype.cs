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

		public void DoStuff()
		{
			c += c;
		}
	}
	
	[libsupcs.NoBaseClass]
	class Program
	{
		static void Main()
		{
			A a = new A();
			a.a = 3;

			B b = new B();
			b.a.a = 2;
			b.c = 5;
			b.DoStuff();
		}
	}
}

