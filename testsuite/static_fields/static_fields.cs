namespace static_fields
{
	[libsupcs.NoBaseClass]
	class Program
	{
		static int a;
		static int b;

		static int Main()
		{
			a = 5;
			b = 4;

			return a;
		}
	}
}

