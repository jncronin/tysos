using System;
using System.Collections.Generic;
using System.Text;

namespace testca
{
    interface IVideo : IOutputStream
    {
        int GetX();
        int GetY();
        void SetX(int x);
        void SetY(int y);
        int GetWidth();
        int GetHeight();

        void Clear();
        void UpdateCursor();
        void SetTextAttribute(byte attr);
    }
}
