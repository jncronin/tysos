namespace vcall
{
	class Base
	{
		public void base_inst_meth()
		{ }

		public virtual void base_v_meth()
		{ }
	}

	class Derived : Base
	{
		public override void base_v_meth()
		{
			base.base_v_meth();
		}

		public virtual void derived_v_meth()
		{
		}
	}

	class Test
	{
		public static int Main()
		{
			Base a = new Base();
			Derived b = new Derived();

			a.base_inst_meth();
			b.base_inst_meth();

			a.base_v_meth();
			b.base_v_meth();

			b.derived_v_meth();

			return 0;
		}
	}
}



