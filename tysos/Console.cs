using System;
using System.Collections.Generic;
using System.Text;

namespace testca
{
    class Console
    {
        static IOutputStream ostream;

        public static void Init(IOutputStream output_stream)
        {
            ostream = output_stream;
        }

        public static void Write(ulong v)
        {
            Formatter.Write(v, ostream);
        }
        public static void Write(char ch)
        {
            Formatter.Write(ch, ostream);
        }
        public static void Write(ulong v, string fmt)
        {
            Formatter.Write(v, fmt, ostream);
        }
        public static void Write(string fmt, params object[] p)
        {
            Formatter.Write(fmt, ostream, p);
        }
        public static void Write(string str)
        {
            Formatter.Write(str, ostream);
        }
        public static void WriteLine(string str)
        {
            Formatter.WriteLine(str, ostream);
        }
        public static void WriteLine()
        {
            Formatter.WriteLine(ostream);
        }
    }
}
