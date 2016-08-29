namespace IfElse
{
	[libsupcs.NoBaseClass]
	class Program
	{
		static int Main()
		{
			int a;
			if(Get2() > Get3())
				a = DoA();
			else
				a = DoB();
			return a;
		}

		static int Get2()
		{
			return 2;
		}

		static int Get3()
		{
			return 3;
		}

		static int DoA()
		{
			return 42;
		}

		static int DoB()
		{
			return 47;
		}
	}
}

