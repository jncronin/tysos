namespace BareBones
{
	[libsupcs.NoBaseClass]
	unsafe class Console
	{
		byte *fb;

		int pos = 0;

		public void Clear()
		{
			for(int i = 0; i < 80 * 25 * 2; i++)
				*(fb + i) = 0;
		}

		public void Print(string s)
		{
			foreach(char c in s)
				Print(c);
		}

		public void Print(char c)
		{
			*(byte *)(fb + pos) = (byte)c;
			*(byte *)(fb + pos + 1) = 0x0f;
			pos += 2;
		}
	}
		
	[libsupcs.NoBaseClass]
	class Program
	{
		static int pos = 0;

		[libsupcs.StaticAlloc] static Console c = new Console((byte *)0xb8000);

		unsafe static void Main()
		{
			c.Clear();

			c.Print("Hello World!");

			while(true);
		}
	}
}

