using System;
using System.Collections.Generic;
using System.Text;

namespace testca
{
    interface IOutputStream
    {
        void Write(char ch);
        void Flush();
    }
}
