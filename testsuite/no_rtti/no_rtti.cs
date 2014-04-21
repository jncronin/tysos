namespace no_rtti_test
{
	class Program
	{
		unsafe static void Main()
		{
			*(byte *)0xb8000 = (byte)'H';
		}
	}
}

