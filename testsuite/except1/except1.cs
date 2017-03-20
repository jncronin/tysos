namespace Except1
{
	class Program
	{
		static int Main()
		{
			System.Console.WriteLine("Outside try");

			try
			{
				System.Console.WriteLine("In try");

				if(System.Console.ReadLine() == "Hello")
					throw(new System.Exception());

				System.Console.WriteLine("Shouldn't get here");
			}
			catch(System.Exception)
			{
				System.Console.WriteLine("In exception handler");
			}
			finally
			{
				System.Console.WriteLine("In finally");
			}

			System.Console.WriteLine("After try");

			return 0;
		}
	}
}

