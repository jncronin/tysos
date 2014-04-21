namespace delegate_test
{
	class Program
	{
		static void Main()
		{
			my_delegate my_delegate_object = my_test_func;
			System.IntPtr my_delegate_addr = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(my_delegate_object);
		}

		delegate void my_delegate();

		static void my_test_func()
		{
		}
	}

}

