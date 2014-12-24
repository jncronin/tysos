namespace IfElse
{
	[libsupcs.NoBaseClass]
	class Program
	{
		static void Main()
		{
			if(Get2() > Get3())
				DoA();
			else
				DoB();
		}

		static int Get2()
		{
			return 2;
		}

		static int Get3()
		{
			return 3;
		}

		static void DoA()
		{
		}

		static void DoB()
		{
		}
	}
}

