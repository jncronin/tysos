namespace vcall
{
	interface IA
	{
		void a_m1();
		void a_m2();
	}

	interface IB
	{
		void b_m1();
	}

	interface IC : IA
	{
		void c_m1();
	}

	class A : IA, IB
	{
		public void a_m1() { }
		public void a_m2() { }
		public void b_m1() { }
	}

	class B : IA, IC
	{
		public void a_m1() { }
		public void a_m2() { }
		public void c_m1() { }
	}

	class Program
	{
		static int Main()
		{
			A a = new A();
			B b = new B();

			a.a_m1();
			b.a_m1();

			IA ia = a as IA;
			ia.a_m1();
			ia = b as IA;
			ia.a_m1();

			other(a);
			other(b);

			return 0;
		}

		static void other(object obj)
		{
			IC ic = obj as IC;

			ic.a_m1();
		}
	}
}

