using System;
using System.Collections.Generic;
using System.Text;

namespace testca
{
    interface ITextWriter
    {
        void Write(Char v);
        void Write(Char[] v);
        void Write(string v);
        void Write(ulong v);
    }
}
