namespace Kernel
{
    public class Kernel
    {
        [Cli.BuiltinFunction("poke_U1")]
        public extern static void poke(ulong addr, byte val);
        [Cli.BuiltinFunction("poke_U2")]
        public extern static void poke(ulong addr, char val);
        [Cli.BuiltinFunction("poke_U4")]
        public extern static void poke(ulong addr, uint val);
        [Cli.BuiltinFunction("poke_U8")]
        public extern static void poke(ulong addr, ulong val);

        [Cli.BuiltinFunction("peek")]
        public extern static byte peek_U1(ulong addr);
        [Cli.BuiltinFunction("peek")]
        public extern static ushort peek_U2(ulong addr);
        [Cli.BuiltinFunction("peek")]
        public extern static uint peek_U4(ulong addr);
        [Cli.BuiltinFunction("peek")]
        public extern static ulong peek_U8(ulong addr);

        [Cli.BuiltinFunction("get_symbol_addr")]
        public extern static object get_symbol_addr(string sym_name);

        [Cli.BuiltinFunction("get_symbol_addr")]
        public extern static StaticArray get_static_array(string sym_name);

        [Cli.BuiltinFunction("get_arg")]
        public extern static ulong get_arg_U8(int arg_no);

        [Cli.BuiltinFunction("reinterpret_cast")]
        public extern static Multiboot.Header reinterpret_as_mboot(ulong addr);

        [Cli.BuiltinFunction("outb")]
        public extern static void port_out(ushort port, byte b);

        [Cli.BuiltinFunction("rdcr3")]
        public extern static ulong rdcr3();
        [Cli.BuiltinFunction("wrcr3")]
        public extern static void wrcr3(ulong v);

        [Cli.BuiltinFunction("invlpg")]
        public extern static void invlpg(ulong v);
    }
}
